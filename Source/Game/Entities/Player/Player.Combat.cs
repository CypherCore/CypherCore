// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Groups;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using Game.Maps;

namespace Game.Entities
{
    public partial class Player
    {
        void SetRegularAttackTime()
        {
            for (WeaponAttackType weaponAttackType = 0; weaponAttackType < WeaponAttackType.Max; ++weaponAttackType)
            {
                Item tmpitem = GetWeaponForAttack(weaponAttackType, true);
                if (tmpitem != null && !tmpitem.IsBroken())
                {
                    ItemTemplate proto = tmpitem.GetTemplate();
                    if (proto.GetDelay() != 0)
                        SetBaseAttackTime(weaponAttackType, proto.GetDelay());
                }
                else
                    SetBaseAttackTime(weaponAttackType, SharedConst.BaseAttackTime);  // If there is no weapon reset attack time to base (might have been changed from forms)
            }
        }

        public void RewardPlayerAndGroupAtEvent(uint creature_id, WorldObject pRewardSource)
        {
            if (pRewardSource == null)
                return;
            ObjectGuid creature_guid = pRewardSource.IsTypeId(TypeId.Unit) ? pRewardSource.GetGUID() : ObjectGuid.Empty;

            // prepare data for near group iteration
            Group group = GetGroup();
            if (group != null)
            {
                foreach (GroupReference groupRef in group.GetMembers())
                {
                    Player player = groupRef.GetSource();
                    if (!player.IsAtGroupRewardDistance(pRewardSource))
                        continue;                               // member (alive or dead) or his corpse at req. distance

                    // quest objectives updated only for alive group member or dead but with not released body
                    if (player.IsAlive() || player.GetCorpse() == null)
                        player.KilledMonsterCredit(creature_id, creature_guid);
                }
            }
            else
                KilledMonsterCredit(creature_id, creature_guid);
        }

        public void AddWeaponProficiency(uint newflag) { m_WeaponProficiency |= newflag; }
        public void AddArmorProficiency(uint newflag) { m_ArmorProficiency |= newflag; }
        public uint GetWeaponProficiency() { return m_WeaponProficiency; }
        public uint GetArmorProficiency() { return m_ArmorProficiency; }
        public void SendProficiency(ItemClass itemClass, uint itemSubclassMask)
        {
            SetProficiency packet = new();
            packet.ProficiencyMask = itemSubclassMask;
            packet.ProficiencyClass = (byte)itemClass;
            SendPacket(packet);
        }

        bool CanTitanGrip() { return m_canTitanGrip; }

        float GetRatingMultiplier(CombatRating cr)
        {
            GtCombatRatingsRecord Rating = CliDB.CombatRatingsGameTable.GetRow(GetLevel());
            if (Rating == null)
                return 1.0f;

            float value = GetGameTableColumnForCombatRating(Rating, cr);
            if (value == 0)
                return 1.0f;                                        // By default use minimum coefficient (not must be called)

            return 1.0f / value;
        }
        public float GetRatingBonusValue(CombatRating cr)
        {
            float baseResult = ApplyRatingDiminishing(cr, m_activePlayerData.CombatRatings[(int)cr] * GetRatingMultiplier(cr));
            if (cr != CombatRating.ResiliencePlayerDamage)
                return baseResult;
            return (float)(1.0f - Math.Pow(0.99f, baseResult)) * 100.0f;
        }

