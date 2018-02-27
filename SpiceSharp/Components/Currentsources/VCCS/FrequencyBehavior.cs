﻿using SpiceSharp.NewSparse;
using SpiceSharp.Simulations;
using SpiceSharp.Attributes;
using SpiceSharp.Behaviors;
using System;
using System.Numerics;

namespace SpiceSharp.Components.VoltageControlledCurrentsourceBehaviors
{
    /// <summary>
    /// AC behavior for a <see cref="VoltageControlledCurrentSource"/>
    /// </summary>
    public class FrequencyBehavior : Behaviors.FrequencyBehavior, IConnectedBehavior
    {
        /// <summary>
        /// Necessary behaviors
        /// </summary>
        BaseParameters bp;
        
        /// <summary>
        /// Nodes
        /// </summary>
        int posNode, negNode, contPosourceNode, contNegateNode;
        protected MatrixElement<Complex> PosControlPosPtr { get; private set; }
        protected MatrixElement<Complex> PosControlNegPtr { get; private set; }
        protected MatrixElement<Complex> NegControlPosPtr { get; private set; }
        protected MatrixElement<Complex> NegControlNegPtr { get; private set; }

        /// <summary>
        /// Device methods and properties
        /// </summary>
        [PropertyName("v"), PropertyInfo("Complex voltage")]
        public Complex GetVoltage(ComplexState state)
        {
			if (state == null)
				throw new ArgumentNullException(nameof(state));

            return state.Solution[posNode] - state.Solution[negNode];
        }
        [PropertyName("c"), PropertyName("i"), PropertyInfo("Complex current")]
        public Complex GetCurrent(ComplexState state)
        {
			if (state == null)
				throw new ArgumentNullException(nameof(state));

            return (state.Solution[contPosourceNode] - state.Solution[contNegateNode]) * bp.Coefficient.Value;
        }
        [PropertyName("p"), PropertyInfo("Power")]
        public Complex GetPower(ComplexState state)
        {
			if (state == null)
				throw new ArgumentNullException(nameof(state));

            Complex v = state.Solution[posNode] - state.Solution[negNode];
            Complex i = (state.Solution[contPosourceNode] - state.Solution[contNegateNode]) * bp.Coefficient.Value;
            return -v * Complex.Conjugate(i);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        public FrequencyBehavior(Identifier name) : base(name) { }

        /// <summary>
        /// Setup the behavior
        /// </summary>
        /// <param name="provider">Data provider</param>
        public override void Setup(SetupDataProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            // Get parameters
            bp = provider.GetParameterSet<BaseParameters>("entity");
        }

        /// <summary>
        /// Connect
        /// </summary>
        /// <param name="pins">Pins</param>
        public void Connect(params int[] pins)
        {
            if (pins == null)
                throw new ArgumentNullException(nameof(pins));
            if (pins.Length != 4)
                throw new Diagnostics.CircuitException("Pin count mismatch: 4 pins expected, {0} given".FormatString(pins.Length));
            posNode = pins[0];
            negNode = pins[1];
            contPosourceNode = pins[2];
            contNegateNode = pins[3];
        }

        /// <summary>
        /// Gets matrix pointers
        /// </summary>
        /// <param name="solver">Solver</param>
        public override void GetEquationPointers(Solver<Complex> solver)
        {
            if (solver == null)
                throw new ArgumentNullException(nameof(solver));

            // Get matrix pointers
            PosControlPosPtr = solver.GetMatrixElement(posNode, contPosourceNode);
            PosControlNegPtr = solver.GetMatrixElement(posNode, contNegateNode);
            NegControlPosPtr = solver.GetMatrixElement(negNode, contPosourceNode);
            NegControlNegPtr = solver.GetMatrixElement(negNode, contNegateNode);
        }
        
        /// <summary>
        /// Unsetup the behavior
        /// </summary>
        public override void Unsetup()
        {
            // Remove references
            PosControlPosPtr = null;
            PosControlNegPtr = null;
            NegControlPosPtr = null;
            NegControlNegPtr = null;
        }

        /// <summary>
        /// Execute behavior for AC analysis
        /// </summary>
        /// <param name="simulation">Frequency-based simulation</param>
        public override void Load(FrequencySimulation simulation)
        {
			if (simulation == null)
				throw new ArgumentNullException(nameof(simulation));

            // Load Y-matrix
            PosControlPosPtr.Value += bp.Coefficient.Value;
            PosControlNegPtr.Value -= bp.Coefficient.Value;
            NegControlPosPtr.Value -= bp.Coefficient.Value;
            NegControlNegPtr.Value += bp.Coefficient.Value;
        }
    }
}
