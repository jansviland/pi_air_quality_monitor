import asyncio
import os
import pprint
import serial
import datetime as dt
import requests
import portalocker
import urllib3
from dateutil import parser
from pytz import timezone
from typing import Optional
from typing import List

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

ser = serial.Serial("/dev/ttyUSB0")

tz = timezone("Etc/GMT-1")

CLIENT_ID = "raspberry-pi-jan"
CSV_PAYLOAD = "{pm2},{pm10},{client_id},{fromTime},{toTime}"
CSV_HEADER = "pm2,pm10,client_id,fromTime,toTime"
APIKEY = os.getenv(
    "XAPIKEY"
)  # use command export XAPIKEY=asdyoukeyhere to set the environment variable

STATION_ID_MILJODIR = 1178
PM10_TIMESERIES_ID = 4375
PM25_TIMESERIES_ID = 4376

# Create temp arrays with InputTimeValue objects
pm25_time_values = []
pm10_time_values = []


# Equivalent C# InputTimeValue class
class InputTimeValue:
    def __init__(self, from_time, to_time, value, data_coverage=None, instrument_flag=None):
        self.from_time = from_time
        self.to_time = to_time
        self.value = value
        self.dataCoverage = data_coverage if data_coverage is not None else -9900
        # self.instrument_flag = instrument_flag

    def __str__(self):
        return '{{"fromTime": "{}", "toTime": "{}", "value": {}, "dataCoverage": {}}}'.format(
            self.from_time.isoformat(),
            self.to_time.isoformat(),
            self.value,
            self.dataCoverage,
            # self.instrument_flag if self.instrument_flag is not None else ""
        )

    def __repr__(self):
        return self.__str__()

    def to_dict(self):
        return {
            "fromTime": self.from_time.isoformat(),
            "toTime": self.to_time.isoformat(),
            "value": self.value,
            "dataCoverage": self.dataCoverage,
            # "instrumentFlag": self.instrument_flag,
        }


class InputTimeSeries:
    def __init__(self, id, component, equipment_serial_number, time_values):
        self.id = id
        self.component = component
        self.serialNumber = equipment_serial_number
        self.timeValues = time_values

    def __str__(self):
        return '{{"id": {}, "component": "{}", "serialNumber": "{}", "timeValues": {}}}'.format(
            self.id,
            self.component,
            self.serialNumber,
            [tv.__str__() for tv in self.timeValues]
        )

    def to_dict(self):
        return {
            "id": self.id,
            "component": self.component,
            "serialNumber": self.serialNumber,
            "timeValues": [tv.to_dict() for tv in self.timeValues],
        }


class TimeSeriesLastReceived:
    def __init__(self, time_series_id: int, component: str, last_from_time_received: Optional[str] = None):
        self.timeSeriesId = time_series_id
        self.component = component
        # self.aqtsGuid = aqts_guid if aqts_guid else uuid.uuid4()
        self.lastFromTimeReceived = last_from_time_received if last_from_time_received else ""


def pretty_print(obj):
    if isinstance(obj, list):
        for item in obj:
            pprint.pprint(item.__dict__)
    else:
        pprint.pprint(obj.__dict__)


def save_data_to_file(currentTime, data):
    year, month, day = currentTime.year, currentTime.month, currentTime.day
    base_path = f"{year}/{month:02d}/{day:02d}"

    os.makedirs(base_path, exist_ok=True)
    file_path = os.path.join(base_path, "measurements.csv")

    timeout = 20  # Timeout in seconds

    try:
        # Check if the file exists, and write the header if it doesn't
        if not os.path.exists(file_path):
            with portalocker.Lock(file_path, mode="w", timeout=timeout) as f:
                f.write(CSV_HEADER)
                f.write("\n")

        # Append the data to the file
        with portalocker.Lock(file_path, mode="a", timeout=timeout) as f:
            f.write(data)
            f.write("\n")

        print(f'Saved data: "{data}" to file: "{file_path}"')

    except portalocker.exceptions.LockTimeout:
        print(f"Could not acquire lock on {file_path} within {timeout} seconds")


