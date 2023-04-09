import datetime
import os
import asyncio
import time
from azure.iot.device.aio import IoTHubDeviceClient
import serial

ser = serial.Serial('/dev/ttyUSB0')

CLIENT_ID = "raspberry-pi-jan"
CONNECTION_STRING = os.getenv("IOTHUB_DEVICE_CONNECTION_STRING")
JSON_PAYLOAD = '{{"pm2": {pm2}, "pm10": {pm10}, "client_id": "{client_id}"}}'
CSV_PAYLOAD = '{pm2},{pm10},{client_id},{time}'
CSV_HEADER = 'pm2,pm10,client_id,time'

def save_data_to_file(currentTime, data):

    year, month, day = currentTime.year, currentTime.month, currentTime.day
    base_path = f"{year}/{month:02d}/{day:02d}"

    os.makedirs(base_path, exist_ok=True)
    file_path = os.path.join(base_path, "measurements.csv")

    # Check if the file exists, and write the header if it doesn't
    if not os.path.exists(file_path):
        with open(file_path, "w") as f:
            f.write(CSV_HEADER)
            f.write("\n")

    with open(file_path, "a") as f:
        f.write(data)
        f.write("\n")

    print(f"Saved data: \"{data}\" to file: \"{file_path}\"")

async def main():

    # Create instance of the device client using the authentication provider
    device_client = IoTHubDeviceClient.create_from_connection_string(CONNECTION_STRING)

    # Connect the device client.
    await device_client.connect()

    while True:
        json = []
        for index in range(0,10):
            datum = ser.read()
            json.append(datum)

        # get the data from the sensor
        pmtwofive = int.from_bytes(b''.join(json[2:4]), byteorder='little') / 10
        pmten = int.from_bytes(b''.join(json[4:6]), byteorder='little') / 10
        currentTime = datetime.datetime.now()

        print(f"Time: {currentTime}, Data point: pm25 = {pmtwofive}, pm10 = {pmten}, client_id = {CLIENT_ID}")

        # Create the JSON and CSV payloads
        json = JSON_PAYLOAD.format(pm2=pmtwofive, pm10=pmten, client_id=CLIENT_ID)
        cvs = CSV_PAYLOAD.format(pm2=pmtwofive, pm10=pmten, client_id=CLIENT_ID, time=currentTime)

        # Save data to file
        save_data_to_file(currentTime, cvs)

        # Send a message to the IoT hub
        print(f"Sending message: {json}")
        await device_client.send_message(json)

        # time.sleep(10)

    # finally, shut down the client
    await device_client.shutdown()

if __name__ == "__main__":
    asyncio.run(main())

