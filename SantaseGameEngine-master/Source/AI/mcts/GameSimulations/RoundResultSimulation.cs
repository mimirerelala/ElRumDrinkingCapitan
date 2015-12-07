namespace Santase.AI.RumDrinkingCapitanPlayer.GameSimulations
{
    internal class RoundResultSimulation
    {
        public RoundResultSimulation(RoundPlayerInfoSimulation firstPlayer, RoundPlayerInfoSimulation secondPlayer)
        {
            this.FirstPlayer = firstPlayer;
            this.SecondPlayer = secondPlayer;
        }

        public RoundPlayerInfoSimulation FirstPlayer { get; }

        public RoundPlayerInfoSimulation SecondPlayer { get; }
    }
}
