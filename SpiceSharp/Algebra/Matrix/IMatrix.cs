﻿using System;

namespace SpiceSharp.Algebra
{
    /// <summary>
    /// Describes a sparse matrix.
    /// </summary>
    /// <typeparam name="T">The base type.</typeparam>
    public interface IMatrix<T> where T : IFormattable
    {
        /// <summary>
        /// Gets the size of the matrix.
        /// </summary>
        /// <value>
        /// The matrix size.
        /// </value>
        int Size { get; }

        /// <summary>
        /// Gets or sets the value at the specified row and column.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        /// <returns>The value.</returns>
        T this[int row, int column] { get; set; }

        /// <summary>
        /// Resets all elements in the matrix.
        /// </summary>
        void ResetMatrix();
    }
}