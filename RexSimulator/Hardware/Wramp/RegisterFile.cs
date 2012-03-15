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
