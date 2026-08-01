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
using System.Windows.Controls;

namespace SharpLog.ViewModel
{
    [ObservableObject]
    public partial class MainLogWindow_VM
    {
        //普通输入框绑定

        [ObservableProperty]
        private DateTime _startTimeDT;

        private string _startTime = string.Empty;

        [ObservableProperty]
        private DateTime _endTimeDT;

        private string _endTime = string.Empty;

        [ObservableProperty]
        private string _callSign = string.Empty;

        [ObservableProperty]
        private string _rmks = string.Empty;

        [ObservableProperty]
        private string _freq = string.Empty;

        [ObservableProperty]
        private string _rst = string.Empty;

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
        private string _dmrId = string.Empty;


        //下拉菜单绑定

        [ObservableProperty]
        private PropagationItem? _SelectPROPItem;

        [ObservableProperty]
        private CommMode? _SelectCommMode;

        //下来菜单绑定的集合

        public ObservableCollection<PropagationItem> PropagationModeList { get; }

        public ObservableCollection<CommMode> CommModeList { get; }

        public ObservableCollection<OneLog> QsoLogCollection { get; }


        //获取选中下拉菜单的Code值

        // 【只读计算属性，自动获取Code，无需手动同步】
        public string? SelectedPropCode => _SelectPROPItem?.Code;
        public string? SelectedCommCode => _SelectCommMode?.Code;

        // 【只读计算属性，自动获取Name，无需手动同步】 
        public string? SelectedPropName => _SelectPROPItem?.Name;
        public string? SelectedCommName => _SelectCommMode?.Name;




        public MainLogWindow_VM()
        {
            PropagationModeList = StaticValues.Set_PropagationModeList;
            CommModeList = StaticValues.Set_CommModeList;
            QsoLogCollection = new ObservableCollection<OneLog>();

            _SelectPROPItem = PropagationModeList[0]; // 默认选择第一个选项

            _SelectCommMode = CommModeList[0]; // 默认选择第一个选项

            //初始化和格式化一些参数的值和格式

            _startTime= DateTimeToLogString(_startTimeDT);

        }

        /// <summary>
        /// 将 DateTime 转换为 yyyy/MM/dd-HH:mm:ss 格式字符串
        /// </summary>
        /// <param name="dateTime">要转换的时间</param>
        /// <returns>格式化后的字符串，如 2026/05/24-19:11:30</returns>
        public static string DateTimeToLogString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd-HH:mm:ss");
        }

        //一个将输入的20260524191130转化为2026/05/24-19:11:30的方法
        static string TimeChanger(string TimeString)
        {
            TimeString = TimeString.Trim();

            string year = TimeString.Substring(0, 4);
            string month = TimeString.Substring(4, 2);
            string day = TimeString.Substring(6, 2);
            string hour = TimeString.Substring(8, 2);
            string minute = TimeString.Substring(10, 2);
            string second = TimeString.Substring(12, 2);

            return $"{year}/{month}/{day}-{hour}:{minute}:{second}";
        }


        [RelayCommand]
        private void AddLogBtnClick()
        {
            var result = QsoLogValidator.ValidateLogInput(_startTime, _endTime, _callSign, SelectedCommName, _freq);
            if (!result.IsValid)
            {
                // 处理校验失败的情况
            }
            else
            {
                // 处理校验成功的情况   <
                OneLog onelog = new OneLog()
                {
                    CardSideBarColor = new System.Windows.Media.SolidColorBrush(StaticValues.ToWpfColor(StaticValues.ModeColorMap[SelectedCommName])),
                    StartTime = TimeChanger(_startTime),
                    EndTime = TimeChanger(_endTime),
                    TimeZoneSuffix = "UTC",
                    Mode = SelectedCommName,
                    PropModeText = SelectedPropName,
                    CallSign = _callSign,
                    RRST = _Rrst,
                    RST = _rst,
                    DisplayFrequency = _freq,
                    Locator = _gridLocator,
                    QTH = _qth,
                    Rig = _rig,
                    Antenna = _ant,
                    Operator = _operator
                };
                QsoLogCollection.Add(onelog);
            }
        }

        int i = 0;

        //测试用添加日志记录点击事件
        [RelayCommand]
        private void TestAddLogBtnClick()
        {
            OneLog onelog = new OneLog();
            
            onelog=StaticValues.ConvertToOneLog(StaticValues.TestQsoLogs[i]);

            QsoLogCollection.Add(onelog);
            i++;
        }













    }
}
