using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // 8232 - Windfury Weapon
    [SpellScript(8232)]
    public class spell_shaman_windfury_weapon : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return Global.SpellMgr.GetSpellInfo(ShamanSpells.SPELL_SHAMAN_WINDFURY_WEAPON_PASSIVE, Difficulty.None) != null;
        }

        private void HandleDummy(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                AuraEffect auraEffect = caster.GetAuraEffect(ShamanSpells.SPELL_SHAMAN_WINDFURY_WEAPON_PASSIVE, 0);
                if (auraEffect != null)
                {
                    auraEffect.SetAmount(GetEffectValue());
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }
}
