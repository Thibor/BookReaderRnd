using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BookReaderRnd
{

	class CChess
	{
		public static CChess This;
		const int piecePawn = 0x01;
		const int pieceKnight = 0x02;
		const int pieceBishop = 0x03;
		const int pieceRook = 0x04;
		const int pieceQueen = 0x05;
		const int pieceKing = 0x06;
		const int colorWhite = 0x08;
		const int colorBlack = 0x10;
		const int colorEmpty = 0x20;
		const int moveflagPassing = 0x02 << 16;
		const int moveflagCastleKing = 0x04 << 16;
		const int moveflagCastleQueen = 0x08 << 16;
		const int moveflagPromotion = 0xf0 << 16;
		const int moveflagPromoteQueen = 0x10 << 16;
		const int moveflagPromoteRook = 0x20 << 16;
		const int moveflagPromoteBishop = 0x40 << 16;
		const int moveflagPromoteKnight = 0x80 << 16;
		const int maskCastle = moveflagCastleKing | moveflagCastleQueen;
		public int optRandom = 0;
		public int target = 0;
		int g_castleRights = 0xf;
		int g_hash = 0;
		int g_passing = 0;
		public int g_move50 = 0;
		int g_moveNumber = 0;
		int g_phase = 32;
		bool g_stop = false;
		public int undoIndex = 0;
		int kingPos = 0;
		int[] arrFieldL = new int[64];
		int[] arrFieldS = new int[256];
		int[] g_board = new int[256];
		int[,] g_hashBoard = new int[256, 16];
		int[] boardCheck = new int[256];
		int[] boardCastle = new int[256];
		public bool whiteTurn = true;
		int usColor = 0;
		int enColor = 0;
		int eeColor = 0;
		string bsFm = "";
		bool lastInsufficient = false;
		int lastScore = 0;
		int[] tmpMaterial = { 0, 100, 300, 310, 500, 800, 0xffff };
		int[] bonMaterial = new int[8];
		int[] arrDirKinght = { 14, -14, 18, -18, 31, -31, 33, -33 };
		int[] arrDirBishop = { 15, -15, 17, -17 };
		int[] arrDirRook = { 1, -1, 16, -16 };
		int[] arrDirQueen = { 1, -1, 15, -15, 16, -16, 17, -17 };
		CUndo[] undoStack = new CUndo[0xfff];
		public Stopwatch stopwatch = Stopwatch.StartNew();
		public static readonly Random random = new Random();

		public CChess()
		{
			This = this;
			g_hash = RAND_32();
			for (int n = 0; n < undoStack.Length; n++)
				undoStack[n] = new CUndo();
			for (int y = 0; y < 8; y++)
				for (int x = 0; x < 8; x++)
				{
					int fieldS = y * 8 + x;
					int fieldL = (y + 4) * 16 + x + 4;
					arrFieldL[fieldS] = fieldL;
					arrFieldS[fieldL] = fieldS;
				}
			for (int n = 0; n < 256; n++)
			{
				boardCheck[n] = 0;
				boardCastle[n] = 15;
				g_board[n] = 0;
				for (int p = 0; p < 16; p++)
					g_hashBoard[n, p] = RAND_32();
			}
			int[] arrCastleI = { 68, 72, 75, 180, 184, 187 };
			int[] arrCasteleV = { 7, 3, 11, 13, 12, 14 };
			int[] arrCheckI = { 71, 72, 73, 183, 184, 185 };
			int[] arrCheckV = { colorBlack | moveflagCastleQueen, colorBlack | maskCastle, colorBlack | moveflagCastleKing, colorWhite | moveflagCastleQueen, colorWhite | maskCastle, colorWhite | moveflagCastleKing };
			for (int n = 0; n < 6; n++)
			{
				boardCastle[arrCastleI[n]] = arrCasteleV[n];
				boardCheck[arrCheckI[n]] = arrCheckV[n];
			}
		}

		void FillMaterial(double v)
		{
			for (int n = 0; n < 6; n++)
				bonMaterial[n] = 100 + Convert.ToInt32((tmpMaterial[n] - 100.0) * v);
			bonMaterial[6] = tmpMaterial[6];
		}

		public void InitializeFromFen(string fen)
		{
			g_phase = 0;
			for (int n = 0; n < 64; n++)
				g_board[arrFieldL[n]] = colorEmpty;
			if (fen == "") fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
			string[] chunks = fen.Split(' ');
			int y = 0;
			int x = 0;
			string pieces = chunks[0];
			for (int i = 0; i < pieces.Length; i++)
			{
				char c = pieces[i];
				if (c == '/')
				{
					y++;
					x = 0;
				}
				else if (c >= '0' && c <= '9')
					x += Int32.Parse(c.ToString());
				else
				{
					g_phase++;
					char b = Char.ToLower(c);
					bool isWhite = b != c;
					int piece = isWhite ? colorWhite : colorBlack;
					int index = (y + 4) * 16 + x + 4;
					switch (b)
					{
						case 'p':
							piece |= piecePawn;
							break;
						case 'b':
							piece |= pieceBishop;
							break;
						case 'n':
							piece |= pieceKnight;
							break;
						case 'r':
							piece |= pieceRook;
							break;
						case 'q':
							piece |= pieceQueen;
							break;
						case 'k':
							piece |= pieceKing;
							break;
					}
					g_board[index] = piece;
					x++;
				}
			}
			whiteTurn = chunks[1] == "w";
			g_castleRights = 0;
			if (chunks[2].IndexOf('K') != -1)
				g_castleRights |= 1;
			if (chunks[2].IndexOf('Q') != -1)
				g_castleRights |= 2;
			if (chunks[2].IndexOf('k') != -1)
				g_castleRights |= 4;
			if (chunks[2].IndexOf('q') != -1)
				g_castleRights |= 8;
			g_passing = 0;
			if (chunks[3].IndexOf('-') == -1)
				g_passing = StrToSquare(chunks[3]);
			g_move50 = 0;
			g_moveNumber = Int32.Parse(chunks[5]);
			if (g_moveNumber > 0) g_moveNumber--;
			g_moveNumber *= 2;
			if (!whiteTurn) g_moveNumber++;
			undoIndex = 0;
		}

		int RAND_32()
		{
			return random.Next();
		}

		string FormatMove(int move)
		{
			string result = FormatSquare(move & 0xFF) + FormatSquare((move >> 8) & 0xFF);
			if ((move & moveflagPromotion) > 0)
			{
				if ((move & moveflagPromoteQueen) > 0) result += 'q';
				else if ((move & moveflagPromoteRook) > 0) result += 'r';
				else if ((move & moveflagPromoteBishop) > 0) result += 'b';
				else result += 'n';
			}
			return result;
		}

		string FormatSquare(int square)
		{
			char[] arr = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
			return arr[(square & 0xf) - 4] + (12 - (square >> 4)).ToString();
		}

		int StrToSquare(string s)
		{
			string fl = "abcdefgh";
			int x = fl.IndexOf(s[0]);
			int y = 12 - Int32.Parse(s[1].ToString());
			return (x + 4) | (y << 4);
		}

		bool IsRepetition()
		{
			for (int n = undoIndex - 4; n >= undoIndex - g_move50; n -= 2)
				if (undoStack[n].hash == g_hash)
				{
					return true;
				}
			return false;
		}

		bool IsAttacked(bool wt, int to)
		{
			int ec = wt ? colorBlack : colorWhite;
			int del = wt ? -16 : 16;
			int fr = to + del;
			if ((g_board[fr + 1] & 0x1f) == (ec | piecePawn))
				return true;
			if ((g_board[fr - 1] & 0x1f) == (ec | piecePawn))
				return true;
			if ((g_board[to + 14] & 0x1f) == (ec | pieceKnight))
				return true;
			if ((g_board[to - 14] & 0x1f) == (ec | pieceKnight))
				return true;
			if ((g_board[to + 18] & 0x1f) == (ec | pieceKnight))
				return true;
			if ((g_board[to - 18] & 0x1f) == (ec | pieceKnight))
				return true;
			if ((g_board[to + 31] & 0x1f) == (ec | pieceKnight))
				return true;
			if ((g_board[to - 31] & 0x1f) == (ec | pieceKnight))
				return true;
			if ((g_board[to + 33] & 0x1f) == (ec | pieceKnight))
				return true;
			if ((g_board[to - 33] & 0x1f) == (ec | pieceKnight))
				return true;
			foreach (int d in arrDirBishop)
			{
				fr = to + d;
				if ((g_board[fr] & 0x1f) == (ec | pieceKing))
					return true;
				while (g_board[fr] > 0)
				{
					if ((g_board[fr] & colorEmpty) > 0)
					{
						fr += d;
						continue;
					}
					if ((g_board[fr] & 0x1f) == (ec | pieceBishop) || (g_board[fr] & 0x1f) == (ec | pieceQueen))
						return true;
					break;
				}
			}
			foreach (int d in arrDirRook)
			{
				fr = to + d;
				if ((g_board[fr] & 0x1f) == (ec | pieceKing))
					return true;
				while (g_board[fr] > 0)
				{
					if ((g_board[fr] & colorEmpty) > 0)
					{
						fr += d;
						continue;
					}
					if ((g_board[fr] & 0x1f) == (ec | pieceRook) || (g_board[fr] & 0x1f) == (ec | pieceQueen))
						return true;
					break;
				}
			}
			return false;
		}

		void AddMove(List<int> moves, int fr, int to, int flag)
		{
			moves.Add(fr | (to << 8) | flag);
		}

		int GetColorScore(bool wt, List<int> moves = null)
		{
			lastScore = 0;
			int pieceM = 0;
			int pieceN = 0;
			int pieceB = 0;
			int w = wt ? 1 : 0;
			usColor = wt ? colorWhite : colorBlack;
			enColor = wt ? colorBlack : colorWhite;
			eeColor = enColor | colorEmpty;
			int colorShUs = usColor & 0xf;
			int colorShEn = enColor & 0xf;
			for (int y = 0; y < 8; y++)
				for (int x = 0; x < 8; x++)
				{
					int n = (y << 3) | x;
					int fr = arrFieldL[n];
					int f = g_board[fr];
					if ((f & usColor) == 0)
						continue;
					int piece = f & 0xf;
					int rank = f & 7;
					lastScore += bonMaterial[rank];
					int c;
					switch (rank)
					{
						case 1:
							pieceM++;
							if (moves != null)
							{
								int del = wt ? -16 : 16;
								int to = fr + del;
								if ((g_board[to - 1] & enColor) > 0)
									GeneratePwnMoves(moves, fr, to - 1, 0);
								else if ((to - 1) == g_passing)
									GeneratePwnMoves(moves, fr, g_passing, moveflagPassing);
								if ((g_board[to + 1] & enColor) > 0)
									GeneratePwnMoves(moves, fr, to + 1, 0);
								else if ((to + 1) == g_passing)
									GeneratePwnMoves(moves, fr, g_passing, moveflagPassing);
							}
							break;
						case 2:
							pieceN++;
							c = moves == null ? CountUniMoves(fr, arrDirKinght, 1) : GenerateUniMoves(moves, true, fr, arrDirKinght, 1);
							break;
						case 3:
							pieceB++;
							c = moves == null ? CountUniMoves(fr, arrDirBishop, 7) : GenerateUniMoves(moves, true, fr, arrDirBishop, 7);
							break;
						case 4:
							pieceM++;
							c = moves == null ? CountUniMoves(fr, arrDirRook, 7) : GenerateUniMoves(moves, true, fr, arrDirRook, 7);
							break;
						case 5:
							pieceM++;
							c = moves == null ? CountUniMoves(fr, arrDirQueen, 7) : GenerateUniMoves(moves, true, fr, arrDirQueen, 7);
							break;
						case 6:
							kingPos = fr;
							if (moves != null)
								GenerateUniMoves(moves, true, fr, arrDirQueen, 1);
							break;
					}
				}
			lastInsufficient = ((pieceM == 0) && (pieceN + (pieceB << 1) < 3));
			return lastScore;
		}

		List<int> GenerateAllMoves(bool wt)
		{
			usColor = wt ? colorWhite : colorBlack;
			enColor = wt ? colorBlack : colorWhite;
			eeColor = enColor | colorEmpty;
			List<int> moves = new List<int>(64);
			for (int n = 0; n < 64; n++)
			{
				int fr = arrFieldL[n];
				int f = g_board[fr];
				if ((f & usColor) > 0)
					f &= 7;
				else
					continue;
				switch (f)
				{
					case 1:
						int del = wt ? -16 : 16;
						int to = fr + del;
						if ((g_board[to] & colorEmpty) > 0)
						{
							GeneratePwnMoves(moves, fr, to, 0);
							if ((g_board[fr - del - del] == 0) && (g_board[to + del] & colorEmpty) > 0)
								GeneratePwnMoves(moves, fr, to + del, 0);
						}
						if ((g_board[to - 1] & enColor) > 0)
							GeneratePwnMoves(moves, fr, to - 1, 0);
						else if ((to - 1) == g_passing)
							GeneratePwnMoves(moves, fr, g_passing, moveflagPassing);
						if ((g_board[to + 1] & enColor) > 0)
							GeneratePwnMoves(moves, fr, to + 1, 0);
						else if ((to + 1) == g_passing)
							GeneratePwnMoves(moves, fr, g_passing, moveflagPassing);
						break;
					case 2:
						GenerateUniMoves(moves, false, fr, arrDirKinght, 1);
						break;
					case 3:
						GenerateUniMoves(moves, false, fr, arrDirBishop, 7);
						break;
					case 4:
						GenerateUniMoves(moves, false, fr, arrDirRook, 7);
						break;
					case 5:
						GenerateUniMoves(moves, false, fr, arrDirQueen, 7);
						break;
					case 6:
						kingPos = fr;
						GenerateUniMoves(moves, false, fr, arrDirQueen, 1);
						int cr = wt ? g_castleRights : g_castleRights >> 2;
						if ((cr & 1) > 0)
							if (((g_board[fr + 1] & colorEmpty) > 0) && ((g_board[fr + 2] & colorEmpty) > 0) && !IsAttacked(wt, fr) && !IsAttacked(wt, fr + 1) && !IsAttacked(wt, fr + 2))
								AddMove(moves, fr, fr + 2, moveflagCastleKing);
						if ((cr & 2) > 0)
							if (((g_board[fr - 1] & colorEmpty) > 0) && ((g_board[fr - 2] & colorEmpty) > 0) && ((g_board[fr - 3] & colorEmpty) > 0) && !IsAttacked(wt, fr) && !IsAttacked(wt, fr - 1) && !IsAttacked(wt, fr - 2))
								AddMove(moves, fr, fr - 2, moveflagCastleQueen);
						break;
				}
			}
			return moves;
		}

		List<int> GenerateLegalMoves(bool wt)
		{
			List<int> moves = GenerateAllMoves(wt);
			for (int n = moves.Count - 1; n >= 0; n--)
			{
				int cm = moves[n];
				MakeMove(cm);
				if (IsAttacked(!whiteTurn, kingPos))
					moves.RemoveAt(n);
				UnmakeMove(cm);
			}
			return moves;
		}


		void GeneratePwnMoves(List<int> moves, int fr, int to, int flag)
		{
			int y = to >> 4;
			if ((y == 4) || (y == 11))
			{
				AddMove(moves, fr, to, moveflagPromoteQueen);
				AddMove(moves, fr, to, moveflagPromoteRook);
				AddMove(moves, fr, to, moveflagPromoteBishop);
				AddMove(moves, fr, to, moveflagPromoteKnight);
			}
			else
				AddMove(moves, fr, to, flag);
		}

		int CountUniMoves(int fr, int[] dir, int count)
		{
			int result = 0;
			for (int n = 0; n < dir.Length; n++)
			{
				int to = fr;
				int c = count;
				while (c-- > 0)
				{
					to += dir[n];
					if ((g_board[to] & colorEmpty) > 0)
						result++;
					else if ((g_board[to] & enColor) > 0)
					{
						result++;
						break;
					}
					else
						break;
				}
			}
			return result;
		}

		int GenerateUniMoves(List<int> moves, bool attack, int fr, int[] dir, int count)
		{
			int result = 0;
			for (int n = 0; n < dir.Length; n++)
			{
				int to = fr;
				int c = count;
				while (c-- > 0)
				{
					to += dir[n];
					if ((g_board[to] & colorEmpty) > 0)
					{
						result++;
						if (!attack)
							AddMove(moves, fr, to, 0);
					}
					else if ((g_board[to] & enColor) > 0)
					{
						result++;
						AddMove(moves, fr, to, 0);
						break;
					}
					else
						break;
				}
			}
			return result;
		}

		public int GetMoveFromString(string moveString)
		{
			List<int> moves = GenerateAllMoves(whiteTurn);
			for (int i = 0; i < moves.Count; i++)
			{
				if (FormatMove(moves[i]) == moveString)
					return moves[i];
			}
			return 0;
		}

		public void MakeMove(int move)
		{
			int fr = move & 0xFF;
			int to = (move >> 8) & 0xFF;
			int flags = move & 0xFF0000;
			int piecefr = g_board[fr];
			int piece = piecefr & 0xf;
			int rank = piecefr & 7;
			int captured = g_board[to];
			if ((flags & moveflagCastleKing) > 0)
			{
				g_board[to - 1] = g_board[to + 1];
				g_board[to + 1] = colorEmpty;
			}
			else if ((flags & moveflagCastleQueen) > 0)
			{
				g_board[to + 1] = g_board[to - 2];
				g_board[to - 2] = colorEmpty;
			}
			else if ((flags & moveflagPassing) > 0)
			{
				int capi = whiteTurn ? to + 16 : to - 16;
				captured = g_board[capi];
				g_board[capi] = colorEmpty;
			}
			CUndo undo = undoStack[undoIndex++];
			undo.captured = captured;
			undo.hash = g_hash;
			undo.passing = g_passing;
			undo.castle = g_castleRights;
			undo.move50 = g_move50;
			undo.kingPos = kingPos;
			g_hash ^= g_hashBoard[fr, piece];
			g_passing = 0;
			int pieceCaptured = captured & 0xF;
			if (pieceCaptured > 0)
			{
				g_move50 = 0;
				g_phase--;
			}
			else if (rank == piecePawn)
			{
				if (to == (fr + 32)) g_passing = (fr + 16);
				if (to == (fr - 32)) g_passing = (fr - 16);
				g_move50 = 0;
			}
			else
				g_move50++;
			if ((flags & moveflagPromotion) > 0)
			{
				int newPiece = piecefr & (~0x7);
				if ((flags & moveflagPromoteKnight) > 0)
					newPiece |= pieceKnight;
				else if ((flags & moveflagPromoteQueen) > 0)
					newPiece |= pieceQueen;
				else if ((flags & moveflagPromoteBishop) > 0)
					newPiece |= pieceBishop;
				else
					newPiece |= pieceRook;
				g_board[to] = newPiece;
				g_hash ^= g_hashBoard[to, newPiece & 7];
			}
			else
			{
				g_board[to] = g_board[fr];
				g_hash ^= g_hashBoard[to, piece];
			}
			if (rank == pieceKing)
				kingPos = to;
			g_board[fr] = colorEmpty;
			g_castleRights &= boardCastle[fr] & boardCastle[to];
			whiteTurn ^= true;
			g_moveNumber++;
		}

		void UnmakeMove(int move)
		{
			int fr = move & 0xFF;
			int to = (move >> 8) & 0xFF;
			int flags = move & 0xFF0000;
			int piece = g_board[to];
			int capi = to;
			CUndo undo = undoStack[--undoIndex];
			g_passing = undo.passing;
			g_castleRights = undo.castle;
			g_move50 = undo.move50;
			g_hash = undo.hash;
			kingPos = undo.kingPos;
			int captured = undo.captured;
			int pieceCaptured = captured & 0xf;
			if ((flags & moveflagCastleKing) > 0)
			{
				g_board[to + 1] = g_board[to - 1];
				g_board[to - 1] = colorEmpty;
			}
			else if ((flags & moveflagCastleQueen) > 0)
			{
				g_board[to - 2] = g_board[to + 1];
				g_board[to + 1] = colorEmpty;
			}
			if ((flags & moveflagPromotion) > 0)
			{
				int newPiece = (piece & (~0x7)) | piecePawn;
				g_board[fr] = newPiece;
			}
			else
			{
				g_board[fr] = g_board[to];
			}
			if ((flags & moveflagPassing) > 0)
			{
				capi = whiteTurn ? to - 16 : to + 16;
				g_board[to] = colorEmpty;
			}
			g_board[capi] = captured;
			if (pieceCaptured > 0)
				g_phase++;
			whiteTurn ^= true;
			g_moveNumber--;
		}

		int Quiesce(int ply, int depth, int alpha, int beta, bool enInsufficient, int enScore)
		{
			List<int> mu = new List<int>(64);
			int score = GetColorScore(whiteTurn, mu) - enScore;
			bool usInsufficient = lastInsufficient;
			int usScore = lastScore;
			if (enInsufficient && usInsufficient)
				return 0;
			if (score >= beta)
				return beta;
			if (score > alpha)
				alpha = score;
			int index = mu.Count;
			while (index-- > 0)
			{
				int cm = mu[index];
				MakeMove(cm);
				if (IsAttacked(!whiteTurn, kingPos))
					score = -0xffff;
				else if (ply < depth)
					score = -Quiesce(ply + 1, depth, -beta, -alpha, usInsufficient, usScore);
				else
				{
					GetColorScore(whiteTurn);
					if (usInsufficient && lastInsufficient)
						score = 0;
					else
						score = usScore - lastScore;
				}
				UnmakeMove(cm);
				if (g_stop) return -0xffff;
				if (alpha < score)
				{
					alpha = score;
					if (alpha >= beta)
						return beta;
				}
			}
			return alpha;
		}

		int Search(List<int> mu, int ply, int depth, int alpha, int beta)
		{
			int n = mu.Count;
			int myMoves = n;
			while (n-- > 0)
			{
				int cm = mu[n];
				MakeMove(cm);
				int osScore = -0xffff;
				if (IsAttacked(!whiteTurn, kingPos))
					myMoves--;
				else if ((g_move50 > 99) || IsRepetition())
					osScore = 0;
				else
				{
					if (ply < depth)
					{
						List<int> me = GenerateAllMoves(whiteTurn);
						osScore = -Search(me, ply + 1, depth, -beta, -alpha);
					}
					else
					{
						GetColorScore(!whiteTurn);
						osScore = -Quiesce(1, depth, -beta, -alpha, lastInsufficient, lastScore);
					}
				}
				UnmakeMove(cm);
				if (g_stop) return -0xffff;
				if (alpha < osScore)
				{
					alpha = osScore;
					if (ply == 1)
						bsFm = FormatMove(cm);
				}
				if (alpha >= beta) break;
			}
			if (myMoves == 0)
				if (IsAttacked(whiteTurn, kingPos))
					alpha = -0xffff + ply;
				else
					alpha = 0;
			return alpha;
		}

		public string Start()
		{
			List<int> mu = GenerateLegalMoves(whiteTurn);
			if ((mu.Count == 0) || (g_phase < 8))
				return "";
			mu.Shuffle();
			string rdFm = FormatMove(mu[0]);
			if ((mu.Count == 1) || (g_moveNumber < 2))
				return rdFm;
			double dOptRan = optRandom / 100.0;
			double dPhase = g_phase / 32.0;
			double dRan = random.NextDouble();
			dOptRan = Math.Min(dPhase, dOptRan);
			if (dOptRan < dRan)
				return "";
			FillMaterial(1.0 - dOptRan);
			int score = Search(mu, 1, 3, -0xffff, 0xffff);
			if (target > score)
				target = score;
			if (target > -dOptRan * 1000)
				return rdFm;
			return bsFm;
		}

	}
}