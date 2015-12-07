namespace Santase.AI.RumDrinkingCapitanPlayer.GameSimulations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class GameSimulation
    {
        private ICollection<Card> playedCardsInCurrentContext;

        private Queue<Card> announces;

        public GameSimulation()
        {
            this.playedCardsInCurrentContext = new List<Card>();
            this.announces = new Queue<Card>();
        }

        public bool AmWinnerIfSimpleSimulateFullGame(ICollection<Card> playedCards, Card chosenCard, IList<Card> myHand, bool iAmFirst, PlayerTurnContext context, int myPoints, int otherPoints)
        {
            ////TODO what if the game is already closed?
            ////  Console.WriteLine(myHand.Count().ToString());
            this.playedCardsInCurrentContext.Clear();
            this.announces.Clear();

            foreach (Card v in playedCards)
            {
                this.playedCardsInCurrentContext.Add(v);
            }

            IList<Card> secretCards = this.GetAllCards(myHand, this.playedCardsInCurrentContext, context.TrumpCard).ToList<Card>();
            //// Console.WriteLine("Secret cards {0}, Played Cards {1}", secretCards.Count(), playedCardsInCurrentContext.Count());

            ////  Console.WriteLine(secretCards.Count().ToString());
            ////randomize
            var rnd = new Random();
            secretCards = secretCards.OrderBy(item => rnd.Next()).ToList();

            myHand = myHand.OrderBy(x => x.GetValue()).ToList();

            IList<Card> oponentCards = new List<Card>();
            var deckLeft = new Queue<Card>();
            IList<Card> firstHandPl;
            IList<Card> secondHandPl;
            int firstPlPoints;
            int secondPlPoints;

            if (secretCards.Count() > myHand.Count() && iAmFirst == true)
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

                ////Play first round
                ////COPY THE OPEN DECK GAME WITHOUT THE FIRST PLAYER CHOICE
                Card firstPlayerCard = chosenCard;
                Card secondPlayerCard = this.PlaySecondOpen(firstHandPl, secondHandPl, firstPlPoints, secondPlPoints, context, firstPlayerCard);

                bool firstIsWinner = this.IfFirstPlayerIsWinner(firstPlayerCard, secondPlayerCard, context);
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
                this.playedCardsInCurrentContext.Add(firstPlayerCard);
                this.playedCardsInCurrentContext.Add(secondPlayerCard);

                if (deckLeft.Count > 0)
                {
                    firstHandPl.Add(deckLeft.Dequeue());
                }

                if (deckLeft.Count > 0)
                {
                    secondHandPl.Add(deckLeft.Dequeue());
                }

                iAmFirst = firstIsWinner;
            }
            else if (secretCards.Count() > myHand.Count())
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

                ////Play first round
                ////COPY THE OPEN DECK GAME WITHOUT THE FIRST PLAYER CHOICE
                Card firstPlayerCard = chosenCard;
                Card secondPlayerCard = this.PlayFirstOpen(firstHandPl, secondHandPl, firstPlPoints, secondPlPoints, context);

                bool firstIsWinner = this.IfFirstPlayerIsWinner(firstPlayerCard, secondPlayerCard, context);
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
                this.playedCardsInCurrentContext.Add(firstPlayerCard);
                this.playedCardsInCurrentContext.Add(secondPlayerCard);
                firstHandPl.Add(deckLeft.Dequeue());
                secondHandPl.Add(deckLeft.Dequeue());
                iAmFirst = !firstIsWinner;
            }
            else
            {
                oponentCards = secretCards;
                ////Console.WriteLine("CLOSE");
                ////Console.WriteLine(oponentCards.Count().ToString());
                ////Console.WriteLine(myHand.Count().ToString());
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
                    ////TODO PLAY
                }
            }

            while (firstPlPoints < 66 && secondPlPoints < 66 && deckLeft.Count() > 1 && firstHandPl.Count() > 0 && secondHandPl.Count() > 0)
            {
                if (iAmFirst)
                {
                    iAmFirst = this.PlayTurnWhileOpen(firstHandPl, secondHandPl, firstPlPoints, secondPlPoints, context);
                    firstHandPl.Add(deckLeft.Dequeue());
                    secondHandPl.Add(deckLeft.Dequeue());
                }
                else
                {
                    iAmFirst = !this.PlayTurnWhileOpen(secondHandPl, firstHandPl, secondPlPoints, firstPlPoints, context);
                    firstHandPl.Add(deckLeft.Dequeue());
                    secondHandPl.Add(deckLeft.Dequeue());
                }
            }

            while (myPoints < 66 && otherPoints < 66 && firstHandPl.Count() > 0 && firstHandPl.Count() > 0 && secondHandPl.Count() > 0)
            {
                this.PlayTurnWhileClosed(firstHandPl, secondHandPl, firstPlPoints, secondPlPoints, context);
                ////change players
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

        public bool PlayTurnWhileOpen(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context)
        {
            Card firstPlayerCard = this.PlayFirstOpen(firstPlayerHandThisTurn, secondPlayerHandThisTurn, pointsFirstPlayer, pointsSecondPlayer, context);

            Card secondPlayerCard = this.PlaySecondOpen(firstPlayerHandThisTurn, secondPlayerHandThisTurn, pointsFirstPlayer, pointsSecondPlayer, context, firstPlayerCard);

            bool firstIsWinner = this.IfFirstPlayerIsWinner(firstPlayerCard, secondPlayerCard, context);
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
            this.playedCardsInCurrentContext.Add(firstPlayerCard);
            this.playedCardsInCurrentContext.Add(secondPlayerCard);

            return firstIsWinner;
        }

        public Card PlayFirstOpen(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context)
        {
            Card firstPlayerCard;

            ////play the first player
            int announceValue = this.Announce20or40(firstPlayerHandThisTurn, context);
            if (announceValue > 0)
            {
                pointsFirstPlayer += announceValue;
                firstPlayerCard = this.announces.Dequeue();
            }
            else
            {
                ////accept it is sorted
                var leftToGive = firstPlayerHandThisTurn.Where(x => x.Suit != context.TrumpCard.Suit);
                if (leftToGive.Count() > 0)
                {
                    firstPlayerCard = leftToGive.FirstOrDefault();
                }
                else
                {
                    firstPlayerCard = firstPlayerHandThisTurn.FirstOrDefault();
                }
            }

            return firstPlayerCard;
        }

        public Card PlaySecondOpen(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context, Card firstPlayerCard)
        {
            Card secondPlayerCard;

            ////play th second player choice
            var secondPlayerSameSuit = secondPlayerHandThisTurn.Where(x => x.Suit == firstPlayerCard.Suit).Where(x => x.GetValue() > firstPlayerCard.GetValue());
            if (secondPlayerSameSuit.Count() > 0)
            {
                secondPlayerCard = secondPlayerSameSuit.FirstOrDefault();
            }
            else if (firstPlayerCard.GetValue() > 4)
            {
                ////CHECK for trump to take the other card       
                ////TODO
                secondPlayerCard = secondPlayerHandThisTurn.FirstOrDefault();
            }
            else
            {
                secondPlayerCard = secondPlayerHandThisTurn.FirstOrDefault();
            }

            return secondPlayerCard;
        }

        public bool PlayTurnWhileClosed(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context)
        {
            Card firstPlayerCard = this.PlayFirstClosed(firstPlayerHandThisTurn, secondPlayerHandThisTurn, pointsFirstPlayer, pointsSecondPlayer, context);

            Card secondPlayerCard = this.PlaySecondClosed(firstPlayerHandThisTurn, secondPlayerHandThisTurn, pointsFirstPlayer, pointsSecondPlayer, context, firstPlayerCard);

            bool firstIsWinner = this.IfFirstPlayerIsWinner(firstPlayerCard, secondPlayerCard, context);
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
            this.playedCardsInCurrentContext.Add(firstPlayerCard);
            this.playedCardsInCurrentContext.Add(secondPlayerCard);

            return firstIsWinner;
        }

        public Card PlayFirstClosed(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context)
        {
            Card firstPlayerCard = firstPlayerHandThisTurn.FirstOrDefault();

            ////play the first player
            int announceValue = this.Announce20or40(firstPlayerHandThisTurn, context);
            if (announceValue > 0)
            {
                pointsFirstPlayer += announceValue;
                firstPlayerCard = this.announces.Dequeue();
            }
            else
            {
                var possibleWinning = this.WinningCards(firstPlayerHandThisTurn, context, this.playedCardsInCurrentContext.ToList());
                if (possibleWinning.Count() > 0)
                {
                    firstPlayerCard = possibleWinning.Dequeue();
                }
                else
                {
                    firstPlayerCard = firstPlayerHandThisTurn.FirstOrDefault();
                }
            }

            return firstPlayerCard;
        }

        public Card PlaySecondClosed(IList<Card> firstPlayerHandThisTurn, IList<Card> secondPlayerHandThisTurn, int pointsFirstPlayer, int pointsSecondPlayer, PlayerTurnContext context, Card firstPlayerCard)
        {
            Card secondPlayerCard;

            ////play th second player choice
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
                ////us trumps
                var trumps = secondPlayerHandThisTurn.Where(x => x.Suit == context.TrumpCard.Suit);
                if (trumps.Count() > 0)
                {
                    secondPlayerCard = trumps.OrderBy(x => x.GetValue()).ElementAt(0);
                }
                else
                {
                    secondPlayerCard = secondPlayerHandThisTurn.FirstOrDefault();
                }
            }

            return secondPlayerCard;
        }

        public bool IfFirstPlayerIsWinner(Card first, Card second, PlayerTurnContext context)
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

        public int Announce20or40(IList<Card> currentHand, PlayerTurnContext context)
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
                                this.announces.Enqueue(card);

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

        public Queue<Card> WinningCards(IList<Card> currentHand, PlayerTurnContext context, IList<Card> playedCardsInCurrentContext)
        {
            Queue<Card> topCards = new Queue<Card>();
            topCards = this.GetListOfWinning(currentHand, CardSuit.Club, topCards);
            topCards = this.GetListOfWinning(currentHand, CardSuit.Spade, topCards);
            topCards = this.GetListOfWinning(currentHand, CardSuit.Diamond, topCards);
            topCards = this.GetListOfWinning(currentHand, CardSuit.Heart, topCards);

            return topCards;
        }

        public Queue<Card> GetListOfWinning(IList<Card> currentHand, CardSuit suit, Queue<Card> q)
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
            var cards = this.playedCardsInCurrentContext.Where(x => x.Suit == suit).ToList();
            foreach (var card in cards)
            {
                currentSuitCards.Remove(card);
            }

            currentSuitCards.OrderByDescending(x => x.GetValue()).ToList();
            if (currentSuitCards.Count() > 0)
            {
                if (currentHand.Contains(currentSuitCards.ElementAt(0)))
                {
                    q.Enqueue(currentSuitCards.First());
                }
            }

            return q;
        }

        public ICollection<Card> GetAllCards(ICollection<Card> myCards, ICollection<Card> playedCardsInCurrentContext, Card activeTrumpCard)
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

            foreach (var card in playedCardsInCurrentContext)
            {
                allCards.Remove(card);
            }

            return allCards;
        }
    }
}
