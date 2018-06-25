using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.Core.Models
{
    public enum ScriptType
    {
        Table = 1,
        View = 2,
        Trigger = 3,
        Schema = 4
    }

    public class ChangeScript : DbObject
    {
        public ScriptType ScriptType { get; set; }

        public string ScriptText { get; set; }
        public TableDetails[] RelatedTables { get; set; }
    }
}
