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

namespace RexSimulator.Hardware.Wramp
{
    /// <summary>
    /// A WRAMP register file.
    /// </summary>
    public class RegisterFile
    {
        #region Enums
        /// <summary>
        /// General purpose registers
        /// </summary>
        public enum GpRegister { r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, sp, ra }
        public enum SpRegister { spr0, spr1, spr2, spr3, cctrl, estat, icount, ccount, evec, ear, esp, ers, ptable, rbase, spr14, spr15 }
        #endregion

        #region Member Variables
        private string mName;
        #endregion

        #region Accessors
        public string Name { get { return mName; } }
        #endregion

        private uint[] mRegisters;

        /// <summary>
        /// Creates a new register file.
        /// </summary>
        public RegisterFile(string name)
        {
            mRegisters = new uint[16];
            mName = name;
        }

        #region Public Methods
        /// <summary>
        /// Resets the registers to a known state.
        /// </summary>
        public void Reset(uint value)
        {
            mRegisters[0] = 0;
            for (int i = 1; i < mRegisters.Length; i++)
            {
                mRegisters[i] = value;
            }
        }
        #endregion

        #region Operators
        public uint this[uint register]
        {
            get { return mRegisters[register]; }
            set { mRegisters[register] = value; }
        }
        public uint this[GpRegister register]
        {
            get { return this[(uint)register]; }
            set
            {
                if (register != GpRegister.r0)
                    this[(uint)register] = value;
            }
        }
        public uint this[SpRegister register]
        {
            get { return this[(uint)register]; }
            set { this[(uint)register] = value; }
        }
        #endregion
    }
}
