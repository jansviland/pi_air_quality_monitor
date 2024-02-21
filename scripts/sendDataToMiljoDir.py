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

# Create temp arrays with TimeValue objects
pm25_time_values = []
pm10_time_values = []


# Equivalent of your C# TimeValue class
class TimeValue:
    def __init__(self, from_time, to_time, value, validity=None, instrument_flag=None):
        self.from_time = from_time
        self.to_time = to_time
        self.value = value
        self.validity = validity if validity is not None else -9900
        self.instrument_flag = instrument_flag

    def __str__(self):
        return f"TimeValue(fromTime={self.from_time}, toTime={self.to_time}, value={self.value}, validity={self.validity}, instrumentFlag={self.instrument_flag})"

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


# Equivalent of your C# RawValueRequest class
class RawValueRequest:
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
            "timeValues": [tv.to_dict() for tv in self.time_values],
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


def send_data_to_api():

    # Check last sent time
    pm10_last_sent = read_last_sent_time_from_file(PM10_TIMESERIES_ID)
    print(f"Last sent time for PM10: {pm10_last_sent}")

    pm25_last_sent = read_last_sent_time_from_file(PM25_TIMESERIES_ID)
    print(f"Last sent time for PM2.5: {pm25_last_sent}")

    # Create the JSON payload
    pm10_request = RawValueRequest(
        PM10_TIMESERIES_ID, "PM10", CLIENT_ID, pm10_time_values
    )
    pm25_request = RawValueRequest(
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
        response = requests.post(
            f"https://luftmalinger-api.d.aks.miljodirektoratet.no/poc/stations/{STATION_ID}/measurement",
            headers={"X-API-Key": APIKEY, "Content-Type": "application/json"},
            json=combined_dict,
            verify=False,
        )
        print(f"MiljoDir Response status code: {response.status_code}")

        if response.status_code == 200:
            save_last_sent_time_to_file(combined)

        response = requests.post(
            f"https://192.168.1.12:7061/poc/stations/{STATION_ID}/measurement",
            headers={"X-API-Key": APIKEY, "Content-Type": "application/json"},
            json=combined_dict,
            verify=False,
        )
        print(f"Local Network: Response status code: {response.status_code}")
        print("")

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

        pm10_time_values.append(TimeValue(from_time, to_time, pmten, coverage))
        pm25_time_values.append(TimeValue(from_time, to_time, pmtwofive, coverage))

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