# read measurements from file, datetime is used to get measurement for that specific day
def get_all_measurements_taken(year, month, day):
    base_path = f"{year}/{month:02d}/{day:02d}"
    file_path = os.path.join(base_path, "measurements.csv")

    try:
        with open(file_path, "r") as f:
            content = f.readlines()
            print(f'Read data from file: "{file_path}"')

        pm10_time_values.clear()
        pm25_time_values.clear()

        # parse the data
        for line in content:
            # skip the header
            if line.startswith("pm2"):
                continue

            values = line.split(",")

            from_time = parser.parse(values[3])
            to_time = parser.parse(values[4])

            # Calculate the total seconds of measurement
            total_seconds = (to_time - from_time).total_seconds()

            # Corrected formula for coverage
            coverage = int(total_seconds / 60 * 100)

            pm10_time_values.append(InputTimeValue(from_time, to_time, float(values[0]), coverage))
            pm25_time_values.append(InputTimeValue(from_time, to_time, float(values[1]), coverage))

    except FileNotFoundError:

        pm10_time_values.clear()
        pm25_time_values.clear()

        print(f"File not found: {file_path}")


def save_last_sent_time_to_file(timeseriesId, lastSent):
    # store last successful sent datetime
    # name should be "nilu-station-" + stationId + "-timeseries-" + timeSeriesId + "-lastSent.txt";
    # overwrite the file if it exists
    file_name = f"miljodir-station-{STATION_ID_MILJODIR}-timeseries-{timeseriesId}-lastSent.txt"
    print(f"Saving last sent time {lastSent}, to file: {file_name}")
    with open(file_name, "w") as f:
        f.write(lastSent.isoformat())


def read_last_sent_time_from_file(timeSeriesId):
    now_winter_time = dt.datetime.now(tz)
    lastSent = now_winter_time - dt.timedelta(days=2)  # default to 2 days ago

    try:
        lastSentString = open(f"miljodir-station-{STATION_ID_MILJODIR}-timeseries-{timeSeriesId}-lastSent.txt",
                              "r").read().strip()
        lastSent = dt.datetime.fromisoformat(lastSentString)

    except FileNotFoundError:
        print(f"File not found: miljodir-station-{STATION_ID_MILJODIR}-timeseries-{timeSeriesId}-lastSent.txt")

    print(f"Last sent time for timeseries {timeSeriesId}: {lastSent}")

    return lastSent


def parse_last_received_array(response_json: List[dict]) -> List[TimeSeriesLastReceived]:
    time_series_list = []
    for item in response_json:
        time_series = TimeSeriesLastReceived(
            time_series_id=item['timeSeriesId'],
            component=item['component'],
            # aqts_guid=item['AqtsGuid'],
            last_from_time_received=item.get('lastFromTimeReceived', '')
        )
        time_series_list.append(time_series)
    return time_series_list


def get_last_received_miljodir():
    try:
        # get access_token
        tokenResponse = requests.get(
            f"https://luftmalinger-api.d.aks.miljodirektoratet.no/poc/maskinporten-test/token",
            headers={"X-API-Key": APIKEY, "Content-Type": "application/json"},
            verify=False,
        )

        print(f"MiljoDir Response status code: {tokenResponse.status_code}")
        print(f"Token response: {tokenResponse.json()}")

        access_token = tokenResponse.json()["access_token"]

        response = requests.get(
            f"https://luftmalinger-api.d.aks.miljodirektoratet.no/poc/stations/{STATION_ID_MILJODIR}/last-received",
            headers={"Authorization": "Bearer " + access_token, "Content-Type": "application/json"},
            verify=False,
        )
        print(f"MiljoDir Response status code: {response.status_code}")

        if response.status_code == 200:

            # parse array with TimeSeriesLastReceived objects
            time_series_json = response.json()
            time_series_objects = parse_last_received_array(time_series_json)

            lastReceived = dt.datetime.now(tz) - dt.timedelta(days=4)  # default to 4 days ago

            # Now you can work with the list of TimeSeriesLastReceived objects
            for time_series in time_series_objects:
                print("Time Series ID:", time_series.timeSeriesId)
                print("Component:", time_series.component)
                # print("AqtsGuid:", time_series.aqtsGuid)
                print("LastFromTimeReceived:", time_series.lastFromTimeReceived)

                parsedLastFromReceived = dt.datetime.fromisoformat(time_series.lastFromTimeReceived)
                if (parsedLastFromReceived > lastReceived):
                    lastReceived = parsedLastFromReceived

        return lastReceived

    except Exception as e:
        print(f"Exception: {e}")


