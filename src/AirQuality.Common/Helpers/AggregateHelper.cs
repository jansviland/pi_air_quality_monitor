using AirQuality.Common.Extensions;
using AirQuality.Common.Models;

namespace AirQuality.Common.Helpers;

public static class AggregateHelper
{
    /// <summary>
    /// Calculate the avg in a rolling/moving window time series, specifically, the Simple Moving Average (SMA)
    /// https://en.wikipedia.org/wiki/Moving_average
    /// 
    /// Data is divided into fixed-sized chunks, called windows, for example:
    /// 
    /// Consider a time series with 10 data points: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10].
    /// If we use a window size of 3, we can generate a set of features using the first three data points,
    /// then slide the window one time step forward and generate a new set of features using the next three data points,
    /// and so on. The resulting feature sets would be:
    /// 
    /// Window 1: [1]       (In this case, we only have 1/3 of the values, we then set "coverage" to 1/3 * 100 = 33.33%)
    /// Window 2: [1, 2]    (In this case, we only have 2/3 of the values, we then set "coverage" to 2/3 * 100 = 66.66%)
    /// Window 3: [1, 2, 3] (In this case, we have all the values, we then set "coverage" to 1 * 100 = 100%)
    /// Window 4: [2, 3, 4]
    /// Window 5: [3, 4, 5]
    /// Window X: [n, n + 1, n + 2]
    /// (...)
    ///
    /// We then take the Simple Moving Average (SMA) of the values in these windows.
    /// </summary>
    /// <param name="values">List of values.</param>
    /// <param name="window">Size of the window. Hour, 8-hour, 24-hour etc.</param>
    /// <param name="interval">How often values are measured. Minute, 10-minute, hour etc.</param>
    /// <returns>Simple Moving Average (SMA) of values within the window</returns>
    public static IEnumerable<Measurement> CalculateSimpleMovingAverage(IEnumerable<Measurement> values, TimeSpan window, TimeSpan interval)
    {
        // order list, can remove this if we can assume that values are ordered
        var orderedValues = values.OrderBy(x => x.UtcTime).ToList();

        // calculate the number of points in the window based on the interval
        // if interval is minute and window is hour, we expect 60 points in the window
        // if interval is hour and window is hour, we expect 1 point in the window
        // if interval is hour and window is day, we expect 24 points in the window
        // (the lowest interval we have is minute, so we can assume that the window is always larger than the interval)
        var windowSize = (int)window.TotalMinutes / (int)interval.TotalMinutes;

        // create a queue to hold the values in the window, then we can dequeue the first value when we add a new one
        var queue = new Queue<Measurement>();
        var result = new List<Measurement>();

        // loop through all values
        double sumPm2 = 0;
        double sumPm10 = 0;
        foreach (var point in orderedValues)
        {
            queue.Enqueue(point);
            sumPm2 += point.Pm2;
            sumPm10 += point.Pm10;

            // While points are in the queue that are outside the moving average window
            while (queue.Count > windowSize)
            {
                var temp = queue.Dequeue();

                sumPm2 -= temp.Pm2;
                sumPm10 -= temp.Pm10;
            }

            var averagePm2 = sumPm2 / queue.Count;
            var averagePm10 = sumPm10 / queue.Count;
            // var coverage = (double)queue.Count / windowSize * 100;

            result.Add(new Measurement
            {
                UtcTime = queue.Last().UtcTime,
                UnixTime = queue.Last().UnixTime,
                ClientId = queue.Last().ClientId,
                Pm2 = averagePm2,
                Pm10 = averagePm10,
            });
        }

        return result;
    }

    /// <summary>
    /// Calculate the average within each window in a time series. Unlike a moving average which slides over the data,
    /// this method partitions the data into distinct windows and computes the average for each window separately.
    /// The method takes into account any gaps in data within a window and calculates coverage as the percentage of non-missing values.
    ///
    /// For example, consider a time series with 10 hourly data points spanning two days.
    /// If we use a window size of 24 hours (1 day), we would have two windows.
    /// The first window contains the data points from the first day and the second window contains the data points from the second day.
    /// The method calculates the average for each day separately, taking into account if any hourly data points are missing.
    ///
    /// If data points are missing within a window, the coverage is calculated as the percentage of non-missing data points.
    /// For instance, if a 24-hour window only has 12 hourly data points, the coverage would be 50%.
    /// </summary>
    /// <param name="values">List of values in a time series.</param>
    /// <param name="window">The size of the window for which to calculate the average. The window could be an hour, a day, a week, etc.</param>
    /// <param name="interval">The interval at which values are measured, e.g., every minute, every 10 minutes, every hour, etc.</param>
    /// <returns>A list of Measurement objects, each representing the average value, the number of data points (count), and the coverage for each window.</returns>
    public static IEnumerable<Measurement> CalculateAverageInWindow(IEnumerable<Measurement> values, TimeSpan window, TimeSpan interval)
    {
        // order list, can remove this if we can assume that values are ordered
        var orderedValues = values.OrderBy(x => x.UtcTime).ToList();

        // calculate the number of points in the window based on the interval
        var windowSize = (int)window.TotalMinutes / (int)interval.TotalMinutes;

        // create a queue to hold the values in the window, then we can dequeue the first value when we add a new one
        var queue = new Queue<Measurement>();
        var result = new List<Measurement>();

        // loop through all values

        double sum = 0;
        DateTime? windowStart = null;

        foreach (var point in orderedValues)
        {
            // If we haven't started a window yet, start one
            if (windowStart == null)
            {
                windowStart = point.UtcTime;
            }
            // If the current point is outside the current window, calculate the average for the current window and start a new one
            else if (point.UtcTime - windowStart >= window)
            {
                var average = sum / queue.Count;
                // var coverage = (double)queue.Count / windowSize * 100;

                result.Add(new Measurement
                {
                    UtcTime = windowStart.Value,
                    Pm2 = average,

                    // TODO:
                    Pm10 = point.Pm10

                    // Count = queue.Count,
                    // Coverage = coverage
                });

                // Reset for the next window
                queue.Clear();
                sum = 0;
                windowStart = point.UtcTime;
            }

            queue.Enqueue(point);

            // TODO: add pm10
            sum += point.Pm2;
        }

        // Calculate the average for the last window
        if (queue.Count > 0)
        {
            var average = sum / queue.Count;
            // var coverage = (double)queue.Count / windowSize * 100;

            result.Add(new Measurement
            {
                UtcTime = windowStart.Value,
                Pm2 = average,

                // TODO:
                Pm10 = average

                // Count = queue.Count,
                // Coverage = coverage
            });
        }

        return result;
    }

