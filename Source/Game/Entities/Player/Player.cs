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
using Framework.Database;
using Game.Achievements;
using Game.AI;
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Chat;
using Game.DataStorage;
using Game.Garrisons;
using Game.Groups;
using Game.Guilds;
using Game.Loots;
using Game.Mails;
using Game.Maps;
using Game.Misc;
using Game.Network;
using Game.Network.Packets;
using Game.PvP;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Player : Unit
    {
        public Player(WorldSession session) : base(true)
        {
            objectTypeMask |= TypeMask.Player;
            objectTypeId = TypeId.Player;

            valuesCount = (int)ActivePlayerFields.End;
            _dynamicValuesCount = (int)ActivePlayerDynamicFields.End;
            Session = session;

            // players always accept
            if (!GetSession().HasPermission(RBACPermissions.CanFilterWhispers))
                SetAcceptWhispers(true);

            m_zoneUpdateId = 0xffffffff;
            m_nextSave = WorldConfig.GetUIntValue(WorldCfg.IntervalSave);

            SetGroupInvite(null);

            atLoginFlags = AtLoginFlags.None;
            PlayerTalkClass = new PlayerMenu(session);
            m_currentBuybackSlot = InventorySlots.BuyBackStart;

            for (byte i = 0; i < (int)MirrorTimerType.Max; i++)
                m_MirrorTimer[i] = -1;

            m_logintime = Time.UnixTime;
            m_Last_tick = m_logintime;

            m_dungeonDifficulty = Difficulty.Normal;
            m_raidDifficulty = Difficulty.NormalRaid;
            m_legacyRaidDifficulty = Difficulty.Raid10N;
            m_prevMapDifficulty = Difficulty.NormalRaid;
            m_InstanceValid = true;

            _specializationInfo = new SpecializationInfo();

            for (byte i = 0; i < (byte)BaseModGroup.End; ++i)
                m_auraBaseMod[i] = new float[] { 0.0f, 1.0f };

            for (var i = 0; i < (int)SpellModOp.Max; ++i)
            {
                m_spellMods[i] = new List<SpellModifier>[(int)SpellModType.End];

                for (var c = 0; c < (int)SpellModType.End; ++c)
                    m_spellMods[i][c] = new List<SpellModifier>();
            }

            // Honor System
            m_lastHonorUpdateTime = Time.UnixTime;

            m_unitMovedByMe = this;
            m_playerMovingMe = this;
            seerView = this;

            m_isActive = true;
            m_ControlledByPlayer = true;

            Global.WorldMgr.IncreasePlayerCount();

            _cinematicMgr = new CinematicManager(this);

            m_achievementSys = new PlayerAchievementMgr(this);
            reputationMgr = new ReputationMgr(this);
            m_questObjectiveCriteriaMgr = new QuestObjectiveCriteriaManager(this);
            m_sceneMgr = new SceneMgr(this);

            m_bgBattlegroundQueueID[0] = new BgBattlegroundQueueID_Rec();
            m_bgBattlegroundQueueID[1] = new BgBattlegroundQueueID_Rec();

            m_bgData = new BGData();

            _restMgr = new RestMgr(this);
        }

        public override void Dispose()
        {
            // Note: buy back item already deleted from DB when player was saved
            for (byte i = 0; i < (int)PlayerSlots.Count; ++i)
            {
                if (m_items[i] != null)
                    m_items[i].Dispose();
            }

            m_spells.Clear();
            _specializationInfo = null;
            m_mail.Clear();

            foreach (var item in mMitems.Values)
                item.Dispose();

            PlayerTalkClass.ClearMenus();
            ItemSetEff.Clear();

            _declinedname = null;
            m_runes = null;
            m_achievementSys = null;
            reputationMgr = null;

            _cinematicMgr.Dispose();

            for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
                _voidStorageItems[i] = null;

            ClearResurrectRequestData();

            Global.WorldMgr.DecreasePlayerCount();

            base.Dispose();
        }

        //Core
        public bool Create(ulong guidlow, CharacterCreateInfo createInfo)
        {
            _Create(ObjectGuid.Create(HighGuid.Player, guidlow));

            SetName(createInfo.Name);

            PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(createInfo.RaceId, createInfo.ClassId);
            if (info == null)
            {
                Log.outError(LogFilter.Player, "PlayerCreate: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with an invalid race/class pair ({2}/{3}) - refusing to do so.",
                    GetSession().GetAccountId(), GetName(), createInfo.RaceId, createInfo.ClassId);
                return false;
            }

            Relocate(info.PositionX, info.PositionY, info.PositionZ, info.Orientation);

            var cEntry = CliDB.ChrClassesStorage.LookupByKey(createInfo.ClassId);
            if (cEntry == null)
            {
                Log.outError(LogFilter.Player, "PlayerCreate: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with an invalid character class ({2}) - refusing to do so (wrong DBC-files?)",
                    GetSession().GetAccountId(), GetName(), createInfo.ClassId);
                return false;
            }

            if (!ValidateAppearance(createInfo.RaceId, createInfo.ClassId, createInfo.Sex, createInfo.HairStyle, createInfo.HairColor, createInfo.Face, createInfo.FacialHairStyle, createInfo.Skin, createInfo.CustomDisplay, true))
            {
                Log.outError(LogFilter.Player, "Player.Create: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with invalid appearance attributes - refusing to do so",
                    GetSession().GetAccountId(), GetName());
                return false;
            }

            SetMap(Global.MapMgr.CreateMap(info.MapId, this));

            int powertype = (int)cEntry.DisplayPower;

            SetObjectScale(1.0f);

            SetFactionForRace(createInfo.RaceId);

            if (!IsValidGender(createInfo.Sex))
            {
                Log.outError(LogFilter.Player, "Player:Create: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with an invalid gender ({2}) - refusing to do so",
                GetSession().GetAccountId(), GetName(), createInfo.Sex);
                return false;
            }

            SetByteValue(UnitFields.Bytes0, 0, (byte)createInfo.RaceId);
            SetByteValue(UnitFields.Bytes0, 1, (byte)createInfo.ClassId);
            SetByteValue(UnitFields.Bytes0, 3, (byte)createInfo.Sex);
            SetUInt32Value(UnitFields.DisplayPower, (uint)powertype);
            InitDisplayIds();
            if ((RealmType)WorldConfig.GetIntValue(WorldCfg.GameType) == RealmType.PVP || (RealmType)WorldConfig.GetIntValue(WorldCfg.GameType) == RealmType.RPPVP)
            {
                SetByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.PvP);
                SetFlag(UnitFields.Flags, UnitFlags.PvpAttackable);
            }

            SetFlag(UnitFields.Flags2, UnitFlags2.RegeneratePower);
            SetFloatValue(UnitFields.HoverHeight, 1.0f);            // default for players in 3.0.3

            SetUInt32Value(ActivePlayerFields.WatchedFactionIndex, 0xFFFFFFFF);  // -1 is default value

            SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId, createInfo.Skin);
            SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId, createInfo.Face);
            SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairStyleId, createInfo.HairStyle);
            SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId, createInfo.HairColor);
            SetByteValue(PlayerFields.Bytes2, PlayerFieldOffsets.Bytes2OffsetFacialStyle, createInfo.FacialHairStyle);
            for (byte i = 0; i < PlayerConst.CustomDisplaySize; ++i)
                SetByteValue(PlayerFields.Bytes2, (byte)(PlayerFieldOffsets.Bytes2OffsetCustomDisplayOption + i), createInfo.CustomDisplay[i]);
            SetUInt32Value(ActivePlayerFields.RestInfo + PlayerFieldOffsets.RestStateXp, (uint)((GetSession().IsARecruiter() || GetSession().GetRecruiterId() != 0) ? PlayerRestState.RAFLinked : PlayerRestState.NotRAFLinked));
            SetUInt32Value(ActivePlayerFields.RestInfo + PlayerFieldOffsets.RestStateHonor, (uint)PlayerRestState.NotRAFLinked);
            SetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender, (byte)createInfo.Sex);
            SetByteValue(PlayerFields.Bytes4, PlayerFieldOffsets.Bytes4OffsetArenaFaction, 0);
            SetInventorySlotCount(InventorySlots.DefaultSize);

            SetGuidValue(UnitFields.GuildGuid, ObjectGuid.Empty);
            SetUInt32Value(PlayerFields.GuildRank, 0);
            SetGuildLevel(0);
            SetUInt32Value(PlayerFields.GuildTimestamp, 0);

            for (int i = 0; i < PlayerConst.KnowTitlesSize; ++i)
                SetUInt64Value(ActivePlayerFields.KnownTitles + i, 0);  // 0=disabled
            SetUInt32Value(PlayerFields.ChosenTitle, 0);

            SetUInt32Value(ActivePlayerFields.Kills, 0);
            SetUInt32Value(ActivePlayerFields.LifetimeHonorableKills, 0);

            // set starting level
            uint start_level = WorldConfig.GetUIntValue(WorldCfg.StartPlayerLevel);
            if (GetClass() == Class.Deathknight)
                start_level= WorldConfig.GetUIntValue(WorldCfg.StartDeathKnightPlayerLevel);
            else if (GetClass() == Class.DemonHunter)
                start_level = WorldConfig.GetUIntValue(WorldCfg.StartDemonHunterPlayerLevel);

            if (createInfo.TemplateSet.HasValue)
            {
                if (GetSession().HasPermission(RBACPermissions.UseCharacterTemplates))
                {
                    CharacterTemplate charTemplate = Global.CharacterTemplateDataStorage.GetCharacterTemplate(createInfo.TemplateSet.Value);
                    if (charTemplate != null)
            {
                        if (charTemplate.Level > start_level)
                            start_level = charTemplate.Level;
                    }
                }
                else
                    Log.outWarn(LogFilter.Cheat, "Account: {0} (IP: {1}) tried to use a character template without given permission. Possible cheating attempt.", GetSession().GetAccountId(), GetSession().GetRemoteAddress());
            }

            if (GetSession().HasPermission(RBACPermissions.UseStartGmLevel))
            {
                uint gm_level = WorldConfig.GetUIntValue(WorldCfg.StartGmLevel);
                if (gm_level > start_level)
                    start_level = gm_level;
            }

            SetUInt32Value(UnitFields.Level, start_level);

            InitRunes();

            SetUInt64Value(ActivePlayerFields.Coinage, WorldConfig.GetUIntValue(WorldCfg.StartPlayerMoney));
            SetCurrency(CurrencyTypes.ApexisCrystals, WorldConfig.GetUIntValue(WorldCfg.CurrencyStartApexisCrystals));
            SetCurrency(CurrencyTypes.JusticePoints, WorldConfig.GetUIntValue(WorldCfg.CurrencyStartJusticePoints));

            // start with every map explored
            if (WorldConfig.GetBoolValue(WorldCfg.StartAllExplored))
            {
                for (ushort i = 0; i < PlayerConst.ExploredZonesSize; i++)
                    SetFlag(ActivePlayerFields.ExploredZones + i, 0xFFFFFFFF);
            }

            //Reputations if "StartAllReputation" is enabled, -- TODO: Fix this in a better way
            if (WorldConfig.GetBoolValue(WorldCfg.StartAllRep))
            {
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(942), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(935), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(936), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(1011), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(970), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(967), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(989), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(932), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(934), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(1038), 42999);
                GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(1077), 42999);

                // Factions depending on team, like cities and some more stuff
                switch (GetTeam())
                {
                    case Team.Alliance:
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(72), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(47), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(69), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(930), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(730), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(978), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(54), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(946), 42999);
                        break;
                    case Team.Horde:
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(76), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(68), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(81), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(911), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(729), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(941), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(530), 42999);
                        GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(947), 42999);
                        break;
                    default:
                        break;
                }
            }

            // Played time
            m_Last_tick = Time.UnixTime;
            m_PlayedTimeTotal = 0;
            m_PlayedTimeLevel = 0;

            // base stats and related field values
            InitStatsForLevel();
            InitTaxiNodesForLevel();
            InitTalentForLevel();
            InitPrimaryProfessions();                               // to max set before any spell added

            // apply original stats mods before spell loading or item equipment that call before equip _RemoveStatsMods()
            UpdateMaxHealth();                                      // Update max Health (for add bonus from stamina)
            SetFullHealth();
            SetFullPower(PowerType.Mana);

            // original spells
            LearnDefaultSkills();
            LearnCustomSpells();

            // Original action bar. Do not use Player.AddActionButton because we do not have skill spells loaded at this time
            // but checks will still be performed later when loading character from db in Player._LoadActions
            foreach (var action in info.action)
            {
                // create new button
                ActionButton ab = new ActionButton();

                // set data
                ab.SetActionAndType(action.action, (ActionButtonType)action.type);

                m_actionButtons[action.button] = ab;
            }

            // original items
            CharStartOutfitRecord oEntry = Global.DB2Mgr.GetCharStartOutfitEntry((byte)createInfo.RaceId, (byte)createInfo.ClassId, (byte)createInfo.Sex);
            if (oEntry != null)
            {
                for (int j = 0; j < ItemConst.MaxOutfitItems - 1; ++j)
                {
                    if (oEntry.ItemID[j] <= 0)
                        continue;

                    uint itemId = (uint)oEntry.ItemID[j];

                    ItemTemplate iProto = Global.ObjectMgr.GetItemTemplate(itemId);
                    if (iProto == null)
                        continue;

                    // BuyCount by default
                    uint count = iProto.GetBuyCount();

                    // special amount for food/drink
                    if (iProto.GetClass() == ItemClass.Consumable && iProto.GetSubClass() == (uint)ItemSubClassConsumable.FoodDrink)
                    {
                        if (!iProto.Effects.Empty())
                        {
                            switch (iProto.Effects[0].SpellCategoryID)
                            {
                                case 11:                                // food
                                    count = (uint)(GetClass() == Class.Deathknight ? 10 : 4);
                                    break;
                                case 59:                                // drink
                                    count = 2;
                                    break;
                            }
                        }
                        if (iProto.GetMaxStackSize() < count)
                            count = iProto.GetMaxStackSize();
                    }
                    StoreNewItemInBestSlots(itemId, count);
                }
            }
            foreach (var item in info.item)
                StoreNewItemInBestSlots(item.item_id, item.item_amount);

            // bags and main-hand weapon must equipped at this moment
            // now second pass for not equipped (offhand weapon/shield if it attempt equipped before main-hand weapon)
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                {
                    ushort eDest;
                    // equip offhand weapon/shield if it attempt equipped before main-hand weapon
                    InventoryResult msg = CanEquipItem(ItemConst.NullSlot, out eDest, pItem, false);
                    if (msg == InventoryResult.Ok)
                    {
                        RemoveItem(InventorySlots.Bag0, i, true);
                        EquipItem(eDest, pItem, true);
                    }
                    // move other items to more appropriate slots
                    else
                    {
                        List<ItemPosCount> sDest = new List<ItemPosCount>();
                        msg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, sDest, pItem, false);
                        if (msg == InventoryResult.Ok)
                        {
                            RemoveItem(InventorySlots.Bag0, i, true);
                            pItem = StoreItem(sDest, pItem, true);
                        }
                    }
                }
            }
            // all item positions resolved

            ChrSpecializationRecord defaultSpec = Global.DB2Mgr.GetDefaultChrSpecializationForClass(GetClass());
            if (defaultSpec != null)
            {
                SetActiveTalentGroup(defaultSpec.OrderIndex);
                SetPrimarySpecialization(defaultSpec.Id);
            }

            return true;
        }
        public override void Update(uint diff)
        {
            if (!IsInWorld)
                return;

            // undelivered mail
            if (m_nextMailDelivereTime != 0 && m_nextMailDelivereTime <= Time.UnixTime)
            {
                SendNewMail();
                ++unReadMails;

                // It will be recalculate at mailbox open (for unReadMails important non-0 until mailbox open, it also will be recalculated)
                m_nextMailDelivereTime = 0;
            }
            // If this is set during update SetSpellModTakingSpell call is missing somewhere in the code
            // Having this would prevent more aura charges to be dropped, so let's crash
            if (m_spellModTakingSpell != null)
            {
                Log.outFatal(LogFilter.Spells, "Player has m_spellModTakingSpell {0} during update!", m_spellModTakingSpell.m_spellInfo.Id);
                m_spellModTakingSpell = null;
            }

            // Update cinematic location, if 500ms have passed and we're doing a cinematic now.
            _cinematicMgr.m_cinematicDiff += diff;
            if (_cinematicMgr.m_activeCinematicCameraId != 0 && Time.GetMSTimeDiffToNow(_cinematicMgr.m_lastCinematicCheck) > 500)
            {
                _cinematicMgr.m_lastCinematicCheck = Time.GetMSTime();
                _cinematicMgr.UpdateCinematicLocation(diff);
            }

            //used to implement delayed far teleports
            SetCanDelayTeleport(true);
            base.Update(diff);
            SetCanDelayTeleport(false);

            long now = Time.UnixTime;

            UpdatePvPFlag(now);

            UpdateContestedPvP(diff);

            UpdateDuelFlag(now);

            CheckDuelDistance(now);

            UpdateAfkReport(now);

            if (GetCombatTimer() != 0) // Only set when in pvp combat
            {
                Aura aura = GetAura(PlayerConst.SpellPvpRulesEnabled);
                if (aura != null)
                    if (!aura.IsPermanent())
                        aura.SetDuration(aura.GetSpellInfo().GetMaxDuration());
            }

            if (IsAIEnabled && GetAI() != null)
                GetAI().UpdateAI(diff);
            else if (NeedChangeAI)
            {
                UpdateCharmAI();
                NeedChangeAI = false;
                IsAIEnabled = GetAI() != null;
            }

            // Update items that have just a limited lifetime
            if (now > m_Last_tick)
                UpdateItemDuration((uint)(now - m_Last_tick));

            // check every second
            if (now > m_Last_tick + 1)
                UpdateSoulboundTradeItems();

            // If mute expired, remove it from the DB
            if (GetSession().m_muteTime != 0 && GetSession().m_muteTime < now)
            {
                GetSession().m_muteTime = 0;
                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_MUTE_TIME);
                stmt.AddValue(0, 0); // Set the mute time to 0
                stmt.AddValue(1, "");
                stmt.AddValue(2, "");
                stmt.AddValue(3, GetSession().GetAccountId());
                DB.Login.Execute(stmt);
            }

            if (!m_timedquests.Empty())
            {
                foreach (var id in m_timedquests)
                {
                    QuestStatusData q_status = m_QuestStatus[id];
                    if (q_status.Timer <= diff)
                        FailQuest(id);
                    else
                    {
                        q_status.Timer -= diff;
                        m_QuestStatusSave[id] = QuestSaveType.Default;
                    }
                }
            }

            m_achievementSys.UpdateTimedCriteria(diff);

            if (HasUnitState(UnitState.MeleeAttacking) && !HasUnitState(UnitState.Casting))
            {
                Unit victim = GetVictim();
                if (victim != null)
                {
                    // default combat reach 10
                    // TODO add weapon, skill check

                    if (isAttackReady(WeaponAttackType.BaseAttack))
                    {
                        if (!IsWithinMeleeRange(victim))
                        {
                            setAttackTimer(WeaponAttackType.BaseAttack, 100);
                            if (m_swingErrorMsg != 1)               // send single time (client auto repeat)
                            {
                                SendAttackSwingNotInRange();
                                m_swingErrorMsg = 1;
                            }
                        }
                        //120 degrees of radiant range, if player is not in boundary radius
                        else if (!IsWithinBoundaryRadius(victim) && !HasInArc(2 * MathFunctions.PI / 3, victim))
                        {
                            setAttackTimer(WeaponAttackType.BaseAttack, 100);
                            if (m_swingErrorMsg != 2)               // send single time (client auto repeat)
                            {
                                SendAttackSwingBadFacingAttack();
                                m_swingErrorMsg = 2;
                            }
                        }
                        else
                        {
                            m_swingErrorMsg = 0;                    // reset swing error state

                            // prevent base and off attack in same time, delay attack at 0.2 sec
                            if (haveOffhandWeapon())
                                if (getAttackTimer(WeaponAttackType.OffAttack) < SharedConst.AttackDisplayDelay)
                                    setAttackTimer(WeaponAttackType.OffAttack, SharedConst.AttackDisplayDelay);

                            // do attack
                            AttackerStateUpdate(victim, WeaponAttackType.BaseAttack);
                            resetAttackTimer(WeaponAttackType.BaseAttack);
                        }
                    }

                    if (!IsInFeralForm() && haveOffhandWeapon() && isAttackReady(WeaponAttackType.OffAttack))
                    {
                        if (!IsWithinMeleeRange(victim))
                            setAttackTimer(WeaponAttackType.OffAttack, 100);
                        else if (!IsWithinBoundaryRadius(victim) && !HasInArc(2 * MathFunctions.PI / 3, victim))
                            setAttackTimer(WeaponAttackType.BaseAttack, 100);
                        else
                        {
                            // prevent base and off attack in same time, delay attack at 0.2 sec
                            if (getAttackTimer(WeaponAttackType.BaseAttack) < SharedConst.AttackDisplayDelay)
                                setAttackTimer(WeaponAttackType.BaseAttack, SharedConst.AttackDisplayDelay);

                            // do attack
                            AttackerStateUpdate(victim, WeaponAttackType.OffAttack);
                            resetAttackTimer(WeaponAttackType.OffAttack);
                        }
                    }
                }
            }

            if (HasFlag(PlayerFields.Flags, PlayerFlags.Resting))
                _restMgr.Update(diff);

            if (m_weaponChangeTimer > 0)
            {
                if (diff >= m_weaponChangeTimer)
                    m_weaponChangeTimer = 0;
                else
                    m_weaponChangeTimer -= diff;
            }

            if (m_zoneUpdateTimer > 0)
            {
                if (diff >= m_zoneUpdateTimer)
                {
                    // On zone update tick check if we are still in an inn if we are supposed to be in one
                    if (_restMgr.HasRestFlag(RestFlag.Tavern))
                    {
                        AreaTriggerRecord atEntry = CliDB.AreaTriggerStorage.LookupByKey(_restMgr.GetInnTriggerId());
                        if (atEntry == null || !IsInAreaTriggerRadius(atEntry))
                            _restMgr.RemoveRestFlag(RestFlag.Tavern);
                    }

                    uint newzone, newarea;
                    GetZoneAndAreaId(out newzone, out newarea);

                    if (m_zoneUpdateId != newzone)
                        UpdateZone(newzone, newarea);                // also update area
                    else
                    {
                        // use area updates as well
                        // needed for free far all arenas for example
                        if (m_areaUpdateId != newarea)
                            UpdateArea(newarea);

                        m_zoneUpdateTimer = 1 * Time.InMilliseconds;
                    }
                }
                else
                    m_zoneUpdateTimer -= diff;
            }
            if (m_timeSyncTimer > 0 && !IsBeingTeleportedFar())
            {
                if (diff >= m_timeSyncTimer)
                    SendTimeSync();
                else
                    m_timeSyncTimer -= diff;
            }

            if (IsAlive())
            {
                m_regenTimer += diff;
                RegenerateAll();
            }

            if (m_deathState == DeathState.JustDied)
                KillPlayer();

            if (m_nextSave > 0)
            {
                if (diff >= m_nextSave)
                {
                    // m_nextSave reset in SaveToDB call
                    Global.ScriptMgr.OnPlayerSave(this);
                    SaveToDB();
                    Log.outDebug(LogFilter.Player, "Player '{0}' (GUID: {1}) saved", GetName(), GetGUID().ToString());
                }
                else
                    m_nextSave -= diff;
            }

            //Handle Water/drowning
            HandleDrowning(diff);

            // Played time
            if (now > m_Last_tick)
            {
                uint elapsed = (uint)(now - m_Last_tick);
                m_PlayedTimeTotal += elapsed;
                m_PlayedTimeLevel += elapsed;
                m_Last_tick = now;
            }

            if (GetDrunkValue() != 0)
            {
                m_drunkTimer += diff;
                if (m_drunkTimer > 9 * Time.InMilliseconds)
                    HandleSobering();
            }
            if (HasPendingBind())
            {
                if (_pendingBindTimer <= diff)
                {
                    // Player left the instance
                    if (_pendingBindId == GetInstanceId())
                        BindToInstance();
                    SetPendingBind(0, 0);
                }
                else
                    _pendingBindTimer -= diff;
            }
            // not auto-free ghost from body in instances
            if (m_deathTimer > 0 && !GetMap().Instanceable() && !HasAuraType(AuraType.PreventResurrection))
            {
                if (diff >= m_deathTimer)
                {
                    m_deathTimer = 0;
                    BuildPlayerRepop();
                    RepopAtGraveyard();
                }
                else
                    m_deathTimer -= diff;
            }

            UpdateEnchantTime(diff);
            UpdateHomebindTime(diff);

            if (!_instanceResetTimes.Empty())
            {
                foreach (var instance in _instanceResetTimes.ToList())
                {
                    if (instance.Value < now)
                        _instanceResetTimes.Remove(instance.Key);
                }
            }

            // group update
            SendUpdateToOutOfRangeGroupMembers();

            Pet pet = GetPet();
            if (pet != null && !pet.IsWithinDistInMap(this, GetMap().GetVisibilityRange()) && !pet.isPossessed())
                RemovePet(pet, PetSaveMode.NotInSlot, true);

            if (IsAlive())
            {
                if (m_hostileReferenceCheckTimer <= diff)
                {
                    m_hostileReferenceCheckTimer = 15 * Time.InMilliseconds;
                    if (!GetMap().IsDungeon())
                        getHostileRefManager().deleteReferencesOutOfRange(GetVisibilityRange());
                }
                else
                    m_hostileReferenceCheckTimer -= diff;
            }

            //we should execute delayed teleports only for alive(!) players
            //because we don't want player's ghost teleported from graveyard
            if (IsHasDelayedTeleport() && IsAlive())
                TeleportTo(teleportDest, m_teleport_options);
        }

        public override void setDeathState(DeathState s)
        {
            bool oldIsAlive = IsAlive();

            if (s == DeathState.JustDied)
            {
                if (!oldIsAlive)
                {
                    Log.outError(LogFilter.Player, "Player.setDeathState: Attempted to kill a dead player '{0}' ({1})", GetName(), GetGUID().ToString());
                    return;
                }

                // drunken state is cleared on death
                SetDrunkValue(0);
                // lost combo points at any target (targeted combo points clear in Unit::setDeathState)
                ClearComboPoints();

                ClearResurrectRequestData();

                //FIXME: is pet dismissed at dying or releasing spirit? if second, add setDeathState(DEAD) to HandleRepopRequestOpcode and define pet unsummon here with (s == DEAD)
                RemovePet(null, PetSaveMode.NotInSlot, true);

                InitializeSelfResurrectionSpells();

                UpdateCriteria(CriteriaTypes.DeathAtMap, 1);
                UpdateCriteria(CriteriaTypes.Death, 1);
                UpdateCriteria(CriteriaTypes.DeathInDungeon, 1);
                ResetCriteria(CriteriaTypes.BgObjectiveCapture, (uint)CriteriaCondition.NoDeath);
                ResetCriteria(CriteriaTypes.HonorableKill, (uint)CriteriaCondition.NoDeath);
                ResetCriteria(CriteriaTypes.GetKillingBlows, (uint)CriteriaCondition.NoDeath);
            }

            base.setDeathState(s);

            if (IsAlive() && !oldIsAlive)
                //clear aura case after resurrection by another way (spells will be applied before next death)
                ClearDynamicValue(ActivePlayerDynamicFields.SelfResSpells);
        }

        public override void DestroyForPlayer(Player target)
        {
            base.DestroyForPlayer(target);

            for (byte i = 0; i < InventorySlots.BagEnd; ++i)
            {
                if (m_items[i] == null)
                    continue;

                m_items[i].DestroyForPlayer(target);
            }

            if (target == this)
            {
                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
                {
                    if (m_items[i] == null)
                        continue;

                    m_items[i].DestroyForPlayer(target);
                }

                for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
                {
                    if (m_items[i] == null)
                        continue;

                    m_items[i].DestroyForPlayer(target);
                }

                for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
                {
                    if (m_items[i] == null)
                        continue;

                    m_items[i].DestroyForPlayer(target);
                }
            }
        }
        public override void CleanupsBeforeDelete(bool finalCleanup = true)
        {
            TradeCancel(false);
            DuelComplete(DuelCompleteType.Interrupted);

            base.CleanupsBeforeDelete(finalCleanup);

            if (GetTransport() != null)
                GetTransport().RemovePassenger(this);

            // clean up player-instance binds, may unload some instance saves
            foreach (var difficultyDic in m_boundInstances.Values)
                foreach (var instanceBind in difficultyDic.Values)
                    instanceBind.save.RemovePlayer(this);
        }

        public override void AddToWorld()
        {
            // Do not add/remove the player from the object storage
            // It will crash when updating the ObjectAccessor
            // The player should only be added when logging in
            base.AddToWorld();

            for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
                if (m_items[i] != null)
                    m_items[i].AddToWorld();
        }
        public override void RemoveFromWorld()
        {
            // cleanup
            if (IsInWorld)
            {
                // Release charmed creatures, unsummon totems and remove pets/guardians
                StopCastingCharm();
                StopCastingBindSight();
                UnsummonPetTemporaryIfAny();
                Global.OutdoorPvPMgr.HandlePlayerLeaveZone(this, m_zoneUpdateId);
                Global.BattleFieldMgr.HandlePlayerLeaveZone(this, m_zoneUpdateId);
            }

            // Remove items from world before self - player must be found in Item.RemoveFromObjectUpdate
            for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
                if (m_items[i] != null)
                    m_items[i].RemoveFromWorld();

            // Do not add/remove the player from the object storage
            // It will crash when updating the ObjectAccessor
            // The player should only be removed when logging out
            base.RemoveFromWorld();

            WorldObject viewpoint = GetViewpoint();
            if (viewpoint != null)
            {
                Log.outError(LogFilter.Player, "Player {0} has viewpoint {1} {2} when removed from world",
                    GetName(), viewpoint.GetEntry(), viewpoint.GetTypeId());
                SetViewpoint(viewpoint, false);
            }
        }

        void ScheduleDelayedOperation(PlayerDelayedOperations operation)
        {
            if (operation < PlayerDelayedOperations.End)
                m_DelayedOperations |= operation;
        }
        public void ProcessDelayedOperations()
        {
            if (m_DelayedOperations == 0)
                return;

            if (m_DelayedOperations.HasAnyFlag(PlayerDelayedOperations.ResurrectPlayer))
                ResurrectUsingRequestDataImpl();

            if (m_DelayedOperations.HasAnyFlag(PlayerDelayedOperations.SavePlayer))
                SaveToDB();

            if (m_DelayedOperations.HasAnyFlag(PlayerDelayedOperations.SpellCastDeserter))
                CastSpell(this, 26013, true);               // Deserter

            if (m_DelayedOperations.HasAnyFlag(PlayerDelayedOperations.BGMountRestore))
            {
                if (m_bgData.mountSpell != 0)
                {
                    CastSpell(this, m_bgData.mountSpell, true);
                    m_bgData.mountSpell = 0;
                }
            }

            if (m_DelayedOperations.HasAnyFlag(PlayerDelayedOperations.BGTaxiRestore))
            {
                if (m_bgData.HasTaxiPath())
                {
                    m_taxi.AddTaxiDestination(m_bgData.taxiPath[0]);
                    m_taxi.AddTaxiDestination(m_bgData.taxiPath[1]);
                    m_bgData.ClearTaxiPath();

                    ContinueTaxiFlight();
                }
            }

            if (m_DelayedOperations.HasAnyFlag(PlayerDelayedOperations.BGGroupRestore))
            {
                Group g = GetGroup();
                if (g != null)
                    g.SendUpdateToPlayer(GetGUID());
            }

            //we have executed ALL delayed ops, so clear the flag
            m_DelayedOperations = 0;
        }

        public override bool IsLoading()
        {
            return GetSession().PlayerLoading();
        }

        new PlayerAI GetAI() { return (PlayerAI)i_AI; }

        //Network
        public void SendPacket(ServerPacket data)
        {
            Session.SendPacket(data);
        }

        //Time
        void ResetTimeSync()
        {
            m_timeSyncTimer = 0;
            m_timeSyncClient = 0;
            m_timeSyncServer = Time.GetMSTime();
        }
        void SendTimeSync()
        {
            m_timeSyncQueue.Push(m_movementCounter++);

            TimeSyncRequest packet = new TimeSyncRequest();
            packet.SequenceIndex = m_timeSyncQueue.Last();
            SendPacket(packet);

            // Schedule next sync in 10 sec
            m_timeSyncTimer = 10000;
            m_timeSyncServer = Time.GetMSTime();

            if (m_timeSyncQueue.Count > 3)
                Log.outError(LogFilter.Network, "Not received CMSG_TIME_SYNC_RESP for over 30 seconds from player {0} ({1}), possible cheater", GetGUID().ToString(), GetName());
        }

        public DeclinedName GetDeclinedNames() { return _declinedname; }

        public void CreateGarrison(uint garrSiteId)
        {
            _garrison = new Garrison(this);
            if (!_garrison.Create(garrSiteId))
                _garrison = null;
        }

        void DeleteGarrison()
        {
            if (_garrison != null)
            {
                _garrison.Delete();
                _garrison = null;
            }
        }

        public Garrison GetGarrison() { return _garrison; }

        public SceneMgr GetSceneMgr() { return m_sceneMgr; }

        public RestMgr GetRestMgr() { return _restMgr; }

        public bool IsAdvancedCombatLoggingEnabled()  { return _advancedCombatLoggingEnabled; }
        public void SetAdvancedCombatLogging(bool enabled) { _advancedCombatLoggingEnabled = enabled; }

        //Taxi
        public void InitTaxiNodesForLevel() { m_taxi.InitTaxiNodesForLevel(GetRace(), GetClass(), getLevel()); }

        //Cheat Commands
        public bool GetCommandStatus(PlayerCommandStates command) { return (_activeCheats & command) != 0; }
        public void SetCommandStatusOn(PlayerCommandStates command) { _activeCheats |= command; }
        public void SetCommandStatusOff(PlayerCommandStates command) { _activeCheats &= ~command; }

        //Pet - Summons - Vehicles

        // last used pet number (for BG's)
        public uint GetLastPetNumber() { return m_lastpetnumber; }
        public void SetLastPetNumber(uint petnumber) { m_lastpetnumber = petnumber; }
        public void LoadPet()
        {
            //fixme: the pet should still be loaded if the player is not in world
            // just not added to the map
            if (IsInWorld)
            {
                Pet pet = new Pet(this);
                pet.LoadPetFromDB(this, 0, 0, true);
            }
        }
        public uint GetTemporaryUnsummonedPetNumber() { return m_temporaryUnsummonedPetNumber; }
        public void SetTemporaryUnsummonedPetNumber(uint petnumber) { m_temporaryUnsummonedPetNumber = petnumber; }
        public void UnsummonPetTemporaryIfAny()
        {
            Pet pet = GetPet();
            if (!pet)
                return;

            if (m_temporaryUnsummonedPetNumber == 0 && pet.isControlled() && !pet.isTemporarySummoned())
            {
                m_temporaryUnsummonedPetNumber = pet.GetCharmInfo().GetPetNumber();
                m_oldpetspell = pet.GetUInt32Value(UnitFields.CreatedBySpell);
            }

            RemovePet(pet, PetSaveMode.AsCurrent);
        }
        public void ResummonPetTemporaryUnSummonedIfAny()
        {
            if (m_temporaryUnsummonedPetNumber == 0)
                return;

            // not resummon in not appropriate state
            if (IsPetNeedBeTemporaryUnsummoned())
                return;

            if (!GetPetGUID().IsEmpty())
                return;

            Pet NewPet = new Pet(this);
            NewPet.LoadPetFromDB(this, 0, m_temporaryUnsummonedPetNumber, true);

            m_temporaryUnsummonedPetNumber = 0;
        }

        public bool IsPetNeedBeTemporaryUnsummoned()
        {
            return !IsInWorld || !IsAlive() || IsMounted();
        }

        public void SendRemoveControlBar()
        {
            SendPacket(new PetSpells());
        }

        public void StopCastingCharm()
        {
            Unit charm = GetCharm();
            if (!charm)
                return;

            if (charm.IsTypeId(TypeId.Unit))
            {
                if (charm.ToCreature().HasUnitTypeMask(UnitTypeMask.Puppet))
                    ((Puppet)charm).UnSummon();
                else if (charm.IsVehicle())
                    ExitVehicle();
            }
            if (!GetCharmGUID().IsEmpty())
                charm.RemoveCharmAuras();

            if (!GetCharmGUID().IsEmpty())
            {
                Log.outFatal(LogFilter.Player, "Player {0} (GUID: {1} is not able to uncharm unit (GUID: {2} Entry: {3}, Type: {4})", GetName(), GetGUID(), GetCharmGUID(), charm.GetEntry(), charm.GetTypeId());
                if (!charm.GetCharmerGUID().IsEmpty())
                {
                    Log.outFatal(LogFilter.Player, "Charmed unit has charmer guid {0}", charm.GetCharmerGUID());
                    Cypher.Assert(false);
                }

                SetCharm(charm, false);
            }
        }
        public void CharmSpellInitialize()
        {
            Unit charm = GetFirstControlled();
            if (!charm)
                return;

            CharmInfo charmInfo = charm.GetCharmInfo();
            if (charmInfo == null)
            {
                Log.outError(LogFilter.Player, "Player:CharmSpellInitialize(): the player's charm ({0}) has no charminfo!", charm.GetGUID());
                return;
            }

            PetSpells petSpells = new PetSpells();
            petSpells.PetGUID = charm.GetGUID();

            if (charm.IsTypeId(TypeId.Unit))
            {
                petSpells.ReactState = charm.ToCreature().GetReactState();
                petSpells.CommandState = charmInfo.GetCommandState();
            }

            for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
                petSpells.ActionButtons[i] = charmInfo.GetActionBarEntry(i).packedData;

            for (byte i = 0; i < SharedConst.MaxSpellCharm; ++i)
            {
                var cspell = charmInfo.GetCharmSpell(i);
                if (cspell.GetAction() != 0)
                    petSpells.Actions.Add(cspell.packedData);
            }

            // Cooldowns
            if (!charm.IsTypeId(TypeId.Player))
                charm.GetSpellHistory().WritePacket(petSpells);

            SendPacket(petSpells);
        }
        public void PossessSpellInitialize()
        {
            Unit charm = GetCharm();
            if (!charm)
                return;

            CharmInfo charmInfo = charm.GetCharmInfo();
            if (charmInfo == null)
            {
                Log.outError(LogFilter.Player, "Player:PossessSpellInitialize(): charm ({0}) has no charminfo!", charm.GetGUID());
                return;
            }

            PetSpells petSpellsPacket = new PetSpells();
            petSpellsPacket.PetGUID = charm.GetGUID();

            for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
                petSpellsPacket.ActionButtons[i] = charmInfo.GetActionBarEntry(i).packedData;

            // Cooldowns
            charm.GetSpellHistory().WritePacket(petSpellsPacket);

            SendPacket(petSpellsPacket);
        }
        public void VehicleSpellInitialize()
        {
            Creature vehicle = GetVehicleCreatureBase();
            if (!vehicle)
                return;

            PetSpells petSpells = new PetSpells();
            petSpells.PetGUID = vehicle.GetGUID();
            petSpells.CreatureFamily = 0;                          // Pet Family (0 for all vehicles)
            petSpells.Specialization = 0;
            petSpells.TimeLimit = vehicle.IsSummon() ? vehicle.ToTempSummon().GetTimer() : 0;
            petSpells.ReactState = vehicle.GetReactState();
            petSpells.CommandState = CommandStates.Follow;
            petSpells.Flag = 0x8;

            for (uint i = 0; i < SharedConst.MaxSpellControlBar; ++i)
                petSpells.ActionButtons[i] = UnitActionBarEntry.MAKE_UNIT_ACTION_BUTTON(0, i + 8);

            for (uint i = 0; i < SharedConst.MaxCreatureSpells; ++i)
            {
                uint spellId = vehicle.m_spells[i];
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
                if (spellInfo == null)
                    continue;

                if (!Global.ConditionMgr.IsObjectMeetingVehicleSpellConditions(vehicle.GetEntry(), spellId, this, vehicle))
                {
                    Log.outDebug(LogFilter.Condition, "VehicleSpellInitialize: conditions not met for Vehicle entry {0} spell {1}", vehicle.ToCreature().GetEntry(), spellId);
                    continue;
                }

                if (spellInfo.IsPassive())
                    vehicle.CastSpell(vehicle, spellId, true);

                petSpells.ActionButtons[i] = UnitActionBarEntry.MAKE_UNIT_ACTION_BUTTON(spellId, i + 8);
            }

            // Cooldowns
            vehicle.GetSpellHistory().WritePacket(petSpells);

            SendPacket(petSpells);
        }
        
        //Currency - Money
        void SetCurrency(CurrencyTypes id, uint count, bool printLog = true)
        {
            var playerCurrency = _currencyStorage.LookupByKey(id);
            if (playerCurrency == null)
            {
                PlayerCurrency cur = new PlayerCurrency();
                cur.state = PlayerCurrencyState.New;
                cur.Quantity = count;
                cur.WeeklyQuantity = 0;
                cur.TrackedQuantity = 0;
                cur.Flags = 0;
                _currencyStorage[(uint)id] = cur;
            }
        }
        public uint GetCurrency(uint id)
        {
            var playerCurrency = _currencyStorage.LookupByKey(id);
            if (playerCurrency == null)
                return 0;

            return playerCurrency.Quantity;
        }
        public void ModifyCurrency(CurrencyTypes id, int count, bool printLog = true, bool ignoreMultipliers = false)
        {
            if (count == 0)
                return;

            CurrencyTypesRecord currency = CliDB.CurrencyTypesStorage.LookupByKey(id);
            Cypher.Assert(currency != null);

            if (!ignoreMultipliers)
                count *= (int)GetTotalAuraMultiplierByMiscValue(AuraType.ModCurrencyGain, (int)id);

            uint oldTotalCount = 0;
            uint oldWeekCount = 0;
            uint oldTrackedCount = 0;

            var playerCurrency = _currencyStorage.LookupByKey(id);
            if (playerCurrency == null)
            {
                PlayerCurrency cur = new PlayerCurrency();
                cur.state = PlayerCurrencyState.New;
                cur.Quantity = 0;
                cur.WeeklyQuantity = 0;
                cur.TrackedQuantity = 0;
                cur.Flags = 0;
                _currencyStorage[(uint)id] = cur;
                playerCurrency = _currencyStorage.LookupByKey(id);
            }
            else
            {
                oldTotalCount = playerCurrency.Quantity;
                oldWeekCount = playerCurrency.WeeklyQuantity;
                oldTrackedCount = playerCurrency.TrackedQuantity;
            }

            // count can't be more then weekCap if used (weekCap > 0)
            uint weekCap = GetCurrencyWeekCap(currency);
            if (weekCap != 0 && count > weekCap)
                count = (int)weekCap;

            // count can't be more then totalCap if used (totalCap > 0)
            uint totalCap = GetCurrencyTotalCap(currency);
            if (totalCap != 0 && count > totalCap)
                count = (int)totalCap;

            int newTrackedCount = (int)(oldTrackedCount) + (count > 0 ? count : 0);
            if (newTrackedCount < 0)
                newTrackedCount = 0;

            int newTotalCount = (int)oldTotalCount + count;
            if (newTotalCount < 0)
                newTotalCount = 0;

            int newWeekCount = (int)oldWeekCount + (count > 0 ? count : 0);
            if (newWeekCount < 0)
                newWeekCount = 0;

            // if we get more then weekCap just set to limit
            if (weekCap != 0 && weekCap < newWeekCount)
            {
                newWeekCount = (int)weekCap;
                // weekCap - oldWeekCount always >= 0 as we set limit before!
                newTotalCount = (int)(oldTotalCount + (weekCap - oldWeekCount));
            }

            // if we get more then totalCap set to maximum;
            if (totalCap != 0 && totalCap < newTotalCount)
            {
                newTotalCount = (int)totalCap;
                newWeekCount = (int)weekCap;
            }

            if (newTotalCount != oldTotalCount)
            {
                if (playerCurrency.state != PlayerCurrencyState.New)
                    playerCurrency.state = PlayerCurrencyState.Changed;

                playerCurrency.Quantity = (uint)newTotalCount;
                playerCurrency.WeeklyQuantity = (uint)newWeekCount;
                playerCurrency.TrackedQuantity = (uint)newTrackedCount;

                if (count > 0)
                    UpdateCriteria(CriteriaTypes.Currency, (uint)id, (uint)count);

                CurrencyChanged((uint)id, count);

                _currencyStorage[(uint)id] = playerCurrency;

                SetCurrency packet = new SetCurrency();
                packet.Type = (uint)id;
                packet.Quantity = newTotalCount;
                packet.SuppressChatLog = !printLog;
                packet.WeeklyQuantity.Set(newWeekCount);
                packet.TrackedQuantity.Set(newTrackedCount);
                packet.Flags = playerCurrency.Flags;

                SendPacket(packet);
            }
        }
        public bool HasCurrency(uint id, uint count)
        {
            var playerCurrency = _currencyStorage.LookupByKey(id);
            return playerCurrency != null && playerCurrency.Quantity >= count;
        }
        public uint GetCurrencyWeekCap(CurrencyTypes id)
        {
            CurrencyTypesRecord entry = CliDB.CurrencyTypesStorage.LookupByKey((uint)id);
            if (entry == null)
                return 0;

            return GetCurrencyWeekCap(entry);
        }
        public uint GetCurrencyWeekCap(CurrencyTypesRecord currency)
        {
            return currency.MaxEarnablePerWeek;
        }
        uint GetCurrencyTotalCap(CurrencyTypesRecord currency)
        {
            uint cap = currency.MaxQty;

            switch ((CurrencyTypes)currency.Id)
            {
                case CurrencyTypes.ApexisCrystals:
                    {
                        uint apexiscap = WorldConfig.GetUIntValue(WorldCfg.CurrencyMaxApexisCrystals);
                        if (apexiscap > 0)
                            cap = apexiscap;
                        break;
                    }
                case CurrencyTypes.JusticePoints:
                    {
                        uint justicecap = WorldConfig.GetUIntValue(WorldCfg.CurrencyMaxJusticePoints);
                        if (justicecap > 0)
                            cap = justicecap;
                        break;
                    }
            }

            return cap;
        }
        uint GetCurrencyOnWeek(CurrencyTypes id)
        {
            var playerCurrency = _currencyStorage.LookupByKey(id);
            if (playerCurrency == null)
                return 0;

            return playerCurrency.WeeklyQuantity;
        }

        //Action Buttons - CUF Profile
        public void SaveCUFProfile(byte id, CUFProfile profile) { _CUFProfiles[id] = profile; }
        public CUFProfile GetCUFProfile(byte id) { return _CUFProfiles[id]; }
        public byte GetCUFProfilesCount()
        {
            return (byte)_CUFProfiles.Count(p => p != null);
        }

        bool IsActionButtonDataValid(byte button, uint action, uint type)
        {
            if (button >= PlayerConst.MaxActionButtons)
            {
                Log.outError(LogFilter.Player, "Action {0} not added into button {1} for player {2} (GUID: {3}): button must be < {4}", action, button, GetName(), GetGUID(), PlayerConst.MaxActionButtons);
                return false;
            }

            if (action >= PlayerConst.MaxActionButtonActionValue)
            {
                Log.outError(LogFilter.Player, "Action {0} not added into button {1} for player {2} (GUID: {3}): action must be < {4}", action, button, GetName(), GetGUID(), PlayerConst.MaxActionButtonActionValue);
                return false;
            }

            switch ((ActionButtonType)type)
            {
                case ActionButtonType.Spell:
                    if (Global.SpellMgr.GetSpellInfo(action) == null)
                    {
                        Log.outError(LogFilter.Player, "Spell action {0} not added into button {1} for player {2} (GUID: {3}): spell not exist", action, button, GetName(), GetGUID());
                        return false;
                    }

                    if (!HasSpell(action))
                    {
                        Log.outError(LogFilter.Player, "Spell action {0} not added into button {1} for player {2} (GUID: {3}): player don't known this spell", action, button, GetName(), GetGUID());
                        return false;
                    }
                    break;
                case ActionButtonType.Item:
                    if (Global.ObjectMgr.GetItemTemplate(action) == null)
                    {
                        Log.outError(LogFilter.Player, "Item action {0} not added into button {1} for player {2} (GUID: {3}): item not exist", action, button, GetName(), GetGUID());
                        return false;
                    }
                    break;
                case ActionButtonType.Mount:
                    var mount = CliDB.MountStorage.LookupByKey(action);
                    if (mount == null)
                    {
                        Log.outError(LogFilter.Player, "Mount action {0} not added into button {1} for player {2} ({3}): mount does not exist", action, button, GetName(), GetGUID().ToString());
                        return false;
                    }

                    if (!HasSpell(mount.SourceSpellID))
                    {
                        Log.outError(LogFilter.Player, "Mount action {0} not added into button {1} for player {2} ({3}): Player does not know this mount", action, button, GetName(), GetGUID().ToString());
                        return false;
                    }
                    break;
                case ActionButtonType.C:
                case ActionButtonType.CMacro:
                case ActionButtonType.Macro:
                case ActionButtonType.Eqset:
                    break;
                default:
                    Log.outError(LogFilter.Player, "Unknown action type {0}", type);
                    return false;                                          // other cases not checked at this moment
            }

            return true;
        }
        public ActionButton AddActionButton(byte button, uint action, uint type)
        {
            if (!IsActionButtonDataValid(button, action, type))
                return null;

            // it create new button (NEW state) if need or return existed
            if (!m_actionButtons.ContainsKey(button))
                m_actionButtons[button] = new ActionButton();

            var ab = m_actionButtons[button];

            // set data and update to CHANGED if not NEW
            ab.SetActionAndType(action, (ActionButtonType)type);

            Log.outDebug(LogFilter.Player, "Player '{0}' Added Action '{1}' (type {2}) to Button '{3}'", GetGUID().ToString(), action, type, button);
            return ab;
        }
        public void RemoveActionButton(byte _button)
        {
            var button = m_actionButtons.LookupByKey(_button);
            if (button == null || button.uState == ActionButtonUpdateState.Deleted)
                return;

            if (button.uState == ActionButtonUpdateState.New)
                m_actionButtons.Remove(_button);                   // new and not saved
            else
                button.uState = ActionButtonUpdateState.Deleted;    // saved, will deleted at next save

            Log.outDebug(LogFilter.Player, "Action Button '{0}' Removed from Player '{1}'", button, GetGUID().ToString());
        }
        public ActionButton GetActionButton(byte _button)
        {
            var button = m_actionButtons.LookupByKey(_button);
            if (button == null || button.uState == ActionButtonUpdateState.Deleted)
                return null;

            return button;
        }
        void SendInitialActionButtons() { SendActionButtons(0); }
        void SendActionButtons(uint state)
        {
            UpdateActionButtons packet = new UpdateActionButtons();

            foreach (var pair in m_actionButtons)
            {
                if (pair.Value.uState != ActionButtonUpdateState.Deleted && pair.Key < packet.ActionButtons.Length)
                    packet.ActionButtons[pair.Key] = pair.Value.packedData;
            }

            packet.Reason = (byte)state;
            SendPacket(packet);
        }

        //Repitation
        public int CalculateReputationGain(ReputationSource source, uint creatureOrQuestLevel, int rep, int faction, bool noQuestBonus = false)
        {
            float percent = 100.0f;

            float repMod = noQuestBonus ? 0.0f : GetTotalAuraModifier(AuraType.ModReputationGain);

            // faction specific auras only seem to apply to kills
            if (source == ReputationSource.Kill)
                repMod += GetTotalAuraModifierByMiscValue(AuraType.ModFactionReputationGain, faction);

            percent += rep > 0 ? repMod : -repMod;

            float rate;
            switch (source)
            {
                case ReputationSource.Kill:
                    rate = WorldConfig.GetFloatValue(WorldCfg.RateReputationLowLevelKill);
                    break;
                case ReputationSource.Quest:
                case ReputationSource.DailyQuest:
                case ReputationSource.WeeklyQuest:
                case ReputationSource.MonthlyQuest:
                case ReputationSource.RepeatableQuest:
                    rate = WorldConfig.GetFloatValue(WorldCfg.RateReputationLowLevelQuest);
                    break;
                case ReputationSource.Spell:
                default:
                    rate = 1.0f;
                    break;
            }

            if (rate != 1.0f && creatureOrQuestLevel < Formulas.GetGrayLevel(getLevel()))
                percent *= rate;

            if (percent <= 0.0f)
                return 0;

            // Multiply result with the faction specific rate
            RepRewardRate repData = Global.ObjectMgr.GetRepRewardRate((uint)faction);
            if (repData != null)
            {
                float repRate = 0.0f;
                switch (source)
                {
                    case ReputationSource.Kill:
                        repRate = repData.creatureRate;
                        break;
                    case ReputationSource.Quest:
                        repRate = repData.questRate;
                        break;
                    case ReputationSource.DailyQuest:
                        repRate = repData.questDailyRate;
                        break;
                    case ReputationSource.WeeklyQuest:
                        repRate = repData.questWeeklyRate;
                        break;
                    case ReputationSource.MonthlyQuest:
                        repRate = repData.questMonthlyRate;
                        break;
                    case ReputationSource.RepeatableQuest:
                        repRate = repData.questRepeatableRate;
                        break;
                    case ReputationSource.Spell:
                        repRate = repData.spellRate;
                        break;
                }

                // for custom, a rate of 0.0 will totally disable reputation gain for this faction/type
                if (repRate <= 0.0f)
                    return 0;

                percent *= repRate;
            }

            if (source != ReputationSource.Spell && GetsRecruitAFriendBonus(false))
                percent *= 1.0f + WorldConfig.GetFloatValue(WorldCfg.RateReputationRecruitAFriendBonus);

            return MathFunctions.CalculatePct(rep, percent);
        }
        // Calculates how many reputation points player gains in victim's enemy factions
        public void RewardReputation(Unit victim, float rate)
        {
            if (!victim || victim.IsTypeId(TypeId.Player))
                return;

            if (victim.ToCreature().IsReputationGainDisabled())
                return;

            ReputationOnKillEntry Rep = Global.ObjectMgr.GetReputationOnKilEntry(victim.ToCreature().GetCreatureTemplate().Entry);
            if (Rep == null)
                return;

            uint ChampioningFaction = 0;

            if (GetChampioningFaction() != 0)
            {
                // support for: Championing - http://www.wowwiki.com/Championing
                Map map = GetMap();
                if (map.IsNonRaidDungeon())
                {
                    LFGDungeonsRecord dungeon = Global.DB2Mgr.GetLfgDungeon(map.GetId(), map.GetDifficultyID());
                    if (dungeon != null)
                        if (dungeon.TargetLevel == 80)
                            ChampioningFaction = GetChampioningFaction();
                }
            }

            Team team = GetTeam();

            if (Rep.RepFaction1 != 0 && (!Rep.TeamDependent || team == Team.Alliance))
            {
                int donerep1 = CalculateReputationGain(ReputationSource.Kill, victim.GetLevelForTarget(this), Rep.RepValue1, (int)(ChampioningFaction != 0 ? ChampioningFaction : Rep.RepFaction1));
                donerep1 = (int)(donerep1 * rate);

                FactionRecord factionEntry1 = CliDB.FactionStorage.LookupByKey(ChampioningFaction != 0 ? ChampioningFaction : Rep.RepFaction1);
                ReputationRank current_reputation_rank1 = GetReputationMgr().GetRank(factionEntry1);
                if (factionEntry1 != null && (uint)current_reputation_rank1 <= Rep.ReputationMaxCap1)
                    GetReputationMgr().ModifyReputation(factionEntry1, donerep1);
            }

            if (Rep.RepFaction2 != 0 && (!Rep.TeamDependent || team == Team.Horde))
            {
                int donerep2 = CalculateReputationGain(ReputationSource.Kill, victim.GetLevelForTarget(this), Rep.RepValue2, (int)(ChampioningFaction != 0 ? ChampioningFaction : Rep.RepFaction2));
                donerep2 = (int)(donerep2 * rate);

                FactionRecord factionEntry2 = CliDB.FactionStorage.LookupByKey(ChampioningFaction != 0 ? ChampioningFaction : Rep.RepFaction2);
                ReputationRank current_reputation_rank2 = GetReputationMgr().GetRank(factionEntry2);
                if (factionEntry2 != null && (uint)current_reputation_rank2 <= Rep.ReputationMaxCap2)
                    GetReputationMgr().ModifyReputation(factionEntry2, donerep2);
            }
        }
        // Calculate how many reputation points player gain with the quest
        void RewardReputation(Quest quest)
        {
            for (byte i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                if (quest.RewardFactionId[i] == 0)
                    continue;

                FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(quest.RewardFactionId[i]);
                if (factionEntry == null)
                    continue;

                int rep = 0;
                bool noQuestBonus = false;

                if (quest.RewardFactionOverride[i] != 0)
                {
                    rep = quest.RewardFactionOverride[i] / 100;
                    noQuestBonus = true;
                }
                else
                {
                    uint row = (uint)((quest.RewardFactionValue[i] < 0) ? 1 : 0) + 1;
                    QuestFactionRewardRecord questFactionRewEntry = CliDB.QuestFactionRewardStorage.LookupByKey(row);
                    if (questFactionRewEntry != null)
                    {
                        uint field = (uint)Math.Abs(quest.RewardFactionValue[i]);
                        rep = questFactionRewEntry.Difficulty[field];
                    }
                }

                if (rep == 0)
                    continue;

                if (quest.RewardFactionCapIn[i] != 0 && rep > 0 && (uint)GetReputationMgr().GetRank(factionEntry) >= quest.RewardFactionCapIn[i])
                    continue;

                if (quest.IsDaily())
                    rep = CalculateReputationGain(ReputationSource.DailyQuest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);
                else if (quest.IsWeekly())
                    rep = CalculateReputationGain(ReputationSource.WeeklyQuest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);
                else if (quest.IsMonthly())
                    rep = CalculateReputationGain(ReputationSource.MonthlyQuest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);
                else if (quest.IsRepeatable())
                    rep = CalculateReputationGain(ReputationSource.RepeatableQuest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);
                else
                    rep = CalculateReputationGain(ReputationSource.Quest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);

                bool noSpillover = Convert.ToBoolean(quest.RewardReputationMask & (1 << i));
                GetReputationMgr().ModifyReputation(factionEntry, rep, noSpillover);
            }
        }

        //Movement
        bool IsCanDelayTeleport() { return m_bCanDelayTeleport; }
        void SetCanDelayTeleport(bool setting) { m_bCanDelayTeleport = setting; }
        bool IsHasDelayedTeleport() { return m_bHasDelayedTeleport; }
        void SetDelayedTeleportFlag(bool setting) { m_bHasDelayedTeleport = setting; }
        public bool TeleportTo(WorldLocation loc, TeleportToOptions options = 0)
        {
            return TeleportTo(loc.GetMapId(), loc.posX, loc.posY, loc.posZ, loc.Orientation, options);
        }
        public bool TeleportTo(uint mapid, float x, float y, float z, float orientation, TeleportToOptions options = 0)
        {
            if (!GridDefines.IsValidMapCoord(mapid, x, y, z, orientation))
            {
                Log.outError(LogFilter.Maps, "TeleportTo: invalid map ({0}) or invalid coordinates (X: {1}, Y: {2}, Z: {3}, O: {4}) given when teleporting player (GUID: {5}, name: {6}, map: {7}, {8}).",
                    mapid, x, y, z, orientation, GetGUID().ToString(), GetName(), GetMapId(), GetPosition().ToString());
                return false;
            }

            if (!GetSession().HasPermission(RBACPermissions.SkipCheckDisableMap) && Global.DisableMgr.IsDisabledFor(DisableType.Map, mapid, this))
            {
                Log.outError(LogFilter.Maps, "Player (GUID: {0}, name: {1}) tried to enter a forbidden map {2}", GetGUID().ToString(), GetName(), mapid);
                SendTransferAborted(mapid, TransferAbortReason.MapNotAllowed);
                return false;
            }

            // preparing unsummon pet if lost (we must get pet before teleportation or will not find it later)
            Pet pet = GetPet();

            MapRecord mEntry = CliDB.MapStorage.LookupByKey(mapid);

            // don't let enter Battlegrounds without assigned Battlegroundid (for example through areatrigger)...
            // don't let gm level > 1 either
            if (!InBattleground() && mEntry.IsBattlegroundOrArena())
                return false;

            // client without expansion support
            if (GetSession().GetExpansion() < mEntry.Expansion())
            {
                Log.outDebug(LogFilter.Maps, "Player {0} using client without required expansion tried teleport to non accessible map {1}", GetName(), mapid);

                Transport _transport = GetTransport();
                if (_transport)
                {
                    _transport.RemovePassenger(this);
                    RepopAtGraveyard();                             // teleport to near graveyard if on transport, looks blizz like :)
                }

                SendTransferAborted(mapid, TransferAbortReason.InsufExpanLvl, (byte)mEntry.Expansion());
                return false;                                       // normal client can't teleport to this map...
            }
            else
                Log.outDebug(LogFilter.Maps, "Player {0} is being teleported to map {1}", GetName(), mapid);

            if (m_vehicle != null)
                ExitVehicle();

            // reset movement flags at teleport, because player will continue move with these flags after teleport
            SetUnitMovementFlags(GetUnitMovementFlags() & MovementFlag.MaskHasPlayerStatusOpcode);
            m_movementInfo.ResetJump();
            DisableSpline();

            Transport transport = GetTransport();
            if (transport)
            {
                if (!options.HasAnyFlag(TeleportToOptions.NotLeaveTransport))
                    transport.RemovePassenger(this);
            }

            // The player was ported to another map and loses the duel immediately.
            // We have to perform this check before the teleport, otherwise the
            // ObjectAccessor won't find the flag.
            if (duel != null && GetMapId() != mapid && GetMap().GetGameObject(GetGuidValue(PlayerFields.DuelArbiter)))
                DuelComplete(DuelCompleteType.Fled);

            if (GetMapId() == mapid)
            {
                //lets reset far teleport flag if it wasn't reset during chained teleports
                SetSemaphoreTeleportFar(false);
                //setup delayed teleport flag
                SetDelayedTeleportFlag(IsCanDelayTeleport());
                //if teleport spell is casted in Unit.Update() func
                //then we need to delay it until update process will be finished
                if (IsHasDelayedTeleport())
                {
                    SetSemaphoreTeleportNear(true);
                    //lets save teleport destination for player
                    teleportDest = new WorldLocation(mapid, x, y, z, orientation);
                    m_teleport_options = options;
                    return true;
                }

                if (!options.HasAnyFlag(TeleportToOptions.NotUnSummonPet))
                {
                    //same map, only remove pet if out of range for new position
                    if (pet && !pet.IsWithinDist3d(x, y, z, GetMap().GetVisibilityRange()))
                        UnsummonPetTemporaryIfAny();
                }

                if (!options.HasAnyFlag(TeleportToOptions.NotLeaveCombat))
                    CombatStop();

                // this will be used instead of the current location in SaveToDB
                teleportDest = new WorldLocation(mapid, x, y, z, orientation);
                m_teleport_options = options;
                SetFallInformation(0, z);

                // code for finish transfer called in WorldSession.HandleMovementOpcodes()
                // at client packet CMSG_MOVE_TELEPORT_ACK
                SetSemaphoreTeleportNear(true);
                // near teleport, triggering send CMSG_MOVE_TELEPORT_ACK from client at landing
                if (!GetSession().PlayerLogout())
                    SendTeleportPacket(teleportDest);
            }
            else
            {
                if (GetClass() == Class.Deathknight && GetMapId() == 609 && !IsGameMaster() && !HasSpell(50977))
                    return false;

                // far teleport to another map
                Map oldmap = IsInWorld ? GetMap() : null;
                // check if we can enter before stopping combat / removing pet / totems / interrupting spells

                // Check enter rights before map getting to avoid creating instance copy for player
                // this check not dependent from map instance copy and same for all instance copies of selected map
                if (Global.MapMgr.PlayerCannotEnter(mapid, this, false) != 0)
                    return false;

                // Seamless teleport can happen only if cosmetic maps match
                if (!oldmap || (oldmap.GetEntry().CosmeticParentMapID != mapid && GetMapId() != mEntry.CosmeticParentMapID &&
                    !((oldmap.GetEntry().CosmeticParentMapID != -1) ^ (oldmap.GetEntry().CosmeticParentMapID != mEntry.CosmeticParentMapID))))
                    options &= ~TeleportToOptions.Seamless;

                //lets reset near teleport flag if it wasn't reset during chained teleports
                SetSemaphoreTeleportNear(false);
                //setup delayed teleport flag
                SetDelayedTeleportFlag(IsCanDelayTeleport());
                //if teleport spell is casted in Unit.Update() func
                //then we need to delay it until update process will be finished
                if (IsHasDelayedTeleport())
                {
                    SetSemaphoreTeleportFar(true);
                    //lets save teleport destination for player
                    teleportDest = new WorldLocation(mapid, x, y, z, orientation);
                    m_teleport_options = options;
                    return true;
                }

                SetSelection(ObjectGuid.Empty);

                CombatStop();

                ResetContestedPvP();

                // remove player from Battlegroundon far teleport (when changing maps)
                Battleground bg = GetBattleground();
                if (bg)
                {
                    // Note: at Battlegroundjoin Battlegroundid set before teleport
                    // and we already will found "current" Battleground
                    // just need check that this is targeted map or leave
                    if (bg.GetMapId() != mapid)
                        LeaveBattleground(false);                   // don't teleport to entry point
                }

                // remove arena spell coldowns/buffs now to also remove pet's cooldowns before it's temporarily unsummoned
                if (mEntry.IsBattleArena())
                {
                    RemoveArenaSpellCooldowns(true);
                    RemoveArenaAuras();
                    if (pet)
                        pet.RemoveArenaAuras();
                }

                // remove pet on map change
                if (pet)
                    UnsummonPetTemporaryIfAny();

                // remove all areatriggers entities
                RemoveAllAreaTriggers();

                // remove all dyn objects
                RemoveAllDynObjects();

                // stop spellcasting
                // not attempt interrupt teleportation spell at caster teleport
                if (!options.HasAnyFlag(TeleportToOptions.Spell))
                    if (IsNonMeleeSpellCast(true))
                        InterruptNonMeleeSpells(true);

                //remove auras before removing from map...
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ChangeMap | SpellAuraInterruptFlags.Move | SpellAuraInterruptFlags.Turning);

                if (!GetSession().PlayerLogout() && !options.HasAnyFlag(TeleportToOptions.Seamless))
                {
                    // send transfer packets
                    TransferPending transferPending = new TransferPending();
                    transferPending.MapID = (int)mapid;
                    transferPending.OldMapPosition = GetPosition();

                    transport = GetTransport();
                    if (transport)
                    {
                        transferPending.Ship.HasValue = true;
                        transferPending.Ship.Value.ID = transport.GetEntry();
                        transferPending.Ship.Value.OriginMapID = (int)GetMapId();
                    }

                    SendPacket(transferPending);
                }

                // remove from old map now
                if (oldmap != null)
                    oldmap.RemovePlayerFromMap(this, false);

                teleportDest = new WorldLocation(mapid, x, y, z, orientation);
                m_teleport_options = options;
                SetFallInformation(0, z);
                // if the player is saved before worldportack (at logout for example)
                // this will be used instead of the current location in SaveToDB

                if (!GetSession().PlayerLogout())
                {
                    SuspendToken suspendToken = new SuspendToken();
                    suspendToken.SequenceIndex = m_movementCounter; // not incrementing
                    suspendToken.Reason = options.HasAnyFlag(TeleportToOptions.Seamless) ? 2 : 1u;
                    SendPacket(suspendToken);
                }

                // move packet sent by client always after far teleport
                // code for finish transfer to new map called in WorldSession.HandleMoveWorldportAckOpcode at client packet
                SetSemaphoreTeleportFar(true);

            }
            return true;
        }
        public bool TeleportToBGEntryPoint()
        {
            if (m_bgData.joinPos.GetMapId() == 0xFFFFFFFF)
                return false;

            ScheduleDelayedOperation(PlayerDelayedOperations.BGMountRestore);
            ScheduleDelayedOperation(PlayerDelayedOperations.BGTaxiRestore);
            ScheduleDelayedOperation(PlayerDelayedOperations.BGGroupRestore);
            return TeleportTo(m_bgData.joinPos);
        }
        public WorldLocation GetStartPosition()
        {
            PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());
            uint mapId = info.MapId;
            if (GetClass() == Class.Deathknight && HasSpell(50977))
                mapId = 0;
            return new WorldLocation(mapId, info.PositionX, info.PositionY, info.PositionZ, 0);
        }
        public override bool IsUnderWater()
        {
            return IsInWater() &&
                GetPositionZ() < (GetMap().GetWaterLevel(GetPhaseShift(), GetPositionX(), GetPositionY()) - 2);
        }
        public override bool IsInWater()
        {
            return m_isInWater;
        }
        public void SetInWater(bool apply)
        {
            if (m_isInWater == apply)
                return;

            //define player in water by opcodes
            //move player's guid into HateOfflineList of those mobs
            //which can't swim and move guid back into ThreatList when
            //on surface.
            // @todo exist also swimming mobs, and function must be symmetric to enter/leave water
            m_isInWater = apply;

            // remove auras that need water/land
            RemoveAurasWithInterruptFlags((apply ? SpellAuraInterruptFlags.NotAbovewater : SpellAuraInterruptFlags.NotUnderwater));

            getHostileRefManager().updateThreatTables();
        }
        public void ValidateMovementInfo(MovementInfo mi)
        {
            var RemoveViolatingFlags = new Action<bool, MovementFlag>((check, maskToRemove) =>
            {
                if (check)
                {
                    Log.outDebug(LogFilter.Unit, "Player.ValidateMovementInfo: Violation of MovementFlags found ({0}). MovementFlags: {1}, MovementFlags2: {2} for player {3}. Mask {4} will be removed.",
                        check, mi.GetMovementFlags(), mi.GetMovementFlags2(), GetGUID().ToString(), maskToRemove);
                    mi.RemoveMovementFlag(maskToRemove);
                }
            });

            if (!GetVehicleBase() || !GetVehicle().GetVehicleInfo().Flags.HasAnyFlag(VehicleFlags.FixedPosition))
                RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Root), MovementFlag.Root);

            /*! This must be a packet spoofing attempt. MOVEMENTFLAG_ROOT sent from the client is not valid
                in conjunction with any of the moving movement flags such as MOVEMENTFLAG_FORWARD.
                It will freeze clients that receive this player's movement info.
            */
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Root) && mi.HasMovementFlag(MovementFlag.MaskMoving), MovementFlag.MaskMoving);

            //! Cannot hover without SPELL_AURA_HOVER
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Hover) && !HasAuraType(AuraType.Hover),
                MovementFlag.Hover);

            //! Cannot ascend and descend at the same time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Ascending) && mi.HasMovementFlag(MovementFlag.Descending),
                MovementFlag.Ascending | MovementFlag.Descending);

            //! Cannot move left and right at the same time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Left) && mi.HasMovementFlag(MovementFlag.Right),
                MovementFlag.Left | MovementFlag.Right);

            //! Cannot strafe left and right at the same time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.StrafeLeft) && mi.HasMovementFlag(MovementFlag.StrafeRight),
                MovementFlag.StrafeLeft | MovementFlag.StrafeRight);

            //! Cannot pitch up and down at the same time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.PitchUp) && mi.HasMovementFlag(MovementFlag.PitchDown),
                MovementFlag.PitchUp | MovementFlag.PitchDown);

            //! Cannot move forwards and backwards at the same time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Forward) && mi.HasMovementFlag(MovementFlag.Backward),
                MovementFlag.Forward | MovementFlag.Backward);

            //! Cannot walk on water without SPELL_AURA_WATER_WALK except for ghosts
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.WaterWalk) &&
                !HasAuraType(AuraType.WaterWalk) && !HasAuraType(AuraType.Ghost), MovementFlag.WaterWalk);

            //! Cannot feather fall without SPELL_AURA_FEATHER_FALL
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.FallingSlow) && !HasAuraType(AuraType.FeatherFall),
                MovementFlag.FallingSlow);

            /*! Cannot fly if no fly auras present. Exception is being a GM.
                Note that we check for account level instead of Player.IsGameMaster() because in some
                situations it may be feasable to use .gm fly on as a GM without having .gm on,
                e.g. aerial combat.
            */

            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Flying | MovementFlag.CanFly) && GetSession().GetSecurity() == AccountTypes.Player &&
                !m_unitMovedByMe.HasAuraType(AuraType.Fly) &&
                !m_unitMovedByMe.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed),
                MovementFlag.Flying | MovementFlag.CanFly);

            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.DisableGravity | MovementFlag.CanFly) && mi.HasMovementFlag(MovementFlag.Falling),
                MovementFlag.Falling);

            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.SplineElevation) && MathFunctions.fuzzyEq(mi.SplineElevation, 0.0f), MovementFlag.SplineElevation);

            // Client first checks if spline elevation != 0, then verifies flag presence
            if (MathFunctions.fuzzyNe(mi.SplineElevation, 0.0f))
                mi.AddMovementFlag(MovementFlag.SplineElevation);
        }
        public void HandleFall(MovementInfo movementInfo)
        {
            // calculate total z distance of the fall
            float z_diff = m_lastFallZ - movementInfo.Pos.posZ;
            Log.outDebug(LogFilter.Server, "zDiff = {0}", z_diff);

            //Players with low fall distance, Feather Fall or physical immunity (charges used) are ignored
            // 14.57 can be calculated by resolving damageperc formula below to 0
            if (z_diff >= 14.57f && !IsDead() && !IsGameMaster() &&
                !HasAuraType(AuraType.Hover) && !HasAuraType(AuraType.FeatherFall) &&
                !HasAuraType(AuraType.Fly) && !IsImmunedToDamage(SpellSchoolMask.Normal))
            {
                //Safe fall, fall height reduction
                int safe_fall = GetTotalAuraModifier(AuraType.SafeFall);

                float damageperc = 0.018f * (z_diff - safe_fall) - 0.2426f;

                if (damageperc > 0)
                {
                    uint damage = (uint)(damageperc * GetMaxHealth() * WorldConfig.GetFloatValue(WorldCfg.RateDamageFall));

                    float height = movementInfo.Pos.posZ;
                    UpdateGroundPositionZ(movementInfo.Pos.posX, movementInfo.Pos.posY, ref height);

                    if (damage > 0)
                    {
                        //Prevent fall damage from being more than the player maximum health
                        if (damage > GetMaxHealth())
                            damage = (uint)GetMaxHealth();

                        // Gust of Wind
                        if (HasAura(43621))
                            damage = (uint)GetMaxHealth() / 2;

                        uint original_health = (uint)GetHealth();
                        uint final_damage = EnvironmentalDamage(EnviromentalDamage.Fall, damage);

                        // recheck alive, might have died of EnvironmentalDamage, avoid cases when player die in fact like Spirit of Redemption case
                        if (IsAlive() && final_damage < original_health)
                            UpdateCriteria(CriteriaTypes.FallWithoutDying, (uint)z_diff * 100);
                    }

                    //Z given by moveinfo, LastZ, FallTime, WaterZ, MapZ, Damage, Safefall reduction
                    Log.outDebug(LogFilter.Player, "FALLDAMAGE z={0} sz={1} pZ{2} FallTime={3} mZ={4} damage={5} SF={6}",
                        movementInfo.Pos.posZ, height, GetPositionZ(), movementInfo.jump.fallTime, height, damage, safe_fall);
                }
            }
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Landing); // No fly zone - Parachute
        }
        public void UpdateFallInformationIfNeed(MovementInfo minfo, ClientOpcodes opcode)
        {
            if (m_lastFallTime >= m_movementInfo.jump.fallTime || m_lastFallZ <= m_movementInfo.Pos.posZ || opcode == ClientOpcodes.MoveFallLand)
                SetFallInformation(m_movementInfo.jump.fallTime, m_movementInfo.Pos.posZ);
        }

        public bool HasSummonPending()
        {
            return m_summon_expire >= Time.UnixTime;
        }

        public void SendSummonRequestFrom(Unit summoner)
        {
            if (!summoner)
                return;

            // Player already has active summon request
            if (HasSummonPending())
                return;

            // Evil Twin (ignore player summon, but hide this for summoner)
            if (HasAura(23445))
                return;

            m_summon_expire = Time.UnixTime + PlayerConst.MaxPlayerSummonDelay;
            m_summon_location = new WorldLocation(summoner);

            SummonRequest summonRequest = new SummonRequest();
            summonRequest.SummonerGUID = summoner.GetGUID();
            summonRequest.SummonerVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            summonRequest.AreaID = (int)summoner.GetZoneId();
            SendPacket(summonRequest);
        }
        public bool IsInAreaTriggerRadius(AreaTriggerRecord trigger)
        {
            if (trigger == null)
                return false;

            if (GetMapId() != trigger.ContinentID && !GetPhaseShift().HasVisibleMapId(trigger.ContinentID))
                return false;

            if (trigger.PhaseID != 0 || trigger.PhaseGroupID != 0 || trigger.PhaseUseFlags != 0)
                if (!PhasingHandler.InDbPhaseShift(this, (PhaseUseFlagsValues)trigger.PhaseUseFlags, trigger.PhaseID, trigger.PhaseGroupID))
                    return false;

            if (trigger.Radius > 0.0f)
            {
                // if we have radius check it
                float dist = GetDistance(trigger.Pos.X, trigger.Pos.Y, trigger.Pos.Z);
                if (dist > trigger.Radius)
                    return false;
            }
            else
            {
                Position center = new Position(trigger.Pos.X, trigger.Pos.Y, trigger.Pos.Z, trigger.BoxYaw);
                if (!IsWithinBox(center, trigger.BoxLength / 2.0f, trigger.BoxWidth / 2.0f, trigger.BoxHeight / 2.0f))
                    return false;
            }

            return true;
        }

        public void SummonIfPossible(bool agree)
        {
            if (!agree)
            {
                m_summon_expire = 0;
                return;
            }

            // expire and auto declined
            if (m_summon_expire < Time.UnixTime)
                return;

            // stop taxi flight at summon
            if (IsInFlight())
            {
                GetMotionMaster().MovementExpired();
                CleanupAfterTaxiFlight();
            }

            // drop flag at summon
            // this code can be reached only when GM is summoning player who carries flag, because player should be immune to summoning spells when he carries flag
            Battleground bg = GetBattleground();
            if (bg)
                bg.EventPlayerDroppedFlag(this);

            m_summon_expire = 0;

            UpdateCriteria(CriteriaTypes.AcceptedSummonings, 1);

            m_summon_location.SetOrientation(GetOrientation());
            TeleportTo(m_summon_location);
        }

        //GM
        public bool isAcceptWhispers() { return m_ExtraFlags.HasAnyFlag(PlayerExtraFlags.AcceptWhispers); }
        public void SetAcceptWhispers(bool on)
        {
            if (on)
                m_ExtraFlags |= PlayerExtraFlags.AcceptWhispers;
            else
                m_ExtraFlags &= ~PlayerExtraFlags.AcceptWhispers;
        }
        public bool IsGameMaster() { return Convert.ToBoolean(m_ExtraFlags & PlayerExtraFlags.GMOn); }
        public bool CanBeGameMaster() { return GetSession().HasPermission(RBACPermissions.CommandGm); }
        public void SetGameMaster(bool on)
        {
            if (on)
            {
                m_ExtraFlags |= PlayerExtraFlags.GMOn;
                SetFaction(35);
                SetFlag(PlayerFields.Flags, PlayerFlags.GM);
                SetFlag(UnitFields.Flags2, UnitFlags2.AllowCheatSpells);

                Pet pet = GetPet();
                if (pet != null)
                {
                    pet.SetFaction(35);
                    pet.getHostileRefManager().setOnlineOfflineState(false);
                }

                RemoveByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.FFAPvp);
                ResetContestedPvP();

                getHostileRefManager().setOnlineOfflineState(false);
                CombatStopWithPets();

                PhasingHandler.SetAlwaysVisible(GetPhaseShift(), true);
                m_serverSideVisibilityDetect.SetValue(ServerSideVisibilityType.GM, GetSession().GetSecurity());
            }
            else
            {
                PhasingHandler.SetAlwaysVisible(GetPhaseShift(), false);

                m_ExtraFlags &= ~PlayerExtraFlags.GMOn;
                SetFactionForRace(GetRace());
                RemoveFlag(PlayerFields.Flags, PlayerFlags.GM);
                RemoveFlag(UnitFields.Flags2, UnitFlags2.AllowCheatSpells);

                Pet pet = GetPet();
                if (pet != null)
                {
                    pet.SetFaction(getFaction());
                    pet.getHostileRefManager().setOnlineOfflineState(true);
                }

                // restore FFA PvP Server state
                if (Global.WorldMgr.IsFFAPvPRealm())
                    SetByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.FFAPvp);

                // restore FFA PvP area state, remove not allowed for GM mounts
                UpdateArea(m_areaUpdateId);

                getHostileRefManager().setOnlineOfflineState(true);
                m_serverSideVisibilityDetect.SetValue(ServerSideVisibilityType.GM, AccountTypes.Player);
            }

            UpdateObjectVisibility();
        }
        public bool isGMChat() { return m_ExtraFlags.HasAnyFlag(PlayerExtraFlags.GMChat); }
        public void SetGMChat(bool on)
        {
            if (on)
                m_ExtraFlags |= PlayerExtraFlags.GMChat;
            else
                m_ExtraFlags &= ~PlayerExtraFlags.GMChat;
        }
        public bool isTaxiCheater() { return m_ExtraFlags.HasAnyFlag(PlayerExtraFlags.TaxiCheat); }
        public void SetTaxiCheater(bool on)
        {
            if (on)
                m_ExtraFlags |= PlayerExtraFlags.TaxiCheat;
            else
                m_ExtraFlags &= ~PlayerExtraFlags.TaxiCheat;
        }
        public bool isGMVisible() { return !m_ExtraFlags.HasAnyFlag(PlayerExtraFlags.GMInvisible); }
        public void SetGMVisible(bool on)
        {
            if (on)
            {
                m_ExtraFlags &= ~PlayerExtraFlags.GMInvisible;         //remove flag
                m_serverSideVisibility.SetValue(ServerSideVisibilityType.GM, AccountTypes.Player);
            }
            else
            {
                m_ExtraFlags |= PlayerExtraFlags.GMInvisible;          //add flag

                SetAcceptWhispers(false);
                SetGameMaster(true);

                m_serverSideVisibility.SetValue(ServerSideVisibilityType.GM, GetSession().GetSecurity());
            }

            foreach (Channel channel in m_channels)
                channel.SetInvisible(this, !on);
        }

        //Chat - Text - Channel
        public void PrepareGossipMenu(WorldObject source, uint menuId = 0, bool showQuests = false)
        {
            PlayerMenu menu = PlayerTalkClass;
            menu.ClearMenus();

            menu.GetGossipMenu().SetMenuId(menuId);

            var menuItemBounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(menuId);

            // if default menuId and no menu options exist for this, use options from default options
            if (menuItemBounds.Empty() && menuId == GetDefaultGossipMenuForSource(source))
                menuItemBounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(0);

            NPCFlags npcflags = 0;

            if (source.IsTypeId(TypeId.Unit))
            {
                npcflags = (NPCFlags)source.GetUInt64Value(UnitFields.NpcFlags);
                if (Convert.ToBoolean(npcflags & NPCFlags.QuestGiver) && showQuests)
                    PrepareQuestMenu(source.GetGUID());
            }
            else if (source.IsTypeId(TypeId.GameObject))
                if (source.ToGameObject().GetGoType() == GameObjectTypes.QuestGiver)
                    PrepareQuestMenu(source.GetGUID());

            foreach (var menuItems in menuItemBounds)
            {
                if (!Global.ConditionMgr.IsObjectMeetToConditions(this, source, menuItems.Conditions))
                    continue;

                bool canTalk = true;
                GameObject go = source.ToGameObject();
                Creature creature = source.ToCreature();
                if (creature)
                {
                    if (!menuItems.OptionNpcFlag.HasAnyFlag(npcflags))
                        continue;

                    switch (menuItems.OptionType)
                    {
                        case GossipOption.Armorer:
                            canTalk = false;                       // added in special mode
                            break;
                        case GossipOption.Spirithealer:
                            if (!IsDead())
                                canTalk = false;
                            break;
                        case GossipOption.Vendor:
                            VendorItemData vendorItems = creature.GetVendorItems();
                            if (vendorItems == null || vendorItems.Empty())
                            {
                                Log.outError(LogFilter.Sql, "Creature (GUID: {0}, Entry: {1}) have UNIT_NPC_FLAG_VENDOR but have empty trading item list.", creature.GetGUID().ToString(), creature.GetEntry());
                                canTalk = false;
                            }
                            break;
                        case GossipOption.Learndualspec:
                                canTalk = false;
                            break;
                        case GossipOption.Unlearntalents:
                            if (!creature.CanResetTalents(this))
                                canTalk = false;
                            break;
                        case GossipOption.Taxivendor:
                            if (GetSession().SendLearnNewTaxiNode(creature))
                                return;
                            break;
                        case GossipOption.Battlefield:
                            if (!creature.isCanInteractWithBattleMaster(this, false))
                                canTalk = false;
                            break;
                        case GossipOption.Stablepet:
                            if (GetClass() != Class.Hunter)
                                canTalk = false;
                            break;
                        case GossipOption.Questgiver:
                            canTalk = false;
                            break;
                        case GossipOption.Trainer:
                        case GossipOption.Gossip:
                        case GossipOption.Spiritguide:
                        case GossipOption.Innkeeper:
                        case GossipOption.Banker:
                        case GossipOption.Petitioner:
                        case GossipOption.Tabarddesigner:
                        case GossipOption.Auctioneer:
                        case GossipOption.Transmogrifier:
                            break;                                  // no checks
                        case GossipOption.Outdoorpvp:
                            if (!Global.OutdoorPvPMgr.CanTalkTo(this, creature, menuItems))
                                canTalk = false;
                            break;
                        default:
                            Log.outError(LogFilter.Sql, "Creature entry {0} have unknown gossip option {1} for menu {2}", creature.GetEntry(), menuItems.OptionType, menuItems.MenuId);
                            canTalk = false;
                            break;
                    }
                }
                else if (go != null)
                {
                    switch (menuItems.OptionType)
                    {
                        case GossipOption.Gossip:
                            if (go.GetGoType() != GameObjectTypes.QuestGiver && go.GetGoType() != GameObjectTypes.Goober)
                                canTalk = false;
                            break;
                        default:
                            canTalk = false;
                            break;
                    }
                }

                if (canTalk)
                {
                    string strOptionText = "";
                    string strBoxText = "";
                    BroadcastTextRecord optionBroadcastText = CliDB.BroadcastTextStorage.LookupByKey(menuItems.OptionBroadcastTextId);
                    BroadcastTextRecord boxBroadcastText = CliDB.BroadcastTextStorage.LookupByKey(menuItems.BoxBroadcastTextId);
                    LocaleConstant locale = GetSession().GetSessionDbLocaleIndex();

                    if (optionBroadcastText != null)
                        strOptionText = Global.DB2Mgr.GetBroadcastTextValue(optionBroadcastText, locale, GetGender());
                    else
                        strOptionText = menuItems.OptionText;

                    if (boxBroadcastText != null)
                        strBoxText = Global.DB2Mgr.GetBroadcastTextValue(boxBroadcastText, locale, GetGender());
                    else
                        strBoxText = menuItems.BoxText;

                    if (locale != LocaleConstant.enUS)
                    {
                        if (optionBroadcastText == null)
                        {
                            // Find localizations from database.
                            GossipMenuItemsLocale gossipMenuLocale = Global.ObjectMgr.GetGossipMenuItemsLocale(menuId, menuItems.OptionIndex);
                            if (gossipMenuLocale != null)
                                ObjectManager.GetLocaleString(gossipMenuLocale.OptionText, locale, ref strOptionText);
                        }

                        if (boxBroadcastText == null)
                        {
                            // Find localizations from database.
                            GossipMenuItemsLocale gossipMenuLocale = Global.ObjectMgr.GetGossipMenuItemsLocale(menuId, menuItems.OptionIndex);
                            if (gossipMenuLocale != null)
                                ObjectManager.GetLocaleString(gossipMenuLocale.BoxText, locale, ref strBoxText);
                        }
                    }

                    menu.GetGossipMenu().AddMenuItem((int)menuItems.OptionIndex, menuItems.OptionIcon, strOptionText, 0, (uint)menuItems.OptionType, strBoxText, menuItems.BoxMoney, menuItems.BoxCoded);
                    menu.GetGossipMenu().AddGossipMenuItemData(menuItems.OptionIndex, menuItems.ActionMenuId, menuItems.ActionPoiId, menuItems.TrainerId);
                }
            }
        }
        public void SendPreparedGossip(WorldObject source)
        {
            if (!source)
                return;

            if (source.IsTypeId(TypeId.Unit) || source.IsTypeId(TypeId.GameObject))
            {
                if (PlayerTalkClass.GetGossipMenu().IsEmpty() && !PlayerTalkClass.GetQuestMenu().IsEmpty())
                {
                    SendPreparedQuest(source);
                    return;
                }
            }

            // in case non empty gossip menu (that not included quests list size) show it
            // (quest entries from quest menu will be included in list)

            uint textId = GetGossipTextId(source);
            uint menuId = PlayerTalkClass.GetGossipMenu().GetMenuId();
            if (menuId != 0)
                textId = GetGossipTextId(menuId, source);

            PlayerTalkClass.SendGossipMenu(textId, source.GetGUID());
        }
        public void OnGossipSelect(WorldObject source, uint optionIndex, uint menuId)
        {
            GossipMenu gossipMenu = PlayerTalkClass.GetGossipMenu();

            // if not same, then something funky is going on
            if (menuId != gossipMenu.GetMenuId())
                return;

            GossipMenuItem item = gossipMenu.GetItem(optionIndex);
            if (item == null)
                return;

            uint gossipOptionType = item.OptionType;
            ObjectGuid guid = source.GetGUID();

            if (source.IsTypeId(TypeId.GameObject))
            {
                if (gossipOptionType > (int)GossipOption.Questgiver)
                {
                    Log.outError(LogFilter.Player, "Player guid {0} request invalid gossip option for GameObject entry {1}", GetGUID().ToString(), source.GetEntry());
                    return;
                }
            }

            GossipMenuItemData menuItemData = gossipMenu.GetItemData(optionIndex);
            if (menuItemData == null)
                return;

            long cost = item.BoxMoney;
            if (!HasEnoughMoney(cost))
            {
                SendBuyError(BuyResult.NotEnoughtMoney, null, 0);
                PlayerTalkClass.SendCloseGossip();
                return;
            }

            switch ((GossipOption)gossipOptionType)
            {
                case GossipOption.Gossip:
                    {
                        if (menuItemData.GossipActionPoi != 0)
                            PlayerTalkClass.SendPointOfInterest(menuItemData.GossipActionPoi);

                        if (menuItemData.GossipActionMenuId != 0)
                        {
                            PrepareGossipMenu(source, menuItemData.GossipActionMenuId);
                            SendPreparedGossip(source);
                        }

                        break;
                    }
                case GossipOption.Outdoorpvp:
                    Global.OutdoorPvPMgr.HandleGossipOption(this, source.ToCreature(), optionIndex);
                    break;
                case GossipOption.Spirithealer:
                    if (IsDead())
                        source.ToCreature().CastSpell(source.ToCreature(), 17251, true, null, null, GetGUID());
                    break;
                case GossipOption.Questgiver:
                    PrepareQuestMenu(guid);
                    SendPreparedQuest(source);
                    break;
                case GossipOption.Vendor:
                case GossipOption.Armorer:
                    GetSession().SendListInventory(guid);
                    break;
                case GossipOption.Stablepet:
                    GetSession().SendStablePet(guid);
                    break;
                case GossipOption.Trainer:
                    GetSession().SendTrainerList(source.ToCreature(), menuItemData.TrainerId);
                    break;
                case GossipOption.Learndualspec:
                    break;
                case GossipOption.Unlearntalents:
                    PlayerTalkClass.SendCloseGossip();
                    SendRespecWipeConfirm(guid, GetNextResetTalentsCost());
                    break;
                case GossipOption.Taxivendor:
                    GetSession().SendTaxiMenu(source.ToCreature());
                    break;
                case GossipOption.Innkeeper:
                    PlayerTalkClass.SendCloseGossip();
                    SetBindPoint(guid);
                    break;
                case GossipOption.Banker:
                    GetSession().SendShowBank(guid);
                    break;
                case GossipOption.Petitioner:
                    PlayerTalkClass.SendCloseGossip();
                    GetSession().SendPetitionShowList(guid);
                    break;
                case GossipOption.Tabarddesigner:
                    PlayerTalkClass.SendCloseGossip();
                    GetSession().SendTabardVendorActivate(guid);
                    break;
                case GossipOption.Auctioneer:
                    GetSession().SendAuctionHello(guid, source.ToCreature());
                    break;
                case GossipOption.Spiritguide:
                    PrepareGossipMenu(source);
                    SendPreparedGossip(source);
                    break;
                case GossipOption.Battlefield:
                    {
                        BattlegroundTypeId bgTypeId = Global.BattlegroundMgr.GetBattleMasterBG(source.GetEntry());

                        if (bgTypeId == BattlegroundTypeId.None)
                        {
                            Log.outError(LogFilter.Player, "a user (guid {0}) requested Battlegroundlist from a npc who is no battlemaster", GetGUID().ToString());
                            return;
                        }

                        Global.BattlegroundMgr.SendBattlegroundList(this, guid, bgTypeId);
                        break;
                    }
                case GossipOption.Transmogrifier:
                    GetSession().SendOpenTransmogrifier(guid);
                    break;
            }

            ModifyMoney(-cost);
        }
        public uint GetGossipTextId(WorldObject source)
        {
            if (source == null)
                return SharedConst.DefaultGossipMessage;

            return GetGossipTextId(GetDefaultGossipMenuForSource(source), source);
        }
        uint GetGossipTextId(uint menuId, WorldObject source)
        {
            uint textId = SharedConst.DefaultGossipMessage;

            if (menuId == 0)
                return textId;

            var menuBounds = Global.ObjectMgr.GetGossipMenusMapBounds(menuId);

            foreach (var menu in menuBounds)
            {
                if (Global.ConditionMgr.IsObjectMeetToConditions(this, source, menu.Conditions))
                    textId = menu.TextId;
            }

            return textId;
        }
        public static uint GetDefaultGossipMenuForSource(WorldObject source)
        {
            switch (source.GetTypeId())
            {
                case TypeId.Unit:
                    return source.ToCreature().GetCreatureTemplate().GossipMenuId;
                case TypeId.GameObject:
                    return source.ToGameObject().GetGoInfo().GetGossipMenuId();
                default:
                    break;
            }

            return 0;
        }

        public bool CanJoinConstantChannelInZone(ChatChannelsRecord channel, AreaTableRecord zone)
        {
            if (channel.Flags.HasAnyFlag(ChannelDBCFlags.ZoneDep) && zone.Flags[0].HasAnyFlag(AreaFlags.ArenaInstance))
                return false;

            if (channel.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly) && !zone.Flags[0].HasAnyFlag(AreaFlags.Capital))
                return false;

            if (channel.Flags.HasAnyFlag(ChannelDBCFlags.GuildReq) && GetGuildId() != 0)
                return false;

            return true;
        }
        public void JoinedChannel(Channel c)
        {
            m_channels.Add(c);
        }
        public void LeftChannel(Channel c)
        {
            m_channels.Remove(c);
        }
        public void CleanupChannels()
        {
            while (!m_channels.Empty())
            {
                Channel ch = m_channels.FirstOrDefault();
                m_channels.RemoveAt(0);               // remove from player's channel list
                ch.LeaveChannel(this, false);                     // not send to client, not remove from player's channel list

                // delete channel if empty
                ChannelManager cMgr = ChannelManager.ForTeam(GetTeam());
                if (cMgr != null)
                {
                    if (ch.IsConstant())
                        cMgr.LeftChannel(ch.GetChannelId(), ch.GetZoneEntry());
                    else
                        cMgr.LeftChannel(ch.GetName());
                }
            }
            Log.outDebug(LogFilter.ChatSystem, "Player {0}: channels cleaned up!", GetName());
        }
        void UpdateLocalChannels(uint newZone)
        {
            if (GetSession().PlayerLoading() && !IsBeingTeleportedFar())
                return;                                              // The client handles it automatically after loading, but not after teleporting

            AreaTableRecord current_zone = CliDB.AreaTableStorage.LookupByKey(newZone);
            if (current_zone == null)
                return;

            ChannelManager cMgr = ChannelManager.ForTeam(GetTeam());
            if (cMgr == null)
                return;

            foreach (var channelEntry in CliDB.ChatChannelsStorage.Values)
            {
                Channel usedChannel = null;
                foreach (var channel in m_channels)
                {
                    if (channel.GetChannelId() == channelEntry.Id)
                    {
                        usedChannel = channel;
                        break;
                    }
                }

                Channel removeChannel = null;
                Channel joinChannel = null;
                bool sendRemove = true;

                if (CanJoinConstantChannelInZone(channelEntry, current_zone))
                {
                    if (!channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Global))
                    {
                        if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly) && usedChannel != null)
                            continue;                            // Already on the channel, as city channel names are not changing

                        joinChannel = cMgr.GetJoinChannel(channelEntry.Id, "", current_zone);
                        if (usedChannel != null)
                        {
                            if (joinChannel != usedChannel)
                            {
                                removeChannel = usedChannel;
                                sendRemove = false;              // Do not send leave channel, it already replaced at client
                            }
                            else
                                joinChannel = null;
                        }
                    }
                    else
                        joinChannel = cMgr.GetJoinChannel(channelEntry.Id, "");
                }
                else
                    removeChannel = usedChannel;

                if (joinChannel != null)
                    joinChannel.JoinChannel(this, "");          // Changed Channel: ... or Joined Channel: ...

                if (removeChannel != null)
                {
                    removeChannel.LeaveChannel(this, sendRemove, true); // Leave old channel

                    LeftChannel(removeChannel);                  // Remove from player's channel list
                    cMgr.LeftChannel(removeChannel.GetChannelId(), removeChannel.GetZoneEntry());                     // Delete if empty
                }
            }
        }

        public List<Channel> GetJoinedChannels() { return m_channels; }

        //Mail
        public void AddMail(Mail mail) { m_mail.Insert(0, mail); }
        public void RemoveMail(uint id)
        {
            foreach (var mail in m_mail)
            {
                if (mail.messageID == id)
                {
                    //do not delete item, because Player.removeMail() is called when returning mail to sender.
                    m_mail.Remove(mail);
                    return;
                }
            }
        }
        public void SendMailResult(uint mailId, MailResponseType mailAction, MailResponseResult mailError, InventoryResult equipError = 0, uint item_guid = 0, uint item_count = 0)
        {
            MailCommandResult result = new MailCommandResult();
            result.MailID = mailId;
            result.Command = (uint)mailAction;
            result.ErrorCode = (uint)mailError;

            if (mailError == MailResponseResult.EquipError)
                result.BagResult = (uint)equipError;
            else if (mailAction == MailResponseType.ItemTaken)
            {
                result.AttachID = item_guid;
                result.QtyInInventory = item_count;
            }

            SendPacket(result);
        }
        void SendNewMail()
        {
            SendPacket(new NotifyRecievedMail());
        }
        public void UpdateNextMailTimeAndUnreads()
        {
            // calculate next delivery time (min. from non-delivered mails
            // and recalculate unReadMail
            long cTime = Time.UnixTime;
            m_nextMailDelivereTime = 0;
            unReadMails = 0;
            foreach (var mail in m_mail)
            {
                if (mail.deliver_time > cTime)
                {
                    if (m_nextMailDelivereTime == 0 || m_nextMailDelivereTime > mail.deliver_time)
                        m_nextMailDelivereTime = mail.deliver_time;
                }
                else if ((mail.checkMask & MailCheckMask.Read) == 0)
                    ++unReadMails;
            }
        }
        public void AddNewMailDeliverTime(long deliver_time)
        {
            if (deliver_time <= Time.UnixTime)                          // ready now
            {
                ++unReadMails;
                SendNewMail();
            }
            else                                                    // not ready and no have ready mails
            {
                if (m_nextMailDelivereTime == 0 || m_nextMailDelivereTime > deliver_time)
                    m_nextMailDelivereTime = deliver_time;
            }
        }
        public bool IsMailsLoaded() { return m_mailsLoaded; }
        public void AddMItem(Item it)
        {
            mMitems[it.GetGUID().GetCounter()] = it;
        }
        public bool RemoveMItem(ulong id)
        {
            return mMitems.Remove(id);
        }
        public Item GetMItem(ulong id) { return mMitems.LookupByKey(id); }
        public Mail GetMail(uint id) { return m_mail.Find(p => p.messageID == id); }
        public List<Mail> GetMails() { return m_mail; }

        //Binds
        public bool HasPendingBind() { return _pendingBindId > 0; }
        void UpdateHomebindTime(uint time)
        {
            // GMs never get homebind timer online
            if (m_InstanceValid || IsGameMaster())
            {
                if (m_HomebindTimer != 0) // instance valid, but timer not reset
                    SendRaidGroupOnlyMessage(RaidGroupReason.None, 0);

                // instance is valid, reset homebind timer
                m_HomebindTimer = 0;
            }
            else if (m_HomebindTimer > 0)
            {
                if (time >= m_HomebindTimer)
                {
                    // teleport to nearest graveyard
                    RepopAtGraveyard();
                }
                else
                    m_HomebindTimer -= time;
            }
            else
            {
                // instance is invalid, start homebind timer
                m_HomebindTimer = 60000;
                // send message to player
                SendRaidGroupOnlyMessage(RaidGroupReason.RequirementsUnmatch, (int)m_HomebindTimer);
                Log.outDebug(LogFilter.Maps, "PLAYER: Player '{0}' (GUID: {1}) will be teleported to homebind in 60 seconds", GetName(), GetGUID().ToString());
            }
        }
        public void SetHomebind(WorldLocation loc, uint areaId)
        {
            homebind = loc;
            homebindAreaId = areaId;

            // update sql homebind
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_PLAYER_HOMEBIND);
            stmt.AddValue(0, homebind.GetMapId());
            stmt.AddValue(1, homebindAreaId);
            stmt.AddValue(2, homebind.posX);
            stmt.AddValue(3, homebind.posY);
            stmt.AddValue(4, homebind.posZ);
            stmt.AddValue(5, GetGUID().GetCounter());
            DB.Characters.Execute(stmt);
        }
        void SetBindPoint(ObjectGuid guid)
        {
            BinderConfirm packet = new BinderConfirm(guid);
            SendPacket(packet);
        }
        public void SendBindPointUpdate()
        {
            BindPointUpdate packet = new BindPointUpdate();
            packet.BindPosition.X = homebind.GetPositionX();
            packet.BindPosition.Y = homebind.GetPositionY();
            packet.BindPosition.Z = homebind.GetPositionZ();
            packet.BindMapID = homebind.GetMapId();
            packet.BindAreaID = homebindAreaId;
            SendPacket(packet);
        }

        //Misc
        public uint GetTotalPlayedTime() { return m_PlayedTimeTotal; }
        public uint GetLevelPlayedTime() { return m_PlayedTimeLevel; }

        public CinematicManager GetCinematicMgr() { return _cinematicMgr; }

        public void SendUpdateWorldState(uint variable, uint value, bool hidden = false)
        {
            UpdateWorldState worldstate = new UpdateWorldState();
            worldstate.VariableID = variable;
            worldstate.Value = (int)value;
            worldstate.Hidden = hidden;
            SendPacket(worldstate);
        }

        void SendInitWorldStates(uint zoneid, uint areaid)
        {
            // data depends on zoneid/mapid...
            Battleground bg = GetBattleground();
            uint mapid = GetMapId();
            OutdoorPvP pvp = Global.OutdoorPvPMgr.GetOutdoorPvPToZoneId(zoneid);
            InstanceScript instance = GetInstanceScript();
            BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(zoneid);

            InitWorldStates packet = new InitWorldStates();
            packet.MapID = mapid;
            packet.AreaID = zoneid;
            packet.SubareaID = areaid;
            packet.AddState(2264, 0);              // 1
            packet.AddState(2263, 0);              // 2
            packet.AddState(2262, 0);              // 3
            packet.AddState(2261, 0);              // 4
            packet.AddState(2260, 0);              // 5
            packet.AddState(2259, 0);              // 6

            packet.AddState(3191, WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress) ? WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId) : 0); // 7 Current Season - Arena season in progress
                                                                                                                                                                                               // 0 - End of season
            packet.AddState(3901, WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId) - (WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress) ? 1 : 0));     // 8 PreviousSeason

            if (mapid == 530)                                       // Outland
            {
                packet.AddState(2495, 0);          // 7
                packet.AddState(2493, 0xF);        // 8
                packet.AddState(2491, 0xF);        // 9
            }

            // insert <field> <value>
            switch (zoneid)
            {
                case 1:                                             // Dun Morogh
                case 11:                                            // Wetlands
                case 12:                                            // Elwynn Forest
                case 38:                                            // Loch Modan
                case 40:                                            // Westfall
                case 51:                                            // Searing Gorge
                case 1519:                                          // Stormwind City
                case 1537:                                          // Ironforge
                case 2257:                                          // Deeprun Tram
                case 3703:                                          // Shattrath City});
                    break;
                case 1377:                                          // Silithus
                    if (pvp != null && pvp.GetTypeId() == OutdoorPvPTypes.Silithus)
                        pvp.FillInitialWorldStates(packet);
                    else
                    {
                        // states are always shown
                        packet.AddState(2313, 0x0); // 7 ally silityst gathered
                        packet.AddState(2314, 0x0); // 8 horde silityst gathered
                        packet.AddState(2317, 0x0); // 9 max silithyst
                    }
                    // dunno about these... aq opening event maybe?
                    packet.AddState(2322, 0x0); // 10 sandworm N
                    packet.AddState(2323, 0x0); // 11 sandworm S
                    packet.AddState(2324, 0x0); // 12 sandworm SW
                    packet.AddState(2325, 0x0); // 13 sandworm E
                    break;
                case 2597:                                          // Alterac Valley
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.AV)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0x7ae, 0x1);           // 7 snowfall n
                        packet.AddState(0x532, 0x1);           // 8 frostwolfhut hc
                        packet.AddState(0x531, 0x0);           // 9 frostwolfhut ac
                        packet.AddState(0x52e, 0x0);           // 10 stormpike firstaid a_a
                        packet.AddState(0x571, 0x0);           // 11 east frostwolf tower horde assaulted -unused
                        packet.AddState(0x570, 0x0);           // 12 west frostwolf tower horde assaulted - unused
                        packet.AddState(0x567, 0x1);           // 13 frostwolfe c
                        packet.AddState(0x566, 0x1);           // 14 frostwolfw c
                        packet.AddState(0x550, 0x1);           // 15 irondeep (N) ally
                        packet.AddState(0x544, 0x0);           // 16 ice grave a_a
                        packet.AddState(0x536, 0x0);           // 17 stormpike grave h_c
                        packet.AddState(0x535, 0x1);           // 18 stormpike grave a_c
                        packet.AddState(0x518, 0x0);           // 19 stoneheart grave a_a
                        packet.AddState(0x517, 0x0);           // 20 stoneheart grave h_a
                        packet.AddState(0x574, 0x0);           // 21 1396 unk
                        packet.AddState(0x573, 0x0);           // 22 iceblood tower horde assaulted -unused
                        packet.AddState(0x572, 0x0);           // 23 towerpoint horde assaulted - unused
                        packet.AddState(0x56f, 0x0);           // 24 1391 unk
                        packet.AddState(0x56e, 0x0);           // 25 iceblood a
                        packet.AddState(0x56d, 0x0);           // 26 towerp a
                        packet.AddState(0x56c, 0x0);           // 27 frostwolfe a
                        packet.AddState(0x56b, 0x0);           // 28 froswolfw a
                        packet.AddState(0x56a, 0x1);           // 29 1386 unk
                        packet.AddState(0x569, 0x1);           // 30 iceblood c
                        packet.AddState(0x568, 0x1);           // 31 towerp c
                        packet.AddState(0x565, 0x0);           // 32 stoneh tower a
                        packet.AddState(0x564, 0x0);           // 33 icewing tower a
                        packet.AddState(0x563, 0x0);           // 34 dunn a
                        packet.AddState(0x562, 0x0);           // 35 duns a
                        packet.AddState(0x561, 0x0);           // 36 stoneheart bunker alliance assaulted - unused
                        packet.AddState(0x560, 0x0);           // 37 icewing bunker alliance assaulted - unused
                        packet.AddState(0x55f, 0x0);           // 38 dunbaldar south alliance assaulted - unused
                        packet.AddState(0x55e, 0x0);           // 39 dunbaldar north alliance assaulted - unused
                        packet.AddState(0x55d, 0x0);           // 40 stone tower d
                        packet.AddState(0x3c6, 0x0);           // 41 966 unk
                        packet.AddState(0x3c4, 0x0);           // 42 964 unk
                        packet.AddState(0x3c2, 0x0);           // 43 962 unk
                        packet.AddState(0x516, 0x1);           // 44 stoneheart grave a_c
                        packet.AddState(0x515, 0x0);           // 45 stonheart grave h_c
                        packet.AddState(0x3b6, 0x0);           // 46 950 unk
                        packet.AddState(0x55c, 0x0);           // 47 icewing tower d
                        packet.AddState(0x55b, 0x0);           // 48 dunn d
                        packet.AddState(0x55a, 0x0);           // 49 duns d
                        packet.AddState(0x559, 0x0);           // 50 1369 unk
                        packet.AddState(0x558, 0x0);           // 51 iceblood d
                        packet.AddState(0x557, 0x0);           // 52 towerp d
                        packet.AddState(0x556, 0x0);           // 53 frostwolfe d
                        packet.AddState(0x555, 0x0);           // 54 frostwolfw d
                        packet.AddState(0x554, 0x1);           // 55 stoneh tower c
                        packet.AddState(0x553, 0x1);           // 56 icewing tower c
                        packet.AddState(0x552, 0x1);           // 57 dunn c
                        packet.AddState(0x551, 0x1);           // 58 duns c
                        packet.AddState(0x54f, 0x0);           // 59 irondeep (N) horde
                        packet.AddState(0x54e, 0x0);           // 60 irondeep (N) ally
                        packet.AddState(0x54d, 0x1);           // 61 mine (S) neutral
                        packet.AddState(0x54c, 0x0);           // 62 mine (S) horde
                        packet.AddState(0x54b, 0x0);           // 63 mine (S) ally
                        packet.AddState(0x545, 0x0);           // 64 iceblood h_a
                        packet.AddState(0x543, 0x1);           // 65 iceblod h_c
                        packet.AddState(0x542, 0x0);           // 66 iceblood a_c
                        packet.AddState(0x540, 0x0);           // 67 snowfall h_a
                        packet.AddState(0x53f, 0x0);           // 68 snowfall a_a
                        packet.AddState(0x53e, 0x0);           // 69 snowfall h_c
                        packet.AddState(0x53d, 0x0);           // 70 snowfall a_c
                        packet.AddState(0x53c, 0x0);           // 71 frostwolf g h_a
                        packet.AddState(0x53b, 0x0);           // 72 frostwolf g a_a
                        packet.AddState(0x53a, 0x1);           // 73 frostwolf g h_c
                        packet.AddState(0x539, 0x0);           // 74 frostwolf g a_c
                        packet.AddState(0x538, 0x0);           // 75 stormpike grave h_a
                        packet.AddState(0x537, 0x0);           // 76 stormpike grave a_a
                        packet.AddState(0x534, 0x0);           // 77 frostwolf hut h_a
                        packet.AddState(0x533, 0x0);           // 78 frostwolf hut a_a
                        packet.AddState(0x530, 0x0);           // 79 stormpike first aid h_a
                        packet.AddState(0x52f, 0x0);           // 80 stormpike first aid h_c
                        packet.AddState(0x52d, 0x1);           // 81 stormpike first aid a_c
                    }
                    break;
                case 3277:                                          // Warsong Gulch
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.WS)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0x62d, 0x0);       // 7 1581 alliance flag captures
                        packet.AddState(0x62e, 0x0);       // 8 1582 horde flag captures
                        packet.AddState(0x609, 0x0);       // 9 1545 unk, set to 1 on alliance flag pickup...
                        packet.AddState(0x60a, 0x0);       // 10 1546 unk, set to 1 on horde flag pickup, after drop it's -1
                        packet.AddState(0x60b, 0x2);       // 11 1547 unk
                        packet.AddState(0x641, 0x3);       // 12 1601 unk (max flag captures?)
                        packet.AddState(0x922, 0x1);       // 13 2338 horde (0 - hide, 1 - flag ok, 2 - flag picked up (flashing), 3 - flag picked up (not flashing)
                        packet.AddState(0x923, 0x1);       // 14 2339 alliance (0 - hide, 1 - flag ok, 2 - flag picked up (flashing), 3 - flag picked up (not flashing)
                    }
                    break;
                case 3358:                                          // Arathi Basin
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.AB)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0x6e7, 0x0);       // 7 1767 stables alliance
                        packet.AddState(0x6e8, 0x0);       // 8 1768 stables horde
                        packet.AddState(0x6e9, 0x0);       // 9 1769 unk, ST?
                        packet.AddState(0x6ea, 0x0);       // 10 1770 stables (show/hide)
                        packet.AddState(0x6ec, 0x0);       // 11 1772 farm (0 - horde controlled, 1 - alliance controlled)
                        packet.AddState(0x6ed, 0x0);       // 12 1773 farm (show/hide)
                        packet.AddState(0x6ee, 0x0);       // 13 1774 farm color
                        packet.AddState(0x6ef, 0x0);       // 14 1775 gold mine color, may be FM?
                        packet.AddState(0x6f0, 0x0);       // 15 1776 alliance resources
                        packet.AddState(0x6f1, 0x0);       // 16 1777 horde resources
                        packet.AddState(0x6f2, 0x0);       // 17 1778 horde bases
                        packet.AddState(0x6f3, 0x0);       // 18 1779 alliance bases
                        packet.AddState(0x6f4, 0x7d0);     // 19 1780 max resources (2000)
                        packet.AddState(0x6f6, 0x0);       // 20 1782 blacksmith color
                        packet.AddState(0x6f7, 0x0);       // 21 1783 blacksmith (show/hide)
                        packet.AddState(0x6f8, 0x0);       // 22 1784 unk, bs?
                        packet.AddState(0x6f9, 0x0);       // 23 1785 unk, bs?
                        packet.AddState(0x6fb, 0x0);       // 24 1787 gold mine (0 - horde contr, 1 - alliance contr)
                        packet.AddState(0x6fc, 0x0);       // 25 1788 gold mine (0 - conflict, 1 - horde)
                        packet.AddState(0x6fd, 0x0);       // 26 1789 gold mine (1 - show/0 - hide)
                        packet.AddState(0x6fe, 0x0);       // 27 1790 gold mine color
                        packet.AddState(0x700, 0x0);       // 28 1792 gold mine color, wtf?, may be LM?
                        packet.AddState(0x701, 0x0);       // 29 1793 lumber mill color (0 - conflict, 1 - horde contr)
                        packet.AddState(0x702, 0x0);       // 30 1794 lumber mill (show/hide)
                        packet.AddState(0x703, 0x0);       // 31 1795 lumber mill color color
                        packet.AddState(0x732, 0x1);       // 32 1842 stables (1 - uncontrolled)
                        packet.AddState(0x733, 0x1);       // 33 1843 gold mine (1 - uncontrolled)
                        packet.AddState(0x734, 0x1);       // 34 1844 lumber mill (1 - uncontrolled)
                        packet.AddState(0x735, 0x1);       // 35 1845 farm (1 - uncontrolled)
                        packet.AddState(0x736, 0x1);       // 36 1846 blacksmith (1 - uncontrolled)
                        packet.AddState(0x745, 0x2);       // 37 1861 unk
                        packet.AddState(0x7a3, 0x708);     // 38 1955 warning limit (1800)
                    }
                    break;
                case 3820:                                          // Eye of the Storm
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.EY)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0xac1, 0x0);       // 7  2753 Horde Bases
                        packet.AddState(0xac0, 0x0);       // 8  2752 Alliance Bases
                        packet.AddState(0xab6, 0x0);       // 9  2742 Mage Tower - Horde conflict
                        packet.AddState(0xab5, 0x0);       // 10 2741 Mage Tower - Alliance conflict
                        packet.AddState(0xab4, 0x0);       // 11 2740 Fel Reaver - Horde conflict
                        packet.AddState(0xab3, 0x0);       // 12 2739 Fel Reaver - Alliance conflict
                        packet.AddState(0xab2, 0x0);       // 13 2738 Draenei - Alliance conflict
                        packet.AddState(0xab1, 0x0);       // 14 2737 Draenei - Horde conflict
                        packet.AddState(0xab0, 0x0);       // 15 2736 unk // 0 at start
                        packet.AddState(0xaaf, 0x0);       // 16 2735 unk // 0 at start
                        packet.AddState(0xaad, 0x0);       // 17 2733 Draenei - Horde control
                        packet.AddState(0xaac, 0x0);       // 18 2732 Draenei - Alliance control
                        packet.AddState(0xaab, 0x1);       // 19 2731 Draenei uncontrolled (1 - yes, 0 - no)
                        packet.AddState(0xaaa, 0x0);       // 20 2730 Mage Tower - Alliance control
                        packet.AddState(0xaa9, 0x0);       // 21 2729 Mage Tower - Horde control
                        packet.AddState(0xaa8, 0x1);       // 22 2728 Mage Tower uncontrolled (1 - yes, 0 - no)
                        packet.AddState(0xaa7, 0x0);       // 23 2727 Fel Reaver - Horde control
                        packet.AddState(0xaa6, 0x0);       // 24 2726 Fel Reaver - Alliance control
                        packet.AddState(0xaa5, 0x1);       // 25 2725 Fel Reaver uncontrolled (1 - yes, 0 - no)
                        packet.AddState(0xaa4, 0x0);       // 26 2724 Boold Elf - Horde control
                        packet.AddState(0xaa3, 0x0);       // 27 2723 Boold Elf - Alliance control
                        packet.AddState(0xaa2, 0x1);       // 28 2722 Boold Elf uncontrolled (1 - yes, 0 - no)
                        packet.AddState(0xac5, 0x1);       // 29 2757 Flag (1 - show, 0 - hide) - doesn't work exactly this way!
                        packet.AddState(0xad2, 0x1);       // 30 2770 Horde top-stats (1 - show, 0 - hide) // 02 . horde picked up the flag
                        packet.AddState(0xad1, 0x1);       // 31 2769 Alliance top-stats (1 - show, 0 - hide) // 02 . alliance picked up the flag
                        packet.AddState(0xabe, 0x0);       // 32 2750 Horde resources
                        packet.AddState(0xabd, 0x0);       // 33 2749 Alliance resources
                        packet.AddState(0xa05, 0x8e);      // 34 2565 unk, constant?
                        packet.AddState(0xaa0, 0x0);       // 35 2720 Capturing progress-bar (100 . empty (only grey), 0 . blue|red (no grey), default 0)
                        packet.AddState(0xa9f, 0x0);       // 36 2719 Capturing progress-bar (0 - left, 100 - right)
                        packet.AddState(0xa9e, 0x0);       // 37 2718 Capturing progress-bar (1 - show, 0 - hide)
                        packet.AddState(0xc0d, 0x17b);     // 38 3085 unk
                                                                                                     // and some more ... unknown
                    }
                    break;
                // any of these needs change! the client remembers the prev setting!
                // ON EVERY ZONE LEAVE, RESET THE OLD ZONE'S WORLD STATE, BUT AT LEAST THE UI STUFF!
                case 3483:                                          // Hellfire Peninsula
                    if (pvp != null && pvp.GetTypeId() == OutdoorPvPTypes.HellfirePeninsula)
                        pvp.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0x9ba, 0x1);           // 10 // add ally tower main gui icon       // maybe should be sent only on login?
                        packet.AddState(0x9b9, 0x1);           // 11 // add horde tower main gui icon      // maybe should be sent only on login?
                        packet.AddState(0x9b5, 0x0);           // 12 // show neutral broken hill icon      // 2485
                        packet.AddState(0x9b4, 0x1);           // 13 // show icon above broken hill        // 2484
                        packet.AddState(0x9b3, 0x0);           // 14 // show ally broken hill icon         // 2483
                        packet.AddState(0x9b2, 0x0);           // 15 // show neutral overlook icon         // 2482
                        packet.AddState(0x9b1, 0x1);           // 16 // show the overlook arrow            // 2481
                        packet.AddState(0x9b0, 0x0);           // 17 // show ally overlook icon            // 2480
                        packet.AddState(0x9ae, 0x0);           // 18 // horde pvp objectives captured      // 2478
                        packet.AddState(0x9ac, 0x0);           // 19 // ally pvp objectives captured       // 2476
                        packet.AddState(2475, 100);            //: ally / horde slider grey area                              // show only in direct vicinity!
                        packet.AddState(2474, 50);             //: ally / horde slider percentage, 100 for ally, 0 for horde  // show only in direct vicinity!
                        packet.AddState(2473, 0);              //: ally / horde slider display                                // show only in direct vicinity!
                        packet.AddState(0x9a8, 0x0);           // 20 // show the neutral stadium icon      // 2472
                        packet.AddState(0x9a7, 0x0);           // 21 // show the ally stadium icon         // 2471
                        packet.AddState(0x9a6, 0x1);           // 22 // show the horde stadium icon        // 2470
                    }
                    break;
                case 3518:                                          // Nagrand
                    if (pvp != null && pvp.GetTypeId() == OutdoorPvPTypes.Nagrand)
                        pvp.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(2503, 0x0);    // 10
                        packet.AddState(2502, 0x0);    // 11
                        packet.AddState(2493, 0x0);    // 12
                        packet.AddState(2491, 0x0);    // 13

                        packet.AddState(2495, 0x0);    // 14
                        packet.AddState(2494, 0x0);    // 15
                        packet.AddState(2497, 0x0);    // 16

                        packet.AddState(2762, 0x0);    // 17
                        packet.AddState(2662, 0x0);    // 18
                        packet.AddState(2663, 0x0);    // 19
                        packet.AddState(2664, 0x0);    // 20

                        packet.AddState(2760, 0x0);    // 21
                        packet.AddState(2670, 0x0);    // 22
                        packet.AddState(2668, 0x0);    // 23
                        packet.AddState(2669, 0x0);    // 24

                        packet.AddState(2761, 0x0);    // 25
                        packet.AddState(2667, 0x0);    // 26
                        packet.AddState(2665, 0x0);    // 27
                        packet.AddState(2666, 0x0);    // 28

                        packet.AddState(2763, 0x0);    // 29
                        packet.AddState(2659, 0x0);    // 30
                        packet.AddState(2660, 0x0);    // 31
                        packet.AddState(2661, 0x0);    // 32

                        packet.AddState(2671, 0x0);    // 33
                        packet.AddState(2676, 0x0);    // 34
                        packet.AddState(2677, 0x0);    // 35
                        packet.AddState(2672, 0x0);    // 36
                        packet.AddState(2673, 0x0);    // 37
                    }
                    break;
                case 3519:                                          // Terokkar Forest
                    if (pvp != null && pvp.GetTypeId() == OutdoorPvPTypes.TerokkarForest)
                        pvp.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0xa41, 0x0);           // 10 // 2625 capture bar pos
                        packet.AddState(0xa40, 0x14);          // 11 // 2624 capture bar neutral
                        packet.AddState(0xa3f, 0x0);           // 12 // 2623 show capture bar
                        packet.AddState(0xa3e, 0x0);           // 13 // 2622 horde towers controlled
                        packet.AddState(0xa3d, 0x5);           // 14 // 2621 ally towers controlled
                        packet.AddState(0xa3c, 0x0);           // 15 // 2620 show towers controlled
                        packet.AddState(0xa88, 0x0);           // 16 // 2696 SE Neu
                        packet.AddState(0xa87, 0x0);           // 17 // SE Horde
                        packet.AddState(0xa86, 0x0);           // 18 // SE Ally
                        packet.AddState(0xa85, 0x0);           // 19 //S Neu
                        packet.AddState(0xa84, 0x0);           // 20 S Horde
                        packet.AddState(0xa83, 0x0);           // 21 S Ally
                        packet.AddState(0xa82, 0x0);           // 22 NE Neu
                        packet.AddState(0xa81, 0x0);           // 23 NE Horde
                        packet.AddState(0xa80, 0x0);           // 24 NE Ally
                        packet.AddState(0xa7e, 0x0);           // 25 // 2686 N Neu
                        packet.AddState(0xa7d, 0x0);           // 26 N Horde
                        packet.AddState(0xa7c, 0x0);           // 27 N Ally
                        packet.AddState(0xa7b, 0x0);           // 28 NW Ally
                        packet.AddState(0xa7a, 0x0);           // 29 NW Horde
                        packet.AddState(0xa79, 0x0);           // 30 NW Neutral
                        packet.AddState(0x9d0, 0x5);           // 31 // 2512 locked time remaining seconds first digit
                        packet.AddState(0x9ce, 0x0);           // 32 // 2510 locked time remaining seconds second digit
                        packet.AddState(0x9cd, 0x0);           // 33 // 2509 locked time remaining minutes
                        packet.AddState(0x9cc, 0x0);           // 34 // 2508 neutral locked time show
                        packet.AddState(0xad0, 0x0);           // 35 // 2768 horde locked time show
                        packet.AddState(0xacf, 0x1);           // 36 // 2767 ally locked time show
                    }
                    break;
                case 3521:                                          // Zangarmarsh
                    if (pvp != null && pvp.GetTypeId() == OutdoorPvPTypes.Zangarmarsh)
                        pvp.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0x9e1, 0x0);           // 10 //2529
                        packet.AddState(0x9e0, 0x0);           // 11
                        packet.AddState(0x9df, 0x0);           // 12
                        packet.AddState(0xa5d, 0x1);           // 13 //2653
                        packet.AddState(0xa5c, 0x0);           // 14 //2652 east beacon neutral
                        packet.AddState(0xa5b, 0x1);           // 15 horde
                        packet.AddState(0xa5a, 0x0);           // 16 ally
                        packet.AddState(0xa59, 0x1);           // 17 // 2649 Twin spire graveyard horde  12???
                        packet.AddState(0xa58, 0x0);           // 18 ally     14 ???
                        packet.AddState(0xa57, 0x0);           // 19 neutral  7???
                        packet.AddState(0xa56, 0x0);           // 20 // 2646 west beacon neutral
                        packet.AddState(0xa55, 0x1);           // 21 horde
                        packet.AddState(0xa54, 0x0);           // 22 ally
                        packet.AddState(0x9e7, 0x0);           // 23 // 2535
                        packet.AddState(0x9e6, 0x0);           // 24
                        packet.AddState(0x9e5, 0x0);           // 25
                        packet.AddState(0xa00, 0x0);           // 26 // 2560
                        packet.AddState(0x9ff, 0x1);           // 27
                        packet.AddState(0x9fe, 0x0);           // 28
                        packet.AddState(0x9fd, 0x0);           // 29
                        packet.AddState(0x9fc, 0x1);           // 30
                        packet.AddState(0x9fb, 0x0);           // 31
                        packet.AddState(0xa62, 0x0);           // 32 // 2658
                        packet.AddState(0xa61, 0x1);           // 33
                        packet.AddState(0xa60, 0x1);           // 34
                        packet.AddState(0xa5f, 0x0);           // 35
                    }
                    break;
                case 3698:                                          // Nagrand Arena
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.NA)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0xa0f, 0x0);           // 7
                        packet.AddState(0xa10, 0x0);           // 8
                        packet.AddState(0xa11, 0x0);           // 9 show
                    }
                    break;
                case 3702:                                          // Blade's Edge Arena
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.BE)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0x9f0, 0x0);           // 7 gold
                        packet.AddState(0x9f1, 0x0);           // 8 green
                        packet.AddState(0x9f3, 0x0);           // 9 show
                    }
                    break;
                case 3968:                                          // Ruins of Lordaeron
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.RL)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0xbb8, 0x0);           // 7 gold
                        packet.AddState(0xbb9, 0x0);           // 8 green
                        packet.AddState(0xbba, 0x0);           // 9 show
                    }
                    break;
                case 4378:                                          // Dalaran Sewers
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.DS)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(3601, 0x0);           // 7 gold
                        packet.AddState(3600, 0x0);           // 8 green
                        packet.AddState(3610, 0x0);           // 9 show
                    }
                    break;
                case 4384:                                          // Strand of the Ancients
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.SA)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        // 1-3 A defend, 4-6 H defend, 7-9 unk defend, 1 - ok, 2 - half destroyed, 3 - destroyed
                        packet.AddState(0xf09, 0x0);       // 7  3849 Gate of Temple
                        packet.AddState(0xe36, 0x0);       // 8  3638 Gate of Yellow Moon
                        packet.AddState(0xe27, 0x0);       // 9  3623 Gate of Green Emerald
                        packet.AddState(0xe24, 0x0);       // 10 3620 Gate of Blue Sapphire
                        packet.AddState(0xe21, 0x0);       // 11 3617 Gate of Red Sun
                        packet.AddState(0xe1e, 0x0);       // 12 3614 Gate of Purple Ametyst

                        packet.AddState(0xdf3, 0x0);       // 13 3571 bonus timer (1 - on, 0 - off)
                        packet.AddState(0xded, 0x0);       // 14 3565 Horde Attacker
                        packet.AddState(0xdec, 0x0);       // 15 3564 Alliance Attacker
                                                                                                      // End Round (timer), better explain this by example, eg. ends in 19:59 . A:BC
                        packet.AddState(0xde9, 0x0);       // 16 3561 C
                        packet.AddState(0xde8, 0x0);       // 17 3560 B
                        packet.AddState(0xde7, 0x0);       // 18 3559 A
                        packet.AddState(0xe35, 0x0);       // 19 3637 East g - Horde control
                        packet.AddState(0xe34, 0x0);       // 20 3636 West g - Horde control
                        packet.AddState(0xe33, 0x0);       // 21 3635 South g - Horde control
                        packet.AddState(0xe32, 0x0);       // 22 3634 East g - Alliance control
                        packet.AddState(0xe31, 0x0);       // 23 3633 West g - Alliance control
                        packet.AddState(0xe30, 0x0);       // 24 3632 South g - Alliance control
                        packet.AddState(0xe2f, 0x0);       // 25 3631 Chamber of Ancients - Horde control
                        packet.AddState(0xe2e, 0x0);       // 26 3630 Chamber of Ancients - Alliance control
                        packet.AddState(0xe2d, 0x0);       // 27 3629 Beach1 - Horde control
                        packet.AddState(0xe2c, 0x0);       // 28 3628 Beach2 - Horde control
                        packet.AddState(0xe2b, 0x0);       // 29 3627 Beach1 - Alliance control
                        packet.AddState(0xe2a, 0x0);       // 30 3626 Beach2 - Alliance control
                                                                                                      // and many unks...
                    }
                    break;
                case 4406:                                          // Ring of Valor
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.RV)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0xe10, 0x0);           // 7 gold
                        packet.AddState(0xe11, 0x0);           // 8 green
                        packet.AddState(0xe1a, 0x0);           // 9 show
                    }
                    break;
                case 4710:
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.IC)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(4221, 1); // 7 BG_IC_ALLIANCE_RENFORT_SET
                        packet.AddState(4222, 1); // 8 BG_IC_HORDE_RENFORT_SET
                        packet.AddState(4226, 300); // 9 BG_IC_ALLIANCE_RENFORT
                        packet.AddState(4227, 300); // 10 BG_IC_HORDE_RENFORT
                        packet.AddState(4322, 1); // 11 BG_IC_GATE_FRONT_H_WS_OPEN
                        packet.AddState(4321, 1); // 12 BG_IC_GATE_WEST_H_WS_OPEN
                        packet.AddState(4320, 1); // 13 BG_IC_GATE_EAST_H_WS_OPEN
                        packet.AddState(4323, 1); // 14 BG_IC_GATE_FRONT_A_WS_OPEN
                        packet.AddState(4324, 1); // 15 BG_IC_GATE_WEST_A_WS_OPEN
                        packet.AddState(4325, 1); // 16 BG_IC_GATE_EAST_A_WS_OPEN
                        packet.AddState(4317, 1); // 17 unknown

                        packet.AddState(4301, 1); // 18 BG_IC_DOCKS_UNCONTROLLED
                        packet.AddState(4296, 1); // 19 BG_IC_HANGAR_UNCONTROLLED
                        packet.AddState(4306, 1); // 20 BG_IC_QUARRY_UNCONTROLLED
                        packet.AddState(4311, 1); // 21 BG_IC_REFINERY_UNCONTROLLED
                        packet.AddState(4294, 1); // 22 BG_IC_WORKSHOP_UNCONTROLLED
                        packet.AddState(4243, 1); // 23 unknown
                        packet.AddState(4345, 1); // 24 unknown
                    }
                    break;
                // The Ruby Sanctum
                case 4987:
                    if (instance != null && mapid == 724)
                        instance.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(5049, 50);             // 9  WORLDSTATE_CORPOREALITY_MATERIAL
                        packet.AddState(5050, 50);             // 10 WORLDSTATE_CORPOREALITY_TWILIGHT
                        packet.AddState(5051, 0);              // 11 WORLDSTATE_CORPOREALITY_TOGGLE
                    }
                    break;
                // Icecrown Citadel
                case 4812:
                    if (instance != null && mapid == 631)
                        instance.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(4903, 0);              // 9  WORLDSTATE_SHOW_TIMER (Blood Quickening weekly)
                        packet.AddState(4904, 30);             // 10 WORLDSTATE_EXECUTION_TIME
                        packet.AddState(4940, 0);              // 11 WORLDSTATE_SHOW_ATTEMPTS
                        packet.AddState(4941, 50);             // 12 WORLDSTATE_ATTEMPTS_REMAINING
                        packet.AddState(4942, 50);             // 13 WORLDSTATE_ATTEMPTS_MAX
                    }
                    break;
                // The Culling of Stratholme
                case 4100:
                    if (instance != null && mapid == 595)
                        instance.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(3479, 0);              // 9  WORLDSTATE_SHOW_CRATES
                        packet.AddState(3480, 0);              // 10 WORLDSTATE_CRATES_REVEALED
                        packet.AddState(3504, 0);              // 11 WORLDSTATE_WAVE_COUNT
                        packet.AddState(3931, 25);             // 12 WORLDSTATE_TIME_GUARDIAN
                        packet.AddState(3932, 0);              // 13 WORLDSTATE_TIME_GUARDIAN_SHOW
                    }
                    break;
                // The Oculus
                case 4228:
                    if (instance != null && mapid == 578)
                        instance.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(3524, 0);              // 9  WORLD_STATE_CENTRIFUGE_CONSTRUCT_SHOW
                        packet.AddState(3486, 0);              // 10 WORLD_STATE_CENTRIFUGE_CONSTRUCT_AMOUNT
                    }
                    break;
                // Ulduar
                case 4273:
                    if (instance != null && mapid == 603)
                        instance.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(4132, 0);              // 9  WORLDSTATE_ALGALON_TIMER_ENABLED
                        packet.AddState(4131, 0);              // 10 WORLDSTATE_ALGALON_DESPAWN_TIMER
                    }
                    break;
                // Halls of Refection
                case 4820:
                    if (instance != null && mapid == 668)
                        instance.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(4884, 0);              // 9  WORLD_STATE_HOR_WAVES_ENABLED
                        packet.AddState(4882, 0);              // 10 WORLD_STATE_HOR_WAVE_COUNT
                    }
                    break;
                // Zul Aman
                case 3805:
                    if (instance != null && mapid == 568)
                        instance.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(3104, 0);              // 9  WORLD_STATE_ZULAMAN_TIMER_ENABLED
                        packet.AddState(3106, 0);              // 10 WORLD_STATE_ZULAMAN_TIMER
                    }
                    break;
                // Twin Peaks
                case 5031:
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.TP)
                        bg.FillInitialWorldStates(packet);
                    else
                    {
                        packet.AddState(0x62d, 0x0);       //  7 1581 alliance flag captures
                        packet.AddState(0x62e, 0x0);       //  8 1582 horde flag captures
                        packet.AddState(0x609, 0x0);       //  9 1545 unk
                        packet.AddState(0x60a, 0x0);       // 10 1546 unk
                        packet.AddState(0x60b, 0x2);       // 11 1547 unk
                        packet.AddState(0x641, 0x3);       // 12 1601 unk
                        packet.AddState(0x922, 0x1);       // 13 2338 horde (0 - hide, 1 - flag ok, 2 - flag picked up (flashing), 3 - flag picked up (not flashing)
                        packet.AddState(0x923, 0x1);       // 14 2339 alliance (0 - hide, 1 - flag ok, 2 - flag picked up (flashing), 3 - flag picked up (not flashing)
                    }
                    break;
                // Battle for Gilneas
                case 5449:
                    if (bg && bg.GetTypeID(true) == BattlegroundTypeId.BFG)
                        bg.FillInitialWorldStates(packet);
                    break;
                // Wintergrasp
                case 4197:
                    if (bf != null && bf.GetTypeId() == (uint)BattleFieldTypes.WinterGrasp)
                        bf.FillInitialWorldStates(packet);
                    goto default;
                // No break here, intended.
                default:
                    packet.AddState(0x914, 0x0);           // 7
                    packet.AddState(0x913, 0x0);           // 8
                    packet.AddState(0x912, 0x0);           // 9
                    packet.AddState(0x915, 0x0);           // 10
                    break;
            }

            SendPacket(packet);
            SendBGWeekendWorldStates();
            SendBattlefieldWorldStates();
        }

        public uint GetBarberShopCost(BarberShopStyleRecord newHairStyle, uint newHairColor, BarberShopStyleRecord newFacialHair, BarberShopStyleRecord newSkin, BarberShopStyleRecord newFace, Array<BarberShopStyleRecord> newCustomDisplay)
        {
            byte hairstyle = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairStyleId);
            byte haircolor = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId);
            byte facialhair = GetByteValue(PlayerFields.Bytes2, PlayerFieldOffsets.Bytes2OffsetFacialStyle);
            byte skincolor = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId);
            byte face = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId);

            Array<byte> customDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
            for (int i = 0; i < PlayerConst.CustomDisplaySize; ++i)
                customDisplay[i] = GetByteValue(PlayerFields.Bytes2, (byte)(PlayerFieldOffsets.Bytes2OffsetCustomDisplayOption + i));

            if ((hairstyle == newHairStyle.Data) &&
                (haircolor == newHairColor) &&
                (facialhair == newFacialHair.Data) &&
                (newSkin == null || (newSkin.Data == skincolor)) &&
                (newFace == null || (newFace.Data == face)) &&
                (newCustomDisplay[0] == null || (newCustomDisplay[0].Data == customDisplay[0])) &&
                (newCustomDisplay[1] == null || (newCustomDisplay[1].Data == customDisplay[1])) &&
                (newCustomDisplay[2] == null || (newCustomDisplay[2].Data == customDisplay[2])))
                return 0;

            GtBarberShopCostBaseRecord bsc = CliDB.BarberShopCostBaseGameTable.GetRow(getLevel());
            if (bsc == null)                                                // shouldn't happen
                return 0xFFFFFFFF;

            uint cost = 0;
            if (hairstyle != newHairStyle.Data)
                cost += (uint)(bsc.Cost * newHairStyle.CostModifier);

            if ((haircolor != newHairColor) && (hairstyle == newHairStyle.Data))
                cost += (uint)(bsc.Cost * 0.5f);                    // +1/2 of price

            if (facialhair != newFacialHair.Data)
                cost += (uint)(bsc.Cost * newFacialHair.CostModifier);

            if (newSkin != null && skincolor != newSkin.Data)
                cost += (uint)(bsc.Cost * newSkin.CostModifier);

            if (newFace != null && face != newFace.Data)
                cost += (uint)(bsc.Cost * newFace.CostModifier);

            for (int i = 0; i < PlayerConst.CustomDisplaySize; ++i)
                if (newCustomDisplay[i] != null && customDisplay[i] != newCustomDisplay[i].Data)
                    cost += (uint)(bsc.Cost * newCustomDisplay[i].CostModifier);

            return cost;
        }

        uint GetChampioningFaction() { return m_ChampioningFaction; }
        public void SetChampioningFaction(uint faction) { m_ChampioningFaction = faction; }
        public void setFactionForRace(Race race)
        {
            m_team = TeamForRace(race);

            ChrRacesRecord rEntry = CliDB.ChrRacesStorage.LookupByKey(race);
            SetFaction(rEntry != null ? (uint)rEntry.FactionID : 0);
        }

        public void SetResurrectRequestData(Unit caster, uint health, uint mana, uint appliedAura)
        {
            Cypher.Assert(!IsResurrectRequested());
            _resurrectionData = new ResurrectionData();
            _resurrectionData.GUID = caster.GetGUID();
            _resurrectionData.Location.WorldRelocate(caster);
            _resurrectionData.Health = health;
            _resurrectionData.Mana = mana;
            _resurrectionData.Aura = appliedAura;
        }
        public void ClearResurrectRequestData()
        {
            _resurrectionData = null;
        }

        public bool IsRessurectRequestedBy(ObjectGuid guid)
        {
            if (!IsResurrectRequested())
                return false;

            return !_resurrectionData.GUID.IsEmpty() && _resurrectionData.GUID == guid;
        }

        public bool IsResurrectRequested() { return _resurrectionData != null; }
        public void ResurrectUsingRequestData()
        {
            // Teleport before resurrecting by player, otherwise the player might get attacked from creatures near his corpse
            TeleportTo(_resurrectionData.Location);

            if (IsBeingTeleported())
            {
                ScheduleDelayedOperation(PlayerDelayedOperations.ResurrectPlayer);
                return;
            }

            ResurrectUsingRequestDataImpl();
        }

        void ResurrectUsingRequestDataImpl()
        {
            ResurrectPlayer(0.0f, false);

            SetHealth(_resurrectionData.Health);
            SetPower(PowerType.Mana, (int)_resurrectionData.Mana);

            SetPower(PowerType.Rage, 0);
            SetFullPower(PowerType.Energy);
            SetFullPower(PowerType.Focus);
            SetPower(PowerType.LunarPower, 0);

            uint aura = _resurrectionData.Aura;
            if (aura != 0)
                CastSpell(this, aura, true, null, null, _resurrectionData.GUID);

            SpawnCorpseBones();
        }

        public void UpdateTriggerVisibility()
        {
            if (m_clientGUIDs.Empty())
                return;

            if (!IsInWorld)
                return;

            UpdateData udata = new UpdateData(GetMapId());
            foreach (var guid in m_clientGUIDs)
            {
                if (guid.IsCreatureOrVehicle())
                {
                    Creature creature = GetMap().GetCreature(guid);
                    // Update fields of triggers, transformed units or unselectable units (values dependent on GM state)
                    if (creature == null || (!creature.IsTrigger() && !creature.HasAuraType(AuraType.Transform) && !creature.HasFlag(UnitFields.Flags, UnitFlags.NotSelectable)))
                        continue;

                    creature.SetFieldNotifyFlag(UpdateFieldFlags.Public);
                    creature.BuildValuesUpdateBlockForPlayer(udata, this);
                    creature.RemoveFieldNotifyFlag(UpdateFieldFlags.Public);
                }
                else if (guid.IsAnyTypeGameObject())
                {
                    GameObject go = GetMap().GetGameObject(guid);
                    if (go == null)
                        continue;

                    go.SetFieldNotifyFlag(UpdateFieldFlags.Public);
                    go.BuildValuesUpdateBlockForPlayer(udata, this);
                    go.RemoveFieldNotifyFlag(UpdateFieldFlags.Public);
                }
            }

            if (!udata.HasData())
                return;

            UpdateObject packet;
            udata.BuildPacket(out packet);
            SendPacket(packet);
        }

        public bool isAllowedToLoot(Creature creature)
        {
            if (!creature.IsDead() || !creature.IsDamageEnoughForLootingAndReward())
                return false;

            if (HasPendingBind())
                return false;

            Loot loot = creature.loot;
            if (loot.isLooted()) // nothing to loot or everything looted.
                return false;
            if (!loot.hasItemForAll() && !loot.hasItemFor(this)) // no loot in creature for this player
                return false;

            if (loot.loot_type == LootType.Skinning)
                return creature.GetSkinner() == GetGUID();

            Group thisGroup = GetGroup();
            if (!thisGroup)
                return this == creature.GetLootRecipient();
            else if (thisGroup != creature.GetLootRecipientGroup())
                return false;

            switch (thisGroup.GetLootMethod())
            {
                case LootMethod.PersonalLoot:// @todo implement personal loot (http://wow.gamepedia.com/Loot#Personal_Loot)
                    return false;
                case LootMethod.MasterLoot:
                case LootMethod.FreeForAll:
                    return true;
                case LootMethod.GroupLoot:
                    // may only loot if the player is the loot roundrobin player
                    // or item over threshold (so roll(s) can be launched)
                    // or if there are free/quest/conditional item for the player
                    if (loot.roundRobinPlayer.IsEmpty() || loot.roundRobinPlayer == GetGUID())
                        return true;

                    if (loot.hasOverThresholdItem())
                        return true;

                    return loot.hasItemFor(this);
            }

            return false;
        }

        public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, uint index, Unit caster)
        {
            SpellEffectInfo effect = spellInfo.GetEffect(GetMap().GetDifficultyID(), index);
            if (effect == null || !effect.IsEffect())
                return false;

            // players are immune to taunt (the aura and the spell effect).
            if (effect.IsAura(AuraType.ModTaunt))
                return true;
            if (effect.IsEffect(SpellEffectName.AttackMe))
                return true;

            return base.IsImmunedToSpellEffect(spellInfo, index, caster);
        }

        void RegenerateAll()
        {
            m_regenTimerCount += m_regenTimer;

            for (PowerType power = PowerType.Mana; power < PowerType.Max; power++)// = power + 1)
                if (power != PowerType.Runes)
                    Regenerate(power);

            // Runes act as cooldowns, and they don't need to send any data
            if (GetClass() == Class.Deathknight)
            {
                uint regeneratedRunes = 0;
                int regenIndex = 0;
                while (regeneratedRunes < PlayerConst.MaxRechargingRunes && m_runes.CooldownOrder.Count > regenIndex)
                {
                    byte runeToRegen = m_runes.CooldownOrder[regenIndex];
                    uint runeCooldown = GetRuneCooldown(runeToRegen);
                    if (runeCooldown > m_regenTimer)
                    {
                        SetRuneCooldown(runeToRegen, runeCooldown - m_regenTimer);
                        ++regenIndex;
                    }
                    else
                        SetRuneCooldown(runeToRegen, 0);

                    ++regeneratedRunes;
                }
            }

            if (m_regenTimerCount >= 2000)
            {
                // Not in combat or they have regeneration
                if (!IsInCombat() || IsPolymorphed() || m_baseHealthRegen != 0 || HasAuraType(AuraType.ModRegenDuringCombat) || HasAuraType(AuraType.ModHealthRegenInCombat))
                    RegenerateHealth();

                m_regenTimerCount -= 2000;
            }

            m_regenTimer = 0;
        }
        void Regenerate(PowerType power)
        {
            // Skip regeneration for power type we cannot have
            uint powerIndex = GetPowerIndex(power);
            if (powerIndex == (int)PowerType.Max || powerIndex >= (int)PowerType.MaxPerClass)
                return;

            // @todo possible use of miscvalueb instead of amount
            if (HasAuraTypeWithValue(AuraType.PreventRegeneratePower, (int)power))
                return;

            int curValue = GetPower(power);

            // TODO: updating haste should update UNIT_FIELD_POWER_REGEN_FLAT_MODIFIER for certain power types
            PowerTypeRecord powerType = Global.DB2Mgr.GetPowerTypeEntry(power);
            if (powerType == null)
                return;

            float addvalue = 0.0f;

            if (!IsInCombat())
            {
                if (powerType.RegenInterruptTimeMS != 0 && Time.GetMSTimeDiffToNow(m_combatExitTime) < powerType.RegenInterruptTimeMS)
                    return;

                addvalue = (powerType.RegenPeace + GetFloatValue(UnitFields.PowerRegenFlatModifier + (int)powerIndex)) * 0.001f * m_regenTimer;
            }
            else
                addvalue = (powerType.RegenCombat + GetFloatValue(UnitFields.PowerRegenInterruptedFlatModifier + (int)powerIndex)) * 0.001f * m_regenTimer;

            WorldCfg[] RatesForPower =
            {
                WorldCfg.RatePowerMana,
                WorldCfg.RatePowerRageLoss,
                WorldCfg.RatePowerFocus,
                WorldCfg.RatePowerEnergy,
                WorldCfg.RatePowerComboPointsLoss,
                0, // runes
                WorldCfg.RatePowerRunicPowerLoss,
                WorldCfg.RatePowerSoulShards,
                WorldCfg.RatePowerLunarPower,
                WorldCfg.RatePowerHolyPower,
                0, // alternate
                WorldCfg.RatePowerMaelstrom,
                WorldCfg.RatePowerChi,
                WorldCfg.RatePowerInsanity,
                0, // burning embers, unused
                0, // demonic fury, unused
                WorldCfg.RatePowerArcaneCharges,
                WorldCfg.RatePowerFury,
                WorldCfg.RatePowerPain,
            };

            if (RatesForPower[(int)power] != 0)
                addvalue *= WorldConfig.GetFloatValue(RatesForPower[(int)power]);

            // Mana regen calculated in Player.UpdateManaRegen()
            if (power != PowerType.Mana)
            {
                addvalue *= GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerRegenPercent, (int)power);
                addvalue += GetTotalAuraModifierByMiscValue(AuraType.ModPowerRegen, (int)power) * ((power != PowerType.Energy) ? m_regenTimerCount : m_regenTimer) / (5 * Time.InMilliseconds);
            }

            int minPower = powerType.MinPower;
            int maxPower = GetMaxPower(power);

            if (addvalue < 0.0f)
            {
                if (curValue <= minPower)
                    return;
            }
            else if (addvalue > 0.0f)
            {
                if (curValue >= maxPower)
                    return;
            }
            else
                return;

            addvalue += m_powerFraction[powerIndex];
            int integerValue = (int)Math.Abs(addvalue);

            if (powerType.CenterPower != 0)
            {
                if (curValue > powerType.CenterPower)
                {
                    addvalue = -Math.Abs(addvalue);
                    minPower = powerType.CenterPower;
                }
                else if (curValue < powerType.CenterPower)
                {
                    addvalue = Math.Abs(addvalue);
                    maxPower = powerType.CenterPower;
                }
                else
                    return;
            }

            if (addvalue < 0.0f)
            {
                if (curValue > minPower + integerValue)
                {
                    curValue -= integerValue;
                    m_powerFraction[powerIndex] = addvalue + integerValue;
                }
                else
                {
                    curValue = minPower;
                    m_powerFraction[powerIndex] = 0;
                }
            }
            else
            {
                if (curValue + integerValue <= maxPower)
                {
                    curValue += integerValue;
                    m_powerFraction[powerIndex] = addvalue - integerValue;
                }
                else
                {
                    curValue = maxPower;
                    m_powerFraction[powerIndex] = 0;
                }
            }

            if (m_regenTimerCount >= 2000)
                SetPower(power, curValue);
            else
                UpdateUInt32Value(UnitFields.Power + (int)powerIndex, (uint)curValue);
        }
        void RegenerateHealth()
        {
            uint curValue = (uint)GetHealth();
            uint maxValue = (uint)GetMaxHealth();

            if (curValue >= maxValue)
                return;

            float HealthIncreaseRate = WorldConfig.GetFloatValue(WorldCfg.RateHealth);
            float addValue = 0.0f;

            // polymorphed case
            if (IsPolymorphed())
                addValue = (float)GetMaxHealth() / 3;
            // normal regen case (maybe partly in combat case)
            else if (!IsInCombat() || HasAuraType(AuraType.ModRegenDuringCombat))
            {
                addValue = HealthIncreaseRate;
                if (!IsInCombat())
                {
                    if (getLevel() < 15)
                        addValue = (0.20f * (GetMaxHealth()) / getLevel() * HealthIncreaseRate);
                    else
                        addValue = 0.015f * (GetMaxHealth()) * HealthIncreaseRate;

                    addValue *= GetTotalAuraMultiplier(AuraType.ModHealthRegenPercent);
                    addValue += GetTotalAuraModifier(AuraType.ModRegen) * 2 * Time.InMilliseconds / (5 * Time.InMilliseconds);
                }
                else if (HasAuraType(AuraType.ModRegenDuringCombat))
                    MathFunctions.ApplyPct(ref addValue, GetTotalAuraModifier(AuraType.ModRegenDuringCombat));

                if (!IsStandState())
                    addValue *= 1.5f;
            }

            // always regeneration bonus (including combat)
            addValue += GetTotalAuraModifier(AuraType.ModHealthRegenInCombat);
            addValue += m_baseHealthRegen / 2.5f;

            if (addValue < 0)
                addValue = 0;

            ModifyHealth((int)addValue);
        }
        public void ResetAllPowers()
        {
            SetFullHealth();
            switch (GetPowerType())
            {
                case PowerType.Mana:
                    SetFullPower(PowerType.Mana);
                    break;
                case PowerType.Rage:
                    SetPower(PowerType.Rage, 0);
                    break;
                case PowerType.Energy:
                    SetFullPower(PowerType.Energy);
                    break;
                case PowerType.RunicPower:
                    SetPower(PowerType.RunicPower, 0);
                    break;
                case PowerType.LunarPower:
                    SetPower(PowerType.LunarPower, 0);
                    break;
                default:
                    break;
            }
        }

        public Unit GetSelectedUnit()
        {
            ObjectGuid selectionGUID = GetTarget();
            if (!selectionGUID.IsEmpty())
                return Global.ObjAccessor.GetUnit(this, selectionGUID);
            return null;
        }
        Player GetSelectedPlayer()
        {
            ObjectGuid selectionGUID = GetTarget();
            if (!selectionGUID.IsEmpty())
                return Global.ObjAccessor.GetPlayer(this, selectionGUID);
            return null;
        }

        static uint GetSelectionFromContext(uint context, Class playerClass)
        {
            switch (context)
            {
                case 1:
                    if (playerClass == Class.Deathknight)
                        return 1;
                    if (playerClass == Class.DemonHunter)
                        return 3;
                    return 0;
                case 2:
                    if (playerClass == Class.Deathknight)
                        return 5;
                    if (playerClass == Class.DemonHunter)
                        return 6;
                    return 4;
                case 3:
                    return 7;
                case 4:
                    if (playerClass == Class.Deathknight)
                        return 9;
                    if (playerClass == Class.DemonHunter)
                        return 10;
                    return 8;
                default:
                    if (playerClass == Class.Deathknight)
                        return 1;
                    if (playerClass == Class.DemonHunter)
                        return 2;
                    return 0;
            }
        }

        static bool ComponentFlagsMatch(CharSectionsRecord entry, uint selection)
        {
            switch (selection)
            {
                case 0:
                    if (!entry.Flags.HasAnyFlag((short)1))
                        return false;
                    return !entry.Flags.HasAnyFlag((short)0x2C);
                case 1:
                    if (!entry.Flags.HasAnyFlag((short)1))
                        return false;
                    if (!entry.Flags.HasAnyFlag((short)0x94))
                        return false;
                    return !entry.Flags.HasAnyFlag((short)8);
                case 2:
                    if (!entry.Flags.HasAnyFlag((short)1))
                        return false;
                    if (!entry.Flags.HasAnyFlag((short)0x70))
                        return false;
                    return !entry.Flags.HasAnyFlag((short)8);
                case 3:
                    if (!entry.Flags.HasAnyFlag((short)1))
                        return false;
                    if (!entry.Flags.HasAnyFlag((short)0x20))
                        return false;
                    return !entry.Flags.HasAnyFlag((short)8);
                case 4:
                case 8:
                    if (!entry.Flags.HasAnyFlag((short)3))
                        return false;
                    return !entry.Flags.HasAnyFlag((short)0x2C);
                case 5:
                case 9:
                    if (!entry.Flags.HasAnyFlag((short)3))
                        return false;
                    if (!entry.Flags.HasAnyFlag((short)0x94))
                        return false;
                    return !entry.Flags.HasAnyFlag((short)8);
                case 6:
                case 10:
                    if (!entry.Flags.HasAnyFlag((short)3))
                        return false;
                    if (!entry.Flags.HasAnyFlag((short)0x70))
                        return false;
                    return !entry.Flags.HasAnyFlag((short)8);
                case 7:
                    return true;
                default:
                    break;
            }

            return false;
        }

        static bool IsSectionFlagValid(CharSectionsRecord entry, Class class_, bool create)
        {
            if (create)
                return ComponentFlagsMatch(entry, GetSelectionFromContext(0, class_));

            return ComponentFlagsMatch(entry, GetSelectionFromContext(2, class_));
        }
        static bool IsSectionValid(Race race, Class class_, Gender gender, CharBaseSectionVariation variation, byte variationIndex, byte colorIndex, bool create)
        {
            CharSectionsRecord section = Global.DB2Mgr.GetCharSectionEntry(race, gender, variation, variationIndex, colorIndex);
            if (section != null)
                return IsSectionFlagValid(section, class_, create);

            return false;
        }
        public static bool IsValidGender(Gender _gender) { return _gender <= Gender.Female; }
        public static bool IsValidClass(Class _class) { return Convert.ToBoolean((1 << ((int)_class - 1)) & (int)Class.ClassMaskAllPlayable); }
        public static bool IsValidRace(Race _race) { return Convert.ToBoolean((1 << ((int)_race - 1)) & (int)Race.RaceMaskAllPlayable); }

        public static bool ValidateAppearance(Race race, Class class_, Gender gender, byte hairID, byte hairColor, byte faceID, byte facialHairId, byte skinColor, Array<byte> customDisplay, bool create = false)
        {
            if (!IsSectionValid(race, class_, gender, CharBaseSectionVariation.Skin, 0, skinColor, create))
                return false;

            if (!IsSectionValid(race, class_, gender, CharBaseSectionVariation.Face, faceID, skinColor, create))
                return false;

            if (!IsSectionValid(race, class_, gender, CharBaseSectionVariation.Hair, hairID, hairColor, create))
                return false;

            if (!IsSectionValid(race, class_, gender, CharBaseSectionVariation.FacialHair, facialHairId, hairColor, create))
                if (Global.DB2Mgr.HasCharSections(race, gender, CharBaseSectionVariation.FacialHair) || !Global.DB2Mgr.HasCharacterFacialHairStyle(race, gender, facialHairId))
                    return false;

            if (!IsSectionValid(race, class_, gender, CharBaseSectionVariation.CustomDisplay1, customDisplay[0], 0, create))
                return false;

            if (!IsSectionValid(race, class_, gender, CharBaseSectionVariation.CustomDisplay2, customDisplay[1], 0, create))
                return false;

            if (!IsSectionValid(race, class_, gender, CharBaseSectionVariation.CustomDisplay3, customDisplay[2], 0, create))
                return false;

            return true;
        }

        public void OnCombatExit()
        {
            UpdatePotionCooldown();
            m_combatExitTime = Time.GetMSTime();
        }

        void LeaveLFGChannel()
        {
            foreach (var i in m_channels)
            {
                if (i.IsLFG())
                {
                    i.LeaveChannel(this);
                    break;
                }
            }
        }

        bool IsImmuneToEnvironmentalDamage()
        {
            // check for GM and death state included in isAttackableByAOE
            return (!isTargetableForAttack(false));
        }
        public uint EnvironmentalDamage(EnviromentalDamage type, uint damage)
        {
            if (IsImmuneToEnvironmentalDamage())
                return 0;

            // Absorb, resist some environmental damage type
            uint absorb = 0;
            uint resist = 0;
            switch (type)
            {
                case EnviromentalDamage.Lava:
                case EnviromentalDamage.Slime:
                    DamageInfo dmgInfo = new DamageInfo(this, this, damage, null, type == EnviromentalDamage.Lava ? SpellSchoolMask.Fire : SpellSchoolMask.Nature, DamageEffectType.Direct, WeaponAttackType.BaseAttack);
                    CalcAbsorbResist(dmgInfo);
                    absorb = dmgInfo.GetAbsorb();
                    resist = dmgInfo.GetResist();
                    damage = dmgInfo.GetDamage();
                    break;
            }

            DealDamageMods(this, ref damage, ref absorb);

            EnvironmentalDamageLog packet = new EnvironmentalDamageLog();
            packet.Victim = GetGUID();
            packet.Type = type != EnviromentalDamage.FallToVoid ? type : EnviromentalDamage.Fall;
            packet.Amount = (int)damage;
            packet.Absorbed = (int)absorb;
            packet.Resisted = (int)resist;

            uint final_damage = DealDamage(this, damage, null, DamageEffectType.Self, SpellSchoolMask.Normal, null, false);
            packet.LogData.Initialize(this);

            SendCombatLogMessage(packet);

            if (!IsAlive())
            {
                if (type == EnviromentalDamage.Fall)                               // DealDamage not apply item durability loss at self damage
                {
                    Log.outDebug(LogFilter.Player, "We are fall to death, loosing 10 percents durability");
                    DurabilityLossAll(0.10f, false);
                    // durability lost message
                    SendDurabilityLoss(this, 10);
                }

                UpdateCriteria(CriteriaTypes.DeathsFrom, 1, (ulong)type);
            }

            return final_damage;
        }

        bool isTotalImmune()
        {
            var immune = GetAuraEffectsByType(AuraType.SchoolImmunity);

            int immuneMask = 0;
            foreach (var eff in immune)
            {
                immuneMask |= eff.GetMiscValue();
                if (Convert.ToBoolean(immuneMask & (int)SpellSchoolMask.All))            // total immunity
                    return true;
            }
            return false;
        }

        public override bool CanAlwaysSee(WorldObject obj)
        {
            // Always can see self
            if (m_unitMovedByMe == obj)
                return true;

            ObjectGuid guid = GetGuidValue(ActivePlayerFields.Farsight);
            if (!guid.IsEmpty())
                if (obj.GetGUID() == guid)
                    return true;

            return false;
        }
        public override bool IsNeverVisibleFor(WorldObject seer)
        {
            if (base.IsNeverVisibleFor(seer))
                return true;

            if (GetSession().PlayerLogout() || GetSession().PlayerLoading())
                return true;

            return false;
        }

        public void BuildPlayerRepop()
        {
            PreRessurect packet = new PreRessurect();
            packet.PlayerGUID = GetGUID();
            SendPacket(packet);

            if (GetRace() == Race.NightElf)
                CastSpell(this, 20584, true);
            CastSpell(this, 8326, true);

            // there must be SMSG.FORCE_RUN_SPEED_CHANGE, SMSG.FORCE_SWIM_SPEED_CHANGE, SMSG.MOVE_WATER_WALK
            // there must be SMSG.STOP_MIRROR_TIMER

            // the player cannot have a corpse already on current map, only bones which are not returned by GetCorpse
            WorldLocation corpseLocation = GetCorpseLocation();
            if (corpseLocation.GetMapId() == GetMapId())
            {
                Log.outError(LogFilter.Player, "BuildPlayerRepop: player {0} ({1}) already has a corpse", GetName(), GetGUID().ToString());
                return;
            }

            // create a corpse and place it at the player's location
            Corpse corpse = CreateCorpse();
            if (corpse == null)
            {
                Log.outError(LogFilter.Player, "Error creating corpse for Player {0} ({1})", GetName(), GetGUID().ToString());
                return;
            }
            GetMap().AddToMap(corpse);

            // convert player body to ghost
            SetHealth(1);

            SetWaterWalking(true);
            if (!GetSession().isLogingOut() && !HasUnitState(UnitState.Stunned))
                SetRooted(false);

            // BG - remove insignia related
            RemoveFlag(UnitFields.Flags, UnitFlags.Skinnable);

            int corpseReclaimDelay = CalculateCorpseReclaimDelay();

            if (corpseReclaimDelay >= 0)
                SendCorpseReclaimDelay(corpseReclaimDelay);

            // to prevent cheating
            corpse.ResetGhostTime();

            StopMirrorTimers();                                     //disable timers(bars)

            // set and clear other
            SetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.AnimTier, (byte)UnitBytes1Flags.AlwaysStand);
        }

        public void StopMirrorTimers()
        {
            StopMirrorTimer(MirrorTimerType.Fatigue);
            StopMirrorTimer(MirrorTimerType.Breath);
            StopMirrorTimer(MirrorTimerType.Fire);
        }

        public bool IsMirrorTimerActive(MirrorTimerType type)
        {
            return m_MirrorTimer[(int)type] == getMaxTimer(type);
        }

        void HandleDrowning(uint time_diff)
        {
            if (m_MirrorTimerFlags == 0)
                return;

            int breathTimer = (int)MirrorTimerType.Breath;
            int fatigueTimer = (int)MirrorTimerType.Fatigue;
            int fireTimer = (int)MirrorTimerType.Fire;

            // In water
            if (m_MirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InWater))
            {
                // Breath timer not activated - activate it
                if (m_MirrorTimer[breathTimer] == -1)
                {
                    m_MirrorTimer[breathTimer] = getMaxTimer(MirrorTimerType.Breath);
                    SendMirrorTimer(MirrorTimerType.Breath, m_MirrorTimer[breathTimer], m_MirrorTimer[breathTimer], -1);
                }
                else                                                              // If activated - do tick
                {
                    m_MirrorTimer[breathTimer] -= (int)time_diff;
                    // Timer limit - need deal damage
                    if (m_MirrorTimer[breathTimer] < 0)
                    {
                        m_MirrorTimer[breathTimer] += 1 * Time.InMilliseconds;
                        // Calculate and deal damage
                        // @todo Check this formula
                        uint damage = (uint)(GetMaxHealth() / 5 + RandomHelper.URand(0, getLevel() - 1));
                        EnvironmentalDamage(EnviromentalDamage.Drowning, damage);
                    }
                    else if (!m_MirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InWater))      // Update time in client if need
                        SendMirrorTimer(MirrorTimerType.Breath, getMaxTimer(MirrorTimerType.Breath), m_MirrorTimer[breathTimer], -1);
                }
            }
            else if (m_MirrorTimer[breathTimer] != -1)        // Regen timer
            {
                int UnderWaterTime = getMaxTimer(MirrorTimerType.Breath);
                // Need breath regen
                m_MirrorTimer[breathTimer] += (int)(10 * time_diff);
                if (m_MirrorTimer[breathTimer] >= UnderWaterTime || !IsAlive())
                    StopMirrorTimer(MirrorTimerType.Breath);
                else if (m_MirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InWater))
                    SendMirrorTimer(MirrorTimerType.Breath, UnderWaterTime, m_MirrorTimer[breathTimer], 10);
            }

            // In dark water
            if (m_MirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InDarkWater))
            {
                // Fatigue timer not activated - activate it
                if (m_MirrorTimer[fatigueTimer] == -1)
                {
                    m_MirrorTimer[fatigueTimer] = getMaxTimer(MirrorTimerType.Fatigue);
                    SendMirrorTimer(MirrorTimerType.Fatigue, m_MirrorTimer[fatigueTimer], m_MirrorTimer[fatigueTimer], -1);
                }
                else
                {
                    m_MirrorTimer[fatigueTimer] -= (int)time_diff;
                    // Timer limit - need deal damage or teleport ghost to graveyard
                    if (m_MirrorTimer[fatigueTimer] < 0)
                    {
                        m_MirrorTimer[fatigueTimer] += 1 * Time.InMilliseconds;
                        if (IsAlive())                                            // Calculate and deal damage
                        {
                            uint damage = (uint)(GetMaxHealth() / 5 + RandomHelper.URand(0, getLevel() - 1));
                            EnvironmentalDamage(EnviromentalDamage.Exhausted, damage);
                        }
                        else if (HasFlag(PlayerFields.Flags, PlayerFlags.Ghost))       // Teleport ghost to graveyard
                            RepopAtGraveyard();
                    }
                    else if (!m_MirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InDarkWater))
                        SendMirrorTimer(MirrorTimerType.Fatigue, getMaxTimer(MirrorTimerType.Fatigue), m_MirrorTimer[fatigueTimer], -1);
                }
            }
            else if (m_MirrorTimer[fatigueTimer] != -1)       // Regen timer
            {
                int DarkWaterTime = getMaxTimer(MirrorTimerType.Fatigue);
                m_MirrorTimer[fatigueTimer] += (int)(10 * time_diff);
                if (m_MirrorTimer[fatigueTimer] >= DarkWaterTime || !IsAlive())
                    StopMirrorTimer(MirrorTimerType.Fatigue);
                else if (m_MirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InDarkWater))
                    SendMirrorTimer(MirrorTimerType.Fatigue, DarkWaterTime, m_MirrorTimer[fatigueTimer], 10);
            }

            if (m_MirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InLava) && !(_lastLiquid != null && _lastLiquid.SpellID != 0))
            {
                // Breath timer not activated - activate it
                if (m_MirrorTimer[fireTimer] == -1)
                    m_MirrorTimer[fireTimer] = getMaxTimer(MirrorTimerType.Fire);
                else
                {
                    m_MirrorTimer[fireTimer] -= (int)time_diff;
                    if (m_MirrorTimer[fireTimer] < 0)
                    {
                        m_MirrorTimer[fireTimer] += 1 * Time.InMilliseconds;
                        // Calculate and deal damage
                        // @todo Check this formula
                        uint damage = RandomHelper.URand(600, 700);
                        if (m_MirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InLava))
                            EnvironmentalDamage(EnviromentalDamage.Lava, damage);
                        // need to skip Slime damage in Undercity,
                        // maybe someone can find better way to handle environmental damage
                        //else if (m_zoneUpdateId != 1497)
                        //    EnvironmentalDamage(DAMAGE_SLIME, damage);
                    }
                }
            }
            else
                m_MirrorTimer[fireTimer] = -1;

            // Recheck timers flag
            m_MirrorTimerFlags &= ~PlayerUnderwaterState.ExistTimers;
            for (byte i = 0; i < (int)MirrorTimerType.Max; ++i)
            {
                if (m_MirrorTimer[i] != -1)
                {
                    m_MirrorTimerFlags |= PlayerUnderwaterState.ExistTimers;
                    break;
                }
            }
            m_MirrorTimerFlagsLast = m_MirrorTimerFlags;
        }

        void HandleSobering()
        {
            m_drunkTimer = 0;

            byte currentDrunkValue = GetDrunkValue();
            byte drunk = (byte)(currentDrunkValue != 0 ? --currentDrunkValue : 0);
            SetDrunkValue(drunk);
        }

        void SendMirrorTimer(MirrorTimerType Type, int MaxValue, int CurrentValue, int Regen)
        {
            if (MaxValue == -1)
            {
                if (CurrentValue != -1)
                    StopMirrorTimer(Type);
                return;
            }

            SendPacket(new StartMirrorTimer(Type, CurrentValue, MaxValue, Regen, 0, false));
        }

        void StopMirrorTimer(MirrorTimerType Type)
        {
            m_MirrorTimer[(int)Type] = -1;
            SendPacket(new StopMirrorTimer(Type));
        }

        int getMaxTimer(MirrorTimerType timer)
        {
            switch (timer)
            {
                case MirrorTimerType.Fatigue:
                    return Time.Minute * Time.InMilliseconds;
                case MirrorTimerType.Breath:
                    {
                        if (!IsAlive() || HasAuraType(AuraType.WaterBreathing) || GetSession().GetSecurity() >= (AccountTypes)WorldConfig.GetIntValue(WorldCfg.DisableBreathing))
                            return -1;
                        int UnderWaterTime = 3 * Time.Minute * Time.InMilliseconds;
                        UnderWaterTime *= (int)GetTotalAuraMultiplier(AuraType.ModWaterBreathing);
                        return UnderWaterTime;
                    }
                case MirrorTimerType.Fire:
                    {
                        if (!IsAlive())
                            return -1;
                        return 1 * Time.InMilliseconds;
                    }
                default:
                    return 0;
            }
        }

        public void UpdateMirrorTimers()
        {
            // Desync flags for update on next HandleDrowning
            if (m_MirrorTimerFlags != 0)
                m_MirrorTimerFlagsLast = ~m_MirrorTimerFlags;
        }

        public void ResurrectPlayer(float restore_percent, bool applySickness = false)
        {
            DeathReleaseLoc packet = new DeathReleaseLoc();
            packet.MapID = -1;
            SendPacket(packet);

            // speed change, land walk

            // remove death flag + set aura
            SetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.AnimTier, 0);
            RemoveFlag(PlayerFields.Flags, PlayerFlags.IsOutOfBounds);

            // This must be called always even on Players with race != RACE_NIGHTELF in case of faction change
            RemoveAurasDueToSpell(20584);                       // speed bonuses
            RemoveAurasDueToSpell(8326);                            // SPELL_AURA_GHOST

            if (GetSession().IsARecruiter() || (GetSession().GetRecruiterId() != 0))
                SetFlag(ObjectFields.DynamicFlags, UnitDynFlags.ReferAFriend);

            setDeathState(DeathState.Alive);

            // add the flag to make sure opcode is always sent
            AddUnitMovementFlag(MovementFlag.WaterWalk);
            SetWaterWalking(false);
            if (!HasUnitState(UnitState.Stunned))
                SetRooted(false);

            m_deathTimer = 0;

            // set health/powers (0- will be set in caller)
            if (restore_percent > 0.0f)
            {
                SetHealth((ulong)(GetMaxHealth() * restore_percent));
                SetPower(PowerType.Mana, (int)(GetMaxPower(PowerType.Mana) * restore_percent));
                SetPower(PowerType.Rage, 0);
                SetPower(PowerType.Energy, (int)(GetMaxPower(PowerType.Energy) * restore_percent));
                SetPower(PowerType.Focus, (int)(GetMaxPower(PowerType.Focus) * restore_percent));
                SetPower(PowerType.LunarPower, 0);
            }

            // trigger update zone for alive state zone updates
            uint newzone, newarea;
            GetZoneAndAreaId(out newzone, out newarea);
            UpdateZone(newzone, newarea);
            Global.OutdoorPvPMgr.HandlePlayerResurrects(this, newzone);

            if (InBattleground())
            {
                Battleground bg = GetBattleground();
                if (bg)
                    bg.HandlePlayerResurrect(this);
            }

            // update visibility
            UpdateObjectVisibility();

            // recast lost by death auras of any items held in the inventory
            CastAllObtainSpells();

            if (!applySickness)
                return;

            //Characters from level 1-10 are not affected by resurrection sickness.
            //Characters from level 11-19 will suffer from one minute of sickness
            //for each level they are above 10.
            //Characters level 20 and up suffer from ten minutes of sickness.
            int startLevel = WorldConfig.GetIntValue(WorldCfg.DeathSicknessLevel);

            if (getLevel() >= startLevel)
            {
                // set resurrection sickness
                CastSpell(this, 15007, true);

                // not full duration
                if (getLevel() < startLevel + 9)
                {
                    int delta = (int)(getLevel() - startLevel + 1) * Time.Minute;
                    Aura aur = GetAura(15007, GetGUID());
                    if (aur != null)
                        aur.SetDuration(delta * Time.InMilliseconds);

                }
            }
        }

        public void KillPlayer()
        {
            if (IsFlying() && GetTransport() == null)
                GetMotionMaster().MoveFall();

            SetRooted(true);

            StopMirrorTimers();                                     //disable timers(bars)

            setDeathState(DeathState.Corpse);

            SetUInt32Value(ObjectFields.DynamicFlags, 0);
            ApplyModFlag(ActivePlayerFields.LocalFlags, PlayerLocalFlags.ReleaseTimer, !CliDB.MapStorage.LookupByKey(GetMapId()).Instanceable() && !HasAuraType(AuraType.PreventResurrection));

            // 6 minutes until repop at graveyard
            m_deathTimer = 6 * Time.Minute * Time.InMilliseconds;

            UpdateCorpseReclaimDelay();                             // dependent at use SetDeathPvP() call before kill

            int corpseReclaimDelay = CalculateCorpseReclaimDelay();

            if (corpseReclaimDelay >= 0)
                SendCorpseReclaimDelay(corpseReclaimDelay);

            // don't create corpse at this moment, player might be falling

            // update visibility
            UpdateObjectVisibility();
        }

        public static void OfflineResurrect(ObjectGuid guid, SQLTransaction trans)
        {
            Corpse.DeleteFromDB(guid, trans);
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
            stmt.AddValue(0, (ushort)AtLoginFlags.Resurrect);
            stmt.AddValue(1, guid.GetCounter());
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        Corpse CreateCorpse()
        {
            // prevent existence 2 corpse for player
            SpawnCorpseBones();

            Corpse corpse = new Corpse(Convert.ToBoolean(m_ExtraFlags & PlayerExtraFlags.PVPDeath) ? CorpseType.ResurrectablePVP : CorpseType.ResurrectablePVE);
            SetPvPDeath(false);

            if (!corpse.Create(GetMap().GenerateLowGuid(HighGuid.Corpse), this))
                return null;

            _corpseLocation = new WorldLocation(this);

            byte skin = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId);
            byte face = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId);
            byte hairstyle = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairStyleId);
            byte haircolor = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId);
            byte facialhair = GetByteValue(PlayerFields.Bytes2, PlayerFieldOffsets.Bytes2OffsetFacialStyle);

            uint _cfb1 = (uint)((0x00) | ((int)GetRace() << 8) | (GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender) << 16) | (skin << 24));
            uint _cfb2 = (uint)((face) | (hairstyle << 8) | (haircolor << 16) | (facialhair << 24));

            corpse.SetUInt32Value(CorpseFields.Bytes1, _cfb1);
            corpse.SetUInt32Value(CorpseFields.Bytes2, _cfb2);

            CorpseFlags flags = 0;
            if (HasByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.PvP))
                flags |= CorpseFlags.PvP;
            if (InBattleground() && !InArena())
                flags |= CorpseFlags.Skinnable;                      // to be able to remove insignia
            if (HasByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.FFAPvp))
                flags |= CorpseFlags.FFAPvP;

            corpse.SetUInt32Value(CorpseFields.Flags, (uint)flags);
            corpse.SetUInt32Value(CorpseFields.DisplayId, GetNativeDisplayId());
            corpse.SetUInt32Value(CorpseFields.FactionTemplate, CliDB.ChrRacesStorage.LookupByKey(GetRace()).FactionID);

            for (byte i = 0; i < EquipmentSlot.End; i++)
            {
                if (m_items[i] != null)
                {
                    uint itemDisplayId = m_items[i].GetDisplayId(this);
                    uint itemInventoryType;
                    ItemRecord itemEntry = CliDB.ItemStorage.LookupByKey(m_items[i].GetVisibleEntry(this));
                    if (itemEntry != null)
                        itemInventoryType = (uint)itemEntry.inventoryType;
                    else
                        itemInventoryType = (uint)m_items[i].GetTemplate().GetInventoryType();

                    corpse.SetUInt32Value(CorpseFields.Item + i, itemDisplayId | (itemInventoryType << 24));
                }
            }

            // register for player, but not show
            GetMap().AddCorpse(corpse);

            // we do not need to save corpses for BG/arenas
            if (!GetMap().IsBattlegroundOrArena())
                corpse.SaveToDB();

            return corpse;
        }

        public void SpawnCorpseBones(bool triggerSave = true)
        {
            _corpseLocation = new WorldLocation();
            if (GetMap().ConvertCorpseToBones(GetGUID()))
                if (triggerSave && !GetSession().PlayerLogoutWithSave())   // at logout we will already store the player
                    SaveToDB();                                             // prevent loading as ghost without corpse
        }

        public Corpse GetCorpse() { return GetMap().GetCorpseByPlayer(GetGUID()); }

        public void RepopAtGraveyard()
        {
            // note: this can be called also when the player is alive
            // for example from WorldSession.HandleMovementOpcodes

            AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(GetAreaId());

            // Such zones are considered unreachable as a ghost and the player must be automatically revived
            if ((!IsAlive() && zone != null && zone.Flags[0].HasAnyFlag(AreaFlags.NeedFly)) || GetTransport() != null || GetPositionZ() < GetMap().GetMinHeight(GetPositionX(), GetPositionY()))
            {
                ResurrectPlayer(0.5f);
                SpawnCorpseBones();
            }

            WorldSafeLocsRecord ClosestGrave = null;

            // Special handle for Battlegroundmaps
            Battleground bg = GetBattleground();
            if (bg)
                ClosestGrave = bg.GetClosestGraveYard(this);
            else
            {
                BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(GetZoneId());
                if (bf != null)
                    ClosestGrave = bf.GetClosestGraveYard(this);
                else
                    ClosestGrave = Global.ObjectMgr.GetClosestGraveYard(this, GetTeam(), this);
            }

            // stop countdown until repop
            m_deathTimer = 0;

            // if no grave found, stay at the current location
            // and don't show spirit healer location
            if (ClosestGrave != null)
            {
                TeleportTo(ClosestGrave.MapID, ClosestGrave.Loc.X, ClosestGrave.Loc.Y, ClosestGrave.Loc.Z, (ClosestGrave.Facing * MathFunctions.PI) / 180);
                if (IsDead())                                        // not send if alive, because it used in TeleportTo()
                {
                    DeathReleaseLoc packet = new DeathReleaseLoc();
                    packet.MapID = (int)ClosestGrave.MapID;
                    packet.Loc = ClosestGrave.Loc;
                    SendPacket(packet);
                }
            }
            else if (GetPositionZ() < GetMap().GetMinHeight(GetPositionX(), GetPositionY()))
                TeleportTo(homebind);

            RemoveFlag(PlayerFields.Flags, PlayerFlags.IsOutOfBounds);
        }

        public bool HasCorpse()
        {
            return _corpseLocation.GetMapId() != 0xFFFFFFFF;
        }
        public WorldLocation GetCorpseLocation() { return _corpseLocation; }

        public uint GetCorpseReclaimDelay(bool pvp)
        {
            if (pvp)
            {
                if (!WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPvp))
                    return PlayerConst.copseReclaimDelay[0];
            }
            else if (!WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPve))
                return 0;

            long now = Time.UnixTime;
            // 0..2 full period
            // should be ceil(x)-1 but not floor(x)
            ulong count = (ulong)((now < m_deathExpireTime - 1) ? (m_deathExpireTime - 1 - now) / PlayerConst.DeathExpireStep : 0);
            return PlayerConst.copseReclaimDelay[count];
        }
        void UpdateCorpseReclaimDelay()
        {
            bool pvp = m_ExtraFlags.HasAnyFlag(PlayerExtraFlags.PVPDeath);

            if ((pvp && !WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPvp)) ||
                (!pvp && !WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPve)))
                return;
            long now = Time.UnixTime;
            if (now < m_deathExpireTime)
            {
                // full and partly periods 1..3
                ulong count = (ulong)(m_deathExpireTime - now) / PlayerConst.DeathExpireStep + 1;
                if (count < PlayerConst.MaxDeathCount)
                    m_deathExpireTime = now + (long)(count + 1) * PlayerConst.DeathExpireStep;
                else
                    m_deathExpireTime = now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep;
            }
            else
                m_deathExpireTime = now + PlayerConst.DeathExpireStep;
        }
        int CalculateCorpseReclaimDelay(bool load = false)
        {
            Corpse corpse = GetCorpse();
            if (load && !corpse)
                return -1;

            bool pvp = corpse ? corpse.GetCorpseType() == CorpseType.ResurrectablePVP : (m_ExtraFlags & PlayerExtraFlags.PVPDeath) != 0;

            uint delay;
            if (load)
            {
                if (corpse.GetGhostTime() > m_deathExpireTime)
                    return -1;

                ulong count = 0;
                if ((pvp && WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPvp)) ||
                   (!pvp && WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPve)))
                {
                    count = (ulong)(m_deathExpireTime - corpse.GetGhostTime()) / PlayerConst.DeathExpireStep;

                    if (count >= PlayerConst.MaxDeathCount)
                        count = PlayerConst.MaxDeathCount - 1;
                }

                long expected_time = corpse.GetGhostTime() + PlayerConst.copseReclaimDelay[count];
                long now = Time.UnixTime;

                if (now >= expected_time)
                    return -1;

                delay = (uint)(expected_time - now);
            }
            else
                delay = GetCorpseReclaimDelay(pvp);

            return (int)(delay * Time.InMilliseconds);
        }
        void SendCorpseReclaimDelay(int delay)
        {
            CorpseReclaimDelay packet = new CorpseReclaimDelay();
            packet.Remaining = (uint)delay;
            SendPacket(packet);
        }

        public override bool CanFly() { return m_movementInfo.HasMovementFlag(MovementFlag.CanFly); }

        public Pet GetPet()
        {
            ObjectGuid petGuid = GetPetGUID();
            if (!petGuid.IsEmpty())
            {
                if (!petGuid.IsPet())
                    return null;

                Pet pet = ObjectAccessor.GetPet(this, petGuid);
                if (pet == null)
                    return null;

                if (IsInWorld && pet != null)
                    return pet;
            }

            return null;
        }

        public Pet SummonPet(uint entry, float x, float y, float z, float ang, PetType petType, uint duration)
        {
            Pet pet = new Pet(this, petType);
            if (petType == PetType.Summon && pet.LoadPetFromDB(this, entry))
            {
                if (duration > 0)
                    pet.SetDuration(duration);

                return null;
            }

            // petentry == 0 for hunter "call pet" (current pet summoned if any)
            if (entry == 0)
                return null;

            pet.Relocate(x, y, z, ang);
            if (!pet.IsPositionValid())
            {
                Log.outError(LogFilter.Server, "Pet (guidlow {0}, entry {1}) not summoned. Suggested coordinates isn't valid (X: {2} Y: {3})",
                    pet.GetGUID().ToString(), pet.GetEntry(), pet.GetPositionX(), pet.GetPositionY());
                return null;
            }

            Map map = GetMap();
            uint pet_number = Global.ObjectMgr.GeneratePetNumber();
            if (!pet.Create(map.GenerateLowGuid(HighGuid.Pet), map, entry))
            {
                Log.outError(LogFilter.Server, "no such creature entry {0}", entry);
                return null;
            }

            PhasingHandler.InheritPhaseShift(pet, this);

            pet.SetCreatorGUID(GetGUID());
            pet.SetUInt32Value(UnitFields.FactionTemplate, getFaction());
            pet.SetUInt64Value(UnitFields.NpcFlags, (uint)NPCFlags.None);
            pet.SetUInt32Value(UnitFields.Bytes1, 0);
            pet.InitStatsForLevel(getLevel());

            SetMinion(pet, true);

            switch (petType)
            {
                case PetType.Summon:
                    // this enables pet details window (Shift+P)
                    pet.GetCharmInfo().SetPetNumber(pet_number, true);
                    pet.SetByteValue(UnitFields.Bytes0, 1, (byte)Class.Mage);
                    pet.SetUInt32Value(UnitFields.PetExperience, 0);
                    pet.SetUInt32Value(UnitFields.PetNextLevelExp, 1000);
                    pet.SetFullHealth();
                    pet.SetFullPower(PowerType.Mana);
                    pet.SetUInt32Value(UnitFields.PetNameTimestamp, (uint)Time.UnixTime); // cast can't be helped in this case
                    break;
                default:
                    break;
            }

            map.AddToMap(pet.ToCreature());

            switch (petType)
            {
                case PetType.Summon:
                    pet.InitPetCreateSpells();
                    pet.SavePetToDB(PetSaveMode.AsCurrent);
                    PetSpellInitialize();
                    break;
                default:
                    break;
            }

            if (duration > 0)
                pet.SetDuration(duration);

            //ObjectAccessor.UpdateObjectVisibility(pet);

            return pet;
        }

        public void RemovePet(Pet pet, PetSaveMode mode, bool returnreagent = false)
        {
            if (!pet)
                pet = GetPet();

            if (pet)
            {
                Log.outDebug(LogFilter.Pet, "RemovePet {0}, {1}, {2}", pet.GetEntry(), mode, returnreagent);

                if (pet.m_removed)
                    return;
            }

            if (returnreagent && (pet || m_temporaryUnsummonedPetNumber != 0) && !InBattleground())
            {
                //returning of reagents only for players, so best done here
                uint spellId = pet ? pet.GetUInt32Value(UnitFields.CreatedBySpell) : m_oldpetspell;
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);

                if (spellInfo != null)
                {
                    for (uint i = 0; i < SpellConst.MaxReagents; ++i)
                    {
                        if (spellInfo.Reagent[i] > 0)
                        {
                            List<ItemPosCount> dest = new List<ItemPosCount>();       //for succubus, voidwalker, felhunter and felguard credit soulshard when despawn reason other than death (out of range, logout)
                            InventoryResult msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, (uint)spellInfo.Reagent[i], spellInfo.ReagentCount[i]);
                            if (msg == InventoryResult.Ok)
                            {
                                Item item = StoreNewItem(dest, (uint)spellInfo.Reagent[i], true);
                                if (IsInWorld)
                                    SendNewItem(item, spellInfo.ReagentCount[i], true, false);
                            }
                        }
                    }
                }
                m_temporaryUnsummonedPetNumber = 0;
            }

            if (!pet || pet.GetOwnerGUID() != GetGUID())
                return;

            pet.CombatStop();

            if (returnreagent)
            {
                switch (pet.GetEntry())
                {
                    //warlock pets except imp are removed(?) when logging out
                    case 1860:
                    case 1863:
                    case 417:
                    case 17252:
                        mode = PetSaveMode.NotInSlot;
                        break;
                }
            }

            // only if current pet in slot
            pet.SavePetToDB(mode);

            SetMinion(pet, false);

            pet.AddObjectToRemoveList();
            pet.m_removed = true;

            if (pet.isControlled())
            {
                SendPacket(new PetSpells());

                if (GetGroup())
                    SetGroupUpdateFlag(GroupUpdateFlags.Pet);
            }
        }

        public bool InArena()
        {
            Battleground bg = GetBattleground();
            if (!bg || !bg.isArena())
                return false;

            return true;
        }

        public void SendOnCancelExpectedVehicleRideAura()
        {
            SendPacket(new OnCancelExpectedRideVehicleAura());
        }

        public void SendMovementSetCollisionHeight(float height)
        {
            MoveSetCollisionHeight setCollisionHeight = new MoveSetCollisionHeight();
            setCollisionHeight.MoverGUID = GetGUID();
            setCollisionHeight.SequenceIndex = m_movementCounter++;
            setCollisionHeight.Height = height;
            setCollisionHeight.Scale = GetObjectScale();
            setCollisionHeight.MountDisplayID = GetUInt32Value(UnitFields.MountDisplayId);
            setCollisionHeight.ScaleDuration = (int)GetUInt32Value(UnitFields.ScaleDuration);
            setCollisionHeight.Reason = UpdateCollisionHeightReason.Mount;
            SendPacket(setCollisionHeight);

            MoveUpdateCollisionHeight updateCollisionHeight = new MoveUpdateCollisionHeight();
            updateCollisionHeight.Status = m_movementInfo;
            updateCollisionHeight.Height = height;
            updateCollisionHeight.Scale = GetObjectScale();
            SendMessageToSet(updateCollisionHeight, false);
        }

        public void SendPlayerChoice(ObjectGuid sender, int choiceId)
        {
            PlayerChoice playerChoice = Global.ObjectMgr.GetPlayerChoice(choiceId);
            if (playerChoice == null)
                return;

            LocaleConstant locale = GetSession().GetSessionDbLocaleIndex();
            PlayerChoiceLocale playerChoiceLocale = locale != LocaleConstant.enUS ? Global.ObjectMgr.GetPlayerChoiceLocale(choiceId) : null;

            PlayerTalkClass.GetInteractionData().Reset();
            PlayerTalkClass.GetInteractionData().SourceGuid = sender;
            PlayerTalkClass.GetInteractionData().PlayerChoiceId = (uint)choiceId;

            DisplayPlayerChoice displayPlayerChoice = new DisplayPlayerChoice();
            displayPlayerChoice.SenderGUID = sender;
            displayPlayerChoice.ChoiceID = choiceId;
            displayPlayerChoice.UiTextureKitID = playerChoice.UiTextureKitId;
            displayPlayerChoice.Question = playerChoice.Question;
            if (playerChoiceLocale != null)
                ObjectManager.GetLocaleString(playerChoiceLocale.Question, locale, ref displayPlayerChoice.Question);

            displayPlayerChoice.CloseChoiceFrame = false;
            displayPlayerChoice.HideWarboardHeader = playerChoice.HideWarboardHeader;
            displayPlayerChoice.KeepOpenAfterChoice = playerChoice.KeepOpenAfterChoice;

            for (var i = 0; i < playerChoice.Responses.Count; ++i)
            {
                PlayerChoiceResponse playerChoiceResponseTemplate = playerChoice.Responses[i];
                var playerChoiceResponse = new Network.Packets.PlayerChoiceResponse();

                playerChoiceResponse.ResponseID = playerChoiceResponseTemplate.ResponseId;
                playerChoiceResponse.ChoiceArtFileID = playerChoiceResponseTemplate.ChoiceArtFileId;
                playerChoiceResponse.Flags = playerChoiceResponseTemplate.Flags;
                playerChoiceResponse.WidgetSetID = playerChoiceResponseTemplate.WidgetSetID;
                playerChoiceResponse.GroupID = playerChoiceResponseTemplate.GroupID;
                playerChoiceResponse.Answer = playerChoiceResponseTemplate.Answer;
                playerChoiceResponse.Header = playerChoiceResponseTemplate.Header;
                playerChoiceResponse.Description = playerChoiceResponseTemplate.Description;
                playerChoiceResponse.Confirmation = playerChoiceResponseTemplate.Confirmation;
                if (playerChoiceLocale != null)
                {
                    PlayerChoiceResponseLocale playerChoiceResponseLocale = playerChoiceLocale.Responses.LookupByKey(playerChoiceResponseTemplate.ResponseId);
                    if (playerChoiceResponseLocale != null)
                    {
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.Answer, locale, ref playerChoiceResponse.Answer);
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.Header, locale, ref playerChoiceResponse.Header);
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.Description, locale, ref playerChoiceResponse.Description);
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.Confirmation, locale, ref playerChoiceResponse.Confirmation);
                    }
                }

                if (playerChoiceResponseTemplate.Reward.HasValue)
                {
                    var reward = new Network.Packets.PlayerChoiceResponseReward();
                    reward.TitleID = playerChoiceResponseTemplate.Reward.Value.TitleId;
                    reward.PackageID = playerChoiceResponseTemplate.Reward.Value.PackageId;
                    reward.SkillLineID = playerChoiceResponseTemplate.Reward.Value.SkillLineId;
                    reward.SkillPointCount = playerChoiceResponseTemplate.Reward.Value.SkillPointCount;
                    reward.ArenaPointCount = playerChoiceResponseTemplate.Reward.Value.ArenaPointCount;
                    reward.HonorPointCount = playerChoiceResponseTemplate.Reward.Value.HonorPointCount;
                    reward.Money = playerChoiceResponseTemplate.Reward.Value.Money;
                    reward.Xp = playerChoiceResponseTemplate.Reward.Value.Xp;

                    foreach (var item in playerChoiceResponseTemplate.Reward.Value.Items)
                    {
                        var rewardEntry = new Network.Packets.PlayerChoiceResponseRewardEntry();
                        rewardEntry.Item.ItemID = item.Id;
                        rewardEntry.Quantity = item.Quantity;
                        if (!item.BonusListIDs.Empty())
                        {
                            rewardEntry.Item.ItemBonus.HasValue = true;
                            rewardEntry.Item.ItemBonus.Value.BonusListIDs = item.BonusListIDs;
                        }
                        reward.Items.Add(rewardEntry);
                    }

                    foreach (var currency in playerChoiceResponseTemplate.Reward.Value.Currency)
                    {
                        var rewardEntry = new Network.Packets.PlayerChoiceResponseRewardEntry();
                        rewardEntry.Item.ItemID = currency.Id;
                        rewardEntry.Quantity = currency.Quantity;
                        reward.Items.Add(rewardEntry);
                    }

                    foreach (var faction in playerChoiceResponseTemplate.Reward.Value.Faction)
                    {
                        var rewardEntry = new Network.Packets.PlayerChoiceResponseRewardEntry();
                        rewardEntry.Item.ItemID = faction.Id;
                        rewardEntry.Quantity = faction.Quantity;
                        reward.Items.Add(rewardEntry);
                    }

                    playerChoiceResponse.Reward.Set(reward);
                    displayPlayerChoice.Responses[i] = playerChoiceResponse;
                }
            }

            SendPacket(displayPlayerChoice);
        }

        public bool MeetPlayerCondition(uint conditionId)
        {
            PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(conditionId);
            if (playerCondition != null)
                if (!ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
                    return false;

            return true;
        }

        public float GetCollisionHeight(bool mounted)
        {
            if (mounted)
            {
                var mountDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(GetUInt32Value(UnitFields.MountDisplayId));
                if (mountDisplayInfo == null)
                    return GetCollisionHeight(false);

                var mountModelData = CliDB.CreatureModelDataStorage.LookupByKey(mountDisplayInfo.ModelID);
                if (mountModelData == null)
                    return GetCollisionHeight(false);

                var displayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(GetNativeDisplayId());
                Cypher.Assert(displayInfo != null);
                var modelData = CliDB.CreatureModelDataStorage.LookupByKey(displayInfo.ModelID);
                Cypher.Assert(modelData != null);

                float scaleMod = GetObjectScale(); // 99% sure about this

                return scaleMod * mountModelData.MountHeight + modelData.CollisionHeight * 0.5f;
            }
            else
            {
                //! Dismounting case - use basic default model data
                var displayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(GetNativeDisplayId());
                Cypher.Assert(displayInfo != null);
                var modelData = CliDB.CreatureModelDataStorage.LookupByKey(displayInfo.ModelID);
                Cypher.Assert(modelData != null);

                return modelData.CollisionHeight;
            }
        }

        // Used in triggers for check "Only to targets that grant experience or honor" req
        public bool isHonorOrXPTarget(Unit victim)
        {
            uint v_level = victim.GetLevelForTarget(this);
            uint k_grey = Formulas.GetGrayLevel(getLevel());

            // Victim level less gray level
            if (v_level < k_grey)
                return false;

            Creature creature = victim.ToCreature();
            if (creature != null)
            {
                if (!creature.CanGiveExperience())
                    return false;
            }
            return true;
        }

        public void setRegenTimerCount(uint time) { m_regenTimerCount = time; }
        void setWeaponChangeTimer(uint time) { m_weaponChangeTimer = time; }

        //Team
        public static Team TeamForRace(Race race)
        {
            switch (TeamIdForRace(race))
            {
                case 0:
                    return Team.Alliance;
                case 1:
                    return Team.Horde;
            }

            return Team.Alliance;
        }
        public static uint TeamIdForRace(Race race)
        {
            ChrRacesRecord rEntry = CliDB.ChrRacesStorage.LookupByKey((byte)race);
            if (rEntry != null)
                return (uint)rEntry.Alliance;

            Log.outError(LogFilter.Player, "Race ({0}) not found in DBC: wrong DBC files?", race);
            return TeamId.Neutral;
        }
        public Team GetTeam() { return m_team; }
        public int GetTeamId() { return m_team == Team.Alliance ? TeamId.Alliance : TeamId.Horde; }

        //Money
        public ulong GetMoney() { return GetUInt64Value(ActivePlayerFields.Coinage); }
        public bool HasEnoughMoney(ulong amount) { return GetMoney() >= amount; }
        public bool HasEnoughMoney(long amount)
        {
            if (amount > 0)
                return (GetMoney() >= (ulong)amount);
            return true;
        }
        public bool ModifyMoney(long amount, bool sendError = true)
        {
            if (amount == 0)
                return true;

            Global.ScriptMgr.OnPlayerMoneyChanged(this, amount);

            if (amount < 0)
                SetMoney((ulong)(GetMoney() > (ulong)-amount ? (long)GetMoney() + amount : 0));
            else
            {
                if (GetMoney() <= (PlayerConst.MaxMoneyAmount - (ulong)amount))
                    SetMoney((ulong)(GetMoney() + (ulong)amount));
                else
                {
                    if (sendError)
                        SendEquipError(InventoryResult.TooMuchGold);
                    return false;
                }
            }
            return true;
        }
        public void SetMoney(ulong value)
        {
            SetUInt64Value(ActivePlayerFields.Coinage, value);
            MoneyChanged((uint)value);
            UpdateCriteria(CriteriaTypes.HighestGoldValueOwned);
        }

        //Target
        // Used for serverside target changes, does not apply to players
        public override void SetTarget(ObjectGuid guid) { }

        public void SetSelection(ObjectGuid guid)
        {
            SetGuidValue(UnitFields.Target, guid);
        }

        //LoginFlag
        public bool HasAtLoginFlag(AtLoginFlags f) { return Convert.ToBoolean(atLoginFlags & f); }
        public void SetAtLoginFlag(AtLoginFlags f) { atLoginFlags |= f; }
        public void RemoveAtLoginFlag(AtLoginFlags flags, bool persist = false)
        {
            atLoginFlags &= ~flags;
            if (persist)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_REM_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)flags);
                stmt.AddValue(1, GetGUID().GetCounter());

                DB.Characters.Execute(stmt);
            }
        }
        void SetFactionForRace(Race race)
        {
            m_team = TeamForRace(race);

            var rEntry = CliDB.ChrRacesStorage.LookupByKey(race);
            SetFaction(rEntry.FactionID);
        }

        //Guild
        public void SetInGuild(ulong guildId)
        {
            if (guildId != 0)
                SetGuidValue(UnitFields.GuildGuid, ObjectGuid.Create(HighGuid.Guild, guildId));
            else
                SetGuidValue(UnitFields.GuildGuid, ObjectGuid.Empty);

            ApplyModFlag(PlayerFields.Flags, PlayerFlags.GuildLevelEnabled, guildId != 0);
        }
        public void SetGuildRank(uint rankId) { SetUInt32Value(PlayerFields.GuildRank, rankId); }
        byte GetGuildRank() { return (byte)GetUInt32Value(PlayerFields.GuildRank); }
        public void SetGuildLevel(uint level) { SetUInt32Value(PlayerFields.GuildLevel, level); }
        uint GetGuildLevel() { return GetUInt32Value(PlayerFields.GuildLevel); }
        public void SetGuildIdInvited(ulong GuildId) { m_GuildIdInvited = GuildId; }
        public uint GetGuildId() { return GetUInt32Value(UnitFields.GuildGuid); }
        public Guild GetGuild()
        {
            uint guildId = GetGuildId();
            return guildId != 0 ? Global.GuildMgr.GetGuildById(guildId) : null;
        }
        public ulong GetGuildIdInvited() { return m_GuildIdInvited; }
        public string GetGuildName()
        {
            return GetGuildId() != 0 ? Global.GuildMgr.GetGuildById(GetGuildId()).GetName() : "";
        }

        public void SetFreePrimaryProfessions(uint profs) { SetUInt32Value(ActivePlayerFields.CharacterPoints, profs); }
        public void GiveLevel(uint level)
        {
            var oldLevel = getLevel();
            if (level == oldLevel)
                return;

            Guild guild = GetGuild();
            if (guild != null)
                guild.UpdateMemberData(this, GuildMemberData.Level, level);

            PlayerLevelInfo info = Global.ObjectMgr.GetPlayerLevelInfo(GetRace(), GetClass(), level);

            uint basemana = 0;
            Global.ObjectMgr.GetPlayerClassLevelInfo(GetClass(), level, out basemana);

            LevelUpInfo packet = new LevelUpInfo();
            packet.Level = level;
            packet.HealthDelta = 0;

            // @todo find some better solution
            packet.PowerDelta[0] = (int)basemana - (int)GetCreateMana();
            packet.PowerDelta[1] = 0;
            packet.PowerDelta[2] = 0;
            packet.PowerDelta[3] = 0;
            packet.PowerDelta[4] = 0;
            packet.PowerDelta[5] = 0;

            for (Stats i = Stats.Strength; i < Stats.Max; ++i)
                packet.StatDelta[(int)i] = info.stats[(int)i] - (int)GetCreateStat(i);

            packet.NumNewTalents = (int)(Global.DB2Mgr.GetNumTalentsAtLevel(level, GetClass()) - Global.DB2Mgr.GetNumTalentsAtLevel(oldLevel, GetClass()));
            packet.NumNewPvpTalentSlots = Global.DB2Mgr.GetPvpTalentNumSlotsAtLevel(level, GetClass()) - Global.DB2Mgr.GetPvpTalentNumSlotsAtLevel(oldLevel, GetClass());

            SendPacket(packet);

            SetUInt32Value(ActivePlayerFields.NextLevelXp, Global.ObjectMgr.GetXPForLevel(level));

            //update level, max level of skills
            m_PlayedTimeLevel = 0;                   // Level Played Time reset

            _ApplyAllLevelScaleItemMods(false);

            SetLevel(level);

            UpdateSkillsForLevel();
            LearnDefaultSkills();
            LearnSpecializationSpells();

            // save base values (bonuses already included in stored stats
            for (var i = Stats.Strength; i < Stats.Max; ++i)
                SetCreateStat(i, info.stats[(int)i]);

            SetCreateHealth(0);
            SetCreateMana(basemana);

            InitTalentForLevel();
            InitTaxiNodesForLevel();

            if (level < PlayerConst.LevelMinHonor)
                ResetPvpTalents();

            UpdateAllStats();

            if (WorldConfig.GetBoolValue(WorldCfg.AlwaysMaxskill)) // Max weapon skill when leveling up
                UpdateSkillsToMaxSkillsForLevel();

            _ApplyAllLevelScaleItemMods(true); // Moved to above SetFullHealth so player will have full health from Heirlooms

            Aura artifactAura = GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
            if (artifactAura != null)
            {
                Item artifact = GetItemByGuid(artifactAura.GetCastItemGUID());
                if (artifact != null)
                    artifact.CheckArtifactRelicSlotUnlock(this);
            }

            // Only health and mana are set to maximum.
            SetFullHealth();
            SetFullPower(PowerType.Mana);

            // update level to hunter/summon pet
            Pet pet = GetPet();
            if (pet)
                pet.SynchronizeLevelWithOwner();

            MailLevelReward mailReward = Global.ObjectMgr.GetMailLevelReward(level, (uint)getRaceMask());
            if (mailReward != null)
            {
                //- TODO: Poor design of mail system
                SQLTransaction trans = new SQLTransaction();
                new MailDraft(mailReward.mailTemplateId).SendMailTo(trans, this, new MailSender(MailMessageType.Creature, mailReward.senderEntry));
                DB.Characters.CommitTransaction(trans);
            }

            UpdateCriteria(CriteriaTypes.ReachLevel);

            // Refer-A-Friend
            if (GetSession().GetRecruiterId() != 0)
            {
                if (level < WorldConfig.GetIntValue(WorldCfg.MaxRecruitAFriendBonusPlayerLevel))
                {
                    if (level % 2 == 0)
                    {
                        ++m_grantableLevels;

                        if (!HasByteFlag(ActivePlayerFields.Bytes, PlayerFieldOffsets.FieldBytesOffsetRafGrantableLevel, 0x01))
                            SetByteFlag(ActivePlayerFields.Bytes, PlayerFieldOffsets.FieldBytesOffsetRafGrantableLevel, 0x01);
                    }
                }
            }

            Global.ScriptMgr.OnPlayerLevelChanged(this, (byte)oldLevel);
        }

        public bool CanParry()
        {
            return m_canParry;
        }
        public bool CanBlock()
        {
            return m_canBlock;
        }

        public void ToggleAFK()
        {
            ToggleFlag(PlayerFields.Flags, PlayerFlags.AFK);

            // afk player not allowed in Battleground
            if (!IsGameMaster() && isAFK() && InBattleground() && !InArena())
                LeaveBattleground();
        }
        public void ToggleDND()
        {
            ToggleFlag(PlayerFields.Flags, PlayerFlags.DND);
        }
        public bool isAFK() { return HasFlag(PlayerFields.Flags, PlayerFlags.AFK); }
        public bool isDND() { return HasFlag(PlayerFields.Flags, PlayerFlags.DND); }
        public ChatFlags GetChatFlags()
        {
            ChatFlags tag = ChatFlags.None;

            if (isGMChat())
                tag |= ChatFlags.GM;
            if (isDND())
                tag |= ChatFlags.DND;
            if (isAFK())
                tag |= ChatFlags.AFK;
            if (HasFlag(PlayerFields.Flags, PlayerFlags.Developer))
                tag |= ChatFlags.Dev;

            return tag;
        }

        public void InitDisplayIds()
        {
            PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());
            if (info == null)
            {
                Log.outError(LogFilter.Player, "Player {0} has incorrect race/class pair. Can't init display ids.", GetGUID().ToString());
                return;
            }

            byte gender = GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender);
            switch ((Gender)gender)
            {
                case Gender.Female:
                    SetDisplayId(info.DisplayId_f);
                    SetNativeDisplayId(info.DisplayId_f);
                    break;
                case Gender.Male:
                    SetDisplayId(info.DisplayId_m);
                    SetNativeDisplayId(info.DisplayId_m);
                    break;
                default:
                    Log.outError(LogFilter.Player, "Player {0} ({1}) has invalid gender {2}", GetName(), GetGUID().ToString(), gender);
                    return;
            }

            SetUInt32Value(UnitFields.StateAnimId, (uint)CliDB.AnimationDataStorage.Count);
        }

        //Creature
        public Creature GetNPCIfCanInteractWith(ObjectGuid guid, NPCFlags npcflagmask)
        {
            // unit checks
            if (guid.IsEmpty())
                return null;

            if (!IsInWorld)
                return null;

            if (IsInFlight())
                return null;

            // exist (we need look pets also for some interaction (quest/etc)
            Creature creature = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);
            if (creature == null)
                return null;

            // Deathstate checks
            if (!IsAlive() && !Convert.ToBoolean(creature.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.GhostVisible))
                return null;

            // alive or spirit healer
            if (!creature.IsAlive() && !Convert.ToBoolean(creature.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.CanInteractWhileDead))
                return null;

            // appropriate npc type
            if (npcflagmask != 0 && !creature.HasFlag64(UnitFields.NpcFlags, npcflagmask))
                return null;

            // not allow interaction under control, but allow with own pets
            if (!creature.GetCharmerGUID().IsEmpty())
                return null;

            // not unfriendly/hostile
            if (creature.GetReactionTo(this) <= ReputationRank.Unfriendly)
                return null;

            // not too far
            if (!creature.IsWithinDistInMap(this, SharedConst.InteractionDistance))
                return null;

            return creature;
        }

        public GameObject GetGameObjectIfCanInteractWith(ObjectGuid guid)
        {
            GameObject go = GetMap().GetGameObject(guid);
            if (go)
            {
                if (go.IsWithinDistInMap(this, go.GetInteractionDistance()))
                    return go;

                Log.outDebug(LogFilter.Maps, "Player.GetGameObjectIfCanInteractWith: GameObject '{0}' ({1}) is too far away from player '{2}' ({3}) to be used by him (Distance: {4}, maximal {5} is allowed)",
                    go.GetName(), go.GetGUID().ToString(), GetName(), GetGUID().ToString(), go.GetDistance(this), go.GetInteractionDistance());
            }

            return null;
        }
        public GameObject GetGameObjectIfCanInteractWith(ObjectGuid guid, GameObjectTypes type)
        {
            GameObject go = GetMap().GetGameObject(guid);
            if (go != null)
            {
                if (go.GetGoType() == type)
                {
                    if (go.IsWithinDistInMap(this, go.GetInteractionDistance()))
                        return go;

                    Log.outDebug(LogFilter.Maps, "Player.GetGameObjectIfCanInteractWith: GameObject '{0}' ({1}) is too far away from player '{2}' ({3}) to be used by him (Distance: {4}, maximal {5} is allowed)",
                        go.GetName(), go.GetGUID().ToString(), GetName(), GetGUID().ToString(), go.GetDistance(this), go.GetInteractionDistance());
                }
            }

            return null;
        }

        public void SendInitialPacketsBeforeAddToMap()
        {
            if (!m_teleport_options.HasAnyFlag(TeleportToOptions.Seamless))
            {
                m_movementCounter = 0;
                ResetTimeSync();
            }

            SendTimeSync();

            GetSocial().SendSocialList(this, SocialFlag.All);

            // SMSG_BINDPOINTUPDATE
            SendBindPointUpdate();

            // SMSG_SET_PROFICIENCY
            // SMSG_SET_PCT_SPELL_MODIFIER
            // SMSG_SET_FLAT_SPELL_MODIFIER

            // SMSG_TALENTS_INFO
            SendTalentsInfoData();

            // SMSG_INITIAL_SPELLS
            SendKnownSpells();

            // SMSG_SEND_UNLEARN_SPELLS
            SendPacket(new SendUnlearnSpells());

            // SMSG_SEND_SPELL_HISTORY
            SendSpellHistory sendSpellHistory = new SendSpellHistory();
            GetSpellHistory().WritePacket(sendSpellHistory);
            SendPacket(sendSpellHistory);

            // SMSG_SEND_SPELL_CHARGES
            SendSpellCharges sendSpellCharges = new SendSpellCharges();
            GetSpellHistory().WritePacket(sendSpellCharges);
            SendPacket(sendSpellCharges);

            ActiveGlyphs activeGlyphs = new ActiveGlyphs();
            foreach (uint glyphId in GetGlyphs(GetActiveTalentGroup()))
            {
                List<uint> bindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphId);
                foreach (uint bindableSpell in bindableSpells)
                    if (HasSpell(bindableSpell) && !m_overrideSpells.ContainsKey(bindableSpell))
                        activeGlyphs.Glyphs.Add(new GlyphBinding(bindableSpell, (ushort)glyphId));
            }

            activeGlyphs.IsFullUpdate = true;
            SendPacket(activeGlyphs);

            // SMSG_ACTION_BUTTONS
            SendInitialActionButtons();

            // SMSG_INITIALIZE_FACTIONS
            reputationMgr.SendInitialReputations();

            // SMSG_SETUP_CURRENCY
            SendCurrencies();

            // SMSG_EQUIPMENT_SET_LIST
            SendEquipmentSetList();

            m_achievementSys.SendAllData(this);
            m_questObjectiveCriteriaMgr.SendAllData(this);

            // SMSG_LOGIN_SETTIMESPEED
            float TimeSpeed = 0.01666667f;
            LoginSetTimeSpeed loginSetTimeSpeed = new LoginSetTimeSpeed();
            loginSetTimeSpeed.NewSpeed = TimeSpeed;
            loginSetTimeSpeed.GameTime = (uint)Global.WorldMgr.GetGameTime();
            loginSetTimeSpeed.ServerTime = (uint)Global.WorldMgr.GetGameTime();
            loginSetTimeSpeed.GameTimeHolidayOffset = 0; // @todo
            loginSetTimeSpeed.ServerTimeHolidayOffset = 0; // @todo
            SendPacket(loginSetTimeSpeed);

            // SMSG_WORLD_SERVER_INFO
            WorldServerInfo worldServerInfo = new WorldServerInfo();
            worldServerInfo.InstanceGroupSize.Set(GetMap().GetMapDifficulty().MaxPlayers);         // @todo
            worldServerInfo.IsTournamentRealm = 0;             // @todo
            worldServerInfo.RestrictedAccountMaxLevel.Clear(); // @todo
            worldServerInfo.RestrictedAccountMaxMoney.Clear(); // @todo
            worldServerInfo.DifficultyID = (uint)GetMap().GetDifficultyID();
            // worldServerInfo.XRealmPvpAlert;  // @todo
            SendPacket(worldServerInfo);

            // Spell modifiers
            SendSpellModifiers();

            // SMSG_ACCOUNT_MOUNT_UPDATE
            AccountMountUpdate mountUpdate = new AccountMountUpdate();
            mountUpdate.IsFullUpdate = true;
            mountUpdate.Mounts = GetSession().GetCollectionMgr().GetAccountMounts();
            SendPacket(mountUpdate);

            // SMSG_ACCOUNT_TOYS_UPDATE
            AccountToysUpdate toysUpdate = new AccountToysUpdate();
            toysUpdate.IsFullUpdate = true;
            toysUpdate.Toys = GetSession().GetCollectionMgr().GetAccountToys();
            SendPacket(toysUpdate);

            // SMSG_ACCOUNT_HEIRLOOM_UPDATE
            AccountHeirloomUpdate heirloomUpdate = new AccountHeirloomUpdate();
            heirloomUpdate.IsFullUpdate = true;
            heirloomUpdate.Heirlooms = GetSession().GetCollectionMgr().GetAccountHeirlooms();
            SendPacket(heirloomUpdate);

            GetSession().GetCollectionMgr().SendFavoriteAppearances();

            InitialSetup initialSetup = new InitialSetup();
            initialSetup.ServerExpansionLevel = (byte)WorldConfig.GetIntValue(WorldCfg.Expansion);
            SendPacket(initialSetup);

            SetMover(this);
        }

        public void SendInitialPacketsAfterAddToMap()
        {
            UpdateVisibilityForPlayer();

            // update zone
            uint newzone, newarea;
            GetZoneAndAreaId(out newzone, out newarea);
            UpdateZone(newzone, newarea);                            // also call SendInitWorldStates();

            GetSession().SendLoadCUFProfiles();

            CastSpell(this, 836, true);                             // LOGINEFFECT

            // set some aura effects that send packet to player client after add player to map
            // SendMessageToSet not send it to player not it map, only for aura that not changed anything at re-apply
            // same auras state lost at far teleport, send it one more time in this case also
            AuraType[] auratypes =
            {
                AuraType.ModFear, AuraType.Transform, AuraType.WaterWalk,
                AuraType.FeatherFall, AuraType.Hover, AuraType.SafeFall,
                AuraType.Fly, AuraType.ModIncreaseMountedFlightSpeed, AuraType.None
            };
            foreach (var aura in auratypes)
            {
                var auraList = GetAuraEffectsByType(aura);
                if (!auraList.Empty())
                    auraList.First().HandleEffect(this, AuraEffectHandleModes.SendForClient, true);
            }

            if (HasAuraType(AuraType.ModStun))
                SetRooted(true);

            MoveSetCompoundState setCompoundState = new MoveSetCompoundState();
            // manual send package (have code in HandleEffect(this, AURA_EFFECT_HANDLE_SEND_FOR_CLIENT, true); that must not be re-applied.
            if (HasAuraType(AuraType.ModRoot) || HasAuraType(AuraType.ModRoot2))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveRoot, m_movementCounter++));

            if (HasAuraType(AuraType.FeatherFall))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetFeatherFall, m_movementCounter++));

            if (HasAuraType(AuraType.WaterWalk))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetWaterWalk, m_movementCounter++));

            if (HasAuraType(AuraType.Hover))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetHovering, m_movementCounter++));

            if (HasAuraType(AuraType.CanTurnWhileFalling))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetCanTurnWhileFalling, m_movementCounter++));

            if (HasAura(196055)) //DH DoubleJump
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveEnableDoubleJump, m_movementCounter++));

            if (!setCompoundState.StateChanges.Empty())
            {
                setCompoundState.MoverGUID = GetGUID();
                SendPacket(setCompoundState);
            }

            SendAurasForTarget(this);
            SendEnchantmentDurations();                             // must be after add to map
            SendItemDurations();                                    // must be after add to map

            // raid downscaling - send difficulty to player
            if (GetMap().IsRaid())
            {
                m_prevMapDifficulty = GetMap().GetDifficultyID();
                DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(m_prevMapDifficulty);
                SendRaidDifficulty(difficulty.Flags.HasAnyFlag(DifficultyFlags.Legacy), (int)m_prevMapDifficulty);
            }
            else if (GetMap().IsNonRaidDungeon())
            {
                m_prevMapDifficulty = GetMap().GetDifficultyID();
                SendDungeonDifficulty((int)m_prevMapDifficulty);
            }
            else if (!GetMap().Instanceable())
            {
                DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(m_prevMapDifficulty);
                SendRaidDifficulty(difficulty.Flags.HasAnyFlag(DifficultyFlags.Legacy));
            }

            PhasingHandler.OnMapChange(this);

            if (_garrison != null)
                _garrison.SendRemoteInfo();

            UpdateItemLevelAreaBasedScaling();
        }

        public bool CanSpeak()
        {
            return GetSession().m_muteTime <= Time.UnixTime;
        }

        public void RemoveSocial()
        {
            Global.SocialMgr.RemovePlayerSocial(GetGUID());
            m_social = null;
        }

        public void SaveRecallPosition()
        {
            m_recall_location = new WorldLocation(this);
        }
        public void Recall() { TeleportTo(m_recall_location); }

        public uint GetSaveTimer() { return m_nextSave; }
        void SetSaveTimer(uint timer) { m_nextSave = timer; }

        void SendAurasForTarget(Unit target)
        {
            if (target == null || target.GetVisibleAuras().Empty())                  // speedup things
                return;

            var visibleAuras = target.GetVisibleAuras();

            AuraUpdate update = new AuraUpdate();
            update.UpdateAll = true;
            update.UnitGUID = target.GetGUID();

            foreach (var auraApp in visibleAuras)
            {
                AuraInfo auraInfo = new AuraInfo();
                auraApp.BuildUpdatePacket(ref auraInfo, false);
                update.Auras.Add(auraInfo);
            }

            SendPacket(update);
        }

        public void InitStatsForLevel(bool reapplyMods = false)
        {
            if (reapplyMods)                                        //reapply stats values only on .reset stats (level) command
                _RemoveAllStatBonuses();

            uint basemana;
            Global.ObjectMgr.GetPlayerClassLevelInfo(GetClass(), getLevel(), out basemana);

            PlayerLevelInfo info = Global.ObjectMgr.GetPlayerLevelInfo(GetRace(), GetClass(), getLevel());

            SetUInt32Value(ActivePlayerFields.MaxLevel, WorldConfig.GetUIntValue(WorldCfg.MaxPlayerLevel));
            SetUInt32Value(ActivePlayerFields.NextLevelXp, Global.ObjectMgr.GetXPForLevel(getLevel()));

            // reset before any aura state sources (health set/aura apply)
            SetUInt32Value(UnitFields.AuraState, 0);

            UpdateSkillsForLevel();

            // set default cast time multiplier
            SetFloatValue(UnitFields.ModCastSpeed, 1.0f);
            SetFloatValue(UnitFields.ModCastHaste, 1.0f);
            SetFloatValue(UnitFields.ModHaste, 1.0f);
            SetFloatValue(UnitFields.ModRangedHaste, 1.0f);
            SetFloatValue(UnitFields.ModHasteRegen, 1.0f);
            SetFloatValue(UnitFields.ModTimeRate, 1.0f);

            // reset size before reapply auras
            SetObjectScale(1.0f);

            // save base values (bonuses already included in stored stats
            for (var i = Stats.Strength; i < Stats.Max; ++i)
                SetCreateStat(i, info.stats[(int)i]);

            for (var i = Stats.Strength; i < Stats.Max; ++i)
                SetStat(i, info.stats[(int)i]);

            SetCreateHealth(0);

            //set create powers
            SetCreateMana(basemana);

            SetArmor((int)(GetCreateStat(Stats.Agility) * 2), 0);

            InitStatBuffMods();

            //reset rating fields values
            for (var index = 0; index < (int)CombatRating.Max; ++index)
                SetUInt32Value(ActivePlayerFields.CombatRating + index, 0);

            SetUInt32Value(ActivePlayerFields.ModHealingDonePos, 0);
            SetFloatValue(ActivePlayerFields.ModHealingPct, 1.0f);
            SetFloatValue(ActivePlayerFields.ModHealingDonePct, 1.0f);
            SetFloatValue(ActivePlayerFields.ModPeriodicHealingDonePercent, 1.0f);
            for (byte i = 0; i < 7; ++i)
            {
                SetInt32Value(ActivePlayerFields.ModDamageDoneNeg + i, 0);
                SetInt32Value(ActivePlayerFields.ModDamageDonePos + i, 0);
                SetFloatValue(ActivePlayerFields.ModDamageDonePct + i, 1.0f);
            }

            SetFloatValue(ActivePlayerFields.ModSpellPowerPct, 1.0f);

            //reset attack power, damage and attack speed fields
            for (byte i = 0; i < (int)WeaponAttackType.Max; ++i)
                SetFloatValue(UnitFields.BaseAttackTime + i, SharedConst.BaseAttackTime);

            SetFloatValue(UnitFields.MinDamage, 0.0f);
            SetFloatValue(UnitFields.MaxDamage, 0.0f);
            SetFloatValue(UnitFields.MinOffHandDamage, 0.0f);
            SetFloatValue(UnitFields.MaxOffHandDamage, 0.0f);
            SetFloatValue(UnitFields.MinRangedDamage, 0.0f);
            SetFloatValue(UnitFields.MaxRangedDamage, 0.0f);
            for (var i = 0; i < 3; ++i)
            {
                SetFloatValue(ActivePlayerFields.WeaponDmgMultipliers + i, 1.0f);
                SetFloatValue(ActivePlayerFields.WeaponAtkSpeedMultipliers + i, 1.0f);
            }

            SetInt32Value(UnitFields.AttackPower, 0);
            SetFloatValue(UnitFields.AttackPowerMultiplier, 0.0f);
            SetInt32Value(UnitFields.RangedAttackPower, 0);
            SetFloatValue(UnitFields.RangedAttackPowerMultiplier, 0.0f);

            // Base crit values (will be recalculated in UpdateAllStats() at loading and in _ApplyAllStatBonuses() at reset
            SetFloatValue(ActivePlayerFields.CritPercentage, 0.0f);
            SetFloatValue(ActivePlayerFields.OffhandCritPercentage, 0.0f);
            SetFloatValue(ActivePlayerFields.RangedCritPercentage, 0.0f);

            // Init spell schools (will be recalculated in UpdateAllStats() at loading and in _ApplyAllStatBonuses() at reset
            SetFloatValue(ActivePlayerFields.SpellCritPercentage1, 0.0f);

            SetFloatValue(ActivePlayerFields.ParryPercentage, 0.0f);
            SetFloatValue(ActivePlayerFields.BlockPercentage, 0.0f);

            // Static 30% damage blocked
            SetUInt32Value(ActivePlayerFields.ShieldBlock, 30);

            // Dodge percentage
            SetFloatValue(ActivePlayerFields.DodgePercentage, 0.0f);

            // set armor (resistance 0) to original value (create_agility*2)
            SetArmor((int)(GetCreateStat(Stats.Agility) * 2), 0);
            SetBonusResistanceMod(SpellSchools.Normal, 0);
            // set other resistance to original value (0)
            for (var spellSchool = SpellSchools.Holy; spellSchool < SpellSchools.Max; ++spellSchool)
            {
                SetResistance(spellSchool, 0);
                SetBonusResistanceMod(spellSchool, 0);
            }

            SetUInt32Value(ActivePlayerFields.ModTargetResistance, 0);
            SetUInt32Value(ActivePlayerFields.ModTargetPhysicalResistance, 0);
            for (var i = 0; i < (int)SpellSchools.Max; ++i)
            {
                SetUInt32Value(UnitFields.PowerCostModifier + i, 0);
                SetFloatValue(UnitFields.PowerCostMultiplier + i, 0.0f);
            }
            // Reset no reagent cost field
            for (byte i = 0; i < 4; ++i)
                SetUInt32Value(ActivePlayerFields.NoReagentCost + i, 0);
            // Init data for form but skip reapply item mods for form
            InitDataForForm(reapplyMods);

            // save new stats
            for (var i = PowerType.Mana; i < PowerType.Max; ++i)
                SetMaxPower(i, GetCreatePowers(i));

            SetMaxHealth(0);                     // stamina bonus will applied later

            // cleanup mounted state (it will set correctly at aura loading if player saved at mount.
            SetUInt32Value(UnitFields.MountDisplayId, 0);

            // cleanup unit flags (will be re-applied if need at aura load).
            RemoveFlag(UnitFields.Flags,
            UnitFlags.NonAttackable | UnitFlags.RemoveClientControl | UnitFlags.NotAttackable1 |
            UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc | UnitFlags.Looting |
            UnitFlags.PetInCombat | UnitFlags.Silenced | UnitFlags.Pacified |
            UnitFlags.Stunned | UnitFlags.InCombat | UnitFlags.Disarmed |
            UnitFlags.Confused | UnitFlags.Fleeing | UnitFlags.NotSelectable |
            UnitFlags.Skinnable | UnitFlags.Mount | UnitFlags.TaxiFlight);
            SetFlag(UnitFields.Flags, UnitFlags.PvpAttackable);   // must be set

            SetFlag(UnitFields.Flags2, UnitFlags2.RegeneratePower);// must be set

            // cleanup player flags (will be re-applied if need at aura load), to avoid have ghost flag without ghost aura, for example.
            RemoveFlag(PlayerFields.Flags, PlayerFlags.AFK | PlayerFlags.DND | PlayerFlags.GM | PlayerFlags.Ghost | PlayerFlags.AllowOnlyAbility);

            RemoveStandFlags(UnitBytes1Flags.All);                 // one form stealth modified bytes
            RemoveByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, (UnitBytes2Flags.FFAPvp | UnitBytes2Flags.Sanctuary));

            // restore if need some important flags
            SetByteValue(ActivePlayerFields.Bytes2, PlayerFieldOffsets.FieldBytes2OffsetIgnorePowerRegenPredictionMask, 0);
            SetByteValue(ActivePlayerFields.Bytes2, PlayerFieldOffsets.FieldBytes2OffsetAuraVision, 0);
            SetByteValue(ActivePlayerFields.Bytes2, 3, 0);

            if (reapplyMods)                                        // reapply stats values only on .reset stats (level) command
                _ApplyAllStatBonuses();

            // set current level health and mana/energy to maximum after applying all mods.
            SetFullHealth();
            SetFullPower(PowerType.Mana);
            SetFullPower(PowerType.Energy);
            if (GetPower(PowerType.Rage) > GetMaxPower(PowerType.Rage))
                SetFullPower(PowerType.Rage);
            SetFullPower(PowerType.Focus);
            SetPower(PowerType.RunicPower, 0);

            // update level to hunter/summon pet
            Pet pet = GetPet();
            if (pet)
                pet.SynchronizeLevelWithOwner();
        }
        public void InitDataForForm(bool reapplyMods = false)
        {
            ShapeShiftForm form = GetShapeshiftForm();

            var ssEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)form);
            if (ssEntry != null && ssEntry.CombatRoundTime != 0)
            {
                SetBaseAttackTime(WeaponAttackType.BaseAttack, ssEntry.CombatRoundTime);
                SetBaseAttackTime(WeaponAttackType.OffAttack, ssEntry.CombatRoundTime);
                SetBaseAttackTime(WeaponAttackType.RangedAttack, SharedConst.BaseAttackTime);
            }
            else
                SetRegularAttackTime();

            UpdateDisplayPower();

            // update auras at form change, ignore this at mods reapply (.reset stats/etc) when form not change.
            if (!reapplyMods)
                UpdateEquipSpellsAtFormChange();

            UpdateAttackPowerAndDamage();
            UpdateAttackPowerAndDamage(true);
        }

        public ReputationRank GetReputationRank(uint faction)
        {
            var factionEntry = CliDB.FactionStorage.LookupByKey(faction);
            return GetReputationMgr().GetRank(factionEntry);
        }
        public ReputationMgr GetReputationMgr()
        {
            return reputationMgr;
        }

        #region Sends / Updates
        void BeforeVisibilityDestroy(WorldObject obj, Player p)
        {
            if (!obj.IsTypeId(TypeId.Unit))
                return;

            if (p.GetPetGUID() == obj.GetGUID() && obj.ToCreature().IsPet())
                ((Pet)obj).Remove(PetSaveMode.NotInSlot, true);
        }
        public void UpdateVisibilityOf(WorldObject target)
        {
            if (HaveAtClient(target))
            {
                if (!CanSeeOrDetect(target, false, true))
                {
                    if (target.IsTypeId(TypeId.Unit))
                        BeforeVisibilityDestroy(target.ToCreature(), this);

                    target.DestroyForPlayer(this);
                    m_clientGUIDs.Remove(target.GetGUID());
                }
            }
            else
            {
                if (CanSeeOrDetect(target, false, true))
                {
                    target.SendUpdateToPlayer(this);
                    m_clientGUIDs.Add(target.GetGUID());

                    // target aura duration for caster show only if target exist at caster client
                    // send data at target visibility change (adding to client)
                    if (target.isTypeMask(TypeMask.Unit))
                        SendInitialVisiblePackets(target.ToUnit());
                }
            }
        }
        public void UpdateVisibilityOf<T>(T target, UpdateData data, List<Unit> visibleNow) where T : WorldObject
        {
            if (HaveAtClient(target))
            {
                if (!CanSeeOrDetect(target, false, true))
                {
                    BeforeVisibilityDestroy(target, this);

                    target.BuildOutOfRangeUpdateBlock(data);
                    m_clientGUIDs.Remove(target.GetGUID());
                }
            }
            else
            {
                if (CanSeeOrDetect(target, false, true))
                {
                    target.BuildCreateUpdateBlockForPlayer(data, this);
                    UpdateVisibilityOf_helper(m_clientGUIDs, target, visibleNow);
                }
            }
        }
        void UpdateVisibilityOf_helper<T>(List<ObjectGuid> s64, T target, List<Unit> v) where  T : WorldObject
        {
            switch (target.GetTypeId())
            {
                case TypeId.GameObject:
                    // @HACK: This is to prevent objects like deeprun tram from disappearing when player moves far from its spawn point while riding it
                    // But exclude stoppable elevators from this hack - they would be teleporting from one end to another
                    // if affected transports move so far horizontally that it causes them to run out of visibility range then you are out of luck
                    // fix visibility instead of adding hacks here
                    if (target.ToGameObject().IsDynTransport())
                        s64.Add(target.GetGUID());
                    break;
                case TypeId.Unit:
                    s64.Add(target.GetGUID());
                    v.Add(target.ToCreature());
                    break;
                case TypeId.Player:
                    s64.Add(target.GetGUID());
                    v.Add(target.ToPlayer());
                    break;
            }
        }

        public void SendInitialVisiblePackets(Unit target)
        {
            SendAurasForTarget(target);
            if (target.IsAlive())
            {
                if (target.HasUnitState(UnitState.MeleeAttacking) && target.GetVictim() != null)
                    target.SendMeleeAttackStart(target.GetVictim());
            }
        }

        public override void UpdateObjectVisibility(bool forced = true)
        {
            // Prevent updating visibility if player is not in world (example: LoadFromDB sets drunkstate which updates invisibility while player is not in map)
            if (!IsInWorld)
                return;

            if (!forced)
                AddToNotify(NotifyFlags.VisibilityChanged);
            else
            {
                base.UpdateObjectVisibility(true);
                UpdateVisibilityForPlayer();
            }
        }

        public void UpdateVisibilityForPlayer()
        {
            // updates visibility of all objects around point of view for current player
            var notifier = new VisibleNotifier(this);
            Cell.VisitAllObjects(seerView, notifier, GetSightRange());
            notifier.SendToSelf();   // send gathered data
        }

        public void SetSeer(WorldObject target) { seerView = target; }

        public override void SendMessageToSetInRange(ServerPacket data, float dist, bool self)
        {
            if (self)
                SendPacket(data);

            MessageDistDeliverer notifier = new MessageDistDeliverer(this, data, dist);
            Cell.VisitWorldObjects(this, notifier, dist);
        }

        void SendMessageToSetInRange(ServerPacket data, float dist, bool self, bool own_team_only)
        {
            if (self)
                SendPacket(data);

            MessageDistDeliverer notifier = new MessageDistDeliverer(this, data, dist, own_team_only);
            Cell.VisitWorldObjects(this, notifier, dist);
        }

        public override void SendMessageToSet(ServerPacket data, Player skipped_rcvr)
        {
            if (skipped_rcvr != this)
                SendPacket(data);

            // we use World.GetMaxVisibleDistance() because i cannot see why not use a distance
            // update: replaced by GetMap().GetVisibilityDistance()
            MessageDistDeliverer notifier = new MessageDistDeliverer(this, data, GetVisibilityRange(), false, skipped_rcvr);
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
        }
        public override void SendMessageToSet(ServerPacket data, bool self)
        {
            SendMessageToSetInRange(data, GetVisibilityRange(), self);
        }

        public override bool UpdatePosition(Position pos, bool teleport = false)
        {
            return UpdatePosition(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation(), teleport);
        }

        public override bool UpdatePosition(float x, float y, float z, float orientation, bool teleport = false)
        {
            if (!base.UpdatePosition(x, y, z, orientation, teleport))
                return false;

            // group update
            if (GetGroup())
                SetGroupUpdateFlag(GroupUpdateFlags.Position);

            if (GetTrader() && !IsWithinDistInMap(GetTrader(), SharedConst.InteractionDistance))
                GetSession().SendCancelTrade();

            CheckAreaExploreAndOutdoor();

            return true;
        }

        void SendNewCurrency(uint id)
        {
            var Curr = _currencyStorage.LookupByKey(id);
            if (Curr == null)
                return;

            CurrencyTypesRecord entry = CliDB.CurrencyTypesStorage.LookupByKey(id);
            if (entry == null) // should never happen
                return;

            SetupCurrency packet = new SetupCurrency();
            SetupCurrency.Record record = new SetupCurrency.Record();
            record.Type = entry.Id;
            record.Quantity = Curr.Quantity;
            record.WeeklyQuantity.Set(Curr.WeeklyQuantity);
            record.MaxWeeklyQuantity.Set(GetCurrencyWeekCap(entry));
            record.TrackedQuantity.Set(Curr.TrackedQuantity);
            record.Flags = Curr.Flags;

            packet.Data.Add(record);

            SendPacket(packet);
        }

        void SendCurrencies()
        {
            SetupCurrency packet = new SetupCurrency();

            foreach (var pair in _currencyStorage)
            {
                CurrencyTypesRecord entry = CliDB.CurrencyTypesStorage.LookupByKey(pair.Key);

                // not send init meta currencies.
                if (entry == null || entry.CategoryID == 89) //CURRENCY_CATEGORY_META_CONQUEST
                    continue;

                SetupCurrency.Record record = new SetupCurrency.Record();
                record.Type = entry.Id;
                record.Quantity = pair.Value.Quantity;
                record.WeeklyQuantity.Set(pair.Value.WeeklyQuantity);
                record.MaxWeeklyQuantity.Set(GetCurrencyWeekCap(entry));
                record.TrackedQuantity.Set(pair.Value.TrackedQuantity);
                record.Flags = pair.Value.Flags;

                packet.Data.Add(record);
            }

            SendPacket(packet);
        }

        public void ResetCurrencyWeekCap()
        {
            for (byte arenaSlot = 0; arenaSlot < 3; arenaSlot++)
            {
                uint arenaTeamId = GetArenaTeamId(arenaSlot);
                if (arenaTeamId != 0)
                {
                    ArenaTeam arenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(arenaTeamId);
                    arenaTeam.FinishWeek();                              // set played this week etc values to 0 in memory, too
                    arenaTeam.SaveToDB();                                // save changes
                    arenaTeam.NotifyStatsChanged();                      // notify the players of the changes
                }
            }

            foreach (var currency in _currencyStorage.Values)
            {

                currency.WeeklyQuantity = 0;
                currency.state = PlayerCurrencyState.Changed;
            }

            SendPacket(new ResetWeeklyCurrency());
        }

        void CheckAreaExploreAndOutdoor()
        {
            if (!IsAlive())
                return;

            if (IsInFlight())
                return;

            bool isOutdoor;
            uint areaId = GetMap().GetAreaId(GetPhaseShift(), GetPositionX(), GetPositionY(), GetPositionZ(), out isOutdoor);
            AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);

            if (WorldConfig.GetBoolValue(WorldCfg.VmapIndoorCheck) && !isOutdoor)
                RemoveAurasWithAttribute(SpellAttr0.OutdoorsOnly);

            if (areaId == 0)
                return;

            if (areaEntry == null)
            {
                Log.outError(LogFilter.Player, "Player '{0}' ({1}) discovered unknown area (x: {2} y: {3} z: {4} map: {5})",
                    GetName(), GetGUID().ToString(), GetPositionX(), GetPositionY(), GetPositionZ(), GetMapId());
                return;
            }

            int offset = areaEntry.AreaBit / 32;
            if (offset >= PlayerConst.ExploredZonesSize)
            {
                Log.outError(LogFilter.Player, "Wrong area flag {0} in map data for (X: {1} Y: {2}) point to field PLAYER_EXPLORED_ZONES_1 + {3} ( {4} must be < {5} ).",
                    areaId, GetPositionX(), GetPositionY(), offset, offset, PlayerConst.ExploredZonesSize);
                return;
            }

            uint val = 1u << (areaEntry.AreaBit % 32);
            uint currFields = GetUInt32Value(ActivePlayerFields.ExploredZones + offset);

            if (!Convert.ToBoolean(currFields & val))
            {
                SetUInt32Value(ActivePlayerFields.ExploredZones + offset, currFields | val);

                UpdateCriteria(CriteriaTypes.ExploreArea);

                if (areaEntry.ExplorationLevel > 0)
                {
                    if (getLevel() >= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                    {
                        SendExplorationExperience(areaId, 0);
                    }
                    else
                    {
                        int diff = (int)(getLevel() - areaEntry.ExplorationLevel);
                        uint XP = 0;
                        if (diff < -5)
                        {
                            XP = (uint)(Global.ObjectMgr.GetBaseXP(getLevel() + 5) * WorldConfig.GetFloatValue(WorldCfg.RateXpExplore));
                        }
                        else if (diff > 5)
                        {
                            int exploration_percent = 100 - ((diff - 5) * 5);
                            if (exploration_percent < 0)
                                exploration_percent = 0;

                            XP = (uint)(Global.ObjectMgr.GetBaseXP(areaEntry.ExplorationLevel) * exploration_percent / 100 * WorldConfig.GetFloatValue(WorldCfg.RateXpExplore));
                        }
                        else
                        {
                            XP = (uint)(Global.ObjectMgr.GetBaseXP(areaEntry.ExplorationLevel) * WorldConfig.GetFloatValue(WorldCfg.RateXpExplore));
                        }

                        GiveXP(XP, null);
                        SendExplorationExperience(areaId, XP);
                    }
                    Log.outInfo(LogFilter.Player, "Player {0} discovered a new area: {1}", GetGUID().ToString(), areaId);
                }
            }
        }
        void SendExplorationExperience(uint Area, uint Experience)
        {
            SendPacket(new ExplorationExperience(Experience, Area));
        }

        public void SendSysMessage(CypherStrings str, params object[] args)
        {
            string input = Global.ObjectMgr.GetCypherString(str);
            string pattern = @"%(\d+(\.\d+)?)?(d|f|s|u)";

            int count = 0;
            string result = System.Text.RegularExpressions.Regex.Replace(input, pattern, m =>
            {
                return string.Concat("{", count++, "}");
            });

            SendSysMessage(result, args);
        }
        public void SendSysMessage(string str, params object[] args)
        {
            new CommandHandler(Session).SendSysMessage(str, args);
        }
        public void SendBuyError(BuyResult msg, Creature creature, uint item)
        {
            BuyFailed packet = new BuyFailed();
            packet.VendorGUID = creature ? creature.GetGUID() : ObjectGuid.Empty;
            packet.Muid = item;
            packet.Reason = msg;
            SendPacket(packet);
        }
        public void SendSellError(SellResult msg, Creature creature, ObjectGuid guid)
        {
            SellResponse sellResponse = new SellResponse();
            sellResponse.VendorGUID = (creature ? creature.GetGUID() : ObjectGuid.Empty);
            sellResponse.ItemGUID = guid;
            sellResponse.Reason = msg;
            SendPacket(sellResponse);
        }
        #endregion

        #region Chat
        public override void Say(string text, Language language, WorldObject obj = null)
        {
            Global.ScriptMgr.OnPlayerChat(this, ChatMsg.Say, language, text);

            ChatPkt data = new ChatPkt();
            data.Initialize(ChatMsg.Say, language, this, this, text);
            SendMessageToSetInRange(data, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), true);
        }
        public override void Say(uint textId, WorldObject target = null)
        {
            Talk(textId, ChatMsg.Say, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
        }
        public override void Yell(string text, Language language, WorldObject obj = null)
        {
            Global.ScriptMgr.OnPlayerChat(this, ChatMsg.Yell, language, text);

            ChatPkt data = new ChatPkt();
            data.Initialize(ChatMsg.Yell, language, this, this, text);
            SendMessageToSetInRange(data, WorldConfig.GetFloatValue(WorldCfg.ListenRangeYell), true);
        }
        public override void Yell(uint textId, WorldObject target = null)
        {
            Talk(textId, ChatMsg.Yell, WorldConfig.GetFloatValue(WorldCfg.ListenRangeYell), target);
        }
        public override void TextEmote(string text, WorldObject obj = null, bool something = false)
        {
            Global.ScriptMgr.OnPlayerChat(this, ChatMsg.Emote, Language.Universal, text);

            ChatPkt data = new ChatPkt();
            data.Initialize(ChatMsg.Emote, Language.Universal, this, this, text);
            SendMessageToSetInRange(data, WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote), !GetSession().HasPermission(RBACPermissions.TwoSideInteractionChat));
        }
        public override void TextEmote(uint textId, WorldObject target = null, bool isBossEmote = false)
        {
            Talk(textId, ChatMsg.Emote, WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote), target);
        }
        public void WhisperAddon(string text, string prefix, bool isLogged, Player receiver)
        {
            Global.ScriptMgr.OnPlayerChat(this, ChatMsg.Whisper, isLogged ? Language.AddonLogged : Language.Addon, text, receiver);

            if (!receiver.GetSession().IsAddonRegistered(prefix))
                return;

            ChatPkt data = new ChatPkt();
            data.Initialize(ChatMsg.Whisper, isLogged ? Language.AddonLogged : Language.Addon, this, this, text, 0, "", LocaleConstant.enUS, prefix);
            receiver.SendPacket(data);
        }
        public override void Whisper(string text, Language language, Player target = null, bool something = false)
        {
            bool isAddonMessage = language == Language.Addon;

            if (!isAddonMessage) // if not addon data
                language = Language.Universal; // whispers should always be readable

            //Player rPlayer = Global.ObjAccessor.FindPlayer(receiver);

            Global.ScriptMgr.OnPlayerChat(this, ChatMsg.Whisper, language, text, target);

            ChatPkt data = new ChatPkt();
            data.Initialize(ChatMsg.Whisper, language, this, this, text);
            target.SendPacket(data);

            // rest stuff shouldn't happen in case of addon message
            if (isAddonMessage)
                return;

            data.Initialize(ChatMsg.WhisperInform, language, target, target, text);
            SendPacket(data);

            if (!isAcceptWhispers() && !IsGameMaster() && !target.IsGameMaster())
            {
                SetAcceptWhispers(true);
                SendSysMessage(CypherStrings.CommandWhisperon);
            }

            // announce afk or dnd message
            if (target.isAFK())
                SendSysMessage(CypherStrings.PlayerAfk, target.GetName(), target.autoReplyMsg);
            else if (target.isDND())
                SendSysMessage(CypherStrings.PlayerDnd, target.GetName(), target.autoReplyMsg);
        }

        public override void Whisper(uint textId, Player target, bool isBossWhisper = false)
        {
            if (!target)
                return;

            BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(textId);
            if (bct == null)
            {
                Log.outError(LogFilter.Unit, "WorldObject.MonsterWhisper: `broadcast_text` was not {0} found", textId);
                return;
            }

            LocaleConstant locale = target.GetSession().GetSessionDbLocaleIndex();
            ChatPkt packet = new ChatPkt();
            packet.Initialize(ChatMsg.Whisper, Language.Universal, this, target, Global.DB2Mgr.GetBroadcastTextValue(bct, locale, GetGender()));
            target.SendPacket(packet);
        }
        #endregion

        public void ClearWhisperWhiteList() { WhisperList.Clear(); }
        public void AddWhisperWhiteList(ObjectGuid guid) { WhisperList.Add(guid); }
        public bool IsInWhisperWhiteList(ObjectGuid guid) { return WhisperList.Contains(guid); }
        public void RemoveFromWhisperWhiteList(ObjectGuid guid) { WhisperList.Remove(guid); }

        public void SetFallInformation(uint time, float z)
        {
            m_lastFallTime = time;
            m_lastFallZ = z;
        }

        public byte getCinematic() { return m_cinematic; }
        public void setCinematic(byte cine) { m_cinematic = cine; }

        public uint GetMovie() { return m_movie; }
        public void SetMovie(uint movie) { m_movie = movie; }

        public void SendCinematicStart(uint CinematicSequenceId)
        {
            TriggerCinematic packet = new TriggerCinematic();
            packet.CinematicID = CinematicSequenceId;
            SendPacket(packet);

            CinematicSequencesRecord sequence = CliDB.CinematicSequencesStorage.LookupByKey(CinematicSequenceId);
            if (sequence != null)
                _cinematicMgr.SetActiveCinematicCamera(sequence.Camera[0]);
        }
        public void SendMovieStart(uint movieId)
        {
            SetMovie(movieId);
            TriggerMovie packet = new TriggerMovie();
            packet.MovieID = movieId;
            SendPacket(packet);
        }

        public override void SetObjectScale(float scale)
        {
            base.SetObjectScale(scale);
            SetFloatValue(UnitFields.BoundingRadius, scale * SharedConst.DefaultWorldObjectSize);
            SetFloatValue(UnitFields.CombatReach, scale * SharedConst.DefaultCombatReach);
            if (IsInWorld)
                SendMovementSetCollisionHeight(scale * GetCollisionHeight(IsMounted()));
        }

        void SetXP(uint xp)
        {
            SetUInt32Value(ActivePlayerFields.Xp, xp);

            int playerLevelDelta = 0;

            // If XP < 50%, player should see scaling creature with -1 level except for level max
            if (getLevel() < SharedConst.MaxLevel && xp < (GetUInt32Value(ActivePlayerFields.NextLevelXp) / 2))
                playerLevelDelta = -1;

            SetInt32Value(UnitFields.ScalingLevelDelta, playerLevelDelta);
        }

        public void GiveXP(uint xp, Unit victim, float group_rate = 1.0f)
        {
            if (xp < 1)
                return;

            if (!IsAlive() && GetBattlegroundId() == 0)
                return;

            if (HasFlag(PlayerFields.Flags, PlayerFlags.NoXPGain))
                return;

            if (victim != null && victim.IsTypeId(TypeId.Unit) && !victim.ToCreature().hasLootRecipient())
                return;

            uint level = getLevel();

            Global.ScriptMgr.OnGivePlayerXP(this, xp, victim);

            // XP to money conversion processed in Player.RewardQuest
            if (level >= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                return;

            uint bonus_xp = 0;
            bool recruitAFriend = GetsRecruitAFriendBonus(true);

            // RaF does NOT stack with rested experience
            if (recruitAFriend)
                bonus_xp = 2 * xp; // xp + bonus_xp must add up to 3 * xp for RaF; calculation for quests done client-side
            else
                bonus_xp = victim != null ? _restMgr.GetRestBonusFor(RestTypes.XP, xp) : 0; // XP resting bonus

            LogXPGain packet = new LogXPGain();
            packet.Victim = victim ? victim.GetGUID() : ObjectGuid.Empty;
            packet.Original = (int)(xp + bonus_xp);
            packet.Reason = victim ? PlayerLogXPReason.Kill : PlayerLogXPReason.NoKill;
            packet.Amount = (int)xp;
            packet.GroupBonus = group_rate;
            packet.ReferAFriendBonusType = (byte)(recruitAFriend ? 1 : 0);
            SendPacket(packet);

            uint curXP = GetUInt32Value(ActivePlayerFields.Xp);
            uint nextLvlXP = GetUInt32Value(ActivePlayerFields.NextLevelXp);
            uint newXP = curXP + xp + bonus_xp;

            while (newXP >= nextLvlXP && level < WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
            {
                newXP -= nextLvlXP;

                if (level < WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                    GiveLevel(level + 1);

                level = getLevel();
                nextLvlXP = GetUInt32Value(ActivePlayerFields.NextLevelXp);
            }

            SetXP(newXP);
        }

        public void HandleBaseModValue(BaseModGroup modGroup, BaseModType modType, float amount, bool apply)
        {
            if (modGroup >= BaseModGroup.End || modType >= BaseModType.End)
            {
                Log.outError(LogFilter.Spells, "ERROR in HandleBaseModValue(): non existed BaseModGroup of wrong BaseModType!");
                return;
            }

            switch (modType)
            {
                case BaseModType.FlatMod:
                    m_auraBaseMod[(int)modGroup][(int)modType] += apply ? amount : -amount;
                    break;
                case BaseModType.PCTmod:
                    MathFunctions.ApplyPercentModFloatVar(ref m_auraBaseMod[(int)modGroup][(int)modType], amount, apply);
                    break;
            }

            if (!CanModifyStats())
                return;

            switch (modGroup)
            {
                case BaseModGroup.CritPercentage:
                    UpdateCritPercentage(WeaponAttackType.BaseAttack);
                    break;
                case BaseModGroup.RangedCritPercentage:
                    UpdateCritPercentage(WeaponAttackType.RangedAttack);
                    break;
                case BaseModGroup.OffhandCritPercentage:
                    UpdateCritPercentage(WeaponAttackType.OffAttack);
                    break;
                default:
                    break;
            }
        }

        public void AddComboPoints(sbyte count, Spell spell = null)
        {
            if (count == 0)
                return;

            sbyte comboPoints = spell != null ? spell.m_comboPointGain : (sbyte)GetPower(PowerType.ComboPoints);

            // without combo points lost (duration checked in aura)
            RemoveAurasByType(AuraType.RetainComboPoints);

            comboPoints += (sbyte)count;

            if (comboPoints > 5)
                comboPoints = 5;
            else if (comboPoints < 0)
                comboPoints = 0;

            if (spell == null)
                SetPower(PowerType.ComboPoints, comboPoints);
            else
                spell.m_comboPointGain = comboPoints;
        }
        public void GainSpellComboPoints(sbyte count)
        {
            if (count == 0)
                return;

            sbyte cp = (sbyte)GetPower(PowerType.ComboPoints);
            cp += count;

            if (cp > 5)
                cp = 5;
            else if (cp < 0)
                cp = 0;

            SetPower(PowerType.ComboPoints, cp);
        }
        public void ClearComboPoints()
        {
            // without combopoints lost (duration checked in aura)
            RemoveAurasByType(AuraType.RetainComboPoints);

            SetPower(PowerType.ComboPoints, 0);
        }
        public byte GetComboPoints() { return (byte)GetPower(PowerType.ComboPoints); }

        public byte GetDrunkValue() { return GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetInebriation); }
        public void SetDrunkValue(byte newDrunkValue, uint itemId = 0)
        {
            bool isSobering = newDrunkValue < GetDrunkValue();
            DrunkenState oldDrunkenState = GetDrunkenstateByValue(GetDrunkValue());
            if (newDrunkValue > 100)
                newDrunkValue = 100;

            // select drunk percent or total SPELL_AURA_MOD_FAKE_INEBRIATE amount, whichever is higher for visibility updates
            int drunkPercent = Math.Max(newDrunkValue, GetTotalAuraModifier(AuraType.ModFakeInebriate));
            if (drunkPercent != 0)
            {
                m_invisibilityDetect.AddFlag(InvisibilityType.Drunk);
                m_invisibilityDetect.SetValue(InvisibilityType.Drunk, drunkPercent);
            }
            else if (!HasAuraType(AuraType.ModFakeInebriate) && newDrunkValue == 0)
                m_invisibilityDetect.DelFlag(InvisibilityType.Drunk);

            DrunkenState newDrunkenState = GetDrunkenstateByValue(newDrunkValue);
            SetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetInebriation, newDrunkValue);
            UpdateObjectVisibility();

            if (!isSobering)
                m_drunkTimer = 0;   // reset sobering timer

            if (newDrunkenState == oldDrunkenState)
                return;

            CrossedInebriationThreshold data = new CrossedInebriationThreshold();
            data.Guid = GetGUID();
            data.Threshold = (uint)newDrunkenState;
            data.ItemID = itemId;

            SendMessageToSet(data, true);
        }
        public static DrunkenState GetDrunkenstateByValue(byte value)
        {
            if (value >= 90)
                return DrunkenState.Smashed;
            if (value >= 50)
                return DrunkenState.Drunk;
            if (value != 0)
                return DrunkenState.Tipsy;
            return DrunkenState.Sober;
        }

        public uint GetDeathTimer() { return m_deathTimer; }
        public bool ActivateTaxiPathTo(List<uint> nodes, Creature npc = null, uint spellid = 0, uint preferredMountDisplay = 0)
        {
            if (nodes.Count < 2)
            {
                GetSession().SendActivateTaxiReply(ActivateTaxiReply.NoSuchPath);
                return false;
            }

            // not let cheating with start flight in time of logout process || while in combat || has type state: stunned || has type state: root
            if (GetSession().isLogingOut() || IsInCombat() || HasUnitState(UnitState.Stunned) || HasUnitState(UnitState.Root))
            {
                GetSession().SendActivateTaxiReply(ActivateTaxiReply.PlayerBusy);
                return false;
            }

            if (HasFlag(UnitFields.Flags, UnitFlags.RemoveClientControl))
                return false;

            // taximaster case
            if (npc != null)
            {
                // not let cheating with start flight mounted
                RemoveAurasByType(AuraType.Mounted);

                if (GetDisplayId() != GetNativeDisplayId())
                    RestoreDisplayId(true);

                if (IsDisallowedMountForm(getTransForm(), ShapeShiftForm.None, GetDisplayId()))
                {
                    GetSession().SendActivateTaxiReply(ActivateTaxiReply.PlayerShapeshifted);
                    return false;
                }

                // not let cheating with start flight in time of logout process || if casting not finished || while in combat || if not use Spell's with EffectSendTaxi
                if (IsNonMeleeSpellCast(false))
                {
                    GetSession().SendActivateTaxiReply(ActivateTaxiReply.PlayerBusy);
                    return false;
                }
            }
            // cast case or scripted call case
            else
            {
                RemoveAurasByType(AuraType.Mounted);

                if (GetDisplayId() != GetNativeDisplayId())
                    RestoreDisplayId(true);

                Spell spell = GetCurrentSpell(CurrentSpellTypes.Generic);
                if (spell != null)
                    if (spell.m_spellInfo.Id != spellid)
                        InterruptSpell(CurrentSpellTypes.Generic, false);

                InterruptSpell(CurrentSpellTypes.AutoRepeat, false);

                spell = GetCurrentSpell(CurrentSpellTypes.Channeled);
                if (spell != null)
                    if (spell.m_spellInfo.Id != spellid)
                        InterruptSpell(CurrentSpellTypes.Channeled, true);
            }

            uint sourcenode = nodes[0];

            // starting node too far away (cheat?)
            var node = CliDB.TaxiNodesStorage.LookupByKey(sourcenode);
            if (node == null)
            {
                GetSession().SendActivateTaxiReply(ActivateTaxiReply.NoSuchPath);
                return false;
            }

            // Prepare to flight start now

            // stop combat at start taxi flight if any
            CombatStop();

            StopCastingCharm();
            StopCastingBindSight();
            ExitVehicle();

            // stop trade (client cancel trade at taxi map open but cheating tools can be used for reopen it)
            TradeCancel(true);

            // clean not finished taxi path if any
            m_taxi.ClearTaxiDestinations();

            // 0 element current node
            m_taxi.AddTaxiDestination(sourcenode);

            // fill destinations path tail
            uint sourcepath = 0;
            uint totalcost = 0;
            uint firstcost = 0;

            uint prevnode = sourcenode;
            uint lastnode = 0;

            for (int i = 1; i < nodes.Count; ++i)
            {
                uint path, cost;

                lastnode = nodes[i];
                Global.ObjectMgr.GetTaxiPath(prevnode, lastnode, out path, out cost);

                if (path == 0)
                {
                    m_taxi.ClearTaxiDestinations();
                    return false;
                }

                totalcost += cost;
                if (i == 1)
                    firstcost = cost;

                if (prevnode == sourcenode)
                    sourcepath = path;

                m_taxi.AddTaxiDestination(lastnode);

                prevnode = lastnode;
            }

            // get mount model (in case non taximaster (npc == NULL) allow more wide lookup)
            //
            // Hack-Fix for Alliance not being able to use Acherus taxi. There is
            // only one mount ID for both sides. Probably not good to use 315 in case DBC nodes
            // change but I couldn't find a suitable alternative. OK to use class because only DK
            // can use this taxi.
            uint mount_display_id;
            if (node.Flags.HasAnyFlag(TaxiNodeFlags.UseFavoriteMount) && preferredMountDisplay != 0)
                mount_display_id = preferredMountDisplay;
            else
                mount_display_id = Global.ObjectMgr.GetTaxiMountDisplayId(sourcenode, GetTeam(), npc == null || (sourcenode == 315 && GetClass() == Class.Deathknight));

            // in spell case allow 0 model
            if ((mount_display_id == 0 && spellid == 0) || sourcepath == 0)
            {
                GetSession().SendActivateTaxiReply(ActivateTaxiReply.UnspecifiedServerError);
                m_taxi.ClearTaxiDestinations();
                return false;
            }

            ulong money = GetMoney();
            if (npc != null)
            {
                float discount = GetReputationPriceDiscount(npc);
                totalcost = (uint)Math.Ceiling(totalcost * discount);
                firstcost = (uint)Math.Ceiling(firstcost * discount);
                m_taxi.SetFlightMasterFactionTemplateId(npc.getFaction());
            }
            else
                m_taxi.SetFlightMasterFactionTemplateId(0);

            if (money < totalcost)
            {
                GetSession().SendActivateTaxiReply(ActivateTaxiReply.NotEnoughMoney);
                m_taxi.ClearTaxiDestinations();
                return false;
            }

            //Checks and preparations done, DO FLIGHT
            UpdateCriteria(CriteriaTypes.FlightPathsTaken, 1);

            if (WorldConfig.GetBoolValue(WorldCfg.InstantTaxi))
            {
                var lastPathNode = CliDB.TaxiNodesStorage.LookupByKey(nodes[nodes.Count - 1]);
                m_taxi.ClearTaxiDestinations();
                ModifyMoney(-totalcost);
                UpdateCriteria(CriteriaTypes.GoldSpentForTravelling, totalcost);
                TeleportTo(lastPathNode.ContinentID, lastPathNode.Pos.X, lastPathNode.Pos.Y, lastPathNode.Pos.Z, GetOrientation());
                return false;
            }
            else
            {
                ModifyMoney(-firstcost);
                UpdateCriteria(CriteriaTypes.GoldSpentForTravelling, firstcost);
                GetSession().SendActivateTaxiReply();
                GetSession().SendDoFlight(mount_display_id, sourcepath);
            }
            return true;
        }

        public bool ActivateTaxiPathTo(uint taxi_path_id, uint spellid = 0)
        {
            var entry = CliDB.TaxiPathStorage.LookupByKey(taxi_path_id);
            if (entry == null)
                return false;

            List<uint> nodes = new List<uint>();

            nodes.Add(entry.FromTaxiNode);
            nodes.Add(entry.ToTaxiNode);

            return ActivateTaxiPathTo(nodes, null, spellid);
        }

        public void CleanupAfterTaxiFlight()
        {
            m_taxi.ClearTaxiDestinations();        // not destinations, clear source node
            Dismount();
            RemoveFlag(UnitFields.Flags, UnitFlags.RemoveClientControl | UnitFlags.TaxiFlight);
            getHostileRefManager().setOnlineOfflineState(true);
        }

        public void ContinueTaxiFlight()
        {
            uint sourceNode = m_taxi.GetTaxiSource();
            if (sourceNode == 0)
                return;

            Log.outDebug(LogFilter.Unit, "WORLD: Restart character {0} taxi flight", GetGUID().ToString());

            uint mountDisplayId = Global.ObjectMgr.GetTaxiMountDisplayId(sourceNode, GetTeam(), true);
            if (mountDisplayId == 0)
                return;

            uint path = m_taxi.GetCurrentTaxiPath();

            // search appropriate start path node
            uint startNode = 0;

            var nodeList = CliDB.TaxiPathNodesByPath[path];

            float distPrev = MapConst.MapSize * MapConst.MapSize;
            float distNext = GetExactDistSq(nodeList[0].Loc.X, nodeList[0].Loc.Y, nodeList[0].Loc.Z);

            for (int i = 1; i < nodeList.Length; ++i)
            {
                var node = nodeList[i];
                var prevNode = nodeList[i - 1];

                // skip nodes at another map
                if (node.ContinentID != GetMapId())
                    continue;

                distPrev = distNext;

                distNext = GetExactDistSq(node.Loc.X, node.Loc.Y, node.Loc.Z);

                float distNodes =
                    (node.Loc.X - prevNode.Loc.X) * (node.Loc.X - prevNode.Loc.X) +
                    (node.Loc.Y - prevNode.Loc.Y) * (node.Loc.Y - prevNode.Loc.Y) +
                    (node.Loc.Z - prevNode.Loc.Z) * (node.Loc.Z - prevNode.Loc.Z);

                if (distNext + distPrev < distNodes)
                {
                    startNode = (uint)i;
                    break;
                }
            }

            GetSession().SendDoFlight(mountDisplayId, path, startNode);
        }

        public byte GetGrantableLevels() { return m_grantableLevels; }
        public void SetGrantableLevels(int val) { m_grantableLevels = (byte)val; }
        public bool GetsRecruitAFriendBonus(bool forXP)
        {
            bool recruitAFriend = false;
            if (getLevel() <= WorldConfig.GetIntValue(WorldCfg.MaxRecruitAFriendBonusPlayerLevel) || !forXP)
            {
                Group group = GetGroup();
                if (group)
                {
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                    {
                        Player player = refe.GetSource();
                        if (!player)
                            continue;

                        if (!player.IsAtRecruitAFriendDistance(this))
                            continue;                               // member (alive or dead) or his corpse at req. distance

                        if (forXP)
                        {
                            // level must be allowed to get RaF bonus
                            if (player.getLevel() > WorldConfig.GetIntValue(WorldCfg.MaxRecruitAFriendBonusPlayerLevel))
                                continue;

                            // level difference must be small enough to get RaF bonus, UNLESS we are lower level
                            if (player.getLevel() < getLevel())
                                if (getLevel() - player.getLevel() > WorldConfig.GetIntValue(WorldCfg.MaxRecruitAFriendBonusPlayerLevelDifference))
                                    continue;
                        }

                        bool ARecruitedB = (player.GetSession().GetRecruiterId() == GetSession().GetAccountId());
                        bool BRecruitedA = (GetSession().GetRecruiterId() == player.GetSession().GetAccountId());
                        if (ARecruitedB || BRecruitedA)
                        {
                            recruitAFriend = true;
                            break;
                        }
                    }
                }
            }
            return recruitAFriend;
        }

        bool IsAtRecruitAFriendDistance(WorldObject pOther)
        {
            if (!pOther)
                return false;
            WorldObject player = GetCorpse();
            if (!player || IsAlive())
                player = this;

            if (player.GetMapId() != pOther.GetMapId() || player.GetInstanceId() != pOther.GetInstanceId())
                return false;

            return pOther.GetDistance(player) <= WorldConfig.GetFloatValue(WorldCfg.MaxRecruitAFriendDistance);
        }

        public bool IsBeingTeleported() { return mSemaphoreTeleport_Near || mSemaphoreTeleport_Far; }
        public bool IsBeingTeleportedNear() { return mSemaphoreTeleport_Near; }
        public bool IsBeingTeleportedFar() { return mSemaphoreTeleport_Far; }
        public bool IsBeingTeleportedSeamlessly() { return IsBeingTeleportedFar() && m_teleport_options.HasAnyFlag(TeleportToOptions.Seamless); }
        public void SetSemaphoreTeleportNear(bool semphsetting) { mSemaphoreTeleport_Near = semphsetting; }
        public void SetSemaphoreTeleportFar(bool semphsetting) { mSemaphoreTeleport_Far = semphsetting; }

        //new
        public uint DoRandomRoll(uint minimum, uint maximum)
        {
            Cypher.Assert(maximum <= 10000);

            uint roll = RandomHelper.URand(minimum, maximum);

            RandomRoll randomRoll = new RandomRoll();
            randomRoll.Min = (int)minimum;
            randomRoll.Max = (int)maximum;
            randomRoll.Result = (int)roll;
            randomRoll.Roller = GetGUID();
            randomRoll.RollerWowAccount = GetSession().GetAccountGUID();

            Group group = GetGroup();
            if (group)
                group.BroadcastPacket(randomRoll, false);
            else
                SendPacket(randomRoll);

            return roll;
        }

        public bool IsVisibleGloballyFor(Player u)
        {
            if (u == null)
                return false;

            // Always can see self
            if (u.GetGUID() == GetGUID())
                return true;

            // Visible units, always are visible for all players
            if (IsVisible())
                return true;

            // GMs are visible for higher gms (or players are visible for gms)
            if (!Global.AccountMgr.IsPlayerAccount(u.GetSession().GetSecurity()))
                return GetSession().GetSecurity() <= u.GetSession().GetSecurity();

            // non faction visibility non-breakable for non-GMs
            return false;
        }

        public float GetReputationPriceDiscount(Creature creature)
        {
            return GetReputationPriceDiscount(creature.GetFactionTemplateEntry());
        }

        public float GetReputationPriceDiscount(FactionTemplateRecord factionTemplate)
        {
            if (factionTemplate == null || factionTemplate.Faction == 0)
                return 1.0f;

            ReputationRank rank = GetReputationRank(factionTemplate.Faction);
            if (rank <= ReputationRank.Neutral)
                return 1.0f;

            return 1.0f - 0.05f * (rank - ReputationRank.Neutral);
        }
        public bool IsSpellFitByClassAndRace(uint spell_id)
        {
            ulong racemask = getRaceMask();
            uint classmask = getClassMask();

            var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spell_id);

            if (bounds.Empty())
                return true;

            foreach (var _spell_idx in bounds)
            {
                // skip wrong race skills
                if (_spell_idx.RaceMask != 0 && (_spell_idx.RaceMask & racemask) == 0)
                    continue;

                // skip wrong class skills
                if (_spell_idx.ClassMask != 0 && (_spell_idx.ClassMask & classmask) == 0)
                    continue;

                return true;
            }

            return false;
        }

        //New shit
        void InitPrimaryProfessions()
        {
            SetFreePrimaryProfessions(WorldConfig.GetUIntValue(WorldCfg.MaxPrimaryTradeSkill));
        }
        public uint GetFreePrimaryProfessionPoints() { return GetUInt32Value(ActivePlayerFields.CharacterPoints); }
        void SetFreePrimaryProfessions(ushort profs) { SetUInt32Value(ActivePlayerFields.CharacterPoints, profs); }
        public bool HaveAtClient(WorldObject u)
        {
            bool one = u.GetGUID() == GetGUID();
            bool two = m_clientGUIDs.Contains(u.GetGUID());

            return one || two;
        }
        public bool HasTitle(CharTitlesRecord title) { return HasTitle(title.MaskID); }
        public bool HasTitle(uint bitIndex)
        {
            if (bitIndex > PlayerConst.MaxTitleIndex)
                return false;

            int fieldIndexOffset = (int)bitIndex / 32;
            uint flag = (uint)(1 << ((int)bitIndex % 32));
            return HasFlag(ActivePlayerFields.KnownTitles + fieldIndexOffset, flag);
        }
        public void SetTitle(CharTitlesRecord title, bool lost = false)
        {
            int fieldIndexOffset = (int)title.MaskID / 32;
            uint flag = (uint)(1 << (int)(title.MaskID % 32));

            if (lost)
            {
                if (!HasFlag(ActivePlayerFields.KnownTitles + fieldIndexOffset, flag))
                    return;

                RemoveFlag(ActivePlayerFields.KnownTitles + fieldIndexOffset, flag);
            }
            else
            {
                if (HasFlag(ActivePlayerFields.KnownTitles + fieldIndexOffset, flag))
                    return;

                SetFlag(ActivePlayerFields.KnownTitles + fieldIndexOffset, flag);
            }

            TitleEarned packet = new TitleEarned(lost ? ServerOpcodes.TitleLost : ServerOpcodes.TitleEarned);
            packet.Index = title.MaskID;
            SendPacket(packet);
        }

        public void SetViewpoint(WorldObject target, bool apply)
        {
            if (apply)
            {
                Log.outDebug(LogFilter.Maps, "Player.CreateViewpoint: Player {0} create seer {1} (TypeId: {2}).", GetName(), target.GetEntry(), target.GetTypeId());

                if (!AddGuidValue(ActivePlayerFields.Farsight, target.GetGUID()))
                {
                    Log.outFatal(LogFilter.Player, "Player.CreateViewpoint: Player {0} cannot add new viewpoint!", GetName());
                    return;

                }

                // farsight dynobj or puppet may be very far away
                UpdateVisibilityOf(target);

                if (target.isTypeMask(TypeMask.Unit) && target != GetVehicleBase())
                    target.ToUnit().AddPlayerToVision(this);
                SetSeer(target);
            }
            else
            {
                Log.outDebug(LogFilter.Maps, "Player.CreateViewpoint: Player {0} remove seer", GetName());

                if (!RemoveGuidValue(ActivePlayerFields.Farsight, target.GetGUID()))
                {
                    Log.outFatal(LogFilter.Player, "Player.CreateViewpoint: Player {0} cannot remove current viewpoint!", GetName());
                    return;
                }

                if (target.isTypeMask(TypeMask.Unit) && target != GetVehicleBase())
                    target.ToUnit().RemovePlayerFromVision(this);

                //must immediately set seer back otherwise may crash
                SetSeer(this);
            }
        }
        public WorldObject GetViewpoint()
        {
            ObjectGuid guid = GetGuidValue(ActivePlayerFields.Farsight);
            if (!guid.IsEmpty())
                return Global.ObjAccessor.GetObjectByTypeMask(this, guid, TypeMask.Seer);
            return null;
        }

        public void SetClientControl(Unit target, bool allowMove)
        {
            ControlUpdate packet = new ControlUpdate();
            packet.Guid = target.GetGUID();
            packet.On = allowMove;
            SendPacket(packet);

            if (this != target)
                SetViewpoint(target, allowMove);

            if (allowMove)
                SetMover(target);
        }
        public void SetMover(Unit target)
        {
            m_unitMovedByMe.m_playerMovingMe = null;
            m_unitMovedByMe = target;
            m_unitMovedByMe.m_playerMovingMe = this;

            MoveSetActiveMover packet = new MoveSetActiveMover();
            packet.MoverGUID = target.GetGUID();
            SendPacket(packet);
        }

        public Item GetWeaponForAttack(WeaponAttackType attackType, bool useable = false)
        {
            byte slot;
            switch (attackType)
            {
                case WeaponAttackType.BaseAttack:
                    slot = EquipmentSlot.MainHand;
                    break;
                case WeaponAttackType.OffAttack:
                    slot = EquipmentSlot.OffHand;
                    break;
                case WeaponAttackType.RangedAttack:
                    slot = EquipmentSlot.MainHand;
                    break;
                default:
                    return null;
            }

            Item item = null;
            if (useable)
                item = GetUseableItemByPos(InventorySlots.Bag0, slot);
            else
                item = GetItemByPos(InventorySlots.Bag0, slot);
            if (item == null || item.GetTemplate().GetClass() != ItemClass.Weapon)
                return null;

            if (!useable)
                return item;

            if (item.IsBroken())
                return null;

            return item;
        }
        public static WeaponAttackType GetAttackBySlot(byte slot, InventoryType inventoryType)
        {
            switch (slot)
            {
                case EquipmentSlot.MainHand:
                    return inventoryType != InventoryType.Ranged && inventoryType != InventoryType.RangedRight ? WeaponAttackType.BaseAttack : WeaponAttackType.RangedAttack;
                case EquipmentSlot.OffHand:
                    return WeaponAttackType.OffAttack;
                default:
                    return WeaponAttackType.Max;
            }
        }
        public void AutoUnequipOffhandIfNeed(bool force = false)
        {
            Item offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
            if (offItem == null)
                return;

            ItemTemplate offtemplate = offItem.GetTemplate();

            // unequip offhand weapon if player doesn't have dual wield anymore
            if (!CanDualWield() && ((offItem.GetTemplate().GetInventoryType() == InventoryType.WeaponOffhand && !offItem.GetTemplate().GetFlags3().HasAnyFlag(ItemFlags3.AlwaysAllowDualWield))
                    || offItem.GetTemplate().GetInventoryType() == InventoryType.Weapon))
                force = true;

            // need unequip offhand for 2h-weapon without TitanGrip (in any from hands)
            if (!force && (CanTitanGrip() || (offtemplate.GetInventoryType() != InventoryType.Weapon2Hand && !IsTwoHandUsed())))
                return;

            List<ItemPosCount> off_dest = new List<ItemPosCount>();
            InventoryResult off_msg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, off_dest, offItem, false);
            if (off_msg == InventoryResult.Ok)
            {
                RemoveItem(InventorySlots.Bag0, EquipmentSlot.OffHand, true);
                StoreItem(off_dest, offItem, true);
            }
            else
            {
                MoveItemFromInventory(InventorySlots.Bag0, EquipmentSlot.OffHand, true);
                SQLTransaction trans = new SQLTransaction();
                offItem.DeleteFromInventoryDB(trans);                   // deletes item from character's inventory
                offItem.SaveToDB(trans);                                // recursive and not have transaction guard into self, item not in inventory and can be save standalone

                string subject = Global.ObjectMgr.GetCypherString(CypherStrings.NotEquippedItem);
                new MailDraft(subject, "There were problems with equipping one or several items").AddItem(offItem).SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);

                DB.Characters.CommitTransaction(trans);
            }
        }

        public WorldLocation GetTeleportDest()
        {
            return teleportDest;
        }
        public WorldLocation GetHomebind()
        {
            return homebind;
        }
        public WorldLocation GetRecall()
        {
            return m_recall_location;
        }

        public bool CanTameExoticPets() { return IsGameMaster() || HasAuraType(AuraType.AllowTamePetType); }

        void SendAttackSwingDeadTarget() { SendPacket(new AttackSwingError(AttackSwingErr.DeadTarget)); }
        void SendAttackSwingCantAttack() { SendPacket(new AttackSwingError(AttackSwingErr.CantAttack)); }
        public void SendAttackSwingNotInRange() { SendPacket(new AttackSwingError(AttackSwingErr.NotInRange)); }
        void SendAttackSwingBadFacingAttack() { SendPacket(new AttackSwingError(AttackSwingErr.BadFacing)); }
        public void SendAttackSwingCancelAttack() { SendPacket(new CancelCombat()); }
        public void SendAutoRepeatCancel(Unit target)
        {
            CancelAutoRepeat cancelAutoRepeat = new CancelAutoRepeat();
            cancelAutoRepeat.Guid = target.GetGUID();                     // may be it's target guid
            SendMessageToSet(cancelAutoRepeat, false);
        }

        public override void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
        {
            if (target == this)
            {
                for (byte i = 0; i < EquipmentSlot.End; ++i)
                {
                    if (m_items[i] == null)
                        continue;

                    m_items[i].BuildCreateUpdateBlockForPlayer(data, target);
                }

                for (byte i = InventorySlots.BagStart; i < InventorySlots.BankBagEnd; ++i)
                {
                    if (m_items[i] == null)
                        continue;

                    m_items[i].BuildCreateUpdateBlockForPlayer(data, target);
                }

                for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
                {
                    if (m_items[i] == null)
                        continue;

                    m_items[i].BuildCreateUpdateBlockForPlayer(data, target);
                }

                for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
                {
                    if (m_items[i] == null)
                        continue;

                    m_items[i].BuildCreateUpdateBlockForPlayer(data, target);
                }
            }

            base.BuildCreateUpdateBlockForPlayer(data, target);
        }

        //Helpers
        public void ADD_GOSSIP_ITEM(GossipOptionIcon icon, string message, uint sender, uint action) { PlayerTalkClass.GetGossipMenu().AddMenuItem(-1, icon, message, sender, action, "", 0); }
        public void ADD_GOSSIP_ITEM_DB(uint menuId, uint menuItemId, uint sender, uint action) { PlayerTalkClass.GetGossipMenu().AddMenuItem(menuId, menuItemId, sender, action); }
        public void ADD_GOSSIP_ITEM_EXTENDED(GossipOptionIcon icon, string message, uint sender, uint action, string boxmessage, uint boxmoney, bool coded) { PlayerTalkClass.GetGossipMenu().AddMenuItem(-1, icon, message, sender, action, boxmessage, boxmoney, coded); }

        // This fuction Sends the current menu to show to client, a - NPCTEXTID(uint32), b - npc guid(uint64)
        public void SEND_GOSSIP_MENU(uint titleId, ObjectGuid objGUID) { PlayerTalkClass.SendGossipMenu(titleId, objGUID); }

        // Closes the Menu
        public void CLOSE_GOSSIP_MENU() { PlayerTalkClass.SendCloseGossip(); }
    }
}
