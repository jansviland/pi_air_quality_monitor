import asyncio
import os
import pprint
import serial, time, datetime
import requests
import portalocker
import json
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

ser = serial.Serial('/dev/ttyUSB0')

CLIENT_ID = "raspberry-pi-jan"
CSV_PAYLOAD = '{pm2},{pm10},{client_id},{fromTime},{toTime}'
CSV_HEADER = 'pm2,pm10,client_id,fromTime,toTime'
APIKEY = os.getenv("XAPIKEY") # use command export XAPIKEY=asdyoukeyhere to set the environment variable

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
            "instrumentFlag": self.instrument_flag
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
            "timeSeriesId": self.time_series_id,
            "component": self.component,
            "equipmentSerialNumber": self.equipment_serial_number,
            "timeValues": [tv.to_dict() for tv in self.time_values]
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

        print(f"Saved data: \"{data}\" to file: \"{file_path}\"")

    except portalocker.exceptions.LockTimeout:
        print(f"Could not acquire lock on {file_path} within {timeout} seconds")


def send_data_to_api():

		# Create the JSON payload
		pm10_request = RawValueRequest("4375", "PM10", CLIENT_ID, pm10_time_values)
		pm25_request = RawValueRequest("4376", "PM2.5", CLIENT_ID, pm25_time_values)

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

			response = requests.post('https://luftmalinger-api.d.aks.miljodirektoratet.no/poc/stations/1179/measurement', headers={'X-API-Key': APIKEY, 'Content-Type': 'application/json'}, json=combined_dict, verify=False)
			# response = requests.post('https://192.168.1.12:7061/poc/stations/1179/measurement', headers={'X-API-Key': APIKEY, 'Content-Type': 'application/json'}, json=combined_dict, verify=False)

			print(f"Response status code: {response.status_code}")
			print(f"Response content: {response.content}")

		except Exception as e:
			print(f"Exception: {e}")

async def main():

	while True:

		fromTime = datetime.datetime.now()  # Start time of measurement

		data = []
		for index in range(0,10):

			# TODO: handle exception
			# serial.serialutil.SerialException: device reports readiness to read but returned no data (device disconnected or multiple access on port?)
			datum = ser.read()
			data.append(datum)

		pmtwofive = int.from_bytes(b''.join(data[2:4]), byteorder='little') / 10
		pmten = int.from_bytes(b''.join(data[4:6]), byteorder='little') / 10

		# currentTime = datetime.datetime.now()
		toTime = datetime.datetime.now()  # End time of measurement

		# Calculate the total seconds of measurement
		total_seconds = (toTime - fromTime).total_seconds()

		# Corrected formula for coverage
		coverage = int(total_seconds / 60 * 100)

		print(f"FromTime: {fromTime}, ToTime: {toTime}, Data points: PM2.5 = {pmtwofive}, PM10 = {pmten}, Coverage: {coverage}%")

		pm10_time_values.append(TimeValue(fromTime, toTime, pmten, coverage))
		pm25_time_values.append(TimeValue(fromTime, toTime, pmtwofive, coverage))

		cvs = CSV_PAYLOAD.format(pm2=pmtwofive, pm10=pmten, client_id=CLIENT_ID, fromTime=fromTime, toTime=toTime)

		# Save data to file
		save_data_to_file(fromTime, cvs)

		# only send between 08:30 - 15:30 monday - friday

		# when the lists contains x items, send the data to the API
		if (pm10_time_values.__len__() >= 5):

			if (fromTime.hour >= 8 and fromTime.hour <= 20):
			# if (fromTime.hour >= 8 and fromTime.hour <= 15 and fromTime.weekday() < 5):

				# Send data to API
				send_data_to_api()

			else:

				print("Not sending data to API, outside of working hours")

			# Clear the lists
			pm10_time_values.clear()
			pm25_time_values.clear()

if __name__ == "__main__":
    asyncio.run(main())






