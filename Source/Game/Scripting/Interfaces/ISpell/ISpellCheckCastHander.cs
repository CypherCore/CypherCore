using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellCheckCastHander : ISpellScript
    {
        SpellCastResult CheckCast();
    }
}