using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rae.Data.Cache
{
    /// <summary>
    /// 缓存接口
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// 缓存超时，默认0为永久保存。单位：秒。
        /// </summary>
        int TimeOut { get; set; }

        /// <summary>
        /// 插入缓存，如果存在则更新缓存。
        /// </summary>
        /// <param name="key">缓存唯一标志</param>
        /// <param name="value">缓存对象</param>
        void Insert(string key, object value);

        /// <summary>
        /// 插入缓存，如果存在则更新缓存，提供过时策略。
        /// </summary>
        /// <param name="key">缓存唯一标志</param>
        /// <param name="value">缓存对象</param>
        /// <param name="timeout">超时</param>
        void Insert(string key, object value, int timeout);

        /// <summary>
        /// 缓存是否存在
        /// </summary>
        /// <param name="key">缓存唯一标志</param>
        /// <returns></returns>
        bool Exist(string key);

        /// <summary>
        /// 从缓存中移除
        /// </summary>
        /// <param name="key">唯一标志</param>
        void Remove(string key);

        /// <summary>
        /// 移除所有缓存
        /// </summary>
        void RemoveAll();

    }
}
