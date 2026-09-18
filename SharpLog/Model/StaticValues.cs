using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Media;

namespace SharpLog.Model
{
    public static class StaticValues
    {


        /// <summary>
        /// 传播模式下拉列表：界面显示名 ↔ ADIF PROP_MODE 代码的映射表。
        ///
        /// ════════════════════════════════════════════════════════════════════
        /// 【数据来源】
        /// 20 条 Code 全部取自 ADIF 3.1.7 官方枚举表
        /// （程序目录 ADIFCode_Json\enumerations_propagation_mode.json，
        ///   由 AdifDataLoader 加载，可在 AdifDataLoader.PropagationModes 中查询）。
        /// 官方该枚举共 20 条，已全部收录，未做取舍。
        ///
        /// 【首项「（不指定）」】
        /// PROP_MODE 在 ADIF 中是可选字段，绝大多数通联记录并不填写它。
        /// 首项即表示「本次未记录传播方式」：Code 为 null →
        /// 存库时 QsoLog.PROP_MODE 写 NULL，导出 ADIF 时该字段整体省略。
        /// 该项同时也是默认选中项，避免把某个具体传播方式（如 F2）
        /// 无差别写进每一条日志，造成事后无法清洗的数据污染。
        ///
        /// 【排序原则】
        /// 常用方式排在最前（便于下拉一次即可选中），其余按官方代码字母序排列，
        /// 网络链路类（EchoLink / IRLP / 互联网辅助）单独归为一组。
        ///
        /// 【已修正的历史问题】
        /// 原列表首项 Code = "Simplex"（尾部还含一个零宽字符 U+200C）、
        /// 末项 Code = "REPEATER"，两者都不是 ADIF 官方代码：
        ///   · 官方的中继/转发器代码是 "RPT"；
        ///   · "Simplex"（单工）根本不是传播模式，ADIF 枚举中无此值。
        /// 现已全部替换为官方标准值。
        /// ════════════════════════════════════════════════════════════════════
        /// </summary>
        public static readonly ObservableCollection<PropagationItem> Set_PropagationModeList = new ObservableCollection<PropagationItem>()
        {
            // 默认项：不记录传播方式（Code 为 null → 存库写 NULL，导出 ADIF 时省略该字段）
            new PropagationItem { DisplayName = "（不指定）", Code = null },

            // ────────────── 常用传播方式（10 项）──────────────
            new PropagationItem { DisplayName = "F2层反射 F2",       Code = "F2"    },
            new PropagationItem { DisplayName = "偶发E层 ES",         Code = "ES"    },
            new PropagationItem { DisplayName = "流星散射 MS",        Code = "MS"    },
            new PropagationItem { DisplayName = "月面反射 EME",       Code = "EME"   },
            new PropagationItem { DisplayName = "卫星转发 SAT",       Code = "SAT"   },
            new PropagationItem { DisplayName = "中继/转发器 RPT",    Code = "RPT"   },
            new PropagationItem { DisplayName = "对流层波导 TR",      Code = "TR"    },
            new PropagationItem { DisplayName = "视距传播 LOS",       Code = "LOS"   },
            new PropagationItem { DisplayName = "地波 GWAVE",         Code = "GWAVE" },
            new PropagationItem { DisplayName = "极光 AUR",           Code = "AUR"   },

            // ────────────── 其他传播方式（7 项，按官方代码字母序）──────────────
            new PropagationItem { DisplayName = "飞机散射 AS",        Code = "AS"    },
            new PropagationItem { DisplayName = "极光E层 AUE",        Code = "AUE"   },
            new PropagationItem { DisplayName = "后向散射 BS",        Code = "BS"    },
            new PropagationItem { DisplayName = "场向不规则体 FAI",    Code = "FAI"   },
            new PropagationItem { DisplayName = "电离层散射 ION",     Code = "ION"   },
            new PropagationItem { DisplayName = "雨散射 RS",          Code = "RS"    },
            new PropagationItem { DisplayName = "跨赤道传播 TEP",     Code = "TEP"   },

            // ────────────── 网络链路类（3 项）──────────────
            new PropagationItem { DisplayName = "EchoLink网络 ECH",   Code = "ECH"      },
            new PropagationItem { DisplayName = "IRLP网络 IRL",       Code = "IRL"      },
            new PropagationItem { DisplayName = "互联网辅助 INTERNET", Code = "INTERNET" }
        };

