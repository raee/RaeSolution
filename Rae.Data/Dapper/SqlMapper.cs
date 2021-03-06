/*
 License: http://www.apache.org/licenses/LICENSE-2.0 
 Home page: http://code.google.com/p/dapper-dot-net/

 Note: to build on C# 3.0 + .NET 3.5, include the CSHARP30 compiler symbol (and yes,
 I know the difference between language and runtime versions; this is a compromise).
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Rae.Data.Dapper
{
    /// <summary>
    ///     Dapper, a light weight object mapper for ADO.NET
    /// </summary>
    public static class SqlMapper
    {
        /// <summary>
        ///     Implement this interface to pass an arbitrary db specific set of parameters to Dapper
        /// </summary>
        public interface IDynamicParameters
        {
            /// <summary>
            ///     Add all the parameters needed to the command just before it executes
            /// </summary>
            /// <param name="command">The raw command prior to execution</param>
            /// <param name="identity">Information about the query</param>
            void AddParameters(IDbCommand command, Identity identity);
        }

        /// <summary>
        ///     Extends IDynamicParameters providing by-name lookup of parameter values
        /// </summary>
        public interface IParameterLookup : IDynamicParameters
        {
            /// <summary>
            ///     Get the value of the specified parameter (return null if not found)
            /// </summary>
            object this[string name] { get; }
        }

        /// <summary>
        ///     Implement this interface to pass an arbitrary db specific parameter to Dapper
        /// </summary>
        [AssemblyNeutral]
        public interface ICustomQueryParameter
        {
            /// <summary>
            ///     Add the parameter needed to the command before it executes
            /// </summary>
            /// <param name="command">The raw command prior to execution</param>
            /// <param name="name">Parameter name</param>
            void AddParameter(IDbCommand command, string name);
        }

        /// <summary>
        ///     Implement this interface to perform custom type-based parameter handling and value parsing
        /// </summary>
        [AssemblyNeutral]
        public interface ITypeHandler
        {
            /// <summary>
            ///     Assign the value of a parameter before a command executes
            /// </summary>
            /// <param name="parameter">The parameter to configure</param>
            /// <param name="value">Parameter value</param>
            void SetValue(IDbDataParameter parameter, object value);

            /// <summary>
            ///     Parse a database value back to a typed value
            /// </summary>
            /// <param name="value">The value from the database</param>
            /// <param name="destinationType">The type to parse to</param>
            /// <returns>The typed value</returns>
            object Parse(Type destinationType, object value);
        }

        /// <summary>
        ///     Base-class for simple type-handlers
        /// </summary>
        public abstract class TypeHandler<T> : ITypeHandler
        {
            void ITypeHandler.SetValue(IDbDataParameter parameter, object value)
            {
                if (value is DBNull)
                {
                    parameter.Value = value;
                }
                else
                {
                    SetValue(parameter, (T) value);
                }
            }

            object ITypeHandler.Parse(Type destinationType, object value)
            {
                return Parse(value);
            }

            /// <summary>
            ///     Assign the value of a parameter before a command executes
            /// </summary>
            /// <param name="parameter">The parameter to configure</param>
            /// <param name="value">Parameter value</param>
            public abstract void SetValue(IDbDataParameter parameter, T value);

            /// <summary>
            ///     Parse a database value back to a typed value
            /// </summary>
            /// <param name="value">The value from the database</param>
            /// <returns>The typed value</returns>
            public abstract T Parse(object value);
        }

        /// <summary>
        ///     Implement this interface to change default mapping of reader columns to type memebers
        /// </summary>
        public interface ITypeMap
        {
            /// <summary>
            ///     Finds best constructor
            /// </summary>
            /// <param name="names">DataReader column names</param>
            /// <param name="types">DataReader column types</param>
            /// <returns>Matching constructor or default one</returns>
            ConstructorInfo FindConstructor(string[] names, Type[] types);

            /// <summary>
            ///     Gets mapping for constructor parameter
            /// </summary>
            /// <param name="constructor">Constructor to resolve</param>
            /// <param name="columnName">DataReader column name</param>
            /// <returns>Mapping implementation</returns>
            IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName);

            /// <summary>
            ///     Gets member mapping for column
            /// </summary>
            /// <param name="columnName">DataReader column name</param>
            /// <returns>Mapping implementation</returns>
            IMemberMap GetMember(string columnName);
        }

        /// <summary>
        ///     Implements this interface to provide custom member mapping
        /// </summary>
        public interface IMemberMap
        {
            /// <summary>
            ///     Source DataReader column name
            /// </summary>
            string ColumnName { get; }

            /// <summary>
            ///     Target member type
            /// </summary>
            Type MemberType { get; }

            /// <summary>
            ///     Target property
            /// </summary>
            PropertyInfo Property { get; }

            /// <summary>
            ///     Target field
            /// </summary>
            FieldInfo Field { get; }

            /// <summary>
            ///     Target constructor parameter
            /// </summary>
            ParameterInfo Parameter { get; }
        }

        /// <summary>
        ///     This is a micro-cache; suitable when the number of terms is controllable (a few hundred, for example),
        ///     and strictly append-only; you cannot change existing values. All key matches are on **REFERENCE**
        ///     equality. The type is fully thread-safe.
        /// </summary>
        internal class Link<TKey, TValue> where TKey : class
        {
            private Link(TKey key, TValue value, Link<TKey, TValue> tail)
            {
                Key = key;
                Value = value;
                Tail = tail;
            }

            public TKey Key { get; private set; }
            public TValue Value { get; private set; }
            public Link<TKey, TValue> Tail { get; private set; }

            public static bool TryGet(Link<TKey, TValue> link, TKey key, out TValue value)
            {
                while (link != null)
                {
                    if (key == link.Key)
                    {
                        value = link.Value;
                        return true;
                    }
                    link = link.Tail;
                }
                value = default(TValue);
                return false;
            }

            public static bool TryAdd(ref Link<TKey, TValue> head, TKey key, ref TValue value)
            {
                bool tryAgain;
                do
                {
                    Link<TKey, TValue> snapshot = Interlocked.CompareExchange(ref head, null, null);
                    TValue found;
                    if (TryGet(snapshot, key, out found))
                    {
                        // existing match; report the existing value instead
                        value = found;
                        return false;
                    }
                    var newNode = new Link<TKey, TValue>(key, value, snapshot);
                    // did somebody move our cheese?
                    tryAgain = Interlocked.CompareExchange(ref head, newNode, snapshot) != snapshot;
                } while (tryAgain);
                return true;
            }
        }

        private class CacheInfo
        {
            private int hitCount;
            public DeserializerState Deserializer { get; set; }
            public Func<IDataReader, object>[] OtherDeserializers { get; set; }
            public Action<IDbCommand, object> ParamReader { get; set; }

            public int GetHitCount()
            {
                return Interlocked.CompareExchange(ref hitCount, 0, 0);
            }

            public void RecordHit()
            {
                Interlocked.Increment(ref hitCount);
            }
        }

        private static int GetColumnHash(IDataReader reader)
        {
            unchecked
            {
                int colCount = reader.FieldCount, hash = colCount;
                for (int i = 0; i < colCount; i++)
                {
                    // binding code is only interested in names - not types
                    object tmp = reader.GetName(i);
                    hash = (hash*31) + (tmp == null ? 0 : tmp.GetHashCode());
                }
                return hash;
            }
        }

        private struct DeserializerState
        {
            public readonly Func<IDataReader, object> Func;
            public readonly int Hash;

            public DeserializerState(int hash, Func<IDataReader, object> func)
            {
                Hash = hash;
                Func = func;
            }
        }

        /// <summary>
        ///     Called if the query cache is purged via PurgeQueryCache
        /// </summary>
        public static event EventHandler QueryCachePurged;

        private static void OnQueryCachePurged()
        {
            EventHandler handler = QueryCachePurged;
            if (handler != null) handler(null, EventArgs.Empty);
        }

#if CSHARP30
        private static readonly Dictionary<Identity, CacheInfo> _queryCache = new Dictionary<Identity, CacheInfo>();
        // note: conflicts between readers and writers are so short-lived that it isn't worth the overhead of
        // ReaderWriterLockSlim etc; a simple lock is faster
        private static void SetQueryCache(Identity key, CacheInfo value)
        {
            lock (_queryCache) { _queryCache[key] = value; }
        }
        private static bool TryGetQueryCache(Identity key, out CacheInfo value)
        {
            lock (_queryCache) { return _queryCache.TryGetValue(key, out value); }
        }
        private static void PurgeQueryCacheByType(Type type)
        {
            lock (_queryCache)
            {
                var toRemove = _queryCache.Keys.Where(id => id.type == type).ToArray();
                foreach (var key in toRemove)
                    _queryCache.Remove(key);
            }
        }
        /// <summary>
        /// Purge the query cache 
        /// </summary>
        public static void PurgeQueryCache()
        {
            lock (_queryCache)
            {
                _queryCache.Clear();
            }
            OnQueryCachePurged();
        }
#else
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Identity, CacheInfo> _queryCache =
            new System.Collections.Concurrent.ConcurrentDictionary<Identity, CacheInfo>();

        private static void SetQueryCache(Identity key, CacheInfo value)
        {
            if (Interlocked.Increment(ref collect) == COLLECT_PER_ITEMS)
            {
                CollectCacheGarbage();
            }
            _queryCache[key] = value;
        }

        private static void CollectCacheGarbage()
        {
            try
            {
                foreach (var pair in _queryCache)
                {
                    if (pair.Value.GetHitCount() <= COLLECT_HIT_COUNT_MIN)
                    {
                        CacheInfo cache;
                        _queryCache.TryRemove(pair.Key, out cache);
                    }
                }
            }

            finally
            {
                Interlocked.Exchange(ref collect, 0);
            }
        }

        private const int COLLECT_PER_ITEMS = 1000, COLLECT_HIT_COUNT_MIN = 0;
        private static int collect;

        private static bool TryGetQueryCache(Identity key, out CacheInfo value)
        {
            if (_queryCache.TryGetValue(key, out value))
            {
                value.RecordHit();
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        ///     Purge the query cache
        /// </summary>
        public static void PurgeQueryCache()
        {
            _queryCache.Clear();
            OnQueryCachePurged();
        }

        private static void PurgeQueryCacheByType(Type type)
        {
            foreach (var entry in _queryCache)
            {
                CacheInfo cache;
                if (entry.Key.type == type)
                    _queryCache.TryRemove(entry.Key, out cache);
            }
        }

        /// <summary>
        ///     Return a count of all the cached queries by dapper
        /// </summary>
        /// <returns></returns>
        public static int GetCachedSQLCount()
        {
            return _queryCache.Count;
        }

        /// <summary>
        ///     Return a list of all the queries cached by dapper
        /// </summary>
        /// <param name="ignoreHitCountAbove"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, string, int>> GetCachedSQL(int ignoreHitCountAbove = int.MaxValue)
        {
            IEnumerable<Tuple<string, string, int>> data =
                _queryCache.Select(
                    pair => Tuple.Create(pair.Key.connectionString, pair.Key.sql, pair.Value.GetHitCount()));
            if (ignoreHitCountAbove < int.MaxValue) data = data.Where(tuple => tuple.Item3 <= ignoreHitCountAbove);
            return data;
        }

        /// <summary>
        ///     Deep diagnostics only: find any hash collisions in the cache
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Tuple<int, int>> GetHashCollissions()
        {
            var counts = new Dictionary<int, int>();
            foreach (Identity key in _queryCache.Keys)
            {
                int count;
                if (!counts.TryGetValue(key.hashCode, out count))
                {
                    counts.Add(key.hashCode, 1);
                }
                else
                {
                    counts[key.hashCode] = count + 1;
                }
            }
            return from pair in counts
                where pair.Value > 1
                select Tuple.Create(pair.Key, pair.Value);
        }
#endif


        private static Dictionary<Type, DbType> typeMap;

        static SqlMapper()
        {
            typeMap = new Dictionary<Type, DbType>();
            typeMap[typeof (byte)] = DbType.Byte;
            typeMap[typeof (sbyte)] = DbType.SByte;
            typeMap[typeof (short)] = DbType.Int16;
            typeMap[typeof (ushort)] = DbType.UInt16;
            typeMap[typeof (int)] = DbType.Int32;
            typeMap[typeof (uint)] = DbType.UInt32;
            typeMap[typeof (long)] = DbType.Int64;
            typeMap[typeof (ulong)] = DbType.UInt64;
            typeMap[typeof (float)] = DbType.Single;
            typeMap[typeof (double)] = DbType.Double;
            typeMap[typeof (decimal)] = DbType.Decimal;
            typeMap[typeof (bool)] = DbType.Boolean;
            typeMap[typeof (string)] = DbType.String;
            typeMap[typeof (char)] = DbType.StringFixedLength;
            typeMap[typeof (Guid)] = DbType.Guid;
            typeMap[typeof (DateTime)] = DbType.DateTime;
            typeMap[typeof (DateTimeOffset)] = DbType.DateTimeOffset;
            typeMap[typeof (TimeSpan)] = DbType.Time;
            typeMap[typeof (byte[])] = DbType.Binary;
            typeMap[typeof (byte?)] = DbType.Byte;
            typeMap[typeof (sbyte?)] = DbType.SByte;
            typeMap[typeof (short?)] = DbType.Int16;
            typeMap[typeof (ushort?)] = DbType.UInt16;
            typeMap[typeof (int?)] = DbType.Int32;
            typeMap[typeof (uint?)] = DbType.UInt32;
            typeMap[typeof (long?)] = DbType.Int64;
            typeMap[typeof (ulong?)] = DbType.UInt64;
            typeMap[typeof (float?)] = DbType.Single;
            typeMap[typeof (double?)] = DbType.Double;
            typeMap[typeof (decimal?)] = DbType.Decimal;
            typeMap[typeof (bool?)] = DbType.Boolean;
            typeMap[typeof (char?)] = DbType.StringFixedLength;
            typeMap[typeof (Guid?)] = DbType.Guid;
            typeMap[typeof (DateTime?)] = DbType.DateTime;
            typeMap[typeof (DateTimeOffset?)] = DbType.DateTimeOffset;
            typeMap[typeof (TimeSpan?)] = DbType.Time;
            typeMap[typeof (object)] = DbType.Object;

            AddTypeHandlerImpl(typeof (DataTable), new DataTableHandler(), false);
        }

        /// <summary>
        ///     Clear the registered type handlers
        /// </summary>
        public static void ResetTypeHandlers()
        {
            typeHandlers = new Dictionary<Type, ITypeHandler>();
            AddTypeHandlerImpl(typeof (DataTable), new DataTableHandler(), true);
        }

        /// <summary>
        ///     Configire the specified type to be mapped to a given db-type
        /// </summary>
        public static void AddTypeMap(Type type, DbType dbType)
        {
            // use clone, mutate, replace to avoid threading issues
            Dictionary<Type, DbType> snapshot = typeMap;

            DbType oldValue;
            if (snapshot.TryGetValue(type, out oldValue) && oldValue == dbType) return; // nothing to do

            var newCopy = new Dictionary<Type, DbType>(snapshot);
            newCopy[type] = dbType;
            typeMap = newCopy;
        }

        /// <summary>
        ///     Configire the specified type to be processed by a custom handler
        /// </summary>
        public static void AddTypeHandler(Type type, ITypeHandler handler)
        {
            AddTypeHandlerImpl(type, handler, true);
        }

        /// <summary>
        ///     Configire the specified type to be processed by a custom handler
        /// </summary>
        public static void AddTypeHandlerImpl(Type type, ITypeHandler handler, bool clone)
        {
            if (type == null) throw new ArgumentNullException("type");

            Type secondary = null;
            if (type.IsValueType)
            {
                Type underlying = Nullable.GetUnderlyingType(type);
                if (underlying == null)
                {
                    secondary = typeof (Nullable<>).MakeGenericType(type); // the Nullable<T>
                    // type is already the T
                }
                else
                {
                    secondary = type; // the Nullable<T>
                    type = underlying; // the T
                }
            }

            Dictionary<Type, ITypeHandler> snapshot = typeHandlers;
            ITypeHandler oldValue;
            if (snapshot.TryGetValue(type, out oldValue) && handler == oldValue) return; // nothing to do

            Dictionary<Type, ITypeHandler> newCopy = clone ? new Dictionary<Type, ITypeHandler>(snapshot) : snapshot;

#pragma warning disable 618
            typeof (TypeHandlerCache<>).MakeGenericType(type)
                .GetMethod("SetHandler", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] {handler});
            if (secondary != null)
            {
                typeof (TypeHandlerCache<>).MakeGenericType(secondary)
                    .GetMethod("SetHandler", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] {handler});
            }
#pragma warning restore 618
            if (handler == null)
            {
                newCopy.Remove(type);
                if (secondary != null) newCopy.Remove(secondary);
            }
            else
            {
                newCopy[type] = handler;
                if (secondary != null) newCopy[secondary] = handler;
            }
            typeHandlers = newCopy;
        }

        /// <summary>
        ///     Configire the specified type to be processed by a custom handler
        /// </summary>
        public static void AddTypeHandler<T>(TypeHandler<T> handler)
        {
            AddTypeHandlerImpl(typeof (T), handler, true);
        }

        /// <summary>
        ///     Not intended for direct usage
        /// </summary>
        [Obsolete("Not intended for direct usage", false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static class TypeHandlerCache<T>
        {
            private static ITypeHandler handler;

            /// <summary>
            ///     Not intended for direct usage
            /// </summary>
            [Obsolete("Not intended for direct usage", true)]
            public static T Parse(object value)
            {
                return (T) handler.Parse(typeof (T), value);
            }

            /// <summary>
            ///     Not intended for direct usage
            /// </summary>
            [Obsolete("Not intended for direct usage", true)]
            public static void SetValue(IDbDataParameter parameter, object value)
            {
                handler.SetValue(parameter, value);
            }

            internal static void SetHandler(ITypeHandler handler)
            {
#pragma warning disable 618
                TypeHandlerCache<T>.handler = handler;
#pragma warning restore 618
            }
        }

        private static Dictionary<Type, ITypeHandler> typeHandlers = new Dictionary<Type, ITypeHandler>();

        internal const string LinqBinary = "System.Data.Linq.Binary";

        internal static DbType LookupDbType(Type type, string name, out ITypeHandler handler)
        {
            DbType dbType;
            handler = null;
            Type nullUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullUnderlyingType != null) type = nullUnderlyingType;
            if (type.IsEnum && !typeMap.ContainsKey(type))
            {
                type = Enum.GetUnderlyingType(type);
            }
            if (typeMap.TryGetValue(type, out dbType))
            {
                return dbType;
            }
            if (type.FullName == LinqBinary)
            {
                return DbType.Binary;
            }
            if (typeof (IEnumerable).IsAssignableFrom(type))
            {
                return DynamicParameters.EnumerableMultiParameter;
            }

            if (typeHandlers.TryGetValue(type, out handler))
            {
                return DbType.Object;
            }
            throw new NotSupportedException(
                string.Format("The member {0} of type {1} cannot be used as a parameter value", name, type));
        }


        /// <summary>
        ///     Identity of a cached query in Dapper, used for extensability
        /// </summary>
        public class Identity : IEquatable<Identity>
        {
            /// <summary>
            ///     The command type
            /// </summary>
            public readonly CommandType? commandType;

            /// <summary>
            /// </summary>
            public readonly string connectionString;

            /// <summary>
            /// </summary>
            public readonly int gridIndex;

            /// <summary>
            /// </summary>
            public readonly int hashCode;

            /// <summary>
            /// </summary>
            public readonly Type parametersType;

            /// <summary>
            ///     The sql
            /// </summary>
            public readonly string sql;

            /// <summary>
            /// </summary>
            public readonly Type type;

            internal Identity(string sql, CommandType? commandType, IDbConnection connection, Type type,
                Type parametersType, Type[] otherTypes)
                : this(sql, commandType, connection.ConnectionString, type, parametersType, otherTypes, 0)
            {
            }

            private Identity(string sql, CommandType? commandType, string connectionString, Type type,
                Type parametersType, Type[] otherTypes, int gridIndex)
            {
                this.sql = sql;
                this.commandType = commandType;
                this.connectionString = connectionString;
                this.type = type;
                this.parametersType = parametersType;
                this.gridIndex = gridIndex;
                unchecked
                {
                    hashCode = 17; // we *know* we are using this in a dictionary, so pre-compute this
                    hashCode = hashCode*23 + commandType.GetHashCode();
                    hashCode = hashCode*23 + gridIndex.GetHashCode();
                    hashCode = hashCode*23 + (sql == null ? 0 : sql.GetHashCode());
                    hashCode = hashCode*23 + (type == null ? 0 : type.GetHashCode());
                    if (otherTypes != null)
                    {
                        foreach (Type t in otherTypes)
                        {
                            hashCode = hashCode*23 + (t == null ? 0 : t.GetHashCode());
                        }
                    }
                    hashCode = hashCode*23 +
                               (connectionString == null
                                   ? 0
                                   : SqlMapper.connectionStringComparer.GetHashCode(connectionString));
                    hashCode = hashCode*23 + (parametersType == null ? 0 : parametersType.GetHashCode());
                }
            }

            /// <summary>
            ///     Compare 2 Identity objects
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool Equals(Identity other)
            {
                return
                    other != null &&
                    gridIndex == other.gridIndex &&
                    type == other.type &&
                    sql == other.sql &&
                    commandType == other.commandType &&
                    SqlMapper.connectionStringComparer.Equals(connectionString, other.connectionString) &&
                    parametersType == other.parametersType;
            }

            internal Identity ForGrid(Type primaryType, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, null, gridIndex);
            }

            internal Identity ForGrid(Type primaryType, Type[] otherTypes, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, otherTypes,
                    gridIndex);
            }

            /// <summary>
            ///     Create an identity for use with DynamicParameters, internal use only
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public Identity ForDynamicParameters(Type type)
            {
                return new Identity(sql, commandType, connectionString, this.type, type, null, -1);
            }

            /// <summary>
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return Equals(obj as Identity);
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return hashCode;
            }
        }

#if CSHARP30
    /// <summary>
    /// Execute parameterized SQL  
    /// </summary>
    /// <returns>Number of rows affected</returns>
        public static int Execute(this IDbConnection cnn, string sql, object param)
        {
            return Execute(cnn, sql, param, null, null, null);
        }

        /// <summary>
        /// Execute parameterized SQL
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(this IDbConnection cnn, string sql, object param, IDbTransaction transaction)
        {
            return Execute(cnn, sql, param, transaction, null, null);
        }

        /// <summary>
        /// Execute parameterized SQL
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(this IDbConnection cnn, string sql, object param, CommandType commandType)
        {
            return Execute(cnn, sql, param, null, null, commandType);
        }

        /// <summary>
        /// Execute parameterized SQL
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(this IDbConnection cnn, string sql, object param, IDbTransaction transaction, CommandType commandType)
        {
            return Execute(cnn, sql, param, transaction, null, commandType);
        }

        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>
        /// </summary>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        public static IDataReader ExecuteReader(this IDbConnection cnn, string sql, object param)
        {
            return ExecuteReader(cnn, sql, param, null, null, null);
        }

        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>
        /// </summary>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        public static IDataReader ExecuteReader(this IDbConnection cnn, string sql, object param, IDbTransaction transaction)
        {
            return ExecuteReader(cnn, sql, param, transaction, null, null);
        }

        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>
        /// </summary>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        public static IDataReader ExecuteReader(this IDbConnection cnn, string sql, object param, CommandType commandType)
        {
            return ExecuteReader(cnn, sql, param, null, null, commandType);
        }

        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>
        /// </summary>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        public static IDataReader ExecuteReader(this IDbConnection cnn, string sql, object param, IDbTransaction transaction, CommandType commandType)
        {
            return ExecuteReader(cnn, sql, param, transaction, null, commandType);
        }

        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object param)
        {
            return Query<T>(cnn, sql, param, null, true, null, null);
        }

        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object param, IDbTransaction transaction)
        {
            return Query<T>(cnn, sql, param, transaction, true, null, null);
        }

        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object param, CommandType commandType)
        {
            return Query<T>(cnn, sql, param, null, true, null, commandType);
        }

        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object param, IDbTransaction transaction, CommandType commandType)
        {
            return Query<T>(cnn, sql, param, transaction, true, null, commandType);
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public static GridReader QueryMultiple(this IDbConnection cnn, string sql, object param, IDbTransaction transaction)
        {
            return QueryMultiple(cnn, sql, param, transaction, null, null);
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public static GridReader QueryMultiple(this IDbConnection cnn, string sql, object param, CommandType commandType)
        {
            return QueryMultiple(cnn, sql, param, null, null, commandType);
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public static GridReader QueryMultiple(this IDbConnection cnn, string sql, object param, IDbTransaction transaction, CommandType commandType)
        {
            return QueryMultiple(cnn, sql, param, transaction, null, commandType);
        }
#endif


        /// <summary>
        ///     Execute parameterized SQL
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(
#if CSHARP30
this IDbConnection cnn, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null
#endif
            )
        {
            var command = new CommandDefinition(sql, (object) param, transaction, commandTimeout, commandType,
                CommandFlags.Buffered);
            return ExecuteImpl(cnn, ref command);
        }

        /// <summary>
        ///     Execute parameterized SQL
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(this IDbConnection cnn, CommandDefinition command)
        {
            return ExecuteImpl(cnn, ref command);
        }


        /// <summary>
        ///     Execute parameterized SQL that selects a single value
        /// </summary>
        /// <returns>The first cell selected</returns>
        public static object ExecuteScalar(
#if CSHARP30
this IDbConnection cnn, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null
#endif
            )
        {
            var command = new CommandDefinition(sql, (object) param, transaction, commandTimeout, commandType,
                CommandFlags.Buffered);
            return ExecuteScalarImpl<object>(cnn, ref command);
        }

        /// <summary>
        ///     Execute parameterized SQL that selects a single value
        /// </summary>
        /// <returns>The first cell selected</returns>
        public static T ExecuteScalar<T>(
#if CSHARP30
this IDbConnection cnn, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null
#endif
            )
        {
            var command = new CommandDefinition(sql, (object) param, transaction, commandTimeout, commandType,
                CommandFlags.Buffered);
            return ExecuteScalarImpl<T>(cnn, ref command);
        }

        /// <summary>
        ///     Execute parameterized SQL that selects a single value
        /// </summary>
        /// <returns>The first cell selected</returns>
        public static object ExecuteScalar(this IDbConnection cnn, CommandDefinition command)
        {
            return ExecuteScalarImpl<object>(cnn, ref command);
        }

        /// <summary>
        ///     Execute parameterized SQL that selects a single value
        /// </summary>
        /// <returns>The first cell selected</returns>
        public static T ExecuteScalar<T>(this IDbConnection cnn, CommandDefinition command)
        {
            return ExecuteScalarImpl<T>(cnn, ref command);
        }

        private static int ExecuteImpl(this IDbConnection cnn, ref CommandDefinition command)
        {
            object param = command.Parameters;
            var multiExec = param as IEnumerable;
            Identity identity;
            CacheInfo info = null;
            if (multiExec != null && !(multiExec is string))
            {
#if ASYNC
                if((command.Flags & CommandFlags.Pipelined) != 0)
                {
                    // this includes all the code for concurrent/overlapped query
                    return ExecuteMultiImplAsync(cnn, command, multiExec).Result;
                }
#endif
                bool isFirst = true;
                int total = 0;
                bool wasClosed = cnn.State == ConnectionState.Closed;
                try
                {
                    if (wasClosed) cnn.Open();
                    using (IDbCommand cmd = command.SetupCommand(cnn, null))
                    {
                        string masterSql = null;
                        foreach (object obj in multiExec)
                        {
                            if (isFirst)
                            {
                                masterSql = cmd.CommandText;
                                isFirst = false;
                                identity = new Identity(command.CommandText, cmd.CommandType, cnn, null, obj.GetType(),
                                    null);
                                info = GetCacheInfo(identity, obj, command.AddToCache);
                            }
                            else
                            {
                                cmd.CommandText = masterSql; // because we do magic replaces on "in" etc
                                cmd.Parameters.Clear(); // current code is Add-tastic
                            }
                            info.ParamReader(cmd, obj);
                            total += cmd.ExecuteNonQuery();
                        }
                    }
                }
                finally
                {
                    if (wasClosed) cnn.Close();
                }
                return total;
            }

            // nice and simple
            if (param != null)
            {
                identity = new Identity(command.CommandText, command.CommandType, cnn, null, param.GetType(), null);
                info = GetCacheInfo(identity, param, command.AddToCache);
            }
            return ExecuteCommand(cnn, ref command, param == null ? null : info.ParamReader);
        }

        /// <summary>
        ///     Execute parameterized SQL and return an <see cref="IDataReader" />
        /// </summary>
        /// <returns>An <see cref="IDataReader" /> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        ///     This is typically used when the results of a query are not processed by Dapper, for example, used to fill a
        ///     <see cref="DataTable" />
        ///     or <see cref="DataSet" />.
        /// </remarks>
        /// <example>
        ///     <code>
        /// <![CDATA[
        /// DataTable table = new DataTable("MyTable");
        /// using (var reader = ExecuteReader(cnn, sql, param))
        /// {
        ///     table.Load(reader);
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public static IDataReader ExecuteReader(
#if CSHARP30
this IDbConnection cnn, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null
#endif
            )
        {
            var command = new CommandDefinition(sql, (object) param, transaction, commandTimeout, commandType,
                CommandFlags.Buffered);
            return ExecuteReaderImpl(cnn, ref command, CommandBehavior.Default);
        }

        /// <summary>
        ///     Execute parameterized SQL and return an <see cref="IDataReader" />
        /// </summary>
        /// <returns>An <see cref="IDataReader" /> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        ///     This is typically used when the results of a query are not processed by Dapper, for example, used to fill a
        ///     <see cref="DataTable" />
        ///     or <see cref="DataSet" />.
        /// </remarks>
        public static IDataReader ExecuteReader(this IDbConnection cnn, CommandDefinition command)
        {
            return ExecuteReaderImpl(cnn, ref command, CommandBehavior.Default);
        }

        /// <summary>
        ///     Execute parameterized SQL and return an <see cref="IDataReader" />
        /// </summary>
        /// <returns>An <see cref="IDataReader" /> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        ///     This is typically used when the results of a query are not processed by Dapper, for example, used to fill a
        ///     <see cref="DataTable" />
        ///     or <see cref="DataSet" />.
        /// </remarks>
        public static IDataReader ExecuteReader(this IDbConnection cnn, CommandDefinition command,
            CommandBehavior commandBehavior)
        {
            return ExecuteReaderImpl(cnn, ref command, commandBehavior);
        }

#if !CSHARP30
        /// <summary>
        ///     Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public static IEnumerable<dynamic> Query(this IDbConnection cnn, string sql, dynamic param = null,
            IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            return Query<DapperRow>(cnn, sql, param as object, transaction, buffered, commandTimeout, commandType);
        }
#else
    /// <summary>
    /// Return a list of dynamic objects, reader is closed after the call
    /// </summary>
        public static IEnumerable<IDictionary<string, object>> Query(this IDbConnection cnn, string sql, object param)
        {
            return Query(cnn, sql, param, null, true, null, null);
        }

        /// <summary>
        /// Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public static IEnumerable<IDictionary<string, object>> Query(this IDbConnection cnn, string sql, object param, IDbTransaction transaction)
        {
            return Query(cnn, sql, param, transaction, true, null, null);
        }

        /// <summary>
        /// Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public static IEnumerable<IDictionary<string, object>> Query(this IDbConnection cnn, string sql, object param, CommandType? commandType)
        {
            return Query(cnn, sql, param, null, true, null, commandType);
        }

        /// <summary>
        /// Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public static IEnumerable<IDictionary<string, object>> Query(this IDbConnection cnn, string sql, object param, IDbTransaction transaction, CommandType? commandType)
        {
            return Query(cnn, sql, param, transaction, true, null, commandType);
        }

        /// <summary>
        /// Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public static IEnumerable<IDictionary<string, object>> Query(this IDbConnection cnn, string sql, object param, IDbTransaction transaction, bool buffered, int? commandTimeout, CommandType? commandType)
        {
            return Query<IDictionary<string, object>>(cnn, sql, param, transaction, buffered, commandTimeout, commandType);
        }
#endif

        /// <summary>
        ///     Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>
        ///     the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs
        ///     completion gets annoying. Eg type new [space] get new object
        /// </remarks>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(
#if CSHARP30
this IDbConnection cnn, string sql, object param, IDbTransaction transaction, bool buffered, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null,
            bool buffered = true, int? commandTimeout = null, CommandType? commandType = null
#endif
            )
        {
            var command = new CommandDefinition(sql, (object) param, transaction, commandTimeout, commandType,
                buffered ? CommandFlags.Buffered : CommandFlags.None);
            IEnumerable<T> data = QueryImpl<T>(cnn, command, typeof (T));
            return command.Buffered ? data.ToList() : data;
        }

        /// <summary>
        ///     Executes a query, returning the data typed as per the Type suggested
        /// </summary>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<object> Query(
#if CSHARP30
this IDbConnection cnn, Type type, string sql, object param, IDbTransaction transaction, bool buffered, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null,
            bool buffered = true, int? commandTimeout = null, CommandType? commandType = null
#endif
            )
        {
            if (type == null) throw new ArgumentNullException("type");
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType,
                buffered ? CommandFlags.Buffered : CommandFlags.None);
            IEnumerable<object> data = QueryImpl<object>(cnn, command, type);
            return command.Buffered ? data.ToList() : data;
        }

        /// <summary>
        ///     Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>
        ///     the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs
        ///     completion gets annoying. Eg type new [space] get new object
        /// </remarks>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(this IDbConnection cnn, CommandDefinition command)
        {
            IEnumerable<T> data = QueryImpl<T>(cnn, command, typeof (T));
            return command.Buffered ? data.ToList() : data;
        }


        /// <summary>
        ///     Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public static GridReader QueryMultiple(
#if CSHARP30
this IDbConnection cnn, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null
#endif
            )
        {
            var command = new CommandDefinition(sql, (object) param, transaction, commandTimeout, commandType,
                CommandFlags.Buffered);
            return QueryMultipleImpl(cnn, ref command);
        }

        /// <summary>
        ///     Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public static GridReader QueryMultiple(this IDbConnection cnn, CommandDefinition command)
        {
            return QueryMultipleImpl(cnn, ref command);
        }

        private static GridReader QueryMultipleImpl(this IDbConnection cnn, ref CommandDefinition command)
        {
            object param = command.Parameters;
            var identity = new Identity(command.CommandText, command.CommandType, cnn, typeof (GridReader),
                param == null ? null : param.GetType(), null);
            CacheInfo info = GetCacheInfo(identity, param, command.AddToCache);

            IDbCommand cmd = null;
            IDataReader reader = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                if (wasClosed) cnn.Open();
                cmd = command.SetupCommand(cnn, info.ParamReader);
                reader =
                    cmd.ExecuteReader(wasClosed
                        ? CommandBehavior.CloseConnection | CommandBehavior.SequentialAccess
                        : CommandBehavior.SequentialAccess);

                var result = new GridReader(cmd, reader, identity);
                wasClosed = false; // *if* the connection was closed and we got this far, then we now have a reader
                // with the CloseConnection flag, so the reader will deal with the connection; we
                // still need something in the "finally" to ensure that broken SQL still results
                // in the connection closing itself
                return result;
            }
            catch
            {
                if (reader != null)
                {
                    if (!reader.IsClosed)
                        try
                        {
                            cmd.Cancel();
                        }
                        catch
                        {
                            /* don't spoil the existing exception */
                        }
                    reader.Dispose();
                }
                if (cmd != null) cmd.Dispose();
                if (wasClosed) cnn.Close();
                throw;
            }
        }

        private static IEnumerable<T> QueryImpl<T>(this IDbConnection cnn, CommandDefinition command, Type effectiveType)
        {
            object param = command.Parameters;
            var identity = new Identity(command.CommandText, command.CommandType, cnn, effectiveType,
                param == null ? null : param.GetType(), null);
            CacheInfo info = GetCacheInfo(identity, param, command.AddToCache);

            IDbCommand cmd = null;
            IDataReader reader = null;

            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                cmd = command.SetupCommand(cnn, info.ParamReader);

                if (wasClosed) cnn.Open();
                reader =
                    cmd.ExecuteReader(wasClosed
                        ? CommandBehavior.CloseConnection | CommandBehavior.SequentialAccess
                        : CommandBehavior.SequentialAccess);
                wasClosed = false; // *if* the connection was closed and we got this far, then we now have a reader
                // with the CloseConnection flag, so the reader will deal with the connection; we
                // still need something in the "finally" to ensure that broken SQL still results
                // in the connection closing itself
                DeserializerState tuple = info.Deserializer;
                int hash = GetColumnHash(reader);
                if (tuple.Func == null || tuple.Hash != hash)
                {
                    if (reader.FieldCount == 0) //https://code.google.com/p/dapper-dot-net/issues/detail?id=57
                        yield break;
                    tuple =
                        info.Deserializer =
                            new DeserializerState(hash, GetDeserializer(effectiveType, reader, 0, -1, false));
                    if (command.AddToCache) SetQueryCache(identity, info);
                }

                Func<IDataReader, object> func = tuple.Func;
                Type convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
                while (reader.Read())
                {
                    object val = func(reader);
                    if (val == null || val is T)
                    {
                        yield return (T) val;
                    }
                    else
                    {
                        yield return (T) Convert.ChangeType(val, convertToType, CultureInfo.InvariantCulture);
                    }
                }
                // happy path; close the reader cleanly - no
                // need for "Cancel" etc
                reader.Dispose();
                reader = null;
            }
            finally
            {
                if (reader != null)
                {
                    if (!reader.IsClosed)
                        try
                        {
                            cmd.Cancel();
                        }
                        catch
                        {
                            /* don't spoil the existing exception */
                        }
                    reader.Dispose();
                }
                if (wasClosed) cnn.Close();
                if (cmd != null) cmd.Dispose();
            }
        }

        /// <summary>
        ///     Maps a query to objects
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset</typeparam>
        /// <typeparam name="TReturn">The return type</typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(