def send_data_to_miljodir():
    # TODO: filter out invalid data, where dataCoverage is less than 10 (10%)
    # filtered_pm10_time_values = []
    # for tv in pm10_time_values:
    #     if tv.dataCoverage > 10:
    #         pm10_time_values.append(tv)
    # 
    # filtered_pm25_time_values = []
    # for tv in pm25_time_values:
    #     if tv.dataCoverage > 10:
    #         filtered_pm25_time_values.append(tv)

    # Create the JSON payload
    pm10_timeseries = InputTimeSeries(
        PM10_TIMESERIES_ID, "PM10", CLIENT_ID, pm10_time_values
    )
    pm25_timeseries = InputTimeSeries(
        PM25_TIMESERIES_ID, "PM2.5", CLIENT_ID, pm25_time_values
    )

    combined = [pm10_timeseries, pm25_timeseries]

    print("")
    print(f"request:")
    pretty_print(combined)
    print("")

    # Convert your requests to dictionaries
    pm10_request_dict = pm10_timeseries.to_dict()
    pm25_request_dict = pm25_timeseries.to_dict()

    combined_dict = [pm10_request_dict, pm25_request_dict]

    try:

        # get access_token
        tokenResponse = requests.get(
            f"https://luftmalinger-api.d.aks.miljodirektoratet.no/poc/maskinporten-test/token",
            headers={"X-API-Key": APIKEY, "Content-Type": "application/json"},
            verify=False,
        )

        print(f"MiljoDir Response status code: {tokenResponse.status_code}")
        print(f"Token response: {tokenResponse.json()}")

        access_token = tokenResponse.json()["access_token"]

        response = requests.post(
            f"https://luftmalinger-api.d.aks.miljodirektoratet.no/poc/stations/{STATION_ID_MILJODIR}/measurement",
            headers={"Authorization": "Bearer " + access_token, "Content-Type": "application/json"},
            json=combined_dict,
            verify=False,
        )
        print(f"MiljoDir Response status code: {response.status_code}")

        lastFromTime = pm10_time_values[pm10_time_values.__len__() - 1].from_time
        if response.status_code == 200:
            save_last_sent_time_to_file(PM10_TIMESERIES_ID, lastFromTime)
            save_last_sent_time_to_file(PM25_TIMESERIES_ID, lastFromTime)

        response = requests.post(
            f"https://192.168.1.12:7061/poc/stations/1179/measurement",
            headers={"X-API-Key": APIKEY, "Content-Type": "application/json"},
            json=combined_dict,
            verify=False,
        )
        print(f"Local Network: Response status code: {response.status_code}")
        print("")

    except Exception as e:
        print(f"Exception: {e}")


def send_data_to_api():
    # Check last sent time

    # get last received from Miljødirektoratet
    pm10_last_sent = get_last_received_miljodir()

    # pm10_last_sent = read_last_sent_time_from_file(PM10_TIMESERIES_ID)
    # print(f"Last sent time for PM10: {pm10_last_sent}")

    if pm10_last_sent is not None:
        # compare last sent and pm10_time_values[0].from_time
        diff = pm10_time_values[0].from_time - pm10_last_sent
        diffMinutes = diff.total_seconds() / 60
        print(f"Pm10 last sent: {pm10_last_sent}, diff: {diffMinutes} minutes")

        # Convert to dates to remove the time component
        date1 = pm10_last_sent.date()
        date2 = pm10_time_values[0].from_time.date()

        # Check if it's the same day (ignoring hours)
        if date1 == date2:
            print("Last sent and last measurement are on the same day.")

            if diffMinutes > 20:
                print("More than 30 minutes since last sent, get all data from today and resend")

                # read measurements from file, will get all data for today, and send up to 24 hours of minute data
                today = dt.datetime.now(tz)
                year, month, day = today.year, today.month, today.day

                get_all_measurements_taken(year, month, day)
                send_data_to_miljodir()

            else:
                # just send the last measurement
                send_data_to_miljodir()
        else:
            print("The dates are not on the same day.")

            # Calculate the exact day difference
            day_difference = (date2 - date1).days
            print(f"Exact day difference: {day_difference} day(s)")

            # if it's not the same day, get the day difference and get the earlier data
            # if it's 4 days ago, we go 4 days back and send all data from that day
            # next check it will hopefully say last sent 3 days ago, and send all data from that day etc
            # until all previous data is sent
            if pm10_time_values[0].from_time.day != pm10_last_sent.day:
                print(f"Not the same day, get all data from {day_difference} day(s) ago")

                # read measurements from file, will get all data for yesterday, and send up to 24 hours of minute data
                previousDay = dt.datetime.now(tz) - dt.timedelta(days=day_difference)
                year, month, day = previousDay.year, previousDay.month, previousDay.day

                print(f"Get all measurements from: {year}-{month}-{day}")
                get_all_measurements_taken(year, month, day)

                # TODO: make sure there is no gap in the data, for every hour missing, fill with -9900

                send_data_to_miljodir()

                # manually set last sent time to the from time of the last measurement
                # to avoid sending the same data again
                nextDay = previousDay + dt.timedelta(days=1)

                save_last_sent_time_to_file(PM25_TIMESERIES_ID, nextDay)
                save_last_sent_time_to_file(PM10_TIMESERIES_ID, nextDay)


