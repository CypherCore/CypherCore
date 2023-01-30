using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player delete failed
    public interface IPlayerOnFailedDelete : IScriptObject
    {
        void OnFailedDelete(ObjectGuid guid, uint accountId);
    }
}