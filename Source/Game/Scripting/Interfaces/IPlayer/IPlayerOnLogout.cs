using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player logs out.
    public interface IPlayerOnLogout : IScriptObject
    {
        void OnLogout(Player player);
    }
}