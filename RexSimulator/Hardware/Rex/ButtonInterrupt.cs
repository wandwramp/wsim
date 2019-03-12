/*
########################################################################
# This file is part of wsim, a WRAMP simulator.
#
# Copyright (C) 2016 Paul Monigatti
# Copyright (C) 2019 The University of Waikato, Hamilton, New Zealand.
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <https://www.gnu.org/licenses/>.
########################################################################
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RexSimulator.Hardware.Rex
{
    /// <summary>
    /// A device that generates a button upon 'pressing' a button.
    /// </summary>
    public class ButtonInterrupt : MemoryDevice
    {
        #region Accessors
        /// <summary>
        /// The interrupt acknowledge register.
        /// </summary>
        public uint InterruptAck
        {
            get { return mMemory[0] & 1; }
            set { mMemory[0] = value & 1; }
        }
        #endregion

        #region Constructor
        public ButtonInterrupt(uint baseAddress, uint size, Bus addressBus, Bus dataBus, string name)
            : base(baseAddress, size, addressBus, dataBus, name)
        {

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs a button press.
        /// </summary>
        public void PressButton()
        {
            Interrupt(true);
        }
        #endregion

        #region Overrides
        public override void Reset()
        {
            Interrupt(false);
        }
        #endregion
    }
}
