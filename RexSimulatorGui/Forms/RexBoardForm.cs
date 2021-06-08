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
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RexSimulator.Hardware;
using System.IO;
using RexSimulatorGui.Properties;
using System.Threading;
using RexSimulator.Hardware.Wramp;
using System.Reflection;
using System.Media;

namespace RexSimulatorGui.Forms
{
    /// <summary>
    /// The main form for the simulator GUI.
    /// </summary>
    public partial class RexBoardForm : Form
    {
        #region Defines
        /// <summary>
        /// The clock rate that the simulator should (try to) run at, if throttling is enabled.
        /// </summary>
        private const long TARGET_CLOCK_RATE = 6250000;
        #endregion

        #region Member Variables
        private RexBoard mRexBoard;
        private Thread mWorker;
        private static ManualResetEvent mWorkerEnabler;

        private BasicSerialPortForm mSerialForm1;
        private BasicSerialPortForm mSerialForm2;
        private RegisterForm mGpRegisterForm;
        private RegisterForm mSpRegisterForm;
        private MemoryForm mRamForm;
        private PeripheralMemoryForm mInterruptButtonForm;
        private PeripheralMemoryForm mSerialConfigForm1;
        private PeripheralMemoryForm mSerialConfigForm2;
        private PeripheralMemoryForm mParallelConfigForm;
        private PeripheralMemoryForm mTimerConfigForm;

        private List<Form> mSubforms;

        private long mLastTickCount = 0;
        private DateTime mLastTickCountUpdate = DateTime.Now;
        public double mLastClockRate = TARGET_CLOCK_RATE;
        private double mLastClockRateSmoothed = TARGET_CLOCK_RATE;
        private bool mThrottleCpu = true;

        private bool mRunning = true;
        private bool mStepping = false;
        private bool mRunOverBreakpoint = false;
        #endregion

        #region Constructor
        public RexBoardForm()
        {
            InitializeComponent();

            //Set up form contents
            ResetToolStripStatusLabel();

            //Set up all REX and WRAMP hardware
            mRexBoard = rexWidget1.mBoard;

            //Load WRAMPmon into ROM
            Stream wmon = new MemoryStream(ASCIIEncoding.ASCII.GetBytes(Resources.monitor_srec));
            rexWidget1.LoadSrec(wmon);
            wmon.Close();

            //Set up the worker thread
            mWorkerEnabler = new ManualResetEvent(true); // CPU begins in a running state, since the reset is finished
            mWorker = new Thread(new ThreadStart(Worker));
            mRexBoard.SetTickEnabler(mWorkerEnabler);

            //Set up all forms
            mSubforms = new List<Form>();
            mSerialForm1 = new BasicSerialPortForm(mRexBoard.Serial1);
            mSerialForm2 = new BasicSerialPortForm(mRexBoard.Serial2);
            mGpRegisterForm = new RegisterForm(mRexBoard.CPU.mGpRegisters, false);
            mSpRegisterForm = new RegisterForm(mRexBoard.CPU.mSpRegisters, true);
            mRamForm = new MemoryForm(mRexBoard.RAM);
            mRamForm.SetCpu(mRexBoard.CPU);
            mInterruptButtonForm = new PeripheralMemoryForm(mRexBoard.InterruptButton);
            mSerialConfigForm1 = new PeripheralMemoryForm(mRexBoard.Serial1);
            mSerialConfigForm2 = new PeripheralMemoryForm(mRexBoard.Serial2);
            mParallelConfigForm = new PeripheralMemoryForm(mRexBoard.Parallel);
            mTimerConfigForm = new PeripheralMemoryForm(mRexBoard.Timer);
            
            // All the sound sources share a single quacker to ensure there's no overlap.
            Quacker quacker = new Quacker(Resources.duck_quack);
            rexWidget1.SetQuacker(quacker);
            mSerialForm1.SetQuacker(quacker);
            mSerialForm2.SetQuacker(quacker);

            //Add all forms to the list of subforms
            mSubforms.Add(mSerialForm1);
            mSubforms.Add(mSerialForm2);
            mSubforms.Add(mGpRegisterForm);
            mSubforms.Add(mSpRegisterForm);
            mSubforms.Add(mRamForm);
            mSubforms.Add(mInterruptButtonForm);
            mSubforms.Add(mSerialConfigForm1);
            mSubforms.Add(mSerialConfigForm2);
            mSubforms.Add(mParallelConfigForm);
            mSubforms.Add(mTimerConfigForm);
            
            //Wire up event handlers
            foreach (Form f in mSubforms)
            {
                f.VisibleChanged += new EventHandler(SubForm_VisibleChanged);
            }

            //Set the GUI update timer going!
            updateTimer.Start();
        }
        #endregion

