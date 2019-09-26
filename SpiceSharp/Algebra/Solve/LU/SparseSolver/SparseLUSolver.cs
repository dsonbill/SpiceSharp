﻿using System;
using SpiceSharp.Algebra.Solve;

namespace SpiceSharp.Algebra
{
    /// <summary>
    /// A base class for sparse linear systems that can be solved using LU decomposition.
    /// Pivoting is controlled by the <see cref="Strategy"/> property. The implementation
    /// is optimized for sparse matrices through the <see cref="ISparseMatrix{T}"/> interface.
    /// </summary>
    /// <typeparam name="M">The matrix type.</typeparam>
    /// <typeparam name="V">The right-hand side vector type.</typeparam>
    /// <typeparam name="T">The base value type.</typeparam>
    public abstract partial class SparseLUSolver<M, V, T> : LinearSystem<M, V, T>, ISolver<T>
        where M : IPermutableMatrix<T>, ISparseMatrix<T>
        where V : IPermutableVector<T>, ISparseVector<T>
        where T : IFormattable, IEquatable<T>
    {
        /// <summary>
        /// Gets or sets the order of the system that needs to be solved.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        /// <remarks>
        /// This property can be used to limit the number of elimination steps.
        /// </remarks>
        public int Order
        {
            get
            {
                if (_order <= 0)
                    return Size + _order;
                return _order;
            }
            set => _order = value;
        }
        private int _order = 0;

        /// <summary>
        /// Occurs before the solver uses the decomposition to find the solution.
        /// </summary>
        public event EventHandler<SolveEventArgs<T>> BeforeSolve;

        /// <summary>
        /// Occurs after the solver used the decomposition to find a solution.
        /// </summary>
        public event EventHandler<SolveEventArgs<T>> AfterSolve;

        /// <summary>
        /// Occurs before the solver uses the transposed decomposition to find the solution.
        /// </summary>
        public event EventHandler<SolveEventArgs<T>> BeforeSolveTransposed;

        /// <summary>
        /// Occurs after the solver uses the transposed decomposition to find a solution.
        /// </summary>
        public event EventHandler<SolveEventArgs<T>> AfterSolveTransposed;

        /// <summary>
        /// Occurs before the solver is factored.
        /// </summary>
        public event EventHandler<EventArgs> BeforeFactor;

        /// <summary>
        /// Occurs after the solver has been factored.
        /// </summary>
        public event EventHandler<EventArgs> AfterFactor;

        /// <summary>
        /// Occurs before the solver is ordered and factored.
        /// </summary>
        public event EventHandler<EventArgs> BeforeOrderAndFactor;

        /// <summary>
        /// Occurs after the solver has been ordered and factored.
        /// </summary>
        public event EventHandler<EventArgs> AfterOrderAndFactor;

        /// <summary>
        /// Number of fill-ins in the matrix generated by the solver.
        /// </summary>
        /// <remarks>
        /// Fill-ins are elements that were auto-generated as a consequence
        /// of the solver trying to solve the matrix. To save memory, this
        /// number should remain small.
        /// </remarks>
        public int Fillins { get; private set; }

        /// <summary>
        /// Gets or sets a flag that reordering is required.
        /// </summary>
        /// <remarks>
        /// Can be used by the pivoting strategy to indicate that a reordering is required.
        /// </remarks>
        public bool NeedsReordering { get; set; }

        /// <summary>
        /// Gets whether or not the solver is factored.
        /// </summary>
        public bool IsFactored { get; protected set; }

        /// <summary>
        /// Gets the pivoting strategy being used.
        /// </summary>
        public SparsePivotStrategy<T> Strategy { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseLUSolver{M, V, T}"/> class.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="vector">The vector.</param>
        /// <param name="strategy">The pivoting strategy that needs to be used.</param>
        protected SparseLUSolver(M matrix, V vector, SparsePivotStrategy<T> strategy)
            : base(matrix, vector)
        {
            NeedsReordering = true;
            Strategy = strategy.ThrowIfNull(nameof(strategy));
        }

        /// <summary>
        /// Preconditions the specified method.
        /// </summary>
        /// <param name="method">The method.</param>
        public virtual void Precondition(PreconditionMethod<T> method)
        {
            bool _isFirstSwap = true;
            void OnMatrixRowsSwapped(object sender, PermutationEventArgs args)
            {
                // Reflect the swapped vector elements in the row translation
                if (_isFirstSwap)
                {
                    _isFirstSwap = false;
                    Row.Swap(args.Index1, args.Index2);
                    Vector.SwapElements(args.Index1, args.Index2);
                    _isFirstSwap = true;
                }
            }
            void OnMatrixColumnsSwapped(object sender, PermutationEventArgs args)
            {
                // Reflect the swapped matrix column in the column translation
                Column.Swap(args.Index1, args.Index2);
            }
            void OnVectorElementsSwapped(object sender, PermutationEventArgs args)
            {
                // Reflect the swapped vector elements in the row translation
                if (_isFirstSwap)
                {
                    _isFirstSwap = false;
                    Row.Swap(args.Index1, args.Index2);
                    Matrix.SwapRows(args.Index1, args.Index2);
                    _isFirstSwap = true;
                }
            }

            Matrix.RowsSwapped += OnMatrixRowsSwapped;
            Matrix.ColumnsSwapped += OnMatrixColumnsSwapped;
            Vector.ElementsSwapped += OnVectorElementsSwapped;
            method(Matrix, Vector);
            Matrix.RowsSwapped -= OnMatrixRowsSwapped;
            Matrix.ColumnsSwapped -= OnMatrixColumnsSwapped;
            Vector.ElementsSwapped -= OnVectorElementsSwapped;
        }

        /// <summary>
        /// Solves the equations using the Y-matrix and Rhs-vector.
        /// </summary>
        /// <param name="solution">The solution.</param>
        public abstract void Solve(IVector<T> solution);

        /// <summary>
        /// Solves the equations using the transposed Y-matrix.
        /// </summary>
        /// <param name="solution">The solution.</param>
        public abstract void SolveTransposed(IVector<T> solution);

        /// <summary>
        /// Factor the Y-matrix and Rhs-vector.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the factoring was successful; otherwise <c>false</c>.
        /// </returns>
        public bool Factor()
        {
            OnBeforeFactor();

            for (var step = 1; step <= Order; step++)
            {
                if (!Elimination(Matrix.FindDiagonalElement(step)))
                {
                    IsFactored = false;
                    OnAfterFactor();
                    return false;
                }
            }
            IsFactored = true;
            OnAfterFactor();
            return true;
        }

        /// <summary>
        /// Order and factor the Y-matrix and Rhs-vector.
        /// </summary>
        public void OrderAndFactor()
        {
            OnBeforeOrderAndFactor();

            int step = 1;
            if (!NeedsReordering)
            {
                // Matrix has been factored before, and reordering is not required
                for (step = 1; step <= Order; step++)
                {
                    var pivot = Matrix.FindDiagonalElement(step);
                    if (Strategy.IsValidPivot(pivot))
                    {
                        if (!Elimination(pivot))
                        {
                            IsFactored = false;
                            throw new AlgebraException("Elimination failed on accepted pivot");
                        }
                        else
                        {
                            NeedsReordering = true;
                            break;
                        }
                    }
                }

                if (!NeedsReordering)
                {
                    IsFactored = true;
                    OnAfterOrderAndFactor();
                    return;
                }
            }

            // Setup the pivot strategy
            Strategy.Setup(Matrix, Vector, step);

            for (; step <= Order; step++)
            {
                var pivot = Strategy.FindPivot(Matrix, step);
                if (pivot == null)
                    throw new SingularException(step);
                MovePivot(pivot, step);
                if (!Elimination(pivot))
                {
                    IsFactored = false;
                    throw new SingularException(step);
                }
            }

            IsFactored = true;
            NeedsReordering = false;
            OnAfterOrderAndFactor();
        }

        /// <summary>
        /// Eliminate the matrix right and below the pivot.
        /// </summary>
        /// <param name="pivot">The pivot element.</param>
        /// <returns>
        /// <c>true</c> if the elimination was successful; otherwise <c>false</c>.
        /// </returns>
        protected abstract bool Elimination(ISparseMatrixElement<T> pivot);

        /// <summary>
        /// Finds the diagonal element at the specified row/column.
        /// </summary>
        /// <param name="index">The row/column index.</param>
        /// <returns>
        /// The matrix element.
        /// </returns>
        public override IMatrixElement<T> FindDiagonalElement(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index > Size)
                return null;
            int row = Row[index];
            int column = Column[index];
            return Matrix.FindMatrixElement(row, column);
        }

        /// <summary>
        /// Gets a pointer to the matrix element at the specified row and column. A
        /// non-zero element is always guaranteed with this method. The matrix is expanded
        /// if necessary.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        /// <returns>
        /// The matrix element.
        /// </returns>
        public override IMatrixElement<T> GetMatrixElement(int row, int column)
        {
            row = Row[row];
            column = Column[column];
            return Matrix.GetMatrixElement(row, column);
        }

        /// <summary>
        /// Finds a pointer to the matrix element at the specified row and column.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        /// <returns>
        /// The matrix element; otherwise <c>null</c>.
        /// </returns>
        public override IMatrixElement<T> FindMatrixElement(int row, int column)
        {
            if (row < 0)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (column < 0)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (row > Size || column > Size)
                return null;
            row = Row[row];
            column = Column[column];
            return Matrix.FindMatrixElement(row, column);
        }

        /// <summary>
        /// Gets a vector element at the specified index. A non-zero element is
        /// always guaranteed with this method. The vector is expanded if
        /// necessary.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The vector element.
        /// </returns>
        public override IVectorElement<T> GetVectorElement(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            index = Row[index];
            return Vector.GetVectorElement(index);
        }

        /// <summary>
        /// Finds a vector element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The vector element; otherwise <c>null</c>.
        /// </returns>
        public override IVectorElement<T> FindVectorElement(int index)
        {
            index = Row[index];
            return Vector.FindVectorElement(index);
        }

        /// <summary>
        /// Move a chosen pivot to the diagonal.
        /// </summary>
        /// <param name="pivot">The pivot element.</param>
        /// <param name="step">The current step of factoring.</param>
        protected void MovePivot(ISparseMatrixElement<T> pivot, int step)
        {
            pivot.ThrowIfNull(nameof(pivot));
            Strategy.MovePivot(Matrix, Vector, pivot, step);

            // Move the pivot in the matrix
            SwapRows(pivot.Row, step);
            SwapColumns(pivot.Column, step);

            // Update the pivoting strategy
            Strategy.Update(Matrix, pivot, step);
        }

        /// <summary>
        /// Create a fill-in element.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        /// <returns>The created element.</returns>
        protected virtual ISparseMatrixElement<T> CreateFillin(int row, int column)
        {
            var result = (ISparseMatrixElement<T>)Matrix.GetMatrixElement(row, column);
            Strategy.CreateFillin(Matrix, result);
            Fillins++;
            return result;
        }

        /// <summary>
        /// Resets all elements in the matrix.
        /// </summary>
        public override void ResetMatrix()
        {
            base.ResetMatrix();
            IsFactored = false;
        }

        /// <summary>
        /// Should be called before solving the decomposition.
        /// </summary>
        /// <param name="args">The <see cref="SolveEventArgs{T}"/> instance containing the event data.</param>
        protected void OnBeforeSolve(SolveEventArgs<T> args) => BeforeSolve?.Invoke(this, args);

        /// <summary>
        /// Should be called after solving the decomposition.
        /// </summary>
        /// <param name="args">The <see cref="SolveEventArgs{T}"/> instance containing the event data.</param>
        protected void OnAfterSolve(SolveEventArgs<T> args) => AfterSolve?.Invoke(this, args);

        /// <summary>
        /// Should be called before solving the transposed decomposition.
        /// </summary>
        /// <param name="args">The <see cref="SolveEventArgs{T}"/> instance containing the event data.</param>
        protected void OnBeforeSolveTransposed(SolveEventArgs<T> args) => BeforeSolveTransposed?.Invoke(this, args);

        /// <summary>
        /// Should be called after solving the transposed decomposition.
        /// </summary>
        /// <param name="args">The <see cref="SolveEventArgs{T}"/> instance containing the event data.</param>
        protected void OnAfterSolveTransposed(SolveEventArgs<T> args) => AfterSolveTransposed?.Invoke(this, args);

        /// <summary>
        /// Should be called before factoring.
        /// </summary>
        protected void OnBeforeFactor() => BeforeFactor?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Should be called after factoring.
        /// </summary>
        protected void OnAfterFactor() => AfterFactor?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Should be called before ordering and factoring.
        /// </summary>
        protected void OnBeforeOrderAndFactor() => BeforeOrderAndFactor?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Should be called after ordering and factoring.
        /// </summary>
        protected void OnAfterOrderAndFactor() => AfterOrderAndFactor?.Invoke(this, EventArgs.Empty);
    }
}
