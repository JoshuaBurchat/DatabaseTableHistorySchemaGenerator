using AuditShadowBuilder.Infrastructure.Logging;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core;
using TableHistorySchemaGenerator.Core.Models;

namespace TableHistorySchemaGenerator.App
{
    public class DbScriptDestinationWriter : IScriptDestinationWriter
    {
        public string ConnectionString { get; set; }
        private ILogger _logger;
        public DbScriptDestinationWriter(string connectionString, ILogger logger)
        {
            this.ConnectionString = connectionString;
            this._logger = logger;
        }
        public void Commit(ChangeScript[] scripts)
        {
            _logger.LogMessage("Starting to commit the changes directly to SQL Server.");
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            {
                connection.Open();

                StringBuilder builder = new StringBuilder();

                ScriptType[] scriptTypeOrder = new ScriptType[] { ScriptType.Schema, ScriptType.Table, ScriptType.View, ScriptType.Trigger };
                foreach (var scriptType in scriptTypeOrder)
                {
                    //Tables first
                    foreach (var script in scripts.Where(s => s.ScriptType == scriptType)) builder.AppendLine(script.ScriptText);
                }
                //Other types
                foreach (var script in scripts.Where(s => !scriptTypeOrder.Contains(s.ScriptType))) builder.AppendLine(script.ScriptText);


                Server server = new Server(new ServerConnection(connection));
                try
                {
                    server.ConnectionContext.BeginTransaction();
                    server.ConnectionContext.ExecuteNonQuery(builder.ToString());
                    server.ConnectionContext.CommitTransaction();
                }
                catch
                {
                    _logger.LogMessage("Transaction rolled back due to exception.");
                    server.ConnectionContext.RollBackTransaction();
                    throw;
                }
            }
            _logger.LogMessage("Changes have been committed.");
        }

     
    }
}
