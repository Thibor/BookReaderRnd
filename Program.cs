using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace BookReaderRnd
{
	class Program
	{
		static void Main(string[] args)
		{
			CChess Chess = new CChess();
			CUci Uci = new CUci();
			string engine = args.Length > 0 ? args[0] : "";
			string arguments = args.Length > 1 ? args[1] : "";
			Chess.optRandom = args.Length > 2 ? Convert.ToInt32(args[2]) : 0;
			if (args.Length == 1)
			{
				engine = "";
				arguments = "";
				Chess.optRandom = Convert.ToInt32(args[0]);
			}
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
						string fen = "";
						int lo = Uci.GetIndex("fen", 0);
						int hi = Uci.GetIndex("moves", Uci.tokens.Length);
						if (lo > 0)
						{
							if (lo > hi)
							{
								hi = Uci.tokens.Length;
							}
							for (int n = lo; n < hi; n++)
							{
								if (n > lo)
								{
									fen += ' ';
								}
								fen += Uci.tokens[n];
							}
						}
						Chess.InitializeFromFen(fen);
						lo = Uci.GetIndex("moves", 0);
						hi = Uci.GetIndex("fen", Uci.tokens.Length);
						if (lo > 0)
						{
							if (lo > hi)
							{
								hi = Uci.tokens.Length;
							}
							for (int n = lo; n < hi; n++)
							{
								string m = Uci.tokens[n];
								Chess.MakeMove(Chess.GetMoveFromString(m));
								if (Chess.g_move50 == 0)
									Chess.undoIndex = 0;
							}
						}
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
