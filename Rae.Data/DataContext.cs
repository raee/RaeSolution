using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rae.Data.Cache;

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

        #endregion

        public DataContext()
        {

        }
    }
}
