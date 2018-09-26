﻿using System;
using SpiceSharp.Algebra;
using SpiceSharp.Simulations;

namespace SpiceSharp.IntegrationMethods
{
    /// <summary>
    /// Gear integration method
    /// </summary>
    public partial class Gear : SpiceIntegrationMethod
    {
        /// <summary>
        /// Integration coefficients
        /// </summary>
        protected double[] Coefficients { get; } = new double[7];

        /// <summary>
        /// Prediction coefficients
        /// </summary>
        protected double[] PredictionCoefficients { get; } = new double[7];

        /// <summary>
        /// Matrix used to solve coefficients
        /// </summary>
        protected DenseMatrix<double> Matrix { get; } = new DenseMatrix<double>(8);

        /// <summary>
        /// Constructor
        /// </summary>
        public Gear()
            : base(6)
        {
        }

        /// <summary>
        /// Initialize the Gear integration method
        /// </summary>
        /// <param name="simulation">The simulation</param>
        public override void Initialize(TimeSimulation simulation)
        {
            base.Initialize(simulation);

            // Reset all coefficients
            for (var i = 0; i < Coefficients.Length; i++)
            {
                Coefficients[i] = 0.0;
                PredictionCoefficients[i] = 0.0;
            }

            // Clear the matrix
            for (var i = 0; i < 8; i++)
                for (var j = 0; j < 8; j++)
                    Matrix[i, j] = 0.0;
        }

        /// <summary>
        /// Unsetup the integration method
        /// </summary>
        public override void Unsetup()
        {
            base.Unsetup();

            // Reset coefficients
            for (var i = 0; i < Coefficients.Length; i++)
            {
                Coefficients[i] = 0.0;
                PredictionCoefficients[i] = 0.0;
            }
        }

        /// <summary>
        /// Predict a new solution based on the previous ones
        /// </summary>
        /// <param name="simulation">Time-based simulation</param>
        protected override void Predict(TimeSimulation simulation)
        {
            if (simulation == null)
                throw new ArgumentNullException(nameof(simulation));

            // Use the previous solutions to predict a new one
            for (var i = 0; i <= Prediction.Length; i++)
            {
                Prediction[i] = 0.0;
                for (var k = 0; k <= Order; k++)
                {
                    Prediction[i] += PredictionCoefficients[k] * IntegrationStates[k + 1].Solution[i];
                }
            }
        }

        /// <summary>
        /// Truncate the timestep using nodes
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">Arguments</param>
        protected override void TruncateNodes(object sender, TruncateEvaluateEventArgs args)
        {
            // Get the state
            var state = args.Simulation.RealState;
            var timetmp = double.PositiveInfinity;
            var nodes = args.Simulation.Nodes;

            var delsum = 0.0;
            for (var i = 0; i <= Order; i++)
                delsum += IntegrationStates[i].Delta;

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var index = node.Index;
                var tol = Math.Max(Math.Abs(state.Solution[index]), Math.Abs(Prediction[index])) * RelTol + AbsTol;
                var diff = state.Solution[index] - Prediction[i];

                if (!diff.Equals(0.0))
                {
                    var tmp = tol * TrTol * delsum / (diff * IntegrationStates[0].Delta);
                    tmp = Math.Abs(tmp);
                    switch (Order)
                    {
                        case 0: break;
                        case 1:
                            tmp = Math.Sqrt(tmp);
                            break;
                        default:
                            tmp = Math.Exp(Math.Log(tmp) / (Order + 1));
                            break;
                    }

                    tmp *= IntegrationStates[0].Delta;
                    timetmp = Math.Min(timetmp, tmp);
                }

                args.Delta = timetmp;
            }
        }

        /// <summary>
        /// Compute the coefficients for Trapezoidal integration
        /// </summary>
        protected override void ComputeCoefficients()
        {
            var delta = IntegrationStates[0].Delta;
            for (var i = 0; i < Coefficients.Length; i++)
                Coefficients[i] = 0.0;
            Coefficients[1] = -1.0 / delta;

            // First, set up the matrix
            double arg = 0, arg1;
            for (var i = 0; i <= Order; i++)
                Matrix[0, i] = 1.0;
            for (var i = 1; i <= Order; i++)
                Matrix[i, 0] = 0.0;

            for (var i = 1; i <= Order; i++)
            {
                arg += IntegrationStates[i - 1].Delta;
                arg1 = 1.0;
                for (var j = 1; j <= Order; j++)
                {
                    arg1 *= arg / delta;
                    Matrix[j, i] = arg1;
                }
            }

            // LU decompose
            // The first column is already decomposed!
            for (var i = 1; i <= Order; i++)
            {
                for (var j = i + 1; j <= Order; j++)
                {
                    Matrix[j, i] /= Matrix[i, i];
                    for (var k = i + 1; k <= Order; k++)
                        Matrix[j, k] -= Matrix[j, i] * Matrix[i, k];
                }
            }

            // Forward substitution
            for (var i = 1; i <= Order; i++)
            {
                for (var j = i + 1; j <= Order; j++)
                    Coefficients[j] = Coefficients[j] - Matrix[j, i] * Coefficients[i];
            }

            // Backward substitution
            Coefficients[Order] /= Matrix[Order, Order];
            for (var i = Order - 1; i >= 0; i--)
            {
                for (var j = i + 1; j <= Order; j++)
                    Coefficients[i] = Coefficients[i] - Matrix[i, j] * Coefficients[j];
                Coefficients[i] /= Matrix[i, i];
            }

            // Predictor calculations
            for (var i = 1; i < PredictionCoefficients.Length; i++)
                PredictionCoefficients[i] = 0.0;
            PredictionCoefficients[0] = 1.0;
            for (var i = 0; i <= Order; i++)
                Matrix[0, i] = 1.0;
            arg = 0.0;
            for (var i = 0; i <= Order; i++)
            {
                arg += IntegrationStates[i].Delta;
                arg1 = 1.0;
                for (var j = 1; j <= Order; j++)
                {
                    arg1 *= arg / delta;
                    Matrix[j, i] = arg1;
                }
            }

            // LU decomposition
            for (var i = 0; i <= Order; i++)
            {
                for (var j = i + 1; j <= Order; j++)
                {
                    Matrix[j, i] /= Matrix[i, i];
                    for (var k = i + 1; k <= Order; k++)
                        Matrix[j, k] -= Matrix[j, i] * Matrix[i, k];
                }
            }

            // Forward substitution
            for (var i = 0; i <= Order; i++)
            {
                for (var j = i + 1; j <= Order; j++)
                    PredictionCoefficients[j] -= Matrix[j, i] * PredictionCoefficients[i];
            }

            // Backward substitution
            PredictionCoefficients[Order] /= Matrix[Order, Order];
            for (var i = Order - 1; i >= 0; i--)
            {
                for (var j = i + 1; j <= Order; j++)
                    PredictionCoefficients[i] -= Matrix[i, j] * PredictionCoefficients[j];
                PredictionCoefficients[i] /= Matrix[i, i];
            }

            // Store the derivative w.r.t. the current timestep
            Slope = Coefficients[0];
        }

        /// <summary>
        /// Produce a derivative for the Gear integration method
        /// </summary>
        /// <returns></returns>
        protected override StateDerivative ProduceDerivative() => new GearStateDerivative(this);
    }
}