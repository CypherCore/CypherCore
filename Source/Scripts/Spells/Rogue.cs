// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Rogue
{
    public struct SpellIds
    {
        public const uint AdrenalineRush = 13750;
        public const uint BetweenTheEyes = 199804;
        public const uint BladeFlurry = 13877;
        public const uint BladeFlurryExtraAttack = 22482;
        public const uint Broadside = 193356;
        public const uint BuriedTreasure = 199600;
        public const uint DeathFromAbove = 152150;
        public const uint GrandMelee = 193358;
        public const uint GrapplingHook = 195457;
        public const uint KillingSpree = 51690;
        public const uint KillingSpreeTeleport = 57840;
        public const uint KillingSpreeWeaponDmg = 57841;
        public const uint KillingSpreeDmgBuff = 61851;
        public const uint MarkedForDeath = 137619;
        public const uint MasterOfSubtletyDamagePercent = 31665;
        public const uint MasterOfSubtletyPassive = 31223;
        public const uint MainGauche = 86392;
        public const uint PremeditationPassive = 343160;
        public const uint PremeditationAura = 343173;
        public const uint RuthlessPrecision = 193357;
        public const uint Sanctuary = 98877;
        public const uint SkullAndCrossbones = 199603;
        public const uint ShadowFocus = 108209;
        public const uint ShadowFocusEffect = 112942;
        public const uint SliceAndDice = 315496;
        public const uint Sprint = 2983;
        public const uint Stealth = 1784;
        public const uint StealthStealthAura = 158185;
        public const uint StealthShapeshiftAura = 158188;
        public const uint SymbolsOfDeathCritAura = 227151;
        public const uint SymbolsOfDeathRank2 = 328077;
        public const uint TrueBearing = 193359;
        public const uint Vanish = 1856;
        public const uint VanishAura = 11327;
        public const uint TricksOfTheTrade = 57934;
        public const uint TricksOfTheTradeProc = 59628;
        public const uint HonorAmongThievesEnergize = 51699;
        public const uint T52pSetBonus = 37169;
        public const uint VenomousWounds = 79134;
    }

    [Script] // 53 - Backstab
    class spell_rog_backstab : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 3;
        }

        void HandleHitDamage(uint effIndex)
        {
            Unit hitUnit = GetHitUnit();
            if (!hitUnit)
                return;

            Unit caster = GetCaster();
            if (hitUnit.IsInBack(caster))
            {
                float currDamage = (float)GetHitDamage();
                float newDamage = MathFunctions.AddPct(ref currDamage, (float)GetEffectInfo(3).CalcValue(caster));
                SetHitDamage((int)newDamage);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHitDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 13877, 33735, (check 51211, 65956) - Blade Flurry
    class spell_rog_blade_flurry_AuraScript : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BladeFlurryExtraAttack);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            _procTarget = GetTarget().SelectNearbyTarget(eventInfo.GetProcTarget());
            return _procTarget != null && eventInfo.GetDamageInfo() != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                CastSpellExtraArgs args = new(aurEff);
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage());
                GetTarget().CastSpell(_procTarget, SpellIds.BladeFlurryExtraAttack, args);
            }
        }

        public override void Register()
        {
            if (m_scriptSpellId == SpellIds.BladeFlurry)
                Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.ModPowerRegenPercent, AuraScriptHookType.EffectProc));
            else
                Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.ModMeleeHaste, AuraScriptHookType.EffectProc));
        }

        Unit _procTarget = null;
    }

    [Script] // 2818 - Deadly Poison
    class spell_rog_deadly_poison_SpellScript : SpellScript, IBeforeHit, IAfterHit
    {
        public override bool Load()
        {
            // at this point CastItem must already be initialized
            return GetCaster().IsPlayer() && GetCastItem();
        }

        public void BeforeHit(SpellMissInfo missInfo)
        {
            if (missInfo != SpellMissInfo.None)
                return;

            Unit target = GetHitUnit();
            if (target != null)
            {
                // Deadly Poison
                AuraEffect aurEff = target.GetAuraEffect(AuraType.PeriodicDummy, SpellFamilyNames.Rogue, new FlagArray128(0x10000, 0x80000, 0), GetCaster().GetGUID());
                if (aurEff != null)
                    _stackAmount = aurEff.GetBase().GetStackAmount();
            }
        }

        public void AfterHit()
        {
            if (_stackAmount < 5)
                return;

            Player player = GetCaster().ToPlayer();
            Unit target = GetHitUnit();
            if (target != null)
            {

                Item item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                if (item == GetCastItem())
                    item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

                if (!item)
                    return;

                // item combat enchantments
                for (byte slot = 0; slot < (int)EnchantmentSlot.Max; ++slot)
                {
                    var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetEnchantmentId((EnchantmentSlot)slot));
                    if (enchant == null)
                        continue;

                    for (byte s = 0; s < 3; ++s)
                    {
                        if (enchant.Effect[s] != ItemEnchantmentType.CombatSpell)
                            continue;

                        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(enchant.EffectArg[s], Difficulty.None);
                        if (spellInfo == null)
                        {
                            Log.outError(LogFilter.Spells, $"Player::CastItemCombatSpell Enchant {enchant.Id}, player (Name: {player.GetName()}, {player.GetGUID()}) cast unknown spell {enchant.EffectArg[s]}");
                            continue;
                        }

                        // Proc only rogue poisons
                        if (spellInfo.SpellFamilyName != SpellFamilyNames.Rogue || spellInfo.Dispel != DispelType.Poison)
                            continue;

                        // Do not reproc deadly
                        if (spellInfo.SpellFamilyFlags & new FlagArray128(0x10000))
                            continue;

                        if (spellInfo.IsPositive())
                            player.CastSpell(player, enchant.EffectArg[s], item);
                        else
                            player.CastSpell(target, enchant.EffectArg[s], item);
                    }
                }
            }
        }

        byte _stackAmount = 0;
    }

    [Script] // 193358 - Grand Melee
    class spell_rog_grand_melee : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SliceAndDice);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();
            return procSpell && procSpell.HasPowerTypeCost(PowerType.ComboPoints);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Spell procSpell = procInfo.GetProcSpell();
            int amount = aurEff.GetAmount() * procSpell.GetPowerTypeCostAmount(PowerType.ComboPoints).Value * 1000;

            Unit target = GetTarget();
            if (target != null)
            {
                Aura aura = target.GetAura(SpellIds.SliceAndDice);
                if (aura != null)
                    aura.SetDuration(aura.GetDuration() + amount);
                else
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.AddSpellMod(SpellValueMod.Duration, amount);
                    target.CastSpell(target, SpellIds.SliceAndDice, args);
                }
            }
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 51690 - Killing Spree
    class spell_rog_killing_spree_SpellScript : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Empty() || GetCaster().GetVehicleBase())
                FinishCast(SpellCastResult.OutOfRange);
        }

        void HandleDummy(uint effIndex)
        {
            Aura aura = GetCaster().GetAura(SpellIds.KillingSpree);
            if (aura != null)
            {
                spell_rog_killing_spree_AuraScript script = aura.GetScript<spell_rog_killing_spree_AuraScript>();
                if (script != null)
                    script.AddTarget(GetHitUnit());
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
            SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script]
    class spell_rog_killing_spree_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        List<ObjectGuid> _targets = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.KillingSpreeTeleport, SpellIds.KillingSpreeWeaponDmg, SpellIds.KillingSpreeDmgBuff);
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.KillingSpreeDmgBuff, true);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            while (!_targets.Empty())
            {
                ObjectGuid guid = _targets.SelectRandom();
                Unit target = Global.ObjAccessor.GetUnit(GetTarget(), guid);
                if (target != null)
                {
                    GetTarget().CastSpell(target, SpellIds.KillingSpreeTeleport, true);
                    GetTarget().CastSpell(target, SpellIds.KillingSpreeWeaponDmg, true);
                    break;
                }
                else
                    _targets.Remove(guid);
            }
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.KillingSpreeDmgBuff);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            Effects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
            Effects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        public void AddTarget(Unit target)
        {
            _targets.Add(target.GetGUID());
        }
    }

    [Script] // 76806 - Mastery: Main Gauche
    class spell_rog_mastery_main_gauche : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MainGauche);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo() != null && eventInfo.GetDamageInfo().GetVictim() != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit target = GetTarget();
            if (target != null)
                target.CastSpell(procInfo.GetDamageInfo().GetVictim(), SpellIds.MainGauche, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script]
    class spell_rog_pickpocket : SpellScript, ICheckCastHander
    {
        public SpellCastResult CheckCast()
        {
            if (!GetExplTargetUnit() || !GetCaster().IsValidAttackTarget(GetExplTargetUnit(), GetSpellInfo()))
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }
    }
    
    [Script] // 79096 - Restless Blades
    class spell_rog_restless_blades : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        static uint[] Spells = { SpellIds.AdrenalineRush, SpellIds.BetweenTheEyes, SpellIds.Sprint, SpellIds.GrapplingHook, SpellIds.Vanish, SpellIds.KillingSpree, SpellIds.MarkedForDeath, SpellIds.DeathFromAbove };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(Spells);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            int? spentCP = procInfo.GetProcSpell()?.GetPowerTypeCostAmount(PowerType.ComboPoints);
            if (spentCP.HasValue)
            {
                int cdExtra = (int)-((float)(aurEff.GetAmount() * spentCP.Value) * 0.1f);

                SpellHistory history = GetTarget().GetSpellHistory();
                foreach (uint spellId in Spells)
                    history.ModifyCooldown(spellId, TimeSpan.FromSeconds(cdExtra), true);
            }
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 315508 - Roll the Bones
    class spell_rog_roll_the_bones : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        static uint[] Spells = { SpellIds.SkullAndCrossbones, SpellIds.GrandMelee, SpellIds.RuthlessPrecision, SpellIds.TrueBearing, SpellIds.BuriedTreasure, SpellIds.Broadside };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(Spells);
        }

        void HandleDummy(uint effIndex)
        {
            int currentDuration = 0;
            foreach (uint spellId in Spells)
            {
                Aura aura = GetCaster().GetAura(spellId);
                if (aura != null)
                {
                    currentDuration = aura.GetDuration();
                    GetCaster().RemoveAura(aura);
                }
            }

            var possibleBuffs = Spells.Shuffle().ToArray();

            // https://www.icy-veins.com/wow/outlaw-rogue-pve-dps-rotation-cooldowns-abilities
            // 1 Roll the Bones buff  : 100.0 % chance;
            // 2 Roll the Bones buffs : 19 % chance;
            // 5 Roll the Bones buffs : 1 % chance.
            int chance = RandomHelper.IRand(1, 100);
            int numBuffs = 1;
            if (chance <= 1)
                numBuffs = 5;
            else if (chance <= 20)
                numBuffs = 2;

            for (int i = 0; i < numBuffs; ++i)
            {
                uint spellId = possibleBuffs[i];
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.Duration, GetSpellInfo().GetDuration() + currentDuration);
                GetCaster().CastSpell(GetCaster(), spellId, args);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 1943 - Rupture
    class spell_rog_rupture_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VenomousWounds);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                canBeRecalculated = false;

                float[] attackpowerPerCombo =
                {
                        0.0f,
                        0.015f,         // 1 point:  ${($m1 + $b1*1 + 0.015 * $AP) * 4} damage over 8 secs
                        0.024f,         // 2 points: ${($m1 + $b1*2 + 0.024 * $AP) * 5} damage over 10 secs
                        0.03f,          // 3 points: ${($m1 + $b1*3 + 0.03 * $AP) * 6} damage over 12 secs
                        0.03428571f,    // 4 points: ${($m1 + $b1*4 + 0.03428571 * $AP) * 7} damage over 14 secs
                        0.0375f         // 5 points: ${($m1 + $b1*5 + 0.0375 * $AP) * 8} damage over 16 secs
                };

                uint cp = caster.GetComboPoints();
                if (cp > 5)
                    cp = 5;

                amount += (int)(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * attackpowerPerCombo[cp]);
            }
        }

        void OnEffectRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
                return;

            Aura aura = GetAura();
            Unit caster = aura.GetCaster();
            if (!caster)
                return;

            Aura auraVenomousWounds = caster.GetAura(SpellIds.VenomousWounds);
            if (auraVenomousWounds == null)
                return;

            // Venomous Wounds: if unit dies while being affected by rupture, regain energy based on remaining duration
            SpellPowerCost cost = GetSpellInfo().CalcPowerCost(PowerType.Energy, false, caster, GetSpellInfo().GetSchoolMask(), null);
            if (cost == null)
                return;

            float pct = (float)aura.GetDuration() / (float)aura.GetMaxDuration();
            int extraAmount = (int)((float)cost.Amount * pct);
            caster.ModifyPower(PowerType.Energy, extraAmount);
        }

        public override void Register()
        {
            Effects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicDummy));
            Effects.Add(new EffectApplyHandler(OnEffectRemoved, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }

    [Script] // 14161 - Ruthlessness
    class spell_rog_ruthlessness : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit target = GetTarget();

            int? cost = procInfo.GetProcSpell()?.GetPowerTypeCostAmount(PowerType.ComboPoints);
            if (cost.HasValue)
                if (RandomHelper.randChance(aurEff.GetSpellEffectInfo().PointsPerResource * (cost.Value)))
                    target.ModifyPower(PowerType.ComboPoints, 1);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 185438 - Shadowstrike
    class spell_rog_shadowstrike : SpellScript, ICheckCastHander, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PremeditationAura, SpellIds.SliceAndDice, SpellIds.PremeditationPassive)
            && Global.SpellMgr.GetSpellInfo(SpellIds.PremeditationPassive, Difficulty.None).GetEffects().Count > 0;
        }

        public SpellCastResult CheckCast()
        {
            // Because the premeditation aura is removed when we're out of stealth,
            // when we reach HandleEnergize the aura won't be there, even if it was when player launched the spell
            _hasPremeditationAura = GetCaster().HasAura(SpellIds.PremeditationAura);
            return SpellCastResult.Success;
        }

        void HandleEnergize(uint effIndex)
        {
            Unit caster = GetCaster();
            if (_hasPremeditationAura)
            {
                if (caster.HasAura(SpellIds.SliceAndDice))
                {
                    Aura premeditationPassive = caster.GetAura(SpellIds.PremeditationPassive);
                    if (premeditationPassive != null)
                    {
                        AuraEffect auraEff = premeditationPassive.GetEffect(1);
                        if (auraEff != null)
                            SetHitDamage(GetHitDamage() + auraEff.GetAmount());
                    }
                }

                // Grant 10 seconds of slice and dice
                int duration = Global.SpellMgr.GetSpellInfo(SpellIds.PremeditationPassive, Difficulty.None).GetEffect(0).CalcValue(GetCaster());

                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.Duration, duration * Time.InMilliseconds);
                caster.CastSpell(caster, SpellIds.SliceAndDice, args);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEnergize, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
        }

        bool _hasPremeditationAura = false;
    }

    [Script] // 193315 - Sinister Strike
    class spell_rog_sinister_strike : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.T52pSetBonus);
        }

        void HandleDummy(uint effIndex)
        {
            int damagePerCombo = GetHitDamage();
            AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T52pSetBonus, 0);
            if (t5 != null)
                damagePerCombo += t5.GetAmount();

            int finalDamage = damagePerCombo;
            var costs = GetSpell().GetPowerCost();
            var c = costs.Find(cost => cost.Power == PowerType.ComboPoints);
                if (c != null)
                    finalDamage *= c.Amount;

                SetHitDamage(finalDamage);
            }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 1784 - Stealth
    class spell_rog_stealth : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MasterOfSubtletyPassive, SpellIds.MasterOfSubtletyDamagePercent, SpellIds.Sanctuary, SpellIds.ShadowFocus, SpellIds.ShadowFocusEffect, SpellIds.StealthStealthAura, SpellIds.StealthShapeshiftAura);
            }

            void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Unit target = GetTarget();

                // Master of Subtlety
                if (target.HasAura(SpellIds.MasterOfSubtletyPassive))
                    target.CastSpell(target, SpellIds.MasterOfSubtletyDamagePercent, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                // Shadow Focus
                if (target.HasAura(SpellIds.ShadowFocus))
                    target.CastSpell(target, SpellIds.ShadowFocusEffect, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                // Premeditation
                if (target.HasAura(SpellIds.PremeditationPassive))
                    target.CastSpell(target, SpellIds.PremeditationAura, true);

                target.CastSpell(target, SpellIds.Sanctuary, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
                target.CastSpell(target, SpellIds.StealthStealthAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
                target.CastSpell(target, SpellIds.StealthShapeshiftAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            }

            void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Unit target = GetTarget();

                // Master of Subtlety
                AuraEffect masterOfSubtletyPassive = GetTarget().GetAuraEffect(SpellIds.MasterOfSubtletyPassive, 0);
                if (masterOfSubtletyPassive != null)
                {
                    Aura masterOfSubtletyAura = GetTarget().GetAura(SpellIds.MasterOfSubtletyDamagePercent);
                    if (masterOfSubtletyAura != null)
                    {
                        masterOfSubtletyAura.SetMaxDuration(masterOfSubtletyPassive.GetAmount());
                        masterOfSubtletyAura.RefreshDuration();
                    }
                }

                // Premeditation
                target.RemoveAura(SpellIds.PremeditationAura);

                target.RemoveAurasDueToSpell(SpellIds.ShadowFocusEffect);
                target.RemoveAurasDueToSpell(SpellIds.StealthStealthAura);
                target.RemoveAurasDueToSpell(SpellIds.StealthShapeshiftAura);
            }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            Effects.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }
    }

    [Script] // 212283 - Symbols of Death
    class spell_rog_symbols_of_death : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SymbolsOfDeathRank2, SpellIds.SymbolsOfDeathCritAura);
        }

        void HandleEffectHitTarget(uint effIndex)
        {
            if (GetCaster().HasAura(SpellIds.SymbolsOfDeathRank2))
                GetCaster().CastSpell(GetCaster(), SpellIds.SymbolsOfDeathCritAura, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 1856 - Vanish - SPELL_ROGUE_VANISH
    class spell_rog_vanish : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VanishAura, SpellIds.StealthShapeshiftAura);
        }

        void OnLaunchTarget(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Unit target = GetHitUnit();

            target.RemoveAurasByType(AuraType.ModStalked);
            if (!target.IsPlayer())
                return;

            if (target.HasAura(SpellIds.VanishAura))
                return;

            target.CastSpell(target, SpellIds.VanishAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            target.CastSpell(target, SpellIds.StealthShapeshiftAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(OnLaunchTarget, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.LaunchTarget));
        }
    }

    [Script] // 11327 - Vanish
    class spell_rog_vanish_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Stealth);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.Stealth, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }

    [Script] // 57934 - Tricks of the Trade
    class spell_rog_tricks_of_the_trade_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TricksOfTheTradeProc);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Default || !GetTarget().HasAura(SpellIds.TricksOfTheTradeProc))
                GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.TricksOfTheTrade);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit rogue = GetTarget();
            if (Global.ObjAccessor.GetUnit(rogue, _redirectTarget))
                rogue.CastSpell(rogue, SpellIds.TricksOfTheTradeProc, new CastSpellExtraArgs(aurEff));
            Remove(AuraRemoveMode.Default);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
            Effects.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        ObjectGuid _redirectTarget;

        public void SetRedirectTarget(ObjectGuid guid) { _redirectTarget = guid; }
    }

    [Script] // 57934 - Tricks of the Trade
    class spell_rog_tricks_of_the_trade : SpellScript, IAfterHit
    {
        public void AfterHit()
        {
            Aura aura = GetHitAura();
            if (aura != null)
            {
                spell_rog_tricks_of_the_trade_aura script = aura.GetScript<spell_rog_tricks_of_the_trade_aura>();
                if (script != null)
                {
                    Unit explTarget = GetExplTargetUnit();
                    if (explTarget != null)
                        script.SetRedirectTarget(explTarget.GetGUID());
                    else
                        script.SetRedirectTarget(ObjectGuid.Empty);
                }
            }
        }
    }

    [Script] // 59628 - Tricks of the Trade (Proc)
    class spell_rog_tricks_of_the_trade_proc : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.TricksOfTheTrade);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }
    }

    // 198031 - Honor Among Thieves
    [Script] /// 7.1.5
    class spell_rog_honor_among_thieves_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HonorAmongThievesEnergize);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.HonorAmongThievesEnergize, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 196819 - Eviscerate
    class spell_rog_eviscerate_SpellScript : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        void CalculateDamage(uint effIndex)
        {
            int damagePerCombo = GetHitDamage();
            AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T52pSetBonus, 0);
            if (t5 != null)
                damagePerCombo += t5.GetAmount();

            int finalDamage = damagePerCombo;
            var costs = GetSpell().GetPowerCost();
            var c = costs.Find(cost => cost.Power == PowerType.ComboPoints);
                if (c != null)
                    finalDamage *= c.Amount;

                SetHitDamage(finalDamage);
            }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(CalculateDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 32645 - Envenom
    class spell_rog_envenom_SpellScript : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        void CalculateDamage(uint effIndex)
        {
            int damagePerCombo = GetHitDamage();
            AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T52pSetBonus, 0);
            if (t5 != null)
                damagePerCombo += t5.GetAmount();

            int finalDamage = damagePerCombo;
            var costs = GetSpell().GetPowerCost();
            var c = costs.Find(cost => cost.Power == PowerType.ComboPoints);
            if (c != null)
                finalDamage *= c.Amount;

            SetHitDamage(finalDamage);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(CalculateDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 79134 - Venomous Wounds - SPELL_ROGUE_VENOMOUS_WOUNDS
    class spell_rog_venomous_wounds : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();
        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            int extraEnergy = aurEff.GetAmount();
            GetTarget().ModifyPower(PowerType.Energy, extraEnergy);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
