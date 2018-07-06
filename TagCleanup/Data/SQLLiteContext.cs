using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagCleanup.Data.Tables;
using System.Reflection;
using System.Data.Linq.Mapping;
using log4net;

//namespace TagCleanup.Data
//{
//    public class SQLLiteContext : DbContext
//    {
//        ILog Logger { get; set; }

//        public static readonly string DefaultDatabaseFile = Path.Combine(Program.ExecutionPath, "mediafiles.db");

//        public SQLLiteContext(ILog logger) : this(logger, DefaultDatabaseFile)
//        {
//        }

//        public SQLLiteContext(ILog logger, string dataSource) :
//            base(new SQLiteConnection()
//            {
//                ConnectionString = new SQLiteConnectionStringBuilder() { DataSource = dataSource, ForeignKeys = true }.ConnectionString
//            }, true)
//        {
//            this.Logger = logger;
//            this.CreateTablesIfNotExist();
//        }

//        protected override void OnModelCreating(DbModelBuilder modelBuilder)
//        {
//            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
//            base.OnModelCreating(modelBuilder);
//        }

//        protected void CreateTablesIfNotExist()
//        {
//            if (Globals.TablesChecked)
//            {
//                return;
//            }

//            if (Globals.VerboseLogging)
//            {
//                Logger.Info("Checking database tables.");
//            }

//            var tables = Assembly.GetExecutingAssembly().GetTypes()
//                            .Where(t => t.Namespace == "TagCleanup.Data.Tables")
//                            .ToList();


//            foreach (var table in tables)
//            {
//                string tableName = "";
//                StringBuilder tableSQL = new StringBuilder();

//                var tableAttibutes = table.GetCustomAttributes(typeof(TableAttribute), true);

//                foreach (TableAttribute tableAttribute in tableAttibutes)
//                {
//                    if (tableAttribute != null)
//                    {
//                        tableName = tableAttribute.Name;
//                    }
//                }

//                if (!string.IsNullOrWhiteSpace(tableName))
//                {
//                    if (Globals.VerboseLogging)
//                    {
//                        Logger.Info($"Checking database table '{tableName}'.");
//                    }

//                    tableSQL.Append("CREATE TABLE IF NOT EXISTS ");
//                    tableSQL.Append(tableName);
//                    tableSQL.Append("( ");

//                    var tableProperties = this.Set(table).ElementType.GetProperties();
//                    bool additionalColumn = false;

//                    foreach (var tableProperty in tableProperties)
//                    {
//                        var columnAttribute = (ColumnAttribute)tableProperty.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();
//                        if (columnAttribute != null)
//                        {
//                            if (additionalColumn)
//                            {
//                                tableSQL.Append(", ");
//                            }

//                            string columnName = columnAttribute.Name;
//                            string columnType = columnAttribute.DbType;
//                            bool primaryKey = columnAttribute.IsPrimaryKey;

//                            tableSQL.Append(columnName);
//                            tableSQL.Append(" ");
//                            tableSQL.Append(columnType);

//                            if (primaryKey)
//                            {
//                                tableSQL.Append(" PRIMARY KEY AUTOINCREMENT UNIQUE");
//                            }
//                            else
//                            {
//                                tableSQL.Append(" NULL");
//                            }

//                            additionalColumn = true;
//                        }
//                    }

//                    tableSQL.Append(" ); ");

//                    ExecuteQuery(tableSQL.ToString());
//                }
//                else
//                {
//                    throw new Exception("Table does not have a name defined in the custom attribute.");
//                }
//            }

//            Globals.TablesChecked = true;
//        }

//        public void ExecuteQuery(string sql)
//        {
//            if (Database.Connection.State != System.Data.ConnectionState.Open)
//            {
//                Database.Connection.Open();
//            }

//            using (SQLiteCommand command = new SQLiteCommand(sql, (SQLiteConnection)Database.Connection))
//            {
//                command.ExecuteNonQuery();
//            }

//            Database.Connection.Close();
//        }

//        public DbSet<MediaFiles> MediaFiles { get; set; }
//        public DbSet<Albums> Albums { get; set; }
//    }
//}