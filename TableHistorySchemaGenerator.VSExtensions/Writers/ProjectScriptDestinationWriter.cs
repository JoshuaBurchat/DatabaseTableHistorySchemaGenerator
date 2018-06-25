using AuditShadowBuilder.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core;
using TableHistorySchemaGenerator.Core.Models;

namespace TableHistorySchemaGenerator.VSExtension.Writers
{

    public class DestinationConfiguration
    {

        public string DefaultPathFormat { get; set; } = "{{Schema}}/{{ScriptType}}/{{Name}}.sql";
        public Dictionary<ScriptType, string> TypedPathFormat { get; set; } = new Dictionary<ScriptType, string>() {
            { ScriptType.Schema,    "Security/{{Name}}.sql" }
        };
        public HistoryCommonConfiguration Common { get; set; } = new HistoryCommonConfiguration();
    }

    public class ProjectScriptDestinationWriter : IScriptDestinationWriter
    {
        private DestinationConfiguration _configuration;
        private ILogger _logger;

        private IProjectManager _projectManager;
        public ProjectScriptDestinationWriter(IProjectManager projectManager, DestinationConfiguration configuration, ILogger logger)
        {
            this._projectManager = projectManager;
            this._configuration = configuration;
            this._logger = logger;
        }
        public void Commit(ChangeScript[] scripts)
        {
            string projectName = _projectManager.GetProjectName();
            _logger.LogMessage("Starting to commit the changes to the output project {0} with format {1}.", projectName, _configuration.DefaultPathFormat);

            var pathTemplate = HandlebarsDotNet.Handlebars.Compile(_configuration.DefaultPathFormat);
            var pathTemplatesByType = _configuration.TypedPathFormat.Select(d => new { Key = d.Key, Template = HandlebarsDotNet.Handlebars.Compile(d.Value) }).ToDictionary(d => d.Key, d => d.Template);
            foreach (var script in scripts)
            {
                Func<object, string> currentTemplate = null;
                if (!pathTemplatesByType.TryGetValue(script.ScriptType, out currentTemplate))
                {
                    currentTemplate = pathTemplate;
                }

                WriteScript(script, currentTemplate);
            }

            _logger.LogMessage("Finished committing the changes to the output folder {0}", projectName);
        }
        private void WriteScript(ChangeScript script, Func<object, string> pathTemplate)
        {
            var filePath = pathTemplate(script);
            _logger.LogMessage("- adding/updating file {0}", filePath);
            _projectManager.UpdateOrAddFile(new ProjectFileDetails()
            {
                Content = script.ScriptText,
                Name = script.Name,
                PathInProject = filePath
            });

        }
    }
}
