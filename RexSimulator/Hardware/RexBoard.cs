using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using RexSimulator.Hardware.Wramp;
using RexSimulator.Hardware.Rex;

namespace RexSimulator.Hardware
{
    /// <summary>
    /// Simulates the Rex board.
    /// </summary>
    public class RexBoard
    {
        #region Debug
        public static bool VERBOSE_OUTPUT = false;
        #endregion

        #region Components
        public readonly Bus mDataBus;
        public readonly Bus mAddressBus;
        public readonly Bus mIrqs;
        public readonly Bus mCs;

        public readonly SimpleWrampCpu CPU;

        public readonly MemoryDevice RAM;
        public readonly SerialIO Serial1;
        public readonly SerialIO Serial2;
        public readonly Timer Timer;
        public readonly ParallelIO Parallel;
        public readonly MemoryDevice ROM;
        public readonly ButtonInterrupt InterruptButton;
        #endregion

        #region Accounting
        private long mTickCounter = 0;
        /// <summary>
        /// The number of ticks elapsed since starting.
        /// </summary>
        public long TickCounter { get { return mTickCounter; } }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates the RexBoard and all components within it.
        /// </summary>
        public RexBoard()
        {
            //Initialise busses
            mDataBus = new Bus();
            mAddressBus = new Bus();
            mIrqs = new Bus();

            //Initialise other components
            CPU = new SimpleWrampCpu(mAddressBus, mDataBus, mIrqs, mCs);

            //Memory and Memory-mapped IO
            RAM = new MemoryDevice(0x00000, 0x20000, mAddressBus, mDataBus, "Memory (RAM)");
            Serial1 = new SerialIO(0x70000, 5, mAddressBus, mDataBus, "Serial Port 1");
            Serial2 = new SerialIO(0x71000, 5, mAddressBus, mDataBus, "Serial Port 2");
            Timer = new Timer(0x72000, 4, mAddressBus, mDataBus, "Timer");
            Parallel = new ParallelIO(0x73000, 6, mAddressBus, mDataBus, "Parallel Port");
            ROM = new MemoryDevice(0x80000, 0x40000, mAddressBus, mDataBus, "Memory (ROM)");
            InterruptButton = new ButtonInterrupt(0x7f000, 1, mAddressBus, mDataBus, "Interrupt Button");

            //IRQs
            InterruptButton.AttachIRQ(mIrqs, 1, 0);
            Timer.AttachIRQ(mIrqs, 2, 3);
            Parallel.AttachIRQ(mIrqs, 3, 5);
            Serial1.AttachIRQ(mIrqs, 4, 4);
            Serial2.AttachIRQ(mIrqs, 5, 4);

            Reset();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs a hard-reset of the board, and internal components.
        /// </summary>
        /// <param name="keepMemoryState">True if you want to preserve the contents of RAM.</param>
        public void Reset(bool keepMemoryState)
        {
            CPU.Reset();

            if(!keepMemoryState)
                RAM.Reset();
            InterruptButton.Reset();
            Serial1.Reset();
            Serial2.Reset();
            Timer.Reset();
            Parallel.Reset();
            //ROM.Reset();
        }

        /// <summary>
        /// Performs a hard-reset of the board, and internal components.
        /// </summary>
        public void Reset()
        {
            Reset(false);
        }

        /// <summary>
        /// Loads an .srec into memory.
        /// Based on description at http://en.wikipedia.org/wiki/SREC_(file_format)
        /// </summary>
        /// <param name="stream">The stream to read the .srec from.</param>
        /// <returns>The number of words loaded.</returns>
        public uint LoadSrec(Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            uint wordsLoaded = 0;

            //Note: All hex values are big endian.

            //Read records
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                int index = 0;
                int checksum = 0;

                //Start code, always 'S'
                if (line[index++] != 'S')
                    throw new InvalidDataException("Expecting 'S'");

                //Record type, 1 digit, 0-9, defining the data field
                //0: Vendor-specific data
                //1: 16-bit data sequence
                //2: 24 bit data sequence
                //3: 32-bit data sequence
                //5: Count of data sequences in the file. Not required.
                //7: Starting address for the program, 32 bit address
                //8: Starting address for the program, 24 bit address
                //9: Starting address for the program, 16 bit address
                int recordType = Convert.ToInt32(line[index++].ToString(), 16);
                int addressLength = 0;
                switch (recordType)
                {
                    case 0:
                    case 1:
                    case 5:
                    case 9:
                        addressLength = 2;
                        break;

                    case 2:
                    case 8:
                        addressLength = 3;
                        break;

                    case 3:
                    case 7:
                        addressLength = 4;
                        break;

                    default:
                        throw new InvalidDataException("Unknown record type");
                }

                //Byte count, 2 digits, number of bytes (2 hex digits) that follow (in address, data, checksum)
                int byteCount = Convert.ToInt32(line.Substring(index, 2), 16);
                index += 2;
                checksum += byteCount;

                //Address, 4, 6 or 8 hex digits determined by the record type
                for (int i = 0; i < addressLength; i++)
                {
                    string ch = line.Substring(index + i * 2, 2);
                    checksum += Convert.ToInt32(ch, 16);
                }

                int address = Convert.ToInt32(line.Substring(index, addressLength * 2), 16);
                index += addressLength * 2;
                byteCount -= addressLength;

                //Data, a sequence of bytes.
                byte[] data = new byte[byteCount - 1];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)Convert.ToInt32(line.Substring(index, 2), 16);
                    index += 2;
                    checksum += data[i];
                }

