using System;

namespace SharpLog.Infrastructure.Exceptions
{
    /// <summary>
    /// SharpLog自定义异常基类
    /// </summary>
    public abstract class SharpLogException : Exception
    {
        public SharpLogException(string message) : base(message) { }

        public SharpLogException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// 数据库操作异常
    /// </summary>
    public class DatabaseException : SharpLogException
    {
        public DatabaseException(string message) : base(message) { }

        public DatabaseException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// 数据验证异常
    /// </summary>
    public class DataValidationException : SharpLogException
    {
        public DataValidationException(string message) : base(message) { }

        public DataValidationException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// 业务逻辑异常
    /// </summary>
    public class BusinessException : SharpLogException
    {
        public BusinessException(string message) : base(message) { }

        public BusinessException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// 文件操作异常
    /// </summary>
    public class FileOperationException : SharpLogException
    {
        public FileOperationException(string message) : base(message) { }

        public FileOperationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
