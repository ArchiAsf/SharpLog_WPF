using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using SharpLog.DB;
using SharpLog.Model;
using SharpLog.ToolsBox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static SharpLog.Model.StaticValues;
using static SharpLog.DB.SharpLogDbTools;
using static SharpLog.ViewModel.ModeConvertTools;
using static SharpLog.ToolsBox.MessageTools;

// ══════════════════════════════════════════════════════════════════════════
//  MainLogWindow_VM 的「普通方法」部分（partial）
//  ──────────────────────────────────────────────────────────────────────────
//  本文件装的是会【改变状态或访问数据库】的方法：
//    · 定时触发（每秒刷新时间显示）
//    · 绑定触发事件（各命令：设置页、日志编辑、日志查看）
//    · 日志查看页（表格分页加载、加载更多、行操作）
//
//  与之相对：
//    · 绑定属性、字段、构造函数 → MainLogWindow_VM.cs
//    · 纯计算的辅助方法          → MainLogWindow_VM.Helpers.cs
// ══════════════════════════════════════════════════════════════════════════

namespace SharpLog.ViewModel
{
    public partial class MainLogWindow_VM
    {
        #region 定时触发相关方法

        /// <summary>
        /// 定时器 Tick 事件：每秒刷新时间显示。
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e) => RefreshTime();

        /// <summary>
        /// 每秒刷新所执行的逻辑：
        /// 1. 刷新 UTC / 本地日期时间显示；
        /// 2. 根据 IsUseUtcTime 更新 LogUseTime（日志记录采用的时间基准）与 ZoneName。
        /// </summary>
        private void RefreshTime()
        {
            // UTC 时间显示
            UtcDate = DateTime.UtcNow.ToString("yyyy/MM/dd");
            UtcTimeZone = "UTC";
            UtcTime = DateTime.UtcNow.ToString("HH:mm:ss");

            // 本地时间显示（含时区偏移）
            LocalDate = DateTime.Now.ToString("yyyy/MM/dd");
            LocalTimeZone = "UTC" + DateTimeOffset.Now.ToString("zzz");
            LocalTime = DateTime.Now.ToString("HH:mm:ss");

            // 日志时间基准：使用 UTC 还是本地时间（StaticValues 全局开关）
            if (IsUseUtcTime == true)
            {
                LogUseTime = DateTime.UtcNow;
                ZoneName = "UTC";
            }
            else
            {
                LogUseTime = DateTime.Now;
                ZoneName = "UTC" + DateTimeOffset.Now.ToString("zzz");
            }

            // ── 晨昏区：每天零点（本地日期变化时）重算一次日出日落 ──
            // 直接复用这个每秒定时器即可，不必再开一个；
            // 判断依据是【本地日期有没有变】，所以程序跨零点持续运行时会自动更新。
            if (DateTime.Now.Date != _lastSunTimeCalcDate)
            {
                _lastSunTimeCalcDate = DateTime.Now.Date;
                RefreshSunTimes();
            }

            // ── 晨昏区：每秒判定一次所属时段，跨过边界时自动切换图标与文字 ──
            // 必须持续判定而不是只在零点算一次：时段会随时间推移自然变化
            // （比如从「日间」进入「黄昏」），只在零点判断的话整天都不会更新。
            RefreshDayPeriod();
        }

        /// <summary>
        /// 根据设置中保存的经纬度，计算当天的【民用日出日落】时刻，
        /// 把结果写入界面绑定属性，并同步缓存到 StaticValues。
        ///
        /// 【触发时机】程序启动时一次，之后每天本地零点各一次（见 RefreshTime）。
        /// 【经纬度来源】读取【已保存】的设置（AppSettingsCache）——
        ///   设置页上正在编辑、但还没点保存的值不会影响这里。
        /// </summary>
        private void RefreshSunTimes()
        {
            // ---------- 1. 解析经纬度 ----------
            // 用户在设置页填的是文本（如 "39.90"），这里解析成十进制度；
            // 任意一个缺失或格式不对，都视为"无法计算"
            bool latitudeOk = SunTimeCalculator.TryParseDegrees(
                AppSettingsCache.Current?.MY_LAT, out double latitude);
            bool longitudeOk = SunTimeCalculator.TryParseDegrees(
                AppSettingsCache.Current?.MY_LON, out double longitude);

            if (!latitudeOk || !longitudeOk)
            {
                ApplySunTimeResult(new SunTimeCalculator.SunTimeResult(
                    SunTimeStatus.Invalid, null, null));
                return;
            }

            // ---------- 2. 计算 ----------
            // 结果显示采用本机所在时区。
            // 多数情况下这与台站所在地一致；将来若要单独设置台站时区，改这一行即可。
            TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

            var result = SunTimeCalculator.CalculateCivilTwilight(
                DateTime.Now, latitude, longitude, utcOffset);

            // ---------- 3. 写到界面与 StaticValues ----------
            ApplySunTimeResult(result);
        }

