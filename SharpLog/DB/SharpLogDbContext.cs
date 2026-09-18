using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpLog.DB
{
    public class SharpLogDbContext : DbContext
    {
        public DbSet<Model.QsoLog> LogTables { get; set; } = null!;

        /// <summary>
        /// 软件设置表（永远只有一行）。
        /// 由 AppSettingsDbTools 负责读写，ViewModel 不直接访问。
        /// </summary>
        public DbSet<Model.AppSettings> AppSettingsTable { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // 配置SQLite数据库连接字符串
            optionsBuilder.UseSqlite("Data Source=SharpLog.db");
        }
    }
}
