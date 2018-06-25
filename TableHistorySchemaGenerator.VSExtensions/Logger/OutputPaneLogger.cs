using AuditShadowBuilder.Infrastructure.Logging;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.VSExtensions.Logger
{
    public class OutputPaneLogger : ILogger
    {
        private IVsOutputWindow _output;
        private static IVsOutputWindowPane _pane;
        public OutputPaneLogger(IVsOutputWindow output)
        {
            _output = output;
        }
    
        private DefaultConsoleLogger _defaultLogger = new DefaultConsoleLogger();
         private void PrintMessageToOutput(string message)
        {
            if (_pane == null)
            {
                Guid guid = Guid.NewGuid();
                _output.CreatePane(ref guid, "Table History Schema Generator", 1, 1);
                _output.GetPane(ref guid, out _pane);
            }
            if(_pane != null)
            {
                _pane.OutputString(message + Environment.NewLine);
            }
        }
        public string LogError(string message, Exception exc, params object[] messageArgs)
        {
            message = _defaultLogger.LogError(message, exc, messageArgs);

            PrintMessageToOutput(message);

            return message;
        }

        public string LogMessage(string message, params object[] messageArgs)
        {
            message = _defaultLogger.LogMessage(message, messageArgs);
            PrintMessageToOutput(message);



            return message;
        }

        public string LogWarning(string message, params object[] messageArgs)
        {
            message = _defaultLogger.LogWarning(message, messageArgs);

            PrintMessageToOutput(message);

            return message;
        }
    }
}
