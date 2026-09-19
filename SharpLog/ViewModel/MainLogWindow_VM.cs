using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpLog.DB;
using SharpLog.Model;
using SharpLog.ToolsBox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    }
}