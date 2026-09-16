using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using SharpLog.Model;
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


namespace SharpLog.ViewModel
{
    /// <summary>
    /// 主窗口（日志编辑页）的 ViewModel。
    ///
    /// 职责：
    /// 1. 为 MainLogWindow.xaml 提供全部绑定属性（输入框、下拉列表、时间显示、消息提示等）；
    /// 2. 校验用户输入（QsoLogValidator + 必填/时间顺序检查）；
    /// 3. 调用 DB 层（SharpLogDbTools）完成日志的增删查，并刷新右侧日志卡片列表；
    /// 4. 每秒刷新 UTC / 本地时间显示，维护 LogUseTime（日志采用的时间基准）。
    ///
    /// 约束（按项目规范）：
    /// - 本类不操作任何 UI 控件实例，只通过属性绑定与命令交互；
    /// - 下拉框使用 SelectedItem 绑定（SelectPROPItem / SelectCommMode）。
    ///
    /// 命名规范：绑定属性名已统一为 ADIF 3.1.7 字段名
    /// （CALL、FREQ、RST_SENT、RST_RCVD、GRIDSQUARE、QTH、RIG 等）。
    /// 时间输入使用复合 DateTime（StartTimeDT/EndTimeDT），
    /// 由 ModeConvertTools 拆分为 QSO_DATE + TIME_ON 存储。
    /// </summary>
    [ObservableObject]
    public partial class MainLogWindow_VM
    {
        #region 日志信息相关绑定属性（表单输入字段）

        /// <summary>
        /// 通联开始时间（复合 DateTime，绑定 hc:DateTimePicker）。
        /// 后台转换时拆分为 ADIF: QSO_DATE（日期）+ TIME_ON（时间）分别存储。
        /// 默认值为当前日志时间 LogUseTime。
        /// </summary>
        [ObservableProperty]
        private DateTime _StartTimeDT = LogUseTime.DateTime;

        /// <summary>
        /// 通联结束时间（复合 DateTime，绑定 hc:DateTimePicker）。
        /// 后台转换时拆分为 ADIF: QSO_DATE_OFF + TIME_OFF。
        /// </summary>
        [ObservableProperty]
        private DateTime _EndTimeDT = LogUseTime.DateTime;

        /// <summary>
        /// 对方呼号（ADIF: CALL），必填。
        /// 输入时自动转大写（见 OnCALLChanged）。
        /// </summary>
        [ObservableProperty]
        private string _CALL = string.Empty;

        /// <summary>
        /// 呼号变更回调：自动将用户输入转为大写（业余无线电惯例）。
        /// </summary>
        partial void OnCALLChanged(string value)
        {
            var upper = value.ToUpperInvariant();
            if (value != upper)
            {
                CALL = upper;
            }
        }

        /// <summary>
        /// 备注（ADIF: COMMENT），绑定右上角多行备注输入框。
        /// </summary>
        [ObservableProperty]
        private string _COMMENT = string.Empty;

        /// <summary>
        /// 通联频率（ADIF: FREQ），输入为 kHz 数值文本（如 "7050"），必填。
        /// 存储进实体时为 double（见 ModeConvertTools）。
        /// </summary>
        [ObservableProperty]
        private string _FREQ = string.Empty;

        /// <summary>
        /// 收到的信号报告（ADIF: RST_RCVD），对应 UI「信号报告_收」。
        /// </summary>
        [ObservableProperty]
        private string _RST_RCVD = string.Empty;

        /// <summary>
        /// 发出的信号报告（ADIF: RST_SENT），对应 UI「信号报告_发」。
        /// </summary>
        [ObservableProperty]
        private string _RST_SENT = string.Empty;

        /// <summary>
        /// 网格定位（ADIF: GRIDSQUARE，Maidenhead 网格，如 FN31pr）。
        /// 输入时自动转大写（见 OnGRIDSQUAREChanged）。
        /// </summary>
        [ObservableProperty]
        private string _GRIDSQUARE = string.Empty;

