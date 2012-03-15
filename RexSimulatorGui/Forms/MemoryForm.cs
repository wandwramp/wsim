using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RexSimulator.Hardware;
using RexSimulator.Hardware.Rex;
using RexSimulator.Hardware.Wramp;

namespace RexSimulatorGui.Forms
{
    /// <summary>
    /// Shows the current contents of memory.
    /// </summary>
    public partial class MemoryForm : Form
    {
        #region Member Variables
        private MemoryDevice mDevice;
        private IR mIr;
        private SimpleWrampCpu mCpu;
        private uint mPc, mSp, mRa, mEvec, mEar;
        private uint[] mShadow;

        private ListViewItem[] mVirtualItems;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mem">The memory device to display.</param>
        public MemoryForm(MemoryDevice mem)
        {
            InitializeComponent();

            this.Text = mem.Name;
            this.mDevice = mem;
            this.mIr = new IR();
            this.mShadow = new uint[mem.Size];
            this.mVirtualItems = new ListViewItem[mem.Size];
            memoryListView.VirtualListSize = (int)mem.Size;

            //Populate view buffer
            for (uint i = 0; i < mDevice.Size; i++)
            {
                uint address = i + mDevice.BaseAddress;
                uint value = 0xffffffff; //Note: this should come from memory, but the default value zero is immediately set to 0xffffffff by WRAMPmon, resulting in a complete (slow) redraw.
                mIr.Instruction = value;
                mShadow[i] = value;

                string addressStr = address.ToString("X8");
                string valueStr = value.ToString("X8");
                string disassembly = mIr.ToString();

                mVirtualItems[i] = new ListViewItem(new string[] { "", addressStr, valueStr, disassembly });
            }
            updateTimer.Start();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets the list view to the appropriate address.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="addrName"></param>
        private void GotoAddress(uint addr, string addrName)
        {
            if (0 <= addr && addr < mDevice.Size)
            {
                memoryListView.SelectedIndices.Clear();
                mVirtualItems[addr].Selected = true;
                memoryListView.EnsureVisible((int)addr);
            }
            else
            {
                MessageBox.Show(addrName + " does not currently point into this address space", "Invalid Pointer");
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Override the default closing behaviour. Rather than close the form, simply hide it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MemoryForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        /// <summary>
        /// Redraws items that have changed.
        /// </summary>
        /// <param name="addresses">The items that should be redrawn.</param>
        private void RedrawItem(params uint[] addresses)
        {
            foreach (uint i in addresses)
            {
                if(0 <= i && i < mDevice.Size)
                    memoryListView.RedrawItems((int)i, (int)i, true);
            }
        }

        /// <summary>
        /// Redraw everything.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            //listView1.BeginUpdate();
            //Update all changed memory locations
            for (uint i = 0; i < mDevice.Size; i++)
            {
                if (mDevice[i + mDevice.BaseAddress] != mShadow[i])
                {
                    uint address = i + mDevice.BaseAddress;
                    uint value = mDevice[address];
                    mIr.Instruction = value;
                    mShadow[i] = value;

                    memoryListView.RedrawItems((int)i, (int)i, true);

                    mVirtualItems[i].SubItems[2].Text = value.ToString("X8");
                    mVirtualItems[i].SubItems[3].Text = mIr.ToString();
                }
            }

            if (mCpu != null)
            {
                //Get new address offsets
                uint newPc = mCpu.PC - mDevice.BaseAddress;
                uint newSp = mCpu.mGpRegisters[RegisterFile.GpRegister.sp] - mDevice.BaseAddress;
                uint newRa = mCpu.mGpRegisters[RegisterFile.GpRegister.ra] - mDevice.BaseAddress;
                uint newEvec = mCpu.mSpRegisters[RegisterFile.SpRegister.evec] - mDevice.BaseAddress;
                uint newEar = mCpu.mSpRegisters[RegisterFile.SpRegister.ear] - mDevice.BaseAddress;

                //Clear old locations
                if (0 <= mPc && mPc < mDevice.Size)
                    mVirtualItems[(int)mPc].SubItems[0].Text = "";

                if (0 <= mSp && mSp < mDevice.Size)
                    mVirtualItems[(int)mSp].SubItems[0].Text = "";

                if (0 <= mRa && mRa < mDevice.Size)
                    mVirtualItems[(int)mRa].SubItems[0].Text = "";

                if (0 <= mEvec && mEvec < mDevice.Size)
                    mVirtualItems[(int)mEvec].SubItems[0].Text = "";

                if (0 <= mEar && mEar < mDevice.Size)
                    mVirtualItems[(int)mEar].SubItems[0].Text = "";

                //Set new locations
                if (0 <= newPc && newPc < mDevice.Size)
                    mVirtualItems[(int)newPc].SubItems[0].Text = "$pc =>";

                if (0 <= newSp && newSp < mDevice.Size)
                    mVirtualItems[(int)newSp].SubItems[0].Text = "$sp =>";

                if (0 <= newRa && newRa < mDevice.Size)
                    mVirtualItems[(int)newRa].SubItems[0].Text = "$ra =>";

                if (0 <= newEvec && newEvec < mDevice.Size)
                    mVirtualItems[(int)newEvec].SubItems[0].Text = "$evec =>";

                if (0 <= newEar && newEar < mDevice.Size)
                    mVirtualItems[(int)newEar].SubItems[0].Text = "$ear =>";

                //Redraw any affected items
                RedrawItem(mPc, mSp, mRa, mEvec, mEar, newPc, newSp, newSp, newRa, newEvec, newEvec);

                mPc = newPc;
                mSp = newSp;
                mRa = newRa;

                mEvec = newEvec;
                mEar = newEar;
            }
            //listView1.EndUpdate();
        }

        private void memoryListView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            e.Item = mVirtualItems[e.ItemIndex];
        }

        private void pcToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GotoAddress(mPc, "$pc");
        }

        private void spToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GotoAddress(mSp, "$sp");
        }

        private void raToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GotoAddress(mRa, "$ra");
        }

        private void evecToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GotoAddress(mEvec, "$evec");
        }

        private void earToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GotoAddress(mEar, "$ear");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// If this form should show any pointers, it needs to know what they are...
        /// </summary>
        /// <param name="cpu"></param>
        public void SetCpu(SimpleWrampCpu cpu)
        {
            this.mCpu = cpu;
            mPc = uint.MaxValue;
            mSp = uint.MaxValue;
            mRa = uint.MaxValue;
            menuStrip1.Show();
        }
        #endregion
    }
}
