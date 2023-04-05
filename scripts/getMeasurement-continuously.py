import serial, time, datetime

ser = serial.Serial('/dev/ttyUSB0')

while True:
	data = []
	for index in range(0,10):
		datum = ser.read()
		data.append(datum)

	pmtwofive = int.from_bytes(b''.join(data[2:4]), byteorder='little') / 10
	pmten = int.from_bytes(b''.join(data[4:6]), byteorder='little') / 10

	currentTime = datetime.datetime.now()

	print(f"Time: {currentTime}, Data point: pm25 = {pmtwofive}, pm10 = {pmten}")
	time.sleep(10)
