using System;

namespace Rae.Data.Dapper
{
    /// <summary>
    ///     Additional state flags that control command behaviour
    /// </summary>
    [Flags]
    public enum CommandFlags
    {
        /// <summary>
        ///     No additonal flags
        /// </summary>
        None = 0,

        /// <summary>
        ///     Should data be buffered before returning?
        /// </summary>
        Buffered = 1,

        /// <summary>
        ///     Can async queries be pipelined?
        /// </summary>
        Pipelined = 2,

        /// <summary>
        ///     Should the plan cache be bypassed?
        /// </summary>
        NoCache = 4
    }
}