#if CSHARP30
this IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null,
            IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null
#endif
            )
        {
            return MultiMap<TFirst, TSecond, DontMap, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map,
                param as object, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Maps a query to objects
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(
#if CSHARP30
this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null,
            IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null
#endif
            )
        {
            return MultiMap<TFirst, TSecond, TThird, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map,
                param as object, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi mapping query with 4 input parameters
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TFourth"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(
#if CSHARP30
this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType
#else
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map,
            dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = null, CommandType? commandType = null
#endif
            )
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map,
                param as object, transaction, buffered, splitOn, commandTimeout, commandType);
        }

#if !CSHARP30
        /// <summary>
        ///     Perform a multi mapping query with 5 input parameters
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TFourth"></typeparam>
        /// <typeparam name="TFifth"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map,
            dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = null, CommandType? commandType = null
            )
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, DontMap, DontMap, TReturn>(cnn, sql, map,
                param as object, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi mapping query with 6 input parameters
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TFourth"></typeparam>
        /// <typeparam name="TFifth"></typeparam>
        /// <typeparam name="TSixth"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map,
            dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = null, CommandType? commandType = null
            )
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, DontMap, TReturn>(cnn, sql, map,
                param as object, transaction, buffered, splitOn, commandTimeout, commandType);
        }


        /// <summary>
        ///     Perform a multi mapping query with 7 input parameters
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TFourth"></typeparam>
        /// <typeparam name="TFifth"></typeparam>
        /// <typeparam name="TSixth"></typeparam>
        /// <typeparam name="TSeventh"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
            this IDbConnection cnn, string sql,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, dynamic param = null,
            IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(cnn, sql, map,
                param as object, transaction, buffered, splitOn, commandTimeout, commandType);
        }
