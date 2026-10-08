using CommunityToolkit.Mvvm.ComponentModel;
using SharpLog.Model;
using SharpLog.ToolsBox;
using System.Collections.Generic;

namespace SharpLog.ViewModel
{
    /// <summary>
    /// 「查询条件」列表里的一行：字段 + 运算符 + 值。
    ///
    /// 【为什么是独立类而不是三个平行集合】
    ///   界面上这三者是【同一行】的三个控件，用 ObservableCollection&lt;本类&gt; +
    ///   ItemsControl 生成，增删行时只需操作一个集合。
    ///   若拆成三个平行集合，增删时还要手工保持三者下标同步，容易错位。
    ///
    /// 【为什么要有 INotifyPropertyChanged】
    ///   数据模板里的控件直接绑到本类属性上，用户改一个下拉框，
    ///   界面要立刻反映 —— 所以它必须是可观察对象。
    /// </summary>
    public partial class QueryConditionRow : ObservableObject
    {
        /// <summary>
        /// 本行选择的字段（绑定「字段」下拉框的 SelectedItem）。
        /// 取值来自 SqlQueryTools.QueryFields 白名单。
        /// </summary>
        [ObservableProperty]
        private QueryFieldOption? _selectedField;

        /// <summary>
        /// 本行选择的运算符（绑定「运算符」下拉框的 SelectedItem）。
        /// </summary>
        [ObservableProperty]
        private QueryOperatorItem _selectedOperator;

        /// <summary>
        /// 本行填写的比较值。
        /// ⚠ 值一律作为 SQL 参数传入，不参与拼接，所以这里可以随便填。
        /// </summary>
        [ObservableProperty]
        private string _value = string.Empty;

        /// <summary>
        /// 新建一行时把运算符预设为「=」，避免下拉框空白。
        /// </summary>
        public QueryConditionRow()
        {
            _selectedOperator = QueryOperatorItem.All[0];
        }
    }

    /// <summary>
    /// 「运算符」下拉框的一项。
    ///
    /// 【为什么要包一层】
    ///   下拉框要显示的是符号（= &gt; &lt; ...），而拼 SQL 用的是 QueryOperator 枚举。
    ///   直接把枚举绑上去，界面会显示 "GreaterOrEqual" 这种名字。
    /// </summary>
    public sealed class QueryOperatorItem
    {
        /// <summary>界面显示名（就是符号本身）。</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>对应的枚举值，供 SqlQueryTools.BuildWhere 使用。</summary>
        public QueryOperator Value { get; init; }

        /// <summary>下拉框的完整数据源。</summary>
        public static IReadOnlyList<QueryOperatorItem> All { get; } = new List<QueryOperatorItem>
        {
            new() { DisplayName = "=",      Value = QueryOperator.Equal },
            new() { DisplayName = "!=",     Value = QueryOperator.NotEqual },
            new() { DisplayName = ">",      Value = QueryOperator.Greater },
            new() { DisplayName = "<",      Value = QueryOperator.Less },
            new() { DisplayName = ">=",     Value = QueryOperator.GreaterOrEqual },
            new() { DisplayName = "<=",     Value = QueryOperator.LessOrEqual },
            new() { DisplayName = "包含",   Value = QueryOperator.Contains },
            new() { DisplayName = "开头是", Value = QueryOperator.StartsWith },
            new() { DisplayName = "结尾是", Value = QueryOperator.EndsWith }
        };
    }
}
