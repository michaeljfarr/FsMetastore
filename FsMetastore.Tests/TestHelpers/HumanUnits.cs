using System;

namespace FsMetastore.Tests.TestHelpers
{
    public static class HumanUnits
    {
        public static string GetHumanAmount(long count)
        {
            var countFactor = 1000;
            if (count < countFactor*10)
            {
                return $"{count}";
            }

            var numFactor = (float)count;
            
            numFactor = numFactor / countFactor;
            if (numFactor < countFactor)
            {
                return $"{numFactor:F2}k";
            }
            numFactor = numFactor / countFactor;
            if (numFactor < countFactor)
            {
                return $"{numFactor:F2}M";
            }
            numFactor = numFactor / countFactor;
            return $"{numFactor:F2}T";
        }
        
        public static string GetHumanDuration(TimeSpan timeSpan)
        {
            if (timeSpan.TotalMilliseconds < 1000)
            {
                return $"{timeSpan.TotalMilliseconds}ms";
            }
            if (timeSpan.TotalSeconds < 100)
            {
                return $"{timeSpan.TotalSeconds:F2}sec";
            }
            if (timeSpan.TotalMinutes < 100)
            {
                return $"{timeSpan.TotalMinutes:F2}min";
            }
            if (timeSpan.TotalHours < 24)
            {
                return $"{timeSpan.TotalHours:F2}hour";
            }
            return $"{timeSpan.TotalDays:F2}days";
        }

        public static string GetHumanSize(long numBytes)
        {
            var byteFactor = 1024;
            if (numBytes < byteFactor*10)
            {
                return $"{numBytes}B";
            }

            var numFactor = (float)numBytes;
            
            numFactor = numFactor / byteFactor;
            if (numFactor < byteFactor)
            {
                return $"{numFactor:F2}KB";
            }
            numFactor = numFactor / byteFactor;
            if (numFactor < byteFactor)
            {
                return $"{numFactor:F2}MB";
            }
            numFactor = numFactor / byteFactor;
            return $"{numFactor:F2}GB";
        }
        
        public static string GetHumanRate(int numItems, TimeSpan timeElapsed, string itemName)
        {
            if (numItems == 0)
            {
                return $"0 {itemName}";
            }

            var negative = numItems < 0;
            if (negative)
            {
                numItems = -numItems;
            }

            var prefix = negative ? "-" : "";

            var numPerMs = numItems / timeElapsed.TotalMilliseconds;
            if (numPerMs >= 5)//5 files/ms or more
            {
                return $"{prefix}{numPerMs:F2} {itemName}/ms";
            }

            var numPerSecond = numItems / timeElapsed.TotalSeconds;
            //5 file per ms is 4000 per sec
            if (numPerSecond >= 5)//1/sec to 4000/sec
            {
                return $"{prefix}{numPerSecond:F2} {itemName}/sec";
            }
            
            var numPerMinute = numItems / timeElapsed.TotalMinutes;
            //1/min is 60/sec
            if (numPerMinute > 1)//60/min to 4000/sec
            {
                return $"{prefix}{numPerMinute:F2} {itemName}/min";
            }
            
            var numPerHour = numItems / timeElapsed.TotalHours;
            if (numPerHour > 1)
            {
                return $"{prefix}{numPerHour:F2} {itemName}/hour";
            }

            var numPerDay = numItems / timeElapsed.TotalDays;
            return $"{prefix}{numPerDay:F2} {itemName}/day";
        }
    }
}