import datetime
import os
import asyncio
import time
import serial
import portalocker

ser = serial.Serial('/dev/ttyUSB0')

CLIENT_ID = "raspberry-pi-jan"
JSON_PAYLOAD = '{{"pm2": {pm2}, "pm10": {pm10}, "client_id": "{client_id}"}}'
CSV_PAYLOAD = '{pm2},{pm10},{client_id},{time}'
CSV_HEADER = 'pm2,pm10,client_id,time'

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


async def main():

    while True:
        json = []
        for index in range(0,10):
            datum = ser.read()
            json.append(datum)

        # get the data from the sensor
        pmtwofive = int.from_bytes(b''.join(json[2:4]), byteorder='little') / 10
        pmten = int.from_bytes(b''.join(json[4:6]), byteorder='little') / 10
        currentTime = datetime.datetime.utcnow()

        print(f"Time: {currentTime}, Data point: pm25 = {pmtwofive}, pm10 = {pmten}, client_id = {CLIENT_ID}")

        # Create the JSON and CSV payloads
        # json = JSON_PAYLOAD.format(pm2=pmtwofive, pm10=pmten, client_id=CLIENT_ID)
        cvs = CSV_PAYLOAD.format(pm2=pmtwofive, pm10=pmten, client_id=CLIENT_ID, time=currentTime)

        # Save data to file
        save_data_to_file(currentTime, cvs)

if __name__ == "__main__":
    asyncio.run(main())

