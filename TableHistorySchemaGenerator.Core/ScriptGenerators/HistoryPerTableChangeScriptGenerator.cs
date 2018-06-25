using AuditShadowBuilder.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core.Models;

namespace TableHistorySchemaGenerator.Core.ScriptGenerators
{
    public class HistoryPerTableChangeScriptGenerator : IChangeScriptGenerator
    {
        private IHistoryCommonConfiguration _historyCommonConfiguration;
        private ILogger _logger;
        public HistoryPerTableChangeScriptGenerator(IHistoryCommonConfiguration historyCommonConfiguration, ILogger logger)
        {
            this._historyCommonConfiguration = historyCommonConfiguration;
            this._logger = logger;
        }

        //Hmmm should I pass in DBObjects instead and then use the factory pattern to create a generator per type?
        public IEnumerable<ChangeScript> Generate(DbSchemaPackage desiredSchema)
        {
            _logger.LogMessage("Starting to generate scripts for all changes to the schema.");

            var tablesWhereHistoryWasModified = desiredSchema.Tables.Where(d => d.HistoricalTable != null && d.HistoricalTable.State != DbObjectState.Same).ToArray();
            var creates = tablesWhereHistoryWasModified.Select(c => GenerateCreateTable(c.HistoricalTable));
            var triggers = tablesWhereHistoryWasModified.Select(c => GenerateHistoryTrigger(c));
            var views = tablesWhereHistoryWasModified.Select(c => GenerateHistoryView(c));
            var schemas = desiredSchema.Schemas.Where(s => s.State != DbObjectState.Same).Select(s => GenerateSchemas(s));
            return creates.Union(triggers).Union(views).Union(schemas).Where(s => s != null);
        }
        private ChangeScript GenerateSchemas(SchemaDetails schema)
        {
            var builder = new StringBuilder();
            if (_historyCommonConfiguration.IncludeDropOrAlterStatements)
            {
                builder.AppendFormat(
                   @"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'hst')  EXEC('CREATE SCHEMA [hst]');   
                    GO
                    ",
                   schema.Name);
            }
            else
            {
                builder.AppendFormat(
                  @"
                    CREATE SCHEMA [{0}]
                    GO
                    ",
                  schema.Name);
            }


            return new ChangeScript()
            {
                Name = schema.Name,
                Schema = schema.Schema,
                ScriptText = builder.ToString(),
                ScriptType = ScriptType.Schema,
                State = schema.State
            };
        }
        private ChangeScript GenerateHistoryView(TableDetails table)
        {
            var auditColumns = new[] { _historyCommonConfiguration.ExpectedCreatedByColumnName, _historyCommonConfiguration.ExpectedCreatedTimestampColumnName };

            if (!auditColumns.Any(a => table.Columns.Any(c => c.Name == a)))
            {
                _logger.LogWarning("The table {0} does not have the proper audit columns to have a view generated.", table.FullName);
                return null;
            }
            _logger.LogMessage("Generating History View for {0}.", table.FullName);

            StringBuilder allColumnBuilder = new StringBuilder();
            StringBuilder historyColumnBuilder = new StringBuilder();
            StringBuilder regularColumnBuilder = new StringBuilder();
            //non audit columns
            foreach (var column in table.HistoricalTable.Columns.Where(c => !auditColumns.Contains(c.Name)))
            {
                historyColumnBuilder.AppendFormat("[{0}],", column.Name);

                if (table.Columns.Any(c => c.Name == column.Name))
                    regularColumnBuilder.AppendFormat("[{0}],", column.Name);
                else
                    regularColumnBuilder.AppendFormat("NULL AS [{0}],", column.Name);

                allColumnBuilder.AppendFormat("AllRecords.[{0}],", column.Name);
            }
            string viewName = string.Format("v{0}History", table.Name);

            //0, allcolumns 1  regColumns, 2 historyColums
            StringBuilder builder = new StringBuilder();

            if (_historyCommonConfiguration.IncludeDropOrAlterStatements)
            {
                builder.AppendFormat(@"IF OBJECT_ID('{0}.{1}') IS NOT NULL DROP VIEW {0}.{1} 
                                     GO
                                    ", _historyCommonConfiguration.Schema, viewName);
            }


            builder.AppendFormat(@"
            CREATE VIEW {0}.{1} AS
            SELECT {2} AllRecords.ModifiedBy, AllRecords.ModifiedTimestamp, FirstRecord.CreatedBy, FirstRecord.CreatedTimestamp
            FROM
            (
                SELECT {3}
                      CAST(1 AS BIT) AS IsCurrent,
                      [CreatedBy] AS ModifiedBy,
                      [CreatedTimestamp] AS ModifiedTimestamp
                FROM {5}
                UNION ALL
                SELECT {4}
                       CAST(0 AS BIT) IsCurrent,
                       [CreatedBy] AS ModifiedBy,
                       [CreatedTimestamp] AS ModifiedTimestamp
                FROM {6}
            ) AS AllRecords
            LEFT JOIN (
                SELECT DISTINCT -- incase dates duplicate..
            ", _historyCommonConfiguration.Schema, viewName, allColumnBuilder, regularColumnBuilder, historyColumnBuilder, table.FullName, table.HistoricalTable.FullName);

            foreach (var columnName in table.PrimaryColumnNames)
                builder.AppendFormat("FirstRecordsMatch.[{0}],\n", columnName);

            builder.Append(@"
                        FirstRecordsMatch.[CreatedTimestamp] , 
                        FirstRecordsMatch.CreatedBy
                FROM 
                (
                    SELECT	
            ");
            foreach (var columnName in table.PrimaryColumnNames)
                builder.AppendFormat("[{0}],\n", columnName);
            builder.AppendFormat(@"
                            MIN([CreatedTimestamp]) AS [CreatedTimestamp]
                    FROM	{0}
                    GROUP BY ID
                ) FirstRecordGroup
                JOIN {0} AS FirstRecordsMatch  
                    ON ", table.HistoricalTable.FullName);
            foreach (var columnName in table.PrimaryColumnNames)
                builder.AppendFormat("FirstRecordGroup.[{0}] = FirstRecordsMatch.[{0}] AND\n", columnName);
            builder.AppendFormat(@"
                    FirstRecordGroup.[CreatedTimestamp] = FirstRecordsMatch.[CreatedTimestamp]
                ) AS FirstRecord
                    ON  ");
            foreach (var columnName in table.PrimaryColumnNames)
                builder.AppendFormat("AllRecords.[{0}] = AllRecords.[{0}] AND ", columnName);

            string toTrim = "AND ";

            builder.Remove(builder.Length - toTrim.Length, toTrim.Length);
            builder.AppendLine(@"
                                GO
            ");


            return new ChangeScript()
            {
                Name = viewName,
                Schema = _historyCommonConfiguration.Schema,
                RelatedTables = new[] { table, table.HistoricalTable },
                ScriptText = builder.ToString(),
                ScriptType = ScriptType.View,
                State = table.HistoricalTable.State
            };
        }
        private ChangeScript GenerateHistoryTrigger(TableDetails table)
        {

            _logger.LogMessage("Generating History Trigger for {0}.", table.FullName);

            StringBuilder columnBuilder = new StringBuilder();
            foreach (var column in table.Columns)
            {
                columnBuilder.AppendFormat("[{0}],", column.Name);
            }
            columnBuilder.Remove(columnBuilder.Length - 1, 1);
            StringBuilder builder = new StringBuilder();
            var triggerName = string.Format("[{0}].[{1}_HistoryTrigger]", table.Schema, table.Name);
            if (_historyCommonConfiguration.IncludeDropOrAlterStatements)
            {
                builder.AppendFormat(@"IF OBJECT_ID('{0}') IS NOT NULL DROP TRIGGER {0}; 
                                       GO
                                      ", triggerName);
            }

            builder.AppendFormat(@"
                    CREATE TRIGGER {0}
                       ON  [{1}].[{2}]
                       AFTER UPDATE, DELETE
                    AS 
                    BEGIN
	                    SET NOCOUNT ON;
	                    INSERT INTO [{3}].[{4}]
                               ({5})
	                    SELECT {5}
	                    FROM DELETED
                    END
                    GO"
                , triggerName
                , table.Schema, table.Name
                , table.HistoricalTable.Schema, table.HistoricalTable.Name
                , columnBuilder.ToString());

            return new ChangeScript()
            {
                Name = string.Format("{0}_HistoryTrigger", table.Name),
                Schema = table.Schema,
                RelatedTables = new[] { table, table.HistoricalTable },
                ScriptText = builder.ToString(),
                ScriptType = ScriptType.Trigger,
                State = table.HistoricalTable.State
            };
        }
        private ChangeScript GenerateCreateTable(TableDetails table)
        {

            StringBuilder builder = new StringBuilder();


            _logger.LogMessage("Generating new table scripts for {0}", table.FullName);
            StringBuilder columnBuilder = new StringBuilder();
            foreach (var column in table.Columns)
            {
                columnBuilder.AppendFormat("[{0}] {1} {2} {3},",
                    column.Name,
                    column.SqlType.ScriptName(),
                    column.SizeDetails,
                    column.IsNullable ? "NULL" : string.Empty
                );
            }
            columnBuilder.Remove(columnBuilder.Length - 1, 1);

            if (_historyCommonConfiguration.IncludeDropOrAlterStatements)
            {

                if (table.State == DbObjectState.Modified)
                {

                    _logger.LogMessage("Generating Column Alters for {0} as it already exists.", table.FullName);
                    foreach (var column in table.Columns.Where(c => c.State == DbObjectState.New))
                    {
                        builder.AppendFormat(@"ALTER TABLE {0} ADD [{1}] {2} {3} {4}; 
                                           GO
                                           ",
                                                table.FullName,
                                                column.Name,
                                                column.SqlType.ScriptName(),
                                                column.SizeDetails,
                                                column.IsNullable ? "NULL" : string.Empty
                        );
                    }
                }
                else
                {
                    _logger.LogMessage("Generating Drop and Create for {0}.", table.FullName);
                    builder.AppendFormat(@"IF OBJECT_ID('{0}') IS NOT NULL DROP TABLE {0}; 
                                        GO
                                        ", table.FullName);
                    builder.AppendFormat(@"CREATE TABLE {0} ( {1} ) 
                                  GO
                                    ", table.FullName, columnBuilder.ToString());
                }
            }
            else
            {
                _logger.LogMessage("Generating Create for {0}.", table.FullName);
                builder.AppendFormat(@"CREATE TABLE {0} ( {1} ) 
                                  GO
                                    ", table.FullName, columnBuilder.ToString());
            }



            return new ChangeScript()
            {
                Name = table.Name,
                Schema = table.Schema,
                RelatedTables = new[] { table },
                ScriptText = builder.ToString(),
                ScriptType = ScriptType.Table,
                State = table.State
            };
        }

    }
}