        /// <summary>
        /// 网格变更回调：自动转大写（Maidenhead 网格规范不区分大小写，统一大写存储）。
        /// </summary>
        partial void OnGRIDSQUAREChanged(string value)
        {
            var upper = value.ToUpperInvariant();
            if (value != upper)
            {
                GRIDSQUARE = upper;
            }
        }

        /// <summary>
        /// 对方发射功率（ADIF: RX_PWR），对应 UI「发射功率_收」，单位 W。
        /// </summary>
        [ObservableProperty]
        private string _RX_PWR = string.Empty;

        /// <summary>
        /// 我方发射功率（ADIF: TX_PWR），对应 UI「发射功率_发」，单位 W。
        /// </summary>
        [ObservableProperty]
        private string _TX_PWR = string.Empty;

        /// <summary>
        /// 对方城市（ADIF: QTH），对应 UI「地理位置」。
        /// </summary>
        [ObservableProperty]
        private string _QTH = string.Empty;

        /// <summary>
        /// 对方电台型号（ADIF: RIG），对应 UI「设备」。
        /// </summary>
        [ObservableProperty]
        private string _RIG = string.Empty;

        /// <summary>
        /// 对方天线类型（项目自定义字段，ADIF 无对应，保留原名）。
        /// </summary>
        [ObservableProperty]
        private string _ant = string.Empty;

        /// <summary>
        /// 发射高度（项目自定义字段，ADIF 无对应，保留原名），单位 M。
        /// </summary>
        [ObservableProperty]
        private string _height = string.Empty;

        /// <summary>
        /// 操作员（项目自定义字段，ADIF 无对应，保留原名）。
        /// </summary>
        [ObservableProperty]
        private string _operator = string.Empty;

        /// <summary>
        /// DMR 数字 ID（项目自定义字段，ADIF 无对应，保留原名）。
        /// </summary>
        [ObservableProperty]
        private string _DigID = string.Empty;

        /// <summary>
        /// 分组 ID（项目自定义字段），绑定 hc:NumericUpDown（0-5）。
        /// </summary>
        [ObservableProperty]
        private int _GroupID = 0;

        /// <summary>
        /// 星级 ID（项目自定义字段），绑定 hc:NumericUpDown（0-5）。
        /// </summary>
        [ObservableProperty]
        private int _StarID = 0;

        /// <summary>
        /// 是否收到 QSL 卡片（ADIF 命名: QSL_RCVD）。
        /// 绑定 hc:ComboBox 的"是/否"字符串，转换时转为 bool。
        /// </summary>
        [ObservableProperty]
        private string _QSL_RCVD = string.Empty;

        /// <summary>
        /// QSL 卡片接收日期（ADIF: QSLRDATE），绑定 hc:DatePicker。
        /// </summary>
        [ObservableProperty]
        private DateTime? _QSLRDATE;

        /// <summary>
        /// QSL 卡片发送日期（ADIF: QSLSDATE），绑定 hc:DatePicker。
        /// </summary>
        [ObservableProperty]
        private DateTime? _QSLSDATE;

        /// <summary>
        /// QSL 卡片邮寄地址（项目自定义字段，保留原名）。
        /// </summary>
        [ObservableProperty]
        private string _QslAddress = string.Empty;

        /// <summary>
        /// QSL 邮政编码（项目自定义字段，保留原名）。
        /// </summary>
        [ObservableProperty]
        private string _PostalCode = string.Empty;

        /// <summary>
        /// QSL 接收人姓名（项目自定义字段，保留原名）。
        /// </summary>
        [ObservableProperty]
        private string _ReceiverName = string.Empty;

        /// <summary>
        /// QSL 接收人联系方式（项目自定义字段，保留原名）。
        /// </summary>
        [ObservableProperty]
        private string _ReceiverContact = string.Empty;

        /// <summary>
        /// 天气（项目自定义字段，保留原名）。
        /// </summary>
        [ObservableProperty]
        private string _Weather = string.Empty;

        /// <summary>
        /// 温度（项目自定义字段，保留原名）。
        /// </summary>
        [ObservableProperty]
        private string _Temperature = string.Empty;

