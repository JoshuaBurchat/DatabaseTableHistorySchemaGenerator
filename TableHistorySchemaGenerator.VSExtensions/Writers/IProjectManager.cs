using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.VSExtension.Writers
{
    public interface IProjectManager
    {
        string GetProjectName();

        void UpdateOrAddFile(ProjectFileDetails file);

        string BuildDacFile();
    }
}
