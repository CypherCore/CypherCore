// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(53651)] // 53651 - Beacon of Light
    internal class spell_pal_light_s_beacon : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.BeaconOfLight, PaladinSpells.BeaconOfLightHeal);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (!eventInfo.GetActionTarget())
                return false;

            if (eventInfo.GetActionTarget().HasAura(PaladinSpells.BeaconOfLight, eventInfo.GetActor().GetGUID()))
                return false;

            return true;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();

            if (healInfo == null ||
                healInfo.GetHeal() == 0)
                return;

            uint heal = MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());

            var auras = GetCaster().GetSingleCastAuras();

            foreach (var eff in auras)
                if (eff.GetId() == PaladinSpells.BeaconOfLight)
                {
                    List<AuraApplication> applications = eff.GetApplicationList();

                    if (!applications.Empty())
                    {
                        CastSpellExtraArgs args = new(aurEff);
                        args.AddSpellMod(SpellValueMod.BasePoint0, (int)heal);
                        eventInfo.GetActor().CastSpell(applications[0].GetTarget(), PaladinSpells.BeaconOfLightHeal, args);
                    }

                    return;
                }
        }
    }
}
