using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.Core.Models
{
    public class DbSchemaPackage
    {
        public List<TableDetails> Tables { get; set; } = new List<TableDetails>();
        public List<SchemaDetails> Schemas { get; set; } = new List<SchemaDetails>();

        public DbSchemaPackage Clone()
        {
            var clone = new DbSchemaPackage()
            {
                Tables = this.Tables.Select(c => c.Clone()).ToList(),
                Schemas = this.Schemas.Select(s => s.Clone()).ToList()
            };
            return clone;
        }
    }
}
