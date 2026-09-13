using Microsoft.EntityFrameworkCore;   // Database.Migrate() 扩展方法所在命名空间
using SharpLog.DB;
using SharpLog.Infrastructure;
using System;
using System.Windows;

namespace SharpLog
{
    /// <summary>
    /// 应用程序入口（App.xaml 的交互逻辑）。
    ///
    /// 启动职责：初始化数据库（通过 EF Core 迁移机制）。
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 程序启动时执行：检查并应用数据库迁移。
        ///
        /// 迁移机制说明（替代原 EnsureCreated）：
        /// - Migrate() 会查询数据库中的 __EFMigrationsHistory 表，找出未应用的迁移并依次执行；
        /// - 首次运行：库不存在 → 自动创建库、建表、建索引，并记录迁移历史；
        /// - 后续运行：库已是最新 → 无任何操作，启动开销极小；
        /// - 实体结构变更后：生成新迁移 → 启动时自动升级表结构，旧数据保留。
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new SharpLogDbContext();

            try
            {
                // 执行所有未应用的迁移（含首次建库）
                db.Database.Migrate();
                Logger.Info("数据库迁移检查完成", "AppStartup");
            }
            catch (Exception ex)
            {
                // 迁移失败属于致命错误：数据库不可用，后续所有操作都会出错。
                // 此处记录日志并终止启动，避免用户在其他功能处遇到更难排查的错误。
                Logger.Error("数据库迁移失败，程序无法启动", ex, "AppStartup");
                Shutdown(-1);
            }
        }
    }
}
