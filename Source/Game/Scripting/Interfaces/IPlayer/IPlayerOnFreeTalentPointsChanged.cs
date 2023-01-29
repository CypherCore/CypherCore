using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player's free talent points change (right before the change is applied);
    public interface IPlayerOnFreeTalentPointsChanged : IScriptObject
    {
        void OnFreeTalentPointsChanged(Player player, uint points);
    }
}