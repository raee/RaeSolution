using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Rae.Data.Cache;
using Rae.Data.Dapper;

namespace Rae.Data
{
    public abstract class DataContext
    {
        #region 字段

        private int _timeout = 3000;

        #endregion

        #region 属性

        /// <summary>
        /// 连接超时
        /// </summary>
        public int TimeOut
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <summary>
        /// 缓存接口
        /// </summary>
        public ICache Cache { get; set; }

        #endregion

        #region 抽象

        public abstract string ConnectionString { get; set; }

        public abstract IDbConnection Connection { get; }

        #endregion

        private void Open()
        {
            if (Connection.State == ConnectionState.Closed)
            {
                Connection.Open();
            }
        }

        private void Close()
        {
            Connection.Close();
        }

        public bool Insert(object model)
        {
            Open();
            Type type = model.GetType();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0}()VALUES()", type.Name);

            string fields = string.Empty;
            string values = string.Empty;

            var properties = type.GetProperties(); // 属性
            foreach (var property in properties)
            {
                object value = property.GetValue(model, null);
                if (value != null && !string.IsNullOrEmpty(value.ToString()))
                {

                }
            }

            Connection.Execute(sb.ToString());

        }
    }
}
