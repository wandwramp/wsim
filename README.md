# Rexsim

This project is a full simulator of the WRAMP (Waikato RISC Architecture
MicroProcessor) CPU used at the University of Waikato for teaching computer
architecture concepts, along with the Basys board that contains the CPU.

Prior versions of this software simulated the Rex board which held an older
version of the hardware, while this version simulates the reimplementation.
It was updated in tandem with the creation of the reimplementation, so it
should accurately reflect the behaviour of the hardware.

## Usage

Running rexsim requires `mono` to be installed.
It has been tested under mono v4.2.1.0, with XBuild Engine Version 12.0.

Either double-clicking `RexSimulatorGui/bin/Debug/RexSimulatorGui.exe` or
running `$ mono RexSimulatorGui.exe` will launch the program.

On first open, two windows will appear: The main board, and the first serial
port. This serial port is used for communication with WRAMPmon. Type `?` for
help, or simply run `load` to upload a program file. Dragging an .srec into
the serial port window will send it, even though the prompt tells you to press
CTRL-A S. Typing `go` after a file is loaded will run it.

The board's physical ports can be interacted with using the main window, and
the tick boxes at the bottom of the window will open other useful windows.

## Building

To build rexsim on Linux,, `xbuild` should be used from the root directory.
From Windows, the project can be opened in Visual Studio.

## Changelog

A changelog can be found in [CHANGELOG.md](CHANGELOG.md)
