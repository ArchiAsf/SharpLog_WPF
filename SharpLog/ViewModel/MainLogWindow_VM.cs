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

namespace SharpLog.ViewModel
{
    [ObservableObject]
    public partial class MainLogWindow_VM
    {

        #region 日志信息相关绑定属性
        //注意：字段命名已统一为 ADIF 3.1.7 规范名称（CALL/FREQ/RST_SENT/RST_RCVD/GRIDSQUARE 等）
        //时间输入使用复合 DateTime（hc:DateTimePicker 绑定），后台转换时拆分为
        //QSO_DATE + TIME_ON / QSO_DATE_OFF + TIME_OFF 分别存储（见 ModeConvertTools）

        //通联开始时间（复合输入，后台拆分存储为 ADIF: QSO_DATE + TIME_ON）
        [ObservableProperty]
        private DateTime _StartTimeDT = LogUseTime.DateTime;

        //通联结束时间（复合输入，后台拆分存储为 ADIF: QSO_DATE_OFF + TIME_OFF）
        [ObservableProperty]
        private DateTime _EndTimeDT = LogUseTime.DateTime;

        //对方呼号（ADIF: CALL）
        [ObservableProperty]
        private string _CALL = string.Empty;

        partial void OnCALLChanged(string value)
        {
            var upper = value.ToUpperInvariant();
            if (value != upper)
            {
                CALL = upper;
            }
        }

        //备注（ADIF: COMMENT）
        [ObservableProperty]
        private string _COMMENT = string.Empty;

        //频率（ADIF: FREQ，输入为 kHz，如 7050）
        [ObservableProperty]
        private string _FREQ = string.Empty;

        //收到的信号报告（ADIF: RST_RCVD，UI「信号报告_收」）
        [ObservableProperty]
        private string _RST_RCVD = string.Empty;

        //发出的信号报告（ADIF: RST_SENT，UI「信号报告_发」）
        [ObservableProperty]
        private string _RST_SENT = string.Empty;

        //网格定位（ADIF: GRIDSQUARE，Maidenhead 网格）
        [ObservableProperty]
        private string _GRIDSQUARE = string.Empty;
        partial void OnGRIDSQUAREChanged(string value)
        {
            var upper = value.ToUpperInvariant();
            if (value != upper)
            {
                GRIDSQUARE = upper;
            }
        }

        //对方发射功率（ADIF: RX_PWR）
        [ObservableProperty]
        private string _RX_PWR = string.Empty;

        //我方发射功率（ADIF: TX_PWR）
        [ObservableProperty]
        private string _TX_PWR = string.Empty;

        //对方QTH城市（ADIF: QTH）
        [ObservableProperty]
        private string _QTH = string.Empty;

        //电台型号（ADIF: RIG）
        [ObservableProperty]
        private string _RIG = string.Empty;

        //天线类型（项目自定义，ADIF 无对应，保留）
        [ObservableProperty]
        private string _ant = string.Empty;

        //发射高度（项目自定义，ADIF 无对应，保留）
        [ObservableProperty]
        private string _height = string.Empty;

        //操作员（项目自定义，ADIF 无对应，保留）
        [ObservableProperty]
        private string _operator = string.Empty;

        //DMR数字ID（项目自定义，ADIF 无对应，保留）
        [ObservableProperty]
        private string _DigID = string.Empty;

        //分组ID（项目自定义）
        [ObservableProperty]
        private int _GroupID = 0;

        //星级ID（项目自定义）
        [ObservableProperty]
        private int _StarID = 0;

        //是否收到QSL卡片（ADIF: QSL_RCVD；当前 string 是/否，bool 转换在转换工具中处理）
        [ObservableProperty]
        private string _QSL_RCVD = string.Empty;

        //QSL卡片接收日期（ADIF: QSLRDATE）
        [ObservableProperty]
        private DateTime? _QSLRDATE;

