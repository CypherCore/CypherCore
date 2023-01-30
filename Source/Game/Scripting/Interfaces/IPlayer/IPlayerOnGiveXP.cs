using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player gains XP (before anything is given);
    public interface IPlayerOnGiveXP : IScriptObject
    {
        void OnGiveXP(Player player, ref uint amount, Unit victim);
    }
}