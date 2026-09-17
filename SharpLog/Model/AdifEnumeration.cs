using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SharpLog.Model
{
    /// <summary>
    /// ADIF 枚举表（对应 JSON 中 "Enumerations" 下的单个枚举节点）。
    ///
    /// 【结构说明】（以波段表 enumerations_band.json 为例）
    /// <code>
    /// "Band": {
    ///   "Header": [                            ← 列名列表
    ///     "Enumeration Name", "Band",
    ///     "Lower Freq (MHz)", "Upper Freq (MHz)",
    ///     "Import-only", "Comments", "ADIF Version", "ADIF Status"
    ///   ],
    ///   "Records": {                           ← 记录集合
    ///     "20m": {                             ← 键 = 该记录的唯一标识
    ///       "Enumeration Name": "Band",
    ///       "Band": "20m",
    ///       "Lower Freq (MHz)": "14.0",        ← 注意：值是【字符串】
    ///       "Upper Freq (MHz)": "14.35"
    ///     },
    ///     "40m": { ... }
    ///   }
    /// }
    /// </code>
    ///
    /// 【重要提示】
    /// Records 中的值统一为【字符串】类型——即使原文是数值（如 ".1357"），
    /// JSON 里也用引号包裹为字符串。因此调用方取出后需要自行解析为数值，
    /// 且解析时务必使用 CultureInfo.InvariantCulture，
    /// 否则在中文等区域设置下可能把小数点 "." 误判为千位分隔符。
    ///
    /// 【本类的定位】
    /// 这是一个"通用中转结构"，忠实映射 JSON 原始格式，不针对具体业务。
    /// 各业务模型（如 AdifBand、AdifMode）由 AdifDataLoader 从本结构中提取生成。
    /// </summary>
    public class AdifEnumeration
    {
        /// <summary>
        /// 列名列表，描述 Records 中每条记录包含哪些字段。
        /// 例如波段表为 ["Enumeration Name", "Band", "Lower Freq (MHz)", ...]。
        /// 程序按字段名取值时以该列表为依据（但实际取值用字典键，不依赖顺序）。
        /// </summary>
        [JsonPropertyName("Header")]
        public List<string>? Header { get; set; }

        /// <summary>
        /// 记录集合。
        /// 外层键 = 记录标识（如波段的 "20m"、模式的 "CW"）
        /// 内层字典 = 该记录的"字段名 → 字段值"映射
        /// </summary>
        [JsonPropertyName("Records")]
        public Dictionary<string, Dictionary<string, string>>? Records { get; set; }
    }
}
