using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RexSimulator.Hardware.Wramp
{
    /// <summary>
    /// Memory Protection Unit for WRAMP
    /// </summary>
    class MPU
    {
        #region Member Variables
        private RegisterFile mSpRegisters;
        private Bus mAddressBus, mDataBus;
        #endregion

        #region Constructor
        public MPU(RegisterFile SpRegisters, Bus AddressBus, Bus DataBus)
        {
            this.mSpRegisters = SpRegisters;
            this.mAddressBus = AddressBus;
            this.mDataBus = DataBus;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Writes an address to the address bus.
        /// If the CPU is in kernel mode, the address is passed on to the bus unchanged.
        /// Otherwise, the address is offset by $rbase and checked against the protection table referenced by $ptable.
        /// If an access violation is found, a General Protection Fault is generated.
        /// </summary>
        /// <param name="value"></param>
        public void Write(uint value)
        {
            if ((mSpRegisters[RegisterFile.SpRegister.cctrl] & 0x00000008) != 0)
            {
                //Kernel mode, so just pass the address through to the bus
                mAddressBus.Write(value);
            }
            else
            {
                //User mode
                
                //Convert the virtual address to a physical address
                value += mSpRegisters[RegisterFile.SpRegister.rbase];

                //Load appropriate word from the permission table
                uint wordOffset = (value >> 15) & 0x1f;
                wordOffset += mSpRegisters[RegisterFile.SpRegister.ptable];
                mAddressBus.Write(wordOffset);
                uint protectionWord = mDataBus.Value;

                //Check that permission is granted for the requested address
                uint bitOffset = (value >> 10) & 0x1f;
                if ((protectionWord & (0x80000000 >> (int)bitOffset)) == 0)
                {
                    throw new AccessViolationException();
                }

                //Write address to the bus
                mAddressBus.Write(value);
            }
        }
        #endregion
    }
}
