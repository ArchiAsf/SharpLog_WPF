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
        /// 对方电台值机员呼号（ADIF: CONTACTED_OP），绑定 TextBox。
        /// 指实际在【对方电台】操作设备的人——远征台、集体台换班时尤其有用。
        /// ⚠ 与 RadioOperator 区分：RadioOperator 是【本站】的操作员呼号（来自软件设置）。
        /// </summary>
        [ObservableProperty]
        private string _CONTACTED_OP = string.Empty;

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
        /// 晨昏区显示的日出时刻文本（如 "05:51"）。
        /// 经纬度未填写时显示 "--:--"；极昼/极夜时显示对应的状态文字。
        /// 由 RefreshSunTimes() 在程序启动时与每天零点更新。
        /// </summary>
        [ObservableProperty]
        private string _sunriseText = "--:--";

        /// <summary>
        /// 晨昏区显示的日落时刻文本（如 "18:52"），规则同 SunriseText。
        /// </summary>
        [ObservableProperty]
        private string _sunsetText = "--:--";

        /// <summary>
        /// 上次计算日出日落时所在的本地日期。
        /// 用来判断程序运行期间有没有跨天——日期一变就在零点重新计算，
        /// 这样程序连续开着好几天也能自动跟上。
        /// </summary>
        private DateTime _lastSunTimeCalcDate = DateTime.MinValue;

        /// <summary>
        /// 最近一次日出日落计算的结果，供"时段判断"复用。
        /// 有了它，每秒判定时段时只需做几次时间比较，不必重跑天文公式。
        /// </summary>
        private SunTimeCalculator.SunTimeResult _currentSunTime =
            new SunTimeCalculator.SunTimeResult(SunTimeStatus.Invalid, null, null);

        /// <summary>
        /// 晨昏区显示的时段文字：黎明 / 日间 / 黄昏 / 夜间 / 极昼 / 极夜 / 未设置。
        /// 每秒随 RefreshTime 重新判定。
        /// </summary>
        [ObservableProperty]
        private string _dayPeriodText = "未设置";

        /// <summary>
        /// 晨昏区显示的时段图标路径（/logo 目录下的 Dawn / Day / Dusk / Night 四张图之一），
        /// 与 DayPeriodText 同步更新。
        /// </summary>
        [ObservableProperty]
        private string _dayPeriodIconPath = "/logo/Day.png";

        /// <summary>
        /// 当前选中的传播模式下拉项（绑定 ComboBox SelectedItem）。
        /// 通过 SelectedPropCode 获取 ADIF 代码（存库值）；
        /// 选中首项「（不指定）」时 Code 为 null，存库 PROP_MODE 写 NULL。
        /// </summary>
        [ObservableProperty]
        private PropagationItem? _SelectPROPItem;

        /// <summary>
        /// 当前选中的通联模式下拉项（绑定 ComboBox SelectedItem）。
        /// 一个选项同时携带三个值：
        ///   DisplayName（界面显示名）、Mode（ADIF 主模式）、Submode（ADIF 子模式），
        /// 分别通过 SelectedCommName / SelectedCommCode / SelectedSubmodeCode 读取。
        /// </summary>
        [ObservableProperty]
        private ModeItem? _SelectCommMode;

        /// <summary>消息提示框显示内容。</summary>
        [ObservableProperty]
        private string _MessageShowStr = string.Empty;

        /// <summary>消息提示框可见性（显示 2 秒后自动隐藏）。</summary>
        [ObservableProperty]
        private Visibility _MessShowISVisibility = Visibility.Hidden;

        /// <summary>当前日志使用的时区名称（"UTC" 或 "UTC+08:00"），显示在底部状态栏。</summary>
        [ObservableProperty]
        private string _ZoneName = string.Empty;

        #endregion

        #region 本地台站设置（设置页专用，属性名尽可能采用 ADIF 标准字段）

        // 说明：本组属性描述【本站】的信息，与通联记录里的对方信息（CALL、QTH 等）无关。
        // ADIF 规范中，本站信息统一使用 MY_ 前缀（或 STATION_/OPERATOR 等专用字段），
        // 导出 ADIF 文件时写入文件头，供对方软件识别本站台站资料。
        // 其中“通讯地址 / QSL邮寄地址 / 电子邮箱”三项在 ADIF 中没有对应的本站字段
        // （ADIF 的 ADDRESS、EMAIL 均指对方电台），故采用项目自定义命名。

        /// <summary>
        /// 本站呼号（ADIF: STATION_CALLSIGN），通联时本站实际使用的呼号。
        /// ⚠ 注意与通联记录的 CALL（对方呼号）区分。
        /// </summary>
        [ObservableProperty]
        private string _STATION_CALLSIGN = string.Empty;

        /// <summary>
        /// 日志操作员呼号（ADIF: OPERATOR）。
        /// ADIF 规范：若 STATION_CALLSIGN 缺失，OPERATOR 同时视为本站呼号与操作员呼号。
        /// </summary>
        [ObservableProperty]
        private string _OPERATOR = string.Empty;

        /// <summary>本站 DXCC 实体代码（ADIF: MY_DXCC）。</summary>
        [ObservableProperty]
        private string _MY_DXCC = string.Empty;

        /// <summary>本站 CQ 分区，取值 1~40（ADIF: MY_CQ_ZONE）。</summary>
        [ObservableProperty]
        private string _MY_CQ_ZONE = string.Empty;

        /// <summary>本站 ITU 分区，取值 1~90（ADIF: MY_ITU_ZONE）。</summary>
        [ObservableProperty]
        private string _MY_ITU_ZONE = string.Empty;

        /// <summary>本站 Maidenhead 网格定位，2/4/6/8 字符（ADIF: MY_GRIDSQUARE）。</summary>
        [ObservableProperty]
        private string _MY_GRIDSQUARE = string.Empty;

        /// <summary>本站所属 DXCC 实体名称（ADIF: MY_COUNTRY）。</summary>
        [ObservableProperty]
        private string _MY_COUNTRY = string.Empty;

        /// <summary>本站经度（ADIF: MY_LON）。</summary>
        [ObservableProperty]
        private string _MY_LON = string.Empty;

        /// <summary>本站纬度（ADIF: MY_LAT）。</summary>
        [ObservableProperty]
        private string _MY_LAT = string.Empty;

        /// <summary>本站邮政编码（ADIF: MY_POSTAL_CODE）。</summary>
        [ObservableProperty]
        private string _MY_POSTAL_CODE = string.Empty;

        /// <summary>
        /// 台站地址（ADIF: MY_STREET，即本站街道地址，指电台架设所在地）。
        /// </summary>
        [ObservableProperty]
        private string _MY_STREET = string.Empty;

        /// <summary>
        /// 通讯地址（项目自定义，ADIF 中无对应的本站字段）。
        /// 与台站地址的区别：台站地址是电台架设地，通讯地址是信件收件地址。
        /// </summary>
        [ObservableProperty]
        private string _MY_MailAddress = string.Empty;

        /// <summary>QSL 卡片邮寄地址（项目自定义，ADIF 中无对应的本站字段）。</summary>
        [ObservableProperty]
        private string _MY_QslAddress = string.Empty;

        /// <summary>
        /// 本站电子邮箱（项目自定义）。
        /// ⚠ 不能复用 ADIF 的 EMAIL 字段——那个指的是对方电台的邮箱。
        /// </summary>
        [ObservableProperty]
        private string _MY_Email = string.Empty;

        /// <summary>本站天线描述（ADIF: MY_ANTENNA）。</summary>
        [ObservableProperty]
        private string _MY_ANTENNA = string.Empty;

        /// <summary>本站设备描述（ADIF: MY_RIG）。</summary>
        [ObservableProperty]
        private string _MY_RIG = string.Empty;

        #endregion

        /// <summary>
        /// 传播模式下拉列表数据源（静态，见 StaticValues.Set_PropagationModeList）。
        /// 首项为「（不指定）」（Code = null），其余为 ADIF 官方 20 条传播模式。
        /// </summary>
        public ObservableCollection<PropagationItem> PropagationModeList { get; }

        /// <summary>
        /// 通联模式下拉列表数据源（静态，见 StaticValues.Set_CommModeList）。
        /// 列表项为 ModeItem，同时携带显示名与 ADIF 的 MODE/SUBMODE 值。
        /// </summary>
        public ObservableCollection<ModeItem> CommModeList { get; }

        /// <summary>
        /// 右侧日志区【最多】显示的记录条数。
        ///
        /// 数据库里可能存有成千上万条日志，而右侧只是一个滚动浏览区：
        /// 全部读进内存既慢又费内存，界面也渲染不动，故只取最近这么多条。
        /// 需要查看更早的记录应走"日志查看"页（待实现）里的条件查询。
        /// </summary>
        private const int MaxDisplayLogCount = 200;

        /// <summary>右侧日志卡片集合（OneLog 显示模型），最新的记录在最上方。</summary>
        public ObservableCollection<OneLog> QsoLogCollection { get; }

        /// <summary>
        /// 界面【当前显示的这批】日志原始实体（最多 MaxDisplayLogCount 条，按主键倒序）。
        /// ⚠ 注意：这不再是全表数据。若将来要做"按呼号/波段统计"之类的全表运算，
        ///   必须另行走数据库查询，不能拿这个列表当数据源。
        /// </summary>
        public List<QsoLog> AllQsoLogList { get; set; } = new List<QsoLog>();

        /// <summary>选中传播模式的 ADIF 代码（只读，自动从 SelectedItem 获取）。</summary>
        public string? SelectedPropCode => _SelectPROPItem?.Code;

        /// <summary>
        /// 选中通联模式的 ADIF 主模式（只读，存库值）。
        /// 例如选中「D-STAR」时为 "DIGITALVOICE"，选中「SSB」时为 "SSB"。
        /// </summary>
        public string? SelectedCommCode => _SelectCommMode?.Mode;

        /// <summary>
        /// 选中通联模式的 ADIF 子模式（只读，存库值）。
        /// 例如选中「D-STAR」时为 "DSTAR"；无子模式的模式（如 SSB）为 null。
        /// </summary>
        public string? SelectedSubmodeCode => _SelectCommMode?.Submode;

        /// <summary>选中传播模式的显示名称（只读，如 "F2层反射 F2"，仅用于界面展示）。</summary>
        public string? SelectedPropName => _SelectPROPItem?.DisplayName;

        /// <summary>选中通联模式的显示名称（只读，如 "D-STAR"，仅用于界面展示，不写库）。</summary>
        public string? SelectedCommName => _SelectCommMode?.DisplayName;

        /// <summary>每秒刷新时间的定时器（窗口关闭时由 View 停止）。</summary>
        public readonly DispatcherTimer _timer;


        /// <summary>
        /// 构造函数：
        /// 1. 初始化下拉列表数据源与默认选中项；
        /// 2. 从数据库加载最近的日志并刷新右侧卡片；
        /// 3. 从数据库加载软件设置并填入设置页；
        /// 4. 启动每秒时间刷新定时器。
        /// </summary>
        public MainLogWindow_VM()
        {
            PropagationModeList = StaticValues.Set_PropagationModeList;
            CommModeList = StaticValues.Set_CommModeList;
            QsoLogCollection = new ObservableCollection<OneLog>();

            _SelectPROPItem = PropagationModeList[0]; // 默认选择第一个选项
            _SelectCommMode = CommModeList[0];        // 默认选择第一个选项

            // 从数据库加载最近的日志并显示到右侧卡片列表。
            // ShowLogInView 内部会自行查库并填充 AllQsoLogList，此处不必再查一次
            // （原实现在这里先查一遍全表、紧接着又被 ShowLogInView 覆盖，属重复查询）。
            ShowLogInView();

            // 从数据库加载软件设置（本地台站信息）并填入设置页。
            // 从未保存过设置时界面保持为空，这属于正常情况而非错误。
            // 用 GetAwaiter().GetResult() 同步等待（与上面加载日志的写法保持一致）；
            // AppSettingsDbTools 内部的 await 均已加 ConfigureAwait(false)，不会死锁。
            ReloadSettingsAsync().GetAwaiter().GetResult();

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

        #region 辅助方法

        /// <summary>
        /// 刷新右侧日志卡片列表（QsoLogCollection）。
        ///
        /// 【取数范围】只取数据库中【最近的 MaxDisplayLogCount 条】。
        ///   排序与截断都在数据库层面完成（ORDER BY Id DESC LIMIT 200），
        ///   内存里只有这 200 条，与表的总行数无关。
        ///   查询失败时 Data 为空列表，界面显示空白，错误详情见 Logs 日志
        ///   （与改动前的静默行为保持一致，避免首次运行尚无数据表时误报）。
        ///
        /// 【排序】主键 Id 自增，Id 越大即越新，故查询结果本就是"最新在前"，
        ///   直接 Add 即可。原实现用 Insert(0, ...) 逐条插到队首，
        ///   既是 O(n²) 的写法，又隐含依赖"查询结果恰好升序"这个前提。
        /// </summary>
        private void ShowLogInView()
        {
            var (data, _) = GetLatestDBDataAsync<QsoLog>(MaxDisplayLogCount).Result;

            AllQsoLogList = data;

            QsoLogCollection.Clear();
            foreach (QsoLog log in data)
            {
                QsoLogCollection.Add(ConvertQsoLogToOneLog(log));
            }
        }

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

        #region 辅助方法（续：输入校验、界面提示等）



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
                ShowErrorMess(MessagePage.LogEdit, "请填写所有必填字段！");
                return false;
            }
            // 检查频率是否为有效的数字
            if (!double.TryParse(FREQ, out double frequency))
            {
                ShowErrorMess(MessagePage.LogEdit, "频率必须是有效的数字！");
                return false;
            }
            // 检查开始时间是否早于结束时间（复合 DateTime 直接比较）
            if (StartTimeDT >= EndTimeDT)
            {
                ShowErrorMess(MessagePage.LogEdit, "开始时间必须早于结束时间！");
                return false;
            }
            // 波段合法性校验结果
            if (!isValid)
            {
                ShowErrorMess(MessagePage.LogEdit, errorMessage);
                return false;
            }
            return true;
        }

        #endregion
    }
}
