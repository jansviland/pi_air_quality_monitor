import serial, time, datetime
import requests
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
        return json.dumps(self, default=lambda o: o.__dict__, sort_keys=True, indent=4)


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

	if (pm10_time_values.count < 5):

		# Add the data to the lists, when the lists contain 5 items (5 minutes), send the data to the API
		pm10_time_values.append(TimeValue(fromTime, toTime, pmten))
		pm25_time_values.append(TimeValue(fromTime, toTime, pmtwofive))

	else:
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