#endif

        private class DontMap
        {
        }

        private static IEnumerable<TReturn> MultiMap
            <TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
            this IDbConnection cnn, string sql, Delegate map, object param, IDbTransaction transaction, bool buffered,
            string splitOn, int? commandTimeout, CommandType? commandType)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType,
                buffered ? CommandFlags.Buffered : CommandFlags.None);
            IEnumerable<TReturn> results =
                MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(cnn, command, map,
                    splitOn, null, null);
            return buffered ? results.ToList() : results;
        }

        private static IEnumerable<TReturn> MultiMapImpl
            <TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn,
                CommandDefinition command, Delegate map, string splitOn, IDataReader reader, Identity identity)
        {
            object param = command.Parameters;
            identity = identity ??
                       new Identity(command.CommandText, command.CommandType, cnn, typeof (TFirst),
                           param == null ? null : param.GetType(),
                           new[]
                           {
                               typeof (TFirst), typeof (TSecond), typeof (TThird), typeof (TFourth), typeof (TFifth),
                               typeof (TSixth), typeof (TSeventh)
                           });
            CacheInfo cinfo = GetCacheInfo(identity, param, command.AddToCache);

            IDbCommand ownedCommand = null;
            IDataReader ownedReader = null;

            bool wasClosed = cnn != null && cnn.State == ConnectionState.Closed;
            try
            {
                if (reader == null)
                {
                    ownedCommand = command.SetupCommand(cnn, cinfo.ParamReader);
                    if (wasClosed) cnn.Open();
                    ownedReader =
                        ownedCommand.ExecuteReader(wasClosed
                            ? CommandBehavior.CloseConnection | CommandBehavior.SequentialAccess
                            : CommandBehavior.SequentialAccess);
                    reader = ownedReader;
                }
                DeserializerState deserializer = default(DeserializerState);
                Func<IDataReader, object>[] otherDeserializers = null;

                int hash = GetColumnHash(reader);
                if ((deserializer = cinfo.Deserializer).Func == null ||
                    (otherDeserializers = cinfo.OtherDeserializers) == null || hash != deserializer.Hash)
                {
                    Func<IDataReader, object>[] deserializers =
                        GenerateDeserializers(
                            new[]
                            {
                                typeof (TFirst), typeof (TSecond), typeof (TThird), typeof (TFourth), typeof (TFifth),
                                typeof (TSixth), typeof (TSeventh)
                            }, splitOn, reader);
                    deserializer = cinfo.Deserializer = new DeserializerState(hash, deserializers[0]);
                    otherDeserializers = cinfo.OtherDeserializers = deserializers.Skip(1).ToArray();
                    if (command.AddToCache) SetQueryCache(identity, cinfo);
                }

                Func<IDataReader, TReturn> mapIt =
                    GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
                        deserializer.Func, otherDeserializers, map);

                if (mapIt != null)
                {
                    while (reader.Read())
                    {
                        yield return mapIt(reader);
                    }
                }
            }
            finally
            {
                try
                {
                    if (ownedReader != null)
                    {
                        ownedReader.Dispose();
                    }
                }
                finally
                {
                    if (ownedCommand != null)
                    {
                        ownedCommand.Dispose();
                    }
                    if (wasClosed) cnn.Close();
                }
            }
        }

        private static Func<IDataReader, TReturn> GenerateMapper
            <TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
            Func<IDataReader, object> deserializer, Func<IDataReader, object>[] otherDeserializers, object map)
        {
            switch (otherDeserializers.Length)
            {
                case 1:
                    return
                        r =>
                            ((Func<TFirst, TSecond, TReturn>) map)((TFirst) deserializer(r),
                                (TSecond) otherDeserializers[0](r));
                case 2:
                    return
                        r =>
                            ((Func<TFirst, TSecond, TThird, TReturn>) map)((TFirst) deserializer(r),
                                (TSecond) otherDeserializers[0](r), (TThird) otherDeserializers[1](r));
                case 3:
                    return
                        r =>
                            ((Func<TFirst, TSecond, TThird, TFourth, TReturn>) map)((TFirst) deserializer(r),
                                (TSecond) otherDeserializers[0](r), (TThird) otherDeserializers[1](r),
                                (TFourth) otherDeserializers[2](r));
#if !CSHARP30
                case 4:
                    return
                        r =>
                            ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>) map)((TFirst) deserializer(r),
                                (TSecond) otherDeserializers[0](r), (TThird) otherDeserializers[1](r),
                                (TFourth) otherDeserializers[2](r), (TFifth) otherDeserializers[3](r));
                case 5:
                    return
                        r =>
                            ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>) map)(
                                (TFirst) deserializer(r), (TSecond) otherDeserializers[0](r),
                                (TThird) otherDeserializers[1](r), (TFourth) otherDeserializers[2](r),
                                (TFifth) otherDeserializers[3](r), (TSixth) otherDeserializers[4](r));
                case 6:
                    return
                        r =>
                            ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>) map)(
                                (TFirst) deserializer(r), (TSecond) otherDeserializers[0](r),
                                (TThird) otherDeserializers[1](r), (TFourth) otherDeserializers[2](r),
                                (TFifth) otherDeserializers[3](r), (TSixth) otherDeserializers[4](r),
                                (TSeventh) otherDeserializers[5](r));
