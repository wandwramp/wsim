using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RexSimulator.Hardware.Rex;

namespace RexSimulatorGui.Forms
{
    /// <summary>
    /// Shows the memory contents of a peripheral device.
    /// This is largely the same as MemoryForm.
    /// </summary>
    public partial class PeripheralMemoryForm : Form
    {
        private MemoryDevice mDevice;
        private uint[] mShadow;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mem">The peripheral device to show the memory contents of.</param>
        public PeripheralMemoryForm(MemoryDevice mem)
        {
            InitializeComponent();

            this.Text = mem.Name + " Registers";
            this.mDevice = mem;
            this.mShadow = new uint[mem.Size];

            //Populate view
            for (uint i = 0; i < mDevice.Size; i++)
            {
                uint address = i + mDevice.BaseAddress;
                uint v = mDevice[address];
                mShadow[i] = v;

                listView1.Items.Add(new ListViewItem(new string[] { address.ToString("X8"), v.ToString(), v.ToString("X8"), Convert.ToString(v, 2).PadLeft(32, '0') }));
            }
            updateTimer.Start();
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            //Update all changed memory locations
            for (uint i = 0; i < mDevice.Size; i++)
            {
                if (mDevice[i + mDevice.BaseAddress] != mShadow[i])
                {
                    uint address = i + mDevice.BaseAddress;
                    uint v = mDevice[address];
                    mShadow[i] = v;

                    listView1.Items[(int)i].SubItems[1].Text = v.ToString();
                    listView1.Items[(int)i].SubItems[2].Text = v.ToString("X8");
                    listView1.Items[(int)i].SubItems[3].Text = Convert.ToString(v, 2).PadLeft(32, '0');
                }
            }
        }

        private void PeripheralMemoryForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
