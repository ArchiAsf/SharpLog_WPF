using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SharpLog.Model
{
    public static class StaticValues
    {
        public static readonly ObservableCollection<PropagationItem> Set_PropagationModeList = new ObservableCollection<PropagationItem>()
            {
                new PropagationItem{ Name = " ", Code = "Simplex‌" },
                new PropagationItem { Name = "F2 电离层", Code = "F2" },
                new PropagationItem { Name = "ES 偶发E", Code = "ES" },
                new PropagationItem { Name = "MS 流星散射", Code = "MS" },
                new PropagationItem { Name = "EME 月面反射", Code = "EME" },
                new PropagationItem { Name = "SAT 卫星", Code = "SAT" },
                new PropagationItem { Name = "中继 Repeater", Code = "REPEATER" }
            };
        public static readonly ObservableCollection<CommMode> Set_CommModeList=new ObservableCollection<CommMode>()
        {
            new CommMode{ Name = "FM", Code = "FM" },
            new CommMode{ Name = "AM", Code = "AM" },
            new CommMode{ Name = "CW", Code = "CW" },
            new CommMode{ Name = "SSB", Code = "SSB" },
            new CommMode{ Name = "FT8", Code = "FT8" },
            new CommMode{ Name = "FT4", Code = "FT4" },
            new CommMode{ Name = "SSTV", Code = "SSTV" },
            new CommMode{ Name = "DMR", Code = "DMR" },
            new CommMode{ Name = "C4FM", Code = "C4FM" },
            new CommMode{ Name = "D-STAR", Code = "D-STAR" },
            new CommMode{ Name = "NXDN", Code = "NXDN" },
            new CommMode{ Name = "Packet", Code = "Packet" },
            new CommMode{ Name = "MSK144", Code = "MSK144" }

        };
    }
}
