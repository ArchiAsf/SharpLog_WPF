using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SharpLog
{
    /// <summary>
    /// OneLog.xaml 的交互逻辑
    /// </summary>
    public partial class OneLog : UserControl
    {
        public OneLog()
        {
            InitializeComponent();
        }
        #region 左侧侧边栏颜色
        public SolidColorBrush CardSideBarColor
        {
            get => (SolidColorBrush)GetValue(CardSideBarColorProperty);
            set => SetValue(CardSideBarColorProperty, value);
        }
        public static readonly DependencyProperty CardSideBarColorProperty =
            DependencyProperty.Register(nameof(CardSideBarColor), typeof(SolidColorBrush), typeof(OneLog));
        #endregion

        #region 时间相关
        public string StartTime
        {
            get => (string)GetValue(StartTimeProperty);
            set => SetValue(StartTimeProperty, value);
        }
        public static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.Register(nameof(StartTime), typeof(string), typeof(OneLog));

        public string EndTime
        {
            get => (string)GetValue(EndTimeProperty);
            set => SetValue(EndTimeProperty, value);
        }
        public static readonly DependencyProperty EndTimeProperty =
            DependencyProperty.Register(nameof(EndTime), typeof(string), typeof(OneLog));

        public string TimeZoneSuffix
        {
            get => (string)GetValue(TimeZoneSuffixProperty);
            set => SetValue(TimeZoneSuffixProperty, value);
        }
        public static readonly DependencyProperty TimeZoneSuffixProperty =
            DependencyProperty.Register(nameof(TimeZoneSuffix), typeof(string), typeof(OneLog));
        #endregion

        #region 通联模式、传播方式
        public string Mode
        {
            get => (string)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(string), typeof(OneLog));

        public string PropModeText
        {
            get => (string)GetValue(PropModeTextProperty);
            set => SetValue(PropModeTextProperty, value);
        }
        public static readonly DependencyProperty PropModeTextProperty =
            DependencyProperty.Register(nameof(PropModeText), typeof(string), typeof(OneLog));
        #endregion

        #region 呼号、信号报告
        public string CallSign
        {
            get => (string)GetValue(CallSignProperty);
            set => SetValue(CallSignProperty, value);
        }
        public static readonly DependencyProperty CallSignProperty =
            DependencyProperty.Register(nameof(CallSign), typeof(string), typeof(OneLog));

        public string RRST
        {
            get => (string)GetValue(RstReceivedProperty);
            set => SetValue(RstReceivedProperty, value);
        }
        public static readonly DependencyProperty RstReceivedProperty =
            DependencyProperty.Register(nameof(RRST), typeof(string), typeof(OneLog));

        public string RST
        {
            get => (string)GetValue(RstSentProperty);
            set => SetValue(RstSentProperty, value);
        }
        public static readonly DependencyProperty RstSentProperty =
            DependencyProperty.Register(nameof(RST), typeof(string), typeof(OneLog));
        #endregion

        #region 频率、网格定位
        public string DisplayFrequency
        {
            get => (string)GetValue(DisplayFrequencyProperty);
            set => SetValue(DisplayFrequencyProperty, value);
        }
        public static readonly DependencyProperty DisplayFrequencyProperty =
            DependencyProperty.Register(nameof(DisplayFrequency), typeof(string), typeof(OneLog));

        public string Locator
        {
            get => (string)GetValue(LocatorProperty);
            set => SetValue(LocatorProperty, value);
        }
        public static readonly DependencyProperty LocatorProperty =
            DependencyProperty.Register(nameof(Locator), typeof(string), typeof(OneLog));
        #endregion

        #region QTH、电台、天线、操作员
        public string QTH
        {
            get => (string)GetValue(QTHProperty);
            set => SetValue(QTHProperty, value);
        }
        public static readonly DependencyProperty QTHProperty =
            DependencyProperty.Register(nameof(QTH), typeof(string), typeof(OneLog));

        public string Rig
        {
            get => (string)GetValue(RigProperty);
            set => SetValue(RigProperty, value);
        }
        public static readonly DependencyProperty RigProperty =
            DependencyProperty.Register(nameof(Rig), typeof(string), typeof(OneLog));

        public string Antenna
        {
            get => (string)GetValue(AntennaProperty);
            set => SetValue(AntennaProperty, value);
        }
        public static readonly DependencyProperty AntennaProperty =
            DependencyProperty.Register(nameof(Antenna), typeof(string), typeof(OneLog));

        public string Operator
        {
            get => (string)GetValue(OperatorProperty);
            set => SetValue(OperatorProperty, value);
        }
        public static readonly DependencyProperty OperatorProperty =
            DependencyProperty.Register(nameof(Operator), typeof(string), typeof(OneLog));
        #endregion
    }
}
