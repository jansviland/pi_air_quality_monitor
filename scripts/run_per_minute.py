import asyncio
import datetime as dt
from pytz import timezone


async def main():
    # Define the timezone: UTC+1
    tz = timezone("Etc/GMT-1")

    while True:
        # Get the current time in the specified timezone
        now = dt.datetime.now(tz)

        # Calculate the seconds until the next whole minute
        seconds_until_next_minute = 60 - now.second
        print("Seconds until the next whole minute:", seconds_until_next_minute)

        # Wait until the start of the next whole minute
        await asyncio.sleep(seconds_until_next_minute)

        # Start time of measurement (aligned with the whole minute)
        from_time = dt.datetime.now(tz)
        from_time = from_time.replace(second=0, microsecond=0)

        # [Your data collection and processing code goes here]
        # Make sure to adjust it to use 'from_time' as the start time.
        print("Collecting and processing data...")
        print("from_time:", from_time)

        await asyncio.sleep(30)

        to_time = dt.datetime.now(tz)
        to_time = to_time.replace(second=0, microsecond=0)

        # Calculate the total seconds of measurement
        total_seconds = (to_time - from_time).total_seconds()

        # Corrected formula for coverage
        coverage = int(total_seconds / 60 * 100)

        print("Coverage:", coverage, "%")
        print("to_time:", to_time)

        # Calculate the wait time until the next minute starts (about 60 seconds minus any processing time)
        # processing_end_time = dt.datetime.now(tz)
        # processing_duration = (processing_end_time - from_time).total_seconds()
        # await asyncio.sleep(max(60 - processing_duration, 0))


if __name__ == "__main__":
    asyncio.run(main())
