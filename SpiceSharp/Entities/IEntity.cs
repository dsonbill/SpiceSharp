﻿using SpiceSharp.Simulations;

namespace SpiceSharp.Entities
{
    /// <summary>
    /// Interface describing an entity that can provide behaviors to a <see cref="ISimulation"/>.
    /// </summary>
    public interface IEntity : ICloneable, IParameterized<IEntity>
    {
        /// <summary>
        /// Gets the name of the entity.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Creates the behaviors for the specified simulation and registers them with the simulation.
        /// </summary>
        /// <param name="simulation">The simulation.</param>
        /// <param name="entities">The collection of entities that is being simulated.</param>
        /// <returns>A dictionary of behaviors that can be used by the simulation.</returns>
        void CreateBehaviors(ISimulation simulation, IEntityCollection entities);
    }
}