        #region Thread Workers
        /// <summary>
        /// Functions as the board's clock source.
        /// </summary>
        private void Worker()
        {
            int stepCount = 0;
            int stepsPerSleep = 0;

            while (true)
            {
                // Wait around if the CPU shouldn't be ticking at the moment, such as during a reset
                mWorkerEnabler.WaitOne();

                uint physPC = mRexBoard.CPU.PC;

                //Convert to physical address
                if ((mRexBoard.CPU.mSpRegisters[RegisterFile.SpRegister.cctrl] & 0x8) == 0)
                {
                    physPC += mRexBoard.CPU.mSpRegisters[RegisterFile.SpRegister.rbase];
                }
                //stop the CPU if a breakpoint has been hit and we're not trying to step over it or continue regardless
                if (!mStepping && mRunning && !mRunOverBreakpoint && mRamForm.Breakpoints.Contains(physPC))
                {
                    this.Invoke(new Action(runButton.PerformClick));
                    continue;
                }
                mRunOverBreakpoint = false;

                if (mRunning)
                {
                    rexWidget1.Step();
                    mRunning ^= mStepping; //stop the CPU running if this is only supposed to do a single step.

                    //Slow the processor down if need be
                    if (mThrottleCpu)
                    {
                        if (stepCount++ >= stepsPerSleep)
                        {
                            stepCount -= stepsPerSleep;
                            Thread.Sleep(5);
                            int diff = (int)mLastClockRate - (int)TARGET_CLOCK_RATE;
                            stepsPerSleep -= diff / 10000;
                            stepsPerSleep = Math.Min(Math.Max(0, stepsPerSleep), 1000000);
                        }
                    }
                }
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Show/hide forms if the checkboxes are clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                EventHandler d = new EventHandler(Checkbox_CheckedChanged);
                this.Invoke(d, sender, e);
            }
            else
            {
                mSerialForm1.Visible = serialForm1Checkbox.Checked;
                mSerialForm2.Visible = serialForm2Checkbox.Checked;
                mGpRegisterForm.Visible = gprCheckbox.Checked;
                mSpRegisterForm.Visible = sprCheckbox.Checked;
                mRamForm.Visible = memoryCheckbox.Checked;
                mInterruptButtonForm.Visible = interruptButtonCheckbox.Checked;
                mSerialConfigForm1.Visible = serialConfig1Checkbox.Checked;
                mSerialConfigForm2.Visible = serialConfig2Checkbox.Checked;
                mParallelConfigForm.Visible = parallelConfigCheckbox.Checked;
                mTimerConfigForm.Visible = timerConfigCheckbox.Checked;
            }
        }

