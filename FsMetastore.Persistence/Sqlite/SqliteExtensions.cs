using System;
using Microsoft.Data.Sqlite;

namespace FsMetastore.Persistence.Sqlite
{
    public static class SqliteExtensions
    {
        public static void AddNamedParameter(this SqliteCommand command, string parameterName)
        {
            var idParam = command.CreateParameter();
            idParam.ParameterName = parameterName;
            command.Parameters.Add(idParam);
        }
        
        public static void SetNamedParameter<T>(this SqliteCommand command, string parameterName, T parameterValue)
        {
            command.Parameters[parameterName].Value = parameterValue == null ? (object)DBNull.Value : parameterValue;
        }

        public static void AddNamedParameter<T>(this SqliteCommand command, string parameterName, T parameterValue)
        {
            var idParam = command.CreateParameter();
            idParam.ParameterName = parameterName;
            command.Parameters.Add(idParam);
            idParam.Value = parameterValue == null ? (object)DBNull.Value : parameterValue;
        }
    }
}