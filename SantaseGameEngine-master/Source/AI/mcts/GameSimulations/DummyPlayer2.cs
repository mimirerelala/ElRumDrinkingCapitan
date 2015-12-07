namespace Santase.AI.RumDrinkingCapitanPlayer.GameSimulations
{
    using System.Linq;

    using Santase.Logic.Extensions;
    using Santase.Logic.Players;
    using Logic.Cards;
    using System.Collections.Generic;

    internal class DummyPlayer2 : BasePlayer
    {
        public DummyPlayer2(ICollection<Card> cards, Card preferedCard = null)
        {
            this.PreferedCard = preferedCard;
            this.AddCards(cards);
        }

        public Card PreferedCard { get; set; }

        public override string Name => "Dummy";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);

            if (this.PreferedCard != null)
            {
                this.PlayCard(this.PreferedCard);
                this.PreferedCard = null;
            }

            var shuffledCards = possibleCardsToPlay.Shuffle();
            var cardToPlay = shuffledCards.First();
            return this.PlayCard(cardToPlay);
        }

        private void AddCards(ICollection<Card> cards)
        {
            foreach (var card in cards)
            {
                this.Cards.Add(card);
            }
        }
    }
}
