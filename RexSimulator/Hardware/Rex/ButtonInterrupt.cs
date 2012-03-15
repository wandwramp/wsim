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
