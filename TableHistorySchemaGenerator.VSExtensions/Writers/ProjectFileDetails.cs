using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.VSExtension.Writers
{
  public  class ProjectFileDetails
    {
        public string PathInProject { get; set; }
        public string Name { get; set; }

        public string Content { get; set; }
    }
}
