using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardGameAI
{
    public class MCTS
    {
        Random rand = new Random(DateTime.Now.Millisecond);

        public static int NumberOfPlayouts = 5000;

        public override IGame.IMove MakeDecision(IGame game)
        {
            float bestResult = float.MinValue;

            List<IGame.IMove> potentialMoves = PreprocessMoves(game, game.GetValidMoves());
            if (potentialMoves.Count == 0)
            {
                List<IGame.IMove> C = game.GetValidMoves();
                return C[0];
            }

            if (potentialMoves.Count == 1)
            {
                return potentialMoves[0];
            }

            TProcess.Minimum = 0;
            TProcess.Maximum = potentialMoves.Count;
            TProcess.Value = 0;
            TProcess.Step = 1;

            Dictionary<float, List<IGame.IMove>> moves = new Dictionary<float, List<IGame.IMove>>();

            foreach (IGame.IMove move in potentialMoves)
            {
                TProcess.PerformStep();

                float currentResult = Expand(game, move);

                Console.WriteLine("Move: " + move + " Value: " + currentResult);

                List<IGame.IMove> list;
                if (moves.TryGetValue(currentResult, out list))
                    list.Add(move);
                else
                {
                    list = new List<IGame.IMove>();
                    list.Add(move);
                    moves.Add(currentResult, list);
                }

                if (currentResult > bestResult)
                    bestResult = currentResult;
            }

            // randomize best move
            int c = moves[bestResult].Count;
            if (c == 1)
                return moves[bestResult][0];
            else
                return moves[bestResult][rand.Next(c)];
        }

        float Expand(IGame game, IGame.IMove move)
        {
            int value = 0;

            for (int play = 0; play < NumberOfPlayouts; ++play)
            {
                IGame playout = game.Clone();

                playout.Move(move);

                IGame.State Winner = Simulate(playout);

                if (Winner == game.CurrentPlayer)
                    value++;
                else if (Winner == game.CurrentOpponent)
                    value--;
                else if (rand.Next(2) == 0)
                    value++;
                else
                    value--;
            }

            return (value / (float)NumberOfPlayouts);
        }

        IGame.State Simulate(IGame game)
        {
            int move;
            while (game.PollState() == IGame.State.Unknown)
            {
                List<IGame.IMove> validMoves = game.GetValidMoves();

                move = rand.Next(0, validMoves.Count - 1);

                game.Move(validMoves[move]);
            }

            return game.PollState();
        }

        public static List<IGame.IMove> PreprocessMoves(IGame game, List<IGame.IMove> TestCandidates)
        {
            List<IGame.IMove> CandidateMoves = new List<IGame.IMove>();

            foreach (IGame.IMove Move in TestCandidates)
            {
                IGame CheckWin = game.Clone();

                // test if immediate win possible
                CheckWin.Move(Move);

                if (CheckWin.PollState() == game.CurrentPlayer)
                {
                    CandidateMoves.Add(Move);
                    return CandidateMoves;
                }
                else
                {
                    // test if we make an assist for our opponent
                    List<IGame.IMove> OpponentMoves = CheckWin.GetValidMoves();
                    bool IsBad = false;
                    foreach (IGame.IMove OMove in OpponentMoves)
                    {
                        IGame Tmp = CheckWin.Clone();

                        Tmp.Move(OMove);

                        // did we make an assist?
                        if (Tmp.PollState() == game.CurrentOpponent)
                        {
                            // forget the move!
                            IsBad = true;
                            break;
                        }
                    }
                    if (!IsBad)
                    {
                        CandidateMoves.Add(Move);
                    }
                }
            }
            return CandidateMoves;
        }
    }
}