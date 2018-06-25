using AuditShadowBuilder.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core;
using TableHistorySchemaGenerator.Core.Models;

namespace TableHistorySchemaGenerator.App
{

    public class DestinationConfiguration
    {
        public string Destination { get; set; }
        public string PathFormat { get; set; } = "{{ScriptType}}/{{Schema}}_{{Name}}.sql";

        public IHistoryCommonConfiguration Common { get; set; } = new HistoryCommonConfiguration();
    }

    public class FolderScriptDestinationWriter : IScriptDestinationWriter
    {
        public DestinationConfiguration Configuration { get; set; }
        private ILogger _logger;
        public FolderScriptDestinationWriter(DestinationConfiguration configuration, ILogger logger)
        {
            this.Configuration = configuration;
            this._logger = logger;
        }
        public void Commit(ChangeScript[] scripts)
        {
            _logger.LogMessage("Starting to commit the changes to the output folder {0} with format {1}.", this.Configuration.Destination, this.Configuration.PathFormat);

            foreach (var script in scripts)
            {
                WriteScript(script);
            }

            _logger.LogMessage("Finished committing the changes to the output folder {0}", this.Configuration.Destination);
        }
        private void WriteScript(ChangeScript script)
        {
            var pathTemplate = HandlebarsDotNet.Handlebars.Compile(Configuration.PathFormat);

            FileInfo file = new FileInfo(string.Format(@"{0}\{1}", Configuration.Destination.Trim('\\'), pathTemplate(script)));
            _logger.LogMessage("- Writing to file {0}", file.FullName);

            if (!file.Directory.Exists) file.Directory.Create();

            if (file.Exists) file.Delete();
            using (FileStream fileStream = file.OpenWrite())
            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                writer.Write(script.ScriptText);
            }
        }
    }
}
