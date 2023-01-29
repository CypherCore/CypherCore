using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player is created.
    public interface IPlayerOnCreate : IScriptObject
    {
        void OnCreate(Player player);
    }
}