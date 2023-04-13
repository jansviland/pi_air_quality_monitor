#!/bin/sh

# Store the date for yesterday
yesterday=$(date -d yesterday "+%Y/%m/%d")

# Change to the application directory
#cd ~/git/pi_air_quality_monitor

# Build the .NET application
#dotnet build src/AirQuality.Console/AirQuality.Console.csproj

# Change to the application output directory (where the .NET application is built)
cd ~/git/pi_air_quality_monitor/src/AirQuality.Console/bin/Debug/net6.0

# Run the .NET application with the yesterday's date
dotnet AirQuality.Console.dll ~/${yesterday}/measurements.csv >> ~/AirQuality.Console.log 2>&1
