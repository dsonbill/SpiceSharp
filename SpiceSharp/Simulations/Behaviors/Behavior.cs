using SpiceSharp.Simulations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace SpiceSharp.Behaviors
{
    /// <summary>
    /// Template for a behavior.
    /// </summary>
    public abstract class Behavior : IBehavior
    {
        /// <summary>
        /// Gets the identifier of the behavior.
        /// </summary>
        /// <remarks>
        /// This should be the same identifier as the entity that created it.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        /// Gets the <see cref="Simulation"/> that this behavior is bound to.
        /// </summary>
        protected Simulation Simulation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Behavior"/> class.
        /// </summary>
        /// <param name="name">The identifier of the behavior.</param>
        /// <remarks>
        /// The identifier of the behavior should be the same as that of the entity creating it.
        /// </remarks>
        protected Behavior(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Setup the behavior.
        /// </summary>
        /// <param name="simulation">The simulation.</param>
        /// <param name="provider">The data provider.</param>
        public virtual void Setup(Simulation simulation, SetupDataProvider provider)
        {
            // TODO: We will call this method "Bind()" in the future.
            // The behavior, when bound to a simulation, is free to extract data from the simulation.
            if (Simulation != null)
                throw new CircuitException("Behavior '{0}' is already bound to simulation '{1}'".FormatString(Name, Simulation.Name));
            Simulation = simulation.ThrowIfNull(nameof(simulation));
        }

        /// <summary>
        /// Destroy the behavior.
        /// </summary>
        /// <param name="simulation">The simulation.</param>
        public virtual void Unsetup(Simulation simulation)
        {
            // TODO: We will call this method "Unbind()" in the future.
            Simulation = null;
        }

        /// <summary>
        /// Create a getter for a behavior parameter (possibly requiring a simulation or simulation state).
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <param name="simulation">The simulation.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="comparer">The comparer used to compare property names.</param>
        /// <returns></returns>
        public Func<T> CreateGetter<T>(Simulation simulation, string name, IEqualityComparer<string> comparer = null)
        {
            // TODO: This whole part will not be necessary anymore, because the simulation isn't needed as an argument.

            // First find the method
            comparer = comparer ?? EqualityComparer<string>.Default;
            var method = Reflection.GetNamedMembers(this, name, comparer).FirstOrDefault(m => m is MethodInfo) as MethodInfo;
            if (method == null || method.ReturnType != typeof(T))
            {
                // Fall back to any member
                return ParameterHelper.CreateGetter<T>(this, name, comparer);
            }
            var parameters = method.GetParameters();

            // Method: TResult Method()
            if (parameters.Length == 0)
                return Reflection.CreateGetterForMethod<T>(this, method);

            // Methods with one parameter
            if (parameters.Length == 1)
            {
                // Method: <T> <Method>(<Simulation>)
                if (parameters[0].ParameterType.GetTypeInfo().IsAssignableFrom(simulation.GetType()))
                {
                    var simMethod = (Func<Simulation, T>)method.CreateDelegate(typeof(Func<Simulation, T>), this);
                    return () => simMethod(simulation);
                }

                // Method: TResult Method(State)
                // Works for any child class of SimulationState
                var paramType = parameters[0].ParameterType;
                if (paramType.GetTypeInfo().IsSubclassOf(typeof(SimulationState)))
                {
                    // Try to find a property of the same type using reflection
                    var stateMember = simulation.GetType().GetTypeInfo()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(property => property.PropertyType == paramType);
                    if (stateMember == null)
                        return null;

                    // Get this state
                    var state = (SimulationState)stateMember.GetValue(simulation);

                    // Create the expression
                    return () => (T)method.Invoke(this, new[] { state });
                }
            }

            return null;
        }
    }
}
