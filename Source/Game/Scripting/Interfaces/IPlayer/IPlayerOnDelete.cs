using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player is deleted.
    public interface IPlayerOnDelete : IScriptObject
    {
        void OnDelete(ObjectGuid guid, uint accountId);
    }
}