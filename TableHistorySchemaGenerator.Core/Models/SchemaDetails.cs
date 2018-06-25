using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.Core.Models
{
    public class SchemaDetails : DbObject
    {

        public SchemaDetails Clone()
        {
            return new SchemaDetails()
            {
                Name = Name,
                Schema = Schema,
                State = State
            };
        }
    }
}
