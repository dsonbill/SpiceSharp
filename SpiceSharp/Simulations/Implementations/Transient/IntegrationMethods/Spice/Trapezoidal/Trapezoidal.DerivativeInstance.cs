﻿using SpiceSharp.IntegrationMethods;
using System;

namespace SpiceSharp.Simulations.IntegrationMethods
{
    public partial class Trapezoidal
    {
        protected partial class Instance
        {
            /// <summary>
            /// Describes a state that can be derived.
            /// </summary>
            /// <seealso cref="IDerivative" />
            protected class DerivativeInstance : IDerivative, ITruncatable
            {
                private readonly int _index;
                private readonly Instance _method;
                private readonly IHistory<IntegrationState> _states;

                /// <summary>
                /// Gets the current derivative.
                /// </summary>
                /// <value>
                /// The derivative.
                /// </value>
                public double Derivative => _states.Value.State[_index + 1];

                /// <summary>
                /// Gets or sets the current value.
                /// </summary>
                /// <value>
                /// The current value.
                /// </value>
                public double Value
                {
                    get => _states.Value.State[_index];
                    set => _states.Value.State[_index] = value;
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="DerivativeInstance"/> class.
                /// </summary>
                /// <param name="method">The integration method.</param>
                /// <param name="index">The derivative value index.</param>
                public DerivativeInstance(Instance method, int index)
                {
                    _method = method.ThrowIfNull(nameof(method));
                    _states = _method.States;
                    _index = index;
                }

                /// <summary>
                /// Gets the jacobian value and Rhs-vector value.
                /// </summary>
                /// <param name="coefficient">The coefficient of the quantity that is derived.</param>
                /// <param name="currentValue">The current value of the derived state.</param>
                /// <returns>
                /// The information for filling in the Y-matrix and Rhs-vector.
                /// </returns>
                public JacobianInfo GetContributions(double coefficient, double currentValue)
                {
                    double g = _method.Slope * coefficient;
                    return new JacobianInfo(
                        g,
                        Derivative - g * currentValue);
                }

                /// <summary>
                /// Gets the Y-matrix value and Rhs-vector contributions for the derived quantity.
                /// The relationship is assumed to be linear.
                /// </summary>
                /// <param name="coefficient">The coefficient of the quantity that is derived.</param>
                /// <returns>
                /// The information for filling in the Y-matrix and Rhs-vector
                /// </returns>
                public JacobianInfo GetContributions(double coefficient)
                {
                    var h = _method.Slope;
                    var s = _states.Value.State;
                    double g = h * coefficient;
                    return new JacobianInfo(
                        g,
                        s[_index + 1] - h * s[_index]);
                }

                /// <summary>
                /// Gets a previous value of the state. An index of 0 indicates the current value.
                /// </summary>
                /// <param name="index">The number of points to go back in time.</param>
                /// <returns>
                /// The previous value.
                /// </returns>
                public double GetPreviousValue(int index)
                    => _states.GetPreviousValue(index).State[_index];

                /// <summary>
                /// Gets a previous derivative of the state. An index of 0 indicates the current value.
                /// </summary>
                /// <param name="index">The number of points to go back in time.</param>
                /// <returns>
                /// The previous derivative.
                /// </returns>
                public double GetPreviousDerivative(int index)
                    => _states.GetPreviousValue(index).State[_index + 1];

                /// <summary>
                /// Integrates the state (calculates the derivative).
                /// </summary>
                public void Integrate()
                {
                    var derivativeIndex = _index + 1;
                    var current = _states.Value.State;
                    var previous = _states.GetPreviousValue(1).State;
                    var ag = _method.Coefficients;

                    switch (_method.Order)
                    {
                        case 1:
                            current[derivativeIndex] = ag[0] * current[_index] + ag[1] * previous[_index];
                            break;

                        case 2:
                            current[derivativeIndex] = -previous[derivativeIndex] * ag[1] +
                                                       ag[0] * (current[_index] - previous[_index]);
                            break;
                    }
                }

                /// <summary>
                /// Truncates the current timestep.
                /// </summary>
                /// <returns>
                /// The maximum timestep allowed by this integration state.
                /// </returns>
                public double Truncate()
                {
                    var derivativeIndex = _index + 1;
                    var current = _states.Value.State;
                    var previous = _states.GetPreviousValue(1).State;

                    var diff = new double[_method.MaxOrder + 2];
                    var deltmp = new double[_states.Length];

                    // Calculate the tolerance
                    var volttol =
                        _method.AbsTol + _method.RelTol * Math.Max(Math.Abs(current[derivativeIndex]), Math.Abs(previous[derivativeIndex]));
                    var chargetol = Math.Max(Math.Abs(current[_index]), Math.Abs(previous[_index]));
                    chargetol = _method.RelTol * Math.Max(chargetol, _method.ChgTol) / _states.Value.Delta;
                    var tol = Math.Max(volttol, chargetol);

                    // Now compute divided differences
                    var j = 0;
                    foreach (var state in _states)
                    {
                        diff[j] = state.State[_index];
                        deltmp[j] = state.Delta;
                        j++;
                    }

                    j = _method.Order;
                    while (true)
                    {
                        for (var i = 0; i <= j; i++)
                            diff[i] = (diff[i] - diff[i + 1]) / deltmp[i];
                        if (--j < 0)
                            break;
                        for (var i = 0; i <= j; i++)
                            deltmp[i] = deltmp[i + 1] + _states.GetPreviousValue(i).Delta;
                    }

                    // Calculate the new timestep
                    double factor = double.NaN;
                    switch (_method.Order)
                    {
                        case 1:
                            factor = 0.5;
                            break;
                        case 2:
                            factor = 0.0833333333;
                            break;
                    }

                    var del = _method.TrTol * tol / Math.Max(_method.AbsTol, factor * Math.Abs(diff[0]));
                    if (_method.Order == 2)
                        del = Math.Sqrt(del);
                    return del;
                }
            }
        }
    }
}
