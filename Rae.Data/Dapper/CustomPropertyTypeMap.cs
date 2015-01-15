using System;
using System.Reflection;

namespace Rae.Data.Dapper
{
    /// <summary>
    ///     Implements custom property mapping by user provided criteria (usually presence of some custom attribute with column
    ///     to member mapping)
    /// </summary>
    internal sealed class CustomPropertyTypeMap : SqlMapper.ITypeMap
    {
        private readonly Func<Type, string, PropertyInfo> _propertySelector;
        private readonly Type _type;

        /// <summary>
        ///     Creates custom property mapping
        /// </summary>
        /// <param name="type">Target entity type</param>
        /// <param name="propertySelector">Property selector based on target type and DataReader column name</param>
        public CustomPropertyTypeMap(Type type, Func<Type, string, PropertyInfo> propertySelector)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");

            _type = type;
            _propertySelector = propertySelector;
        }

        /// <summary>
        ///     Always returns default constructor
        /// </summary>
        /// <param name="names">DataReader column names</param>
        /// <param name="types">DataReader column types</param>
        /// <returns>Default constructor</returns>
        public ConstructorInfo FindConstructor(string[] names, Type[] types)
        {
            return _type.GetConstructor(new Type[0]);
        }

        /// <summary>
        ///     Not impelmeneted as far as default constructor used for all cases
        /// </summary>
        /// <param name="constructor"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Returns property based on selector strategy
        /// </summary>
        /// <param name="columnName">DataReader column name</param>
        /// <returns>Poperty member map</returns>
        public SqlMapper.IMemberMap GetMember(string columnName)
        {
            PropertyInfo prop = _propertySelector(_type, columnName);
            return prop != null ? new SimpleMemberMap(columnName, prop) : null;
        }
    }
}