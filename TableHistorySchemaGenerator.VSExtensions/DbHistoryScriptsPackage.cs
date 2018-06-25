//------------------------------------------------------------------------------
// <copyright file="DbHistoryScriptsPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE80;
using AuditShadowBuilder.Infrastructure.Logging;
using TableHistorySchemaGenerator.VSExtensions.Logger;

namespace TableHistorySchemaGenerator.VSExtensions
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(DbHistoryScriptsPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class DbHistoryScriptsPackage : Package
    {
        public static DTE2 _dte;
        public static ILogger Logger { get; private set; }

        public const string PackageGuidString = "87db1d31-3425-42b6-aab6-8e31e1691a44";


        public DbHistoryScriptsPackage()
        {
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            _dte = GetService(typeof(EnvDTE.DTE)) as DTE2;
            Logger = new OutputPaneLogger((IVsOutputWindow)this.GetService(typeof(SVsOutputWindow)));
            base.Initialize();
            TableHistorySchemaGenerator.VSExtensions.GenerateCommand.Initialize(this);
        }

        #endregion
    }
}
