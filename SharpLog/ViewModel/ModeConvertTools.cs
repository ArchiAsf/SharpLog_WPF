using SharpLog.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpLog.ViewModel
{
    public static class ModeConvertTools
    {
        /// <summary>
        /// 将输入框中的参数转换为QsoLog对象
        /// 字段命名已统一为 ADIF 3.1.7 规范名称
        /// 时间说明：UI 使用复合 DateTime（hc:DateTimePicker）输入，
        /// 此处拆分为 QSO_DATE + TIME_ON / QSO_DATE_OFF + TIME_OFF 分别存储
        /// </summary>
        /// <param name="startTimeDT">通联开始时间（复合，拆分存储为 ADIF: QSO_DATE + TIME_ON）</param>
        /// <param name="endTimeDT">通联结束时间（复合，拆分存储为 ADIF: QSO_DATE_OFF + TIME_OFF）</param>
        /// <param name="timeZone">时区</param>
        /// <param name="call">对方呼号（ADIF: CALL）</param>
        /// <param name="freq">频率（kHz 输入）</param>
        /// <param name="mode">通联模式（ADIF: MODE）</param>
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
        /// <param name="propMode">传播模式（ADIF: PROP_MODE）</param>
        /// <param name="digId">DMR数字ID（项目自定义）</param>
        /// <param name="groupID">分组ID（项目自定义）</param>
        /// <param name="starID">星级ID（项目自定义）</param>
        /// <param name="qslRcvd">是否收到QSL卡片（ADIF: QSL_RCVD，string 是/否）</param>
        /// <param name="qslRDate">QSL接收日期（ADIF: QSLRDATE）</param>
        /// <param name="qslSDate">QSL发送日期（ADIF: QSLSDATE）</param>
        /// <param name="qslAddress">QSL地址（项目自定义）</param>
        /// <param name="receiverName">收件人（项目自定义）</param>
        /// <param name="postalCode">邮编（项目自定义）</param>
        /// <param name="receiverContact">联系方式（项目自定义）</param>
        /// <returns></returns>
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
                // 复合时间拆分：日期存 QSO_DATE（取 Date 部分），时间存 TIME_ON（HHmmss 文本）
                QSO_DATE = startTimeDT.Date,
                TIME_ON = startTimeDT.ToString("HHmmss"),
                QSO_DATE_OFF = endTimeDT.Date,
                TIME_OFF = endTimeDT.ToString("HHmmss"),
                TimeZone = timeZone,
                CALL = call,
                FREQ = double.Parse(freq),
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
                GroupID = groupID,
                StarID = starID,
                QSL_RCVD = qslRcvd == "是" ? true : false,
                QSLRDATE = qslRDate,
                QSLSDATE = qslSDate,
                QslAddress = qslAddress,
                ReceiverName = receiverName,
                PostalCode = postalCode,
                ReceiverContact = receiverContact,

                RadioOperator = "BG2GPT"


            };
            return qsoLog;
        }

        /// <summary>
        /// 将QsoLog对象转换为OneLog对象（日志卡片显示模型）
        /// </summary>
        /// <param name="qsoLog"></param>
        /// <returns></returns>
        public static OneLog ConvertQsoLogToOneLog(QsoLog qsoLog)
        {
            OneLog oneLog = new OneLog
            {
                CardSideBarColor = new System.Windows.Media.SolidColorBrush(StaticValues.ModeColorMap[qsoLog.MODE]),
                // 开始时间显示：QSO_DATE(日期) + TIME_ON(时间)，格式 yyyy/MM/dd-HH:mm:ss
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
        /// 组合 日期+时间 为显示字符串（yyyy/MM/dd-HH:mm:ss），任一缺失则只显示存在的部分
        /// </summary>
        private static string CombineDateTimeString(DateTime? date, string? time)
        {
            string dateStr = date?.ToString("yyyy/MM/dd") ?? string.Empty;
            string timeStr = string.Empty;
            if (!string.IsNullOrWhiteSpace(time) && TimeOnly.TryParseExact(time, "HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            {
                timeStr = t.ToString("HH:mm:ss");
            }
            if (string.IsNullOrEmpty(dateStr)) return timeStr;
            if (string.IsNullOrEmpty(timeStr)) return dateStr;
            return dateStr + "-" + timeStr;
        }

    }
}
