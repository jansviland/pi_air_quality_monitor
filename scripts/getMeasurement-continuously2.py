import serial, time, datetime

ser = serial.Serial('/dev/ttyUSB0')

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
