using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using SharpLog.DB;
using SharpLog.Model;
using SharpLog.ToolsBox;
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
using static SharpLog.ToolsBox.MessageTools;

// ══════════════════════════════════════════════════════════════════════════
//  MainLogWindow_VM 的「辅助方法」部分（partial）
//  ──────────────────────────────────────────────────────────────────────────
//  本文件装的是【不访问数据库、不改变界面状态】的小工具方法：
//    · 输入校验、格式化、界面提示等
//
//  与之相对：
//    · 绑定属性、字段、构造函数 → MainLogWindow_VM.cs
//    · 会改状态 / 访问数据库的方法 → MainLogWindow_VM.Methods.cs
// ══════════════════════════════════════════════════════════════════════════

namespace SharpLog.ViewModel
{
    public partial class MainLogWindow_VM
    {
        #region 辅助方法

        /// <summary>
        /// 刷新右侧日志卡片列表（QsoLogCollection）。
        ///
        /// 【取数范围】只取数据库中【最近的 MaxDisplayLogCount 条】。
        ///   排序与截断都在数据库层面完成（ORDER BY Id DESC LIMIT 200），
        ///   内存里只有这 200 条，与表的总行数无关。
        ///   查询失败时 Data 为空列表，界面显示空白，错误详情见 Logs 日志
        ///   （与改动前的静默行为保持一致，避免首次运行尚无数据表时误报）。
        ///
        /// 【排序】主键 Id 自增，Id 越大即越新，故查询结果本就是"最新在前"，
        ///   直接 Add 即可。原实现用 Insert(0, ...) 逐条插到队首，
        ///   既是 O(n²) 的写法，又隐含依赖"查询结果恰好升序"这个前提。
        /// </summary>
        private void ShowLogInView()
        {
            var (data, _) = GetLatestDBDataAsync<QsoLog>(MaxDisplayLogCount).Result;

            AllQsoLogList = data;

            QsoLogCollection.Clear();
            foreach (QsoLog log in data)
            {
                QsoLogCollection.Add(ConvertQsoLogToOneLog(log));
            }
        }

        #endregion
        #region 辅助方法（续：输入校验、界面提示等）



        /// <summary>
        /// 添加日志前的输入校验，全部通过才允许写入数据库：
        /// 1. 必填项：呼号、频率、模式（时间由 DateTimePicker 保证有值）；
        /// 2. 频率必须为有效数字，且在合法业余波段内（QsoLogValidator）；
        /// 3. 开始时间必须早于结束时间。
        /// </summary>
        /// <returns>true = 校验通过，可添加</returns>
        private bool CanAddLog()
        {
            // 呼号格式与频率波段校验（静态工具类，见 QsoLogValidator）
            (bool isValid, string errorMessage) = QsoLogValidator.ValidateLogInput(CALL, FREQ);

            // 检查必填字段是否为空（CALL / FREQ / MODE）
            if (string.IsNullOrWhiteSpace(CALL) || string.IsNullOrWhiteSpace(FREQ)
                || string.IsNullOrWhiteSpace(SelectedCommCode))
            {
                ShowErrorMess(MessagePage.LogEdit, "请填写所有必填字段！");
                return false;
            }
            // 检查频率是否为有效的数字
            if (!double.TryParse(FREQ, out double frequency))
            {
                ShowErrorMess(MessagePage.LogEdit, "频率必须是有效的数字！");
                return false;
            }
            // 检查开始时间是否早于结束时间（复合 DateTime 直接比较）
            if (StartTimeDT >= EndTimeDT)
            {
                ShowErrorMess(MessagePage.LogEdit, "开始时间必须早于结束时间！");
                return false;
            }
            // 波段合法性校验结果
            if (!isValid)
            {
                ShowErrorMess(MessagePage.LogEdit, errorMessage);
                return false;
            }
            return true;
        }

        #endregion
    }
}
