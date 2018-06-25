using AuditShadowBuilder.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core.Models;

namespace TableHistorySchemaGenerator.Core.Inspectors
{
    public class HistoryTableInspector : IHistoryTableInspector
    {
        private IHistoryCommonConfiguration _historyCommonConfiguration;
        private ILogger _logger;

        public HistoryTableInspector(IHistoryCommonConfiguration historyCommonConfiguration, ILogger logger)
        {
            this._historyCommonConfiguration = historyCommonConfiguration;
            this._logger = logger;
        }

        public DbObject BuildHistoryName(TableDetails table)
        {
            return new DbObject()
            {
                Name = _historyCommonConfiguration.Prefix + table.Name,
                Schema = _historyCommonConfiguration.Schema
            };
        }

        public DbSchemaPackage InspectChanges(DbSchemaPackage existingSchema)
        {

            _logger.LogMessage("Starting to Inspect existing schema for changes.");
            var newSchema = existingSchema.Clone();
            foreach (var table in newSchema.Tables)
            {
                var newHistoryTable = BuildHistoryTable(table);
                if (newHistoryTable != null && newHistoryTable.State != DbObjectState.Same)
                {
                    table.HistoricalTable = newHistoryTable;
                }
            }
            //When history schema doesnt exist, add it
            if (!newSchema.Schemas.Any(c => c.Name == _historyCommonConfiguration.Schema))
            {
                newSchema.Schemas.Add(new SchemaDetails()
                {
                    Name = _historyCommonConfiguration.Schema,
                    Schema = "dbo",
                    State = DbObjectState.New
                });
            }

            _logger.LogMessage("Inspection complete!");
            return newSchema;
        }

        public void SetMaxSize(ColumnDetails column)
        {
            column.SizeDetails = column.SqlType.MaxSize();
            //if then size blah
        }
        public TableDetails BuildHistoryTable(TableDetails table)
        {
            if (table.IsHistorical
               || !table.Columns.Any(c => c.Name == _historyCommonConfiguration.ExpectedCreatedByColumnName)
               || !table.Columns.Any(c => c.Name == _historyCommonConfiguration.ExpectedCreatedTimestampColumnName))
            {
                return null;
            }
            _logger.LogMessage("Inspecting History table for {0}", table.FullName);
            if (table.HistoricalTable != null)
            {

                var historyTable = table.HistoricalTable.Clone();
                historyTable.Name = _historyCommonConfiguration.Prefix + table.Name;
                historyTable.Schema = _historyCommonConfiguration.Schema;
                foreach (var column in table.Columns)
                {
                    var existingHistoryColumn = historyTable.Columns.FirstOrDefault(c => c.Name == column.Name);
                    var desiredHistoryColumn = column.Clone();
                    desiredHistoryColumn.IsNullable = true;
                    if (existingHistoryColumn == null)
                    {
                        desiredHistoryColumn.State = DbObjectState.New;
                        historyTable.Columns.Add(desiredHistoryColumn);
                        historyTable.State = DbObjectState.Modified;
                    }
                    else
                    {
                        //Should there be warnings for smaller sizes?
                        //History should always be same or large

                        //Any size change
                        if (existingHistoryColumn.SqlType != desiredHistoryColumn.SqlType)
                        {
                            var message = string.Format("Error while building {0}{1} history table for column {2} has an invalid type of {3}, expected type of {4}.",
                               table.Schema, table.Name, column.Name, existingHistoryColumn.SqlType, desiredHistoryColumn.SqlType
                            );
                            throw new Exception(message);
                            //TODO maybe a flag per column or an ignore all
                        }
                        else if (existingHistoryColumn.SizeDetails != desiredHistoryColumn.SizeDetails)
                        {
                            this.SetMaxSize(desiredHistoryColumn);
                            desiredHistoryColumn.State = DbObjectState.Modified;
                            historyTable.Columns.Remove(existingHistoryColumn);
                            historyTable.Columns.Add(desiredHistoryColumn);
                            historyTable.State = DbObjectState.Modified;
                        }
                    }
                }
                _logger.LogMessage("- History found {0} and state changed to {1}", historyTable.FullName, historyTable.State.ToString());

                return historyTable;
            }
            else
            {
                var historyTable = table.Clone();

                historyTable.Name = _historyCommonConfiguration.Prefix + historyTable.Name;
                historyTable.Schema = _historyCommonConfiguration.Schema;

                foreach (var column in historyTable.Columns) { column.IsNullable = true; }
                historyTable.IsHistorical = true;
                historyTable.State = DbObjectState.New;
                _logger.LogMessage("- New History table required for {0} of name {1}", table.FullName, historyTable.FullName);
                return historyTable;
            }
        }
        public bool IsHistoryTable(TableDetails table)
        {
            return (table.Schema.ToLower() == _historyCommonConfiguration.Schema.ToLower()
              && table.Name.ToLower().StartsWith(_historyCommonConfiguration.Prefix.ToLower()));
        }
    }
}
