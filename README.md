# Raspberry Pi Air Quality Monitor

A simple air quality monitoring service for the Raspberry Pi. It uses a Nova PM Sensor (SDS011) to measure the air quality and sends the data to Azure IoT Hub.

To set this up you need 

- Raspberry Pi
- Nova PM Sensor (SDS011)
- Container to keep the sensor in

Here is how it looks like:

![image](wiki/air-quality-monitor.jpg)

## Install required packages for Raspberry Pi


### Install Python packages (for sendTestDataToAzure.py)
```bash
python -m pip install --upgrade pip
pip install portalocker
pip install azure-iot-device
pip install pyserial
pip install asyncio

```

### Install .NET 6 (for AirQuality.Console)


```bash
wget -O - https://raw.githubusercontent.com/pjgpetecodes/dotnet6pi/master/install.sh | sudo bash
```

## Setup

Set access key for Azure IoT Hub:

![image](wiki/azure-iot-hub-device-connection-string.png)

```bash
export IOTHUB_DEVICE_CONNECTION_STRING='HostName=air-monitor-hub.azure-devices.net;DeviceId=measuring-device-id;SharedAccessKey=XXXXXXXXX_YOUR_ACCESS_KEY_XXXXXX
```

## Test

To test the sensor, run the following command:

```bash
python /home/pi/git/pi_air_quality_monitor/scripts/getMeasurement.py
```

This should print out something like:

```json
{
  "device_id": "your device id",
  "pm10": 10.8,
  "pm2.5": 4.8
}
```

## Run continuously in the background and send data to azure every minute

To run, use the run command:

```bash
nohup python -u /home/pi/git/pi_air_quality_monitor/scripts/sendTestDataToAzure.py >> azurelog.log &
```

## Stop script

To kill you script, you can use ps -aux and kill commands.

```bash
ps -aux | grep python
```

This should show something like this:

```
pi       23338  0.0  2.2  93084 21384 ?        Sl   Feb10  20:14 python -u /home/pi/git/pi_air_quality_monitor/scripts/sendTestDataToAzure.py
```

Then you can kill the process like this:

```bash
kill -9 23338
```



