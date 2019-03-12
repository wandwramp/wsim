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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RexSimulator.Hardware.Wramp;

namespace RexSimulatorGui.Forms
{
    /// <summary>
    /// Shows the current contents of a register file.
    /// </summary>
    public partial class RegisterForm : Form
    {
        #region Member Variables
        private RegisterFile mReg;
        private bool mIsSpecial;
        public string[] mGpRegNames = new string[] { "$0", "$1", "$2", "$3", "$4", "$5", "$6", "$7", "$8", "$9", "$10", "$11", "$12", "$13", "$sp", "$ra" };
        public string[] mSpRegNames = new string[] { "$spr0", "$spr1", "$spr2", "$spr3", "$cctrl", "$estat", "$icount", "$ccount", "$evec", "$ear", "$esp", "$ers", "$ptable", "$rbase", "$spr14", "$spr15" };
        private uint[] mShadow;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="reg">The register file to display.</param>
        /// <param name="isSpecial">True if the register file is the special registers.</param>
        public RegisterForm(RegisterFile reg, bool isSpecial)
        {
            InitializeComponent();

            this.Text = reg.Name;
            this.mReg = reg;
            this.mIsSpecial = isSpecial;

            this.mShadow = new uint[16];

            //Add an entry for all WRAMP registers
            for (uint i = 0; i <= 0xf; i++)
            {
                RegisterFile.GpRegister greg = (RegisterFile.GpRegister)i;
                RegisterFile.SpRegister sreg = (RegisterFile.SpRegister)i;

                uint v;
                string name;
                if (mIsSpecial)
                {
                    v = mReg[sreg];
                    name = mSpRegNames[i];
                }
                else
                {
                    v = mReg[greg];
                    name = mGpRegNames[i];
                }

                listView1.Items.Add(new ListViewItem(new string[] { name, v.ToString(), v.ToString("X8"), Convert.ToString(v, 2).PadLeft(32, '0') }));
            }
            updateTimer.Start();
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            for (uint i = 0; i <= 0xf; i++)
            {
                uint v = mReg[i];

                if (v != mShadow[i])
                {
                    listView1.Items[(int)i].SubItems[1].Text = v.ToString();
                    listView1.Items[(int)i].SubItems[2].Text = v.ToString("X8");
                    listView1.Items[(int)i].SubItems[3].Text = Convert.ToString(v, 2).PadLeft(32, '0');

                    mShadow[i] = v;
                }
            }
        }

        /// <summary>
        /// Override the default closing behaviour. Rather than close the form, simply hide it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegisterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
