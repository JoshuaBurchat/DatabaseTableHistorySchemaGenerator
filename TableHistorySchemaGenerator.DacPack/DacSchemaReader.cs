using AuditShadowBuilder.Infrastructure.Logging;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core;
using TableHistorySchemaGenerator.Core.Models;

namespace TableHistorySchemaGenerator.DacPack
{


    public class DacSchemaReader : IDbSchemaReader
    {

        private Dictionary<SqlDataType, CommonHistorySqlType> _typeMap = new Dictionary<SqlDataType, CommonHistorySqlType>()
        {
            { SqlDataType.BigInt ,  CommonHistorySqlType.BigInt },
            { SqlDataType.Int , CommonHistorySqlType.Int },
            { SqlDataType.SmallInt , CommonHistorySqlType.SmallInt },
            { SqlDataType.TinyInt , CommonHistorySqlType.TinyInt },
            { SqlDataType.Bit , CommonHistorySqlType.Bit },
            { SqlDataType.Decimal , CommonHistorySqlType.Decimal },
            { SqlDataType.Numeric , CommonHistorySqlType.Numeric },
            { SqlDataType.Money , CommonHistorySqlType.Money },
            { SqlDataType.SmallMoney ,CommonHistorySqlType.SmallMoney },
            { SqlDataType.Float , CommonHistorySqlType.Float },
            { SqlDataType.Real , CommonHistorySqlType.Real },
            { SqlDataType.DateTime , CommonHistorySqlType.DateTime },
            { SqlDataType.SmallDateTime , CommonHistorySqlType.SmallDateTime },
            { SqlDataType.Char , CommonHistorySqlType.Char },
            { SqlDataType.VarChar , CommonHistorySqlType.VarChar },
            { SqlDataType.Text , CommonHistorySqlType.Text },
            { SqlDataType.NChar , CommonHistorySqlType.NChar },
            { SqlDataType.NVarChar , CommonHistorySqlType.NVarChar },
            { SqlDataType.NText , CommonHistorySqlType.NText },
            { SqlDataType.Binary , CommonHistorySqlType.Binary },
            { SqlDataType.VarBinary , CommonHistorySqlType.VarBinary },
            { SqlDataType.Image , CommonHistorySqlType.Image },
            { SqlDataType.Timestamp , CommonHistorySqlType.Timestamp },
            //Note unique id is not used in history
            { SqlDataType.UniqueIdentifier , CommonHistorySqlType.Int },
            { SqlDataType.Xml , CommonHistorySqlType.Xml },
            { SqlDataType.Date , CommonHistorySqlType.Date },
            { SqlDataType.Time , CommonHistorySqlType.Time },
            { SqlDataType.DateTime2 ,CommonHistorySqlType.DateTime2},
            { SqlDataType.DateTimeOffset ,CommonHistorySqlType.DateTimeOffset },
            { SqlDataType.Rowversion , CommonHistorySqlType.Rowversion }
        };

