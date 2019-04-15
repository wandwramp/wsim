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
using System.Linq;
using System.Text;
using RexSimulator.Hardware.Wramp;
using System.Diagnostics;

namespace RexSimulator.Hardware
{
    /// <summary>
    /// Simulates a WRAMP CPU.
    /// </summary>
    public class SimpleWrampCpu
    {
        #region WRAMP Interface
        private Bus mAddressBus, mDataBus, mIrqs;
        private int mTicksToNextInstruction = -1;
        #endregion

        #region Internal Logic
        public readonly RegisterFile mGpRegisters, mSpRegisters;
        private ALU mAlu;
        private uint mPC;
        private IR mIR;
        private uint mInterruptStatus = 0;
        private MPU mMPU;
        #endregion

        #region Accessors
        /// <summary>
        /// The position of the program counter.
        /// </summary>
        public uint PC
        {
            get { return mPC; }
            set { mPC = value & 0xFFFFF; }
        }

        /// <summary>
        /// The interrupt status register. Do not use unless you know what you're doing!
        /// </summary>
        public uint InterruptStatus
        {
            get { return mInterruptStatus; }
            set { mInterruptStatus = value; }
        }

        /// <summary>
        /// The instruction register of the CPU.
        /// </summary>
        public IR IR
        {
            get { return mIR; }
        }
        #endregion

        #region Exception Sources
        /// <summary>
        /// The source of an exception
        /// </summary>
        private enum ExceptionSource { GPF = 0x1000, SYSCALL = 0x2000, BREAK = 0x4000, ARITH = 0x8000 }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new WRAMP CPU.
        /// </summary>
        /// <param name="addressBus">The address bus.</param>
        /// <param name="dataBus">The data bus.</param>
        /// <param name="irqs">The interrupt request lines (IRQs).</param>
        /// <param name="cs">The chip-select lines.</param>
        public SimpleWrampCpu(Bus addressBus, Bus dataBus, Bus irqs)
        {
            mAddressBus = addressBus;
            mDataBus = dataBus;
            mIrqs = irqs;

            mGpRegisters = new RegisterFile("General Purpose Registers");
            mSpRegisters = new RegisterFile("Special Purpose Registers");

            mAlu = new ALU();

            mIR = new IR();

            mMPU = new MPU(mSpRegisters, mAddressBus, mDataBus);
        }
        #endregion

