// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // 53651 - Beacon of Light Proc / Beacon of Faith (proc aura) - 177173
    [SpellScript(new uint[] { 53651, 177173 })]
    public class spell_pal_beacon_of_light_proc : AuraScript, IHasAuraEffects, IAuraCheckProc
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private int GetPctBySpell(uint spellID)
        {
            int pct = 0;

            switch (spellID)
            {
                case PaladinSpells.ARCING_LIGHT_HEAL: // Light's Hammer
                case PaladinSpells.HolyPrismTargetAlly: // Holy Prism
                case PaladinSpells.LIGHT_OF_DAWN: // Light of Dawn
                    pct = 15; // 15% heal from these spells
                    break;
                default:
                    pct = 40; // 40% heal from all other heals
                    break;
            }

            return pct;
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit ownerOfBeacon = GetTarget();
            Unit targetOfBeacon = GetCaster();
            Unit targetOfHeal = eventInfo.GetActionTarget();

            //if (eventInfo.GetSpellInfo() && eventInfo.GetSpellInfo()->Id != BEACON_OF_LIGHT_HEAL && eventInfo.GetSpellInfo()->Id != LIGHT_OF_THE_MARTYR && targetOfBeacon->IsWithinLOSInMap(ownerOfBeacon) && targetOfHeal->GetGUID() != targetOfBeacon->GetGUID())
            return true;

            return false;
        }

        private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            bool auraCheck = false;
            Unit ownerOfBeacon = GetTarget();
            Unit targetOfBeacon = GetCaster();

            if (targetOfBeacon == null)
            {
                return;
            }

            HealInfo healInfo = eventInfo.GetHealInfo();

            if (healInfo == null)
            {
                return;
            }

            var bp = MathFunctions.CalculatePct(healInfo.GetHeal(), GetPctBySpell(GetSpellInfo().Id));

            if (GetSpellInfo().Id == PaladinSpells.BEACON_OF_LIGHT_PROC_AURA && (targetOfBeacon.HasAura(PaladinSpells.BeaconOfLight) || targetOfBeacon.HasAura(PaladinSpells.BEACON_OF_VIRTUE)))
            {
                ownerOfBeacon.CastSpell(targetOfBeacon, PaladinSpells.BeaconOfLightHeal, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp));
                auraCheck = true;
            }

            if ((GetSpellInfo().Id == PaladinSpells.BEACON_OF_FAITH_PROC_AURA && targetOfBeacon.HasAura(PaladinSpells.BEACON_OF_FAITH)))
            {
                bp /= 2;
                ownerOfBeacon.CastSpell(targetOfBeacon, PaladinSpells.BeaconOfLightHeal, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp));
                auraCheck = true;
            }

            if (!auraCheck)
            {
                ownerOfBeacon.RemoveAura(GetSpellInfo().Id);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
