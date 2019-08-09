using System;
using SpiceSharp.Algebra;
using SpiceSharp.Attributes;
using SpiceSharp.Behaviors;
using SpiceSharp.Simulations;

namespace SpiceSharp.Components.ResistorBehaviors
{
    /// <summary>
    /// General behavior for <see cref="Resistor"/>
    /// </summary>
    public class BiasingBehavior : TemperatureBehavior, IBiasingBehavior
    {
        /// <summary>
        /// Gets the voltage across the resistor.
        /// </summary>
        [ParameterName("v"), ParameterInfo("Voltage")]
        public double GetVoltage()
        {
            // There is no argument necessary anymore. The real-state can also be cached
            if (_state == null)
                throw new CircuitException("Behavior '{0}' is not bound to a simulation".FormatString(Name));
            return _state.Solution[PosNode] - _state.Solution[NegNode];
        }

        /// <summary>
        /// Gets the current through the resistor.
        /// </summary>
        [ParameterName("i"), ParameterInfo("Current")]
        public double GetCurrent()
        {
            if (_state == null)
                throw new CircuitException("Behavior '{0}' is not bound to a simulation".FormatString(Name));
            return (_state.Solution[PosNode] - _state.Solution[NegNode]) * Conductance;
        }

        /// <summary>
        /// Gets the power dissipated by the resistor.
        /// </summary>
        [ParameterName("p"), ParameterInfo("Power")]
        public double GetPower()
        {
            if (_state == null)
                throw new CircuitException("Behavior '{0}' is not bound to a simulation".FormatString(Name));
            var v = _state.Solution[PosNode] - _state.Solution[NegNode];
            return v * v * Conductance;
        }

        /// <summary>
        /// Gets the positive node.
        /// </summary>
        protected int PosNode { get; private set; }

        /// <summary>
        /// Gets the negative node.
        /// </summary>
        protected int NegNode { get; private set; }

        /// <summary>
        /// Gets the (positive, positive) element.
        /// </summary>
        protected MatrixElement<double> PosPosPtr { get; private set; }

        /// <summary>
        /// Gets the (negative, negative) element.
        /// </summary>
        protected MatrixElement<double> NegNegPtr { get; private set; }

        /// <summary>
        /// Gets the (positive, negative) element.
        /// </summary>
        protected MatrixElement<double> PosNegPtr { get; private set; }

        /// <summary>
        /// Gets the (negative, positive) element.
        /// </summary>
        protected MatrixElement<double> NegPosPtr { get; private set; }

        /// <summary>
        /// The simulation state that the behavior is bound to
        /// </summary>
        private BaseSimulationState _state;

        /// <summary>
        /// Creates a new instance of the <see cref="BiasingBehavior"/> class.
        /// </summary>
        /// <param name="name">Name</param>
        public BiasingBehavior(string name) : base(name)
        {
        }

        /// <summary>
        /// Setup the behavior.
        /// </summary>
        /// <param name="simulation">The simulation.</param>
        /// <param name="provider">The setup data provider.</param>
        public override void Setup(Simulation simulation, SetupDataProvider provider)
        {
            base.Setup(simulation, provider);
            _state = ((BaseSimulation)Simulation).RealState.ThrowIfNull("State");

            // Connections
            var p = (ComponentDataProvider)provider;
            PosNode = p.Pins[0];
            NegNode = p.Pins[1];

            var solver = _state.Solver;
            PosPosPtr = solver.GetMatrixElement(PosNode, PosNode);
            NegNegPtr = solver.GetMatrixElement(NegNode, NegNode);
            PosNegPtr = solver.GetMatrixElement(PosNode, NegNode);
            NegPosPtr = solver.GetMatrixElement(NegNode, PosNode);
        }

        /// <summary>
        /// Gets matrix pointers
        /// </summary>
        /// <param name="variables">Nodes</param>
        /// <param name="solver">Solver</param>
        public void GetEquationPointers(VariableSet variables, Solver<double> solver)
        {
        }

        /// <summary>
        /// Unsetup the behavior.
        /// </summary>
        /// <param name="simulation"></param>
        public override void Unsetup(Simulation simulation)
        {
            PosPosPtr = null;
            NegNegPtr = null;
            PosNegPtr = null;
            NegPosPtr = null;
            _state = null;
            base.Unsetup(simulation);
        }

        /// <summary>
        /// Execute behavior
        /// </summary>
        /// <param name="simulation">Base simulation</param>
        public void Load(BaseSimulation simulation)
        {
            // TODO: There's already no reference to the simulation in here, because
            // it only needs the matrix elements.
            var conductance = Conductance;
            PosPosPtr.Value += conductance;
            NegNegPtr.Value += conductance;
            PosNegPtr.Value -= conductance;
            NegPosPtr.Value -= conductance;
        }

        /// <summary>
        /// Tests convergence at the device-level.
        /// </summary>
        /// <param name="simulation">The base simulation.</param>
        /// <returns>
        /// <c>true</c> if the device determines the solution converges; otherwise, <c>false</c>.
        /// </returns>
        public bool IsConvergent(BaseSimulation simulation) => true;
    }
}
