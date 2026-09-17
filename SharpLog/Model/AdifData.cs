using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SharpLog.Model
{
    /// <summary>
    /// ADIF 数据主体（对应 JSON 中 "Adif" 节点内的内容）。
    ///
    /// 包含两部分信息：
    ///   1. Version      —— 该数据所遵循的 ADIF 规范版本（如 "3.1.7"）
    ///   2. Enumerations —— 全部枚举表，键为枚举名称
    ///
    /// 常见的枚举名称（键名）：
    ///   "Band"             波段表
    ///   "Mode"             通联模式表
    ///   "Submode"          子模式表
    ///   "Propagation_Mode" 传播模式表
    ///   "Continent"        大洲表
    ///   "DXCC_Entity_Code" DXCC 实体代码表
    ///   …… 等等
    /// </summary>
    public class AdifData
    {
        /// <summary>
        /// ADIF 规范版本号（如 "3.1.7"）。
        /// 可用于在程序界面或日志中标注当前使用的数据版本。
        /// </summary>
        [JsonPropertyName("Version")]
        public string? Version { get; set; }

        /// <summary>
        /// 全部枚举表集合。
        /// 键   = 枚举名称（如 "Band"、"Mode"）
        /// 值   = 该枚举的完整数据（列头 + 记录集合），见 AdifEnumeration
        /// </summary>
        [JsonPropertyName("Enumerations")]
        public Dictionary<string, AdifEnumeration>? Enumerations { get; set; }
    }
}
