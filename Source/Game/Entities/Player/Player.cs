// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Game.Achievements;
using Game.AI;
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.BattlePets;
using Game.Chat;
using Game.DataStorage;
using Game.Garrisons;
using Game.Groups;
using Game.Guilds;
using Game.Loots;
using Game.Mails;
using Game.Maps;
using Game.Maps.Dos;
using Game.Maps.Notifiers;
using Game.Misc;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;
using Group = Game.Groups.Group;

namespace Game.Entities
{
    public partial class Player : Unit
    {
        public Player(WorldSession session) : base(true)
        {
            ObjectTypeMask |= TypeMask.Player;
            ObjectTypeId = TypeId.Player;

            PlayerData = new PlayerData();
            ActivePlayerData = new ActivePlayerData();

            _session = session;

            // players always accept
            if (!GetSession().HasPermission(RBACPermissions.CanFilterWhispers))
                SetAcceptWhispers(true);

            _zoneUpdateId = 0xffffffff;
            _nextSave = WorldConfig.GetUIntValue(WorldCfg.IntervalSave);
            _customizationsChanged = false;

            SetGroupInvite(null);

            AtLoginFlags = AtLoginFlags.None;
            PlayerTalkClass = new PlayerMenu(session);
            _currentBuybackSlot = InventorySlots.BuyBackStart;

            for (byte i = 0; i < (int)MirrorTimerType.Max; i++)
                _mirrorTimer[i] = -1;

            _logintime = GameTime.GetGameTime();
            _last_tick = _logintime;

            _dungeonDifficulty = Difficulty.Normal;
            _raidDifficulty = Difficulty.NormalRaid;
            _legacyRaidDifficulty = Difficulty.Raid10N;
            InstanceValid = true;

            _specializationInfo = new SpecializationInfo();

            for (byte i = 0; i < (byte)BaseModGroup.End; ++i)
            {
                _auraBaseFlatMod[i] = 0.0f;
                _auraBasePctMod[i] = 1.0f;
            }

            for (var i = 0; i < (int)SpellModOp.Max; ++i)
            {
                _spellMods[i] = new List<SpellModifier>[(int)SpellModType.End];

                for (var c = 0; c < (int)SpellModType.End; ++c)
                    _spellMods[i][c] = new List<SpellModifier>();
            }

            // Honor System
            _lastHonorUpdateTime = GameTime.GetGameTime();

            UnitMovedByMe = this;
            PlayerMovingMe = this;
            SeerView = this;

            IsActive = true;
            ControlledByPlayer = true;

            Global.WorldMgr.IncreasePlayerCount();

            _cinematicMgr = new CinematicManager(this);

            _achievementSys = new PlayerAchievementMgr(this);
            _reputationMgr = new ReputationMgr(this);
            _questObjectiveCriteriaMgr = new QuestObjectiveCriteriaManager(this);
            _sceneMgr = new SceneMgr(this);

            _bgBattlegroundQueueID[0] = new BgBattlegroundQueueID_Rec();
            _bgBattlegroundQueueID[1] = new BgBattlegroundQueueID_Rec();

            _bgData = new BGData();

            _restMgr = new RestMgr(this);

            _groupUpdateTimer = new TimeTracker(5000);

            // Adds the extra bag slots for having an authenticator.
            if (ConfigMgr.GetDefaultValue("player.enableExtaBagSlots", false) &&
                !HasPlayerLocalFlag(PlayerLocalFlags.AccountSecured))
                SetPlayerLocalFlag(PlayerLocalFlags.AccountSecured);
        }

        public override void Dispose()
        {
            // Note: buy back Item already deleted from DB when player was saved
            for (byte i = 0; i < (int)PlayerSlots.Count; ++i)
                _items[i]?.Dispose();

            _spells.Clear();
            _specializationInfo = null;
            _mail.Clear();

            foreach (var item in _mMitems.Values)
                item.Dispose();

            PlayerTalkClass.ClearMenus();
            ItemSetEff.Clear();

            _declinedname = null;
            _runes = null;
            _achievementSys = null;
            _reputationMgr = null;

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
                Log.outError(LogFilter.Player,
                             "PlayerCreate: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with an invalid race/class pair ({2}/{3}) - refusing to do so.",
                             GetSession().GetAccountId(),
                             GetName(),
                             createInfo.RaceId,
                             createInfo.ClassId);

                return false;
            }

            var cEntry = CliDB.ChrClassesStorage.LookupByKey(createInfo.ClassId);

            if (cEntry == null)
            {
                Log.outError(LogFilter.Player,
                             "PlayerCreate: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with an invalid character class ({2}) - refusing to do so (wrong DBC-files?)",
                             GetSession().GetAccountId(),
                             GetName(),
                             createInfo.ClassId);

                return false;
            }

            if (!GetSession().ValidateAppearance(createInfo.RaceId, createInfo.ClassId, createInfo.Sex, createInfo.Customizations))
            {
                Log.outError(LogFilter.Player,
                             "Player.Create: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with invalid appearance attributes - refusing to do so",
                             GetSession().GetAccountId(),
                             GetName());

                return false;
            }

            var position = createInfo.UseNPE && info.CreatePositionNPE.HasValue ? info.CreatePositionNPE.Value : info.CreatePositionInfo;

            _createTime = GameTime.GetGameTime();
            _createMode = createInfo.UseNPE && info.CreatePositionNPE.HasValue ? PlayerCreateMode.NPE : PlayerCreateMode.Normal;

            Relocate(position.Loc);

            SetMap(Global.MapMgr.CreateMap(position.Loc.GetMapId(), this));

            if (position.TransportGuid.HasValue)
            {
                Transport transport = ObjectAccessor.GetTransport(this, ObjectGuid.Create(HighGuid.Transport, position.TransportGuid.Value));

                if (transport != null)
                {
                    transport.AddPassenger(this);
                    MovementInfo.Transport.Pos.Relocate(position.Loc);
                    position.Loc.GetPosition(out float x, out float y, out float z, out float o);
                    transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                    Relocate(x, y, z, o);
                }
            }

            // set initial _homebind position
            SetHomebind(this, GetAreaId());

            PowerType powertype = cEntry.DisplayPower;

            SetObjectScale(1.0f);

            SetFactionForRace(createInfo.RaceId);

            if (!IsValidGender(createInfo.Sex))
            {
                Log.outError(LogFilter.Player,
                             "Player:Create: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with an invalid Gender ({2}) - refusing to do so",
                             GetSession().GetAccountId(),
                             GetName(),
                             createInfo.Sex);

                return false;
            }

            SetRace(createInfo.RaceId);
            SetClass(createInfo.ClassId);
            SetGender(createInfo.Sex);
            SetPowerType(powertype, false);
            InitDisplayIds();

            if ((RealmType)WorldConfig.GetIntValue(WorldCfg.GameType) == RealmType.PVP ||
                (RealmType)WorldConfig.GetIntValue(WorldCfg.GameType) == RealmType.RPPVP)
            {
                SetPvpFlag(UnitPVPStateFlags.PvP);
                SetUnitFlag(UnitFlags.PlayerControlled);
            }

            SetUnitFlag2(UnitFlags2.RegeneratePower);
            SetHoverHeight(1.0f); // default for players in 3.0.3

            SetWatchedFactionIndex(0xFFFFFFFF);

            SetCustomizations(createInfo.Customizations);
            SetRestState(RestTypes.XP, ((GetSession().IsARecruiter() || GetSession().GetRecruiterId() != 0) ? PlayerRestState.RAFLinked : PlayerRestState.Normal));
            SetRestState(RestTypes.Honor, PlayerRestState.Normal);
            SetNativeGender(createInfo.Sex);
            SetInventorySlotCount(InventorySlots.DefaultSize);

            // set starting level
            SetLevel(GetStartLevel(createInfo.RaceId, createInfo.ClassId, createInfo.TemplateSet));