        #region Control Logic
        /// <summary>
        /// Executes the current instruction.
        /// </summary>
        private void ExecuteInstruction()
        {
            mAddressBus.IsWrite = false;
            mSpRegisters[RegisterFile.SpRegister.icount]++;

            try
            {
                switch (mIR.OpCode)
                {
                    case IR.Opcode.arith_r:
                    case IR.Opcode.test_r_special:
                        if (mIR.OpCode == IR.Opcode.test_r_special &&
                            (mIR.Func == IR.Function.movgs || mIR.Func == IR.Function.movsg || mIR.Func == IR.Function.lhi))
                        {
                            switch (mIR.Func)
                            {
                                case IR.Function.movgs: //break
                                    mInterruptStatus |= (uint)ExceptionSource.BREAK;
                                    break;

                                case IR.Function.movsg: //syscall
                                    mInterruptStatus |= (uint)ExceptionSource.SYSCALL;
                                    break;

                                case IR.Function.lhi: //rfe
                                    ProcessRfe();
                                    break;
                            }
                        }
                        else
                        {
                            mAlu.Func = mIR.Func;
                            mAlu.Rs = mGpRegisters[(RegisterFile.GpRegister)mIR.Rs];
                            mAlu.Rt = mGpRegisters[(RegisterFile.GpRegister)mIR.Rt];
                            mGpRegisters[(RegisterFile.GpRegister)mIR.Rd] = mAlu.Result;
                        }
                        break;

                    case IR.Opcode.arith_i:
                    case IR.Opcode.test_i_special:
                        if (mIR.OpCode == IR.Opcode.test_i_special && (mIR.Func == IR.Function.movgs || mIR.Func == IR.Function.movsg))
                        {
                            //Make sure we are in kernel mode!
                            if ((mSpRegisters[RegisterFile.SpRegister.cctrl] & 0x00000008) == 0)
                            {
                                mInterruptStatus |= (uint)ExceptionSource.GPF;
                            }
                            else
                            {
                                switch (mIR.Func)
                                {
                                    case IR.Function.movgs:
                                        mSpRegisters[(RegisterFile.SpRegister)mIR.Rd] = mGpRegisters[(RegisterFile.GpRegister)mIR.Rs];
                                        break;

                                    case IR.Function.movsg:
                                        mGpRegisters[(RegisterFile.GpRegister)mIR.Rd] = mSpRegisters[(RegisterFile.SpRegister)mIR.Rs];
                                        break;
                                }
                            }
                        }
                        else
                        {
                            mAlu.Func = mIR.Func;
                            mAlu.Rs = mGpRegisters[(RegisterFile.GpRegister)mIR.Rs];

                            mAlu.Rt = mIR.Immed16;
                            if ((((uint)mIR.Func) & 1) == 0) //if this instruction is signed, sign-extend it
                            {
                                if ((mAlu.Rt & 0x8000) != 0)
                                    mAlu.Rt |= 0xFFFF0000;
                            }

                            mGpRegisters[(RegisterFile.GpRegister)mIR.Rd] = mAlu.Result;
                        }
                        break;

                    case IR.Opcode.j:
                        PC = mIR.Immed20;
                        break;

                    case IR.Opcode.jr:
                        PC = mGpRegisters[(RegisterFile.GpRegister)mIR.Rs];
                        break;

                    case IR.Opcode.jal:
                        mGpRegisters[RegisterFile.GpRegister.ra] = PC;
                        goto case IR.Opcode.j; //OK, I used a goto. Shoot me.

                    case IR.Opcode.jalr:
                        mGpRegisters[RegisterFile.GpRegister.ra] = PC;
                        goto case IR.Opcode.jr; //OK, I used a goto. Shoot me.

                    case IR.Opcode.lw:
                        mMPU.Write((uint)(mGpRegisters[(RegisterFile.GpRegister)mIR.Rs] + mIR.SignedImmed20) & 0xfffff);
                        mGpRegisters[(RegisterFile.GpRegister)mIR.Rd] = mDataBus.Value;
                        break;

                    case IR.Opcode.sw:
                        mMPU.Write((uint)(mGpRegisters[(RegisterFile.GpRegister)mIR.Rs] + mIR.SignedImmed20) & 0xfffff);
                        mAddressBus.IsWrite = true;
                        mDataBus.Write(mGpRegisters[(RegisterFile.GpRegister)mIR.Rd]);
                        break;

                    case IR.Opcode.beqz:
                        if (mGpRegisters[(RegisterFile.GpRegister)mIR.Rs] == 0)
                            PC = (uint)(PC + mIR.SignedImmed20);
                        break;

                    case IR.Opcode.bnez:
                        if (mGpRegisters[(RegisterFile.GpRegister)mIR.Rs] != 0)
                            PC = (uint)(PC + mIR.SignedImmed20);
                        break;

                    case IR.Opcode.la:
                        mGpRegisters[(RegisterFile.GpRegister)mIR.Rd] = mIR.Immed20;
                        break;

                    default:
                        //Unknown op-code, so throw up a GPF
                        mInterruptStatus |= (uint)ExceptionSource.GPF;
                        break;
                }
            }
            catch (DivideByZeroException)
            {
                mInterruptStatus |= (uint)ExceptionSource.ARITH;
            }
            catch (OverflowException)
            {
                mInterruptStatus |= (uint)ExceptionSource.ARITH;
            }
            catch (AccessViolationException)
            {
                // Thrown by lw or sw if we are in user mode and code attempts to
                // access memory restricted by $ptable.
                mInterruptStatus |= (uint)ExceptionSource.GPF;
            }
        }

