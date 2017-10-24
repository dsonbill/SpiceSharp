﻿using System.Numerics;
using SpiceSharp.Behaviors;

namespace SpiceSharp.Components.ComponentBehaviors
{
    /// <summary>
    /// AC behaviour for <see cref="Inductor"/>
    /// </summary>
    public class InductorAcBehavior : CircuitObjectBehaviorAcLoad
    {
        /// <summary>
        /// Execute behaviour
        /// </summary>
        /// <param name="ckt">Circuit</param>
        public override void Execute(Circuit ckt)
        {
            var ind = ComponentTyped<Inductor>();
            var cstate = ckt.State.Complex;
            Complex val = cstate.Laplace * ind.INDinduct.Value;

            cstate.Matrix[ind.INDposNode, ind.INDbrEq] += 1.0;
            cstate.Matrix[ind.INDnegNode, ind.INDbrEq] -= 1.0;
            cstate.Matrix[ind.INDbrEq, ind.INDnegNode] -= 1.0;
            cstate.Matrix[ind.INDbrEq, ind.INDposNode] += 1.0;
            cstate.Matrix[ind.INDbrEq, ind.INDbrEq] -= val;
        }
    }
}
