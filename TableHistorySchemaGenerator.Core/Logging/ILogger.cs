using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditShadowBuilder.Infrastructure.Logging
{
    public interface ILogger
    {
        string LogError(string message, Exception exc, params object[] messageArgs);
        string LogWarning(string message, params object[] messageArgs);
        string LogMessage(string message, params object[] messageArgs);
    }

}