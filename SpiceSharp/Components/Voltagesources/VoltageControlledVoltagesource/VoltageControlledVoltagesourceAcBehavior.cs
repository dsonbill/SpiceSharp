﻿using SpiceSharp.Behaviors;
using SpiceSharp.Circuits;
using SpiceSharp.Sparse;

namespace SpiceSharp.Components.ComponentBehaviors
{
    /// <summary>
    /// AC behaviour for a <see cref="VoltageControlledVoltagesource"/>
    /// </summary>
    public class VoltageControlledVoltagesourceAcBehavior : CircuitObjectBehaviorAcLoad
    {
        /// <summary>
        /// Necessary behaviors
        /// </summary>
        private VoltageControlledVoltagesourceLoadBehavior load;

        /// <summary>
        /// Nodes
        /// </summary>
        protected int VCVSposNode { get; private set; }
        protected int VCVSnegNode { get; private set; }
        protected int VCVScontPosNode { get; private set; }
        protected int VCVScontNegNode { get; private set; }
        protected int VCVSbranch { get; private set; }

        /// <summary>
        /// Matrix elements
        /// </summary>
        protected MatrixElement VCVSposIbrptr { get; private set; }
        protected MatrixElement VCVSnegIbrptr { get; private set; }
        protected MatrixElement VCVSibrPosptr { get; private set; }
        protected MatrixElement VCVSibrNegptr { get; private set; }
        protected MatrixElement VCVSibrContPosptr { get; private set; }
        protected MatrixElement VCVSibrContNegptr { get; private set; }

        /// <summary>
        /// Setup behavior
        /// </summary>
        /// <param name="component">Component</param>
        /// <param name="ckt">Circuit</param>
        /// <returns></returns>
        public override bool Setup(CircuitObject component, Circuit ckt)
        {
            var vcvs = component as VoltageControlledVoltagesource;

            // Get behaviors
            load = GetBehavior<VoltageControlledVoltagesourceLoadBehavior>(component);

            // Get nodes
            VCVSposNode = vcvs.VCVSposNode;
            VCVSnegNode = vcvs.VCVSnegNode;
            VCVScontPosNode = vcvs.VCVScontPosNode;
            VCVScontNegNode = vcvs.VCVScontNegNode;
            VCVSbranch = load.VCVSbranch;

            // Get matrix elements
            var matrix = ckt.State.Matrix;
            VCVSposIbrptr = matrix.GetElement(VCVSposNode, VCVSbranch);
            VCVSnegIbrptr = matrix.GetElement(VCVSnegNode, VCVSbranch);
            VCVSibrPosptr = matrix.GetElement(VCVSbranch, VCVSposNode);
            VCVSibrNegptr = matrix.GetElement(VCVSbranch, VCVSnegNode);
            VCVSibrContPosptr = matrix.GetElement(VCVSbranch, VCVScontPosNode);
            VCVSibrContNegptr = matrix.GetElement(VCVSbranch, VCVScontNegNode);
            return true;
        }

        /// <summary>
        /// Unsetup
        /// </summary>
        public override void Unsetup()
        {
            // Remove references
            VCVSposIbrptr = null;
            VCVSnegIbrptr = null;
            VCVSibrPosptr = null;
            VCVSibrNegptr = null;
            VCVSibrContPosptr = null;
            VCVSibrContNegptr = null;
        }

        /// <summary>
        /// Execute behaviour
        /// </summary>
        /// <param name="ckt">Circuit</param>
        public override void Load(Circuit ckt)
        {
            var vcvs = ComponentTyped<VoltageControlledVoltagesource>();
            var cstate = ckt.State;

            VCVSposIbrptr.Add(1.0);
            VCVSibrPosptr.Add(1.0);
            VCVSnegIbrptr.Sub(1.0);
            VCVSibrNegptr.Sub(1.0);
            VCVSibrContPosptr.Sub(load.VCVScoeff);
            VCVSibrContNegptr.Add(load.VCVScoeff);
        }
    }
}