        void GetDodgeFromAgility(float diminishing, float nondiminishing)
        {
            /*// Table for base dodge values
            float[] dodge_base =
            {
                0.037580f, // Warrior
                0.036520f, // Paladin
                -0.054500f, // Hunter
                -0.005900f, // Rogue
                0.031830f, // Priest
                0.036640f, // DK
                0.016750f, // Shaman
                0.034575f, // Mage
                0.020350f, // Warlock
                0.0f,      // ??
                0.049510f  // Druid
            };
            // Crit/agility to dodge/agility coefficient multipliers; 3.2.0 increased required agility by 15%
            float[] crit_to_dodge =
            {
                0.85f/1.15f,    // Warrior
                1.00f/1.15f,    // Paladin
                1.11f/1.15f,    // Hunter
                2.00f/1.15f,    // Rogue
                1.00f/1.15f,    // Priest
                0.85f/1.15f,    // DK
                1.60f/1.15f,    // Shaman
                1.00f/1.15f,    // Mage
                0.97f/1.15f,    // Warlock (?)
                0.0f,           // ??
                2.00f/1.15f     // Druid
            };

            uint level = getLevel();
            uint pclass = (uint)GetClass();

            if (level > CliDB.GtChanceToMeleeCritStorage.GetTableRowCount())
                level = CliDB.GtChanceToMeleeCritStorage.GetTableRowCount() - 1;

            // Dodge per agility is proportional to crit per agility, which is available from DBC files
            var dodgeRatio = CliDB.GtChanceToMeleeCritStorage.EvaluateTable(level - 1, pclass - 1);
            if (dodgeRatio == null || pclass > (int)Class.Max)
                return;

            // @todo research if talents/effects that increase total agility by x% should increase non-diminishing part
            float base_agility = GetCreateStat(Stats.Agility) * GetPctModifierValue(UnitMods(UNIT_MOD_STAT_START + STAT_AGILITY), BASE_PCT);
            float bonus_agility = GetStat(Stats.Agility) - base_agility;

            // calculate diminishing (green in char screen) and non-diminishing (white) contribution
            diminishing = 100.0f * bonus_agility * dodgeRatio.Value * crit_to_dodge[(int)pclass - 1];
            nondiminishing = 100.0f * (dodge_base[(int)pclass - 1] + base_agility * dodgeRatio.Value * crit_to_dodge[pclass - 1]);
            */
        }