        private string _connectionString;
        private Stream _stream;
        private bool _streamIsFile;
        private ILogger _logger;
        public DacSchemaReader(string connectionString, ILogger logger)
        {
            this._connectionString = connectionString;
            this._logger = logger;
        }
        public DacSchemaReader(Stream stream, bool isFile, ILogger logger)
        {
            this._stream = stream;
            this._streamIsFile = isFile;
            this._logger = logger;
        }
        private TSqlModel LoadSqlModel()
        {
            if (_stream != null)
            {
                _logger.LogMessage("Loading Dacpac file from stream...");
                return TSqlModel.LoadFromDacpac(_stream, new ModelLoadOptions() { ModelStorageType = _streamIsFile ? DacSchemaModelStorageType.File : DacSchemaModelStorageType.Memory });
            }
            else
            {
                _logger.LogMessage("Loading Dacpac file from connection string \"{0}\"...", _connectionString);
                return TSqlModel.LoadFromDatabase(_connectionString);
            }
        }
        public DbSchemaPackage GetDetails(IHistoryTableInspector inspector)
        {
            _logger.LogMessage("Starting to read Dac source.");

            Dictionary<Tuple<string, string>, TableDetails> historyTables = new Dictionary<Tuple<string, string>, TableDetails>();

            DbSchemaPackage results = new DbSchemaPackage();

            using (TSqlModel model = LoadSqlModel())
            {
                _logger.LogMessage("Inspecting tables");
                var allSchemas = model.GetObjects(DacQueryScopes.All, ModelSchema.Schema);

                foreach(var schema in allSchemas)
                {
                    results.Schemas.Add(new SchemaDetails()
                    {
                        Name = schema.Name.Parts[0],
                        Schema = "dbo",
                        State = DbObjectState.Same
                    });
                }

                var allTables = model.GetObjects(DacQueryScopes.All, ModelSchema.Table);

                foreach (var table in allTables)
                {
                    var currentTable = new TableDetails()
                    {
                        Name = table.Name.Parts[1],
                        Schema = table.Name.Parts[0]
                    };
                    IEnumerable<TSqlObject> primaryKeys = table.GetReferencing(PrimaryKeyConstraint.Host, DacQueryScopes.UserDefined);
                    foreach (var primaryColumn in primaryKeys)
                    {
                        foreach (var column in primaryColumn.GetReferenced(PrimaryKeyConstraint.Columns))
                        {
                            currentTable.PrimaryColumnNames.Add(column.Name.Parts[2]);
                        }
                    }


                    foreach (var column in table.GetReferenced(Table.Columns))
                    {

                        bool isNullable = column.GetProperty<bool>(Column.Nullable);

                        var length = column.GetProperty<int>(Column.Length);
                        var precision = column.GetProperty<int>(Column.Precision);
                        var scale = column.GetProperty<int>(Column.Scale);
                        SqlDataType type = column.GetReferenced(Column.DataType).First().GetProperty<SqlDataType>(DataType.SqlDataType);




                        string sizeDetails = string.Empty;
                        if (length != 0)
                        {
                            sizeDetails = "(" + length + ")";
                        }
                        else if (type == SqlDataType.DateTime2)
                        {
                            sizeDetails = "(" + scale + ")";
                        }
                        else if (scale != 0 || precision != 0)
                        {
                            sizeDetails = "(" + precision + "," + scale + ")";
                        }
                        CommonHistorySqlType commonHistorySqlType;
                        if (_typeMap.TryGetValue(type, out commonHistorySqlType))
                        {
                            currentTable.Columns.Add(new ColumnDetails()
                            {
                                Name = column.Name.Parts[2],
                                SqlType = _typeMap[type],
                                IsNullable = isNullable,
                                SizeDetails = sizeDetails
                            });
                        }
                        else
                        {
                            throw new InvalidCastException(string.Format(
                                 "Unable to map type {0} for table {1}, and column {1}. This type is not recognized as a history mappable type.",
                                 type.ToString(),
                                 table.Name,
                                 column.Name.Parts[2]
                            ));
                        }
                    }
                    if (inspector.IsHistoryTable(currentTable))
                    {
                        _logger.LogMessage("History table found: {0}", currentTable.FullName);

                        currentTable.IsHistorical = true;
                        historyTables.Add(
                            new Tuple<string, string>(
                                currentTable.Schema.ToLower(),
                                currentTable.Name.ToLower()), currentTable);
                    }
                    else
                    {
                        results.Tables.Add(currentTable);
                    }
                }
                foreach (var table in results.Tables)
                {
                    var historyNaming = inspector.BuildHistoryName(table);
                    TableDetails matchingHistoryTable = null;
                    if (historyTables.TryGetValue(new Tuple<string, string>(historyNaming.Schema.ToLower(), historyNaming.Name.ToLower()), out matchingHistoryTable))
                    {
                        _logger.LogMessage("Matched History table {0} to {1}", matchingHistoryTable.FullName,  table.FullName);
                        table.HistoricalTable = matchingHistoryTable;
                    }
                }
            }
            return results;
        }
    }
}