#endif
                default:
                    throw new NotSupportedException();
            }
        }

        private static Func<IDataReader, object>[] GenerateDeserializers(Type[] types, string splitOn,
            IDataReader reader)
        {
            var deserializers = new List<Func<IDataReader, object>>();
            string[] splits = splitOn.Split(',').Select(s => s.Trim()).ToArray();
            bool isMultiSplit = splits.Length > 1;
            if (types.First() == typeof (Object))
            {
                // we go left to right for dynamic multi-mapping so that the madness of TestMultiMappingVariations
                // is supported
                bool first = true;
                int currentPos = 0;
                int splitIdx = 0;
                string currentSplit = splits[splitIdx];
                foreach (Type type in types)
                {
                    if (type == typeof (DontMap))
                    {
                        break;
                    }

                    int splitPoint = GetNextSplitDynamic(currentPos, currentSplit, reader);
                    if (isMultiSplit && splitIdx < splits.Length - 1)
                    {
                        currentSplit = splits[++splitIdx];
                    }
                    deserializers.Add((GetDeserializer(type, reader, currentPos, splitPoint - currentPos, !first)));
                    currentPos = splitPoint;
                    first = false;
                }
            }
            else
            {
                // in this we go right to left through the data reader in order to cope with properties that are
                // named the same as a subsequent primary key that we split on
                int currentPos = reader.FieldCount;
                int splitIdx = splits.Length - 1;
                string currentSplit = splits[splitIdx];
                for (int typeIdx = types.Length - 1; typeIdx >= 0; --typeIdx)
                {
                    Type type = types[typeIdx];
                    if (type == typeof (DontMap))
                    {
                        continue;
                    }

                    int splitPoint = 0;
                    if (typeIdx > 0)
                    {
                        splitPoint = GetNextSplit(currentPos, currentSplit, reader);
                        if (isMultiSplit && splitIdx > 0)
                        {
                            currentSplit = splits[--splitIdx];
                        }
                    }

                    deserializers.Add((GetDeserializer(type, reader, splitPoint, currentPos - splitPoint, typeIdx > 0)));
                    currentPos = splitPoint;
                }

                deserializers.Reverse();
            }
            return deserializers.ToArray();
        }

        private static int GetNextSplitDynamic(int startIdx, string splitOn, IDataReader reader)
        {
            if (startIdx == reader.FieldCount)
            {
                throw new ArgumentException(MultiMapSplitExceptionMessage);
            }

            if (splitOn == "*")
            {
                return ++startIdx;
            }

            for (int i = startIdx + 1; i < reader.FieldCount; ++i)
            {
                if (string.Equals(splitOn, reader.GetName(i), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return reader.FieldCount;
        }

        private static int GetNextSplit(int startIdx, string splitOn, IDataReader reader)
        {
            if (splitOn == "*")
            {
                return --startIdx;
            }

            for (int i = startIdx - 1; i > 0; --i)
            {
                if (string.Equals(splitOn, reader.GetName(i), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            throw new ArgumentException(MultiMapSplitExceptionMessage);
        }

        private static CacheInfo GetCacheInfo(Identity identity, object exampleParameters, bool addToCache)
        {
            CacheInfo info;
            if (!TryGetQueryCache(identity, out info))
            {
                info = new CacheInfo();
                if (identity.parametersType != null)
                {
                    if (exampleParameters is IDynamicParameters)
                    {
                        info.ParamReader = (cmd, obj) => { ((IDynamicParameters) obj).AddParameters(cmd, identity); };
                    }
#if !CSHARP30
                        // special-case dictionary && `dynamic`
                    else if (exampleParameters is IEnumerable<KeyValuePair<string, object>> &&
                             exampleParameters is System.Dynamic.IDynamicMetaObjectProvider)
                    {
                        info.ParamReader = (cmd, obj) =>
                        {
                            IDynamicParameters mapped = new DynamicParameters(obj);
                            mapped.AddParameters(cmd, identity);
                        };
                    }
#endif
                    else
                    {
                        IList<LiteralToken> literals = GetLiteralTokens(identity.sql);
                        info.ParamReader = CreateParamInfoGenerator(identity, false, true, literals);
                    }
                }
                if (addToCache) SetQueryCache(identity, info);
            }
            return info;
        }

        private static Func<IDataReader, object> GetDeserializer(Type type, IDataReader reader, int startBound,
            int length, bool returnNullIfFirstMissing)
        {
#if !CSHARP30
            // dynamic is passed in as Object ... by c# design
            if (type == typeof (object)
                || type == typeof (DapperRow))
            {
                return GetDapperRowDeserializer(reader, startBound, length, returnNullIfFirstMissing);
            }
#else
            if (type.IsAssignableFrom(typeof(Dictionary<string, object>)))
            {
                return GetDictionaryDeserializer(reader, startBound, length, returnNullIfFirstMissing);
            }
#endif
            Type underlyingType = null;
            if (!(typeMap.ContainsKey(type) || type.IsEnum || type.FullName == LinqBinary ||
                  (type.IsValueType && (underlyingType = Nullable.GetUnderlyingType(type)) != null &&
                   underlyingType.IsEnum)))
            {
                ITypeHandler handler;
                if (typeHandlers.TryGetValue(type, out handler))
                {
                    return GetHandlerDeserializer(handler, type, startBound);
                }
                return GetTypeDeserializer(type, reader, startBound, length, returnNullIfFirstMissing);
            }
            return GetStructDeserializer(type, underlyingType ?? type, startBound);
        }

        private static Func<IDataReader, object> GetHandlerDeserializer(ITypeHandler handler, Type type, int startBound)
        {
            return (IDataReader reader) =>
                handler.Parse(type, reader.GetValue(startBound));
        }

#if !CSHARP30
        private sealed class DapperTable
        {
            private readonly Dictionary<string, int> fieldNameLookup;
            private string[] fieldNames;

            public DapperTable(string[] fieldNames)
            {
                if (fieldNames == null) throw new ArgumentNullException("fieldNames");
                this.fieldNames = fieldNames;

                fieldNameLookup = new Dictionary<string, int>(fieldNames.Length, StringComparer.Ordinal);
                // if there are dups, we want the **first** key to be the "winner" - so iterate backwards
                for (int i = fieldNames.Length - 1; i >= 0; i--)
                {
                    string key = fieldNames[i];
                    if (key != null) fieldNameLookup[key] = i;
                }
            }

            internal string[] FieldNames
            {
                get { return fieldNames; }
            }

            public int FieldCount
            {
                get { return fieldNames.Length; }
            }

            internal int IndexOfName(string name)
            {
                int result;
                return (name != null && fieldNameLookup.TryGetValue(name, out result)) ? result : -1;
            }

            internal int AddField(string name)
            {
                if (name == null) throw new ArgumentNullException("name");
                if (fieldNameLookup.ContainsKey(name))
                    throw new InvalidOperationException("Field already exists: " + name);
                int oldLen = fieldNames.Length;
                Array.Resize(ref fieldNames, oldLen + 1);
                    // yes, this is sub-optimal, but this is not the expected common case
                fieldNames[oldLen] = name;
                fieldNameLookup[name] = oldLen;
                return oldLen;
            }


            internal bool FieldExists(string key)
            {
                return key != null && fieldNameLookup.ContainsKey(key);
            }
        }

        private sealed class DapperRowMetaObject : System.Dynamic.DynamicMetaObject
        {
            private static readonly MethodInfo getValueMethod =
                typeof (IDictionary<string, object>).GetProperty("Item").GetGetMethod();

            private static readonly MethodInfo setValueMethod = typeof (DapperRow).GetMethod("SetValue",
                new[] {typeof (string), typeof (object)});

            public DapperRowMetaObject(
                System.Linq.Expressions.Expression expression,
                System.Dynamic.BindingRestrictions restrictions
                )
                : base(expression, restrictions)
            {
            }

            public DapperRowMetaObject(
                System.Linq.Expressions.Expression expression,
                System.Dynamic.BindingRestrictions restrictions,
                object value
                )
                : base(expression, restrictions, value)
            {
            }

            private System.Dynamic.DynamicMetaObject CallMethod(
                MethodInfo method,
                System.Linq.Expressions.Expression[] parameters
                )
            {
                var callMethod = new System.Dynamic.DynamicMetaObject(
                    System.Linq.Expressions.Expression.Call(
                        System.Linq.Expressions.Expression.Convert(Expression, LimitType),
                        method,
                        parameters),
                    System.Dynamic.BindingRestrictions.GetTypeRestriction(Expression, LimitType)
                    );
                return callMethod;
            }

            public override System.Dynamic.DynamicMetaObject BindGetMember(System.Dynamic.GetMemberBinder binder)
            {
                var parameters = new System.Linq.Expressions.Expression[]
                {
                    System.Linq.Expressions.Expression.Constant(binder.Name)
                };

                DynamicMetaObject callMethod = CallMethod(getValueMethod, parameters);

                return callMethod;
            }

            // Needed for Visual basic dynamic support
            public override System.Dynamic.DynamicMetaObject BindInvokeMember(System.Dynamic.InvokeMemberBinder binder,
                System.Dynamic.DynamicMetaObject[] args)
            {
                var parameters = new System.Linq.Expressions.Expression[]
                {
                    System.Linq.Expressions.Expression.Constant(binder.Name)
                };

                DynamicMetaObject callMethod = CallMethod(getValueMethod, parameters);

                return callMethod;
            }

            public override System.Dynamic.DynamicMetaObject BindSetMember(System.Dynamic.SetMemberBinder binder,
                System.Dynamic.DynamicMetaObject value)
            {
                var parameters = new[]
                {
                    System.Linq.Expressions.Expression.Constant(binder.Name),
                    value.Expression
                };

                DynamicMetaObject callMethod = CallMethod(setValueMethod, parameters);

                return callMethod;
            }
        }

        private sealed class DapperRow
            : System.Dynamic.IDynamicMetaObjectProvider
                , IDictionary<string, object>
        {
            private readonly DapperTable table;
            private object[] values;

            public DapperRow(DapperTable table, object[] values)
            {
                if (table == null) throw new ArgumentNullException("table");
                if (values == null) throw new ArgumentNullException("values");
                this.table = table;
                this.values = values;
            }

            int ICollection<KeyValuePair<string, object>>.Count
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (!(values[i] is DeadValue)) count++;
                    }
                    return count;
                }
            }

            public bool TryGetValue(string name, out object value)
            {
                int index = table.IndexOfName(name);
                if (index < 0)
                {
                    // doesn't exist
                    value = null;
                    return false;
                }
                // exists, **even if** we don't have a value; consider table rows heterogeneous
                value = index < values.Length ? values[index] : null;
                if (value is DeadValue)
                {
                    // pretend it isn't here
                    value = null;
                    return false;
                }
                return true;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                string[] names = table.FieldNames;
                for (int i = 0; i < names.Length; i++)
                {
                    object value = i < values.Length ? values[i] : null;
                    if (!(value is DeadValue))
                    {
                        yield return new KeyValuePair<string, object>(names[i], value);
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(
                System.Linq.Expressions.Expression parameter)
            {
                return new DapperRowMetaObject(parameter, System.Dynamic.BindingRestrictions.Empty, this);
            }

            #region Implementation of ICollection<KeyValuePair<string,object>>

            void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
            {
                IDictionary<string, object> dic = this;
                dic.Add(item.Key, item.Value);
            }

            void ICollection<KeyValuePair<string, object>>.Clear()
            {
                // removes values for **this row**, but doesn't change the fundamental table
                for (int i = 0; i < values.Length; i++)
                    values[i] = DeadValue.Default;
            }

            bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
            {
                object value;
                return TryGetValue(item.Key, out value) && Equals(value, item.Value);
            }

            void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                foreach (var kv in this)
                {
                    array[arrayIndex++] = kv; // if they didn't leave enough space; not our fault
                }
            }

            bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
            {
                IDictionary<string, object> dic = this;
                return dic.Remove(item.Key);
            }

            bool ICollection<KeyValuePair<string, object>>.IsReadOnly
            {
                get { return false; }
            }

            #endregion

            #region Implementation of IDictionary<string,object>

            bool IDictionary<string, object>.ContainsKey(string key)
            {
                int index = table.IndexOfName(key);
                if (index < 0 || index >= values.Length || values[index] is DeadValue) return false;
                return true;
            }

            void IDictionary<string, object>.Add(string key, object value)
            {
                SetValue(key, value, true);
            }

            bool IDictionary<string, object>.Remove(string key)
            {
                int index = table.IndexOfName(key);
                if (index < 0 || index >= values.Length || values[index] is DeadValue) return false;
                values[index] = DeadValue.Default;
                return true;
            }

            object IDictionary<string, object>.this[string key]
            {
                get
                {
                    object val;
                    TryGetValue(key, out val);
                    return val;
                }
                set { SetValue(key, value, false); }
            }

            ICollection<string> IDictionary<string, object>.Keys
            {
                get { return this.Select(kv => kv.Key).ToArray(); }
            }

            ICollection<object> IDictionary<string, object>.Values
            {
                get { return this.Select(kv => kv.Value).ToArray(); }
            }

            public object SetValue(string key, object value)
            {
                return SetValue(key, value, false);
            }

            private object SetValue(string key, object value, bool isAdd)
            {
                if (key == null) throw new ArgumentNullException("key");
                int index = table.IndexOfName(key);
                if (index < 0)
                {
                    index = table.AddField(key);
                }
                else if (isAdd && index < values.Length && !(values[index] is DeadValue))
                {
                    // then semantically, this value already exists
                    throw new ArgumentException("An item with the same key has already been added", "key");
                }
                int oldLength = values.Length;
                if (oldLength <= index)
                {
                    // we'll assume they're doing lots of things, and
                    // grow it to the full width of the table
                    Array.Resize(ref values, table.FieldCount);
                    for (int i = oldLength; i < values.Length; i++)
                    {
                        values[i] = DeadValue.Default;
                    }
                }
                return values[index] = value;
            }

            #endregion

            public override string ToString()
            {
                var sb = new StringBuilder("{DapperRow");
                foreach (var kv in this)
                {
                    object value = kv.Value;
                    sb.Append(", ").Append(kv.Key);
                    if (value != null)
                    {
                        sb.Append(" = '").Append(kv.Value).Append('\'');
                    }
                    else
                    {
                        sb.Append(" = NULL");
                    }
                }

                return sb.Append('}').ToString();
            }

            private sealed class DeadValue
            {
                public static readonly DeadValue Default = new DeadValue();

                private DeadValue()
                {
                }
            }
        }
#endif

        private const string MultiMapSplitExceptionMessage =
            "When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id";

#if !CSHARP30
        internal static Func<IDataReader, object> GetDapperRowDeserializer(IDataRecord reader, int startBound,
            int length, bool returnNullIfFirstMissing)
        {
            int fieldCount = reader.FieldCount;
            if (length == -1)
            {
                length = fieldCount - startBound;
            }

            if (fieldCount <= startBound)
            {
                throw new ArgumentException(MultiMapSplitExceptionMessage, "splitOn");
            }

            int effectiveFieldCount = Math.Min(fieldCount - startBound, length);

            DapperTable table = null;

            return
                r =>
                {
                    if (table == null)
                    {
                        var names = new string[effectiveFieldCount];
                        for (int i = 0; i < effectiveFieldCount; i++)
                        {
                            names[i] = r.GetName(i + startBound);
                        }
                        table = new DapperTable(names);
                    }

                    var values = new object[effectiveFieldCount];

                    if (returnNullIfFirstMissing)
                    {
                        values[0] = r.GetValue(startBound);
                        if (values[0] is DBNull)
                        {
                            return null;
                        }
                    }

                    if (startBound == 0)
                    {
                        r.GetValues(values);
                        for (int i = 0; i < values.Length; i++)
                            if (values[i] is DBNull) values[i] = null;
                    }
                    else
                    {
                        int begin = returnNullIfFirstMissing ? 1 : 0;
                        for (int iter = begin; iter < effectiveFieldCount; ++iter)
                        {
                            object obj = r.GetValue(iter + startBound);
                            values[iter] = obj is DBNull ? null : obj;
                        }
                    }
                    return new DapperRow(table, values);
                };
        }
#else
        internal static Func<IDataReader, object> GetDictionaryDeserializer(IDataRecord reader, int startBound, int length, bool returnNullIfFirstMissing)
        {
            var fieldCount = reader.FieldCount;
            if (length == -1)
            {
                length = fieldCount - startBound;
            }

            if (fieldCount <= startBound)
            {
                throw new ArgumentException(MultiMapSplitExceptionMessage, "splitOn");
            }

            return
                 r =>
                 {
                     IDictionary<string, object> row = new Dictionary<string, object>(length);
                     for (var i = startBound; i < startBound + length; i++)
                     {
                         var tmp = r.GetValue(i);
                         tmp = tmp == DBNull.Value ? null : tmp;
                         row[r.GetName(i)] = tmp;
                         if (returnNullIfFirstMissing && i == startBound && tmp == null)
                         {
                             return null;
                         }
                     }
                     return row;
                 };
        }
#endif

        /// <summary>
        ///     Internal use only
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", false)]
        public static char ReadChar(object value)
        {
            if (value == null || value is DBNull) throw new ArgumentNullException("value");
            var s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", "value");
            return s[0];
        }

        /// <summary>
        ///     Internal use only
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", false)]
        public static char? ReadNullableChar(object value)
        {
            if (value == null || value is DBNull) return null;
            var s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", "value");
            return s[0];
        }


        /// <summary>
        ///     Internal use only
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", true)]
        public static IDbDataParameter FindOrAddParameter(IDataParameterCollection parameters, IDbCommand command,
            string name)
        {
            IDbDataParameter result;
            if (parameters.Contains(name))
            {
                result = (IDbDataParameter) parameters[name];
            }
            else
            {
                result = command.CreateParameter();
                result.ParameterName = name;
                parameters.Add(result);
            }
            return result;
        }

        /// <summary>
        ///     Internal use only
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", false)]
        public static void PackListParameters(IDbCommand command, string namePrefix, object value)
        {
            // initially we tried TVP, however it performs quite poorly.
            // keep in mind SQL support up to 2000 params easily in sp_executesql, needing more is rare

            if (FeatureSupport.Get(command.Connection).Arrays)
            {
                IDbDataParameter arrayParm = command.CreateParameter();
                arrayParm.Value = value ?? DBNull.Value;
                arrayParm.ParameterName = namePrefix;
                command.Parameters.Add(arrayParm);
            }
            else
            {
                var list = value as IEnumerable;
                int count = 0;
                bool isString = value is IEnumerable<string>;
                bool isDbString = value is IEnumerable<DbString>;
                foreach (object item in list)
                {
                    count++;
                    IDbDataParameter listParam = command.CreateParameter();
                    listParam.ParameterName = namePrefix + count;
                    listParam.Value = item ?? DBNull.Value;
                    if (isString)
                    {
                        listParam.Size = 4000;
                        if (item != null && ((string) item).Length > 4000)
                        {
                            listParam.Size = -1;
                        }
                    }
                    if (isDbString && item as DbString != null)
                    {
                        var str = item as DbString;
                        str.AddParameter(command, listParam.ParameterName);
                    }
                    else
                    {
                        command.Parameters.Add(listParam);
                    }
                }

                string regexIncludingUnknown = @"([?@:]" + Regex.Escape(namePrefix) + @")(\s+(?i)unknown(?-i))?";
                if (count == 0)
                {
                    command.CommandText = Regex.Replace(command.CommandText, regexIncludingUnknown, match =>
                    {
                        string variableName = match.Groups[1].Value;
                        if (match.Groups[2].Success)
                        {
                            // looks like an optimize hint; leave it alone!
                            return match.Value;
                        }
                        return "(SELECT " + variableName + " WHERE 1 = 0)";
                    });
                    IDbDataParameter dummyParam = command.CreateParameter();
                    dummyParam.ParameterName = namePrefix;
                    dummyParam.Value = DBNull.Value;
                    command.Parameters.Add(dummyParam);
                }
                else
                {
                    command.CommandText = Regex.Replace(command.CommandText, regexIncludingUnknown, match =>
                    {
                        string variableName = match.Groups[1].Value;
                        if (match.Groups[2].Success)
                        {
                            // looks like an optimize hint; expand it
                            string suffix = match.Groups[2].Value;

                            StringBuilder sb = new StringBuilder(variableName).Append(1).Append(suffix);
                            for (int i = 2; i <= count; i++)
                            {
                                sb.Append(',').Append(variableName).Append(i).Append(suffix);
                            }
                            return sb.ToString();
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder("(").Append(variableName).Append(1);
                            for (int i = 2; i <= count; i++)
                            {
                                sb.Append(',').Append(variableName).Append(i);
                            }
                            return sb.Append(')').ToString();
                        }
                    });
                }
            }
        }

        private static IEnumerable<PropertyInfo> FilterParameters(IEnumerable<PropertyInfo> parameters, string sql)
        {
            return
                parameters.Where(
                    p =>
                        Regex.IsMatch(sql, @"[?@:]" + p.Name + "([^a-zA-Z0-9_]+|$)",
                            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant));
        }


        // look for ? / @ / : *by itself*
        private static readonly Regex smellsLikeOleDb = new Regex(@"(?<![a-zA-Z0-9_])[?@:](?![a-zA-Z0-9_])",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled),
            literalTokens = new Regex(@"\{=([a-zA-Z0-9_]+)\}",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        ///     Represents a placeholder for a value that should be replaced as a literal value in the resulting sql
        /// </summary>
        internal struct LiteralToken
        {
            internal static readonly IList<LiteralToken> None = new LiteralToken[0];
            private readonly string member;
            private readonly string token;

            internal LiteralToken(string token, string member)
            {
                this.token = token;
                this.member = member;
            }

            /// <summary>
            ///     The text in the original command that should be replaced
            /// </summary>
            public string Token
            {
                get { return token; }
            }

            /// <summary>
            ///     The name of the member referred to by the token
            /// </summary>
            public string Member
            {
                get { return member; }
            }
        }

        /// <summary>
        ///     Replace all literal tokens with their text form
        /// </summary>
        public static void ReplaceLiterals(this IParameterLookup parameters, IDbCommand command)
        {
            IList<LiteralToken> tokens = GetLiteralTokens(command.CommandText);
            if (tokens.Count != 0) ReplaceLiterals(parameters, command, tokens);
        }

        internal static readonly MethodInfo format = typeof (SqlMapper).GetMethod("Format",
            BindingFlags.Public | BindingFlags.Static);

        /// <summary>
        ///     Convert numeric values to their string form for SQL literal purposes
        /// </summary>
        [Obsolete("This is intended for internal usage only")]
        public static string Format(object value)
        {
            if (value == null)
            {
                return "null";
            }
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.DBNull:
                    return "null";
                case TypeCode.Boolean:
                    return ((bool) value) ? "1" : "0";
                case TypeCode.Byte:
                    return ((byte) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.SByte:
                    return ((sbyte) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.UInt16:
                    return ((ushort) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Int16:
                    return ((short) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.UInt32:
                    return ((uint) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Int32:
                    return ((int) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.UInt64:
                    return ((ulong) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Int64:
                    return ((long) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Single:
                    return ((float) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Double:
                    return ((double) value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Decimal:
                    return ((decimal) value).ToString(CultureInfo.InvariantCulture);
                default:
                    if (value is IEnumerable && !(value is string))
                    {
                        var sb = new StringBuilder();
                        bool first = true;
                        foreach (object subval in (IEnumerable) value)
                        {
                            sb.Append(first ? '(' : ',').Append(Format(subval));
                            first = false;
                        }
                        if (first)
                        {
                            return "(select null where 1=0)";
                        }
                        return sb.Append(')').ToString();
                    }
                    throw new NotSupportedException(value.GetType().Name);
            }
        }


        internal static void ReplaceLiterals(IParameterLookup parameters, IDbCommand command, IList<LiteralToken> tokens)
        {
            string sql = command.CommandText;
            foreach (LiteralToken token in tokens)
            {
                object value = parameters[token.Member];
#pragma warning disable 0618
                string text = Format(value);
#pragma warning restore 0618
                sql = sql.Replace(token.Token, text);
            }
            command.CommandText = sql;
        }

        internal static IList<LiteralToken> GetLiteralTokens(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return LiteralToken.None;
            if (!literalTokens.IsMatch(sql)) return LiteralToken.None;

            MatchCollection matches = literalTokens.Matches(sql);
            var found = new HashSet<string>(StringComparer.InvariantCulture);
            var list = new List<LiteralToken>(matches.Count);
            foreach (Match match in matches)
            {
                string token = match.Value;
                if (found.Add(match.Value))
                {
                    list.Add(new LiteralToken(token, match.Groups[1].Value));
                }
            }
            return list.Count == 0 ? LiteralToken.None : list;
        }

        /// <summary>
        ///     Internal use only
        /// </summary>
        public static Action<IDbCommand, object> CreateParamInfoGenerator(Identity identity, bool checkForDuplicates,
            bool removeUnused)
        {
            return CreateParamInfoGenerator(identity, checkForDuplicates, removeUnused, GetLiteralTokens(identity.sql));
        }

        internal static Action<IDbCommand, object> CreateParamInfoGenerator(Identity identity, bool checkForDuplicates,
            bool removeUnused, IList<LiteralToken> literals)
        {
            Type type = identity.parametersType;

            bool filterParams = false;
            if (removeUnused && identity.commandType.GetValueOrDefault(CommandType.Text) == CommandType.Text)
            {
                filterParams = !smellsLikeOleDb.IsMatch(identity.sql);
            }
            var dm = new DynamicMethod(string.Format("ParamInfo{0}", Guid.NewGuid()), null,
                new[] {typeof (IDbCommand), typeof (object)}, type, true);

            ILGenerator il = dm.GetILGenerator();

            il.DeclareLocal(type); // 0
            bool haveInt32Arg1 = false;
            il.Emit(OpCodes.Ldarg_1); // stack is now [untyped-param]
            il.Emit(OpCodes.Unbox_Any, type); // stack is now [typed-param]
            il.Emit(OpCodes.Stloc_0); // stack is now empty

            il.Emit(OpCodes.Ldarg_0); // stack is now [command]
            il.EmitCall(OpCodes.Callvirt, typeof (IDbCommand).GetProperty("Parameters").GetGetMethod(), null);
                // stack is now [parameters]

            PropertyInfo[] propsArr = type.GetProperties().Where(p => p.GetIndexParameters().Length == 0).ToArray();
            ConstructorInfo[] ctors = type.GetConstructors();
            ParameterInfo[] ctorParams;
            IEnumerable<PropertyInfo> props = null;
            // try to detect tuple patterns, e.g. anon-types, and use that to choose the order
            // otherwise: alphabetical
            if (ctors.Length == 1 && propsArr.Length == (ctorParams = ctors[0].GetParameters()).Length)
            {
                // check if reflection was kind enough to put everything in the right order for us
                bool ok = true;
                for (int i = 0; i < propsArr.Length; i++)
                {
                    if (
                        !string.Equals(propsArr[i].Name, ctorParams[i].Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    // pre-sorted; the reflection gods have smiled upon us
                    props = propsArr;
                }
                else
                {
                    // might still all be accounted for; check the hard way
                    var positionByName = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (ParameterInfo param in ctorParams)
                    {
                        positionByName[param.Name] = param.Position;
                    }
                    if (positionByName.Count == propsArr.Length)
                    {
                        var positions = new int[propsArr.Length];
                        ok = true;
                        for (int i = 0; i < propsArr.Length; i++)
                        {
                            int pos;
                            if (!positionByName.TryGetValue(propsArr[i].Name, out pos))
                            {
                                ok = false;
                                break;
                            }
                            positions[i] = pos;
                        }
                        if (ok)
                        {
                            Array.Sort(positions, propsArr);
                            props = propsArr;
                        }
                    }
                }
            }
            if (props == null) props = propsArr.OrderBy(x => x.Name);
            if (filterParams)
            {
                props = FilterParameters(props, identity.sql);
            }

            foreach (PropertyInfo prop in props)
            {
                if (typeof (ICustomQueryParameter).IsAssignableFrom(prop.PropertyType))
                {
                    il.Emit(OpCodes.Ldloc_0); // stack is now [parameters] [typed-param]
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod()); // stack is [parameters] [custom]
                    il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [custom] [command]
                    il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [custom] [command] [name]
                    il.EmitCall(OpCodes.Callvirt, prop.PropertyType.GetMethod("AddParameter"), null);
                        // stack is now [parameters]
                    continue;
                }
                ITypeHandler handler;
                DbType dbType = LookupDbType(prop.PropertyType, prop.Name, out handler);
                if (dbType == DynamicParameters.EnumerableMultiParameter)
                {
                    // this actually represents special handling for list types;
                    il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [command]
                    il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [command] [name]
                    il.Emit(OpCodes.Ldloc_0); // stack is now [parameters] [command] [name] [typed-param]
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                        // stack is [parameters] [command] [name] [typed-value]
                    if (prop.PropertyType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, prop.PropertyType); // stack is [parameters] [command] [name] [boxed-value]
                    }
                    il.EmitCall(OpCodes.Call, typeof (SqlMapper).GetMethod("PackListParameters"), null);
                        // stack is [parameters]
                    continue;
                }
                il.Emit(OpCodes.Dup); // stack is now [parameters] [parameters]

                il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [parameters] [command]

                if (checkForDuplicates)
                {
                    // need to be a little careful about adding; use a utility method
                    il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [parameters] [command] [name]
                    il.EmitCall(OpCodes.Call, typeof (SqlMapper).GetMethod("FindOrAddParameter"), null);
                        // stack is [parameters] [parameter]
                }
                else
                {
                    // no risk of duplicates; just blindly add
                    il.EmitCall(OpCodes.Callvirt, typeof (IDbCommand).GetMethod("CreateParameter"), null);
                        // stack is now [parameters] [parameters] [parameter]

                    il.Emit(OpCodes.Dup); // stack is now [parameters] [parameters] [parameter] [parameter]
                    il.Emit(OpCodes.Ldstr, prop.Name);
                        // stack is now [parameters] [parameters] [parameter] [parameter] [name]
                    il.EmitCall(OpCodes.Callvirt, typeof (IDataParameter).GetProperty("ParameterName").GetSetMethod(),
                        null); // stack is now [parameters] [parameters] [parameter]
                }
                if (dbType != DbType.Time && handler == null)
                    // https://connect.microsoft.com/VisualStudio/feedback/details/381934/sqlparameter-dbtype-dbtype-time-sets-the-parameter-to-sqldbtype-datetime-instead-of-sqldbtype-time
                {
                    il.Emit(OpCodes.Dup); // stack is now [parameters] [[parameters]] [parameter] [parameter]
                    EmitInt32(il, (int) dbType);
                        // stack is now [parameters] [[parameters]] [parameter] [parameter] [db-type]

                    il.EmitCall(OpCodes.Callvirt, typeof (IDataParameter).GetProperty("DbType").GetSetMethod(), null);
                        // stack is now [parameters] [[parameters]] [parameter]
                }

                il.Emit(OpCodes.Dup); // stack is now [parameters] [[parameters]] [parameter] [parameter]
                EmitInt32(il, (int) ParameterDirection.Input);
                    // stack is now [parameters] [[parameters]] [parameter] [parameter] [dir]
                il.EmitCall(OpCodes.Callvirt, typeof (IDataParameter).GetProperty("Direction").GetSetMethod(), null);
                    // stack is now [parameters] [[parameters]] [parameter]

                il.Emit(OpCodes.Dup); // stack is now [parameters] [[parameters]] [parameter] [parameter]
                il.Emit(OpCodes.Ldloc_0);
                    // stack is now [parameters] [[parameters]] [parameter] [parameter] [typed-param]
                il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                    // stack is [parameters] [[parameters]] [parameter] [parameter] [typed-value]
                bool checkForNull = true;
                if (prop.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, prop.PropertyType);
                        // stack is [parameters] [[parameters]] [parameter] [parameter] [boxed-value]
                    if (Nullable.GetUnderlyingType(prop.PropertyType) == null)
                    {
                        // struct but not Nullable<T>; boxed value cannot be null
                        checkForNull = false;
                    }
                }
                if (checkForNull)
                {
                    if ((dbType == DbType.String || dbType == DbType.AnsiString) && !haveInt32Arg1)
                    {
                        il.DeclareLocal(typeof (int));
                        haveInt32Arg1 = true;
                    }
                    // relative stack: [boxed value]
                    il.Emit(OpCodes.Dup); // relative stack: [boxed value] [boxed value]
                    Label notNull = il.DefineLabel();
                    Label? allDone = (dbType == DbType.String || dbType == DbType.AnsiString)
                        ? il.DefineLabel()
                        : (Label?) null;
                    il.Emit(OpCodes.Brtrue_S, notNull);
                    // relative stack [boxed value = null]
                    il.Emit(OpCodes.Pop); // relative stack empty
                    il.Emit(OpCodes.Ldsfld, typeof (DBNull).GetField("Value")); // relative stack [DBNull]
                    if (dbType == DbType.String || dbType == DbType.AnsiString)
                    {
                        EmitInt32(il, 0);
                        il.Emit(OpCodes.Stloc_1);
                    }
                    if (allDone != null) il.Emit(OpCodes.Br_S, allDone.Value);
                    il.MarkLabel(notNull);
                    if (prop.PropertyType == typeof (string))
                    {
                        il.Emit(OpCodes.Dup); // [string] [string]
                        il.EmitCall(OpCodes.Callvirt, typeof (string).GetProperty("Length").GetGetMethod(), null);
                            // [string] [length]
                        EmitInt32(il, 4000); // [string] [length] [4000]
                        il.Emit(OpCodes.Cgt); // [string] [0 or 1]
                        Label isLong = il.DefineLabel(), lenDone = il.DefineLabel();
                        il.Emit(OpCodes.Brtrue_S, isLong);
                        EmitInt32(il, 4000); // [string] [4000]
                        il.Emit(OpCodes.Br_S, lenDone);
                        il.MarkLabel(isLong);
                        EmitInt32(il, -1); // [string] [-1]
                        il.MarkLabel(lenDone);
                        il.Emit(OpCodes.Stloc_1); // [string] 
                    }
                    if (prop.PropertyType.FullName == LinqBinary)
                    {
                        il.EmitCall(OpCodes.Callvirt,
                            prop.PropertyType.GetMethod("ToArray", BindingFlags.Public | BindingFlags.Instance), null);
                    }
                    if (allDone != null) il.MarkLabel(allDone.Value);
                    // relative stack [boxed value or DBNull]
                }

                if (handler != null)
                {
#pragma warning disable 618
                    il.Emit(OpCodes.Call,
                        typeof (TypeHandlerCache<>).MakeGenericType(prop.PropertyType).GetMethod("SetValue"));
                        // stack is now [parameters] [[parameters]] [parameter]
#pragma warning restore 618
                }
                else
                {
                    il.EmitCall(OpCodes.Callvirt, typeof (IDataParameter).GetProperty("Value").GetSetMethod(), null);
                        // stack is now [parameters] [[parameters]] [parameter]
                }

                if (prop.PropertyType == typeof (string))
                {
                    Label endOfSize = il.DefineLabel();
                    // don't set if 0
                    il.Emit(OpCodes.Ldloc_1); // [parameters] [[parameters]] [parameter] [size]
                    il.Emit(OpCodes.Brfalse_S, endOfSize); // [parameters] [[parameters]] [parameter]

                    il.Emit(OpCodes.Dup); // stack is now [parameters] [[parameters]] [parameter] [parameter]
                    il.Emit(OpCodes.Ldloc_1); // stack is now [parameters] [[parameters]] [parameter] [parameter] [size]
                    il.EmitCall(OpCodes.Callvirt, typeof (IDbDataParameter).GetProperty("Size").GetSetMethod(), null);
                        // stack is now [parameters] [[parameters]] [parameter]

                    il.MarkLabel(endOfSize);
                }
                if (checkForDuplicates)
                {
                    // stack is now [parameters] [parameter]
                    il.Emit(OpCodes.Pop); // don't need parameter any more
                }
                else
                {
                    // stack is now [parameters] [parameters] [parameter]
                    // blindly add
                    il.EmitCall(OpCodes.Callvirt, typeof (IList).GetMethod("Add"), null); // stack is now [parameters]
                    il.Emit(OpCodes.Pop); // IList.Add returns the new index (int); we don't care
                }
            }

            // stack is currently [parameters]
            il.Emit(OpCodes.Pop); // stack is now empty

            if (literals.Count != 0 && propsArr != null)
            {
                il.Emit(OpCodes.Ldarg_0); // command
                il.Emit(OpCodes.Ldarg_0); // command, command
                PropertyInfo cmdText = typeof (IDbCommand).GetProperty("CommandText");
                il.EmitCall(OpCodes.Callvirt, cmdText.GetGetMethod(), null); // command, sql
                Dictionary<Type, LocalBuilder> locals = null;
                LocalBuilder local = null;
                foreach (LiteralToken literal in literals)
                {
                    // find the best member, preferring case-sensitive
                    PropertyInfo exact = null, fallback = null;
                    string huntName = literal.Member;
                    for (int i = 0; i < propsArr.Length; i++)
                    {
                        string thisName = propsArr[i].Name;
                        if (string.Equals(thisName, huntName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            fallback = propsArr[i];
                            if (string.Equals(thisName, huntName, StringComparison.InvariantCulture))
                            {
                                exact = fallback;
                                break;
                            }
                        }
                    }
                    PropertyInfo prop = exact ?? fallback;

                    if (prop != null)
                    {
                        il.Emit(OpCodes.Ldstr, literal.Token);
                        il.Emit(OpCodes.Ldloc_0); // command, sql, typed parameter
                        il.EmitCall(OpCodes.Callvirt, prop.GetGetMethod(), null); // command, sql, typed value
                        Type propType = prop.PropertyType;
                        TypeCode typeCode = Type.GetTypeCode(propType);
                        switch (typeCode)
                        {
                            case TypeCode.Boolean:
                            case TypeCode.Byte:
                            case TypeCode.SByte:
                            case TypeCode.UInt16:
                            case TypeCode.Int16:
                            case TypeCode.UInt32:
                            case TypeCode.Int32:
                            case TypeCode.UInt64:
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                // neeed to stloc, ldloca, call
                                // re-use existing locals (both the last known, and via a dictionary)
                                MethodInfo convert = GetToString(typeCode);
                                if (local == null || local.LocalType != propType)
                                {
                                    if (locals == null)
                                    {
                                        locals = new Dictionary<Type, LocalBuilder>();
                                        local = null;
                                    }
                                    else
                                    {
                                        if (!locals.TryGetValue(propType, out local)) local = null;
                                    }
                                    if (local == null)
                                    {
                                        local = il.DeclareLocal(propType);
                                        locals.Add(propType, local);
                                    }
                                }
                                il.Emit(OpCodes.Stloc, local); // command, sql
                                il.Emit(OpCodes.Ldloca, local); // command, sql, ref-to-value
                                il.EmitCall(OpCodes.Call, InvariantCulture, null);
                                    // command, sql, ref-to-value, culture
                                il.EmitCall(OpCodes.Call, convert, null); // command, sql, string value
                                break;
                            default:
                                if (propType.IsValueType) il.Emit(OpCodes.Box, propType); // command, sql, object value
                                il.EmitCall(OpCodes.Call, format, null); // command, sql, string value
                                break;
                        }
                        il.EmitCall(OpCodes.Callvirt, StringReplace, null);
                    }
                }
                il.EmitCall(OpCodes.Callvirt, cmdText.GetSetMethod(), null); // empty
            }

            il.Emit(OpCodes.Ret);
            return (Action<IDbCommand, object>) dm.CreateDelegate(typeof (Action<IDbCommand, object>));
        }

        private static readonly Dictionary<TypeCode, MethodInfo> toStrings = new[]
        {
            typeof (bool), typeof (sbyte), typeof (byte), typeof (ushort), typeof (short),
            typeof (uint), typeof (int), typeof (ulong), typeof (long), typeof (float), typeof (double),
            typeof (decimal)
        }.ToDictionary(x => Type.GetTypeCode(x),
            x =>
                x.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null,
                    new[] {typeof (IFormatProvider)}, null));

        private static MethodInfo GetToString(TypeCode typeCode)
        {
            MethodInfo method;
            return toStrings.TryGetValue(typeCode, out method) ? method : null;
        }

        private static readonly MethodInfo StringReplace = typeof (string).GetMethod("Replace",
            BindingFlags.Instance | BindingFlags.Public, null, new[] {typeof (string), typeof (string)}, null),
            InvariantCulture =
                typeof (CultureInfo).GetProperty("InvariantCulture", BindingFlags.Public | BindingFlags.Static)
                    .GetGetMethod();

        private static int ExecuteCommand(IDbConnection cnn, ref CommandDefinition command,
            Action<IDbCommand, object> paramReader)
        {
            IDbCommand cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                cmd = command.SetupCommand(cnn, paramReader);
                if (wasClosed) cnn.Open();
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                if (wasClosed) cnn.Close();
                if (cmd != null) cmd.Dispose();
            }
        }

        private static T ExecuteScalarImpl<T>(IDbConnection cnn, ref CommandDefinition command)
        {
            Action<IDbCommand, object> paramReader = null;
            object param = command.Parameters;
            if (param != null)
            {
                var identity = new Identity(command.CommandText, command.CommandType, cnn, null, param.GetType(), null);
                paramReader = GetCacheInfo(identity, command.Parameters, command.AddToCache).ParamReader;
            }

            IDbCommand cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            object result;
            try
            {
                cmd = command.SetupCommand(cnn, paramReader);
                if (wasClosed) cnn.Open();
                result = cmd.ExecuteScalar();
            }
            finally
            {
                if (wasClosed) cnn.Close();
                if (cmd != null) cmd.Dispose();
            }
            return Parse<T>(result);
        }

        private static IDataReader ExecuteReaderImpl(IDbConnection cnn, ref CommandDefinition command,
            CommandBehavior commandBehavior)
        {
            Action<IDbCommand, object> paramReader = GetParameterReader(cnn, ref command);

            IDbCommand cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                cmd = command.SetupCommand(cnn, paramReader);
                if (wasClosed) cnn.Open();
                if (wasClosed) commandBehavior |= CommandBehavior.CloseConnection;
                IDataReader reader = cmd.ExecuteReader(commandBehavior);
                wasClosed = false; // don't dispose before giving it to them!
                return reader;
            }
            finally
            {
                if (wasClosed) cnn.Close();
                if (cmd != null) cmd.Dispose();
            }
        }

        private static Action<IDbCommand, object> GetParameterReader(IDbConnection cnn, ref CommandDefinition command)
        {
            object param = command.Parameters;
            var multiExec = param as IEnumerable;
            Identity identity;
            CacheInfo info = null;
            if (multiExec != null && !(multiExec is string))
            {
                throw new NotSupportedException("MultiExec is not supported by ExecuteReader");
            }

            // nice and simple
            if (param != null)
            {
                identity = new Identity(command.CommandText, command.CommandType, cnn, null, param.GetType(), null);
                info = GetCacheInfo(identity, param, command.AddToCache);
            }
            Action<IDbCommand, object> paramReader = info == null ? null : info.ParamReader;
            return paramReader;
        }

        private static Func<IDataReader, object> GetStructDeserializer(Type type, Type effectiveType, int index)
        {
            // no point using special per-type handling here; it boils down to the same, plus not all are supported anyway (see: SqlDataReader.GetChar - not supported!)
#pragma warning disable 618
            if (type == typeof (char))
            {
                // this *does* need special handling, though
                return r => SqlMapper.ReadChar(r.GetValue(index));
            }
            if (type == typeof (char?))
            {
                return r => SqlMapper.ReadNullableChar(r.GetValue(index));
            }
            if (type.FullName == LinqBinary)
            {
                return r => Activator.CreateInstance(type, r.GetValue(index));
            }
#pragma warning restore 618

            if (effectiveType.IsEnum)
            {
                // assume the value is returned as the correct type (int/byte/etc), but box back to the typed enum
                return r =>
                {
                    object val = r.GetValue(index);
                    if (val is float || val is double || val is decimal)
                    {
                        val = Convert.ChangeType(val, Enum.GetUnderlyingType(effectiveType),
                            CultureInfo.InvariantCulture);
                    }
                    return val is DBNull ? null : Enum.ToObject(effectiveType, val);
                };
            }
            ITypeHandler handler;
            if (typeHandlers.TryGetValue(type, out handler))
            {
                return r =>
                {
                    object val = r.GetValue(index);
                    return val is DBNull ? null : handler.Parse(type, val);
                };
            }
            return r =>
            {
                object val = r.GetValue(index);
                return val is DBNull ? null : val;
            };
        }

        private static T Parse<T>(object value)
        {
            if (value == null || value is DBNull) return default(T);
            if (value is T) return (T) value;
            Type type = typeof (T);
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum)
            {
                if (value is float || value is double || value is decimal)
                {
                    value = Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                }
                return (T) Enum.ToObject(type, value);
            }
            ITypeHandler handler;
            if (typeHandlers.TryGetValue(type, out handler))
            {
                return (T) handler.Parse(type, value);
            }
            return (T) Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        private static readonly MethodInfo
            enumParse = typeof (Enum).GetMethod("Parse", new[] {typeof (Type), typeof (string), typeof (bool)}),
            getItem = typeof (IDataRecord).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetIndexParameters().Any() && p.GetIndexParameters()[0].ParameterType == typeof (int))
                .Select(p => p.GetGetMethod()).First();

        /// <summary>
        ///     Gets type-map for the given type
        /// </summary>
        /// <returns>Type map implementation, DefaultTypeMap instance if no override present</returns>
        public static ITypeMap GetTypeMap(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            var map = (ITypeMap) _typeMaps[type];
            if (map == null)
            {
                lock (_typeMaps)
                {
                    // double-checked; store this to avoid reflection next time we see this type
                    // since multiple queries commonly use the same domain-entity/DTO/view-model type
                    map = (ITypeMap) _typeMaps[type];
                    if (map == null)
                    {
                        map = new DefaultTypeMap(type);
                        _typeMaps[type] = map;
                    }
                }
            }
            return map;
        }

        // use Hashtable to get free lockless reading
        private static readonly Hashtable _typeMaps = new Hashtable();

        /// <summary>
        ///     Set custom mapping for type deserializers
        /// </summary>
        /// <param name="type">Entity type to override</param>
        /// <param name="map">Mapping rules impementation, null to remove custom map</param>
        public static void SetTypeMap(Type type, ITypeMap map)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (map == null || map is DefaultTypeMap)
            {
                lock (_typeMaps)
                {
                    _typeMaps.Remove(type);
                }
            }
            else
            {
                lock (_typeMaps)
                {
                    _typeMaps[type] = map;
                }
            }

            PurgeQueryCacheByType(type);
        }

        /// <summary>
        ///     Internal use only
        /// </summary>
        /// <param name="type"></param>
        /// <param name="reader"></param>
        /// <param name="startBound"></param>
        /// <param name="length"></param>
        /// <param name="returnNullIfFirstMissing"></param>
        /// <returns></returns>
        public static Func<IDataReader, object> GetTypeDeserializer(
#if CSHARP30
Type type, IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing
#else
            Type type, IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false
#endif
            )
        {
            var dm = new DynamicMethod(string.Format("Deserialize{0}", Guid.NewGuid()), typeof (object),
                new[] {typeof (IDataReader)}, true);
            ILGenerator il = dm.GetILGenerator();
            il.DeclareLocal(typeof (int));
            il.DeclareLocal(type);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0);

            if (length == -1)
            {
                length = reader.FieldCount - startBound;
            }

            if (reader.FieldCount <= startBound)
            {
                throw new ArgumentException(MultiMapSplitExceptionMessage, "splitOn");
            }

            string[] names = Enumerable.Range(startBound, length).Select(i => reader.GetName(i)).ToArray();

            ITypeMap typeMap = GetTypeMap(type);

            int index = startBound;

            ConstructorInfo specializedConstructor = null;

            bool supportInitialize = false;
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Ldloca_S, (byte) 1);
                il.Emit(OpCodes.Initobj, type);
            }
            else
            {
                var types = new Type[length];
                for (int i = startBound; i < startBound + length; i++)
                {
                    types[i - startBound] = reader.GetFieldType(i);
                }

                ConstructorInfo ctor = typeMap.FindConstructor(names, types);
                if (ctor == null)
                {
                    string proposedTypes = "(" +
                                           string.Join(", ",
                                               types.Select((t, i) => t.FullName + " " + names[i]).ToArray()) + ")";
                    throw new InvalidOperationException(
                        string.Format(
                            "A parameterless default constructor or one matching signature {0} is required for {1} materialization",
                            proposedTypes, type.FullName));
                }

                if (ctor.GetParameters().Length == 0)
                {
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Stloc_1);
                    supportInitialize = typeof (ISupportInitialize).IsAssignableFrom(type);
                    if (supportInitialize)
                    {
                        il.Emit(OpCodes.Ldloc_1);
                        il.EmitCall(OpCodes.Callvirt, typeof (ISupportInitialize).GetMethod("BeginInit"), null);
                    }
                }
                else
                    specializedConstructor = ctor;
            }

            il.BeginExceptionBlock();
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Ldloca_S, (byte) 1); // [target]
            }
            else if (specializedConstructor == null)
            {
                il.Emit(OpCodes.Ldloc_1); // [target]
            }

            List<IMemberMap> members = (specializedConstructor != null
                ? names.Select(n => typeMap.GetConstructorParameter(specializedConstructor, n))
                : names.Select(n => typeMap.GetMember(n))).ToList();

            // stack is now [target]

            bool first = true;
            Label allDone = il.DefineLabel();
            int enumDeclareLocal = -1, valueCopyLocal = il.DeclareLocal(typeof (object)).LocalIndex;
            foreach (IMemberMap item in members)
            {
                if (item != null)
                {
                    if (specializedConstructor == null)
                        il.Emit(OpCodes.Dup); // stack is now [target][target]
                    Label isDbNullLabel = il.DefineLabel();
                    Label finishLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_0); // stack is now [target][target][reader]
                    EmitInt32(il, index); // stack is now [target][target][reader][index]
                    il.Emit(OpCodes.Dup); // stack is now [target][target][reader][index][index]
                    il.Emit(OpCodes.Stloc_0); // stack is now [target][target][reader][index]
                    il.Emit(OpCodes.Callvirt, getItem); // stack is now [target][target][value-as-object]
                    il.Emit(OpCodes.Dup); // stack is now [target][target][value-as-object][value-as-object]
                    StoreLocal(il, valueCopyLocal);
                    Type colType = reader.GetFieldType(index);
                    Type memberType = item.MemberType;

                    if (memberType == typeof (char) || memberType == typeof (char?))
                    {
                        il.EmitCall(OpCodes.Call, typeof (SqlMapper).GetMethod(
                            memberType == typeof (char) ? "ReadChar" : "ReadNullableChar",
                            BindingFlags.Static | BindingFlags.Public), null);
                            // stack is now [target][target][typed-value]
                    }
                    else
                    {
                        il.Emit(OpCodes.Dup); // stack is now [target][target][value][value]
                        il.Emit(OpCodes.Isinst, typeof (DBNull));
                            // stack is now [target][target][value-as-object][DBNull or null]
                        il.Emit(OpCodes.Brtrue_S, isDbNullLabel); // stack is now [target][target][value-as-object]

                        // unbox nullable enums as the primitive, i.e. byte etc

                        Type nullUnderlyingType = Nullable.GetUnderlyingType(memberType);
                        Type unboxType = nullUnderlyingType != null && nullUnderlyingType.IsEnum
                            ? nullUnderlyingType
                            : memberType;

                        if (unboxType.IsEnum)
                        {
                            Type numericType = Enum.GetUnderlyingType(unboxType);
                            if (colType == typeof (string))
                            {
                                if (enumDeclareLocal == -1)
                                {
                                    enumDeclareLocal = il.DeclareLocal(typeof (string)).LocalIndex;
                                }
                                il.Emit(OpCodes.Castclass, typeof (string)); // stack is now [target][target][string]
                                StoreLocal(il, enumDeclareLocal); // stack is now [target][target]
                                il.Emit(OpCodes.Ldtoken, unboxType); // stack is now [target][target][enum-type-token]
                                il.EmitCall(OpCodes.Call, typeof (Type).GetMethod("GetTypeFromHandle"), null);
                                    // stack is now [target][target][enum-type]
                                LoadLocal(il, enumDeclareLocal); // stack is now [target][target][enum-type][string]
                                il.Emit(OpCodes.Ldc_I4_1); // stack is now [target][target][enum-type][string][true]
                                il.EmitCall(OpCodes.Call, enumParse, null);
                                    // stack is now [target][target][enum-as-object]
                                il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [target][target][typed-value]
                            }
                            else
                            {
                                FlexibleConvertBoxedFromHeadOfStack(il, colType, unboxType, numericType);
                            }

                            if (nullUnderlyingType != null)
                            {
                                il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] {nullUnderlyingType}));
                                    // stack is now [target][target][typed-value]
                            }
                        }
                        else if (memberType.FullName == LinqBinary)
                        {
                            il.Emit(OpCodes.Unbox_Any, typeof (byte[])); // stack is now [target][target][byte-array]
                            il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] {typeof (byte[])}));
                                // stack is now [target][target][binary]
                        }
                        else
                        {
                            TypeCode dataTypeCode = Type.GetTypeCode(colType),
                                unboxTypeCode = Type.GetTypeCode(unboxType);
                            bool hasTypeHandler;
                            if ((hasTypeHandler = typeHandlers.ContainsKey(unboxType)) || colType == unboxType ||
                                dataTypeCode == unboxTypeCode || dataTypeCode == Type.GetTypeCode(nullUnderlyingType))
                            {
                                if (hasTypeHandler)
                                {
#pragma warning disable 618
                                    il.EmitCall(OpCodes.Call,
                                        typeof (TypeHandlerCache<>).MakeGenericType(unboxType).GetMethod("Parse"), null);
                                        // stack is now [target][target][typed-value]
#pragma warning restore 618
                                }
                                else
                                {
                                    il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [target][target][typed-value]
                                }
                            }
                            else
                            {
                                // not a direct match; need to tweak the unbox
                                FlexibleConvertBoxedFromHeadOfStack(il, colType, nullUnderlyingType ?? unboxType, null);
                                if (nullUnderlyingType != null)
                                {
                                    il.Emit(OpCodes.Newobj, unboxType.GetConstructor(new[] {nullUnderlyingType}));
                                        // stack is now [target][target][typed-value]
                                }
                            }
                        }
                    }
                    if (specializedConstructor == null)
                    {
                        // Store the value in the property/field
                        if (item.Property != null)
                        {
                            if (type.IsValueType)
                            {
                                il.Emit(OpCodes.Call, DefaultTypeMap.GetPropertySetter(item.Property, type));
                                    // stack is now [target]
                            }
                            else
                            {
                                il.Emit(OpCodes.Callvirt, DefaultTypeMap.GetPropertySetter(item.Property, type));
                                    // stack is now [target]
                            }
                        }
                        else
                        {
                            il.Emit(OpCodes.Stfld, item.Field); // stack is now [target]
                        }
                    }

                    il.Emit(OpCodes.Br_S, finishLabel); // stack is now [target]

                    il.MarkLabel(isDbNullLabel); // incoming stack: [target][target][value]
                    if (specializedConstructor != null)
                    {
                        il.Emit(OpCodes.Pop);
                        if (item.MemberType.IsValueType)
                        {
                            int localIndex = il.DeclareLocal(item.MemberType).LocalIndex;
                            LoadLocalAddress(il, localIndex);
                            il.Emit(OpCodes.Initobj, item.MemberType);
                            LoadLocal(il, localIndex);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldnull);
                        }
                    }
                    else
                    {
                        il.Emit(OpCodes.Pop); // stack is now [target][target]
                        il.Emit(OpCodes.Pop); // stack is now [target]
                    }

                    if (first && returnNullIfFirstMissing)
                    {
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ldnull); // stack is now [null]
                        il.Emit(OpCodes.Stloc_1);
                        il.Emit(OpCodes.Br, allDone);
                    }

                    il.MarkLabel(finishLabel);
                }
                first = false;
                index += 1;
            }
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                if (specializedConstructor != null)
                {
                    il.Emit(OpCodes.Newobj, specializedConstructor);
                }
                il.Emit(OpCodes.Stloc_1); // stack is empty
                if (supportInitialize)
                {
                    il.Emit(OpCodes.Ldloc_1);
                    il.EmitCall(OpCodes.Callvirt, typeof (ISupportInitialize).GetMethod("EndInit"), null);
                }
            }
            il.MarkLabel(allDone);
            il.BeginCatchBlock(typeof (Exception)); // stack is Exception
            il.Emit(OpCodes.Ldloc_0); // stack is Exception, index
            il.Emit(OpCodes.Ldarg_0); // stack is Exception, index, reader
            LoadLocal(il, valueCopyLocal); // stack is Exception, index, reader, value
            il.EmitCall(OpCodes.Call, typeof (SqlMapper).GetMethod("ThrowDataException"), null);
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldloc_1); // stack is [rval]
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
            il.Emit(OpCodes.Ret);

            return (Func<IDataReader, object>) dm.CreateDelegate(typeof (Func<IDataReader, object>));
        }

        private static void FlexibleConvertBoxedFromHeadOfStack(ILGenerator il, Type from, Type to, Type via)
        {
            MethodInfo op;
            if (from == (via ?? to))
            {
                il.Emit(OpCodes.Unbox_Any, to); // stack is now [target][target][typed-value]
            }
            else if ((op = GetOperator(from, to)) != null)
            {
                // this is handy for things like decimal <===> double
                il.Emit(OpCodes.Unbox_Any, from); // stack is now [target][target][data-typed-value]
                il.Emit(OpCodes.Call, op); // stack is now [target][target][typed-value]
            }
            else
            {
                bool handled = false;
                OpCode opCode = default(OpCode);
                switch (Type.GetTypeCode(from))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                        handled = true;
                        switch (Type.GetTypeCode(via ?? to))
                        {
                            case TypeCode.Byte:
                                opCode = OpCodes.Conv_Ovf_I1_Un;
                                break;
                            case TypeCode.SByte:
                                opCode = OpCodes.Conv_Ovf_I1;
                                break;
                            case TypeCode.UInt16:
                                opCode = OpCodes.Conv_Ovf_I2_Un;
                                break;
                            case TypeCode.Int16:
                                opCode = OpCodes.Conv_Ovf_I2;
                                break;
                            case TypeCode.UInt32:
                                opCode = OpCodes.Conv_Ovf_I4_Un;
                                break;
                            case TypeCode.Boolean: // boolean is basically an int, at least at this level
                            case TypeCode.Int32:
                                opCode = OpCodes.Conv_Ovf_I4;
                                break;
                            case TypeCode.UInt64:
                                opCode = OpCodes.Conv_Ovf_I8_Un;
                                break;
                            case TypeCode.Int64:
                                opCode = OpCodes.Conv_Ovf_I8;
                                break;
                            case TypeCode.Single:
                                opCode = OpCodes.Conv_R4;
                                break;
                            case TypeCode.Double:
                                opCode = OpCodes.Conv_R8;
                                break;
                            default:
                                handled = false;
                                break;
                        }
                        break;
                }
                if (handled)
                {
                    il.Emit(OpCodes.Unbox_Any, from); // stack is now [target][target][col-typed-value]
                    il.Emit(opCode); // stack is now [target][target][typed-value]
                    if (to == typeof (bool))
                    {
                        // compare to zero; I checked "csc" - this is the trick it uses; nice
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                    }
                }
                else
                {
                    il.Emit(OpCodes.Ldtoken, via ?? to); // stack is now [target][target][value][member-type-token]
                    il.EmitCall(OpCodes.Call, typeof (Type).GetMethod("GetTypeFromHandle"), null);
                        // stack is now [target][target][value][member-type]
                    il.EmitCall(OpCodes.Call,
                        typeof (Convert).GetMethod("ChangeType", new[] {typeof (object), typeof (Type)}), null);
                        // stack is now [target][target][boxed-member-type-value]
                    il.Emit(OpCodes.Unbox_Any, to); // stack is now [target][target][typed-value]
                }
            }
        }

        private static MethodInfo GetOperator(Type from, Type to)
        {
            if (to == null) return null;
            MethodInfo[] fromMethods, toMethods;
            return ResolveOperator(fromMethods = from.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to,
                "op_Implicit")
                   ??
                   ResolveOperator(toMethods = to.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to,
                       "op_Implicit")
                   ?? ResolveOperator(fromMethods, from, to, "op_Explicit")
                   ?? ResolveOperator(toMethods, from, to, "op_Explicit");
        }

        private static MethodInfo ResolveOperator(MethodInfo[] methods, Type from, Type to, string name)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != name || methods[i].ReturnType != to) continue;
                ParameterInfo[] args = methods[i].GetParameters();
                if (args.Length != 1 || args[0].ParameterType != from) continue;
                return methods[i];
            }
            return null;
        }

        private static void LoadLocal(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException("index");
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Ldloc_S, (byte) index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc, (short) index);
                    }
                    break;
            }
        }

        private static void StoreLocal(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException("index");
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Stloc_3);
                    break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Stloc_S, (byte) index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc, (short) index);
                    }
                    break;
            }
        }

        private static void LoadLocalAddress(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException("index");

            if (index <= 255)
            {
                il.Emit(OpCodes.Ldloca_S, (byte) index);
            }
            else
            {
                il.Emit(OpCodes.Ldloca, (short) index);
            }
        }

        /// <summary>
        ///     Throws a data exception, only used internally
        /// </summary>
        [Obsolete("Intended for internal use only")]
        public static void ThrowDataException(Exception ex, int index, IDataReader reader, object value)
        {
            Exception toThrow;
            try
            {
                string name = "(n/a)", formattedValue = "(n/a)";
                if (reader != null && index >= 0 && index < reader.FieldCount)
                {
                    name = reader.GetName(index);
                    try
                    {
                        if (value == null || value is DBNull)
                        {
                            formattedValue = "<null>";
                        }
                        else
                        {
                            formattedValue = Convert.ToString(value) + " - " + Type.GetTypeCode(value.GetType());
                        }
                    }
                    catch (Exception valEx)
                    {
                        formattedValue = valEx.Message;
                    }
                }
                toThrow =
                    new DataException(string.Format("Error parsing column {0} ({1}={2})", index, name, formattedValue),
                        ex);
            }
            catch
            {
                // throw the **original** exception, wrapped as DataException
                toThrow = new DataException(ex.Message, ex);
            }
            throw toThrow;
        }

        private static void EmitInt32(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte) value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }


        /// <summary>
        ///     Key used to indicate the type name associated with a DataTable
        /// </summary>
        private const string DataTableTypeNameKey = "dapper:TypeName";

        /// <summary>
        ///     How should connection strings be compared for equivalence? Defaults to StringComparer.Ordinal.
        ///     Providing a custom implementation can be useful for allowing multi-tenancy databases with identical
        ///     schema to share startegies. Note that usual equivalence rules apply: any equivalent connection strings
        ///     <b>MUST</b> yield the same hash-code.
        /// </summary>
        public static IEqualityComparer<string> ConnectionStringComparer
        {
            get { return connectionStringComparer; }
            set { connectionStringComparer = value ?? StringComparer.Ordinal; }
        }

        private static IEqualityComparer<string> connectionStringComparer = StringComparer.Ordinal;


        /// <summary>
        ///     The grid reader provides interfaces for reading multiple result sets from a Dapper query
        /// </summary>
        public class GridReader : IDisposable
        {
            private IDataReader reader;
            private IDbCommand command;
            private readonly Identity identity;

            internal GridReader(IDbCommand command, IDataReader reader, Identity identity)
            {
                this.command = command;
                this.reader = reader;
                this.identity = identity;
            }

#if !CSHARP30

            /// <summary>
            ///     Read the next grid of results, returned as a dynamic object
            /// </summary>
            public IEnumerable<dynamic> Read(bool buffered = true)
            {
                return ReadImpl<dynamic>(typeof (DapperRow), buffered);
            }
#endif

#if CSHARP30
    /// <summary>
    /// Read the next grid of results
    /// </summary>
            public IEnumerable<T> Read<T>()
            {
                return Read<T>(true);
            }
#endif

            /// <summary>
            ///     Read the next grid of results
            /// </summary>
#if CSHARP30
            public IEnumerable<T> Read<T>(bool buffered)
#else
            public IEnumerable<T> Read<T>(bool buffered = true)
#endif
            {
                return ReadImpl<T>(typeof (T), buffered);
            }

            /// <summary>
            ///     Read the next grid of results
            /// </summary>
#if CSHARP30
            public IEnumerable<object> Read(Type type, bool buffered)
#else
            public IEnumerable<object> Read(Type type, bool buffered = true)
#endif
            {
                if (type == null) throw new ArgumentNullException("type");
                return ReadImpl<object>(type, buffered);
            }

            private IEnumerable<T> ReadImpl<T>(Type type, bool buffered)
            {
                if (reader == null)
                    throw new ObjectDisposedException(GetType().FullName,
                        "The reader has been disposed; this can happen after all data has been consumed");
                if (consumed)
                    throw new InvalidOperationException(
                        "Query results must be consumed in the correct order, and each result can only be consumed once");
                Identity typedIdentity = identity.ForGrid(type, gridIndex);
                CacheInfo cache = GetCacheInfo(typedIdentity, null, true);
                DeserializerState deserializer = cache.Deserializer;

                int hash = GetColumnHash(reader);
                if (deserializer.Func == null || deserializer.Hash != hash)
                {
                    deserializer = new DeserializerState(hash, GetDeserializer(type, reader, 0, -1, false));
                    cache.Deserializer = deserializer;
                }
                consumed = true;
                IEnumerable<T> result = ReadDeferred<T>(gridIndex, deserializer.Func, typedIdentity);
                return buffered ? result.ToList() : result;
            }


            private IEnumerable<TReturn> MultiReadInternal
                <TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Delegate func, string splitOn)
            {
                Identity identity = this.identity.ForGrid(typeof (TReturn), new[]
                {
                    typeof (TFirst),
                    typeof (TSecond),
                    typeof (TThird),
                    typeof (TFourth),
                    typeof (TFifth),
                    typeof (TSixth),
                    typeof (TSeventh)
                }, gridIndex);
                try
                {
                    foreach (
                        TReturn r in
                            SqlMapper.MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
                                null, default(CommandDefinition), func, splitOn, reader, identity))
                    {
                        yield return r;
                    }
                }
                finally
                {
                    NextResult();
                }
            }

