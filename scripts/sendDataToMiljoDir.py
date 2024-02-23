import asyncio
import os
import pprint
import serial, time, datetime
import requests
import portalocker
import json
import urllib3

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

ser = serial.Serial("/dev/ttyUSB0")

CLIENT_ID = "raspberry-pi-jan"
CSV_PAYLOAD = "{pm2},{pm10},{client_id},{fromTime},{toTime}"
CSV_HEADER = "pm2,pm10,client_id,fromTime,toTime"
APIKEY = os.getenv(
    "XAPIKEY"
)  # use command export XAPIKEY=asdyoukeyhere to set the environment variable

STATION_ID = 1178
PM10_TIMESERIES_ID = 4375
PM25_TIMESERIES_ID = 4376

# Create temp arrays with InputTimeValue objects
pm25_time_values = []
pm10_time_values = []


# Equivalent of your C# InputTimeValue class
class InputTimeValue:
    def __init__(self, from_time, to_time, value, validity=None, instrument_flag=None):
        self.from_time = from_time
        self.to_time = to_time
        self.value = value
        self.validity = validity if validity is not None else -9900
        self.instrument_flag = instrument_flag

    def __str__(self):
        return f"InputTimeValue(fromTime={self.from_time}, toTime={self.to_time}, value={self.value}, validity={self.validity}, instrumentFlag={self.instrument_flag})"

    def __repr__(self):
        return self.__str__()

    def to_dict(self):
        return {
            "fromTime": self.from_time.isoformat(),
            "toTime": self.to_time.isoformat(),
            "value": self.value,
            "validity": self.validity,
            "instrumentFlag": self.instrument_flag,
        }


class InputTimeSeries:
    def __init__(self, time_series_id, component, equipment_serial_number, time_values):
        self.time_series_id = time_series_id
        self.component = component
        self.equipment_serial_number = equipment_serial_number
        self.time_values = time_values

    def to_dict(self):
        return {
            "id": self.time_series_id,
            "component": self.component,
            "serialNumber": self.equipment_serial_number,
            "InputTimeValues": [tv.to_dict() for tv in self.time_values],
        }


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
def read_data_from_file(dayOfMeasurement):
    year, month, day = dayOfMeasurement.year, dayOfMeasurement.month, dayOfMeasurement.day
    base_path = f"{year}/{month:02d}/{day:02d}"
    file_path = os.path.join(base_path, "measurements.csv")
    try:
        with open(file_path, "r") as f:
            data = f.read()
            print(f'Read data from file: "{file_path}"')
            print(data)

        pm10_time_values.clear()
        pm25_time_values.clear()

        # parse the data
        for line in data:
            print(line)
            # skip the header
            if line == CSV_HEADER:
                continue

            values = line.split(",")
            from_time = dayOfMeasurement.datetime.strptime(values[3], "%Y-%m-%dT%H:%M:%S")
            to_time = dayOfMeasurement.datetime.strptime(values[4], "%Y-%m-%dT%H:%M:%S")

            # Calculate the total seconds of measurement
            total_seconds = (to_time - from_time).total_seconds()

            # Corrected formula for coverage
            coverage = int(total_seconds / 60 * 100)

            print(
                f"FromTime: {from_time}, ToTime: {to_time}, Data points: PM2.5 = {values[0]}, PM10 = {values[1]}, Coverage: {coverage}%")

            pm10_time_values.append(InputTimeValue(from_time, to_time, float(values[0]), coverage))
            pm25_time_values.append(InputTimeValue(from_time, to_time, float(values[1]), coverage))

    except FileNotFoundError:
        print(f"File not found: {file_path}")


def save_last_sent_time_to_file(combined):
    # store last successful sent datetime
    # name should be "nilu-station-" + stationId + "-timeseries-" + timeSeriesId + "-lastSent.txt";
    # overwrite the file if it exists
    for request in combined:
        file_name = f"miljodir-station-{STATION_ID}-timeseries-{request.time_series_id}-lastSent.txt"
        print(f"Saving last sent time to file: {file_name}")
        with open(file_name, "w") as f:
            f.write(datetime.datetime.now().isoformat())


def read_last_sent_time_from_file(timeSeriesId):
    open(
        f"miljodir-station-{STATION_ID}-timeseries-{timeSeriesId}-lastSent.txt", "r"
    ).read()


# TODO: implement this, get jwt token and use this when sending measurements to the API
def get_token():
    response = requests.post(
        f"https://luftmalinger-api.d.aks.miljodirektoratet.no/poc/token",
        headers={"X-API-Key": APIKEY, "Content-Type": "application/json"},
        verify=False,
    )
    print(f"MiljoDir Response status code: {response.status_code}")

    if response.status_code == 200:
        return response.json()["token"]
    else:
        return None


