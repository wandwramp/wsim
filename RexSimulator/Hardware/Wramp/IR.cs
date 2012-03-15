using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RexSimulator.Hardware.Wramp
{
    /// <summary>
    /// The WRAMP instruction register.
    /// </summary>
    public class IR
    {
        #region Enums
        /// <summary>
        /// Op-codes supported by WRAMP.
        /// </summary>
        public enum Opcode { arith_r, arith_i, test_r_special, test_i_special, j, jr, jal, jalr, lw, sw, beqz, bnez, la, undefD, undefE, undefF };

        /// <summary>
        /// Functions supported by WRAMP's ALU.
        /// </summary>
        public enum Function
        {
            add, addu, sub, subu, mult, multu, div, divu, rem, remu, sll, and, srl, or, sra, xor,
            slt, sltu, sgt, sgtu, sle, sleu, sge, sgeu, seq, sequ, sne, sneu, movgs, movsg, lhi, inc
        }
        #endregion

        #region Member Vars
        private uint mInstruction;
        #endregion

        /// <summary>
        /// Gets or sets the instruction that the decoder is working on.
        /// </summary>
        public uint Instruction { get { return mInstruction; } set { mInstruction = value; } }

        #region Decoded Fields
        /// <summary>
        /// The op-code. All instruction types.
        /// </summary>
        public Opcode OpCode { get { return (Opcode)((mInstruction >> 28) & 0xF); } }

        /// <summary>
        /// The destination register. All instruction types.
        /// </summary>
        public uint Rd { get { return (mInstruction >> 24) & 0xF; } }

        /// <summary>
        /// The first source register. All instruction types.
        /// </summary>
        public uint Rs { get { return (mInstruction >> 20) & 0xF; } }

        /// <summary>
        /// The second source register. R-type only.
        /// </summary>
        public uint Rt { get { return (mInstruction >> 0) & 0xF; } }

        /// <summary>
        /// The function field. I and R-type only.
        /// </summary>
        public Function Func { get { return (Function)(((mInstruction >> 16) & 0xF) | (((uint)OpCode) & 0x2) << 3); } }

        /// <summary>
        /// The 16-bit immediate value. I-type only.
        /// </summary>
        public uint Immed16 { get { return (mInstruction >> 0) & 0xFFFF; } }

        /// <summary>
        /// The 20-bit immediate value. J-type only.
        /// </summary>
        public uint Immed20 { get { return (mInstruction >> 0) & 0xFFFFF; } }

        /// <summary>
        /// The sign-extended 16-bit immediate value. I-type only.
        /// </summary>
        public int SignedImmed16
        {
            get
            {
                if ((mInstruction & 0x8000) != 0)
                    return (int)(mInstruction | 0xFFFF0000);
                return (int)(mInstruction & 0xFFFF);
            }
        }

        /// <summary>
        /// The sign-extended 20-bit immediate value. J-type only.
        /// </summary>
        public int SignedImmed20
        {
            get
            {
                if ((mInstruction & 0x80000) != 0)
                    return (int)(mInstruction | 0xFFF00000);
                return (int)(mInstruction & 0xFFFFF);
            }
        }

        /// <summary>
        /// Gets the number of ticks required to execute this instruction.
        /// </summary>
        public int TicksRequired
        {
            get
            {
                switch (this.OpCode)
                {
                    case Opcode.arith_i:
                    case Opcode.arith_r:
                    case Opcode.test_i_special:
                    case Opcode.test_r_special:
                        switch (Func)
                        {
                            case Function.add:
                            case Function.addu:
                            case Function.sub:
                            case Function.subu:

                            case Function.and:
                            case Function.or:
                            case Function.xor:

                            case Function.sll:
                            case Function.srl:
                            case Function.sra:

                            case Function.slt:
                            case Function.sltu:
                            case Function.sgt:
                            case Function.sgtu:
                            case Function.sle:
                            case Function.sleu:
                            case Function.sge:
                            case Function.sgeu:
                            case Function.seq:
                            case Function.sequ:
                            case Function.sne:
                            case Function.sneu:

                            case Function.movgs:
                            case Function.movsg:

                            case Function.lhi:
                                return 4;

                            case Function.mult:
                                return 72;

                            case Function.multu:
                                return 70;

                            case Function.div:
                            case Function.rem:
                                return 74;

                            case Function.divu:
                            case Function.remu:
                                return 71;

                            case Function.inc:

                            default:
                                throw new InvalidOperationException("Unknown Instruction!");
                        }
                    case Opcode.la:
                    case Opcode.j:
                    case Opcode.jr:
                    case Opcode.beqz: //Note: 1 additional tick if the branch is taken
                    case Opcode.bnez: //Note: 1 additional tick if the branch is taken
                        return 4;

                    case Opcode.lw:
                    case Opcode.sw:
                    case Opcode.jal:
                    case Opcode.jalr:
                        return 5;

                    default:
                        return 4; //Note: an unknown instruction will cause a GPF.
                }
            }
        }
        #endregion

        /// <summary>
        /// Gets a human-readable disassembly of this instruction.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            switch (this.OpCode)
            {
                case Opcode.arith_r:
                case Opcode.test_r_special:
                    if (OpCode == Opcode.test_r_special)
                    {
                        switch (Func)
                        {
                            case Function.movgs: //break
                                return "break";
                            case Function.movsg: //syscall
                                return "syscall";
                            case Function.lhi: //rfe
                                return "rfe";
                        }
                    }
                    return string.Format("{0} ${1}, ${2}, ${3}", Func, Rd, Rs, Rt);

                case Opcode.arith_i:
                case Opcode.test_i_special:
                    if (OpCode == Opcode.test_i_special)
                    {
                        switch (Func)
                        {
                            case Function.movgs:
                                return string.Format("{0} ${1}, ${2}", Func, (RegisterFile.SpRegister)Rd, Rs);
                            case Function.movsg:
                                return string.Format("{0} ${1}, ${2}", Func, Rd, (RegisterFile.SpRegister)Rs);
                        }
                    }
                    return string.Format("{0}i ${1}, ${2}, 0x{3:X4}", Func, Rd, Rs, Immed16);

                case Opcode.la:
                    return string.Format("{0} ${1}, 0x{2:X5}", OpCode, Rd, Immed20);

                case Opcode.j:
                case Opcode.jal:
                    return string.Format("{0} 0x{1:X5}", OpCode, Immed20);

                case Opcode.jr:
                case Opcode.jalr:
                    return string.Format("{0} ${1}", OpCode, Rs);

                case Opcode.lw:
                case Opcode.sw:
                    return string.Format("{0} ${1}, 0x{2:X5}(${3})", OpCode, Rd, Immed20, Rs);

                case Opcode.beqz:
                case Opcode.bnez:
                    if(SignedImmed20 >= 0)
                        return string.Format("{0} ${1}, +0x{2:X5}", OpCode, Rs, SignedImmed20);
                    return string.Format("{0} ${1}, -0x{2:X5}", OpCode, Rs, -SignedImmed20);
            }
            return "?";
        }
    }
}