        #endregion

        #region 被动属性（只读展示 / 下拉 / 消息提示）

        /// <summary>UTC 日期显示（yyyy/MM/dd），每秒刷新。</summary>
        [ObservableProperty]
        private string _UtcDate = "0000/00/00";

        /// <summary>本地日期显示（yyyy/MM/dd），每秒刷新。</summary>
        [ObservableProperty]
        private string _LocalDate = "0000/00/00";

        /// <summary>UTC 时区标识（固定 "UTC"）。</summary>
        [ObservableProperty]
        private string _UtcTimeZone = "UTC";

        /// <summary>本地时区偏移显示（如 UTC+08:00）。</summary>
        [ObservableProperty]
        private string _LocalTimeZone = "UTC+NaN";

        /// <summary>UTC 时间显示（HH:mm:ss），每秒刷新。</summary>
        [ObservableProperty]
        private string _UtcTime = "00:00:00";

        /// <summary>本地时间显示（HH:mm:ss），每秒刷新。</summary>
        [ObservableProperty]
        private string _LocalTime = "00:00:00";

        /// <summary>
        /// 当前选中的传播模式下拉项（绑定 ComboBox SelectedItem）。
        /// 通过 SelectedPropCode 获取 ADIF 代码（存库值）。
        /// </summary>
        [ObservableProperty]
        private PropagationItem? _SelectPROPItem;

        /// <summary>
        /// 当前选中的通联模式下拉项（绑定 ComboBox SelectedItem）。
        /// 通过 SelectedCommCode 获取 ADIF 代码（存库值）。
        /// </summary>
        [ObservableProperty]
        private CommMode? _SelectCommMode;

        /// <summary>消息提示框显示内容。</summary>
        [ObservableProperty]
        private string _MessageShowStr = string.Empty;

        /// <summary>消息提示框可见性（显示 2 秒后自动隐藏）。</summary>
        [ObservableProperty]
        private Visibility _MessShowISVisibility = Visibility.Hidden;

        /// <summary>当前日志使用的时区名称（"UTC" 或 "UTC+08:00"），显示在底部状态栏。</summary>
        [ObservableProperty]
        private string _ZoneName = string.Empty;

        /// <summary>传播模式下拉列表数据源（静态，见 StaticValues）。</summary>
        public ObservableCollection<PropagationItem> PropagationModeList { get; }

        /// <summary>通联模式下拉列表数据源（静态，见 StaticValues）。</summary>
        public ObservableCollection<CommMode> CommModeList { get; }

        /// <summary>右侧日志卡片集合（OneLog 显示模型），新日志插入到顶部。</summary>
        public ObservableCollection<OneLog> QsoLogCollection { get; }

        /// <summary>内存中的全部日志记录（当前为全表加载，后续建议改为分页查询）。</summary>
        public List<QsoLog> AllQsoLogList { get; set; } = new List<QsoLog>();

        /// <summary>选中传播模式的 ADIF 代码（只读，自动从 SelectedItem 获取）。</summary>
        public string? SelectedPropCode => _SelectPROPItem?.Code;

        /// <summary>选中通联模式的 ADIF 代码（只读，自动从 SelectedItem 获取）。</summary>
        public string? SelectedCommCode => _SelectCommMode?.Code;

        /// <summary>选中传播模式的显示名称（只读）。</summary>
        public string? SelectedPropName => _SelectPROPItem?.Name;

        /// <summary>选中通联模式的显示名称（只读）。</summary>
        public string? SelectedCommName => _SelectCommMode?.Name;

        /// <summary>每秒刷新时间的定时器（窗口关闭时由 View 停止）。</summary>
        public readonly DispatcherTimer _timer;

        #endregion

