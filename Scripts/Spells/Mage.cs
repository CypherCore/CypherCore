/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Mage
{
    struct SpellIds
    {
        public const uint BlazingBarrierTrigger = 235314;
        public const uint ColdSnap = 11958;
        public const uint ConjureRefreshment = 116136;
        public const uint ConjureRefreshmentTable = 167145;
        public const uint FingersOfFrost = 44544;
        public const uint FocusMagicProc = 54648;
        public const uint FrostNova = 122;
        public const uint ImprovedPolymorphRank1 = 11210;
        public const uint ImprovedPolymorphStunRank1 = 83046;
        public const uint ImprovedPolymorphMarker = 87515;
        public const uint Ignite = 12654;
        public const uint ManaSurge = 37445;
        public const uint MasterOfElementsEnergize = 29077;
        public const uint Permafrost = 91394;
        public const uint Slow = 31589;
        public const uint SquirrelForm = 32813;
        public const uint GiraffeForm = 32816;
        public const uint SerpentForm = 32817;
        public const uint DragonhawkForm = 32818;
        public const uint WorgenForm = 32819;
        public const uint SheepForm = 32820;

        public const uint ConeOfColdAuraR1 = 11190;
        public const uint ConeOfColdAuraR2 = 12489;
        public const uint ConeOfColdTriggerR1 = 83301;
        public const uint ConeOfColdTriggerR2 = 83302;

        public const uint RingOfFrostSummon = 82676;
        public const uint RingOfFrostFreeze = 82691;
        public const uint RingOfFrostDummy = 91264;

        public const uint TemporalDisplacement = 80354;

        public const uint Chilled = 205708;

        //Misc
        public const uint HunterInsanity = 95809;
        public const uint PriestShadowWordDeath = 32409;
        public const uint ShamanExhaustion = 57723;
        public const uint ShamanSated = 57724;
    }

    [Script] // 235313 - Blazing Barrier
    class spell_mage_blazing_barrier : SpellScriptLoader
    {
        public spell_mage_blazing_barrier() : base("spell_mage_blazing_barrier") { }

        class spell_mage_blazing_barrier_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.BlazingBarrierTrigger);
            }

            void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
            {
                canBeRecalculated = false;
                Unit caster = GetCaster();
                if (caster)
                    amount = (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
            }

            void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();
                Unit caster = eventInfo.GetDamageInfo().GetVictim();
                Unit target = eventInfo.GetDamageInfo().GetAttacker();

                if (caster && target)
                    caster.CastSpell(target, SpellIds.BlazingBarrierTrigger, true);
            }

            public override void Register()
            {
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
                OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.ProcTriggerSpell));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_blazing_barrier_AuraScript();
        }
    }

    [Script] // 198063 - Burning Determination
    class spell_mage_burning_determination : SpellScriptLoader
    {
        public spell_mage_burning_determination() : base("spell_mage_burning_determination") { }

        class spell_mage_burning_determination_AuraScript : AuraScript
        {
            bool CheckProc(ProcEventInfo eventInfo)
            {
                SpellInfo spellInfo = eventInfo.GetSpellInfo();
                if (spellInfo != null)
                    if (spellInfo.GetAllEffectsMechanicMask().HasAnyFlag((uint)((1 << (int)Mechanics.Interrupt) | (1 << (int)Mechanics.Silence))))
                        return true;

                return false;
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_burning_determination_AuraScript();
        }
    }

    // 11958 - Cold Snap
    [Script]
    class spell_mage_cold_snap : SpellScriptLoader
    {
        public spell_mage_cold_snap() : base("spell_mage_cold_snap") { }

        class spell_mage_cold_snap_SpellScript : SpellScript
        {
            public override bool Load()
            {
                return GetCaster().IsTypeId(TypeId.Player);
            }

            void HandleDummy(uint effIndex)
            {
                GetCaster().GetSpellHistory().ResetCooldowns(p =>
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(p.Key);
                    return spellInfo.SpellFamilyName == SpellFamilyNames.Mage && spellInfo.GetSchoolMask().HasAnyFlag(SpellSchoolMask.Frost) &&
                           spellInfo.Id != SpellIds.ColdSnap && spellInfo.GetRecoveryTime() > 0;
                }, true);
            }

            public override void Register()
            {
                OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_mage_cold_snap_SpellScript();
        }
    }
    
    // Updated 4.3.4
    [Script] // 120 - Cone of Cold
    class spell_mage_cone_of_cold : SpellScriptLoader
    {
        public spell_mage_cone_of_cold() : base("spell_mage_cone_of_cold") { }

        class spell_mage_cone_of_cold_SpellScript : SpellScript
        {
            void HandleConeOfColdScript(uint effIndex)
            {
                Unit caster = GetCaster();
                Unit unitTarget = GetHitUnit();
                if (unitTarget)
                {
                    if (caster.HasAura(SpellIds.ConeOfColdAuraR1)) // Improved Cone of Cold Rank 1
                        unitTarget.CastSpell(unitTarget, SpellIds.ConeOfColdTriggerR1, true);
                    else if (caster.HasAura(SpellIds.ConeOfColdAuraR2)) // Improved Cone of Cold Rank 2
                        unitTarget.CastSpell(unitTarget, SpellIds.ConeOfColdTriggerR2, true);
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleConeOfColdScript, 0, SpellEffectName.ApplyAura));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_mage_cone_of_cold_SpellScript();
        }
    }

    
    [Script] // 190336 - Conjure Refreshment
    class spell_mage_conjure_refreshment : SpellScriptLoader
    {
        public spell_mage_conjure_refreshment() : base("spell_mage_conjure_refreshment") { }

        class spell_mage_conjure_refreshment_SpellScript : SpellScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.ConjureRefreshment, SpellIds.ConjureRefreshmentTable);
            }

            void HandleDummy(uint effIndex)
            {
                Player caster = GetCaster().ToPlayer();
                if (caster)
                {
                    Group group = caster.GetGroup();
                    if (group)
                        caster.CastSpell(caster, SpellIds.ConjureRefreshmentTable, true);
                    else
                        caster.CastSpell(caster, SpellIds.ConjureRefreshment, true);
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_mage_conjure_refreshment_SpellScript();
        }
    }

    // 54646 - Focus Magic
    [Script]
    class spell_mage_focus_magic : SpellScriptLoader
    {
        public spell_mage_focus_magic() : base("spell_mage_focus_magic") { }

        class spell_mage_focus_magic_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.FocusMagicProc);
            }

            public override bool Load()
            {
                _procTarget = null;
                return true;
            }

            bool CheckProc(ProcEventInfo eventInfo)
            {
                _procTarget = GetCaster();
                return _procTarget && _procTarget.IsAlive();
            }

            void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();
                GetTarget().CastSpell(_procTarget, SpellIds.FocusMagicProc, true, null, aurEff);
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
                OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ModSpellCritChance));
            }

            Unit _procTarget;
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_focus_magic_AuraScript();
        }
    }

    [Script] // 195283 - Hot Streak
    class spell_mage_hot_streak : SpellScriptLoader
    {
        public spell_mage_hot_streak() : base("spell_mage_hot_streak") { }

        class spell_mage_hot_streak_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return true;
            }

            void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
            }

            public override void Register()
            {
                OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_hot_streak_AuraScript();
        }
    }

    // 56374 - Glyph of Icy Veins
    [Script]
    class spell_mage_glyph_of_icy_veins : SpellScriptLoader
    {
        public spell_mage_glyph_of_icy_veins() : base("spell_mage_glyph_of_icy_veins") { }

        class spell_mage_glyph_of_icy_veins_AuraScript : AuraScript
        {
            void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();

                GetTarget().RemoveAurasByType(AuraType.HasteSpells, ObjectGuid.Empty, null, true, false);
                GetTarget().RemoveAurasByType(AuraType.ModDecreaseSpeed);
            }

            public override void Register()
            {
                OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_glyph_of_icy_veins_AuraScript();
        }
    }

    // 56375 - Glyph of Polymorph
    [Script]
    class spell_mage_glyph_of_polymorph : SpellScriptLoader
    {
        public spell_mage_glyph_of_polymorph() : base("spell_mage_glyph_of_polymorph") { }

        class spell_mage_glyph_of_polymorph_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.PriestShadowWordDeath);
            }

            void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();
                Unit target = eventInfo.GetProcTarget();

                target.RemoveAurasByType(AuraType.PeriodicDamage, ObjectGuid.Empty, target.GetAura(SpellIds.PriestShadowWordDeath)); // SW:D shall not be removed.
                target.RemoveAurasByType(AuraType.PeriodicDamagePercent);
                target.RemoveAurasByType(AuraType.PeriodicLeech);
            }

            public override void Register()
            {
                OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_glyph_of_polymorph_AuraScript();
        }
    }

    // 37447 - Improved Mana Gems
    [Script] // 61062 - Improved Mana Gems
    class spell_mage_imp_mana_gems : SpellScriptLoader
    {
        public spell_mage_imp_mana_gems() : base("spell_mage_imp_mana_gems") { }

        class spell_mage_imp_mana_gems_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.ManaSurge);
            }

            void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();
                eventInfo.GetActor().CastSpell((Unit)null, SpellIds.ManaSurge, true);
            }

            public override void Register()
            {
                OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.OverrideClassScripts));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_imp_mana_gems_AuraScript();
        }
    }

    // 44457 - Living Bomb
    [Script]
    class spell_mage_living_bomb : SpellScriptLoader
    {
        public spell_mage_living_bomb() : base("spell_mage_living_bomb") { }

        class spell_mage_living_bomb_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo((uint)spellInfo.GetEffect(1).CalcValue());
            }

            void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                AuraRemoveMode removeMode = GetTargetApplication().GetRemoveMode();
                if (removeMode != AuraRemoveMode.EnemySpell && removeMode != AuraRemoveMode.Expire)
                    return;
                Unit caster = GetCaster();
                if (caster)
                    caster.CastSpell(GetTarget(), (uint)aurEff.GetAmount(), true, null, aurEff);
            }

            public override void Register()
            {
                AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_living_bomb_AuraScript();
        }
    }
    
    [Script] // 11426 - Ice Barrier
    class spell_mage_ice_barrier : SpellScriptLoader
    {
        public spell_mage_ice_barrier() : base("spell_mage_ice_barrier") { }

        class spell_mage_ice_barrier_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellEntry)
            {
                return ValidateSpellInfo(SpellIds.Chilled);
            }

            void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
            {
                canBeRecalculated = false;
                Unit caster = GetCaster();
                if (caster)
                    amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 10.0f);
            }

            void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                Unit caster = eventInfo.GetDamageInfo().GetVictim();
                Unit target = eventInfo.GetDamageInfo().GetAttacker();

                if (caster && target)
                    caster.CastSpell(target, SpellIds.Chilled, true);
            }

            public override void Register()
            {
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
                OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.SchoolAbsorb));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_ice_barrier_AuraScript();
        }
    }

    // -11119 - Ignite
    [Script]
    class spell_mage_ignite : SpellScriptLoader
    {
        public spell_mage_ignite() : base("spell_mage_ignite") { }

        class spell_mage_ignite_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.Ignite);
            }

            bool CheckProc(ProcEventInfo eventInfo)
            {
                return eventInfo.GetProcTarget();
            }

            void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();

                SpellInfo igniteDot = Global.SpellMgr.GetSpellInfo(SpellIds.Ignite);
                int pct = 8 * GetSpellInfo().GetRank();

                int amount = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), pct) / igniteDot.GetMaxTicks(Difficulty.None));
                amount += (int)eventInfo.GetProcTarget().GetRemainingPeriodicAmount(eventInfo.GetActor().GetGUID(), SpellIds.Ignite, AuraType.PeriodicDamage);
                GetTarget().CastCustomSpell(SpellIds.Ignite, SpellValueMod.BasePoint0, amount, eventInfo.GetProcTarget(), true, null, aurEff);
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
                OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_ignite_AuraScript();
        }
    }

    // -29074 - Master of Elements
    [Script]
    class spell_mage_master_of_elements : SpellScriptLoader
    {
        public spell_mage_master_of_elements() : base("spell_mage_master_of_elements") { }

        class spell_mage_master_of_elements_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.MasterOfElementsEnergize);
            }

            bool CheckProc(ProcEventInfo eventInfo)
            {
                return eventInfo.GetDamageInfo().GetSpellInfo() != null; // eventInfo.GetSpellInfo()
            }

            void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();

                var costs = eventInfo.GetDamageInfo().GetSpellInfo().CalcPowerCost(GetTarget(), eventInfo.GetDamageInfo().GetSchoolMask());
                var m = costs.Find(cost => cost.Power == PowerType.Mana);
                if (m != null)
                {
                    int mana = MathFunctions.CalculatePct(m.Amount, aurEff.GetAmount());
                    if (mana > 0)
                        GetTarget().CastCustomSpell(SpellIds.MasterOfElementsEnergize, SpellValueMod.BasePoint0, mana, GetTarget(), true, null, aurEff);
                }
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
                OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_master_of_elements_AuraScript();
        }
    }

    // 86181 - Nether Vortex
    [Script]
    class spell_mage_nether_vortex : SpellScriptLoader
    {
        public spell_mage_nether_vortex() : base("spell_mage_nether_vortex") { }

        class spell_mage_nether_vortex_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.Slow);
            }

            bool DoCheck(ProcEventInfo eventInfo)
            {
                Aura aura = eventInfo.GetProcTarget().GetAura(SpellIds.Slow);
                if (aura != null)
                    if (aura.GetCasterGUID() != GetTarget().GetGUID())
                        return false;

                return true;
            }

            void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();
                GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.Slow, true, null, aurEff);
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(DoCheck));
                OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_nether_vortex_AuraScript();
        }
    }

    // -11175 - Permafrost
    [Script]
    class spell_mage_permafrost : SpellScriptLoader
    {
        public spell_mage_permafrost() : base("spell_mage_permafrost") { }

        class spell_mage_permafrost_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.Permafrost);
            }

            bool DoCheck(ProcEventInfo eventInfo)
            {
                return GetTarget().GetGuardianPet() && eventInfo.GetDamageInfo().GetDamage() != 0;
            }

            void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();

                int heal = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()));
                GetTarget().CastCustomSpell(SpellIds.Permafrost, SpellValueMod.BasePoint0, heal, null, true, null, aurEff);
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(DoCheck));
                OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_permafrost_AuraScript();
        }
    }

    // 118 - Polymorph
    [Script]
    class spell_mage_polymorph : SpellScriptLoader
    {
        public spell_mage_polymorph() : base("spell_mage_polymorph") { }

        class spell_mage_polymorph_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.ImprovedPolymorphRank1, SpellIds.ImprovedPolymorphStunRank1, SpellIds.ImprovedPolymorphMarker);
            }

            public override bool Load()
            {
                _caster = null;
                return true;
            }

            bool DoCheck(ProcEventInfo eventInfo)
            {
                _caster = GetCaster();
                return _caster && eventInfo.GetDamageInfo() != null;
            }

            void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();
                // Improved Polymorph
                AuraEffect improvedPolymorph = _caster.GetAuraEffectOfRankedSpell(SpellIds.ImprovedPolymorphRank1, 0);
                if (improvedPolymorph != null)
                {
                    if (_caster.HasAura(SpellIds.ImprovedPolymorphMarker))
                        return;

                    GetTarget().CastSpell(GetTarget(), Global.SpellMgr.GetSpellWithRank(SpellIds.ImprovedPolymorphStunRank1, improvedPolymorph.GetSpellInfo().GetRank()), true, null, aurEff);
                    _caster.CastSpell(_caster, SpellIds.ImprovedPolymorphMarker, true, null, aurEff);
                }
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(DoCheck));
                OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.ModConfuse));
            }

            Unit _caster;
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_polymorph_AuraScript();
        }
    }

    // @todo move out of here and rename - not a mage spell
    // 32826 - Polymorph (Visual)
    [Script]
    class spell_mage_polymorph_cast_visual : SpellScriptLoader
    {
        public spell_mage_polymorph_cast_visual() : base("spell_mage_polymorph_visual") { }

        const uint NPC_AUROSALIA = 18744;

        class spell_mage_polymorph_cast_visual_SpellScript : SpellScript
        {
            uint[] PolymorhForms =
            {
                SpellIds.SquirrelForm,
                SpellIds.GiraffeForm,
                SpellIds.SerpentForm,
                SpellIds.DragonhawkForm,
                SpellIds.WorgenForm,
                SpellIds.SheepForm
            };

            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(PolymorhForms);
            }

            void HandleDummy(uint effIndex)
            {
                Unit target = GetCaster().FindNearestCreature(NPC_AUROSALIA, 30.0f);
                if (target)
                    if (target.IsTypeId(TypeId.Unit))
                        target.CastSpell(target, PolymorhForms[RandomHelper.IRand(0, 5)], true);
            }

            public override void Register()
            {
                // add dummy effect spell handler to Polymorph visual
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_mage_polymorph_cast_visual_SpellScript();
        }
    }

    [Script] // 235450 - Prismatic Barrier
    class spell_mage_prismatic_barrier : SpellScriptLoader
    {
        public spell_mage_prismatic_barrier() : base("spell_mage_prismatic_barrier") { }

        class spell_mage_prismatic_barrier_AuraScript : AuraScript
        {
            void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
            {
                canBeRecalculated = false;
                Unit caster = GetCaster();
                if (caster)
                    amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
            }

            public override void Register()
            {
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_prismatic_barrier_AuraScript();
        }
    }

    // 82676 - Ring of Frost
    // Updated 4.3.4
    [Script]
    class spell_mage_ring_of_frost : SpellScriptLoader
    {
        public spell_mage_ring_of_frost() : base("spell_mage_ring_of_frost") { }

        class spell_mage_ring_of_frost_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.RingOfFrostSummon, SpellIds.RingOfFrostFreeze, SpellIds.RingOfFrostDummy);
            }

            public override bool Load()
            {
                ringOfFrost = null;
                return true;
            }

            void HandleEffectPeriodic(AuraEffect aurEff)
            {
                if (ringOfFrost)
                    if (GetMaxDuration() - (int)ringOfFrost.GetTimer() >= Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostDummy).GetDuration())
                        GetTarget().CastSpell(ringOfFrost.GetPositionX(), ringOfFrost.GetPositionY(), ringOfFrost.GetPositionZ(), SpellIds.RingOfFrostFreeze, true);
            }

            void Apply(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                List<TempSummon> MinionList = new List<TempSummon>();
                GetTarget().GetAllMinionsByEntry(MinionList, (uint)GetSpellInfo().GetEffect(0).MiscValue);

                // Get the last summoned RoF, save it and despawn older ones
                foreach (var creature in MinionList)
                {
                    TempSummon summon = creature.ToTempSummon();

                    if (ringOfFrost && summon)
                    {
                        if (summon.GetTimer() > ringOfFrost.GetTimer())
                        {
                            ringOfFrost.DespawnOrUnsummon();
                            ringOfFrost = summon;
                        }
                        else
                            summon.DespawnOrUnsummon();
                    }
                    else if (summon)
                        ringOfFrost = summon;
                }
            }

            TempSummon ringOfFrost;

            public override void Register()
            {
                OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.ProcTriggerSpell));
                OnEffectApply.Add(new EffectApplyHandler(Apply, 1, AuraType.ProcTriggerSpell, AuraEffectHandleModes.RealOrReapplyMask));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_ring_of_frost_AuraScript();
        }
    }

    // 82691 - Ring of Frost (freeze efect)
    // Updated 4.3.4
    [Script]
    class spell_mage_ring_of_frost_freeze : SpellScriptLoader
    {
        public spell_mage_ring_of_frost_freeze() : base("spell_mage_ring_of_frost_freeze") { }

        class spell_mage_ring_of_frost_freeze_SpellScript : SpellScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.RingOfFrostSummon, SpellIds.RingOfFrostFreeze);
            }

            void FilterTargets(List<WorldObject> targets)
            {
                float outRadius = Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostSummon).GetEffect(0).CalcRadius();
                float inRadius = 4.7f;

                foreach (var obj in targets.ToList())
                {
                    Unit unit = obj.ToUnit();
                    if (unit)
                        if (unit.HasAura(SpellIds.RingOfFrostDummy) || unit.HasAura(SpellIds.RingOfFrostFreeze) || unit.GetExactDist(GetExplTargetDest()) > outRadius || unit.GetExactDist(GetExplTargetDest()) < inRadius)
                            targets.Remove(obj);
                }
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_mage_ring_of_frost_freeze_SpellScript();
        }

        class spell_mage_ring_of_frost_freeze_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.RingOfFrostDummy);
            }

            void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                    if (GetCaster())
                        GetCaster().CastSpell(GetTarget(), SpellIds.RingOfFrostDummy, true);
            }

            public override void Register()
            {
                AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_mage_ring_of_frost_freeze_AuraScript();
        }
    }

    // 80353 - Time Warp
    [Script]
    class spell_mage_time_warp : SpellScriptLoader
    {
        public spell_mage_time_warp() : base("spell_mage_time_warp") { }

        class spell_mage_time_warp_SpellScript : SpellScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.TemporalDisplacement, SpellIds.HunterInsanity, SpellIds.ShamanExhaustion, SpellIds.ShamanSated);
            }

            void RemoveInvalidTargets(List<WorldObject> targets)
            {
                targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.TemporalDisplacement));
                targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.HunterInsanity));
                targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.ShamanExhaustion));
                targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.ShamanSated));
            }

            void ApplyDebuff()
            {
                Unit target = GetHitUnit();
                if (target)
                    target.CastSpell(target, SpellIds.TemporalDisplacement, true);
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
                AfterHit.Add(new HitHandler(ApplyDebuff));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_mage_time_warp_SpellScript();
        }
    }

    [Script] //228597 - Frostbolt   84721  - Frozen Orb   190357 - Blizzard
    class spell_mage_trigger_chilled : SpellScriptLoader
    {
        public spell_mage_trigger_chilled() : base("spell_mage_trigger_chilled") { }

        class spell_mage_trigger_chilled_SpellScript : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(SpellIds.Chilled);
            }

            void HandleChilled()
            {
                Unit target = GetHitUnit();
                if (target)
                    GetCaster().CastSpell(target, SpellIds.Chilled, true);
            }

            public override void Register()
            {
                OnHit.Add(new HitHandler(HandleChilled));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_mage_trigger_chilled_SpellScript();
        }
    }
   
    [Script] // 33395 Water Elemental's Freeze
    class spell_mage_water_elemental_freeze : SpellScriptLoader
    {
        public spell_mage_water_elemental_freeze() : base("spell_mage_water_elemental_freeze") { }

        class spell_mage_water_elemental_freeze_SpellScript : SpellScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.FingersOfFrost);
            }

            void HandleImprovedFreeze()
            {
                Unit owner = GetCaster().GetOwner();
                if (!owner)
                    return;

                owner.CastSpell(owner, SpellIds.FingersOfFrost, true);
            }

            public override void Register()
            {
                AfterHit.Add(new HitHandler(HandleImprovedFreeze));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_mage_water_elemental_freeze_SpellScript();
        }
    }
}
