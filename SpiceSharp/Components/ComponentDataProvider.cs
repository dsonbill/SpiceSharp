using System;
using System.Collections.Generic;
using System.Text;

namespace SpiceSharp.Components
{
    /// <summary>
    /// A <see cref="SetupDataProvider"/> class for components.
    /// </summary>
    public class ComponentDataProvider : SetupDataProvider
    {
        /// <summary>
        /// Gets the pins that the component is connected to.
        /// </summary>
        public int[] Pins { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="ComponentDataProvider"/> class.
        /// </summary>
        public ComponentDataProvider()
        {
        }

        /// <summary>
        /// Specify the connected variables.
        /// </summary>
        /// <param name="pins">The pins.</param>
        public void Connect(params int[] pins)
        {
            if (pins == null || pins.Length == 0)
            {
                Pins = new int[0];
                return;
            }

            Pins = new int[pins.Length];
            for (var i = 0; i < pins.Length; i++)
                Pins[i] = pins[i];
        }
    }
}
