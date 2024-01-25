import serial, time
import requests
from datetime import datetime  # Ensure this import is correct
import json

ser = serial.Serial('/dev/ttyUSB0')

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

# Equivalent of your C# RawValueRequest class
class RawValueRequest:
    def __init__(self, time_series_id, component, equipment_serial_number, time_values):
        self.time_series_id = time_series_id
        self.component = component
        self.equipment_serial_number = equipment_serial_number
        self.time_values = time_values

    def to_json(self):
        def default(o):
            # Ensure 'datetime' is correctly referenced
            if isinstance(o, datetime):
                return o.isoformat()  # Convert datetime to ISO format string
            elif hasattr(o, '__dict__'):
                return o.__dict__
            else:
                return str(o)  # Fallback for other types

        return json.dumps(self, default=default, indent=4) # Pretty print JSON


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

	print(f"FromTime: {fromTime}, ToTime: {toTime}, Data points: PM2.5 = {pmtwofive}, PM10 = {pmten}")

	pm10_time_values.append(TimeValue(fromTime, toTime, pmten))
	pm25_time_values.append(TimeValue(fromTime, toTime, pmtwofive))

	# when the lists contains x items, send the data to the API
	if (pm10_time_values.__len__() >= 2):

		# Create the JSON payload
		pm10_request = RawValueRequest("123", "PM10", "raspberry-pi-jan", pm10_time_values)
		pm25_request = RawValueRequest("123", "PM2.5", "raspberry-pi-jan", pm25_time_values)

		# print the JSON payload, pretty
		print(pm10_request.to_json())
		print(pm25_request.to_json())

		# TODO: Send the JSON payload to the API

		# Clear the lists
		pm10_time_values.clear()
		pm25_time_values.clear()





