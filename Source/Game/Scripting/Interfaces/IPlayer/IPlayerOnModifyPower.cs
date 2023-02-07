using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    public interface IPlayerOnModifyPower : IScriptObject
    {
        void OnModifyPower(Player player, PowerType power, int oldValue, ref int newValue, bool regen);
    }
}
