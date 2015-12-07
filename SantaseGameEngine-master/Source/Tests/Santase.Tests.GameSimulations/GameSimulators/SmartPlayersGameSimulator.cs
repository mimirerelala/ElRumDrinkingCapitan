namespace Santase.Tests.GameSimulations.GameSimulators
{
    using AI.DummyPlayer;
    using AI.RumDrinkingCapitanPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    // ReSharper disable once UnusedMember.Global
    public class SmartPlayersGameSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new ELRumDrinkingCapitanPlayer(); // new PlayerWithLoggerDecorator(new SmartPlayer(), new ConsoleLogger("[-]"))
            IPlayer secondPlayer = new SmartPlayer();
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
