/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Framework.Dynamic;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Shaman
{
    struct SpellIds
    {
        public const uint AncestralGuidance = 108281;
        public const uint AncestralGuidanceHeal = 114911;
        public const uint ChainedHeal = 70809;
        public const uint CrashLightningCleave = 187878;
        public const uint EarthShieldHeal = 204290;
        public const uint EarthenRagePassive = 170374;
        public const uint EarthenRagePeriodic = 170377;
        public const uint EarthenRageDamage = 170379;
        public const uint Electrified = 64930;
        public const uint ElementalBlastCrit = 118522;
        public const uint ElementalBlastHaste = 173183;
        public const uint ElementalBlastMastery = 173184;
        public const uint ElementalMastery = 16166;
        public const uint EnergySurge = 40465;
        public const uint Exhaustion = 57723;
        public const uint FlameShock = 8050;
        public const uint FlameShockMaelstrom = 188389;
        public const uint FlametongueAttack = 10444;
        public const uint FlametongueWeaponEnchant = 334294;
        public const uint FlametongueWeaponAura = 319778;
        public const uint GatheringStorms = 198299;
        public const uint GatheringStormsBuff = 198300;
        public const uint HealingRainVisual = 147490;
        public const uint HealingRainHeal = 73921;
        public const uint ItemLightningShield = 23552;
        public const uint ItemLightningShieldDamage = 27635;
        public const uint ItemManaSurge = 23571;
        public const uint LavaBurst = 51505;
        public const uint LavaBurstBonusDamage = 71824;
        public const uint LavaSurge = 77762;
        public const uint LiquidMagmaHit = 192231;
        public const uint PathOfFlamesSpread = 210621;
        public const uint PathOfFlamesTalent = 201909;
        public const uint PowerSurge = 40466;
        public const uint Sated = 57724;
        public const uint TidalWaves = 53390;
        public const uint TotemicPowerMp5 = 28824;
        public const uint TotemicPowerSpellPower = 28825;
        public const uint TotemicPowerAttackPower = 28826;
        public const uint TotemicPowerArmor = 28827;
        public const uint WindfuryAttack = 25504;

        //Misc
        public const uint HunterInsanity = 95809;
        public const uint MageTemporalDisplacement = 80354;
        public const uint PetNetherwindsFatigued = 160455;
    }

    struct CreatureIds
    {
        public const uint HealingRainInvisibleStalker = 73400;
    }

    [Script] // 108281 - Ancestral Guidance
    class spell_sha_ancestral_guidance : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AncestralGuidanceHeal);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetHealInfo().GetSpellInfo().Id == SpellIds.AncestralGuidanceHeal)
                return false;
            return true;
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            int bp0 = MathFunctions.CalculatePct((int)eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
            if (bp0 != 0)
            {
                CastSpellExtraArgs args = new(aurEff);
                args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
                eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.AncestralGuidanceHeal, args);
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 114911 - Ancestral Guidance Heal
    class spell_sha_ancestral_guidance_heal : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AncestralGuidance);
        }

        void ResizeTargets(List<WorldObject> targets)
        {
            targets.Resize(3);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(ResizeTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    [Script] // 2825 - Bloodlust
    class spell_sha_bloodlust : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Sated, SpellIds.HunterInsanity, SpellIds.MageTemporalDisplacement, SpellIds.PetNetherwindsFatigued);
        }

        void RemoveInvalidTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.Sated));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.HunterInsanity));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.MageTemporalDisplacement));
        }

        void ApplyDebuff()
        {
            Unit target = GetHitUnit();
            if (target)
                target.CastSpell(target, SpellIds.Sated, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 0, Targets.UnitCasterAreaRaid));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 1, Targets.UnitCasterAreaRaid));
            AfterHit.Add(new HitHandler(ApplyDebuff));
        }
    }

    [Script] // 187874 - Crash Lightning
    class spell_sha_crash_lightning : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CrashLightningCleave);
        }

        void CountTargets(List<WorldObject> targets)
        {
            _targetsHit = targets.Count;
        }

        void TriggerCleaveBuff()
        {
            if (_targetsHit >= 2)
                GetCaster().CastSpell(GetCaster(), SpellIds.CrashLightningCleave, true);

            AuraEffect gatheringStorms = GetCaster().GetAuraEffect(SpellIds.GatheringStorms, 0);
            if (gatheringStorms != null)
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)(gatheringStorms.GetAmount() * _targetsHit));
                GetCaster().CastSpell(GetCaster(), SpellIds.GatheringStormsBuff, args);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitConeEnemy104));
            AfterCast.Add(new CastHandler(TriggerCleaveBuff));
        }

        int _targetsHit;
    }

    [Script] // 204288 - Earth Shield
    class spell_sha_earth_shield : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EarthShieldHeal);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetDamageInfo() == null || !HasEffect(1) || eventInfo.GetDamageInfo().GetDamage() < GetTarget().CountPctFromMaxHealth(GetEffect(1).GetAmount()))
                return false;
            return true;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            GetTarget().CastSpell(GetTarget(), SpellIds.EarthShieldHeal, new CastSpellExtraArgs(aurEff, GetCasterGUID()));
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    [Script] // 170374 - Earthen Rage (Passive)
    public class spell_sha_earthen_rage_passive : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EarthenRagePeriodic, SpellIds.EarthenRageDamage);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            _procTargetGuid = eventInfo.GetProcTarget().GetGUID();
            eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.EarthenRagePeriodic, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }

        public ObjectGuid GetProcTargetGuid()
        {
            return _procTargetGuid;
        }

        ObjectGuid _procTargetGuid;
    }

    [Script] // 170377 - Earthen Rage (Proc Aura)
    class spell_sha_earthen_rage_proc_aura : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EarthenRagePassive, SpellIds.EarthenRageDamage);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            Aura aura = GetCaster().GetAura(SpellIds.EarthenRagePassive);
            if (aura != null)
            {
                var earthen_rage_script = aura.GetScript<spell_sha_earthen_rage_passive>("spell_sha_earthen_rage_passive");
                if (earthen_rage_script != null)
                {
                    Unit procTarget = Global.ObjAccessor.GetUnit(GetCaster(), earthen_rage_script.GetProcTargetGuid());
                    if (procTarget)
                        GetTarget().CastSpell(procTarget, SpellIds.EarthenRageDamage, true);

                }
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 117014 - Elemental Blast
    class spell_sha_elemental_blast : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ElementalBlastCrit, SpellIds.ElementalBlastHaste, SpellIds.ElementalBlastMastery);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void TriggerBuff()
        {
            Player caster = GetCaster().ToPlayer();
            uint spellId = RandomHelper.RAND(SpellIds.ElementalBlastCrit, SpellIds.ElementalBlastHaste, SpellIds.ElementalBlastMastery);

            caster.CastSpell(caster, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }

        public override void Register()
        {
            AfterCast.Add(new CastHandler(TriggerBuff));
        }
    }

    [Script] // 318038 - Flametongue Weapon
    class spell_sha_flametongue_weapon : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlametongueWeaponEnchant);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleEffectHitTarget(uint index)
        {
            Player player = GetCaster().ToPlayer();
            byte slot = EquipmentSlot.MainHand;

            if (player.GetPrimarySpecialization() == (uint)TalentSpecialization.ShamanEnhancement)
                slot = EquipmentSlot.OffHand;

            Item targetItem = player.GetItemByPos(InventorySlots.Bag0, slot);
            if (targetItem == null || !targetItem.GetTemplate().IsWeapon())
                return;

            GetCaster().CastSpell(targetItem, SpellIds.FlametongueWeaponEnchant, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 319778 - Flametongue
    class spell_sha_flametongue_weapon_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlametongueAttack);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit attacker = eventInfo.GetActor();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, Math.Max(1, (int)(attacker.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.0264f)));
            attacker.CastSpell(eventInfo.GetActionTarget(), SpellIds.FlametongueAttack, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 73920 - Healing Rain
    class spell_sha_healing_rain : SpellScript
    {
        void InitializeVisualStalker()
        {
            Aura aura = GetHitAura();
            if (aura != null)
            {
                WorldLocation dest = GetExplTargetDest();
                if (dest != null)
                {
                    int duration = GetSpellInfo().CalcDuration(GetOriginalCaster());
                    TempSummon summon = GetCaster().GetMap().SummonCreature(CreatureIds.HealingRainInvisibleStalker, dest, null, (uint)duration, GetOriginalCaster());
                    if (summon == null)
                        return;

                    summon.CastSpell(summon, SpellIds.HealingRainVisual, true);

                    var script = aura.GetScript<spell_sha_healing_rain_AuraScript>("spell_sha_healing_rain");
                    if (script != null)
                        script.SetVisualDummy(summon);
                }
            }
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(InitializeVisualStalker));
        }
    }
    
    [Script] // 73920 - Healing Rain (Aura)
    class spell_sha_healing_rain_AuraScript : AuraScript
    {
        ObjectGuid _visualDummy;
        float _x;
        float _y;
        float _z;

        public void SetVisualDummy(TempSummon summon)
        {
            _visualDummy = summon.GetGUID();
            summon.GetPosition(out _x, out _y, out _z);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            GetTarget().CastSpell(new Position(_x, _y, _z), SpellIds.HealingRainHeal, new CastSpellExtraArgs(aurEff));
        }

        void HandleEffecRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature summon = ObjectAccessor.GetCreature(GetTarget(), _visualDummy);
            if (summon != null)
                summon.DespawnOrUnsummon();
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(HandleEffecRemoved, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.PeriodicDummy));
        }
    }
    
    [Script] // 52042 - Healing Stream Totem
    class spell_sha_healing_stream_totem_heal : SpellScript
    {
        void SelectTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(target => !target.ToUnit() || target.ToUnit().IsFullHealth());

            targets.RandomResize(1);

            if (targets.Empty())
                targets.Add(GetOriginalCaster());
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(SelectTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    [Script] // 32182 - Heroism
    class spell_sha_heroism : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhaustion, SpellIds.HunterInsanity, SpellIds.MageTemporalDisplacement, SpellIds.PetNetherwindsFatigued);
        }

        void RemoveInvalidTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.Exhaustion));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.HunterInsanity));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.MageTemporalDisplacement));
        }

        void ApplyDebuff()
        {
            Unit target = GetHitUnit();
            if (target)
                target.CastSpell(target, SpellIds.Exhaustion, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 0, Targets.UnitCasterAreaRaid));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 1, Targets.UnitCasterAreaRaid));
            AfterHit.Add(new HitHandler(ApplyDebuff));
        }
    }

    [Script] // 23551 - Lightning Shield T2 Bonus
    class spell_sha_item_lightning_shield : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemLightningShield);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.ItemLightningShield, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 23552 - Lightning Shield T2 Bonus
    class spell_sha_item_lightning_shield_trigger : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemLightningShieldDamage);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.ItemLightningShieldDamage, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 23572 - Mana Surge
    class spell_sha_item_mana_surge : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemManaSurge);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcSpell() != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            var costs = eventInfo.GetProcSpell().GetPowerCost();
            var m = costs.Find(cost => cost.Power == PowerType.Mana);
            if (m != null)
            {
                int mana = MathFunctions.CalculatePct(m.Amount, 35);
                if (mana > 0)
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, mana);
                    GetTarget().CastSpell(GetTarget(), SpellIds.ItemManaSurge, args);
                }
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 40463 - Shaman Tier 6 Trinket
    class spell_sha_item_t6_trinket : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EnergySurge, SpellIds.PowerSurge);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null)
                return;

            uint spellId;
            int chance;

            // Lesser Healing Wave
            if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000080u))
            {
                spellId = SpellIds.EnergySurge;
                chance = 10;
            }
            // Lightning Bolt
            else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000001u))
            {
                spellId = SpellIds.EnergySurge;
                chance = 15;
            }
            // Stormstrike
            else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000010u))
            {
                spellId = SpellIds.PowerSurge;
                chance = 50;
            }
            else
                return;

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell((Unit)null, spellId, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 70811 - Item - Shaman T10 Elemental 2P Bonus
    class spell_sha_item_t10_elemental_2p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ElementalMastery);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Player target = GetTarget().ToPlayer();
            if (target)
                target.GetSpellHistory().ModifyCooldown(SpellIds.ElementalMastery, -aurEff.GetAmount());
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 189063 - Lightning Vortex (proc 185881 Item - Shaman T18 Elemental 4P Bonus)
    class spell_sha_item_t18_elemental_4p_bonus : AuraScript
    {
        void DiminishHaste(AuraEffect aurEff)
        {
            PreventDefaultAction();
            AuraEffect hasteBuff = GetEffect(0);
            if (hasteBuff != null)
                hasteBuff.ChangeAmount(hasteBuff.GetAmount() - aurEff.GetAmount());
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(DiminishHaste, 1, AuraType.PeriodicDummy));
        }
    }

    [Script] // 51505 - Lava burst
    class spell_sha_lava_burst : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PathOfFlamesTalent, SpellIds.PathOfFlamesSpread);
        }

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                Unit target = GetExplTargetUnit();
                if (target)
                    if (caster.HasAura(SpellIds.PathOfFlamesTalent))
                        caster.CastSpell(target, SpellIds.PathOfFlamesSpread, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 77756 - Lava Surge
    class spell_sha_lava_surge : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LavaSurge);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.LavaSurge, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 77762 - Lava Surge
    class spell_sha_lava_surge_proc : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LavaBurst);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void ResetCooldown()
        {
            GetCaster().GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(SpellIds.LavaBurst, GetCastDifficulty()).ChargeCategoryId);
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(ResetCooldown));
        }
    }

    [Script] // 192223 - Liquid Magma Totem (erupting hit spell)
    class spell_sha_liquid_magma_totem : SpellScript
    {
        bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LiquidMagmaHit);
        }

        void HandleEffectHitTarget(uint effIndex)
        {
            Unit hitUnit = GetHitUnit();
            if (hitUnit != null)
                GetCaster().CastSpell(hitUnit, SpellIds.LiquidMagmaHit, true);
        }

        void HandleTargetSelect(List<WorldObject> targets)
        {
            // choose one random target from targets
            if (targets.Count > 1)
            {
                WorldObject selected = targets.SelectRandom();
                targets.Clear();
                targets.Add(selected);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(HandleTargetSelect, 0, Targets.UnitDestAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
        }
    }
    
    [Script] // 210621 - Path of Flames Spread
    class spell_sha_path_of_flames_spread : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlameShockMaelstrom);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.Remove(GetExplTargetUnit());
            targets.RandomResize(target => target.IsTypeId(TypeId.Unit) && !target.ToUnit().HasAura(SpellIds.FlameShockMaelstrom, GetCaster().GetGUID()), 1);
        }

        void HandleScript(uint effIndex)
        {
            Unit mainTarget = GetExplTargetUnit();
            if (mainTarget)
            {
                Aura flameShock = mainTarget.GetAura(SpellIds.FlameShockMaelstrom, GetCaster().GetGUID());
                if (flameShock != null)
                {
                    Aura newAura = GetCaster().AddAura(SpellIds.FlameShockMaelstrom, GetHitUnit());
                    if (newAura != null)
                    {
                        newAura.SetDuration(flameShock.GetDuration());
                        newAura.SetMaxDuration(flameShock.GetDuration());
                    }
                }
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 1, SpellEffectName.Dummy));
        }
    }

    [Script] // 51562 - Tidal Waves
    class spell_sha_tidal_waves : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TidalWaves);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, -aurEff.GetAmount());
            args.AddSpellMod(SpellValueMod.BasePoint1, aurEff.GetAmount());

            GetTarget().CastSpell(GetTarget(), SpellIds.TidalWaves, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 28823 - Totemic Power
    class spell_sha_t3_6p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TotemicPowerArmor, SpellIds.TotemicPowerAttackPower, SpellIds.TotemicPowerSpellPower, SpellIds.TotemicPowerMp5);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            uint spellId;
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            switch (target.GetClass())
            {
                case Class.Paladin:
                case Class.Priest:
                case Class.Shaman:
                case Class.Druid:
                    spellId = SpellIds.TotemicPowerMp5;
                    break;
                case Class.Mage:
                case Class.Warlock:
                    spellId = SpellIds.TotemicPowerSpellPower;
                    break;
                case Class.Hunter:
                case Class.Rogue:
                    spellId = SpellIds.TotemicPowerAttackPower;
                    break;
                case Class.Warrior:
                    spellId = SpellIds.TotemicPowerArmor;
                    break;
                default:
                    return;
            }

            caster.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 28820 - Lightning Shield
    class spell_sha_t3_8p_bonus : AuraScript
    {
        void PeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();

            // Need remove self if Lightning Shield not active
            if (GetTarget().GetAuraEffect(AuraType.ProcTriggerSpell, SpellFamilyNames.Shaman, new FlagArray128(0x400), GetCaster().GetGUID()) == null)
                Remove();
        }

        public override void Register()
        {
            OnEffectPeriodic .Add(new EffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
        }
    }
    
    [Script] // 64928 - Item - Shaman T8 Elemental 4P Bonus
    class spell_sha_t8_elemental_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Electrified);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.Electrified, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            // Add remaining ticks to damage done
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();
            amount += (int)target.GetRemainingPeriodicAmount(caster.GetGUID(), SpellIds.Electrified, AuraType.PeriodicDamage);

            CastSpellExtraArgs args = new(aurEff);
            args.SpellValueOverrides.Add(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.Electrified, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 67228 - Item - Shaman T9 Elemental 4P Bonus (Lava Burst)
    class spell_sha_t9_elemental_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LavaBurstBonusDamage);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.LavaBurstBonusDamage, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            // Add remaining ticks to damage done
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();
            amount += (int)target.GetRemainingPeriodicAmount(caster.GetGUID(), SpellIds.LavaBurstBonusDamage, AuraType.PeriodicDamage);

            CastSpellExtraArgs args = new(aurEff);
            args.SpellValueOverrides.Add(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.LavaBurstBonusDamage, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 70817 - Item - Shaman T10 Elemental 4P Bonus
    class spell_sha_t10_elemental_4p_bonus : AuraScript
    {
        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            // try to find spell Flame Shock on the target
            AuraEffect flameShock = target.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Shaman, new FlagArray128(0x10000000), caster.GetGUID());
            if (flameShock == null)
                return;

            Aura flameShockAura = flameShock.GetBase();

            int maxDuration = flameShockAura.GetMaxDuration();
            int newDuration = flameShockAura.GetDuration() + aurEff.GetAmount() * Time.InMilliseconds;

            flameShockAura.SetDuration(newDuration);
            // is it blizzlike to change max duration for FS?
            if (newDuration > maxDuration)
                flameShockAura.SetMaxDuration(newDuration);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 70808 - Item - Shaman T10 Restoration 4P Bonus
    class spell_sha_t10_restoration_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChainedHeal);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.ChainedHeal, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            // Add remaining ticks to healing done
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();
            amount += (int)target.GetRemainingPeriodicAmount(caster.GetGUID(), SpellIds.ChainedHeal, AuraType.PeriodicHeal);

            CastSpellExtraArgs args = new(aurEff);
            args.SpellValueOverrides.Add(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.ChainedHeal, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 33757 - Windfury
    class spell_sha_windfury : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WindfuryAttack);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            for (uint i = 0; i < 2; ++i)
                eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.WindfuryAttack, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }
}