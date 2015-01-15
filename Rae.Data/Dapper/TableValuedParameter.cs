using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace Rae.Data.Dapper
{
    /// <summary>
    ///     Used to pass a DataTable as a TableValuedParameter
    /// </summary>
    internal sealed class TableValuedParameter : SqlMapper.ICustomQueryParameter
    {
        private static readonly Action<SqlParameter, string> setTypeName;
        private readonly DataTable table;
        private readonly string typeName;

        static TableValuedParameter()
        {
            PropertyInfo prop = typeof (SqlParameter).GetProperty("TypeName",
                BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof (string) && prop.CanWrite)
            {
                setTypeName = (Action<SqlParameter, string>)
                    Delegate.CreateDelegate(typeof (Action<SqlParameter, string>), prop.GetSetMethod());
            }
        }

        /// <summary>
        ///     Create a new instance of TableValuedParameter
        /// </summary>
        public TableValuedParameter(DataTable table) : this(table, null)
        {
        }

        /// <summary>
        ///     Create a new instance of TableValuedParameter
        /// </summary>
        public TableValuedParameter(DataTable table, string typeName)
        {
            this.table = table;
            this.typeName = typeName;
        }

        void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
        {
            IDbDataParameter param = command.CreateParameter();
            param.ParameterName = name;
            Set(param, table, typeName);
            command.Parameters.Add(param);
        }

        internal static void Set(IDbDataParameter parameter, DataTable table, string typeName)
        {
            parameter.Value = (object) table ?? DBNull.Value;
            if (string.IsNullOrEmpty(typeName) && table != null)
            {
                typeName = table.GetTypeName();
            }
            if (!string.IsNullOrEmpty(typeName))
            {
                var sqlParam = parameter as SqlParameter;
                if (sqlParam != null)
                {
                    if (setTypeName != null) setTypeName(sqlParam, typeName);
                    sqlParam.SqlDbType = SqlDbType.Structured;
                }
            }
        }
    }
}