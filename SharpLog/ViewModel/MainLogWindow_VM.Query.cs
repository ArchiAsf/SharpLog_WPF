using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpLog.DB;
using SharpLog.Infrastructure;
using SharpLog.Model;
using SharpLog.ToolsBox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static SharpLog.ToolsBox.MessageTools;

// ══════════════════════════════════════════════════════════════════════════
//  MainLogWindow_VM 的「日志查看页 · 查询 / 筛选 / 统计」部分（partial）
//  ──────────────────────────────────────────────────────────────────────────
//  本文件装的是右侧查询面板要用的全部逻辑：
//    · 查询条件（动态增删的行）
//    · 筛选器（持久化到 AppSettings 表，关软件再开还在）
//    · SQL 自由查询（只读直接跑，写操作要双重确认）
//    · 统计区（总数 + CQ / ITU / DXCC 分区数）
//
//  与之相对：
//    · 绑定属性、字段、构造函数 → MainLogWindow_VM.cs
//    · 定时触发、命令、日志查看页分页 → MainLogWindow_VM.Methods.cs
//    · 输入校验、界面提示 → MainLogWindow_VM.Helpers.cs
//
//  ⚠ 与 SqlQueryTools 的分工：
//     本文件只管【界面状态与交互】；SQL 怎么拼、怎么判危险、怎么执行，
//     全部在 ToolsBox\SqlQueryTools 里，本文件不拼任何 SQL 文本。
// ══════════════════════════════════════════════════════════════════════════

namespace SharpLog.ViewModel
{
    public partial class MainLogWindow_VM
    {
        /// <summary>
        /// 查询结果的最大行数。
        /// 与日志查看页的分页大小保持一致，避免查询模式下把整库拉进内存。
        /// </summary>
        private const int MaxQueryRows = 2000;

        #region 一、查询条件（动态行）

        /// <summary>
        /// 当前的查询条件行集合，绑定到界面上的 ItemsControl。
        /// 默认放一行空的，用户点「+ 添加条件」往里加。
        /// </summary>
        public ObservableCollection<QueryConditionRow> QueryConditions { get; } = new()
        {
            // 默认放一行空的 —— 用户一切到这一页就能直接填，不用先点「+ 添加条件」
            new QueryConditionRow()
        };

        /// <summary>「字段」下拉框的数据源（白名单，见 SqlQueryTools.QueryFields）。</summary>
        public IReadOnlyList<QueryFieldOption> QueryFieldOptions => SqlQueryTools.QueryFields;

        /// <summary>「运算符」下拉框的数据源。</summary>
        public IReadOnlyList<QueryOperatorItem> QueryOperatorOptions => QueryOperatorItem.All;

        /// <summary>新增一行空条件。</summary>
        [RelayCommand]
        private void AddQueryCondition()
        {
            QueryConditions.Add(new QueryConditionRow());
        }

        /// <summary>删掉指定的一行条件（界面每行末尾的 ✕ 按钮）。</summary>
        [RelayCommand]
        private void RemoveQueryCondition(QueryConditionRow? row)
        {
            if (row == null)
            {
                return;
            }

            QueryConditions.Remove(row);

            // 全删光了就补一行空的，免得界面上出现一片空白、用户不知道从哪加
            if (QueryConditions.Count == 0)
            {
                QueryConditions.Add(new QueryConditionRow());
            }
        }

        /// <summary>
        /// 清空全部查询条件，并恢复显示（此时只剩筛选器生效）。
        ///
        /// ⚠ 筛选器【不清空】—— 用户要求筛选器是持续生效的，
        ///   所以清空查询后显示的仍然是"满足筛选器的全部记录"。
        /// </summary>
        [RelayCommand]
        private void ClearQueryConditions()
        {
            QueryConditions.Clear();
            QueryConditions.Add(new QueryConditionRow());
            ApplyQuery();
        }

