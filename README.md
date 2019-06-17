# Wsim (Formerly rexsim)

This project is a full simulator of the WRAMP (Waikato RISC Architecture
MicroProcessor) CPU used at the University of Waikato for teaching computer
architecture concepts, along with the Basys 3 FPGA that contains the CPU.

Prior versions of this software simulated the Rex board which held an older
version of the hardware, while this version simulates the reimplementation.
It was updated in tandem with the creation of the reimplementation, so it
should accurately reflect the behaviour of the hardware.

## Usage

Running wim requires `mono` to be installed.
It has been tested under mono v4.2.1.0, with XBuild Engine Version 12.0.

Either double-clicking `RexSimulatorGui/bin/Debug/RexSimulatorGui.exe` or
running `$ mono RexSimulatorGui.exe` will launch the program.

On first open, two windows will appear: The main board, and the first serial
port. This serial port is used for communication with WRAMPmon. Type `?` for
help, or simply run `load` to upload a program file. Dragging an .srec into the
serial port window will send it, even though the prompt tells you to press
CTRL-A S. You can also press CTRL-A to open the `Send File` dialog. Typing `go`
after a file is loaded will run it.

The board's physical ports can be interacted with using the main window, and
the tick boxes at the bottom of the window will open other useful windows. See
the Debugging section below for more details.

## Building

To build wsim on Linux, `xbuild` should be used from the root directory. If
you want to build a release copy, use `xbuild /p:Configuration=Release`.
From Windows, the project can be opened in Visual Studio.

## Changelog

A changelog can be found in [CHANGELOG.md](CHANGELOG.md).

## Debugging

wsim contains a number of useful debugging features that can help when writing
programs for WRAMP. On top of the usual commands included with WRAMPmon (type
`help` in the WRAMPmon prompt for information), wsim offers full read access to
all memory locations and registers, as well as breakpoints, single-step mode,
and the ability to view the buses and interrupts.

### Viewing Registers

To view the contents of the registers, click the check box at the bottom of the
form corresponding to the set of registers you are interested in. The General
Purpose Registers are those numbered `$0`~`$13` (plus `$sp` and `$ra`), while
the Special Purpose Registers are the named ones that must be interacted with
via the `movgs` and `movsg` instructions.

You can also view the registers of each of the I/O devices, using the check
boxes in the rightmost column. These can be useful to ensure that the devices
actually behave as you expect them to.

All these forms offer decimal, hexadecimal, and binary representations of the
numbers contained in the registers.

### Viewing Memory

To view the memory, click the check box labelled Memory (RAM). This form has a
number of features, but the first one you will notice is that any data contained
in the user-writeable memory space (0x0000~0x3FFF) is viewable. The form will
show the hexadecimal value, as well as a disassembly of that value. If the data
in memory is an instruction, this disassembly will reflect the original assembly
code which was used to generate that instruction. As such, this form is useful
to trace how your program executes its code.

The leftmost column will show the locations of any special registers that
currently point at memory addresses in range, as well as the program counter. To
jump to the location of a particular register, click `Go To > $regname`.
Particularly useful here are `$pc`, `$ra`, `$evec`, and `$ear`. `$sp` will
generally be near the bottom of memory, and can be useful to help visualise your
stack frame. Be aware that there are no additional amenities, so you must
figure out where your stack frame begins and ends on your own. `$rbase` and
`$ptable` can generally be ignored, unless you are using the experimental user
mode features.

### Viewing Interrupts and Buses

Below the picture of the Basys board are a number of words in grey and black.
These represent the current state of the WRAMP system.

* The Address Bus shows which memory address is currently being interacted with.
* The Data Bus shows what data is being sent to another place in the system
  (such as a register, or a memory location).
* The Program Counter is a pointer to the next instruction that will be
  executed. If this number is larger than 0x80000, then the system is currently
  executing code that is part of WRAMPmon.

The second row shows the currently active interrupt lines. The word
corresponding to a particular interrupt type will turn black when that interrupt
is both active and unmasked in `$cctrl`. The word `Interrupts:` will be black if
any interrupt is active, regardless of the value of `$cctrl`. This is useful to
see if there are any active interrupts which are unmasked, which normally
indicates misconfiguration of an I/O device.

### Stopping Execution

Stepping slowly through code is one of the most useful methods of debugging
WRAMP code. The most basic method to begin this is to click the large green
`Stop` button on the main form. This will pause execution and change the button
to a bright red colour, with its text changing to `Run`. While the system is
paused, you can browse the register and memory contents however you desire. You
can also interact with the parallel and serial ports, but you should be aware
that some actions will not get through to the system while it is paused.

* Flipping the switches will be persisted, since they stay flipped.
* Pressing a button will not be persisted, and the system will not notice they
  were ever pressed.
* This does not apply to the user interrupt button (green) or either of the
  reset buttons (red). You will see their effects once you unpause the system.
* Typing into either of the serial ports will still work, but only the first
  character typed will make it to the system.

The `Single Step` button beneath the `Stop/Run` button will cause the emulated
WRAMP CPU to execute a single instruction. The `Memory (RAM)` form shows the
location of the program counter, which is also the next instruction that will
execute. This is useful to see the exact environment that a particular
instruction is executed in: For example, you can see what the two input
registers hold and check that the output is what you expected.

You might also notice the `Full Speed` check-box. This will cause the program to
stop attempting to match the real clock rate of the Basys implementation of
WRAMP, and instead just run as fast as your computer will allow. This might come
in useful while waiting for long messages to send, large files to load into
WRAMPmon, or during other long-running computations.

### Breakpoints

If you want to pause execution at a particular time in your program, you can set
a breakpoint on a particular instruction. With the `Memory (RAM)` form open,
double-click on any memory address. The `Address` field will change to show a
`[B]` label next to the address of the instruction, indicating the presence of a
breakpoint. When the program counter reaches this instruction, execution will
stop as though you pressed the green `Stop` button. You can then continue as you
did by pressing the button yourself, viewing memory and register contents and
otherwise interacting with the halted board. You can choose to restart
execution, or use the `Single Step` functionality to continue debugging.
