using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SharpLog.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static SharpLog.Infrastructure.Logger;

namespace SharpLog.DB
{
    static class SharpLogDbTools
    {
        // ==================================================
        //  新增
        // ==================================================

        /// <summary>
        /// 向数据库中添加一条数据（异步）
        /// </summary>
        /// <returns>(是否成功, 错误提示信息)</returns>
        public static async Task<(bool Success, string? WarningMessage)> AddDBDataAsync<T>(T data) where T : class
        {
            try
            {
                using var dbContext = new SharpLogDbContext();
                dbContext.Set<T>().Add(data);
                await dbContext.SaveChangesAsync();

                Logger.Info($"成功添加{typeof(T).Name}类型的数据", "DBHelper");
                return (true, null);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                string msg = "添加失败：数据已存在，请勿重复添加！";
                Logger.Warn($"数据重复：尝试添加已存在的{typeof(T).Name}数据", "DBHelper");
                return (false, msg);
            }
            catch (SqliteException ex)
            {
                string msg = $"SQLite数据库错误：{ex.Message}";
                Logger.Error($"SQLite数据库错误：{ex.Message}", ex, "DBHelper");
                return (false, msg);
            }
            catch (DbUpdateException ex)
            {
                string msg = ex.InnerException?.Message ?? ex.Message;
                Logger.Error($"数据更新失败：{msg}", ex, "DBHelper");
                return (false, $"数据更新失败：{msg}");
            }
            catch (NullReferenceException ex)
            {
                string msg = $"操作错误：数据对象为空，请检查输入！\r\n错误提示：{ex.Message}";
                Logger.Error($"操作错误：数据对象为空", ex, "DBHelper");
                return (false, msg);
            }
            catch (Exception ex)
            {
                string msg = $"添加数据失败：{ex.Message}";
                Logger.Error($"添加数据失败：{ex.Message}", ex, "DBHelper");
                return (false, msg);
            }
        }

        // ==================================================
        //  查询
        // ==================================================

        /// <summary>
        /// 按条件查询数据（异步，数据库层面过滤）
        /// </summary>
        /// <returns>(数据列表, 错误提示信息)</returns>
        public static async Task<(List<T> Data, string? WarningMessage)> GetDBDataAsync<T>(
            Expression<Func<T, bool>> predicate) where T : class
        {
            try
            {
                using var dbContext = new SharpLogDbContext();
                var data = await dbContext.Set<T>().Where(predicate).ToListAsync();
                return (data, null);
            }
            catch (SqliteException ex)
            {
                string msg = ex.SqliteErrorCode switch
                {
                    1 => "查询失败：数据库中不存在当前实体对应的表/字段！",
                    5 => "查询失败：数据库文件权限不足/无法打开！",
                    _ => $"SQLite数据库查询错误：{ex.Message}"
                };
                Logger.Error($"SQLite查询异常：{msg}", ex, "DBHelper");
                return (new List<T>(), msg);
            }
            catch (InvalidOperationException ex)
            {
                string msg = $"查询逻辑错误：{ex.Message}";
                Logger.Error($"查询逻辑错误：{ex.Message}", ex, "DBHelper");
                return (new List<T>(), msg);
            }
            catch (Exception ex)
            {
                string msg = $"查询数据失败：{ex.Message}";
                Logger.Error($"查询数据失败：{ex.Message}", ex, "DBHelper");
                return (new List<T>(), msg);
            }
        }

        /// <summary>
        /// 查询全部数据（异步）
        /// </summary>
        public static async Task<(List<T> Data, string? WarningMessage)> GetAllDBDataAsync<T>() where T : class
        {
            try
            {
                using var dbContext = new SharpLogDbContext();
                var data = await dbContext.Set<T>().ToListAsync();
                return (data, null);
            }
            catch (Exception ex)
            {
                return (new List<T>(), ex.Message);
            }
        }

