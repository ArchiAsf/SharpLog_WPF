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

        // ============================================================
        // 【已移除】原 ValidAmateurBands 硬编码波段表
        // ------------------------------------------------------------
        // 移除原因：该表与 ADIF 官方数据存在边界不一致，例如
        //   80m：本表 3.5-3.9 MHz，官方 3.5-4.0 MHz
        //   40m：本表 7.0-7.2 MHz，官方 7.0-7.3 MHz
        // 会导致本项目校验通过的频率，在导入其他软件时被判为非法波段。
        //
        // 现改用 ADIF 官方 enumerations_band.json 的数据：
        //   · 数据文件位于程序目录 ADIFCode_Json 文件夹，随程序发布一起复制
        //   · 由 ToolsBox\AdifDataLoader 在程序启动时一次性加载到内存
        //   · 频率校验与波段查询请使用 AdifDataLoader.Bands
        // ============================================================


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