        /// <summary>
        /// Update checkboxes to reflect the state of all subforms.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubForm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                EventHandler d = new EventHandler(SubForm_VisibleChanged);
                this.Invoke(d, sender, e);
            }
            else
            {
                serialForm1Checkbox.Checked = mSerialForm1.Visible;
                serialForm2Checkbox.Checked = mSerialForm2.Visible;
                gprCheckbox.Checked = mGpRegisterForm.Visible;
                sprCheckbox.Checked = mSpRegisterForm.Visible;
                memoryCheckbox.Checked = mRamForm.Visible;
                interruptButtonCheckbox.Checked = mInterruptButtonForm.Visible;
                serialConfig1Checkbox.Checked = mSerialConfigForm1.Visible;
                serialConfig2Checkbox.Checked = mSerialConfigForm2.Visible;
                parallelConfigCheckbox.Checked = mParallelConfigForm.Visible;
                timerConfigCheckbox.Checked = mTimerConfigForm.Visible;
            }
        }

        private void RexBoardForm_Load(object sender, EventArgs e)
        {
            //Open default forms
            Checkbox_CheckedChanged(this, null);

            //Start the CPU running.
            mWorker.Start();
        }

        /// <summary>
        /// Clean up all threads before closing the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RexBoardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mWorker.Abort();
            mSerialForm1.KillWorkers();
            mSerialForm2.KillWorkers();
            //Application.Exit();
        }

        /// <summary>
        /// Redraw the REX widget.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RexBoardForm_Paint(object sender, PaintEventArgs e)
        {
            rexWidget1.Invalidate();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Resets the toolstrip to show the version of the program.
        /// </summary>
        private void ResetToolStripStatusLabel()
        {
            string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            version = version.Substring(0, version.Length - 2); // We don't care about the revision number, since we follow semver's major.minor.patch format.
            toolStripStatusLabel1.Text = $"wsim v{version}";
            statusStrip1.BackColor = SystemColors.Control;
        }

        /// <summary>
        /// Opens an about dialog when the version label is clicked, or, if the
        /// board is running slowly, opens a messagebox with some potential
        /// fixes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            if (toolStripStatusLabel1.Text.Contains("WRAMP is running slowly!"))
            {
                MessageBox.Show(
                    "If wsim is running slowly, you're probably in a virtual machine on Windows." + Environment.NewLine +
                    "The best way to get around this is by running Linux! If you're confident, try finding some instructions on how to run a live CD of Ubuntu! Otherwise, keep reading." + Environment.NewLine + Environment.NewLine +
                    "If you're running Windows 10 Home, your computer doesn't support virtualisation (also known as Hyper-V, VT-x, or VT-d). This will almost certainly cause performance issues." + Environment.NewLine +
                    "Try asking your lecturer or tutor if they know how to obtain a license key for Windows 10 Professional from Microsoft Azure Education." + Environment.NewLine + Environment.NewLine +
                    "If you're already using Windows 10 Professional, and virtualisation is turned on, you probably need to increase the number of CPU cores allocated to your VM. 2 is a good minimum, but 4 should be comfortable. Make sure you also give it a decent amount of RAM!" + Environment.NewLine + Environment.NewLine +
                    "Another way to get better performance is to run wsim directly under Windows with a copy downloaded from Github. It can be tricky to set up shared folders in way that makes it easy to get the .srec files out from the VM though, so good luck :)",
                    "WRAMP is running slowly!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
            }
            else
            {
                AboutBox aboutBox = new AboutBox();
                aboutBox.Show();
            }
        }

        /// <summary>
        /// Recalculate the simulated CPU clock rate.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updateTimer_Tick(object sender, EventArgs e)
        {
            long ticksSinceLastUpdate = mRexBoard.TickCounter - mLastTickCount;
            TimeSpan timeSinceLastUpdate = DateTime.Now.Subtract(mLastTickCountUpdate);
            mLastTickCount = mRexBoard.TickCounter;
            mLastTickCountUpdate = DateTime.Now;

            double rate = 0.5;
            mLastClockRate = ticksSinceLastUpdate / timeSinceLastUpdate.TotalSeconds;
            mLastClockRateSmoothed = mLastClockRateSmoothed * (1.0 - rate) + mLastClockRate * rate;

            this.Text = string.Format("Basys WRAMP Board Simulator: Clock Rate: {0:0.000} MHz ({1:000}%)", mLastClockRateSmoothed / 1e6, mLastClockRateSmoothed * 100 / TARGET_CLOCK_RATE);

            //Set status message if user mode is enabled
            if ((mRexBoard.CPU.mSpRegisters[RegisterFile.SpRegister.cctrl] & 0x8) == 0)
            {
                statusStrip1.BackColor = Color.Red;
                toolStripStatusLabel1.Text = "WARNING: User Mode Enabled (Alpha Feature)";
            }
            // Set status message if the board is running slowly (<50%)
            // This also enables the messagebox that can give tips on how to
            // get around bad performance in VMs under Windows hosts.
            else if (mRunning && (mLastClockRateSmoothed * 100 / TARGET_CLOCK_RATE) < 50)
            {
                statusStrip1.BackColor = Color.Yellow;
                toolStripStatusLabel1.Text = "WARNING: WRAMP is running slowly! Click me for help!";
            }
            else
            {
                ResetToolStripStatusLabel();
            }
            rexWidget1.Invalidate();
        }

        /// <summary>
        /// Set the CPU running, or halted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runButton_Click(object sender, EventArgs e)
        {
            mStepping = false;
            mRunning ^= true;
            mRunOverBreakpoint = true;
            ((Button)sender).Text = mRunning ? "Stop" : "Run";
            ((Button)sender).BackColor = mRunning ? Color.Green : Color.Red;
        }

        /// <summary>
        /// Single-step the WRAMP program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stepButton_Click(object sender, EventArgs e)
        {
            if (mRunning)
                runButton.PerformClick();
            mStepping = true;
            mRunning = true;
        }

        /// <summary>
        /// Toggle CPU throttling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFullSpeed_CheckedChanged(object sender, EventArgs e)
        {
            mThrottleCpu = !((CheckBox)sender).Checked;
        }
        #endregion
    }
}
