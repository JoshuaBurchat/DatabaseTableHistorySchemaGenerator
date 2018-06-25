using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditShadowBuilder.Infrastructure.Logging
{
    public class DefaultConsoleLogger : ILogger
    {

        private string PrependDate( string message)
        {
            return string.Format("{0}: {1}", DateTime.Now, message);
        }

        public string LogError(string message, Exception exc, params object[] messageArgs)
        {
            message = string.Format(message, messageArgs);
            message = PrependDate(string.Format("ERROR: {0}, {1}", message, exc));
            Console.WriteLine(message);
            return message;
        }

        public string LogMessage(string message, params object[] messageArgs)
        {
            message = PrependDate(string.Format(message, messageArgs));
            Console.WriteLine(message);
            return message;
        }


        public string LogWarning(string message, params object[] messageArgs)
        {
            message = PrependDate(string.Format("WARNING: " + message, messageArgs));
            Console.WriteLine(message);
            return message;
        }
    }
}