#if CSHARP30
    /// <summary>
    /// Read multiple objects from a single recordset on the grid
    /// </summary>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn)
            {
                return Read<TFirst, TSecond, TReturn>(func, splitOn, true);
            }
#endif

            /// <summary>
            ///     Read multiple objects from a single recordset on the grid
            /// </summary>
#if CSHARP30
            public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn, bool buffered)
#else
            public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func,
                string splitOn = "id", bool buffered = true)
#endif
            {
                IEnumerable<TReturn> result =
                    MultiReadInternal<TFirst, TSecond, DontMap, DontMap, DontMap, DontMap, DontMap, TReturn>(func,
                        splitOn);
                return buffered ? result.ToList() : result;
            }

#if CSHARP30
    /// <summary>
    /// Read multiple objects from a single recordset on the grid
    /// </summary>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn)
            {
                return Read<TFirst, TSecond, TThird, TReturn>(func, splitOn, true);
            }
#endif

            /// <summary>
            ///     Read multiple objects from a single recordset on the grid
            /// </summary>
#if CSHARP30
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn, bool buffered)
#else
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(
                Func<TFirst, TSecond, TThird, TReturn> func, string splitOn = "id", bool buffered = true)
#endif
            {
                IEnumerable<TReturn> result =
                    MultiReadInternal<TFirst, TSecond, TThird, DontMap, DontMap, DontMap, DontMap, TReturn>(func,
                        splitOn);
                return buffered ? result.ToList() : result;
            }

