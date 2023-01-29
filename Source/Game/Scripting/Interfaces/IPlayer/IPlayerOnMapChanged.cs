using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player changes to a new map (after moving to new map);
    public interface IPlayerOnMapChanged : IScriptObject
    {
        void OnMapChanged(Player player);
    }
}