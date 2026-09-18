using SharpLog.DB;
using SharpLog.Infrastructure;
using SharpLog.Model;
using System;
using System.Threading.Tasks;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// 软件设置的全局内存缓存（静态）。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【要使用设置数据时，请读这里，不要每次去查数据库】
    ///
    /// 为什么不做成"用一次查一次库"：
    ///   ① 设置数据的特点是【读多写少】——程序运行期间会反复用到（新建通联要写
    ///      本站呼号、导出 ADIF 要写文件头…），而修改只有用户点"保存设置"那一下；
    ///   ② 每次查询都要新建 DbContext、走一遍 SQLite 的 IO，
    ///      在 UI 线程同步等待还容易造成界面卡顿；
    ///   ③ 查库拿到的只是"某一瞬间"的快照，反而容易出现同一个流程里
    ///      前后两次读到的值不一致。
    ///
    /// 因此采用「启动时加载一次 + 保存时同步更新」的策略：
    ///   程序启动        → App.xaml.cs 调用 RefreshAsync() 把数据库的值读进 Current
    ///   点"保存设置"    → AppSettingsDbTools.SaveAsync() 内部调用 Update() 刷新缓存
    ///   任何地方要用    → 直接读 Current 或下面的便捷属性
    ///
    /// 这样内存中的值始终与数据库保持一致，且读取零开销。
    /// ════════════════════════════════════════════════════════════════════════
    ///
    /// 【注意与 MainLogWindow_VM 上那 16 个属性的区别】
    ///   · 本类（Current）      → 【已保存】的设置，全局共享，只读
    ///   · VM 上的 16 个属性    → 设置页【正在编辑中】的内容，可能还没保存
    ///   用户在界面上改了但没点保存时，两者是不一样的——这是有意为之。
    /// </summary>
    public static class AppSettingsCache
    {
        /// <summary>
        /// 当前生效的设置。程序启动时加载，保存设置后自动刷新。
        /// 若用户从未保存过设置，本属性为 null（用 HasSettings 判断）。
        /// </summary>
        public static AppSettings? Current { get; private set; }

        /// <summary>是否已经载入过设置（数据库中是否存在已保存的设置）。</summary>
        public static bool HasSettings => Current != null;

        /// <summary>本站呼号（ADIF: STATION_CALLSIGN），未设置时返回空串。</summary>
        public static string StationCallsign => Current?.STATION_CALLSIGN ?? string.Empty;

        /// <summary>日志操作员呼号（ADIF: OPERATOR），未设置时返回空串。</summary>
        public static string OperatorCall => Current?.OPERATOR ?? string.Empty;

        /// <summary>
        /// 通联记录里应写入的本站操作员呼号（对应 QsoLog.RadioOperator 字段）。
        ///
        /// 取值优先级：STATION_CALLSIGN → OPERATOR → 空串。
        /// （该列在数据库中不允许为 NULL，因此最差也要返回空串而非 null）
        /// </summary>
        public static string RadioOperatorForQso
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Current?.STATION_CALLSIGN))
                    return Current!.STATION_CALLSIGN!;

                if (!string.IsNullOrWhiteSpace(Current?.OPERATOR))
                    return Current!.OPERATOR!;

                return string.Empty;
            }
        }

        /// <summary>
        /// 从数据库重新读取设置并刷新缓存。由 App.xaml.cs 在程序启动时调用一次。
        /// 读不到设置（用户尚未保存过）【不算错误】，此时 Current 为 null。
        /// </summary>
        /// <returns>(是否成功, 错误信息)</returns>
        public static async Task<(bool Success, string? WarningMessage)> RefreshAsync()
        {
            try
            {
                // ConfigureAwait(false)：允许调用方在 UI 线程用 GetAwaiter().GetResult() 同步等待
                var (settings, error) = await AppSettingsDbTools.LoadAsync().ConfigureAwait(false);

                if (error != null)
                {
                    return (false, error);
                }

                Current = settings;   // 可能为 null，表示"还没保存过设置"
                return (true, null);
            }
            catch (Exception ex)
            {
                Logger.Error($"刷新设置缓存失败：{ex.Message}", ex, "AppSettingsCache");
                return (false, $"刷新设置缓存失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 用最新的设置对象刷新缓存。由 AppSettingsDbTools.SaveAsync() 在保存成功后调用，
        /// 以保证"保存完成后，其他模块立刻就能读到新值"。
        /// </summary>
        /// <param name="settings">刚保存进数据库的设置对象</param>
        public static void Update(AppSettings? settings)
        {
            Current = settings;
        }
    }
}
