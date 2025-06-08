// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using static Global;

namespace Scripts.Spells.Rogue
{

    struct SpellIds
    {
        public const uint AdrenalineRush = 13750;
        public const uint BetweenTheEyes = 199804;
        public const uint BlackjackTalent = 379005;
        public const uint Blackjack = 394119;
        public const uint BladeFlurry = 13877;
        public const uint BladeFlurryExtraAttack = 22482;
        public const uint Broadside = 193356;
        public const uint BuriedTreasure = 199600;
        public const uint CheatDeathDummy = 31231;
        public const uint CheatedDeath = 45181;
        public const uint CheatingDeath = 45182;
        public const uint DeathFromAbove = 152150;
        public const uint GrandMelee = 193358;
        public const uint GrapplingHook = 195457;
        public const uint ImrovedShiv = 319032;
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
        public const uint PreyOnTheWeakTalent = 131511;
        public const uint PreyOnTheWeak = 255909;
        public const uint RuthlessPrecision = 193357;
        public const uint Sanctuary = 98877;
        public const uint SkullAndCrossbones = 199603;
        public const uint ShadowFocus = 108209;
        public const uint ShadowFocusEffect = 112942;
        public const uint ShivNatureDamage = 319504;
        public const uint SliceAndDice = 315496;
        public const uint Sprint = 2983;
        public const uint Stealth = 1784;
        public const uint StealthStealthAura = 158185;
        public const uint StealthShapeshiftAura = 158188;
        public const uint SymbolsOfDeathCritAura = 227151;
        public const uint SymbolsOfDeathRank2 = 328077;
        public const uint TrueBearing = 193359;
        public const uint TurnTheTablesBuff = 198027;
        public const uint Vanish = 1856;
        public const uint VanishAura = 11327;
        public const uint TricksOfTheTrade = 57934;
        public const uint TricksOfTheTradeProc = 59628;
        public const uint HonorAmongThievesEnergize = 51699;
        public const uint T52PSetBonus = 37169;
        public const uint VenomousWounds = 79134;
    }

    struct Misc
    {
        public static int? GetFinishingMoveCPCost(Spell spell)
        {
            if (spell == null)
                return null;

            return spell.GetPowerTypeCostAmount(PowerType.ComboPoints);
        }
    }

