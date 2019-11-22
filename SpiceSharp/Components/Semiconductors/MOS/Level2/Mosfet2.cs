﻿using SpiceSharp.Attributes;
using SpiceSharp.Behaviors;
using SpiceSharp.Entities;
using SpiceSharp.Components.MosfetBehaviors.Level2;
using SpiceSharp.Simulations;

namespace SpiceSharp.Components
{
    /// <summary>
    /// A MOS2 Mosfet.
    /// Level 2, A. Vladimirescu and S. Liu, The Simulation of MOS Integrated Circuits Using SPICE2, ERL Memo No. M80/7, Electronics Research Laboratory University of California, Berkeley, October 1980.
    /// </summary>
    [Pin(0, "Drain"), Pin(1, "Gate"), Pin(2, "Source"), Pin(3, "Bulk"), Connected(0, 2), Connected(0, 3)]
    public class Mosfet2 : Component
    {
        /// <summary>
        /// Constants
        /// </summary>
        [ParameterName("pincount"), ParameterInfo("Number of pins")]
		public const int Mosfet2PinCount = 4;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Mosfet2"/> class.
        /// </summary>
        /// <param name="name">The name of the device</param>
        public Mosfet2(string name) : base(name, Mosfet2PinCount)
        {
            Parameters.Add(new BaseParameters());
        }

        /// <summary>
        /// Creates the behaviors for the specified simulation and registers them with the simulation.
        /// </summary>
        /// <param name="simulation">The simulation.</param>
        public override void CreateBehaviors(ISimulation simulation)
        {
            var behaviors = new BehaviorContainer(Name,
                LinkParameters ? Parameters : (IParameterSetDictionary)Parameters.Clone());
            behaviors.Parameters.CalculateDefaults();
            var context = new ComponentBindingContext(simulation, behaviors, MapNodes(simulation.Variables), Model);
            var eb = simulation.EntityBehaviors;
            if (simulation.UsesBehaviors<INoiseBehavior>())
                behaviors.Add(new NoiseBehavior(Name, context));
            else if (simulation.UsesBehaviors<IFrequencyBehavior>())
                behaviors.Add(new FrequencyBehavior(Name, context));
            if (simulation.UsesBehaviors<ITimeBehavior>())
                behaviors.Add(new TransientBehavior(Name, context));

            if (simulation.UsesBehaviors<IBiasingBehavior>())
            {
                if (!behaviors.ContainsKey(typeof(IBiasingBehavior)))
                    behaviors.Add(new BiasingBehavior(Name, context));
            }
            else if (simulation.UsesBehaviors<ITemperatureBehavior>() && !behaviors.ContainsKey(typeof(ITemperatureBehavior)))
                behaviors.Add(new TemperatureBehavior(Name, context));
            simulation.EntityBehaviors.Add(behaviors);
        }
    }
}
