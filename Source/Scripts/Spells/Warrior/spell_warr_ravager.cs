using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
    // Ravager - 152277
    // Ravager - 228920
    [SpellScript(new uint[] { 152277, 228920 })]
    public class spell_warr_ravager : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();

        private void HandleOnHit(uint UnnamedParameter)
        {
            WorldLocation dest = GetExplTargetDest();
            if (dest != null)
            {
                GetCaster().CastSpell(dest.GetPosition(), WarriorSpells.RAVAGER_SUMMON, true);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }
    }
}
