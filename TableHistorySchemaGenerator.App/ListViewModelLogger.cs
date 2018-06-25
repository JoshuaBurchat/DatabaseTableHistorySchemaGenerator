using AuditShadowBuilder.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.App
{
    public enum LogType
    {
        Message,
        Error,
        Warning
    }
    public class LogMessage
    {
        public LogType Type { get; set; }
        public string Message { get; set; }
    }
    public class ListViewModelLogger : ObservableCollection<LogMessage>, ILogger
    {
        private DefaultConsoleLogger _defaultLogger = new DefaultConsoleLogger();
        public string LogError(string message, Exception exc, params object[] messageArgs)
        {
            message = _defaultLogger.LogError(message, exc, messageArgs);

            this.Add(new LogMessage()
            {
                Message = message,
                Type = LogType.Error
            });

            return message;
        }

        public string LogMessage(string message, params object[] messageArgs)
        {
            message = _defaultLogger.LogMessage(message, messageArgs);

            this.Add(new LogMessage()
            {
                Message = message,
                Type = LogType.Message
            });

            return message;
        }

        public string LogWarning(string message, params object[] messageArgs)
        {
            message = _defaultLogger.LogWarning(message, messageArgs);

            this.Add(new LogMessage()
            {
                Message = message,
                Type = LogType.Warning
            });

            return message;
        }
    }
}
