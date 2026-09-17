using Microsoft.EntityFrameworkCore;   // Database.Migrate() 扩展方法所在命名空间
using SharpLog.DB;
using SharpLog.Infrastructure;
using SharpLog.ToolsBox;
using System;
using System.Windows;

namespace SharpLog
{
    /// <summary>
    /// 应用程序入口（App.xaml 的交互逻辑）。
    ///
    /// 启动职责：
    ///   1. 初始化数据库 —— 通过 EF Core 迁移机制建库/升级表结构；
    ///   2. 加载 ADIF 基础数据 —— 把官方的波段表、通联模式表、
    ///      传播模式表一次性读入内存，供后续校验与界面使用。
    ///
    /// 两项都属于"程序运行的前提条件"，任一失败都会终止启动，
    /// 以避免程序在缺少基础数据的状态下运行并产生难以排查的错误。
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 程序启动时执行：初始化数据库与加载 ADIF 基础数据。
        ///
        /// 【数据库迁移机制说明】（替代原 EnsureCreated）
        /// · Migrate() 会查询数据库中的 __EFMigrationsHistory 表，
        ///   找出未应用的迁移并依次执行；
        /// · 首次运行：库不存在 → 自动创建库、建表、建索引，并记录迁移历史；
        /// · 后续运行：库已是最新 → 无任何操作，启动开销极小；
        /// · 实体结构变更后：生成新迁移 → 启动时自动升级表结构，旧数据保留。
        ///
        /// 【ADIF 数据加载说明】
        /// 由 AdifDataLoader.Initialize() 从程序目录的 ADIFCode_Json 文件夹
        /// 读取三个 JSON 文件并转换为内存对象。之所以在启动时一次性加载，
        /// 是为了避免在每次频率校验时都去读取磁盘文件（那样既慢又浪费 IO）。
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // ---------- 第 1 步：数据库初始化（含首次建库与结构升级） ----------
                using var db = new SharpLogDbContext();
                db.Database.Migrate();
                Logger.Info("数据库迁移检查完成", "AppStartup");

                // ---------- 第 2 步：加载 ADIF 基础数据到内存 ----------
                // 加载失败会抛出 FileOperationException，由下方 catch 统一处理
                AdifDataLoader.Initialize();
            }
            catch (Exception ex)
            {
                // 初始化失败属于致命错误：数据库或基础数据不可用，
                // 后续所有操作都会出错。此处记录日志并终止启动，
                // 避免用户在其他功能处遇到更难排查的错误。
                Logger.Error("程序初始化失败，程序无法启动", ex, "AppStartup");
                Shutdown(-1);
            }
        }
    }
}
