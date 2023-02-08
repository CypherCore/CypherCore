// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
namespace Scripts.Spells.Warrior
{
    // 12328, 18765, 35429 - Sweeping Strikes
    [Script]
    internal class spell_warr_sweeping_strikes : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        private Unit _procTarget;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WarriorSpells.SWEEPING_STRIKES_EXTRA_ATTACK_1, WarriorSpells.SWEEPING_STRIKES_EXTRA_ATTACK_2);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            _procTarget = eventInfo.GetActor().SelectNearbyTarget(eventInfo.GetProcTarget());

            return _procTarget;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo != null)
            {
                SpellInfo spellInfo = damageInfo.GetSpellInfo();

                if (spellInfo != null &&
                    (spellInfo.Id == WarriorSpells.BLADESTORM_PERIODIC_WHIRLWIND || (spellInfo.Id == WarriorSpells.EXECUTE && !_procTarget.HasAuraState(AuraStateType.Wounded20Percent))))
                {
                    // If triggered by Execute (while Target is not under 20% hp) or Bladestorm deals normalized weapon Damage
                    GetTarget().CastSpell(_procTarget, WarriorSpells.SWEEPING_STRIKES_EXTRA_ATTACK_2, new CastSpellExtraArgs(aurEff));
                }
                else
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage());
                    GetTarget().CastSpell(_procTarget, WarriorSpells.SWEEPING_STRIKES_EXTRA_ATTACK_1, args);
                }
            }
        }
    }
}