        /// <summary>
        /// Processes any outstanding interrupts.
        /// </summary>
        private void ProcessExceptions()
        {
            //Check IRQ lines
            /*
             * IRQ0 -
             * IRQ1 Interrupt Button
             * IRQ2 Timer
             * IRQ3 Parallel
             * IRQ4 Serial 1
             * IRQ5 Serial 2
             * IRQ6 -
             * IRQ7 -
             */
            mInterruptStatus &= 0x0000F000;
            mInterruptStatus |= ((mIrqs.Value << 4) & 0x00000FF0);

            if (mInterruptStatus != 0)
            {
                uint cctrl = mSpRegisters[RegisterFile.SpRegister.cctrl];

                uint mask = cctrl | 0x0000F000;
                if ((mask & 2) == 0) //if interrupts are disabled, mask out.
                    mask &= 0xFFFFF00F;

                //Check if an exception needs processing
                if ((mInterruptStatus & mask) != 0)
                {
                    //Enable kernel mode, disable interrupts, backup ie & ku
                    uint ieku = (cctrl & 0xA) >> 1;
                    uint cctrlt = (cctrl & 0xFFFFFFF8) | 0x8;
                    mSpRegisters[RegisterFile.SpRegister.cctrl] = cctrlt | ieku;

                    //back up necessary registers
                    mSpRegisters[RegisterFile.SpRegister.ear] = PC;
                    mSpRegisters[RegisterFile.SpRegister.ers] = mGpRegisters[RegisterFile.GpRegister.r13];

                    //Jump to interrupt handler
                    PC = mSpRegisters[RegisterFile.SpRegister.evec];

                    //Copy status
                    mSpRegisters[RegisterFile.SpRegister.estat] = mInterruptStatus & mask;
                }
                mInterruptStatus = 0; //finished with this!
            }
        }

        /// <summary>
        /// Returns from an exception.
        /// </summary>
        private void ProcessRfe()
        {
            uint oieku = (mSpRegisters[RegisterFile.SpRegister.cctrl] & 0x5) << 1;
            uint cctrlt = (mSpRegisters[RegisterFile.SpRegister.cctrl] & 0xFFFFFFF0) | oieku;
            mSpRegisters[RegisterFile.SpRegister.cctrl] = cctrlt;
            mGpRegisters[RegisterFile.GpRegister.r13] = mSpRegisters[RegisterFile.SpRegister.ers];
            PC = mSpRegisters[RegisterFile.SpRegister.ear];
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Resets the CPU into a known state.
        /// </summary>
        public void Reset()
        {
            //CPU Reset logic
            mGpRegisters.Reset(0xffffffff);
            mSpRegisters.Reset(0x00000000);

            mSpRegisters[RegisterFile.SpRegister.cctrl] = 0x00000008;

            //PC = 0; //Run user program, assuming its entrypoint is the default of 0.
            PC = 0x00080000; //Run monitor

            mInterruptStatus = 0;
        }

        /// <summary>
        /// Performs a single tick.
        /// Most instructions require 4 ticks, but some require 70+.
        /// </summary>
        /// <returns>True if this tick resulted in a completely executed instruction.</returns>
        public bool Tick()
        {
            mSpRegisters[RegisterFile.SpRegister.ccount]++;
            if (mTicksToNextInstruction-- <= 0)
            {
                mAddressBus.IsWrite = false;

                //Check interrupts
                ProcessExceptions();

                //Fetch next instruction & increment $PC
                try
                {
                    mMPU.Write(PC++);
                }
                catch (AccessViolationException)
                {
                    // If we are in user mode and an instruction within protected memory
                    // is fetched, we should immediately throw a GPF.
                    // This takes us out of user mode, so mMPU.Write(PC); will not throw another exception.
                    mInterruptStatus |= (uint)ExceptionSource.GPF;
                    ProcessExceptions();
                    mMPU.Write(PC);
                }
                mIR.Instruction = mDataBus.Value;

                mTicksToNextInstruction = mIR.TicksRequired;
                if (mIR.OpCode == IR.Opcode.bnez && mGpRegisters[(RegisterFile.GpRegister)mIR.Rs] != 0 ||
                    mIR.OpCode == IR.Opcode.beqz && mGpRegisters[(RegisterFile.GpRegister)mIR.Rs] == 0)
                {
                    mTicksToNextInstruction++;
                }

                //Decode & Execute
                ExecuteInstruction();
                return true;
            }
            return false;
        }
        #endregion
    }
}
