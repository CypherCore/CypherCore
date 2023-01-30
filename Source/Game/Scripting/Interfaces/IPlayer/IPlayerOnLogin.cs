using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player logs in.
    public interface IPlayerOnLogin : IScriptObject
    {
        void OnLogin(Player player);
    }
}