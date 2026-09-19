using CommunityToolkit.Mvvm.ComponentModel;
using SharpLog.Model;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace SharpLog.ViewModel
{
    /// <summary>
    /// 通联记录编辑窗口（View/EditQso.xaml）的 ViewModel。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【为什么需要单独一个 VM，而不是直接把 QsoLog 当 DataContext】
    ///   QsoLog 是数据库实体，按 ADIF 规范存储，与界面控件需要的数据形态对不上：
    ///     · 时间：实体拆成 QSO_DATE（DateTime）+ TIME_ON（"143000" 文本）两列，
    ///             而 hc:DateTimePicker 只接受一个 DateTime；
    ///     · 模式：实体拆成 MODE（"DIGITALVOICE"）+ SUBMODE（"DSTAR"）两列，
    ///             而下拉框要的是一个 ModeItem 对象（用户看到的是 "D-STAR"）；
    ///     · 频率：实体是 double，界面输入框是文本，还要做格式校验。
    ///   若直接绑实体，这些转换只能硬塞进 XAML 转换器里，
    ///   而校验、保存这类有业务含义的逻辑就无处安放了。
    ///
    /// 【本 VM 目前是单向的】
    ///   构造函数把 QsoLog 的值转换后填进各属性，供界面显示。
    ///   反向（界面 → 实体）的保存逻辑，等按钮事件确定后再补。
    /// ════════════════════════════════════════════════════════════════════════
    ///
    /// 属性命名沿用实体的 ADIF 字段名，方便与 QsoLog、数据库表对照。
    /// </summary>
    public partial class EditQso_VM : ObservableObject
    {
        #region 构造与数据源

        /// <summary>通联模式下拉列表数据源（静态，见 StaticValues.Set_CommModeList）。</summary>
        public ObservableCollection<ModeItem> CommModeList { get; }

        /// <summary>传播模式下拉列表数据源（静态，见 StaticValues.Set_PropagationModeList）。</summary>
        public ObservableCollection<PropagationItem> PropagationModeList { get; }

        /// <summary>
        /// 用一条通联记录构造 VM，并把它的值转换后填进各绑定属性。
        /// </summary>
        /// <param name="source">要编辑的记录（由 MainLogWindow 双击 / 右键传入）</param>
        public EditQso_VM(QsoLog source)
        {
            // 两个下拉列表与主界面共用同一份静态数据源，保证选项完全一致
            CommModeList = StaticValues.Set_CommModeList;
            PropagationModeList = StaticValues.Set_PropagationModeList;

            LoadFrom(source);
        }

        #endregion

        #region 时间（hc:DateTimePicker 用）

        /// <summary>通联开始时间（复合，保存时拆回 QSO_DATE + TIME_ON）。</summary>
        [ObservableProperty]
        private DateTime _StartTimeDT;

        /// <summary>通联结束时间（复合，保存时拆回 QSO_DATE_OFF + TIME_OFF）。</summary>
        [ObservableProperty]
        private DateTime _EndTimeDT;

        #endregion

        #region 下拉框（ComboBox 用）

        /// <summary>选中的通联模式（ModeItem 对象，同时携带 Mode 与 Submode）。</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ModeDisplayName))]
        private ModeItem? _SelectCommMode;

        /// <summary>
        /// 当前选中模式的显示名（如 "SSB"），供 H1:J1 的颜色条绑定。
        /// ⚠ 为什么不让 XAML 直接绑 SelectCommMode.DisplayName：
        ///   那样在 SelectCommMode 为 null 时，WPF 会走不通属性路径链、
        ///   拿到 DependencyProperty.UnsetValue，转换器根本不会被调用。
        ///   用这个只读属性则永远返回可用的字符串（空值返回空串）。
        /// </summary>
        public string ModeDisplayName => SelectCommMode?.DisplayName ?? string.Empty;

        /// <summary>选中的传播模式（PropagationItem 对象，Code 为 null 表示「不指定」）。</summary>
        [ObservableProperty]
        private PropagationItem? _SelectPROPItem;

        #endregion

        #region 数字（hc:NumericUpDown 用）

        /// <summary>通联分组（0~5）。</summary>
        [ObservableProperty]
        private int? _GroupID;

        /// <summary>通联星级（0~5）。</summary>
        [ObservableProperty]
        private int? _StarID;

        #endregion

        #region 主表文本字段

        /// <summary>对方呼号（ADIF: CALL）。</summary>
        [ObservableProperty]
        private string _CALL = string.Empty;

        /// <summary>频率，单位 kHz 的文本（ADIF: FREQ，实体里是 double）。</summary>
        [ObservableProperty]
        private string _FREQ = string.Empty;

        /// <summary>收到的信号报告（ADIF: RST_RCVD）。</summary>
        [ObservableProperty]
        private string _RST_RCVD = string.Empty;

        /// <summary>发出的信号报告（ADIF: RST_SENT）。</summary>
        [ObservableProperty]
        private string _RST_SENT = string.Empty;

        /// <summary>网格定位（ADIF: GRIDSQUARE）。</summary>
        [ObservableProperty]
        private string _GRIDSQUARE = string.Empty;

        #endregion

        #region 上方滚动区字段

        /// <summary>对方城市（ADIF: QTH）。</summary>
        [ObservableProperty]
        private string _QTH = string.Empty;

        /// <summary>对方发射功率（ADIF: RX_PWR）。</summary>
        [ObservableProperty]
        private string _RX_PWR = string.Empty;

        /// <summary>我方发射功率（ADIF: TX_PWR）。</summary>
        [ObservableProperty]
        private string _TX_PWR = string.Empty;

        /// <summary>对方电台型号（ADIF: RIG）。</summary>
        [ObservableProperty]
        private string _RIG = string.Empty;

        /// <summary>对方天线（项目自定义字段 ANT）。</summary>
        [ObservableProperty]
        private string _ANT = string.Empty;

        /// <summary>天线高度（项目自定义字段 Height）。</summary>
        [ObservableProperty]
        private string _Height = string.Empty;

        /// <summary>对方值机员（ADIF: CONTACTED_OP）。</summary>
        [ObservableProperty]
        private string _CONTACTED_OP = string.Empty;

        /// <summary>
        /// 结束日期（ADIF: QSO_DATE_OFF）的显示文本。
        /// ⚠ 本格用普通 TextBox 显示，故转成 yyyy-MM-dd 字符串，
        ///   避免 DateTime 直接绑 TextBox 时显示出带时间的完整格式。
        /// </summary>
        [ObservableProperty]
        private string _QSO_DATE_OFF = string.Empty;

        /// <summary>结束时间（ADIF: TIME_OFF）的显示文本，格式 HH:mm:ss。</summary>
        [ObservableProperty]
        private string _TIME_OFF = string.Empty;

        /// <summary>天气（项目自定义字段 Weather）。</summary>
        [ObservableProperty]
        private string _Weather = string.Empty;

        /// <summary>温度（项目自定义字段 Temperature）。</summary>
        [ObservableProperty]
        private string _Temperature = string.Empty;

        #endregion

        #region 底部滚动区字段

        /// <summary>
        /// QSL 卡片是否已收到（ADIF: QSL_RCVD）。
        /// ⚠ 实体里是 bool，界面上用「是 / 否」文本显示，故这里存字符串。
        /// </summary>
        [ObservableProperty]
        private string _QSL_RCVD = string.Empty;

        /// <summary>收卡日期（ADIF: QSLRDATE）的显示文本，yyyy-MM-dd。</summary>
        [ObservableProperty]
        private string _QSLRDATE = string.Empty;

        /// <summary>发卡日期（ADIF: QSLSDATE）的显示文本，yyyy-MM-dd。</summary>
        [ObservableProperty]
        private string _QSLSDATE = string.Empty;

        /// <summary>QSL 邮寄地址（项目自定义字段 QslAddress）。</summary>
        [ObservableProperty]
        private string _QslAddress = string.Empty;

        /// <summary>邮政编码（项目自定义字段 PostalCode）。</summary>
        [ObservableProperty]
        private string _PostalCode = string.Empty;

        /// <summary>收件人姓名（项目自定义字段 ReceiverName）。</summary>
        [ObservableProperty]
        private string _ReceiverName = string.Empty;

        /// <summary>收件人联系方式（项目自定义字段 ReceiverContact）。</summary>
        [ObservableProperty]
        private string _ReceiverContact = string.Empty;

        /// <summary>本站操作员（项目自定义字段 RadioOperator）。</summary>
        [ObservableProperty]
        private string _RadioOperator = string.Empty;

        /// <summary>时区（ADIF: 由 TimeZone 记录，如 "UTC+8"）。</summary>
        [ObservableProperty]
        private string _TimeZone = string.Empty;

        /// <summary>DMR 数字集群 ID（项目自定义字段 DigitalID）。</summary>
        [ObservableProperty]
        private string _DigitalID = string.Empty;

        /// <summary>
        /// 是否双向确认通联（项目自定义字段 IsConfirmed）。
        /// ⚠ 同 QSL_RCVD，实体是 bool，界面用「是 / 否」文本。
        /// </summary>
        [ObservableProperty]
        private string _IsConfirmed = string.Empty;

        #endregion

        #region 备注

        /// <summary>备注（ADIF: COMMENT），多行文本。</summary>
        [ObservableProperty]
        private string _COMMENT = string.Empty;

        #endregion

        #region 载入：QsoLog → VM

        /// <summary>
        /// 把数据库实体的值转换后填进各绑定属性。
        /// </summary>
        private void LoadFrom(QsoLog source)
        {
            // ── 时间：日期 + HHmmss 文本 合成一个 DateTime ──
            StartTimeDT = CombineDateTime(source.QSO_DATE, source.TIME_ON);
            EndTimeDT = CombineDateTime(source.QSO_DATE_OFF, source.TIME_OFF);

            // ── 下拉框：由 MODE + SUBMODE 反查对应的下拉项对象 ──
            SelectCommMode = FindModeItem(source.MODE, source.SUBMODE);
            SelectPROPItem = FindPropItem(source.PROP_MODE);

            // ── 数字 ──
            GroupID = source.GroupID;
            StarID = source.StarID;

            // ── 主表文本 ──
            CALL = source.CALL ?? string.Empty;
            // 频率用 InvariantCulture 转文本，避免在某些区域设置下小数点变成逗号
            FREQ = source.FREQ.ToString(CultureInfo.InvariantCulture);
            RST_RCVD = source.RST_RCVD ?? string.Empty;
            RST_SENT = source.RST_SENT ?? string.Empty;
            GRIDSQUARE = source.GRIDSQUARE ?? string.Empty;

            // ── 上方滚动区 ──
            QTH = source.QTH ?? string.Empty;
            RX_PWR = source.RX_PWR ?? string.Empty;
            TX_PWR = source.TX_PWR ?? string.Empty;
            RIG = source.RIG ?? string.Empty;
            ANT = source.ANT ?? string.Empty;
            Height = source.Height ?? string.Empty;
            CONTACTED_OP = source.CONTACTED_OP ?? string.Empty;
            QSO_DATE_OFF = source.QSO_DATE_OFF?.ToString("yyyy-MM-dd") ?? string.Empty;
            TIME_OFF = FormatHhmmss(source.TIME_OFF);
            Weather = source.Weather ?? string.Empty;
            Temperature = source.Temperature ?? string.Empty;

            // ── 底部滚动区 ──
            QSL_RCVD = BoolToYesNo(source.QSL_RCVD);
            QSLRDATE = source.QSLRDATE?.ToString("yyyy-MM-dd") ?? string.Empty;
            QSLSDATE = source.QSLSDATE?.ToString("yyyy-MM-dd") ?? string.Empty;
            QslAddress = source.QslAddress ?? string.Empty;
            PostalCode = source.PostalCode ?? string.Empty;
            ReceiverName = source.ReceiverName ?? string.Empty;
            ReceiverContact = source.ReceiverContact ?? string.Empty;
            RadioOperator = source.RadioOperator ?? string.Empty;
            TimeZone = source.TimeZone ?? string.Empty;
            DigitalID = source.DigitalID ?? string.Empty;
            IsConfirmed = BoolToYesNo(source.IsConfirmed);

            // ── 备注 ──
            COMMENT = source.COMMENT ?? string.Empty;
        }

        #endregion

        #region 转换辅助

        /// <summary>
        /// 把 ADIF 的「日期 + HHmmss 时间文本」合成一个 DateTime。
        ///
        /// 容错规则：
        ///   · 日期为空 → 用今天兜底（编辑窗口的 DateTimePicker 不接受空值）；
        ///   · 时间为空或格式不对 → 只取日期，时分秒归零；
        ///   · 两者都缺 → 返回今天零点。
        /// </summary>
        /// <param name="date">QSO_DATE 或 QSO_DATE_OFF</param>
        /// <param name="time">TIME_ON 或 TIME_OFF，格式 HHmmss（如 "143000"）</param>
        private static DateTime CombineDateTime(DateTime? date, string? time)
        {
            DateTime baseDate = date ?? DateTime.Today;

            if (!string.IsNullOrWhiteSpace(time)
                && TimeOnly.TryParseExact(time, "HHmmss", CultureInfo.InvariantCulture,
                                          DateTimeStyles.None, out TimeOnly parsedTime))
            {
                return baseDate.Date + parsedTime.ToTimeSpan();
            }

            return baseDate;
        }

        /// <summary>
        /// HHmmss 文本 → "HH:mm:ss" 显示格式；解析失败或为空时原样返回空串。
        /// </summary>
        private static string FormatHhmmss(string? time)
        {
            if (!string.IsNullOrWhiteSpace(time)
                && TimeOnly.TryParseExact(time, "HHmmss", CultureInfo.InvariantCulture,
                                          DateTimeStyles.None, out TimeOnly parsedTime))
            {
                return parsedTime.ToString("HH:mm:ss");
            }

            return string.Empty;
        }

        /// <summary>bool → 「是 / 否」。</summary>
        private static string BoolToYesNo(bool value) => value ? "是" : "否";

        /// <summary>
        /// 由 ADIF 的 MODE + SUBMODE 反查下拉列表里对应的那一项。
        ///
        /// 两级匹配：先找 Mode 与 Submode 都一致的（如 DIGITALVOICE + DSTAR），
        /// 再退回只匹配 Mode 且该项没有子模式的通用项（如 SSB）。
        /// 库里存的是外部导入的历史数据、模式名本项目未收录时，
        /// 返回 null，界面上下拉框显示为空（而不是抛异常）。
        /// </summary>
        private ModeItem? FindModeItem(string? mode, string? submode)
        {
            if (string.IsNullOrWhiteSpace(mode)) return null;

            // ① 主模式 + 子模式都一致
            ModeItem? exact = CommModeList.FirstOrDefault(
                m => string.Equals(m.Mode, mode, StringComparison.OrdinalIgnoreCase)
                  && string.Equals(m.Submode, submode, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // ② 只匹配主模式、且该项本身没有子模式
            return CommModeList.FirstOrDefault(
                m => string.Equals(m.Mode, mode, StringComparison.OrdinalIgnoreCase)
                  && string.IsNullOrEmpty(m.Submode));
        }

        /// <summary>
        /// 由 ADIF 的 PROP_MODE 代码反查传播模式下拉项。
        /// 代码为 null 时匹配到的正是首项「（不指定）」（它的 Code 也是 null）。
        /// </summary>
        private PropagationItem? FindPropItem(string? code)
        {
            return PropagationModeList.FirstOrDefault(
                p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region 校验

        /// <summary>
        /// 保存前的统一校验：呼号格式、频率合法性、起止时间先后。
        ///
        /// 【为什么不在这里直接弹消息】
        ///   项目规范要求 ViewModel 不操作 UI；本方法只把结果返回给调用方，
        ///   由 View 或命令层决定用 Growl 显示还是别的处理方式。
        /// </summary>
        /// <returns>(是否通过, 错误信息)；通过时错误信息为空串</returns>
        public (bool IsValid, string ErrorMessage) Validate()
        {
            // ① 呼号格式 + 频率格式与波段归属（复用已有的静态校验工具）
            var (basicOk, basicMsg) = QsoLogValidator.ValidateLogInput(CALL, FREQ);
            if (!basicOk) return (false, basicMsg);

            // ② 开始时间必须早于结束时间
            var (timeOk, timeMsg) = QsoLogValidator.ValidateTimeRange(StartTimeDT, EndTimeDT);
            if (!timeOk) return (false, timeMsg);

            return (true, string.Empty);
        }

        #endregion
    }
}
