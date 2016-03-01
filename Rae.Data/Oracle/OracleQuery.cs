using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Rae.Data.Dapper;
using Rae.Data.Mapping;

namespace Rae.Data.Oracle
{
    class OracleQuery<T> : IQuery<T> where T : class
    {
        private readonly IDbConnection _db;
        private readonly IMapping _map;

        public OracleQuery(IDbConnection db, IMapping map)
        {
            _db = db;
            _map = map;
        }

        public List<T> Select()
        {
            return _db.Query<T>(string.Format("SELECT * FROM {0}", _map.TableName)).ToList();
        }

        public List<T> Select(string sql, object param = null)
        {
            return _db.Query<T>(sql, param).ToList();
        }

        public T Add(T m)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0}(", _map.TableName);
            dynamic  param = new {};
            foreach (Column column in _map.GetColumnInfos())
            {
                sb.AppendFormat("{0}", column.Name);
               param
            }

        }

        public bool Delete(T m)
        {
            throw new NotImplementedException();
        }

        public bool Update(T m)
        {
            throw new NotImplementedException();
        }

        public T Add(IEnumerable<T> m)
        {
            throw new NotImplementedException();
        }

        public bool Delete(IEnumerable<T> m)
        {
            throw new NotImplementedException();
        }

        public bool Update(IEnumerable<T> m)
        {
            throw new NotImplementedException();
        }
    }
}
