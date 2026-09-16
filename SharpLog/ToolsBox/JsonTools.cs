using SharpLog.Infrastructure;
using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// JSON 序列化 / 反序列化工具类（静态，无状态）。
    ///
    /// ============================ 用途 ============================
    /// 在两个方向之间转换数据：
    ///   · ObjectToJson —— 把 C# 对象　→　JSON 文本（序列化 / 存盘）
    ///   · JsonToObject —— 把 JSON 文本　→　C# 对象（反序列化 / 读取）
    ///
    /// 典型场景：
    ///   · 保存 / 读取程序配置
    ///   · 加载静态资源数据（如 ADIF 字段表、波段表、模式表的 JSON 文件）
    ///   · 与外部程序交换数据
    ///
    /// ========================== 技术说明 ==========================
    /// 1. 使用 .NET 8 内置的 System.Text.Json，【不需要】任何第三方 NuGet 包；
    /// 2. 序列化配置在类内统一管理（见 _options），保证全项目行为一致；
    /// 3. 两个方法都是【异步】的：内部使用 Task.Run 把解析工作放到线程池执行，
    ///    这样解析大文本时不会卡住界面（UI 线程）；
    /// 4. 解析失败【不会抛异常中断程序】，而是记录日志并返回 null / 空串，
    ///    调用方只需判断返回值是否为 null 即可（具体错误原因见 Logs 目录日志）。
    ///
    /// ========================== 使用示例 ==========================
    /// <code>
    /// // 1) 对象 → JSON 文本
    /// var json = await JsonTools.ObjectToJson(myObject);
    ///
    /// // 2) JSON 文本 → 对象
    /// var obj = await JsonTools.JsonToObject&lt;MyClass&gt;(json);
    /// if (obj == null) { /* 解析失败，日志里已有详细原因 */ }
    ///
    /// // 3) 配合文件读写（加载 JSON 资源文件的完整流程）
    /// string text = await File.ReadAllTextAsync(@"Resources\band.json");
    /// var bands = await JsonTools.JsonToObject&lt;List&lt;BandInfo&gt;&gt;(text);
    /// </code>
    /// </summary>
    static class JsonTools
    {
        /// <summary>
        /// 全局统一的 JSON 序列化 / 反序列化配置（静态只读，全项目共用一份）。
        ///
        /// 各项设置的作用说明：
        ///   WriteIndented = true
        ///       → 输出的 JSON 带缩进和换行，便于人工打开查看、也方便 Git 比对差异。
        ///         若追求文件体积（如几千条记录），可改为 false 压缩输出。
        ///   Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        ///       → 中文【原样输出】。System.Text.Json 默认会把中文转义成 中文 形式，
        ///         加上这一项后 JSON 里的"北京""东京"仍是可读的中文。
        ///   PropertyNameCaseInsensitive = true
        ///       → 反序列化时忽略属性名的大小写差异。
        ///         例如 JSON 里写 "CALL"，C# 属性叫 Call，也能正确匹配（对 ADIF 全大写字段名很友好）。
        ///   ReadCommentHandling = JsonCommentHandling.Skip
        ///       → 允许 JSON 文件中写 // 或 /* */ 注释（手工维护资源文件时非常实用）。
        ///   AllowTrailingCommas = true
        ///       → 允许对象或数组的最后一项后面多一个逗号（手工编辑时少写容易出错）。
        /// </summary>
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>
        /// 【反序列化】将 JSON 文本解析为指定类型的对象。
        ///
        /// 执行流程：
        ///   1. 检查传入文本是否为空 → 空则记警告日志并返回 null，不做无意义的解析；
        ///   2. 通过 Task.Run 把解析放到线程池执行 → 不阻塞 UI 线程；
        ///   3. 解析成功返回对象；解析失败记录日志并返回 null。
        ///
        /// 容错说明：本方法内部已捕获所有异常，调用方【无需】再包 try-catch，
        ///          只需判断返回值是否为 null。失败的具体原因会写入 Logs 日志文件。
        /// </summary>
        /// <typeparam name="T">
        /// 要还原成的目标类型。可以是：
        ///   · 单个实体类（如 QsoLog）
        ///   · 集合（如 List&lt;QsoLog&gt;、Dictionary&lt;string, BandInfo&gt;）
        ///   注意：目标类型的属性名要与 JSON 中的键名对应（大小写不敏感）。
        /// </typeparam>
        /// <param name="json">待解析的 JSON 文本（须为完整的 JSON 结构）</param>
        /// <returns>
        /// 解析成功 → 对应的对象实例；
        /// 输入为空或解析失败 → null
        /// </returns>
        public static async Task<T?> JsonToObject<T>(string json)
        {
            // ---------- 前置检查：空文本直接返回，避免无意义的解析与异常 ----------
            if (string.IsNullOrWhiteSpace(json))
            {
                Logger.Warn("JsonToObject：传入的 JSON 文本为空，已跳过解析", "JsonTools");
                return default;
            }

            try
            {
                // ---------- 放到线程池执行，避免大文本解析时界面卡顿 ----------
                return await Task.Run(() => JsonSerializer.Deserialize<T>(json, _options));
            }
            catch (JsonException ex)
            {
                // JSON 结构或类型不匹配（括号缺失、值类型不对、字段格式错误等）
                Logger.Error($"JSON 解析失败：{ex.Message}", ex, "JsonTools");
                return default;
            }
            catch (Exception ex)
            {
                // 兜底：捕获其他未预料的异常，保证工具类不会把异常抛给调用方
                Logger.Error($"JSON 解析发生未预期错误：{ex.Message}", ex, "JsonTools");
                return default;
            }
        }

        /// <summary>
        /// 【序列化】将对象转换为 JSON 文本。
        ///
        /// 执行流程：
        ///   1. 检查传入对象是否为 null → 为 null 时记警告日志并返回空串；
        ///   2. 通过 Task.Run 把序列化放到线程池执行 → 不阻塞 UI 线程；
        ///   3. 序列化成功返回 JSON 文本；失败记录日志并返回空串。
        ///
        /// 容错说明：与 JsonToObject 相同，本方法内部已捕获所有异常，
        ///          调用方只需判断返回值是否为空串。
        /// </summary>
        /// <typeparam name="T">
        /// 要序列化的对象类型。可以是实体类，也可以是集合、字典等。
        /// 注意：只有【公开属性】会被序列化（字段和私有成员不会输出）。
        /// </typeparam>
        /// <param name="obj">待转换的对象实例</param>
        /// <returns>
        /// 转换成功 → JSON 文本（带缩进，中文不转义）；
        /// 输入为 null 或转换失败 → 空字符串
        /// </returns>
        public static async Task<string> ObjectToJson<T>(T obj)
        {
            // ---------- 前置检查：空对象直接返回，避免生成 "null" 这种无意义文本 ----------
            if (obj == null)
            {
                Logger.Warn("ObjectToJson：传入的对象为 null，已跳过序列化", "JsonTools");
                return string.Empty;
            }

            try
            {
                // ---------- 放到线程池执行，避免大对象序列化时界面卡顿 ----------
                return await Task.Run(() => JsonSerializer.Serialize(obj, _options));
            }
            catch (Exception ex)
            {
                // 序列化失败的常见原因：对象存在循环引用、属性为不支持的类型等
                Logger.Error($"对象序列化为 JSON 失败：{ex.Message}", ex, "JsonTools");
                return string.Empty;
            }
        }
    }
}
