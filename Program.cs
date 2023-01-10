using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using NSChessExt;
using NSUci;

namespace NSProgram
{
	class Program
	{
		static void Main(string[] args)
		{
			CChessExt Chess = new CChessExt();
			CUci Uci = new CUci();
			string ax = "-rnd";
			List<string> listEf = new List<string>();
			List<string> listEa = new List<string>();
			for (int n = 0; n < args.Length; n++)
			{
				string ac = args[n];
				switch (ac)
				{
					case "-rnd":
					case "-ef":
					case "-ea":
						ax = ac;
						break;
					default:
						switch (ax)
						{
							case "-rnd":
								Chess.optRandom = int.Parse(ac);
								break;
							case "-ef":
								listEf.Add(ac);
								break;
							case "-ea":
								listEa.Add(ac);
								break;
						}
						break;
				}
			}
			string engineFile = String.Join(" ", listEf);
			string engineArguments = String.Join(" ", listEa);
			Process engineProcess = new Process();
			if (File.Exists(engineFile))
			{
				engineProcess.StartInfo.FileName = engineFile;
				engineProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engineFile);
				engineProcess.StartInfo.UseShellExecute = false;
				engineProcess.StartInfo.RedirectStandardInput = true;
				engineProcess.StartInfo.Arguments = engineArguments;
				engineProcess.Start();
			}
			else
			{
				if (engineFile != string.Empty)
					Console.WriteLine("info string missing engine");
				engineFile = String.Empty;
			}

			do{
				string msg = Console.ReadLine();
				Uci.SetMsg(msg);
				if ((Uci.command != "go") && (engineFile != String.Empty))
					engineProcess.StandardInput.WriteLine(msg);
				switch (Uci.command)
				{
					case "position":
						string fen = Uci.GetValue("fen", "moves");
						string moves = Uci.GetValue("moves", "fen");
						Chess.SetFen(fen);
						Chess.MakeMoves(moves);
						break;
					case "go":
						string move = Chess.GetMove();
						if (move != String.Empty)
						{
							Console.WriteLine("info string book");
							Console.WriteLine($"bestmove {move}");
						}
						else if (engineFile == String.Empty)
							Console.WriteLine("enginemove");
						else
							engineProcess.StandardInput.WriteLine(msg);
						break;
				}
			} while (Uci.command != "quit") ;

		}
	}
}
