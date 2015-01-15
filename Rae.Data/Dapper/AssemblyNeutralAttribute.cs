using System;

namespace Rae.Data.Dapper
{
    [AssemblyNeutral,
     AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = false,
         Inherited = false)]
    internal sealed class AssemblyNeutralAttribute : Attribute
    {
    }
}