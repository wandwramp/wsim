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
    /// Registers: { Switches, Buttons, Left SSD, Right SSD, Control, Interrupt ack, LeftLeft SSD, LeftRight SSD, (Right)Left SSD, (Right)Right SSD }
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
            get { return mMemory[0] & 0xffff; }
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
            get { return mMemory[1] & 0x1f; }
            set
            {
                if (mMemory[1] != value && ((Control & 2) != 0))
                    Interrupt(true);
                mMemory[1] = value;
            }
        }

		/// <summary>
        /// The state of the left SSD in the left pair.
        /// </summary>
		public uint LeftLeftSSD { get { return mMemory[6] & 0xff; } set { mMemory[6] = value; } }
		
		/// <summary>
        /// The state of the right SSD in the left pair.
        /// </summary>
        public uint LeftRightSSD { get { return mMemory[7] & 0xff; } set { mMemory[7] = value; } }

        /// <summary>
        /// The state of the left SSD in the right pair.
        /// </summary>
        public uint LeftSSD { get { return mMemory[8] & 0xff; } set { mMemory[2] = value; mMemory[8] = value; } }

        /// <summary>
        /// The state of the right SSD in the right pair.
        /// </summary>
        public uint RightSSD { get { return mMemory[9] & 0xff; } set { mMemory[3] = value; mMemory[9] = value; } }

        /// <summary>
        /// Gets the state of all SSDs.
        /// </summary>
        public uint SSD { get { return ((LeftLeftSSD & 0xf) << 12) | ((LeftRightSSD & 0xf) << 8) | ((LeftSSD & 0xf) << 4) | (RightSSD & 0xf); } }

        /// <summary>
        /// The control register.
        /// </summary>
        public uint Control { get { return mMemory[4]; } set { mMemory[4] = value; } }

        /// <summary>
        /// The interrupt acknowledge register.
        /// </summary>
        public uint InterruptAck { get { return mMemory[5]; } set { mMemory[5] = value; } }
        
        /// <summary>
        /// The state of the LEDs.
        /// </summary>
        public uint Leds { get { return mMemory[10]; } set { mMemory[10] = value; } }

        /// <summary>
        /// The raw left SSD output, for the left pair.
        /// </summary>
        public uint LeftLeftSSDOut { get { if ((Control & 1) != 0) return SSD_DECODE[LeftLeftSSD & 0xf]; return LeftLeftSSD; } }

        /// <summary>
        /// The raw left SSD output, for the left pair.
        /// </summary>
        public uint LeftRightSSDOut { get { if ((Control & 1) != 0) return SSD_DECODE[LeftRightSSD & 0xf]; return LeftRightSSD; } }

        /// <summary>
        /// The raw left SSD output, for the right pair.
        /// </summary>
        public uint LeftSSDOut { get { if ((Control & 1) != 0) return SSD_DECODE[LeftSSD & 0xf]; return LeftSSD; } }

        /// <summary>
        /// The raw right SSD output, for the right pair.
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
            LeftLeftSSD = 0;
            LeftRightSSD = 0;
            LeftSSD = 0;
            RightSSD = 0;
            Control = 1;
            Leds = 0;
            Interrupt(false);
        }
        public override void Write()
        {
            if (mAddressBus.Value == mBaseAddress + 0 || mAddressBus.Value == mBaseAddress + 1)
                return; //read-only switches and buttons
            base.Write();

            // Right pair of SSDs are double-mapped, so we write twice.
            if (mAddressBus.Value == mBaseAddress + 2 || mAddressBus.Value == mBaseAddress + 3)
            {
                mAddressBus.Write(mAddressBus.Value + 6);
                base.Write();
                SSD_Changed = true;
            }
            if (mAddressBus.Value == mBaseAddress + 8 || mAddressBus.Value == mBaseAddress + 9)
            {
                mAddressBus.Write(mAddressBus.Value - 6);
                base.Write();
                SSD_Changed = true;
            }

            if (mIrqBus != null)
                mIrqBus.SetBit(mIrqNumber, mMemory[mIrqOffset] != 0);
        }
        #endregion
    }
}
