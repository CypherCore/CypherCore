using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a Duel is requested
    public interface IPlayerOnDuelRequest : IScriptObject
    {
        void OnDuelRequest(Player target, Player challenger);
    }
}

