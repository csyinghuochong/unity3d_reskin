using System;

namespace ET
{
    public class TimeInfo : IDisposable
    {
        public static TimeInfo Instance = new TimeInfo();

        private int timeZone;

        public int TimeZone
        {
            get
            {
                return this.timeZone;
            }
            set
            {
                this.timeZone = value;
                dt = dt1970.AddHours(TimeZone);
            }
        }

        private readonly DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public long ServerMinusClientTime { private get; set; }

        public long FrameTime;

        private TimeInfo()
        {
            this.FrameTime = this.ClientNow();
        }

        public void Update()
        {
            this.FrameTime = this.ClientNow();
        }

        /// <summary> 
        /// 根据时间戳获取时间 
        /// </summary>  
        public DateTime ToDateTime(long timeStamp)
        {
            return dt.AddTicks(timeStamp * 10000);
        }

        /// <summary>
        /// 将年月整数转换为该月起始时间戳（毫秒）
        /// </summary>
        /// <param name="yearMonth">前4位年份，后2位月份，例如：202204、202512</param>
        /// <returns>该月1日0点0分0秒的毫秒时间戳</returns>
        public long GetMonthStartTimestamp(int yearMonth)
        {
            // 提取年份和月份
            int year = yearMonth / 100;
            int month = yearMonth % 100;
            
            // 验证月份有效性
            if (month < 1 || month > 12)
            {
                throw new ArgumentException($"无效的月份: {month}，月份必须在1-12之间");
            }
            
            // 创建该月第一天的日期（东八区）
            DateTime monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
            
            // 转换为UTC时间（因为时间戳通常基于UTC）
            DateTime utcTime = monthStart.ToUniversalTime();
            
            // 计算毫秒时间戳（从1970年1月1日开始的毫秒数）
            TimeSpan diff = utcTime - dt1970;
            long timestamp = (long)diff.TotalMilliseconds;
            
            return timestamp;
        }
        
        /// <summary>
        /// 将年月整数转换为该月最后一天的起始时间戳（毫秒）- 即最后一天的0点0分0秒
        /// </summary>
        /// <param name="yearMonth">前4位年份，后2位月份，例如：202204、202512</param>
        /// <returns>该月最后一天0点0分0秒的毫秒时间戳</returns>
        public long GetMonthEndTimestamp(int yearMonth)
        {
            // 提取年份和月份
            int year = yearMonth / 100;
            int month = yearMonth % 100;
    
            // 验证月份有效性
            if (month < 1 || month > 12)
            {
                throw new ArgumentException($"无效的月份: {month}，月份必须在1-12之间");
            }
    
            // 获取该月的天数
            int daysInMonth = DateTime.DaysInMonth(year, month);
    
            // 创建该月最后一天的日期（东八区）
            DateTime monthEnd = new DateTime(year, month, daysInMonth, 0, 0, 0, DateTimeKind.Local);
    
            // 转换为UTC时间（因为时间戳通常基于UTC）
            DateTime utcTime = monthEnd.ToUniversalTime();
    
            // 计算毫秒时间戳（从1970年1月1日开始的毫秒数）
            TimeSpan diff = utcTime - dt1970;
            long timestamp = (long)diff.TotalMilliseconds;
    
            return timestamp;
        }
        
        // 线程安全
        public long ClientNow()
        {
            return (DateTime.UtcNow.Ticks - this.dt1970.Ticks) / 10000;
        }

        public long ServerNow()
        {
            return ClientNow() + Instance.ServerMinusClientTime;
        }

        public long ClientFrameTime()
        {
            return this.FrameTime;
        }

        public long ServerFrameTime()
        {
            return this.FrameTime + Instance.ServerMinusClientTime;
        }

        public long Transition(DateTime d)
        {
            return (d.Ticks - dt.Ticks) / 10000;
        }

        public void Dispose()
        {
            Instance = null;
        }
    }
}