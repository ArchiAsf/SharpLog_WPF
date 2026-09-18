using Microsoft.EntityFrameworkCore;
using SharpLog.Infrastructure;
using SharpLog.Model;
using SharpLog.ToolsBox;
using System;
using System.Threading.Tasks;

namespace SharpLog.DB
{
    /// <summary>
    /// 软件设置的数据访问层（静态，无状态）。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【为什么单独建一个类，而不复用 SharpLogDbTools】
    ///   SharpLogDbTools 提供的是通用 CRUD，面向"多条记录"的表；
    ///   而设置表有两条特殊约定，必须在数据访问层屏蔽掉：
    ///     ① 永远只有一行 —— 首次保存时插入，之后都是覆盖更新；
    ///     ② "读不到"不算错误 —— 用户还没保存过设置是正常情况，
    ///        应返回 null 让上层显示空界面，而不是弹错误提示。
    ///
    ///   把这些规则封装在这里，ViewModel 只需调用 LoadAsync / SaveAsync 两个方法，
    ///   完全不必关心"这次到底该 Insert 还是 Update"。
    /// ════════════════════════════════════════════════════════════════════════
    /// </summary>
    public static class AppSettingsDbTools
    {
        /// <summary>
        /// 读取已保存的软件设置。
        ///
        /// 说明：数据库中还没有任何设置记录时返回 (null, null)——
        /// 这是【正常情况】而非错误，调用方据此显示空界面即可。
        /// </summary>
        /// <returns>(设置对象, 错误信息)；从未保存过时设置对象为 null 且无错误信息</returns>
        public static async Task<(AppSettings? Settings, string? WarningMessage)> LoadAsync()
        {
            try
            {
                using var db = new SharpLogDbContext();

                // 本表只有一行，取第一行即可；从未保存过时结果为 null
                // ConfigureAwait(false)：不要求回到调用方的同步上下文，
                // 这样 ViewModel 在 UI 线程用 .Result 同步等待时不会死锁
                var settings = await db.AppSettingsTable.FirstOrDefaultAsync().ConfigureAwait(false);

                Logger.Info(settings == null ? "未找到已保存的设置，使用空设置" : "已加载软件设置", "AppSettings");
                return (settings, null);
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                string msg = ex.SqliteErrorCode switch
                {
                    1 => "读取设置失败：数据库中不存在设置表，请确认数据库迁移已执行！",
                    5 => "读取设置失败：数据库文件权限不足或无法打开！",
                    _ => $"读取设置失败：{ex.Message}"
                };
                Logger.Error($"读取设置异常：{msg}", ex, "AppSettings");
                return (null, msg);
            }
            catch (Exception ex)
            {
                string msg = $"读取设置失败：{ex.Message}";
                Logger.Error(msg, ex, "AppSettings");
                return (null, msg);
            }
        }

        /// <summary>
        /// 保存软件设置（首次调用插入新行，之后每次都是覆盖更新）。
        ///
        /// 【实现要点】
        /// 更新时不直接 db.Update(settings)，而是先把数据库里的现有行取出来，
        /// 再把传入对象的值按属性名拷进去。这样做有两个好处：
        ///   · 不会因为传入对象的 Id 与数据库不一致而插出第二行；
        ///   · 只更新我们关心的字段，UpdatedAt 等元数据由本方法统一维护。
        /// </summary>
        /// <param name="settings">要保存的设置（通常由 ViewModel 的当前输入组装而成）</param>
        /// <returns>(是否成功, 错误信息)</returns>
        public static async Task<(bool Success, string? WarningMessage)> SaveAsync(AppSettings settings)
        {
            if (settings == null)
            {
                return (false, "保存设置失败：设置对象为空！");
            }

            try
            {
                using var db = new SharpLogDbContext();

                // 取出数据库中已有的那一行（没有则为 null）
                var existing = await db.AppSettingsTable.FirstOrDefaultAsync().ConfigureAwait(false);

                if (existing == null)
                {
                    // ---------- 首次保存：插入新行 ----------
                    settings.Id = 0;                       // 主键交给数据库自增，避免传入手工设置的 Id
                    settings.UpdatedAt = DateTime.UtcNow;
                    db.AppSettingsTable.Add(settings);
                    Logger.Info("设置已保存（首次写入数据库）", "AppSettings");
                }
                else
                {
                    // ---------- 已有数据：覆盖更新 ----------
                    // 按属性名把界面上的值拷到数据库实体上；
                    // 排除 Id 与 UpdatedAt —— 前者不能改，后者由本方法统一赋值
                    PropertyCopyTools.CopySameNameProperties(
                        settings, existing,
                        nameof(AppSettings.Id),
                        nameof(AppSettings.UpdatedAt));

                    existing.UpdatedAt = DateTime.UtcNow;
                    Logger.Info("设置已保存（覆盖更新）", "AppSettings");
                }

                await db.SaveChangesAsync().ConfigureAwait(false);

                // 保存成功后同步刷新全局缓存，保证其他模块立刻能读到新设置。
                // 更新分支里 existing 是数据库中的实体（已写入新值）；
                // 新增分支里用传入的 settings。
                AppSettingsCache.Update(existing ?? settings);

                return (true, null);
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                string msg = ex.SqliteErrorCode switch
                {
                    1 => "保存设置失败：数据库中不存在设置表，请确认数据库迁移已执行！",
                    5 => "保存设置失败：数据库文件被占用或权限不足，无法写入！",
                    19 => "保存设置失败：数据违反唯一约束！",
                    _ => $"保存设置失败：{ex.Message}"
                };
                Logger.Error($"保存设置异常：{msg}", ex, "AppSettings");
                return (false, msg);
            }
            catch (DbUpdateException ex)
            {
                string realMsg = ex.InnerException?.Message ?? ex.Message;
                Logger.Error($"保存设置失败：{realMsg}", ex, "AppSettings");
                return (false, $"保存设置失败：{realMsg}");
            }
            catch (Exception ex)
            {
                string msg = $"保存设置失败：{ex.Message}";
                Logger.Error(msg, ex, "AppSettings");
                return (false, msg);
            }
        }
    }
}
