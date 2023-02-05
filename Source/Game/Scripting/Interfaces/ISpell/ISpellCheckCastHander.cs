using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellCheckCast : ISpellScript
    {
        SpellCastResult CheckCast();
    }
}