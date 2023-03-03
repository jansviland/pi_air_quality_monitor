# Raspberry Pi Air Quality Monitor

A simple air quality monitoring service for the Raspberry Pi.

## Setup

Set access key for Azure IoT Hub:

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



