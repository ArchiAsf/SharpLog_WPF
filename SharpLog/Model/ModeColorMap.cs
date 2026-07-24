using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;


namespace SharpLog.Model
{
    public static class ModeColorMap
    {
        // 核心字典：键=通联模式名称，值=对应Color对象
        public static readonly Dictionary<string, Color> ModeColorMapDic = new Dictionary<string, Color>()
        {
        // FM：橙黄色(255,153,0)——VHF/UHF最常用的调频模式，橙黄活力醒目，贴合日常通联的高频使用场景
        { "FM", Color.FromArgb(255, 153, 0) },
        
        // AM：深棕色(102,51,0)——传统调幅模式，深棕自带复古质感，匹配AM模式的经典属性
        { "AM", Color.FromArgb(102, 51, 0) },
        
        // CW：明黄色(255,204,0)——莫尔斯码经典模式，明黄是国际通用的警示/醒目色，匹配CW信号易识别的特征
        { "CW", Color.FromArgb(255, 204, 0) },

        // SSB：浅蓝色(0,102,204)——HF频段主流语音模式，浅蓝专业冷静，贴合远距离通联的技术属性
        { "SSB", Color.FromArgb(0, 102, 204) },
        
        // FT8：猩红色(220,20,60)——数字弱信号核心模式，猩红高对比度醒目，红色系突出数字模式的重要性，且不刺眼
        { "FT8", Color.FromArgb(220, 20, 60) },
        
        // FT4：番茄红(255,99,71)——FT8衍生模式，番茄红比猩红浅一度，同红色系但易区分，保持数字模式的视觉关联
        { "FT4", Color.FromArgb(255, 99, 71) },
        
        // SSTV：粉紫色(255,51,153)——慢扫描电视模式，粉紫色彩丰富，贴合SSTV传图的视觉化特征
        { "SSTV", Color.FromArgb(255, 51, 153) },
        
        // DMR：深灰色(102,102,102)——数字集群模式，深灰工业感强，匹配DMR商用/业余通用的稳重属性
        { "DMR", Color.FromArgb(102, 102, 102) },
        
        // C4FM：浅绿色(0,204,153)——数字调频模式，浅绿清新柔和，与FM橙黄形成区分，贴合数字调频的低功耗特征
        { "C4FM", Color.FromArgb(0, 204, 153) },
        
        // D-STAR：深蓝色(0,51,153)——数字星状网模式，深蓝科技感强，匹配“STAR（星）”的太空/远距离通联意象
        { "D-STAR", Color.FromArgb(0, 51, 153) },
        
        // NXDN：紫色(153,102,204)——窄带数字模式，紫色稳重且独特，与其他数字模式颜色形成区分
        { "NXDN", Color.FromArgb(153, 102, 204) },
        
        // Packet：浅灰色(204,204,204)——分组数据模式，浅灰轻量中性，贴合Packet低速数据传输的低调特征
        { "Packet", Color.FromArgb(204, 204, 204) },
        
        // MSK144：青蓝色(0,255,255)——流星散射模式，青蓝高饱和度鲜艳，像流星划过的亮色轨迹，视觉冲击力强
        { "MSK144", Color.FromArgb(0, 255, 255) }
        };
    }
}
