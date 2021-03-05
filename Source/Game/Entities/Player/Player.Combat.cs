﻿/*
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
        private void SetRegularAttackTime()
        {
            for (WeaponAttackType weaponAttackType = 0; weaponAttackType < WeaponAttackType.Max; ++weaponAttackType)
            {
                var tmpitem = GetWeaponForAttack(weaponAttackType, true);
                if (tmpitem != null && !tmpitem.IsBroken())
                {
                    var proto = tmpitem.GetTemplate();
                    if (proto.GetDelay() != 0)
                        SetBaseAttackTime(weaponAttackType, proto.GetDelay());
                }
                else
                    SetBaseAttackTime(weaponAttackType, SharedConst.BaseAttackTime);  // If there is no weapon reset attack time to base (might have been changed from forms)
            }
        }

        public void RewardPlayerAndGroupAtKill(Unit victim, bool isBattleground)
        {
            new KillRewarder(this, victim, isBattleground).Reward();
        }

        public void RewardPlayerAndGroupAtEvent(uint creature_id, WorldObject pRewardSource)
        {
            if (pRewardSource == null)
                return;
            var creature_guid = pRewardSource.IsTypeId(TypeId.Unit) ? pRewardSource.GetGUID() : ObjectGuid.Empty;

            // prepare data for near group iteration
            var group = GetGroup();
            if (group)
            {
                for (var refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    var player = refe.GetSource();
                    if (!player)
                        continue;

                    if (!player.IsAtGroupRewardDistance(pRewardSource))
                        continue;                               // member (alive or dead) or his corpse at req. distance

                    // quest objectives updated only for alive group member or dead but with not released body
                    if (player.IsAlive() || !player.GetCorpse())
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
            var packet = new SetProficiency();
            packet.ProficiencyMask = itemSubclassMask;
            packet.ProficiencyClass = (byte)itemClass;
            SendPacket(packet);
        }

        private bool CanTitanGrip() { return m_canTitanGrip; }

        private float GetRatingMultiplier(CombatRating cr)
        {
            var Rating = CliDB.CombatRatingsGameTable.GetRow(GetLevel());
            if (Rating == null)
                return 1.0f;

            var value = GetGameTableColumnForCombatRating(Rating, cr);
            if (value == 0)
                return 1.0f;                                        // By default use minimum coefficient (not must be called)

            return 1.0f / value;
        }
        public float GetRatingBonusValue(CombatRating cr)
        {
            var baseResult = ApplyRatingDiminishing(cr, m_activePlayerData.CombatRatings[(int)cr] * GetRatingMultiplier(cr));
            if (cr != CombatRating.ResiliencePlayerDamage)
                return baseResult;
            return (float)(1.0f - Math.Pow(0.99f, baseResult)) * 100.0f;
        }

        private void GetDodgeFromAgility(float diminishing, float nondiminishing)
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

        private float ApplyRatingDiminishing(CombatRating cr, float bonusValue)
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
            var baseExpertise = 7.5f;
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

        private void CheckTitanGripPenalty()
        {
            if (!CanTitanGrip())
                return;

            var apply = IsUsingTwoHandedWeaponInOneHand();
            if (apply)
            {
                if (!HasAura(m_titanGripPenaltySpellId))
                    CastSpell((Unit)null, m_titanGripPenaltySpellId, true);
            }
            else
                RemoveAurasDueToSpell(m_titanGripPenaltySpellId);
        }

        private bool IsTwoHandUsed()
        {
            var mainItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
            if (!mainItem)
                return false;

            var itemTemplate = mainItem.GetTemplate();
            return (itemTemplate.GetInventoryType() == InventoryType.Weapon2Hand && !CanTitanGrip()) ||
                itemTemplate.GetInventoryType() == InventoryType.Ranged ||
                (itemTemplate.GetInventoryType() == InventoryType.RangedRight && itemTemplate.GetClass() == ItemClass.Weapon && (ItemSubClassWeapon)itemTemplate.GetSubClass() != ItemSubClassWeapon.Wand);
        }

        private bool IsUsingTwoHandedWeaponInOneHand()
        {
            var offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
            if (offItem && offItem.GetTemplate().GetInventoryType() == InventoryType.Weapon2Hand)
                return true;

            var mainItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
            if (!mainItem || mainItem.GetTemplate().GetInventoryType() == InventoryType.Weapon2Hand)
                return false;

            if (!offItem)
                return false;

            return true;
        }

        public void _ApplyWeaponDamage(uint slot, Item item, bool apply)
        {
            var proto = item.GetTemplate();
            var attType = WeaponAttackType.BaseAttack;
            var damage = 0.0f;

            if (slot == EquipmentSlot.MainHand && (proto.GetInventoryType() == InventoryType.Ranged || proto.GetInventoryType() == InventoryType.RangedRight))
                attType = WeaponAttackType.RangedAttack;
            else if (slot == EquipmentSlot.OffHand)
                attType = WeaponAttackType.OffAttack;

            var itemLevel = item.GetItemLevel(this);
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

            var shapeshift = CliDB.SpellShapeshiftFormStorage.LookupByKey(GetShapeshiftForm());
            if (proto.GetDelay() != 0 && !(shapeshift != null && shapeshift.CombatRoundTime != 0))
                SetBaseAttackTime(attType, apply ? proto.GetDelay() : SharedConst.BaseAttackTime);

            var weaponBasedAttackPower = apply ? (int)(proto.GetDPS(itemLevel) * 6.0f) : 0;
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

        public override float GetBlockPercent(uint attackerLevel)
        {
            var blockArmor = (float)m_activePlayerData.ShieldBlock;
            var armorConstant = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, attackerLevel, -2, 0, Class.None);

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

        private void UpdateDuelFlag(long currTime)
        {
            if (duel == null || duel.startTimer == 0 || currTime < duel.startTimer + 3)
                return;

            Global.ScriptMgr.OnPlayerDuelStart(this, duel.opponent);

            SetDuelTeam(1);
            duel.opponent.SetDuelTeam(2);

            duel.startTimer = 0;
            duel.startTime = currTime;
            duel.opponent.duel.startTimer = 0;
            duel.opponent.duel.startTime = currTime;
        }

        private void CheckDuelDistance(long currTime)
        {
            if (duel == null)
                return;

            ObjectGuid duelFlagGUID = m_playerData.DuelArbiter;
            var obj = GetMap().GetGameObject(duelFlagGUID);
            if (!obj)
                return;

            if (duel.outOfBound == 0)
            {
                if (!IsWithinDistInMap(obj, 50))
                {
                    duel.outOfBound = currTime;
                    SendPacket(new DuelOutOfBounds());
                }
            }
            else
            {
                if (IsWithinDistInMap(obj, 40))
                {
                    duel.outOfBound = 0;
                    SendPacket(new DuelInBounds());
                }
                else if (currTime >= (duel.outOfBound + 10))
                    DuelComplete(DuelCompleteType.Fled);
            }
        }
        public void DuelComplete(DuelCompleteType type)
        {
            // duel not requested
            if (duel == null)
                return;

            // Check if DuelComplete() has been called already up in the stack and in that case don't do anything else here
            if (duel.isCompleted || duel.opponent.duel.isCompleted)
                return;

            duel.isCompleted = true;
            duel.opponent.duel.isCompleted = true;

            Log.outDebug(LogFilter.Player, "Duel Complete {0} {1}", GetName(), duel.opponent.GetName());

            var duelCompleted = new DuelComplete();
            duelCompleted.Started = type != DuelCompleteType.Interrupted;
            SendPacket(duelCompleted);

            if (duel.opponent.GetSession() != null)
                duel.opponent.SendPacket(duelCompleted);

            if (type != DuelCompleteType.Interrupted)
            {
                var duelWinner = new DuelWinner();
                duelWinner.BeatenName = (type == DuelCompleteType.Won ? duel.opponent.GetName() : GetName());
                duelWinner.WinnerName = (type == DuelCompleteType.Won ? GetName() : duel.opponent.GetName());
                duelWinner.BeatenVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
                duelWinner.WinnerVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
                duelWinner.Fled = type != DuelCompleteType.Won;

                SendMessageToSet(duelWinner, true);
            }

            duel.opponent.DisablePvpRules();
            DisablePvpRules();

            Global.ScriptMgr.OnPlayerDuelEnd(duel.opponent, this, type);

            switch (type)
            {
                case DuelCompleteType.Fled:
                    // if initiator and opponent are on the same team
                    // or initiator and opponent are not PvP enabled, forcibly stop attacking
                    if (duel.initiator.GetTeam() == duel.opponent.GetTeam())
                    {
                        duel.initiator.AttackStop();
                        duel.opponent.AttackStop();
                    }
                    else
                    {
                        if (!duel.initiator.IsPvP())
                            duel.initiator.AttackStop();
                        if (!duel.opponent.IsPvP())
                            duel.opponent.AttackStop();
                    }
                    break;
                case DuelCompleteType.Won:
                    UpdateCriteria(CriteriaTypes.LoseDuel, 1);
                    duel.opponent.UpdateCriteria(CriteriaTypes.WinDuel, 1);

                    // Credit for quest Death's Challenge
                    if (GetClass() == Class.Deathknight && duel.opponent.GetQuestStatus(12733) == QuestStatus.Incomplete)
                        duel.opponent.CastSpell(duel.opponent, 52994, true);

                    // Honor points after duel (the winner) - ImpConfig
                    var amount = WorldConfig.GetIntValue(WorldCfg.HonorAfterDuel);
                    if (amount != 0)
                        duel.opponent.RewardHonor(null, 1, amount);

                    break;
                default:
                    break;
            }

            // Victory emote spell
            if (type != DuelCompleteType.Interrupted)
                duel.opponent.CastSpell(duel.opponent, 52852, true);

            //Remove Duel Flag object
            var obj = GetMap().GetGameObject(m_playerData.DuelArbiter);
            if (obj)
                duel.initiator.RemoveGameObject(obj, true);

            //remove auras
            var itsAuras = duel.opponent.GetAppliedAuras();
            foreach (var pair in itsAuras)
            {
                var aura = pair.Value.GetBase();
                if (!pair.Value.IsPositive() && aura.GetCasterGUID() == GetGUID() && aura.GetApplyTime() >= duel.startTime)
                    duel.opponent.RemoveAura(pair);
            }

            var myAuras = GetAppliedAuras();
            foreach (var pair in myAuras)
            {
                var aura = pair.Value.GetBase();
                if (!pair.Value.IsPositive() && aura.GetCasterGUID() == duel.opponent.GetGUID() && aura.GetApplyTime() >= duel.startTime)
                    RemoveAura(pair);
            }

            // cleanup combo points
            ClearComboPoints();
            duel.opponent.ClearComboPoints();

            //cleanups
            SetDuelArbiter(ObjectGuid.Empty);
            SetDuelTeam(0);
            duel.opponent.SetDuelArbiter(ObjectGuid.Empty);
            duel.opponent.SetDuelTeam(0);

            duel.opponent.duel = null;
            duel = null;
        }
        public void SetDuelArbiter(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.DuelArbiter), guid); }
        private void SetDuelTeam(uint duelTeam) { SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.DuelTeam), duelTeam); }

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

        private void UpdateAfkReport(long currTime)
        {
            if (m_bgData.bgAfkReportedTimer <= currTime)
            {
                m_bgData.bgAfkReportedCount = 0;
                m_bgData.bgAfkReportedTimer = currTime + 5 * Time.Minute;
            }
        }

        public void SetContestedPvP(Player attackedPlayer = null)
        {
            if (attackedPlayer != null && (attackedPlayer == this || (duel != null && duel.opponent == attackedPlayer)))
                return;

            SetContestedPvPTimer(30000);
            if (!HasUnitState(UnitState.AttackPlayer))
            {
                AddUnitState(UnitState.AttackPlayer);
                AddPlayerFlag(PlayerFlags.ContestedPVP);
                // call MoveInLineOfSight for nearby contested guards
                var notifier = new AIRelocationNotifier(this);
                Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
            }
            foreach (var unit in m_Controlled)
            {
                if (!unit.HasUnitState(UnitState.AttackPlayer))
                {
                    unit.AddUnitState(UnitState.AttackPlayer);
                    var notifier = new AIRelocationNotifier(unit);
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

            if (pvpInfo.EndTimer == 0 || currTime < (pvpInfo.EndTimer + 300) || pvpInfo.IsHostile)
                return;

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
                pvpInfo.EndTimer = Time.UnixTime;
                SetPvP(state);
            }
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
                    AddPvpFlag(UnitPVPStateFlags.FFAPvp);
                    foreach (var unit in m_Controlled)
                        unit.AddPvpFlag(UnitPVPStateFlags.FFAPvp);
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
                    pvpInfo.EndTimer = Time.UnixTime;                  // start toggle-off
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
