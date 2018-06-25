using AuditShadowBuilder.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core;
using TableHistorySchemaGenerator.Core.Models;

namespace TableHistorySchemaGenerator.App
{
    public class FileScriptDestinationWriter : IScriptDestinationWriter
    {
        public string Destination { get; set; }
        public IHistoryCommonConfiguration Configuration { get; set; }
        private ILogger _logger;
        public FileScriptDestinationWriter(string destination, IHistoryCommonConfiguration configuration, ILogger logger)
        {
            this.Destination = destination;
            this.Configuration = configuration;
            this._logger = logger;
        }


        public void Commit(ChangeScript[] scripts)
        {
            _logger.LogMessage("Starting to commit the changes to the output file {0}.", Destination);
            StringBuilder builder = new StringBuilder();

            ScriptType[] scriptTypeOrder = new ScriptType[] { ScriptType.Schema, ScriptType.Table, ScriptType.View, ScriptType.Trigger };
            foreach (var scriptType in scriptTypeOrder)
            {
                //Tables first
                foreach (var script in scripts.Where(s => s.ScriptType == scriptType)) builder.AppendLine(script.ScriptText);
            }
            //Other types
            foreach (var script in scripts.Where(s => !scriptTypeOrder.Contains(s.ScriptType))) builder.AppendLine(script.ScriptText);

            FileInfo file = new FileInfo(Destination);

            if (!file.Directory.Exists) file.Directory.Create();

            if (file.Exists) file.Delete();
            using (FileStream fileStream = file.OpenWrite())
            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                writer.Write(builder.ToString());
            }
            _logger.LogMessage("Finished committing to file {0}.", Destination);
        }

    }
}
