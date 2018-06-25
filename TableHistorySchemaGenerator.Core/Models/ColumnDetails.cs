using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.Core.Models
{
    public class ColumnDetails : DbObject
    {


        public CommonHistorySqlType SqlType { get; set; }
        public bool IsNullable { get; set; }
        public string SizeDetails { get; set; }
        public ColumnDetails Clone()
        {
            return new ColumnDetails()
            {
                IsNullable = IsNullable,
                Name = Name,
                SizeDetails = SizeDetails,
                SqlType = SqlType
            };
        }
    }
}
