using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace Rae.Data.Oracle
{
    /// <summary>
    /// Oracle连接管理
    /// 作者:ChenRui
    /// 时间:2015-04-23
    /// </summary>
    internal class OracleConnectionManager : IDisposable
    {
        private readonly IDbConnection _connection;

        public string ConnectionString { get; set; }

        public OracleConnectionManager()
        {
            _connection = new OracleConnection(ConnectionString);
        }

        public IDbConnection GetConnection()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }

            return _connection;
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}