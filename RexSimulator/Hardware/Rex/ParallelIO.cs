using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using RexSimulator.Hardware.Rex;

namespace RexSimulator.Hardware.Rex
{
    /// <summary>
    /// The Parallel IO device on the REX board.
    /// Registers: { Switches, Buttons, Left SSD, Right SSD, Control, Interrupt ack }
    /// </summary>
    public class ParallelIO : MemoryDevice
    {
        /// <summary>
        /// Turns a hexadecimal digit into a seven-segment display code (the segments that should be lit)
        /// </summary>
        private static readonly uint[] SSD_DECODE = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x27, 0x7F, 0x6F, 0x77, 0x7C, 0x39, 0x5E, 0x79, 0x71 };

        /// <summary>
        /// This is set to true every time the SSD is changed. Useful to determine if the WRAMP program has written to the SSD.
        /// </summary>
        public bool SSD_Changed = false;

        #region Accessors
        /// <summary>
        /// The state of the switches.
        /// </summary>
        public uint Switches
        {
            get { return mMemory[0] & 0xff; }
            set
            {
                if (mMemory[0] != value && ((Control & 2) != 0))
                    Interrupt(true);
                mMemory[0] = value;
            }
        }

        /// <summary>
        /// The state of the buttons.
        /// </summary>
        public uint Buttons
        {
            get { return mMemory[1] & 0x03; }
            set
            {
                if (mMemory[1] != value && ((Control & 2) != 0))
                    Interrupt(true);
                mMemory[1] = value;
            }
        }

        /// <summary>
        /// The state of the left SSD.
        /// </summary>
        public uint LeftSSD { get { return mMemory[2] & 0xff; } set { mMemory[2] = value; } }

        /// <summary>
        /// The state of the right SSD.
        /// </summary>
        public uint RightSSD { get { return mMemory[3] & 0xff; } set { mMemory[3] = value; } }

        /// <summary>
        /// Gets the state of both SSDs.
        /// </summary>
        public uint SSD { get { return LeftSSD << 4 | RightSSD; } }

        /// <summary>
        /// The control register.
        /// </summary>
        public uint Control { get { return mMemory[4]; } set { mMemory[4] = value; } }

        /// <summary>
        /// The interrupt acknowledge register.
        /// </summary>
        public uint InterruptAck { get { return mMemory[5]; } set { mMemory[5] = value; } }

        /// <summary>
        /// The raw left SSD output.
        /// </summary>
        public uint LeftSSDOut { get { if ((Control & 1) != 0) return SSD_DECODE[LeftSSD & 0xf]; return LeftSSD; } }

        /// <summary>
        /// The raw right SSD output.
        /// </summary>
        public uint RightSSDOut { get { if ((Control & 1) != 0) return SSD_DECODE[RightSSD & 0xf]; return RightSSD; } }
        #endregion

        #region Constructor
        public ParallelIO(uint baseAddress, uint size, Bus addressBus, Bus dataBus, string name)
            : base(baseAddress, size, addressBus, dataBus, name)
        {

        }
        #endregion

        #region Overrides
        public override void Reset()
        {
            Switches = 0;
            Buttons = 0;
            LeftSSD = 0;
            RightSSD = 0;
            Control = 1;
            Interrupt(false);
        }
        public override void Write()
        {
            if (mAddressBus.Value == mBaseAddress + 0 || mAddressBus.Value == mBaseAddress + 1)
                return; //read-only switches and buttons
            base.Write();

            if (mAddressBus.Value == mBaseAddress + 2 || mAddressBus.Value == mBaseAddress + 3)
                SSD_Changed = true;

            if (mIrqBus != null)
                mIrqBus.SetBit(mIrqNumber, mMemory[mIrqOffset] != 0);
        }
        #endregion
    }
}
