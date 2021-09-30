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

		public int GetMoveScore(int emo)
		{
			int fr = emo & 0xff;
			int to = (emo >> 8) & 0xff;
			int valFr = 0x7 - (g_board[fr] & 0x7);
			int valTo = g_board[to] & 0x7;
			return (valTo << 3) & valFr;
		}

		public void SortMoves(List<int> list)
		{
			list.Sort(delegate (int m1, int m2)
			{
				return GetMoveScore(m2) - GetMoveScore(m1);
			});
		}

		public string GetMove()
		{
			double dPhase = GetPhase() / 32.0;
			double dOptRan = optRandom / 100.0;
			double r = random.NextDouble();
			if (((r < dPhase) && ( r < dOptRan)) || (optRandom > 100))
			{
				List<int> moves = GenerateValidMoves(out _);
				if (moves.Count > 0)
				{
					moves.Shuffle();
					SortMoves(moves);
					return EmoToUmo(moves[0]);
				}
			}
			return String.Empty;
		}

	}
}
