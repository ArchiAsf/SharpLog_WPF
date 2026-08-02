using SharpLog.DB;
using System.Configuration;
using System.Data;
using System.Windows;

namespace SharpLog
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // App.xaml.cs
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new SharpLogDbContext();
            db.Database.EnsureCreated();  // ✅ 一行搞定：不存在就建，存在就跳过
        }
    }

}