        float ApplyRatingDiminishing(CombatRating cr, float bonusValue)
        {
            uint diminishingCurveId = 0;
            switch (cr)
            {
                case CombatRating.Dodge:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.DodgeDiminishing);
                    break;
                case CombatRating.Parry:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.ParryDiminishing);
                    break;
                case CombatRating.Block:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.BlockDiminishing);
                    break;
                case CombatRating.CritMelee:
                case CombatRating.CritRanged:
                case CombatRating.CritSpell:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.CritDiminishing);
                    break;
                case CombatRating.Speed:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.SpeedDiminishing);
                    break;
                case CombatRating.Lifesteal:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.LifestealDiminishing);
                    break;
                case CombatRating.HasteMelee:
                case CombatRating.HasteRanged:
                case CombatRating.HasteSpell:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.HasteDiminishing);
                    break;
                case CombatRating.Avoidance:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.AvoidanceDiminishing);
                    break;
                case CombatRating.Mastery:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.MasteryDiminishing);
                    break;
                case CombatRating.VersatilityDamageDone:
                case CombatRating.VersatilityHealingDone:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.VersatilityDoneDiminishing);
                    break;
                case CombatRating.VersatilityDamageTaken:
                    diminishingCurveId = Global.DB2Mgr.GetGlobalCurveId(GlobalCurve.VersatilityTakenDiminishing);
                    break;
                default:
                    break;
            }

            if (diminishingCurveId != 0)
                return Global.DB2Mgr.GetCurveValueAt(diminishingCurveId, bonusValue);

            return bonusValue;
        }
        
        public float GetExpertiseDodgeOrParryReduction(WeaponAttackType attType)
        {
            float baseExpertise = 7.5f;
            switch (attType)
            {
                case WeaponAttackType.BaseAttack:
                    return baseExpertise + m_activePlayerData.MainhandExpertise / 4.0f;
                case WeaponAttackType.OffAttack:
                    return baseExpertise + m_activePlayerData.OffhandExpertise / 4.0f;
                default:
                    break;
            }
            return 0.0f;
        }

        public bool IsUseEquipedWeapon(bool mainhand)
        {
            // disarm applied only to mainhand weapon
            return !IsInFeralForm() && (!mainhand || !HasUnitFlag(UnitFlags.Disarmed));
        }

        public void SetCanTitanGrip(bool value, uint penaltySpellId = 0)
        {
            if (value == m_canTitanGrip)
                return;

            m_canTitanGrip = value;
            m_titanGripPenaltySpellId = penaltySpellId;
        }

        void CheckTitanGripPenalty()
        {
            if (!CanTitanGrip())
                return;

            bool apply = IsUsingTwoHandedWeaponInOneHand();
            if (apply)
            {
                if (!HasAura(m_titanGripPenaltySpellId))
                    CastSpell((Unit)null, m_titanGripPenaltySpellId, true);
            }
            else
                RemoveAurasDueToSpell(m_titanGripPenaltySpellId);
        }

        bool IsTwoHandUsed()
        {
            Item mainItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
            if (mainItem == null)
                return false;

            ItemTemplate itemTemplate = mainItem.GetTemplate();
            return (itemTemplate.GetInventoryType() == InventoryType.Weapon2Hand && !CanTitanGrip()) ||
                itemTemplate.GetInventoryType() == InventoryType.Ranged ||
                (itemTemplate.GetInventoryType() == InventoryType.RangedRight && itemTemplate.GetClass() == ItemClass.Weapon && (ItemSubClassWeapon)itemTemplate.GetSubClass() != ItemSubClassWeapon.Wand);
        }

        bool IsUsingTwoHandedWeaponInOneHand()
        {
            Item offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
            if (offItem != null && offItem.GetTemplate().GetInventoryType() == InventoryType.Weapon2Hand)
                return true;

            Item mainItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
            if (mainItem == null || mainItem.GetTemplate().GetInventoryType() == InventoryType.Weapon2Hand)
                return false;

            if (offItem == null)
                return false;

            return true;
        }

        public void _ApplyWeaponDamage(byte slot, Item item, bool apply)
        {
            ItemTemplate proto = item.GetTemplate();
            WeaponAttackType attType = GetAttackBySlot(slot, proto.GetInventoryType());
            if (!IsInFeralForm() && apply && !CanUseAttackType(attType))
                return;

            float damage = 0.0f;
            uint itemLevel = item.GetItemLevel(this);
            float minDamage, maxDamage;
            proto.GetDamage(itemLevel, out minDamage, out maxDamage);

            if (minDamage > 0)
            {
                damage = apply ? minDamage : SharedConst.BaseMinDamage;
                SetBaseWeaponDamage(attType, WeaponDamageRange.MinDamage, damage);
            }

            if (maxDamage > 0)
            {
                damage = apply ? maxDamage : SharedConst.BaseMaxDamage;
                SetBaseWeaponDamage(attType, WeaponDamageRange.MaxDamage, damage);
            }

            SpellShapeshiftFormRecord shapeshift = CliDB.SpellShapeshiftFormStorage.LookupByKey(GetShapeshiftForm());
            if (proto.GetDelay() != 0 && !(shapeshift != null && shapeshift.CombatRoundTime != 0))
                SetBaseAttackTime(attType, apply ? proto.GetDelay() : SharedConst.BaseAttackTime);

            int weaponBasedAttackPower = apply ? (int)(proto.GetDPS(itemLevel) * 6.0f) : 0;
            switch (attType)
            {
                case WeaponAttackType.BaseAttack:
                    SetMainHandWeaponAttackPower(weaponBasedAttackPower);
                    break;
                case WeaponAttackType.OffAttack:
                    SetOffHandWeaponAttackPower(weaponBasedAttackPower);
                    break;
                case WeaponAttackType.RangedAttack:
                    SetRangedWeaponAttackPower(weaponBasedAttackPower);
                    break;
                default:
                    break;
            }

            if (CanModifyStats() && (damage != 0 || proto.GetDelay() != 0))
                UpdateDamagePhysical(attType);
        }

        public override void AtEnterCombat()
        {
            base.AtEnterCombat();
            if (GetCombatManager().HasPvPCombat())
                EnablePvpRules(true);
        }

        public override void AtExitCombat()
        {
            base.AtExitCombat();
            UpdatePotionCooldown();
            m_regenInterruptTimestamp = GameTime.Now();
        }

        public override float GetBlockPercent(uint attackerLevel)
        {
            float blockArmor = (float)m_activePlayerData.ShieldBlock;
            float armorConstant = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, attackerLevel, -2, 0, Class.None, 0);

            if ((blockArmor + armorConstant) == 0)
                return 0;

            return Math.Min(blockArmor / (blockArmor + armorConstant), 0.85f);
        }
        
        public void SetCanParry(bool value)
        {
            if (m_canParry == value)
                return;

            m_canParry = value;
            UpdateParryPercentage();
        }

        public void SetCanBlock(bool value)
        {
            if (m_canBlock == value)
                return;

            m_canBlock = value;
            UpdateBlockPercentage();
        }

        // duel health and mana reset methods
        public void SaveHealthBeforeDuel() { healthBeforeDuel = (uint)GetHealth(); }
        public void SaveManaBeforeDuel() { manaBeforeDuel = (uint)GetPower(PowerType.Mana); }
        public void RestoreHealthAfterDuel() { SetHealth(healthBeforeDuel); }
        public void RestoreManaAfterDuel() { SetPower(PowerType.Mana, (int)manaBeforeDuel); }

        void UpdateDuelFlag(long currTime)
        {
            if (duel != null && duel.State == DuelState.Countdown && duel.StartTime <= currTime)
            {
                Global.ScriptMgr.OnPlayerDuelStart(this, duel.Opponent);

                SetDuelTeam(1);
                duel.Opponent.SetDuelTeam(2);

                duel.State = DuelState.InProgress;
                duel.Opponent.duel.State = DuelState.InProgress;
            }
        }

        void CheckDuelDistance(long currTime)
        {
            if (duel == null)
                return;

            ObjectGuid duelFlagGUID = m_playerData.DuelArbiter;
            GameObject obj = GetMap().GetGameObject(duelFlagGUID);
            if (obj == null)
                return;

            if (duel.OutOfBoundsTime == 0)
            {
                if (!IsWithinDistInMap(obj, 50))
                {
                    duel.OutOfBoundsTime = currTime + 10;
                    SendPacket(new DuelOutOfBounds());
                }
            }
            else
            {
                if (IsWithinDistInMap(obj, 40))
                {
                    duel.OutOfBoundsTime = 0;
                    SendPacket(new DuelInBounds());
                }
                else if (currTime >= duel.OutOfBoundsTime)
                    DuelComplete(DuelCompleteType.Fled);
            }
        }
        public void DuelComplete(DuelCompleteType type)
        {
            // duel not requested
            if (duel == null)
                return;

            // Check if DuelComplete() has been called already up in the stack and in that case don't do anything else here
            if (duel.State == DuelState.Completed)
                return;

            Player opponent = duel.Opponent;
            duel.State = DuelState.Completed;
            opponent.duel.State = DuelState.Completed;

            Log.outDebug(LogFilter.Player, $"Duel Complete {GetName()} {opponent.GetName()}");

            DuelComplete duelCompleted = new();
            duelCompleted.Started = type != DuelCompleteType.Interrupted;
            SendPacket(duelCompleted);

            if (opponent.GetSession() != null)
                opponent.SendPacket(duelCompleted);

            if (type != DuelCompleteType.Interrupted)
            {
                DuelWinner duelWinner = new();
                duelWinner.BeatenName = (type == DuelCompleteType.Won ? opponent : this).GetName();
                duelWinner.WinnerName = (type == DuelCompleteType.Won ? this : opponent).GetName();
                duelWinner.BeatenVirtualRealmAddress = (type == DuelCompleteType.Won ? opponent : this).m_playerData.VirtualPlayerRealm;
                duelWinner.WinnerVirtualRealmAddress = (type == DuelCompleteType.Won ? this : opponent).m_playerData.VirtualPlayerRealm;
                duelWinner.Fled = type != DuelCompleteType.Won;

                SendMessageToSet(duelWinner, true);
            }

            opponent.DisablePvpRules();
            DisablePvpRules();

            Global.ScriptMgr.OnPlayerDuelEnd(opponent, this, type);

            switch (type)
            {
                case DuelCompleteType.Fled:
                    // if initiator and opponent are on the same team
                    // or initiator and opponent are not PvP enabled, forcibly stop attacking
                    if (GetEffectiveTeam() == opponent.GetEffectiveTeam())
                    {
                        AttackStop();
                        opponent.AttackStop();
                    }
                    else
                    {
                        if (!IsPvP())
                            AttackStop();
                        if (!opponent.IsPvP())
                            opponent.AttackStop();
                    }
                    break;
                case DuelCompleteType.Won:
                    UpdateCriteria(CriteriaType.LoseDuel, 1);
                    opponent.UpdateCriteria(CriteriaType.WinDuel, 1);

                    // Credit for quest Death's Challenge
                    if (GetClass() == Class.DeathKnight && opponent.GetQuestStatus(12733) == QuestStatus.Incomplete)
                        opponent.CastSpell(duel.Opponent, 52994, true);

                    // Honor points after duel (the winner) - ImpConfig
                    int amount = WorldConfig.GetIntValue(WorldCfg.HonorAfterDuel);
                    if (amount != 0)
                        opponent.RewardHonor(null, 1, amount);

                    break;
                default:
                    break;
            }

            // Victory emote spell
            if (type != DuelCompleteType.Interrupted)
                opponent.CastSpell(duel.Opponent, 52852, true);

            //Remove Duel Flag object
            GameObject obj = GetMap().GetGameObject(m_playerData.DuelArbiter);
            if (obj != null)
                duel.Initiator.RemoveGameObject(obj, true);

            //remove auras
            var itsAuras = opponent.GetAppliedAuras();
            foreach (var pair in itsAuras)
            {
                Aura aura = pair.Value.GetBase();
                if (!pair.Value.IsPositive() && aura.GetCasterGUID() == GetGUID() && aura.GetApplyTime() >= duel.StartTime)
                    opponent.RemoveAura(pair);
            }

            var myAuras = GetAppliedAuras();
            foreach (var pair in myAuras)
            {
                Aura aura = pair.Value.GetBase();
                if (!pair.Value.IsPositive() && aura.GetCasterGUID() == opponent.GetGUID() && aura.GetApplyTime() >= duel.StartTime)
                    RemoveAura(pair);
            }

            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.DuelEnd);
            opponent.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.DuelEnd);

            // cleanup combo points
            SetPower(PowerType.ComboPoints, 0);
            opponent.SetPower(PowerType.ComboPoints, 0);

            //cleanups
            SetDuelArbiter(ObjectGuid.Empty);
            SetDuelTeam(0);
            opponent.SetDuelArbiter(ObjectGuid.Empty);
            opponent.SetDuelTeam(0);

            opponent.duel = null;
            duel = null;
        }
        public void SetDuelArbiter(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.DuelArbiter), guid); }
        void SetDuelTeam(uint duelTeam) { SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.DuelTeam), duelTeam); }

        //PVP
        public void SetPvPDeath(bool on)
        {
            if (on)
                m_ExtraFlags |= PlayerExtraFlags.PVPDeath;
            else
                m_ExtraFlags &= ~PlayerExtraFlags.PVPDeath;
        }

        public void SetContestedPvPTimer(uint newTime) { m_contestedPvPTimer = newTime; }

        public void ResetContestedPvP()
        {
            ClearUnitState(UnitState.AttackPlayer);
            RemovePlayerFlag(PlayerFlags.ContestedPVP);
            m_contestedPvPTimer = 0;
        }
        void UpdateAfkReport(long currTime)
        {
            if (m_bgData.bgAfkReportedTimer <= currTime)
            {
                m_bgData.bgAfkReportedCount = 0;
                m_bgData.bgAfkReportedTimer = currTime + 5 * Time.Minute;
            }
        }

        public void SetContestedPvP(Player attackedPlayer = null)
        {
            if (attackedPlayer != null && (attackedPlayer == this || (duel != null && duel.Opponent == attackedPlayer)))
                return;

            SetContestedPvPTimer(30000);
            if (!HasUnitState(UnitState.AttackPlayer))
            {
                AddUnitState(UnitState.AttackPlayer);
                SetPlayerFlag(PlayerFlags.ContestedPVP);
                // call MoveInLineOfSight for nearby contested guards
                AIRelocationNotifier notifier = new(this);
                Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
            }
            foreach (Unit unit in m_Controlled)
            {
                if (!unit.HasUnitState(UnitState.AttackPlayer))
                {
                    unit.AddUnitState(UnitState.AttackPlayer);
                    AIRelocationNotifier notifier = new(unit);
                    Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
                }
            }
        }
        
        public void UpdateContestedPvP(uint diff)
        {
            if (m_contestedPvPTimer == 0 || IsInCombat())
                return;

            if (m_contestedPvPTimer <= diff)
                ResetContestedPvP();
            else
                m_contestedPvPTimer -= diff;
        }

        public void UpdatePvPFlag(long currTime)
        {
            if (!IsPvP())
                return;

            if (pvpInfo.EndTimer == 0 || (currTime < pvpInfo.EndTimer + 300) || pvpInfo.IsHostile)
                return;

            if (pvpInfo.EndTimer <= currTime)
            {
                pvpInfo.EndTimer = 0;
                RemovePlayerFlag(PlayerFlags.PVPTimer);
            }
            
            UpdatePvP(false);
        }

        public void UpdatePvP(bool state, bool Override = false)
        {
            if (!state || Override)
            {
                SetPvP(state);
                pvpInfo.EndTimer = 0;
            }
            else
            {
                pvpInfo.EndTimer = GameTime.GetGameTime();
                SetPvP(state);
            }
        }

        void InitPvP()
        {
            // pvp flag should stay after relog
            if (HasPlayerFlag(PlayerFlags.InPVP))
                UpdatePvP(true, true);
        }

        public void UpdatePvPState(bool onlyFFA = false)
        {
            // @todo should we always synchronize UNIT_FIELD_BYTES_2, 1 of controller and controlled?
            // no, we shouldn't, those are checked for affecting player by client
            if (!pvpInfo.IsInNoPvPArea && !IsGameMaster()
                && (pvpInfo.IsInFFAPvPArea || Global.WorldMgr.IsFFAPvPRealm() || HasAuraType(AuraType.SetFFAPvp)))
            {
                if (!IsFFAPvP())
                {
                    SetPvpFlag(UnitPVPStateFlags.FFAPvp);
                    foreach (var unit in m_Controlled)
                        unit.SetPvpFlag(UnitPVPStateFlags.FFAPvp);
                }
            }
            else if (IsFFAPvP())
            {
                RemovePvpFlag(UnitPVPStateFlags.FFAPvp);
                foreach (var unit in m_Controlled)
                    unit.RemovePvpFlag(UnitPVPStateFlags.FFAPvp);
            }

            if (onlyFFA)
                return;

            if (pvpInfo.IsHostile)                               // in hostile area
            {
                if (!IsPvP() || pvpInfo.EndTimer != 0)
                    UpdatePvP(true, true);
            }
            else                                                    // in friendly area
            {
                if (IsPvP() && !HasPlayerFlag(PlayerFlags.InPVP) && pvpInfo.EndTimer == 0)
                    pvpInfo.EndTimer = GameTime.GetGameTime();                  // start toggle-off
            }
        }

        public override void SetPvP(bool state)
        {
            base.SetPvP(state);
            foreach (var unit in m_Controlled)
                unit.SetPvP(state);
        }
    }
}
