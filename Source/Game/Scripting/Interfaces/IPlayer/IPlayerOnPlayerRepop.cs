using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player presses release when he died
    public interface IPlayerOnPlayerRepop : IScriptObject
    {
        void OnPlayerRepop(Player player);
    }
}