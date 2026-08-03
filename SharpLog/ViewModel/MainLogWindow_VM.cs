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
        //普通输入框绑定
        [ObservableProperty]
        private DateTime _StartTimeDT = LogUseTime.DateTime;

        [ObservableProperty]
        private DateTime _EndTimeDT = LogUseTime.DateTime;

        [ObservableProperty]
        private string _CallSign = string.Empty;

        [ObservableProperty]
        private string _Rmks = string.Empty;

        [ObservableProperty]
        private string _Freq = string.Empty;

        [ObservableProperty]
        private string _Rst = string.Empty;

        [ObservableProperty]
        private string _Rrst = string.Empty;

        [ObservableProperty]
        private string _gridLocator = string.Empty;

        [ObservableProperty]
        private string _rxPower = string.Empty;

        [ObservableProperty]
        private string _txPower = string.Empty;

        [ObservableProperty]
        private string _qth = string.Empty;

        [ObservableProperty]
        private string _rig = string.Empty;

        [ObservableProperty]
        private string _ant = string.Empty;

        [ObservableProperty]
        private string _height = string.Empty;

        [ObservableProperty]
        private string _operator = string.Empty;

        [ObservableProperty]
        private string _DigID = string.Empty;

        [ObservableProperty]
        private int _GroupID = 0;

        [ObservableProperty]
        private int _StarID = 0;

        [ObservableProperty]
        private bool _IsQSL = false;

        [ObservableProperty]
        private string _QslReceiveTime = string.Empty;

        [ObservableProperty]
        private string _QslSendTime = string.Empty;

        [ObservableProperty]
        private string _QslAddress = string.Empty;

        [ObservableProperty]
        private string _PostalCode = string.Empty;

        [ObservableProperty]
        private string _ReceiverName = string.Empty;

        [ObservableProperty]
        private string _ReceiverContact = string.Empty;

        [ObservableProperty]
        private string _Weather = string.Empty;

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
                    StartTimeDT, EndTimeDT, ZoneName, CallSign, Freq, SelectedCommCode, Rst, Rrst,
                    GridLocator, RxPower, TxPower, Qth, Rig, Ant, Rmks, Height, Operator, SelectedPropCode, DigID));

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

        [RelayCommand]
        private void StartTimeTextClick_TimeInPutStartTime() => StartTimeDT = LogUseTime.DateTime;

        [RelayCommand]
        private void EndTimeTextClick_TimeInPutEndTime() => EndTimeDT = LogUseTime.DateTime;

        #endregion



        #region 辅助方法

        /// <summary>
        /// 将 DateTime 转换为 yyyy/MM/dd-HH:mm:ss 格式字符串
        /// </summary>
        /// <param name="dateTime">要转换的时间</param>
        /// <returns>格式化后的字符串，如 2026/05/24-19:11:30</returns>
        public static string DateTimeToLogString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd-HH:mm:ss");
        }

        /// <summary>
        /// 将 QsoLog 对象转换为 OneLog 对象，并添加到 QsoLogCollection 中
        /// </summary>
        private void ShowLogInView()
        {
            QsoLogCollection.Clear();
            AllQsoLogList = GetAllDBDataAsync<QsoLog>().Result.Data;
            AllQsoLogList.ForEach(l =>
            {
                QsoLogCollection.Add(ConvertQsoLogToOneLog(l));
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
            (bool isValid, string errorMessage) = QsoLogValidator.ValidateLogInput(CallSign, Freq);

            // 检查必填字段是否为空
            if (string.IsNullOrWhiteSpace(CallSign) || string.IsNullOrWhiteSpace(Freq) || string.IsNullOrWhiteSpace(StartTimeDT.ToString()) || string.IsNullOrWhiteSpace(EndTimeDT.ToString()) || string.IsNullOrWhiteSpace(SelectedCommCode))
            {
                ShowMessInMessStr("请填写所有必填字段！");
                return false;
            }
            // 检查频率是否为有效的数字
            if (!double.TryParse(Freq, out double frequency))
            {
                ShowMessInMessStr("频率必须是有效的数字！");
                return false;
            }
            // 检查开始时间是否早于结束时间
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