#if CSHARP30
    /// <summary>
    /// Read multiple objects from a single record set on the grid
    /// </summary>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn)
            {
                return Read<TFirst, TSecond, TThird, TFourth, TReturn>(func, splitOn, true);
            }
#endif

            /// <summary>
            ///     Read multiple objects from a single record set on the grid
            /// </summary>
#if CSHARP30
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn, bool buffered)
#else
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(
                Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn = "id", bool buffered = true)
#endif
            {
                IEnumerable<TReturn> result =
                    MultiReadInternal<TFirst, TSecond, TThird, TFourth, DontMap, DontMap, DontMap, TReturn>(func,
                        splitOn);
                return buffered ? result.ToList() : result;
            }


#if !CSHARP30
            /// <summary>
            ///     Read multiple objects from a single record set on the grid
            /// </summary>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(
                Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> func, string splitOn = "id",
                bool buffered = true)
            {
                IEnumerable<TReturn> result =
                    MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, DontMap, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            /// <summary>
            ///     Read multiple objects from a single record set on the grid
            /// </summary>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(
                Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> func, string splitOn = "id",
                bool buffered = true)
            {
                IEnumerable<TReturn> result =
                    MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            /// <summary>
            ///     Read multiple objects from a single record set on the grid
            /// </summary>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
                Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> func, string splitOn = "id",
                bool buffered = true)
            {
                IEnumerable<TReturn> result =
                    MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }
