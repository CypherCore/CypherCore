using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called in Spell.Cast.
    public interface IPlayerOnSpellCast : IScriptObject
    {
        void OnSpellCast(Player player, Spell spell, bool skipCheck);
    }
}