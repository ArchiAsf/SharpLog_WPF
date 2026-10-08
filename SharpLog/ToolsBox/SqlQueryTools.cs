using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SharpLog.DB;
using SharpLog.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// SQL 语句的执行类别。
    /// 「日志查看页 · SQL 自由查询」靠它决定要不要弹双重确认。
    /// </summary>
    public enum SqlStatementKind
    {
        /// <summary>空语句：只有空白或注释，不执行。</summary>
        Empty,

        /// <summary>只读查询（SELECT / EXPLAIN / 纯查询的 WITH），直接执行，不弹确认。</summary>
        ReadOnly,

        /// <summary>写操作（增删改、改结构等），必须双重确认后才允许执行。</summary>
        Write
    }

    /// <summary>
    /// 「查询条件」下拉框里的一个字段。
    ///
    /// 【为什么要有白名单】
    ///   SQL 里【列名不能参数化】（只有值能），所以列名只能是程序自己写死的，
    ///   绝不能来自用户输入 —— 否则就是注入漏洞。
    ///   界面上用户看到的永远是 DisplayName，真正拼进 SQL 的只有这里的 ColumnName。
    /// </summary>
    public sealed class QueryFieldOption
    {
        /// <summary>界面显示名（中文）。</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>对应的数据库列名。取自白名单，用户无法输入。</summary>
        public string ColumnName { get; init; } = string.Empty;

        /// <summary>true = 数值列（值按数字比较）；false = 文本列（可做 LIKE 模糊匹配）。</summary>
        public bool IsNumeric { get; init; }
    }

    /// <summary>
    /// 日志查看页「查询条件 / 筛选器 / SQL 自由查询」的 SQL 组装与执行工具。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【安全约定】（这是本类存在的首要理由，改代码时务必守住）
    ///   1. 【值一律参数化】—— 任何来自用户的字符串都通过 SqliteParameter 传，
    ///      绝不拼进 SQL 文本。拼接的只有【列名】和【运算符】。
    ///   2. 【列名走白名单】—— 见 QueryFields，用户只能选，不能写。
    ///   3. 【运算符走枚举映射】—— 见 OperatorToSql，不接收原始字符串。
    ///   4. 【写操作必须确认】—— Classify 会把非只读语句判为 Write，
    ///      由 ViewModel 弹双重确认；本类只负责"判断"，不负责"拦"。
    ///   5. 【行数上限与超时】—— MaxRows / TimeoutSeconds，
    ///      防止 SELECT 自连接把内存吃满、UI 冻住。
    /// ════════════════════════════════════════════════════════════════════════
    /// </summary>
    public static class SqlQueryTools
    {
        /// <summary>单次查询最多返回的行数（超出的部分不取，SQL 里也不加 LIMIT，靠读取时截断）。</summary>
        public const int MaxRows = 500;

        /// <summary>命令超时（秒）。超时后抛异常，由调用方转成错误提示。</summary>
        public const int TimeoutSeconds = 5;

        /// <summary>
        /// 「波段」这个伪列的标记。
        /// 波段不是数据库列（由 FREQ 换算），所以在白名单里用一个不可能与真实列名冲突的标记，
        /// 拼 WHERE 时单独走频率区间的分支。
        /// </summary>
        private const string BandColumnToken = "__BAND__";

        /// <summary>
        /// 「查询条件」可选的字段白名单，也是界面「字段」下拉框的数据源。
        /// ⚠ 顺序就是下拉框里的显示顺序。
        /// </summary>
        public static IReadOnlyList<QueryFieldOption> QueryFields { get; } = new List<QueryFieldOption>
        {
            new() { DisplayName = "日志 ID",     ColumnName = "Id",          IsNumeric = true  },
            new() { DisplayName = "呼号",        ColumnName = "CALL",        IsNumeric = false },
            new() { DisplayName = "日期",        ColumnName = "QSO_DATE",    IsNumeric = false },
            new() { DisplayName = "时间",        ColumnName = "TIME_ON",     IsNumeric = false },
            new() { DisplayName = "频率(kHz)",   ColumnName = "FREQ",        IsNumeric = true  },
            new() { DisplayName = "波段",        ColumnName = BandColumnToken, IsNumeric = false },
            new() { DisplayName = "模式",        ColumnName = "MODE",        IsNumeric = false },
            new() { DisplayName = "传播模式",    ColumnName = "PROP_MODE",   IsNumeric = false },
            new() { DisplayName = "网格",        ColumnName = "GRIDSQUARE",  IsNumeric = false },
            new() { DisplayName = "对方城市",    ColumnName = "QTH",         IsNumeric = false },
            new() { DisplayName = "信号报告(发)", ColumnName = "RST_SENT",    IsNumeric = false },
            new() { DisplayName = "信号报告(收)", ColumnName = "RST_RCVD",    IsNumeric = false },
            new() { DisplayName = "QSL 已收",    ColumnName = "QSL_RCVD",    IsNumeric = false },
            new() { DisplayName = "备注",        ColumnName = "COMMENT",     IsNumeric = false },
        };

        /// <summary>
        /// 会被判为「写操作」的关键字。
        /// ⚠ 这里列的是【保守清单】—— 宁可多让用户确认一次，也不要放过一条危险语句。
        /// </summary>
        private static readonly string[] WriteKeywords =
        {
            "INSERT", "UPDATE", "DELETE", "REPLACE", "DROP", "ALTER", "CREATE",
            "TRUNCATE", "VACUUM", "REINDEX", "ATTACH", "DETACH", "PRAGMA"
        };

        // ====================================================================
        //  一、语句分类
        // ====================================================================

        /// <summary>
        /// 判断一段 SQL 属于哪一类。
        ///
        /// 【为什么要按分号拆开逐条判断】
        ///   SQLite 允许一次执行多条语句（用分号隔开），
        ///   所以 "SELECT 1; DROP TABLE LogTable" 这种写法，
        ///   只看开头第一个关键字会把它误判成只读查询。
        ///   ⇒ 必须逐条判断，只要【有一条】是写操作，整体就算写操作。
        /// </summary>
        /// <param name="sql">用户输入的原始 SQL</param>
        /// <returns>语句类别</returns>
        public static SqlStatementKind Classify(string? sql)
        {
            List<string> statements = SplitStatements(sql);
            if (statements.Count == 0)
            {
                return SqlStatementKind.Empty;
            }

            foreach (string statement in statements)
            {
                if (IsWriteStatement(statement))
                {
                    return SqlStatementKind.Write;
                }
            }

            return SqlStatementKind.ReadOnly;
        }

        /// <summary>
        /// 把多语句 SQL 拆成单条（去掉注释、去掉空段）。
        /// </summary>
        private static List<string> SplitStatements(string? sql)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(sql))
            {
                return result;
            }

            string cleaned = StripComments(sql);
            foreach (string part in cleaned.Split(';'))
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }

        /// <summary>
        /// 去掉 SQL 注释（-- 行注释 与 /* */ 块注释），保留字符串字面量里的原样内容。
        /// </summary>
        private static string StripComments(string sql)
        {
            var sb = new StringBuilder(sql.Length);
            bool inLineComment = false;
            bool inBlockComment = false;
            bool inString = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                char next = i + 1 < sql.Length ? sql[i + 1] : '\0';

                if (inLineComment)
                {
                    // 行注释到行尾为止，换行符本身保留（否则会把两条语句粘在一起）
                    if (c == '\n')
                    {
                        inLineComment = false;
                        sb.Append(c);
                    }
                    continue;
                }

                if (inBlockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }
                    continue;
                }

                if (inString)
                {
                    sb.Append(c);
                    if (c == '\'')
                    {
                        inString = false;
                    }
                    continue;
                }

                if (c == '\'')
                {
                    inString = true;
                    sb.Append(c);
                    continue;
                }

                if (c == '-' && next == '-')
                {
                    inLineComment = true;
                    i++;
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 把字符串字面量替换成空白（保留长度结构）。
        ///
        /// 【用途】判断语句里有没有写操作关键字时，必须先把字符串内容抹掉 ——
        ///   否则 SELECT * FROM LogTable WHERE COMMENT = 'DELETE' 这种
        ///   正常查询会因为备注里写了 DELETE 而被误判成写操作。
        /// </summary>
        private static string StripStringLiterals(string sql)
        {
            var sb = new StringBuilder(sql.Length);
            bool inString = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                if (inString)
                {
                    // SQL 里两个连续单引号表示一个转义的单引号
                    if (c == '\'')
                    {
                        if (i + 1 < sql.Length && sql[i + 1] == '\'')
                        {
                            i++;
                            continue;
                        }
                        inString = false;
                    }
                    continue;
                }

                if (c == '\'')
                {
                    inString = true;
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>取一段语句的第一个关键字（大写）。</summary>
        private static string FirstKeyword(string sql)
        {
            int i = 0;
            while (i < sql.Length && char.IsWhiteSpace(sql[i]))
            {
                i++;
            }

            int start = i;
            while (i < sql.Length && (char.IsLetter(sql[i]) || sql[i] == '_'))
            {
                i++;
            }

            return sql.Substring(start, i - start).ToUpperInvariant();
        }

        /// <summary>判断单条语句是不是写操作。</summary>
        private static bool IsWriteStatement(string statement)
        {
            string noLiteral = StripStringLiterals(statement).ToUpperInvariant();
            string first = FirstKeyword(noLiteral);

            // SELECT 与 EXPLAIN 一定只读
            if (first == "SELECT" || first == "EXPLAIN")
            {
                return false;
            }

            // WITH（CTE）：可能是 WITH ... SELECT，也可能是 WITH ... DELETE / UPDATE / INSERT，
            // 只看首关键字判断不了，所以扫全文找写关键字
            if (first == "WITH")
            {
                return WriteKeywords.Any(kw => ContainsKeyword(noLiteral, kw));
            }

            // 其余一律当写操作处理（保守策略：宁可多一次确认，也不放过危险语句）
            return true;
        }

        /// <summary>
        /// 按词边界判断关键字是否出现（避免把 UPDATED_AT 这类列名误判成 UPDATE）。
        /// </summary>
        private static bool ContainsKeyword(string upperSql, string keyword)
        {
            int from = 0;
            while (true)
            {
                int idx = upperSql.IndexOf(keyword, from, StringComparison.Ordinal);
                if (idx < 0)
                {
                    return false;
                }

                bool leftOk = idx == 0 || !(char.IsLetterOrDigit(upperSql[idx - 1]) || upperSql[idx - 1] == '_');
                int end = idx + keyword.Length;
                bool rightOk = end >= upperSql.Length
                               || !(char.IsLetterOrDigit(upperSql[end]) || upperSql[end] == '_');

                if (leftOk && rightOk)
                {
                    return true;
                }

                from = idx + 1;
            }
        }

        // ====================================================================
        //  二、执行 SQL
        // ====================================================================

        /// <summary>
        /// 执行一段 SQL。
        ///
        /// 【返回值的三种情形】
        ///   · 只读查询  → Table 有值、AffectedRows = 返回行数
        ///   · 写操作    → Table 为 null、AffectedRows = 影响行数
        ///   · 执行失败  → Success = false、Message 是错误原因
        ///
        /// ⚠ 本方法【不做任何权限判断】—— 该不该执行由调用方（ViewModel 弹确认框）决定。
        /// </summary>
        /// <param name="sql">要执行的 SQL</param>
        /// <returns>(是否成功, 结果表, 影响/返回行数, 提示信息)</returns>
        public static (bool Success, DataTable? Table, int AffectedRows, string Message) Execute(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return (false, null, 0, "SQL 语句为空。");
            }

            try
            {
                // 连接串不写死 —— 从 DbContext 取，保证和 EF 用的是同一个库
                using var db = new SharpLogDbContext();
                string connectionString = db.Database.GetConnectionString() ?? "Data Source=SharpLog.db";

                using var conn = new SqliteConnection(connectionString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = TimeoutSeconds;

                using var reader = cmd.ExecuteReader();

                // ── 写操作：FieldCount 为 0 ──
                if (reader.FieldCount == 0)
                {
                    // ⚠ 必须把结果集读完，语句才真正执行完毕，RecordsAffected 也才是最终值
                    while (reader.Read())
                    {
                    }

                    int affected = reader.RecordsAffected;
                    return (true, null, affected, $"执行成功，影响 {affected} 行。");
                }

                // ── 只读查询：把结果装进 DataTable（列是动态的，所以用 DataTable 而不是实体） ──
                var table = new DataTable();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    table.Columns.Add(reader.GetName(i), typeof(object));
                }

                int rows = 0;
                while (rows < MaxRows && reader.Read())
                {
                    DataRow row = table.NewRow();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    }
                    table.Rows.Add(row);
                    rows++;
                }

                string message = rows >= MaxRows
                    ? $"返回 {rows} 行（已达上限 {MaxRows}，只显示前 {MaxRows} 行）。"
                    : $"返回 {rows} 行。";

                return (true, table, rows, message);
            }
            catch (Exception ex)
            {
                // 错误直接给用户看（SQLite 的报错本身带行号与原因，对写 SQL 的人有用）
                return (false, null, 0, ex.Message);
            }
        }

        // ====================================================================
        //  三、查询条件 → WHERE 子句
        // ====================================================================

        /// <summary>
        /// 把「查询条件行」翻译成 WHERE 子句（不含 WHERE 关键字本身）。
        ///
        /// 【空值行的处理】值为空的行直接跳过 —— 用户点了「添加条件」还没填的那种行，
        ///   不应该把结果集过滤成空。
        ///
        /// 【全部为空时】返回空串，调用方据此不加 WHERE（即显示全部）。
        /// </summary>
        /// <param name="conditions">(字段, 运算符, 值) 三元组序列</param>
        /// <returns>(是否成功, WHERE 子句, 参数列表, 错误信息)</returns>
        public static (bool Success, string Where, List<SqliteParameter> Parameters, string Message) BuildWhere(
            IEnumerable<(QueryFieldOption Field, QueryOperator Op, string Value)> conditions)
        {
            var sb = new StringBuilder();
            var parameters = new List<SqliteParameter>();
            int index = 0;

            foreach ((QueryFieldOption field, QueryOperator op, string value) in conditions)
            {
                if (field == null || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                string trimmed = value.Trim();

                // ── 波段：不是数据库列，换算成频率区间（FREQ 存的是 kHz，波段表是 MHz） ──
                if (field.ColumnName == BandColumnToken)
                {
                    AdifBand? band = AdifDataLoader.Bands?
                        .FirstOrDefault(b => string.Equals(b.Band, trimmed, StringComparison.OrdinalIgnoreCase));

                    if (band == null)
                    {
                        return (false, string.Empty, parameters,
                            $"未知波段「{trimmed}」。请填写 ADIF 波段名，例如 20m、40m、2m。");
                    }

                    string lowName = "@q" + index++;
                    string highName = "@q" + index++;

                    if (sb.Length > 0)
                    {
                        sb.Append(" AND ");
                    }

                    sb.Append("FREQ >= ").Append(lowName).Append(" AND FREQ <= ").Append(highName);

                    parameters.Add(new SqliteParameter(lowName, band.LowerFreqMhz * 1000.0));
                    parameters.Add(new SqliteParameter(highName, band.UpperFreqMhz * 1000.0));
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(" AND ");
                }

                // ── 数值列 ──
                if (field.IsNumeric)
                {
                    if (!double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                    {
                        return (false, string.Empty, parameters,
                            $"字段「{field.DisplayName}」需要填写数字，当前值「{trimmed}」无法解析。");
                    }

                    string pname = "@q" + index++;
                    sb.Append(field.ColumnName).Append(' ').Append(OperatorToSql(op)).Append(' ').Append(pname);
                    parameters.Add(new SqliteParameter(pname, number));
                    continue;
                }

                // ── 文本列：模糊匹配走 LIKE，其余走普通比较 ──
                if (op == QueryOperator.Contains || op == QueryOperator.StartsWith || op == QueryOperator.EndsWith)
                {
                    string pattern = op switch
                    {
                        QueryOperator.Contains => "%" + EscapeLike(trimmed) + "%",
                        QueryOperator.StartsWith => EscapeLike(trimmed) + "%",
                        _ => "%" + EscapeLike(trimmed)
                    };

                    string pname = "@q" + index++;
                    sb.Append(field.ColumnName).Append(" LIKE ").Append(pname).Append(" ESCAPE '\\'");
                    parameters.Add(new SqliteParameter(pname, pattern));
                    continue;
                }

                {
                    string pname = "@q" + index++;
                    sb.Append(field.ColumnName).Append(' ').Append(OperatorToSql(op)).Append(' ').Append(pname);
                    parameters.Add(new SqliteParameter(pname, trimmed));
                }
            }

            return (true, sb.ToString(), parameters, string.Empty);
        }

        /// <summary>
        /// 把「筛选器」翻译成 WHERE 子句。
        ///
        /// 【与查询条件的关系】两者是【并且】的关系：
        ///   结果 = 满足筛选器 AND 满足查询条件。
        ///   筛选器持续生效（且持久化），查询条件可以随时清空。
        /// </summary>
        /// <param name="call">呼号（模糊匹配），空表示不限</param>
        /// <param name="mode">模式（精确匹配），空表示不限</param>
        /// <param name="band">波段（如 20m），空表示不限</param>
        /// <param name="onlyConfirmed">只看已双向确认</param>
        /// <param name="onlyToday">只看今天（按本地日期）</param>
        public static (bool Success, string Where, List<SqliteParameter> Parameters, string Message) BuildFilterWhere(
            string? call, string? mode, string? band, bool onlyConfirmed, bool onlyToday)
        {
            var sb = new StringBuilder();
            var parameters = new List<SqliteParameter>();
            int index = 0;

            if (!string.IsNullOrWhiteSpace(call))
            {
                string pname = "@f" + index++;
                if (sb.Length > 0)
                {
                    sb.Append(" AND ");
                }
                sb.Append("CALL LIKE ").Append(pname).Append(" ESCAPE '\\'");
                parameters.Add(new SqliteParameter(pname, "%" + EscapeLike(call.Trim()) + "%"));
            }

            if (!string.IsNullOrWhiteSpace(mode))
            {
                string pname = "@f" + index++;
                if (sb.Length > 0)
                {
                    sb.Append(" AND ");
                }
                sb.Append("MODE = ").Append(pname);
                parameters.Add(new SqliteParameter(pname, mode.Trim().ToUpperInvariant()));
            }

            if (!string.IsNullOrWhiteSpace(band))
            {
                AdifBand? bandInfo = AdifDataLoader.Bands?
                    .FirstOrDefault(b => string.Equals(b.Band, band.Trim(), StringComparison.OrdinalIgnoreCase));

                if (bandInfo == null)
                {
                    return (false, string.Empty, parameters, $"未知波段「{band}」。");
                }

                string lowName = "@f" + index++;
                string highName = "@f" + index++;
                if (sb.Length > 0)
                {
                    sb.Append(" AND ");
                }
                sb.Append("FREQ >= ").Append(lowName).Append(" AND FREQ <= ").Append(highName);
                parameters.Add(new SqliteParameter(lowName, bandInfo.LowerFreqMhz * 1000.0));
                parameters.Add(new SqliteParameter(highName, bandInfo.UpperFreqMhz * 1000.0));
            }

            if (onlyConfirmed)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" AND ");
                }
                sb.Append("IsConfirmed = 1");
            }

            if (onlyToday)
            {
                // QSO_DATE 按 ADIF 存成 "yyyyMMdd" 定长文本，本地当天直接用同样格式比较
                string pname = "@f" + index++;
                if (sb.Length > 0)
                {
                    sb.Append(" AND ");
                }
                sb.Append("QSO_DATE = ").Append(pname);
                parameters.Add(new SqliteParameter(pname, DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)));
            }

            return (true, sb.ToString(), parameters, string.Empty);
        }

        /// <summary>
        /// 按 WHERE 子句查日志（返回实体列表，供左侧【手工列】表格显示）。
        ///
        /// ⚠ 只用 EF 的 FromSqlRaw 而不是自己拼 SELECT *：
        ///   这样实体字段与数据库列的映射关系仍然由 EF 负责，将来加字段不用改这里。
        /// ⚠ 参数仍走 SqliteParameter，不拼字符串。
        /// </summary>
        /// <param name="where">WHERE 子句（不含 WHERE 关键字，可为空）</param>
        /// <param name="parameters">参数列表</param>
        /// <param name="limit">最多返回的行数</param>
        public static (bool Success, List<QsoLog> Data, string Message) QueryLogs(
            string? where, List<SqliteParameter> parameters, int limit)
        {
            try
            {
                using var db = new SharpLogDbContext();

                string sql = "SELECT * FROM LogTable";
                if (!string.IsNullOrWhiteSpace(where))
                {
                    sql += " WHERE " + where;
                }
                sql += " ORDER BY Id DESC LIMIT " + limit.ToString(CultureInfo.InvariantCulture);

                List<QsoLog> data = db.LogTables
                    .FromSqlRaw(sql, parameters.Cast<object>().ToArray())
                    .AsNoTracking()
                    .ToList();

                return (true, data, $"共 {data.Count} 条。");
            }
            catch (Exception ex)
            {
                return (false, new List<QsoLog>(), ex.Message);
            }
        }

        // ====================================================================
        //  四、辅助
        // ====================================================================

        /// <summary>运算符枚举 → SQL 比较符。只接受枚举，不接受原始字符串。</summary>
        private static string OperatorToSql(QueryOperator op) => op switch
        {
            QueryOperator.Equal => "=",
            QueryOperator.NotEqual => "<>",
            QueryOperator.Greater => ">",
            QueryOperator.Less => "<",
            QueryOperator.GreaterOrEqual => ">=",
            QueryOperator.LessOrEqual => "<=",
            _ => "="
        };

        /// <summary>
        /// 转义 LIKE 模式里的通配符。
        /// ⚠ 反斜杠必须【先】替换，否则会把后面新加的反斜杠又转义一遍。
        /// </summary>
        private static string EscapeLike(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }
    }
}
