﻿using SpiceSharp.Behaviors;
using SpiceSharp.Components.Distributed;
using SpiceSharp.Algebra;

namespace SpiceSharp.Components.DelayBehaviors
{
    /// <summary>
    /// Time behavior for a <see cref="VoltageDelay"/>.
    /// </summary>
    public class TimeBehavior : BiasingBehavior, ITimeBehavior
    {
        /// <summary>
        /// Gets the vector elements.
        /// </summary>
        /// <value>
        /// The vector elements.
        /// </value>
        protected ElementSet<double> TransientElements { get; private set; }

        /// <summary>
        /// Gets the delayed signal.
        /// </summary>
        public DelayedSignal Signal { get; private set; }

        private int _contPosNode, _contNegNode, _branchEq;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeBehavior"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="context">The context.</param>
        public TimeBehavior(string name, ComponentBindingContext context)
            : base(name, context)
        {
            _contPosNode = BiasingState.Map[context.Nodes[2]];
            _contNegNode = BiasingState.Map[context.Nodes[3]];
            _branchEq = BiasingState.Map[Branch];
            TransientElements = new ElementSet<double>(BiasingState.Solver, null, new[] { _branchEq });
            Signal = new DelayedSignal(1, BaseParameters.Delay);
        }

        /// <summary>
        /// Calculates the state values from the current DC solution.
        /// </summary>
        void ITimeBehavior.InitializeStates()
        {
            var sol = BiasingState.Solution;
            var input = sol[_contPosNode] - sol[_contNegNode];
            Signal.SetProbedValues(input);
        }

        /// <summary>
        /// Loads the Y-matrix and Rhs-vector.
        /// </summary>
        void IBiasingBehavior.Load()
        {
            var sol = BiasingState.Solution;
            var input = sol[_contPosNode] - sol[_contNegNode];
            Signal.SetProbedValues(input);

            if (BiasingState.UseDc)
                Elements.Add(1, -1, 1, -1, -1, 1);
            else
            {
                Elements.Add(1, -1, 1, -1);
                TransientElements.Add(Signal.Values[0]);
            }
        }
    }
}