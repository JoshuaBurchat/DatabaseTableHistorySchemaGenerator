using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.App
{
    public interface ISystemSourceRequestor
    {
        string GetFile();
        string GetFolder();
    }
}
