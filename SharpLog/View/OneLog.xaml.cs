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
    /// 日志卡片控件（OneLog.xaml 的交互逻辑）。
    ///
    /// 用途：右侧日志列表中的单条 QSO 卡片显示。
    /// 设计要点：
    /// - 全部显示数据通过【依赖属性】（DependencyProperty）暴露，
    ///   由 ItemsControl 数据绑定注入，本控件自身无业务逻辑；
    /// - 属性命名已统一为 ADIF 3.1.7 字段名（CALL、RST_SENT、RST_RCVD、
    ///   GRIDSQUARE、MODE、RIG 等）；
    /// - 卡片左侧竖条颜色（CardSideBarColor）= 通联模式配色。
    /// </summary>
    public partial class OneLog : UserControl
    {
        public OneLog()
        {
            InitializeComponent();
        }

        #region 左侧侧边栏颜色

        /// <summary>
        /// 卡片左侧竖条颜色（按通联模式配色，见 StaticValues.ModeColorMap）。
        /// 由 ModeConvertTools.ResolveModeColor 取色：模式不在配色表内时回落到中性灰，
        /// 不使用字典索引器直接取值，避免遇到未知模式（历史数据、外部导入）时抛异常。
        /// </summary>
        public SolidColorBrush CardSideBarColor
        {
            get => (SolidColorBrush)GetValue(CardSideBarColorProperty);
            set => SetValue(CardSideBarColorProperty, value);
        }
        public static readonly DependencyProperty CardSideBarColorProperty =
            DependencyProperty.Register(nameof(CardSideBarColor), typeof(SolidColorBrush), typeof(OneLog));

        #endregion

        #region 时间相关

        /// <summary>开始时间显示字符串（"yyyy/MM/dd-HH:mm:ss"）。</summary>
        public string StartTime
        {
            get => (string)GetValue(StartTimeProperty);
            set => SetValue(StartTimeProperty, value);
        }
        public static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.Register(nameof(StartTime), typeof(string), typeof(OneLog));

        /// <summary>结束时间显示字符串（"yyyy/MM/dd-HH:mm:ss"）。</summary>
        public string EndTime
        {
            get => (string)GetValue(EndTimeProperty);
            set => SetValue(EndTimeProperty, value);
        }
        public static readonly DependencyProperty EndTimeProperty =
            DependencyProperty.Register(nameof(EndTime), typeof(string), typeof(OneLog));

        /// <summary>时区后缀（如 UTC、UTC+08:00），小字号显示在时间右侧。</summary>
        public string TimeZoneSuffix
        {
            get => (string)GetValue(TimeZoneSuffixProperty);
            set => SetValue(TimeZoneSuffixProperty, value);
        }
        public static readonly DependencyProperty TimeZoneSuffixProperty =
            DependencyProperty.Register(nameof(TimeZoneSuffix), typeof(string), typeof(OneLog));

        #endregion

        #region 通联模式、传播方式

        /// <summary>
        /// 卡片上显示的通联模式文字（大字号显示，如 "SSB"、"FT4"、"D-STAR"）。
        ///
        /// ⚠️ 注意：本属性承载的是【界面显示名】，不是数据库中的 ADIF 主模式。
        /// 数据库里可能存 MODE="DIGITALVOICE" + SUBMODE="DSTAR"，
        /// 经 ModeConvertTools.ResolveDisplayName 反查后，这里显示为 "D-STAR"。
        /// </summary>
        public string MODE
        {
            get => (string)GetValue(MODEProperty);
            set => SetValue(MODEProperty, value);
        }
        public static readonly DependencyProperty MODEProperty =
            DependencyProperty.Register(nameof(MODE), typeof(string), typeof(OneLog));

        /// <summary>传播模式文本（ADIF: PROP_MODE）。
        /// ⚠️ 已知问题：ModeConvertTools 转换时未赋值，卡片上暂不显示。</summary>
        public string PropModeText
        {
            get => (string)GetValue(PropModeTextProperty);
            set => SetValue(PropModeTextProperty, value);
        }
        public static readonly DependencyProperty PropModeTextProperty =
            DependencyProperty.Register(nameof(PropModeText), typeof(string), typeof(OneLog));

        #endregion

        #region 呼号、信号报告

        /// <summary>对方呼号（ADIF: CALL），卡片大字号显示。</summary>
        public string CALL
        {
            get => (string)GetValue(CALLProperty);
            set => SetValue(CALLProperty, value);
        }
        public static readonly DependencyProperty CALLProperty =
            DependencyProperty.Register(nameof(CALL), typeof(string), typeof(OneLog));

        /// <summary>发出的信号报告（ADIF: RST_SENT），卡片大字号显示。</summary>
        public string RST_SENT
        {
            get => (string)GetValue(RST_SENTProperty);
            set => SetValue(RST_SENTProperty, value);
        }
        public static readonly DependencyProperty RST_SENTProperty =
            DependencyProperty.Register(nameof(RST_SENT), typeof(string), typeof(OneLog));

        /// <summary>收到的信号报告（ADIF: RST_RCVD），卡片大字号显示。</summary>
        public string RST_RCVD
        {
            get => (string)GetValue(RST_RCVDProperty);
            set => SetValue(RST_RCVDProperty, value);
        }
        public static readonly DependencyProperty RST_RCVDProperty =
            DependencyProperty.Register(nameof(RST_RCVD), typeof(string), typeof(OneLog));

        #endregion

        #region 频率、网格定位

        /// <summary>频率显示字符串（如 "7050khz"）。</summary>
        public string DisplayFrequency
        {
            get => (string)GetValue(DisplayFrequencyProperty);
            set => SetValue(DisplayFrequencyProperty, value);
        }
        public static readonly DependencyProperty DisplayFrequencyProperty =
            DependencyProperty.Register(nameof(DisplayFrequency), typeof(string), typeof(OneLog));

        /// <summary>网格定位（ADIF: GRIDSQUARE，如 FN31pr）。</summary>
        public string GRIDSQUARE
        {
            get => (string)GetValue(GRIDSQUAREProperty);
            set => SetValue(GRIDSQUAREProperty, value);
        }
        public static readonly DependencyProperty GRIDSQUAREProperty =
            DependencyProperty.Register(nameof(GRIDSQUARE), typeof(string), typeof(OneLog));

        #endregion

        #region QTH、电台、天线、操作员

        /// <summary>对方城市（ADIF: QTH）。</summary>
        public string QTH
        {
            get => (string)GetValue(QTHProperty);
            set => SetValue(QTHProperty, value);
        }
        public static readonly DependencyProperty QTHProperty =
            DependencyProperty.Register(nameof(QTH), typeof(string), typeof(OneLog));

        /// <summary>电台型号（ADIF: RIG）。</summary>
        public string RIG
        {
            get => (string)GetValue(RIGProperty);
            set => SetValue(RIGProperty, value);
        }
        public static readonly DependencyProperty RIGProperty =
            DependencyProperty.Register(nameof(RIG), typeof(string), typeof(OneLog));

        /// <summary>天线类型（项目自定义字段）。</summary>
        public string Antenna
        {
            get => (string)GetValue(AntennaProperty);
            set => SetValue(AntennaProperty, value);
        }
        public static readonly DependencyProperty AntennaProperty =
            DependencyProperty.Register(nameof(Antenna), typeof(string), typeof(OneLog));

        /// <summary>操作员（项目自定义字段）。</summary>
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
