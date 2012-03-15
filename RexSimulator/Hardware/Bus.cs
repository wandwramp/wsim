using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RexSimulator.Hardware
{
    /// <summary>
    /// A bus.
    /// </summary>
    public class Bus
    {
        #region Members
        private uint mValue;
        private bool mIsWrite;
        #endregion

        #region Properties
        /// <summary>
        /// If true, then a memory device (not the WRAMP CPU) should write.
        /// </summary>
        public bool IsWrite { get { return mIsWrite; } set { mIsWrite = value; } }

        /// <summary>
        /// Gets the current value on this bus.
        /// </summary>
        public uint Value { get { return mValue; } }
        #endregion

        #region EventArgs
        /// <summary>
        /// Puts the value of the bus through an event handler.
        /// </summary>
        public class BusChangedEventArgs : EventArgs
        {
            /// <summary>
            /// The value on the bus.
            /// </summary>
            public readonly uint Value;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="value">The value to send.</param>
            public BusChangedEventArgs(uint value)
            {
                this.Value = value;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Fired whenever this bus has had its value changed.
        /// </summary>
        public event EventHandler<BusChangedEventArgs> Changed;
        #endregion

        #region Public Methods
        /// <summary>
        /// Writes a value to this bus.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(uint value)
        {
            mValue = value;
            if (Changed != null)
            {
                Changed(this, new BusChangedEventArgs(mValue));
            }
        }

        /// <summary>
        /// Sets a bit in the bus.
        /// </summary>
        /// <param name="bit">The bit to set. 0 for the lsb, 1 for the next and so on.</param>
        /// <param name="on">True to set the bit to '1', false for '0'</param>
        public void SetBit(int bit, bool on)
        {
            if (on)
                mValue |= (1u << bit);
            else
                mValue &= ~(1u << bit);

            if (Changed != null)
            {
                Changed(this, new BusChangedEventArgs(mValue));
            }
        }

        /// <summary>
        /// Gets a bit from the bus.
        /// </summary>
        /// <param name="bit">The bit to get. 0 for the lsb, 1 for the next and so on.</param>
        /// <returns>True if set (1), false if not (0)</returns>
        public bool GetBit(int bit)
        {
            return ((mValue >> bit) & 1) == 1;
        }
        #endregion
    }
}
