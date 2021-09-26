using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSChess;
using NSExtensions;

namespace NSChessExt
{
	class CChessExt : CChess
	{
		public int optRandom = 0;

		public int GetPhase()
		{
			int result = 0;
			for (int n = 0; n < 64; n++)
			{
				int fr = arrField[n];
				int f = g_board[fr];
				if ((f & 7) > 0)
					result++;
			}
			return result;
		}

		public string Start()
		{
			double dPhase = GetPhase() / 32.0;
			double dOptRan = optRandom / 100.0;
			if (random.NextDouble() < dPhase)
				if (random.NextDouble() < dOptRan)
				{
					List<int> moves = GenerateValidMoves(out _);
					if (moves.Count > 0)
					{
						moves.Shuffle();
						return EmoToUmo(moves[0]);
					}
				}
			return String.Empty;
		}

	}
}
