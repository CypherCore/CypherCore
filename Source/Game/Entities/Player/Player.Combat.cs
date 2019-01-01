/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;

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

        public void RewardPlayerAndGroupAtKill(Unit victim, bool isBattleground)
        {
            new KillRewarder(this, victim, isBattleground).Reward();
        }

        public void RewardPlayerAndGroupAtEvent(uint creature_id, WorldObject pRewardSource)
        {
            if (pRewardSource == null)
                return;
            ObjectGuid creature_guid = pRewardSource.IsTypeId(TypeId.Unit) ? pRewardSource.GetGUID() : ObjectGuid.Empty;

            // prepare data for near group iteration
            Group group = GetGroup();
            if (group)
            {
                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                {
                    Player player = refe.GetSource();
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
            SetProficiency packet = new SetProficiency();
            packet.ProficiencyMask = itemSubclassMask;
            packet.ProficiencyClass = (byte)itemClass;
            SendPacket(packet);
        }

        bool CanTitanGrip() { return m_canTitanGrip; }

        public override bool CanUseAttackType(WeaponAttackType attacktype)
        {
            switch (attacktype)
            {
                case WeaponAttackType.BaseAttack:
                    return !HasFlag(UnitFields.Flags, UnitFlags.Disarmed);
                case WeaponAttackType.OffAttack:
                    return !HasFlag(UnitFields.Flags2, UnitFlags2.DisarmOffhand);
                case WeaponAttackType.RangedAttack:
                    return !HasFlag(UnitFields.Flags2, UnitFlags2.DisarmRanged);
            }
            return true;
        }

        float GetRatingMultiplier(CombatRating cr)
        {
            GtCombatRatingsRecord Rating = CliDB.CombatRatingsGameTable.GetRow(getLevel());
            if (Rating == null)
                return 1.0f;

            float value = GetGameTableColumnForCombatRating(Rating, cr);
            if (value == 0)
                return 1.0f;                                        // By default use minimum coefficient (not must be called)

            return 1.0f / value;
        }
        public float GetRatingBonusValue(CombatRating cr)
        {
            float baseResult = GetFloatValue(ActivePlayerFields.CombatRating + (int)cr) * GetRatingMultiplier(cr);
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
            float base_agility = GetCreateStat(Stats.Agility) * m_auraModifiersGroup[(int)UnitMods.StatAgility][(int)UnitModifierType.BasePCT];
            float bonus_agility = GetStat(Stats.Agility) - base_agility;

            // calculate diminishing (green in char screen) and non-diminishing (white) contribution
            diminishing = 100.0f * bonus_agility * dodgeRatio.Value * crit_to_dodge[(int)pclass - 1];
            nondiminishing = 100.0f * (dodge_base[(int)pclass - 1] + base_agility * dodgeRatio.Value * crit_to_dodge[pclass - 1]);
            */
        }

        float GetTotalPercentageModValue(BaseModGroup modGroup)
        {
            return m_auraBaseMod[(int)modGroup][0] + m_auraBaseMod[(int)modGroup][1];
        }

        public float GetExpertiseDodgeOrParryReduction(WeaponAttackType attType)
        {
            float baseExpertise = 7.5f;
            switch (attType)
            {
                case WeaponAttackType.BaseAttack:
                    return baseExpertise + GetUInt32Value(ActivePlayerFields.Expertise) / 4.0f;
                case WeaponAttackType.OffAttack:
                    return baseExpertise + GetUInt32Value(ActivePlayerFields.OffhandExpertise) / 4.0f;
                default:
                    break;
            }
            return 0.0f;
        }

        public bool IsUseEquipedWeapon(bool mainhand)
        {
            // disarm applied only to mainhand weapon
            return !IsInFeralForm() && (!mainhand || !HasFlag(UnitFields.Flags, UnitFlags.Disarmed));
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
            if (!mainItem)
                return false;

            ItemTemplate itemTemplate = mainItem.GetTemplate();
            return (itemTemplate.GetInventoryType() == InventoryType.Weapon2Hand && !CanTitanGrip()) ||
                itemTemplate.GetInventoryType() == InventoryType.Ranged ||
                (itemTemplate.GetInventoryType() == InventoryType.RangedRight && itemTemplate.GetClass() == ItemClass.Weapon && (ItemSubClassWeapon)itemTemplate.GetSubClass() != ItemSubClassWeapon.Wand);
        }

        bool IsUsingTwoHandedWeaponInOneHand()
        {
            Item offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
            if (offItem && offItem.GetTemplate().GetInventoryType() == InventoryType.Weapon2Hand)
                return true;

            Item mainItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
            if (!mainItem || mainItem.GetTemplate().GetInventoryType() == InventoryType.Weapon2Hand)
                return false;

            if (!offItem)
                return false;

            return true;
        }

        public void _ApplyWeaponDamage(uint slot, Item item, bool apply)
        {
            ItemTemplate proto = item.GetTemplate();
            WeaponAttackType attType = WeaponAttackType.BaseAttack;
            float damage = 0.0f;

            if (slot == EquipmentSlot.MainHand && (proto.GetInventoryType() == InventoryType.Ranged || proto.GetInventoryType() == InventoryType.RangedRight))
                attType = WeaponAttackType.RangedAttack;
            else if (slot == EquipmentSlot.OffHand)
                attType = WeaponAttackType.OffAttack;

            float minDamage, maxDamage;
            item.GetDamage(this, out minDamage, out maxDamage);

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

            if (CanModifyStats() && (damage != 0 || proto.GetDelay() != 0))
                UpdateDamagePhysical(attType);
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
            if (duel == null || duel.startTimer == 0 || currTime < duel.startTimer + 3)
                return;

            Global.ScriptMgr.OnPlayerDuelStart(this, duel.opponent);

            SetUInt32Value(PlayerFields.DuelTeam, 1);
            duel.opponent.SetUInt32Value(PlayerFields.DuelTeam, 2);

            duel.startTimer = 0;
            duel.startTime = currTime;
            duel.opponent.duel.startTimer = 0;
            duel.opponent.duel.startTime = currTime;
        }

        void CheckDuelDistance(long currTime)
        {
            if (duel == null)
                return;

            ObjectGuid duelFlagGUID = GetGuidValue(PlayerFields.DuelArbiter);
            GameObject obj = GetMap().GetGameObject(duelFlagGUID);
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

            DuelComplete duelCompleted = new DuelComplete();
            duelCompleted.Started = type != DuelCompleteType.Interrupted;
            SendPacket(duelCompleted);

            if (duel.opponent.GetSession() != null)
                duel.opponent.SendPacket(duelCompleted);

            if (type != DuelCompleteType.Interrupted)
            {
                DuelWinner duelWinner = new DuelWinner();
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
                    int amount = WorldConfig.GetIntValue(WorldCfg.HonorAfterDuel);
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
            GameObject obj = GetMap().GetGameObject(GetGuidValue(PlayerFields.DuelArbiter));
            if (obj)
                duel.initiator.RemoveGameObject(obj, true);

            //remove auras
            var itsAuras = duel.opponent.GetAppliedAuras();
            foreach (var pair in itsAuras)
            {
                Aura aura = pair.Value.GetBase();
                if (!pair.Value.IsPositive() && aura.GetCasterGUID() == GetGUID() && aura.GetApplyTime() >= duel.startTime)
                    duel.opponent.RemoveAura(pair);
            }

            var myAuras = GetAppliedAuras();
            foreach (var pair in myAuras)
            {
                Aura aura = pair.Value.GetBase();
                if (!pair.Value.IsPositive() && aura.GetCasterGUID() == duel.opponent.GetGUID() && aura.GetApplyTime() >= duel.startTime)
                    RemoveAura(pair);
            }

            // cleanup combo points
            ClearComboPoints();
            duel.opponent.ClearComboPoints();

            //cleanups
            SetGuidValue(PlayerFields.DuelArbiter, ObjectGuid.Empty);
            SetUInt32Value(PlayerFields.DuelTeam, 0);
            duel.opponent.SetGuidValue(PlayerFields.DuelArbiter, ObjectGuid.Empty);
            duel.opponent.SetUInt32Value(PlayerFields.DuelTeam, 0);

            duel.opponent.duel = null;
            duel = null;
        }

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
            RemoveFlag(PlayerFields.Flags, PlayerFlags.ContestedPVP);
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

        public void UpdateContestedPvP(uint diff)
        {
            if (m_contestedPvPTimer == 0 || IsInCombat())
                return;

            if (m_contestedPvPTimer <= diff)
            {
                ResetContestedPvP();
            }
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
                && (pvpInfo.IsInFFAPvPArea || Global.WorldMgr.IsFFAPvPRealm()))
            {
                if (!IsFFAPvP())
                {
                    SetByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.FFAPvp);
                    foreach (var unit in m_Controlled)
                        unit.SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, (byte)UnitBytes2Flags.FFAPvp);
                }
            }
            else if (IsFFAPvP())
            {
                RemoveByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.FFAPvp);
                foreach (var unit in m_Controlled)
                    unit.RemoveByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.FFAPvp);
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
                if (IsPvP() && !HasFlag(PlayerFields.Flags, PlayerFlags.InPVP) && pvpInfo.EndTimer == 0)
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
