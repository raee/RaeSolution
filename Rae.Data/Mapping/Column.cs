using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Rae.Data.Mapping
{
    public class Column
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public DbType DbType { get; set; }
        public bool IsPrimaryKey { get; set; }

        public Column(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
