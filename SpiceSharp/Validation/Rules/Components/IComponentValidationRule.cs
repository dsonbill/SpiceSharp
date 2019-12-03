﻿using SpiceSharp.Components;

namespace SpiceSharp.Validation
{
    /// <summary>
    /// An <see cref="IRule"/> that will check components.
    /// </summary>
    /// <seealso cref="IRule" />
    public interface IComponentValidationRule : IRule
    {
        /// <summary>
        /// Checks the specified component against the rule.
        /// </summary>
        /// <param name="component">The component.</param>
        void Check(IComponent component);
    }
}
