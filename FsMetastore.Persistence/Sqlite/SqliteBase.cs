using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace FsMetastore.Persistence.Sqlite
{
    abstract class SqliteBase
    {
        protected SqliteConnection _connection;
        protected string _filePath;

        protected void DeleteDb()
        {
            _connection?.Close();
            if(File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }

        protected void OpenConnection()
        {
            var filePath = _filePath;
            _connection = new SqliteConnection($"Data Source={filePath}");
            _connection.Open();
            //We now require WAL journal mode because of MetaEnumeratorHelper
            //WAL is required to enable simultaneous readers and writers
            Execute("PRAGMA main.journal_mode=WAL");
        }

        protected void EnsureTableExists(string dataTableName, string columnDdl)
        {
            if (!IsTableExists(dataTableName))
            {
                var command = _connection.CreateCommand();
                command.CommandText = $"CREATE TABLE {dataTableName}{columnDdl};";
                command.ExecuteNonQuery();
            }
        }

        protected void CleanTable(string dataTableName)
        {
            if (IsTableExists(dataTableName))
            {
                var command = _connection.CreateCommand();
                command.CommandText = $"DELETE FROM {dataTableName}";
                command.ExecuteNonQuery();
            }
        }

        private IEnumerable<string> GetIndexesOn(string tableName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT name FROM sqlite_master WHERE type= 'index' and tbl_name='{tableName}';";
                var result = command.ExecuteReader(CommandBehavior.SequentialAccess);
                while (result.Read())
                {
                    yield return result[0] as string;
                }
            }
        }

        private bool IsTableExists(string tableName)
        {
            var res = ExecuteScalar($"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='{tableName}';");
            return ScalarToLong(res) > 0;
        }

        
        protected int MaxId(string dataTableName)
        {
            var maxIdObj = ExecuteScalar($"SELECT max(id) FROM {dataTableName}");
            return ScalarToInt(maxIdObj) ?? 0;
        }

        private int? ScalarToInt(object obj)
        {
            if(obj == null || obj == DBNull.Value)
            {
                return null;
            }
            if(obj is long lval)
            {
                return (int) lval;
            }
            if(obj is int ival)
            {
                return (int) ival;
            }
            throw new ApplicationException($"Unknown scalar type {obj.GetType()}");
        }
        private long? ScalarToLong(object obj)
        {
            if(obj == null || obj == DBNull.Value)
            {
                return null;
            }
            if(obj is long lval)
            {
                return lval;
            }
            if(obj is int ival)
            {
                return ival;
            }
            throw new ApplicationException($"Unknown scalar type {obj.GetType()}");
        }

        protected void DropAllIndexes(string tableName)
        {
            foreach(var indexName in GetIndexesOn(tableName).ToList())
            {
                DropIndex(indexName);
            }
        }
        private void DropIndex(string indexName)
        {
            Execute($"DROP INDEX [IF EXISTS] {indexName};");
        }

        protected void EnsureIndex(bool unique, string indexName, string tableName, string columnsAndConditions)
        {
            //CREATE [UNIQUE] INDEX [IF NOT EXISTS] index_name ON table_name (column1 [ASC | DESC], column2 [ASC | DESC], column_n  [ASC | DESC]) [ WHERE conditions ];
            var uniqueness = unique ? "UNIQUE" : "";
            var command = $"CREATE {uniqueness} INDEX IF NOT EXISTS {indexName} ON {tableName} {columnsAndConditions}";
            Execute(command);
        }
        
        public int? ExecuteScalarInt32(string commandText)
        {
            var val = ExecuteScalar(commandText);
            if (val == null || val == DBNull.Value)
            {
                return null;
            }
            if (val is IConvertible convertible)
            {
                return convertible.ToInt32(CultureInfo.InvariantCulture);
            }

            return (int) val;
        }

        private object ExecuteScalar(string commandText)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = commandText;
                return command.ExecuteScalar();
            }
        }

        protected int Execute(string commandText)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = commandText;
                return command.ExecuteNonQuery();
            }
        }


        protected abstract bool ShouldDeleteOnClose();
        
        public void Close()
        {
            Flush();
            if(_connection != null && _connection.State == ConnectionState.Open && ShouldDeleteOnClose())
            {
                DeleteDb();
            }
            else
            {
                _connection?.Close();
            }
        }

        public abstract void Flush();
    }
}