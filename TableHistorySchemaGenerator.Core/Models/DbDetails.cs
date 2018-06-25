using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.Core.Models
{
    public class DbDetails
    {
        public List<TableDetails> Tables { get; set; }
        public List<TableDetails> HistoryTables { get; set; }

    }

 
}
