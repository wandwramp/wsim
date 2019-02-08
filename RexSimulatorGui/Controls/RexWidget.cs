using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RexSimulator.Hardware;
using System.IO;
using RexSimulatorGui.Properties;
using System.Diagnostics;
using System.Media;

namespace RexSimulatorGui.Controls
{
    /// <summary>
    /// A widget displaying the REX board, almost complete. To execute instructions, Tick() must be called continuously for the board to do anything useful.
    /// </summary>
    public partial class RexWidget : UserControl
    {
        #region Enums
        /// <summary>
        /// The control that currently has mouse focus.
        /// </summary>
        [Flags]
        private enum ControlWithFocus { 
            None = 0, Reset = 1, SoftReset = 1 << 1, Interrupt = 1 << 2, Duck = 1 << 3,
            
            Switch0 = 1 << 4, Switch1 = 1 << 5, Switch2 = 1 << 6, Switch3 = 1 << 7,
            Switch4 = 1 << 8, Switch5 = 1 << 9, Switch6 = 1 << 10, Switch7 = 1 << 11,
            Switch8 = 1 << 12, Switch9 = 1 << 13, Switch10 = 1 << 14, Switch11 = 1 << 15,
            Switch12 = 1 << 16, Switch13 = 1 << 17, Switch14 = 1 << 18, Switch15 = 1 << 19,
            
            ButtonR = 1 << 20, ButtonC = 1 << 21, ButtonL = 1 << 22
            };
        #endregion

        #region Members
        public readonly RexBoard mBoard;

        private int mButtonRad = 13;
        private Size mSwitchBorderSize = new Size(20, 60);
        private Size mSwitchSize = new Size(13, 13);
        private int mDuckRad = 40;

        private Point mResetLoc = new Point(787, 63);
        private Point mSoftResetLoc = new Point (671, 252);
        private Point mInterruptLoc = new Point(671, 345);
        private Point[] mButtonLoc = new Point[] { 
            new Point(726, 299),	//right
            new Point(671, 299), 	//center
            new Point(614, 299),	//left
        };
        private Point[] mSwitchLoc = new Point[] { 
            new Point(815, 443),    //0
            new Point(768, 443),
            new Point(723, 443),
            new Point(680, 443),
            new Point(637, 443),
            new Point(590, 443),
            new Point(543, 443),
            new Point(498, 443),
            new Point(450, 443),
            new Point(405, 443),
            new Point(360, 443),
            new Point(313, 443),
            new Point(266, 443),
            new Point(221, 443),
            new Point(177, 443),
            new Point(132, 443)    //15
        };
        private Point[] mLedLoc = new Point[] {
            new Point(788, 371),   //0
            new Point(747, 371),
            new Point(704, 371),
            new Point(661, 371),
            new Point(618, 371),
            new Point(575, 371),
            new Point(531, 371),
            new Point(488, 371),
            new Point(446, 371),
            new Point(402, 371),
            new Point(358, 371),
            new Point(314, 371),
            new Point(270, 371),
            new Point(227, 371),
            new Point(185, 371),
            new Point(142, 371),   //15
        };
        private Point mDuckLoc = new Point(470,208);

        
        private ControlWithFocus mActiveControl = ControlWithFocus.None;
        #endregion

        public RexWidget()
        {
            InitializeComponent();

            mBoard = new RexBoard();
        }

