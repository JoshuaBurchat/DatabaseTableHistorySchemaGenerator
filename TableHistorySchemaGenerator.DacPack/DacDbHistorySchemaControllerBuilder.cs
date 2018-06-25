using AuditShadowBuilder.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core;

namespace TableHistorySchemaGenerator.DacPack
{
    public class DacDbHistorySchemaControllerBuilder : DbHistorySchemaControllerBuilder
    {
        private ILogger _logger;
        public DacDbHistorySchemaControllerBuilder(IHistoryCommonConfiguration configurationReference, ILogger logger) : base(configurationReference, logger)
        {
            _logger = logger;
        }

        public  DbHistorySchemaController Build(string connectionString,  IScriptDestinationWriter writer)
        {
            return base.Build(new DacSchemaReader(connectionString, _logger), writer);
        }
        public  DbHistorySchemaController Build(Stream stream, bool isFile,  IScriptDestinationWriter writer)
        {
            return base.Build(new DacSchemaReader(null, false, _logger), writer);
        }
    }
}
