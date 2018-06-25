using AuditShadowBuilder.Infrastructure.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core.Inspectors;
using TableHistorySchemaGenerator.Core.Models;
using TableHistorySchemaGenerator.Core.ScriptGenerators;

namespace TableHistorySchemaGenerator.Core
{



    /// <summary>
    /// This is intended to be used to create variations of the controller with different flow implementations
    /// The source and destination should always be supplied by the consummer.
    /// </summary>
    public class DbHistorySchemaControllerBuilder
    {
        public IHistoryCommonConfiguration Configuration { get; set; }
        private ILogger _logger;
        public DbHistorySchemaControllerBuilder( IHistoryCommonConfiguration _configurationReference, ILogger logger)
        {
            this.Configuration = _configurationReference;
            this._logger = logger;
        }

        public virtual DbHistorySchemaController Build(IDbSchemaReader reader, IScriptDestinationWriter writer)
        {
            _logger.LogMessage("Building Db History Schema Controller using the following configuration {0}", GetConfigurationLogString());

            return new DbHistorySchemaController(
                reader,
                new HistoryTableInspector(this.Configuration, _logger),
                new HistoryPerTableChangeScriptGenerator(this.Configuration, _logger),
                writer,
                _logger
            );
        }
        private string GetConfigurationLogString()
        {
            return JsonConvert.SerializeObject(this.Configuration, Formatting.None);
        }


    }
}
