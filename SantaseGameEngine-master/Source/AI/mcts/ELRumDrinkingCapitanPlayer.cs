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

        protected static readonly ICollection<Card> playedCards = new List<Card>();//may be should not be static

        public static Queue<Card> announces = new Queue<Card>();


        private readonly OpponentSuitCardsProvider opponentSuitCardsProvider = new OpponentSuitCardsProvider();

        private Random rand = new Random(DateTime.Now.Millisecond);

        public override string Name => "Test Player";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            return this.MakeDecision(context);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            playedCards.Add(context.FirstPlayedCard);
            playedCards.Add(context.SecondPlayedCard);
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
                   playedCards,
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
                    playedCards,
                    context.TrumpCard,
                    context.TrumpCard.Suit);

            foreach (var move in testCandidates)
            {

                //TODO PUT THE SIMULATOR HERE
                
                var game = AmWinnerIfSimpleSimulateFullGame(move, this.Cards.ToList<Card>(),  true, context, context.FirstPlayerRoundPoints, context.SecondPlayerRoundPoints);

               //var game = new RoundSimulation(n new SantaseGameRules());
                //game.Play(0, 0);
                // test if immediate win possible
                if (game)
                {
                    candidateMoves.Add(move);
                    return candidateMoves;
                }
                else
                {
                    // test if we make an assist for our opponent
                    ICollection<Card> opponentMoves = this.opponentSuitCardsProvider.GetOpponentCards(
                    this.Cards,
                    playedCards,
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




        public static bool AmWinnerIfSimpleSimulateFullGame(Card ChosenCard, IList<Card> myHand, bool iAmFirst, PlayerTurnContext context, int myPoints, int otherPoints)
        {
            //TODO what if the game is already closed?

            IList<Card> secretCards = GetAllCards(myHand, playedCards, context.TrumpCard).ToList<Card>();

            //randomize
            var rnd = new Random();
            secretCards = secretCards.OrderBy(item => rnd.Next()).ToList();

            myHand = myHand.OrderBy(x => x.GetValue()).ToList();

            IList<Card> oponentCards = new List<Card>();
            var deckLeft = new Queue<Card>();
            IList<Card> firstHandPl;
            IList<Card> secondHandPl;
            int firstPlPoints;
            int secondPlPoints;

            if (secretCards.Count > myHand.Count && iAmFirst == true)
            {
                for (int i = 0; i < myHand.Count; i++)
                {
                    var randomCard = secretCards.ElementAt(0);
                    oponentCards.Add(randomCard);
                    secretCards.RemoveAt(0);
                }
                oponentCards = oponentCards.OrderBy(x => x.GetValue()).ToList();
                foreach (var sc in secretCards)
                {
                    deckLeft.Enqueue(sc);
                }
                firstHandPl = myHand;
                secondHandPl = oponentCards;
                firstPlPoints = myPoints;
                secondPlPoints = otherPoints;
                //Play first round
                //COPY THE OPEN DECK GAME WITHOUT THE FIRST PLAYER CHOICE
                Card firstPlayerCard = ChosenCard;
                Card secondPlayerCard = PlayFirstOpen(firstHandPl, secondHandPl, firstPlPoints, secondPlPoints, context);

                bool firstIsWinner = IfFirstPlayerIsWinner(firstPlayerCard, secondPlayerCard, context);
                int turnPoints = firstPlayerCard.GetValue() + secondPlayerCard.GetValue();
                if (firstIsWinner)
                {
                    firstPlPoints += turnPoints;
                }
                else
                {
                    secondPlPoints += turnPoints;
                }
                firstHandPl.Remove(firstPlayerCard);
                secondHandPl.Remove(secondPlayerCard);
                playedCards.Add(firstPlayerCard);
                playedCards.Add(secondPlayerCard);
                firstHandPl.Add(deckLeft.Dequeue());
                secondHandPl.Add(deckLeft.Dequeue());
                iAmFirst = firstIsWinner;
            }
            else if (secretCards.Count > myHand.Count)
            {
                for (int i = 0; i < myHand.Count; i++)
                {
                    var randomCard = secretCards.ElementAt(0);
                    oponentCards.Add(randomCard);
                    secretCards.RemoveAt(0);
                }
                oponentCards = oponentCards.OrderBy(x => x.GetValue()).ToList();
                foreach (var sc in secretCards)
                {
                    deckLeft.Enqueue(sc);
                }
                firstHandPl = myHand;
                secondHandPl = oponentCards;
                firstPlPoints = myPoints;
                secondPlPoints = otherPoints;
                //Play first round
                //COPY THE OPEN DECK GAME WITHOUT THE FIRST PLAYER CHOICE
                Card firstPlayerCard = ChosenCard;
                Card secondPlayerCard = PlayFirstOpen(firstHandPl, secondHandPl, firstPlPoints, secondPlPoints, context);

                bool firstIsWinner = IfFirstPlayerIsWinner(firstPlayerCard, secondPlayerCard, context);
                int turnPoints = firstPlayerCard.GetValue() + secondPlayerCard.GetValue();
                if (firstIsWinner)
                {
                    firstPlPoints += turnPoints;
                }
                else
                {
                    secondPlPoints += turnPoints;
                }
                firstHandPl.Remove(firstPlayerCard);
                secondHandPl.Remove(secondPlayerCard);
                playedCards.Add(firstPlayerCard);
                playedCards.Add(secondPlayerCard);
                firstHandPl.Add(deckLeft.Dequeue());
                secondHandPl.Add(deckLeft.Dequeue());
                iAmFirst = !firstIsWinner;
            }


            //TODO PLAY TURN IF SECOND
            else//closed deck
            {
                oponentCards = secretCards;
                if (iAmFirst)
                {
                    firstHandPl = myHand;
                    secondHandPl = oponentCards;
                    firstPlPoints = myPoints;
                    secondPlPoints = otherPoints;

                }
                else
                {
                    firstHandPl = oponentCards;
                    secondHandPl = myHand;
                    firstPlPoints = otherPoints;
                    secondPlPoints = myPoints;
                }
            }

            while (firstPlPoints < 66 && secondPlPoints < 66 && deckLeft.Count > 0)
            {
                if (iAmFirst)
                {
                    iAmFirst = PlayTurnWhileOpen(firstHandPl, secondHandPl, firstPlPoints, secondPlPoints, context);
                    firstHandPl.Add(deckLeft.Dequeue());
                    secondHandPl.Add(deckLeft.Dequeue());

                }
                else
                {
                    iAmFirst = !PlayTurnWhileOpen(secondHandPl, firstHandPl, secondPlPoints, firstPlPoints, context);
                    firstHandPl.Add(deckLeft.Dequeue());
                    secondHandPl.Add(deckLeft.Dequeue());
                }
            }
            while (myPoints < 66 && otherPoints < 66 && firstHandPl.Count() > 0)
            {
                PlayTurnWhileClosed(firstHandPl, secondHandPl, firstPlPoints, secondPlPoints, context);
                //change players


            }


            if (iAmFirst)
            {
                myPoints = firstPlPoints;
                otherPoints = secondPlPoints;
            }
            else
            {
                myPoints = secondPlPoints;
                otherPoints = firstPlPoints;
            }


            if (myPoints > otherPoints)
            {
                return true;
            }
            return false;
        }

        public static bool PlayTurnWhileOpen(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context)
        {

            Card firstPlayerCard = PlayFirstOpen(firstPlayerHandThisTurn, secondPlayerHandThisTurn, pointsFirstPlayer, pointsSecondPlayer, context);

            Card secondPlayerCard = PlaySecondOpen(firstPlayerHandThisTurn, secondPlayerHandThisTurn, pointsFirstPlayer, pointsSecondPlayer, context, firstPlayerCard);

            bool firstIsWinner = IfFirstPlayerIsWinner(firstPlayerCard, secondPlayerCard, context);
            int turnPoints = firstPlayerCard.GetValue() + secondPlayerCard.GetValue();
            if (firstIsWinner)
            {
                pointsFirstPlayer += turnPoints;
            }
            else
            {
                pointsSecondPlayer += turnPoints;
            }
            firstPlayerHandThisTurn.Remove(firstPlayerCard);
            secondPlayerHandThisTurn.Remove(secondPlayerCard);
            playedCards.Add(firstPlayerCard);
            playedCards.Add(secondPlayerCard);
            return firstIsWinner;


        }


        public static Card PlayFirstOpen(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context)
        {
            Card firstPlayerCard;

            //play the first player
            int announceValue = Announce20or40(firstPlayerHandThisTurn, context);
            if (announceValue > 0)
            {
                pointsFirstPlayer += announceValue;
                firstPlayerCard = announces.Dequeue();
            }

            else {
                var leftToGive = firstPlayerHandThisTurn.Where(x => x.Suit != context.TrumpCard.Suit);//accept it is sorted
                if (leftToGive.Count() > 0)
                {
                    firstPlayerCard = leftToGive.FirstOrDefault();
                }
                else
                {
                    firstPlayerCard = firstPlayerHandThisTurn.FirstOrDefault(); ;
                }
            }
            return firstPlayerCard;
        }


        public static Card PlaySecondOpen(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context, Card firstPlayerCard)
        {
            Card secondPlayerCard;


            //play th second player choice
            var secondPlayerSameSuit = secondPlayerHandThisTurn.Where(x => x.Suit == firstPlayerCard.Suit).Where(x => x.GetValue() > firstPlayerCard.GetValue());
            if (secondPlayerSameSuit.Count() > 0)
            {
                secondPlayerCard = secondPlayerSameSuit.FirstOrDefault();
            }
            else if (firstPlayerCard.GetValue() > 4)
            {
                //CHECK for trump to take the other card       
                //TODO
                secondPlayerCard = secondPlayerHandThisTurn.FirstOrDefault();
            }
            else
            {
                secondPlayerCard = secondPlayerHandThisTurn.FirstOrDefault();
            }
            return secondPlayerCard;
        }


        public static bool PlayTurnWhileClosed(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context)
        {
            Card firstPlayerCard = PlayFirstClosed(firstPlayerHandThisTurn, secondPlayerHandThisTurn, pointsFirstPlayer, pointsSecondPlayer, context);

            Card secondPlayerCard = PlaySecondClosed(firstPlayerHandThisTurn, secondPlayerHandThisTurn, pointsFirstPlayer, pointsSecondPlayer, context, firstPlayerCard);

            bool firstIsWinner = IfFirstPlayerIsWinner(firstPlayerCard, secondPlayerCard, context);
            int turnPoints = firstPlayerCard.GetValue() + secondPlayerCard.GetValue();
            if (firstIsWinner)
            {
                pointsFirstPlayer += turnPoints;
            }
            else
            {
                pointsSecondPlayer += turnPoints;
            }
            firstPlayerHandThisTurn.Remove(firstPlayerCard);
            secondPlayerHandThisTurn.Remove(secondPlayerCard);
            playedCards.Add(firstPlayerCard);
            playedCards.Add(secondPlayerCard);
            return firstIsWinner;

        }



        public static Card PlayFirstClosed(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context)
        {
            Card firstPlayerCard = firstPlayerHandThisTurn.FirstOrDefault();

            //play the first player
            int announceValue = Announce20or40(firstPlayerHandThisTurn, context);
            if (announceValue > 0)
            {
                pointsFirstPlayer += announceValue;
                firstPlayerCard = announces.Dequeue();
            }

            else
            {
                var possibleWinning = WinningCards(firstPlayerHandThisTurn, context, playedCards.ToList());
                if (possibleWinning.Count() > 0)
                {
                    firstPlayerCard = possibleWinning.Dequeue();
                }
                else firstPlayerCard = firstPlayerHandThisTurn.FirstOrDefault();

            }
            return firstPlayerCard;
        }



        public static Card PlaySecondClosed(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context, Card firstPlayerCard)
        {
            Card secondPlayerCard;

            //play th second player choice
            IList<Card> secondPlayerSameSuit = secondPlayerHandThisTurn.Where(x => x.Suit == firstPlayerCard.Suit).ToList();

            if (secondPlayerSameSuit.Count() > 0)
            {
                secondPlayerSameSuit.OrderByDescending(x => x.GetValue());
                if (secondPlayerSameSuit.First().GetValue() > firstPlayerCard.GetValue())
                {
                    secondPlayerCard = secondPlayerSameSuit.First();
                }
                else
                {
                    secondPlayerCard = secondPlayerSameSuit.Last();
                }
            }
            else
            {
                //us trumps
                var trumps = secondPlayerHandThisTurn.Where(x => x.Suit == context.TrumpCard.Suit);
                secondPlayerCard = trumps.OrderBy(x => x.GetValue()).ElementAt(0);
                if (trumps.Count() == 0)
                {
                    secondPlayerCard = secondPlayerHandThisTurn.First();
                }
            }
            return secondPlayerCard;
        }


        public static bool IfFirstPlayerIsWinner(Card first, Card second, PlayerTurnContext context)
        {
            if (first.Suit == second.Suit)
            {
                return first.GetValue() > second.GetValue();
            }
            else
            {
                if (second.Suit == context.TrumpCard.Suit)
                {
                    return false;
                }
                return true;
            }
        }


        public static int Announce20or40(IList<Card> currentHand, PlayerTurnContext context)
        {
            foreach (Card card in currentHand)
            {
                if (card.GetValue() == 3)
                {
                    foreach (Card secondCard in currentHand)
                    {
                        if (secondCard.GetValue() == 4)
                        {
                            if (card.Suit == secondCard.Suit)
                            {
                                announces.Enqueue(card);
                                if (card.Suit == context.TrumpCard.Suit)
                                {
                                    return 40;
                                }
                                return 20;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        public static Queue<Card> WinningCards(IList<Card> currentHand, PlayerTurnContext context, IList<Card> playedCards)
        {
            Queue<Card> topCards = new Queue<Card>();
            topCards = GetListOfWinning(currentHand, CardSuit.Club, topCards);
            topCards = GetListOfWinning(currentHand, CardSuit.Spade, topCards);
            topCards = GetListOfWinning(currentHand, CardSuit.Diamond, topCards);
            topCards = GetListOfWinning(currentHand, CardSuit.Heart, topCards);
            return topCards;
        }

        public static Queue<Card> GetListOfWinning(IList<Card> currentHand, CardSuit suit, Queue<Card> q)
        {
            var currentSuitCards = new CardCollection
                                  {
                                      new Card(suit, CardType.Nine),
                                      new Card(suit, CardType.Jack),
                                      new Card(suit, CardType.Queen),
                                      new Card(suit, CardType.King),
                                      new Card(suit, CardType.Ten),
                                      new Card(suit, CardType.Ace),
                                  };

            foreach (var card in playedCards.Where(x => x.Suit == suit))
            {
                currentSuitCards.Remove(card);
            }

            currentSuitCards.OrderByDescending(x => x.GetValue());
            if (currentHand.Contains(currentSuitCards.First()))
            {
                q.Enqueue(currentSuitCards.First());
            }
            return q;
        }

        public static ICollection<Card> GetAllCards(ICollection<Card> myCards, ICollection<Card> playedCards, Card activeTrumpCard)
        {
            var allCards = new CardCollection
                                  {
                                      new Card(CardSuit.Spade, CardType.Nine),
                                      new Card(CardSuit.Spade, CardType.Jack),
                                      new Card(CardSuit.Spade, CardType.Queen),
                                      new Card(CardSuit.Spade, CardType.King),
                                      new Card(CardSuit.Spade, CardType.Ten),
                                      new Card(CardSuit.Spade, CardType.Ace),
                                      new Card(CardSuit.Diamond, CardType.Nine),
                                      new Card(CardSuit.Diamond, CardType.Jack),
                                      new Card(CardSuit.Diamond, CardType.Queen),
                                      new Card(CardSuit.Diamond, CardType.King),
                                      new Card(CardSuit.Diamond, CardType.Ten),
                                      new Card(CardSuit.Diamond, CardType.Ace),
                                      new Card(CardSuit.Club, CardType.Nine),
                                      new Card(CardSuit.Club, CardType.Jack),
                                      new Card(CardSuit.Club, CardType.Queen),
                                      new Card(CardSuit.Club, CardType.King),
                                      new Card(CardSuit.Club, CardType.Ten),
                                      new Card(CardSuit.Club, CardType.Ace),
                                      new Card(CardSuit.Heart, CardType.Nine),
                                      new Card(CardSuit.Heart, CardType.Jack),
                                      new Card(CardSuit.Heart, CardType.Queen),
                                      new Card(CardSuit.Heart, CardType.King),
                                      new Card(CardSuit.Heart, CardType.Ten),
                                      new Card(CardSuit.Heart, CardType.Ace),
                                  };

            foreach (var card in myCards)
            {
                allCards.Remove(card);
            }
            foreach (var card in playedCards)
            {
                allCards.Remove(card);
            }
            return allCards;
        }
    }
}