        /// <summary>
        /// 按当前【筛选器 + 查询条件】重新查询，结果替换掉左侧日志列表。
        ///
        /// 【两者的关系】结果必须【同时满足】筛选器与查询条件（AND 关系）。
        /// 【空条件】没有任何条件时等价于"显示全部（受筛选器限制）"。
        /// </summary>
        [RelayCommand]
        private void ApplyQuery()
        {
            // ── ① 筛选器 → WHERE ──
            // ⚠ 下拉框里的「（全部）」是"不限"的意思，传给工具类前要转成空串
            var filter = SqlQueryTools.BuildFilterWhere(
                FilterCall,
                FilterMode == AnyOption ? string.Empty : FilterMode,
                FilterBand == AnyOption ? string.Empty : FilterBand,
                FilterOnlyConfirmed,
                FilterOnlyToday);

            if (!filter.Success)
            {
                ShowErrorMess(MessagePage.LogView, filter.Message);
                return;
            }

            // ── ② 查询条件 → WHERE（值为空的行会被工具类自动跳过） ──
            var conditions = QueryConditions
                .Where(c => c.SelectedField != null)
                .Select(c => (c.SelectedField!, c.SelectedOperator.Value, c.Value));

            var query = SqlQueryTools.BuildWhere(conditions);
            if (!query.Success)
            {
                ShowErrorMess(MessagePage.LogView, query.Message);
                return;
            }

            // ── ③ 合并两段 WHERE ──
            string where = MergeWhere(filter.Where, query.Where);
            var parameters = filter.Parameters.Concat(query.Parameters).ToList();

            // ── ④ 查询 ──
            var (success, data, message) = SqlQueryTools.QueryLogs(where, parameters, MaxQueryRows);
            if (!success)
            {
                ShowErrorMess(MessagePage.LogView, message);
                return;
            }

            // ── ⑤ 结果替换左侧列表 ──
            LogViewRows.Clear();
            LogViewRows.AddRange(data);

            // ⚠ 查询模式下关掉分页：结果已经一次取全，
            //   若不关掉，滚到底还会去加载"下一页"，把查询结果又冲掉一截。
            _hasMoreLogViewData = false;
            _logViewCursorId = data.Count > 0 ? data[^1].Id : int.MaxValue;

            // 切回手工列视图（从 SQL 结果模式返回）
            IsSqlResultMode = false;

            UpdateLoadMoreText();

            // ── ⑥ 筛选器落库（用户要求关软件再开还在） ──
            SaveFilterToSettings();

            // ── ⑦ 统计刷新 ──
            RefreshStatistics();

            if (data.Count >= MaxQueryRows)
            {
                _ = ShowErrorMess(MessagePage.LogView,
                    $"结果超过 {MaxQueryRows} 条，只显示前 {MaxQueryRows} 条，请再补充条件缩小范围。");
            }
        }

        /// <summary>
        /// 把两段 WHERE 用 AND 连起来。
        /// ⚠ 各自都要加括号 —— 否则 A OR B AND C 会因为优先级（AND 高于 OR）改变语义。
        /// </summary>
        private static string MergeWhere(string first, string second)
        {
            bool hasFirst = !string.IsNullOrWhiteSpace(first);
            bool hasSecond = !string.IsNullOrWhiteSpace(second);

            if (hasFirst && hasSecond)
            {
                return "(" + first + ") AND (" + second + ")";
            }

            return hasFirst ? first : second;
        }

        #endregion

        #region 二、筛选器（持久化到 AppSettings 表）

        /// <summary>筛选器：呼号（模糊匹配）。空表示不限。</summary>
        [ObservableProperty]
        private string _FilterCall = string.Empty;

        /// <summary>筛选器：模式（精确匹配）。空表示不限。</summary>
        [ObservableProperty]
        private string _FilterMode = AnyOption;

        /// <summary>筛选器：波段（如 20m）。取值为「（全部）」时表示不限。</summary>
        [ObservableProperty]
        private string _FilterBand = AnyOption;

        /// <summary>筛选器：只看已双向确认的 QSL。</summary>
        [ObservableProperty]
        private bool _FilterOnlyConfirmed;

        /// <summary>筛选器：只看今天的通联。</summary>
        [ObservableProperty]
        private bool _FilterOnlyToday;

        /// <summary>
        /// 「不限」在下拉框里的显示文字。
        /// ⚠ 不能直接用空串当选项 —— 那样下拉框选到它时是一片空白，
        ///   用户看不出"这里是不限的意思"。
        /// </summary>
        private const string AnyOption = "（全部）";

        /// <summary>「模式」筛选下拉框的选项（第一项表示不限）。</summary>
        public IReadOnlyList<string> FilterModeOptions { get; } = new List<string>
        {
            AnyOption, "SSB", "CW", "FT8", "FT4", "FM", "AM", "RTTY", "PSK31", "JT65", "DMR"
        };

        /// <summary>「波段」筛选下拉框的选项（第一项表示不限）。</summary>
        public IReadOnlyList<string> FilterBandOptions { get; } = new List<string>
        {
            AnyOption, "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m",
            "6m", "4m", "2m", "1.25m", "70cm", "33cm", "23cm"
        };