        /// <summary>
        /// 把计算结果分别写进界面绑定属性与 StaticValues。
        ///
        /// 显示规则：
        ///   · 正常   → 显示 "HH:mm" 时刻
        ///   · 极昼   → 两个位置都显示「极昼」
        ///   · 极夜   → 两个位置都显示「极夜」
        ///   · 无法算 → 两个位置都显示 "--:--"（通常是经纬度还没填）
        /// </summary>
        private void ApplySunTimeResult(SunTimeCalculator.SunTimeResult result)
        {
            switch (result.Status)
            {
                case SunTimeStatus.PolarDay:
                    SunriseText = "极昼";
                    SunsetText = "极昼";
                    break;

                case SunTimeStatus.PolarNight:
                    SunriseText = "极夜";
                    SunsetText = "极夜";
                    break;

                case SunTimeStatus.Normal:
                    SunriseText = result.Sunrise?.ToString("HH:mm") ?? "--:--";
                    SunsetText = result.Sunset?.ToString("HH:mm") ?? "--:--";
                    break;

                default:   // SunTimeStatus.Invalid —— 经纬度缺失或非法
                    SunriseText = "--:--";
                    SunsetText = "--:--";
                    break;
            }

            // 存入 StaticValues，供后续功能（传播判断、波段窗口提示等）取用
            StaticValues.SunriseTime = result.Sunrise;
            StaticValues.SunsetTime = result.Sunset;
            StaticValues.SunTimeState = result.Status;

            // 缓存下来供每秒的时段判断复用
            _currentSunTime = result;
        }

        /// <summary>
        /// 根据当前时刻与当天的日出日落，判断所处时段并更新晨昏区的图标与文字。
        ///
        /// 【调用时机】每秒一次（随 RefreshTime）——
        ///   时段会随时间推移自然切换，必须持续判定。
        ///   由于日出日落结果已缓存在 _currentSunTime，这里只做几次时间比较，
        ///   每秒执行也不会有性能负担。
        ///
        /// 【划分规则】见 Model\DayPeriod.cs 的说明：
        ///   黎明 = 日出前后各 30 分钟、黄昏 = 日落前后各 30 分钟，其余为日间/夜间。
        /// </summary>
        private void RefreshDayPeriod()
        {
            // 黎明与黄昏各自向两侧延伸的过渡时长
            TimeSpan transition = TimeSpan.FromMinutes(30);

            DayPeriod period = SunTimeCalculator.ResolveDayPeriod(
                DateTime.Now, _currentSunTime, transition);

            // 时段 → 文字 + 图标
            // 图标路径写成 "/logo/xxx.png" 这种形式，WPF 会自动当作资源路径解析
            switch (period)
            {
                case DayPeriod.Dawn:
                    DayPeriodText = "黎明";
                    DayPeriodIconPath = "/logo/Dawn.png";
                    break;

                case DayPeriod.Day:
                    DayPeriodText = "日间";
                    DayPeriodIconPath = "/logo/Day.png";
                    break;

                case DayPeriod.Dusk:
                    DayPeriodText = "黄昏";
                    DayPeriodIconPath = "/logo/Dusk.png";
                    break;

                case DayPeriod.Night:
                    DayPeriodText = "夜间";
                    DayPeriodIconPath = "/logo/Night.png";
                    break;

                case DayPeriod.PolarDay:
                    // 太阳整日不落，配日间的图最贴切
                    DayPeriodText = "极昼";
                    DayPeriodIconPath = "/logo/Day.png";
                    break;

                case DayPeriod.PolarNight:
                    DayPeriodText = "极夜";
                    DayPeriodIconPath = "/logo/Night.png";
                    break;

                default:
                    // DayPeriod.Unknown —— 通常是经纬度还没填写
                    DayPeriodText = "未设置";
                    DayPeriodIconPath = "/logo/Day.png";
                    break;
            }
        }

        #endregion

