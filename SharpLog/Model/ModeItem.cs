using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLog.Model
{
    /// <summary>
    /// 通联模式下拉项模型 —— 连接「界面显示」与「ADIF 存储」的桥梁。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【为什么需要三个属性】
    /// ADIF 3.1.7 对通联模式采用「MODE + SUBMODE」两级体系：
    ///   · MODE    —— 主模式，必须取官方枚举值（如 DIGITALVOICE、MFSK）；
    ///   · SUBMODE —— 子模式，仅当主模式下有细分时才使用（如 DMR、FT4）。
    ///
    /// 但如果直接把主模式显示给用户会造成困扰：用户在电台上明明用的是 D-STAR，
    /// 界面上却只能看到 "DIGITALVOICE"，无法与其他数字语音模式区分。
    ///
    /// 因此本模型把三个值绑定在一起：
    ///   DisplayName —— 界面显示的名称，贴近火腿日常叫法（如 "D-STAR"）
    ///   Mode        —— 写入数据库 QsoLog.MODE 字段的 ADIF 主模式（如 "DIGITALVOICE"）
    ///   Submode     —— 写入数据库 QsoLog.SUBMODE 字段的 ADIF 子模式（如 "DSTAR"）
    ///
    /// 【举例】
    ///   用户选「D-STAR」→ 存库 MODE=DIGITALVOICE, SUBMODE=DSTAR
    ///   用户选「FT4」   → 存库 MODE=MFSK,         SUBMODE=FT4
    ///   用户选「SSB」   → 存库 MODE=SSB,          SUBMODE=null（该模式无子模式）
    /// ════════════════════════════════════════════════════════════════════════
    ///
    /// 本模型只用于「下拉列表选项」：
    ///   · 导出 ADIF 文件时，必须同时写出 MODE 与 SUBMODE 两个字段；
    ///   · 界面卡片显示时，用 DisplayName 反查（见 ModeConvertTools）。
    /// </summary>
    public class ModeItem
    {
        /// <summary>
        /// 界面显示名称（如 "D-STAR"、"SSB"）。
        /// 同时作为卡片配色字典 StaticValues.ModeColorMap 的键。
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// ADIF 主模式（MODE 字段值），必须取自 ADIF 官方枚举，
        /// 例如 SSB / FM / CW / FT8 / MFSK / DIGITALVOICE。
        /// </summary>
        public string Mode { get; init; } = string.Empty;

        /// <summary>
        /// ADIF 子模式（SUBMODE 字段值），仅当主模式下有细分时才有值；
        /// 无子模式的模式（如 SSB、CW）此属性为 null。
        /// </summary>
        public string? Submode { get; init; }

        /// <summary>
        /// 重写 ToString：让未设置 DisplayMemberPath 的下拉框也能显示正常文字，
        /// 避免出现 "SharpLog.Model.ModeItem" 这样的类型名。
        /// </summary>
        public override string ToString() => DisplayName;
    }
}
