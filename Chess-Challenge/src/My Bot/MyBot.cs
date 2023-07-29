using ChessChallenge.API;
using System;
using System.Linq;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    private const sbyte EXACT = 0, LOWERBOUND = -1, UPPERBOUND = 1, INVALID = -2;
    struct Transposition
    {
        public Transposition(ulong zobristHash, int eval, int depth)
        {
            this.zobristHash = zobristHash;
            this.evaluation = eval;
            this.depth = depth;
            FLAG = INVALID;
        }

        public ulong zobristHash = 0;
        public int depth = 0;
        public int evaluation = 0;
        public sbyte FLAG = INVALID;
    }

    private ulong mask = 0x7FFFFF;

    private Transposition[] transpositionTable;

    public MyBot()
    {
        transpositionTable = new Transposition[mask + 1];
    }

    // empty, pawn, horse, bishop, castle, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

    Move bestMove;
    int mDepth;
    int nodes = 0;

    int infinity = 999999;
    bool endgame = false;

    public Move Think(Board board, Timer timer)
    {

        if (bestMove.IsCapture)
        {
            endgame = Endgame(board);
        }
        mDepth = 5;
        Search(board, mDepth, -infinity, infinity);
        Console.WriteLine(nodes);
        return bestMove;
    }


    int Evaluate(Board board)
    {
        int whiteVal = CountMaterial(board, true);
        int blackVal = CountMaterial(board, false);

        int eval = whiteVal - blackVal;

        int perspective = board.IsWhiteToMove ? 1 : -1;

        return eval * perspective;
    }

    int Search(Board board, int depth, int alpha, int beta)
    {
        if(depth > 0 && board.IsRepeatedPosition())
        {
            return 0;
        }
        {
            
        }
        if(board.IsDraw())
        {
            return 0;
        }

        Move[] moves = OrderMoves(board);

        if (depth == 0 || moves.Length == 0)
        {

            if(board.IsInCheckmate())
            {
                return depth -infinity;
            }
            //return Evaluate(board);
            return SearchForCapture(board, alpha, beta);
        }

        int highestEval = int.MinValue;

        ref Transposition transposition = ref transpositionTable[board.ZobristKey & mask];
        if (transposition.zobristHash == board.ZobristKey && transposition.FLAG != INVALID && transposition.depth >= depth)
        {

            if (transposition.FLAG == EXACT) return transposition.evaluation;
            else if (transposition.FLAG == LOWERBOUND) alpha = Math.Max(alpha, transposition.evaluation);
            else if (transposition.FLAG == UPPERBOUND) beta = Math.Min(beta, transposition.evaluation);
            if (alpha >= beta) return transposition.evaluation;
        }

        foreach (Move move in moves)
        {
            nodes++;
            board.MakeMove(move);

            int value = -Search(board, depth - 1, -beta, -alpha);

            board.UndoMove(move);

            if(highestEval < value)
            {
                highestEval = value;
                if(depth == mDepth)
                {
                    bestMove = move;
                }
            }

            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
            {
                break; 
            }
        }

        transposition.evaluation = highestEval;
        transposition.zobristHash = board.ZobristKey;
        if(highestEval < alpha)
        {
            transposition.FLAG = UPPERBOUND;
        }
        else if(highestEval >= beta)
        {
            transposition.FLAG = LOWERBOUND;
        }

        else
        {
            transposition.FLAG = EXACT; 
        }
        transposition.depth = depth;

        return highestEval;
    }

    Move[] OrderMoves(Board board, bool capturesOnly = false)
    {
        Move[] moves = board.GetLegalMoves(capturesOnly);
        int[] scores = new int[moves.Length];
        int i = 0;
        foreach (Move move in moves)
        {
            int scoreGuess = 0;
            Piece start = board.GetPiece(move.StartSquare);
            Piece end = board.GetPiece(move.TargetSquare);

            if (end.PieceType == PieceType.None)
            {
                scoreGuess -= 10 * (pieceValues[(int)end.PieceType] - pieceValues[(int)start.PieceType]);
            }

            if (move.IsPromotion)
            {
                scoreGuess -= 5 * pieceValues[(int)move.PromotionPieceType];
            }
            scores[i] = scoreGuess;
            i++;
        }

        Array.Sort(scores, moves);
        moves.Reverse();
        /*Console.WriteLine("begin : " + counter);
        foreach (Move move in moves)
        {

            Console.WriteLine(move);
        }

        Console.WriteLine("end : " + counter);*/

        return moves;
    }

    int SearchForCapture(Board board, int alpha, int beta)
    {
        //Console.WriteLine("hi");
        int score = Evaluate(board);


        if (score >= beta)
        {
            return beta;
        }
        alpha = Math.Max(alpha, score);


        Move[] captures = OrderMoves(board, true);

        foreach (Move capture in captures)
        {
            nodes++;
            board.MakeMove(capture);
            score = -SearchForCapture(board, -beta, -alpha);

            board.UndoMove(capture);

            if (score >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, score);
        }
        //Console.WriteLine("beta : " +beta);
        //Console.WriteLine("alpha :" + alpha);
        return alpha;
    }

    int MopUpEval(Square myKing, Square opposeKing)
    {
        int dstCenterFile = Math.Min(3 - opposeKing.File, opposeKing.File - 4);
        int dstCenterRank = Math.Min(3 - opposeKing.Rank, opposeKing.Rank - 4);

        int dstCenter = dstCenterFile + dstCenterRank;

        int dstKingFile = Math.Abs(myKing.File - opposeKing.File);
        int dstKingRank = Math.Abs(myKing.Rank - opposeKing.Rank);
        int dstKing = dstKingFile + dstKingRank;

        return (int)(4.7 * dstCenter + 1.6 * (14 - dstKing));
    }

    bool Endgame(Board board)
    {
        
        int value = 0;
        value += board.GetPieceList(PieceType.Knight, false).Count * 3;
        value += board.GetPieceList(PieceType.Bishop, false).Count * 3;
        value += board.GetPieceList(PieceType.Rook, false).Count * 5;
        value += board.GetPieceList(PieceType.Queen, false).Count * 9;
        Console.WriteLine(value);
        if(value <= 13)
        {

            Console.WriteLine("it's ENDGAME!");
            return true;
        }
        return false;
    }

    int CountMaterial(Board board, bool isWhite)
    {
        int value = 0;

        value += board.GetPieceList(PieceType.Pawn, isWhite).Count * pieceValues[(int)(PieceType.Pawn)];
        value += (int)(board.GetPieceList(PieceType.Knight, isWhite).Count * pieceValues[(int)(PieceType.Knight)] * 1);
        value += board.GetPieceList(PieceType.Bishop, isWhite).Count * pieceValues[(int)(PieceType.Bishop)];
        value += (int)(board.GetPieceList(PieceType.Rook, isWhite).Count * pieceValues[(int)(PieceType.Rook)] * 1);
        value += (int)(board.GetPieceList(PieceType.Queen, isWhite).Count * pieceValues[(int)(PieceType.Queen)] * 1);
        value += board.GetPieceList(PieceType.King, isWhite).Count * pieceValues[(int)(PieceType.King)];
        value += (int)(board.GetLegalMoves().Length * 0.2);

        if(endgame) 
            value += MopUpEval(board.GetKingSquare(isWhite), board.GetKingSquare(!isWhite));
        
        return value;
    }

}