        //QSL卡片发送日期（ADIF: QSLSDATE）
        [ObservableProperty]
        private DateTime? _QSLSDATE;

        //QSL卡片接收地址（项目自定义，保留）
        [ObservableProperty]
        private string _QslAddress = string.Empty;

        //QSL邮政编码（项目自定义，保留）
        [ObservableProperty]
        private string _PostalCode = string.Empty;

        //QSL接收人姓名（项目自定义，保留）
        [ObservableProperty]
        private string _ReceiverName = string.Empty;

        //QSL接收人联系方式（项目自定义，保留）
        [ObservableProperty]
        private string _ReceiverContact = string.Empty;

        //天气（项目自定义，保留）
        [ObservableProperty]
        private string _Weather = string.Empty;

        //温度（项目自定义，保留）
        [ObservableProperty]
        private string _Temperature = string.Empty;

        #endregion

        #region 被动属性

        //日期时间显示属性绑定
        [ObservableProperty]
        private string _UtcDate = "0000/00/00";

        [ObservableProperty]
        private string _LocalDate = "0000/00/00";

        [ObservableProperty]
        private string _UtcTimeZone = "UTC";

        [ObservableProperty]
        private string _LocalTimeZone = "UTC+NaN";

        [ObservableProperty]
        private string _UtcTime = "00:00:00";

        [ObservableProperty]
        private string _LocalTime = "00:00:00";

        //下拉菜单绑定
        [ObservableProperty]
        private PropagationItem? _SelectPROPItem;

        [ObservableProperty]
        private CommMode? _SelectCommMode;

        //消息提示框相关属性
        [ObservableProperty]
        private string _MessageShowStr = string.Empty;

        [ObservableProperty]
        private Visibility _MessShowISVisibility = Visibility.Hidden;

        //显示时区名称
        [ObservableProperty]
        private string _ZoneName = string.Empty;

        //下拉菜单绑定的集合
        public ObservableCollection<PropagationItem> PropagationModeList { get; }

        public ObservableCollection<CommMode> CommModeList { get; }

        //日志展示绑定的集合
        public ObservableCollection<OneLog> QsoLogCollection { get; }

        //所有日志记录的集合
        public List<QsoLog> AllQsoLogList { get; set; } = new List<QsoLog>();


        //获取选中下拉菜单的Code值

        // 【只读计算属性，自动获取Code，无需手动同步】
        public string? SelectedPropCode => _SelectPROPItem?.Code;
        public string? SelectedCommCode => _SelectCommMode?.Code;

        // 【只读计算属性，自动获取Name，无需手动同步】
        public string? SelectedPropName => _SelectPROPItem?.Name;
        public string? SelectedCommName => _SelectCommMode?.Name;

        // ========== 定时器 ==========
        public readonly DispatcherTimer _timer;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainLogWindow_VM()
        {
            PropagationModeList = StaticValues.Set_PropagationModeList;
            CommModeList = StaticValues.Set_CommModeList;
            QsoLogCollection = new ObservableCollection<OneLog>();

            _SelectPROPItem = PropagationModeList[0]; // 默认选择第一个选项

            _SelectCommMode = CommModeList[0]; // 默认选择第一个选项

            //初始化和格式化一些参数的值和格式
            var a = GetAllDBDataAsync<QsoLog>();
            AllQsoLogList = a.Result.Data.ToList();
            ShowLogInView();

            CALL = CALL.ToUpper();
            GRIDSQUARE = GRIDSQUARE.ToUpper();


            //打开计时器刷新，每秒刷新
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            _timer.Start();

            // 立即刷新一次，不用等1秒
            RefreshTime();

        }

        #region 定时触发相关方法

        /// <summary>
        /// 每秒触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object? sender, EventArgs e) => RefreshTime();



