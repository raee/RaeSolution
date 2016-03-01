using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rae.Data
{
    public interface IQuery<T> where T : class
    {
        #region 查询操作

        List<T> Select();

        List<T> Select(string sql, object param = null);

        #endregion

        #region 单实体

        /// <summary>
        /// 添加实体
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns>`0.</returns>
        T Add(T m);

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool Delete(T m);

        /// <summary>
        /// 更新实体
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool Update(T m);

        #endregion

        #region 实体批量操作
        /// <summary>
        /// 添加实体
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns>`0.</returns>
        T Add(IEnumerable<T> m);

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool Delete(IEnumerable<T> m);

        /// <summary>
        /// 更新实体
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool Update(IEnumerable<T> m);
        #endregion
    }
}
