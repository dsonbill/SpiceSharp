﻿using System;

namespace SpiceSharp.Diagnostics
{
    /// <summary>
    /// Exception for a bad parameter.
    /// </summary>
    [Serializable]
    public class BadParameterException : CircuitException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="param"></param>
        public BadParameterException(string param)
            : base("Invalid parameter value for '{0}'".FormatString(param))
        {
        }
    }
}
