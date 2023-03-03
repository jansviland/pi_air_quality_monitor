import datetime
import os
import asyncio
import time
from azure.iot.device.aio import IoTHubDeviceClient
import serial

ser = serial.Serial('/dev/ttyUSB0')

CLIENT_ID = "raspberry-pi-jan"
CONNECTION_STRING = os.getenv("IOTHUB_DEVICE_CONNECTION_STRING")
PAYLOAD = '{{"pm2": {pm2}, "pm10": {pm10}, "client_id": "{client_id}"}}'

async def main():

    # Create instance of the device client using the authentication provider
    device_client = IoTHubDeviceClient.create_from_connection_string(CONNECTION_STRING)

    # Connect the device client.
    await device_client.connect()

    while True:
        data = []
        for index in range(0,10):
            datum = ser.read()
            data.append(datum)

        pmtwofive = int.from_bytes(b''.join(data[2:4]), byteorder='little') / 10
        pmten = int.from_bytes(b''.join(data[4:6]), byteorder='little') / 10
        currentTime = datetime.datetime.now()

        print(f"Time: {currentTime}, Data point: pm25 = {pmtwofive}, pm10 = {pmten}, client_id = {CLIENT_ID}")
        data = PAYLOAD.format(pm2=pmtwofive, pm10=pmten, client_id=CLIENT_ID)

        # Send a message to the IoT hub
        print(f"Sending message: {data}")
        await device_client.send_message(data)

        time.sleep(10)

    # finally, shut down the client
    await device_client.shutdown()

if __name__ == "__main__":
    asyncio.run(main())