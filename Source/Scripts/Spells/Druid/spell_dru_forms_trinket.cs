// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 37336 - Druid Forms Trinket
    internal class spell_dru_forms_trinket : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.FormsTrinketBear, DruidSpellIds.FormsTrinketCat, DruidSpellIds.FormsTrinketMoonkin, DruidSpellIds.FormsTrinketNone, DruidSpellIds.FormsTrinketTree);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActor();

            switch (target.GetShapeshiftForm())
            {
                case ShapeShiftForm.BearForm:
                case ShapeShiftForm.DireBearForm:
                case ShapeShiftForm.CatForm:
                case ShapeShiftForm.MoonkinForm:
                case ShapeShiftForm.None:
                case ShapeShiftForm.TreeOfLife:
                    return true;
                default:
                    break;
            }

            return false;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit target = eventInfo.GetActor();
            uint triggerspell;

            switch (target.GetShapeshiftForm())
            {
                case ShapeShiftForm.BearForm:
                case ShapeShiftForm.DireBearForm:
                    triggerspell = DruidSpellIds.FormsTrinketBear;

                    break;
                case ShapeShiftForm.CatForm:
                    triggerspell = DruidSpellIds.FormsTrinketCat;

                    break;
                case ShapeShiftForm.MoonkinForm:
                    triggerspell = DruidSpellIds.FormsTrinketMoonkin;

                    break;
                case ShapeShiftForm.None:
                    triggerspell = DruidSpellIds.FormsTrinketNone;

                    break;
                case ShapeShiftForm.TreeOfLife:
                    triggerspell = DruidSpellIds.FormsTrinketTree;

                    break;
                default:
                    return;
            }

            target.CastSpell(target, triggerspell, new CastSpellExtraArgs(aurEff));
        }
    }
}