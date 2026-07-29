using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLog.Model
{
    /// <summary>
    /// 频段频率区间 MHz
    /// </summary>
    public readonly struct FrequencyRange
    {
        public double MinMhz { get; }
        public double MaxMhz { get; }

        public FrequencyRange(double minMhz, double maxMhz)
        {
            MinMhz = minMhz;
            MaxMhz = maxMhz;
        }

        /// <summary>
        /// 判断目标频率(MHz)是否在此区间
        /// </summary>
        public bool IsInRange(double freqMhz)
        {
            return freqMhz >= MinMhz && freqMhz <= MaxMhz;
        }
    }
}
