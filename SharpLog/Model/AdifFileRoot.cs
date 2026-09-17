using System.Text.Json.Serialization;

namespace SharpLog.Model
{
    /// <summary>
    /// ADIF 官方 JSON 数据文件的根结构。
    ///
    /// 【背景说明】
    /// ADIF 官方（adif.org）为每种枚举导出了 JSON 格式的数据文件，
    /// 它们统一采用如下的三层结构：
    /// <code>
    /// {
    ///   "Adif": {                          ← 本类（AdifFileRoot）对应这一层
    ///     "Version": "3.1.7",
    ///     "Enumerations": {
    ///       "Band": {                      ← AdifEnumeration（枚举表）
    ///         "Header":  [ ... ],
    ///         "Records": { ... }
    ///       }
    ///     }
    ///   }
    /// }
    /// </code>
    ///
    /// 一个 JSON 文件可能只包含一个枚举（如 enumerations_band.json 只含 Band），
    /// 也可能包含多个（如 enumerations.json 含全部枚举），
    /// 因此 Enumerations 设计为字典结构，按名称取用。
    ///
    /// 【数据来源】
    /// 文件位于程序运行目录下的 ADIFCode_Json 文件夹，
    /// 由 csproj 配置为随程序发布一起复制。
    /// </summary>
    public class AdifFileRoot
    {
        /// <summary>
        /// ADIF 数据主体（对应 JSON 中最外层的 "Adif" 节点）。
        /// </summary>
        [JsonPropertyName("Adif")]
        public AdifData? Adif { get; set; }
    }
}
