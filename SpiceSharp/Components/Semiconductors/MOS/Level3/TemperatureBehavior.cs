﻿using System;
using SpiceSharp.Attributes;
using SpiceSharp.Behaviors;
using SpiceSharp.Diagnostics;
using SpiceSharp.Simulations;

namespace SpiceSharp.Components.MosfetBehaviors.Level3
{
    /// <summary>
    /// Temperature behavior for a <see cref="Mosfet3"/>
    /// </summary>
    public class TemperatureBehavior : Behaviors.TemperatureBehavior
    {
        /// <summary>
        /// Necessary behaviors and parameters
        /// </summary>
        private BaseParameters _bp;
        private ModelBaseParameters _mbp;
        private ModelTemperatureBehavior _modeltemp;

        /// <summary>
        /// Shared parameters
        /// </summary>
        [PropertyName("sourceconductance"), PropertyInfo("Source conductance")]
        public double SourceConductance { get; protected set; }
        [PropertyName("drainconductance"), PropertyInfo("Drain conductance")]
        public double DrainConductance { get; protected set; }
        [PropertyName("sourcevcrit"), PropertyInfo("Critical source voltage")]
        public double SourceVCritical { get; protected set; }
        [PropertyName("drainvcrit"), PropertyInfo("Critical drain voltage")]
        public double DrainVCritical { get; protected set; }
        [PropertyName("cbd0"), PropertyInfo("Zero-Bias B-D junction capacitance")]
        public double CapBd { get; protected set; }
        [PropertyName("cbdsw0"), PropertyInfo("Zero-Bias B-D sidewall capacitance")]
        public double CapBdSidewall { get; protected set; }
        [PropertyName("cbs0"), PropertyInfo("Zero-Bias B-S junction capacitance")]
        public double CapBs { get; protected set; }
        [PropertyName("cbssw0"), PropertyInfo("Zero-Bias B-S sidewall capacitance")]
        public double CapBsSidewall { get; protected set; }

        [PropertyName("rs"), PropertyInfo("Source resistance")]
        public double SourceResistance
        {
            get
            {
                if (SourceConductance > 0.0)
                    return 1.0 / SourceConductance;
                return 0.0;
            }
        }
        [PropertyName("rd"), PropertyInfo("Drain resistance")]
        public double DrainResistance
        {
            get
            {
                if (DrainConductance > 0.0)
                    return 1.0 / DrainConductance;
                return 0.0;
            }
        }

