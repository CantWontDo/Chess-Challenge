using ChessChallenge.API;
using System;
using System.Linq;
using System.Collections.Generic;

// Made by a chess and programming novice :D

public class MyBot : IChessBot
{
    // empty, pawn, horse, bishop, castle, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };
    int counter = 0;
    int turn = 0;

    public Move Think(Board board, Timer timer)
    {
        turn++;
        // Start with just random moves
        Random rng = new Random();

        Move[] moves = board.GetLegalMoves();
        Console.WriteLine("turn : " + turn);
        Console.WriteLine("amount of legal moves : " + moves.Length);

        Move goodMove = moves[rng.Next(moves.Length)];

        int highestValue = 0;

        foreach (Move move in moves)
        {
            if (MoveIsCheckmate(board, move))
            {
                goodMove = move;
                break;
            }

            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int pieceValue = pieceValues[(int)(capturedPiece.PieceType)];

            board.MakeMove(move);
            int search = Search(board, 5, int.MinValue, int.MaxValue);
            int value = search + pieceValue;
            Console.WriteLine("search value : " + search);
            Console.WriteLine("piece value : " + pieceValue);
            Console.WriteLine("value : " + value + " move : " + move);
            counter++;
            board.UndoMove(move);

            

            if (highestValue < value)
            {
                highestValue = value;
                goodMove = move;
            }
            if(highestValue == value)
            {
                board.MakeMove(move);
                int doubleCheck = Evaluate(board);
                board.UndoMove(move);

                board.MakeMove(goodMove);
                int doubleCheck2 = Evaluate(board);
                board.UndoMove(goodMove);

                if(doubleCheck > doubleCheck2)
                {
                    goodMove = move;
                }
            }
   
        }

        Console.WriteLine("best value : " + highestValue);
        Console.WriteLine("best move : " + goodMove);
        return goodMove;
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
        if (depth == 0)
        {
            return -SearchForCapture(board, 3, -beta, -alpha); 
            //return Evaluate(board);
        }
        Move[] moves = OrderMoves(board); 
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int value = -Search(board, depth - 1, -beta, -alpha);

            board.UndoMove(move);

            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
            {
                break; // Alpha-beta pruning
            }
        }

        return alpha;
    }

    Move[] OrderMoves(Board board, bool capturesOnly = false)
    {
        Move[] moves = board.GetLegalMoves(capturesOnly);
        int[] scores = new int[moves.Length];
        int i = 0;
        foreach(Move move in moves)
        {
            int scoreGuess = 0;
            Piece start = board.GetPiece(move.StartSquare);
            Piece end = board.GetPiece(move.TargetSquare);

            if(end.PieceType == PieceType.None)
            {
                scoreGuess -= 10 * pieceValues[(int)end.PieceType] - pieceValues[(int)start.PieceType];
            }

            if(move.IsPromotion)
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

    int SearchForCapture(Board board, int depth, int alpha, int beta)
    {
        //Console.WriteLine("hi");
        int score = Evaluate(board);

        if(depth <= 0)
        {
            counter = 0;
            return score;
        }

        if (score >= beta)
        {
            return beta;
        }
        alpha = Math.Max(alpha, score);

        Move[] captures = OrderMoves(board, true);
        
        foreach (Move capture in captures)
        {
            board.MakeMove(capture);
            score = -SearchForCapture(board, depth - 1, -beta, -alpha);
            counter++;

            board.UndoMove(capture);

            if(score >= beta)
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
        value += board.GetPieceList(PieceType.Knight, isWhite).Count * pieceValues[(int)(PieceType.Knight)];
        value += board.GetPieceList(PieceType.Bishop, isWhite).Count * pieceValues[(int)(PieceType.Bishop)];
        value += board.GetPieceList(PieceType.Rook, isWhite).Count * pieceValues[(int)(PieceType.Rook)];
        value += board.GetPieceList(PieceType.Queen, isWhite).Count * pieceValues[(int)(PieceType.Queen)];
        value += board.GetPieceList(PieceType.King, isWhite).Count * pieceValues[(int)(PieceType.King)];
        value += (int)(board.GetLegalMoves().Length * 0.1);

        return value;
    }
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    int DoubledPawns(Board board, bool isWhite)
    {
        PieceList pawns = board.GetPieceList(PieceType.Pawn, isWhite);

        int doubled = 0;
        foreach (Piece pawn1 in pawns)
        {
            foreach (Piece pawn2 in pawns)
            {
                if (pawn1.Square.File == pawn2.Square.File)
                {
                    doubled++;
                }
            }
        }
        return doubled / 2;
    }

}