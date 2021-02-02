# BookReaderRnd
BoookReaderRnd can be used as normal UCI chess engine in chess GUI like Arena.
This program just adds random or stupid moves making easier to win games with chess UCI engines.
To use this program you need install  <a href="https://dotnet.microsoft.com/download/dotnet-framework/net48">.NET Framework 4.8</a>

## Parameters

**-rnd** procent of RaNDom moves from 0 to 100, where 0 mean only first move will be random<br/>
**-ef** chess Engine File name<br/>
**-ea** chess Engine Arguments<br/>

### Examples

-rnd 90 -ef stockfish.exe<br />
90 -ef stockfish.exe

Run chess engine stockfish.exe with no arguments and setup random level on 90%