def send_data_to_api():
    # Check last sent time
    pm10_last_sent = read_last_sent_time_from_file(PM10_TIMESERIES_ID)
    print(f"Last sent time for PM10: {pm10_last_sent}")

    # pm25_last_sent = read_last_sent_time_from_file(PM25_TIMESERIES_ID)
    # print(f"Last sent time for PM2.5: {pm25_last_sent}")

    if pm10_last_sent is not None:
        # compare last sent and pm10_time_values[0].from_time
        diff = pm10_time_values[0].from_time - pm10_last_sent
        diffMinutes = diff.total_seconds() / 60
        print(f"Pm10 last sent: {pm10_last_sent}, diff: {diffMinutes} minutes")

        if diffMinutes > 30:
            print("More than 30 minutes since last sent, get additional data")

            # send the last 48 hours of data
            today = datetime.datetime.now()

            # read measurements from file, will get all data for today, and send up to 24 hours of minute data
            read_data_from_file(today)

    # Create the JSON payload
    pm10_request = InputTimeSeries(
        PM10_TIMESERIES_ID, "PM10", CLIENT_ID, pm10_time_values
    )
    pm25_request = InputTimeSeries(
        PM25_TIMESERIES_ID, "PM2.5", CLIENT_ID, pm25_time_values
    )

    combined = [pm10_request, pm25_request]

    print("")
    print(f"request:")
    pretty_print(combined)
    print("")

    # Convert your requests to dictionaries
    pm10_request_dict = pm10_request.to_dict()
    pm25_request_dict = pm25_request.to_dict()

    combined_dict = [pm10_request_dict, pm25_request_dict]

    try:

        # get token
        tokenResponse = requests.post(
            f"https://luftmalinger-api.d.aks.miljodirektoratet.no/poc/stations/{STATION_ID}/poc/maskinporten-test/token",
            headers={"X-API-Key": APIKEY, "Content-Type": "application/json"},
            json=combined_dict,
            verify=False,
        )

        print(f"MiljoDir Response status code: {tokenResponse.status_code}")
        print(f"Token response: {tokenResponse.json()}")

        token = tokenResponse.json()["access_token"]

        response = requests.post(
            f"https://luftmalinger-api.d.aks.miljodirektoratet.no/poc/stations/{STATION_ID}/measurement",
            headers={"Authorization": token, "Content-Type": "application/json"},
            json=combined_dict,
            verify=False,
        )
        print(f"MiljoDir Response status code: {response.status_code}")

        if response.status_code == 200:
            save_last_sent_time_to_file(combined)

        # response = requests.post(
        #     f"https://192.168.1.12:7061/poc/stations/{STATION_ID}/measurement",
        #     headers={"X-API-Key": APIKEY, "Content-Type": "application/json"},
        #     json=combined_dict,
        #     verify=False,
        # )
        # print(f"Local Network: Response status code: {response.status_code}")
        # print("")

    except Exception as e:
        print(f"Exception: {e}")


async def main():
    if APIKEY is None:
        print("XAPIKEY environment variable not set")
        exit(1)

    while True:
        # Start time of measurement
        # fromTime = datetime.datetime.now()
        winter_time = datetime.timezone(
            datetime.timedelta(hours=1)
        )  # Use Norwegian winter time (UTC+1)
        from_time = datetime.datetime.now(winter_time)
        data = []

        for index in range(0, 10):
            # TODO: handle exception
            # serial.serialutil.SerialException: device reports readiness to read but returned no data (device disconnected or multiple access on port?)

            datum = ser.read()
            data.append(datum)

        pmtwofive = int.from_bytes(b"".join(data[2:4]), byteorder="little") / 10
        pmten = int.from_bytes(b"".join(data[4:6]), byteorder="little") / 10

        # to_time = datetime.datetime.now()  # End time of measurement
        winter_time = datetime.timezone(
            datetime.timedelta(hours=1)
        )  # Use Norwegian winter time (UTC+1)
        to_time = datetime.datetime.now(winter_time)

        # Calculate the total seconds of measurement
        total_seconds = (to_time - from_time).total_seconds()

        # Corrected formula for coverage
        coverage = int(total_seconds / 60 * 100)

        print(
            f"FromTime: {from_time}, ToTime: {to_time}, Data points: PM2.5 = {pmtwofive}, PM10 = {pmten}, Coverage: {coverage}%"
        )

        pm10_time_values.append(InputTimeValue(from_time, to_time, pmten, coverage))
        pm25_time_values.append(InputTimeValue(from_time, to_time, pmtwofive, coverage))

        cvs = CSV_PAYLOAD.format(
            pm2=pmtwofive,
            pm10=pmten,
            client_id=CLIENT_ID,
            fromTime=from_time,
            toTime=to_time,
        )

        # Save data to file
        # TODO: do this as a background task, so we can continue to measure while saving data
        save_data_to_file(from_time, cvs)

        # when the lists contains x items, send the data to the API
        if pm10_time_values.__len__() >= 5:
            # only send between 08:00 - 16:00 monday - friday
            if from_time.hour >= 7 and from_time.hour <= 15 and from_time.weekday() < 5:
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
