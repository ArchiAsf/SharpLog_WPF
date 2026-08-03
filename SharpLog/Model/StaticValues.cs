using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using static SharpLog.Model.RadioValues;
using System.Windows.Media;

namespace SharpLog.Model
{
    public static class StaticValues
    {


        public static readonly ObservableCollection<PropagationItem> Set_PropagationModeList = new ObservableCollection<PropagationItem>()
        {
            new PropagationItem{ Name = " ", Code = "Simplex‌" },
            new PropagationItem { Name = "F2 电离层", Code = "F2" },
            new PropagationItem { Name = "ES 偶发E", Code = "ES" },
            new PropagationItem { Name = "MS 流星散射", Code = "MS" },
            new PropagationItem { Name = "EME 月面反射", Code = "EME" },
            new PropagationItem { Name = "SAT 卫星", Code = "SAT" },
            new PropagationItem { Name = "中继 Repeater", Code = "REPEATER" }
        };
        public static readonly ObservableCollection<CommMode> Set_CommModeList = new ObservableCollection<CommMode>()
        {
            new CommMode{ Name = "FM", Code = "FM" },
            new CommMode{ Name = "AM", Code = "AM" },
            new CommMode{ Name = "CW", Code = "CW" },
            new CommMode{ Name = "SSB", Code = "SSB" },
            new CommMode{ Name = "FT8", Code = "FT8" },
            new CommMode{ Name = "FT4", Code = "FT4" },
            new CommMode{ Name = "SSTV", Code = "SSTV" },
            new CommMode{ Name = "DMR", Code = "DMR" },
            new CommMode{ Name = "C4FM", Code = "C4FM" },
            new CommMode{ Name = "D-STAR", Code = "D-STAR" },
            new CommMode{ Name = "NXDN", Code = "NXDN" },
            new CommMode{ Name = "Packet", Code = "Packet" },
            new CommMode{ Name = "MSK144", Code = "MSK144" }

        };




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

        /// <summary>
        /// 公开只读的合法业余无线电频段字典
        /// 键：标准化波段名（如"6m"、"20m"）
        /// 值：频率区间（单位：MHz）
        /// </summary>
        public static readonly IReadOnlyDictionary<string, FrequencyRange> ValidAmateurBands =
            new ReadOnlyDictionary<string, FrequencyRange>(new Dictionary<string, FrequencyRange>
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
            });


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


        // 核心字典：键=通联模式名称，值=对应Color对象
        public static readonly Dictionary<string, Color> ModeColorMap = new Dictionary<string, Color>()
        {
        // FM：橙黄色(255,153,0)——VHF/UHF最常用的调频模式，橙黄活力醒目，贴合日常通联的高频使用场景
        { "FM", Color.FromArgb(255,255, 153, 0) },
        
        // AM：深棕色(102,51,0)——传统调幅模式，深棕自带复古质感，匹配AM模式的经典属性
        { "AM", Color.FromArgb(255,102, 51, 0) },
        
        // CW：明黄色(255,204,0)——莫尔斯码经典模式，明黄是国际通用的警示/醒目色，匹配CW信号易识别的特征
        { "CW", Color.FromArgb(255,255, 204, 0) },

        // SSB：浅蓝色(0,102,204)——HF频段主流语音模式，浅蓝专业冷静，贴合远距离通联的技术属性
        { "SSB", Color.FromArgb(255, 0, 102, 204) },
        
        // FT8：猩红色(220,20,60)——数字弱信号核心模式，猩红高对比度醒目，红色系突出数字模式的重要性，且不刺眼
        { "FT8", Color.FromArgb(255, 220, 20, 60) },
        
        // FT4：番茄红(255,99,71)——FT8衍生模式，番茄红比猩红浅一度，同红色系但易区分，保持数字模式的视觉关联
        { "FT4", Color.FromArgb(255,255, 99, 71) },
        
        // SSTV：粉紫色(255,51,153)——慢扫描电视模式，粉紫色彩丰富，贴合SSTV传图的视觉化特征
        { "SSTV", Color.FromArgb(255,255, 51, 153) },
        
        // DMR：深灰色(102,102,102)——数字集群模式，深灰工业感强，匹配DMR商用/业余通用的稳重属性
        { "DMR", Color.FromArgb(255, 102, 102, 102) },
        
        // C4FM：浅绿色(0,204,153)——数字调频模式，浅绿清新柔和，与FM橙黄形成区分，贴合数字调频的低功耗特征
        { "C4FM", Color.FromArgb(255, 0, 204, 153) },
        
        // D-STAR：深蓝色(0,51,153)——数字星状网模式，深蓝科技感强，匹配“STAR（星）”的太空/远距离通联意象
        { "D-STAR", Color.FromArgb(255, 0, 51, 153) },
        
        // NXDN：紫色(153,102,204)——窄带数字模式，紫色稳重且独特，与其他数字模式颜色形成区分
        { "NXDN", Color.FromArgb(255, 153, 102, 204) },
        
        // Packet：浅灰色(204,204,204)——分组数据模式，浅灰轻量中性，贴合Packet低速数据传输的低调特征
        { "Packet", Color.FromArgb(255, 204, 204, 204) },
        
        // MSK144：青蓝色(0,255,255)——流星散射模式，青蓝高饱和度鲜艳，像流星划过的亮色轨迹，视觉冲击力强
        { "MSK144", Color.FromArgb(255, 0, 255, 255) }
        };

        #endregion


        // 现在计算机所在时区的时间
        public static DateTimeOffset LocalNow => DateTimeOffset.Now;

        //现在的UTC时间
        public static DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        //程序要使用的时间
        public static DateTimeOffset LogUseTime { get; set; } = UtcNow;

        //如果为true就是使用UTC时间，如果为false就是使用本地时间
        public static bool IsUseUtcTime { get; set; } = true;



        //生成一个将LogTable对象转换为OneLog对象的方法
        public static OneLog ConvertToOneLog(QsoLog log)
        {
            return new OneLog
            {
                CardSideBarColor = new System.Windows.Media.SolidColorBrush(StaticValues.ModeColorMap[log.Mode]),
                StartTime = log.StartTime.ToString("yyyy/MM/dd-HH:mm:ss"),
                EndTime = log.EndTime.ToString("yyyy/MM/dd-HH:mm:ss"),
                TimeZoneSuffix = log.TimeZone,
                CallSign = log.CallSign,
                DisplayFrequency = log.Frequency.ToString() + "khz",
                Mode = log.Mode,
                RST = log.RST,
                RRST = log.ReceivedRST,
                QTH = log.QTH,
                Rig = log.Rig,
                Antenna = log.ANT,
                Operator = log.OP,
                Locator = log.Locator,
            };
        }


    }
}
