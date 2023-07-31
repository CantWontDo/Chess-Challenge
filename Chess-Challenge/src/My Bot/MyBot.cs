﻿using ChessChallenge.API;
using System;
using System.Linq;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    private const sbyte INVALID = 0, EXACT = 1, LOWERBOUND = 2, UPPERBOUND = 3;
    struct Transposition
    {

        public ulong zobristHash;
        public int depth;
        public int evaluation;
        public sbyte FLAG;
        public Move move;

    }

    private ulong mask = 0x7FFFFF;

    private Transposition[] transpositionTable;

    public MyBot()
    {
        transpositionTable = new Transposition[mask + 1];
    }

    // empty, pawn, horse, bishop, castle, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

    int nodes = 0;
    int mDepth;

    int infinity = 999999;
    int fraction = 40;

    public Move Think(Board board, Timer timer)
    {
        Console.WriteLine("______________________________________");
        int maxTime = timer.MillisecondsRemaining / fraction;
        Transposition bestMove = transpositionTable[board.ZobristKey & mask];
        for (int depth = 1; ; depth++)
        {
            mDepth = depth;
            int search = Search(board, depth, -infinity, infinity, 0);
            bestMove = transpositionTable[board.ZobristKey & mask];
            Console.WriteLine(nodes);
            Console.WriteLine("time elapsed : " + (float)(timer.MillisecondsElapsedThisTurn));

            Console.WriteLine("depth : " + depth);
            Console.WriteLine("bestMove : " + bestMove.move);
            Console.WriteLine("search value : " + search);

            if (!ShouldDeepen(timer, maxTime)) break;
        }
        return bestMove.move;
    }

    bool ShouldDeepen(Timer timer, int maxTime)
    {
        int currentThinkTime = timer.MillisecondsElapsedThisTurn;
        return ((maxTime - currentThinkTime) > currentThinkTime * 2);
    }

    int Evaluate(Board board)
    {

        nodes++;
        int whiteVal = CountMaterial(board, true);
        int blackVal = CountMaterial(board, false);

        int eval = whiteVal - blackVal;

        int perspective = board.IsWhiteToMove ? 1 : -1;

        return eval * perspective;
    }

    int Search(Board board, int depth, int alpha, int beta, int numExtensions)
    {
        if(depth > 0 && board.IsRepeatedPosition())
        {
            //Console.WriteLine('a');
            return 0;
        }
        {
            
        }
        if(board.IsDraw())
        {
            //Console.WriteLine('b');
            return 0;
        }

        Move[] moves = OrderMoves(board);

        if (depth == 0 || moves.Length == 0)
        {

            if(board.IsInCheckmate())
            {
                int depthFromRoot = mDepth - depth;
                //Console.WriteLine('c');
                return -infinity + depthFromRoot;
                
            }
            //return Evaluate(board);
            //Console.WriteLine('d');
            return SearchForCapture(board, alpha, beta);
        }

        int highestEval = int.MinValue;
        int startingAlpha = alpha;
        ref Transposition transposition = ref transpositionTable[board.ZobristKey & mask];
        if (transposition.zobristHash == board.ZobristKey && transposition.FLAG != INVALID && transposition.depth >= depth)
        {

            if (transposition.FLAG == EXACT)
            {
                //Console.WriteLine('e');
                return transposition.evaluation;

            }
            else if (transposition.FLAG == LOWERBOUND) alpha = Math.Max(alpha, transposition.evaluation);
            else if (transposition.FLAG == UPPERBOUND) beta = Math.Min(beta, transposition.evaluation);
            if (alpha >= beta)
            {
                //Console.WriteLine('f');
                return transposition.evaluation;
            }
        }
        Move bestMove = moves[0];
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int extension = numExtensions < 16 && board.IsInCheck() ? 1 : 0;
            int value = -Search(board, depth - 1 + extension, -beta, -alpha, numExtensions + extension);

            board.UndoMove(move);

            if(highestEval < value)
            {
                highestEval = value;
                    bestMove = move;
                
            }

            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
            {
                break; 
            }
        }

        transposition.evaluation = highestEval;
        transposition.zobristHash = board.ZobristKey;
        if(highestEval < startingAlpha)
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
        transposition.move = bestMove;
        //Console.WriteLine('g');
        return highestEval;
    }

    Move[] OrderMoves(Board board, bool capturesOnly = false)
    {
        Move[] moves = board.GetLegalMoves(capturesOnly);
        int[] scores = new int[moves.Length];
        int i = 0;
        
        foreach (Move move in moves)
        {
            Transposition best = transpositionTable[board.ZobristKey & mask];
            int scoreGuess = 0;
            Piece start = board.GetPiece(move.StartSquare);
            Piece end = board.GetPiece(move.TargetSquare);
            if(best.move == move && board.ZobristKey == best.zobristHash)
            {
                scoreGuess += infinity;
            }
            

            if (end.PieceType != PieceType.None)
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
    int CountMaterial(Board board, bool isWhite)
    {
        int value = 0;

        value += board.GetPieceList(PieceType.Pawn, isWhite).Count * pieceValues[(int)(PieceType.Pawn)];
        value += (int)(board.GetPieceList(PieceType.Knight, isWhite).Count * pieceValues[(int)(PieceType.Knight)]); /*-
            ((board.GetPieceList(PieceType.Pawn, !isWhite).Count * 20)));*/
        value += board.GetPieceList(PieceType.Bishop, isWhite).Count * pieceValues[(int)(PieceType.Bishop)];
        value += (int)(board.GetPieceList(PieceType.Rook, isWhite).Count * pieceValues[(int)(PieceType.Rook)]); /*+
            ((board.GetPieceList(PieceType.Pawn, !isWhite).Count * 20)));*/
        value += (int)(board.GetPieceList(PieceType.Queen, isWhite).Count * pieceValues[(int)(PieceType.Queen)] * 1);
        value += board.GetPieceList(PieceType.King, isWhite).Count * pieceValues[(int)(PieceType.King)];
        value += (int)(board.GetLegalMoves().Length * 0.2);
        return value;
    }

}