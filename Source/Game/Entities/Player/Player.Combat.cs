// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Groups;
using Game.Maps;
using Game.Maps.Notifiers;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Game.Entities
{
    public partial class Player
    {
        public void RewardPlayerAndGroupAtEvent(uint creature_id, WorldObject pRewardSource)
        {
            if (pRewardSource == null)
                return;

            ObjectGuid creature_guid = pRewardSource.IsTypeId(TypeId.Unit) ? pRewardSource.GetGUID() : ObjectGuid.Empty;

            // prepare _data for near group iteration
            Group group = GetGroup();

            if (group)
                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player player = refe.GetSource();

                    if (!player)
                        continue;

                    if (!player.IsAtGroupRewardDistance(pRewardSource))
                        continue; // member (alive or dead) or his corpse at req. distance

                    // quest objectives updated only for alive group member or dead but with not released body
                    if (player.IsAlive() ||
                        !player.GetCorpse())
                        player.KilledMonsterCredit(creature_id, creature_guid);
                }
            else
                KilledMonsterCredit(creature_id, creature_guid);
        }

        public void AddWeaponProficiency(uint newflag)
        {
            _weaponProficiency |= newflag;
        }

        public void AddArmorProficiency(uint newflag)
        {
            _armorProficiency |= newflag;
        }

        public uint GetWeaponProficiency()
        {
            return _weaponProficiency;
        }

        public uint GetArmorProficiency()
        {
            return _armorProficiency;
        }

        public void SendProficiency(ItemClass itemClass, uint itemSubclassMask)
        {
            SetProficiency packet = new();
            packet.ProficiencyMask = itemSubclassMask;
            packet.ProficiencyClass = (byte)itemClass;
            SendPacket(packet);
        }

        public float GetRatingBonusValue(CombatRating cr)
        {
            float baseResult = ApplyRatingDiminishing(cr, ActivePlayerData.CombatRatings[(int)cr] * GetRatingMultiplier(cr));

            if (cr != CombatRating.ResiliencePlayerDamage)
                return baseResult;

            return (float)(1.0f - Math.Pow(0.99f, baseResult)) * 100.0f;
        }

        public float GetExpertiseDodgeOrParryReduction(WeaponAttackType attType)
        {
            float baseExpertise = 7.5f;

            switch (attType)
            {
                case WeaponAttackType.BaseAttack:
                    return baseExpertise + ActivePlayerData.MainhandExpertise / 4.0f;
                case WeaponAttackType.OffAttack:
                    return baseExpertise + ActivePlayerData.OffhandExpertise / 4.0f;
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
            if (value == _canTitanGrip)
                return;

            _canTitanGrip = value;
            _titanGripPenaltySpellId = penaltySpellId;
        }

        public void _ApplyWeaponDamage(byte slot, Item item, bool apply)
        {
            ItemTemplate proto = item.GetTemplate();
            WeaponAttackType attType = GetAttackBySlot(slot, proto.GetInventoryType());

            if (!IsInFeralForm() &&
                apply &&
                !CanUseAttackType(attType))
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

            if (proto.GetDelay() != 0 &&
                !(shapeshift != null && shapeshift.CombatRoundTime != 0))
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

            if (CanModifyStats() &&
                (damage != 0 || proto.GetDelay() != 0))
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
            _combatExitTime = Time.GetMSTime();
        }

        public override float GetBlockPercent(uint attackerLevel)
        {
            float blockArmor = (float)ActivePlayerData.ShieldBlock;
            float armorConstant = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, attackerLevel, -2, 0, Class.None);

            if ((blockArmor + armorConstant) == 0)
                return 0;

            return Math.Min(blockArmor / (blockArmor + armorConstant), 0.85f);
        }

        public void SetCanParry(bool value)
        {
            if (_canParry == value)
                return;

            _canParry = value;
            UpdateParryPercentage();
        }

        public void SetCanBlock(bool value)
        {
            if (_canBlock == value)
                return;

            _canBlock = value;
            UpdateBlockPercentage();
        }

        // Duel health and mana reset methods
        public void SaveHealthBeforeDuel()
        {
            _healthBeforeDuel = (uint)GetHealth();
        }

        public void SaveManaBeforeDuel()
        {
            _manaBeforeDuel = (uint)GetPower(PowerType.Mana);
        }

        public void RestoreHealthAfterDuel()
        {
            SetHealth(_healthBeforeDuel);
        }

        public void RestoreManaAfterDuel()
        {
            SetPower(PowerType.Mana, (int)_manaBeforeDuel);
        }

        public void DuelComplete(DuelCompleteType type)
        {
            // Duel not requested
            if (Duel == null)
                return;

            // Check if DuelComplete() has been called already up in the stack and in that case don't do anything else here
            if (Duel.State == DuelState.Completed)
                return;

            Player opponent = Duel.Opponent;
            Duel.State = DuelState.Completed;
            opponent.Duel.State = DuelState.Completed;

            Log.outDebug(LogFilter.Player, $"Duel Complete {GetName()} {opponent.GetName()}");

            DuelComplete duelCompleted = new();
            duelCompleted.Started = type != DuelCompleteType.Interrupted;
            SendPacket(duelCompleted);

            if (opponent.GetSession() != null)
                opponent.SendPacket(duelCompleted);

            if (type != DuelCompleteType.Interrupted)
            {
                DuelWinner duelWinner = new();
                duelWinner.BeatenName = (type == DuelCompleteType.Won ? opponent.GetName() : GetName());
                duelWinner.WinnerName = (type == DuelCompleteType.Won ? GetName() : opponent.GetName());
                duelWinner.BeatenVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
                duelWinner.WinnerVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
                duelWinner.Fled = type != DuelCompleteType.Won;

                SendMessageToSet(duelWinner, true);
            }

            opponent.DisablePvpRules();
            DisablePvpRules();

            Global.ScriptMgr.ForEach<IPlayerOnDuelEnd>(p => p.OnDuelEnd(type == DuelCompleteType.Won ? this : opponent,
                                                                        type == DuelCompleteType.Won ? opponent : this,
                                                                        type));

            switch (type)
            {
                case DuelCompleteType.Fled:
                    // if initiator and opponent are on the same team
                    // or initiator and opponent are not PvP enabled, forcibly stop attacking
                    if (GetTeam() == opponent.GetTeam())
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
                    if (GetClass() == Class.Deathknight &&
                        opponent.GetQuestStatus(12733) == QuestStatus.Incomplete)
                        opponent.CastSpell(Duel.Opponent, 52994, true);

                    // Honor points after Duel (the winner) - ImpConfig
                    int amount = WorldConfig.GetIntValue(WorldCfg.HonorAfterDuel);

                    if (amount != 0)
                        opponent.RewardHonor(null, 1, amount);

                    break;
                default:
                    break;
            }

            // Victory Emote spell
            if (type != DuelCompleteType.Interrupted)
                opponent.CastSpell(Duel.Opponent, 52852, true);

            //Remove Duel Flag object
            GameObject obj = GetMap().GetGameObject(PlayerData.DuelArbiter);

            if (obj)
                Duel.Initiator.RemoveGameObject(obj, true);

            //remove Auras
            var itsAuras = opponent.GetAppliedAuras();

            foreach (var pair in itsAuras)
            {
                Aura aura = pair.Value.GetBase();

                if (!pair.Value.IsPositive() &&
                    aura.GetCasterGUID() == GetGUID() &&
                    aura.GetApplyTime() >= Duel.StartTime)
                    opponent.RemoveAura(pair);
            }

            var myAuras = GetAppliedAuras();

            foreach (var pair in myAuras)
            {
                Aura aura = pair.Value.GetBase();

                if (!pair.Value.IsPositive() &&
                    aura.GetCasterGUID() == opponent.GetGUID() &&
                    aura.GetApplyTime() >= Duel.StartTime)
                    RemoveAura(pair);
            }

            // cleanup combo points
            ClearComboPoints();
            opponent.ClearComboPoints();

            //cleanups
            SetDuelArbiter(ObjectGuid.Empty);
            SetDuelTeam(0);
            opponent.SetDuelArbiter(ObjectGuid.Empty);
            opponent.SetDuelTeam(0);

            opponent.Duel = null;
            Duel = null;
        }

        public void SetDuelArbiter(ObjectGuid guid)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.DuelArbiter), guid);
        }

        //PVP
        public void SetPvPDeath(bool on)
        {
            if (on)
                _extraFlags |= PlayerExtraFlags.PVPDeath;
            else
                _extraFlags &= ~PlayerExtraFlags.PVPDeath;
        }

        public void SetContestedPvPTimer(uint newTime)
        {
            _contestedPvPTimer = newTime;
        }

        public void ResetContestedPvP()
        {
            ClearUnitState(UnitState.AttackPlayer);
            RemovePlayerFlag(PlayerFlags.ContestedPVP);
            _contestedPvPTimer = 0;
        }

        public void SetContestedPvP(Player attackedPlayer = null)
        {
            if (attackedPlayer != null &&
                (attackedPlayer == this || (Duel != null && Duel.Opponent == attackedPlayer)))
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

            foreach (Unit unit in Controlled)
                if (!unit.HasUnitState(UnitState.AttackPlayer))
                {
                    unit.AddUnitState(UnitState.AttackPlayer);
                    AIRelocationNotifier notifier = new(unit);
                    Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
                }
        }

        public void UpdateContestedPvP(uint diff)
        {
            if (_contestedPvPTimer == 0 ||
                IsInCombat())
                return;

            if (_contestedPvPTimer <= diff)
                ResetContestedPvP();
            else
                _contestedPvPTimer -= diff;
        }

        public void UpdatePvPFlag(long currTime)
        {
            if (!IsPvP())
                return;

            if (PvpInfo.EndTimer == 0 ||
                (currTime < PvpInfo.EndTimer + 300) ||
                PvpInfo.IsHostile)
                return;

            if (PvpInfo.EndTimer <= currTime)
            {
                PvpInfo.EndTimer = 0;
                RemovePlayerFlag(PlayerFlags.PVPTimer);
            }

            UpdatePvP(false);
        }

        public void UpdatePvP(bool state, bool Override = false)
        {
            if (!state || Override)
            {
                SetPvP(state);
                PvpInfo.EndTimer = 0;
            }
            else
            {
                PvpInfo.EndTimer = GameTime.GetGameTime();
                SetPvP(state);
            }
        }

        public void UpdatePvPState(bool onlyFFA = false)
        {
            // @todo should we always synchronize UNIT_FIELD_BYTES_2, 1 of controller and controlled?
            // no, we shouldn't, those are checked for affecting player by client
            if (!PvpInfo.IsInNoPvPArea &&
                !IsGameMaster() &&
                (PvpInfo.IsInFFAPvPArea || Global.WorldMgr.IsFFAPvPRealm() || HasAuraType(AuraType.SetFFAPvp)))
            {
                if (!IsFFAPvP())
                {
                    SetPvpFlag(UnitPVPStateFlags.FFAPvp);

                    foreach (var unit in Controlled)
                        unit.SetPvpFlag(UnitPVPStateFlags.FFAPvp);
                }
            }
            else if (IsFFAPvP())
            {
                RemovePvpFlag(UnitPVPStateFlags.FFAPvp);

                foreach (var unit in Controlled)
                    unit.RemovePvpFlag(UnitPVPStateFlags.FFAPvp);
            }

            if (onlyFFA)
                return;

            if (PvpInfo.IsHostile) // in hostile area
            {
                if (!IsPvP() ||
                    PvpInfo.EndTimer != 0)
                    UpdatePvP(true, true);
            }
            else // in friendly area
            {
                if (IsPvP() &&
                    !HasPlayerFlag(PlayerFlags.InPVP) &&
                    PvpInfo.EndTimer == 0)
                    PvpInfo.EndTimer = GameTime.GetGameTime(); // start toggle-off
            }
        }

        public override void SetPvP(bool state)
        {
            base.SetPvP(state);

            foreach (var unit in Controlled)
                unit.SetPvP(state);
        }

        private void SetRegularAttackTime()
        {
            for (WeaponAttackType weaponAttackType = 0; weaponAttackType < WeaponAttackType.Max; ++weaponAttackType)
            {
                Item tmpitem = GetWeaponForAttack(weaponAttackType, true);

                if (tmpitem != null &&
                    !tmpitem.IsBroken())
                {
                    ItemTemplate proto = tmpitem.GetTemplate();

                    if (proto.GetDelay() != 0)
                        SetBaseAttackTime(weaponAttackType, proto.GetDelay());
                }
                else
                {
                    SetBaseAttackTime(weaponAttackType, SharedConst.BaseAttackTime); // If there is no weapon reset attack Time to base (might have been changed from forms)
                }
            }
        }

        private bool CanTitanGrip()
        {
            return _canTitanGrip;
        }

        private float GetRatingMultiplier(CombatRating cr)
        {
            GtCombatRatingsRecord Rating = CliDB.CombatRatingsGameTable.GetRow(GetLevel());

            if (Rating == null)
                return 1.0f;

            float value = GetGameTableColumnForCombatRating(Rating, cr);

            if (value == 0)
                return 1.0f; // By default use minimum coefficient (not must be called)

            return 1.0f / value;
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

        private void CheckTitanGripPenalty()
        {
            if (!CanTitanGrip())
                return;

            bool apply = IsUsingTwoHandedWeaponInOneHand();

            if (apply)
            {
                if (!HasAura(_titanGripPenaltySpellId))
                    CastSpell((Unit)null, _titanGripPenaltySpellId, true);
            }
            else
            {
                RemoveAurasDueToSpell(_titanGripPenaltySpellId);
            }
        }

        private bool IsTwoHandUsed()
        {
            Item mainItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

            if (!mainItem)
                return false;

            ItemTemplate itemTemplate = mainItem.GetTemplate();

            return (itemTemplate.GetInventoryType() == InventoryType.Weapon2Hand && !CanTitanGrip()) ||
                   itemTemplate.GetInventoryType() == InventoryType.Ranged ||
                   (itemTemplate.GetInventoryType() == InventoryType.RangedRight && itemTemplate.GetClass() == ItemClass.Weapon && (ItemSubClassWeapon)itemTemplate.GetSubClass() != ItemSubClassWeapon.Wand);
        }

        private bool IsUsingTwoHandedWeaponInOneHand()
        {
            Item offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

            if (offItem && offItem.GetTemplate().GetInventoryType() == InventoryType.Weapon2Hand)
                return true;

            Item mainItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

            if (!mainItem ||
                mainItem.GetTemplate().GetInventoryType() == InventoryType.Weapon2Hand)
                return false;

            if (!offItem)
                return false;

            return true;
        }

        private void UpdateDuelFlag(long currTime)
        {
            if (Duel != null &&
                Duel.State == DuelState.Countdown &&
                Duel.StartTime <= currTime)
            {
                Global.ScriptMgr.ForEach<IPlayerOnDuelStart>(p => p.OnDuelStart(this, Duel.Opponent));

                SetDuelTeam(1);
                Duel.Opponent.SetDuelTeam(2);

                Duel.State = DuelState.InProgress;
                Duel.Opponent.Duel.State = DuelState.InProgress;
            }
        }

        private void CheckDuelDistance(long currTime)
        {
            if (Duel == null)
                return;

            ObjectGuid duelFlagGUID = PlayerData.DuelArbiter;
            GameObject obj = GetMap().GetGameObject(duelFlagGUID);

            if (!obj)
                return;

            if (Duel.OutOfBoundsTime == 0)
            {
                if (!IsWithinDistInMap(obj, 50))
                {
                    Duel.OutOfBoundsTime = currTime + 10;
                    SendPacket(new DuelOutOfBounds());
                }
            }
            else
            {
                if (IsWithinDistInMap(obj, 40))
                {
                    Duel.OutOfBoundsTime = 0;
                    SendPacket(new DuelInBounds());
                }
                else if (currTime >= Duel.OutOfBoundsTime)
                {
                    DuelComplete(DuelCompleteType.Fled);
                }
            }
        }

        private void SetDuelTeam(uint duelTeam)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.DuelTeam), duelTeam);
        }

        private void UpdateAfkReport(long currTime)
        {
            if (_bgData.AfkReportedTimer <= currTime)
            {
                _bgData.AfkReportedCount = 0;
                _bgData.AfkReportedTimer = currTime + 5 * Time.Minute;
            }
        }

        private void InitPvP()
        {
            // pvp flag should stay after relog
            if (HasPlayerFlag(PlayerFlags.InPVP))
                UpdatePvP(true, true);
        }
    }
}