using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagCleanup.Data.Tables;
using System.Reflection;
using System.Data.Linq.Mapping;
using log4net;
using MySql.Data.Entity;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.ComponentModel.DataAnnotations;

namespace TagCleanup.Data
{
    public class MySQLContext : DbContext
    {
        ILog Logger { get; set; }

        public MySQLContext(ILog logger) : this(logger, "MySQL")
        {
        }

        public MySQLContext(ILog logger, string connectionStringName) :
            base(connectionStringName)
        {
            this.Logger = logger;
            this.CreateTablesIfNotExist();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Database.Connection.State != System.Data.ConnectionState.Closed)
                {
                    Database.Connection.Close();
                    Database.Connection.Dispose();
                }
            }
            catch
            {

            }

            base.Dispose(disposing);
        }

        protected void CreateTablesIfNotExist()
        {
            if (Globals.TablesChecked)
            {
                return;
            }

            if (Globals.VerboseLogging)
            {
                Logger.Info("Checking database tables.");
            }

            System.Data.Common.DbConnectionStringBuilder builder = new System.Data.Common.DbConnectionStringBuilder();
            builder.ConnectionString = Database.Connection.ConnectionString;

            string databaseName = builder["Database"] as string;

            var tables = Assembly.GetExecutingAssembly().GetTypes()
                            .Where(t => t.Namespace == "TagCleanup.Data.Tables")
                            .ToList();

            foreach (var table in tables)
            {
                string tableName = "";
                StringBuilder tableSQL = new StringBuilder();
                List<string> tableIndexes = new List<string>();

                var tableAttibutes = table.GetCustomAttributes(typeof(TableAttribute), true);

                foreach (TableAttribute tableAttribute in tableAttibutes)
                {
                    if (tableAttribute != null)
                    {
                        tableName = tableAttribute.Name;
                    }
                }

                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Checking database table '{tableName}'.");
                    }

                    tableSQL.Append("CREATE TABLE IF NOT EXISTS `");
                    tableSQL.Append(databaseName);
                    tableSQL.Append("`.`");
                    tableSQL.Append(tableName);
                    tableSQL.Append("` ( ");

                    var tableProperties = this.Set(table).ElementType.GetProperties();
                    bool additionalColumn = false;

                    foreach (var tableProperty in tableProperties)
                    {
                        var columnAttribute = (ColumnAttribute)tableProperty.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();
                        var stringLengthAttribute = (StringLengthAttribute)tableProperty.GetCustomAttributes(typeof(StringLengthAttribute), true).FirstOrDefault();
                        var indexAttributes = tableProperty.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.IndexAttribute), true).Cast<System.ComponentModel.DataAnnotations.Schema.IndexAttribute>().ToList();

                        if (columnAttribute != null)
                        {
                            if (additionalColumn)
                            {
                                tableSQL.Append(", ");
                            }

                            string columnName = columnAttribute.Name;
                            string columnType = columnAttribute.DbType;
                            bool primaryKey = columnAttribute.IsPrimaryKey;

                            if (columnType == "INTEGER")
                            {
                                columnType = "INT";
                            }

                            tableSQL.Append($"`{columnName}` ");
                            tableSQL.Append(columnType);

                            if (stringLengthAttribute != null)
                            {
                                tableSQL.Append($"({stringLengthAttribute.MaximumLength.ToString()})");
                            }

                            if (primaryKey)
                            {
                                tableSQL.Append(" NOT NULL AUTO_INCREMENT");
                                tableIndexes.Add($"PRIMARY KEY (`{columnName}`)");
                            }
                            else
                            {
                                tableSQL.Append(" NULL");
                            }

                            additionalColumn = true;

                            if (indexAttributes.Any())
                            {
                                var indexNumber = 0;

                                foreach (var index in indexAttributes)
                                {
                                    if (index.IsUnique)
                                    {
                                        tableIndexes.Add($"UNIQUE INDEX `{columnName}_UNIQUE` (`{columnName}` ASC) VISIBLE");
                                    }
                                    else
                                    {
                                        tableIndexes.Add($"INDEX `{columnName}_{indexNumber}` (`{columnName}` ASC) VISIBLE");
                                    }

                                    indexNumber++;
                                }
                            }
                        }
                    }

                    foreach(var index in tableIndexes)
                    {
                        tableSQL.Append(", ");
                        tableSQL.Append(index);
                    }

                    tableSQL.Append(" ) ");

                    var result = ExecuteQuery(tableSQL.ToString());
                }
                else
                {
                    throw new Exception("Table does not have a name defined in the custom attribute.");
                }
            }

            Globals.TablesChecked = true;
        }

        public void DropTablesIfExist()
        {
            if (Globals.VerboseLogging)
            {
                Logger.Info("Dropping database tables.");
            }

            System.Data.Common.DbConnectionStringBuilder builder = new System.Data.Common.DbConnectionStringBuilder();
            builder.ConnectionString = Database.Connection.ConnectionString;

            string databaseName = builder["Database"] as string;

            var tables = Assembly.GetExecutingAssembly().GetTypes()
                            .Where(t => t.Namespace == "TagCleanup.Data.Tables")
                            .ToList();

            foreach (var table in tables)
            {
                string tableName = "";
                StringBuilder tableSQL = new StringBuilder();
                List<string> tableIndexes = new List<string>();

                var tableAttibutes = table.GetCustomAttributes(typeof(TableAttribute), true);

                foreach (TableAttribute tableAttribute in tableAttibutes)
                {
                    if (tableAttribute != null)
                    {
                        tableName = tableAttribute.Name;
                    }
                }

                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Dropping database table '{tableName}'.");
                    }

                    tableSQL.Append("DROP TABLE IF EXISTS `");
                    tableSQL.Append(databaseName);
                    tableSQL.Append("`.`");
                    tableSQL.Append(tableName);
                    tableSQL.Append("`; ");

                    var result = ExecuteQuery(tableSQL.ToString());
                }
                else
                {
                    throw new Exception("Table does not have a name defined in the custom attribute.");
                }
            }
        }

        public int ExecuteQuery(string sql)
        {
            int queryReturn = -1;

            if (Database.Connection.State != System.Data.ConnectionState.Open)
            {
                Database.Connection.Open();
            }

            using (MySqlCommand command = new MySqlCommand(sql, (MySqlConnection)Database.Connection))
            {
                queryReturn = command.ExecuteNonQuery();
            }

            Database.Connection.Close();

            return queryReturn;
        }

        public DbSet<MediaFiles> MediaFiles { get; set; }
        public DbSet<Albums> Albums { get; set; }
        public DbSet<Scans> Scans { get; set; }
    }
}