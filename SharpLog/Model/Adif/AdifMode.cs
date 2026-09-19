using System.Collections.Generic;

namespace SharpLog.Model
{
    /// <summary>
    /// 业余无线电通联模式模型（来源于 ADIF 官方 enumerations_mode.json 的 Mode 枚举）。
    ///
    /// 【用途】
    ///   · 通联模式下拉列表的数据源（后续可将 StaticValues.Set_CommModeList 切换为本数据）
    ///   · 通联模式代码的合法性校验
    ///
    /// 【数据来源】
    ///   程序运行目录下：ADIFCode_Json\enumerations_mode.json
    ///   由 AdifDataLoader 在程序启动时一次性加载并转换为本类的实例集合。
    ///
    /// 【背景说明：模式与子模式的关系】
    ///   ADIF 规范中，部分模式存在"子模式"概念，例如：
    ///     · 数字语音 DIGITALVOICE 之下有 C4FM、DMR、DSTAR、FREEDV、M17
    ///     · PSK 之下有 PSK31、PSK63、8PSK125 等
    ///     · CW 之下有 PCW
    ///   规范要求：有子模式的通联记录，MODE 与 SUBMODE 字段应同时导出。
    ///
    ///   ⚠ 本项目当前的下拉列表（StaticValues.Set_CommModeList）把 C4FM、D-STAR
    ///     当作独立的模式，这不符合 ADIF 规定——它们应是 DIGITALVOICE 的子模式。
    ///     此问题已记录在待办事项中，后续做"ADIF 合规性修正"时处理。
    ///     本类的 Submodes 属性即为该修正提供数据支撑。
    /// </summary>
    public class AdifMode
    {
        /// <summary>
        /// 模式代码（ADIF 官方代码，全大写，如 "CW"、"SSB"、"FT8"、"DIGITALVOICE"）。
        /// 该值即为 ADIF 文件中 MODE 字段应写入的内容。
        /// </summary>
        public string Mode { get; init; } = string.Empty;

        /// <summary>
        /// 模式的英文描述（可选）。
        /// 只有部分模式提供，例如 ARDOP 的描述为
        /// "Amateur Radio Digital Open Protocol"。
        /// 未提供描述的模式该属性为 null。
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// 该模式下的子模式代码列表（如 PSK 模式下的 "PSK31"、"8PSK125"）。
        /// 无子模式时为空列表（不会为 null，便于调用方直接遍历）。
        /// </summary>
        public List<string> Submodes { get; init; } = new List<string>();

        /// <summary>
        /// 判断本模式是否包含子模式。
        /// </summary>
        public bool HasSubmodes => Submodes.Count > 0;
    }
}
