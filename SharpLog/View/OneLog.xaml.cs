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

        #region 通联模式、传播方式（命名已统一为 ADIF 规范）
        public string MODE
        {
            get => (string)GetValue(MODEProperty);
            set => SetValue(MODEProperty, value);
        }
        public static readonly DependencyProperty MODEProperty =
            DependencyProperty.Register(nameof(MODE), typeof(string), typeof(OneLog));

        public string PropModeText
        {
            get => (string)GetValue(PropModeTextProperty);
            set => SetValue(PropModeTextProperty, value);
        }
        public static readonly DependencyProperty PropModeTextProperty =
            DependencyProperty.Register(nameof(PropModeText), typeof(string), typeof(OneLog));
        #endregion

        #region 呼号、信号报告（命名已统一为 ADIF 规范）
        public string CALL
        {
            get => (string)GetValue(CALLProperty);
            set => SetValue(CALLProperty, value);
        }
        public static readonly DependencyProperty CALLProperty =
            DependencyProperty.Register(nameof(CALL), typeof(string), typeof(OneLog));

        public string RST_SENT
        {
            get => (string)GetValue(RST_SENTProperty);
            set => SetValue(RST_SENTProperty, value);
        }
        public static readonly DependencyProperty RST_SENTProperty =
            DependencyProperty.Register(nameof(RST_SENT), typeof(string), typeof(OneLog));

        public string RST_RCVD
        {
            get => (string)GetValue(RST_RCVDProperty);
            set => SetValue(RST_RCVDProperty, value);
        }
        public static readonly DependencyProperty RST_RCVDProperty =
            DependencyProperty.Register(nameof(RST_RCVD), typeof(string), typeof(OneLog));
        #endregion

        #region 频率、网格定位（命名已统一为 ADIF 规范）
        public string DisplayFrequency
        {
            get => (string)GetValue(DisplayFrequencyProperty);
            set => SetValue(DisplayFrequencyProperty, value);
        }
        public static readonly DependencyProperty DisplayFrequencyProperty =
            DependencyProperty.Register(nameof(DisplayFrequency), typeof(string), typeof(OneLog));

        public string GRIDSQUARE
        {
            get => (string)GetValue(GRIDSQUAREProperty);
            set => SetValue(GRIDSQUAREProperty, value);
        }
        public static readonly DependencyProperty GRIDSQUAREProperty =
            DependencyProperty.Register(nameof(GRIDSQUARE), typeof(string), typeof(OneLog));
        #endregion

        #region QTH、电台、天线、操作员
        public string QTH
        {
            get => (string)GetValue(QTHProperty);
            set => SetValue(QTHProperty, value);
        }
        public static readonly DependencyProperty QTHProperty =
            DependencyProperty.Register(nameof(QTH), typeof(string), typeof(OneLog));

        public string RIG
        {
            get => (string)GetValue(RIGProperty);
            set => SetValue(RIGProperty, value);
        }
        public static readonly DependencyProperty RIGProperty =
            DependencyProperty.Register(nameof(RIG), typeof(string), typeof(OneLog));

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
