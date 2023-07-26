using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        // empty, pawn, horse, bishop, castle, queen, king
        int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };
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

                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int pieceValue = pieceValues[(int)(capturedPiece.PieceType)];

                board.MakeMove(move);
                int value = Search(board, 3, int.MinValue, int.MaxValue) + pieceValue;
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

            int perspective = board.IsWhiteToMove ? 1 : -1;

            return eval * perspective;
        }

        int Search(Board board, int depth, int alpha, int beta)
        {
            if (depth == 0)
            {
                return SearchForCapture(board, alpha, beta);
            }

            Move[] moves = OrderMoves(board);
            if (moves.Length <= 0)
            {
                if (board.IsInCheckmate())
                {
                    return int.MinValue;
                }
                return 0;
            }

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = -Search(board, depth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (score >= beta)
                {
                    return beta;
                }
                alpha = Math.Max(score, alpha);
            }

            return alpha;
        }

        Move[] OrderMoves(Board board)
        {
            Move[] moves = board.GetLegalMoves();
            int[] scores = new int[moves.Length];
            int i = 0;
            foreach (Move move in moves)
            {
                int scoreGuess = 0;
                Piece start = board.GetPiece(move.StartSquare);
                Piece end = board.GetPiece(move.TargetSquare);

                if (end.PieceType == PieceType.None)
                {
                    scoreGuess += 10 * pieceValues[(int)end.PieceType] - pieceValues[(int)start.PieceType];
                }

                if (move.IsPromotion)
                {
                    scoreGuess += 5 * pieceValues[(int)move.PromotionPieceType];
                }
                scores[i] = scoreGuess;
                i++;
            }

            Array.Sort(scores, moves);
            return moves;
        }

        int SearchForCapture(Board board, int alpha, int beta)
        {
            int score = Evaluate(board);
            if (score >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, score);

            Move[] captures = OrderMoves(board);

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
            value -= (int)((DoubledPawns(board, isWhite) * 0.5));

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
}