            InitRunes();

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Coinage), (ulong)WorldConfig.GetIntValue(WorldCfg.StartPlayerMoney));
            SetCreateCurrency(CurrencyTypes.ApexisCrystals, WorldConfig.GetUIntValue(WorldCfg.CurrencyStartApexisCrystals));
            SetCreateCurrency(CurrencyTypes.JusticePoints, WorldConfig.GetUIntValue(WorldCfg.CurrencyStartJusticePoints));

            // Played Time
            _last_tick = GameTime.GetGameTime();
            _playedTimeTotal = 0;
            _playedTimeLevel = 0;

            // base Stats and related field values
            InitStatsForLevel();
            InitTaxiNodesForLevel();
            InitTalentForLevel();
            InitializeSkillFields();
            InitPrimaryProfessions(); // to max set before any spell added

            // apply original Stats mods before spell loading or Item equipment that call before equip _RemoveStatsMods()
            UpdateMaxHealth(); // Update max Health (for add bonus from stamina)
            SetFullHealth();
            SetFullPower(PowerType.Mana);

            // original spells
            LearnDefaultSkills();
            LearnCustomSpells();

            // Original Action bar. Do not use Player.AddActionButton because we do not have skill spells loaded at this Time
            // but checks will still be performed later when loading character from db in Player._LoadActions
            foreach (var action in info.Action)
            {
                // create new Button
                ActionButton ab = new();

                // set _data
                ab.SetActionAndType(action.Action, (ActionButtonType)action.Type);

                _actionButtons[action.Button] = ab;
            }

            // original items
            foreach (PlayerCreateInfoItem initialItem in info.Item)
                StoreNewItemInBestSlots(initialItem.ItemId, initialItem.ItemAmount, info.ItemContext);

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
                        List<ItemPosCount> sDest = new();
                        msg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, sDest, pItem, false);

                        if (msg == InventoryResult.Ok)
                        {
                            RemoveItem(InventorySlots.Bag0, i, true);
                            StoreItem(sDest, pItem, true);
                        }
                    }
                }
            }
            // all Item positions resolved

            ChrSpecializationRecord defaultSpec = Global.DB2Mgr.GetDefaultChrSpecializationForClass(GetClass());

            if (defaultSpec != null)
            {
                SetActiveTalentGroup(defaultSpec.OrderIndex);
                SetPrimarySpecialization(defaultSpec.Id);
            }

            GetThreatManager().Initialize();

            // Adds the extra bag slots for having an authenticator.
            if (ConfigMgr.GetDefaultValue("player.enableExtaBagSlots", false) &&
                !HasPlayerLocalFlag(PlayerLocalFlags.AccountSecured))
                SetPlayerLocalFlag(PlayerLocalFlags.AccountSecured);

            return true;
        }

        public override void Update(uint diff)
        {
            if (!IsInWorld)
                return;

            // undelivered mail
            if (_nextMailDelivereTime != 0 &&
                _nextMailDelivereTime <= GameTime.GetGameTime())
            {
                SendNewMail();
                ++UnReadMails;

                // It will be recalculate at mailbox open (for UnReadMails important non-0 until mailbox open, it also will be recalculated)
                _nextMailDelivereTime = 0;
            }

            // Update cinematic location, if 500ms have passed and we're doing a cinematic now.
            _cinematicMgr._cinematicDiff += diff;

            if (_cinematicMgr._cinematicCamera != null &&
                _cinematicMgr._activeCinematic != null &&
                Time.GetMSTimeDiffToNow(_cinematicMgr._lastCinematicCheck) > 500)
            {
                _cinematicMgr._lastCinematicCheck = GameTime.GetGameTimeMS();
                _cinematicMgr.UpdateCinematicLocation(diff);
            }

            //used to implement delayed far teleports
            SetCanDelayTeleport(true);
            base.Update(diff);
            SetCanDelayTeleport(false);

            long now = GameTime.GetGameTime();

            UpdatePvPFlag(now);

            UpdateContestedPvP(diff);

            UpdateDuelFlag(now);

            CheckDuelDistance(now);

            UpdateAfkReport(now);

            if (GetCombatManager().HasPvPCombat()) // Only set when in pvp combat
            {
                Aura aura = GetAura(PlayerConst.SpellPvpRulesEnabled);

                if (aura != null)
                    if (!aura.IsPermanent())
                        aura.SetDuration(aura.GetSpellInfo().GetMaxDuration());
            }

            AIUpdateTick(diff);

            // Update items that have just a limited Lifetime
            if (now > _last_tick)
                UpdateItemDuration((uint)(now - _last_tick));

            // check every second
            if (now > _last_tick + 1)
                UpdateSoulboundTradeItems();

            // If mute expired, remove it from the DB
            if (GetSession()._muteTime != 0 &&
                GetSession()._muteTime < now)
            {
                GetSession()._muteTime = 0;
                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_MUTE_TIME);
                stmt.AddValue(0, 0); // Set the mute Time to 0
                stmt.AddValue(1, "");
                stmt.AddValue(2, "");
                stmt.AddValue(3, GetSession().GetAccountId());
                DB.Login.Execute(stmt);
            }

            if (!_timedquests.Empty())
                foreach (var id in _timedquests)
                {
                    QuestStatusData q_status = _questStatus[id];

                    if (q_status.Timer <= diff)
                    {
                        FailQuest(id);
                    }
                    else
                    {
                        q_status.Timer -= diff;
                        _questStatusSave[id] = QuestSaveType.Default;
                    }
                }

            _achievementSys.UpdateTimedCriteria(diff);

            if (HasUnitState(UnitState.MeleeAttacking) &&
                !HasUnitState(UnitState.Casting | UnitState.Charging))
            {
                Unit victim = GetVictim();

                if (victim != null)
                {
                    // default combat reach 10
                    // TODO add weapon, skill check

                    if (IsAttackReady(WeaponAttackType.BaseAttack))
                    {
                        if (!IsWithinMeleeRange(victim))
                        {
                            SetAttackTimer(WeaponAttackType.BaseAttack, 100);

                            if (_swingErrorMsg != 1) // send single Time (client auto repeat)
                            {
                                SendAttackSwingNotInRange();
                                _swingErrorMsg = 1;
                            }
                        }
                        //120 degrees of radiant range, if player is not in boundary radius
                        else if (!IsWithinBoundaryRadius(victim) &&
                                 !HasInArc(2 * MathFunctions.PI / 3, victim))
                        {
                            SetAttackTimer(WeaponAttackType.BaseAttack, 100);

                            if (_swingErrorMsg != 2) // send single Time (client auto repeat)
                            {
                                SendAttackSwingBadFacingAttack();
                                _swingErrorMsg = 2;
                            }
                        }
                        else
                        {
                            _swingErrorMsg = 0; // reset swing error State

                            // prevent base and off attack in same Time, delay attack at 0.2 sec
                            if (HaveOffhandWeapon())
                                if (GetAttackTimer(WeaponAttackType.OffAttack) < SharedConst.AttackDisplayDelay)
                                    SetAttackTimer(WeaponAttackType.OffAttack, SharedConst.AttackDisplayDelay);

                            // do attack
                            AttackerStateUpdate(victim, WeaponAttackType.BaseAttack);
                            ResetAttackTimer(WeaponAttackType.BaseAttack);
                        }
                    }

                    if (!IsInFeralForm() &&
                        HaveOffhandWeapon() &&
                        IsAttackReady(WeaponAttackType.OffAttack))
                    {
                        if (!IsWithinMeleeRange(victim))
                        {
                            SetAttackTimer(WeaponAttackType.OffAttack, 100);
                        }
                        else if (!IsWithinBoundaryRadius(victim) &&
                                 !HasInArc(2 * MathFunctions.PI / 3, victim))
                        {
                            SetAttackTimer(WeaponAttackType.BaseAttack, 100);
                        }
                        else
                        {
                            // prevent base and off attack in same Time, delay attack at 0.2 sec
                            if (GetAttackTimer(WeaponAttackType.BaseAttack) < SharedConst.AttackDisplayDelay)
                                SetAttackTimer(WeaponAttackType.BaseAttack, SharedConst.AttackDisplayDelay);

                            // do attack
                            AttackerStateUpdate(victim, WeaponAttackType.OffAttack);
                            ResetAttackTimer(WeaponAttackType.OffAttack);
                        }
                    }
                }
            }

            if (HasPlayerFlag(PlayerFlags.Resting))
                _restMgr.Update(diff);

            if (_weaponChangeTimer > 0)
            {
                if (diff >= _weaponChangeTimer)
                    _weaponChangeTimer = 0;
                else
                    _weaponChangeTimer -= diff;
            }

            if (_zoneUpdateTimer > 0)
            {
                if (diff >= _zoneUpdateTimer)
                {
                    // On zone update tick check if we are still in an inn if we are supposed to be in one
                    if (_restMgr.HasRestFlag(RestFlag.Tavern))
                    {
                        AreaTriggerRecord atEntry = CliDB.AreaTriggerStorage.LookupByKey(_restMgr.GetInnTriggerId());

                        if (atEntry == null ||
                            !IsInAreaTriggerRadius(atEntry))
                            _restMgr.RemoveRestFlag(RestFlag.Tavern);
                    }

                    uint newzone, newarea;
                    GetZoneAndAreaId(out newzone, out newarea);

                    if (_zoneUpdateId != newzone)
                    {
                        UpdateZone(newzone, newarea); // also update area
                    }
                    else
                    {
                        // use area updates as well
                        // needed for free far all arenas for example
                        if (_areaUpdateId != newarea)
                            UpdateArea(newarea);

                        _zoneUpdateTimer = 1 * Time.InMilliseconds;
                    }
                }
                else
                {
                    _zoneUpdateTimer -= diff;
                }
            }

            if (IsAlive())
            {
                RegenTimer += diff;
                RegenerateAll();
            }

            if (DeathState == DeathState.JustDied)
                KillPlayer();

            if (_nextSave > 0)
            {
                if (diff >= _nextSave)
                {
                    // _nextSave reset in SaveToDB call
                    Global.ScriptMgr.ForEach<IPlayerOnSave>(p => p.OnSave(this));
                    SaveToDB();
                    Log.outDebug(LogFilter.Player, "Player '{0}' (GUID: {1}) saved", GetName(), GetGUID().ToString());
                }
                else
                {
                    _nextSave -= diff;
                }
            }

            //Handle Water/drowning
            HandleDrowning(diff);

            // Played Time
            if (now > _last_tick)
            {
                uint elapsed = (uint)(now - _last_tick);
                _playedTimeTotal += elapsed;
                _playedTimeLevel += elapsed;
                _last_tick = now;
            }

            if (GetDrunkValue() != 0)
            {
                _drunkTimer += diff;

                if (_drunkTimer > 9 * Time.InMilliseconds)
                    HandleSobering();
            }

            if (HasPendingBind())
            {
                if (_pendingBindTimer <= diff)
                {
                    // Player left the instance
                    if (_pendingBindId == GetInstanceId())
                        ConfirmPendingBind();

                    SetPendingBind(0, 0);
                }
                else
                {
                    _pendingBindTimer -= diff;
                }
            }

            // not auto-free ghost from body in instances
            if (_deathTimer > 0 &&
                !GetMap().Instanceable() &&
                !HasAuraType(AuraType.PreventResurrection))
            {
                if (diff >= _deathTimer)
                {
                    _deathTimer = 0;
                    BuildPlayerRepop();
                    RepopAtGraveyard();
                }
                else
                {
                    _deathTimer -= diff;
                }
            }

            UpdateEnchantTime(diff);
            UpdateHomebindTime(diff);

            if (!_instanceResetTimes.Empty())
                foreach (var instance in _instanceResetTimes.ToList())
                    if (instance.Value < now)
                        _instanceResetTimes.Remove(instance.Key);

            // group update
            _groupUpdateTimer.Update(diff);

            if (_groupUpdateTimer.Passed())
            {
                SendUpdateToOutOfRangeGroupMembers();
                _groupUpdateTimer.Reset(5000);
            }

            Pet pet = GetPet();

            if (pet != null &&
                !pet.IsWithinDistInMap(this, GetMap().GetVisibilityRange()) &&
                !pet.IsPossessed())
                RemovePet(pet, PetSaveMode.NotInSlot, true);

            if (IsAlive())
            {
                if (_hostileReferenceCheckTimer <= diff)
                {
                    _hostileReferenceCheckTimer = 15 * Time.InMilliseconds;

                    if (!GetMap().IsDungeon())
                        GetCombatManager().EndCombatBeyondRange(GetVisibilityRange(), true);
                }
                else
                {
                    _hostileReferenceCheckTimer -= diff;
                }
            }

            //we should execute delayed teleports only for alive(!) players
            //because we don't want player's ghost teleported from graveyard
            if (IsHasDelayedTeleport() &&
                IsAlive())
                TeleportTo(_teleportDest, _teleport_options);
        }

        public override void SetDeathState(DeathState s)
        {
            bool oldIsAlive = IsAlive();

            if (s == DeathState.JustDied)
            {
                if (!oldIsAlive)
                {
                    Log.outError(LogFilter.Player, "Player.setDeathState: Attempted to kill a dead player '{0}' ({1})", GetName(), GetGUID().ToString());

                    return;
                }

                // drunken State is cleared on death
                SetDrunkValue(0);
                // lost combo points at any Target (targeted combo points clear in Unit::setDeathState)
                ClearComboPoints();

                ClearResurrectRequestData();

                //FIXME: is pet dismissed at dying or releasing spirit? if second, add setDeathState(DEAD) to HandleRepopRequestOpcode and define pet unsummon here with (s == DEAD)
                RemovePet(null, PetSaveMode.NotInSlot, true);

                InitializeSelfResurrectionSpells();

                UpdateCriteria(CriteriaType.DieOnMap, 1);
                UpdateCriteria(CriteriaType.DieAnywhere, 1);
                UpdateCriteria(CriteriaType.DieInInstance, 1);

                // reset all death criterias
                ResetCriteria(CriteriaFailEvent.Death, 0);
            }

            base.SetDeathState(s);

            if (IsAlive() &&
                !oldIsAlive)
                //clear aura case after resurrection by another way (spells will be applied before next death)
                ClearSelfResSpell();
        }

        public override void DestroyForPlayer(Player target)
        {
            base.DestroyForPlayer(target);

            if (target == this)
            {
                for (byte i = EquipmentSlot.Start; i < InventorySlots.BankBagEnd; ++i)
                {
                    if (_items[i] == null)
                        continue;

                    _items[i].DestroyForPlayer(target);
                }

                for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
                {
                    if (_items[i] == null)
                        continue;

                    _items[i].DestroyForPlayer(target);
                }
            }
        }

        public override void CleanupsBeforeDelete(bool finalCleanup = true)
        {
            TradeCancel(false);
            DuelComplete(DuelCompleteType.Interrupted);

            base.CleanupsBeforeDelete(finalCleanup);

            GetTransport()?.RemovePassenger(this);
        }

        public override void AddToWorld()
        {
            // Do not add/remove the player from the object storage
            // It will crash when updating the ObjectAccessor
            // The player should only be added when logging in
            base.AddToWorld();

            for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
                _items[i]?.AddToWorld();
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
                ClearComboPoints();
                GetSession().DoLootReleaseAll();
                _lootRolls.Clear();
                Global.OutdoorPvPMgr.HandlePlayerLeaveZone(this, _zoneUpdateId);
                Global.BattleFieldMgr.HandlePlayerLeaveZone(this, _zoneUpdateId);
            }

            // Remove items from world before self - player must be found in Item.RemoveFromObjectUpdate
            for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
                _items[i]?.RemoveFromWorld();

            // Do not add/remove the player from the object storage
            // It will crash when updating the ObjectAccessor
            // The player should only be removed when logging out
            base.RemoveFromWorld();

            WorldObject viewpoint = GetViewpoint();

            if (viewpoint != null)
            {
                Log.outError(LogFilter.Player,
                             "Player {0} has viewpoint {1} {2} when removed from world",
                             GetName(),
                             viewpoint.GetEntry(),
                             viewpoint.GetTypeId());

                SetViewpoint(viewpoint, false);
            }

            RemovePlayerLocalFlag(PlayerLocalFlags.OverrideTransportServerTime);
            SetTransportServerTime(0);
        }

        public void ProcessDelayedOperations()
        {
            if (_delayedOperations == 0)
                return;

            if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.ResurrectPlayer))
                ResurrectUsingRequestDataImpl();

            if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.SavePlayer))
                SaveToDB();

            if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.SpellCastDeserter))
                CastSpell(this, 26013, true); // Deserter

            if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.BGMountRestore))
                if (_bgData.MountSpell != 0)
                {
                    CastSpell(this, _bgData.MountSpell, true);
                    _bgData.MountSpell = 0;
                }

            if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.BGTaxiRestore))
                if (_bgData.HasTaxiPath())
                {
                    Taxi.AddTaxiDestination(_bgData.TaxiPath[0]);
                    Taxi.AddTaxiDestination(_bgData.TaxiPath[1]);
                    _bgData.ClearTaxiPath();

                    ContinueTaxiFlight();
                }

            if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.BGGroupRestore))
            {
                Group g = GetGroup();

                g?.SendUpdateToPlayer(GetGUID());
            }

            //we have executed ALL delayed ops, so clear the flag
            _delayedOperations = 0;
        }

        public override bool IsLoading()
        {
            return GetSession().PlayerLoading();
        }

        //Network
        public void SendPacket(ServerPacket data)
        {
            _session.SendPacket(data);
        }

        public DeclinedName GetDeclinedNames()
        {
            return _declinedname;
        }

        public void CreateGarrison(uint garrSiteId)
        {
            _garrison = new Garrison(this);

            if (!_garrison.Create(garrSiteId))
                _garrison = null;
        }

        public Garrison GetGarrison()
        {
            return _garrison;
        }

        public SceneMgr GetSceneMgr()
        {
            return _sceneMgr;
        }

        public RestMgr GetRestMgr()
        {
            return _restMgr;
        }

        public bool IsAdvancedCombatLoggingEnabled()
        {
            return _advancedCombatLoggingEnabled;
        }

        public void SetAdvancedCombatLogging(bool enabled)
        {
            _advancedCombatLoggingEnabled = enabled;
        }

        public void SetInvSlot(uint slot, ObjectGuid guid)
        {
            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.InvSlots, (int)slot), guid);
        }

        //Taxi
        public void InitTaxiNodesForLevel()
        {
            Taxi.InitTaxiNodesForLevel(GetRace(), GetClass(), GetLevel());
        }

        //Cheat Commands
        public bool GetCommandStatus(PlayerCommandStates command)
        {
            return (_activeCheats & command) != 0;
        }

        public void SetCommandStatusOn(PlayerCommandStates command)
        {
            _activeCheats |= command;
        }

        public void SetCommandStatusOff(PlayerCommandStates command)
        {
            _activeCheats &= ~command;
        }

        //Pet - Summons - Vehicles
        public PetStable GetPetStable()
        {
            return _petStable;
        }

        public PetStable GetOrInitPetStable()
        {
            if (_petStable == null)
                _petStable = new PetStable();

            return _petStable;
        }

        // last used pet number (for BG's)
        public uint GetLastPetNumber()
        {
            return _lastpetnumber;
        }

        public void SetLastPetNumber(uint petnumber)
        {
            _lastpetnumber = petnumber;
        }

        public uint GetTemporaryUnsummonedPetNumber()
        {
            return _temporaryUnsummonedPetNumber;
        }

        public void SetTemporaryUnsummonedPetNumber(uint petnumber)
        {
            _temporaryUnsummonedPetNumber = petnumber;
        }

        public void UnsummonPetTemporaryIfAny()
        {
            Pet pet = GetPet();

            if (!pet)
                return;

            if (_temporaryUnsummonedPetNumber == 0 &&
                pet.IsControlled() &&
                !pet.IsTemporarySummoned())
            {
                _temporaryUnsummonedPetNumber = pet.GetCharmInfo().GetPetNumber();
                _oldpetspell = pet.UnitData.CreatedBySpell;
            }

            RemovePet(pet, PetSaveMode.AsCurrent);
        }

        public void ResummonPetTemporaryUnSummonedIfAny()
        {
            if (_temporaryUnsummonedPetNumber == 0)
                return;

            // not resummon in not appropriate State
            if (IsPetNeedBeTemporaryUnsummoned())
                return;

            if (!GetPetGUID().IsEmpty())
                return;

            Pet NewPet = new(this);
            NewPet.LoadPetFromDB(this, 0, _temporaryUnsummonedPetNumber, true);

            _temporaryUnsummonedPetNumber = 0;
        }

        public bool IsPetNeedBeTemporaryUnsummoned()
        {
            return !IsInWorld || !IsAlive() || IsMounted();
        }

        public void SendRemoveControlBar()
        {
            SendPacket(new PetSpells());
        }

        public Creature GetSummonedBattlePet()
        {
            Creature summonedBattlePet = ObjectAccessor.GetCreatureOrPetOrVehicle(this, GetCritterGUID());

            if (summonedBattlePet != null)
                if (!GetSummonedBattlePetGUID().IsEmpty() &&
                    GetSummonedBattlePetGUID() == summonedBattlePet.GetBattlePetCompanionGUID())
                    return summonedBattlePet;

            return null;
        }

        public void SetBattlePetData(BattlePet pet = null)
        {
            if (pet != null)
            {
                SetSummonedBattlePetGUID(pet.PacketInfo.Guid);
                SetCurrentBattlePetBreedQuality(pet.PacketInfo.Quality);
                SetBattlePetCompanionExperience(pet.PacketInfo.Exp);
                SetWildBattlePetLevel(pet.PacketInfo.Level);
            }
            else
            {
                SetSummonedBattlePetGUID(ObjectGuid.Empty);
                SetCurrentBattlePetBreedQuality((byte)BattlePetBreedQuality.Poor);
                SetBattlePetCompanionExperience(0);
                SetWildBattlePetLevel(0);
            }
        }

        public void StopCastingCharm()
        {
            Unit charm = GetCharmed();

            if (!charm)
                return;

            if (charm.IsTypeId(TypeId.Unit))
            {
                if (charm.ToCreature().HasUnitTypeMask(UnitTypeMask.Puppet))
                {
                    ((Puppet)charm).UnSummon();
                }
                else if (charm.IsVehicle())
                {
                    ExitVehicle();

                    // Temporary for issue https://github.com/TrinityCore/TrinityCore/issues/24876
                    if (!GetCharmedGUID().IsEmpty() &&
                        !charm.HasAuraTypeWithCaster(AuraType.ControlVehicle, GetGUID()))
                    {
                        Log.outFatal(LogFilter.Player, $"Player::StopCastingCharm Player '{GetName()}' ({GetGUID()}) is not able to uncharm vehicle ({GetCharmedGUID()}) because of missing SPELL_AURA_CONTROL_VEHICLE");

                        // attempt to recover from missing HandleAuraControlVehicle unapply handling
                        // THIS IS A HACK, NEED TO FIND HOW IS IT EVEN POSSBLE TO NOT HAVE THE AURA
                        _ExitVehicle();
                    }
                }
            }

            if (!GetCharmedGUID().IsEmpty())
                charm.RemoveCharmAuras();

            if (!GetCharmedGUID().IsEmpty())
            {
                Log.outFatal(LogFilter.Player, "Player {0} (GUID: {1} is not able to uncharm unit (GUID: {2} Entry: {3}, Type: {4})", GetName(), GetGUID(), GetCharmedGUID(), charm.GetEntry(), charm.GetTypeId());

                if (!charm.GetCharmerGUID().IsEmpty())
                {
                    Log.outFatal(LogFilter.Player, $"Player::StopCastingCharm: Charmed unit has charmer {charm.GetCharmerGUID()}\nPlayer debug info: {GetDebugInfo()}\nCharm debug info: {charm.GetDebugInfo()}");
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

            PetSpells petSpells = new();
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
            Unit charm = GetCharmed();

            if (!charm)
                return;

            CharmInfo charmInfo = charm.GetCharmInfo();

            if (charmInfo == null)
            {
                Log.outError(LogFilter.Player, "Player:PossessSpellInitialize(): charm ({0}) has no charminfo!", charm.GetGUID());

                return;
            }

            PetSpells petSpellsPacket = new();
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

            PetSpells petSpells = new();
            petSpells.PetGUID = vehicle.GetGUID();
            petSpells.CreatureFamily = 0; // Pet Family (0 for all vehicles)
            petSpells.Specialization = 0;
            petSpells.TimeLimit = vehicle.IsSummon() ? vehicle.ToTempSummon().GetTimer() : 0;
            petSpells.ReactState = vehicle.GetReactState();
            petSpells.CommandState = CommandStates.Follow;
            petSpells.Flag = 0x8;

            for (uint i = 0; i < SharedConst.MaxSpellControlBar; ++i)
                petSpells.ActionButtons[i] = UnitActionBarEntry.MAKE_UNIT_ACTION_BUTTON(0, i + 8);

            for (uint i = 0; i < SharedConst.MaxCreatureSpells; ++i)
            {
                uint spellId = vehicle.Spells[i];
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetMap().GetDifficultyID());

                if (spellInfo == null)
                    continue;

                if (spellInfo.HasAttribute(SpellAttr5.NotAvailableWhileCharmed))
                    continue;

                if (!Global.ConditionMgr.IsObjectMeetingVehicleSpellConditions(vehicle.GetEntry(), spellId, this, vehicle))
                {
                    Log.outDebug(LogFilter.Condition, "VehicleSpellInitialize: conditions not met for Vehicle entry {0} spell {1}", vehicle.ToCreature().GetEntry(), spellId);

                    continue;
                }

                if (spellInfo.IsPassive())
                    vehicle.CastSpell(vehicle, spellInfo.Id, true);

                petSpells.ActionButtons[i] = UnitActionBarEntry.MAKE_UNIT_ACTION_BUTTON(spellId, i + 8);
            }

            // Cooldowns
            vehicle.GetSpellHistory().WritePacket(petSpells);

            SendPacket(petSpells);
        }

        public uint GetCurrency(uint id)
        {
            var playerCurrency = _currencyStorage.LookupByKey(id);

            if (playerCurrency == null)
                return 0;

            return playerCurrency.Quantity;
        }

        public void ModifyCurrency(uint id, int count, bool printLog = true, bool ignoreMultipliers = false)
        {
            if (count == 0)
                return;

            CurrencyTypesRecord currency = CliDB.CurrencyTypesStorage.LookupByKey(id);
            Cypher.Assert(currency != null);

            if (!ignoreMultipliers)
                count *= (int)GetTotalAuraMultiplierByMiscValue(AuraType.ModCurrencyGain, (int)id);

            // Currency that is immediately converted into reputation with that faction instead
            FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(currency.FactionID);

            if (factionEntry != null)
            {
                if (currency.Flags[0].HasAnyFlag((int)CurrencyFlags.HighPrecision))
                    count /= 100;

                GetReputationMgr().ModifyReputation(factionEntry, count, false, true);

                return;
            }

            if (id == (uint)CurrencyTypes.Azerite)
            {
                if (count > 0)
                {
                    Item heartOfAzeroth = GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

                    heartOfAzeroth?.ToAzeriteItem().GiveXP((ulong)count);
                }

                return;
            }

            uint oldTotalCount = 0;
            uint oldWeekCount = 0;
            uint oldTrackedCount = 0;

            var playerCurrency = _currencyStorage.LookupByKey(id);

            if (playerCurrency == null)
            {
                PlayerCurrency cur = new();
                cur.State = PlayerCurrencyState.New;
                cur.Quantity = 0;
                cur.WeeklyQuantity = 0;
                cur.TrackedQuantity = 0;
                cur.Flags = 0;
                _currencyStorage[id] = cur;
                playerCurrency = _currencyStorage.LookupByKey(id);
            }
            else
            {
                oldTotalCount = playerCurrency.Quantity;
                oldWeekCount = playerCurrency.WeeklyQuantity;
                oldTrackedCount = playerCurrency.TrackedQuantity;
            }

            // Count can't be more then weekCap if used (weekCap > 0)
            uint weekCap = GetCurrencyWeekCap(currency);

            if (weekCap != 0 &&
                count > weekCap)
                count = (int)weekCap;

            // Count can't be more then totalCap if used (totalCap > 0)
            uint totalCap = GetCurrencyTotalCap(currency);

            if (totalCap != 0 &&
                count > totalCap)
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
            if (weekCap != 0 &&
                weekCap < newWeekCount)
            {
                newWeekCount = (int)weekCap;
                // weekCap - oldWeekCount always >= 0 as we set limit before!
                newTotalCount = (int)(oldTotalCount + (weekCap - oldWeekCount));
            }

            // if we get more then totalCap set to maximum;
            if (totalCap != 0 &&
                totalCap < newTotalCount)
            {
                newTotalCount = (int)totalCap;
                newWeekCount = (int)weekCap;
            }

            if (newTotalCount != oldTotalCount)
            {
                if (playerCurrency.State != PlayerCurrencyState.New)
                    playerCurrency.State = PlayerCurrencyState.Changed;

                CurrencyChanged(id, count);

                playerCurrency.Quantity = (uint)newTotalCount;
                playerCurrency.WeeklyQuantity = (uint)newWeekCount;
                playerCurrency.TrackedQuantity = (uint)newTrackedCount;

                if (count > 0)
                    UpdateCriteria(CriteriaType.CurrencyGained, id, (uint)count);

                _currencyStorage[id] = playerCurrency;

                SetCurrency packet = new();
                packet.Type = id;
                packet.Quantity = newTotalCount;
                packet.SuppressChatLog = !printLog;
                packet.WeeklyQuantity = newWeekCount;
                packet.TrackedQuantity = newTrackedCount;
                packet.Flags = playerCurrency.Flags;
                packet.QuantityChange = count;

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

        public uint GetTrackedCurrencyCount(uint id)
        {
            if (!_currencyStorage.ContainsKey(id))
                return 0;

            return _currencyStorage[id].TrackedQuantity;
        }

        //Action Buttons - CUF Profile
        public void SaveCUFProfile(byte id, CUFProfile profile)
        {
            _cUFProfiles[id] = profile;
        }

        public CUFProfile GetCUFProfile(byte id)
        {
            return _cUFProfiles[id];
        }

        public byte GetCUFProfilesCount()
        {
            return (byte)_cUFProfiles.Count(p => p != null);
        }

        public void SetMultiActionBars(byte mask)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.MultiActionBars), mask);
        }

        public ActionButton AddActionButton(byte button, ulong action, uint type)
        {
            if (!IsActionButtonDataValid(button, action, type))
                return null;

            // it create new Button (NEW State) if need or return existed
            if (!_actionButtons.ContainsKey(button))
                _actionButtons[button] = new ActionButton();

            var ab = _actionButtons[button];

            // set _data and update to CHANGED if not NEW
            ab.SetActionAndType(action, (ActionButtonType)type);

            Log.outDebug(LogFilter.Player, $"Player::AddActionButton: Player '{GetName()}' ({GetGUID()}) added Action '{action}' (Type {type}) to Button '{button}'");

            return ab;
        }

        public void RemoveActionButton(byte _button)
        {
            var button = _actionButtons.LookupByKey(_button);

            if (button == null ||
                button.UState == ActionButtonUpdateState.Deleted)
                return;

            if (button.UState == ActionButtonUpdateState.New)
                _actionButtons.Remove(_button); // new and not saved
            else
                button.UState = ActionButtonUpdateState.Deleted; // saved, will deleted at next save

            Log.outDebug(LogFilter.Player, "Action Button '{0}' Removed from Player '{1}'", button, GetGUID().ToString());
        }

        public ActionButton GetActionButton(byte _button)
        {
            var button = _actionButtons.LookupByKey(_button);

            if (button == null ||
                button.UState == ActionButtonUpdateState.Deleted)
                return null;

            return button;
        }

        //Repitation
        public int CalculateReputationGain(ReputationSource source, uint creatureOrQuestLevel, int rep, int faction, bool noQuestBonus = false)
        {
            bool noBonuses = false;
            var factionEntry = CliDB.FactionStorage.LookupByKey(faction);

            if (factionEntry != null)
            {
                var friendshipReputation = CliDB.FriendshipReputationStorage.LookupByKey(factionEntry.FriendshipRepID);

                if (friendshipReputation != null)
                    if (friendshipReputation.Flags.HasAnyFlag(FriendshipReputationFlags.NoRepGainModifiers))
                        noBonuses = true;
            }

            float percent = 100.0f;

            if (!noBonuses)
            {
                float repMod = noQuestBonus ? 0.0f : GetTotalAuraModifier(AuraType.ModReputationGain);

                // faction specific Auras only seem to apply to kills
                if (source == ReputationSource.Kill)
                    repMod += GetTotalAuraModifierByMiscValue(AuraType.ModFactionReputationGain, faction);

                percent += rep > 0 ? repMod : -repMod;
            }

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

            if (rate != 1.0f &&
                creatureOrQuestLevel < Formulas.GetGrayLevel(GetLevel()))
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
                        repRate = repData.CreatureRate;

                        break;
                    case ReputationSource.Quest:
                        repRate = repData.QuestRate;

                        break;
                    case ReputationSource.DailyQuest:
                        repRate = repData.QuestDailyRate;

                        break;
                    case ReputationSource.WeeklyQuest:
                        repRate = repData.QuestWeeklyRate;

                        break;
                    case ReputationSource.MonthlyQuest:
                        repRate = repData.QuestMonthlyRate;

                        break;
                    case ReputationSource.RepeatableQuest:
                        repRate = repData.QuestRepeatableRate;

                        break;
                    case ReputationSource.Spell:
                        repRate = repData.SpellRate;

                        break;
                }

                // for custom, a rate of 0.0 will totally disable reputation gain for this faction/Type
                if (repRate <= 0.0f)
                    return 0;

                percent *= repRate;
            }

            if (source != ReputationSource.Spell &&
                GetsRecruitAFriendBonus(false))
                percent *= 1.0f + WorldConfig.GetFloatValue(WorldCfg.RateReputationRecruitAFriendBonus);

            return MathFunctions.CalculatePct(rep, percent);
        }

        // Calculates how many reputation points player gains in victim's enemy factions
        public void RewardReputation(Unit victim, float rate)
        {
            if (!victim ||
                victim.IsTypeId(TypeId.Player))
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
                    {
                        var dungeonLevels = Global.DB2Mgr.GetContentTuningData(dungeon.ContentTuningID, PlayerData.CtrOptions.GetValue().ContentTuningConditionMask);

                        if (dungeonLevels.HasValue)
                            if (dungeonLevels.Value.TargetLevelMax == Global.ObjectMgr.GetMaxLevelForExpansion(Expansion.WrathOfTheLichKing))
                                ChampioningFaction = GetChampioningFaction();
                    }
                }
            }

            Team team = GetTeam();

            if (Rep.RepFaction1 != 0 &&
                (!Rep.TeamDependent || team == Team.Alliance))
            {
                int donerep1 = CalculateReputationGain(ReputationSource.Kill, victim.GetLevelForTarget(this), Rep.RepValue1, (int)(ChampioningFaction != 0 ? ChampioningFaction : Rep.RepFaction1));
                donerep1 = (int)(donerep1 * rate);

                FactionRecord factionEntry1 = CliDB.FactionStorage.LookupByKey(ChampioningFaction != 0 ? ChampioningFaction : Rep.RepFaction1);
                ReputationRank current_reputation_rank1 = GetReputationMgr().GetRank(factionEntry1);

                if (factionEntry1 != null)
                    GetReputationMgr().ModifyReputation(factionEntry1, donerep1, (uint)current_reputation_rank1 > Rep.ReputationMaxCap1);
            }

            if (Rep.RepFaction2 != 0 &&
                (!Rep.TeamDependent || team == Team.Horde))
            {
                int donerep2 = CalculateReputationGain(ReputationSource.Kill, victim.GetLevelForTarget(this), Rep.RepValue2, (int)(ChampioningFaction != 0 ? ChampioningFaction : Rep.RepFaction2));
                donerep2 = (int)(donerep2 * rate);

                FactionRecord factionEntry2 = CliDB.FactionStorage.LookupByKey(ChampioningFaction != 0 ? ChampioningFaction : Rep.RepFaction2);
                ReputationRank current_reputation_rank2 = GetReputationMgr().GetRank(factionEntry2);

                if (factionEntry2 != null)
                    GetReputationMgr().ModifyReputation(factionEntry2, donerep2, (uint)current_reputation_rank2 > Rep.ReputationMaxCap2);
            }
        }

        public bool TeleportTo(WorldLocation loc, TeleportToOptions options = 0, uint? instanceId = null)
        {
            return TeleportTo(loc.GetMapId(), loc.X, loc.Y, loc.Z, loc.Orientation, options, instanceId);
        }

        public bool TeleportTo(uint mapid, float x, float y, float z, float orientation, TeleportToOptions options = 0, uint? instanceId = null)
        {
            if (!GridDefines.IsValidMapCoord(mapid, x, y, z, orientation))
            {
                Log.outError(LogFilter.Maps,
                             "TeleportTo: invalid map ({0}) or invalid coordinates (X: {1}, Y: {2}, Z: {3}, O: {4}) given when teleporting player (GUID: {5}, Name: {6}, map: {7}, {8}).",
                             mapid,
                             x,
                             y,
                             z,
                             orientation,
                             GetGUID().ToString(),
                             GetName(),
                             GetMapId(),
                             GetPosition().ToString());

                return false;
            }

            if (!GetSession().HasPermission(RBACPermissions.SkipCheckDisableMap) &&
                Global.DisableMgr.IsDisabledFor(DisableType.Map, mapid, this))
            {
                Log.outError(LogFilter.Maps, "Player (GUID: {0}, Name: {1}) tried to enter a forbidden map {2}", GetGUID().ToString(), GetName(), mapid);
                SendTransferAborted(mapid, TransferAbortReason.MapNotAllowed);

                return false;
            }

            // preparing unsummon pet if lost (we must get pet before teleportation or will not find it later)
            Pet pet = GetPet();

            MapRecord mEntry = CliDB.MapStorage.LookupByKey(mapid);

            // don't let enter Battlegrounds without assigned Battlegroundid (for example through areatrigger)...
            // don't let gm level > 1 either
            if (!InBattleground() &&
                mEntry.IsBattlegroundOrArena())
                return false;

            // client without expansion support
            if (GetSession().GetExpansion() < mEntry.Expansion())
            {
                Log.outDebug(LogFilter.Maps, "Player {0} using client without required expansion tried teleport to non accessible map {1}", GetName(), mapid);

                ITransport _transport = GetTransport();

                if (_transport != null)
                {
                    _transport.RemovePassenger(this);
                    RepopAtGraveyard(); // teleport to near graveyard if on Transport, looks blizz like :)
                }

                SendTransferAborted(mapid, TransferAbortReason.InsufExpanLvl, (byte)mEntry.Expansion());

                return false; // normal client can't teleport to this map...
            }
            else
            {
                Log.outDebug(LogFilter.Maps, "Player {0} is being teleported to map {1}", GetName(), mapid);
            }

            if (Vehicle != null)
                ExitVehicle();

            // reset movement Flags at teleport, because player will continue move with these Flags after teleport
            SetUnitMovementFlags(GetUnitMovementFlags() & MovementFlag.MaskHasPlayerStatusOpcode);
            MovementInfo.ResetJump();
            DisableSpline();
            GetMotionMaster().Remove(MovementGeneratorType.Effect);

            ITransport transport = GetTransport();

            if (transport != null)
                if (!options.HasAnyFlag(TeleportToOptions.NotLeaveTransport))
                    transport.RemovePassenger(this);

            // The player was ported to another map and loses the Duel immediately.
            // We have to perform this check before the teleport, otherwise the
            // ObjectAccessor won't find the flag.
            if (Duel != null &&
                GetMapId() != mapid &&
                GetMap().GetGameObject(PlayerData.DuelArbiter))
                DuelComplete(DuelCompleteType.Fled);

            if (GetMapId() == mapid &&
                (!instanceId.HasValue || GetInstanceId() == instanceId))
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
                    _teleportDest = new WorldLocation(mapid, x, y, z, orientation);
                    _teleport_instanceId = null;
                    _teleport_options = options;

                    return true;
                }

                if (!options.HasAnyFlag(TeleportToOptions.NotUnSummonPet))
                    //same map, only remove pet if out of range for new position
                    if (pet && !pet.IsWithinDist3d(x, y, z, GetMap().GetVisibilityRange()))
                        UnsummonPetTemporaryIfAny();

                if (!IsAlive() &&
                    options.HasAnyFlag(TeleportToOptions.ReviveAtTeleport))
                    ResurrectPlayer(0.5f);

                if (!options.HasAnyFlag(TeleportToOptions.NotLeaveCombat))
                    CombatStop();

                // this will be used instead of the current location in SaveToDB
                _teleportDest = new WorldLocation(mapid, x, y, z, orientation);
                _teleport_instanceId = null;
                _teleport_options = options;
                SetFallInformation(0, GetPositionZ());

                // code for finish transfer called in WorldSession.HandleMovementOpcodes()
                // at client packet CMSG_MOVE_TELEPORT_ACK
                SetSemaphoreTeleportNear(true);

                // near teleport, triggering send CMSG_MOVE_TELEPORT_ACK from client at landing
                if (!GetSession().PlayerLogout())
                    SendTeleportPacket(_teleportDest);
            }
            else
            {
                if (GetClass() == Class.Deathknight &&
                    GetMapId() == 609 &&
                    !IsGameMaster() &&
                    !HasSpell(50977))
                {
                    SendTransferAborted(mapid, TransferAbortReason.UniqueMessage, 1);

                    return false;
                }

                // far teleport to another map
                Map oldmap = IsInWorld ? GetMap() : null;
                // check if we can enter before stopping combat / removing pet / totems / interrupting spells

                // Check enter rights before map getting to avoid creating instance copy for player
                // this check not dependent from map instance copy and same for all instance copies of selected map
                TransferAbortParams abortParams = Map.PlayerCannotEnter(mapid, this);

                if (abortParams != null)
                {
                    SendTransferAborted(mapid, abortParams.Reason, abortParams.Arg, abortParams.MapDifficultyXConditionId);

                    return false;
                }

                // Seamless teleport can happen only if cosmetic maps match
                if (!oldmap ||
                    (oldmap.GetEntry().CosmeticParentMapID != mapid &&
                     GetMapId() != mEntry.CosmeticParentMapID &&
                     !((oldmap.GetEntry().CosmeticParentMapID != -1) ^ (oldmap.GetEntry().CosmeticParentMapID != mEntry.CosmeticParentMapID))))
                    options &= ~TeleportToOptions.Seamless;

                //lets reset near teleport flag if it wasn't reset during chained teleports
                SetSemaphoreTeleportNear(false);
                //setup delayed teleport flag
                SetDelayedTeleportFlag(IsCanDelayTeleport());

                //if teleport spell is cast in Unit::Update() func
                //then we need to delay it until update process will be finished
                if (IsHasDelayedTeleport())
                {
                    SetSemaphoreTeleportFar(true);
                    //lets save teleport destination for player
                    _teleportDest = new WorldLocation(mapid, x, y, z, orientation);
                    _teleport_instanceId = instanceId;
                    _teleport_options = options;

                    return true;
                }

                SetSelection(ObjectGuid.Empty);

                CombatStop();

                ResetContestedPvP();

                // remove player from Battlegroundon far teleport (when changing maps)
                Battleground bg = GetBattleground();

                if (bg)
                    // Note: at Battlegroundjoin Battlegroundid set before teleport
                    // and we already will found "current" Battleground
                    // just need check that this is targeted map or leave
                    if (bg.GetMapId() != mapid)
                        LeaveBattleground(false); // don't teleport to entry point

                // remove arena spell coldowns/buffs now to also remove pet's cooldowns before it's temporarily unsummoned
                if (mEntry.IsBattleArena() &&
                    !IsGameMaster())
                {
                    RemoveArenaSpellCooldowns(true);
                    RemoveArenaAuras();

                    if (pet)
                        pet.RemoveArenaAuras();
                }

                // remove pet on map change
                if (pet)
                    UnsummonPetTemporaryIfAny();

                // remove all dyn objects
                RemoveAllDynObjects();

                // remove all areatriggers entities
                RemoveAllAreaTriggers();

                // stop spellcasting
                // not attempt interrupt teleportation spell at caster teleport
                if (!options.HasAnyFlag(TeleportToOptions.Spell))
                    if (IsNonMeleeSpellCast(true))
                        InterruptNonMeleeSpells(true);

                //remove Auras before removing from map...
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Moving | SpellAuraInterruptFlags.Turning);

                if (!GetSession().PlayerLogout() &&
                    !options.HasAnyFlag(TeleportToOptions.Seamless))
                {
                    // send transfer packets
                    TransferPending transferPending = new();
                    transferPending.MapID = (int)mapid;
                    transferPending.OldMapPosition = GetPosition();

                    Transport transport1 = (Transport)GetTransport();

                    if (transport1 != null)
                    {
                        TransferPending.ShipTransferPending shipTransferPending = new();
                        shipTransferPending.Id = transport1.GetEntry();
                        shipTransferPending.OriginMapID = (int)GetMapId();
                        transferPending.Ship = shipTransferPending;
                    }

                    SendPacket(transferPending);
                }

                // remove from old map now
                oldmap?.RemovePlayerFromMap(this, false);

                _teleportDest = new WorldLocation(mapid, x, y, z, orientation);
                _teleport_instanceId = instanceId;
                _teleport_options = options;
                SetFallInformation(0, GetPositionZ());
                // if the player is saved before worldportack (at logout for example)
                // this will be used instead of the current location in SaveToDB

                if (!GetSession().PlayerLogout())
                {
                    SuspendToken suspendToken = new();
                    suspendToken.SequenceIndex = MovementCounter; // not incrementing
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
            if (_bgData.JoinPos.GetMapId() == 0xFFFFFFFF)
                return false;

            ScheduleDelayedOperation(PlayerDelayedOperations.BGMountRestore);
            ScheduleDelayedOperation(PlayerDelayedOperations.BGTaxiRestore);
            ScheduleDelayedOperation(PlayerDelayedOperations.BGGroupRestore);

            return TeleportTo(_bgData.JoinPos);
        }

        public uint GetStartLevel(Race race, Class playerClass, uint? characterTemplateId = null)
        {
            uint startLevel = WorldConfig.GetUIntValue(WorldCfg.StartPlayerLevel);

            if (CliDB.ChrRacesStorage.LookupByKey(race).GetFlags().HasAnyFlag(ChrRacesFlag.IsAlliedRace))
                startLevel = WorldConfig.GetUIntValue(WorldCfg.StartAlliedRaceLevel);

            if (playerClass == Class.Deathknight)
            {
                if (race == Race.PandarenAlliance ||
                    race == Race.PandarenHorde)
                    startLevel = Math.Max(WorldConfig.GetUIntValue(WorldCfg.StartAlliedRaceLevel), startLevel);
                else
                    startLevel = Math.Max(WorldConfig.GetUIntValue(WorldCfg.StartDeathKnightPlayerLevel), startLevel);
            }
            else if (playerClass == Class.DemonHunter)
            {
                startLevel = Math.Max(WorldConfig.GetUIntValue(WorldCfg.StartDemonHunterPlayerLevel), startLevel);
            }
            else if (playerClass == Class.Evoker)
            {
                startLevel = Math.Max(WorldConfig.GetUIntValue(WorldCfg.StartEvokerPlayerLevel), startLevel);
            }

            if (characterTemplateId.HasValue)
            {
                if (GetSession().HasPermission(RBACPermissions.UseCharacterTemplates))
                {
                    CharacterTemplate charTemplate = Global.CharacterTemplateDataStorage.GetCharacterTemplate(characterTemplateId.Value);

                    if (charTemplate != null)
                        startLevel = Math.Max(charTemplate.Level, startLevel);
                }
                else
                {
                    Log.outWarn(LogFilter.Cheat, $"Account: {GetSession().GetAccountId()} (IP: {GetSession().GetRemoteAddress()}) tried to use a character template without given permission. Possible cheating attempt.");
                }
            }

            if (GetSession().HasPermission(RBACPermissions.UseStartGmLevel))
                startLevel = Math.Max(WorldConfig.GetUIntValue(WorldCfg.StartGmLevel), startLevel);

            return startLevel;
        }

        public void ValidateMovementInfo(MovementInfo mi)
        {
            var RemoveViolatingFlags = new Action<bool, MovementFlag>((check, maskToRemove) =>
                                                                      {
                                                                          if (check)
                                                                          {
                                                                              Log.outDebug(LogFilter.Unit,
                                                                                           "Player.ValidateMovementInfo: Violation of MovementFlags found ({0}). MovementFlags: {1}, MovementFlags2: {2} for player {3}. Mask {4} will be removed.",
                                                                                           check,
                                                                                           mi.GetMovementFlags(),
                                                                                           mi.GetMovementFlags2(),
                                                                                           GetGUID().ToString(),
                                                                                           maskToRemove);

                                                                              mi.RemoveMovementFlag(maskToRemove);
                                                                          }
                                                                      });

            if (!UnitMovedByMe.GetVehicleBase() ||
                !UnitMovedByMe.GetVehicle().GetVehicleInfo().Flags.HasAnyFlag(VehicleFlags.FixedPosition))
                RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Root), MovementFlag.Root);

            /*! This must be a packet spoofing attempt. MOVEMENTFLAG_ROOT sent from the client is not valid
			    in conjunction with any of the moving movement Flags such as MOVEMENTFLAG_FORWARD.
			    It will freeze clients that receive this player's movement info.
			*/
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Root) && mi.HasMovementFlag(MovementFlag.MaskMoving), MovementFlag.MaskMoving);

            //! Cannot hover without SPELL_AURA_HOVER
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Hover) && !UnitMovedByMe.HasAuraType(AuraType.Hover),
                                 MovementFlag.Hover);

            //! Cannot ascend and descend at the same Time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Ascending) && mi.HasMovementFlag(MovementFlag.Descending),
                                 MovementFlag.Ascending | MovementFlag.Descending);

            //! Cannot move left and right at the same Time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Left) && mi.HasMovementFlag(MovementFlag.Right),
                                 MovementFlag.Left | MovementFlag.Right);

            //! Cannot strafe left and right at the same Time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.StrafeLeft) && mi.HasMovementFlag(MovementFlag.StrafeRight),
                                 MovementFlag.StrafeLeft | MovementFlag.StrafeRight);

            //! Cannot pitch up and down at the same Time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.PitchUp) && mi.HasMovementFlag(MovementFlag.PitchDown),
                                 MovementFlag.PitchUp | MovementFlag.PitchDown);

            //! Cannot move forwards and backwards at the same Time
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Forward) && mi.HasMovementFlag(MovementFlag.Backward),
                                 MovementFlag.Forward | MovementFlag.Backward);

            //! Cannot walk on water without SPELL_AURA_WATER_WALK except for ghosts
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.WaterWalk) &&
                                 !UnitMovedByMe.HasAuraType(AuraType.WaterWalk) &&
                                 !UnitMovedByMe.HasAuraType(AuraType.Ghost),
                                 MovementFlag.WaterWalk);

            //! Cannot feather fall without SPELL_AURA_FEATHER_FALL
            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.FallingSlow) && !UnitMovedByMe.HasAuraType(AuraType.FeatherFall),
                                 MovementFlag.FallingSlow);

            /*! Cannot fly if no fly Auras present. Exception is being a GM.
			    Note that we check for account level instead of Player.IsGameMaster() because in some
			    situations it may be feasable to use .gm fly on as a GM without having .gm on,
			    e.g. aerial combat.
			*/

            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.Flying | MovementFlag.CanFly) &&
                                 GetSession().GetSecurity() == AccountTypes.Player &&
                                 !UnitMovedByMe.HasAuraType(AuraType.Fly) &&
                                 !UnitMovedByMe.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed),
                                 MovementFlag.Flying | MovementFlag.CanFly);

            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.DisableGravity | MovementFlag.CanFly) && mi.HasMovementFlag(MovementFlag.Falling),
                                 MovementFlag.Falling);

            RemoveViolatingFlags(mi.HasMovementFlag(MovementFlag.SplineElevation) && MathFunctions.fuzzyEq(mi.stepUpStartElevation, 0.0f), MovementFlag.SplineElevation);

            // Client first checks if spline elevation != 0, then verifies flag presence
            if (MathFunctions.fuzzyNe(mi.stepUpStartElevation, 0.0f))
                mi.AddMovementFlag(MovementFlag.SplineElevation);
        }

        public void HandleFall(MovementInfo movementInfo)
        {
            // calculate total z distance of the fall
            float z_diff = _lastFallZ - movementInfo.Pos.Z;
            Log.outDebug(LogFilter.Server, "zDiff = {0}", z_diff);

            //Players with low fall distance, Feather Fall or physical immunity (charges used) are ignored
            // 14.57 can be calculated by resolving damageperc formula below to 0
            if (z_diff >= 14.57f &&
                !IsDead() &&
                !IsGameMaster() &&
                !HasAuraType(AuraType.Hover) &&
                !HasAuraType(AuraType.FeatherFall) &&
                !HasAuraType(AuraType.Fly) &&
                !IsImmunedToDamage(SpellSchoolMask.Normal))
            {
                //Safe fall, fall height reduction
                int safe_fall = GetTotalAuraModifier(AuraType.SafeFall);

                float damageperc = 0.018f * (z_diff - safe_fall) - 0.2426f;

                if (damageperc > 0)
                {
                    uint damage = (uint)(damageperc * GetMaxHealth() * WorldConfig.GetFloatValue(WorldCfg.RateDamageFall));

                    float height = movementInfo.Pos.Z;
                    height = UpdateGroundPositionZ(movementInfo.Pos.X, movementInfo.Pos.Y, height);

                    damage = (uint)(damage * GetTotalAuraMultiplier(AuraType.ModifyFallDamagePct));

                    if (damage > 0)
                    {
                        //Prevent fall Damage from being more than the player maximum health
                        if (damage > GetMaxHealth())
                            damage = (uint)GetMaxHealth();

                        // Gust of Wind
                        if (HasAura(43621))
                            damage = (uint)GetMaxHealth() / 2;

                        uint original_health = (uint)GetHealth();
                        uint final_damage = EnvironmentalDamage(EnviromentalDamage.Fall, damage);

                        // recheck alive, might have died of EnvironmentalDamage, avoid cases when player die in fact like Spirit of Redemption case
                        if (IsAlive() &&
                            final_damage < original_health)
                            UpdateCriteria(CriteriaType.MaxDistFallenWithoutDying, (uint)z_diff * 100);
                    }

                    //Z given by moveinfo, LastZ, FallTime, WaterZ, MapZ, Damage, Safefall reduction
                    Log.outDebug(LogFilter.Player, $"FALLDAMAGE z={movementInfo.Pos.GetPositionZ()} sz={height} pZ={GetPositionZ()} FallTime={movementInfo.Jump.FallTime} mZ={height} Damage={damage} SF={safe_fall}\nPlayer debug info:\n{GetDebugInfo()}");
                }
            }
        }

        public void UpdateFallInformationIfNeed(MovementInfo minfo, ClientOpcodes opcode)
        {
            if (_lastFallTime >= MovementInfo.Jump.FallTime ||
                _lastFallZ <= MovementInfo.Pos.Z ||
                opcode == ClientOpcodes.MoveFallLand)
                SetFallInformation(MovementInfo.Jump.FallTime, MovementInfo.Pos.Z);
        }

        public bool HasSummonPending()
        {
            return _summon_expire >= GameTime.GetGameTime();
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

            _summon_expire = GameTime.GetGameTime() + PlayerConst.MaxPlayerSummonDelay;
            _summon_location = new WorldLocation(summoner);
            _summon_instanceId = summoner.GetInstanceId();

            SummonRequest summonRequest = new();
            summonRequest.SummonerGUID = summoner.GetGUID();
            summonRequest.SummonerVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            summonRequest.AreaID = (int)summoner.GetZoneId();
            SendPacket(summonRequest);

            Group group = GetGroup();

            if (group != null)
            {
                BroadcastSummonCast summonCast = new();
                summonCast.Target = GetGUID();
                group.BroadcastPacket(summonCast, false);
            }
        }

        public bool IsInAreaTriggerRadius(AreaTriggerRecord trigger)
        {
            if (trigger == null)
                return false;

            if (GetMapId() != trigger.ContinentID &&
                !GetPhaseShift().HasVisibleMapId(trigger.ContinentID))
                return false;

            if (trigger.PhaseID != 0 ||
                trigger.PhaseGroupID != 0 ||
                trigger.PhaseUseFlags != 0)
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
                Position center = new(trigger.Pos.X, trigger.Pos.Y, trigger.Pos.Z, trigger.BoxYaw);

                if (!IsWithinBox(center, trigger.BoxLength / 2.0f, trigger.BoxWidth / 2.0f, trigger.BoxHeight / 2.0f))
                    return false;
            }

            return true;
        }

        public void SummonIfPossible(bool agree)
        {
            void broadcastSummonResponse(bool accepted)
            {
                Group group = GetGroup();

                if (group != null)
                {
                    BroadcastSummonResponse summonResponse = new();
                    summonResponse.Target = GetGUID();
                    summonResponse.Accepted = accepted;
                    group.BroadcastPacket(summonResponse, false);
                }
            }

            if (!agree)
            {
                _summon_expire = 0;
                broadcastSummonResponse(false);

                return;
            }

            // expire and auto declined
            if (_summon_expire < GameTime.GetGameTime())
            {
                broadcastSummonResponse(false);

                return;
            }

            // stop taxi flight at summon
            FinishTaxiFlight();

            // drop flag at summon
            // this code can be reached only when GM is summoning player who carries flag, because player should be immune to summoning spells when he carries flag
            Battleground bg = GetBattleground();

            if (bg)
                bg.EventPlayerDroppedFlag(this);

            _summon_expire = 0;

            UpdateCriteria(CriteriaType.AcceptSummon, 1);
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Summon);

            TeleportTo(_summon_location, 0, _summon_instanceId);

            broadcastSummonResponse(true);
        }

        public override void OnPhaseChange()
        {
            base.OnPhaseChange();

            GetMap().UpdatePersonalPhasesForPlayer(this);
        }

        //GM
        public bool IsDeveloper()
        {
            return HasPlayerFlag(PlayerFlags.Developer);
        }

        public void SetDeveloper(bool on)
        {
            if (on)
                SetPlayerFlag(PlayerFlags.Developer);
            else
                RemovePlayerFlag(PlayerFlags.Developer);
        }

        public bool IsAcceptWhispers()
        {
            return _extraFlags.HasAnyFlag(PlayerExtraFlags.AcceptWhispers);
        }

        public void SetAcceptWhispers(bool on)
        {
            if (on)
                _extraFlags |= PlayerExtraFlags.AcceptWhispers;
            else
                _extraFlags &= ~PlayerExtraFlags.AcceptWhispers;
        }

        public bool IsGameMaster()
        {
            return _extraFlags.HasAnyFlag(PlayerExtraFlags.GMOn);
        }

        public bool IsGameMasterAcceptingWhispers()
        {
            return IsGameMaster() && IsAcceptWhispers();
        }

        public bool CanBeGameMaster()
        {
            return GetSession().HasPermission(RBACPermissions.CommandGm);
        }

        public void SetGameMaster(bool on)
        {
            if (on)
            {
                _extraFlags |= PlayerExtraFlags.GMOn;
                SetFaction(35);
                SetPlayerFlag(PlayerFlags.GM);
                SetUnitFlag2(UnitFlags2.AllowCheatSpells);

                Pet pet = GetPet();

                pet?.SetFaction(35);

                RemovePvpFlag(UnitPVPStateFlags.FFAPvp);
                ResetContestedPvP();

                CombatStopWithPets();

                PhasingHandler.SetAlwaysVisible(this, true, false);
                ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.GM, GetSession().GetSecurity());
            }
            else
            {
                PhasingHandler.SetAlwaysVisible(this, HasAuraType(AuraType.PhaseAlwaysVisible), false);

                _extraFlags &= ~PlayerExtraFlags.GMOn;
                RestoreFaction();
                RemovePlayerFlag(PlayerFlags.GM);
                RemoveUnitFlag2(UnitFlags2.AllowCheatSpells);

                Pet pet = GetPet();

                pet?.SetFaction(GetFaction());

                // restore FFA PvP Server State
                if (Global.WorldMgr.IsFFAPvPRealm())
                    SetPvpFlag(UnitPVPStateFlags.FFAPvp);

                // restore FFA PvP area State, remove not allowed for GM mounts
                UpdateArea(_areaUpdateId);

                ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.GM, AccountTypes.Player);
            }

            UpdateObjectVisibility();
        }

        public bool IsGMChat()
        {
            return _extraFlags.HasAnyFlag(PlayerExtraFlags.GMChat);
        }

        public void SetGMChat(bool on)
        {
            if (on)
                _extraFlags |= PlayerExtraFlags.GMChat;
            else
                _extraFlags &= ~PlayerExtraFlags.GMChat;
        }

        public bool IsTaxiCheater()
        {
            return _extraFlags.HasAnyFlag(PlayerExtraFlags.TaxiCheat);
        }

        public void SetTaxiCheater(bool on)
        {
            if (on)
                _extraFlags |= PlayerExtraFlags.TaxiCheat;
            else
                _extraFlags &= ~PlayerExtraFlags.TaxiCheat;
        }

        public bool IsGMVisible()
        {
            return !_extraFlags.HasAnyFlag(PlayerExtraFlags.GMInvisible);
        }

        public void SetGMVisible(bool on)
        {
            if (on)
            {
                _extraFlags &= ~PlayerExtraFlags.GMInvisible; //remove flag
                ServerSideVisibility.SetValue(ServerSideVisibilityType.GM, AccountTypes.Player);
            }
            else
            {
                _extraFlags |= PlayerExtraFlags.GMInvisible; //add flag

                SetAcceptWhispers(false);
                SetGameMaster(true);

                ServerSideVisibility.SetValue(ServerSideVisibilityType.GM, GetSession().GetSecurity());
            }

            foreach (Channel channel in _channels)
                channel.SetInvisible(this, !on);
        }

        //Chat - Text - Channel
        public void PrepareGossipMenu(WorldObject source, uint menuId, bool showQuests = false)
        {
            PlayerMenu menu = PlayerTalkClass;
            menu.ClearMenus();

            menu.GetGossipMenu().SetMenuId(menuId);

            var menuItemBounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(menuId);

            if (source.IsTypeId(TypeId.Unit))
            {
                if (showQuests && source.ToUnit().IsQuestGiver())
                    PrepareQuestMenu(source.GetGUID());
            }
            else if (source.IsTypeId(TypeId.GameObject))
            {
                if (source.ToGameObject().GetGoType() == GameObjectTypes.QuestGiver)
                    PrepareQuestMenu(source.GetGUID());
            }

            foreach (var gossipMenuItem in menuItemBounds)
            {
                if (!Global.ConditionMgr.IsObjectMeetToConditions(this, source, gossipMenuItem.Conditions))
                    continue;

                bool canTalk = true;
                GameObject go = source.ToGameObject();
                Creature creature = source.ToCreature();

                if (creature)
                    switch (gossipMenuItem.OptionNpc)
                    {
                        case GossipOptionNpc.Taxinode:
                            if (GetSession().SendLearnNewTaxiNode(creature))
                                return;

                            break;
                        case GossipOptionNpc.SpiritHealer:
                            if (!IsDead())
                                canTalk = false;

                            break;
                        case GossipOptionNpc.Battlemaster:
                            if (!creature.CanInteractWithBattleMaster(this, false))
                                canTalk = false;

                            break;
                        case GossipOptionNpc.TalentMaster:
                        case GossipOptionNpc.SpecializationMaster:
                        case GossipOptionNpc.GlyphMaster:
                            if (!creature.CanResetTalents(this))
                                canTalk = false;

                            break;
                        case GossipOptionNpc.Stablemaster:
                        case GossipOptionNpc.PetSpecializationMaster:
                            if (GetClass() != Class.Hunter)
                                canTalk = false;

                            break;
                        case GossipOptionNpc.DisableXPGain:
                            if (HasPlayerFlag(PlayerFlags.NoXPGain) ||
                                IsMaxLevel())
                                canTalk = false;

                            break;
                        case GossipOptionNpc.EnableXPGain:
                            if (!HasPlayerFlag(PlayerFlags.NoXPGain) ||
                                IsMaxLevel())
                                canTalk = false;

                            break;
                        case GossipOptionNpc.None:
                        case GossipOptionNpc.Vendor:
                        case GossipOptionNpc.Trainer:
                        case GossipOptionNpc.Binder:
                        case GossipOptionNpc.Banker:
                        case GossipOptionNpc.PetitionVendor:
                        case GossipOptionNpc.TabardVendor:
                        case GossipOptionNpc.Auctioneer:
                        case GossipOptionNpc.Mailbox:
                        case GossipOptionNpc.Transmogrify:
                        case GossipOptionNpc.AzeriteRespec:
                            break; // No checks
                        case GossipOptionNpc.CemeterySelect:
                            canTalk = false; // Deprecated

                            break;
                        case GossipOptionNpc.GuildBanker:
                        case GossipOptionNpc.Spellclick:
                        case GossipOptionNpc.WorldPvPQueue:
                        case GossipOptionNpc.LFGDungeon:
                        case GossipOptionNpc.ArtifactRespec:
                        case GossipOptionNpc.QueueScenario:
                        case GossipOptionNpc.GarrisonArchitect:
                        case GossipOptionNpc.GarrisonMissionNpc:
                        case GossipOptionNpc.ShipmentCrafter:
                        case GossipOptionNpc.GarrisonTradeskillNpc:
                        case GossipOptionNpc.GarrisonRecruitment:
                        case GossipOptionNpc.AdventureMap:
                        case GossipOptionNpc.GarrisonTalent:
                        case GossipOptionNpc.ContributionCollector:
                        case GossipOptionNpc.IslandsMissionNpc:
                        case GossipOptionNpc.UIItemInteraction:
                        case GossipOptionNpc.WorldMap:
                        case GossipOptionNpc.Soulbind:
                        case GossipOptionNpc.ChromieTimeNpc:
                        case GossipOptionNpc.CovenantPreviewNpc:
                        case GossipOptionNpc.RuneforgeLegendaryCrafting:
                        case GossipOptionNpc.NewPlayerGuide:
                        case GossipOptionNpc.RuneforgeLegendaryUpgrade:
                        case GossipOptionNpc.CovenantRenownNpc:
                            break; // NYI
                        default:
                            Log.outError(LogFilter.Sql, $"Creature entry {creature.GetEntry()} has an unknown gossip option icon {gossipMenuItem.OptionNpc} for menu {gossipMenuItem.MenuID}.");
                            canTalk = false;

                            break;
                    }
                else if (go != null)
                    switch (gossipMenuItem.OptionNpc)
                    {
                        case GossipOptionNpc.None:
                            if (go.GetGoType() != GameObjectTypes.QuestGiver &&
                                go.GetGoType() != GameObjectTypes.Goober)
                                canTalk = false;

                            break;
                        default:
                            canTalk = false;

                            break;
                    }

                if (canTalk)
                    menu.GetGossipMenu().AddMenuItem(gossipMenuItem, gossipMenuItem.MenuID, gossipMenuItem.OrderIndex);
            }
        }

        public void SendPreparedGossip(WorldObject source)
        {
            if (!source)
                return;

            if (source.IsTypeId(TypeId.Unit) ||
                source.IsTypeId(TypeId.GameObject))
                if (PlayerTalkClass.GetGossipMenu().IsEmpty() &&
                    !PlayerTalkClass.GetQuestMenu().IsEmpty())
                {
                    SendPreparedQuest(source);

                    return;
                }

            // in case non empty gossip menu (that not included quests list size) show it
            // (quest entries from quest menu will be included in list)

            uint textId = GetGossipTextId(source);
            uint menuId = PlayerTalkClass.GetGossipMenu().GetMenuId();

            if (menuId != 0)
                textId = GetGossipTextId(menuId, source);

            PlayerTalkClass.SendGossipMenu(textId, source.GetGUID());
        }

        public void OnGossipSelect(WorldObject source, int gossipOptionId, uint menuId)
        {
            GossipMenu gossipMenu = PlayerTalkClass.GetGossipMenu();

            // if not same, then something funky is going on
            if (menuId != gossipMenu.GetMenuId())
                return;

            GossipMenuItem item = gossipMenu.GetItem(gossipOptionId);

            if (item == null)
                return;

            GossipOptionNpc gossipOptionNpc = item.OptionNpc;
            ObjectGuid guid = source.GetGUID();

            if (source.IsTypeId(TypeId.GameObject))
                if (gossipOptionNpc != GossipOptionNpc.None)
                {
                    Log.outError(LogFilter.Player, "Player Guid {0} request invalid gossip option for GameObject entry {1}", GetGUID().ToString(), source.GetEntry());

                    return;
                }

            long cost = item.BoxMoney;

            if (!HasEnoughMoney(cost))
            {
                SendBuyError(BuyResult.NotEnoughtMoney, null, 0);
                PlayerTalkClass.SendCloseGossip();

                return;
            }

            if (item.ActionPoiID != 0)
                PlayerTalkClass.SendPointOfInterest(item.ActionPoiID);

            if (item.ActionMenuID != 0)
            {
                PrepareGossipMenu(source, item.ActionMenuID);
                SendPreparedGossip(source);
            }

            // types that have their dedicated open opcode dont send WorldPackets::NPC::GossipOptionNPCInteraction
            bool handled = true;

            switch (gossipOptionNpc)
            {
                case GossipOptionNpc.Vendor:
                    GetSession().SendListInventory(guid);

                    break;
                case GossipOptionNpc.Taxinode:
                    GetSession().SendTaxiMenu(source.ToCreature());

                    break;
                case GossipOptionNpc.Trainer:
                    GetSession().SendTrainerList(source.ToCreature(), Global.ObjectMgr.GetCreatureTrainerForGossipOption(source.GetEntry(), menuId, item.OrderIndex));

                    break;
                case GossipOptionNpc.SpiritHealer:
                    source.CastSpell(source.ToCreature(), 17251, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(GetGUID()));
                    handled = false;

                    break;
                case GossipOptionNpc.PetitionVendor:
                    PlayerTalkClass.SendCloseGossip();
                    GetSession().SendPetitionShowList(guid);

                    break;
                case GossipOptionNpc.Battlemaster:
                    {
                        BattlegroundTypeId bgTypeId = Global.BattlegroundMgr.GetBattleMasterBG(source.GetEntry());

                        if (bgTypeId == BattlegroundTypeId.None)
                        {
                            Log.outError(LogFilter.Player, "a user (Guid {0}) requested Battlegroundlist from a npc who is no battlemaster", GetGUID().ToString());

                            return;
                        }

                        Global.BattlegroundMgr.SendBattlegroundList(this, guid, bgTypeId);

                        break;
                    }
                case GossipOptionNpc.Auctioneer:
                    GetSession().SendAuctionHello(guid, source.ToCreature());

                    break;
                case GossipOptionNpc.TalentMaster:
                    PlayerTalkClass.SendCloseGossip();
                    SendRespecWipeConfirm(guid, WorldConfig.GetBoolValue(WorldCfg.NoResetTalentCost) ? 0 : GetNextResetTalentsCost(), SpecResetType.Talents);

                    break;
                case GossipOptionNpc.Stablemaster:
                    GetSession().SendStablePet(guid);

                    break;
                case GossipOptionNpc.PetSpecializationMaster:
                    PlayerTalkClass.SendCloseGossip();
                    SendRespecWipeConfirm(guid, WorldConfig.GetBoolValue(WorldCfg.NoResetTalentCost) ? 0 : GetNextResetTalentsCost(), SpecResetType.PetTalents);

                    break;
                case GossipOptionNpc.GuildBanker:
                    Guild guild = GetGuild();

                    if (guild != null)
                        guild.SendBankList(GetSession(), 0, true);
                    else
                        Guild.SendCommandResult(GetSession(), GuildCommandType.ViewTab, GuildCommandError.PlayerNotInGuild);

                    break;
                case GossipOptionNpc.Spellclick:
                    Unit sourceUnit = source.ToUnit();

                    sourceUnit?.HandleSpellClick(this);

                    break;
                case GossipOptionNpc.DisableXPGain:
                    PlayerTalkClass.SendCloseGossip();
                    CastSpell(null, PlayerConst.SpellExperienceEliminated, true);
                    SetPlayerFlag(PlayerFlags.NoXPGain);

                    break;
                case GossipOptionNpc.EnableXPGain:
                    PlayerTalkClass.SendCloseGossip();
                    RemoveAurasDueToSpell(PlayerConst.SpellExperienceEliminated);
                    RemovePlayerFlag(PlayerFlags.NoXPGain);

                    break;
                case GossipOptionNpc.SpecializationMaster:
                    PlayerTalkClass.SendCloseGossip();
                    SendRespecWipeConfirm(guid, 0, SpecResetType.Specialization);

                    break;
                case GossipOptionNpc.GlyphMaster:
                    PlayerTalkClass.SendCloseGossip();
                    SendRespecWipeConfirm(guid, 0, SpecResetType.Glyphs);

                    break;
                case GossipOptionNpc.GarrisonTradeskillNpc: // NYI
                    break;
                case GossipOptionNpc.GarrisonRecruitment: // NYI
                    break;
                case GossipOptionNpc.ChromieTimeNpc: // NYI
                    break;
                case GossipOptionNpc.RuneforgeLegendaryCrafting: // NYI
                    break;
                case GossipOptionNpc.RuneforgeLegendaryUpgrade: // NYI
                    break;
                case GossipOptionNpc.ProfessionsCraftingOrder: // NYI
                    break;
                case GossipOptionNpc.ProfessionsCustomerOrder: // NYI
                    break;
                case GossipOptionNpc.BarbersChoice: // NYI - unknown if needs sending
                default:
                    handled = false;

                    break;
            }

            if (!handled)
            {
                if (item.GossipNpcOptionID.HasValue)
                {
                    GossipMenuAddon addon = Global.ObjectMgr.GetGossipMenuAddon(menuId);

                    GossipOptionNPCInteraction npcInteraction = new();
                    npcInteraction.GossipGUID = source.GetGUID();
                    npcInteraction.GossipNpcOptionID = item.GossipNpcOptionID.Value;

                    if (addon != null &&
                        addon.FriendshipFactionID != 0)
                        npcInteraction.FriendshipFactionID = addon.FriendshipFactionID;

                    SendPacket(npcInteraction);
                }
                else
                {
                    PlayerInteractionType[] GossipOptionNpcToInteractionType =
                    {
                        PlayerInteractionType.None, PlayerInteractionType.Vendor, PlayerInteractionType.TaxiNode, PlayerInteractionType.Trainer, PlayerInteractionType.SpiritHealer, PlayerInteractionType.Binder, PlayerInteractionType.Banker, PlayerInteractionType.PetitionVendor, PlayerInteractionType.TabardVendor, PlayerInteractionType.BattleMaster, PlayerInteractionType.Auctioneer, PlayerInteractionType.TalentMaster, PlayerInteractionType.StableMaster, PlayerInteractionType.None, PlayerInteractionType.GuildBanker, PlayerInteractionType.None, PlayerInteractionType.None, PlayerInteractionType.None, PlayerInteractionType.MailInfo, PlayerInteractionType.None, PlayerInteractionType.LFGDungeon, PlayerInteractionType.ArtifactForge, PlayerInteractionType.None, PlayerInteractionType.SpecializationMaster, PlayerInteractionType.None, PlayerInteractionType.None, PlayerInteractionType.GarrArchitect, PlayerInteractionType.GarrMission, PlayerInteractionType.ShipmentCrafter, PlayerInteractionType.GarrTradeskill, PlayerInteractionType.GarrRecruitment, PlayerInteractionType.AdventureMap, PlayerInteractionType.GarrTalent, PlayerInteractionType.ContributionCollector, PlayerInteractionType.Transmogrifier, PlayerInteractionType.AzeriteRespec, PlayerInteractionType.IslandQueue, PlayerInteractionType.ItemInteraction, PlayerInteractionType.WorldMap, PlayerInteractionType.Soulbind, PlayerInteractionType.ChromieTime, PlayerInteractionType.CovenantPreview, PlayerInteractionType.LegendaryCrafting, PlayerInteractionType.NewPlayerGuide, PlayerInteractionType.LegendaryCrafting, PlayerInteractionType.Renown, PlayerInteractionType.BlackMarketAuctioneer, PlayerInteractionType.PerksProgramVendor, PlayerInteractionType.ProfessionsCraftingOrder, PlayerInteractionType.Professions, PlayerInteractionType.ProfessionsCustomerOrder, PlayerInteractionType.TraitSystem, PlayerInteractionType.BarbersChoice, PlayerInteractionType.MajorFactionRenown
                    };

                    PlayerInteractionType interactionType = GossipOptionNpcToInteractionType[(int)gossipOptionNpc];

                    if (interactionType != PlayerInteractionType.None)
                    {
                        NPCInteractionOpenResult npcInteraction = new();
                        npcInteraction.Npc = source.GetGUID();
                        npcInteraction.InteractionType = interactionType;
                        npcInteraction.Success = true;
                        SendPacket(npcInteraction);
                    }
                }
            }

            ModifyMoney(-cost);
        }

        public uint GetGossipTextId(WorldObject source)
        {
            if (source == null)
                return SharedConst.DefaultGossipMessage;

            return GetGossipTextId(GetDefaultGossipMenuForSource(source), source);
        }

        public uint GetGossipTextId(uint menuId, WorldObject source)
        {
            uint textId = SharedConst.DefaultGossipMessage;

            if (menuId == 0)
                return textId;

            var menuBounds = Global.ObjectMgr.GetGossipMenusMapBounds(menuId);

            foreach (var menu in menuBounds)
                if (Global.ConditionMgr.IsObjectMeetToConditions(this, source, menu.Conditions))
                    textId = menu.TextId;

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
            if (channel.Flags.HasAnyFlag(ChannelDBCFlags.ZoneDep) &&
                zone.HasFlag(AreaFlags.ArenaInstance))
                return false;

            if (channel.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly) &&
                !zone.HasFlag(AreaFlags.Capital))
                return false;

            if (channel.Flags.HasAnyFlag(ChannelDBCFlags.GuildReq) &&
                GetGuildId() != 0)
                return false;

            if (channel.Flags.HasAnyFlag(ChannelDBCFlags.NoClientJoin))
                return false;

            return true;
        }

        public void JoinedChannel(Channel c)
        {
            _channels.Add(c);
        }

        public void LeftChannel(Channel c)
        {
            _channels.Remove(c);
        }

        public void CleanupChannels()
        {
            while (!_channels.Empty())
            {
                Channel ch = _channels.FirstOrDefault();
                _channels.RemoveAt(0);        // remove from player's channel list
                ch.LeaveChannel(this, false); // not send to client, not remove from player's channel list

                // delete channel if empty
                ChannelManager cMgr = ChannelManager.ForTeam(GetTeam());

                if (cMgr != null)
                    if (ch.IsConstant())
                        cMgr.LeftChannel(ch.GetChannelId(), ch.GetZoneEntry());
            }

            Log.outDebug(LogFilter.ChatSystem, "Player {0}: channels cleaned up!", GetName());
        }

        public List<Channel> GetJoinedChannels()
        {
            return _channels;
        }

        //Mail
        public void AddMail(Mail mail)
        {
            _mail.Insert(0, mail);
        }

        public void RemoveMail(ulong id)
        {
            foreach (var mail in _mail)
                if (mail.MessageID == id)
                {
                    //do not delete Item, because Player.removeMail() is called when returning mail to sender.
                    _mail.Remove(mail);

                    return;
                }
        }

        public void SendMailResult(ulong mailId, MailResponseType mailAction, MailResponseResult mailError, InventoryResult equipError = 0, ulong itemGuid = 0, uint itemCount = 0)
        {
            MailCommandResult result = new();
            result.MailID = mailId;
            result.Command = (uint)mailAction;
            result.ErrorCode = (uint)mailError;

            if (mailError == MailResponseResult.EquipError)
                result.BagResult = (uint)equipError;
            else if (mailAction == MailResponseType.ItemTaken)
            {
                result.AttachID = itemGuid;
                result.QtyInInventory = itemCount;
            }

            SendPacket(result);
        }

        void SendNewMail()
        {
            SendPacket(new NotifyReceivedMail());
        }

        public void UpdateNextMailTimeAndUnreads()
        {
            // calculate next delivery Time (min. from non-delivered mails
            // and recalculate unReadMail
            long cTime = GameTime.GetGameTime();
            _nextMailDelivereTime = 0;
            UnReadMails = 0;

            foreach (var mail in _mail)
                if (mail.Deliver_time > cTime)
                {
                    if (_nextMailDelivereTime == 0 ||
                        _nextMailDelivereTime > mail.Deliver_time)
                        _nextMailDelivereTime = mail.Deliver_time;
                }
                else if ((mail.CheckMask & MailCheckMask.Read) == 0)
                {
                    ++UnReadMails;
                }
        }

        public void AddNewMailDeliverTime(long deliver_time)
        {
            if (deliver_time <= GameTime.GetGameTime()) // ready now
            {
                ++UnReadMails;
                SendNewMail();
            }
            else // not ready and no have ready mails
            {
                if (_nextMailDelivereTime == 0 ||
                    _nextMailDelivereTime > deliver_time)
                    _nextMailDelivereTime = deliver_time;
            }
        }

        public void AddMItem(Item it)
        {
            _mMitems[it.GetGUID().GetCounter()] = it;
        }

        public bool RemoveMItem(ulong id)
        {
            return _mMitems.Remove(id);
        }


        public Item GetMItem(ulong id)
        {
            return _mMitems.LookupByKey(id);
        }

        public Mail GetMail(ulong id)
        {
            return _mail.Find(p => p.MessageID == id);
        }

        public List<Mail> GetMails()
        {
            return _mail;
        }

        public uint GetMailSize()
        {
            return (uint)_mail.Count;
        }

        //Binds
        public bool HasPendingBind()
        {
            return _pendingBindId > 0;
        }

        public void SetHomebind(WorldLocation loc, uint areaId)
        {
            _homebind.WorldRelocate(loc);
            _homebindAreaId = areaId;

            // update sql _homebind
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_PLAYER_HOMEBIND);
            stmt.AddValue(0, _homebind.GetMapId());
            stmt.AddValue(1, _homebindAreaId);
            stmt.AddValue(2, _homebind.GetPositionX());
            stmt.AddValue(3, _homebind.GetPositionY());
            stmt.AddValue(4, _homebind.GetPositionZ());
            stmt.AddValue(5, _homebind.GetOrientation());
            stmt.AddValue(6, GetGUID().GetCounter());
            DB.Characters.Execute(stmt);
        }

        public void SetBindPoint(ObjectGuid guid)
        {
            NPCInteractionOpenResult npcInteraction = new();
            npcInteraction.Npc = guid;
            npcInteraction.InteractionType = PlayerInteractionType.Binder;
            npcInteraction.Success = true;
            SendPacket(npcInteraction);
        }

        public void SendBindPointUpdate()
        {
            BindPointUpdate packet = new();
            packet.BindPosition = new Vector3(_homebind.GetPositionX(), _homebind.GetPositionY(), _homebind.GetPositionZ());
            packet.BindMapID = _homebind.GetMapId();
            packet.BindAreaID = _homebindAreaId;
            SendPacket(packet);
        }

        public void SendPlayerBound(ObjectGuid binderGuid, uint areaId)
        {
            PlayerBound packet = new(binderGuid, areaId);
            SendPacket(packet);
        }

        //Misc
        public uint GetTotalPlayedTime()
        {
            return _playedTimeTotal;
        }

        public uint GetLevelPlayedTime()
        {
            return _playedTimeLevel;
        }

        public CinematicManager GetCinematicMgr()
        {
            return _cinematicMgr;
        }

        public void SendUpdateWorldState(WorldStates variable, uint value, bool hidden = false)
        {
            SendUpdateWorldState((uint)variable, value, hidden);
        }

        public void SendUpdateWorldState(uint variable, uint value, bool hidden = false)
        {
            UpdateWorldState worldstate = new();
            worldstate.VariableID = variable;
            worldstate.Value = (int)value;
            worldstate.Hidden = hidden;
            SendPacket(worldstate);
        }

        public long GetBarberShopCost(List<ChrCustomizationChoice> newCustomizations)
        {
            if (HasAuraType(AuraType.RemoveBarberShopCost))
                return 0;

            GtBarberShopCostBaseRecord bsc = CliDB.BarberShopCostBaseGameTable.GetRow(GetLevel());

            if (bsc == null) // shouldn't happen
                return 0;

            long cost = 0;

            foreach (ChrCustomizationChoice newChoice in newCustomizations)
            {
                int currentCustomizationIndex = PlayerData.Customizations.FindIndexIf(currentCustomization => { return currentCustomization.ChrCustomizationOptionID == newChoice.ChrCustomizationOptionID; });

                if (currentCustomizationIndex == -1 ||
                    PlayerData.Customizations[currentCustomizationIndex].ChrCustomizationChoiceID != newChoice.ChrCustomizationChoiceID)
                {
                    ChrCustomizationOptionRecord customizationOption = CliDB.ChrCustomizationOptionStorage.LookupByKey(newChoice.ChrCustomizationOptionID);

                    if (customizationOption != null)
                        cost += (long)(bsc.Cost * customizationOption.BarberShopCostModifier);
                }
            }

            return cost;
        }

        public void SetChampioningFaction(uint faction)
        {
            _championingFaction = faction;
        }

        public static byte GetFactionGroupForRace(Race race)
        {
            var rEntry = CliDB.ChrRacesStorage.LookupByKey((uint)race);

            if (rEntry != null)
            {
                var faction = CliDB.FactionTemplateStorage.LookupByKey(rEntry.FactionID);

                if (faction != null)
                    return faction.FactionGroup;
            }

            return 1;
        }

        public void SetFactionForRace(Race race)
        {
            _team = TeamForRace(race);

            ChrRacesRecord rEntry = CliDB.ChrRacesStorage.LookupByKey(race);
            SetFaction(rEntry != null ? (uint)rEntry.FactionID : 0);
        }

        public void SetResurrectRequestData(WorldObject caster, uint health, uint mana, uint appliedAura)
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

        public bool IsResurrectRequested()
        {
            return _resurrectionData != null;
        }

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

        public void UpdateTriggerVisibility()
        {
            if (ClientGUIDs.Empty())
                return;

            if (!IsInWorld)
                return;

            UpdateData udata = new(GetMapId());

            foreach (var guid in ClientGUIDs)
                if (guid.IsCreatureOrVehicle())
                {
                    Creature creature = GetMap().GetCreature(guid);

                    // Update fields of triggers, transformed units or unselectable units (values dependent on GM State)
                    if (creature == null ||
                        (!creature.IsTrigger() && !creature.HasAuraType(AuraType.Transform) && !creature.HasUnitFlag(UnitFlags.Uninteractible)))
                        continue;

                    creature.Values.ModifyValue(UnitData).ModifyValue(UnitData.DisplayID);
                    creature.Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags);
                    creature.ForceUpdateFieldChange();
                    creature.BuildValuesUpdateBlockForPlayer(udata, this);
                }
                else if (guid.IsAnyTypeGameObject())
                {
                    GameObject go = GetMap().GetGameObject(guid);

                    if (go == null)
                        continue;

                    go.Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags);
                    go.ForceUpdateFieldChange();
                    go.BuildValuesUpdateBlockForPlayer(udata, this);
                }

            if (!udata.HasData())
                return;

            UpdateObject packet;
            udata.BuildPacket(out packet);
            SendPacket(packet);
        }

        public bool IsAllowedToLoot(Creature creature)
        {
            if (!creature.IsDead())
                return false;

            if (HasPendingBind())
                return false;

            Loot loot = creature.GetLootForPlayer(this);

            if (loot == null ||
                loot.IsLooted()) // nothing to loot or everything looted.
                return false;

            if (!loot.HasAllowedLooter(GetGUID()) ||
                (!loot.HasItemForAll() && !loot.HasItemFor(this))) // no loot in creature for this player
                return false;

            switch (loot.GetLootMethod())
            {
                case LootMethod.PersonalLoot:
                case LootMethod.FreeForAll:
                    return true;
                case LootMethod.RoundRobin:
                    // may only loot if the player is the loot roundrobin player
                    // or if there are free/quest/conditional Item for the player
                    if (loot.RoundRobinPlayer.IsEmpty() ||
                        loot.RoundRobinPlayer == GetGUID())
                        return true;

                    return loot.HasItemFor(this);
                case LootMethod.MasterLoot:
                case LootMethod.GroupLoot:
                case LootMethod.NeedBeforeGreed:
                    // may only loot if the player is the loot roundrobin player
                    // or Item over threshold (so roll(s) can be launched or to preview master looted items)
                    // or if there are free/quest/conditional Item for the player
                    if (loot.RoundRobinPlayer.IsEmpty() ||
                        loot.RoundRobinPlayer == GetGUID())
                        return true;

                    if (loot.HasOverThresholdItem())
                        return true;

                    return loot.HasItemFor(this);
            }

            return false;
        }

        public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, WorldObject caster, bool requireImmunityPurgesEffectAttribute = false)
        {
            // players are immune to taunt (the aura and the spell effect).
            if (spellEffectInfo.IsAura(AuraType.ModTaunt))
                return true;

            if (spellEffectInfo.IsEffect(SpellEffectName.AttackMe))
                return true;

            return base.IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster, requireImmunityPurgesEffectAttribute);
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

        public Player GetSelectedPlayer()
        {
            ObjectGuid selectionGUID = GetTarget();

            if (!selectionGUID.IsEmpty())
                return Global.ObjAccessor.GetPlayer(this, selectionGUID);

            return null;
        }

        public static bool IsValidGender(Gender _gender)
        {
            return _gender <= Gender.Female;
        }

        public static bool IsValidClass(Class _class)
        {
            return Convert.ToBoolean((1 << ((int)_class - 1)) & (int)Class.ClassMaskAllPlayable);
        }

        public static bool IsValidRace(Race _race)
        {
            return Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(_race) & SharedConst.RaceMaskAllPlayable);
        }

        public uint EnvironmentalDamage(EnviromentalDamage type, uint damage)
        {
            if (IsImmuneToEnvironmentalDamage())
                return 0;

            damage = (uint)(damage * GetTotalAuraMultiplier(AuraType.ModEnvironmentalDamageTaken));

            // Absorb, Resist some environmental Damage Type
            uint absorb = 0;
            uint resist = 0;

            switch (type)
            {
                case EnviromentalDamage.Lava:
                case EnviromentalDamage.Slime:
                    DamageInfo dmgInfo = new(this, this, damage, null, type == EnviromentalDamage.Lava ? SpellSchoolMask.Fire : SpellSchoolMask.Nature, DamageEffectType.Direct, WeaponAttackType.BaseAttack);
                    CalcAbsorbResist(dmgInfo);
                    absorb = dmgInfo.GetAbsorb();
                    resist = dmgInfo.GetResist();
                    damage = dmgInfo.GetDamage();

                    break;
            }

            DealDamageMods(null, this, ref damage, ref absorb);

            EnvironmentalDamageLog packet = new();
            packet.Victim = GetGUID();
            packet.Type = type != EnviromentalDamage.FallToVoid ? type : EnviromentalDamage.Fall;
            packet.Amount = (int)damage;
            packet.Absorbed = (int)absorb;
            packet.Resisted = (int)resist;

            uint final_damage = DealDamage(this, this, damage, null, DamageEffectType.Self, SpellSchoolMask.Normal, null, false);
            packet.LogData.Initialize(this);

            SendCombatLogMessage(packet);

            if (!IsAlive())
            {
                if (type == EnviromentalDamage.Fall) // DealDamage not apply Item durability loss at self Damage
                {
                    Log.outDebug(LogFilter.Player, $"Player::EnvironmentalDamage: Player '{GetName()}' ({GetGUID()}) fall to death, losing {WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossOnDeath)} durability");
                    DurabilityLossAll(WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossOnDeath), false);
                    // durability lost message
                    SendDurabilityLoss(this, (uint)(WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossOnDeath) * 100.0f));
                }

                UpdateCriteria(CriteriaType.DieFromEnviromentalDamage, 1, (ulong)type);
            }

            return final_damage;
        }

        public override bool CanNeverSee(WorldObject obj)
        {
            // the intent is to delay sending visible objects until client is ready for them
            // some gameobjects dont function correctly if they are sent before TransportServerTime is correctly set (after CMSG_MOVE_INIT_ACTIVE_MOVER_COMPLETE)
            return !HasPlayerLocalFlag(PlayerLocalFlags.OverrideTransportServerTime) || base.CanNeverSee(obj);
        }

        public override bool CanAlwaysSee(WorldObject obj)
        {
            // Always can see self
            if (GetUnitBeingMoved() == obj)
                return true;

            ObjectGuid guid = ActivePlayerData.FarsightObject;

            if (!guid.IsEmpty())
                if (obj.GetGUID() == guid)
                    return true;

            return false;
        }

        public override bool IsAlwaysDetectableFor(WorldObject seer)
        {
            if (base.IsAlwaysDetectableFor(seer))
                return true;

            if (Duel != null &&
                Duel.State != DuelState.Challenged &&
                Duel.Opponent == seer)
                return false;

            Player seerPlayer = seer.ToPlayer();

            if (seerPlayer != null)
                if (IsGroupVisibleFor(seerPlayer))
                    return true;

            return false;
        }

        public override bool IsNeverVisibleFor(WorldObject seer)
        {
            if (base.IsNeverVisibleFor(seer))
                return true;

            if (GetSession().PlayerLogout() ||
                GetSession().PlayerLoading())
                return true;

            return false;
        }

        public void BuildPlayerRepop()
        {
            PreRessurect packet = new();
            packet.PlayerGUID = GetGUID();
            SendPacket(packet);

            // If the player has the Wisp racial then cast the Wisp aura on them
            if (HasSpell(20585))
                CastSpell(this, 20584, true);

            CastSpell(this, 8326, true);

            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Release);

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
            SetDeathState(DeathState.Dead);
            SetHealth(1);

            SetWaterWalking(true);

            if (!GetSession().IsLogingOut() &&
                !HasUnitState(UnitState.Stunned))
                SetRooted(false);

            // BG - remove insignia related
            RemoveUnitFlag(UnitFlags.Skinnable);

            int corpseReclaimDelay = CalculateCorpseReclaimDelay();

            if (corpseReclaimDelay >= 0)
                SendCorpseReclaimDelay(corpseReclaimDelay);

            // to prevent cheating
            corpse.ResetGhostTime();

            StopMirrorTimers(); //disable timers(bars)

            // OnPlayerRepop hook
            Global.ScriptMgr.ForEach<IPlayerOnPlayerRepop>(p => p.OnPlayerRepop(this));
        }

        public void StopMirrorTimers()
        {
            StopMirrorTimer(MirrorTimerType.Fatigue);
            StopMirrorTimer(MirrorTimerType.Breath);
            StopMirrorTimer(MirrorTimerType.Fire);
        }

        public bool IsMirrorTimerActive(MirrorTimerType type)
        {
            return _mirrorTimer[(int)type] == GetMaxTimer(type);
        }

        public void UpdateMirrorTimers()
        {
            // Desync Flags for update on next HandleDrowning
            if (_mirrorTimerFlags != 0)
                _mirrorTimerFlagsLast = ~_mirrorTimerFlags;
        }

        public void ResurrectPlayer(float restore_percent, bool applySickness = false)
        {
            DeathReleaseLoc packet = new();
            packet.MapID = -1;
            SendPacket(packet);

            // speed change, land walk

            // remove death flag + set aura
            RemovePlayerFlag(PlayerFlags.IsOutOfBounds);

            // This must be called always even on Players with race != RACE_NIGHTELF in case of faction change
            RemoveAurasDueToSpell(20584); // speed bonuses
            RemoveAurasDueToSpell(8326);  // SPELL_AURA_GHOST

            if (GetSession().IsARecruiter() ||
                (GetSession().GetRecruiterId() != 0))
                SetDynamicFlag(UnitDynFlags.ReferAFriend);

            SetDeathState(DeathState.Alive);

            // add the flag to make sure opcode is always sent
            AddUnitMovementFlag(MovementFlag.WaterWalk);
            SetWaterWalking(false);

            if (!HasUnitState(UnitState.Stunned))
                SetRooted(false);

            _deathTimer = 0;

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

            // trigger update zone for alive State zone updates
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

            // recast lost by death Auras of any items held in the inventory
            CastAllObtainSpells();

            if (!applySickness)
                return;

            //Characters from level 1-10 are not affected by resurrection sickness.
            //Characters from level 11-19 will suffer from one minute of sickness
            //for each level they are above 10.
            //Characters level 20 and up suffer from ten minutes of sickness.
            int startLevel = WorldConfig.GetIntValue(WorldCfg.DeathSicknessLevel);
            var raceEntry = CliDB.ChrRacesStorage.LookupByKey(GetRace());

            if (GetLevel() >= startLevel)
            {
                // set resurrection sickness
                CastSpell(this, raceEntry.ResSicknessSpellID, true);

                // not full duration
                if (GetLevel() < startLevel + 9)
                {
                    int delta = (int)(GetLevel() - startLevel + 1) * Time.Minute;
                    Aura aur = GetAura(raceEntry.ResSicknessSpellID, GetGUID());

                    aur?.SetDuration(delta * Time.InMilliseconds);
                }
            }
        }

        public void KillPlayer()
        {
            if (IsFlying() &&
                GetTransport() == null)
                GetMotionMaster().MoveFall();

            SetRooted(true);

            StopMirrorTimers(); //disable timers(bars)

            SetDeathState(DeathState.Corpse);

            ReplaceAllDynamicFlags(UnitDynFlags.None);

            if (!CliDB.MapStorage.LookupByKey(GetMapId()).Instanceable() &&
                !HasAuraType(AuraType.PreventResurrection))
                SetPlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
            else
                RemovePlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);

            // 6 minutes until repop at graveyard
            _deathTimer = 6 * Time.Minute * Time.InMilliseconds;

            UpdateCorpseReclaimDelay(); // dependent at use SetDeathPvP() call before kill

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

        public void SpawnCorpseBones(bool triggerSave = true)
        {
            _corpseLocation = new WorldLocation();

            if (GetMap().ConvertCorpseToBones(GetGUID()))
                if (triggerSave && !GetSession().PlayerLogoutWithSave()) // at logout we will already store the player
                    SaveToDB();                                          // prevent loading as ghost without corpse
        }

        public Corpse GetCorpse()
        {
            return GetMap().GetCorpseByPlayer(GetGUID());
        }

        public void RepopAtGraveyard()
        {
            // note: this can be called also when the player is alive
            // for example from WorldSession.HandleMovementOpcodes

            AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(GetAreaId());

            bool shouldResurrect = false;

            // Such zones are considered unreachable as a ghost and the player must be automatically revived
            if ((!IsAlive() && zone != null && zone.HasFlag(AreaFlags.NeedFly)) ||
                GetTransport() != null ||
                GetPositionZ() < GetMap().GetMinHeight(GetPhaseShift(), GetPositionX(), GetPositionY()))
            {
                shouldResurrect = true;
                SpawnCorpseBones();
            }

            WorldSafeLocsEntry ClosestGrave;

            // Special handle for Battlegroundmaps
            Battleground bg = GetBattleground();

            if (bg)
            {
                ClosestGrave = bg.GetClosestGraveYard(this);
            }
            else
            {
                BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(GetMap(), GetZoneId());

                if (bf != null)
                    ClosestGrave = bf.GetClosestGraveYard(this);
                else
                    ClosestGrave = Global.ObjectMgr.GetClosestGraveYard(this, GetTeam(), this);
            }

            // stop countdown until repop
            _deathTimer = 0;

            // if no grave found, stay at the current location
            // and don't show spirit healer location
            if (ClosestGrave != null)
            {
                TeleportTo(ClosestGrave.Loc, shouldResurrect ? TeleportToOptions.ReviveAtTeleport : 0);

                if (IsDead()) // not send if alive, because it used in TeleportTo()
                {
                    DeathReleaseLoc packet = new();
                    packet.MapID = (int)ClosestGrave.Loc.GetMapId();
                    packet.Loc = ClosestGrave.Loc;
                    SendPacket(packet);
                }
            }
            else if (GetPositionZ() < GetMap().GetMinHeight(GetPhaseShift(), GetPositionX(), GetPositionY()))
            {
                TeleportTo(_homebind);
            }

            RemovePlayerFlag(PlayerFlags.IsOutOfBounds);
        }

        public bool HasCorpse()
        {
            return _corpseLocation != null && _corpseLocation.GetMapId() != 0xFFFFFFFF;
        }

        public WorldLocation GetCorpseLocation()
        {
            return _corpseLocation;
        }

        public uint GetCorpseReclaimDelay(bool pvp)
        {
            if (pvp)
            {
                if (!WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPvp))
                    return PlayerConst.copseReclaimDelay[0];
            }
            else if (!WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPve))
            {
                return 0;
            }

            long now = GameTime.GetGameTime();
            // 0..2 full period
            // should be ceil(x)-1 but not floor(x)
            ulong count = (ulong)((now < _deathExpireTime - 1) ? (_deathExpireTime - 1 - now) / PlayerConst.DeathExpireStep : 0);

            return PlayerConst.copseReclaimDelay[count];
        }

        public override bool CanFly()
        {
            return MovementInfo.HasMovementFlag(MovementFlag.CanFly);
        }

        public override bool CanEnterWater()
        {
            return true;
        }

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

                if (IsInWorld)
                    return pet;
            }

            return null;
        }

        public Pet SummonPet(uint entry, PetSaveMode? slot, float x, float y, float z, float ang, uint duration)
        {
            return SummonPet(entry, slot, x, y, z, ang, duration, out _);
        }

        public Pet SummonPet(uint entry, PetSaveMode? slot, float x, float y, float z, float ang, uint duration, out bool isNew)
        {
            isNew = false;

            PetStable petStable = GetOrInitPetStable();

            Pet pet = new(this, PetType.Summon);

            if (pet.LoadPetFromDB(this, entry, 0, false, slot))
            {
                if (duration > 0)
                    pet.SetDuration(duration);

                return null;
            }

            // petentry == 0 for hunter "call pet" (current pet summoned if any)
            if (entry == 0)
                return null;

            // only SUMMON_PET are handled here

            pet.Relocate(x, y, z, ang);

            if (!pet.IsPositionValid())
            {
                Log.outError(LogFilter.Server,
                             "Pet (guidlow {0}, entry {1}) not summoned. Suggested coordinates isn't valid (X: {2} Y: {3})",
                             pet.GetGUID().ToString(),
                             pet.GetEntry(),
                             pet.GetPositionX(),
                             pet.GetPositionY());

                return null;
            }

            Map map = GetMap();
            uint petNumber = Global.ObjectMgr.GeneratePetNumber();

            if (!pet.Create(map.GenerateLowGuid(HighGuid.Pet), map, entry, petNumber))
            {
                Log.outError(LogFilter.Server, "no such creature entry {0}", entry);

                return null;
            }

            if (petStable.GetCurrentPet() != null)
                RemovePet(null, PetSaveMode.NotInSlot);

            PhasingHandler.InheritPhaseShift(pet, this);

            pet.SetCreatorGUID(GetGUID());
            pet.SetFaction(GetFaction());
            pet.ReplaceAllNpcFlags(NPCFlags.None);
            pet.ReplaceAllNpcFlags2(NPCFlags2.None);
            pet.InitStatsForLevel(GetLevel());

            SetMinion(pet, true);

            // this enables pet details window (Shift+P)
            pet.GetCharmInfo().SetPetNumber(petNumber, true);
            pet.SetClass(Class.Mage);
            pet.SetPetExperience(0);
            pet.SetPetNextLevelExperience(1000);
            pet.SetFullHealth();
            pet.SetFullPower(PowerType.Mana);
            pet.SetPetNameTimestamp((uint)GameTime.GetGameTime());

            map.AddToMap(pet.ToCreature());

            Cypher.Assert(!petStable.CurrentPetIndex.HasValue);
            petStable.SetCurrentUnslottedPetIndex((uint)petStable.UnslottedPets.Count);
            PetStable.PetInfo petInfo = new();
            pet.FillPetInfo(petInfo);
            petStable.UnslottedPets.Add(petInfo);

            pet.InitPetCreateSpells();
            pet.SavePetToDB(PetSaveMode.AsCurrent);
            PetSpellInitialize();

            if (duration > 0)
                pet.SetDuration(duration);

            //ObjectAccessor.UpdateObjectVisibility(pet);

            isNew = true;

            return pet;
        }

        public void RemovePet(Pet pet, PetSaveMode mode, bool returnreagent = false)
        {
            if (!pet)
                pet = GetPet();

            if (pet)
            {
                Log.outDebug(LogFilter.Pet, "RemovePet {0}, {1}, {2}", pet.GetEntry(), mode, returnreagent);

                if (pet.Removed)
                    return;
            }

            if (returnreagent &&
                (pet || _temporaryUnsummonedPetNumber != 0) &&
                !InBattleground())
            {
                //returning of reagents only for players, so best done here
                uint spellId = pet ? pet.UnitData.CreatedBySpell : _oldpetspell;
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetMap().GetDifficultyID());

                if (spellInfo != null)
                    for (uint i = 0; i < SpellConst.MaxReagents; ++i)
                        if (spellInfo.Reagent[i] > 0)
                        {
                            List<ItemPosCount> dest = new(); //for succubus, voidwalker, felhunter and felguard credit soulshard when despawn reason other than death (out of range, logout)
                            InventoryResult msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, (uint)spellInfo.Reagent[i], spellInfo.ReagentCount[i]);

                            if (msg == InventoryResult.Ok)
                            {
                                Item item = StoreNewItem(dest, (uint)spellInfo.Reagent[i], true);

                                if (IsInWorld)
                                    SendNewItem(item, spellInfo.ReagentCount[i], true, false);
                            }
                        }

                _temporaryUnsummonedPetNumber = 0;
            }

            if (pet == null)
            {
                // Handle removing pet while it is in "temporarily unsummoned" State, for example on Mount
                if (mode == PetSaveMode.NotInSlot &&
                    _petStable != null &&
                    _petStable.CurrentPetIndex.HasValue)
                    _petStable.CurrentPetIndex = null;

                return;
            }

            pet.CombatStop();

            // only if current pet in Slot
            pet.SavePetToDB(mode);

            PetStable.PetInfo currentPet = _petStable.GetCurrentPet();
            Cypher.Assert(currentPet != null && currentPet.PetNumber == pet.GetCharmInfo().GetPetNumber());

            if (mode == PetSaveMode.NotInSlot ||
                mode == PetSaveMode.AsDeleted)
                _petStable.CurrentPetIndex = null;
            // else if (stable slots) handled in opcode handlers due to required swaps
            // else (current pet) doesnt need to do anything

            SetMinion(pet, false);

            pet.AddObjectToRemoveList();
            pet.Removed = true;

            if (pet.IsControlled())
            {
                SendPacket(new PetSpells());

                if (GetGroup())
                    SetGroupUpdateFlag(GroupUpdateFlags.Pet);
            }
        }

        public void SendTameFailure(PetTameResult result)
        {
            PetTameFailure petTameFailure = new();
            petTameFailure.Result = (byte)result;
            SendPacket(petTameFailure);
        }

        public void AddPetAura(PetAura petSpell)
        {
            PetAuras.Add(petSpell);

            Pet pet = GetPet();

            pet?.CastPetAura(petSpell);
        }

        public void RemovePetAura(PetAura petSpell)
        {
            PetAuras.Remove(petSpell);

            Pet pet = GetPet();

            pet?.RemoveAurasDueToSpell(petSpell.GetAura(pet.GetEntry()));
        }

        public bool InArena()
        {
            Battleground bg = GetBattleground();

            if (!bg ||
                !bg.IsArena())
                return false;

            return true;
        }

        public void SendOnCancelExpectedVehicleRideAura()
        {
            SendPacket(new OnCancelExpectedRideVehicleAura());
        }

        public void SendMovementSetCollisionHeight(float height, UpdateCollisionHeightReason reason)
        {
            MoveSetCollisionHeight setCollisionHeight = new();
            setCollisionHeight.MoverGUID = GetGUID();
            setCollisionHeight.SequenceIndex = MovementCounter++;
            setCollisionHeight.Height = height;
            setCollisionHeight.Scale = GetObjectScale();
            setCollisionHeight.MountDisplayID = GetMountDisplayId();
            setCollisionHeight.ScaleDuration = UnitData.ScaleDuration;
            setCollisionHeight.Reason = reason;
            SendPacket(setCollisionHeight);

            MoveUpdateCollisionHeight updateCollisionHeight = new();
            updateCollisionHeight.Status = MovementInfo;
            updateCollisionHeight.Height = height;
            updateCollisionHeight.Scale = GetObjectScale();
            SendMessageToSet(updateCollisionHeight, false);
        }

        public void SendPlayerChoice(ObjectGuid sender, int choiceId)
        {
            PlayerChoice playerChoice = Global.ObjectMgr.GetPlayerChoice(choiceId);

            if (playerChoice == null)
                return;

            Locale locale = GetSession().GetSessionDbLocaleIndex();
            PlayerChoiceLocale playerChoiceLocale = locale != Locale.enUS ? Global.ObjectMgr.GetPlayerChoiceLocale(choiceId) : null;

            PlayerTalkClass.GetInteractionData().Reset();
            PlayerTalkClass.GetInteractionData().SourceGuid = sender;
            PlayerTalkClass.GetInteractionData().PlayerChoiceId = (uint)choiceId;

            DisplayPlayerChoice displayPlayerChoice = new();
            displayPlayerChoice.SenderGUID = sender;
            displayPlayerChoice.ChoiceID = choiceId;
            displayPlayerChoice.UiTextureKitID = playerChoice.UiTextureKitId;
            displayPlayerChoice.SoundKitID = playerChoice.SoundKitId;
            displayPlayerChoice.Question = playerChoice.Question;

            if (playerChoiceLocale != null)
                ObjectManager.GetLocaleString(playerChoiceLocale.Question, locale, ref displayPlayerChoice.Question);

            displayPlayerChoice.CloseChoiceFrame = false;
            displayPlayerChoice.HideWarboardHeader = playerChoice.HideWarboardHeader;
            displayPlayerChoice.KeepOpenAfterChoice = playerChoice.KeepOpenAfterChoice;

            for (var i = 0; i < playerChoice.Responses.Count; ++i)
            {
                PlayerChoiceResponse playerChoiceResponseTemplate = playerChoice.Responses[i];
                var playerChoiceResponse = new Networking.Packets.PlayerChoiceResponse();

                playerChoiceResponse.ResponseID = playerChoiceResponseTemplate.ResponseId;
                playerChoiceResponse.ResponseIdentifier = playerChoiceResponseTemplate.ResponseIdentifier;
                playerChoiceResponse.ChoiceArtFileID = playerChoiceResponseTemplate.ChoiceArtFileId;
                playerChoiceResponse.Flags = playerChoiceResponseTemplate.Flags;
                playerChoiceResponse.WidgetSetID = playerChoiceResponseTemplate.WidgetSetID;
                playerChoiceResponse.UiTextureAtlasElementID = playerChoiceResponseTemplate.UiTextureAtlasElementID;
                playerChoiceResponse.SoundKitID = playerChoiceResponseTemplate.SoundKitID;
                playerChoiceResponse.GroupID = playerChoiceResponseTemplate.GroupID;
                playerChoiceResponse.UiTextureKitID = playerChoiceResponseTemplate.UiTextureKitID;
                playerChoiceResponse.Answer = playerChoiceResponseTemplate.Answer;
                playerChoiceResponse.Header = playerChoiceResponseTemplate.Header;
                playerChoiceResponse.SubHeader = playerChoiceResponseTemplate.SubHeader;
                playerChoiceResponse.ButtonTooltip = playerChoiceResponseTemplate.ButtonTooltip;
                playerChoiceResponse.Description = playerChoiceResponseTemplate.Description;
                playerChoiceResponse.Confirmation = playerChoiceResponseTemplate.Confirmation;

                if (playerChoiceLocale != null)
                {
                    PlayerChoiceResponseLocale playerChoiceResponseLocale = playerChoiceLocale.Responses.LookupByKey(playerChoiceResponseTemplate.ResponseId);

                    if (playerChoiceResponseLocale != null)
                    {
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.Answer, locale, ref playerChoiceResponse.Answer);
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.Header, locale, ref playerChoiceResponse.Header);
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.SubHeader, locale, ref playerChoiceResponse.SubHeader);
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.ButtonTooltip, locale, ref playerChoiceResponse.ButtonTooltip);
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.Description, locale, ref playerChoiceResponse.Description);
                        ObjectManager.GetLocaleString(playerChoiceResponseLocale.Confirmation, locale, ref playerChoiceResponse.Confirmation);
                    }
                }

                if (playerChoiceResponseTemplate.Reward != null)
                {
                    var reward = new Networking.Packets.PlayerChoiceResponseReward();
                    reward.TitleID = playerChoiceResponseTemplate.Reward.TitleId;
                    reward.PackageID = playerChoiceResponseTemplate.Reward.PackageId;
                    reward.SkillLineID = playerChoiceResponseTemplate.Reward.SkillLineId;
                    reward.SkillPointCount = playerChoiceResponseTemplate.Reward.SkillPointCount;
                    reward.ArenaPointCount = playerChoiceResponseTemplate.Reward.ArenaPointCount;
                    reward.HonorPointCount = playerChoiceResponseTemplate.Reward.HonorPointCount;
                    reward.Money = playerChoiceResponseTemplate.Reward.Money;
                    reward.Xp = playerChoiceResponseTemplate.Reward.Xp;

                    foreach (var item in playerChoiceResponseTemplate.Reward.Items)
                    {
                        var rewardEntry = new Networking.Packets.PlayerChoiceResponseRewardEntry();
                        rewardEntry.Item.ItemID = item.Id;
                        rewardEntry.Quantity = item.Quantity;

                        if (!item.BonusListIDs.Empty())
                        {
                            rewardEntry.Item.ItemBonus = new ItemBonuses();
                            rewardEntry.Item.ItemBonus.BonusListIDs = item.BonusListIDs;
                        }

                        reward.Items.Add(rewardEntry);
                    }

                    foreach (var currency in playerChoiceResponseTemplate.Reward.Currency)
                    {
                        var rewardEntry = new Networking.Packets.PlayerChoiceResponseRewardEntry();
                        rewardEntry.Item.ItemID = currency.Id;
                        rewardEntry.Quantity = currency.Quantity;
                        reward.Items.Add(rewardEntry);
                    }

                    foreach (var faction in playerChoiceResponseTemplate.Reward.Faction)
                    {
                        var rewardEntry = new Networking.Packets.PlayerChoiceResponseRewardEntry();
                        rewardEntry.Item.ItemID = faction.Id;
                        rewardEntry.Quantity = faction.Quantity;
                        reward.Items.Add(rewardEntry);
                    }

                    foreach (PlayerChoiceResponseRewardItem item in playerChoiceResponseTemplate.Reward.ItemChoices)
                    {
                        var rewardEntry = new Networking.Packets.PlayerChoiceResponseRewardEntry();
                        rewardEntry.Item.ItemID = item.Id;
                        rewardEntry.Quantity = item.Quantity;

                        if (!item.BonusListIDs.Empty())
                        {
                            rewardEntry.Item.ItemBonus = new ItemBonuses();
                            rewardEntry.Item.ItemBonus.BonusListIDs = item.BonusListIDs;
                        }

                        reward.ItemChoices.Add(rewardEntry);
                    }

                    playerChoiceResponse.Reward = reward;
                    displayPlayerChoice.Responses[i] = playerChoiceResponse;
                }

                playerChoiceResponse.RewardQuestID = playerChoiceResponseTemplate.RewardQuestID;

                if (playerChoiceResponseTemplate.MawPower.HasValue)
                {
                    var mawPower = new Networking.Packets.PlayerChoiceResponseMawPower();
                    mawPower.TypeArtFileID = playerChoiceResponse.MawPower.Value.TypeArtFileID;
                    mawPower.Rarity = playerChoiceResponse.MawPower.Value.Rarity;
                    mawPower.RarityColor = playerChoiceResponse.MawPower.Value.RarityColor;
                    mawPower.SpellID = playerChoiceResponse.MawPower.Value.SpellID;
                    mawPower.MaxStacks = playerChoiceResponse.MawPower.Value.MaxStacks;

                    playerChoiceResponse.MawPower = mawPower;
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

        public void SetWarModeDesired(bool enabled)
        {
            // Only allow to toggle on when in stormwind/orgrimmar, and to toggle off in any rested place.
            // Also disallow when in combat
            if ((enabled == IsWarModeDesired()) ||
                IsInCombat() ||
                !HasPlayerFlag(PlayerFlags.Resting))
                return;

            if (enabled && !CanEnableWarModeInArea())
                return;

            // Don't allow to chang when aura SPELL_PVP_RULES_ENABLED is on
            if (HasAura(PlayerConst.SpellPvpRulesEnabled))
                return;

            if (enabled)
            {
                SetPlayerFlag(PlayerFlags.WarModeDesired);
                SetPvP(true);
            }
            else
            {
                RemovePlayerFlag(PlayerFlags.WarModeDesired);
                SetPvP(false);
            }

            UpdateWarModeAuras();
        }

        public bool CanEnableWarModeInArea()
        {
            var zone = CliDB.AreaTableStorage.LookupByKey(GetZoneId());

            if (zone == null ||
                !IsFriendlyArea(zone))
                return false;

            var area = CliDB.AreaTableStorage.LookupByKey(GetAreaId());

            if (area == null)
                area = zone;

            do
            {
                if ((area.Flags[1] & (uint)AreaFlags2.CanEnableWarMode) != 0)
                    return true;

                area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
            } while (area != null);

            return false;
        }

        public bool IsWarModeLocalActive()
        {
            return HasPlayerLocalFlag(PlayerLocalFlags.WarMode);
        }

        // Used in triggers for check "Only to targets that grant experience or honor" req
        public bool IsHonorOrXPTarget(Unit victim)
        {
            uint v_level = victim.GetLevelForTarget(this);
            uint k_grey = Formulas.GetGrayLevel(GetLevel());

            // Victim level less gray level
            if (v_level < k_grey &&
                WorldConfig.GetIntValue(WorldCfg.MinCreatureScaledXpRatio) == 0)
                return false;

            Creature creature = victim.ToCreature();

            if (creature != null)
                if (creature.IsCritter() ||
                    creature.IsTotem())
                    return false;

            return true;
        }

        public void SetRegenTimerCount(uint time)
        {
            _regenTimerCount = time;
        }

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

        public Team GetTeam()
        {
            return _team;
        }

        public int GetTeamId()
        {
            return _team == Team.Alliance ? TeamId.Alliance : TeamId.Horde;
        }

        public Team GetEffectiveTeam()
        {
            return HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode) ? (GetTeam() == Team.Alliance ? Team.Horde : Team.Alliance) : GetTeam();
        }

        public int GetEffectiveTeamId()
        {
            return GetEffectiveTeam() == Team.Alliance ? TeamId.Alliance : TeamId.Horde;
        }

        //Money
        public ulong GetMoney()
        {
            return ActivePlayerData.Coinage;
        }

        public bool HasEnoughMoney(ulong amount)
        {
            return GetMoney() >= amount;
        }

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

            Global.ScriptMgr.ForEach<IPlayerOnMoneyChanged>(p => p.OnMoneyChanged(this, amount));

            if (amount < 0)
            {
                SetMoney((ulong)(GetMoney() > (ulong)-amount ? (long)GetMoney() + amount : 0));
            }
            else
            {
                if (GetMoney() <= (PlayerConst.MaxMoneyAmount - (ulong)amount))
                {
                    SetMoney((ulong)(GetMoney() + (ulong)amount));
                }
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
            bool loading = GetSession().PlayerLoading();

            if (!loading)
                MoneyChanged((uint)value);

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Coinage), value);

            if (!loading)
                UpdateCriteria(CriteriaType.MostMoneyOwned);
        }

        //Target
        // Used for serverside Target changes, does not apply to players
        public override void SetTarget(ObjectGuid guid)
        {
        }

        public void SetSelection(ObjectGuid guid)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Target), guid);
        }

        //LoginFlag
        public bool HasAtLoginFlag(AtLoginFlags f)
        {
            return Convert.ToBoolean(AtLoginFlags & f);
        }

        public void SetAtLoginFlag(AtLoginFlags f)
        {
            AtLoginFlags |= f;
        }

        public void RemoveAtLoginFlag(AtLoginFlags flags, bool persist = false)
        {
            AtLoginFlags &= ~flags;

            if (persist)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_REM_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)flags);
                stmt.AddValue(1, GetGUID().GetCounter());

                DB.Characters.Execute(stmt);
            }
        }

        //Guild
        public void SetInGuild(ulong guildId)
        {
            if (guildId != 0)
            {
                SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.GuildGUID), ObjectGuid.Create(HighGuid.Guild, guildId));
                SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.GuildClubMemberID), GetGUID().GetCounter());
                SetPlayerFlag(PlayerFlags.GuildLevelEnabled);
            }
            else
            {
                SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.GuildGUID), ObjectGuid.Empty);
                RemovePlayerFlag(PlayerFlags.GuildLevelEnabled);
            }

            Global.CharacterCacheStorage.UpdateCharacterGuildId(GetGUID(), guildId);
        }

        public void SetGuildRank(byte rankId)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.GuildRankID), rankId);
        }

        public uint GetGuildRank()
        {
            return PlayerData.GuildRankID;
        }

        public void SetGuildLevel(uint level)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.GuildLevel), level);
        }

        public uint GetGuildLevel()
        {
            return PlayerData.GuildLevel;
        }

        public void SetGuildIdInvited(ulong GuildId)
        {
            _guildIdInvited = GuildId;
        }

        public ulong GetGuildId()
        {
            return ((ObjectGuid)UnitData.GuildGUID).GetCounter();
        }

        public Guild GetGuild()
        {
            ulong guildId = GetGuildId();

            return guildId != 0 ? Global.GuildMgr.GetGuildById(guildId) : null;
        }

        public ulong GetGuildIdInvited()
        {
            return _guildIdInvited;
        }

        public string GetGuildName()
        {
            return GetGuildId() != 0 ? Global.GuildMgr.GetGuildById(GetGuildId()).GetName() : "";
        }

        public void SetFreePrimaryProfessions(uint profs)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CharacterPoints), profs);
        }

        public void GiveLevel(uint level)
        {
            var oldLevel = GetLevel();

            if (level == oldLevel)
                return;

            Guild guild = GetGuild();

            guild?.UpdateMemberData(this, GuildMemberData.Level, level);

            PlayerLevelInfo info = Global.ObjectMgr.GetPlayerLevelInfo(GetRace(), GetClass(), level);

            Global.ObjectMgr.GetPlayerClassLevelInfo(GetClass(), level, out uint basemana);

            LevelUpInfo packet = new();
            packet.Level = level;
            packet.HealthDelta = 0;

            // @todo find some better solution
            packet.PowerDelta[0] = (int)basemana - (int)GetCreateMana();
            packet.PowerDelta[1] = 0;
            packet.PowerDelta[2] = 0;
            packet.PowerDelta[3] = 0;
            packet.PowerDelta[4] = 0;
            packet.PowerDelta[5] = 0;
            packet.PowerDelta[6] = 0;

            for (Stats i = Stats.Strength; i < Stats.Max; ++i)
                packet.StatDelta[(int)i] = info.Stats[(int)i] - (int)GetCreateStat(i);

            packet.NumNewTalents = (int)(Global.DB2Mgr.GetNumTalentsAtLevel(level, GetClass()) - Global.DB2Mgr.GetNumTalentsAtLevel(oldLevel, GetClass()));
            packet.NumNewPvpTalentSlots = Global.DB2Mgr.GetPvpTalentNumSlotsAtLevel(level, GetClass()) - Global.DB2Mgr.GetPvpTalentNumSlotsAtLevel(oldLevel, GetClass());

            SendPacket(packet);

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NextLevelXP), Global.ObjectMgr.GetXPForLevel(level));

            //update level, max level of Skills
            _playedTimeLevel = 0; // Level Played Time reset

            _ApplyAllLevelScaleItemMods(false);

            SetLevel(level, false);

            UpdateSkillsForLevel();
            LearnDefaultSkills();
            LearnSpecializationSpells();

            // save base values (bonuses already included in stored Stats
            for (var i = Stats.Strength; i < Stats.Max; ++i)
                SetCreateStat(i, info.Stats[(int)i]);

            SetCreateHealth(0);
            SetCreateMana(basemana);

            InitTalentForLevel();
            InitTaxiNodesForLevel();

            UpdateAllStats();

            _ApplyAllLevelScaleItemMods(true); // Moved to above SetFullHealth so player will have full health from Heirlooms

            Aura artifactAura = GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

            if (artifactAura != null)
            {
                Item artifact = GetItemByGuid(artifactAura.GetCastItemGUID());

                artifact?.CheckArtifactRelicSlotUnlock(this);
            }

            // Only health and mana are set to maximum.
            SetFullHealth();
            SetFullPower(PowerType.Mana);

            // update level to hunter/summon pet
            Pet pet = GetPet();

            if (pet)
                pet.SynchronizeLevelWithOwner();

            MailLevelReward mailReward = Global.ObjectMgr.GetMailLevelReward(level, (uint)SharedConst.GetMaskForRace(GetRace()));

            if (mailReward != null)
            {
                //- TODO: Poor design of mail system
                SQLTransaction trans = new();
                new MailDraft(mailReward.MailTemplateId).SendMailTo(trans, this, new MailSender(MailMessageType.Creature, mailReward.SenderEntry));
                DB.Characters.CommitTransaction(trans);
            }

            UpdateCriteria(CriteriaType.ReachLevel);
            UpdateCriteria(CriteriaType.ActivelyReachLevel, level);

            PushQuests();

            Global.ScriptMgr.ForEach<IPlayerOnLevelChanged>(p => p.OnLevelChanged(this, (byte)oldLevel));
        }

        public bool CanParry()
        {
            return _canParry;
        }

        public bool CanBlock()
        {
            return _canBlock;
        }

        public void ToggleAFK()
        {
            if (IsAFK())
                RemovePlayerFlag(PlayerFlags.AFK);
            else
                SetPlayerFlag(PlayerFlags.AFK);

            // afk player not allowed in Battleground
            if (!IsGameMaster() &&
                IsAFK() &&
                InBattleground() &&
                !InArena())
                LeaveBattleground();
        }

        public void ToggleDND()
        {
            if (IsDND())
                RemovePlayerFlag(PlayerFlags.DND);
            else
                SetPlayerFlag(PlayerFlags.DND);
        }

        public bool IsAFK()
        {
            return HasPlayerFlag(PlayerFlags.AFK);
        }

        public bool IsDND()
        {
            return HasPlayerFlag(PlayerFlags.DND);
        }

        public bool IsMaxLevel()
        {
            if (ConfigMgr.GetDefaultValue("character.MaxLevelDeterminedByConfig", false))
                return GetLevel() >= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel);

            return GetLevel() >= ActivePlayerData.MaxLevel;
        }

        public ChatFlags GetChatFlags()
        {
            ChatFlags tag = ChatFlags.None;

            if (IsGMChat())
                tag |= ChatFlags.GM;

            if (IsDND())
                tag |= ChatFlags.DND;

            if (IsAFK())
                tag |= ChatFlags.AFK;

            if (IsDeveloper())
                tag |= ChatFlags.Dev;

            return tag;
        }

        public void InitDisplayIds()
        {
            ChrModelRecord model = Global.DB2Mgr.GetChrModel(GetRace(), GetNativeGender());

            if (model == null)
            {
                Log.outError(LogFilter.Player, $"Player {GetGUID()} has incorrect race/Gender pair. Can't init display ids.");

                return;
            }

            SetDisplayId(model.DisplayID);
            SetNativeDisplayId(model.DisplayID);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.StateAnimID), Global.DB2Mgr.GetEmptyAnimStateID());
        }

        //Creature
        public Creature GetNPCIfCanInteractWith(ObjectGuid guid, NPCFlags npcFlags, NPCFlags2 npcFlags2)
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
            if (!IsAlive() &&
                !Convert.ToBoolean(creature.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.VisibleToGhosts))
                return null;

            // alive or spirit healer
            if (!creature.IsAlive() &&
                !Convert.ToBoolean(creature.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.InteractWhileDead))
                return null;

            // appropriate npc Type
            bool hasNpcFlags()
            {
                if (npcFlags == 0 &&
                    npcFlags2 == 0)
                    return true;

                if (creature.HasNpcFlag(npcFlags))
                    return true;

                if (creature.HasNpcFlag2(npcFlags2))
                    return true;

                return false;
            }

            ;

            if (!hasNpcFlags())
                return null;

            // not allow interaction under control, but allow with own pets
            if (!creature.GetCharmerGUID().IsEmpty())
                return null;

            // not unfriendly/hostile
            if (creature.GetReactionTo(this) <= ReputationRank.Unfriendly)
                return null;

            // not too far, taken from CGGameUI::SetInteractTarget
            if (!creature.IsWithinDistInMap(this, creature.GetCombatReach() + 4.0f))
                return null;

            return creature;
        }

        public GameObject GetGameObjectIfCanInteractWith(ObjectGuid guid)
        {
            if (guid.IsEmpty())
                return null;

            if (!IsInWorld)
                return null;

            if (IsInFlight())
                return null;

            // exist
            GameObject go = ObjectAccessor.GetGameObject(this, guid);

            if (go == null)
                return null;

            // Players cannot interact with gameobjects that use the "Point" icon
            if (go.GetGoInfo().IconName == "Point")
                return null;

            if (!go.IsWithinDistInMap(this))
                return null;

            return go;
        }

        public GameObject GetGameObjectIfCanInteractWith(ObjectGuid guid, GameObjectTypes type)
        {
            GameObject go = GetGameObjectIfCanInteractWith(guid);

            if (!go)
                return null;

            if (go.GetGoType() != type)
                return null;

            return go;
        }

        public void SendInitialPacketsBeforeAddToMap()
        {
            if (!_teleport_options.HasAnyFlag(TeleportToOptions.Seamless))
            {
                MovementCounter = 0;
                GetSession().ResetTimeSync();
            }

            GetSession().SendTimeSync();

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
            SendUnlearnSpells();

            // SMSG_SEND_SPELL_HISTORY
            SendSpellHistory sendSpellHistory = new();
            GetSpellHistory().WritePacket(sendSpellHistory);
            SendPacket(sendSpellHistory);

            // SMSG_SEND_SPELL_CHARGES
            SendSpellCharges sendSpellCharges = new();
            GetSpellHistory().WritePacket(sendSpellCharges);
            SendPacket(sendSpellCharges);

            ActiveGlyphs activeGlyphs = new();

            foreach (uint glyphId in GetGlyphs(GetActiveTalentGroup()))
            {
                List<uint> bindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphId);

                foreach (uint bindableSpell in bindableSpells)
                    if (HasSpell(bindableSpell) &&
                        !_overrideSpells.ContainsKey(bindableSpell))
                        activeGlyphs.Glyphs.Add(new GlyphBinding(bindableSpell, (ushort)glyphId));
            }

            activeGlyphs.IsFullUpdate = true;
            SendPacket(activeGlyphs);

            // SMSG_ACTION_BUTTONS
            SendInitialActionButtons();

            // SMSG_INITIALIZE_FACTIONS
            _reputationMgr.SendInitialReputations();

            // SMSG_SETUP_CURRENCY
            SendCurrencies();

            // SMSG_EQUIPMENT_SET_LIST
            SendEquipmentSetList();

            _achievementSys.SendAllData(this);
            _questObjectiveCriteriaMgr.SendAllData(this);

            // SMSG_LOGIN_SETTIMESPEED
            float TimeSpeed = 0.01666667f;
            LoginSetTimeSpeed loginSetTimeSpeed = new();
            loginSetTimeSpeed.NewSpeed = TimeSpeed;
            loginSetTimeSpeed.GameTime = (uint)GameTime.GetGameTime();
            loginSetTimeSpeed.ServerTime = (uint)GameTime.GetGameTime();
            loginSetTimeSpeed.GameTimeHolidayOffset = 0; // @todo
            loginSetTimeSpeed.ServerTimeHolidayOffset = 0; // @todo
            SendPacket(loginSetTimeSpeed);

            // SMSG_WORLD_SERVER_INFO
            WorldServerInfo worldServerInfo = new();
            worldServerInfo.InstanceGroupSize = GetMap().GetMapDifficulty().MaxPlayers; // @todo
            worldServerInfo.IsTournamentRealm = false;                                  // @todo
            worldServerInfo.RestrictedAccountMaxLevel = null;                                   // @todo
            worldServerInfo.RestrictedAccountMaxMoney = null;                                   // @todo
            worldServerInfo.DifficultyID = (uint)GetMap().GetDifficultyID();
            // worldServerInfo.XRealmPvpAlert;  // @todo
            SendPacket(worldServerInfo);

            // Spell modifiers
            SendSpellModifiers();

            // SMSG_ACCOUNT_MOUNT_UPDATE
            AccountMountUpdate mountUpdate = new();
            mountUpdate.IsFullUpdate = true;
            mountUpdate.Mounts = GetSession().GetCollectionMgr().GetAccountMounts();
            SendPacket(mountUpdate);

            // SMSG_ACCOUNT_TOYS_UPDATE
            AccountToyUpdate toyUpdate = new();
            toyUpdate.IsFullUpdate = true;
            toyUpdate.Toys = GetSession().GetCollectionMgr().GetAccountToys();
            SendPacket(toyUpdate);

            // SMSG_ACCOUNT_HEIRLOOM_UPDATE
            AccountHeirloomUpdate heirloomUpdate = new();
            heirloomUpdate.IsFullUpdate = true;
            heirloomUpdate.Heirlooms = GetSession().GetCollectionMgr().GetAccountHeirlooms();
            SendPacket(heirloomUpdate);

            GetSession().GetCollectionMgr().SendFavoriteAppearances();

            InitialSetup initialSetup = new();
            initialSetup.ServerExpansionLevel = (byte)WorldConfig.GetIntValue(WorldCfg.Expansion);
            SendPacket(initialSetup);

            SetMovedUnit(this);
        }

        public void SendInitialPacketsAfterAddToMap()
        {
            UpdateVisibilityForPlayer();

            // update zone
            uint newzone, newarea;
            GetZoneAndAreaId(out newzone, out newarea);
            UpdateZone(newzone, newarea); // also call SendInitWorldStates();

            GetSession().SendLoadCUFProfiles();

            CastSpell(this, 836, true); // LOGINEFFECT

            // set some aura effects that send packet to player client after add player to map
            // SendMessageToSet not send it to player not it map, only for aura that not changed anything at re-apply
            // same Auras State lost at far teleport, send it one more Time in this case also
            AuraType[] auratypes =
            {
                AuraType.ModFear, AuraType.Transform, AuraType.WaterWalk, AuraType.FeatherFall, AuraType.Hover, AuraType.SafeFall, AuraType.Fly, AuraType.ModIncreaseMountedFlightSpeed, AuraType.None
            };

            foreach (var aura in auratypes)
            {
                var auraList = GetAuraEffectsByType(aura);

                if (!auraList.Empty())
                    auraList.First().HandleEffect(this, AuraEffectHandleModes.SendForClient, true);
            }

            if (HasAuraType(AuraType.ModStun) ||
                HasAuraType(AuraType.ModStunDisableGravity))
                SetRooted(true);

            MoveSetCompoundState setCompoundState = new();

            // manual send package (have code in HandleEffect(this, AURA_EFFECT_HANDLE_SEND_FOR_CLIENT, true); that must not be re-applied.
            if (HasAuraType(AuraType.ModRoot) ||
                HasAuraType(AuraType.ModRoot2) ||
                HasAuraType(AuraType.ModRootDisableGravity))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveRoot, MovementCounter++));

            if (HasAuraType(AuraType.FeatherFall))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetFeatherFall, MovementCounter++));

            if (HasAuraType(AuraType.WaterWalk))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetWaterWalk, MovementCounter++));

            if (HasAuraType(AuraType.Hover))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetHovering, MovementCounter++));

            if (HasAuraType(AuraType.ModRootDisableGravity) ||
                HasAuraType(AuraType.ModStunDisableGravity))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveDisableGravity, MovementCounter++));

            if (HasAuraType(AuraType.CanTurnWhileFalling))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetCanTurnWhileFalling, MovementCounter++));

            if (HasAura(196055)) //DH DoubleJump
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveEnableDoubleJump, MovementCounter++));

            if (HasAuraType(AuraType.IgnoreMovementForces))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetIgnoreMovementForces, MovementCounter++));

            if (HasAuraType(AuraType.DisableInertia))
                setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveDisableInertia, MovementCounter++));

            if (!setCompoundState.StateChanges.Empty())
            {
                setCompoundState.MoverGUID = GetGUID();
                SendPacket(setCompoundState);
            }

            SendAurasForTarget(this);
            SendEnchantmentDurations(); // must be after add to map
            SendItemDurations();        // must be after add to map

            // raid downscaling - send difficulty to player
            if (GetMap().IsRaid())
            {
                Difficulty mapDifficulty = GetMap().GetDifficultyID();
                var difficulty = CliDB.DifficultyStorage.LookupByKey(mapDifficulty);
                SendRaidDifficulty((difficulty.Flags & DifficultyFlags.Legacy) != 0, (int)mapDifficulty);
            }
            else if (GetMap().IsNonRaidDungeon())
            {
                SendDungeonDifficulty((int)GetMap().GetDifficultyID());
            }

            PhasingHandler.OnMapChange(this);

            _garrison?.SendRemoteInfo();

            UpdateItemLevelAreaBasedScaling();

            if (!GetPlayerSharingQuest().IsEmpty())
            {
                Quest quest = Global.ObjectMgr.GetQuestTemplate(GetSharedQuestID());

                if (quest != null)
                    PlayerTalkClass.SendQuestGiverQuestDetails(quest, GetGUID(), true, false);
                else
                    ClearQuestSharingInfo();
            }

            GetSceneMgr().TriggerDelayedScenes();
        }

        public void RemoveSocial()
        {
            Global.SocialMgr.RemovePlayerSocial(GetGUID());
            _social = null;
        }

        public void SaveRecallPosition()
        {
            _recall_location = new WorldLocation(this);
            _recall_instanceId = GetInstanceId();
        }

        public void Recall()
        {
            TeleportTo(_recall_location, 0, _recall_instanceId);
        }

        public uint GetSaveTimer()
        {
            return _nextSave;
        }

        public void InitStatsForLevel(bool reapplyMods = false)
        {
            if (reapplyMods) //reapply Stats values only on .reset Stats (level) command
                _RemoveAllStatBonuses();

            uint basemana;
            Global.ObjectMgr.GetPlayerClassLevelInfo(GetClass(), GetLevel(), out basemana);

            PlayerLevelInfo info = Global.ObjectMgr.GetPlayerLevelInfo(GetRace(), GetClass(), GetLevel());

            int exp_max_lvl = (int)Global.ObjectMgr.GetMaxLevelForExpansion(GetSession().GetExpansion());
            int conf_max_lvl = WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel);

            if (exp_max_lvl == SharedConst.DefaultMaxLevel ||
                exp_max_lvl >= conf_max_lvl)
                SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.MaxLevel), conf_max_lvl);
            else
                SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.MaxLevel), exp_max_lvl);

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NextLevelXP), Global.ObjectMgr.GetXPForLevel(GetLevel()));

            if (ActivePlayerData.XP >= ActivePlayerData.NextLevelXP)
                SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.XP), ActivePlayerData.NextLevelXP - 1);

            // reset before any aura State sources (health set/aura apply)
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AuraState), 0u);

            UpdateSkillsForLevel();

            // set default cast Time Multiplier
            SetModCastingSpeed(1.0f);
            SetModSpellHaste(1.0f);
            SetModHaste(1.0f);
            SetModRangedHaste(1.0f);
            SetModHasteRegen(1.0f);
            SetModTimeRate(1.0f);

            // reset size before reapply Auras
            SetObjectScale(1.0f);

            // save base values (bonuses already included in stored Stats
            for (var i = Stats.Strength; i < Stats.Max; ++i)
                SetCreateStat(i, info.Stats[(int)i]);

            for (var i = Stats.Strength; i < Stats.Max; ++i)
                SetStat(i, info.Stats[(int)i]);

            SetCreateHealth(0);

            //set create powers
            SetCreateMana(basemana);

            SetArmor((int)(GetCreateStat(Stats.Agility) * 2), 0);

            InitStatBuffMods();

            //reset rating fields values
            for (int index = 0; index < (int)CombatRating.Max; ++index)
                SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CombatRatings, index), 0u);

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModHealingDonePos), 0);
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModHealingPercent), 1.0f);
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModPeriodicHealingDonePercent), 1.0f);

            for (byte i = 0; i < (int)SpellSchools.Max; ++i)
            {
                SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDoneNeg, i), 0);
                SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDonePos, i), 0);
                SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDonePercent, i), 1.0f);
                SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModHealingDonePercent, i), 1.0f);
            }

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModSpellPowerPercent), 1.0f);

            //reset attack power, Damage and attack speed fields
            for (WeaponAttackType attackType = 0; attackType < WeaponAttackType.Max; ++attackType)
                SetBaseAttackTime(attackType, SharedConst.BaseAttackTime);

            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinDamage), 0.0f);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxDamage), 0.0f);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinOffHandDamage), 0.0f);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxOffHandDamage), 0.0f);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinRangedDamage), 0.0f);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxRangedDamage), 0.0f);

            for (int i = 0; i < 3; ++i)
            {
                SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.WeaponDmgMultipliers, i), 1.0f);
                SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.WeaponAtkSpeedMultipliers, i), 1.0f);
            }

            SetAttackPower(0);
            SetAttackPowerMultiplier(0.0f);
            SetRangedAttackPower(0);
            SetRangedAttackPowerMultiplier(0.0f);

            // Base crit values (will be recalculated in UpdateAllStats() at loading and in _ApplyAllStatBonuses() at reset
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CritPercentage), 0.0f);
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OffhandCritPercentage), 0.0f);
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.RangedCritPercentage), 0.0f);

            // Init spell schools (will be recalculated in UpdateAllStats() at loading and in _ApplyAllStatBonuses() at reset
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellCritPercentage), 0.0f);

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ParryPercentage), 0.0f);
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.BlockPercentage), 0.0f);

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ShieldBlock), 0u);

            // Dodge percentage
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.DodgePercentage), 0.0f);

            // set armor (resistance 0) to original value (create_agility*2)
            SetArmor((int)(GetCreateStat(Stats.Agility) * 2), 0);
            SetBonusResistanceMod(SpellSchools.Normal, 0);

            // set other resistance to original value (0)
            for (var spellSchool = SpellSchools.Holy; spellSchool < SpellSchools.Max; ++spellSchool)
            {
                SetResistance(spellSchool, 0);
                SetBonusResistanceMod(spellSchool, 0);
            }

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModTargetResistance), 0);
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModTargetPhysicalResistance), 0);

            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.ManaCostModifier, i), 0);

            // Reset no reagent cost field
            SetNoRegentCostMask(new FlagArray128());

            // Init _data for form but skip reapply Item mods for form
            InitDataForForm(reapplyMods);

            // save new Stats
            for (var i = PowerType.Mana; i < PowerType.Max; ++i)
                SetMaxPower(i, GetCreatePowerValue(i));

            SetMaxHealth(0); // stamina bonus will applied later

            // cleanup mounted State (it will set correctly at aura loading if player saved at Mount.
            SetMountDisplayId(0);

            // cleanup unit Flags (will be re-applied if need at aura load).
            RemoveUnitFlag(UnitFlags.NonAttackable |
                           UnitFlags.RemoveClientControl |
                           UnitFlags.NotAttackable1 |
                           UnitFlags.ImmuneToPc |
                           UnitFlags.ImmuneToNpc |
                           UnitFlags.Looting |
                           UnitFlags.PetInCombat |
                           UnitFlags.Silenced |
                           UnitFlags.Pacified |
                           UnitFlags.Stunned |
                           UnitFlags.InCombat |
                           UnitFlags.Disarmed |
                           UnitFlags.Confused |
                           UnitFlags.Fleeing |
                           UnitFlags.Uninteractible |
                           UnitFlags.Skinnable |
                           UnitFlags.Mount |
                           UnitFlags.OnTaxi);

            SetUnitFlag(UnitFlags.PlayerControlled); // must be set

            SetUnitFlag2(UnitFlags2.RegeneratePower); // must be set

            // cleanup player Flags (will be re-applied if need at aura load), to avoid have ghost flag without ghost aura, for example.
            RemovePlayerFlag(PlayerFlags.AFK | PlayerFlags.DND | PlayerFlags.GM | PlayerFlags.Ghost);

            RemoveVisFlag(UnitVisFlags.All); // one form stealth modified bytes
            RemovePvpFlag(UnitPVPStateFlags.FFAPvp | UnitPVPStateFlags.Sanctuary);

            // restore if need some important Flags
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LocalRegenFlags), (byte)0);
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.AuraVision), (byte)0);

            if (reapplyMods) // reapply Stats values only on .reset Stats (level) command
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

            if (ssEntry != null &&
                ssEntry.CombatRoundTime != 0)
            {
                SetBaseAttackTime(WeaponAttackType.BaseAttack, ssEntry.CombatRoundTime);
                SetBaseAttackTime(WeaponAttackType.OffAttack, ssEntry.CombatRoundTime);
                SetBaseAttackTime(WeaponAttackType.RangedAttack, SharedConst.BaseAttackTime);
            }
            else
            {
                SetRegularAttackTime();
            }

            UpdateDisplayPower();

            // update Auras at form change, ignore this at mods reapply (.reset Stats/etc) when form not change.
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
            return _reputationMgr;
        }

        public void SetReputation(uint factionentry, int value)
        {
            GetReputationMgr().SetReputation(CliDB.FactionStorage.LookupByKey(factionentry), value);
        }

        public int GetReputation(uint factionentry)
        {
            return GetReputationMgr().GetReputation(CliDB.FactionStorage.LookupByKey(factionentry));
        }

        public void ClearWhisperWhiteList()
        {
            _whisperList.Clear();
        }

        public void AddWhisperWhiteList(ObjectGuid guid)
        {
            _whisperList.Add(guid);
        }

        public bool IsInWhisperWhiteList(ObjectGuid guid)
        {
            return _whisperList.Contains(guid);
        }

        public void RemoveFromWhisperWhiteList(ObjectGuid guid)
        {
            _whisperList.Remove(guid);
        }

        public void SetFallInformation(uint time, float z)
        {
            _lastFallTime = time;
            _lastFallZ = z;
        }

        public PlayerCreateMode GetCreateMode()
        {
            return _createMode;
        }

        public byte GetCinematic()
        {
            return _cinematic;
        }

        public void SetCinematic(byte cine)
        {
            _cinematic = cine;
        }

        public uint GetMovie()
        {
            return _movie;
        }

        public void SetMovie(uint movie)
        {
            _movie = movie;
        }

        public void SendCinematicStart(uint CinematicSequenceId)
        {
            TriggerCinematic packet = new();
            packet.CinematicID = CinematicSequenceId;
            SendPacket(packet);

            CinematicSequencesRecord sequence = CliDB.CinematicSequencesStorage.LookupByKey(CinematicSequenceId);

            if (sequence != null)
                _cinematicMgr.BeginCinematic(sequence);
        }

        public void SendMovieStart(uint movieId)
        {
            SetMovie(movieId);
            TriggerMovie packet = new();
            packet.MovieID = movieId;
            SendPacket(packet);
        }

        public override void SetObjectScale(float scale)
        {
            base.SetObjectScale(scale);
            SetBoundingRadius(scale * SharedConst.DefaultPlayerBoundingRadius);
            SetCombatReach(scale * SharedConst.DefaultPlayerCombatReach);

            if (IsInWorld)
                SendMovementSetCollisionHeight(GetCollisionHeight(), UpdateCollisionHeightReason.Scale);
        }

        public bool HasRaceChanged()
        {
            return _extraFlags.HasFlag(PlayerExtraFlags.HasRaceChanged);
        }

        public void SetHasRaceChanged()
        {
            _extraFlags |= PlayerExtraFlags.HasRaceChanged;
        }

        public bool HasBeenGrantedLevelsFromRaF()
        {
            return _extraFlags.HasFlag(PlayerExtraFlags.GrantedLevelsFromRaf);
        }

        public void SetBeenGrantedLevelsFromRaF()
        {
            _extraFlags |= PlayerExtraFlags.GrantedLevelsFromRaf;
        }

        public bool HasLevelBoosted()
        {
            return _extraFlags.HasFlag(PlayerExtraFlags.LevelBoosted);
        }

        public void SetHasLevelBoosted()
        {
            _extraFlags |= PlayerExtraFlags.LevelBoosted;
        }

        public uint GetXP()
        {
            return ActivePlayerData.XP;
        }

        public uint GetXPForNextLevel()
        {
            return ActivePlayerData.NextLevelXP;
        }

        public void SetXP(uint xp)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.XP), xp);

            int playerLevelDelta = 0;

            // If XP < 50%, player should see scaling creature with -1 level except for level max
            if (GetLevel() < SharedConst.MaxLevel &&
                xp < (ActivePlayerData.NextLevelXP / 2))
                playerLevelDelta = -1;

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ScalingPlayerLevelDelta), playerLevelDelta);
        }

        public void GiveXP(uint xp, Unit victim, float group_rate = 1.0f)
        {
            if (xp < 1)
                return;

            if (!IsAlive() &&
                GetBattlegroundId() == 0)
                return;

            if (HasPlayerFlag(PlayerFlags.NoXPGain))
                return;

            if (victim != null &&
                victim.IsTypeId(TypeId.Unit) &&
                !victim.ToCreature().HasLootRecipient())
                return;

            uint level = GetLevel();

            Global.ScriptMgr.ForEach<IPlayerOnGiveXP>(p => p.OnGiveXP(this, ref xp, victim));

            // XP to money conversion processed in Player.RewardQuest
            if (IsMaxLevel())
                return;

            uint bonus_xp;
            bool recruitAFriend = GetsRecruitAFriendBonus(true);

            // RaF does NOT stack with rested experience
            if (recruitAFriend)
                bonus_xp = 2 * xp; // xp + bonus_xp must add up to 3 * xp for RaF; calculation for quests done client-side
            else
                bonus_xp = victim != null ? _restMgr.GetRestBonusFor(RestTypes.XP, xp) : 0; // XP resting bonus

            LogXPGain packet = new();
            packet.Victim = victim ? victim.GetGUID() : ObjectGuid.Empty;
            packet.Original = (int)(xp + bonus_xp);
            packet.Reason = victim ? PlayerLogXPReason.Kill : PlayerLogXPReason.NoKill;
            packet.Amount = (int)xp;
            packet.GroupBonus = group_rate;
            SendPacket(packet);

            uint nextLvlXP = GetXPForNextLevel();
            uint newXP = GetXP() + xp + bonus_xp;

            while (newXP >= nextLvlXP && !IsMaxLevel())
            {
                newXP -= nextLvlXP;

                if (!IsMaxLevel())
                    GiveLevel(level + 1);

                level = GetLevel();
                nextLvlXP = GetXPForNextLevel();
            }

            SetXP(newXP);
        }

        public void HandleBaseModFlatValue(BaseModGroup modGroup, float amount, bool apply)
        {
            if (modGroup >= BaseModGroup.End)
            {
                Log.outError(LogFilter.Spells, $"Player.HandleBaseModFlatValue: Invalid BaseModGroup ({modGroup}) for player '{GetName()}' ({GetGUID()})");

                return;
            }

            _auraBaseFlatMod[(int)modGroup] += apply ? amount : -amount;
            UpdateBaseModGroup(modGroup);
        }

        public void ApplyBaseModPctValue(BaseModGroup modGroup, float pct)
        {
            if (modGroup >= BaseModGroup.End)
            {
                Log.outError(LogFilter.Spells, $"Player.ApplyBaseModPctValue: Invalid BaseModGroup/BaseModType ({modGroup}/{BaseModType.FlatMod}) for player '{GetName()}' ({GetGUID()})");

                return;
            }

            _auraBasePctMod[(int)modGroup] = MathFunctions.AddPct(_auraBasePctMod[(int)modGroup], pct);
            UpdateBaseModGroup(modGroup);
        }

        public void SetBaseModFlatValue(BaseModGroup modGroup, float val)
        {
            if (_auraBaseFlatMod[(int)modGroup] == val)
                return;

            _auraBaseFlatMod[(int)modGroup] = val;
            UpdateBaseModGroup(modGroup);
        }

        public void SetBaseModPctValue(BaseModGroup modGroup, float val)
        {
            if (_auraBasePctMod[(int)modGroup] == val)
                return;

            _auraBasePctMod[(int)modGroup] = val;
            UpdateBaseModGroup(modGroup);
        }

        public override void UpdateDamageDoneMods(WeaponAttackType attackType, int skipEnchantSlot = -1)
        {
            base.UpdateDamageDoneMods(attackType, skipEnchantSlot);

            UnitMods unitMod = attackType switch
            {
                WeaponAttackType.BaseAttack => UnitMods.DamageMainHand,
                WeaponAttackType.OffAttack => UnitMods.DamageOffHand,
                WeaponAttackType.RangedAttack => UnitMods.DamageRanged,
                _ => throw new NotImplementedException()
            };

            float amount = 0.0f;
            Item item = GetWeaponForAttack(attackType, true);

            if (item == null)
                return;

            for (var slot = EnchantmentSlot.Perm; slot < EnchantmentSlot.Max; ++slot)
            {
                if (skipEnchantSlot == (int)slot)
                    continue;

                SpellItemEnchantmentRecord enchantmentEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetEnchantmentId(slot));

                if (enchantmentEntry == null)
                    continue;

                for (byte i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                    switch (enchantmentEntry.Effect[i])
                    {
                        case ItemEnchantmentType.Damage:
                            amount += enchantmentEntry.EffectScalingPoints[i];

                            break;
                        case ItemEnchantmentType.Totem:
                            if (GetClass() == Class.Shaman)
                                amount += enchantmentEntry.EffectScalingPoints[i] * item.GetTemplate().GetDelay() / 1000.0f;

                            break;
                        default:
                            break;
                    }
            }

            HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, amount, true);
        }

        public byte GetDrunkValue()
        {
            return PlayerData.Inebriation;
        }

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
                InvisibilityDetect.AddFlag(InvisibilityType.Drunk);
                InvisibilityDetect.SetValue(InvisibilityType.Drunk, drunkPercent);
            }
            else if (!HasAuraType(AuraType.ModFakeInebriate) &&
                     newDrunkValue == 0)
            {
                InvisibilityDetect.DelFlag(InvisibilityType.Drunk);
            }

            DrunkenState newDrunkenState = GetDrunkenstateByValue(newDrunkValue);
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.Inebriation), newDrunkValue);
            UpdateObjectVisibility();

            if (!isSobering)
                _drunkTimer = 0; // reset sobering timer

            if (newDrunkenState == oldDrunkenState)
                return;

            CrossedInebriationThreshold data = new();
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

        public uint GetDeathTimer()
        {
            return _deathTimer;
        }

        public bool ActivateTaxiPathTo(List<uint> nodes, Creature npc = null, uint spellid = 0, uint preferredMountDisplay = 0)
        {
            if (nodes.Count < 2)
            {
                GetSession().SendActivateTaxiReply(ActivateTaxiReply.NoSuchPath);

                return false;
            }

            // not let cheating with start flight in Time of logout process || while in combat || has Type State: stunned || has Type State: root
            if (GetSession().IsLogingOut() ||
                IsInCombat() ||
                HasUnitState(UnitState.Stunned) ||
                HasUnitState(UnitState.Root))
            {
                GetSession().SendActivateTaxiReply(ActivateTaxiReply.PlayerBusy);

                return false;
            }

            if (HasUnitFlag(UnitFlags.RemoveClientControl))
                return false;

            // taximaster case
            if (npc != null)
            {
                // not let cheating with start flight mounted
                RemoveAurasByType(AuraType.Mounted);

                if (GetDisplayId() != GetNativeDisplayId())
                    RestoreDisplayId(true);

                if (IsDisallowedMountForm(GetTransformSpell(), ShapeShiftForm.None, GetDisplayId()))
                {
                    GetSession().SendActivateTaxiReply(ActivateTaxiReply.PlayerShapeshifted);

                    return false;
                }

                // not let cheating with start flight in Time of logout process || if casting not finished || while in combat || if not use Spell's with EffectSendTaxi
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
                    if (spell.SpellInfo.Id != spellid)
                        InterruptSpell(CurrentSpellTypes.Generic, false);

                InterruptSpell(CurrentSpellTypes.AutoRepeat, false);

                spell = GetCurrentSpell(CurrentSpellTypes.Channeled);

                if (spell != null)
                    if (spell.SpellInfo.Id != spellid)
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
            Taxi.ClearTaxiDestinations();

            // 0 element current node
            Taxi.AddTaxiDestination(sourcenode);

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
                    Taxi.ClearTaxiDestinations();

                    return false;
                }

                totalcost += cost;

                if (i == 1)
                    firstcost = cost;

                if (prevnode == sourcenode)
                    sourcepath = path;

                Taxi.AddTaxiDestination(lastnode);

                prevnode = lastnode;
            }

            // get Mount model (in case non taximaster (npc == NULL) allow more wide lookup)
            //
            // Hack-Fix for Alliance not being able to use Acherus taxi. There is
            // only one Mount ID for both sides. Probably not good to use 315 in case DBC nodes
            // change but I couldn't find a suitable alternative. OK to use class because only DK
            // can use this taxi.
            uint mount_display_id;

            if (node.Flags.HasAnyFlag(TaxiNodeFlags.UseFavoriteMount) &&
                preferredMountDisplay != 0)
                mount_display_id = preferredMountDisplay;
            else
                mount_display_id = Global.ObjectMgr.GetTaxiMountDisplayId(sourcenode, GetTeam(), npc == null || (sourcenode == 315 && GetClass() == Class.Deathknight));

            // in spell case allow 0 model
            if ((mount_display_id == 0 && spellid == 0) ||
                sourcepath == 0)
            {
                GetSession().SendActivateTaxiReply(ActivateTaxiReply.UnspecifiedServerError);
                Taxi.ClearTaxiDestinations();

                return false;
            }

            ulong money = GetMoney();

            if (npc != null)
            {
                float discount = GetReputationPriceDiscount(npc);
                totalcost = (uint)Math.Ceiling(totalcost * discount);
                firstcost = (uint)Math.Ceiling(firstcost * discount);
                Taxi.SetFlightMasterFactionTemplateId(npc.GetFaction());
            }
            else
            {
                Taxi.SetFlightMasterFactionTemplateId(0);
            }

            if (money < totalcost)
            {
                GetSession().SendActivateTaxiReply(ActivateTaxiReply.NotEnoughMoney);
                Taxi.ClearTaxiDestinations();

                return false;
            }

            //Checks and preparations done, DO FLIGHT
            UpdateCriteria(CriteriaType.BuyTaxi, 1);

            if (WorldConfig.GetBoolValue(WorldCfg.InstantTaxi))
            {
                var lastPathNode = CliDB.TaxiNodesStorage.LookupByKey(nodes[^1]);
                Taxi.ClearTaxiDestinations();
                ModifyMoney(-totalcost);
                UpdateCriteria(CriteriaType.MoneySpentOnTaxis, totalcost);
                TeleportTo(lastPathNode.ContinentID, lastPathNode.Pos.X, lastPathNode.Pos.Y, lastPathNode.Pos.Z, GetOrientation());

                return false;
            }
            else
            {
                ModifyMoney(-firstcost);
                UpdateCriteria(CriteriaType.MoneySpentOnTaxis, firstcost);
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

            List<uint> nodes = new();

            nodes.Add(entry.FromTaxiNode);
            nodes.Add(entry.ToTaxiNode);

            return ActivateTaxiPathTo(nodes, null, spellid);
        }

        public void FinishTaxiFlight()
        {
            if (!IsInFlight())
                return;

            GetMotionMaster().Remove(MovementGeneratorType.Flight);
            Taxi.ClearTaxiDestinations(); // not destinations, clear source node
        }

        public void CleanupAfterTaxiFlight()
        {
            Taxi.ClearTaxiDestinations(); // not destinations, clear source node
            Dismount();
            RemoveUnitFlag(UnitFlags.RemoveClientControl | UnitFlags.OnTaxi);
        }

        public void ContinueTaxiFlight()
        {
            uint sourceNode = Taxi.GetTaxiSource();

            if (sourceNode == 0)
                return;

            Log.outDebug(LogFilter.Unit, "WORLD: Restart character {0} taxi flight", GetGUID().ToString());

            uint mountDisplayId = Global.ObjectMgr.GetTaxiMountDisplayId(sourceNode, GetTeam(), true);

            if (mountDisplayId == 0)
                return;

            uint path = Taxi.GetCurrentTaxiPath();

            // search appropriate start path node
            uint startNode = 0;

            var nodeList = CliDB.TaxiPathNodesByPath[path];

            float distPrev;
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

        public bool GetsRecruitAFriendBonus(bool forXP)
        {
            bool recruitAFriend = false;

            if (GetLevel() <= WorldConfig.GetIntValue(WorldCfg.MaxRecruitAFriendBonusPlayerLevel) ||
                !forXP)
            {
                Group group = GetGroup();

                if (group)
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                    {
                        Player player = refe.GetSource();

                        if (!player)
                            continue;

                        if (!player.IsAtRecruitAFriendDistance(this))
                            continue; // member (alive or dead) or his corpse at req. distance

                        if (forXP)
                        {
                            // level must be allowed to get RaF bonus
                            if (player.GetLevel() > WorldConfig.GetIntValue(WorldCfg.MaxRecruitAFriendBonusPlayerLevel))
                                continue;

                            // level difference must be small enough to get RaF bonus, UNLESS we are lower level
                            if (player.GetLevel() < GetLevel())
                                if (GetLevel() - player.GetLevel() > WorldConfig.GetIntValue(WorldCfg.MaxRecruitAFriendBonusPlayerLevelDifference))
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

            return recruitAFriend;
        }

        public TeleportToOptions GetTeleportOptions()
        {
            return _teleport_options;
        }

        public bool IsBeingTeleported()
        {
            return IsBeingTeleportedNear() || IsBeingTeleportedFar();
        }

        public bool IsBeingTeleportedNear()
        {
            return _mSemaphoreTeleport_Near;
        }

        public bool IsBeingTeleportedFar()
        {
            return _mSemaphoreTeleport_Far;
        }

        public bool IsBeingTeleportedSeamlessly()
        {
            return IsBeingTeleportedFar() && _teleport_options.HasAnyFlag(TeleportToOptions.Seamless);
        }

        public void SetSemaphoreTeleportNear(bool semphsetting)
        {
            _mSemaphoreTeleport_Near = semphsetting;
        }

        public void SetSemaphoreTeleportFar(bool semphsetting)
        {
            _mSemaphoreTeleport_Far = semphsetting;
        }

        public bool IsReagentBankUnlocked()
        {
            return HasPlayerFlagEx(PlayerFlagsEx.ReagentBankUnlocked);
        }

        public void UnlockReagentBank()
        {
            SetPlayerFlagEx(PlayerFlagsEx.ReagentBankUnlocked);
        }

        //new
        public uint DoRandomRoll(uint minimum, uint maximum)
        {
            Cypher.Assert(maximum <= 1000000);

            uint roll = RandomHelper.URand(minimum, maximum);

            RandomRoll randomRoll = new();
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
            if (factionTemplate == null ||
                factionTemplate.Faction == 0)
                return 1.0f;

            ReputationRank rank = GetReputationRank(factionTemplate.Faction);

            if (rank <= ReputationRank.Neutral)
                return 1.0f;

            return 1.0f - 0.05f * (rank - ReputationRank.Neutral);
        }

        public bool IsSpellFitByClassAndRace(uint spell_id)
        {
            long racemask = SharedConst.GetMaskForRace(GetRace());
            uint classmask = GetClassMask();

            var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spell_id);

            if (bounds.Empty())
                return true;

            foreach (var _spell_idx in bounds)
            {
                // skip wrong race Skills
                if (_spell_idx.RaceMask != 0 &&
                    (_spell_idx.RaceMask & racemask) == 0)
                    continue;

                // skip wrong class Skills
                if (_spell_idx.ClassMask != 0 &&
                    (_spell_idx.ClassMask & classmask) == 0)
                    continue;

                // skip wrong class and race skill saved in SkillRaceClassInfo.dbc
                if (Global.DB2Mgr.GetSkillRaceClassInfo(_spell_idx.SkillLine, GetRace(), GetClass()) == null)
                    continue;

                return true;
            }

            return false;
        }

        public uint GetFreePrimaryProfessionPoints()
        {
            return ActivePlayerData.CharacterPoints;
        }

        public bool HaveAtClient(WorldObject u)
        {
            bool one = u.GetGUID() == GetGUID();
            bool two = ClientGUIDs.Contains(u.GetGUID());

            return one || two;
        }

        public bool HasTitle(CharTitlesRecord title)
        {
            return HasTitle(title.MaskID);
        }

        public bool HasTitle(uint bitIndex)
        {
            uint fieldIndexOffset = bitIndex / 64;

            if (fieldIndexOffset >= ActivePlayerData.KnownTitles.Size())
                return false;

            ulong flag = 1ul << ((int)bitIndex % 64);

            return (ActivePlayerData.KnownTitles[(int)fieldIndexOffset] & flag) != 0;
        }

        public void SetTitle(CharTitlesRecord title, bool lost = false)
        {
            int fieldIndexOffset = (title.MaskID / 64);
            ulong flag = 1ul << (title.MaskID % 64);

            if (lost)
            {
                if (!HasTitle(title))
                    return;

                RemoveUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.KnownTitles, fieldIndexOffset), flag);
            }
            else
            {
                if (HasTitle(title))
                    return;

                SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.KnownTitles, fieldIndexOffset), flag);
            }

            TitleEarned packet = new(lost ? ServerOpcodes.TitleLost : ServerOpcodes.TitleEarned);
            packet.Index = title.MaskID;
            SendPacket(packet);
        }

        public void SetChosenTitle(uint title)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerTitle), title);
        }

        public void SetKnownTitles(int index, ulong mask)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.KnownTitles, index), mask);
        }

        public void SetViewpoint(WorldObject target, bool apply)
        {
            if (apply)
            {
                Log.outDebug(LogFilter.Maps, "Player.CreateViewpoint: Player {0} create seer {1} (TypeId: {2}).", GetName(), target.GetEntry(), target.GetTypeId());

                if (ActivePlayerData.FarsightObject != ObjectGuid.Empty)
                {
                    Log.outFatal(LogFilter.Player, "Player.CreateViewpoint: Player {0} cannot add new viewpoint!", GetName());

                    return;
                }

                SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.FarsightObject), target.GetGUID());

                // farsight dynobj or puppet may be very far away
                UpdateVisibilityOf(target);

                if (target.IsTypeMask(TypeMask.Unit) &&
                    target != GetVehicleBase())
                    target.ToUnit().AddPlayerToVision(this);

                SetSeer(target);
            }
            else
            {
                Log.outDebug(LogFilter.Maps, "Player.CreateViewpoint: Player {0} remove seer", GetName());

                if (target.GetGUID() != ActivePlayerData.FarsightObject)
                {
                    Log.outFatal(LogFilter.Player, "Player.CreateViewpoint: Player {0} cannot remove current viewpoint!", GetName());

                    return;
                }

                SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.FarsightObject), ObjectGuid.Empty);

                if (target.IsTypeMask(TypeMask.Unit) &&
                    target != GetVehicleBase())
                    target.ToUnit().RemovePlayerFromVision(this);

                //must immediately set seer back otherwise may crash
                SetSeer(this);
            }
        }

        public WorldObject GetViewpoint()
        {
            ObjectGuid guid = ActivePlayerData.FarsightObject;

            if (!guid.IsEmpty())
                return Global.ObjAccessor.GetObjectByTypeMask(this, guid, TypeMask.Seer);

            return null;
        }

        public void SetClientControl(Unit target, bool allowMove)
        {
            // a player can never client control nothing
            Cypher.Assert(target);

            // don't allow possession to be overridden
            if (target.HasUnitState(UnitState.Charmed) &&
                (GetGUID() != target.GetCharmerGUID()))
            {
                // this should never happen, otherwise _unitBeingMoved might be left dangling!
                Log.outError(LogFilter.Player, $"Player '{GetName()}' attempt to client control '{target.GetName()}', which is charmed by GUID {target.GetCharmerGUID()}");

                return;
            }

            // still affected by some aura that shouldn't allow control, only allow on last such aura to be removed
            if (target.HasUnitState(UnitState.Fleeing | UnitState.Confused))
                allowMove = false;

            ControlUpdate packet = new();
            packet.Guid = target.GetGUID();
            packet.On = allowMove;
            SendPacket(packet);

            WorldObject viewpoint = GetViewpoint();

            if (viewpoint == null)
                viewpoint = this;

            if (target != viewpoint)
            {
                if (viewpoint != this)
                    SetViewpoint(viewpoint, false);

                if (target != this)
                    SetViewpoint(target, true);
            }

            SetMovedUnit(target);
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

            Item item;

            if (useable)
                item = GetUseableItemByPos(InventorySlots.Bag0, slot);
            else
                item = GetItemByPos(InventorySlots.Bag0, slot);

            if (item == null ||
                item.GetTemplate().GetClass() != ItemClass.Weapon)
                return null;

            if ((attackType == WeaponAttackType.RangedAttack) != item.GetTemplate().IsRangedWeapon())
                return null;

            if (!useable)
                return item;

            if (item.IsBroken())
                return null;

            return item;
        }

        public static WeaponAttackType GetAttackBySlot(byte slot, InventoryType inventoryType)
        {
            return slot switch
            {
                EquipmentSlot.MainHand => inventoryType != InventoryType.Ranged && inventoryType != InventoryType.RangedRight ? WeaponAttackType.BaseAttack : WeaponAttackType.RangedAttack,
                EquipmentSlot.OffHand => WeaponAttackType.OffAttack,
                _ => WeaponAttackType.Max
            };
        }

        public void AutoUnequipOffhandIfNeed(bool force = false)
        {
            Item offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

            if (offItem == null)
                return;

            ItemTemplate offtemplate = offItem.GetTemplate();

            // unequip offhand weapon if player doesn't have dual wield anymore
            if (!CanDualWield() &&
                ((offItem.GetTemplate().GetInventoryType() == InventoryType.WeaponOffhand && !offItem.GetTemplate().HasFlag(ItemFlags3.AlwaysAllowDualWield)) || offItem.GetTemplate().GetInventoryType() == InventoryType.Weapon))
                force = true;

            // need unequip offhand for 2h-weapon without TitanGrip (in any from hands)
            if (!force &&
                (CanTitanGrip() || (offtemplate.GetInventoryType() != InventoryType.Weapon2Hand && !IsTwoHandUsed())))
                return;

            List<ItemPosCount> off_dest = new();
            InventoryResult off_msg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, off_dest, offItem, false);

            if (off_msg == InventoryResult.Ok)
            {
                RemoveItem(InventorySlots.Bag0, EquipmentSlot.OffHand, true);
                StoreItem(off_dest, offItem, true);
            }
            else
            {
                MoveItemFromInventory(InventorySlots.Bag0, EquipmentSlot.OffHand, true);
                SQLTransaction trans = new();
                offItem.DeleteFromInventoryDB(trans); // deletes Item from character's inventory
                offItem.SaveToDB(trans);              // recursive and not have transaction guard into self, Item not in inventory and can be save standalone

                string subject = Global.ObjectMgr.GetCypherString(CypherStrings.NotEquippedItem);
                new MailDraft(subject, "There were problems with equipping one or several items").AddItem(offItem).SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);

                DB.Characters.CommitTransaction(trans);
            }
        }

        public WorldLocation GetTeleportDest()
        {
            return _teleportDest;
        }

        public uint? GetTeleportDestInstanceId()
        {
            return _teleport_instanceId;
        }

        public WorldLocation GetHomebind()
        {
            return _homebind;
        }

        public WorldLocation GetRecall()
        {
            return _recall_location;
        }

        public void SetRestState(RestTypes type, PlayerRestState state)
        {
            RestInfo restInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.RestInfo, (int)type);
            SetUpdateFieldValue(restInfo.ModifyValue(restInfo.StateID), (byte)state);
        }

        public void SetRestThreshold(RestTypes type, uint threshold)
        {
            RestInfo restInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.RestInfo, (int)type);
            SetUpdateFieldValue(restInfo.ModifyValue(restInfo.Threshold), threshold);
        }

        public bool HasPlayerFlag(PlayerFlags flags)
        {
            return (PlayerData.PlayerFlags & (uint)flags) != 0;
        }

        public void SetPlayerFlag(PlayerFlags flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlags), (uint)flags);
        }

        public void RemovePlayerFlag(PlayerFlags flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlags), (uint)flags);
        }

        public void ReplaceAllPlayerFlags(PlayerFlags flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlags), (uint)flags);
        }

        public bool HasPlayerFlagEx(PlayerFlagsEx flags)
        {
            return (PlayerData.PlayerFlagsEx & (uint)flags) != 0;
        }

        public void SetPlayerFlagEx(PlayerFlagsEx flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlagsEx), (uint)flags);
        }

        public void RemovePlayerFlagEx(PlayerFlagsEx flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlagsEx), (uint)flags);
        }

        public void ReplaceAllPlayerFlagsEx(PlayerFlagsEx flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlagsEx), (uint)flags);
        }

        public void SetAverageItemLevelTotal(float newItemLevel)
        {
            SetUpdateFieldValue(ref Values.ModifyValue(PlayerData).ModifyValue(PlayerData.AvgItemLevel, 0), newItemLevel);
        }

        public void SetAverageItemLevelEquipped(float newItemLevel)
        {
            SetUpdateFieldValue(ref Values.ModifyValue(PlayerData).ModifyValue(PlayerData.AvgItemLevel, 1), newItemLevel);
        }

        public uint GetCustomizationChoice(uint chrCustomizationOptionId)
        {
            int choiceIndex = PlayerData.Customizations.FindIndexIf(choice => { return choice.ChrCustomizationOptionID == chrCustomizationOptionId; });

            if (choiceIndex >= 0)
                return PlayerData.Customizations[choiceIndex].ChrCustomizationChoiceID;

            return 0;
        }

        public void SetCustomizations(List<ChrCustomizationChoice> customizations, bool markChanged = true)
        {
            if (markChanged)
                _customizationsChanged = true;

            ClearDynamicUpdateFieldValues(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.Customizations));

            foreach (var customization in customizations)
            {
                ChrCustomizationChoice newChoice = new();
                newChoice.ChrCustomizationOptionID = customization.ChrCustomizationOptionID;
                newChoice.ChrCustomizationChoiceID = customization.ChrCustomizationChoiceID;
                AddDynamicUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.Customizations), newChoice);
            }
        }

        public override Gender GetNativeGender()
        {
            return (Gender)(byte)PlayerData.NativeSex;
        }

        public override void SetNativeGender(Gender sex)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.NativeSex), (byte)sex);
        }

        public void SetPvpTitle(byte pvpTitle)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PvpTitle), pvpTitle);
        }

        public void SetArenaFaction(byte arenaFaction)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.ArenaFaction), arenaFaction);
        }

        public void ApplyModFakeInebriation(int mod, bool apply)
        {
            ApplyModUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.FakeInebriation), mod, apply);
        }

        public void SetVirtualPlayerRealm(uint virtualRealmAddress)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.VirtualPlayerRealm), virtualRealmAddress);
        }

        public void SetCurrentBattlePetBreedQuality(byte battlePetBreedQuality)
        {
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.CurrentBattlePetBreedQuality), battlePetBreedQuality);
        }

        public void AddHeirloom(uint itemId, uint flags)
        {
            AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Heirlooms), itemId);
            AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.HeirloomFlags), flags);
        }

        public void SetHeirloom(int slot, uint itemId)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Heirlooms, slot), itemId);
        }

        public void SetHeirloomFlags(int slot, uint flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.HeirloomFlags, slot), flags);
        }

        public void AddToy(uint itemId, uint flags)
        {
            AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Toys), itemId);
            AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ToyFlags), flags);
        }

        public void AddTransmogBlock(uint blockValue)
        {
            AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Transmog), blockValue);
        }

        public void AddTransmogFlag(int slot, uint flag)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Transmog, slot), flag);
        }

        public void AddConditionalTransmog(uint itemModifiedAppearanceId)
        {
            AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ConditionalTransmog), itemModifiedAppearanceId);
        }

        public void RemoveConditionalTransmog(uint itemModifiedAppearanceId)
        {
            int index = ActivePlayerData.ConditionalTransmog.FindIndex(itemModifiedAppearanceId);

            if (index >= 0)
                RemoveDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ConditionalTransmog), index);
        }

        public void AddIllusionBlock(uint blockValue)
        {
            AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TransmogIllusions), blockValue);
        }

        public void AddIllusionFlag(int slot, uint flag)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TransmogIllusions, slot), flag);
        }

        public void AddSelfResSpell(uint spellId)
        {
            AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SelfResSpells), spellId);
        }

        public void RemoveSelfResSpell(uint spellId)
        {
            int index = ActivePlayerData.SelfResSpells.FindIndex(spellId);

            if (index >= 0)
                RemoveDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SelfResSpells), index);
        }

        public void ClearSelfResSpell()
        {
            ClearDynamicUpdateFieldValues(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SelfResSpells));
        }

        public ObjectGuid GetSummonedBattlePetGUID()
        {
            return ActivePlayerData.SummonedBattlePetGUID;
        }

        public void SetSummonedBattlePetGUID(ObjectGuid guid)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SummonedBattlePetGUID), guid);
        }

        public void SetTrackCreatureFlag(uint flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TrackCreatureMask), flags);
        }

        public void RemoveTrackCreatureFlag(uint flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TrackCreatureMask), flags);
        }

        public void SetVersatilityBonus(float value)
        {
            SetUpdateFieldStatValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.VersatilityBonus), value);
        }

        public void ApplyModOverrideSpellPowerByAPPercent(float mod, bool apply)
        {
            ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OverrideSpellPowerByAPPercent), mod, apply);
        }

        public void ApplyModOverrideAPBySpellPowerPercent(float mod, bool apply)
        {
            ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OverrideAPBySpellPowerPercent), mod, apply);
        }

        public bool HasPlayerLocalFlag(PlayerLocalFlags flags)
        {
            return (ActivePlayerData.LocalFlags & (int)flags) != 0;
        }

        public void SetPlayerLocalFlag(PlayerLocalFlags flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LocalFlags), (uint)flags);
        }

        public void RemovePlayerLocalFlag(PlayerLocalFlags flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LocalFlags), (uint)flags);
        }

        public void ReplaceAllPlayerLocalFlags(PlayerLocalFlags flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LocalFlags), (uint)flags);
        }

        public byte GetNumRespecs()
        {
            return ActivePlayerData.NumRespecs;
        }

        public void SetNumRespecs(byte numRespecs)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NumRespecs), numRespecs);
        }

        public void SetWatchedFactionIndex(uint index)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.WatchedFactionIndex), index);
        }

        public void AddAuraVision(PlayerFieldByte2Flags flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.AuraVision), (byte)flags);
        }

        public void RemoveAuraVision(PlayerFieldByte2Flags flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.AuraVision), (byte)flags);
        }

        public void SetTransportServerTime(int transportServerTime)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TransportServerTime), transportServerTime);
        }

        public bool CanTameExoticPets()
        {
            return IsGameMaster() || HasAuraType(AuraType.AllowTamePetType);
        }

        public void SendAttackSwingCancelAttack()
        {
            SendPacket(new CancelCombat());
        }

        public void SendAttackSwingNotInRange()
        {
            SendPacket(new AttackSwingError(AttackSwingErr.NotInRange));
        }

        public void SendAutoRepeatCancel(Unit target)
        {
            CancelAutoRepeat cancelAutoRepeat = new();
            cancelAutoRepeat.Guid = target.GetGUID(); // may be it's Target Guid
            SendMessageToSet(cancelAutoRepeat, true);
        }

        public override void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
        {
            if (target == this)
            {
                for (byte i = EquipmentSlot.Start; i < InventorySlots.BankBagEnd; ++i)
                {
                    if (_items[i] == null)
                        continue;

                    _items[i].BuildCreateUpdateBlockForPlayer(data, target);
                }

                for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
                {
                    if (_items[i] == null)
                        continue;

                    _items[i].BuildCreateUpdateBlockForPlayer(data, target);
                }
            }

            base.BuildCreateUpdateBlockForPlayer(data, target);
        }

        public override UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
        {
            UpdateFieldFlag flags = base.GetUpdateFieldFlagsFor(target);

            if (IsInSameRaidWith(target))
                flags |= UpdateFieldFlag.PartyMember;

            return flags;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt8((byte)flags);
            ObjectData.WriteCreate(buffer, flags, this, target);
            UnitData.WriteCreate(buffer, flags, this, target);
            PlayerData.WriteCreate(buffer, flags, this, target);

            if (target == this)
                ActivePlayerData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt32((uint)(Values.GetChangedObjectTypeMask() & ~((target != this ? 1 : 0) << (int)TypeId.ActivePlayer)));

            if (Values.HasChanged(TypeId.Object))
                ObjectData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.Unit))
                UnitData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.Player))
                PlayerData.WriteUpdate(buffer, flags, this, target);

            if (target == this &&
                Values.HasChanged(TypeId.ActivePlayer))
                ActivePlayerData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);
            valuesMask.Set((int)TypeId.Unit);
            valuesMask.Set((int)TypeId.Player);

            WorldPacket buffer = new();

            UpdateMask mask = new(191);
            UnitData.AppendAllowedFieldsMaskForFlag(mask, flags);
            UnitData.WriteUpdate(buffer, mask, true, this, target);

            UpdateMask mask2 = new(161);
            PlayerData.AppendAllowedFieldsMaskForFlag(mask2, flags);
            PlayerData.WriteUpdate(buffer, mask2, true, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteUInt32(valuesMask.GetBlock(0));
            data.WriteBytes(buffer);
        }

        public override void ClearUpdateMask(bool remove)
        {
            Values.ClearChangesMask(PlayerData);
            Values.ClearChangesMask(ActivePlayerData);
            base.ClearUpdateMask(remove);
        }

        //Helpers
        public void AddGossipItem(GossipOptionNpc optionNpc, string text, uint sender, uint action)
        {
            PlayerTalkClass.GetGossipMenu().AddMenuItem(0, -1, optionNpc, text, 0, GossipOptionFlags.None, null, 0, 0, false, 0, "", null, null, sender, action);
        }

        public void AddGossipItem(GossipOptionNpc optionNpc, string text, uint sender, uint action, string popupText, uint popupMoney, bool coded)
        {
            PlayerTalkClass.GetGossipMenu().AddMenuItem(0, -1, optionNpc, text, 0, GossipOptionFlags.None, null, 0, 0, coded, popupMoney, popupText, null, null, sender, action);
        }

        public void AddGossipItem(uint gossipMenuID, uint gossipMenuItemID, uint sender, uint action)
        {
            PlayerTalkClass.GetGossipMenu().AddMenuItem(gossipMenuID, gossipMenuItemID, sender, action);
        }

        // This fuction Sends the current menu to show to client, a - NPCTEXTID(uint32), b - npc Guid(uint64)
        public void SendGossipMenu(uint titleId, ObjectGuid objGUID)
        {
            PlayerTalkClass.SendGossipMenu(titleId, objGUID);
        }

        // Closes the Menu
        public void CloseGossipMenu()
        {
            PlayerTalkClass.SendCloseGossip();
        }

        public void InitGossipMenu(uint menuId)
        {
            PlayerTalkClass.GetGossipMenu().SetMenuId(menuId);
        }

        //Clears the Menu
        public void ClearGossipMenu()
        {
            PlayerTalkClass.ClearMenus();
        }

        private void ScheduleDelayedOperation(PlayerDelayedOperations operation)
        {
            if (operation < PlayerDelayedOperations.End)
                _delayedOperations |= operation;
        }

        private new PlayerAI GetAI()
        {
            return (PlayerAI)IAi;
        }

        private void DeleteGarrison()
        {
            if (_garrison != null)
            {
                _garrison.Delete();
                _garrison = null;
            }
        }

        //Currency - Money
        private void SetCreateCurrency(CurrencyTypes id, uint count, bool printLog = true)
        {
            var playerCurrency = _currencyStorage.LookupByKey(id);

            if (playerCurrency == null)
            {
                PlayerCurrency cur = new();
                cur.State = PlayerCurrencyState.New;
                cur.Quantity = count;
                cur.WeeklyQuantity = 0;
                cur.TrackedQuantity = 0;
                cur.Flags = 0;
                _currencyStorage[(uint)id] = cur;
            }
        }

        private uint GetCurrencyTotalCap(CurrencyTypesRecord currency)
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

        private uint GetCurrencyOnWeek(CurrencyTypes id)
        {
            var playerCurrency = _currencyStorage.LookupByKey(id);

            if (playerCurrency == null)
                return 0;

            return playerCurrency.WeeklyQuantity;
        }

        private bool IsActionButtonDataValid(byte button, ulong action, uint type)
        {
            if (button >= PlayerConst.MaxActionButtons)
            {
                Log.outError(LogFilter.Player, $"Player::IsActionButtonDataValid: Action {action} not added into Button {button} for player {GetName()} ({GetGUID()}): Button must be < {PlayerConst.MaxActionButtons}");

                return false;
            }

            if (action >= PlayerConst.MaxActionButtonActionValue)
            {
                Log.outError(LogFilter.Player, $"Player::IsActionButtonDataValid: Action {action} not added into Button {button} for player {GetName()} ({GetGUID()}): Action must be < {PlayerConst.MaxActionButtonActionValue}");

                return false;
            }

            switch ((ActionButtonType)type)
            {
                case ActionButtonType.Spell:
                    if (!Global.SpellMgr.HasSpellInfo((uint)action, Difficulty.None))
                    {
                        Log.outError(LogFilter.Player, $"Player::IsActionButtonDataValid: Spell Action {action} not added into Button {button} for player {GetName()} ({GetGUID()}): spell not exist");

                        return false;
                    }

                    break;
                case ActionButtonType.Item:
                    if (Global.ObjectMgr.GetItemTemplate((uint)action) == null)
                    {
                        Log.outError(LogFilter.Player, $"Player::IsActionButtonDataValid: Item Action {action} not added into Button {button} for player {GetName()} ({GetGUID()}): Item not exist");

                        return false;
                    }

                    break;
                case ActionButtonType.Companion:
                    {
                        if (GetSession().GetBattlePetMgr().GetPet(ObjectGuid.Create(HighGuid.BattlePet, action)) == null)
                        {
                            Log.outError(LogFilter.Player, $"Player::IsActionButtonDataValid: Companion Action {action} not added into Button {button} for player {GetName()} ({GetGUID()}): companion does not exist");

                            return false;
                        }

                        break;
                    }
                case ActionButtonType.Mount:
                    var mount = CliDB.MountStorage.LookupByKey(action);

                    if (mount == null)
                    {
                        Log.outError(LogFilter.Player, $"Player::IsActionButtonDataValid: Mount Action {action} not added into Button {button} for player {GetName()} ({GetGUID()}): Mount does not exist");

                        return false;
                    }

                    if (!HasSpell(mount.SourceSpellID))
                    {
                        Log.outError(LogFilter.Player, $"Player::IsActionButtonDataValid: Mount Action {action} not added into Button {button} for player {GetName()} ({GetGUID()}): Player does not know this Mount");

                        return false;
                    }

                    break;
                case ActionButtonType.C:
                case ActionButtonType.CMacro:
                case ActionButtonType.Macro:
                case ActionButtonType.Eqset:
                    break;
                default:
                    Log.outError(LogFilter.Player, $"Unknown Action Type {type}");

                    return false; // other cases not checked at this moment
            }

            return true;
        }

        private void SendInitialActionButtons()
        {
            SendActionButtons(0);
        }

        private void SendActionButtons(uint state)
        {
            UpdateActionButtons packet = new();

            foreach (var pair in _actionButtons)
                if (pair.Value.UState != ActionButtonUpdateState.Deleted &&
                    pair.Key < packet.ActionButtons.Length)
                    packet.ActionButtons[pair.Key] = pair.Value.PackedData;

            packet.Reason = (byte)state;
            SendPacket(packet);
        }

        // Calculate how many reputation points player gain with the quest
        private void RewardReputation(Quest quest)
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

                if (quest.RewardFactionCapIn[i] != 0 &&
                    rep > 0 &&
                    (int)GetReputationMgr().GetRank(factionEntry) >= quest.RewardFactionCapIn[i])
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
                GetReputationMgr().ModifyReputation(factionEntry, rep, false, noSpillover);
            }
        }

        //Movement
        private bool IsCanDelayTeleport()
        {
            return _bCanDelayTeleport;
        }

        private void SetCanDelayTeleport(bool setting)
        {
            _bCanDelayTeleport = setting;
        }

        private bool IsHasDelayedTeleport()
        {
            return _bHasDelayedTeleport;
        }

        private void SetDelayedTeleportFlag(bool setting)
        {
            _bHasDelayedTeleport = setting;
        }

        private void UpdateLocalChannels(uint newZone)
        {
            if (GetSession().PlayerLoading() &&
                !IsBeingTeleportedFar())
                return; // The client handles it automatically after loading, but not after teleporting

            AreaTableRecord current_zone = CliDB.AreaTableStorage.LookupByKey(newZone);

            if (current_zone == null)
                return;

            ChannelManager cMgr = ChannelManager.ForTeam(GetTeam());

            if (cMgr == null)
                return;

            foreach (var channelEntry in CliDB.ChatChannelsStorage.Values)
            {
                if (!channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Initial))
                    continue;

                Channel usedChannel = null;

                foreach (var channel in _channels)
                    if (channel.GetChannelId() == channelEntry.Id)
                    {
                        usedChannel = channel;

                        break;
                    }

                Channel removeChannel = null;
                Channel joinChannel = null;
                bool sendRemove = true;

                if (CanJoinConstantChannelInZone(channelEntry, current_zone))
                {
                    if (!channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Global))
                    {
                        if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly) &&
                            usedChannel != null)
                            continue; // Already on the channel, as city channel names are not changing

                        joinChannel = cMgr.GetSystemChannel(channelEntry.Id, current_zone);

                        if (usedChannel != null)
                        {
                            if (joinChannel != usedChannel)
                            {
                                removeChannel = usedChannel;
                                sendRemove = false; // Do not send leave channel, it already replaced at client
                            }
                            else
                            {
                                joinChannel = null;
                            }
                        }
                    }
                    else
                    {
                        joinChannel = cMgr.GetSystemChannel(channelEntry.Id);
                    }
                }
                else
                {
                    removeChannel = usedChannel;
                }

                joinChannel?.JoinChannel(this); // Changed Channel: ... or Joined Channel: ...

                if (removeChannel != null)
                {
                    removeChannel.LeaveChannel(this, sendRemove, true); // Leave old channel

                    LeftChannel(removeChannel);                                                   // Remove from player's channel list
                    cMgr.LeftChannel(removeChannel.GetChannelId(), removeChannel.GetZoneEntry()); // Delete if empty
                }
            }
        }

        private void UpdateHomebindTime(uint time)
        {
            // GMs never get _homebind timer online
            if (InstanceValid || IsGameMaster())
            {
                if (_homebindTimer != 0) // instance valid, but timer not reset
                    SendRaidGroupOnlyMessage(RaidGroupReason.None, 0);

                // instance is valid, reset _homebind timer
                _homebindTimer = 0;
            }
            else if (_homebindTimer > 0)
            {
                if (time >= _homebindTimer)
                    // teleport to nearest graveyard
                    RepopAtGraveyard();
                else
                    _homebindTimer -= time;
            }
            else
            {
                // instance is invalid, start _homebind timer
                _homebindTimer = 60000;
                // send message to player
                SendRaidGroupOnlyMessage(RaidGroupReason.RequirementsUnmatch, (int)_homebindTimer);
                Log.outDebug(LogFilter.Maps, "PLAYER: Player '{0}' (GUID: {1}) will be teleported to _homebind in 60 seconds", GetName(), GetGUID().ToString());
            }
        }

        private void SendInitWorldStates(uint zoneId, uint areaId)
        {
            // _data depends on zoneid/mapid...
            uint mapid = GetMapId();

            InitWorldStates packet = new();
            packet.MapID = mapid;
            packet.AreaID = zoneId;
            packet.SubareaID = areaId;

            Global.WorldStateMgr.FillInitialWorldStates(packet, GetMap(), areaId);

            SendPacket(packet);
        }

        private uint GetChampioningFaction()
        {
            return _championingFaction;
        }

        private void ResurrectUsingRequestDataImpl()
        {
            // save health and mana before resurrecting, _resurrectionData can be erased
            uint resurrectHealth = _resurrectionData.Health;
            uint resurrectMana = _resurrectionData.Mana;
            uint resurrectAura = _resurrectionData.Aura;
            ObjectGuid resurrectGUID = _resurrectionData.GUID;

            ResurrectPlayer(0.0f, false);

            SetHealth(resurrectHealth);
            SetPower(PowerType.Mana, (int)resurrectMana);

            SetPower(PowerType.Rage, 0);
            SetFullPower(PowerType.Energy);
            SetFullPower(PowerType.Focus);
            SetPower(PowerType.LunarPower, 0);

            if (resurrectAura != 0)
                CastSpell(this, resurrectAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(resurrectGUID));

            SpawnCorpseBones();
        }

        private void RegenerateAll()
        {
            _regenTimerCount += RegenTimer;
            _foodEmoteTimerCount += RegenTimer;

            for (PowerType power = PowerType.Mana; power < PowerType.Max; power++) // = power + 1)
                if (power != PowerType.Runes)
                    Regenerate(power);

            // Runes act as cooldowns, and they don't need to send any _data
            if (GetClass() == Class.Deathknight)
            {
                uint regeneratedRunes = 0;
                int regenIndex = 0;

                while (regeneratedRunes < PlayerConst.MaxRechargingRunes && _runes.CooldownOrder.Count > regenIndex)
                {
                    byte runeToRegen = _runes.CooldownOrder[regenIndex];
                    uint runeCooldown = GetRuneCooldown(runeToRegen);

                    if (runeCooldown > RegenTimer)
                    {
                        SetRuneCooldown(runeToRegen, runeCooldown - RegenTimer);
                        ++regenIndex;
                    }
                    else
                    {
                        SetRuneCooldown(runeToRegen, 0);
                    }

                    ++regeneratedRunes;
                }
            }

            if (_regenTimerCount >= 2000)
            {
                // Not in combat or they have regeneration
                if (!IsInCombat() ||
                    IsPolymorphed() ||
                    _baseHealthRegen != 0 ||
                    HasAuraType(AuraType.ModRegenDuringCombat) ||
                    HasAuraType(AuraType.ModHealthRegenInCombat))
                    RegenerateHealth();

                _regenTimerCount -= 2000;
            }

            RegenTimer = 0;

            // Handles the emotes for drinking and eating.
            // According to sniffs there is a background timer going on that repeats independed from the Time window where the aura applies.
            // That's why we dont need to reset the timer on apply. In sniffs I have seen that the first call for the spell visual is totally random, then after
            // 5 seconds over and over again which confirms my theory that we have a independed timer.
            if (_foodEmoteTimerCount >= 5000)
            {
                List<AuraEffect> auraList = GetAuraEffectsByType(AuraType.ModRegen);
                auraList.AddRange(GetAuraEffectsByType(AuraType.ModPowerRegen));

                foreach (var auraEffect in auraList)
                    // Food Emote comes above drinking Emote if we have to decide (mage regen food for example)
                    if (auraEffect.GetBase().HasEffectType(AuraType.ModRegen) &&
                        auraEffect.GetSpellInfo().HasAuraInterruptFlag(SpellAuraInterruptFlags.Standing))
                    {
                        SendPlaySpellVisualKit(SpellConst.VisualKitFood, 0, 0);

                        break;
                    }
                    else if (auraEffect.GetBase().HasEffectType(AuraType.ModPowerRegen) &&
                             auraEffect.GetSpellInfo().HasAuraInterruptFlag(SpellAuraInterruptFlags.Standing))
                    {
                        SendPlaySpellVisualKit(SpellConst.VisualKitDrink, 0, 0);

                        break;
                    }

                _foodEmoteTimerCount -= 5000;
            }
        }

        private void Regenerate(PowerType power)
        {
            // Skip regeneration for power Type we cannot have
            uint powerIndex = GetPowerIndex(power);

            if (powerIndex == (int)PowerType.Max ||
                powerIndex >= (int)PowerType.MaxPerClass)
                return;

            // @todo possible use of miscvalueb instead of amount
            if (HasAuraTypeWithValue(AuraType.PreventRegeneratePower, (int)power))
                return;

            int curValue = GetPower(power);

            // TODO: updating haste should update UNIT_FIELD_POWER_REGEN_FLAT_MODIFIER for certain power types
            PowerTypeRecord powerType = Global.DB2Mgr.GetPowerTypeEntry(power);

            if (powerType == null)
                return;

            float addvalue;

            if (!IsInCombat())
            {
                if (powerType.RegenInterruptTimeMS != 0 &&
                    Time.GetMSTimeDiffToNow(_combatExitTime) < powerType.RegenInterruptTimeMS)
                    return;

                addvalue = (powerType.RegenPeace + UnitData.PowerRegenFlatModifier[(int)powerIndex]) * 0.001f * RegenTimer;
            }
            else
            {
                addvalue = (powerType.RegenCombat + UnitData.PowerRegenInterruptedFlatModifier[(int)powerIndex]) * 0.001f * RegenTimer;
            }

            WorldCfg[] RatesForPower =
            {
                WorldCfg.RatePowerMana, WorldCfg.RatePowerRageLoss, WorldCfg.RatePowerFocus, WorldCfg.RatePowerEnergy, WorldCfg.RatePowerComboPointsLoss, 0, // runes
				WorldCfg.RatePowerRunicPowerLoss, WorldCfg.RatePowerSoulShards, WorldCfg.RatePowerLunarPower, WorldCfg.RatePowerHolyPower, 0,                // alternate
				WorldCfg.RatePowerMaelstrom, WorldCfg.RatePowerChi, WorldCfg.RatePowerInsanity, 0,                                                           // burning embers, unused
				0,                                                                                                                                           // demonic fury, unused
				WorldCfg.RatePowerArcaneCharges, WorldCfg.RatePowerFury, WorldCfg.RatePowerPain
            };

            if (RatesForPower[(int)power] != 0)
                addvalue *= WorldConfig.GetFloatValue(RatesForPower[(int)power]);

            // Mana regen calculated in Player.UpdateManaRegen()
            if (power != PowerType.Mana)
            {
                addvalue *= GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerRegenPercent, (int)power);
                addvalue += GetTotalAuraModifierByMiscValue(AuraType.ModPowerRegen, (int)power) * ((power != PowerType.Energy) ? _regenTimerCount : RegenTimer) / (5 * Time.InMilliseconds);
            }

            int minPower = powerType.MinPower;
            int maxPower = GetMaxPower(power);

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
                {
                    return;
                }
            }

            addvalue += _powerFraction[powerIndex];
            int integerValue = (int)Math.Abs(addvalue);

            bool forcesSetPower = false;

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
            {
                return;
            }

            if (addvalue < 0.0f)
            {
                if (curValue > minPower + integerValue)
                {
                    curValue -= integerValue;
                    _powerFraction[powerIndex] = addvalue + integerValue;
                }
                else
                {
                    curValue = minPower;
                    _powerFraction[powerIndex] = 0;
                    forcesSetPower = true;
                }
            }
            else
            {
                if (curValue + integerValue <= maxPower)
                {
                    curValue += integerValue;
                    _powerFraction[powerIndex] = addvalue - integerValue;
                }
                else
                {
                    curValue = maxPower;
                    _powerFraction[powerIndex] = 0;
                    forcesSetPower = true;
                }
            }

            if (GetCommandStatus(PlayerCommandStates.Power))
                curValue = maxPower;

            if (_regenTimerCount >= 2000 || forcesSetPower)
                SetPower(power, curValue);
            else
                // throttle packet sending
                DoWithSuppressingObjectUpdates(() =>
                                               {
                                                   SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.Power, (int)powerIndex), curValue);
                                                   UnitData.ClearChanged(UnitData.Power, (int)powerIndex);
                                               });
        }

        private void RegenerateHealth()
        {
            uint curValue = (uint)GetHealth();
            uint maxValue = (uint)GetMaxHealth();

            if (curValue >= maxValue)
                return;

            float HealthIncreaseRate = WorldConfig.GetFloatValue(WorldCfg.RateHealth);
            float addValue = 0.0f;

            // polymorphed case
            if (IsPolymorphed())
            {
                addValue = (float)GetMaxHealth() / 3;
            }
            // normal regen case (maybe partly in combat case)
            else if (!IsInCombat() ||
                     HasAuraType(AuraType.ModRegenDuringCombat))
            {
                addValue = HealthIncreaseRate;

                if (!IsInCombat())
                {
                    if (GetLevel() < 15)
                        addValue = (0.20f * (GetMaxHealth()) / GetLevel() * HealthIncreaseRate);
                    else
                        addValue = 0.015f * (GetMaxHealth()) * HealthIncreaseRate;

                    addValue *= GetTotalAuraMultiplier(AuraType.ModHealthRegenPercent);
                    addValue += GetTotalAuraModifier(AuraType.ModRegen) * 2 * Time.InMilliseconds / (5 * Time.InMilliseconds);
                }
                else if (HasAuraType(AuraType.ModRegenDuringCombat))
                {
                    addValue = MathFunctions.CalculatePct(addValue, GetTotalAuraModifier(AuraType.ModRegenDuringCombat));
                }

                if (!IsStandState())
                    addValue *= 1.5f;
            }

            // always regeneration bonus (including combat)
            addValue += GetTotalAuraModifier(AuraType.ModHealthRegenInCombat);
            addValue += _baseHealthRegen / 2.5f;

            if (addValue < 0)
                addValue = 0;

            ModifyHealth((int)addValue);
        }

        private void LeaveLFGChannel()
        {
            foreach (var i in _channels)
                if (i.IsLFG())
                {
                    i.LeaveChannel(this);

                    break;
                }
        }

        private bool IsImmuneToEnvironmentalDamage()
        {
            // check for GM and death State included in isAttackableByAOE
            return (!IsTargetableForAttack(false));
        }

        private bool IsTotalImmune()
        {
            var immune = GetAuraEffectsByType(AuraType.SchoolImmunity);

            int immuneMask = 0;

            foreach (var eff in immune)
            {
                immuneMask |= eff.GetMiscValue();

                if (Convert.ToBoolean(immuneMask & (int)SpellSchoolMask.All)) // total immunity
                    return true;
            }

            return false;
        }

        private void HandleDrowning(uint time_diff)
        {
            if (_mirrorTimerFlags == 0)
                return;

            int breathTimer = (int)MirrorTimerType.Breath;
            int fatigueTimer = (int)MirrorTimerType.Fatigue;
            int fireTimer = (int)MirrorTimerType.Fire;

            // In water
            if (_mirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InWater))
            {
                // Breath timer not activated - activate it
                if (_mirrorTimer[breathTimer] == -1)
                {
                    _mirrorTimer[breathTimer] = GetMaxTimer(MirrorTimerType.Breath);
                    SendMirrorTimer(MirrorTimerType.Breath, _mirrorTimer[breathTimer], _mirrorTimer[breathTimer], -1);
                }
                else // If activated - do tick
                {
                    _mirrorTimer[breathTimer] -= (int)time_diff;

                    // Timer limit - need deal Damage
                    if (_mirrorTimer[breathTimer] < 0)
                    {
                        _mirrorTimer[breathTimer] += 1 * Time.InMilliseconds;
                        // Calculate and deal Damage
                        // @todo Check this formula
                        uint damage = (uint)(GetMaxHealth() / 5 + RandomHelper.URand(0, GetLevel() - 1));
                        EnvironmentalDamage(EnviromentalDamage.Drowning, damage);
                    }
                    else if (!_mirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InWater)) // Update Time in client if need
                    {
                        SendMirrorTimer(MirrorTimerType.Breath, GetMaxTimer(MirrorTimerType.Breath), _mirrorTimer[breathTimer], -1);
                    }
                }
            }
            else if (_mirrorTimer[breathTimer] != -1) // Regen timer
            {
                int UnderWaterTime = GetMaxTimer(MirrorTimerType.Breath);
                // Need breath regen
                _mirrorTimer[breathTimer] += (int)(10 * time_diff);

                if (_mirrorTimer[breathTimer] >= UnderWaterTime ||
                    !IsAlive())
                    StopMirrorTimer(MirrorTimerType.Breath);
                else if (_mirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InWater))
                    SendMirrorTimer(MirrorTimerType.Breath, UnderWaterTime, _mirrorTimer[breathTimer], 10);
            }

            // In dark water
            if (_mirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InDarkWater))
            {
                // Fatigue timer not activated - activate it
                if (_mirrorTimer[fatigueTimer] == -1)
                {
                    _mirrorTimer[fatigueTimer] = GetMaxTimer(MirrorTimerType.Fatigue);
                    SendMirrorTimer(MirrorTimerType.Fatigue, _mirrorTimer[fatigueTimer], _mirrorTimer[fatigueTimer], -1);
                }
                else
                {
                    _mirrorTimer[fatigueTimer] -= (int)time_diff;

                    // Timer limit - need deal Damage or teleport ghost to graveyard
                    if (_mirrorTimer[fatigueTimer] < 0)
                    {
                        _mirrorTimer[fatigueTimer] += 1 * Time.InMilliseconds;

                        if (IsAlive()) // Calculate and deal Damage
                        {
                            uint damage = (uint)(GetMaxHealth() / 5 + RandomHelper.URand(0, GetLevel() - 1));
                            EnvironmentalDamage(EnviromentalDamage.Exhausted, damage);
                        }
                        else if (HasPlayerFlag(PlayerFlags.Ghost)) // Teleport ghost to graveyard
                        {
                            RepopAtGraveyard();
                        }
                    }
                    else if (!_mirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InDarkWater))
                    {
                        SendMirrorTimer(MirrorTimerType.Fatigue, GetMaxTimer(MirrorTimerType.Fatigue), _mirrorTimer[fatigueTimer], -1);
                    }
                }
            }
            else if (_mirrorTimer[fatigueTimer] != -1) // Regen timer
            {
                int DarkWaterTime = GetMaxTimer(MirrorTimerType.Fatigue);
                _mirrorTimer[fatigueTimer] += (int)(10 * time_diff);

                if (_mirrorTimer[fatigueTimer] >= DarkWaterTime ||
                    !IsAlive())
                    StopMirrorTimer(MirrorTimerType.Fatigue);
                else if (_mirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InDarkWater))
                    SendMirrorTimer(MirrorTimerType.Fatigue, DarkWaterTime, _mirrorTimer[fatigueTimer], 10);
            }

            if (_mirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InLava) &&
                !(_lastLiquid != null && _lastLiquid.SpellID != 0))
            {
                // Breath timer not activated - activate it
                if (_mirrorTimer[fireTimer] == -1)
                {
                    _mirrorTimer[fireTimer] = GetMaxTimer(MirrorTimerType.Fire);
                }
                else
                {
                    _mirrorTimer[fireTimer] -= (int)time_diff;

                    if (_mirrorTimer[fireTimer] < 0)
                    {
                        _mirrorTimer[fireTimer] += 1 * Time.InMilliseconds;
                        // Calculate and deal Damage
                        // @todo Check this formula
                        uint damage = RandomHelper.URand(600, 700);

                        if (_mirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InLava))
                            EnvironmentalDamage(EnviromentalDamage.Lava, damage);
                        // need to skip Slime Damage in Undercity,
                        // maybe someone can find better way to handle environmental Damage
                        //else if (_zoneUpdateId != 1497)
                        //    EnvironmentalDamage(DAMAGE_SLIME, Damage);
                    }
                }
            }
            else
            {
                _mirrorTimer[fireTimer] = -1;
            }

            // Recheck timers flag
            _mirrorTimerFlags &= ~PlayerUnderwaterState.ExistTimers;

            for (byte i = 0; i < (int)MirrorTimerType.Max; ++i)
                if (_mirrorTimer[i] != -1)
                {
                    _mirrorTimerFlags |= PlayerUnderwaterState.ExistTimers;

                    break;
                }

            _mirrorTimerFlagsLast = _mirrorTimerFlags;
        }

        private void HandleSobering()
        {
            _drunkTimer = 0;

            byte currentDrunkValue = GetDrunkValue();
            byte drunk = (byte)(currentDrunkValue != 0 ? --currentDrunkValue : 0);
            SetDrunkValue(drunk);
        }

        private void SendMirrorTimer(MirrorTimerType Type, int MaxValue, int CurrentValue, int Regen)
        {
            if (MaxValue == -1)
            {
                if (CurrentValue != -1)
                    StopMirrorTimer(Type);

                return;
            }

            SendPacket(new StartMirrorTimer(Type, CurrentValue, MaxValue, Regen, 0, false));
        }

        private void StopMirrorTimer(MirrorTimerType Type)
        {
            _mirrorTimer[(int)Type] = -1;
            SendPacket(new StopMirrorTimer(Type));
        }

        private int GetMaxTimer(MirrorTimerType timer)
        {
            switch (timer)
            {
                case MirrorTimerType.Fatigue:
                    return Time.Minute * Time.InMilliseconds;
                case MirrorTimerType.Breath:
                    {
                        if (!IsAlive() ||
                            HasAuraType(AuraType.WaterBreathing) ||
                            GetSession().GetSecurity() >= (AccountTypes)WorldConfig.GetIntValue(WorldCfg.DisableBreathing))
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

        private Corpse CreateCorpse()
        {
            // prevent existence 2 corpse for player
            SpawnCorpseBones();

            Corpse corpse = new(Convert.ToBoolean(_extraFlags & PlayerExtraFlags.PVPDeath) ? CorpseType.ResurrectablePVP : CorpseType.ResurrectablePVE);
            SetPvPDeath(false);

            if (!corpse.Create(GetMap().GenerateLowGuid(HighGuid.Corpse), this))
                return null;

            _corpseLocation = new WorldLocation(this);

            CorpseFlags flags = 0;

            if (HasPvpFlag(UnitPVPStateFlags.PvP))
                flags |= CorpseFlags.PvP;

            if (InBattleground() &&
                !InArena())
                flags |= CorpseFlags.Skinnable; // to be able to remove insignia

            if (HasPvpFlag(UnitPVPStateFlags.FFAPvp))
                flags |= CorpseFlags.FFAPvP;

            corpse.SetRace((byte)GetRace());
            corpse.SetSex((byte)GetNativeGender());
            corpse.SetClass((byte)GetClass());
            corpse.SetCustomizations(PlayerData.Customizations);
            corpse.ReplaceAllFlags(flags);
            corpse.SetDisplayId(GetNativeDisplayId());
            corpse.SetFactionTemplate(CliDB.ChrRacesStorage.LookupByKey(GetRace()).FactionID);

            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
                if (_items[i] != null)
                {
                    uint itemDisplayId = _items[i].GetDisplayId(this);
                    uint itemInventoryType;
                    ItemRecord itemEntry = CliDB.ItemStorage.LookupByKey(_items[i].GetVisibleEntry(this));

                    if (itemEntry != null)
                        itemInventoryType = (uint)itemEntry.inventoryType;
                    else
                        itemInventoryType = (uint)_items[i].GetTemplate().GetInventoryType();

                    corpse.SetItem(i, itemDisplayId | (itemInventoryType << 24));
                }

            // register for player, but not show
            GetMap().AddCorpse(corpse);

            corpse.UpdatePositionData();
            corpse.SetZoneScript();

            // we do not need to save corpses for instances
            if (!GetMap().Instanceable())
                corpse.SaveToDB();

            return corpse;
        }

        private void UpdateCorpseReclaimDelay()
        {
            bool pvp = _extraFlags.HasAnyFlag(PlayerExtraFlags.PVPDeath);

            if ((pvp && !WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPvp)) ||
                (!pvp && !WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPve)))
                return;

            long now = GameTime.GetGameTime();

            if (now < _deathExpireTime)
            {
                // full and partly periods 1..3
                ulong count = (ulong)(_deathExpireTime - now) / PlayerConst.DeathExpireStep + 1;

                if (count < PlayerConst.MaxDeathCount)
                    _deathExpireTime = now + (long)(count + 1) * PlayerConst.DeathExpireStep;
                else
                    _deathExpireTime = now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep;
            }
            else
            {
                _deathExpireTime = now + PlayerConst.DeathExpireStep;
            }
        }

        private int CalculateCorpseReclaimDelay(bool load = false)
        {
            Corpse corpse = GetCorpse();

            if (load && !corpse)
                return -1;

            bool pvp = corpse ? corpse.GetCorpseType() == CorpseType.ResurrectablePVP : (_extraFlags & PlayerExtraFlags.PVPDeath) != 0;

            uint delay;

            if (load)
            {
                if (corpse.GetGhostTime() > _deathExpireTime)
                    return -1;

                ulong count = 0;

                if ((pvp && WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPvp)) ||
                    (!pvp && WorldConfig.GetBoolValue(WorldCfg.DeathCorpseReclaimDelayPve)))
                {
                    count = (ulong)(_deathExpireTime - corpse.GetGhostTime()) / PlayerConst.DeathExpireStep;

                    if (count >= PlayerConst.MaxDeathCount)
                        count = PlayerConst.MaxDeathCount - 1;
                }

                long expected_time = corpse.GetGhostTime() + PlayerConst.copseReclaimDelay[count];
                long now = GameTime.GetGameTime();

                if (now >= expected_time)
                    return -1;

                delay = (uint)(expected_time - now);
            }
            else
            {
                delay = GetCorpseReclaimDelay(pvp);
            }

            return (int)(delay * Time.InMilliseconds);
        }

        private void SendCorpseReclaimDelay(int delay)
        {
            CorpseReclaimDelay packet = new();
            packet.Remaining = (uint)delay;
            SendPacket(packet);
        }

        private bool IsInFriendlyArea()
        {
            var areaEntry = CliDB.AreaTableStorage.LookupByKey(GetAreaId());

            if (areaEntry != null)
                return IsFriendlyArea(areaEntry);

            return false;
        }

        private bool IsFriendlyArea(AreaTableRecord areaEntry)
        {
            Cypher.Assert(areaEntry != null);

            var factionTemplate = GetFactionTemplateEntry();

            if (factionTemplate == null)
                return false;

            if ((factionTemplate.FriendGroup & areaEntry.FactionGroupMask) == 0)
                return false;

            return true;
        }

        private void SetWarModeLocal(bool enabled)
        {
            if (enabled)
                SetPlayerLocalFlag(PlayerLocalFlags.WarMode);
            else
                RemovePlayerLocalFlag(PlayerLocalFlags.WarMode);
        }

        private void UpdateWarModeAuras()
        {
            uint auraInside = 282559;
            uint auraOutside = PlayerConst.WarmodeEnlistedSpellOutside;

            if (IsWarModeDesired())
            {
                if (CanEnableWarModeInArea())
                {
                    RemovePlayerFlag(PlayerFlags.WarModeActive);
                    CastSpell(this, auraInside, true);
                    RemoveAurasDueToSpell(auraOutside);
                }
                else
                {
                    SetPlayerFlag(PlayerFlags.WarModeActive);
                    CastSpell(this, auraOutside, true);
                    RemoveAurasDueToSpell(auraInside);
                }

                SetWarModeLocal(true);
                SetPvpFlag(UnitPVPStateFlags.PvP);
            }
            else
            {
                SetWarModeLocal(false);
                RemoveAurasDueToSpell(auraOutside);
                RemoveAurasDueToSpell(auraInside);
                RemovePlayerFlag(PlayerFlags.WarModeActive);
                RemovePvpFlag(UnitPVPStateFlags.PvP);
            }
        }

        private bool IsWarModeDesired()
        {
            return HasPlayerFlag(PlayerFlags.WarModeDesired);
        }

        private bool IsWarModeActive()
        {
            return HasPlayerFlag(PlayerFlags.WarModeActive);
        }

        private void SetWeaponChangeTimer(uint time)
        {
            _weaponChangeTimer = time;
        }

        private void SetSaveTimer(uint timer)
        {
            _nextSave = timer;
        }

        private void SendAurasForTarget(Unit target)
        {
            if (target == null ||
                target.GetVisibleAuras().Empty()) // speedup things
                return;

            var visibleAuras = target.GetVisibleAuras();

            AuraUpdate update = new();
            update.UpdateAll = true;
            update.UnitGUID = target.GetGUID();

            foreach (var auraApp in visibleAuras)
            {
                AuraInfo auraInfo = new();
                auraApp.BuildUpdatePacket(ref auraInfo, false);
                update.Auras.Add(auraInfo);
            }

            SendPacket(update);
        }

        private void UpdateBaseModGroup(BaseModGroup modGroup)
        {
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

        private float GetBaseModValue(BaseModGroup modGroup, BaseModType modType)
        {
            if (modGroup >= BaseModGroup.End ||
                modType >= BaseModType.End)
            {
                Log.outError(LogFilter.Spells, $"Player.GetBaseModValue: Invalid BaseModGroup/BaseModType ({modGroup}/{modType}) for player '{GetName()}' ({GetGUID()})");

                return 0.0f;
            }

            return (modType == BaseModType.FlatMod ? _auraBaseFlatMod[(int)modGroup] : _auraBasePctMod[(int)modGroup]);
        }

        private float GetTotalBaseModValue(BaseModGroup modGroup)
        {
            if (modGroup >= BaseModGroup.End)
            {
                Log.outError(LogFilter.Spells, $"Player.GetTotalBaseModValue: Invalid BaseModGroup ({modGroup}) for player '{GetName()}' ({GetGUID()})");

                return 0.0f;
            }

            return _auraBaseFlatMod[(int)modGroup] * _auraBasePctMod[(int)modGroup];
        }

        private bool IsAtRecruitAFriendDistance(WorldObject pOther)
        {
            if (!pOther ||
                !IsInMap(pOther))
                return false;

            WorldObject player = GetCorpse();

            if (!player ||
                IsAlive())
                player = this;

            return pOther.GetDistance(player) <= WorldConfig.GetFloatValue(WorldCfg.MaxRecruitAFriendDistance);
        }


        private void SetActiveCombatTraitConfigID(int traitConfigId)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ActiveCombatTraitConfigID), (uint)traitConfigId);
        }

        private void InitPrimaryProfessions()
        {
            SetFreePrimaryProfessions(WorldConfig.GetUIntValue(WorldCfg.MaxPrimaryTradeSkill));
        }

        private void SetFreePrimaryProfessions(ushort profs)
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CharacterPoints), profs);
        }

        private void SendAttackSwingCantAttack()
        {
            SendPacket(new AttackSwingError(AttackSwingErr.CantAttack));
        }

        private void SendAttackSwingDeadTarget()
        {
            SendPacket(new AttackSwingError(AttackSwingErr.DeadTarget));
        }

        private void SendAttackSwingBadFacingAttack()
        {
            SendPacket(new AttackSwingError(AttackSwingErr.BadFacing));
        }

        private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedUnitMask, UpdateMask requestedPlayerMask, UpdateMask requestedActivePlayerMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);

            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            UnitData.FilterDisallowedFieldsMaskForFlag(requestedUnitMask, flags);

            if (requestedUnitMask.IsAnySet())
                valuesMask.Set((int)TypeId.Unit);

            PlayerData.FilterDisallowedFieldsMaskForFlag(requestedPlayerMask, flags);

            if (requestedPlayerMask.IsAnySet())
                valuesMask.Set((int)TypeId.Player);

            if (target == this &&
                requestedActivePlayerMask.IsAnySet())
                valuesMask.Set((int)TypeId.ActivePlayer);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Unit])
                UnitData.WriteUpdate(buffer, requestedUnitMask, true, this, target);

            if (valuesMask[(int)TypeId.Player])
                PlayerData.WriteUpdate(buffer, requestedPlayerMask, true, this, target);

            if (valuesMask[(int)TypeId.ActivePlayer])
                ActivePlayerData.WriteUpdate(buffer, requestedActivePlayerMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        #region Sends / Updates

        private void BeforeVisibilityDestroy(WorldObject obj, Player p)
        {
            if (!obj.IsTypeId(TypeId.Unit))
                return;

            if (p.GetPetGUID() == obj.GetGUID() &&
                obj.ToCreature().IsPet())
                ((Pet)obj).Remove(PetSaveMode.NotInSlot, true);
        }

        public void UpdateVisibilityOf(ICollection<WorldObject> targets)
        {
            if (targets.Empty())
                return;

            UpdateData udata = new(GetMapId());
            List<Unit> newVisibleUnits = new();

            foreach (WorldObject target in targets)
            {
                if (target == this)
                    continue;

                switch (target.GetTypeId())
                {
                    case TypeId.Unit:
                        UpdateVisibilityOf(target.ToCreature(), udata, newVisibleUnits);

                        break;
                    case TypeId.Player:
                        UpdateVisibilityOf(target.ToPlayer(), udata, newVisibleUnits);

                        break;
                    case TypeId.GameObject:
                        UpdateVisibilityOf(target.ToGameObject(), udata, newVisibleUnits);

                        break;
                    case TypeId.DynamicObject:
                        UpdateVisibilityOf(target.ToDynamicObject(), udata, newVisibleUnits);

                        break;
                    case TypeId.Corpse:
                        UpdateVisibilityOf(target.ToCorpse(), udata, newVisibleUnits);

                        break;
                    case TypeId.AreaTrigger:
                        UpdateVisibilityOf(target.ToAreaTrigger(), udata, newVisibleUnits);

                        break;
                    case TypeId.SceneObject:
                        UpdateVisibilityOf(target.ToSceneObject(), udata, newVisibleUnits);

                        break;
                    case TypeId.Conversation:
                        UpdateVisibilityOf(target.ToConversation(), udata, newVisibleUnits);

                        break;
                    default:
                        break;
                }
            }

            if (!udata.HasData())
                return;

            udata.BuildPacket(out UpdateObject packet);
            SendPacket(packet);

            foreach (var visibleUnit in newVisibleUnits)
                SendInitialVisiblePackets(visibleUnit);
        }

        public void UpdateVisibilityOf(WorldObject target)
        {
            if (HaveAtClient(target))
            {
                if (!CanSeeOrDetect(target, false, true))
                {
                    if (target.IsTypeId(TypeId.Unit))
                        BeforeVisibilityDestroy(target.ToCreature(), this);

                    if (!target.IsDestroyedObject())
                        target.SendOutOfRangeForPlayer(this);
                    else
                        target.DestroyForPlayer(this);

                    ClientGUIDs.Remove(target.GetGUID());
                }
            }
            else
            {
                if (CanSeeOrDetect(target, false, true))
                {
                    target.SendUpdateToPlayer(this);
                    ClientGUIDs.Add(target.GetGUID());

                    // Target aura duration for caster show only if Target exist at caster client
                    // send _data at Target visibility change (adding to client)
                    if (target.IsTypeMask(TypeMask.Unit))
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

                    if (!target.IsDestroyedObject())
                        target.BuildOutOfRangeUpdateBlock(data);
                    else
                        target.BuildDestroyUpdateBlock(data);

                    ClientGUIDs.Remove(target.GetGUID());
                }
            }
            else
            {
                if (CanSeeOrDetect(target, false, true))
                {
                    target.BuildCreateUpdateBlockForPlayer(data, this);
                    UpdateVisibilityOf_helper(ClientGUIDs, target, visibleNow);
                }
            }
        }

        private void UpdateVisibilityOf_helper<T>(List<ObjectGuid> s64, T target, List<Unit> v) where T : WorldObject
        {
            s64.Add(target.GetGUID());

            switch (target.GetTypeId())
            {
                case TypeId.Unit:
                    v.Add(target.ToCreature());

                    break;
                case TypeId.Player:
                    v.Add(target.ToPlayer());

                    break;
            }
        }

        public void SendInitialVisiblePackets(Unit target)
        {
            SendAurasForTarget(target);

            if (target.IsAlive())
                if (target.HasUnitState(UnitState.MeleeAttacking) &&
                    target.GetVictim() != null)
                    target.SendMeleeAttackStart(target.GetVictim());
        }

        public override void UpdateObjectVisibility(bool forced = true)
        {
            // Prevent updating visibility if player is not in world (example: LoadFromDB sets drunkstate which updates invisibility while player is not in map)
            if (!IsInWorld)
                return;

            if (!forced)
            {
                AddToNotify(NotifyFlags.VisibilityChanged);
            }
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
            Cell.VisitAllObjects(SeerView, notifier, GetSightRange());
            notifier.SendToSelf(); // send gathered _data
        }

        public void SetSeer(WorldObject target)
        {
            SeerView = target;
        }

        public override void SendMessageToSetInRange(ServerPacket data, float dist, bool self)
        {
            if (self)
                SendPacket(data);

            PacketSenderRef sender = new(data);
            var notifier = new MessageDistDeliverer<PacketSenderRef>(this, sender, dist);
            Cell.VisitWorldObjects(this, notifier, dist);
        }

        private void SendMessageToSetInRange(ServerPacket data, float dist, bool self, bool own_team_only, bool required3dDist = false)
        {
            if (self)
                SendPacket(data);

            PacketSenderRef sender = new(data);
            var notifier = new MessageDistDeliverer<PacketSenderRef>(this, sender, dist, own_team_only, null, required3dDist);
            Cell.VisitWorldObjects(this, notifier, dist);
        }

        public override void SendMessageToSet(ServerPacket data, Player skipped_rcvr)
        {
            if (skipped_rcvr != this)
                SendPacket(data);

            // we use World.GetMaxVisibleDistance() because i cannot see why not use a distance
            // update: replaced by GetMap().GetVisibilityDistance()
            PacketSenderRef sender = new(data);
            var notifier = new MessageDistDeliverer<PacketSenderRef>(this, sender, GetVisibilityRange(), false, skipped_rcvr);
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

            if (GetTrader() &&
                !IsWithinDistInMap(GetTrader(), SharedConst.InteractionDistance))
                GetSession().SendCancelTrade();

            CheckAreaExploreAndOutdoor();

            return true;
        }

        private void SendNewCurrency(uint id)
        {
            var Curr = _currencyStorage.LookupByKey(id);

            if (Curr == null)
                return;

            CurrencyTypesRecord entry = CliDB.CurrencyTypesStorage.LookupByKey(id);

            if (entry == null) // should never happen
                return;

            SetupCurrency        packet = new();
            SetupCurrency.Record record = new();
            record.Type = entry.Id;
            record.Quantity = Curr.Quantity;
            record.WeeklyQuantity = Curr.WeeklyQuantity;
            record.MaxWeeklyQuantity = GetCurrencyWeekCap(entry);
            record.TrackedQuantity = Curr.TrackedQuantity;
            record.Flags = Curr.Flags;

            packet.Data.Add(record);

            SendPacket(packet);
        }

        private void SendCurrencies()
        {
            SetupCurrency packet = new();

            foreach (var pair in _currencyStorage)
            {
                CurrencyTypesRecord entry = CliDB.CurrencyTypesStorage.LookupByKey(pair.Key);

                // not send init meta currencies.
                if (entry == null ||
                    entry.CategoryID == 89) //CURRENCY_CATEGORY_META_CONQUEST
                    continue;

                SetupCurrency.Record record = new();
                record.Type = entry.Id;
                record.Quantity = pair.Value.Quantity;
                record.WeeklyQuantity = pair.Value.WeeklyQuantity;
                record.MaxWeeklyQuantity = GetCurrencyWeekCap(entry);
                record.TrackedQuantity = pair.Value.TrackedQuantity;
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
                    arenaTeam.FinishWeek();         // set played this week etc values to 0 in memory, too
                    arenaTeam.SaveToDB();           // save changes
                    arenaTeam.NotifyStatsChanged(); // notify the players of the changes
                }
            }

            foreach (var currency in _currencyStorage.Values)
            {
                currency.WeeklyQuantity = 0;
                currency.State = PlayerCurrencyState.Changed;
            }

            SendPacket(new ResetWeeklyCurrency());
        }

        public void AddExploredZones(uint pos, ulong mask)
        {
            SetUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ExploredZones, (int)pos), mask);
        }

        public void RemoveExploredZones(uint pos, ulong mask)
        {
            RemoveUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ExploredZones, (int)pos), mask);
        }

        private void CheckAreaExploreAndOutdoor()
        {
            if (!IsAlive())
                return;

            if (IsInFlight())
                return;

            if (WorldConfig.GetBoolValue(WorldCfg.VmapIndoorCheck))
                RemoveAurasWithAttribute(IsOutdoors() ? SpellAttr0.OnlyIndoors : SpellAttr0.OnlyOutdoors);

            uint areaId = GetAreaId();

            if (areaId == 0)
                return;

            var areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);

            if (areaEntry == null)
            {
                Log.outError(LogFilter.Player,
                             "Player '{0}' ({1}) discovered unknown area (x: {2} y: {3} z: {4} map: {5})",
                             GetName(),
                             GetGUID().ToString(),
                             GetPositionX(),
                             GetPositionY(),
                             GetPositionZ(),
                             GetMapId());

                return;
            }

            int offset = areaEntry.AreaBit / ActivePlayerData.ExploredZonesBits;

            if (offset >= PlayerConst.ExploredZonesSize)
            {
                Log.outError(LogFilter.Player,
                             "Wrong area flag {0} in map _data for (X: {1} Y: {2}) point to field PLAYER_EXPLORED_ZONES_1 + {3} ( {4} must be < {5} ).",
                             areaId,
                             GetPositionX(),
                             GetPositionY(),
                             offset,
                             offset,
                             PlayerConst.ExploredZonesSize);

                return;
            }

            ulong val = 1ul << (areaEntry.AreaBit % ActivePlayerData.ExploredZonesBits);
            ulong currFields = ActivePlayerData.ExploredZones[offset];

            if (!Convert.ToBoolean(currFields & val))
            {
                SetUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ExploredZones, (int)offset), val);

                UpdateCriteria(CriteriaType.RevealWorldMapOverlay, GetAreaId());

                var areaLevels = Global.DB2Mgr.GetContentTuningData(areaEntry.ContentTuningID, PlayerData.CtrOptions.GetValue().ContentTuningConditionMask);

                if (areaLevels.HasValue)
                {
                    if (IsMaxLevel())
                    {
                        SendExplorationExperience(areaId, 0);
                    }
                    else
                    {
                        ushort areaLevel = (ushort)Math.Min(Math.Max((ushort)GetLevel(), areaLevels.Value.MinLevel), areaLevels.Value.MaxLevel);
                        int diff = (int)GetLevel() - areaLevel;
                        uint XP;

                        if (diff < -5)
                        {
                            XP = (uint)(Global.ObjectMgr.GetBaseXP(GetLevel() + 5) * WorldConfig.GetFloatValue(WorldCfg.RateXpExplore));
                        }
                        else if (diff > 5)
                        {
                            int exploration_percent = 100 - ((diff - 5) * 5);

                            if (exploration_percent < 0)
                                exploration_percent = 0;

                            XP = (uint)(Global.ObjectMgr.GetBaseXP(areaLevel) * exploration_percent / 100 * WorldConfig.GetFloatValue(WorldCfg.RateXpExplore));
                        }
                        else
                        {
                            XP = (uint)(Global.ObjectMgr.GetBaseXP(areaLevel) * WorldConfig.GetFloatValue(WorldCfg.RateXpExplore));
                        }

                        if (WorldConfig.GetIntValue(WorldCfg.MinDiscoveredScaledXpRatio) != 0)
                        {
                            uint minScaledXP = (uint)(Global.ObjectMgr.GetBaseXP(areaLevel) * WorldConfig.GetFloatValue(WorldCfg.RateXpExplore)) * WorldConfig.GetUIntValue(WorldCfg.MinDiscoveredScaledXpRatio) / 100;
                            XP = Math.Max(minScaledXP, XP);
                        }

                        GiveXP(XP, null);
                        SendExplorationExperience(areaId, XP);
                    }

                    Log.outInfo(LogFilter.Player, "Player {0} discovered a new area: {1}", GetGUID().ToString(), areaId);
                }
            }
        }

        private void SendExplorationExperience(uint Area, uint Experience)
        {
            SendPacket(new ExplorationExperience(Experience, Area));
        }

        public void SendSysMessage(CypherStrings str, params object[] args)
        {
            string input = Global.ObjectMgr.GetCypherString(str);
            string pattern = @"%(\d+(\.\d+)?)?(d|f|s|u)";

            int count = 0;
            string result = Regex.Replace(input, pattern, m => { return string.Concat("{", count++, "}"); });

            SendSysMessage(result, args);
        }

        public void SendSysMessage(string str, params object[] args)
        {
            new CommandHandler(_session).SendSysMessage(string.Format(str, args));
        }

        public void SendBuyError(BuyResult msg, Creature creature, uint item)
        {
            BuyFailed packet = new();
            packet.VendorGUID = creature ? creature.GetGUID() : ObjectGuid.Empty;
            packet.Muid = item;
            packet.Reason = msg;
            SendPacket(packet);
        }

        public void SendSellError(SellResult msg, Creature creature, ObjectGuid guid)
        {
            SellResponse sellResponse = new();
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

            SendChatMessageToSetInRange(ChatMsg.Say, language, text, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay));
        }

        private void SendChatMessageToSetInRange(ChatMsg chatMsg, Language language, string text, float range)
        {
            CustomChatTextBuilder builder = new(this, chatMsg, text, language, this);
            LocalizedDo localizer = new(builder);

            // Send to self
            localizer.Invoke(this);

            // Send to players
            MessageDistDeliverer<LocalizedDo> notifier = new(this, localizer, range, false, null, true);
            Cell.VisitWorldObjects(this, notifier, range);
        }

        public override void Say(uint textId, WorldObject target = null)
        {
            Talk(textId, ChatMsg.Say, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
        }

        public override void Yell(string text, Language language, WorldObject obj = null)
        {
            Global.ScriptMgr.OnPlayerChat(this, ChatMsg.Yell, language, text);

            ChatPkt data = new();
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

            ChatPkt data = new();
            data.Initialize(ChatMsg.Emote, Language.Universal, this, this, text);
            SendMessageToSetInRange(data, WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote), true, !GetSession().HasPermission(RBACPermissions.TwoSideInteractionChat), true);
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

            ChatPkt data = new();
            data.Initialize(ChatMsg.Whisper, isLogged ? Language.AddonLogged : Language.Addon, this, this, text, 0, "", Locale.enUS, prefix);
            receiver.SendPacket(data);
        }

        public override void Whisper(string text, Language language, Player target = null, bool something = false)
        {
            bool isAddonMessage = language == Language.Addon;

            if (!isAddonMessage)               // if not addon _data
                language = Language.Universal; // whispers should always be readable

            //Player rPlayer = Global.ObjAccessor.FindPlayer(receiver);

            Global.ScriptMgr.OnPlayerChat(this, ChatMsg.Whisper, language, text, target);

            ChatPkt data = new();
            data.Initialize(ChatMsg.Whisper, language, this, this, text);
            target.SendPacket(data);

            // rest stuff shouldn't happen in case of addon message
            if (isAddonMessage)
                return;

            data.Initialize(ChatMsg.WhisperInform, language, target, target, text);
            SendPacket(data);

            if (!IsAcceptWhispers() &&
                !IsGameMaster() &&
                !target.IsGameMaster())
            {
                SetAcceptWhispers(true);
                SendSysMessage(CypherStrings.CommandWhisperon);
            }

            // announce afk or dnd message
            if (target.IsAFK())
                SendSysMessage(CypherStrings.PlayerAfk, target.GetName(), target.AutoReplyMsg);
            else if (target.IsDND())
                SendSysMessage(CypherStrings.PlayerDnd, target.GetName(), target.AutoReplyMsg);
        }

        public override void Whisper(uint textId, Player target, bool isBossWhisper = false)
        {
            if (!target)
                return;

            BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(textId);

            if (bct == null)
            {
                Log.outError(LogFilter.Unit, "WorldObject.Whisper: `broadcast_text` was not {0} found", textId);

                return;
            }

            Locale locale = target.GetSession().GetSessionDbLocaleIndex();
            ChatPkt packet = new();
            packet.Initialize(ChatMsg.Whisper, Language.Universal, this, target, Global.DB2Mgr.GetBroadcastTextValue(bct, locale, GetGender()));
            target.SendPacket(packet);
        }

        public bool CanUnderstandLanguage(Language language)
        {
            if (IsGameMaster())
                return true;

            foreach (var languageDesc in Global.LanguageMgr.GetLanguageDescById(language))
                if (languageDesc.SkillId != 0 &&
                    HasSkill((SkillType)languageDesc.SkillId))
                    return true;

            if (HasAuraTypeWithMiscvalue(AuraType.ComprehendLanguage, (int)language))
                return true;

            return false;
        }

        #endregion
    }
}