using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
        private enum ActiveControl { None, Reset, Interrupt, Switch0, Switch1, Switch2, Switch3, Switch4, Switch5, Switch6, Switch7, Button0, Button1, Duck };
        #endregion

        #region Members
        public readonly RexBoard mBoard;

        private int mButtonRad = 13;
        private Size mSwitchBorderSize = new Size(15, 40);
        private Size mSwitchSize = new Size(10, 10);
        private int mDuckRad = 40;

        private Point mResetLoc = new Point(74, 509);
        private Point mInterruptLoc = new Point(165, 512);
        private Point[] mButtonLoc = new Point[] { new Point(740, 547), new Point(707, 548) };
        private Point[] mSwitchLoc = new Point[] { new Point(727, 489), new Point(695, 489), new Point(665, 489), new Point(633, 489), new Point(603, 489), new Point(572, 489), new Point(540, 489), new Point(510, 489) };
        private Point mDuckLoc = new Point(351, 209);

        
        private ActiveControl mActiveControl = ActiveControl.None;
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
        private ActiveControl GetActiveControl(Point mouseLoc)
        {
            mouseLoc.X = mouseLoc.X * Resources.RexBoardPhoto.Width / this.Width;
            mouseLoc.Y = mouseLoc.Y * Resources.RexBoardPhoto.Height/ this.Height;

            if (Distance(mouseLoc, mResetLoc) < mButtonRad)
                return ActiveControl.Reset;

            if (Distance(mouseLoc, mInterruptLoc) < mButtonRad)
                return ActiveControl.Interrupt;

            if (Distance(mouseLoc, mDuckLoc) < mDuckRad)
                return ActiveControl.Duck;

            for (int i = 0; i < mButtonLoc.Length; i++)
            {
                if (Distance(mouseLoc, mButtonLoc[i]) < mButtonRad)
                    return ActiveControl.Button0 + i;
            }

            for (int i = 0; i < mSwitchLoc.Length; i++)
            {
                if (Distance(mouseLoc, mSwitchLoc[i]) < mButtonRad)
                    return ActiveControl.Switch0 + i;
            }

            return ActiveControl.None;
            
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
            DrawSSD(g, mBoard.Parallel.LeftSSDOut, 578, 320, 25, 30);
            DrawSSD(g, mBoard.Parallel.RightSSDOut, 613, 320, 25, 30);
        }

        /// <summary>
        /// Draws the value of the data and address busses.
        /// </summary>
        /// <param name="g">The graphics to draw on.</param>
        private void DrawBusses(Graphics g)
        {
            g.DrawString(string.Format("0x{0:X5}", mBoard.mAddressBus.Value), new Font(FontFamily.GenericMonospace, 15, FontStyle.Bold), Brushes.Red, 465, 240);
            g.DrawString(string.Format("0x{0:X8}", mBoard.mDataBus.Value), new Font(FontFamily.GenericMonospace, 15, FontStyle.Bold), Brushes.Red, 465, 195);

            g.DrawString(string.Format("0x{0:X5}", mBoard.CPU.PC), new Font(FontFamily.GenericMonospace, 15, FontStyle.Bold), Brushes.Red, 130, 241);
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

        /// <summary>
        /// Draws IRQ lights on the board.
        /// </summary>
        /// <param name="g"></param>
        private void DrawIRQs(Graphics g)
        {
            DrawLed(g, mBoard.mIrqs.GetBit(1), 225, 470); //Button
            DrawLed(g, mBoard.mIrqs.GetBit(2), 443, 355); //Timer
            DrawLed(g, mBoard.mIrqs.GetBit(3), 590, 295); //Parallel
            DrawLed(g, mBoard.mIrqs.GetBit(4), 648, 27); //Serial 1
            DrawLed(g, mBoard.mIrqs.GetBit(5), 648, 95); //Serial 2

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
        /// Draws the reset, interrupt and parallel port buttons
        /// </summary>
        /// <param name="g"></param>
        private void DrawButtons(Graphics g)
        {
            //Reset
            Brush b = (mActiveControl == ActiveControl.Reset) ? Brushes.Red : Brushes.DarkRed;
            DrawButton(g, mResetLoc, b);

            //Interrupt
            b = (mActiveControl == ActiveControl.Interrupt) ? Brushes.DarkGray : Brushes.Gray;
            DrawButton(g, mInterruptLoc, b);

            //Parallel Buttons
            for(int i=0; i < mButtonLoc.Length; i++)
            {
                b = (mActiveControl == (ActiveControl)(ActiveControl.Button0 + i)) ? Brushes.DarkGray : Brushes.Gray;
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
            int y = on ? mSwitchSize.Height : -mSwitchSize.Height;
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
                Brush b = (mActiveControl == (ActiveControl)(ActiveControl.Switch0 + i)) ? Brushes.LightGray : Brushes.White;
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
            mActiveControl = GetActiveControl(e.Location);
        }

        private void RexWidget_Click(object sender, EventArgs e)
        {
            switch (mActiveControl)
            {
                case ActiveControl.Reset:
                    uint switchBk = mBoard.Parallel.Switches;
                    mBoard.Reset();
                    mBoard.Parallel.Switches = switchBk;
                    break;

                case ActiveControl.Interrupt:
                    mBoard.InterruptButton.PressButton();
                    break;

                case ActiveControl.Duck:
                    SoundPlayer sp = new SoundPlayer(Resources.duck_quack);
                    sp.Play();
                    break;
            }

            for (int i = 0; i < mSwitchLoc.Length; i++)
            {
                if (mActiveControl == ActiveControl.Switch0 + i)
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
            switch (mActiveControl)
            {
                case ActiveControl.Button0:
                    mBoard.Parallel.Buttons |= 1;
                    break;

                case ActiveControl.Button1:
                    mBoard.Parallel.Buttons |= 2;
                    break;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Executes a single instruction.
        /// </summary>
        public void Step()
        {
            while (!mBoard.Tick()) ;
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