        #region 绑定触发事件（命令）

        #region 设置页命令（保存设置 / 恢复到默认设置 / 取消更改）

        /// <summary>
        /// 「保存设置」命令：把设置页上的 16 项内容写入数据库。
        ///
        /// 首次保存时插入新行，之后每次都是覆盖更新
        /// （"到底是插入还是更新"的规则封装在 AppSettingsDbTools 里，
        ///   这里不需要判断）。
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            // 把界面上的值组装成实体：
            // 用反射按属性名批量拷贝，省去手写 16 行赋值，将来增删字段也不必改这里
            var settings = new AppSettings();
            PropertyCopyTools.CopySameNameProperties(
                this, settings,
                nameof(AppSettings.Id), nameof(AppSettings.UpdatedAt));

            var (success, error) = await AppSettingsDbTools.SaveAsync(settings);

            if (!success)
            {
                await ShowErrorMess(MessagePage.Settings, error ?? "保存设置失败！");
                return;
            }

            await ShowSuccessMess(MessagePage.Settings, "设置已保存！");
        }

        /// <summary>
        /// 「恢复到默认设置」命令：把设置页上的内容重置为默认值。
        ///
        /// 注意：本命令【只修改界面、不写数据库】——
        /// 真正生效需要用户再点一次「保存设置」；
        /// 如果反悔，点「取消更改」即可回到数据库中的已保存值。
        ///
        /// 默认值由 AppSettings.CreateDefault() 统一提供，目前各项均为空；
        /// 将来要给某个字段设初始值，只改那个方法即可，这里无需改动。
        /// </summary>
        [RelayCommand]
        private void ResetSettings()
        {
            // 二次确认：避免误点后界面上的内容被一扫而空。
            // 用 HandyControl 的 MessageBox（项目规范禁止使用原生 MessageBox.Show）。
            // 默认按钮设为「取消」，这样即使误按回车也不会清空界面。
            var choice = HandyControl.Controls.MessageBox.Show(
                "确定要把所有设置项恢复为默认值吗？\n\n此操作只影响界面，点「保存设置」后才会写入数据库。",
                "恢复默认设置",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Cancel);

            if (choice != MessageBoxResult.OK) return;

            var defaultSettings = AppSettings.CreateDefault();

            PropertyCopyTools.CopySameNameProperties(
                defaultSettings, this,
                nameof(AppSettings.Id), nameof(AppSettings.UpdatedAt));

            ShowSuccessMess(MessagePage.Settings, "已恢复默认设置（尚未保存）");
        }

        /// <summary>
        /// 「取消更改」命令：放弃界面上尚未保存的修改，
        /// 重新从数据库读取设置并填回界面。
        /// </summary>
        [RelayCommand]
        private async Task CancelSettingsChangeAsync()
        {
            await ReloadSettingsAsync();
            await ShowSuccessMess(MessagePage.Settings, "已放弃更改，界面已恢复为已保存的设置");
        }

        /// <summary>
        /// 从数据库重新读取设置并填入界面。
        ///
        /// 三个场景共用此方法：
        ///   · 程序启动时加载；
        ///   · 点击「取消更改」时回滚；
        ///   · 将来需要"重新载入"时也可直接调用。
        /// </summary>
        private async Task ReloadSettingsAsync()
        {
            var (settings, error) = await AppSettingsDbTools.LoadAsync();

            if (error != null)
            {
                await ShowErrorMess(MessagePage.Settings, error);
                return;
            }

            // 数据库中还没有设置（从未保存过）→ 用默认值填充界面，目前即全部为空
            var source = settings ?? AppSettings.CreateDefault();

            PropertyCopyTools.CopySameNameProperties(
                source, this,
                nameof(AppSettings.Id), nameof(AppSettings.UpdatedAt));
        }

        #endregion

        /// <summary>
        /// 「添加日志」按钮点击命令：
        /// 1. CanAddLog() 校验输入（必填、频率合法性、时间顺序）；
        /// 2. 通过 ModeConvertTools 组装 QsoLog 实体（含时间拆分）；
        /// 3. 写入数据库（SharpLogDbTools.AddDBDataAsync）；
        /// 4. 成功则刷新右侧卡片列表并提示，失败则显示错误信息。
        /// </summary>
        [RelayCommand()]
        private void AddLogBtnClick()
        {
            if (CanAddLog())
            {
                Task<(bool Success, string? WarningMessage)> Result = AddDBDataAsync<QsoLog>(ConvertMainViewToQsoLog(
                    // SelectedCommCode = ADIF 主模式（MODE），SelectedSubmodeCode = ADIF 子模式（SUBMODE）
                    StartTimeDT, EndTimeDT, ZoneName, CALL, FREQ, SelectedCommCode, SelectedSubmodeCode,
                    RST_RCVD, RST_SENT, GRIDSQUARE, RX_PWR, TX_PWR, QTH, RIG, Ant, COMMENT,
                    Height, CONTACTED_OP, SelectedPropCode, DigID,
                    GroupID, StarID, QSL_RCVD, QSLRDATE, QSLSDATE, QslAddress, ReceiverName, PostalCode, ReceiverContact));

                if (!Result.Result.Success)
                {
                    ShowErrorMess(MessagePage.LogEdit, Result.Result.WarningMessage ?? "添加日志记录失败！");
                }
                else
                {
                    ShowSuccessMess(MessagePage.LogEdit, "日志记录添加成功！");
                    ShowLogInView();
                }
            }
        }

        /// <summary>
        /// 「时区转换」命令：切换日志时间基准（UTC ↔ 本地），
        /// 并将开始/结束时间输入框同步为切换后的当前时间。
        /// </summary>
        [RelayCommand]
        private void ConvertTimeZone()
        {
            if (IsUseUtcTime == false)
            {
                // 当前是本地时间 → 切到 UTC
                LogUseTime = UtcNow;
                IsUseUtcTime = !IsUseUtcTime;
                StartTimeDT = LogUseTime.DateTime;
                EndTimeDT = LogUseTime.DateTime;
            }
            else
            {
                // 当前是 UTC → 切到本地时间
                LogUseTime = LocalNow;
                IsUseUtcTime = !IsUseUtcTime;
                StartTimeDT = LogUseTime.DateTime;
                EndTimeDT = LogUseTime.DateTime;
            }

            RefreshTime();
        }

        /// <summary>
        /// 点击「开始时间」标签命令：把开始时间输入框设为当前日志时间。
        /// （后台存储时拆分为 QSO_DATE + TIME_ON）
        /// </summary>
        [RelayCommand]
        private void StartTimeTextClick_TimeInPutStartTime() => StartTimeDT = LogUseTime.DateTime;

        /// <summary>
        /// 点击「结束时间」标签命令：把结束时间输入框设为当前日志时间。
        /// （后台存储时拆分为 QSO_DATE_OFF + TIME_OFF）
        /// </summary>
        [RelayCommand]
        private void EndTimeTextClick_TimeInPutEndTime() => EndTimeDT = LogUseTime.DateTime;

        #endregion
        #region 日志查看页（表格 + 加载更多 + 行操作）

        /// <summary>日志查看页的表格一次加载的条数。</summary>
        private const int LogViewPageSize = 2000;

        /// <summary>
        /// 日志查看页的表格数据源。
        /// 直接装 QsoLog 实体（不另做显示模型），需要变形的少数几列由
        /// View\Converters\LogViewConverters.cs 里的转换器处理。
        /// </summary>
        public RangeObservableCollection<QsoLog> LogViewRows { get; } = new RangeObservableCollection<QsoLog>();

        /// <summary>
        /// 表格当前选中的那一行。
        /// 右键菜单与双击都以它为操作对象 —— 把选中项放在 VM 里，
        /// 是为了避免 XAML 里在 ContextMenu 内部去做跨视觉树的绑定。
        /// </summary>
        [ObservableProperty]
        private QsoLog? _selectedLogViewRow;

        /// <summary>底部「加载更多」的提示文字。</summary>
        [ObservableProperty]
        private string _loadMoreText = "正在加载…";

        /// <summary>是否正在加载。用于防止用户连点导致重复请求。</summary>
        [ObservableProperty]
        private bool _isLoadingMore;

        /// <summary>是否还有更旧的数据可加载（取到 0 条即认为已到底）。</summary>
        private bool _hasMoreLogViewData = true;

        /// <summary>
        /// 下一页的游标：上一批最后一条（本批里 Id 最小那条）的主键。
        /// 初始为 int.MaxValue，表示「从最新一条开始往前取」。
        /// </summary>
        private int _logViewCursorId = int.MaxValue;

        /// <summary>首屏是否已加载过（避免每次切回这个标签页都重新拉数据）。</summary>
        private bool _logViewFirstPageLoaded;

        /// <summary>
        /// 首屏加载入口，由 View 在表格第一次显示时调用。
        ///
        /// 之所以不在构造函数里加载：日志查看页是用户可能根本不点的标签页，
        /// 启动时就白查 2000 条不划算。（TabItem 的内容是首次选中才实例化的，
        /// 所以表格的 Loaded 事件天然就是「用户第一次切到这个页」的时机。）
        /// </summary>
        public void LoadLogViewFirstPage()
        {
            if (_logViewFirstPageLoaded) return;
            _logViewFirstPageLoaded = true;

            // 走命令而不是直接 await：AsyncRelayCommand 会自己处理重入与异常
            LoadMoreLogViewCommand.Execute(null);
        }

        /// <summary>
        /// 强制重新加载【两个日志列表】：左侧的日志查看表格 + 右侧的日志卡片。
        ///
        /// 使用场景：编辑窗口关闭后返回，两处都需要反映最新数据。
        ///
        /// ════════════════════════════════════════════════════════════════════════
        /// 【为什么不复用 LoadLogViewFirstPage】
        ///   那个方法带着 _logViewFirstPageLoaded 哨兵 —— 它只在
        ///   「用户第一次切到日志查看页」时加载一次，之后再调用会【直接 return】。
        ///   拿它当刷新用是刷不动的（本次就踩了这个坑：
        ///   编辑窗口保存后调它，列表纹丝不动，用户以为功能没做）。
        /// ════════════════════════════════════════════════════════════════════════
        ///
        /// 使用场景：编辑窗口里删除 / 保存后返回，列表需要反映最新数据。
        ///
        /// ⚠ 副作用：会丢掉用户已加载的后续批次与滚动位置，回到第一页顶部。
        ///   编辑单条记录的场景下可接受；若日后觉得不便，
        ///   可改成「只把被改的那一条替换掉」而不是整表重载。
        /// </summary>
        public void ReloadAfterEdit()
        {
            // ① 清空列表（发 Reset 通知，DataGrid 随之重建行）
            LogViewRows.Clear();

            // ② 复位分页状态。
            //    ⚠ 这一步不能省：游标仍停在上一批的位置时，
            //    重新加载会从列表中间接着取，第一页的数据就永远回不来了
            _logViewCursorId = int.MaxValue;
            _hasMoreLogViewData = true;

            // ③ 解除「首屏只加载一次」的哨兵，否则第 ④ 步会被它挡掉
            _logViewFirstPageLoaded = false;

            // ④ 重新取第一页
            LoadLogViewFirstPage();

            // ⑤ 右侧的日志卡片列表也一起刷新。
            //    它只取最近 MaxDisplayLogCount 条，查询很轻，直接同步取即可。
            ShowLogInView();
        }

        /// <summary>
        /// 加载下一批日志。首屏与「加载更多」共用这一个方法。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanLoadMoreLogView))]
        private async Task LoadMoreLogViewAsync()
        {
            // 防连点：上一次还没加载完，这次点击直接忽略。
            // （按钮本身也会因 CanExecute 返回 false 而变灰点不动，这里是双保险）
            if (IsLoadingMore) return;

            // 已经取到过 0 条，说明到底了，不必再查库
            if (!_hasMoreLogViewData)
            {
                UpdateLoadMoreText();
                return;
            }

            IsLoadingMore = true;
            LoadMoreText = "正在加载…";

            try
            {
                // 键集分页：取「主键小于游标」的前 N 条，也就是比上一批更旧的那些
                var (data, error) = await GetLatestDBDataAsync<QsoLog>(LogViewPageSize, _logViewCursorId);

                if (error != null)
                {
                    ShowErrorMess(MessagePage.LogView, error);
                    LoadMoreText = "加载失败，点击重试";
                    return;
                }

                if (data.Count == 0)
                {
                    _hasMoreLogViewData = false;
                    UpdateLoadMoreText();
                    return;
                }

                // 记下本批最后一条的主键，作为下一批的游标。
                // 查询是 Id 降序，所以列表末尾那条就是本批里 Id 最小的。
                _logViewCursorId = data[data.Count - 1].Id;

                // 取满一整页说明后面多半还有；取不满说明这就是最后一批了
                _hasMoreLogViewData = data.Count >= LogViewPageSize;

                // 一次性追加，只触发一次界面刷新
                //（逐条 Add 会让 DataGrid 刷 2000 次，见 RangeObservableCollection 的说明）
                LogViewRows.AddRange(data);

                UpdateLoadMoreText();
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        /// <summary>
        /// 刷新底部「已加载 N 条…」的提示文字。
        /// </summary>
        private void UpdateLoadMoreText()
        {
            int loadedCount = LogViewRows.Count;

            LoadMoreText = _hasMoreLogViewData
                ? $"已加载 {loadedCount:N0} 条，点击继续加载"
                : $"已加载 {loadedCount:N0} 条，已全部加载完毕";
        }

        /// <summary>
        /// 「加载更多」按钮是否可点。
        /// 正在加载时返回 false，按钮自动变灰且点不动 ——
        /// 这样用户连点也不会重复发起请求、不会重复加载同一批数据。
        /// </summary>
        private bool CanLoadMoreLogView() => !IsLoadingMore;

        /// <summary>
        /// IsLoadingMore 每次变化时，通知命令重新求值 CanExecute，
        /// 按钮的可用状态才会跟着变。
        /// （partial 方法由 [ObservableProperty] 源生成器自动挂钩到属性 setter）
        /// </summary>
        partial void OnIsLoadingMoreChanged(bool value)
        {
            LoadMoreLogViewCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 「请求编辑某条日志」事件。
        ///
        /// View 层订阅它来弹出编辑窗口 —— 这样 ViewModel 不必认识任何 Window 类型。
        /// </summary>
        public event EventHandler<QsoLog>? EditRequested;

        /// <summary>
        /// 请求打开某条日志的编辑窗口。
        /// 双击整行、右键菜单「更改该记录」都会走到这里。
        ///
        /// 【为什么这里只抛事件、不直接 new 窗口】
        ///   项目规范禁止 ViewModel 持有并操作 UI 控件实例，而 Window 正是 UI 控件。
        ///   VM 只负责广播「用户想编辑这条记录」这件事；
        ///   至于用哪个窗口、模态还是非模态，全部交给 View 层决定
        ///   （订阅方见 MainLogWindow.xaml.cs 的 OnEditRequested）。
        /// </summary>
        /// <param name="row">要编辑的那条记录，由 XAML 绑定的 SelectedLogViewRow 传入。</param>
        [RelayCommand]
        private void OpenLogEdit(QsoLog? row)
        {
            if (row == null) return;

            EditRequested?.Invoke(this, row);
        }

        /// <summary>
        /// 删除某条日志（右键菜单「删除该记录」，含二次确认）。
        /// </summary>
        [RelayCommand]
        private async Task DeleteLogAsync(QsoLog? row)
        {
            if (row == null) return;

            // 二次确认。项目规范禁止使用原生 MessageBox.Show，
            // 这里用 HandyControl 的 MessageBox，与设置页「恢复默认」保持一致。
            // ⚠ 默认按钮设为 Cancel：用户直接按回车时不会误删。
            string dateText = row.QSO_DATE.HasValue
                ? row.QSO_DATE.Value.ToString("yyyy-MM-dd")
                : "（未填）";

            var choice = HandyControl.Controls.MessageBox.Show(
                $"确定要删除这条通联记录吗？\n\n" +
                $"呼号：{row.CALL}\n" +
                $"日期：{dateText}\n" +
                $"频率：{row.FREQ} kHz\n\n" +
                $"删除后无法恢复！",
                "删除确认",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Cancel);

            if (choice != MessageBoxResult.OK) return;

            var (success, error, isFullTableWarning) = await DeleteDBDataAsync<QsoLog>(x => x.Id == row.Id);

            if (!success)
            {
                // 边界情况：表里恰好只剩这一条时，会被 DB 层的「全表删除检测」拦下，
                // 但用户要删的本来也就是它，所以这里放行，走强制清空。
                if (isFullTableWarning)
                {
                    var (forceSuccess, forceError) = await ForceDeleteAllAsync<QsoLog>();
                    if (!forceSuccess)
                    {
                        ShowErrorMess(MessagePage.LogView, forceError ?? "删除失败");
                        return;
                    }
                }
                else
                {
                    ShowErrorMess(MessagePage.LogView, error ?? "删除失败");
                    return;
                }
            }

            // 从界面上移除该行并刷新底部计数
            LogViewRows.Remove(row);
            UpdateLoadMoreText();

            ShowSuccessMess(MessagePage.LogView, $"已删除 {row.CALL} 的记录");
        }

        #endregion
    }
}