                //Checksum, two hex digits. Inverted LSB of the sum of values, including byte count, address and all data.
                int readChecksum = (byte)Convert.ToInt32(line.Substring(index, 2), 16);
                checksum = (~checksum & 0xFF);
                if (readChecksum != checksum)
                    throw new InvalidDataException("Failed Checksum!");

                //Put in memory
                Debug.Assert(data.Length % 4 == 0, "Data should only contain full 32-bit words.");
                switch (recordType)
                {
                    case 3: //data intended to be stored in memory.
                        List<uint> memContents = new List<uint>();
                        for (int i = 0; i < data.Length; i += 4)
                        {
                            uint val = 0;
                            for (int j = i; j < i + 4; j++)
                            {
                                val <<= 8;
                                val |= data[j];
                            }
                            memContents.Add(val);
                        }

                        mAddressBus.IsWrite = true;
                        for (int i = 0; i < memContents.Count; i++)
                        {
                            mDataBus.Write(memContents[i]);
                            mAddressBus.Write((uint)(i + address));
                            RAM.Write();
                            ROM.Write();
                            wordsLoaded++;
                        }
                        break;

                    case 7: //entry point for the program.
                        CPU.PC = (uint)address;
                        break;
                }
            }

            return wordsLoaded;
        }

        /// <summary>
        /// Executes a single clock cycle of the board, including the CPU and all other devices.
        /// </summary>
        /// <returns>True if the CPU completed execution of an instruction.</returns>
        public bool Tick()
        {
            bool ret = false;
            mTickCounter++;

            if (CPU.Tick())
            {
                RAM.Write();
                InterruptButton.Write();
                Serial1.Write();
                Serial2.Write();
                Timer.Write();
                Parallel.Write();
                ROM.Write();
                ret = true;
            }
            Timer.Tick();
            Serial1.Tick();
            Serial2.Tick();

            return ret;
        }

        /// <summary>
        /// Disassembles code from memory.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public string Disassemble(uint start, uint len)
        {
            StringBuilder sb = new StringBuilder();
            IR ir = new IR();
            mAddressBus.IsWrite = false;

            for (uint address = start; address < start + len; address++)
            {
                mAddressBus.Write(address);
                ir.Instruction = mDataBus.Value;
                sb.AppendLine(string.Format("0x{0:X5}: 0x{1:X8} {2}", address, ir.Instruction, ir.ToString()));
            }

            return sb.ToString();
        }
        #endregion
    }
}