    // public static IEnumerable<Measurement> GetYearAggregate(IEnumerable<Measurement> values, int timeStep = 3600)
    // {
    //     var groupedByYear = values.GroupBy(x => x.DateTime.Year);
    //     return groupedByYear.Select((x, _) =>
    //     {
    //         var year = x.Key;
    //         var secondsInAYear = DateTime.IsLeapYear(year) ? 31622400 : 31536000;
    //         var stepsInAYear = secondsInAYear / timeStep;
    //
    //         return new Measurement()
    //         {
    //             Value = x.Average(y => y.Value),
    //             DateTime = new DateTime(year, 1, 1),
    //             Count = x.Count(),
    //             Coverage = Math.Round((double)x.Count() / stepsInAYear * 100, 2)
    //         };
    //     }).OrderBy(x => x.DateTime);
    // }
    //
    // public static IEnumerable<Measurement> GetMonthAggregate(IEnumerable<Measurement> values, int timeStep = 3600)
    // {
    //     var result = new List<Measurement>();
    //
    //     var groupedByYear = values.GroupBy(x => x.DateTime.Year);
    //     foreach (var year in groupedByYear)
    //     {
    //         var groupedByMonth = year.GroupBy(x => x.DateTime.Month);
    //         foreach (var month in groupedByMonth)
    //         {
    //             var secondsInAMonth = DateTime.DaysInMonth(year.Key, month.Key) * 86400;
    //             var stepsInAMonth = secondsInAMonth / timeStep;
    //
    //             result.Add(new Measurement()
    //             {
    //                 Value = month.Average(y => y.Value),
    //                 DateTime = new DateTime(year.Key, month.Key, 1),
    //                 Count = month.Count(),
    //                 Coverage = Math.Round((double)month.Count() / stepsInAMonth * 100, 2)
    //             });
    //         }
    //     }
    //
    //     return result.OrderBy(x => x.DateTime);
    // }

    public static IEnumerable<Measurement> GetHourAggregate(IEnumerable<Measurement> values)
    {
        var result = new List<Measurement>();

        var groupedByYear = values.GroupBy(x => x.UtcTime.Year);
        foreach (var year in groupedByYear)
        {
            var groupedByMonth = year.GroupBy(x => x.UtcTime.Month);
            foreach (var month in groupedByMonth)
            {
                var groupedByDay = month.GroupBy(x => x.UtcTime.Date);
                foreach (var day in groupedByDay)
                {
                    var groupedByHour = day.GroupBy(x => x.UtcTime.Hour);
                    foreach (var hour in groupedByHour)
                    {
                        // const int secondsInDay = 86400;
                        // var stepsInADay = secondsInDay / timeStep;

                        var date = new DateTime(day.Key.Year, day.Key.Month, day.Key.Day, hour.Key, 0, 0, DateTimeKind.Utc);

                        result.Add(new Measurement
                        {
                            Pm2 = hour.Average(x => x.Pm2),
                            Pm10 = hour.Average(x => x.Pm10),
                            UtcTime = date,
                            UnixTime = date.ToUnixTime(),
                            ClientId = hour.First().ClientId
                            // Count = day.Count(),
                            // Coverage = timeStep == secondsInDay ? 100 : Math.Round((double)day.Count() / stepsInADay * 100, 2),
                        });
                    }
                }
            }
        }

        return result.OrderBy(x => x.UtcTime);
    }

    public static IEnumerable<Measurement> GetDayAggregate(IEnumerable<Measurement> values)
    {
        var result = new List<Measurement>();

        var groupedByYear = values.GroupBy(x => x.UtcTime.Year);
        foreach (var year in groupedByYear)
        {
            var groupedByMonth = year.GroupBy(x => x.UtcTime.Month);
            foreach (var month in groupedByMonth)
            {
                var groupedByDay = month.GroupBy(x => x.UtcTime.Date);
                foreach (var day in groupedByDay)
                {
                    // const int secondsInDay = 86400;
                    // var stepsInADay = secondsInDay / timeStep;

                    result.Add(new Measurement
                    {
                        Pm2 = day.Average(x => x.Pm2),
                        Pm10 = day.Average(x => x.Pm10),
                        UtcTime = day.Key,
                        UnixTime = day.Key.ToUnixTime(),
                        ClientId = day.First().ClientId
                        // Count = day.Count(),
                        // Coverage = timeStep == secondsInDay ? 100 : Math.Round((double)day.Count() / stepsInADay * 100, 2),
                    });
                }
            }
        }

        return result.OrderBy(x => x.UtcTime);
    }
}