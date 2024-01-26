import os
import pprint
import serial, time, datetime
import requests
import json
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)


ser = serial.Serial('/dev/ttyUSB0')

APIKEY = os.getenv("XAPIKEY")
# use command export XAPIKEY=asdyoukeyhere to set the environment variable

# Create TimeValue objects
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
        return f"TimeValue(from_time={self.from_time}, to_time={self.to_time}, value={self.value}, validity={self.validity}, instrument_flag={self.instrument_flag})"

    def __repr__(self):
        return self.__str__()

    def to_dict(self):
        return {
            "from_time": self.from_time.isoformat(),
            "to_time": self.to_time.isoformat(),
            "value": self.value,
            "validity": self.validity,
            "instrument_flag": self.instrument_flag
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
            "time_series_id": self.time_series_id,
            "component": self.component,
            "equipment_serial_number": self.equipment_serial_number,
            "time_values": [tv.to_dict() for tv in self.time_values]
        }

def pretty_print(obj):
    if isinstance(obj, list):
        for item in obj:
            pprint.pprint(item.__dict__)
    else:
        pprint.pprint(obj.__dict__)

while True:

	fromTime = datetime.datetime.now()  # Start time of measurement

	data = []
	for index in range(0,10):
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

	# when the lists contains x items, send the data to the API
	if (pm10_time_values.__len__() >= 5):

		# Create the JSON payload
		pm10_request = RawValueRequest("4375", "PM10", "raspberry-pi-jan", pm10_time_values)
		pm25_request = RawValueRequest("4376", "PM2.5", "raspberry-pi-jan", pm25_time_values)

		combined = [pm10_request, pm25_request]

		# print the JSON payload, pretty
		# print("")
		# print(f"PM10 request:")
		# pretty_print(pm10_request)

		# print("")
		# print(f"PM2.5 request:")
		# pretty_print(pm25_request)

		print("")
		print(f"request:")
		pretty_print(combined)
		print("")

		# TODO: Send the JSON payload to the API
#   curl -X 'POST' \
#   'https://localhost:7061/poc/stations/1179/measurement' \
#   -H 'accept: */*' \
#   -H 'X-API-Key: abc' \
#   -H 'Content-Type: application/json' \
#   -d '[
#   {
#     "timeSeriesId": 4375,
#     "component": "PM10",
#     "equipmentSerialNumber": "string",
#     "timeValues": [
#       {
#         "fromTime": "2024-01-26T15:42:36.054Z",
#         "toTime": "2024-01-26T15:43:36.054Z",
#         "value": 5,
#         "validity": 100,
#         "instrumentFlag": 0
#       }
#     ]
#   }
# ]'

		# Convert your requests to dictionaries
		pm10_request_dict = pm10_request.to_dict()
		pm25_request_dict = pm25_request.to_dict()

		combined_dict = [pm10_request_dict, pm25_request_dict]

		response = requests.post('https://192.168.1.12:7061/poc/stations/1179/measurement',
                           headers={'X-API-Key': APIKEY, 'Content-Type': 'application/json'}, json=combined_dict, verify=False)

		# Clear the lists
		pm10_time_values.clear()
		pm25_time_values.clear()





