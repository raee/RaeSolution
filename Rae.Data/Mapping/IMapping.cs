using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rae.Data.Mapping
{
    /// <summary>
    /// 实体映射接口
    /// 作者:ChenRui
    /// 时间:2015-04-23
    /// </summary>
    internal interface IMapping
    {
        string TableName { get; set; }

        List<Column> GetColumnInfos();
    }
}
