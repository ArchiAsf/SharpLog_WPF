using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// 对象属性拷贝工具类（静态，无状态，基于反射）。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【用途】
    ///   在「数据库实体 ↔ ViewModel」之间按【属性名】批量互拷。
    ///   例如把 AppSettings 实体上的 16 个设置字段一次性赋给
    ///   MainLogWindow_VM 上同名的 16 个属性，省去手写 16 行赋值。
    ///
    /// 【为什么用反射而不是手写赋值】
    ///   设置项会不断增加。手写映射的话，每加一个字段就要改三处
    ///   （加载一次、保存一次、重置一次），很容易漏改其中一处；
    ///   按名自动拷贝后，只需在实体与 ViewModel 里各加一个同名属性即可。
    ///
    /// 【限制】
    ///   · 只拷贝【名字相同、类型相同、双方都可读写】的属性，其余自动跳过；
    ///   · 不处理"名字不同但语义相同"的字段（那种情况需要手工映射）；
    ///   · 本方法使用反射，性能低于直接赋值，因此仅用于
    ///     程序启动加载、点击保存这类低频场景，不要放进循环里调用。
    /// ════════════════════════════════════════════════════════════════════════
    /// </summary>
    public static class PropertyCopyTools
    {
        /// <summary>
        /// 把 source 中与 target 同名、同类型且可读写的属性值，逐个拷贝到 target。
        ///
        /// 典型用法：
        ///   · 实体 → ViewModel（加载设置到界面）
        ///       PropertyCopyTools.CopySameNameProperties(settingsEntity, this);
        ///   · ViewModel → 实体（保存界面到数据库）
        ///       PropertyCopyTools.CopySameNameProperties(this, settingsEntity);
        /// </summary>
        /// <param name="source">数据来源对象</param>
        /// <param name="target">赋值目标对象（本对象上同名属性会被覆盖）</param>
        /// <param name="excludeNames">需要跳过的属性名（如 Id、更新时间的元数据字段）</param>
        public static void CopySameNameProperties(object source, object target, params string[] excludeNames)
        {
            // 容错：任一方为空则什么都不做，避免抛 NullReferenceException
            if (source == null || target == null) return;

            // 用 HashSet 而不是数组做排除判断，属性多时查找更快
            var exclude = new HashSet<string>(excludeNames ?? Array.Empty<string>());

            // 先收集目标对象上"可写入"的属性，建立 名字 → 属性 的字典
            var targetProps = target.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && !exclude.Contains(p.Name))
                .GroupBy(p => p.Name)                    // 防止隐藏属性导致的重复键
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var srcProp in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // 跳过只写属性、被排除的属性
                if (!srcProp.CanRead || exclude.Contains(srcProp.Name)) continue;

                // 目标对象上没有同名属性 → 跳过
                if (!targetProps.TryGetValue(srcProp.Name, out var dstProp)) continue;

                // 类型不一致则跳过（避免把 string 硬塞进 int 之类的错误）
                if (dstProp.PropertyType != srcProp.PropertyType) continue;

                dstProp.SetValue(target, srcProp.GetValue(source));
            }
        }
    }
}
