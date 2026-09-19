using System;
using System.IO;
using System.Text;

namespace SharpLog.Infrastructure
{
    /// <summary>
    /// 统一日志记录工具
    /// </summary>
    public static class Logger
    {
        // 日志目录：程序运行目录下的 Logs 文件夹
        private static readonly string LogDirectory =
            Path.Combine(AppContext.BaseDirectory, "Logs");

        private static readonly object _lockObject = new object();

        static Logger()
        {
            // 初始化日志目录
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Info(string message, string? module = null)
        {
            WriteLog("INFO", message, module);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void Warn(string message, string? module = null)
        {
            WriteLog("WARN", message, module);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(string message, Exception? ex = null, string? module = null)
        {
            var sb = new StringBuilder(message);
            if (ex != null)
            {
                sb.AppendLine();
                sb.AppendLine("=== 异常详情 ===");
                sb.AppendLine($"异常类型：{ex.GetType().Name}");
                sb.AppendLine($"异常消息：{ex.Message}");
                sb.AppendLine($"堆栈跟踪：{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    sb.AppendLine($"内部异常：{ex.InnerException.Message}");
                }
            }
            WriteLog("ERROR", sb.ToString(), module);
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        public static void Debug(string message, string? module = null)
        {
            #if DEBUG
            WriteLog("DEBUG", message, module);
            #endif
        }

        private static void WriteLog(string level, string message, string? module)
        {
            lock (_lockObject)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logMessage = $"[{timestamp}] [{level}] {(module != null ? $"[{module}] " : "")}{message}";

                    // 写入当天的日志文件
                    string logFile = Path.Combine(LogDirectory, $"SharpLog_{DateTime.Now:yyyy-MM-dd}.log");
                    File.AppendAllText(logFile, logMessage + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // 如果日志写入失败，在调试时输出到控制台
                    System.Diagnostics.Debug.WriteLine(message);
                }
            }
        }

        /// <summary>
        /// 清理旧日志文件（保留最近7天）
        /// </summary>
        public static void CleanOldLogs(int daysToKeep = 7)
        {
            try
            {
                if (!Directory.Exists(LogDirectory)) return;

                var logFiles = Directory.GetFiles(LogDirectory, "*.log");
                DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug($"清理旧日志失败：{ex.Message}");
            }
        }
    }
}
