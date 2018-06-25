using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.Core.Models
{
    public enum DbObjectState
    {
        New,
        Modified,
        Same
    }
    public class DbObject
    {
        public string Schema { get; set; }
        public string Name { get; set; }


        public string FullName { get { return string.Format("[{0}].[{1}]", Schema, Name); } }

        public DbObjectState State { get; set; } = DbObjectState.Same;
    }
}
