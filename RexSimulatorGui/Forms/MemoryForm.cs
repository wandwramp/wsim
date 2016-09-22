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
        private uint mPc, mSp, mRa, mEvec, mEar, mRbase, mPtable;
        private uint[] mShadow;

        private ListViewItem[] mVirtualItems;

        private List<uint> mBreakpoints;
        #endregion

        #region Accessors
        /// <summary>
        /// Gets a list of currently set breakpoints.
        /// </summary>
        public List<uint> Breakpoints
        {
            get { return mBreakpoints; }
        }
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
            mBreakpoints = new List<uint>();

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

        /// <summary>
        /// Sets the text in the "pointer" column of the table.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="text"></param>
        /// <param name="append"></param>
        private void SetPointerText(uint address, string text, bool append)
        {
            address -= mDevice.BaseAddress;

            if (0 <= address && address < mDevice.Size)
            {
                if (!append)
                    mVirtualItems[(int)address].SubItems[0].Text = "";

                mVirtualItems[(int)address].SubItems[0].Text += text;
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
                uint newPc = mCpu.PC;
                uint newSp = mCpu.mGpRegisters[RegisterFile.GpRegister.sp];
                uint newRa = mCpu.mGpRegisters[RegisterFile.GpRegister.ra];
                uint newEvec = mCpu.mSpRegisters[RegisterFile.SpRegister.evec];
                uint newEar = mCpu.mSpRegisters[RegisterFile.SpRegister.ear];
                uint newRbase = mCpu.mSpRegisters[RegisterFile.SpRegister.rbase];
                uint newPtable = mCpu.mSpRegisters[RegisterFile.SpRegister.ptable];

                //Offset program counter by virtual address if enabled
                if ((mCpu.mSpRegisters[RegisterFile.SpRegister.cctrl] & 0x8) == 0)
                {
                    newPc += newRbase;
                    newSp += newRbase;
                    newRa += newRbase;
                    newEar += newRbase;
                }

                //Clear old locations
                SetPointerText(mPc, "", false);
                SetPointerText(mSp, "", false);
                SetPointerText(mRa, "", false);
                SetPointerText(mEvec, "", false);
                SetPointerText(mEar, "", false);
                SetPointerText(mRbase, "", false);
                SetPointerText(mPtable, "", false);

                //Set new locations
                SetPointerText(newPc, "$pc ", true);
                SetPointerText(newSp, "$sp ", true);
                SetPointerText(newRa, "$ra ", true);
                SetPointerText(newEvec, "$evec ", true);
                SetPointerText(newEar, "$ear ", true);
                SetPointerText(newRbase, "$rbase ", true);
                SetPointerText(newPtable, "$ptable ", true);

                //Redraw any affected items
                RedrawItem(mPc, mSp, mRa, mEvec, mEar, mRbase, mPtable, newPc, newSp, newSp, newRa, newEvec, newEvec, newRbase, newPtable);

                mPc = newPc;
                mSp = newSp;
                mRa = newRa;

                mEvec = newEvec;
                mEar = newEar;

                mRbase = newRbase;
                mPtable = newPtable;
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

        private void rbaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GotoAddress(mRbase, "$rbase");
        }

        private void ptableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GotoAddress(mPtable, "$ptable");
        }

        /// <summary>
        /// Sets or removes a breakpoint.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void memoryListView_DoubleClick(object sender, EventArgs e)
        {
            if (memoryListView.SelectedIndices.Count == 1)
            {
                uint addr = (uint)memoryListView.SelectedIndices[0];

                if (mBreakpoints.Contains(addr))
                {
                    mBreakpoints.Remove(addr);
                    mVirtualItems[addr].SubItems[1].Text = addr.ToString("X8");
                }
                else
                {
                    if (addr == mCpu.mSpRegisters[RegisterFile.SpRegister.evec])
                    {
                        MessageBox.Show("It is not possible to place a breakpoint at the same address at $evec, since the CPU will execute the instruction at the address specified by $evec before it has a chance to break execution.", "Invalid Breakpoint", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    mBreakpoints.Add(addr);
                    mVirtualItems[addr].SubItems[1].Text = addr.ToString("X8") + " [B]";
                }

                RedrawItem(addr);
            }
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
