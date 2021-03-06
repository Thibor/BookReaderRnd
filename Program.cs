﻿using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using NSUci;

namespace BookReaderRnd
{
	class Program
	{
		static void Main(string[] args)
		{
			CChess Chess = new CChess();
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
			string engine = String.Join(" ", listEf);
			string arguments = String.Join(" ", listEa);
			Process myProcess = new Process();
			if (File.Exists(engine))
			{
				myProcess.StartInfo.FileName = engine;
				myProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engine);
				myProcess.StartInfo.UseShellExecute = false;
				myProcess.StartInfo.RedirectStandardInput = true;
				myProcess.StartInfo.Arguments = arguments;
				myProcess.Start();
			}
			else
			{
				if (engine != "")
					Console.WriteLine("info string missing engine");
				engine = "";
			}

			while (true)
			{
				string msg = Console.ReadLine();
				Uci.SetMsg(msg);
				if ((Uci.command != "go") && (engine != ""))
					myProcess.StandardInput.WriteLine(msg);
				switch (Uci.command)
				{
					case "ucinewgame":
						Chess.target = 0;
						break;
					case "position":
						string fen = Uci.GetValue("fen", "moves");
						string moves = Uci.GetValue("moves", "fen");
						Chess.SetFen(fen);
						Chess.MakeMoves(moves);
						Chess.undoIndex = 0;
						break;
					case "go":
						string move = Chess.Start();
						if (move != "")
						{
							Console.WriteLine("info string book");
							Console.WriteLine($"bestmove {move}");
						}
						else if (engine == "")
							Console.WriteLine("enginemove");
						else
							myProcess.StandardInput.WriteLine(msg);
						break;
				}
			}


		}
	}
}