        /// <summary>
        /// 通联模式下拉列表：界面显示名 ↔ ADIF MODE/SUBMODE 的映射表。
        ///
        /// ════════════════════════════════════════════════════════════════════
        /// 【数据来源】
        /// 每一项的 Mode / Submode 取值均来自 ADIF 3.1.7 官方枚举表
        /// （程序目录 ADIFCode_Json\enumerations_mode.json，由 AdifDataLoader 加载），
        /// 已逐项与官方数据核对，保证导出的 ADIF 文件能被其他日志软件正确识别。
        ///
        /// 【为什么只有 23 项】
        /// 官方 MODE 有 91 个、SUBMODE 有 196 个，其中绝大多数
        /// （如 FST4W、ISCAT、Q65、DOMINO 等）业余场合极少使用。
        /// 本列表按「常用程度」精选 23 项，覆盖绝大多数实际通联场景，并按模式类型分组。
        /// 后续如需扩展，请从 AdifDataLoader.Modes 中挑选并补充到本列表。
        ///
        /// 【特别注意：以下选项存库的是「主模式 + 子模式」两个字段】
        ///   FT4    → MODE=MFSK,         SUBMODE=FT4
        ///   PSK31  → MODE=PSK,          SUBMODE=PSK31
        ///   JS8    → MODE=MFSK,         SUBMODE=JS8
        ///   DMR    → MODE=DIGITALVOICE, SUBMODE=DMR
        ///   D-STAR → MODE=DIGITALVOICE, SUBMODE=DSTAR
        ///   C4FM   → MODE=DIGITALVOICE, SUBMODE=C4FM
        ///   M17    → MODE=DIGITALVOICE, SUBMODE=M17
        /// 这样导出 ADIF 后，其他软件既能识别大类（如数字语音），
        /// 又能区分具体制式（DMR / D-STAR / C4FM）。
        /// 其余无子模式的选项，SUBMODE 字段留空（null），不写库。
        /// ════════════════════════════════════════════════════════════════════
        /// </summary>
        public static readonly ObservableCollection<ModeItem> Set_CommModeList = new ObservableCollection<ModeItem>()
        {
            // ────────────── 语音类（3 项）──────────────
            new ModeItem { DisplayName = "SSB", Mode = "SSB" },
            new ModeItem { DisplayName = "FM",  Mode = "FM"  },
            new ModeItem { DisplayName = "AM",  Mode = "AM"  },

            // ────────────── 电码类（1 项）──────────────
            new ModeItem { DisplayName = "CW",  Mode = "CW"  },

            // ────────────── 数字弱信号类（10 项）──────────────
            new ModeItem { DisplayName = "FT8",    Mode = "FT8"    },
            new ModeItem { DisplayName = "FT4",    Mode = "MFSK",  Submode = "FT4"   },
            new ModeItem { DisplayName = "RTTY",   Mode = "RTTY"   },
            new ModeItem { DisplayName = "PSK31",  Mode = "PSK",   Submode = "PSK31" },
            new ModeItem { DisplayName = "JS8",    Mode = "MFSK",  Submode = "JS8"   },
            new ModeItem { DisplayName = "WSPR",   Mode = "WSPR"   },
            new ModeItem { DisplayName = "JT65",   Mode = "JT65"   },
            new ModeItem { DisplayName = "MSK144", Mode = "MSK144" },
            new ModeItem { DisplayName = "OLIVIA", Mode = "OLIVIA" },
            new ModeItem { DisplayName = "MFSK",   Mode = "MFSK"   },

            // ────────────── 数字语音类（5 项）──────────────
            new ModeItem { DisplayName = "DMR",    Mode = "DIGITALVOICE", Submode = "DMR"   },
            new ModeItem { DisplayName = "D-STAR", Mode = "DIGITALVOICE", Submode = "DSTAR" },
            new ModeItem { DisplayName = "C4FM",   Mode = "DIGITALVOICE", Submode = "C4FM"  },
            new ModeItem { DisplayName = "M17",    Mode = "DIGITALVOICE", Submode = "M17"   },
            // 不指明具体制式时的通用项，只写主模式，不写子模式
            new ModeItem { DisplayName = "数字语音", Mode = "DIGITALVOICE" },

            // ────────────── 图像类（2 项）──────────────
            new ModeItem { DisplayName = "SSTV",   Mode = "SSTV"   },
            new ModeItem { DisplayName = "ATV",    Mode = "ATV"    },

            // ────────────── 数据类（2 项）──────────────
            new ModeItem { DisplayName = "PKT",    Mode = "PKT"    },
            new ModeItem { DisplayName = "ARDOP",  Mode = "ARDOP"  }
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

        // ============================================================
        // 【已移除】原 ModulationMode 枚举
        // ------------------------------------------------------------
        // 移除原因：该枚举与 ADIF 3.1.7 的「MODE + SUBMODE」两级体系冲突——
        // 枚举里的 FT4、DMR、C4FM、D_STAR、NXDN、Packet 在 ADIF 中都不是主模式
        // （FT4 是 MFSK 的子模式，DMR/C4FM/D-STAR 是 DIGITALVOICE 的子模式），
        // 按枚举值存库会导致导出的 ADIF 文件不符合规范；
        // 且该枚举在全项目中无任何引用（死代码）。
        // 现由 Set_CommModeList（ModeItem 列表）承载通联模式数据。
        // ============================================================

        /// <summary>
        /// 卡片配色字典：键 = ModeItem.DisplayName（界面显示名），值 = 卡片左侧竖条颜色。
        ///
        /// ════════════════════════════════════════════════════════════════════
        /// 【键为什么用显示名而不是 MODE】
        /// ADIF 中 DMR、D-STAR、C4FM、M17 的 MODE 都是 DIGITALVOICE，
        /// 若按 MODE 配色，这四种制式会挤在同一个颜色里无法区分。
        /// 因此配色统一按界面显示名（如 "D-STAR"）索引，
        /// 由 ModeConvertTools.ResolveDisplayName(MODE, SUBMODE) 反查后取色。
        ///
        /// 【取色务必用 TryGetValue】
        /// 本字典必须与 Set_CommModeList 的 23 项保持一致。
        /// 若数据库中存在历史数据，或导入了外部的 ADIF 文件，
        /// 其中出现了字典里没有的模式名，直接用索引器 ModeColorMap[key]
        /// 会抛 KeyNotFoundException 导致程序崩溃，
        /// 应改用 TryGetValue，未命中时回落到 DefaultModeColor。
        /// ════════════════════════════════════════════════════════════════════
        /// </summary>
        public static readonly Dictionary<string, Color> ModeColorMap = new Dictionary<string, Color>()
        {
            // ────────────── 语音类 ──────────────
            // SSB：浅蓝色(0,102,204)——HF频段主流语音模式，浅蓝专业冷静，贴合远距离通联的技术属性
            { "SSB", Color.FromArgb(255, 0, 102, 204) },

            // FM：橙黄色(255,153,0)——VHF/UHF最常用的调频模式，橙黄活力醒目，贴合日常通联的高频使用场景
            { "FM", Color.FromArgb(255, 255, 153, 0) },

            // AM：深棕色(102,51,0)——传统调幅模式，深棕自带复古质感，匹配AM模式的经典属性
            { "AM", Color.FromArgb(255, 102, 51, 0) },

            // ────────────── 电码类 ──────────────
            // CW：明黄色(255,204,0)——莫尔斯码经典模式，明黄醒目，匹配CW信号易识别的特征
            { "CW", Color.FromArgb(255, 255, 204, 0) },

            // ────────────── 数字弱信号类 ──────────────
            // FT8：猩红色(220,20,60)——数字弱信号核心模式，猩红高对比度醒目，红色系突出数字模式的重要性，且不刺眼
            { "FT8", Color.FromArgb(255, 220, 20, 60) },

            // FT4：番茄红(255,99,71)——FT8衍生模式，番茄红比猩红浅一度，同红色系但易区分，保持数字模式的视觉关联
            { "FT4", Color.FromArgb(255, 255, 99, 71) },

            // RTTY：琥珀橙(255,140,0)——最经典的电传打字模式，深橙带复古味，与FM的橙黄通过更红、更暗的色相区分
            { "RTTY", Color.FromArgb(255, 255, 140, 0) },

            // PSK31：天蓝色(0,153,204)——PSK系列代表模式，天蓝清爽，与SSB的深蓝形成同色系、不同明度的区分
            { "PSK31", Color.FromArgb(255, 0, 153, 204) },

            // JS8：暖橙黄(255,179,71)——FT8家族的通联扩展模式，橙黄比FT4更暖，提示其「数字+人工键盘通联」的混合属性
            { "JS8", Color.FromArgb(255, 255, 179, 71) },

            // WSPR：蓝紫色(138,43,226)——弱信号传播信标模式，蓝紫神秘感，匹配其「只发信标、不用于通联」的探测用途
            { "WSPR", Color.FromArgb(255, 138, 43, 226) },

            // JT65：深粉色(255,20,147)——EME月面反射经典模式，深粉高辨识度，与同族的FT8/FT4红色系拉开层次
            { "JT65", Color.FromArgb(255, 255, 20, 147) },

            // MSK144：青蓝色(0,255,255)——流星散射模式，青蓝高饱和度鲜艳，像流星划过的亮色轨迹，视觉冲击力强
            { "MSK144", Color.FromArgb(255, 0, 255, 255) },

            // OLIVIA：海洋绿(102,205,170)——低信噪比下的多频移键控模式，海绿柔和，贴合其「穿透噪声、稳定可靠」的特性
            { "OLIVIA", Color.FromArgb(255, 102, 205, 170) },

            // MFSK：中绿色(60,179,113)——MFSK家族的主模式（FT4/JS8 均属其子模式），中绿居中，呼应其「家族根节点」的地位
            { "MFSK", Color.FromArgb(255, 60, 179, 113) },

            // ────────────── 数字语音类 ──────────────
            // DMR：深灰色(102,102,102)——数字集群模式，深灰工业感强，匹配DMR商用/业余通用的稳重属性
            { "DMR", Color.FromArgb(255, 102, 102, 102) },

            // D-STAR：深蓝色(0,51,153)——数字星状网模式，深蓝科技感强，契合「STAR（星）」的太空与远距离通联意象
            { "D-STAR", Color.FromArgb(255, 0, 51, 153) },

            // C4FM：浅绿色(0,204,153)——数字调频模式，浅绿清新柔和，与FM的橙黄形成区分，贴合数字调频的低功耗特征
            { "C4FM", Color.FromArgb(255, 0, 204, 153) },

            // M17：深青色(0,128,128)——开源数字语音项目，深青带开源社区的硬核技术气质，与D-STAR的深蓝区分
            { "M17", Color.FromArgb(255, 0, 128, 128) },

            // 数字语音：石板灰(112,128,144)——不指明具体制式时的通用项，石板灰中性克制，且与DMR的深灰、PKT的浅灰错开
            { "数字语音", Color.FromArgb(255, 112, 128, 144) },

            // ────────────── 图像类 ──────────────
            // SSTV：粉紫色(255,51,153)——慢扫描电视模式，粉紫色彩丰富，贴合SSTV传图的视觉化特征
            { "SSTV", Color.FromArgb(255, 255, 51, 153) },

            // ATV：淡紫红(219,112,147)——业余电视模式，与SSTV同属图像类的紫调，但更浅更柔，便于并排区分
            { "ATV", Color.FromArgb(255, 219, 112, 147) },

            // ────────────── 数据类 ──────────────
            // PKT：浅灰色(204,204,204)——分组数据模式，浅灰轻量中性，贴合分组无线低速数据传输的低调特征
            { "PKT", Color.FromArgb(255, 204, 204, 204) },

            // ARDOP：浅钢蓝(176,196,222)——HF频段的开源数据调制解调模式，钢蓝冷静，与PSK31的天蓝拉开明度差
            { "ARDOP", Color.FromArgb(255, 176, 196, 222) }
        };

        /// <summary>
        /// 模式配色的兜底颜色（中性灰）。
        ///
        /// 使用场景：数据库中存在的模式名不在 ModeColorMap 内时
        /// （例如早期的历史数据，或从外部 ADIF 文件导入的记录），
        /// 用该颜色代替，避免取色失败导致程序崩溃。
        /// </summary>
        public static readonly Color DefaultModeColor = Color.FromArgb(255, 128, 128, 128);

        #endregion


        // 现在计算机所在时区的时间
        public static DateTimeOffset LocalNow => DateTimeOffset.Now;

        //现在的UTC时间
        public static DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        //程序要使用的时间
        public static DateTimeOffset LogUseTime { get; set; } = UtcNow;

        //如果为true就是使用UTC时间，如果为false就是使用本地时间
        public static bool IsUseUtcTime { get; set; } = true;


        // ============================================================
        //  晨昏（日出 / 日落）信息
        // ------------------------------------------------------------
        // 由 MainLogWindow_VM 在【程序启动时】与【每天本地零点】调用
        // ToolsBox\SunTimeCalculator 计算后写入这里，供界面显示，
        // 以及后续的传播判断（如 160m/80m 波段的夜间通联窗口）取用。
        //
        // 注意：算出来的是【民用晨昏】时刻（太阳位于地平线下 6°），
        // 比日常说的"日出日落"早/晚各 20~30 分钟，这是有意为之。
        // ============================================================

        /// <summary>
        /// 今日民用日出时刻（本地时间）。
        /// 尚未计算、当天为极昼/极夜、或经纬度未设置时为 null。
        /// </summary>
        public static DateTime? SunriseTime { get; set; }

        /// <summary>
        /// 今日民用日落时刻（本地时间）。
        /// 尚未计算、当天为极昼/极夜、或经纬度未设置时为 null。
        /// </summary>
        public static DateTime? SunsetTime { get; set; }

        /// <summary>
        /// 日出日落的计算状态，用于区分「正常 / 极昼 / 极夜 / 无法计算」。
        /// </summary>
        public static SunTimeStatus SunTimeState { get; set; } = SunTimeStatus.Invalid;



        // ============================================================
        // 【已移除】原 ConvertToOneLog 方法与 CombineQsoDateTime 辅助方法
        // ------------------------------------------------------------
        // 移除原因：
        //   1. 与 ViewModel\ModeConvertTools.cs 的 ConvertQsoLogToOneLog 功能完全重复，
        //      两处各维护一份转换逻辑，改一处漏一处极易产生不一致；
        //   2. 全项目中无任何调用（死代码），日志卡片统一走 ModeConvertTools 转换；
        //   3. 原实现使用 ModeColorMap[log.MODE] 直接索引取色，
        //      一旦遇到字典中没有的模式名就会抛 KeyNotFoundException 使程序崩溃。
        // ============================================================


    }
}
