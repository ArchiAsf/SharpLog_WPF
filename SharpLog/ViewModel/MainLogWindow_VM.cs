using CommunityToolkit.Mvvm.ComponentModel;
using SharpLog.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SharpLog.ViewModel
{
    [ObservableObject]
    public partial class MainLogWindow_VM
    {


        [ObservableProperty]
        private PropagationItem? _SelectPROPItem;

        [ObservableProperty]
        private CommMode? _SelectCommMode;



        public ObservableCollection<PropagationItem> PropagationModeList { get; }

        public ObservableCollection<CommMode> CommModeList { get; }



        // 【只读计算属性，自动获取Code，无需手动同步】
        public string? SelectedPropCode => SelectPROPItem?.Code;
        public string? SelectedCommCode => SelectCommMode?.Code;




        public MainLogWindow_VM()
        {
            PropagationModeList = StaticValues.Set_PropagationModeList;
            CommModeList = StaticValues.Set_CommModeList;

            SelectPROPItem = PropagationModeList[0]; // 默认选择第一个选项

            SelectCommMode = CommModeList[0]; // 默认选择第一个选项


        }













    }
}
