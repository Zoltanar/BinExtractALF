Extract data from Eushully games archived using a combination of a header/directory .INI file (SYS4INI.BIN, SYS5INI.BIN) or AAI file (APPEND01.AAI, APPEND02.AAI) and an archived data .ALF file.
This extractor supersedes exs4alf, which was able to extract from version 4 of such archives, with the added ability to extract from version 5.

Usage is the same as exs4alf, open the command prompt on the folder with BinExtractALF.exe, and enter the executable name, followed by a single parameter for the header/directory file path, an optional output directory parameter has also been added.
Examples:
BinExtractALF SYS4INI.BIN
BinExtractALF APPEND01.AAI Output
BinExtractALF SYS4INI.BIN "..\Extractor Output"

The archived data (.ALF) files must be present in the same folder as the header/directory (.INI or .AAI) file.

This application is able to extract data from the trial version of Eushully's game Hyakusen no Jou ni Kawatareshi Toki, and may require updates to work with the full version when it is released. 
The application was built from porting exs4alf and adding support for version 5.

exs4alf credits:
// exs4alf.cpp, v1.1 2009/04/26
// coded by asmodean

// contact: 
//   web:   http://asmodean.reverse.net
//   email: asmodean [at] hush.com
//   irc:   asmodean on efnet (irc.efnet.net)
