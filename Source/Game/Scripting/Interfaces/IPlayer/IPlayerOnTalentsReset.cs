using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player's talent points are reset (right before the reset is done);
    public interface IPlayerOnTalentsReset : IScriptObject
    {
        void OnTalentsReset(Player player, bool noCost);
    }
}