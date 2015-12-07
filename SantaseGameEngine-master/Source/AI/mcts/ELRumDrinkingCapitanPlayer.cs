namespace Santase.AI.RumDrinkingCapitanPlayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.RumDrinkingCapitanPlayer.Helpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using GameSimulations;

    public class ELRumDrinkingCapitanPlayer : BasePlayer
    {
        public const int NumberOfPlayouts = 50;

        private readonly ICollection<Card> playedCards = new List<Card>();

        private readonly OpponentSuitCardsProvider opponentSuitCardsProvider = new OpponentSuitCardsProvider();

        private Random rand = new Random(DateTime.Now.Millisecond);

        public override string Name => "Test Player";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            return this.MakeDecision(context);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.playedCards.Add(context.FirstPlayedCard);
            this.playedCards.Add(context.SecondPlayedCard);
        }

        public PlayerAction MakeDecision(PlayerTurnContext context)
        {
            float bestResult = float.MinValue;

            var potentialCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards).ToList();

            var tryToAnnonce20Or40 = this.TryToAnnounce20Or40(context, potentialCardsToPlay);

            if (tryToAnnonce20Or40 != null)
            {
                return tryToAnnonce20Or40;
            }

            var potentialMoves = this.PreprocessMoves(context, potentialCardsToPlay);

            if (potentialMoves.Count == 0)
            {
                var move = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
                return this.PlayCard(move.FirstOrDefault());
            }

            if (potentialMoves.Count == 1)
            {
                return this.PlayCard(potentialMoves.FirstOrDefault());
            }

            Dictionary<float, List<Card>> moves = new Dictionary<float, List<Card>>();

            foreach (Card move in potentialMoves)
            {
                float currentResult = this.Expand(move, context);

                List<Card> list;
                if (moves.TryGetValue(currentResult, out list))
                {
                    list.Add(move);
                }
                else
                {
                    list = new List<Card>();
                    list.Add(move);
                    moves.Add(currentResult, list);
                }

                if (currentResult > bestResult)
                {
                    bestResult = currentResult;
                }
            }

            // randomize best move
            int count = moves[bestResult].Count;
            if (count == 1)
            {
                return this.PlayCard(moves[bestResult][0]);
            }
            else
            {
                return this.PlayCard(moves[bestResult][this.rand.Next(count)]);
            }
        }

        private float Expand(Card move, PlayerTurnContext context)
        {
            int value = 0;
            var oppositeCard = this.opponentSuitCardsProvider.GetOpponentCards(
                   this.Cards,
                   this.playedCards,
                   context.TrumpCard,
                   context.TrumpCard.Suit);

            for (int play = 0; play < NumberOfPlayouts; ++play)
            {
                var playout = new RoundSimulation(new SmartPlayerSimulation(this.Cards, move), new SmartPlayerSimulation(oppositeCard), new SantaseGameRules());
                playout.Play(0, 0);

                if (playout.IWon())
                {
                    value++;
                }
                else if (playout.OppositePlayerWon())
                {
                    value--;
                }
                else if (this.rand.Next(2) == 0)
                {
                    value++;
                }
                else
                {
                    value--;
                }
            }

            return value / (float)NumberOfPlayouts;
        }

        private ICollection<Card> PreprocessMoves(PlayerTurnContext context, ICollection<Card> testCandidates)
        {
            var candidateMoves = new List<Card>();
            var oppositeCard = this.opponentSuitCardsProvider.GetOpponentCards(
                    this.Cards,
                    this.playedCards,
                    context.TrumpCard,
                    context.TrumpCard.Suit);

            foreach (var move in testCandidates)
            {
                var game = new RoundSimulation(new SmartPlayerSimulation(this.Cards, move), new SmartPlayerSimulation(oppositeCard), new SantaseGameRules());
                game.Play(0, 0);
                // test if immediate win possible
                if (game.IWon())
                {
                    candidateMoves.Add(move);
                    return candidateMoves;
                }
                else
                {
                    // test if we make an assist for our opponent
                    ICollection<Card> opponentMoves = this.opponentSuitCardsProvider.GetOpponentCards(
                    this.Cards,
                    this.playedCards,
                    context.TrumpCard,
                    context.TrumpCard.Suit);
                    bool isBad = false;

                    //foreach (var opponentsMove in opponentMoves)
                    //{
                    //    var tmp = game.DeepClone();

                    //   // tmp.Move(opponentsMove);

                    //    // did we make an assist?
                    //    if (tmp.OppositePlayerWon())
                    //    {
                    //        // forget the move!
                    //        isBad = true;
                    //        break;
                    //    }
                    //}

                    if (!isBad)
                    {
                        candidateMoves.Add(move);
                    }
                }
            }

            return candidateMoves;
        }

        private PlayerAction TryToAnnounce20Or40(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // Choose card with announce 40 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard) == Announce.Forty)
                {
                    return this.PlayCard(card);
                }
            }

            // Choose card with announce 20 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard)
                    == Announce.Twenty)
                {
                    return this.PlayCard(card);
                }
            }

            return null;
        }
    }
}
