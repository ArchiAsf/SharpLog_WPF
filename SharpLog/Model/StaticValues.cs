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
        //注意：字段命名已统一为 ADIF 3.1.7 规范名称
        public static OneLog ConvertToOneLog(QsoLog log)
        {
            return new OneLog
            {
                CardSideBarColor = new System.Windows.Media.SolidColorBrush(StaticValues.ModeColorMap[log.MODE]),
                StartTime = CombineQsoDateTime(log.QSO_DATE, log.TIME_ON),
                EndTime = CombineQsoDateTime(log.QSO_DATE_OFF, log.TIME_OFF),
                TimeZoneSuffix = log.TimeZone,
                CALL = log.CALL,
                DisplayFrequency = log.FREQ.ToString() + "khz",
                MODE = log.MODE,
                RST_RCVD = log.RST_RCVD,
                RST_SENT = log.RST_SENT,
                QTH = log.QTH,
                RIG = log.RIG,
                Antenna = log.ANT,
                Operator = log.OP,
                GRIDSQUARE = log.GRIDSQUARE,
            };
        }

        /// <summary>
        /// 组合 日期+时间 为显示字符串（yyyy/MM/dd-HH:mm:ss）
        /// </summary>
        private static string CombineQsoDateTime(DateTime? date, string? time)
        {
            string dateStr = date?.ToString("yyyy/MM/dd") ?? string.Empty;
            string timeStr = string.Empty;
            if (!string.IsNullOrWhiteSpace(time) && TimeOnly.TryParseExact(time, "HHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var t))
            {
                timeStr = t.ToString("HH:mm:ss");
            }
            if (string.IsNullOrEmpty(dateStr)) return timeStr;
            if (string.IsNullOrEmpty(timeStr)) return dateStr;
            return dateStr + "-" + timeStr;
        }


    }
}