        /// <summary>
        /// 从数据库读回上次保存的筛选器。程序启动时调用一次。
        /// ⚠ 数据库里还没有设置行时返回 null，属正常情况，什么都不做即可。
        /// </summary>
        public void LoadFilterFromSettings()
        {
            try
            {
                // AppSettingsDbTools 内部已 ConfigureAwait(false)，UI 线程同步等待不会死锁
                var (settings, _) = AppSettingsDbTools.LoadAsync().Result;
                if (settings == null)
                {
                    return;
                }

                FilterCall = settings.FilterCall ?? string.Empty;
                // 库里存的是 null/空串（表示不限），界面上要显示成「（全部）」
                FilterMode = string.IsNullOrEmpty(settings.FilterMode) ? AnyOption : settings.FilterMode;
                FilterBand = string.IsNullOrEmpty(settings.FilterBand) ? AnyOption : settings.FilterBand;
                FilterOnlyConfirmed = settings.FilterOnlyConfirmed;
                FilterOnlyToday = settings.FilterOnlyToday;
            }
            catch (Exception ex)
            {
                Logger.Error($"读取筛选器设置失败：{ex.Message}", ex, "LogQuery");
            }
        }

        /// <summary>
        /// 把当前筛选器写回数据库。
        ///
        /// ⚠ 先把已有的设置整行读出来，只改这 5 个字段再存回去 ——
        ///   这样站台设置等其它字段不会被清掉。
        /// ⚠ 保存时机选在「应用查询」时，而不是每个字符的 Changed 回调里：
        ///   后者会在用户输入呼号时每敲一个字就写一次库。
        /// </summary>
        private void SaveFilterToSettings()
        {
            _ = SaveFilterToSettingsAsync();
        }

        private async Task SaveFilterToSettingsAsync()
        {
            try
            {
                var (existing, _) = await AppSettingsDbTools.LoadAsync();
                AppSettings target = existing ?? new AppSettings();

                target.FilterCall = FilterCall;
                // 界面上的「（全部）」存回库里要变回 null，表示"没有设置"
                target.FilterMode = FilterMode == AnyOption ? null : FilterMode;
                target.FilterBand = FilterBand == AnyOption ? null : FilterBand;
                target.FilterOnlyConfirmed = FilterOnlyConfirmed;
                target.FilterOnlyToday = FilterOnlyToday;

                var (success, message) = await AppSettingsDbTools.SaveAsync(target);
                if (!success)
                {
                    Logger.Error($"保存筛选器设置失败：{message}", null, "LogQuery");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"保存筛选器设置异常：{ex.Message}", ex, "LogQuery");
            }
        }

        #endregion

        #region 三、SQL 自由查询

        /// <summary>用户在 SQL 框里输入的语句。</summary>
        [ObservableProperty]
        private string _SqlText = string.Empty;

        /// <summary>
        /// SQL 只读查询的结果表（列是动态的，所以用 DataTable 而不是实体集合）。
        /// </summary>
        [ObservableProperty]
        private DataTable? _SqlResultTable;

        /// <summary>
        /// true = 左侧显示【SQL 查询结果】（自动生成列）；
        /// false = 左侧显示【日志列表】（手工定义的 38 列）。
        /// 两者是叠在同一位置的两个 DataGrid，靠它切换可见性。
        /// </summary>
        [ObservableProperty]
        private bool _isSqlResultMode;

        /// <summary>
        /// 「执行查询」按钮 —— 把「条件查询」和「SQL 自由查询」合二为一。
        ///
        /// 【判定顺序】（用户定的规则）
        ///   ① SQL 框有内容  → 跑 SQL 自由查询（优先，查询条件忽略）
        ///   ② SQL 空、条件也全空 → 提示用户先填点什么
        ///   ③ SQL 空、条件有内容 → 跑条件查询（同时套上筛选器）
        ///
        /// 【为什么优先 SQL】用户写了一条完整语句时，意图很明确，
        ///   不该再去清空旁边的条件行才能生效。
        /// </summary>
        [RelayCommand]
        private void ExecuteQuery()
        {
            if (!string.IsNullOrWhiteSpace(SqlText))
            {
                ExecuteSql();
                return;
            }

            bool hasCondition = QueryConditions.Any(
                c => c.SelectedField != null && !string.IsNullOrWhiteSpace(c.Value));

            if (hasCondition)
            {
                ApplyQuery();
                return;
            }

            _ = ShowErrorMess(MessagePage.LogView,
                "请先填写查询条件，或在「SQL 自由查询」里写一条语句。");
        }