async def main():
    if APIKEY is None:
        print("XAPIKEY environment variable not set")
        exit(1)

    while True:

        # Get the current time in the specified timezone
        now = dt.datetime.now(tz)

        # Calculate the seconds until the next whole minute
        seconds_until_next_minute = 60 - now.second
        print("Seconds until the next whole minute:", seconds_until_next_minute)

        # Wait until the start of the next whole minute
        await asyncio.sleep(seconds_until_next_minute)

        # Start time of measurement (aligned with the whole minute)
        from_time = dt.datetime.now(tz)
        from_time = from_time.replace(second=0, microsecond=0)

        print("from_time:", from_time)
        print("Collecting and processing data...")

        # [Your data collection and processing code goes here]
        data = []
        for index in range(0, 10):
            # TODO: handle exception
            # serial.serialutil.SerialException: device reports readiness to read but returned no data (device disconnected or multiple access on port?)

            datum = ser.read()
            data.append(datum)

        pmtwofive = int.from_bytes(b"".join(data[2:4]), byteorder="little") / 10
        pmten = int.from_bytes(b"".join(data[4:6]), byteorder="little") / 10

        to_time = from_time + dt.timedelta(minutes=1)

        # Calculate the total seconds of measurement
        # total_seconds = (dt.datetime.now(tz) - from_time).total_seconds()

        # Corrected formula for coverage
        # coverage = int(total_seconds / 60 * 100)

        coverage = 100

        print(
            f"FromTime: {from_time}, ToTime: {to_time}, Data points: PM2.5 = {pmtwofive}, PM10 = {pmten}, Coverage: {coverage}%"
        )

        # only save and send data if coverage is above 10%
        # if coverage > 10:

        pm10_time_values.append(InputTimeValue(from_time, to_time, pmten, coverage))
        pm25_time_values.append(InputTimeValue(from_time, to_time, pmtwofive, coverage))

        cvs = CSV_PAYLOAD.format(
            pm2=pmtwofive,
            pm10=pmten,
            client_id=CLIENT_ID,
            fromTime=from_time,
            toTime=to_time,
        )

        # TODO: do this as a background task, so we can continue to measure while saving data
        # Save data to file
        save_data_to_file(from_time, cvs)

        # else:
        #     print("Invalid data, coverage less than 10%, not saving to file")

        # when the lists contains x items, send the data to the API
        if pm10_time_values.__len__() >= 5:
            # only send between 08:00 - 16:00 monday - friday

            if 7 <= from_time.hour <= 15 and from_time.weekday() < 5:
                # if fromTime.hour >= 8 and fromTime.hour <= 20:
                # Send data to API
                # TODO: to this as a background task, so we can continue to measure while sending data
                send_data_to_api()

            else:
                # Handle gap in data when outside of working hours
                print("Not sending data to API, outside of working hours")

            # Clear the lists
            pm10_time_values.clear()
            pm25_time_values.clear()


if __name__ == "__main__":
    asyncio.run(main())