        #region Private Methods
        /// <summary>
        /// Gets the distance between two points.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        /// <summary>
        /// Gets the control currently under the mouse pointer.
        /// </summary>
        /// <returns></returns>
        private ControlWithFocus GetActiveControl(Point mouseLoc)
        {
            mouseLoc.X = mouseLoc.X * Resources.RexBoardPhoto.Width / this.Width;
            mouseLoc.Y = mouseLoc.Y * Resources.RexBoardPhoto.Height/ this.Height;

            //Reset Button
            if (Distance(mouseLoc, mResetLoc) < mButtonRad)
                return ControlWithFocus.Reset;
            //Soft Reset Button
            if (Distance(mouseLoc, mSoftResetLoc) < mButtonRad)
                return ControlWithFocus.SoftReset;

            //User Interrupt Button
            if (Distance(mouseLoc, mInterruptLoc) < mButtonRad)
                return ControlWithFocus.Interrupt;

            //Quacker
            if (Distance(mouseLoc, mDuckLoc) < mDuckRad)
                return ControlWithFocus.Duck;

            //Push Buttons
            for (int i = 0; i < mButtonLoc.Length; i++)
            {
                if (Distance(mouseLoc, mButtonLoc[i]) < mButtonRad)
                    return (ControlWithFocus)((int)(ControlWithFocus.ButtonR) << i);
            }

            // Sets of two buttons.
            // This array contains information about overlaps between adjacent buttons.
            // The first two elements are the indices of the buttons.
            // The last element is an approximation of the distance between the buttons.
            int[][] overlaps = {
                new int[]{0, 1, 5}, // Right and middle
                new int[]{1, 2, 5}, // Middle and left
            };

            foreach (int[] overlap in overlaps)
            {
                int i = overlap[0];
                int j = overlap[1];
                int multiplier = overlap[2];
                
                if (Distance(mouseLoc, mButtonLoc[i]) + Distance(mouseLoc, mButtonLoc[j]) < multiplier * mButtonRad)
                {
                    return (ControlWithFocus)(
                        ((int)(ControlWithFocus.ButtonR) << i) |
                        ((int)(ControlWithFocus.ButtonR) << j));
                }
            }

            //Switches
            for (int i = 0; i < mSwitchLoc.Length; i++)
            {
                if (Distance(mouseLoc, mSwitchLoc[i]) < mButtonRad)
                {
                    return (ControlWithFocus)((int)(ControlWithFocus.Switch0) << i);
                }
            }

            return ControlWithFocus.None;
            
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draws an SSD.
        /// </summary>
        /// <param name="g">The graphics to draw on.</param>
        /// <param name="value">The value to draw. See WRAMP manual for details.</param>
        /// <param name="xOffset">The x co-ordinate to draw at.</param>
        /// <param name="yOffset">The y co-ordinate to draw at.</param>
        /// <param name="width">The width to draw.</param>
        /// <param name="height">The height to draw.</param>
        private void DrawSSD(Graphics g, uint value, int xOffset, int yOffset, int width, int height)
        {
            Pen ssdPen = new Pen(Brushes.Red, 4);

            int left = 5 + xOffset;
            int right = width + xOffset;
            int top = 10 + yOffset;
            int mid = 10 + height / 2 + yOffset;
            int bottom = 10 + height + yOffset;

            Point[] points = new Point[] { //from top left to bottom right
                new Point(left, top),
                new Point(right, top),
                new Point(left, mid),
                new Point(right, mid),
                new Point(left, bottom),
                new Point(right, bottom)
            };

            if ((value & 0x01) != 0) g.DrawLine(ssdPen, points[0], points[1]);
            if ((value & 0x02) != 0) g.DrawLine(ssdPen, points[1], points[3]);
            if ((value & 0x04) != 0) g.DrawLine(ssdPen, points[3], points[5]);
            if ((value & 0x08) != 0) g.DrawLine(ssdPen, points[4], points[5]);
            if ((value & 0x10) != 0) g.DrawLine(ssdPen, points[2], points[4]);
            if ((value & 0x20) != 0) g.DrawLine(ssdPen, points[0], points[2]);
            if ((value & 0x40) != 0) g.DrawLine(ssdPen, points[2], points[3]);
            if ((value & 0x80) != 0) g.DrawEllipse(ssdPen, points[5].X, points[5].Y, 1, 1);
        }

        /// <summary>
        /// Draws the parallel IO device.
        /// </summary>
        /// <param name="g">The graphics to draw on.</param>
        private void DrawSSDs(Graphics g)
        {
            g.FillRectangle(Brushes.Black, 180, 290, 210, 65);
            DrawSSD(g, mBoard.Parallel.LeftLeftSSDOut, 185, 290, 30, 45);
            DrawSSD(g, mBoard.Parallel.LeftRightSSDOut, 240, 290, 30, 45);
            DrawSSD(g, mBoard.Parallel.LeftSSDOut, 295, 290, 30, 45);
            DrawSSD(g, mBoard.Parallel.RightSSDOut, 350, 290, 30, 45);
        }

        /// <summary>
        /// Draws the value of the data and address busses.
        /// </summary>
        /// <param name="g">The graphics to draw on.</param>
        private void DrawBusses(Graphics g)
        {
            g.DrawString(
                string.Format(
                    "Address Bus: 0x{0:X5}    Data Bus: 0x{1:X8}    Program Counter: 0x{2:X5}",
                    mBoard.mAddressBus.Value,
                    mBoard.mDataBus.Value,
                    mBoard.CPU.PC),
                new Font(FontFamily.GenericMonospace, 12, FontStyle.Bold),
                Brushes.Black,
                5, 525);
        }

        /// <summary>
        /// Draws an LED on the board.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="on"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void DrawLed(Graphics g, bool on, int x, int y)
        {
            Brush b = on ? Brushes.Red : Brushes.Black;
            g.FillEllipse(b, x, y, 15, 15);
        }

        private void DrawIRQ(Graphics g, string name, bool on, int x, int y)
        {
            g.DrawString(
                name,
                new Font(FontFamily.GenericMonospace,
                    12,
                    FontStyle.Bold),
                on ? Brushes.Black : Brushes.Silver,
                x, y);
        }

        /// <summary>
        /// Draws IRQ lights on the board.
        /// </summary>
        /// <param name="g"></param>
        private void DrawIRQs(Graphics g)
        {
            // The width of the text is around 10px/character, so
            // these magic numbers give approximately 40px of space
            // between each label.
            DrawIRQ(g, "Interrupts:", mBoard.mIrqs.Value != 0, 5, 550);
            
            DrawIRQ(g, "Button",   mBoard.mIrqs.GetBit(1) && (mBoard.CPU.mSpRegisters[RexSimulator.Hardware.Wramp.RegisterFile.SpRegister.cctrl] & (1 << 5)) != 0, 150, 550);
            DrawIRQ(g, "Timer",    mBoard.mIrqs.GetBit(2) && (mBoard.CPU.mSpRegisters[RexSimulator.Hardware.Wramp.RegisterFile.SpRegister.cctrl] & (1 << 6)) != 0, 250, 550);
            DrawIRQ(g, "Parallel", mBoard.mIrqs.GetBit(3) && (mBoard.CPU.mSpRegisters[RexSimulator.Hardware.Wramp.RegisterFile.SpRegister.cctrl] & (1 << 7)) != 0, 340, 550);
            DrawIRQ(g, "Serial 1", mBoard.mIrqs.GetBit(4) && (mBoard.CPU.mSpRegisters[RexSimulator.Hardware.Wramp.RegisterFile.SpRegister.cctrl] & (1 << 8)) != 0, 460, 550);
            DrawIRQ(g, "Serial 2", mBoard.mIrqs.GetBit(5) && (mBoard.CPU.mSpRegisters[RexSimulator.Hardware.Wramp.RegisterFile.SpRegister.cctrl] & (1 << 9)) != 0, 580, 550);
        }

        private void DrawLeds(Graphics g)
        {
            for (int i = 0; i < mLedLoc.Length; i++)
            {
                DrawLed(g, (mBoard.Parallel.Leds & (1 << i)) != 0, mLedLoc[i].X, mLedLoc[i].Y);
            }
        }

        /// <summary>
        /// Draws a button.
        /// </summary>
        /// <param name="g">The graphics to draw to.</param>
        /// <param name="p">The centerpoint of the button.</param>
        /// <param name="b">The brush to draw the button with.</param>
        private void DrawButton(Graphics g, Point p, Brush b)
        {
            int r = mButtonRad + 2;
            g.FillEllipse(Brushes.Black, p.X - r, p.Y - r, r * 2, r * 2);
            r -= 2;
            g.FillEllipse(b, p.X - r, p.Y - r, r * 2, r * 2);
        }

        /// <summary>
        /// Draws the buttons
        /// </summary>
        /// <param name="g"></param>
        private void DrawButtons(Graphics g)
        {
            //Reset
            Brush b = (mActiveControl & ControlWithFocus.Reset) != 0 ? Brushes.Red : Brushes.DarkRed;
            DrawButton(g, mResetLoc, b);

            // Soft reset
            b = (mActiveControl & ControlWithFocus.SoftReset) != 0 ? Brushes.Red : Brushes.DarkRed;
            DrawButton(g, mSoftResetLoc, b);

            //Interrupt
            b = (mActiveControl & ControlWithFocus.Interrupt) != 0 ? Brushes.Green : Brushes.DarkGreen;
            DrawButton(g, mInterruptLoc, b);

            //Parallel Buttons
            for (int i = 0; i < mButtonLoc.Length; i++)
            {
                b = (mActiveControl & (ControlWithFocus)((int)ControlWithFocus.ButtonR << i)) != 0 ? Brushes.DarkGray : Brushes.Gray;
                DrawButton(g, mButtonLoc[i], b);
            }
        }

        /// <summary>
        /// Draws a switch.
        /// </summary>
        /// <param name="g">The graphics to draw to.</param>
        /// <param name="p">The centerpoint of the switch.</param>
        /// <param name="b">The brush to draw the switch with.</param>
        /// <param name="switchNo">The switch number it is.</param>
        private void DrawSwitch(Graphics g, Point p, Brush b, int switchNo)
        {
            g.FillRectangle(b, p.X - mSwitchBorderSize.Width / 2, p.Y - mSwitchBorderSize.Height / 2, mSwitchBorderSize.Width, mSwitchBorderSize.Height);
            
            bool on = ((mBoard.Parallel.Switches & (1 << switchNo)) != 0);
            int y = on ? -mSwitchSize.Height : mSwitchSize.Height;
            y += p.Y;
            g.FillRectangle(b, p.X - mSwitchBorderSize.Width / 2, p.Y - mSwitchBorderSize.Height / 2, mSwitchBorderSize.Width, mSwitchBorderSize.Height);

            g.FillRectangle(Brushes.Black, p.X - mSwitchSize.Width / 2, y - mSwitchSize.Height / 2, mSwitchSize.Width, mSwitchSize.Height);
        }

        /// <summary>
        /// Draws all switches of the parallel port.
        /// </summary>
        /// <param name="g"></param>
        private void DrawSwitches(Graphics g)
        {
            for (int i = 0; i < mSwitchLoc.Length; i++)
            {
                ControlWithFocus controlToTest = (ControlWithFocus)((int)(ControlWithFocus.Switch0) << i);
                Brush b = (mActiveControl == controlToTest)	? Brushes.LightGray : Brushes.White;
                DrawSwitch(g, mSwitchLoc[i], b, i);
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Redraws the board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RexWidget_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Bitmap bbuf = (Bitmap)Resources.RexBoardPhoto;
                Graphics g = Graphics.FromImage(bbuf);
                DrawSSDs(g);
                DrawBusses(g);
                DrawIRQs(g);
                DrawLeds(g);
                DrawButtons(g);
                DrawSwitches(g);

                e.Graphics.DrawImage(bbuf, 0, 0, this.Width, this.Height);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Prevents flickering by overriding the default blanking behaviour.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(PaintEventArgs e) { }

        private void RexWidget_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None)
                mActiveControl = GetActiveControl(e.Location);
        }

        private void RexWidget_Click(object sender, EventArgs e)
        {
            switch (mActiveControl)
            {
                case ControlWithFocus.Reset:
                case ControlWithFocus.SoftReset:
                    uint switchBk = mBoard.Parallel.Switches;
                    mBoard.Reset();
                    mBoard.Parallel.Switches = switchBk;
                    break;

                case ControlWithFocus.Interrupt:
                    mBoard.InterruptButton.PressButton();
                    break;

                case ControlWithFocus.Duck:
                    SoundPlayer sp = new SoundPlayer(Resources.duck_quack);
                    sp.Play();
                    break;
            }

            for (int i = 0; i < mSwitchLoc.Length; i++)
            {
                if (mActiveControl == (ControlWithFocus)((int)ControlWithFocus.Switch0 << i))
                {
                    mBoard.Parallel.Switches ^= (1u << i);
                }
            }
        }

        private void RexWidget_MouseUp(object sender, MouseEventArgs e)
        {
            mBoard.Parallel.Buttons = 0; //currently none being pressed.
        }

        private void RexWidget_MouseDown(object sender, MouseEventArgs e)
        {
            mBoard.Parallel.Buttons = (uint)(mActiveControl &
                   (ControlWithFocus.ButtonR |
                    ControlWithFocus.ButtonC |
                    ControlWithFocus.ButtonL)) >> 20;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Executes a single instruction.
        /// </summary>
        public void Step()
        {
            while (!mBoard.Tick());
        }

        /// <summary>
        /// Loads an srec file into the board.
        /// </summary>
        /// <param name="stream"></param>
        public void LoadSrec(Stream stream)
        {
            mBoard.LoadSrec(stream);
        }
        #endregion
    }
}