        /// <summary>
        /// 按主键倒序查询【最近 N 条】数据（异步）。
        ///
        /// 【为什么要有这个方法】
        ///   列表类界面往往只需展示最近若干条记录，若照旧全表加载，
        ///   日志积累到几万条时既慢又占内存。排序与截断都交给 SQLite 完成，
        ///   生成的 SQL 形如：
        ///       SELECT * FROM LogTable ORDER BY Id DESC LIMIT 200
        ///   内存里只有这 200 条，与表的总行数无关。
        ///
        /// ⚠ 主键名约定：本方法按名为 "Id" 的属性排序，要求实体存在该属性
        ///   （本项目所有实体均满足）。之所以用 EF.Property 而不是直接写
        ///   e => e.Id，是因为泛型 T 在编译期并不知道自己有 Id 这个成员。
        /// </summary>
        /// <param name="count">要取的条数；小于等于 0 时直接返回空列表</param>
        /// <returns>(数据列表——按主键从大到小即最新在前, 错误提示信息)</returns>
        public static async Task<(List<T> Data, string? WarningMessage)> GetLatestDBDataAsync<T>(int count) where T : class
        {
            // 条数非法时不必访问数据库
            if (count <= 0)
            {
                return (new List<T>(), null);
            }

            try
            {
                using var dbContext = new SharpLogDbContext();

                // OrderByDescending + Take 会被 EF Core 翻译成 SQL 的
                // ORDER BY Id DESC LIMIT @count，不会把整张表读进内存
                var data = await dbContext.Set<T>()
                                          .OrderByDescending(e => EF.Property<int>(e, "Id"))
                                          .Take(count)
                                          .ToListAsync();
                return (data, null);
            }
            catch (SqliteException ex)
            {
                string msg = ex.SqliteErrorCode switch
                {
                    1 => "查询失败：数据库中不存在当前实体对应的表/字段！",
                    5 => "查询失败：数据库文件权限不足/无法打开！",
                    _ => $"SQLite数据库查询错误：{ex.Message}"
                };
                Logger.Error($"SQLite查询异常：{msg}", ex, "DBHelper");
                return (new List<T>(), msg);
            }
            catch (InvalidOperationException ex)
            {
                string msg = $"查询逻辑错误：{ex.Message}";
                Logger.Error($"查询逻辑错误：{msg}", ex, "DBHelper");
                return (new List<T>(), msg);
            }
            catch (Exception ex)
            {
                string msg = $"查询数据失败：{ex.Message}";
                Logger.Error($"查询数据失败：{msg}", ex, "DBHelper");
                return (new List<T>(), msg);
            }
        }

        // ==================================================
        //  更新
        // ==================================================

        /// <summary>
        /// 更新数据（异步，通过主键匹配）
        /// </summary>
        /// <returns>(是否成功, 错误提示信息)</returns>
        public static async Task<(bool Success, string? WarningMessage)> UpdateDBDataAsync<T>(T data) where T : class
        {
            if (data == null)
            {
                return (false, "更新失败：传入的更新数据不能为空！");
            }

            try
            {
                using var dbContext = new SharpLogDbContext();
                dbContext.Set<T>().Update(data);
                await dbContext.SaveChangesAsync();

                Logger.Info($"成功更新{typeof(T).Name}数据", "DBHelper");
                return (true, null);
            }
            catch (SqliteException ex)
            {
                string msg = ex.SqliteErrorCode switch
                {
                    19 => "更新失败：违反唯一约束，数据值与现有记录重复！",
                    1 => "更新失败：数据库中不存在当前实体对应的表/字段！",
                    5 => "更新失败：数据库文件被占用/权限不足，无法写入！",
                    _ => $"SQLite数据库错误：{ex.Message}"
                };
                Logger.Error($"SQLite更新异常：{msg}", ex, "DBHelper");
                return (false, msg);
            }
            catch (DbUpdateException ex)
            {
                string realMsg = ex.InnerException?.Message ?? ex.Message;
                Logger.Error($"EF更新异常：{realMsg}", ex, "DBHelper");
                return (false, $"数据更新失败：{realMsg}");
            }
            catch (InvalidOperationException ex)
            {
                string msg = ex.Message.Contains("primary key")
                    ? "更新失败：实体无主键/传入数据缺少主键，EF无法匹配更新记录！"
                    : $"更新逻辑错误：{ex.Message}";
                Logger.Error($"更新逻辑错误：{ex.Message}", ex, "DBHelper");
                return (false, msg);
            }
            catch (NullReferenceException ex)
            {
                string msg = $"更新失败：空值引用，请检查数据对象属性！{ex.Message}";
                Logger.Error($"空值引用异常：{ex.Message}", ex, "DBHelper");
                return (false, msg);
            }
            catch (Exception ex)
            {
                string msg = $"数据更新失败：{ex.Message}";
                Logger.Error($"数据更新失败：{ex.Message}", ex, "DBHelper");
                return (false, msg);
            }
        }