        /// <summary>
        /// 执行 SQL 框里的语句。
        ///
        /// 【两类语句的差别】
        ///   · 只读（SELECT / EXPLAIN）—— 直接执行，结果切到自动列视图显示；
        ///   · 写操作（其余全部）—— 【双重确认】后才执行，执行完刷新日志列表。
        ///     ⚠ 第二道确认框里会原样显示要执行的语句，让用户看清自己写了什么。
        /// </summary>
        [RelayCommand]
        private void ExecuteSql()
        {
            string sql = SqlText;
            SqlStatementKind kind = SqlQueryTools.Classify(sql);

            if (kind == SqlStatementKind.Empty)
            {
                _ = ShowErrorMess(MessagePage.LogView, "请先输入要执行的 SQL 语句。");
                return;
            }

            // ── 写操作：双重确认 ──
            if (kind == SqlStatementKind.Write)
            {
                if (!ConfirmDangerousSql(sql))
                {
                    return;
                }
            }

            var (success, table, affectedRows, message) = SqlQueryTools.Execute(sql);

            if (!success)
            {
                _ = ShowErrorMess(MessagePage.LogView, "SQL 执行失败：" + message);
                return;
            }

            if (table != null)
            {
                // 只读查询：结果切到自动列视图
                SqlResultTable = table;
                IsSqlResultMode = true;
                _ = ShowSuccessMess(MessagePage.LogView, message);
                return;
            }

            // 写操作：数据变了，刷新列表与统计
            _ = ShowSuccessMess(MessagePage.LogView, message);
            ApplyQuery();
        }

        /// <summary>
        /// 危险语句的双重确认。
        ///
        /// ⚠ 用 HandyControl 的 MessageBox（项目规范禁止原生 MessageBox.Show）。
        /// ⚠ 第一道确认框里【原样显示用户要执行的语句】—— 让他看清自己写了什么，
        ///   这是防止"手滑执行了 DELETE"最关键的一道防线。
        /// ⚠ 两道框的默认按钮都是 No。
        /// </summary>
        /// <returns>true = 两道都点了"是"</returns>
        private static bool ConfirmDangerousSql(string sql)
        {
            var first = HandyControl.Controls.MessageBox.Show(
                "这条语句【会改动数据库】，而且没有撤销功能。\n\n"
                + "即将执行的语句：\n"
                + "————————————————\n"
                + sql + "\n"
                + "————————————————\n\n"
                + "确定要继续吗？",
                "危险操作确认（第 1 / 2 道）",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No);

            if (first != System.Windows.MessageBoxResult.Yes)
            {
                return false;
            }

            var second = HandyControl.Controls.MessageBox.Show(
                "再次确认：上面那条语句将【立即执行并写入数据库】。\n\n"
                + "误删的记录无法恢复，请确认你已经想清楚。\n\n"
                + "真的要执行吗？",
                "二次确认（第 2 / 2 道）",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Stop,
                System.Windows.MessageBoxResult.No);

            return second == System.Windows.MessageBoxResult.Yes;
        }

        /// <summary>退回日志列表视图（从 SQL 结果模式返回）。</summary>
        [RelayCommand]
        private void ExitSqlResultMode()
        {
            IsSqlResultMode = false;
            SqlResultTable = null;
        }

        #endregion

        #region 四、统计（总数 + CQ / ITU / DXCC 分区数）

        /// <summary>库中通联记录总数。</summary>
        [ObservableProperty]
        private int _qsoTotalCount;

        /// <summary>出现过的 CQ 分区数（去重）。</summary>
        [ObservableProperty]
        private int _cqZoneCount;

        /// <summary>出现过的 ITU 分区数（去重）。</summary>
        [ObservableProperty]
        private int _ituZoneCount;

        /// <summary>出现过的 DXCC 实体数（去重）。</summary>
        [ObservableProperty]
        private int _dxccCount;

        /// <summary>
        /// 刷新右下角统计区。
        ///
        /// ⚠ CQZ / ITUZ / DXCC 三个字段目前【全是 null】（呼号解析尚未实现），
        ///   所以后三个数字现在会显示 0 —— COUNT(DISTINCT 列) 会忽略 NULL。
        ///   等将来把解析接上、字段有值了，这里不用改代码就会显示正确结果。
        /// </summary>
        private void RefreshStatistics()
        {
            var (success, table, _, _) = SqlQueryTools.Execute(
                "SELECT COUNT(*) AS Total, "
                + "COUNT(DISTINCT CQZ) AS CqCount, "
                + "COUNT(DISTINCT ITUZ) AS ItuCount, "
                + "COUNT(DISTINCT DXCC) AS DxccCount "
                + "FROM LogTable");

            if (!success || table == null || table.Rows.Count == 0)
            {
                return;
            }

            DataRow row = table.Rows[0];
            QsoTotalCount = ToInt(row["Total"]);
            CqZoneCount = ToInt(row["CqCount"]);
            ItuZoneCount = ToInt(row["ItuCount"]);
            DxccCount = ToInt(row["DxccCount"]);
        }

        /// <summary>把 SQL 返回的单元格安全地转成 int（NULL 与 DBNull 都算 0）。</summary>
        private static int ToInt(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
