//------------------------------------------------------------------------------
// <copyright file="GenerateCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TableHistorySchemaGenerator.VSExtension.Writers;
using AuditShadowBuilder.Infrastructure.Logging;
using TableHistorySchemaGenerator.DacPack;
using TableHistorySchemaGenerator.Core;
using System.IO;
using TableHistorySchemaGenerator.VSExtensions.Logger;
using TableHistorySchemaGenerator.Core.Models;
using System.Collections.Generic;
using Microsoft.VisualStudio;

namespace TableHistorySchemaGenerator.VSExtensions
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenerateCommand : ExtensionPointPackage
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("8785a9d8-53d3-4426-8df7-94e60604dadb");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private GenerateCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenerateCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new GenerateCommand(package);
        }


        private DestinationConfiguration LoadConfiguration(DbProjectManager manager, ILogger logger)
        {


            string fileName = "HistoryTableGeneration.json";
            try
            {
                var fileDetails = manager.GetFile(fileName);
                if (fileDetails == null)
                {
                    var dialogResult = VsShellUtilities.ShowMessageBox(
                         this.ServiceProvider,
                      string.Format("The file {0} cannot be found, and is required by the generator, would you like to have the default file added to the project?", fileName),
                         "Missing Configuration",
                         OLEMSGICON.OLEMSGICON_INFO,
                         OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                         OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    if (dialogResult == (int)VSConstants.MessageBoxResult.IDYES)
                    {
                        logger.LogMessage("Creating default configuration file {0}", fileName);
                        manager.UpdateOrAddFile(new ProjectFileDetails()
                        {
                            Name = fileName,
                            PathInProject = fileName,
                            //Behold the default
                            Content = Newtonsoft.Json.JsonConvert.SerializeObject(
                                new DestinationConfiguration()
                                {
                                    Common = new HistoryCommonConfiguration()
                                    {
                                        ExpectedCreatedByColumnName = "CreatedBy",
                                        ExpectedCreatedTimestampColumnName = "CreatedTimestamp",
                                        Prefix = "History_",
                                        Schema = "hst"
                                    },
                                    DefaultPathFormat = "{{Schema}}/{{ScriptType}}/{{Name}}.sql",
                                    TypedPathFormat = new Dictionary<ScriptType, string>() {
                                        { ScriptType.Schema,    "Security/{{Name}}.sql" }
                                    }
                                })
                        });
                    }
                }
                else
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<DestinationConfiguration>(fileDetails.Content);
                }
            }
            catch (Exception exc)
            {
                logger.LogError("There was an error while loading the configuration", exc);
            }
            return null;
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            var logger = DbHistoryScriptsPackage.Logger;
            try
            {
                DbProjectManager manager = new DbProjectManager(DbHistoryScriptsPackage._dte, new DefaultConsoleLogger());
                var configuration = this.LoadConfiguration(manager, logger);
                if (configuration != null)
                {
                    var dacFilePath = manager.BuildDacFile();
                    if (!string.IsNullOrEmpty(dacFilePath))
                    {

                        DacDbHistorySchemaControllerBuilder builder = new DacDbHistorySchemaControllerBuilder(configuration.Common, logger);
                        DbHistorySchemaController controller = null;
                        using (FileStream dacFileStream = new FileStream(dacFilePath, FileMode.Open))
                        {
                            DacSchemaReader reader = new DacSchemaReader(dacFileStream, true, logger);
                            controller = builder.Build(reader, new ProjectScriptDestinationWriter(new DbProjectManager(DbHistoryScriptsPackage._dte, logger), configuration, logger));

                            if (controller != null)
                            {
                                controller.GenerateHistorySchemaObjects();
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                logger.LogError("There was an error while generating history scripts.", exc);
            }

        }
    }
}
