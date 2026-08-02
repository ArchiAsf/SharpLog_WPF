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
        public DbSet<Model.LogTable> LogTables { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // 配置SQLite数据库连接字符串
            optionsBuilder.UseSqlite("Data Source=SharpLog.db");
        }
    }
}
