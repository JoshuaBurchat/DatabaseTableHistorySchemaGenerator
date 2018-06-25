using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.Core.Models
{
    public class TableDetails : DbObject
    {


        public bool IsHistorical { get; set; }
        public List<ColumnDetails> Columns { get; set; } = new List<ColumnDetails>();

        public List<string> PrimaryColumnNames { get; set; } = new List<string>();
        public TableDetails HistoricalTable { get; set; }

        public TableDetails Clone()
        {
            return new TableDetails()
            {
                Columns = Columns.Select(c => c.Clone()).ToList(),
                Name = Name,
                HistoricalTable = HistoricalTable != null ? HistoricalTable.Clone() : null,
                IsHistorical = IsHistorical,
                State = State,
                Schema = Schema,
                PrimaryColumnNames = PrimaryColumnNames.ToList()
            };
        }

    }
}
