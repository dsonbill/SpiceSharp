﻿using System.Collections.Generic;

namespace SpiceSharp.Entities
{
    /// <summary>
    /// Template for a collection of <see cref="Entity" />.
    /// </summary>
    /// <seealso cref="ICloneable" />
    /// <seealso cref="IEnumerable{T}" />
    /// <seealso cref="ICollection{T}" />
    public interface IEntityCollection : IEnumerable<IEntity>, ICollection<IEntity>, ICloneable
    {
        /// <summary>
        /// Gets the <see cref="Entity"/> with the specified name.
        /// </summary>
        /// <value>
        /// The <see cref="Entity"/>.
        /// </value>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        IEntity this[string name] { get; }

        /// <summary>
        /// Gets the comparer used to compare <see cref="Entity"/> identifiers.
        /// </summary>
        /// <value>
        /// The comparer.
        /// </value>
        IEqualityComparer<string> Comparer { get; }

        /// <summary>
        /// Adds the specified entities to the collection.
        /// </summary>
        /// <param name="entities">The entities.</param>
        void Add(params IEntity[] entities);

        /// <summary>
        /// Removes the <see cref="Entity"/> with specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        bool Remove(string name);

        /// <summary>
        /// Determines whether this instance contains an <see cref="Entity"/> with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///   <c>true</c> if the collection contains the entity; otherwise, <c>false</c>.
        /// </returns>
        bool Contains(string name);

        /// <summary>
        /// Tries to find an <see cref="Entity"/> in the collection.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>
        /// <c>True</c> if the entity is found; otherwise <c>false</c>.
        /// </returns>
        bool TryGetEntity(string name, out IEntity entity);

        /// <summary>
        /// Gets all entities that are of a specified type.
        /// </summary>
        /// <typeparam name="E">The type of entity.</typeparam>
        /// <returns>The entities.</returns>
        IEnumerable<E> ByType<E>() where E : IEntity;
    }
}