    [Script] // 53 - Backstab
    class spell_rog_backstab : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 3));
        }

        void HandleHitDamage(uint effIndex)
        {
            Unit hitUnit = GetHitUnit();
            if (hitUnit == null)
                return;

            Unit caster = GetCaster();
            if (hitUnit.IsInBack(caster))
            {
                float currDamage = (float)(GetHitDamage());
                float newDamage = MathFunctions.AddPct(ref currDamage, (float)(GetEffectInfo(3).CalcValue(caster)));
                SetHitDamage((int)newDamage);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHitDamage, 1, SpellEffectName.SchoolDamage));
        }
    }

    // 379005 - Blackjack
    [Script] // Called by Sap - 6770 and Blind - 2094
    class spell_rog_blackjack : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlackjackTalent, SpellIds.Blackjack);
        }

        void EffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
                if (caster.HasAura(SpellIds.BlackjackTalent))
                    caster.CastSpell(GetTarget(), SpellIds.Blackjack, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(EffectRemove, 0, AuraType.Any, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 13877, 33735, (check 51211, 65956) - Blade Flurry
    class spell_rog_blade_flurry : AuraScript
    {
        Unit _procTarget;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BladeFlurryExtraAttack);
        }

        bool CheckProc(ProcEventInfo eventInfo)
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
            DoCheckProc.Add(new(CheckProc));
            if (m_scriptSpellId == SpellIds.BladeFlurry)
                OnEffectProc.Add(new(HandleProc, 0, AuraType.ModPowerRegenPercent));
            else
                OnEffectProc.Add(new(HandleProc, 0, AuraType.ModMeleeHaste));
        }
    }

    [Script] // 31230 - Cheat Death
    class spell_rog_cheat_death : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CheatDeathDummy, SpellIds.CheatedDeath, SpellIds.CheatingDeath)
                && ValidateSpellEffect((spellInfo.Id, 1));
        }

        void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            Unit target = GetTarget();
            if (target.HasAura(SpellIds.CheatedDeath))
            {
                absorbAmount = 0;
                return;
            }

            PreventDefaultAction();

            target.CastSpell(target, SpellIds.CheatDeathDummy, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
            target.CastSpell(target, SpellIds.CheatedDeath, TriggerCastFlags.DontReportCastError);
            target.CastSpell(target, SpellIds.CheatingDeath, TriggerCastFlags.DontReportCastError);

            target.SetHealth(target.CountPctFromMaxHealth(GetEffectInfo(1).CalcValue(target)));
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new(HandleAbsorb, 0));
        }
    }

    [Script] // 2818 - Deadly Poison
    class spell_rog_deadly_poison : SpellScript
    {
        byte _stackAmount;

        public override bool Load()
        {
            // at this point CastItem must already be initialized
            return GetCaster().IsPlayer() && GetCastItem() != null;
        }

        void HandleBeforeHit(SpellMissInfo missInfo)
        {
            if (missInfo != SpellMissInfo.None)
                return;

            Unit target = GetHitUnit();
            if (target != null)
            {
                // Deadly Poison
                AuraEffect aurEff = target.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Rogue, new FlagArray128(0x10000, 0x80000, 0), GetCaster().GetGUID());
                if (aurEff != null)
                    _stackAmount = aurEff.GetBase().GetStackAmount();
            }
        }

        void HandleAfterHit()
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

                if (item == null)
                    return;

                // item combat enchantments
                for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
                {
                    SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetEnchantmentId(slot));
                    if (enchant == null)
                        continue;

                    for (byte s = 0; s < 3; ++s)
                    {
                        if (enchant.Effect[s] != ItemEnchantmentType.CombatSpell)
                            continue;

                        SpellInfo spellInfo = SpellMgr.GetSpellInfo(enchant.EffectArg[s], Difficulty.None);
                        if (spellInfo == null)
                        {
                            Log.outError(LogFilter.Spells, $"Player.CastItemCombatSpell Enchant {enchant.Id}, player (Name: {player.GetName()}, {player.GetGUID().ToString()})cast unknown spell {enchant.EffectArg[s]}");
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

        public override void Register()
        {
            BeforeHit.Add(new(HandleBeforeHit));
            AfterHit.Add(new(HandleAfterHit));
        }
    }

    [Script] // 32645 - Envenom
    class spell_rog_envenom : SpellScript
    {
        void CalculateDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            pctMod *= GetSpell().GetPowerTypeCostAmount(PowerType.ComboPoints).GetValueOrDefault(0);

            AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T52PSetBonus, 0);
            if (t5 != null)
                flatMod += t5.GetAmount();
        }

        public override void Register()
        {
            CalcDamage.Add(new(CalculateDamage));
        }
    }

    [Script] // 196819 - Eviscerate
    class spell_rog_eviscerate : SpellScript
    {
        void CalculateDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            pctMod *= GetSpell().GetPowerTypeCostAmount(PowerType.ComboPoints).GetValueOrDefault(0);

            AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T52PSetBonus, 0);
            if (t5 != null)
                flatMod += t5.GetAmount();
        }

        public override void Register()
        {
            CalcDamage.Add(new(CalculateDamage));
        }
    }

    [Script] // 193358 - Grand Melee
    class spell_rog_grand_melee : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SliceAndDice);
        }

        bool HandleCheckProc(ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();
            return procSpell != null && procSpell.HasPowerTypeCost(PowerType.ComboPoints);
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
            DoCheckProc.Add(new(HandleCheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 198031 - Honor Among Thieves
    [Script] // 7.1.5
    class spell_rog_honor_among_thieves : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HonorAmongThievesEnergize);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.HonorAmongThievesEnergize, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 5938 - Shiv
    class spell_rog_improved_shiv : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShivNatureDamage);
        }

        public override bool Load()
        {
            return GetCaster().HasAura(SpellIds.ImrovedShiv);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ShivNatureDamage, new CastSpellExtraArgs()
                .SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
                .SetTriggeringSpell(GetSpell()));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 51690 - Killing Spree
    class spell_rog_killing_spree_AuraScript : AuraScript
    {
        List<ObjectGuid> _targets = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.KillingSpreeTeleport,
                SpellIds.KillingSpreeWeaponDmg,
                SpellIds.KillingSpreeDmgBuff
           );
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
                Unit target = ObjAccessor.GetUnit(GetTarget(), guid);
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
            AfterEffectApply.Add(new(HandleApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
        }

        public void AddTarget(Unit target)
        {
            _targets.Add(target.GetGUID());
        }
    }

    [Script]
    class spell_rog_killing_spree : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Empty() || GetCaster().GetVehicleBase() != null)
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
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaEnemy));
            OnEffectHitTarget.Add(new(HandleDummy, 1, SpellEffectName.Dummy));
        }
    }

    [Script] // 385627 - Kingsbane
    class spell_rog_kingsbane : AuraScript
    {
        bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            return procInfo.GetActionTarget().HasAura(GetId(), GetCasterGUID());
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 4, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 76806 - Mastery: Main Gauche
    class spell_rog_mastery_main_gauche : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MainGauche);
        }

        bool HandleCheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo()?.GetVictim() != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit target = GetTarget();
            if (target != null)
                target.CastSpell(procInfo.GetDamageInfo().GetVictim(), SpellIds.MainGauche, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(HandleCheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script]
    class spell_rog_pickpocket : SpellScript
    {
        SpellCastResult CheckCast()
        {
            if (GetExplTargetUnit() == null || !GetCaster().IsValidAttackTarget(GetExplTargetUnit(), GetSpellInfo()))
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
        }
    }

    // 131511 - Prey on the Weak
    [Script] // Called by Cheap Shot - 1833 and Kidney Shot - 408
    class spell_rog_prey_on_the_weak : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PreyOnTheWeakTalent, SpellIds.PreyOnTheWeak);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
                if (caster.HasAura(SpellIds.PreyOnTheWeakTalent))
                    caster.CastSpell(GetTarget(), SpellIds.PreyOnTheWeak, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 79096 - Restless Blades
    class spell_rog_restless_blades : AuraScript
    {
        uint[] Spells = { SpellIds.AdrenalineRush, SpellIds.BetweenTheEyes, SpellIds.Sprint, SpellIds.GrapplingHook, SpellIds.Vanish, SpellIds.KillingSpree, SpellIds.MarkedForDeath, SpellIds.DeathFromAbove };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(Spells);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            var spentCP = Misc.GetFinishingMoveCPCost(procInfo.GetProcSpell());
            if (spentCP.HasValue)
            {
                int cdExtra = -(int)((float)(aurEff.GetAmount() * spentCP.Value) * 0.1f);

                SpellHistory history = GetTarget().GetSpellHistory();
                foreach (uint spellId in Spells)
                    history.ModifyCooldown(spellId, TimeSpan.FromSeconds(cdExtra), true);
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 315508 - Roll the Bones
    class spell_rog_roll_the_bones : SpellScript
    {
        uint[] Spells = { SpellIds.SkullAndCrossbones, SpellIds.GrandMelee, SpellIds.RuthlessPrecision, SpellIds.TrueBearing, SpellIds.BuriedTreasure, SpellIds.Broadside };

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

            List<uint> possibleBuffs = new(Spells);
            possibleBuffs.Shuffle();

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
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script] // 1943 - Rupture
    class spell_rog_rupture : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VenomousWounds);
        }

        void OnEffectRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
                return;

            Aura aura = GetAura();
            Unit caster = aura.GetCaster();
            if (caster == null)
                return;

            Aura auraVenomousWounds = caster.GetAura(SpellIds.VenomousWounds);
            if (auraVenomousWounds == null)
                return;

            // Venomous Wounds: if unit dies while being affected by rupture, regain energy based on remaining duration
            var cost = GetSpellInfo().CalcPowerCost(PowerType.Energy, false, caster, GetSpellInfo().GetSchoolMask(), null);
            if (cost == null)
                return;

            float pct = (float)(aura.GetDuration()) / (float)(aura.GetMaxDuration());
            int extraAmount = (int)((float)(cost.Amount) * pct);
            caster.ModifyPower(PowerType.Energy, extraAmount);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(OnEffectRemoved, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 14161 - Ruthlessness
    class spell_rog_ruthlessness : AuraScript
    {
        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit target = GetTarget();

            var cost = Misc.GetFinishingMoveCPCost(procInfo.GetProcSpell());
            if (cost.HasValue)
                if (RandomHelper.randChance(aurEff.GetSpellEffectInfo().PointsPerResource * cost.Value))
                    target.ModifyPower(PowerType.ComboPoints, 1);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 185438 - Shadowstrike
    class spell_rog_shadowstrike : SpellScript
    {
        bool _hasPremeditationAura;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PremeditationAura, SpellIds.SliceAndDice, SpellIds.PremeditationPassive)
            && ValidateSpellEffect((SpellIds.PremeditationPassive, 0));
        }

        SpellCastResult HandleCheckCast()
        {
            // Because the premeditation aura is Removed when we're out of stealth,
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
                int duration = SpellMgr.GetSpellInfo(SpellIds.PremeditationPassive, Difficulty.None).GetEffect(0).CalcValue(GetCaster());

                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.Duration, duration * Time.InMilliseconds);
                caster.CastSpell(caster, SpellIds.SliceAndDice, args);
            }
        }

        public override void Register()
        {
            OnCheckCast.Add(new(HandleCheckCast));
            OnEffectHitTarget.Add(new(HandleEnergize, 1, SpellEffectName.Energize));
        }
    }

    [Script] // 193315 - Sinister Strike
    class spell_rog_sinister_strike : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.T52PSetBonus);
        }

        void HandleDummy(uint effIndex)
        {
            int damagePerCombo = GetHitDamage();
            AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T52PSetBonus, 0);
            if (t5 != null)
                damagePerCombo += t5.GetAmount();

            int finalDamage = damagePerCombo;
            int? comboPointCost = GetSpell().GetPowerTypeCostAmount(PowerType.ComboPoints);
            if (comboPointCost.HasValue)
                finalDamage *= comboPointCost.Value;

            SetHitDamage(finalDamage);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 2, SpellEffectName.Dummy));
        }
    }

    [Script] // 1784 - Stealth
    class spell_rog_stealth : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MasterOfSubtletyPassive,
                SpellIds.MasterOfSubtletyDamagePercent,
                SpellIds.Sanctuary,
                SpellIds.ShadowFocus,
                SpellIds.ShadowFocusEffect,
                SpellIds.StealthStealthAura,
                SpellIds.StealthShapeshiftAura
           );
        }

        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();

            // Master of Subtlety
            if (target.HasAura(SpellIds.MasterOfSubtletyPassive))
                target.CastSpell(target, SpellIds.MasterOfSubtletyDamagePercent, TriggerCastFlags.FullMask);

            // Shadow Focus
            if (target.HasAura(SpellIds.ShadowFocus))
                target.CastSpell(target, SpellIds.ShadowFocusEffect, TriggerCastFlags.FullMask);

            // Premeditation
            if (target.HasAura(SpellIds.PremeditationPassive))
                target.CastSpell(target, SpellIds.PremeditationAura, true);

            target.CastSpell(target, SpellIds.Sanctuary, TriggerCastFlags.FullMask);
            target.CastSpell(target, SpellIds.StealthStealthAura, TriggerCastFlags.FullMask);
            target.CastSpell(target, SpellIds.StealthShapeshiftAura, TriggerCastFlags.FullMask);
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
            AfterEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 212283 - Symbols of Death
    class spell_rog_symbols_of_death : SpellScript
    {
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
            OnEffectHitTarget.Add(new(HandleEffectHitTarget, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script] // 57934 - Tricks of the Trade
    class spell_rog_tricks_of_the_trade_AuraScript : AuraScript
    {
        ObjectGuid _redirectTarget;

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
            if (ObjAccessor.GetUnit(rogue, _redirectTarget) != null)
                rogue.CastSpell(rogue, SpellIds.TricksOfTheTradeProc, aurEff);
            Remove(AuraRemoveMode.Default);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
        }

        public void SetRedirectTarget(ObjectGuid guid) { _redirectTarget = guid; }
    }

    [Script] // 57934 - Tricks of the Trade
    class spell_rog_tricks_of_the_trade : SpellScript
    {
        void DoAfterHit()
        {
            Aura aura = GetHitAura();
            if (aura != null)
            {
                var script = aura.GetScript<spell_rog_tricks_of_the_trade_AuraScript>();
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

        public override void Register()
        {
            AfterHit.Add(new(DoAfterHit));
        }
    }

    [Script] // 59628 - Tricks of the Trade (Proc)
    class spell_rog_tricks_of_the_trade_proc : AuraScript
    {
        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.TricksOfTheTrade);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 198020 - Turn the Tables (PvP Talent)
    class spell_rog_turn_the_tables : AuraScript
    {
        bool CheckForStun(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcSpell() != null && eventInfo.GetProcSpell().GetSpellInfo().HasAura(AuraType.ModStun);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckForStun, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 198023 - Turn the Tables (periodic)
    class spell_rog_turn_the_tables_periodic_check : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TurnTheTablesBuff);
        }

        void CheckForStun(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (!target.HasAuraType(AuraType.ModStun))
            {
                target.CastSpell(target, SpellIds.TurnTheTablesBuff, aurEff);
                PreventDefaultAction();
                Remove();
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(CheckForStun, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 1856 - Vanish - SpellIds.Vanish
    class spell_rog_vanish : SpellScript
    {
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

            target.CastSpell(target, SpellIds.VanishAura, TriggerCastFlags.FullMask);
            target.CastSpell(target, SpellIds.StealthShapeshiftAura, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new(OnLaunchTarget, 1, SpellEffectName.TriggerSpell));
        }
    }

    [Script] // 11327 - Vanish
    class spell_rog_vanish_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Stealth);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.Stealth, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 79134 - Venomous Wounds - SpellIds.VenomousWounds
    class spell_rog_venomous_wounds : AuraScript
    {
        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            int extraEnergy = aurEff.GetAmount();
            GetTarget().ModifyPower(PowerType.Energy, extraEnergy);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
        }
    }
}