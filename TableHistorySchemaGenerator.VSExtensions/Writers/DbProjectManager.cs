using AuditShadowBuilder.Infrastructure.Logging;
using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.VSExtension.Writers
{

    //CREDIT to the https://github.com/madskristensen/AddAnyFile project as I used it to learn how to manage the project
    //Props to https://github.com/madskristensen
    public class DbProjectManager : IProjectManager
    {
        private DTE2 _dte;
        private ILogger _logger;
        public DbProjectManager(DTE2 dte, ILogger logger)
        {
            this._dte = dte;
            this._logger = logger;
        }
        public string BuildDacFile()
        {
            var currentProject = GetCurrentDbProject();
            if (currentProject != null)
            {
                _logger.LogMessage("Starting to build project {0} to produce a dacpack file.", currentProject.Name);
                this._dte.Solution.SolutionBuild.BuildProject("Debug", currentProject.FullName, true);
                if (this._dte.Solution.SolutionBuild.LastBuildInfo == 0)
                {
                    //ty https://stackoverflow.com/questions/5626303/how-do-i-get-the-output-directories-from-the-last-build
                    var builtGroup = currentProject.ConfigurationManager.ActiveConfiguration.OutputGroups.OfType<EnvDTE.OutputGroup>().First(x => x.CanonicalName == "Built");
                    var builtFiles = ((object[])builtGroup.FileURLs).OfType<string>().Select(f => new FileInfo(new Uri(f).LocalPath));

                    var dacFile = builtFiles.FirstOrDefault(d => d.Extension.ToLower() == ".dacpac");
                    if (dacFile != null)
                    {
                        _logger.LogMessage("Dacpack file {0} produced by build", dacFile.FullName);
                        return dacFile.FullName;
                    }
                }
                else
                {
                    _logger.LogWarning("Dacpack file not retrieved due to build failure!");
                    return null;
                }
            }
            else
            {
                _logger.LogWarning("You do not have a SQL Server DB Project selected.");
            }
            return null;
        }

        public ProjectFileDetails GetFile(string pathInProject)
        {
            var currentProject = GetCurrentDbProject();
            if (currentProject != null)
            {
                var root = GetRootFolder(currentProject);
                var fullFilePath = string.Format(@"{0}\{1}", root, pathInProject);
                FileInfo file = new FileInfo(fullFilePath);
                var existing = _dte.Solution.FindProjectItem(file.FullName);
                if (existing != null && file.Exists)
                {
                    using (var fileStream = file.OpenRead())
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        return new ProjectFileDetails()
                        {
                            Content = reader.ReadToEnd(),
                            Name = file.Name,
                            PathInProject = pathInProject
                        };
                    }

                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        private Project GetCurrentDbProject()
        {
            try
            {
                Project results = null;
                var activeSolutionProjects = _dte.ActiveSolutionProjects as Array;

                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                {
                    results = activeSolutionProjects.GetValue(0) as Project;
                }
                else
                {

                    var doc = _dte.ActiveDocument;

                    if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                    {
                        var item = (_dte.Solution != null) ? _dte.Solution.FindProjectItem(doc.FullName) : null;

                        if (item != null)
                            results = item.ContainingProject;
                    }
                }
                if (results.Kind == ProjectTypes.DB)
                {
                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting the active project", ex);
            }

            return null;
        }
        private string GetRootFolder(Project project)
        {
            if (project == null)
                return null;

            if (string.IsNullOrEmpty(project.FullName))
                return null;

            string fullPath = project.Properties.Item("FullPath").Value as string;

            if (string.IsNullOrEmpty(fullPath))
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;

            if (Directory.Exists(fullPath))
                return fullPath;

            if (File.Exists(fullPath))
                return Path.GetDirectoryName(fullPath);

            return null;
        }
        private ProjectItems CreateAndGetFolders(Project project, string fileFolder)
        {
            var root = GetRootFolder(project);
            var pathWithoutRoot = fileFolder.Replace(root, string.Empty);
            if (string.IsNullOrWhiteSpace(pathWithoutRoot))
            {
                return project.ProjectItems;
            }
            var folderTree = pathWithoutRoot.Trim('\\').Split('\\');
            bool createMode = false;
            string currentFolderPath = root;
            ProjectItems lastItemFound = project.ProjectItems;
            foreach (var folder in folderTree)
            {
                if (createMode && lastItemFound != null)
                {
                    lastItemFound = lastItemFound.AddFolder(folder).ProjectItems;
                }
                else
                {
                    currentFolderPath += "\\" + folder;
                    var currentItemFound = _dte.Solution.FindProjectItem(currentFolderPath);
                    if (currentItemFound != null)
                    {
                        lastItemFound = currentItemFound.ProjectItems;
                    }
                    else
                    {
                        createMode = true;
                        lastItemFound = lastItemFound.AddFolder(folder).ProjectItems;
                    }
                }
            }
            return lastItemFound;
        }
        public void UpdateOrAddFile(ProjectFileDetails projectFileDetails)
        {
            var currentProject = GetCurrentDbProject();
            if (currentProject != null)
            {
                var root = GetRootFolder(currentProject);
                var fullFilePath = string.Format(@"{0}\{1}", root, projectFileDetails.PathInProject);
                FileInfo file = new FileInfo(fullFilePath);
                var existing = _dte.Solution.FindProjectItem(file.FullName);
                if (existing != null)
                {
                    _logger.LogMessage("Updating file to project {0}.", projectFileDetails.PathInProject);
                    using (StreamWriter writer = new StreamWriter(file.FullName, false))
                    {
                        writer.Write(projectFileDetails.Content);
                    }
                    existing.Open();
                    existing.Save();
                }
                else
                {
                    _logger.LogMessage("Adding file to project {0}.", projectFileDetails.PathInProject);
                    var itemsCollection = CreateAndGetFolders(currentProject, file.DirectoryName);
                    using (StreamWriter writer = new StreamWriter(file.FullName, false))
                    {
                        writer.Write(projectFileDetails.Content);
                    }
                    var item = itemsCollection.AddFromFile(file.FullName);
                }
                currentProject.Save();
            }
        }

        public string GetProjectName()
        {
            var currentProject = GetCurrentDbProject();
            if (currentProject != null)
            {
                return currentProject.Name;
            }
            return null;
        }
    }

    public static class ProjectTypes
    {
        public const string ASPNET_5 = "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}";
        public const string DOTNET_Core = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
        public const string WEBSITE_PROJECT = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        public const string UNIVERSAL_APP = "{262852C6-CD72-467D-83FE-5EEB1973A190}";
        public const string NODE_JS = "{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}";
        public const string SSDT = "{00d1a9c2-b5f0-4af3-8072-f6c62b433612}";
        public const string DB = "{00d1a9c2-b5f0-4af3-8072-f6c62b433612}";
    }
}
