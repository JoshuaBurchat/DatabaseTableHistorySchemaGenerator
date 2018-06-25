using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.Core
{
    public interface IHistoryCommonConfiguration
    {
        string ExpectedCreatedByColumnName { get; }
        string ExpectedCreatedTimestampColumnName { get; }
        string Prefix { get; }
        string Schema { get; }

        bool IncludeDropOrAlterStatements { get; }
    }
    public class HistoryCommonConfiguration : IHistoryCommonConfiguration
    {
        public string ExpectedCreatedByColumnName { get; set; }
        public string ExpectedCreatedTimestampColumnName { get; set; }
        public string Prefix { get; set; }
        public string Schema { get; set; }

        public bool IncludeDropOrAlterStatements { get; set; }

    }
}