        /// <summary>
        /// 构造函数：
        /// 1. 初始化下拉列表数据源与默认选中项；
        /// 2. 从数据库加载全部日志并刷新右侧卡片；
        /// 3. 启动每秒时间刷新定时器。
        /// ⚠️ 已知问题：GetAllDBDataAsync().Result 同步等待会阻塞 UI 线程，后续建议改异步。
        /// </summary>
        public MainLogWindow_VM()
        {
            PropagationModeList = StaticValues.Set_PropagationModeList;
            CommModeList = StaticValues.Set_CommModeList;
            QsoLogCollection = new ObservableCollection<OneLog>();

            _SelectPROPItem = PropagationModeList[0]; // 默认选择第一个选项
            _SelectCommMode = CommModeList[0];        // 默认选择第一个选项

            // 从数据库加载全部日志并显示到右侧卡片列表
            var a = GetAllDBDataAsync<QsoLog>();
            AllQsoLogList = a.Result.Data.ToList();
            ShowLogInView();

            // 呼号与网格统一转为大写（初始值安全处理）
            CALL = CALL.ToUpper();
            GRIDSQUARE = GRIDSQUARE.ToUpper();

            // 打开计时器刷新，每秒刷新一次时间显示
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // 立即刷新一次，不用等 1 秒
            RefreshTime();
        }

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
        }

        #endregion

        #region 绑定触发事件（命令）

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
                    StartTimeDT, EndTimeDT, ZoneName, CALL, FREQ, SelectedCommCode,
                    RST_RCVD, RST_SENT, GRIDSQUARE, RX_PWR, TX_PWR, QTH, RIG, Ant, COMMENT,
                    Height, Operator, SelectedPropCode, DigID,
                    GroupID, StarID, QSL_RCVD, QSLRDATE, QSLSDATE, QslAddress, ReceiverName, PostalCode, ReceiverContact));

                if (!Result.Result.Success)
                {
                    ShowErrorMess_OnMainView(Result.Result.WarningMessage ?? "添加日志记录失败！");
                }
                else
                {
                    ShowSuccessMess_OnMainView("日志记录添加成功！");
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

        #region 辅助方法

        /// <summary>
        /// 将数据库中的全部日志刷新到右侧卡片列表（QsoLogCollection）。
        /// 新记录插入到列表顶部（Insert(0, ...)）。
        /// ⚠️ 已知问题：全表加载到内存 + 同步 .Result，后续建议改分页/异步。
        /// </summary>
        private void ShowLogInView()
        {
            QsoLogCollection.Clear();
            AllQsoLogList = GetAllDBDataAsync<QsoLog>().Result.Data;
            AllQsoLogList.ForEach(l =>
            {
                QsoLogCollection.Insert(0, ConvertQsoLogToOneLog(l));
            });
        }



        /// <summary>
        /// 添加日志前的输入校验，全部通过才允许写入数据库：
        /// 1. 必填项：呼号、频率、模式（时间由 DateTimePicker 保证有值）；
        /// 2. 频率必须为有效数字，且在合法业余波段内（QsoLogValidator）；
        /// 3. 开始时间必须早于结束时间。
        /// </summary>
        /// <returns>true = 校验通过，可添加</returns>
        private bool CanAddLog()
        {
            // 呼号格式与频率波段校验（静态工具类，见 QsoLogValidator）
            (bool isValid, string errorMessage) = QsoLogValidator.ValidateLogInput(CALL, FREQ);

            // 检查必填字段是否为空（CALL / FREQ / MODE）
            if (string.IsNullOrWhiteSpace(CALL) || string.IsNullOrWhiteSpace(FREQ)
                || string.IsNullOrWhiteSpace(SelectedCommCode))
            {
                ShowErrorMess_OnMainView("请填写所有必填字段！");
                return false;
            }
            // 检查频率是否为有效的数字
            if (!double.TryParse(FREQ, out double frequency))
            {
                ShowErrorMess_OnMainView("频率必须是有效的数字！");
                return false;
            }
            // 检查开始时间是否早于结束时间（复合 DateTime 直接比较）
            if (StartTimeDT >= EndTimeDT)
            {
                ShowErrorMess_OnMainView("开始时间必须早于结束时间！");
                return false;
            }
            // 波段合法性校验结果
            if (!isValid)
            {
                ShowErrorMess_OnMainView(errorMessage);
                return false;
            }
            return true;
        }

        #endregion
    }
}
