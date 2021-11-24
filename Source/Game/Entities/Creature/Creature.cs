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
using Framework.Database;
using Framework.Dynamic;
using Game.AI;
using Game.DataStorage;
using Game.Groups;
using Game.Loots;
using Game.Maps;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Creature : Unit
    {
        public Creature() : this(false) { }

        public Creature(bool worldObject) : base(worldObject)
        {
            m_respawnDelay = 300;
            m_corpseDelay = 60;
            m_boundaryCheckTime = 2500;
            reactState = ReactStates.Aggressive;
            DefaultMovementType = MovementGeneratorType.Idle;
            _regenerateHealth = true;
            m_meleeDamageSchoolMask = SpellSchoolMask.Normal;
            triggerJustAppeared = true;

            RegenTimer = SharedConst.CreatureRegenInterval;

            m_SightDistance = SharedConst.SightRangeUnit;

            ResetLootMode(); // restore default loot mode

            m_homePosition = new WorldLocation();

            _currentWaypointNodeInfo = new();
        }

        public override void AddToWorld()
        {
            // Register the creature for guid lookup
            if (!IsInWorld)
            {
                GetMap().GetObjectsStore().Add(GetGUID(), this);
                if (m_spawnId != 0)
                    GetMap().GetCreatureBySpawnIdStore().Add(m_spawnId, this);

                base.AddToWorld();
                SearchFormation();
                InitializeAI();
                if (IsVehicle())
                    GetVehicleKit().Install();

                if (m_zoneScript != null)
                    m_zoneScript.OnCreatureCreate(this);
            }
        }

        public override void RemoveFromWorld()
        {
            if (IsInWorld)
            {
                if (m_zoneScript != null)
                    m_zoneScript.OnCreatureRemove(this);

                if (m_formation != null)
                    FormationMgr.RemoveCreatureFromGroup(m_formation, this);

                base.RemoveFromWorld();

                if (m_spawnId != 0)
                    GetMap().GetCreatureBySpawnIdStore().Remove(m_spawnId, this);
                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        public void DisappearAndDie()
        {
            ForcedDespawn(0);
        }

        public bool IsReturningHome()
        {
            if (GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Home)
                return true;

            return false;
        }

        public void SearchFormation()
        {
            if (IsSummon())
                return;

            ulong lowguid = GetSpawnId();
            if (lowguid == 0)
                return;

            var formationInfo = FormationMgr.GetFormationInfo(lowguid);
            if (formationInfo != null)
                FormationMgr.AddCreatureToGroup(formationInfo.LeaderSpawnId, this);
        }

        public bool IsFormationLeader()
        {
            if (m_formation == null)
                return false;

            return m_formation.IsLeader(this);
        }

        public void SignalFormationMovement(Position destination, uint id = 0, WaypointMoveType moveType = 0, bool orientation = false)
        {
            if (m_formation == null)
                return;

            if (!m_formation.IsLeader(this))
                return;

            m_formation.LeaderMoveTo(destination, id, moveType, orientation);
        }

        public bool IsFormationLeaderMoveAllowed()
        {
            if (m_formation == null)
                return false;

            return m_formation.CanLeaderStartMoving();
        }

        public void RemoveCorpse(bool setSpawnTime = true, bool destroyForNearbyPlayers = true)
        {
            if (GetDeathState() != DeathState.Corpse)
                return;

            if (m_respawnCompatibilityMode)
            {
                m_corpseRemoveTime = GameTime.GetGameTime();
                SetDeathState(DeathState.Dead);
                RemoveAllAuras();
                //DestroyForNearbyPlayers(); // old UpdateObjectVisibility()
                loot.Clear();
                uint respawnDelay = m_respawnDelay;
                CreatureAI ai = GetAI();
                if (ai != null)
                    ai.CorpseRemoved(respawnDelay);

                if (destroyForNearbyPlayers)
                    DestroyForNearbyPlayers();

                // Should get removed later, just keep "compatibility" with scripts
                if (setSpawnTime)
                    m_respawnTime = Math.Max(GameTime.GetGameTime() + respawnDelay, m_respawnTime);

                // if corpse was removed during falling, the falling will continue and override relocation to respawn position
                if (IsFalling())
                    StopMoving();

                float x, y, z, o;
                GetRespawnPosition(out x, out y, out z, out o);

                // We were spawned on transport, calculate real position
                if (IsSpawnedOnTransport())
                {
                    Position pos = m_movementInfo.transport.pos;
                    pos.posX = x;
                    pos.posY = y;
                    pos.posZ = z;
                    pos.SetOrientation(o);

                    ITransport transport = GetDirectTransport();
                    if (transport != null)
                        transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                }

                UpdateAllowedPositionZ(x, y, ref z);
                SetHomePosition(x, y, z, o);
                GetMap().CreatureRelocation(this, x, y, z, o);
            }
            else
            {
                // In case this is called directly and normal respawn timer not set
                // Since this timer will be longer than the already present time it
                // will be ignored if the correct place added a respawn timer
                if (setSpawnTime)
                {
                    uint respawnDelay = m_respawnDelay;
                    m_respawnTime = Math.Max(GameTime.GetGameTime() + respawnDelay, m_respawnTime);

                    SaveRespawnTime(0, false);
                }

                TempSummon summon = ToTempSummon();
                if (summon != null)
                    summon.UnSummon();
                else
                    AddObjectToRemoveList();
            }
        }

        public bool InitEntry(uint entry, CreatureData data = null)
        {
            CreatureTemplate normalInfo = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (normalInfo == null)
            {
                Log.outError(LogFilter.Sql, "Creature.InitEntry creature entry {0} does not exist.", entry);
                return false;
            }

            // get difficulty 1 mode entry
            CreatureTemplate cInfo = null;
            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(GetMap().GetDifficultyID());
            while (cInfo == null && difficultyEntry != null)
            {
                int idx = CreatureTemplate.DifficultyIDToDifficultyEntryIndex(difficultyEntry.Id);
                if (idx == -1)
                    break;

                if (normalInfo.DifficultyEntry[idx] != 0)
                {
                    cInfo = Global.ObjectMgr.GetCreatureTemplate(normalInfo.DifficultyEntry[idx]);
                    break;
                }

                if (difficultyEntry.FallbackDifficultyID == 0)
                    break;

                difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficultyEntry.FallbackDifficultyID);
            }

            if (cInfo == null)
                cInfo = normalInfo;

            // Initialize loot duplicate count depending on raid difficulty
            if (GetMap().Is25ManRaid())
                loot.maxDuplicates = 3;

            SetEntry(entry);                                        // normal entry always
            m_creatureInfo = cInfo;                                 // map mode related always

            // equal to player Race field, but creature does not have race
            SetRace(0);
            SetClass((Class)cInfo.UnitClass);

            // Cancel load if no model defined
            if (cInfo.GetFirstValidModel() == null)
            {
                Log.outError(LogFilter.Sql, "Creature (Entry: {0}) has no model defined in table `creature_template`, can't load. ", entry);
                return false;
            }

            CreatureModel model = ObjectManager.ChooseDisplayId(cInfo, data);
            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelRandomGender(ref model, cInfo);
            if (minfo == null)                                             // Cancel load if no model defined
            {
                Log.outError(LogFilter.Sql, "Creature (Entry: {0}) has invalid model {1} defined in table `creature_template`, can't load.", entry, model.CreatureDisplayID);
                return false;
            }

            SetDisplayId(model.CreatureDisplayID, model.DisplayScale);
            SetNativeDisplayId(model.CreatureDisplayID, model.DisplayScale);

            // Load creature equipment
            if (data == null || data.equipmentId == 0)
                LoadEquipment(); // use default equipment (if available)
            else
            {
                m_originalEquipmentId = data.equipmentId;
                LoadEquipment(data.equipmentId);
            }

            SetName(normalInfo.Name);                              // at normal entry always

            SetModCastingSpeed(1.0f);
            SetModSpellHaste(1.0f);
            SetModHaste(1.0f);
            SetModRangedHaste(1.0f);
            SetModHasteRegen(1.0f);
            SetModTimeRate(1.0f);

            SetSpeedRate(UnitMoveType.Walk, cInfo.SpeedWalk);
            SetSpeedRate(UnitMoveType.Run, cInfo.SpeedRun);
            SetSpeedRate(UnitMoveType.Swim, 1.0f);      // using 1.0 rate
            SetSpeedRate(UnitMoveType.Flight, 1.0f);    // using 1.0 rate

            SetObjectScale(cInfo.Scale);

            SetHoverHeight(cInfo.HoverHeight);

            SetCanDualWield(cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.UseOffhandAttack));

            // checked at loading
            DefaultMovementType = (MovementGeneratorType)(data != null ? data.movementType : cInfo.MovementType);
            if (m_respawnradius == 0 && DefaultMovementType == MovementGeneratorType.Random)
                DefaultMovementType = MovementGeneratorType.Idle;

            for (byte i = 0; i < SharedConst.MaxCreatureSpells; ++i)
                m_spells[i] = GetCreatureTemplate().Spells[i];

            return true;
        }

        public bool UpdateEntry(uint entry, CreatureData data = null, bool updateLevel = true)
        {
            if (!InitEntry(entry, data))
                return false;

            CreatureTemplate cInfo = GetCreatureTemplate();

            _regenerateHealth = cInfo.RegenHealth;

            // creatures always have melee weapon ready if any unless specified otherwise
            if (GetCreatureAddon() == null)
                SetSheath(SheathState.Melee);

            SetFaction(cInfo.Faction);

            ulong npcFlags;
            uint unitFlags, unitFlags2, unitFlags3, dynamicFlags;
            ObjectManager.ChooseCreatureFlags(cInfo, out npcFlags, out unitFlags, out unitFlags2, out unitFlags3, out dynamicFlags, data);

            if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Worldevent))
                npcFlags |= Global.GameEventMgr.GetNPCFlag(this);

            SetNpcFlags((NPCFlags)(npcFlags & 0xFFFFFFFF));
            SetNpcFlags2((NPCFlags2)(npcFlags >> 32));

            // if unit is in combat, keep this flag
            unitFlags &= ~(uint)UnitFlags.InCombat;
            if (IsInCombat())
                unitFlags |= (uint)UnitFlags.InCombat;

            SetUnitFlags((UnitFlags)unitFlags);
            SetUnitFlags2((UnitFlags2)unitFlags2);
            SetUnitFlags3((UnitFlags3)unitFlags3);

            SetDynamicFlags((UnitDynFlags)dynamicFlags);

            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.StateAnimID), Global.DB2Mgr.GetEmptyAnimStateID());

            SetCanDualWield(cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.UseOffhandAttack));

            SetBaseAttackTime(WeaponAttackType.BaseAttack, cInfo.BaseAttackTime);
            SetBaseAttackTime(WeaponAttackType.OffAttack, cInfo.BaseAttackTime);
            SetBaseAttackTime(WeaponAttackType.RangedAttack, cInfo.RangeAttackTime);

            if (updateLevel)
                SelectLevel();
            else
                UpdateLevelDependantStats(); // We still re-initialize level dependant stats on entry update

            SetMeleeDamageSchool((SpellSchools)cInfo.DmgSchool);
            SetStatFlatModifier(UnitMods.ResistanceHoly, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Holy]);
            SetStatFlatModifier(UnitMods.ResistanceFire, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Fire]);
            SetStatFlatModifier(UnitMods.ResistanceNature, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Nature]);
            SetStatFlatModifier(UnitMods.ResistanceFrost, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Frost]);
            SetStatFlatModifier(UnitMods.ResistanceShadow, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Shadow]);
            SetStatFlatModifier(UnitMods.ResistanceArcane, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Arcane]);

            SetCanModifyStats(true);
            UpdateAllStats();

            // checked and error show at loading templates
            var factionTemplate = CliDB.FactionTemplateStorage.LookupByKey(cInfo.Faction);
            if (factionTemplate != null)
                SetPvP(factionTemplate.Flags.HasAnyFlag((ushort)FactionTemplateFlags.PVP));

            // updates spell bars for vehicles and set player's faction - should be called here, to overwrite faction that is set from the new template
            if (IsVehicle())
            {
                Player owner = GetCharmerOrOwnerPlayerOrPlayerItself();
                if (owner != null) // this check comes in case we don't have a player
                {
                    SetFaction(owner.GetFaction()); // vehicles should have same as owner faction
                    owner.VehicleSpellInitialize();
                }
            }

            // trigger creature is always not selectable and can not be attacked
            if (IsTrigger())
                AddUnitFlag(UnitFlags.NotSelectable);

            InitializeReactState();

            if (Convert.ToBoolean(cInfo.FlagsExtra & CreatureFlagsExtra.NoTaunt))
            {
                ApplySpellImmune(0, SpellImmunity.State, AuraType.ModTaunt, true);
                ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.AttackMe, true);
            }

            if (GetMovementTemplate().IsRooted())
                SetControlled(true, UnitState.Root);

            UpdateMovementFlags();
            LoadCreaturesAddon();

            LoadTemplateImmunities();
            GetThreatManager().EvaluateSuppressed();

            //We must update last scriptId or it looks like we reloaded a script, breaking some things such as gossip temporarily
            LastUsedScriptID = GetScriptId();

            return true;
        }

        public override void Update(uint diff)
        {
            if (IsAIEnabled() && triggerJustAppeared && m_deathState != DeathState.Dead)
            {
                if (m_respawnCompatibilityMode && VehicleKit != null)
                    VehicleKit.Reset();

                triggerJustAppeared = false;
                GetAI().JustAppeared();
            }

            UpdateMovementFlags();

            switch (m_deathState)
            {
                case DeathState.JustRespawned:
                case DeathState.JustDied:
                    Log.outError(LogFilter.Unit, $"Creature ({GetGUID()}) in wrong state: {m_deathState}");
                    break;
                case DeathState.Dead:
                {
                    if (!m_respawnCompatibilityMode)
                    {
                        Log.outError(LogFilter.Unit, $"Creature (GUID: {GetGUID().GetCounter()} Entry: {GetEntry()}) in wrong state: DEAD (3)");
                        break;
                    }
                    long now = GameTime.GetGameTime();
                    if (m_respawnTime <= now)
                    {
                        // Delay respawn if spawn group is not active
                        if (m_creatureData != null && !GetMap().IsSpawnGroupActive(m_creatureData.spawnGroupData.groupId))
                        {
                            m_respawnTime = now + RandomHelper.URand(4, 7);
                            break; // Will be rechecked on next Update call after delay expires
                        }

                        ObjectGuid dbtableHighGuid = ObjectGuid.Create(HighGuid.Creature, GetMapId(), GetEntry(), m_spawnId);
                        long linkedRespawnTime = GetMap().GetLinkedRespawnTime(dbtableHighGuid);
                        if (linkedRespawnTime == 0)             // Can respawn
                            Respawn();
                        else                                // the master is dead
                        {
                            ObjectGuid targetGuid = Global.ObjectMgr.GetLinkedRespawnGuid(dbtableHighGuid);
                            if (targetGuid == dbtableHighGuid) // if linking self, never respawn (check delayed to next day)
                                SetRespawnTime(Time.Week);
                            else
                            {
                                // else copy time from master and add a little
                                long baseRespawnTime = Math.Max(linkedRespawnTime, now);
                                long offset = RandomHelper.URand(5, Time.Minute);

                                // linked guid can be a boss, uses std::numeric_limits<time_t>::max to never respawn in that instance
                                // we shall inherit it instead of adding and causing an overflow
                                if (baseRespawnTime <= long.MaxValue - offset)
                                    m_respawnTime = baseRespawnTime + offset;
                                else
                                    m_respawnTime = long.MaxValue;
                            }
                            SaveRespawnTime(); // also save to DB immediately
                        }
                    }
                    break;
                }
                case DeathState.Corpse:
                    base.Update(diff);
                    if (m_deathState != DeathState.Corpse)
                        break;

                    if (m_groupLootTimer != 0 && !lootingGroupLowGUID.IsEmpty())
                    {
                        if (m_groupLootTimer <= diff)
                        {
                            Group group = Global.GroupMgr.GetGroupByGUID(lootingGroupLowGUID);
                            if (group)
                                group.EndRoll(loot, GetMap());

                            m_groupLootTimer = 0;
                            lootingGroupLowGUID.Clear();
                        }
                        else m_groupLootTimer -= diff;
                    }
                    else if (m_corpseRemoveTime <= GameTime.GetGameTime())
                    {
                        RemoveCorpse(false);
                        Log.outDebug(LogFilter.Unit, "Removing corpse... {0} ", GetEntry());
                    }
                    break;
                case DeathState.Alive:
                    base.Update(diff);

                    if (!IsAlive())
                        break;

                    GetThreatManager().Update(diff);

                    if (m_shouldReacquireTarget && !IsFocusing(null, true))
                    {
                        SetTarget(m_suppressedTarget);

                        if (!HasUnitFlag2(UnitFlags2.DisableTurn))
                        {
                            if (!m_suppressedTarget.IsEmpty())
                            {
                                WorldObject objTarget = Global.ObjAccessor.GetWorldObject(this, m_suppressedTarget);
                                if (objTarget)
                                    SetFacingToObject(objTarget);
                            }
                            else
                                SetFacingTo(m_suppressedOrientation);
                        }

                        m_shouldReacquireTarget = false;
                    }

                    // periodic check to see if the creature has passed an evade boundary
                    if (IsAIEnabled() && !IsInEvadeMode() && IsEngaged())
                    {
                        if (diff >= m_boundaryCheckTime)
                        {
                            GetAI().CheckInRoom();
                            m_boundaryCheckTime = 2500;
                        }
                        else
                            m_boundaryCheckTime -= diff;
                    }

                    // if periodic combat pulse is enabled and we are both in combat and in a dungeon, do this now
                    if (m_combatPulseDelay > 0 && IsEngaged() && GetMap().IsDungeon())
                    {
                        if (diff > m_combatPulseTime)
                            m_combatPulseTime = 0;
                        else
                            m_combatPulseTime -= diff;

                        if (m_combatPulseTime == 0)
                        {
                            var players = GetMap().GetPlayers();
                            foreach (var player in players)
                            {
                                if (player.IsGameMaster())
                                    continue;

                                if (player.IsAlive() && IsHostileTo(player))
                                    EngageWithTarget(player);
                            }
                            m_combatPulseTime = m_combatPulseDelay * Time.InMilliseconds;
                        }
                    }

                    // do not allow the AI to be changed during update
                    AIUpdateTick(diff);

                    if (!IsAlive())
                        break;

                    if (RegenTimer > 0)
                    {
                        if (diff >= RegenTimer)
                            RegenTimer = 0;
                        else
                            RegenTimer -= diff;
                    }

                    if (RegenTimer == 0)
                    {
                        bool bInCombat = IsInCombat() && (!GetVictim() ||                                        // if IsInCombat() is true and this has no victim
                                                          !GetVictim().GetCharmerOrOwnerPlayerOrPlayerItself() ||                // or the victim/owner/charmer is not a player
                                                          !GetVictim().GetCharmerOrOwnerPlayerOrPlayerItself().IsGameMaster()); // or the victim/owner/charmer is not a GameMaster

                        if (!IsInEvadeMode() && (!bInCombat || IsPolymorphed() || CanNotReachTarget())) // regenerate health if not in combat or if polymorphed
                            RegenerateHealth();

                        if (GetPowerType() == PowerType.Energy)
                            Regenerate(PowerType.Energy);
                        else
                            Regenerate(PowerType.Mana);

                        RegenTimer = SharedConst.CreatureRegenInterval;
                    }

                    if (CanNotReachTarget() && !IsInEvadeMode() && !GetMap().IsRaid())
                    {
                        m_cannotReachTimer += diff;
                        if (m_cannotReachTimer >= SharedConst.CreatureNoPathEvadeTime)
                        {
                            CreatureAI ai = GetAI();
                            if (ai != null)
                                ai.EnterEvadeMode(EvadeReason.NoPath);
                        }
                    }
                    break;
            }
        }

        public void Regenerate(PowerType power)
        {
            int curValue = GetPower(power);
            int maxValue = GetMaxPower(power);

            if (!HasUnitFlag2(UnitFlags2.RegeneratePower))
                return;

            if (curValue >= maxValue)
                return;

            float addvalue;

            switch (power)
            {
                case PowerType.Focus:
                {
                    // For hunter pets.
                    addvalue = 24 * WorldConfig.GetFloatValue(WorldCfg.RatePowerFocus);
                    break;
                }
                case PowerType.Energy:
                {
                    // For deathknight's ghoul.
                    addvalue = 20;
                    break;
                }
                case PowerType.Mana:
                {
                    // Combat and any controlled creature
                    if (IsInCombat() || GetCharmerOrOwnerGUID().IsEmpty())
                    {
                        float ManaIncreaseRate = WorldConfig.GetFloatValue(WorldCfg.RatePowerMana);
                        addvalue = (27.0f / 5.0f + 17.0f) * ManaIncreaseRate;
                    }
                    else
                        addvalue = maxValue / 3;

                    break;
                }
                default:
                    return;
            }

            // Apply modifiers (if any).
            addvalue *= GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerRegenPercent, (int)power);
            addvalue += GetTotalAuraModifierByMiscValue(AuraType.ModPowerRegen, (int)power) * (IsHunterPet() ? SharedConst.PetFocusRegenInterval : SharedConst.CreatureRegenInterval) / (5 * Time.InMilliseconds);

            ModifyPower(power, (int)addvalue);
        }

        void RegenerateHealth()
        {
            if (!CanRegenerateHealth())
                return;

            ulong curValue = GetHealth();
            ulong maxValue = GetMaxHealth();

            if (curValue >= maxValue)
                return;

            long addvalue;

            // Not only pet, but any controlled creature (and not polymorphed)
            if (!GetCharmerOrOwnerGUID().IsEmpty() && !IsPolymorphed())
            {
                float HealthIncreaseRate = WorldConfig.GetFloatValue(WorldCfg.RateHealth);
                addvalue = (uint)(0.015f * GetMaxHealth() * HealthIncreaseRate);
            }
            else
                addvalue = (long)maxValue / 3;

            // Apply modifiers (if any).
            addvalue *= (int)GetTotalAuraMultiplier(AuraType.ModHealthRegenPercent);
            addvalue += GetTotalAuraModifier(AuraType.ModRegen) * SharedConst.CreatureRegenInterval / (5 * Time.InMilliseconds);

            ModifyHealth(addvalue);
        }

        public void DoFleeToGetAssistance()
        {
            if (!GetVictim())
                return;

            if (HasAuraType(AuraType.PreventsFleeing))
                return;

            float radius = WorldConfig.GetFloatValue(WorldCfg.CreatureFamilyFleeAssistanceRadius);
            if (radius > 0)
            {
                var u_check = new NearestAssistCreatureInCreatureRangeCheck(this, GetVictim(), radius);
                var searcher = new CreatureLastSearcher(this, u_check);
                Cell.VisitGridObjects(this, searcher, radius);

                var creature = searcher.GetTarget();

                SetNoSearchAssistance(true);
                UpdateSpeed(UnitMoveType.Run);

                if (!creature)
                    SetControlled(true, UnitState.Fleeing);
                else
                    GetMotionMaster().MoveSeekAssistance(creature.GetPositionX(), creature.GetPositionY(), creature.GetPositionZ());
            }
        }

        bool DestoryAI()
        {
            PopAI();
            RefreshAI();
            return true;
        }

        public bool InitializeAI(CreatureAI ai = null)
        {
            InitializeMovementAI();

            SetAI(ai != null ? ai : AISelector.SelectAI(this));

            i_AI.InitializeAI();

            // Initialize vehicle
            if (GetVehicleKit() != null)
                GetVehicleKit().Reset();

            return true;
        }

        void InitializeMovementAI()
        {
            if (m_formation != null)
            {
                if (m_formation.GetLeader() == this)
                    m_formation.FormationReset(false);
                else if (m_formation.IsFormed())
                {
                    GetMotionMaster().MoveIdle(); //wait the order of leader
                    return;
                }
            }

            GetMotionMaster().Initialize();
        }

        public static Creature CreateCreature(uint entry, Map map, Position pos, uint vehId = 0)
        {
            CreatureTemplate cInfo = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (cInfo == null)
                return null;

            ulong lowGuid;
            if (vehId != 0 || cInfo.VehicleId != 0)
                lowGuid = map.GenerateLowGuid(HighGuid.Vehicle);
            else
                lowGuid = map.GenerateLowGuid(HighGuid.Creature);

            Creature creature = new();
            if (!creature.Create(lowGuid, map, entry, pos, null, vehId))
                return null;

            return creature;
        }

        public static Creature CreateCreatureFromDB(ulong spawnId, Map map, bool addToMap = true, bool allowDuplicate = false)
        {
            Creature creature = new();
            if (!creature.LoadFromDB(spawnId, map, addToMap, allowDuplicate))
                return null;

            return creature;
        }

        public bool Create(ulong guidlow, Map map, uint entry, Position pos, CreatureData data = null, uint vehId = 0, bool dynamic = false)
        {
            SetMap(map);

            if (data != null)
            {
                PhasingHandler.InitDbPhaseShift(GetPhaseShift(), data.phaseUseFlags, data.phaseId, data.phaseGroup);
                PhasingHandler.InitDbVisibleMapId(GetPhaseShift(), data.terrainSwapMap);
            }

            // Set if this creature can handle dynamic spawns
            if (!dynamic)
                SetRespawnCompatibilityMode();

            CreatureTemplate cinfo = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (cinfo == null)
            {
                Log.outError(LogFilter.Sql, "Creature.Create: creature template (guidlow: {0}, entry: {1}) does not exist.", guidlow, entry);
                return false;
            }

            //! Relocate before CreateFromProto, to initialize coords and allow
            //! returning correct zone id for selecting OutdoorPvP/Battlefield script
            Relocate(pos);

            // Check if the position is valid before calling CreateFromProto(), otherwise we might add Auras to Creatures at
            // invalid position, triggering a crash about Auras not removed in the destructor
            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Unit, $"Creature.Create: given coordinates for creature (guidlow {guidlow}, entry {entry}) are not valid ({pos})");
                return false;
            }
            UpdatePositionData();

            // Allow players to see those units while dead, do it here (mayby altered by addon auras)
            if (cinfo.TypeFlags.HasAnyFlag(CreatureTypeFlags.GhostVisible))
                m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive | GhostVisibilityType.Ghost);

            if (!CreateFromProto(guidlow, entry, data, vehId))
                return false;

            cinfo = GetCreatureTemplate(); // might be different than initially requested
            if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.DungeonBoss) && map.IsDungeon())
                m_respawnDelay = 0; // special value, prevents respawn for dungeon bosses unless overridden

            switch (cinfo.Rank)
            {
                case CreatureEliteType.Rare:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayRare);
                    break;
                case CreatureEliteType.Elite:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayElite);
                    break;
                case CreatureEliteType.RareElite:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayRareelite);
                    break;
                case CreatureEliteType.WorldBoss:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayWorldboss);
                    break;
                default:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayNormal);
                    break;
            }
            LoadCreaturesAddon();

            //! Need to be called after LoadCreaturesAddon - MOVEMENTFLAG_HOVER is set there
            posZ += GetHoverOffset();

            LastUsedScriptID = GetScriptId();

            if (IsSpiritHealer() || IsSpiritGuide() || GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.GhostVisibility))
            {
                m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
                m_serverSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
            }

            if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.IgnorePathfinding))
                AddUnitState(UnitState.IgnorePathfinding);

            if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.ImmunityKnockback))
            {
                ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
                ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBackDest, true);
            }

            GetThreatManager().Initialize();

            return true;
        }

        public Unit SelectVictim()
        {
            Unit target;

            if (CanHaveThreatList())
                target = GetThreatManager().GetCurrentVictim();
            else if (!HasReactState(ReactStates.Passive))
            {
                // We're a player pet, probably
                target = GetAttackerForHelper();
                if (!target && IsSummon())
                {
                    Unit owner = ToTempSummon().GetOwner();
                    if (owner != null)
                    {
                        if (owner.IsInCombat())
                            target = owner.GetAttackerForHelper();
                        if (!target)
                        {
                            foreach (var itr in owner.m_Controlled)
                            {
                                if (itr.IsInCombat())
                                {
                                    target = itr.GetAttackerForHelper();
                                    if (target)
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            else
                return null;

            if (target && _IsTargetAcceptable(target) && CanCreatureAttack(target))
            {
                if (!IsFocusing(null, true))
                    SetInFront(target);
                return target;
            }

            /// @todo a vehicle may eat some mob, so mob should not evade
            if (GetVehicle())
                return null;

            var iAuras = GetAuraEffectsByType(AuraType.ModInvisibility);
            if (!iAuras.Empty())
            {
                foreach (var itr in iAuras)
                {
                    if (itr.GetBase().IsPermanent())
                    {
                        GetAI().EnterEvadeMode(EvadeReason.Other);
                        break;
                    }
                }
                return null;
            }

            // enter in evade mode in other case
            GetAI().EnterEvadeMode(EvadeReason.NoHostiles);

            return null;
        }

        public void InitializeReactState()
        {
            if (IsTotem() || IsTrigger() || IsCritter() || IsSpiritService())
                SetReactState(ReactStates.Passive);
            else
                SetReactState(ReactStates.Aggressive);
        }

        public bool CanInteractWithBattleMaster(Player player, bool msg)
        {
            if (!IsBattleMaster())
                return false;

            BattlegroundTypeId bgTypeId = Global.BattlegroundMgr.GetBattleMasterBG(GetEntry());
            if (!msg)
                return player.GetBGAccessByLevel(bgTypeId);

            if (!player.GetBGAccessByLevel(bgTypeId))
            {
                player.PlayerTalkClass.ClearMenus();
                switch (bgTypeId)
                {
                    case BattlegroundTypeId.AV:
                        player.PlayerTalkClass.SendGossipMenu(7616, GetGUID());
                        break;
                    case BattlegroundTypeId.WS:
                        player.PlayerTalkClass.SendGossipMenu(7599, GetGUID());
                        break;
                    case BattlegroundTypeId.AB:
                        player.PlayerTalkClass.SendGossipMenu(7642, GetGUID());
                        break;
                    case BattlegroundTypeId.EY:
                    case BattlegroundTypeId.NA:
                    case BattlegroundTypeId.BE:
                    case BattlegroundTypeId.AA:
                    case BattlegroundTypeId.RL:
                    case BattlegroundTypeId.SA:
                    case BattlegroundTypeId.DS:
                    case BattlegroundTypeId.RV:
                        player.PlayerTalkClass.SendGossipMenu(10024, GetGUID());
                        break;
                    default: break;
                }
                return false;
            }
            return true;
        }

        public bool CanResetTalents(Player player)
        {
            return player.GetLevel() >= 15 && player.GetClass() == GetCreatureTemplate().TrainerClass;
        }

        public void SetTextRepeatId(byte textGroup, byte id)
        {
            if (!m_textRepeat.ContainsKey(textGroup))
            {
                m_textRepeat.Add(textGroup, id);
                return;
            }

            var repeats = m_textRepeat[textGroup];
            if (!repeats.Contains(id))
                repeats.Add(id);
            else
                Log.outError(LogFilter.Sql, "CreatureTextMgr: TextGroup {0} for ({1}) {2}, id {3} already added", textGroup, GetName(), GetGUID().ToString(), id);
        }

        public List<byte> GetTextRepeatGroup(byte textGroup)
        {
            return m_textRepeat.LookupByKey(textGroup);
        }

        public void ClearTextRepeatGroup(byte textGroup)
        {
            var groupList = m_textRepeat[textGroup];
            if (groupList != null)
                groupList.Clear();
        }

        public bool CanGiveExperience()
        {
            return !IsCritter()
                && !IsPet()
                && !IsTotem()
                && !GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoXpAtKill);
        }

        public override void AtEnterCombat()
        {
            base.AtEnterCombat();

            if (!GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.MountedCombatAllowed))
                Dismount();

            if (IsPet() || IsGuardian()) // update pets' speed for catchup OOC speed
            {
                UpdateSpeed(UnitMoveType.Run);
                UpdateSpeed(UnitMoveType.Swim);
                UpdateSpeed(UnitMoveType.Flight);
            }
        }

        public override void AtExitCombat()
        {
            base.AtExitCombat();

            ClearUnitState(UnitState.AttackPlayer);
            if (HasDynamicFlag(UnitDynFlags.Tapped))
                SetDynamicFlags((UnitDynFlags)GetCreatureTemplate().DynamicFlags);

            if (IsPet() || IsGuardian()) // update pets' speed for catchup OOC speed
            {
                UpdateSpeed(UnitMoveType.Run);
                UpdateSpeed(UnitMoveType.Swim);
                UpdateSpeed(UnitMoveType.Flight);
            }
        }

        public bool IsEscortNPC(bool onlyIfActive = true)
        {
            CreatureAI ai = GetAI();
            if (ai != null)
                return ai.IsEscortNPC(onlyIfActive);

            return false;
        }

        public override bool IsMovementPreventedByCasting()
        {
            // first check if currently a movement allowed channel is active and we're not casting
            Spell spell = GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (spell != null)
            {
                if (spell.GetState() != SpellState.Finished && spell.IsChannelActive())
                    if (spell.GetSpellInfo().IsMoveAllowedChannel())
                        if (HasUnitState(UnitState.Casting))
                            return true;
            }

            if (IsFocusing(null, true))
                return true;

            if (HasUnitState(UnitState.Casting))
                return true;

            return false;
        }

        public void StartPickPocketRefillTimer()
        {
            _pickpocketLootRestore = GameTime.GetGameTime() + WorldConfig.GetIntValue(WorldCfg.CreaturePickpocketRefill);
        }
        public void ResetPickPocketRefillTimer() { _pickpocketLootRestore = 0; }
        public bool CanGeneratePickPocketLoot() { return _pickpocketLootRestore <= GameTime.GetGameTime(); }
        public ObjectGuid GetLootRecipientGUID() { return m_lootRecipient; }

        public Player GetLootRecipient()
        {
            if (m_lootRecipient.IsEmpty())
                return null;
            return Global.ObjAccessor.FindPlayer(m_lootRecipient);
        }

        public Group GetLootRecipientGroup()
        {
            if (m_lootRecipientGroup.IsEmpty())
                return null;

            return Global.GroupMgr.GetGroupByGUID(m_lootRecipientGroup);
        }

        public void SetLootRecipient(Unit unit, bool withGroup = true)
        {
            // set the player whose group should receive the right
            // to loot the creature after it dies
            // should be set to NULL after the loot disappears

            if (unit == null)
            {
                m_lootRecipient.Clear();
                m_lootRecipientGroup.Clear();
                RemoveDynamicFlag(UnitDynFlags.Lootable | UnitDynFlags.Tapped);
                return;
            }

            if (!unit.IsTypeId(TypeId.Player) && !unit.IsVehicle())
                return;

            Player player = unit.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (player == null)                                             // normal creature, no player involved
                return;

            m_lootRecipient = player.GetGUID();
            if (withGroup)
            {
                Group group = player.GetGroup();
                if (group)
                    m_lootRecipientGroup = group.GetGUID();
            }
            else
                m_lootRecipientGroup = ObjectGuid.Empty;

            AddDynamicFlag(UnitDynFlags.Tapped);
        }

        public bool IsTappedBy(Player player)
        {
            if (player.GetGUID() == m_lootRecipient)
                return true;

            Group playerGroup = player.GetGroup();
            if (!playerGroup || playerGroup != GetLootRecipientGroup()) // if we dont have a group we arent the recipient
                return false;                                           // if creature doesnt have group bound it means it was solo killed by someone else

            return true;
        }

        public void SaveToDB()
        {
            // this should only be used when the creature has already been loaded
            // preferably after adding to map, because mapid may not be valid otherwise
            CreatureData data = Global.ObjectMgr.GetCreatureData(m_spawnId);
            if (data == null)
            {
                Log.outError(LogFilter.Unit, "Creature.SaveToDB failed, cannot get creature data!");
                return;
            }

            uint mapId = GetTransport() ? (uint)GetTransport().GetGoInfo().MoTransport.SpawnMap : GetMapId();
            SaveToDB(mapId, data.spawnDifficulties);
        }

        public virtual void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties)
        {
            // update in loaded data
            if (m_spawnId == 0)
                m_spawnId = Global.ObjectMgr.GenerateCreatureSpawnId();

            CreatureData data = Global.ObjectMgr.NewOrExistCreatureData(m_spawnId);

            uint displayId = GetNativeDisplayId();
            ulong npcflag = ((ulong)m_unitData.NpcFlags[1] << 32) | m_unitData.NpcFlags[0];
            uint unitFlags = m_unitData.Flags;
            uint unitFlags2 = m_unitData.Flags2;
            uint unitFlags3 = m_unitData.Flags3;
            UnitDynFlags dynamicflags = (UnitDynFlags)(uint)m_objectData.DynamicFlags;

            // check if it's a custom model and if not, use 0 for displayId
            CreatureTemplate cinfo = GetCreatureTemplate();
            if (cinfo != null)
            {
                foreach (CreatureModel model in cinfo.Models)
                    if (displayId != 0 && displayId == model.CreatureDisplayID)
                        displayId = 0;

                if (npcflag == (uint)cinfo.Npcflag)
                    npcflag = 0;

                if (unitFlags == (uint)cinfo.UnitFlags)
                    unitFlags = 0;

                if (unitFlags2 == cinfo.UnitFlags2)
                    unitFlags2 = 0;

                if (unitFlags3 == cinfo.UnitFlags3)
                    unitFlags3 = 0;

                if (dynamicflags == (UnitDynFlags)cinfo.DynamicFlags)
                    dynamicflags = 0;
            }

            if (data.spawnId == 0)
                data.spawnId = m_spawnId;
            Cypher.Assert(data.spawnId == m_spawnId);

            data.Id = GetEntry();
            data.displayid = displayId;
            data.equipmentId = (sbyte)GetCurrentEquipmentId();

            if (GetTransport() == null)
                data.spawnPoint.WorldRelocate(this);
            else
                data.spawnPoint.WorldRelocate(mapid, GetTransOffsetX(), GetTransOffsetY(), GetTransOffsetZ(), GetTransOffsetO());

            data.spawntimesecs = (int)m_respawnDelay;
            // prevent add data integrity problems
            data.spawndist = GetDefaultMovementType() == MovementGeneratorType.Idle ? 0.0f : m_respawnradius;
            data.currentwaypoint = 0;
            data.curhealth = (uint)GetHealth();
            data.curmana = (uint)GetPower(PowerType.Mana);
            // prevent add data integrity problems
            data.movementType = (byte)(m_respawnradius == 0 && GetDefaultMovementType() == MovementGeneratorType.Random
                ? MovementGeneratorType.Idle : GetDefaultMovementType());
            data.spawnDifficulties = spawnDifficulties;
            data.npcflag = npcflag;
            data.unit_flags = unitFlags;
            data.unit_flags2 = unitFlags2;
            data.unit_flags3 = unitFlags3;
            data.dynamicflags = (uint)dynamicflags;
            if (data.spawnGroupData == null)
                data.spawnGroupData = Global.ObjectMgr.GetDefaultSpawnGroup();

            data.phaseId = GetDBPhase() > 0 ? (uint)GetDBPhase() : data.phaseId;
            data.phaseGroup = GetDBPhase() < 0 ? (uint)-GetDBPhase() : data.phaseGroup;

            // update in DB
            SQLTransaction trans = new();

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_CREATURE);
            stmt.AddValue(0, m_spawnId);
            trans.Append(stmt);

            byte index = 0;

            stmt = DB.World.GetPreparedStatement(WorldStatements.INS_CREATURE);
            stmt.AddValue(index++, m_spawnId);
            stmt.AddValue(index++, GetEntry());
            stmt.AddValue(index++, mapid);
            stmt.AddValue(index++, data.spawnDifficulties.Empty() ? "" : string.Join(',', data.spawnDifficulties));
            stmt.AddValue(index++, data.phaseId);
            stmt.AddValue(index++, data.phaseGroup);
            stmt.AddValue(index++, displayId);
            stmt.AddValue(index++, GetCurrentEquipmentId());
            stmt.AddValue(index++, GetPositionX());
            stmt.AddValue(index++, GetPositionY());
            stmt.AddValue(index++, GetPositionZ());
            stmt.AddValue(index++, GetOrientation());
            stmt.AddValue(index++, m_respawnDelay);
            stmt.AddValue(index++, m_respawnradius);
            stmt.AddValue(index++, 0);
            stmt.AddValue(index++, GetHealth());
            stmt.AddValue(index++, GetPower(PowerType.Mana));
            stmt.AddValue(index++, (byte)GetDefaultMovementType());
            stmt.AddValue(index++, npcflag);
            stmt.AddValue(index++, unitFlags);
            stmt.AddValue(index++, unitFlags2);
            stmt.AddValue(index++, unitFlags3);
            stmt.AddValue(index++, (uint)dynamicflags);
            trans.Append(stmt);

            DB.World.CommitTransaction(trans);
        }

        public void SelectLevel()
        {
            CreatureTemplate cInfo = GetCreatureTemplate();

            // level
            var minMaxLevels = cInfo.GetMinMaxLevel();
            int minlevel = Math.Min(minMaxLevels[0], minMaxLevels[1]);
            int maxlevel = Math.Max(minMaxLevels[0], minMaxLevels[1]);
            int level = (minlevel == maxlevel ? minlevel : RandomHelper.IRand(minlevel, maxlevel));
            SetLevel((uint)level);

            CreatureLevelScaling scaling = cInfo.GetLevelScaling(GetMap().GetDifficultyID());

            var levels = Global.DB2Mgr.GetContentTuningData(scaling.ContentTuningID, 0);
            if (levels != null)
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ScalingLevelMin), levels.Value.MinLevel);
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ScalingLevelMax), levels.Value.MaxLevel);
            }

            int mindelta = Math.Min(scaling.DeltaLevelMax, scaling.DeltaLevelMin);
            int maxdelta = Math.Max(scaling.DeltaLevelMax, scaling.DeltaLevelMin);
            int delta = mindelta == maxdelta ? mindelta : RandomHelper.IRand(mindelta, maxdelta);

            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ScalingLevelDelta), delta);
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ContentTuningID), scaling.ContentTuningID);

            UpdateLevelDependantStats();
        }

        void UpdateLevelDependantStats()
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureEliteType rank = IsPet() ? 0 : cInfo.Rank;
            uint level = GetLevel();
            CreatureBaseStats stats = Global.ObjectMgr.GetCreatureBaseStats(level, cInfo.UnitClass);

            // health
            float healthmod = _GetHealthMod(rank);

            uint basehp = (uint)GetMaxHealthByLevel(level);
            uint health = (uint)(basehp * healthmod);

            SetCreateHealth(health);
            SetMaxHealth(health);
            SetHealth(health);
            ResetPlayerDamageReq();

            // mana
            uint mana = stats.GenerateMana(cInfo);
            SetCreateMana(mana);

            switch (GetClass())
            {
                case Class.Paladin:
                case Class.Mage:
                    SetMaxPower(PowerType.Mana, (int)mana);
                    SetPower(PowerType.Mana, (int)mana);
                    break;
                default: // We don't set max power here, 0 makes power bar hidden
                    break;
            }

            SetStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Base, health);

            //Damage
            float basedamage = GetBaseDamageForLevel(level);
            float weaponBaseMinDamage = basedamage;
            float weaponBaseMaxDamage = basedamage * 1.5f;

            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

            SetBaseWeaponDamage(WeaponAttackType.OffAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
            SetBaseWeaponDamage(WeaponAttackType.OffAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

            SetBaseWeaponDamage(WeaponAttackType.RangedAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
            SetBaseWeaponDamage(WeaponAttackType.RangedAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

            SetStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Base, stats.AttackPower);
            SetStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Base, stats.RangedAttackPower);

            float armor = GetBaseArmorForLevel(level); /// @todo Why is this treated as uint32 when it's a float?
            SetStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, armor);
        }

        void SelectWildBattlePetLevel()
        {
            if (IsWildBattlePet())
            {
                byte wildBattlePetLevel = 1;

                var areaTable = CliDB.AreaTableStorage.LookupByKey(GetZoneId());
                if (areaTable != null)
                    if (areaTable.WildBattlePetLevelMin > 0)
                        wildBattlePetLevel = (byte)RandomHelper.URand(areaTable.WildBattlePetLevelMin, areaTable.WildBattlePetLevelMax);

                SetWildBattlePetLevel(wildBattlePetLevel);
            }
        }

        float _GetHealthMod(CreatureEliteType Rank)
        {
            switch (Rank)                                           // define rates for each elite rank
            {
                case CreatureEliteType.Normal:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureNormalHp);
                case CreatureEliteType.Elite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteEliteHp);
                case CreatureEliteType.RareElite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteRareeliteHp);
                case CreatureEliteType.WorldBoss:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteWorldbossHp);
                case CreatureEliteType.Rare:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteRareHp);
                default:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteRareeliteHp);
            }
        }

        public void LowerPlayerDamageReq(ulong unDamage)
        {
            if (m_PlayerDamageReq != 0)
            {
                if (m_PlayerDamageReq > unDamage)
                    m_PlayerDamageReq -= unDamage;
                else
                    m_PlayerDamageReq = 0;
            }
        }

        public static float _GetDamageMod(CreatureEliteType Rank)
        {
            switch (Rank)                                           // define rates for each elite rank
            {
                case CreatureEliteType.Normal:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureNormalDamage);
                case CreatureEliteType.Elite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteEliteDamage);
                case CreatureEliteType.RareElite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteRareeliteDamage);
                case CreatureEliteType.WorldBoss:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteWorldbossDamage);
                case CreatureEliteType.Rare:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteRareDamage);
                default:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteEliteDamage);
            }
        }

        public float GetSpellDamageMod(CreatureEliteType Rank)
        {
            switch (Rank)                                           // define rates for each elite rank
            {
                case CreatureEliteType.Normal:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureNormalSpelldamage);
                case CreatureEliteType.Elite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteEliteSpelldamage);
                case CreatureEliteType.RareElite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteRareeliteSpelldamage);
                case CreatureEliteType.WorldBoss:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteWorldbossSpelldamage);
                case CreatureEliteType.Rare:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteRareSpelldamage);
                default:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureEliteEliteSpelldamage);
            }
        }

        bool CreateFromProto(ulong guidlow, uint entry, CreatureData data = null, uint vehId = 0)
        {
            SetZoneScript();
            if (m_zoneScript != null && data != null)
            {
                entry = m_zoneScript.GetCreatureEntry(guidlow, data);
                if (entry == 0)
                    return false;
            }

            CreatureTemplate cinfo = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (cinfo == null)
            {
                Log.outError(LogFilter.Sql, "Creature.CreateFromProto: creature template (guidlow: {0}, entry: {1}) does not exist.", guidlow, entry);
                return false;
            }

            SetOriginalEntry(entry);

            if (vehId != 0 || cinfo.VehicleId != 0)
                _Create(ObjectGuid.Create(HighGuid.Vehicle, GetMapId(), entry, guidlow));
            else
                _Create(ObjectGuid.Create(HighGuid.Creature, GetMapId(), entry, guidlow));

            if (!UpdateEntry(entry, data))
                return false;

            if (vehId == 0)
            {
                if (GetCreatureTemplate().VehicleId != 0)
                {
                    vehId = GetCreatureTemplate().VehicleId;
                    entry = GetCreatureTemplate().Entry;
                }
                else
                    vehId = cinfo.VehicleId;
            }

            if (vehId != 0)
                CreateVehicleKit(vehId, entry, true);

            return true;
        }

        public override void SetCanDualWield(bool value)
        {
            base.SetCanDualWield(value);
            UpdateDamagePhysical(WeaponAttackType.OffAttack);
        }

        public void LoadEquipment(int id = 1, bool force = true)
        {
            if (id == 0)
            {
                if (force)
                {
                    for (byte i = 0; i < SharedConst.MaxEquipmentItems; ++i)
                        SetVirtualItem(i, 0);
                    m_equipmentId = 0;
                }
                return;
            }

            EquipmentInfo einfo = Global.ObjectMgr.GetEquipmentInfo(GetEntry(), id);
            if (einfo == null)
                return;

            m_equipmentId = (byte)id;
            for (byte i = 0; i < SharedConst.MaxEquipmentItems; ++i)
                SetVirtualItem(i, einfo.Items[i].ItemId, einfo.Items[i].AppearanceModId, einfo.Items[i].ItemVisual);
        }

        public void SetSpawnHealth()
        {
            if (_regenerateHealthLock)
                return;

            ulong curhealth;
            if (m_creatureData != null && !_regenerateHealth)
            {
                curhealth = m_creatureData.curhealth;
                if (curhealth != 0)
                {
                    curhealth = (uint)(curhealth * _GetHealthMod(GetCreatureTemplate().Rank));
                    if (curhealth < 1)
                        curhealth = 1;
                }

                SetPower(PowerType.Mana, (int)m_creatureData.curmana);
            }
            else
            {
                curhealth = GetMaxHealth();
                SetFullPower(PowerType.Mana);
            }

            SetHealth((m_deathState == DeathState.Alive || m_deathState == DeathState.JustRespawned) ? curhealth : 0);
        }

        public override bool HasQuest(uint quest_id)
        {
            var qr = Global.ObjectMgr.GetCreatureQuestRelationBounds(GetEntry());
            foreach (var id in qr)
            {
                if (id == quest_id)
                    return true;
            }
            return false;
        }

        public override bool HasInvolvedQuest(uint quest_id)
        {
            var qir = Global.ObjectMgr.GetCreatureQuestInvolvedRelationBounds(GetEntry());
            foreach (var id in qir)
            {
                if (id == quest_id)
                    return true;
            }
            return false;
        }

        public void DeleteFromDB()
        {
            if (m_spawnId == 0)
            {
                Log.outError(LogFilter.Unit, "Trying to delete not saved {0}", GetGUID().ToString(), GetEntry());
                return;
            }

            GetMap().RemoveRespawnTime(SpawnObjectType.Creature, m_spawnId);
            Global.ObjectMgr.DeleteCreatureData(m_spawnId);

            SQLTransaction trans = new();

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_CREATURE);
            stmt.AddValue(0, m_spawnId);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_SPAWNGROUP_MEMBER);
            stmt.AddValue(0, (byte)SpawnObjectType.Creature);
            stmt.AddValue(1, m_spawnId);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_CREATURE_ADDON);
            stmt.AddValue(0, m_spawnId);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_GAME_EVENT_CREATURE);
            stmt.AddValue(0, m_spawnId);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_GAME_EVENT_MODEL_EQUIP);
            stmt.AddValue(0, m_spawnId);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, m_spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToCreature);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, m_spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToGO);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, m_spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToCreature);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, m_spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToCreature);
            trans.Append(stmt);

            DB.World.CommitTransaction(trans);

            // then delete any active instances of the creature
            var spawnMap = GetMap().GetCreatureBySpawnIdStore().LookupByKey(m_spawnId);
            foreach (var creature in spawnMap.ToList())
                creature.AddObjectToRemoveList();
        }

        public override bool IsInvisibleDueToDespawn()
        {
            if (base.IsInvisibleDueToDespawn())
                return true;

            if (IsAlive() || m_corpseRemoveTime > GameTime.GetGameTime())
                return false;

            return true;
        }

        public override bool CanAlwaysSee(WorldObject obj)
        {
            if (IsAIEnabled() && GetAI<CreatureAI>().CanSeeAlways(obj))
                return true;

            return false;
        }

        public bool CanStartAttack(Unit who, bool force)
        {
            if (IsCivilian())
                return false;

            // This set of checks is should be done only for creatures
            if ((IsImmuneToNPC() && !who.HasUnitFlag(UnitFlags.PlayerControlled)) || (IsImmuneToPC() && who.HasUnitFlag(UnitFlags.PlayerControlled)))
                return false;

            // Do not attack non-combat pets
            if (who.IsTypeId(TypeId.Unit) && who.GetCreatureType() == CreatureType.NonCombatPet)
                return false;

            if (!CanFly() && (GetDistanceZ(who) > SharedConst.CreatureAttackRangeZ + m_CombatDistance))
                return false;

            if (!force)
            {
                if (!_IsTargetAcceptable(who))
                    return false;

                if (!force && (IsNeutralToAll() || !IsWithinDistInMap(who, GetAttackDistance(who) + m_CombatDistance)))
                    return false;
            }

            if (!CanCreatureAttack(who, force))
                return false;

            return IsWithinLOSInMap(who);
        }

        public float GetAttackDistance(Unit player)
        {
            // WoW Wiki: the minimum radius seems to be 5 yards, while the maximum range is 45 yards
            float maxRadius = (45.0f * WorldConfig.GetFloatValue(WorldCfg.RateCreatureAggro));
            float minRadius = (5.0f * WorldConfig.GetFloatValue(WorldCfg.RateCreatureAggro));
            float aggroRate = WorldConfig.GetFloatValue(WorldCfg.RateCreatureAggro);
            byte expansionMaxLevel = (byte)Global.ObjectMgr.GetMaxLevelForExpansion((Expansion)GetCreatureTemplate().RequiredExpansion);

            uint playerLevel = player.GetLevelForTarget(this);
            uint creatureLevel = GetLevelForTarget(player);

            if (aggroRate == 0.0f)
                return 0.0f;

            // The aggro radius for creatures with equal level as the player is 20 yards.
            // The combatreach should not get taken into account for the distance so we drop it from the range (see Supremus as expample)
            float baseAggroDistance = 20.0f - GetCombatReach();
            float aggroRadius = baseAggroDistance;

            // detect range auras
            if ((creatureLevel + 5) <= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
            {
                aggroRadius += GetTotalAuraModifier(AuraType.ModDetectRange);
                aggroRadius += player.GetTotalAuraModifier(AuraType.ModDetectedRange);
            }

            // The aggro range of creatures with higher levels than the total player level for the expansion should get the maxlevel treatment
            // This makes sure that creatures such as bosses wont have a bigger aggro range than the rest of the npc's
            // The following code is used for blizzlike behavior such as skipable bosses (e.g. Commander Springvale at level 85)
            if (creatureLevel > expansionMaxLevel)
                aggroRadius += (float)expansionMaxLevel - (float)playerLevel;
            // + - 1 yard for each level difference between player and creature
            else
                aggroRadius += (float)creatureLevel - (float)playerLevel;

            // Make sure that we wont go over the total range limits
            if (aggroRadius > maxRadius)
                aggroRadius = maxRadius;
            else if (aggroRadius < minRadius)
                aggroRadius = minRadius;

            return (aggroRadius * aggroRate);
        }

        public override void SetDeathState(DeathState s)
        {
            base.SetDeathState(s);

            if (s == DeathState.JustDied)
            {
                m_corpseRemoveTime = GameTime.GetGameTime() + m_corpseDelay;
                uint respawnDelay = m_respawnDelay;
                uint scalingMode = WorldConfig.GetUIntValue(WorldCfg.RespawnDynamicMode);
                if (scalingMode != 0)
                    GetMap().ApplyDynamicModeRespawnScaling(this, m_spawnId, ref respawnDelay, scalingMode);
                // @todo remove the boss respawn time hack in a dynspawn follow-up once we have creature groups in instances
                if (m_respawnCompatibilityMode)
                {
                    if (IsDungeonBoss() && m_respawnDelay == 0)
                        m_respawnTime = long.MaxValue; // never respawn in this instance
                    else
                        m_respawnTime = GameTime.GetGameTime() + respawnDelay + m_corpseDelay;
                }
                else
                {
                    if (IsDungeonBoss() && m_respawnDelay == 0)
                        m_respawnTime = long.MaxValue; // never respawn in this instance
                    else
                        m_respawnTime = GameTime.GetGameTime() + respawnDelay;
                }

                // always save boss respawn time at death to prevent crash cheating
                if (WorldConfig.GetBoolValue(WorldCfg.SaveRespawnTimeImmediately) || IsWorldBoss())
                    SaveRespawnTime();
                else if (!m_respawnCompatibilityMode)
                    SaveRespawnTime(0, false);

                ReleaseFocus(null, false);               // remove spellcast focus
                DoNotReacquireTarget(); // cancel delayed re-target
                SetTarget(ObjectGuid.Empty); // drop target - dead mobs shouldn't ever target things

                SetNpcFlags(NPCFlags.None);
                SetNpcFlags2(NPCFlags2.None);

                SetMountDisplayId(0); // if creature is mounted on a virtual mount, remove it at death

                SetActive(false);

                if (HasSearchedAssistance())
                {
                    SetNoSearchAssistance(false);
                    UpdateSpeed(UnitMoveType.Run);
                }

                //Dismiss group if is leader
                if (m_formation != null && m_formation.GetLeader() == this)
                    m_formation.FormationReset(true);

                bool needsFalling = IsFlying() || IsHovering();
                SetHover(false);

                if (needsFalling)
                    GetMotionMaster().MoveFall();

                base.SetDeathState(DeathState.Corpse);
            }
            else if (s == DeathState.JustRespawned)
            {
                if (IsPet())
                    SetFullHealth();
                else
                    SetSpawnHealth();

                SetLootRecipient(null);
                ResetPlayerDamageReq();

                SetCannotReachTarget(false);
                UpdateMovementFlags();

                ClearUnitState(UnitState.AllErasable);

                if (!IsPet())
                {
                    CreatureData creatureData = GetCreatureData();
                    CreatureTemplate cInfo = GetCreatureTemplate();

                    ulong npcFlags;
                    uint unitFlags, unitFlags2, unitFlags3, dynamicFlags;
                    ObjectManager.ChooseCreatureFlags(cInfo, out npcFlags, out unitFlags, out unitFlags2, out unitFlags3, out dynamicFlags, creatureData);

                    if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Worldevent))
                        npcFlags |= Global.GameEventMgr.GetNPCFlag(this);

                    SetNpcFlags((NPCFlags)(npcFlags & 0xFFFFFFFF));
                    SetNpcFlags2((NPCFlags2)(npcFlags >> 32));

                    SetUnitFlags((UnitFlags)unitFlags);
                    SetUnitFlags2((UnitFlags2)unitFlags2);
                    SetUnitFlags3((UnitFlags3)unitFlags3);
                    SetDynamicFlags((UnitDynFlags)dynamicFlags);

                    RemoveUnitFlag(UnitFlags.InCombat);

                    SetMeleeDamageSchool((SpellSchools)cInfo.DmgSchool);
                }

                InitializeMovementAI();
                base.SetDeathState(DeathState.Alive);
                LoadCreaturesAddon();
            }
        }

        public void Respawn(bool force = false)
        {
            if (force)
            {
                if (IsAlive())
                    SetDeathState(DeathState.JustDied);
                else if (GetDeathState() != DeathState.Corpse)
                    SetDeathState(DeathState.Corpse);
            }

            if (m_respawnCompatibilityMode)
            {
                DestroyForNearbyPlayers();
                RemoveCorpse(false, false);

                if (GetDeathState() == DeathState.Dead)
                {
                    if (m_spawnId != 0)
                        GetMap().RemoveRespawnTime(SpawnObjectType.Creature, m_spawnId);

                    Log.outDebug(LogFilter.Unit, "Respawning creature {0} ({1})", GetName(), GetGUID().ToString());
                    m_respawnTime = 0;
                    ResetPickPocketRefillTimer();
                    loot.Clear();

                    if (m_originalEntry != GetEntry())
                        UpdateEntry(m_originalEntry);

                    SelectLevel();

                    SetDeathState(DeathState.JustRespawned);

                    CreatureModel display = new(GetNativeDisplayId(), GetNativeDisplayScale(), 1.0f);
                    if (Global.ObjectMgr.GetCreatureModelRandomGender(ref display, GetCreatureTemplate()) != null)
                    {
                        SetDisplayId(display.CreatureDisplayID, display.DisplayScale);
                        SetNativeDisplayId(display.CreatureDisplayID, display.DisplayScale);
                    }

                    GetMotionMaster().InitializeDefault();

                    //Re-initialize reactstate that could be altered by movementgenerators
                    InitializeReactState();

                    UnitAI ai = GetAI();
                    if (ai != null) // reset the AI to be sure no dirty or uninitialized values will be used till next tick
                        ai.Reset();

                    triggerJustAppeared = true;

                    uint poolid = GetSpawnId() != 0 ? Global.PoolMgr.IsPartOfAPool<Creature>(GetSpawnId()) : 0;
                    if (poolid != 0)
                        Global.PoolMgr.UpdatePool<Creature>(poolid, GetSpawnId());
                }
                UpdateObjectVisibility();
            }
            else
            {
                if (m_spawnId != 0)
                    GetMap().RemoveRespawnTime(SpawnObjectType.Creature, m_spawnId, true);
            }

            Log.outDebug(LogFilter.Unit, $"Respawning creature {GetName()} ({GetGUID()})");
        }

        public void ForcedDespawn(uint timeMSToDespawn = 0, TimeSpan forceRespawnTimer = default)
        {
            if (timeMSToDespawn != 0)
            {
                m_Events.AddEvent(new ForcedDespawnDelayEvent(this, forceRespawnTimer), m_Events.CalculateTime(timeMSToDespawn));
                return;
            }

            if (m_respawnCompatibilityMode)
            {
                uint corpseDelay = GetCorpseDelay();
                uint respawnDelay = GetRespawnDelay();

                // do it before killing creature
                DestroyForNearbyPlayers();

                bool overrideRespawnTime = false;
                if (IsAlive())
                {
                    if (forceRespawnTimer > TimeSpan.Zero)
                    {
                        SetCorpseDelay(0);
                        SetRespawnDelay((uint)forceRespawnTimer.TotalSeconds);
                        overrideRespawnTime = false;
                    }

                    SetDeathState(DeathState.JustDied);
                }

                // Skip corpse decay time
                RemoveCorpse(overrideRespawnTime, false);

                SetCorpseDelay(corpseDelay);
                SetRespawnDelay(respawnDelay);
            }
            else
            {
                if (forceRespawnTimer > TimeSpan.Zero)
                    SaveRespawnTime((uint)forceRespawnTimer.TotalSeconds);
                else
                {
                    uint respawnDelay = m_respawnDelay;
                    uint scalingMode = WorldConfig.GetUIntValue(WorldCfg.RespawnDynamicMode);
                    if (scalingMode != 0)
                        GetMap().ApplyDynamicModeRespawnScaling(this, m_spawnId, ref respawnDelay, scalingMode);
                    m_respawnTime = GameTime.GetGameTime() + respawnDelay;
                    SaveRespawnTime();
                }

                AddObjectToRemoveList();
            }
        }

        public void DespawnOrUnsummon(TimeSpan time, TimeSpan forceRespawnTimer = default) { DespawnOrUnsummon((uint)time.TotalMilliseconds, forceRespawnTimer); }

        public void DespawnOrUnsummon(uint msTimeToDespawn = 0, TimeSpan forceRespawnTimer = default)
        {
            TempSummon summon = ToTempSummon();
            if (summon != null)
                summon.UnSummon(msTimeToDespawn);
            else
                ForcedDespawn(msTimeToDespawn, forceRespawnTimer);
        }

        public void LoadTemplateImmunities()
        {
            // uint32 max used for "spell id", the immunity system will not perform SpellInfo checks against invalid spells
            // used so we know which immunities were loaded from template
            uint placeholderSpellId = uint.MaxValue;

            // unapply template immunities (in case we're updating entry)
            for (uint i = 0; i < (int)Mechanics.Max; ++i)
                ApplySpellImmune(placeholderSpellId, SpellImmunity.Mechanic, i, false);

            for (var i = (int)SpellSchools.Normal; i < (int)SpellSchools.Max; ++i)
                ApplySpellImmune(placeholderSpellId, SpellImmunity.School, 1u << i, false);

            // don't inherit immunities for hunter pets
            if (GetOwnerGUID().IsPlayer() && IsHunterPet())
                return;

            uint mask = GetCreatureTemplate().MechanicImmuneMask;
            if (mask != 0)
            {
                for (uint i = 0 + 1; i < (int)Mechanics.Max; ++i)
                {
                    if ((mask & (1u << ((int)i - 1))) != 0)
                        ApplySpellImmune(placeholderSpellId, SpellImmunity.Mechanic, i, true);
                }
            }

            mask = GetCreatureTemplate().SpellSchoolImmuneMask;
            if (mask != 0)
                for (var i = (int)SpellSchools.Normal; i <= (int)SpellSchools.Max; ++i)
                    if ((mask & (1 << i)) != 0)
                        ApplySpellImmune(placeholderSpellId, SpellImmunity.School, 1u << i, true);
        }

        public override bool IsImmunedToSpell(SpellInfo spellInfo, WorldObject caster)
        {
            if (spellInfo == null)
                return false;

            bool immunedToAllEffects = true;
            foreach (var spellEffectInfo in spellInfo.GetEffects())
            {
                if (!spellEffectInfo.IsEffect())
                    continue;

                if (!IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster))
                {
                    immunedToAllEffects = false;
                    break;
                }
            }

            if (immunedToAllEffects)
                return true;

            return base.IsImmunedToSpell(spellInfo, caster);
        }

        public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, WorldObject caster)
        {
            if (GetCreatureTemplate().CreatureType == CreatureType.Mechanical && spellEffectInfo.IsEffect(SpellEffectName.Heal))
                return true;

            return base.IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster);
        }

        public bool IsElite()
        {
            if (IsPet())
                return false;

            var rank = GetCreatureTemplate().Rank;
            return rank != CreatureEliteType.Elite && rank != CreatureEliteType.RareElite;
        }

        public bool IsWorldBoss()
        {
            if (IsPet())
                return false;

            return Convert.ToBoolean(GetCreatureTemplate().TypeFlags & CreatureTypeFlags.BossMob);
        }

        // select nearest hostile unit within the given distance (regardless of threat list).
        public Unit SelectNearestTarget(float dist = 0)
        {
            if (dist == 0.0f)
                dist = SharedConst.MaxVisibilityDistance;

            var u_check = new NearestHostileUnitCheck(this, dist);
            var searcher = new UnitLastSearcher(this, u_check);
            Cell.VisitAllObjects(this, searcher, dist);

            return searcher.GetTarget();
        }

        // select nearest hostile unit within the given attack distance (i.e. distance is ignored if > than ATTACK_DISTANCE), regardless of threat list.
        public Unit SelectNearestTargetInAttackDistance(float dist = 0)
        {
            if (dist > SharedConst.MaxVisibilityDistance)
            {
                Log.outError(LogFilter.Unit, "Creature ({0}) SelectNearestTargetInAttackDistance called with dist > MAX_VISIBILITY_DISTANCE. Distance set to ATTACK_DISTANCE.", GetGUID().ToString());
                dist = SharedConst.AttackDistance;
            }

            var u_check = new NearestHostileUnitInAttackDistanceCheck(this, dist);
            var searcher = new UnitLastSearcher(this, u_check);

            Cell.VisitAllObjects(this, searcher, Math.Max(dist, SharedConst.AttackDistance));

            return searcher.GetTarget();
        }

        public void SendAIReaction(AiReaction reactionType)
        {
            AIReaction packet = new();

            packet.UnitGUID = GetGUID();
            packet.Reaction = reactionType;

            SendMessageToSet(packet, true);
        }

        public void CallAssistance()
        {
            if (!m_AlreadyCallAssistance && GetVictim() != null && !IsPet() && !IsCharmed())
            {
                SetNoCallAssistance(true);

                float radius = WorldConfig.GetFloatValue(WorldCfg.CreatureFamilyAssistanceRadius);

                if (radius > 0)
                {
                    List<Creature> assistList = new();

                    var u_check = new AnyAssistCreatureInRangeCheck(this, GetVictim(), radius);
                    var searcher = new CreatureListSearcher(this, assistList, u_check);
                    Cell.VisitGridObjects(this, searcher, radius);

                    if (!assistList.Empty())
                    {
                        AssistDelayEvent e = new(GetVictim().GetGUID(), this);
                        while (!assistList.Empty())
                        {
                            // Pushing guids because in delay can happen some creature gets despawned
                            e.AddAssistant(assistList.First().GetGUID());
                            assistList.Remove(assistList.First());
                        }
                        m_Events.AddEvent(e, m_Events.CalculateTime(WorldConfig.GetUIntValue(WorldCfg.CreatureFamilyAssistanceDelay)));
                    }
                }
            }
        }

        public void CallForHelp(float radius)
        {
            if (radius <= 0.0f || !IsEngaged() || IsPet() || IsCharmed())
                return;

            Unit target = GetThreatManager().GetCurrentVictim();
            if (target == null)
                target = GetThreatManager().GetAnyTarget();
            if (target == null)
                target = GetCombatManager().GetAnyTarget();

            if (target == null)
            {
                Log.outError(LogFilter.Unit, $"Creature {GetEntry()} ({GetName()}) is engaged without threat list");
                return;
            }

            var u_do = new CallOfHelpCreatureInRangeDo(this, target, radius);
            var worker = new CreatureWorker(this, u_do);
            Cell.VisitGridObjects(this, worker, radius);
        }

        public bool CanAssistTo(Unit u, Unit enemy, bool checkfaction = true)
        {
            // is it true?
            if (!HasReactState(ReactStates.Aggressive))
                return false;

            // we don't need help from zombies :)
            if (!IsAlive())
                return false;

            // we cannot assist in evade mode
            if (IsInEvadeMode())
                return false;

            // or if enemy is in evade mode
            if (enemy.GetTypeId() == TypeId.Unit && enemy.ToCreature().IsInEvadeMode())
                return false;

            // we don't need help from non-combatant ;)
            if (IsCivilian())
                return false;

            if (HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.NotSelectable) || IsImmuneToNPC())
                return false;

            // skip fighting creature
            if (IsEngaged())
                return false;

            // only free creature
            if (!GetCharmerOrOwnerGUID().IsEmpty())
                return false;

            // only from same creature faction
            if (checkfaction)
            {
                if (GetFaction() != u.GetFaction())
                    return false;
            }
            else
            {
                if (!IsFriendlyTo(u))
                    return false;
            }

            // skip non hostile to caster enemy creatures
            if (!IsHostileTo(enemy))
                return false;

            return true;
        }

        public bool _IsTargetAcceptable(Unit target)
        {
            // if the target cannot be attacked, the target is not acceptable
            if (IsFriendlyTo(target) || !target.IsTargetableForAttack(false)
                || (m_vehicle != null && (IsOnVehicle(target) || m_vehicle.GetBase().IsOnVehicle(target))))
                return false;

            if (target.HasUnitState(UnitState.Died))
            {
                // guards can detect fake death
                if (IsGuard() && target.HasUnitFlag2(UnitFlags2.FeignDeath))
                    return true;
                else
                    return false;
            }

            // if I'm already fighting target, or I'm hostile towards the target, the target is acceptable
            if (IsEngagedBy(target) || IsHostileTo(target))
                return true;

            // if the target's victim is not friendly, or the target is friendly, the target is not acceptable
            return false;
        }

        public override void SaveRespawnTime(uint forceDelay = 0, bool saveToDb = true)
        {
            if (IsSummon() || m_spawnId == 0 || (m_creatureData != null && !m_creatureData.dbData))
                return;

            if (m_respawnCompatibilityMode)
            {
                GetMap().SaveRespawnTimeDB(SpawnObjectType.Creature, m_spawnId, m_respawnTime);
                return;
            }

            long thisRespawnTime = forceDelay != 0 ? GameTime.GetGameTime() + forceDelay : m_respawnTime;
            GetMap().SaveRespawnTime(SpawnObjectType.Creature, m_spawnId, GetEntry(), thisRespawnTime, GetMap().GetZoneId(GetPhaseShift(), GetHomePosition()), GridDefines.ComputeGridCoord(GetHomePosition().GetPositionX(), GetHomePosition().GetPositionY()).GetId(), saveToDb && m_creatureData != null && m_creatureData.dbData);
        }

        public bool CanCreatureAttack(Unit victim, bool force = true)
        {
            if (!victim.IsInMap(this))
                return false;

            if (!IsValidAttackTarget(victim))
                return false;

            if (!victim.IsInAccessiblePlaceFor(this))
                return false;

            if (IsAIEnabled() && !GetAI().CanAIAttack(victim))
                return false;

            // we cannot attack in evade mode
            if (IsInEvadeMode())
                return false;

            // or if enemy is in evade mode
            if (victim.GetTypeId() == TypeId.Unit && victim.ToCreature().IsInEvadeMode())
                return false;

            if (!GetCharmerOrOwnerGUID().IsPlayer())
            {
                if (GetMap().IsDungeon())
                    return true;

                // don't check distance to home position if recently damaged, this should include taunt auras
                if (!IsWorldBoss() && (GetLastDamagedTime() > GameTime.GetGameTime() || HasAuraType(AuraType.ModTaunt)))
                    return true;
            }

            // Map visibility range, but no more than 2*cell size
            float dist = Math.Min(GetMap().GetVisibilityRange(), MapConst.SizeofCells * 2);

            Unit unit = GetCharmerOrOwner();
            if (unit != null)
                return victim.IsWithinDist(unit, dist);
            else
            {
                // include sizes for huge npcs
                dist += GetCombatReach() + victim.GetCombatReach();

                // to prevent creatures in air ignore attacks because distance is already too high...
                if (GetMovementTemplate().IsFlightAllowed())
                    return victim.IsInDist2d(m_homePosition, dist);
                else
                    return victim.IsInDist(m_homePosition, dist);
            }
        }

        CreatureAddon GetCreatureAddon()
        {
            if (m_spawnId != 0)
            {
                CreatureAddon addon = Global.ObjectMgr.GetCreatureAddon(m_spawnId);
                if (addon != null)
                    return addon;
            }

            // dependent from difficulty mode entry
            return Global.ObjectMgr.GetCreatureTemplateAddon(GetCreatureTemplate().Entry);
        }

        public bool LoadCreaturesAddon()
        {
            CreatureAddon cainfo = GetCreatureAddon();
            if (cainfo == null)
                return false;

            if (cainfo.mount != 0)
                Mount(cainfo.mount);

            if (cainfo.bytes1 != 0)
            {
                // 0 StandState
                // 1 FreeTalentPoints   Pet only, so always 0 for default creature
                // 2 StandFlags
                // 3 StandMiscFlags

                SetStandState((UnitStandStateType)(cainfo.bytes1 & 0xFF));
                SetVisFlags((UnitVisFlags)((cainfo.bytes1 >> 16) & 0xFF));
                SetAnimTier((UnitBytes1Flags)((cainfo.bytes1 >> 24) & 0xFF), false);

                //! Suspected correlation between UNIT_FIELD_BYTES_1, offset 3, value 0x2:
                //! If no inhabittype_fly (if no MovementFlag_DisableGravity or MovementFlag_CanFly flag found in sniffs)
                //! Check using InhabitType as movement flags are assigned dynamically
                //! basing on whether the creature is in air or not
                //! Set MovementFlag_Hover. Otherwise do nothing.
                if (CanHover())
                    AddUnitMovementFlag(MovementFlag.Hover);
            }

            if (cainfo.bytes2 != 0)
            {
                // 0 SheathState
                // 1 PvpFlags
                // 2 PetFlags           Pet only, so always 0 for default creature
                // 3 ShapeshiftForm     Must be determined/set by shapeshift spell/aura

                SetSheath((SheathState)(cainfo.bytes2 & 0xFF));
                SetPvpFlags(UnitPVPStateFlags.None);
                SetPetFlags(UnitPetFlags.None);
                SetShapeshiftForm(ShapeShiftForm.None);
            }

            if (cainfo.emote != 0)
                SetEmoteState((Emote)cainfo.emote);

            SetAIAnimKitId(cainfo.aiAnimKit);
            SetMovementAnimKitId(cainfo.movementAnimKit);
            SetMeleeAnimKitId(cainfo.meleeAnimKit);

            // Check if visibility distance different
            if (cainfo.visibilityDistanceType != VisibilityDistanceType.Normal)
                SetVisibilityDistanceOverride(cainfo.visibilityDistanceType);

            //Load Path
            if (cainfo.path_id != 0)
                _waypointPathId = cainfo.path_id;

            if (cainfo.auras != null)
            {
                foreach (var id in cainfo.auras)
                {
                    SpellInfo AdditionalSpellInfo = Global.SpellMgr.GetSpellInfo(id, GetMap().GetDifficultyID());
                    if (AdditionalSpellInfo == null)
                    {
                        Log.outError(LogFilter.Sql, "Creature ({0}) has wrong spell {1} defined in `auras` field.", GetGUID().ToString(), id);
                        continue;
                    }

                    // skip already applied aura
                    if (HasAura(id))
                        continue;

                    AddAura(id, this);
                    Log.outDebug(LogFilter.Unit, "Spell: {0} added to creature ({1})", id, GetGUID().ToString());
                }
            }
            return true;
        }

        // Send a message to LocalDefense channel for players opposition team in the zone
        public void SendZoneUnderAttackMessage(Player attacker)
        {
            Team enemy_team = attacker.GetTeam();

            ZoneUnderAttack packet = new();
            packet.AreaID = (int)GetAreaId();
            Global.WorldMgr.SendGlobalMessage(packet, null, (enemy_team == Team.Alliance ? Team.Horde : Team.Alliance));
        }

        public override bool HasSpell(uint spellId)
        {
            return m_spells.Contains(spellId);
        }

        public long GetRespawnTimeEx()
        {
            long now = GameTime.GetGameTime();
            if (m_respawnTime > now)
                return m_respawnTime;
            else
                return now;
        }

        public void GetRespawnPosition(out float x, out float y, out float z)
        {
            GetRespawnPosition(out x, out y, out z, out _, out _);
        }
        public void GetRespawnPosition(out float x, out float y, out float z, out float ori)
        {
            GetRespawnPosition(out x, out y, out z, out ori, out _);
        }
        public void GetRespawnPosition(out float x, out float y, out float z, out float ori, out float dist)
        {
            if (m_creatureData != null)
            {
                m_creatureData.spawnPoint.GetPosition(out x, out y, out z, out ori);
                dist = m_creatureData.spawndist;
            }
            else
            {
                Position homePos = GetHomePosition();
                homePos.GetPosition(out x, out y, out z, out ori);
                dist = 0;
            }
        }

        bool IsSpawnedOnTransport() { return m_creatureData != null && m_creatureData.spawnPoint.GetMapId() != GetMapId(); }

        public CreatureMovementData GetMovementTemplate()
        {
            CreatureMovementData movementOverride = Global.ObjectMgr.GetCreatureMovementOverride(m_spawnId);
            if (movementOverride != null)
                return movementOverride;

            return GetCreatureTemplate().Movement;
        }
        
        public void AllLootRemovedFromCorpse()
        {
            if (loot.loot_type != LootType.Skinning && !IsPet() && GetCreatureTemplate().SkinLootId != 0 && HasLootRecipient())
                if (LootStorage.Skinning.HaveLootFor(GetCreatureTemplate().SkinLootId))
                    AddUnitFlag(UnitFlags.Skinnable);

            long now = GameTime.GetGameTime();
            // Do not reset corpse remove time if corpse is already removed
            if (m_corpseRemoveTime <= now)
                return;

            float decayRate = WorldConfig.GetFloatValue(WorldCfg.RateCorpseDecayLooted);
            // corpse skinnable, but without skinning flag, and then skinned, corpse will despawn next update
            if (loot.loot_type == LootType.Skinning)
                m_corpseRemoveTime = now;
            else
                m_corpseRemoveTime = now + (uint)(m_corpseDelay * decayRate);

            m_respawnTime = Math.Max(m_corpseRemoveTime + m_respawnDelay, m_respawnTime);
        }

        public bool HasScalableLevels()
        {
            CreatureTemplate cinfo = GetCreatureTemplate();
            CreatureLevelScaling scaling = cinfo.GetLevelScaling(GetMap().GetDifficultyID());

            return scaling.ContentTuningID != 0;
        }

        ulong GetMaxHealthByLevel(uint level)
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureLevelScaling scaling = cInfo.GetLevelScaling(GetMap().GetDifficultyID());
            float baseHealth = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureHealth, level, cInfo.GetHealthScalingExpansion(), scaling.ContentTuningID, (Class)cInfo.UnitClass);

            return (ulong)(baseHealth * cInfo.ModHealth * cInfo.ModHealthExtra);
        }

        public override float GetHealthMultiplierForTarget(WorldObject target)
        {
            if (!HasScalableLevels())
                return 1.0f;

            uint levelForTarget = GetLevelForTarget(target);
            if (GetLevel() < levelForTarget)
                return 1.0f;

            return (float)GetMaxHealthByLevel(levelForTarget) / GetCreateHealth();
        }

        float GetBaseDamageForLevel(uint level)
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureLevelScaling scaling = cInfo.GetLevelScaling(GetMap().GetDifficultyID());
            return Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureAutoAttackDps, level, cInfo.GetHealthScalingExpansion(), scaling.ContentTuningID, (Class)cInfo.UnitClass);
        }

        public override float GetDamageMultiplierForTarget(WorldObject target)
        {
            if (!HasScalableLevels())
                return 1.0f;

            uint levelForTarget = GetLevelForTarget(target);

            return GetBaseDamageForLevel(levelForTarget) / GetBaseDamageForLevel(GetLevel());
        }

        float GetBaseArmorForLevel(uint level)
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureLevelScaling scaling = cInfo.GetLevelScaling(GetMap().GetDifficultyID());
            float baseArmor = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureArmor, level, cInfo.GetHealthScalingExpansion(), scaling.ContentTuningID, (Class)cInfo.UnitClass);
            return baseArmor * cInfo.ModArmor;
        }

        public override float GetArmorMultiplierForTarget(WorldObject target)
        {
            if (!HasScalableLevels())
                return 1.0f;

            uint levelForTarget = GetLevelForTarget(target);

            return GetBaseArmorForLevel(levelForTarget) / GetBaseArmorForLevel(GetLevel());
        }

        public override uint GetLevelForTarget(WorldObject target)
        {
            Unit unitTarget = target.ToUnit();
            if (unitTarget)
            {
                if (IsWorldBoss())
                {
                    int level = (int)(unitTarget.GetLevel() + WorldConfig.GetIntValue(WorldCfg.WorldBossLevelDiff));
                    return (uint)MathFunctions.RoundToInterval(ref level, 1u, 255u);
                }

                // If this creature should scale level, adapt level depending of target level
                // between UNIT_FIELD_SCALING_LEVEL_MIN and UNIT_FIELD_SCALING_LEVEL_MAX
                if (HasScalableLevels())
                {
                    int scalingLevelMin = m_unitData.ScalingLevelMin;
                    int scalingLevelMax = m_unitData.ScalingLevelMax;
                    int scalingLevelDelta = m_unitData.ScalingLevelDelta;
                    int scalingFactionGroup = m_unitData.ScalingFactionGroup;
                    int targetLevel = unitTarget.m_unitData.EffectiveLevel;
                    if (targetLevel == 0)
                        targetLevel = (int)unitTarget.GetLevel();

                    int targetLevelDelta = 0;

                    Player playerTarget = target.ToPlayer();
                    if (playerTarget != null)
                    {
                        if (scalingFactionGroup != 0 && CliDB.FactionTemplateStorage.LookupByKey(CliDB.ChrRacesStorage.LookupByKey(playerTarget.GetRace()).FactionID).FactionGroup != scalingFactionGroup)
                            scalingLevelMin = scalingLevelMax;

                        int maxCreatureScalingLevel = playerTarget.m_activePlayerData.MaxCreatureScalingLevel;
                        targetLevelDelta = Math.Min(maxCreatureScalingLevel > 0 ? maxCreatureScalingLevel - targetLevel : 0, playerTarget.m_activePlayerData.ScalingPlayerLevelDelta);
                    }

                    int levelWithDelta = targetLevel + targetLevelDelta;
                    int level = MathFunctions.RoundToInterval(ref levelWithDelta, scalingLevelMin, scalingLevelMax) + scalingLevelDelta;
                    return (uint)MathFunctions.RoundToInterval(ref level, 1, SharedConst.MaxLevel + 3);
                }

            }

            return base.GetLevelForTarget(target);
        }

        public string GetAIName()
        {
            return Global.ObjectMgr.GetCreatureTemplate(GetEntry()).AIName;
        }

        public string GetScriptName()
        {
            return Global.ObjectMgr.GetScriptName(GetScriptId());
        }

        public uint GetScriptId()
        {
            CreatureData creatureData = GetCreatureData();
            if (creatureData != null)
            {
                uint scriptId = creatureData.ScriptId;
                if (scriptId != 0)
                    return scriptId;
            }

            return Global.ObjectMgr.GetCreatureTemplate(GetEntry()) != null ? Global.ObjectMgr.GetCreatureTemplate(GetEntry()).ScriptID : 0;
        }

        public VendorItemData GetVendorItems()
        {
            return Global.ObjectMgr.GetNpcVendorItemList(GetEntry());
        }

        public uint GetVendorItemCurrentCount(VendorItem vItem)
        {
            if (vItem.maxcount == 0)
                return vItem.maxcount;

            VendorItemCount vCount = null;
            for (var i = 0; i < m_vendorItemCounts.Count; i++)
            {
                vCount = m_vendorItemCounts[i];
                if (vCount.itemId == vItem.item)
                    break;
            }

            if (vCount == null)
                return vItem.maxcount;

            long ptime = GameTime.GetGameTime();

            if (vCount.lastIncrementTime + vItem.incrtime <= ptime)
            {
                ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(vItem.item);

                uint diff = (uint)((ptime - vCount.lastIncrementTime) / vItem.incrtime);
                if ((vCount.count + diff * pProto.GetBuyCount()) >= vItem.maxcount)
                {
                    m_vendorItemCounts.Remove(vCount);
                    return vItem.maxcount;
                }

                vCount.count += diff * pProto.GetBuyCount();
                vCount.lastIncrementTime = ptime;
            }

            return vCount.count;
        }

        public uint UpdateVendorItemCurrentCount(VendorItem vItem, uint used_count)
        {
            if (vItem.maxcount == 0)
                return 0;

            VendorItemCount vCount = null;
            for (var i = 0; i < m_vendorItemCounts.Count; i++)
            {
                vCount = m_vendorItemCounts[i];
                if (vCount.itemId == vItem.item)
                    break;
            }

            if (vCount == null)
            {
                uint new_count = vItem.maxcount > used_count ? vItem.maxcount - used_count : 0;
                m_vendorItemCounts.Add(new VendorItemCount(vItem.item, new_count));
                return new_count;
            }

            long ptime = GameTime.GetGameTime();

            if (vCount.lastIncrementTime + vItem.incrtime <= ptime)
            {
                ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(vItem.item);

                uint diff = (uint)((ptime - vCount.lastIncrementTime) / vItem.incrtime);
                if ((vCount.count + diff * pProto.GetBuyCount()) < vItem.maxcount)
                    vCount.count += diff * pProto.GetBuyCount();
                else
                    vCount.count = vItem.maxcount;
            }

            vCount.count = vCount.count > used_count ? vCount.count - used_count : 0;
            vCount.lastIncrementTime = ptime;
            return vCount.count;
        }

        public override string GetName(Locale locale = Locale.enUS)
        {
            if (locale != Locale.enUS)
            {
                CreatureLocale cl = Global.ObjectMgr.GetCreatureLocale(GetEntry());
                if (cl != null)
                {
                    if (cl.Name.Length > (int)locale && !cl.Name[(int)locale].IsEmpty())
                        return cl.Name[(int)locale];
                }
            }

            return base.GetName(locale);
        }

        public virtual byte GetPetAutoSpellSize() { return 4; }
        public virtual uint GetPetAutoSpellOnPos(byte pos)
        {
            if (pos >= SharedConst.MaxSpellCharm || GetCharmInfo().GetCharmSpell(pos).GetActiveState() != ActiveStates.Enabled)
                return 0;
            else
                return GetCharmInfo().GetCharmSpell(pos).GetAction();
        }

        public float GetPetChaseDistance()
        {
            float range = 0f;

            for (byte i = 0; i < GetPetAutoSpellSize(); ++i)
            {
                uint spellID = GetPetAutoSpellOnPos(i);
                if (spellID == 0)
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellID, GetMap().GetDifficultyID());
                if (spellInfo != null)
                {
                    if (spellInfo.GetRecoveryTime() == 0 && spellInfo.RangeEntry.Id != 1 /*Self*/ && spellInfo.RangeEntry.Id != 2 /*Combat Range*/ && spellInfo.GetMaxRange() > range)
                        range = spellInfo.GetMaxRange();
                }
            }

            return range;
        }

        public void SetCannotReachTarget(bool cannotReach)
        {
            if (cannotReach == m_cannotReachTarget)
                return;

            m_cannotReachTarget = cannotReach;
            m_cannotReachTimer = 0;
        }
        bool CanNotReachTarget() { return m_cannotReachTarget; }

        public float GetAggroRange(Unit target)
        {
            // Determines the aggro range for creatures (usually pets), used mainly for aggressive pet target selection.
            // Based on data from wowwiki due to lack of 3.3.5a data

            if (target != null && IsPet())
            {
                uint targetLevel = 0;

                if (target.IsTypeId(TypeId.Player))
                    targetLevel = target.GetLevelForTarget(this);
                else if (target.IsTypeId(TypeId.Unit))
                    targetLevel = target.ToCreature().GetLevelForTarget(this);

                uint myLevel = GetLevelForTarget(target);
                int levelDiff = (int)(targetLevel - myLevel);

                // The maximum Aggro Radius is capped at 45 yards (25 level difference)
                if (levelDiff < -25)
                    levelDiff = -25;

                // The base aggro radius for mob of same level
                float aggroRadius = 20;

                // Aggro Radius varies with level difference at a rate of roughly 1 yard/level
                aggroRadius -= levelDiff;

                // detect range auras
                aggroRadius += GetTotalAuraModifier(AuraType.ModDetectRange);

                // detected range auras
                aggroRadius += target.GetTotalAuraModifier(AuraType.ModDetectedRange);

                // Just in case, we don't want pets running all over the map
                if (aggroRadius > SharedConst.MaxAggroRadius)
                    aggroRadius = SharedConst.MaxAggroRadius;

                // Minimum Aggro Radius for a mob seems to be combat range (5 yards)
                //  hunter pets seem to ignore minimum aggro radius so we'll default it a little higher
                if (aggroRadius < 10)
                    aggroRadius = 10;

                return (aggroRadius);
            }

            // Default
            return 0.0f;
        }

        public Unit SelectNearestHostileUnitInAggroRange(bool useLOS = false)
        {
            // Selects nearest hostile target within creature's aggro range. Used primarily by
            //  pets set to aggressive. Will not return neutral or friendly targets
            var u_check = new NearestHostileUnitInAggroRangeCheck(this, useLOS);
            var searcher = new UnitSearcher(this, u_check);
            Cell.VisitGridObjects(this, searcher, SharedConst.MaxAggroRadius);
            return searcher.GetTarget();
        }

        public void UpdateMovementFlags()
        {
            // Do not update movement flags if creature is controlled by a player (charm/vehicle)
            if (m_playerMovingMe != null)
                return;

            // Creatures with CREATURE_FLAG_EXTRA_NO_MOVE_FLAGS_UPDATE should control MovementFlags in your own scripts
            if (GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoMoveFlagsUpdate))
                return;

            // Set the movement flags if the creature is in that mode. (Only fly if actually in air, only swim if in water, etc)
            float ground = GetFloorZ();

            bool canHover = CanHover();
            bool isInAir = (MathFunctions.fuzzyGt(GetPositionZ(), ground + (canHover ? m_unitData.HoverHeight : 0.0f) + MapConst.GroundHeightTolerance) || MathFunctions.fuzzyLt(GetPositionZ(), ground - MapConst.GroundHeightTolerance)); // Can be underground too, prevent the falling

            if (GetMovementTemplate().IsFlightAllowed() && isInAir && !IsFalling())
            {
                if (GetMovementTemplate().Flight == CreatureFlightMovementType.CanFly)
                    SetCanFly(true);
                else
                    SetDisableGravity(true);

                if (!HasAuraType(AuraType.Hover))
                    SetHover(false);
            }
            else
            {
                SetCanFly(false);
                SetDisableGravity(false);
                if (IsAlive() && (CanHover() || HasAuraType(AuraType.Hover)))
                    SetHover(true);
            }

            if (!isInAir)
                SetFall(false);

            SetSwim(GetMovementTemplate().IsSwimAllowed() && IsInWater());
        }

        public override void SetObjectScale(float scale)
        {
            base.SetObjectScale(scale);

            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelInfo(GetDisplayId());
            if (minfo != null)
            {
                SetBoundingRadius((IsPet() ? 1.0f : minfo.BoundingRadius) * scale);
                SetCombatReach((IsPet() ? SharedConst.DefaultPlayerCombatReach : minfo.CombatReach) * scale);
            }
        }

        public override void SetDisplayId(uint modelId, float displayScale = 1f)
        {
            base.SetDisplayId(modelId, displayScale);

            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelInfo(modelId);
            if (minfo != null)
            {
                SetBoundingRadius((IsPet() ? 1.0f : minfo.BoundingRadius) * GetObjectScale());
                SetCombatReach((IsPet() ? SharedConst.DefaultPlayerCombatReach : minfo.CombatReach) * GetObjectScale());
            }
        }

        public void SetDisplayFromModel(int modelIdx)
        {
            CreatureModel model = GetCreatureTemplate().GetModelByIdx(modelIdx);
            if (model != null)
                SetDisplayId(model.CreatureDisplayID, model.DisplayScale);
        }

        public override void SetTarget(ObjectGuid guid)
        {
            if (IsFocusing(null, true))
                m_suppressedTarget = guid;
            else
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Target), guid);
        }

        public void FocusTarget(Spell focusSpell, WorldObject target)
        {
            // already focused
            if (_focusSpell != null)
                return;

            // some spells shouldn't track targets
            if (focusSpell.IsFocusDisabled())
                return;

            SpellInfo spellInfo = focusSpell.GetSpellInfo();

            // don't use spell focus for vehicle spells
            if (spellInfo.HasAura(AuraType.ControlVehicle))
                return;

            if ((!target || target == this) && focusSpell.GetCastTime() == 0) // instant cast, untargeted (or self-targeted) spell doesn't need any facing updates
                return;

            // store pre-cast values for target and orientation (used to later restore)
            if (!IsFocusing(null, true))
            { // only overwrite these fields if we aren't transitioning from one spell focus to another
                m_suppressedTarget = GetTarget();
                m_suppressedOrientation = GetOrientation();
            }

            _focusSpell = focusSpell;

            // set target, then force send update packet to players if it changed to provide appropriate facing
            ObjectGuid newTarget = target ? target.GetGUID() : ObjectGuid.Empty;
            if (GetTarget() != newTarget)
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Target), newTarget);

                // here we determine if the (relatively expensive) forced update is worth it, or whether we can afford to wait until the scheduled update tick
                // only require instant update for spells that actually have a visual
                if (spellInfo.GetSpellVisual() != 0 && (focusSpell.GetCastTime() == 0 || // if the spell is instant cast
                   spellInfo.HasAttribute(SpellAttr5.DontTurnDuringCast))) // client gets confused if we attempt to turn at the regularly scheduled update packet
                {
                    List<Unit> playersNearby = GetPlayerListInGrid(GetVisibilityRange());
                    foreach (Player player in playersNearby)
                    {
                        // only update players that are known to the client (have already been created)
                        if (player.HaveAtClient(this))
                            SendUpdateToPlayer(player);
                    }
                }
            }

            bool noTurnDuringCast = spellInfo.HasAttribute(SpellAttr5.DontTurnDuringCast);

            if (!HasUnitFlag2(UnitFlags2.DisableTurn))
            {
                // Face the target - we need to do this before the unit state is modified for no-turn spells
                if (target)
                    SetFacingToObject(target, false);
                else if (!noTurnDuringCast)
                {
                    Unit victim = GetVictim();
                    if (victim)
                        SetFacingToObject(victim, false); // ensure server-side orientation is correct at beginning of cast
                }
            }

            if (!noTurnDuringCast)
                AddUnitState(UnitState.Focusing);
        }

        public override bool IsFocusing(Spell focusSpell = null, bool withDelay = false)
        {
            if (!IsAlive()) // dead creatures cannot focus
            {
                ReleaseFocus(null, false);
                return false;
            }

            if (focusSpell && (focusSpell != _focusSpell))
                return false;

            if (!_focusSpell)
            {
                if (!withDelay || _focusDelay == 0)
                    return false;
                if (Time.GetMSTimeDiffToNow(_focusDelay) > 1000) // @todo figure out if we can get rid of this magic number somehow
                {
                    _focusDelay = 0; // save checks in the future
                    return false;
                }
            }

            return true;
        }

        public void ReleaseFocus(Spell focusSpell = null, bool withDelay = true)
        {
            if (_focusSpell == null)
                return;

            // focused to something else
            if (focusSpell && focusSpell != _focusSpell)
                return;

            if (IsPet() && !HasUnitFlag2(UnitFlags2.DisableTurn))// player pets do not use delay system
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Target), m_suppressedTarget);
                if (!m_suppressedTarget.IsEmpty())
                {
                    WorldObject objTarget = Global.ObjAccessor.GetWorldObject(this, m_suppressedTarget);
                    if (objTarget)
                        SetFacingToObject(objTarget, false);
                }
                else
                    SetFacingTo(m_suppressedOrientation, false);
            }
            else
                // tell the creature that it should reacquire its actual target after the delay expires (this is handled in ::Update)
                // player pets don't need to do this, as they automatically reacquire their target on focus release
                MustReacquireTarget();

            if (_focusSpell.GetSpellInfo().HasAttribute(SpellAttr5.DontTurnDuringCast))
                ClearUnitState(UnitState.Focusing);

            _focusSpell = null;
            _focusDelay = (!IsPet() && withDelay) ? GameTime.GetGameTimeMS() : 0; // don't allow re-target right away to prevent visual bugs
        }

        public void MustReacquireTarget() { m_shouldReacquireTarget = true; } // flags the Creature for forced (client displayed) target reacquisition in the next Update call

        public void DoNotReacquireTarget()
        {
            m_shouldReacquireTarget = false;
            m_suppressedTarget = ObjectGuid.Empty;
            m_suppressedOrientation = 0.0f;
        }

        public ulong GetSpawnId() { return m_spawnId; }

        public void SetCorpseDelay(uint delay) { m_corpseDelay = delay; }
        public uint GetCorpseDelay() { return m_corpseDelay; }
        public bool IsRacialLeader() { return GetCreatureTemplate().RacialLeader; }
        public bool IsCivilian()
        {
            return GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Civilian);
        }
        public bool IsTrigger()
        {
            return GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Trigger);
        }
        public bool IsGuard()
        {
            return GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Guard);
        }

        public bool CanWalk() { return GetMovementTemplate().IsGroundAllowed(); }
        public override bool CanSwim() { return GetMovementTemplate().IsSwimAllowed() || IsPet();}
        public override bool CanFly()  { return GetMovementTemplate().IsFlightAllowed(); }
        bool CanHover() { return GetMovementTemplate().Ground == CreatureGroundMovementType.Hover; }
        
        public bool IsDungeonBoss() { return (GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.DungeonBoss)); }
        public override bool IsAffectedByDiminishingReturns() { return base.IsAffectedByDiminishingReturns() || GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.AllDiminish); }

        public void SetReactState(ReactStates st)
        {
            reactState = st;
        }
        public ReactStates GetReactState()
        {
            return reactState;
        }
        public bool HasReactState(ReactStates state)
        {
            return (reactState == state);
        }

        public override void SetImmuneToAll(bool apply) { SetImmuneToAll(apply, HasReactState(ReactStates.Passive)); }
        public override void SetImmuneToPC(bool apply) { SetImmuneToPC(apply, HasReactState(ReactStates.Passive)); }
        public override void SetImmuneToNPC(bool apply) { SetImmuneToNPC(apply, HasReactState(ReactStates.Passive)); }

        public bool IsInEvadeMode() { return HasUnitState(UnitState.Evade); }
        public bool IsEvadingAttacks() { return IsInEvadeMode() || CanNotReachTarget(); }

        public override CreatureAI GetAI()
        {
            return (CreatureAI)i_AI;
        }

        public T GetAI<T>() where T : CreatureAI
        {
            return (T)i_AI;
        }

        public override SpellSchoolMask GetMeleeDamageSchoolMask(WeaponAttackType attackType = WeaponAttackType.BaseAttack) { return m_meleeDamageSchoolMask; }
        public void SetMeleeDamageSchool(SpellSchools school) { m_meleeDamageSchoolMask = (SpellSchoolMask)(1 << (int)school); }

        public sbyte GetOriginalEquipmentId() { return m_originalEquipmentId; }
        public byte GetCurrentEquipmentId() { return m_equipmentId; }
        public void SetCurrentEquipmentId(byte id) { m_equipmentId = id; }

        public CreatureTemplate GetCreatureTemplate() { return m_creatureInfo; }
        public CreatureData GetCreatureData() { return m_creatureData; }

        public override bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate)
        {
            if (!allowDuplicate)
            {
                // If an alive instance of this spawnId is already found, skip creation
                // If only dead instance(s) exist, despawn them and spawn a new (maybe also dead) version
                var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(spawnId);
                List<Creature> despawnList = new();

                foreach (var creature in creatureBounds)
                {
                    if (creature.IsAlive())
                    {
                        Log.outDebug(LogFilter.Maps, "Would have spawned {0} but {1} already exists", spawnId, creature.GetGUID().ToString());
                        return false;
                    }
                    else
                    {
                        despawnList.Add(creature);
                        Log.outDebug(LogFilter.Maps, "Despawned dead instance of spawn {0} ({1})", spawnId, creature.GetGUID().ToString());
                    }
                }

                foreach (Creature despawnCreature in despawnList)
                {
                    despawnCreature.AddObjectToRemoveList();
                }
            }

            CreatureData data = Global.ObjectMgr.GetCreatureData(spawnId);
            if (data == null)
            {
                Log.outError(LogFilter.Sql, $"Creature (SpawnID: {spawnId}) not found in table `creature`, can't load.");
                return false;
            }

            m_spawnId = spawnId;
            m_respawnCompatibilityMode = data.spawnGroupData.flags.HasAnyFlag(SpawnGroupFlags.CompatibilityMode);
            m_creatureData = data;
            m_respawnradius = data.spawndist;
            m_respawnDelay = (uint)data.spawntimesecs;

            if (!Create(map.GenerateLowGuid(HighGuid.Creature), map, data.Id, data.spawnPoint, data, 0, !m_respawnCompatibilityMode))
                return false;

            //We should set first home position, because then AI calls home movement
            SetHomePosition(this);

            m_deathState = DeathState.Alive;

            m_respawnTime = GetMap().GetCreatureRespawnTime(m_spawnId);

            if (m_respawnTime == 0 && !map.IsSpawnGroupActive(data.spawnGroupData.groupId))
            {
                // @todo pools need fixing! this is just a temporary crashfix, but they violate dynspawn principles
                Cypher.Assert(m_respawnCompatibilityMode || Global.PoolMgr.IsPartOfAPool<Creature>(spawnId) != 0, $"Creature (SpawnID {spawnId}) trying to load in inactive spawn group {data.spawnGroupData.name}.");
                m_respawnTime = GameTime.GetGameTime() + RandomHelper.URand(4, 7);
            }

            if (m_respawnTime != 0)                          // respawn on UpdateLoadCreatureFromDB
            {
                Cypher.Assert(m_respawnCompatibilityMode || Global.PoolMgr.IsPartOfAPool<Creature>(spawnId) != 0, $"Creature (SpawnID {spawnId}) trying to load despite a respawn timer in progress.");
                m_deathState = DeathState.Dead;
                if (CanFly())
                {
                    float tz = map.GetHeight(GetPhaseShift(), data.spawnPoint, true, MapConst.MaxFallDistance);
                    if (data.spawnPoint.GetPositionZ() - tz > 0.1f && GridDefines.IsValidMapCoord(tz))
                        Relocate(data.spawnPoint.GetPositionX(), data.spawnPoint.GetPositionY(), tz);
                }
            }

            SetSpawnHealth();

            SelectWildBattlePetLevel();

            // checked at creature_template loading
            DefaultMovementType = (MovementGeneratorType)data.movementType;

            loot.SetGUID(ObjectGuid.Create(HighGuid.LootObject, GetMapId(), data.Id, GetMap().GenerateLowGuid(HighGuid.LootObject)));

            if (addToMap && !GetMap().AddToMap(this))
                return false;
            return true;
        }

        public bool HasLootRecipient() { return !m_lootRecipient.IsEmpty() || !m_lootRecipientGroup.IsEmpty(); }

        public LootModes GetLootMode() { return m_LootMode; }
        public bool HasLootMode(LootModes lootMode) { return Convert.ToBoolean(m_LootMode & lootMode); }
        public void SetLootMode(LootModes lootMode) { m_LootMode = lootMode; }
        public void AddLootMode(LootModes lootMode) { m_LootMode |= lootMode; }
        public void RemoveLootMode(LootModes lootMode) { m_LootMode &= ~lootMode; }
        public void ResetLootMode() { m_LootMode = LootModes.Default; }

        public void SetNoCallAssistance(bool val) { m_AlreadyCallAssistance = val; }
        public void SetNoSearchAssistance(bool val) { m_AlreadySearchedAssistance = val; }
        public bool HasSearchedAssistance() { return m_AlreadySearchedAssistance; }

        public override MovementGeneratorType GetDefaultMovementType() { return DefaultMovementType; }
        public void SetDefaultMovementType(MovementGeneratorType mgt) { DefaultMovementType = mgt; }

        public long GetRespawnTime() { return m_respawnTime; }
        public void SetRespawnTime(uint respawn) { m_respawnTime = respawn != 0 ? GameTime.GetGameTime() + respawn : 0; }

        public uint GetRespawnDelay() { return m_respawnDelay; }
        public void SetRespawnDelay(uint delay) { m_respawnDelay = delay; }

        public float GetRespawnRadius() { return m_respawnradius; }
        public void SetRespawnRadius(float dist) { m_respawnradius = dist; }

        public void DoImmediateBoundaryCheck() { m_boundaryCheckTime = 0; }
        uint GetCombatPulseDelay() { return m_combatPulseDelay; }
        public void SetCombatPulseDelay(uint delay) // (secs) interval at which the creature pulses the entire zone into combat (only works in dungeons)
        {
            m_combatPulseDelay = delay;
            if (m_combatPulseTime == 0 || m_combatPulseTime > delay)
                m_combatPulseTime = delay;
        }

        public bool CanRegenerateHealth() { return !_regenerateHealthLock && _regenerateHealth; }
        public void SetRegenerateHealth(bool value) { _regenerateHealthLock = !value; }

        public void SetHomePosition(float x, float y, float z, float o)
        {
            m_homePosition.Relocate(x, y, z, o);
        }
        public void SetHomePosition(Position pos)
        {
            m_homePosition.Relocate(pos);
        }
        public void GetHomePosition(out float x, out float y, out float z, out float ori)
        {
            m_homePosition.GetPosition(out x, out y, out z, out ori);
        }
        public Position GetHomePosition()
        {
            return m_homePosition;
        }

        public void SetTransportHomePosition(float x, float y, float z, float o) { m_transportHomePosition.Relocate(x, y, z, o); }
        public void SetTransportHomePosition(Position pos) { m_transportHomePosition.Relocate(pos); }
        public void GetTransportHomePosition(out float x, out float y, out float z, out float ori) { m_transportHomePosition.GetPosition(out x, out y, out z, out ori); }
        public Position GetTransportHomePosition() { return m_transportHomePosition; }

        public uint GetWaypointPath() { return _waypointPathId; }
        public void LoadPath(uint pathid) { _waypointPathId = pathid; }

        public (uint nodeId, uint pathId) GetCurrentWaypointInfo() { return _currentWaypointNodeInfo; }
        public void UpdateCurrentWaypointInfo(uint nodeId, uint pathId) { _currentWaypointNodeInfo = (nodeId, pathId); }

        public CreatureGroup GetFormation() { return m_formation; }
        public void SetFormation(CreatureGroup formation) { m_formation = formation; }

        void SetDisableReputationGain(bool disable) { DisableReputationGain = disable; }
        public bool IsReputationGainDisabled() { return DisableReputationGain; }
        public bool IsDamageEnoughForLootingAndReward() { return m_creatureInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoPlayerDamageReq) || m_PlayerDamageReq == 0; }

        // Part of Evade mechanics
        long GetLastDamagedTime() { return _lastDamagedTime; }
        public void SetLastDamagedTime(long val) { _lastDamagedTime = val; }

        public void ResetPlayerDamageReq() { m_PlayerDamageReq = (uint)(GetHealth() / 2); }

        public uint GetOriginalEntry()
        {
            return m_originalEntry;
        }
        void SetOriginalEntry(uint entry)
        {
            m_originalEntry = entry;
        }

        // There's many places not ready for dynamic spawns. This allows them to live on for now.
        void SetRespawnCompatibilityMode(bool mode = true) { m_respawnCompatibilityMode = mode; }
        public bool GetRespawnCompatibilityMode() { return m_respawnCompatibilityMode; }
    }

    public class VendorItemCount
    {
        public VendorItemCount(uint _item, uint _count)
        {
            itemId = _item;
            count = _count;
            lastIncrementTime = GameTime.GetGameTime();
        }
        public uint itemId;
        public uint count;
        public long lastIncrementTime;
    }

    public class AssistDelayEvent : BasicEvent
    {
        AssistDelayEvent() { }
        public AssistDelayEvent(ObjectGuid victim, Unit owner)
        {
            m_victim = victim;
            m_owner = owner;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            Unit victim = Global.ObjAccessor.GetUnit(m_owner, m_victim);
            if (victim != null)
            {
                while (!m_assistants.Empty())
                {
                    Creature assistant = m_owner.GetMap().GetCreature(m_assistants[0]);
                    m_assistants.RemoveAt(0);

                    if (assistant != null && assistant.CanAssistTo(m_owner, victim))
                    {
                        assistant.SetNoCallAssistance(true);
                        assistant.EngageWithTarget(victim);
                    }
                }
            }
            return true;
        }
        public void AddAssistant(ObjectGuid guid) { m_assistants.Add(guid); }


        ObjectGuid m_victim;
        List<ObjectGuid> m_assistants = new();
        Unit m_owner;
    }

    public class ForcedDespawnDelayEvent : BasicEvent
    {
        public ForcedDespawnDelayEvent(Creature owner, TimeSpan respawnTimer = default)
        {
            m_owner = owner;
            m_respawnTimer = respawnTimer;
        }
        public override bool Execute(ulong e_time, uint p_time)
        {
            m_owner.DespawnOrUnsummon(0, m_respawnTimer);    // since we are here, we are not TempSummon as object type cannot change during runtime
            return true;
        }

        Creature m_owner;
        TimeSpan m_respawnTimer;
    }
}