        /// <summary>
        /// 每秒刷新时所执行的事件
        /// </summary>
        private void RefreshTime()
        {


            UtcDate = DateTime.UtcNow.ToString("yyyy/MM/dd");
            UtcTimeZone = "UTC";
            UtcTime = DateTime.UtcNow.ToString("HH:mm:ss");

            LocalDate = DateTime.Now.ToString("yyyy/MM/dd");
            LocalTimeZone = "UTC" + DateTimeOffset.Now.ToString("zzz");
            LocalTime = DateTime.Now.ToString("HH:mm:ss");

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



        #region 绑定触发事件

        /// <summary>
        /// 添加日志记录按钮点击事件
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
                    ShowMessInMessStr(Result.Result.WarningMessage ?? "添加日志记录失败！");

                }
                else
                {
                    ShowMessInMessStr("日志记录添加成功！");
                    ShowLogInView();
                }
            }

        }



        /// <summary>
        /// 转换全局时间显示的时区
        /// </summary>
        [RelayCommand]
        private void ConvertTimeZone()
        {
            if (IsUseUtcTime == false)
            {
                LogUseTime = UtcNow;
                IsUseUtcTime = !IsUseUtcTime;
                StartTimeDT = LogUseTime.DateTime;
                EndTimeDT = LogUseTime.DateTime;
            }
            else
            {
                LogUseTime = LocalNow;
                IsUseUtcTime = !IsUseUtcTime;
                StartTimeDT = LogUseTime.DateTime;
                EndTimeDT = LogUseTime.DateTime;
            }
        }

        /// <summary>
        /// 点击开始时间标签 → 填入当前日志时间（后台拆分为 QSO_DATE + TIME_ON）
        /// </summary>
        [RelayCommand]
        private void StartTimeTextClick_TimeInPutStartTime() => StartTimeDT = LogUseTime.DateTime;

        /// <summary>
        /// 点击结束时间标签 → 填入当前日志时间（后台拆分为 QSO_DATE_OFF + TIME_OFF）
        /// </summary>
        [RelayCommand]
        private void EndTimeTextClick_TimeInPutEndTime() => EndTimeDT = LogUseTime.DateTime;

        #endregion



        #region 辅助方法

        /// <summary>
        /// 将 QsoLog 对象转换为 OneLog 对象，并添加到 QsoLogCollection 中
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
        /// 显示消息提示，并在2秒后隐藏
        /// </summary>
        /// <param name="Value">消息体</param>
        /// <returns></returns>
        private async Task ShowMessInMessStr(string Value)
        {
            MessageShowStr = Value;
            MessShowISVisibility = Visibility.Visible;
            await Task.Delay(2000);
            MessShowISVisibility = Visibility.Hidden;
            MessageShowStr = string.Empty;
        }

        /// <summary>
        /// 检查是否可以添加日志记录
        /// </summary>
        /// <returns></returns>
        private bool CanAddLog()
        {
            (bool isValid, string errorMessage) = QsoLogValidator.ValidateLogInput(CALL, FREQ);

            // 检查必填字段是否为空（CALL/FREQ/MODE；时间由 DateTimePicker 保证有值）
            if (string.IsNullOrWhiteSpace(CALL) || string.IsNullOrWhiteSpace(FREQ)
                || string.IsNullOrWhiteSpace(SelectedCommCode))
            {
                ShowMessInMessStr("请填写所有必填字段！");
                return false;
            }
            // 检查频率是否为有效的数字
            if (!double.TryParse(FREQ, out double frequency))
            {
                ShowMessInMessStr("频率必须是有效的数字！");
                return false;
            }
            // 检查开始时间是否早于结束时间（复合 DateTime 直接比较）
            if (StartTimeDT >= EndTimeDT)
            {
                ShowMessInMessStr("开始时间必须早于结束时间！");
                return false;
            }
            if (!isValid)
            {
                ShowMessInMessStr(errorMessage);
                return false;
            }
            return true;
        }

        #endregion






    }
}