        /// <summary>
        /// Extra variables
        /// </summary>
        public double TempTransconductance { get; protected set; }
        public double TempSurfaceMobility { get; protected set; }
        public double TempPhi { get; protected set; }
        public double TempVoltageBi { get; protected set; }
        public double TempVt0 { get; protected set; }
        public double TempSaturationCurrent { get; protected set; }
        public double TempSaturationCurrentDensity { get; protected set; }
        public double TempCapBd { get; protected set; }
        public double TempCapBs { get; protected set; }
        public double TempJunctionCap { get; protected set; }
        public double TempJunctionCapSidewall { get; protected set; }
        public double TempBulkPotential { get; protected set; }
        public double TempDepletionCap { get; protected set; }
        public double F2D { get; protected set; }
        public double F3D { get; protected set; }
        public double F4D { get; protected set; }
        public double F2S { get; protected set; }
        public double F3S { get; protected set; }
        public double F4S { get; protected set; }
        public double CapGs { get; protected set; }
        public double CapGd { get; protected set; }
        public double CapGb { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        public TemperatureBehavior(Identifier name) : base(name) { }

        /// <summary>
        /// Setup behavior
        /// </summary>
        /// <param name="provider">Data provider</param>
        public override void Setup(SetupDataProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            // Get parameters
            _bp = provider.GetParameterSet<BaseParameters>("entity");
            _mbp = provider.GetParameterSet<ModelBaseParameters>("model");

            // Get behaviors
            _modeltemp = provider.GetBehavior<ModelTemperatureBehavior>("model");
        }

        /// <summary>
        /// Do temperature-dependent calculations
        /// </summary>
        /// <param name="simulation">Base simulation</param>
        public override void Temperature(BaseSimulation simulation)
        {
			if (simulation == null)
				throw new ArgumentNullException(nameof(simulation));

            var state = simulation.RealState;
            double czbd, czbdsw,
                czbs,
                czbssw;

            /* perform the parameter defaulting */

            if (!_bp.Temperature.Given)
            {
                _bp.Temperature.Value = state.Temperature;
            }
            var vt = _bp.Temperature * Circuit.KOverQ;
            var ratio = _bp.Temperature / _mbp.NominalTemperature;
            var fact2 = _bp.Temperature / Circuit.ReferenceTemperature;
            var kt = _bp.Temperature * Circuit.Boltzmann;
            var egfet = 1.16 - 7.02e-4 * _bp.Temperature * _bp.Temperature / (_bp.Temperature + 1108);
            var arg = -egfet / (kt + kt) + 1.1150877 / (Circuit.Boltzmann * (Circuit.ReferenceTemperature + Circuit.ReferenceTemperature));
            var pbfact = -2 * vt * (1.5 * Math.Log(fact2) + Circuit.Charge * arg);

            if (_mbp.DrainResistance.Given)
            {
                if (!_mbp.DrainResistance.Value.Equals(0.0))
                {
                    DrainConductance = 1 / _mbp.DrainResistance;
                }
                else
                {
                    DrainConductance = 0;
                }
            }
            else if (_mbp.SheetResistance.Given)
            {
                if (!_mbp.SheetResistance.Value.Equals(0.0))
                {
                    DrainConductance = 1 / (_mbp.SheetResistance * _bp.DrainSquares);
                }
                else
                {
                    DrainConductance = 0;
                }
            }
            else
            {
                DrainConductance = 0;
            }
            if (_mbp.SourceResistance.Given)
            {
                if (!_mbp.SourceResistance.Value.Equals(0.0))
                {
                    SourceConductance = 1 / _mbp.SourceResistance;
                }
                else
                {
                    SourceConductance = 0;
                }
            }
            else if (_mbp.SheetResistance.Given)
            {
                if (!_mbp.SheetResistance.Value.Equals(0.0))
                {
                    SourceConductance = 1 / (_mbp.SheetResistance * _bp.SourceSquares);
                }
                else
                {
                    SourceConductance = 0;
                }
            }
            else
            {
                SourceConductance = 0;
            }

            if (_bp.Length - 2 * _mbp.LateralDiffusion <= 0)
                throw new CircuitException("{0}: effective channel length less than zero".FormatString(Name));
            var ratio4 = ratio * Math.Sqrt(ratio);
            TempTransconductance = _mbp.Transconductance / ratio4;
            TempSurfaceMobility = _mbp.SurfaceMobility / ratio4;
            var phio = (_mbp.Phi - _modeltemp.PbFactor1) / _modeltemp.Fact1;
            TempPhi = fact2 * phio + pbfact;
            TempVoltageBi = _mbp.Vt0 - _mbp.MosfetType * (_mbp.Gamma * Math.Sqrt(_mbp.Phi)) + .5 * (_modeltemp.EgFet1 - egfet) +
                _mbp.MosfetType * .5 * (TempPhi - _mbp.Phi);
            TempVt0 = TempVoltageBi + _mbp.MosfetType * _mbp.Gamma * Math.Sqrt(TempPhi);
            TempSaturationCurrent = _mbp.JunctionSatCur * Math.Exp(-egfet / vt + _modeltemp.EgFet1 / _modeltemp.VtNominal);
            TempSaturationCurrentDensity = _mbp.JunctionSatCurDensity * Math.Exp(-egfet / vt + _modeltemp.EgFet1 / _modeltemp.VtNominal);
            var pbo = (_mbp.BulkJunctionPotential - _modeltemp.PbFactor1) / _modeltemp.Fact1;
            var gmaold = (_mbp.BulkJunctionPotential - pbo) / pbo;
            var capfact = 1 / (1 + _mbp.BulkJunctionBotGradingCoefficient * (4e-4 * (_mbp.NominalTemperature - Circuit.ReferenceTemperature) - gmaold));
            TempCapBd = _mbp.CapBd * capfact;
            TempCapBs = _mbp.CapBs * capfact;
            TempJunctionCap = _mbp.BulkCapFactor * capfact;
            capfact = 1 / (1 + _mbp.BulkJunctionSideGradingCoefficient * (4e-4 * (_mbp.NominalTemperature - Circuit.ReferenceTemperature) - gmaold));
            TempJunctionCapSidewall = _mbp.SidewallCapFactor * capfact;
            TempBulkPotential = fact2 * pbo + pbfact;
            var gmanew = (TempBulkPotential - pbo) / pbo;
            capfact = 1 + _mbp.BulkJunctionBotGradingCoefficient * (4e-4 * (_bp.Temperature - Circuit.ReferenceTemperature) - gmanew);
            TempCapBd *= capfact;
            TempCapBs *= capfact;
            TempJunctionCap *= capfact;
            capfact = 1 + _mbp.BulkJunctionSideGradingCoefficient * (4e-4 * (_bp.Temperature - Circuit.ReferenceTemperature) - gmanew);
            TempJunctionCapSidewall *= capfact;
            TempDepletionCap = _mbp.ForwardCapDepletionCoefficient * TempBulkPotential;

            if (_mbp.JunctionSatCurDensity.Value <= 0 || _bp.DrainArea.Value <= 0 || _bp.SourceArea.Value <= 0)
            {
                SourceVCritical = DrainVCritical = vt * Math.Log(vt / (Circuit.Root2 * _mbp.JunctionSatCur));
            }
            else
            {
                DrainVCritical = vt * Math.Log(vt / (Circuit.Root2 * _mbp.JunctionSatCurDensity * _bp.DrainArea));
                SourceVCritical = vt * Math.Log(vt / (Circuit.Root2 * _mbp.JunctionSatCurDensity * _bp.SourceArea));
            }
            if (_mbp.CapBd.Given)
            {
                czbd = TempCapBd;
            }
            else
            {
                if (_mbp.BulkCapFactor.Given)
                {
                    czbd = TempJunctionCap * _bp.DrainArea;
                }
                else
                {
                    czbd = 0;
                }
            }
            if (_mbp.SidewallCapFactor.Given)
            {
                czbdsw = TempJunctionCapSidewall * _bp.DrainPerimeter;
            }
            else
            {
                czbdsw = 0;
            }
            arg = 1 - _mbp.ForwardCapDepletionCoefficient;
            var sarg = Math.Exp(-_mbp.BulkJunctionBotGradingCoefficient * Math.Log(arg));
            var sargsw = Math.Exp(-_mbp.BulkJunctionSideGradingCoefficient * Math.Log(arg));
            CapBd = czbd;
            CapBdSidewall = czbdsw;
            F2D = czbd * (1 - _mbp.ForwardCapDepletionCoefficient * (1 + _mbp.BulkJunctionBotGradingCoefficient)) * sarg / arg + czbdsw * (1 -
                _mbp.ForwardCapDepletionCoefficient * (1 + _mbp.BulkJunctionSideGradingCoefficient)) * sargsw / arg;
            F3D = czbd * _mbp.BulkJunctionBotGradingCoefficient * sarg / arg / _mbp.BulkJunctionPotential + czbdsw *
                _mbp.BulkJunctionSideGradingCoefficient * sargsw / arg / _mbp.BulkJunctionPotential;
            F4D = czbd * _mbp.BulkJunctionPotential * (1 - arg * sarg) / (1 - _mbp.BulkJunctionBotGradingCoefficient) + czbdsw *
                _mbp.BulkJunctionPotential * (1 - arg * sargsw) / (1 - _mbp.BulkJunctionSideGradingCoefficient) - F3D / 2 * (TempDepletionCap *
                TempDepletionCap) - TempDepletionCap * F2D;
            if (_mbp.CapBs.Given)
            {
                czbs = TempCapBs;
            }
            else
            {
                if (_mbp.BulkCapFactor.Given)
                {
                    czbs = TempJunctionCap * _bp.SourceArea;
                }
                else
                {
                    czbs = 0;
                }
            }
            if (_mbp.SidewallCapFactor.Given)
            {
                czbssw = TempJunctionCapSidewall * _bp.SourcePerimeter;
            }
            else
            {
                czbssw = 0;
            }
            arg = 1 - _mbp.ForwardCapDepletionCoefficient;
            sarg = Math.Exp(-_mbp.BulkJunctionBotGradingCoefficient * Math.Log(arg));
            sargsw = Math.Exp(-_mbp.BulkJunctionSideGradingCoefficient * Math.Log(arg));
            CapBs = czbs;
            CapBsSidewall = czbssw;
            F2S = czbs * (1 - _mbp.ForwardCapDepletionCoefficient * (1 + _mbp.BulkJunctionBotGradingCoefficient)) * sarg / arg + czbssw * (1 -
                _mbp.ForwardCapDepletionCoefficient * (1 + _mbp.BulkJunctionSideGradingCoefficient)) * sargsw / arg;
            F3S = czbs * _mbp.BulkJunctionBotGradingCoefficient * sarg / arg / _mbp.BulkJunctionPotential + czbssw *
                _mbp.BulkJunctionSideGradingCoefficient * sargsw / arg / _mbp.BulkJunctionPotential;
            F4S = czbs * _mbp.BulkJunctionPotential * (1 - arg * sarg) / (1 - _mbp.BulkJunctionBotGradingCoefficient) + czbssw *
                _mbp.BulkJunctionPotential * (1 - arg * sargsw) / (1 - _mbp.BulkJunctionSideGradingCoefficient) - F3S / 2 * (TempBulkPotential *
                TempBulkPotential) - TempBulkPotential * F2S;
        }
    }
}
