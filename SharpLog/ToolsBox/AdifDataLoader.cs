using SharpLog.Infrastructure;
using SharpLog.Infrastructure.Exceptions;
using SharpLog.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// ADIF 基础数据加载器（静态类，全程序共享一份内存数据）。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【设计目的】
    /// 把 ADIF 官方 JSON 文件中的数据【一次性】读入内存并转换为强类型对象，
    /// 供后续的频率校验、模式校验等功能直接查询。
    ///
    /// 这样做的好处：
    ///   · 校验时直接查内存，不读磁盘 → 速度快，没有 IO 等待；
    ///   · 数据只解析一次，不在每次校验时重复反序列化；
    ///   · 全程序共用同一份数据，不会出现多个副本不一致的问题。
    /// ════════════════════════════════════════════════════════════════════════
    ///
    /// 【数据文件位置】
    /// 程序运行目录下的 ADIFCode_Json 文件夹：
    ///     ADIFCode_Json\enumerations_band.json              波段表
    ///     ADIFCode_Json\enumerations_mode.json              通联模式表
    ///     ADIFCode_Json\enumerations_propagation_mode.json  传播模式表
    ///
    /// 这些文件在 SharpLog.csproj 中配置为 Content 并设置
    /// CopyToOutputDirectory = PreserveNewest，
    /// 因此会随程序打包/发布/迁移自动一起复制到输出目录。
    ///
    /// 【加载时机】
    /// 由 App.OnStartup 在程序启动时调用 Initialize() 完成加载。
    /// 若加载失败（文件缺失、格式错误、内容为空等），
    /// 将抛出自定义异常 FileOperationException，
    /// 由 App 层捕获后终止程序运行——因为缺少这些基础数据，
    /// 频率校验等功能无法正常工作，继续运行会产生不可预期的行为。
    ///
    /// 【为什么不用 JsonTools 加载】
    /// JsonTools 的设计目标是"容错"——失败时返回 null 而不抛异常，
    /// 适合业务数据解析；而本加载器需要严格区分失败原因并在失败时中断程序，
    /// 因此直接使用 JsonSerializer 并显式抛出异常，语义更清晰。
    /// </summary>
    public static class AdifDataLoader
    {
        /// <summary>
        /// 数据文件所在文件夹名（相对于程序运行目录）。
        /// </summary>
        private const string DataFolderName = "ADIFCode_Json";

        /// <summary>波段数据在内存中的缓存（加载后不再变动）</summary>
        private static readonly List<AdifBand> _bands = new List<AdifBand>();

        /// <summary>通联模式数据在内存中的缓存</summary>
        private static readonly List<AdifMode> _modes = new List<AdifMode>();

        /// <summary>传播模式数据在内存中的缓存</summary>
        private static readonly List<AdifPropagationMode> _propagationModes = new List<AdifPropagationMode>();

        /// <summary>
        /// JSON 反序列化配置。
        /// 与 JsonTools 中的配置保持一致的容错策略，
        /// 但此处不做写操作，因此无需设置写入相关的选项。
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            // 属性名忽略大小写，避免 JSON 字段名大小写与模型不完全一致时匹配失败
            PropertyNameCaseInsensitive = true,
            // 允许 JSON 中写注释（便于日后手工维护数据文件）
            ReadCommentHandling = JsonCommentHandling.Skip,
            // 允许最后一项后面多一个逗号
            AllowTrailingCommas = true,
        };

        /// <summary>是否已完成加载（防止重复加载）</summary>
        private static bool _isLoaded;

        /// <summary>
        /// ADIF 数据版本号（从 JSON 文件中读出，如 "3.1.7"）。
        /// 加载完成后方有值。
        /// </summary>
        public static string Version { get; private set; } = string.Empty;

        /// <summary>
        /// 全部合法业余波段（只读）。
        /// 用于频率合法性校验与频率反查波段名称。
        /// </summary>
        public static IReadOnlyList<AdifBand> Bands => _bands;

        /// <summary>
        /// 全部 ADIF 官方通联模式（只读）。
        /// </summary>
        public static IReadOnlyList<AdifMode> Modes => _modes;

        /// <summary>
        /// 全部 ADIF 官方传播模式（只读）。
        /// </summary>
        public static IReadOnlyList<AdifPropagationMode> PropagationModes => _propagationModes;

        /// <summary>
        /// 加载全部 ADIF 基础数据到内存。
        ///
        /// 调用时机：程序启动时（App.OnStartup）调用一次。
        /// 本方法可重复调用，第二次及以后会直接返回（内部有 _isLoaded 标志）。
        ///
        /// 失败处理：任何一步失败都会抛出 FileOperationException，
        /// 异常信息中包含具体的文件路径与失败原因。
        /// </summary>
        /// <exception cref="FileOperationException">
        /// 数据文件不存在、无法读取、格式错误，或解析后内容为空时抛出。
        /// </exception>
        public static void Initialize()
        {
            // 已经加载过则直接返回，避免重复读盘
            if (_isLoaded)
            {
                return;
            }

            // 数据文件夹的完整路径（相对于程序运行目录）
            string dataFolder = Path.Combine(AppContext.BaseDirectory, DataFolderName);

            // ---------- 依次加载三张表 ----------
            // 任一环节失败都会抛出异常，由 App 层统一处理
            LoadBands(Path.Combine(dataFolder, "enumerations_band.json"));
            LoadModes(Path.Combine(dataFolder, "enumerations_mode.json"));
            LoadPropagationModes(Path.Combine(dataFolder, "enumerations_propagation_mode.json"));

            _isLoaded = true;
            Logger.Info(
                $"ADIF 基础数据加载完成（版本 {Version}）：" +
                $"波段 {_bands.Count} 条、模式 {_modes.Count} 条、传播模式 {_propagationModes.Count} 条",
                "AdifDataLoader");
        }

        #region 各数据表的加载实现

        /// <summary>
        /// 加载波段表（enumerations_band.json）。
        /// </summary>
        private static void LoadBands(string filePath)
        {
            AdifEnumeration enumeration = ReadEnumeration(filePath, "Band");

            _bands.Clear();
            foreach (Dictionary<string, string> record in enumeration.Records!.Values)
            {
                // 依次取出需要的三个字段，缺任一字段则跳过该条记录
                if (!record.TryGetValue("Band", out string? bandName)) continue;
                if (!record.TryGetValue("Lower Freq (MHz)", out string? lowerText)) continue;
                if (!record.TryGetValue("Upper Freq (MHz)", out string? upperText)) continue;

                // 频率在 JSON 中是字符串（如 ".1357"），需要解析为 double
                // 注意：必须使用 InvariantCulture，否则在中文区域设置下
                //       可能把小数点 "." 误判为千位分隔符，导致解析失败或结果错误
                if (!double.TryParse(lowerText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lower)) continue;
                if (!double.TryParse(upperText, NumberStyles.Float, CultureInfo.InvariantCulture, out double upper)) continue;

                _bands.Add(new AdifBand
                {
                    Band = bandName,
                    LowerFreqMhz = lower,
                    UpperFreqMhz = upper,
                });
            }

            if (_bands.Count == 0)
            {
                throw new FileOperationException($"波段数据解析后为空，请检查文件内容：{filePath}");
            }
        }

        /// <summary>
        /// 加载通联模式表（enumerations_mode.json）。
        /// </summary>
        private static void LoadModes(string filePath)
        {
            AdifEnumeration enumeration = ReadEnumeration(filePath, "Mode");

            _modes.Clear();
            foreach (Dictionary<string, string> record in enumeration.Records!.Values)
            {
                if (!record.TryGetValue("Mode", out string? modeCode)) continue;

                // 描述与子模式为可选字段
                record.TryGetValue("Description", out string? description);
                record.TryGetValue("Submodes", out string? submodesText);

                _modes.Add(new AdifMode
                {
                    Mode = modeCode,
                    Description = description,
                    // 子模式在 JSON 中是逗号分隔的字符串，此处拆分为列表
                    Submodes = SplitToList(submodesText),
                });
            }

            if (_modes.Count == 0)
            {
                throw new FileOperationException($"通联模式数据解析后为空，请检查文件内容：{filePath}");
            }
        }

        /// <summary>
        /// 加载传播模式表（enumerations_propagation_mode.json）。
        /// 注意：该表中的代码列名是 "Enumeration"，而非 "Code"。
        /// </summary>
        private static void LoadPropagationModes(string filePath)
        {
            AdifEnumeration enumeration = ReadEnumeration(filePath, "Propagation_Mode");

            _propagationModes.Clear();
            foreach (Dictionary<string, string> record in enumeration.Records!.Values)
            {
                if (!record.TryGetValue("Enumeration", out string? code)) continue;

                record.TryGetValue("Description", out string? description);

                _propagationModes.Add(new AdifPropagationMode
                {
                    Code = code,
                    Description = description,
                });
            }

            if (_propagationModes.Count == 0)
            {
                throw new FileOperationException($"传播模式数据解析后为空，请检查文件内容：{filePath}");
            }
        }

        #endregion

        #region 通用辅助方法

        /// <summary>
        /// 读取一个 ADIF JSON 数据文件，并取出指定名称的枚举表。
        ///
        /// 本方法负责完整的校验流程：
        ///   1. 文件是否存在
        ///   2. 能否成功反序列化
        ///   3. 结构是否完整（Adif → Enumerations → 指定枚举 → Records）
        /// 任一环节不通过都抛出 FileOperationException，并附带明确的错误说明。
        /// </summary>
        /// <param name="filePath">JSON 文件的完整路径</param>
        /// <param name="enumerationName">
        /// 要提取的枚举名称，须与 JSON 中的键名完全一致
        /// （如 "Band"、"Mode"、"Propagation_Mode"）
        /// </param>
        /// <returns>提取到的枚举表</returns>
        private static AdifEnumeration ReadEnumeration(string filePath, string enumerationName)
        {
            // ---------- 检查文件是否存在 ----------
            if (!File.Exists(filePath))
            {
                throw new FileOperationException(
                    $"ADIF 数据文件不存在：{filePath}\n" +
                    $"请确认程序的 {DataFolderName} 文件夹是否完整。");
            }

            // ---------- 读取文件文本 ----------
            string json;
            try
            {
                json = File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new FileOperationException($"ADIF 数据文件读取失败：{filePath}", ex);
            }

            // ---------- 反序列化为对象 ----------
            AdifFileRoot? root;
            try
            {
                root = JsonSerializer.Deserialize<AdifFileRoot>(json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                throw new FileOperationException(
                    $"ADIF 数据文件格式错误，无法解析：{filePath}\n{ex.Message}", ex);
            }

            // ---------- 逐层校验结构完整性 ----------
            if (root?.Adif?.Enumerations == null)
            {
                throw new FileOperationException(
                    $"ADIF 数据文件结构异常（缺少 Adif.Enumerations 节点）：{filePath}");
            }

            if (!root.Adif.Enumerations.TryGetValue(enumerationName, out AdifEnumeration? enumeration))
            {
                throw new FileOperationException(
                    $"ADIF 数据文件中未找到名为 {enumerationName} 的枚举：{filePath}");
            }

            if (enumeration.Records == null || enumeration.Records.Count == 0)
            {
                throw new FileOperationException(
                    $"ADIF 数据文件中的 {enumerationName} 枚举没有任何记录：{filePath}");
            }

            // ---------- 记录版本号（三个文件版本一致，取任一即可） ----------
            if (string.IsNullOrWhiteSpace(Version) && !string.IsNullOrWhiteSpace(root.Adif.Version))
            {
                Version = root.Adif.Version;
            }

            return enumeration;
        }

        /// <summary>
        /// 把逗号分隔的字符串拆分为列表（如 "PSK31,PSK63,8PSK125" → 三个元素）。
        /// 输入为空或仅含空白时返回空列表（而非 null），便于调用方直接遍历。
        /// 拆分结果会自动去除每项前后的空白，并忽略空项。
        /// </summary>
        private static List<string> SplitToList(string? text)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            foreach (string item in text.Split(','))
            {
                string trimmed = item.Trim();
                if (trimmed.Length > 0)
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }

        #endregion
    }
}