        // ==================================================
        //  删除
        // ==================================================

        /// <summary>
        /// 按条件删除数据（异步，含全表删除检测）
        /// </summary>
        /// <returns>(是否成功, 错误提示信息, 是否为全表删除警告)</returns>
        public static async Task<(bool Success, string? WarningMessage, bool IsFullTableWarning)> DeleteDBDataAsync<T>(
            Expression<Func<T, bool>> predicate) where T : class
        {
            if (predicate == null)
            {
                return (false, "删除失败：未传入删除条件，无法执行删除操作！", false);
            }

            try
            {
                using var dbContext = new SharpLogDbContext();
                var dbSet = dbContext.Set<T>();

                List<T> dataList = await dbSet.Where(predicate).ToListAsync();

                if (dataList.Count == 0)
                {
                    return (false, "未查询到符合删除条件的数据，无需执行删除！", false);
                }

                // 全表删除检测 → 返回警告，由上层决定是否继续
                int totalCount = await dbSet.CountAsync();
                if (dataList.Count == totalCount)
                {
                    string warn = $"警告：即将删除【{typeof(T).Name}】表中全部 {dataList.Count} 条数据，此操作不可恢复！";
                    return (false, warn, true);  // IsFullTableWarning = true
                }

                dbSet.RemoveRange(dataList);
                await dbContext.SaveChangesAsync();

                Logger.Info($"成功删除{dataList.Count}条{typeof(T).Name}数据", "DBHelper");
                return (true, null, false);
            }
            catch (SqliteException ex)
            {
                string msg = ex.SqliteErrorCode switch
                {
                    1 => $"删除失败：数据库中不存在{typeof(T).Name}对应的表/字段！",
                    5 => "删除失败：数据库文件被占用/权限不足，无法执行删除！",
                    1299 => "删除失败：违反外键约束，该数据被其他表关联，无法直接删除！",
                    _ => $"SQLite数据库错误：{ex.Message}"
                };
                Logger.Error($"SQLite删除异常：{msg}", ex, "DBHelper");
                return (false, msg, false);
            }
            catch (DbUpdateException ex)
            {
                string realMsg = ex.InnerException?.Message ?? ex.Message;
                if (realMsg.Contains("foreign key") || realMsg.Contains("constraint"))
                {
                    realMsg = "删除失败：该数据与其他表存在关联关系，请先删除关联数据再尝试！";
                }
                Logger.Error($"EF删除异常：{realMsg}", ex, "DBHelper");
                return (false, $"数据删除失败：{realMsg}", false);
            }
            catch (InvalidOperationException ex)
            {
                string msg = $"删除逻辑错误：{ex.Message}";
                Logger.Error($"删除逻辑错误：{ex.Message}", ex, "DBHelper");
                return (false, msg, false);
            }
            catch (NullReferenceException ex)
            {
                string msg = $"删除失败：空值引用，请检查删除条件！{ex.Message}";
                Logger.Error($"空值引用异常：{ex.Message}", ex, "DBHelper");
                return (false, msg, false);
            }
            catch (Exception ex)
            {
                string msg = $"数据删除失败：{ex.Message}";
                Logger.Error($"数据删除失败：{ex.Message}", ex, "DBHelper");
                return (false, msg, false);
            }
        }

        /// <summary>
        /// 强制删除（跳过全表删除检测，确认后调用）
        /// </summary>
        public static async Task<(bool Success, string? WarningMessage)> ForceDeleteAllAsync<T>() where T : class
        {
            try
            {
                using var dbContext = new SharpLogDbContext();
                var dbSet = dbContext.Set<T>();
                int count = await dbSet.CountAsync();

                if (count == 0) return (true, null);

                dbSet.RemoveRange(dbSet);
                await dbContext.SaveChangesAsync();

                Logger.Info($"强制清空{typeof(T).Name}表，共{count}条", "DBHelper");
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
