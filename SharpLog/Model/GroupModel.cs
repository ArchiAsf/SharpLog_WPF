using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;


namespace SharpLog.Model
{
    // 辅助实体（同时携带Id和显示文本）
    public class GroupModel
    {
        public int GroupID { get; set; }
        public string Color { get; set; }

        public GroupModel(int id, string color)
        {
            GroupID = id;
            Color = color;
        }
    }
}
