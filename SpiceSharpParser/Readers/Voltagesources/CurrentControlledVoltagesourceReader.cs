﻿using System.Collections.Generic;
using SpiceSharp.Components;

namespace SpiceSharp.Parser.Readers
{
    /// <summary>
    /// A class that can read current-controlled voltage sources
    /// </summary>
    public class CurrentControlledVoltagesourceReader : Reader
    {
        /// <summary>
        /// Read
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="parameters">Parameters</param>
        /// <param name="netlist">Netlist</param>
        /// <returns></returns>
        public override bool Read(Token name, List<object> parameters, Netlist netlist)
        {
            if (name.image[0] != 'h' && name.image[0] != 'H')
                return false;

            CurrentControlledVoltagesource ccvs = new CurrentControlledVoltagesource(name.image);
            ReadNodes(ccvs, parameters, 2);
            switch (parameters.Count)
            {
                case 2: ThrowAfter(parameters[1], "Voltage source expected"); break;
                case 3: ThrowAfter(parameters[2], "Value expected"); break;
            }

            ccvs.Set("control", ReadWord(parameters[2]));
            ccvs.Set("gain", ReadValue(parameters[2]));

            netlist.Circuit.Components.Add(ccvs);
            return true;
        }
    }
}
