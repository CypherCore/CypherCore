using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    public interface IPlayerOnAfterModifyPower : IScriptObject
    {
        void OnAfterModifyPower(Player player, PowerType power, int oldValue, int newValue, bool regen);
    }
}
