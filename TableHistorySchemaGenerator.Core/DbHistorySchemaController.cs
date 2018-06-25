using AuditShadowBuilder.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core.Models;

namespace TableHistorySchemaGenerator.Core
{







    public class DbHistorySchemaController
    {
        private IDbSchemaReader _reader;
        private IHistoryTableInspector _inspector;
        private IChangeScriptGenerator _scriptGenerator;
        private IScriptDestinationWriter _writer;
        private ILogger _logger;
        public DbHistorySchemaController(
            IDbSchemaReader reader,
            IHistoryTableInspector inspector,
            IChangeScriptGenerator scriptGenerator,
            IScriptDestinationWriter writer,
            ILogger logger
        )
        {
            this._reader = reader;
            this._inspector = inspector;
            this._scriptGenerator = scriptGenerator;
            this._writer = writer;
            this._logger = logger;

        }
        public void GenerateHistorySchemaObjects()
        {
            try
            {
                _logger.LogMessage("Starting Script Generation Process!");

                var sourceSchemaTables = _reader.GetDetails(_inspector);
                var requiredChanges = _inspector.InspectChanges(sourceSchemaTables);
                var requiredScripts = _scriptGenerator.Generate(requiredChanges).ToArray();
                _writer.Commit(requiredScripts);

                _logger.LogMessage("Script Generation Complete!");
            }
            catch (Exception exc)
            {
                _logger.LogError("There was a failure while building the schema changes in the controller", exc);
            }
        }

    }
}
