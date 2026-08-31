using SharpLog.Model;
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
    /// 1. ConvertMainViewToQsoLog：表单参数 → QsoLog 实体（含时间拆分、字符串转类型）；
    /// 2. ConvertQsoLogToOneLog：QsoLog 实体 → OneLog 卡片显示模型（含时间组合、频率格式化）。
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
        /// 2. 频率：字符串 kHz 文本 → double（FREQ 存储语义保持 kHz）；
        /// 3. QSL 状态：字符串"是/否" → bool；
        /// 4. RadioOperator：当前硬编码为 "BG2GPT"（本站呼号）。
        /// </summary>
        /// <param name="startTimeDT">通联开始时间（复合，拆分为 QSO_DATE + TIME_ON）</param>
        /// <param name="endTimeDT">通联结束时间（复合，拆分为 QSO_DATE_OFF + TIME_OFF）</param>
        /// <param name="timeZone">时区（如 UTC、UTC+08:00）</param>
        /// <param name="call">对方呼号（ADIF: CALL）</param>
        /// <param name="freq">频率（kHz 文本，如 "7050"）</param>
        /// <param name="mode">通联模式（ADIF: MODE，来自下拉选中项的 Code）</param>
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
        /// <param name="op">操作员（项目自定义）</param>
        /// <param name="propMode">传播模式（ADIF: PROP_MODE，来自下拉选中项的 Code）</param>
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
            string call, string freq, string mode, string rstRcvd, string rstSent,
            string gridSquare, string rxPower, string txPower,
            string qth, string rig, string ant,
            string comment, string height, string op, string propMode, string digId,

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
                MODE = mode,
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
                OP = op,
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
                // 操作员：当前硬编码为本站呼号（后续可改为用户配置）
                RadioOperator = "BG2GPT"
            };
            return qsoLog;
        }

        /// <summary>
        /// 将 QsoLog 实体转换为 OneLog 卡片显示模型（用于右侧日志卡片列表）。
        ///
        /// 关键转换逻辑：
        /// 1. 时间组合：QSO_DATE + TIME_ON → "yyyy/MM/dd-HH:mm:ss" 显示字符串；
        /// 2. 卡片左侧竖条颜色 = 模式对应的 ModeColorMap 颜色；
        /// 3. 频率显示：FREQ 数值 + "khz" 后缀。
        /// ⚠️ 已知问题：未设置 PropModeText（卡片上的传播模式不显示）。
        /// </summary>
        /// <param name="qsoLog">数据库实体</param>
        /// <returns>卡片显示模型</returns>
        public static OneLog ConvertQsoLogToOneLog(QsoLog qsoLog)
        {
            OneLog oneLog = new OneLog
            {
                // 卡片左侧竖条颜色（按模式配色，见 StaticValues.ModeColorMap）
                CardSideBarColor = new System.Windows.Media.SolidColorBrush(StaticValues.ModeColorMap[qsoLog.MODE]),
                // 开始时间显示：QSO_DATE（日期）+ TIME_ON（时间）
                StartTime = CombineDateTimeString(qsoLog.QSO_DATE, qsoLog.TIME_ON),
                // 结束时间显示：QSO_DATE_OFF + TIME_OFF
                EndTime = CombineDateTimeString(qsoLog.QSO_DATE_OFF, qsoLog.TIME_OFF),
                TimeZoneSuffix = qsoLog.TimeZone,
                CALL = qsoLog.CALL,
                DisplayFrequency = qsoLog.FREQ.ToString() + "khz",
                MODE = qsoLog.MODE,
                RST_RCVD = qsoLog.RST_RCVD,
                RST_SENT = qsoLog.RST_SENT,
                QTH = qsoLog.QTH,
                RIG = qsoLog.RIG,
                Antenna = qsoLog.ANT,
                Operator = qsoLog.OP,
                GRIDSQUARE = qsoLog.GRIDSQUARE,
            };
            return oneLog;
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
