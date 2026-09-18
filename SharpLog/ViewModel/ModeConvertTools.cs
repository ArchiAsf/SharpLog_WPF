using SharpLog.Model;
using SharpLog.ToolsBox;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpLog.ViewModel
{
    /// <summary>
    /// 模式转换工具类（静态，无状态）。
    ///
    /// 职责：在「UI 表单 / QsoLog 实体 / OneLog 卡片显示模型」三者之间做数据转换：
    /// 1. ConvertMainViewToQsoLog：表单参数 → QsoLog 实体
    ///    （含时间拆分 QSO_DATE+TIME_ON，模式拆分 MODE+SUBMODE）；
    /// 2. ConvertQsoLogToOneLog：QsoLog 实体 → OneLog 卡片显示模型
    ///    （含时间组合、模式反查为显示名、按模式取卡片配色）；
    /// 3. ResolveDisplayName / ResolveModeColor：由 ADIF 的 MODE+SUBMODE
    ///    反查界面显示名与配色（供卡片显示使用）。
    ///
    /// 命名规范：字段名已统一为 ADIF 3.1.7 标准字段名。
    /// </summary>
    public static class ModeConvertTools
    {
        /// <summary>
        /// 将表单输入参数转换为 QsoLog 实体（用于写入数据库）。
        ///
        /// 关键转换逻辑：
        /// 1. 时间拆分：UI 使用复合 DateTime（hc:DateTimePicker）输入，
        ///    此处按 ADIF 规范拆分为 QSO_DATE（日期）+ TIME_ON（时间 HHmmss）分别存储；
        /// 2. 模式拆分：界面下拉选中项（ModeItem）拆成主模式 mode 与子模式 submode，
        ///    例如界面选「FT4」时 mode="MFSK"、submode="FT4"；
        /// 3. 频率：字符串 kHz 文本 → double（FREQ 存储语义保持 kHz）；
        /// 4. QSL 状态：字符串"是/否" → bool；
        /// </summary>
        /// <param name="startTimeDT">通联开始时间（复合，拆分为 QSO_DATE + TIME_ON）</param>
        /// <param name="endTimeDT">通联结束时间（复合，拆分为 QSO_DATE_OFF + TIME_OFF）</param>
        /// <param name="timeZone">时区（如 UTC、UTC+08:00）</param>
        /// <param name="call">对方呼号（ADIF: CALL）</param>
        /// <param name="freq">频率（kHz 文本，如 "7050"）</param>
        /// <param name="mode">通联主模式（ADIF: MODE，取自选中项的 ModeItem.Mode）</param>
        /// <param name="submode">通联子模式（ADIF: SUBMODE，取自 ModeItem.Submode；无子模式时传 null）</param>
        /// <param name="rstRcvd">收到的信号报告（ADIF: RST_RCVD）</param>
        /// <param name="rstSent">发出的信号报告（ADIF: RST_SENT）</param>
        /// <param name="gridSquare">网格定位（ADIF: GRIDSQUARE）</param>
        /// <param name="rxPower">对方发射功率（ADIF: RX_PWR）</param>
        /// <param name="txPower">我方发射功率（ADIF: TX_PWR）</param>
        /// <param name="qth">对方城市（ADIF: QTH）</param>
        /// <param name="rig">电台型号（ADIF: RIG）</param>
        /// <param name="ant">天线类型（项目自定义）</param>
        /// <param name="comment">备注（ADIF: COMMENT）</param>
        /// <param name="height">发射高度（项目自定义）</param>
        /// <param name="propMode">传播模式（ADIF: PROP_MODE，来自下拉选中项的 Code；选「（不指定）」时传 null）</param>
        /// <param name="digId">DMR 数字 ID（项目自定义）</param>
        /// <param name="groupID">分组 ID（项目自定义）</param>
        /// <param name="starID">星级 ID（项目自定义）</param>
        /// <param name="qslRcvd">是否收到 QSL 卡片（"是"/"否" 字符串）</param>
        /// <param name="qslRDate">QSL 接收日期（ADIF: QSLRDATE）</param>
        /// <param name="qslSDate">QSL 发送日期（ADIF: QSLSDATE）</param>
        /// <param name="qslAddress">QSL 地址（项目自定义）</param>
        /// <param name="receiverName">收件人（项目自定义）</param>
        /// <param name="postalCode">邮编（项目自定义）</param>
        /// <param name="receiverContact">联系方式（项目自定义）</param>
        /// <returns>组装完成的 QsoLog 实体</returns>
        public static QsoLog ConvertMainViewToQsoLog(
            DateTime startTimeDT, DateTime endTimeDT, string timeZone,
            string call, string freq, string mode, string? submode, string rstRcvd, string rstSent,
            string gridSquare, string rxPower, string txPower,
            string qth, string rig, string ant,
            string comment, string height, string? propMode, string digId,

            int? groupID, int starID,
            string qslRcvd, DateTime? qslRDate, DateTime? qslSDate,
            string qslAddress, string receiverName, string postalCode, string receiverContact)
        {
            QsoLog qsoLog = new QsoLog
            {
                // 时间拆分：日期存 QSO_DATE（取 Date 部分），时间存 TIME_ON（HHmmss 文本）
                QSO_DATE = startTimeDT.Date,
                TIME_ON = startTimeDT.ToString("HHmmss"),
                QSO_DATE_OFF = endTimeDT.Date,
                TIME_OFF = endTimeDT.ToString("HHmmss"),
                TimeZone = timeZone,
                // 对方电台信息
                CALL = call,
                FREQ = double.Parse(freq),          // kHz 文本 → double
                // 模式按 ADIF 两级体系分别存库：
                // MODE 存官方主模式（如 DIGITALVOICE），SUBMODE 存具体制式（如 DSTAR，无则 null）
                MODE = mode,
                SUBMODE = submode,
                RST_RCVD = rstRcvd,
                RST_SENT = rstSent,
                GRIDSQUARE = gridSquare,
                RX_PWR = rxPower,
                TX_PWR = txPower,
                QTH = qth,
                RIG = rig,
                ANT = ant,
                COMMENT = comment,
                Height = height,
                // 本站操作员呼号：从全局设置缓存中读取
                // （优先取 STATION_CALLSIGN，其次 OPERATOR）。
                // ⚠ 该列在数据库中是 NOT NULL，所以缓存里没有值时返回空串而非 null。
                RadioOperator = AppSettingsCache.RadioOperatorForQso,
                PROP_MODE = propMode,
                DigitalID = digId,
                // 分组 / 星级（项目自定义）
                GroupID = groupID,
                StarID = starID,
                // QSL 管理
                QSL_RCVD = qslRcvd == "是" ? true : false,   // "是/否" → bool
                QSLRDATE = qslRDate,
                QSLSDATE = qslSDate,
                QslAddress = qslAddress,
                ReceiverName = receiverName,
                PostalCode = postalCode,
                ReceiverContact = receiverContact,
            };
            return qsoLog;
        }

        /// <summary>
        /// 将 QsoLog 实体转换为 OneLog 卡片显示模型（用于右侧日志卡片列表）。
        ///
        /// 关键转换逻辑：
        /// 1. 时间组合：QSO_DATE + TIME_ON → "yyyy/MM/dd-HH:mm:ss" 显示字符串；
        /// 2. 模式显示：由 MODE + SUBMODE 反查界面显示名
        ///    （数据库中存的是 DIGITALVOICE + DSTAR，卡片上要显示 "D-STAR"）；
        /// 3. 卡片左侧竖条颜色 = 模式显示名对应的配色（含兜底，不会因未知模式崩溃）；
        /// 4. 频率显示：FREQ 数值 + "khz" 后缀。
        /// </summary>
        /// <param name="qsoLog">数据库实体</param>
        /// <returns>卡片显示模型</returns>
        public static OneLog ConvertQsoLogToOneLog(QsoLog qsoLog)
        {
            // 先算出模式显示名，供卡片文字与配色共用，避免重复反查
            string modeDisplayName = ResolveDisplayName(qsoLog.MODE, qsoLog.SUBMODE);

            OneLog oneLog = new OneLog
            {
                // 卡片左侧竖条颜色（按模式显示名取色，见 StaticValues.ModeColorMap）
                CardSideBarColor = new System.Windows.Media.SolidColorBrush(
                    ResolveModeColor(qsoLog.MODE, qsoLog.SUBMODE)),
                // 开始时间显示：QSO_DATE（日期）+ TIME_ON（时间）
                StartTime = CombineDateTimeString(qsoLog.QSO_DATE, qsoLog.TIME_ON),
                // 结束时间显示：QSO_DATE_OFF + TIME_OFF
                EndTime = CombineDateTimeString(qsoLog.QSO_DATE_OFF, qsoLog.TIME_OFF),
                TimeZoneSuffix = qsoLog.TimeZone,
                CALL = qsoLog.CALL,
                DisplayFrequency = qsoLog.FREQ.ToString() + "khz",
                // 卡片上显示的是模式显示名（如 "D-STAR"），不是 ADIF 主模式（如 "DIGITALVOICE"）
                MODE = modeDisplayName,
                // 传播模式显示名：由 PROP_MODE 代码反查（如 "F2" → "F2层反射 F2"）；
                // 未记录传播方式（库中为 NULL）时返回空串，卡片上该处显示为空白
                PropModeText = ResolvePropDisplayName(qsoLog.PROP_MODE),
                RST_RCVD = qsoLog.RST_RCVD,
                RST_SENT = qsoLog.RST_SENT,
                QTH = qsoLog.QTH,
                RIG = qsoLog.RIG,
                Antenna = qsoLog.ANT,
                GRIDSQUARE = qsoLog.GRIDSQUARE,
            };
            return oneLog;
        }

        /// <summary>
        /// 由 ADIF 的 MODE + SUBMODE 反查界面显示名（如 MODE=MFSK, SUBMODE=FT4 → "FT4"）。
        ///
        /// 转换规则（按优先级依次尝试）：
        /// 1. MODE 与 SUBMODE 都一致的项
        ///    （如 DIGITALVOICE + DSTAR → "D-STAR"）；
        /// 2. 只匹配 MODE、且该项没有子模式的「通用项」
        ///    （如 DIGITALVOICE + null → "数字语音"）；
        /// 3. 都没找到，则原样返回 mode
        ///    （如外部导入的 "Q65"，本项目的下拉列表里没有，就直接显示 ADIF 代码）。
        ///
        /// 采用「逐级回落」的写法，保证任何输入都能得到可显示的文字，
        /// 不会出现空白或抛异常。
        /// 比较时忽略大小写，以兼容外部 ADIF 文件中的大小写差异。
        /// </summary>
        /// <param name="mode">ADIF 主模式（QsoLog.MODE），可为 null</param>
        /// <param name="submode">ADIF 子模式（QsoLog.SUBMODE），无子模式时为 null</param>
        /// <returns>界面显示名；输入为空时返回空串</returns>
        public static string ResolveDisplayName(string? mode, string? submode)
        {
            // 容错：MODE 为空（正常情况下 MODE 是必填字段，此处仅为防御）
            if (string.IsNullOrWhiteSpace(mode)) return string.Empty;

            var list = StaticValues.Set_CommModeList;

            // 第 1 级：MODE + SUBMODE 精确匹配（忽略大小写）
            var exact = list.FirstOrDefault(m =>
                string.Equals(m.Mode, mode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.Submode, submode, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact.DisplayName;

            // 第 2 级：只匹配 MODE，并且要求该项没有子模式（即列表里的「通用项」）
            var generic = list.FirstOrDefault(m =>
                string.Equals(m.Mode, mode, StringComparison.OrdinalIgnoreCase) &&
                m.Submode == null);
            if (generic != null) return generic.DisplayName;

            // 第 3 级：下拉列表中没有该模式，直接显示 ADIF 代码本身
            return mode;
        }

        /// <summary>
        /// 按模式取卡片左侧竖条的颜色（内部先反查显示名，再查配色字典）。
        ///
        /// 使用 TryGetValue 而非索引器 StaticValues.ModeColorMap[key]：
        /// 索引器在键不存在时会抛 KeyNotFoundException 直接使程序崩溃，
        /// 而 TryGetValue 未命中时回落到中性灰（DefaultModeColor），
        /// 保证导入外部 ADIF 文件、或数据库存在历史数据时界面依然正常。
        /// </summary>
        /// <param name="mode">ADIF 主模式</param>
        /// <param name="submode">ADIF 子模式</param>
        /// <returns>配色；未收录的模式返回兜底色</returns>
        public static System.Windows.Media.Color ResolveModeColor(string? mode, string? submode)
        {
            string displayName = ResolveDisplayName(mode, submode);

            if (StaticValues.ModeColorMap.TryGetValue(displayName, out var color))
            {
                return color;
            }

            return StaticValues.DefaultModeColor;
        }

        /// <summary>
        /// 由 ADIF 的 PROP_MODE 代码反查界面显示名（如 "F2" → "F2层反射 F2"）。
        ///
        /// 转换规则：
        /// 1. 在 Set_PropagationModeList 中按 Code 匹配（忽略大小写），命中则返回其 DisplayName；
        /// 2. 代码为空（NULL，即用户选了「（不指定）」）时返回空串，卡片上不显示内容；
        /// 3. 列表中找不到（外部导入的 ADIF 文件可能带本项目未收录的代码）则原样返回代码。
        ///
        /// 注意：首项「（不指定）」的 Code 为 null，
        /// string.Equals(null, code) 在 code 非空时恒为 false，不会被误匹配。
        /// </summary>
        /// <param name="code">ADIF 传播模式代码（QsoLog.PROP_MODE），可为 null</param>
        /// <returns>界面显示名；未记录传播方式时返回空串</returns>
        public static string ResolvePropDisplayName(string? code)
        {
            // 未记录传播方式 → 卡片上不显示任何内容
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;

            var item = StaticValues.Set_PropagationModeList.FirstOrDefault(
                m => string.Equals(m.Code, code, StringComparison.OrdinalIgnoreCase));

            // 命中则用中文显示名，未收录则原样显示 ADIF 代码
            return item?.DisplayName ?? code;
        }

        /// <summary>
        /// 组合 日期+时间 为卡片显示字符串（格式 yyyy/MM/dd-HH:mm:ss）。
        /// 日期或时间任一缺失时只显示存在的部分（容错处理）。
        /// </summary>
        /// <param name="date">日期（QSO_DATE 或 QSO_DATE_OFF）</param>
        /// <param name="time">时间文本（TIME_ON 或 TIME_OFF，HHmmss）</param>
        /// <returns>显示字符串，如 "2026/08/31-14:30:00"</returns>
        private static string CombineDateTimeString(DateTime? date, string? time)
        {
            // 日期部分：yyyy/MM/dd
            string dateStr = date?.ToString("yyyy/MM/dd") ?? string.Empty;
            // 时间部分：HHmmss 文本 → HH:mm:ss（解析失败则忽略时间）
            string timeStr = string.Empty;
            if (!string.IsNullOrWhiteSpace(time)
                && TimeOnly.TryParseExact(time, "HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            {
                timeStr = t.ToString("HH:mm:ss");
            }
            // 组合：日期与时间都有 → "日期-时间"；只有其一 → 只显示其一；都无 → 空串
            if (string.IsNullOrEmpty(dateStr)) return timeStr;
            if (string.IsNullOrEmpty(timeStr)) return dateStr;
            return dateStr + "-" + timeStr;
        }
    }
}
