using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a Duel ends
    public interface IPlayerOnDuelEnd : IScriptObject
    {
        void OnDuelEnd(Player winner, Player loser, DuelCompleteType type);

    }
}