#endif

            private IEnumerable<T> ReadDeferred<T>(int index, Func<IDataReader, object> deserializer,
                Identity typedIdentity)
            {
                try
                {
                    while (index == gridIndex && reader.Read())
                    {
                        yield return (T) deserializer(reader);
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    if (index == gridIndex)
                    {
                        NextResult();
                    }
                }
            }

            private int gridIndex, readCount;
            private bool consumed;

            /// <summary>
            ///     Has the underlying reader been consumed?
            /// </summary>
            public bool IsConsumed
            {
                get { return consumed; }
            }

            private void NextResult()
            {
                if (reader.NextResult())
                {
                    readCount++;
                    gridIndex++;
                    consumed = false;
                }
                else
                {
                    // happy path; close the reader cleanly - no
                    // need for "Cancel" etc
                    reader.Dispose();
                    reader = null;

                    Dispose();
                }
            }

            /// <summary>
            ///     Dispose the grid, closing and disposing both the underlying reader and command.
            /// </summary>
            public void Dispose()
            {
                if (reader != null)
                {
                    if (!reader.IsClosed && command != null) command.Cancel();
                    reader.Dispose();
                    reader = null;
                }
                if (command != null)
                {
                    command.Dispose();
                    command = null;
                }
            }
        }

        /// <summary>
        ///     Used to pass a DataTable as a TableValuedParameter
        /// </summary>
        public static ICustomQueryParameter AsTableValuedParameter(this DataTable table, string typeName
#if !CSHARP30
            = null
#endif
            )
        {
            return new TableValuedParameter(table, typeName);
        }

        /// <summary>
        ///     Associate a DataTable with a type name
        /// </summary>
        public static void SetTypeName(this DataTable table, string typeName)
        {
            if (table != null)
            {
                if (string.IsNullOrEmpty(typeName))
                    table.ExtendedProperties.Remove(DataTableTypeNameKey);
                else
                    table.ExtendedProperties[DataTableTypeNameKey] = typeName;
            }
        }

        /// <summary>
        ///     Fetch the type name associated with a DataTable
        /// </summary>
        public static string GetTypeName(this DataTable table)
        {
            return table == null ? null : table.ExtendedProperties[DataTableTypeNameKey] as string;
        }
    }
}