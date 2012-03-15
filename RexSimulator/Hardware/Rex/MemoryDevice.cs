using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RexSimulator.Hardware.Rex
{
    /// <summary>
    /// A memory (or memory-mapped) device.
    /// </summary>
    public class MemoryDevice
    {
        #region Members
        protected uint mBaseAddress;
        protected uint[] mMemory;
        protected Bus mAddressBus, mDataBus, mIrqBus;
        protected string mName;
        protected int mIrqNumber = int.MinValue, mIrqOffset = int.MinValue;
        #endregion

        #region Properties
        /// <summary>
        /// The name of this device.
        /// </summary>
        public string Name { get { return mName; } }
        /// <summary>
        /// The base address of this device.
        /// </summary>
        public uint BaseAddress { get { return mBaseAddress; } }
        /// <summary>
        /// The number of words that can be stored in this device.
        /// </summary>
        public uint Size { get { return (uint)mMemory.Length; } }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new Memory Device.
        /// </summary>
        /// <param name="baseAddress">The address that this device begins with.</param>
        /// <param name="size">The number of words that this device can store.</param>
        /// <param name="addressBus">The address bus to use.</param>
        /// <param name="dataBus">The data bus to use.</param>
        /// <param name="irqBus">The IRQ bus to use, if needed.</param>
        /// <param name="name">Human-readable name of this device.</param>
        public MemoryDevice(uint baseAddress, uint size, Bus addressBus, Bus dataBus, string name)
        {
            mBaseAddress = baseAddress;
            mMemory = new uint[size];

            for (int i = 0; i < mMemory.Length; i++)
            {
                mMemory[i] = uint.MaxValue;
            }

            mAddressBus = addressBus;
            mDataBus = dataBus;
            
            mName = name;

            mAddressBus.Changed += new EventHandler<Bus.BusChangedEventArgs>(mAddressBus_Updated);
            Reset();
        }

        /// <summary>
        /// If the address bus is updated, read the new value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mAddressBus_Updated(object sender, Bus.BusChangedEventArgs e)
        {
            Read();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Attaches an IRQ line to this device.
        /// </summary>
        /// <param name="irqBus">The bus of all IRQ lines.</param>
        /// <param name="irqNumber">The IRQ number to use.</param>
        /// <param name="irqOffset">The offset to the interrupt acknowledgement register in this device.</param>
        public void AttachIRQ(Bus irqBus, int irqNumber, int irqOffset)
        {
            mIrqBus = irqBus;
            mIrqNumber = irqNumber;
            mIrqOffset = irqOffset;
        }

        /// <summary>
        /// Resets the device into a known state.
        /// </summary>
        public virtual void Reset()
        {
            for (int i = 0; i < mMemory.Length; i++)
            {
                mMemory[i] = uint.MaxValue;
            }
        }

        /// <summary>
        /// Reads from memory to the data bus.
        /// </summary>
        public virtual void Read()
        {
            if (mBaseAddress <= mAddressBus.Value && mAddressBus.Value < mBaseAddress + mMemory.Length
                && !mAddressBus.IsWrite)
            {
                mDataBus.Write(mMemory[mAddressBus.Value - mBaseAddress]);
            }
        }

        /// <summary>
        /// Writes the value on the data bus to memory, iff:
        /// -The memory address is within the range of this device
        /// -The memory address bus is set to write
        /// </summary>
        public virtual void Write()
        {
            if (mBaseAddress <= mAddressBus.Value && mAddressBus.Value < mBaseAddress + mMemory.Length
                && mAddressBus.IsWrite)
            {
                mMemory[mAddressBus.Value - mBaseAddress] = mDataBus.Value;
                if (mIrqBus != null)
                    mIrqBus.SetBit(mIrqNumber, mMemory[mIrqOffset] != 0);
            }
        }

        /// <summary>
        /// Loads an array into memory.
        /// </summary>
        /// <param name="arr">The array to load.</param>
        public void Load(uint[] arr, int address)
        {
            Array.Copy(arr, 0, mMemory, address, arr.Length);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Sets the IRQ line appropriately
        /// </summary>
        /// <param name="on"></param>
        protected void Interrupt(bool on)
        {
            if (mIrqBus != null)
            {
                mMemory[mIrqOffset] = on ? 1u : 0u;
                mIrqBus.SetBit(mIrqNumber, on);
            }
        }
        #endregion

        #region Operators
        /// <summary>
        /// Allows direct access to the internal memory contents.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public uint this[uint address]
        {
            get { return mMemory[address - mBaseAddress]; }
        }
        #endregion
    }
}
