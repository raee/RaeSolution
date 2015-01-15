using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Rae.Data.Dapper
{
    /// <summary>
    ///     A bag of parameters that can be passed to the Dapper Query and Execute methods
    /// </summary>
    internal class DynamicParameters : SqlMapper.IDynamicParameters, SqlMapper.IParameterLookup
    {
        internal const DbType EnumerableMultiParameter = (DbType) (-1);

        private static readonly Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> paramReaderCache =
            new Dictionary<SqlMapper.Identity, Action<IDbCommand, object>>();

        private readonly Dictionary<string, ParamInfo> parameters = new Dictionary<string, ParamInfo>();
        private List<object> templates;

        /// <summary>
        ///     construct a dynamic parameter bag
        /// </summary>
        public DynamicParameters()
        {
            RemoveUnused = true;
        }

        /// <summary>
        ///     construct a dynamic parameter bag
        /// </summary>
        /// <param name="template">can be an anonymous type or a DynamicParameters bag</param>
        public DynamicParameters(object template)
        {
            RemoveUnused = true;
            AddDynamicParams(template);
        }

        /// <summary>
        ///     If true, the command-text is inspected and only values that are clearly used are included on the connection
        /// </summary>
        public bool RemoveUnused { get; set; }

        /// <summary>
        ///     All the names of the param in the bag, use Get to yank them out
        /// </summary>
        public IEnumerable<string> ParameterNames
        {
            get { return parameters.Select(p => p.Key); }
        }

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            AddParameters(command, identity);
        }

        object SqlMapper.IParameterLookup.this[string member]
        {
            get
            {
                ParamInfo param;
                return parameters.TryGetValue(member, out param) ? param.Value : null;
            }
        }

        /// <summary>
        ///     Append a whole object full of params to the dynamic
        ///     EG: AddDynamicParams(new {A = 1, B = 2}) // will add property A and B to the dynamic
        /// </summary>
        /// <param name="param"></param>
        public void AddDynamicParams(
#if CSHARP30
object param
#else
            dynamic param
#endif
            )
        {
            var obj = param as object;
            if (obj != null)
            {
                var subDynamic = obj as DynamicParameters;
                if (subDynamic == null)
                {
                    var dictionary = obj as IEnumerable<KeyValuePair<string, object>>;
                    if (dictionary == null)
                    {
                        templates = templates ?? new List<object>();
                        templates.Add(obj);
                    }
                    else
                    {
                        foreach (var kvp in dictionary)
                        {
#if CSHARP30
                            Add(kvp.Key, kvp.Value, null, null, null);
#else
                            Add(kvp.Key, kvp.Value);
#endif
                        }
                    }
                }
                else
                {
                    if (subDynamic.parameters != null)
                    {
                        foreach (var kvp in subDynamic.parameters)
                        {
                            parameters.Add(kvp.Key, kvp.Value);
                        }
                    }

                    if (subDynamic.templates != null)
                    {
                        templates = templates ?? new List<object>();
                        foreach (object t in subDynamic.templates)
                        {
                            templates.Add(t);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Add a parameter to this dynamic parameter list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="dbType"></param>
        /// <param name="direction"></param>
        /// <param name="size"></param>
        public void Add(
#if CSHARP30
string name, object value, DbType? dbType, ParameterDirection? direction, int? size
#else
            string name, object value = null, DbType? dbType = null, ParameterDirection? direction = null,
            int? size = null
#endif
            )
        {
            parameters[Clean(name)] = new ParamInfo
            {
                Name = name,
                Value = value,
                ParameterDirection = direction ?? ParameterDirection.Input,
                DbType = dbType,
                Size = size
            };
        }

        private static string Clean(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                switch (name[0])
                {
                    case '@':
                    case ':':
                    case '?':
                        return name.Substring(1);
                }
            }
            return name;
        }

        /// <summary>
        ///     Add all the parameters needed to the command just before it executes
        /// </summary>
        /// <param name="command">The raw command prior to execution</param>
        /// <param name="identity">Information about the query</param>
        protected void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            IList<SqlMapper.LiteralToken> literals = SqlMapper.GetLiteralTokens(identity.sql);
            if (templates != null)
            {
                foreach (object template in templates)
                {
                    SqlMapper.Identity newIdent = identity.ForDynamicParameters(template.GetType());
                    Action<IDbCommand, object> appender;

                    lock (paramReaderCache)
                    {
                        if (!paramReaderCache.TryGetValue(newIdent, out appender))
                        {
                            appender = SqlMapper.CreateParamInfoGenerator(newIdent, true, RemoveUnused, literals);
                            paramReaderCache[newIdent] = appender;
                        }
                    }

                    appender(command, template);
                }
            }

            foreach (ParamInfo param in parameters.Values)
            {
                DbType? dbType = param.DbType;
                object val = param.Value;
                string name = Clean(param.Name);
                bool isCustomQueryParameter = val is SqlMapper.ICustomQueryParameter;

                SqlMapper.ITypeHandler handler = null;
                if (dbType == null && val != null && !isCustomQueryParameter)
                    dbType = SqlMapper.LookupDbType(val.GetType(), name, out handler);
                if (dbType == EnumerableMultiParameter)
                {
#pragma warning disable 612, 618
                    SqlMapper.PackListParameters(command, name, val);
#pragma warning restore 612, 618
                }
                else if (isCustomQueryParameter)
                {
                    ((SqlMapper.ICustomQueryParameter) val).AddParameter(command, name);
                }
                else
                {
                    bool add = !command.Parameters.Contains(name);
                    IDbDataParameter p;
                    if (add)
                    {
                        p = command.CreateParameter();
                        p.ParameterName = name;
                    }
                    else
                    {
                        p = (IDbDataParameter) command.Parameters[name];
                    }

                    p.Direction = param.ParameterDirection;
                    if (handler == null)
                    {
                        p.Value = val ?? DBNull.Value;
                        if (dbType != null && p.DbType != dbType)
                        {
                            p.DbType = dbType.Value;
                        }
                        var s = val as string;
                        if (s != null)
                        {
                            if (s.Length <= 4000)
                            {
                                p.Size = 4000;
                            }
                        }
                        if (param.Size != null)
                        {
                            p.Size = param.Size.Value;
                        }
                    }
                    else
                    {
                        if (dbType != null) p.DbType = dbType.Value;
                        if (param.Size != null) p.Size = param.Size.Value;
                        handler.SetValue(p, val ?? DBNull.Value);
                    }

                    if (add)
                    {
                        command.Parameters.Add(p);
                    }
                    param.AttachedParam = p;
                }
            }
            // note: most non-priveleged implementations would use: this.ReplaceLiterals(command);
            if (literals.Count != 0) SqlMapper.ReplaceLiterals(this, command, literals);
        }


        /// <summary>
        ///     Get the value of a parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns>The value, note DBNull.Value is not returned, instead the value is returned as null</returns>
        public T Get<T>(string name)
        {
            object val = parameters[Clean(name)].AttachedParam.Value;
            if (val == DBNull.Value)
            {
                if (default(T) != null)
                {
                    throw new ApplicationException("Attempting to cast a DBNull to a non nullable type!");
                }
                return default(T);
            }
            return (T) val;
        }

        private class ParamInfo
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public ParameterDirection ParameterDirection { get; set; }
            public DbType? DbType { get; set; }
            public int? Size { get; set; }
            public IDbDataParameter AttachedParam { get; set; }
        }
    }
}