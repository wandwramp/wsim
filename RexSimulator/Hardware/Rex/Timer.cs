using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RexSimulator.Hardware.Rex
{
    /// <summary>
    /// The REX timer device.
    /// </summary>
    public class Timer : MemoryDevice
    {
        #region Member Variables
        private int tickCount = 0;
        #endregion

        #region Accessors
        /// <summary>
        /// The control register.
        /// </summary>
        public uint Control { get { return mMemory[0]; } set { mMemory[0] = value; } }

        /// <summary>
        /// The load register.
        /// </summary>
        public uint Load
        {
            get { return mMemory[1] & 0xFFFF; }
            set
            {
                mMemory[1] = value;
                if ((value & 1) != 0)
                    Count = Load;
            }
        }

        /// <summary>
        /// The count register.
        /// </summary>
        public uint Count { get { return mMemory[2] & 0xFFFF; } set { mMemory[2] = value; } }

        /// <summary>
        /// The status register.
        /// </summary>
        public uint InterruptAck
        {
            get { return mMemory[3]; }
            set { mMemory[3] = value; }
        }
        #endregion

        #region Constructor
        public Timer(uint baseAddress, uint size, Bus addressBus, Bus dataBus, string name)
            : base(baseAddress, size, addressBus, dataBus, name)
        {

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// This must be called on every clock tick. Internal timer state is updated.
        /// </summary>
        public void Tick()
        {
            if (tickCount-- == 0)
            {
                tickCount = 1667; //about 2400 Hz, assuming a 4 MHz system clock
               
                //Do timer events
                if ((Control & 1) != 0) //if enabled
                {
                    if (Count-- == 0)
                    {
                        if ((Control & 2) != 0) //if automatic restart
                        {
                            Count = Load;
                        }
                        else
                        {
                            Control = 0; //disable timer - is this correct behaviour?
                        }
                        if (InterruptAck == 1)
                        {
                            Interrupt(true);
                            InterruptAck |= 2; //set overrun bit
                        }
                        else
                        {
                            Interrupt(true);
                        }
                    }
                }
            }
        }
        #endregion

        #region Overrides
        public override void Write()
        {
            if (mAddressBus.Value - mBaseAddress == 0)
            {
                if ((mMemory[0] & 1) == 1 && (mDataBus.Value & 1) == 0)
                    Count = Load;
            }
            else if (mAddressBus.Value - mBaseAddress == 2) //count register is read only.
            {
                return;
            }
            base.Write();
        }
        public override void Reset()
        {
            for (int i = 0; i < mMemory.Length; i++)
            {
                mMemory[i] = 0;
            }
            Interrupt(false);
        }
        #endregion
    }
}
