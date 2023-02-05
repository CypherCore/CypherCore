using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    /// Updated 8.3.7
    // 12975 - Last Stand
    [SpellScript(12975)]
    public class spell_warr_last_stand : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(WarriorSpells.LAST_STAND_TRIGGERED);
        }

        private void HandleDummy(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            CastSpellExtraArgs args = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)caster.CountPctFromMaxHealth(GetEffectValue()));
            caster.CastSpell(caster, WarriorSpells.LAST_STAND_TRIGGERED, args);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }
    }
}
