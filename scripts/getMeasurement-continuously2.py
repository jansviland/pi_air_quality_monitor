import pprint
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

    def __str__(self):
        return f"TimeValue(from_time={self.from_time}, to_time={self.to_time}, value={self.value}, validity={self.validity}, instrument_flag={self.instrument_flag})"

    def __repr__(self):
        return self.__str__()

# Equivalent of your C# RawValueRequest class
class RawValueRequest:
    def __init__(self, time_series_id, component, equipment_serial_number, time_values):
        self.time_series_id = time_series_id
        self.component = component
        self.equipment_serial_number = equipment_serial_number
        self.time_values = time_values

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

	# validity is the percentage of data points in a minute, 60 would be 100%, calculate the number of data points in a minute
	coverage = int(60 / (toTime - fromTime).total_seconds() * 100)

	print(f"FromTime: {fromTime}, ToTime: {toTime}, Data points: PM2.5 = {pmtwofive}, PM10 = {pmten}, Coverage: {coverage}%")

	pm10_time_values.append(TimeValue(fromTime, toTime, pmten, coverage))
	pm25_time_values.append(TimeValue(fromTime, toTime, pmtwofive, coverage))

	# when the lists contains x items, send the data to the API
	if (pm10_time_values.__len__() >= 5):

		# Create the JSON payload
		pm10_request = RawValueRequest("123", "PM10", "raspberry-pi-jan", pm10_time_values)
		pm25_request = RawValueRequest("123", "PM2.5", "raspberry-pi-jan", pm25_time_values)

		# print the JSON payload, pretty
		print("")
		print(f"PM10 request:")
		pretty_print(pm10_request)

		print("")
		print(f"PM2.5 request:")
		pretty_print(pm25_request)

		# TODO: Send the JSON payload to the API

		# Clear the lists
		pm10_time_values.clear()
		pm25_time_values.clear()





