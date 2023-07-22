using ChessChallenge.API;
using System;

// Made by a chess and programming novice :D

public class MyBot : IChessBot
{
    // empty, pawn, horse, bishop, castle, queen, king
    int[] pieceValues = { 0, 1000, 3000, 3000, 5000, 9000, 90000 };

    public Move Think(Board board, Timer timer)
    {
        // Start with just random moves
        Random rng = new Random(); 

        Move[] moves = board.GetLegalMoves();

        Move goodMove = moves[rng.Next(moves.Length)];

        int highestValue = 0;

        foreach (Move move in moves)
        {
            if (MoveIsCheckmate(board, move))
            {
                goodMove = move;
                break;
            }
            if (MoveIsDraw(board, move))
            {
                continue;
            }

            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int pieceValue = pieceValues[(int)(capturedPiece.PieceType)];

            board.MakeMove(move);
            int value = minimax(board, 5, int.MinValue, int.MaxValue, true) + (pieceValue * 5);
            board.UndoMove(move);
            if (highestValue < value)
            {
                highestValue = value;
                goodMove = move;
            }
   
        }
        return goodMove;
    }


    int Evaluate(Board board)
    {
        int whiteVal = CountMaterial(board, true);
        int blackVal = CountMaterial(board, false);

        int eval = whiteVal - blackVal;

        return eval;
    }

    int minimax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0 || board.IsInCheckmate())
        {
            return Evaluate(board);
        }

        Move[] moves = board.GetLegalMoves();

        if (maximizingPlayer)
        {
            int maxVal = int.MinValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = minimax(board, depth - 1, alpha, beta, false);
                maxVal = Math.Max(score, maxVal);
                alpha = Math.Max(alpha, maxVal);
                board.UndoMove(move);
                if (beta <= alpha)
                {
                    break;
                }

            }
            return maxVal;
        }
        else
        {
            int minValue = int.MaxValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = minimax(board, depth - 1, alpha, beta, true);
                minValue = Math.Min(score, minValue);
                beta = Math.Min(beta, minValue);
                board.UndoMove(move);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return minValue;
        }
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

        return value;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool MoveIsDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool isDraw = board.IsDraw();
        board.UndoMove(move);
        return isDraw;
    }
}