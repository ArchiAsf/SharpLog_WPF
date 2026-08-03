using SharpLog.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpLog.ViewModel
{
    public static class ModeConvertTools
    {
        /// <summary>
        /// 将输入框中的参数转换为QsoLog对象
        /// </summary>
        /// <param name="callSign"></param>
        /// <param name="freq"></param>
        /// <param name="rst"></param>
        /// <param name="Rrst"></param>
        /// <param name="gridLocator"></param>
        /// <param name="rxPower"></param>
        /// <param name="txPower"></param>
        /// <param name="qth"></param>
        /// <param name="rig"></param>
        /// <param name="ant"></param>
        /// <param name="startTimeDT"></param>
        /// <param name="endTimeDT"></param>
        /// <param name="rmks"></param>
        /// <returns></returns>
        public static QsoLog ConvertMainViewToQsoLog(
            DateTime startTimeDT, DateTime endTimeDT,string TimeZone,
            string callSign, string freq, string mode, string rst, string Rrst,
            string gridLocator, string rxPower, string txPower,
            string qth, string rig, string ant,
            string rmks, string height, string Op, string PropMode, string DigID)
        {
            QsoLog qsoLog = new QsoLog
            {
                StartTime = startTimeDT,
                EndTime = endTimeDT,
                TimeZone = TimeZone,
                CallSign = callSign,
                Frequency = double.Parse(freq),
                Mode = mode,
                RST = rst,
                ReceivedRST = Rrst,
                Locator = gridLocator,
                RxPower = rxPower,
                TxPower = txPower,
                QTH = qth,
                Rig = rig,
                ANT = ant,
                Remarks = rmks,
                Height = height,
                OP = Op,
                PropMode = PropMode,
                DigitalID = DigID,
                
                RadioOperator="BG2GPT"


            };
            return qsoLog;
        }

        /// <summary>
        /// 将QsoLog对象转换为OneLog对象
        /// </summary>
        /// <param name="qsoLog"></param>
        /// <returns></returns>
        public static OneLog ConvertQsoLogToOneLog(QsoLog qsoLog)
        {
            OneLog oneLog = new OneLog
            {
                CardSideBarColor = new System.Windows.Media.SolidColorBrush(StaticValues.ModeColorMap[qsoLog.Mode]),
                StartTime = qsoLog.StartTime.ToString("yyyy/MM/dd-HH:mm:ss"),
                EndTime = qsoLog.EndTime.ToString("yyyy/MM/dd-HH:mm:ss"),
                TimeZoneSuffix = qsoLog.TimeZone,
                CallSign = qsoLog.CallSign,
                DisplayFrequency = qsoLog.Frequency.ToString() + "khz",
                Mode = qsoLog.Mode,
                RST = qsoLog.RST,
                RRST = qsoLog.ReceivedRST,
                QTH = qsoLog.QTH,
                Rig = qsoLog.Rig,
                Antenna = qsoLog.ANT,
                Operator = qsoLog.OP,
                Locator = qsoLog.Locator,
            };
            return oneLog;
        }

    }
}
