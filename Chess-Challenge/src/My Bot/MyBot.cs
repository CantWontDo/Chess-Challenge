using ChessChallenge.API;
using System;

// Made by a complete chess novice :D

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
            }

            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int pieceValue = pieceValues[(int)(capturedPiece.PieceType)];

            if (pieceValue > highestValue)
            {
                goodMove = move;
                highestValue = pieceValue;
            }
        }
        return goodMove;
    }
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}