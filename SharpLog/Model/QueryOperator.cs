namespace SharpLog.Model
{
    /// <summary>
    /// 「查询条件行」里可选的比较运算符。
    ///
    /// 【为什么放在 Model 层而不是 ViewModel 层】
    ///   这个枚举同时被两处使用：
    ///     · ViewModel —— 界面上的「运算符」下拉框（绑定用）
    ///     · ToolsBox\SqlQueryTools —— 翻译成 SQL 的比较符
    ///   如果放在 ViewModel 层，工具类就得反向引用 ViewModel，层次就颠倒了。
    ///
    /// 【对应的 SQL 写法】见 SqlQueryTools.OperatorToSql
    /// </summary>
    public enum QueryOperator
    {
        /// <summary>等于（SQL: =）</summary>
        Equal,

        /// <summary>不等于（SQL: &lt;&gt;）</summary>
        NotEqual,

        /// <summary>大于（SQL: &gt;）</summary>
        Greater,

        /// <summary>小于（SQL: &lt;）</summary>
        Less,

        /// <summary>大于等于（SQL: &gt;=）</summary>
        GreaterOrEqual,

        /// <summary>小于等于（SQL: &lt;=）</summary>
        LessOrEqual,

        /// <summary>包含（文本专用，SQL: LIKE %值%）</summary>
        Contains,

        /// <summary>开头是（文本专用，SQL: LIKE 值%）</summary>
        StartsWith,

        /// <summary>结尾是（文本专用，SQL: LIKE %值）</summary>
        EndsWith
    }
}
