using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLog.Model
{
    public static class RadioValues
    {
        /// <summary>
        /// 频率区间模型 - 存储频段的上下限（单位：MHz）
        /// </summary>
        public class FrequencyRange
        {
            public double LowerLimit { get; }  // 频率下限（MHz）
            public double UpperLimit { get; }  // 频率上限（MHz）

            public FrequencyRange(double lowerLimit, double upperLimit)
            {
                LowerLimit = lowerLimit;
                UpperLimit = upperLimit;
            }

            /// <summary>
            /// 检查指定频率是否在当前区间内
            /// </summary>
            public bool Contains(double frequencyMhz)
            {
                return frequencyMhz >= LowerLimit - double.Epsilon &&
                       frequencyMhz <= UpperLimit + double.Epsilon;
            }

            public override string ToString()
            {
                return $"{LowerLimit} - {UpperLimit} MHz";
            }
        }

        #region 无线电相关参数
        // C# 字符串数组 - SSTV全波段常用频点（全球通用）
        readonly static public string[] sstvCommonFrequencies = new string[]
        {
            "1.890MHz",    // 160米 - SSTV呼叫
            "3.845MHz",    // 80米 - 主呼叫（美洲/通用）
            "7.171MHz",    // 40米 - 主呼叫（美洲）
            "7.170MHz",    // 40米 - 主呼叫（亚非）
            "10.132MHz",   // 30米 - 窄带SSTV
            "14.230MHz",   // 20米 - 全球最活跃主呼叫
            "18.117MHz",   // 17米 - 常规通联
            "21.340MHz",   // 15米 - 主呼叫
            "24.927MHz",   // 12米 - 常规通联
            "28.680MHz",   // 10米 - 主呼叫
            "50.680MHz",   // 6米 - FM呼叫
            "144.195MHz",  // 2米 - SSB SSTV
            "144.500MHz",  // 2米 - FM常规通联
            "145.800MHz"   // 2米 - ISS卫星SSTV下行


        };

        //

        // FT8常用热门频点
        readonly public static string[] Ft8CommonFrequencies = new string[]
        {
        "1.840MHz",    // 160米 - FT8主频点
        "3.570MHz",    // 80米 - FT8主频点
        "5.357MHz",    // 60米 - FT8专用（美洲/通用）
        "7.074MHz",    // 40米 - FT8全球核心频点（最活跃）
        "10.136MHz",   // 30米 - FT8主频点
        "14.074MHz",   // 20米 - FT8全球核心频点
        "18.095MHz",   // 17米 - FT8主频点
        "21.074MHz",   // 15米 - FT8主频点
        "24.915MHz",   // 12米 - FT8主频点
        "28.074MHz",   // 10米 - FT8主频点
        "50.313MHz",   // 6米 - FT8主频点
        "144.174MHz",  // 2米 - VHF FT8主频点
        "432.174MHz"   // 70厘米 - UHF FT8主频点
        };

        // FT4常用热门频点
        readonly public static string[] Ft4CommonFrequencies = new string[]
        {
        "1.841MHz",    // 160米 - FT4主频点（FT8+1kHz）
        "3.571MHz",    // 80米 - FT4主频点（FT8+1kHz）
        "7.078MHz",    // 40米 - FT4全球核心频点
        "10.138MHz",   // 30米 - FT4主频点
        "14.078MHz",   // 20米 - FT4全球核心频点
        "18.098MHz",   // 17米 - FT4主频点
        "21.078MHz",   // 15米 - FT4主频点
        "24.918MHz",   // 12米 - FT4主频点
        "28.078MHz",   // 10米 - FT4主频点
        "50.318MHz",   // 6米 - FT4主频点
        "144.178MHz",  // 2米 - VHF FT4主频点
        "432.178MHz"   // 70厘米 - UHF FT4主频点
        };

        /*        // 国际通用合规的操作后缀列表（全大写）
                public static readonly HashSet<string> ValidOperationSuffixes = new HashSet<string>
                {
                    "P", "M", "MM", "AM", "E", "K", "QRP", "QRO", "A", "R", "TT"
                };
         */

        /// <summary>
        /// 公开只读的合法业余无线电频段字典
        /// 键：标准化波段名（如"6m"、"20m"）
        /// 值：频率区间（单位：MHz）
        /// </summary>
        public static readonly IReadOnlyDictionary<string, FrequencyRange> ValidAmateurBands =
            new Dictionary<string, FrequencyRange>
        {
            // 长波/中波/短波（HF）
            {"2200m", new FrequencyRange(0.1357, 0.1378)},    // 135.7KHz-137.8KHz
            {"160m",  new FrequencyRange(1.8000, 2.0000)},    // 1.8MHz-2.0MHz
            {"80m",   new FrequencyRange(3.5000, 3.9000)},    // 3.5MHz-3.9MHz
            {"60m",   new FrequencyRange(5.3515, 5.3665)},    // 5.3515MHz-5.3665MHz
            {"40m",   new FrequencyRange(7.0000, 7.2000)},    // 7.0MHz-7.2MHz
            {"30m",   new FrequencyRange(10.1000, 10.1500)},  // 10.10MHz-10.15MHz
            {"20m",   new FrequencyRange(14.0000, 14.3000)},  // 14.0MHz-14.3MHz
            {"17m",   new FrequencyRange(18.0680, 18.1680)},  // 18.068MHz-18.1680MHz
            {"15m",   new FrequencyRange(21.0000, 21.4500)},  // 21.00MHz-21.45MHz
            {"12m",   new FrequencyRange(24.8900, 24.9900)},  // 24.89MHz-24.99MHz
            {"10m",   new FrequencyRange(28.0000, 29.7000)},  // 28.0MHz-29.7MHz
            
            // 甚高频（VHF）
            {"6m",    new FrequencyRange(50.0000, 54.0000)},  // 50MHz-54MHz
            {"2m",    new FrequencyRange(144.0000, 148.0000)}, // 144MHz-148MHz
            
            // 特高频（UHF）
            {"70cm",  new FrequencyRange(430.0000, 440.0000)}, // 430MHz-440MHz
            {"23cm",  new FrequencyRange(1240.0000, 1300.0000)}, // 1240MHz-1300MHz
            {"13cm",  new FrequencyRange(2300.0000, 2450.0000)}, // 2300MHz-2450MHz
            
            // 超高频（SHF）
            {"9cm",   new FrequencyRange(3300.0000, 3500.0000)}, // 3300MHz-3500MHz
            {"5cm",   new FrequencyRange(5650.0000, 5850.0000)}, // 5650MHz-5850MHz
            {"3cm",   new FrequencyRange(10000.0000, 10500.0000)}, // 10.0GHz-10.5GHz
            
            // 极高频（EHF）/毫米波
            {"1.2cm", new FrequencyRange(24000.0000, 24050.0000)}, // 24.00GHz-24.05GHz
            {"1.2cm_ext", new FrequencyRange(24050.0000, 24250.0000)}, // 24.05GHz-24.25GHz
            {"6mm",   new FrequencyRange(47000.0000, 47200.0000)}, // 47.0GHz-47.2GHz
            {"4mm",   new FrequencyRange(76000.0000, 77500.0000)}, // 76.0GHz-77.5GHz
            {"4mm_ext", new FrequencyRange(77500.0000, 78000.0000)}, // 77.5GHz-78.0GHz
            {"4mm_ext2", new FrequencyRange(78000.0000, 81000.0000)}, // 78GHz-81GHz
            {"2.5mm", new FrequencyRange(122250.0000, 123000.0000)}, // 122.25GHz-123.00GHz
            {"2mm",   new FrequencyRange(134000.0000, 136000.0000)}, // 134GHz-136GHz
            {"2mm_ext", new FrequencyRange(136000.0000, 141000.0000)}, // 136GHz-141GHz
            {"1mm",   new FrequencyRange(241000.0000, 248000.0000)}, // 241GHz-248GHz
            {"1mm_ext", new FrequencyRange(248000.0000, 250000.0000)} // 248GHz-250GHz
        }.AsReadOnly();


        /// <summary>
        /// 业余无线电通联模式枚举
        /// </summary>
        public enum ModulationMode
        {
            FM = 0,
            AM = 1,
            SSB = 2,
            CW = 3,
            FT8 = 4,
            FT4 = 5,
            SSTV = 6,
            DMR = 7,
            C4FM = 8,
            D_STAR = 9,
            NXDN = 10,
            Packet = 11,
            MSK144 = 12
        }


        #endregion
    }
}
