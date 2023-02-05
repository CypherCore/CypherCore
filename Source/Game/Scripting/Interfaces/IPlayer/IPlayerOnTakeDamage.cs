using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player takes damage
    public interface IPlayerOnTakeDamage : IScriptObject, IClassRescriction
    {
        void OnPlayerTakeDamage(Player player, uint amount, SpellSchoolMask schoolMask);
    }
}