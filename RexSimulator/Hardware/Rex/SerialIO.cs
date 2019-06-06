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
using System.Threading;

namespace RexSimulator.Hardware.Rex
{
    public class SerialIO : MemoryDevice
    {
        #region Defines
        /// <summary>
        /// Set this to the board's primary clock rate. It is used to determine how many clock cycles are required to transmit/receive serial data.
        /// </summary>
        private readonly uint SYSTEM_CLOCK_RATE = 6250000;
        #endregion

        #region Member Variables
        private uint mClocksToTransmit = 0, mClocksToReceive = 0, mClocksToProcessRecv = 0;
        private uint mRecvValue;
        #endregion

        #region Accessors
        /// <summary>
        /// The baud rate of this device, in bits/second.
        /// </summary>
        protected uint BaudRate
        {
            get
            {
                return (uint)Math.Pow(2, Control & 0x00000007) * 300;
            }
        }

        /// <summary>
        /// The number of bits per symbol. Data bits + start bit + stop bit(s) + parity.
        /// </summary>
        protected uint BitsPerSymbol
        {
            get
            {
                uint dataBits = ((Control >> 6) & 0x00000003) + 5;
                uint parity = (Control >> 4) & 0x00000003;
                if (parity == 0x02 || parity == 0x03)
                    parity = 1;
                else
                    parity = 0;

                uint stopBits = ((Control >> 3) & 1) + 1;
                return dataBits + parity + stopBits + 1; //1 start bit as well
            }
        }

        /// <summary>
        /// The number of clock cycles required to transmit/receive a single symbol.
        /// </summary>
        protected uint ClocksPerSymbol
        {
            get
            {
                return BitsPerSymbol * SYSTEM_CLOCK_RATE / BaudRate;
            }
        }

        /// <summary>
        /// The transmit register.
        /// </summary>
        public uint Transmit
        {
            get { return mMemory[0]; }
            set
            {
                Status &= 0xFFFFFFFD;
                mMemory[0] = value & 0xFF;
                mClocksToTransmit = ClocksPerSymbol;
            }
        }

        /// <summary>
        /// The receive register.
        /// </summary>
        public uint Receive
        {
            get
            {
                Status &= 0xFFFFFFFE;
                return mMemory[1];
            }
            set
            {
                mClocksToReceive = ClocksPerSymbol;
                mRecvValue = value & 0xFF;
            }
        }

        /// <summary>
        /// The control register.
        /// </summary>
        public uint Control { get { return mMemory[2]; } set { mMemory[2] = value; } }

        /// <summary>
        /// The status register.
        /// </summary>
        public uint Status { get { return mMemory[3]; } set { mMemory[3] = value; } }

        /// <summary>
        /// The interrupt acknowledge register.
        /// </summary>
        public uint InterruptAck { get { return mMemory[4]; } set { mMemory[4] = value; } }
        #endregion

        #region Events
        public class SerialEventArgs : EventArgs
        {
            public readonly uint Data;
            public SerialEventArgs(uint value)
            {
                this.Data = value;
            }
        }
        public event EventHandler<SerialEventArgs> SerialDataTransmitted;
        #endregion

        #region Constructor
        public SerialIO(uint baseAddress, uint size, Bus addressBus, Bus dataBus, string name)
            : base(baseAddress, size, addressBus, dataBus, name)
        {
            
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Overloads the transmit register to send a byte to the host.
        /// Other write operations as usual.
        /// </summary>
        public override void Write()
        {
            if (mAddressBus.Value == mBaseAddress + 1)
                return; //read-only RECV

            if (mBaseAddress <= mAddressBus.Value && mAddressBus.Value < mBaseAddress + mMemory.Length
                && mAddressBus.IsWrite)
            {
                uint address = mAddressBus.Value - mBaseAddress;
                switch (address)
                {
                    case 0: Transmit = mDataBus.Value; break;
                    default: mMemory[address] = mDataBus.Value; break;
                }
            }

            if (mIrqBus != null)
                mIrqBus.SetBit(mIrqNumber, mMemory[mIrqOffset] != 0);
        }

        /// <summary>
        /// Overloads the receive register: RDR bit is cleared on read, when a byte is read by the WRAMP board.
        /// Other read operations as usual.
        /// </summary>
        public override void Read()
        {
            if (mBaseAddress <= mAddressBus.Value && mAddressBus.Value < mBaseAddress + mMemory.Length
                && !mAddressBus.IsWrite)
            {
                uint address = mAddressBus.Value - mBaseAddress;
                switch(address)
                {
                    case 1: mDataBus.Write(Receive); break;
                    default: mDataBus.Write(mMemory[mAddressBus.Value - mBaseAddress]); break;
                }
            }
        }

        public override void Reset()
        {
            mClocksToProcessRecv = 0;
            mClocksToReceive = 0;
            mClocksToTransmit = 0;

            for (int i = 0; i < mMemory.Length; i++)
            {
                mMemory[i] = 0;
            }
            Control = 0xC7; //8 data bits, no parity, 1 stop bit, 38400 baud
            Status = 0x02;
            Interrupt(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sends a character to the WRAMP board.
        /// Note: this is blocking until the serial device is ready. If the WRAMP CPU is clocked from the same thread as the one calling this, a deadlock will result.
        /// </summary>
        /// <param name="c"></param>
        public void Send(char c)
        {
            do
            {
                if (mClocksToReceive == 0)
                {
                    if((Status & 1) == 0 || mClocksToProcessRecv == 0)
                    {
                        Receive = c;
                        break;
                    }
                }
                //Thread.Yield();
                Thread.Sleep(0);
            }
            while (true);
        }

        /// <summary>
        /// Attempts to send a character to the serial port.
        /// </summary>
        /// <param name="c">The character to send</param>
        /// <returns>True if the character was successfully sent.</returns>
        public bool SendAsync(char c)
        {
            if (mClocksToReceive == 0)
            {
                if ((Status & 1) == 0 || mClocksToProcessRecv == 0)
                {
                    Receive = c;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Must be called on every clock tick. Used to perfom internal operations.
        /// </summary>
        public void Tick()
        {
            //Only transmit once the serialisation delay is over
            if (mClocksToTransmit > 0)
            {
                if (--mClocksToTransmit == 0)
                {
                    if (SerialDataTransmitted != null)
                        SerialDataTransmitted(this, new SerialEventArgs(Transmit));
                    if ((Control & 0x200u) != 0)
                    {
                        uint oldIack = InterruptAck;
                        Interrupt(true);
                        InterruptAck = oldIack | 2;  // Set TDS Interrupt bit
                    }
                    Status |= 0x00000002;
                }
            }

            if (mClocksToReceive > 0)
            {
                if (mClocksToReceive == 1)
                {
                    mClocksToProcessRecv = 100000;
                    mMemory[1] = mRecvValue;
                    Status |= 0x00000001;
                    if ((Control & 0x100u) != 0)
                    {
                        uint oldIack = InterruptAck;
                        Interrupt(true);
                        InterruptAck = oldIack | 1;  // Set RDR Interrupt bit - redundant, but explicit
                    }
                }
                mClocksToReceive--;
            }

            if (mClocksToProcessRecv > 0)
                mClocksToProcessRecv--;
        }
        #endregion
    }
}
