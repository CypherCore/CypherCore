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

        public override void Dispose()
        {
            i_AI = null;

            base.Dispose();
        }

        public override void AddToWorld()
        {
            // Register the creature for guid lookup
            if (!IsInWorld)
            {
                if (m_zoneScript != null)
                    m_zoneScript.OnCreatureCreate(this);

                GetMap().GetObjectsStore().Add(GetGUID(), this);
                if (m_spawnId != 0)
                    GetMap().GetCreatureBySpawnIdStore().Add(m_spawnId, this);

                base.AddToWorld();
                SearchFormation();
                InitializeAI();
                if (IsVehicle())
                    GetVehicleKit().Install();
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
            if (GetMotionMaster().GetMotionSlotType(MovementSlot.Active) == MovementGeneratorType.Home)
                return true;

            return false;
        }
        
        public void SearchFormation()
        {
            if (IsSummon())
                return;

            var lowguid = GetSpawnId();
            if (lowguid == 0)
                return;

            var frmdata = FormationMgr.CreatureGroupMap.LookupByKey(lowguid);
            if (frmdata != null)
                FormationMgr.AddCreatureToGroup(frmdata.leaderGUID, this);
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
                m_corpseRemoveTime = Time.UnixTime;
                SetDeathState(DeathState.Dead);
                RemoveAllAuras();
                //DestroyForNearbyPlayers(); // old UpdateObjectVisibility()
                loot.Clear();
                var respawnDelay = m_respawnDelay;
                if (IsAIEnabled)
                    GetAI().CorpseRemoved(respawnDelay);

                if (destroyForNearbyPlayers)
                    DestroyForNearbyPlayers();

                // Should get removed later, just keep "compatibility" with scripts
                if (setSpawnTime)
                    m_respawnTime = Math.Max(Time.UnixTime + respawnDelay, m_respawnTime);

                // if corpse was removed during falling, the falling will continue and override relocation to respawn position
                if (IsFalling())
                    StopMoving();

                float x, y, z, o;
                GetRespawnPosition(out x, out y, out z, out o);

                // We were spawned on transport, calculate real position
                if (IsSpawnedOnTransport())
                {
                    var pos = m_movementInfo.transport.pos;
                    pos.posX = x;
                    pos.posY = y;
                    pos.posZ = z;
                    pos.SetOrientation(o);

                    var transport = GetDirectTransport();
                    if (transport != null)
                        transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                }

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
                    var respawnDelay = m_respawnDelay;
                    m_respawnTime = Math.Max(Time.UnixTime + respawnDelay, m_respawnTime);

                    SaveRespawnTime(0, false);
                }

                var summon = ToTempSummon();
                if (summon != null)
                    summon.UnSummon();
                else
                    AddObjectToRemoveList();
            }
        }

        public bool InitEntry(uint entry, CreatureData data = null)
        {
            var normalInfo = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (normalInfo == null)
            {
                Log.outError(LogFilter.Sql, "Creature.InitEntry creature entry {0} does not exist.", entry);
                return false;
            }

            // get difficulty 1 mode entry
            CreatureTemplate cInfo = null;
            var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(GetMap().GetDifficultyID());
            while (cInfo == null && difficultyEntry != null)
            {
                var idx = CreatureTemplate.DifficultyIDToDifficultyEntryIndex(difficultyEntry.Id);
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

            var model = ObjectManager.ChooseDisplayId(cInfo, data);
            var minfo = Global.ObjectMgr.GetCreatureModelRandomGender(ref model, cInfo);
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

            var cInfo = GetCreatureTemplate();

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
                var owner = GetCharmerOrOwnerPlayerOrPlayerItself();
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

            if (cInfo.InhabitType.HasAnyFlag(InhabitType.Root))
                SetControlled(true, UnitState.Root);

            UpdateMovementFlags();
            LoadCreaturesAddon();
            LoadMechanicTemplateImmunity();
            return true;
        }

        public override void Update(uint diff)
        {
            if (IsAIEnabled && triggerJustAppeared && m_deathState == DeathState.Alive)
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
                    Log.outError(LogFilter.Unit, "Creature ({0}) in wrong state: {1}", GetGUID().ToString(), m_deathState);
                    break;
                case DeathState.Dead:
                    {
                        var now = Time.UnixTime;
                        if (m_respawnTime <= now)
                        {
                            // First check if there are any scripts that object to us respawning
                            if (!Global.ScriptMgr.CanSpawn(GetSpawnId(), GetEntry(), GetCreatureData(), GetMap()))
                            {
                                m_respawnTime = now + RandomHelper.URand(4, 7);
                                break; // Will be rechecked on next Update call after delay expires
                            }

                            var dbtableHighGuid = ObjectGuid.Create(HighGuid.Creature, GetMapId(), GetEntry(), m_spawnId);
                            var linkedRespawnTime = GetMap().GetLinkedRespawnTime(dbtableHighGuid);
                            if (linkedRespawnTime == 0)             // Can respawn
                                Respawn();
                            else                                // the master is dead
                            {
                                var targetGuid = Global.ObjectMgr.GetLinkedRespawnGuid(dbtableHighGuid);
                                if (targetGuid == dbtableHighGuid) // if linking self, never respawn (check delayed to next day)
                                    SetRespawnTime(Time.Week);
                                else
                                {
                                    // else copy time from master and add a little
                                    var baseRespawnTime = Math.Max(linkedRespawnTime, now);
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
                            var group = Global.GroupMgr.GetGroupByGUID(lootingGroupLowGUID);
                            if (group)
                                group.EndRoll(loot, GetMap());

                            m_groupLootTimer = 0;
                            lootingGroupLowGUID.Clear();
                        }
                        else m_groupLootTimer -= diff;
                    }
                    else if (m_corpseRemoveTime <= Time.UnixTime)
                    {
                        RemoveCorpse(false);
                        Log.outDebug(LogFilter.Unit, "Removing corpse... {0} ", GetEntry());
                    }
                    break;
                case DeathState.Alive:
                    base.Update(diff);

                    if (!IsAlive())
                        break;

                    if (m_shouldReacquireTarget && !IsFocusing(null, true))
                    {
                        SetTarget(m_suppressedTarget);
                        if (!m_suppressedTarget.IsEmpty())
                        {
                            var objTarget = Global.ObjAccessor.GetWorldObject(this, m_suppressedTarget);
                            if (objTarget)
                                SetFacingToObject(objTarget);
                        }
                        else
                            SetFacingTo(m_suppressedOrientation);
                        m_shouldReacquireTarget = false;
                    }

                    // if creature is charmed, switch to charmed AI (and back)
                    if (NeedChangeAI)
                    {
                        UpdateCharmAI();
                        NeedChangeAI = false;
                        IsAIEnabled = true;
                        if (!IsInEvadeMode() && !LastCharmerGUID.IsEmpty())
                        {
                            var charmer = Global.ObjAccessor.GetUnit(this, LastCharmerGUID);
                            if (charmer)
                                if (CanStartAttack(charmer, true))
                                    i_AI.AttackStart(charmer);
                        }

                        LastCharmerGUID.Clear();
                    }

                    // periodic check to see if the creature has passed an evade boundary
                    if (IsAIEnabled && !IsInEvadeMode() && IsEngaged())
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

                    if (!IsInEvadeMode() && IsAIEnabled)
                    {
                        // do not allow the AI to be changed during update
                        m_AI_locked = true;
                        i_AI.UpdateAI(diff);
                        m_AI_locked = false;
                    }

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
                        var bInCombat = IsInCombat() && (!GetVictim() ||                                        // if IsInCombat() is true and this has no victim
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
                            if (IsAIEnabled)
                                GetAI().EnterEvadeMode(EvadeReason.NoPath);
                    }
                    break;
            }
        }

        public void Regenerate(PowerType power)
        {
            var curValue = GetPower(power);
            var maxValue = GetMaxPower(power);

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
                            var ManaIncreaseRate = WorldConfig.GetFloatValue(WorldCfg.RatePowerMana);
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

            var curValue = GetHealth();
            var maxValue = GetMaxHealth();

            if (curValue >= maxValue)
                return;

            long addvalue;

            // Not only pet, but any controlled creature (and not polymorphed)
            if (!GetCharmerOrOwnerGUID().IsEmpty() && !IsPolymorphed())
            {
                var HealthIncreaseRate = WorldConfig.GetFloatValue(WorldCfg.RateHealth);
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

            var radius = WorldConfig.GetFloatValue(WorldCfg.CreatureFamilyFleeAssistanceRadius);
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

        bool AIDestory()
        {
            if (m_AI_locked)
            {
                Log.outDebug(LogFilter.Scripts, "AIM_Destroy: failed to destroy, locked.");
                return false;
            }

            Cypher.Assert(i_disabledAI == null, "The disabled AI wasn't cleared!");

            i_AI = null;

            IsAIEnabled = false;
            return true;
        }

        public bool InitializeAI(CreatureAI ai = null)
        {
            // make sure nothing can change the AI during AI update
            if (m_AI_locked)
            {
                Log.outDebug(LogFilter.Scripts, "InitializeAI: failed to init, locked.");
                return false;
            }

            AIDestory();

            InitializeMovementAI();

            i_AI = ai ?? AISelector.SelectAI(this);

            IsAIEnabled = true;
            i_AI.InitializeAI();
            // Initialize vehicle
            if (GetVehicleKit() != null)
                GetVehicleKit().Reset();

            return true;
        }

        void InitializeMovementAI()
        {
            if (m_formation == null)
                GetMotionMaster().Initialize();
            else if (m_formation.GetLeader() == this)
            {
                m_formation.FormationReset(false);
                GetMotionMaster().Initialize();
            }
            else if (m_formation.IsFormed())
                GetMotionMaster().MoveIdle(); //wait the order of leader
            else
                GetMotionMaster().Initialize();
        }

        public static Creature CreateCreature(uint entry, Map map, Position pos, uint vehId = 0)
        {
            var cInfo = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (cInfo == null)
                return null;

            ulong lowGuid;
            if (vehId != 0 || cInfo.VehicleId != 0)
                lowGuid = map.GenerateLowGuid(HighGuid.Vehicle);
            else
                lowGuid = map.GenerateLowGuid(HighGuid.Creature);

            var creature = new Creature();
            if (!creature.Create(lowGuid, map, entry, pos, null, vehId))
                return null;

            return creature;
        }

        public static Creature CreateCreatureFromDB(ulong spawnId, Map map, bool addToMap = true, bool allowDuplicate = false)
        {
            var creature = new Creature();
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

            var cinfo = Global.ObjectMgr.GetCreatureTemplate(entry);
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
            if (HasUnitMovementFlag(MovementFlag.Hover))
            {
                //! Relocate again with updated Z coord
                posZ += m_unitData.HoverHeight;
            }

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

            return true;
        }

        void InitializeReactState()
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

            var bgTypeId = Global.BattlegroundMgr.GetBattleMasterBG(GetEntry());
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

        public bool IsEscortNPC(bool onlyIfActive = true)
        {
            if (!IsAIEnabled)
                return false;

            return GetAI().IsEscortNPC(onlyIfActive);
        }

        public override bool IsMovementPreventedByCasting()
        {
            // first check if currently a movement allowed channel is active and we're not casting
            var spell = GetCurrentSpell(CurrentSpellTypes.Channeled);
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
            _pickpocketLootRestore = Time.UnixTime + WorldConfig.GetIntValue(WorldCfg.CreaturePickpocketRefill);
        }
        public void ResetPickPocketRefillTimer() { _pickpocketLootRestore = 0; }
        public bool CanGeneratePickPocketLoot() { return _pickpocketLootRestore <= Time.UnixTime; }
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

            var player = unit.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (player == null)                                             // normal creature, no player involved
                return;

            m_lootRecipient = player.GetGUID();
            if (withGroup)
            {
                var group = player.GetGroup();
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

            var playerGroup = player.GetGroup();
            if (!playerGroup || playerGroup != GetLootRecipientGroup()) // if we dont have a group we arent the recipient
                return false;                                           // if creature doesnt have group bound it means it was solo killed by someone else

            return true;
        }

        public void SaveToDB()
        {
            // this should only be used when the creature has already been loaded
            // preferably after adding to map, because mapid may not be valid otherwise
            var data = Global.ObjectMgr.GetCreatureData(m_spawnId);
            if (data == null)
            {
                Log.outError(LogFilter.Unit, "Creature.SaveToDB failed, cannot get creature data!");
                return;
            }

            var mapId = GetTransport() ? (uint)GetTransport().GetGoInfo().MoTransport.SpawnMap : GetMapId();
            SaveToDB(mapId, data.spawnDifficulties);
        }

        public virtual void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties)
        {
            // update in loaded data
            if (m_spawnId == 0)
                m_spawnId = Global.ObjectMgr.GenerateCreatureSpawnId();

            var data = Global.ObjectMgr.NewOrExistCreatureData(m_spawnId);

            var displayId = GetNativeDisplayId();
            var npcflag = ((ulong)m_unitData.NpcFlags[1] << 32) | m_unitData.NpcFlags[0];
            uint unitFlags = m_unitData.Flags;
            uint unitFlags2 = m_unitData.Flags2;
            uint unitFlags3 = m_unitData.Flags3;
            var dynamicflags = (UnitDynFlags)(uint)m_objectData.DynamicFlags;

            // check if it's a custom model and if not, use 0 for displayId
            var cinfo = GetCreatureTemplate();
            if (cinfo != null)
            {
                foreach (var model in cinfo.Models)
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
            var trans = new SQLTransaction();

            var stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_CREATURE);
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
            stmt.AddValue(index++, GetDefaultMovementType());
            stmt.AddValue(index++, npcflag);
            stmt.AddValue(index++, unitFlags);
            stmt.AddValue(index++, unitFlags2);
            stmt.AddValue(index++, unitFlags3);
            stmt.AddValue(index++, dynamicflags);
            trans.Append(stmt);

            DB.World.CommitTransaction(trans);
        }

        public void SelectLevel()
        {
            var cInfo = GetCreatureTemplate();

            // level
            var levels = cInfo.GetMinMaxLevel();
            var minlevel = Math.Min(levels[0], levels[1]);
            var maxlevel = Math.Max(levels[0], levels[1]);
            var level = (minlevel == maxlevel ? minlevel : RandomHelper.IRand(minlevel, maxlevel));
            SetLevel((uint)level);

            var scaling = cInfo.GetLevelScaling(GetMap().GetDifficultyID());

            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ScalingLevelMin), scaling.MinLevel);
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ScalingLevelMax), scaling.MaxLevel);

            int mindelta = Math.Min(scaling.DeltaLevelMax, scaling.DeltaLevelMin);
            int maxdelta = Math.Max(scaling.DeltaLevelMax, scaling.DeltaLevelMin);
            var delta = mindelta == maxdelta ? mindelta : RandomHelper.IRand(mindelta, maxdelta);

            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ScalingLevelDelta), delta);
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ContentTuningID), scaling.ContentTuningID);

            UpdateLevelDependantStats();
        }

        void UpdateLevelDependantStats()
        {
            var cInfo = GetCreatureTemplate();
            var rank = IsPet() ? 0 : cInfo.Rank;
            var level = GetLevel();
            var stats = Global.ObjectMgr.GetCreatureBaseStats(level, cInfo.UnitClass);

            // health
            var healthmod = _GetHealthMod(rank);

            var basehp = (uint)GetMaxHealthByLevel(level);
            var health = (uint)(basehp * healthmod);

            SetCreateHealth(health);
            SetMaxHealth(health);
            SetHealth(health);
            ResetPlayerDamageReq();

            // mana
            var mana = stats.GenerateMana(cInfo);
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
            var basedamage = GetBaseDamageForLevel(level);
            var weaponBaseMinDamage = basedamage;
            var weaponBaseMaxDamage = basedamage * 1.5f;

            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

            SetBaseWeaponDamage(WeaponAttackType.OffAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
            SetBaseWeaponDamage(WeaponAttackType.OffAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

            SetBaseWeaponDamage(WeaponAttackType.RangedAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
            SetBaseWeaponDamage(WeaponAttackType.RangedAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

            SetStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Base, stats.AttackPower);
            SetStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Base, stats.RangedAttackPower);

            var armor = GetBaseArmorForLevel(level); /// @todo Why is this treated as uint32 when it's a float?
            SetStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, armor);
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

            var cinfo = Global.ObjectMgr.GetCreatureTemplate(entry);
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

            var einfo = Global.ObjectMgr.GetEquipmentInfo(GetEntry(), id);
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

            var trans = new SQLTransaction();

            var stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_CREATURE);
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

            if (IsAlive() || m_corpseRemoveTime > Time.UnixTime)
                return false;

            return true;
        }

        public override bool CanAlwaysSee(WorldObject obj)
        {
            if (IsAIEnabled && GetAI<CreatureAI>().CanSeeAlways(obj))
                return true;

            return false;
        }

        public bool CanStartAttack(Unit who, bool force)
        {
            if (IsCivilian())
                return false;

            // This set of checks is should be done only for creatures
            if ((HasUnitFlag(UnitFlags.ImmuneToNpc) && !who.IsTypeId(TypeId.Player))                                // flag is valid only for non player characters
                || (HasUnitFlag(UnitFlags.ImmuneToPc) && who.IsTypeId(TypeId.Player))                               // immune to PC and target is a player, return false
                || (who.GetOwner() && who.GetOwner().IsTypeId(TypeId.Player) && HasUnitFlag(UnitFlags.ImmuneToPc))) // player pets are immune to pc as well
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

                if (who.IsEngaged() && IsWithinDist(who, SharedConst.AttackDistance))
                {
                    var victim = who.GetAttackerForHelper();
                    if (victim != null)
                        if (IsWithinDistInMap(victim, WorldConfig.GetFloatValue(WorldCfg.CreatureFamilyAssistanceRadius)))
                            force = true;
                }

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
            var maxRadius = (45.0f * WorldConfig.GetFloatValue(WorldCfg.RateCreatureAggro));
            var minRadius = (5.0f * WorldConfig.GetFloatValue(WorldCfg.RateCreatureAggro));
            var aggroRate = WorldConfig.GetFloatValue(WorldCfg.RateCreatureAggro);
            var expansionMaxLevel = (byte)Global.ObjectMgr.GetMaxLevelForExpansion((Expansion)GetCreatureTemplate().RequiredExpansion);

            var playerLevel = player.GetLevelForTarget(this);
            var creatureLevel = GetLevelForTarget(player);

            if (aggroRate == 0.0f)
                return 0.0f;

            // The aggro radius for creatures with equal level as the player is 20 yards.
            // The combatreach should not get taken into account for the distance so we drop it from the range (see Supremus as expample)
            var baseAggroDistance = 20.0f - GetCombatReach();
            var aggroRadius = baseAggroDistance;

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
                m_corpseRemoveTime = Time.UnixTime + m_corpseDelay;
                var respawnDelay = m_respawnDelay;
                var scalingMode = WorldConfig.GetUIntValue(WorldCfg.RespawnDynamicMode);
                if (scalingMode != 0)
                    GetMap().ApplyDynamicModeRespawnScaling(this, m_spawnId, ref respawnDelay, scalingMode);
                // @todo remove the boss respawn time hack in a dynspawn follow-up once we have creature groups in instances
                if (m_respawnCompatibilityMode)
                {
                    if (IsDungeonBoss() && m_respawnDelay == 0)
                        m_respawnTime = long.MaxValue; // never respawn in this instance
                    else
                        m_respawnTime = Time.UnixTime + respawnDelay + m_corpseDelay;
                }
                else
                {
                    if (IsDungeonBoss() && m_respawnDelay == 0)
                        m_respawnTime = long.MaxValue; // never respawn in this instance
                    else
                        m_respawnTime = Time.UnixTime + respawnDelay;
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

                if ((CanFly() || IsFlying()))
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
                    var creatureData = GetCreatureData();
                    var cInfo = GetCreatureTemplate();

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

                    var display = new CreatureModel(GetNativeDisplayId(), GetNativeDisplayScale(), 1.0f);
                    if (Global.ObjectMgr.GetCreatureModelRandomGender(ref display, GetCreatureTemplate()) != null)
                    {
                        SetDisplayId(display.CreatureDisplayID, display.DisplayScale);
                        SetNativeDisplayId(display.CreatureDisplayID, display.DisplayScale);
                    }

                    GetMotionMaster().InitDefault();
                    //Re-initialize reactstate that could be altered by movementgenerators
                    InitializeReactState();

                    if (IsAIEnabled) // reset the AI to be sure no dirty or uninitialized values will be used till next tick
                        GetAI().Reset();

                    triggerJustAppeared = true;

                    var poolid = GetSpawnId() != 0 ? Global.PoolMgr.IsPartOfAPool<Creature>(GetSpawnId()) : 0;
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
                var corpseDelay = GetCorpseDelay();
                var respawnDelay = GetRespawnDelay();

                // do it before killing creature
                DestroyForNearbyPlayers();

                var overrideRespawnTime = false;
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
                    var respawnDelay = m_respawnDelay;
                    var scalingMode = WorldConfig.GetUIntValue(WorldCfg.RespawnDynamicMode);
                    if (scalingMode != 0)
                        GetMap().ApplyDynamicModeRespawnScaling(this, m_spawnId, ref respawnDelay, scalingMode);
                    m_respawnTime = Time.UnixTime + respawnDelay;
                    SaveRespawnTime();
                }

                AddObjectToRemoveList();
            }
        }

        public void DespawnOrUnsummon(TimeSpan time, TimeSpan forceRespawnTimer = default) { DespawnOrUnsummon((uint)time.TotalMilliseconds, forceRespawnTimer); }

        public void DespawnOrUnsummon(uint msTimeToDespawn = 0, TimeSpan forceRespawnTimer = default)
        {
            var summon = ToTempSummon();
            if (summon != null)
                summon.UnSummon(msTimeToDespawn);
            else
                ForcedDespawn(msTimeToDespawn, forceRespawnTimer);
        }

        public void LoadMechanicTemplateImmunity()
        {
            // uint32 max used for "spell id", the immunity system will not perform SpellInfo checks against invalid spells
            // used so we know which immunities were loaded from template
            var placeholderSpellId = uint.MaxValue;

            // unapply template immunities (in case we're updating entry)
            for (uint i = 1; i < (int)Mechanics.Max; ++i)
                ApplySpellImmune(placeholderSpellId, SpellImmunity.Mechanic, i, false);

            // don't inherit immunities for hunter pets
            if (GetOwnerGUID().IsPlayer() && IsHunterPet())
                return;

            var mask = GetCreatureTemplate().MechanicImmuneMask;
            if (mask != 0)
            {
                for (uint i = 0 + 1; i < (int)Mechanics.Max; ++i)
                {
                    if ((mask & (1u << ((int)i - 1))) != 0)
                        ApplySpellImmune(placeholderSpellId, SpellImmunity.Mechanic, i, true);
                }
            }
        }

        public override bool IsImmunedToSpell(SpellInfo spellInfo, Unit caster)
        {
            if (spellInfo == null)
                return false;

            var immunedToAllEffects = true;
            foreach (var effect in spellInfo.GetEffects())
            {
                if (effect == null || !effect.IsEffect())
                    continue;

                if (!IsImmunedToSpellEffect(spellInfo, effect.EffectIndex, caster))
                {
                    immunedToAllEffects = false;
                    break;
                }
            }

            if (immunedToAllEffects)
                return true;

            return base.IsImmunedToSpell(spellInfo, caster);
        }

        public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, uint index, Unit caster)
        {
            var effect = spellInfo.GetEffect(index);
            if (effect == null)
                return true;

            if (GetCreatureTemplate().CreatureType == CreatureType.Mechanical && effect.Effect == SpellEffectName.Heal)
                return true;

            return base.IsImmunedToSpellEffect(spellInfo, index, caster);
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
            var packet = new AIReaction();

            packet.UnitGUID = GetGUID();
            packet.Reaction = reactionType;

            SendMessageToSet(packet, true);
        }

        public void CallAssistance()
        {
            if (!m_AlreadyCallAssistance && GetVictim() != null && !IsPet() && !IsCharmed())
            {
                SetNoCallAssistance(true);

                var radius = WorldConfig.GetFloatValue(WorldCfg.CreatureFamilyAssistanceRadius);

                if (radius > 0)
                {
                    var assistList = new List<Creature>();

                    var u_check = new AnyAssistCreatureInRangeCheck(this, GetVictim(), radius);
                    var searcher = new CreatureListSearcher(this, assistList, u_check);
                    Cell.VisitGridObjects(this, searcher, radius);

                    if (!assistList.Empty())
                    {
                        var e = new AssistDelayEvent(GetVictim().GetGUID(), this);
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
            if (radius <= 0.0f || GetVictim() == null || IsPet() || IsCharmed())
                return;

            var u_do = new CallOfHelpCreatureInRangeDo(this, GetVictim(), radius);
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

            if (HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.NotSelectable | UnitFlags.ImmuneToPc))
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

            var targetVictim = target.GetAttackerForHelper();

            // if I'm already fighting target, or I'm hostile towards the target, the target is acceptable
            if (IsEngagedBy(target) || IsHostileTo(target))
                return true;

            // if the target's victim is friendly, and the target is neutral, the target is acceptable
            if (targetVictim != null && IsFriendlyTo(targetVictim))
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

            var thisRespawnTime = forceDelay != 0 ? Time.UnixTime + forceDelay : m_respawnTime;
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

            if (IsAIEnabled && !GetAI().CanAIAttack(victim))
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
            var dist = Math.Min(GetMap().GetVisibilityRange(), MapConst.SizeofCells * 2);

            var unit = GetCharmerOrOwner();
            if (unit != null)
                return victim.IsWithinDist(unit, dist);
            else
            {
                // include sizes for huge npcs
                dist += GetCombatReach() + victim.GetCombatReach();

                // to prevent creatures in air ignore attacks because distance is already too high...
                if (GetCreatureTemplate().InhabitType.HasAnyFlag(InhabitType.Air))
                    return victim.IsInDist2d(m_homePosition, dist);
                else
                    return victim.IsInDist(m_homePosition, dist);
            }
        }

        CreatureAddon GetCreatureAddon()
        {
            if (m_spawnId != 0)
            {
                var addon = Global.ObjectMgr.GetCreatureAddon(m_spawnId);
                if (addon != null)
                    return addon;
            }

            // dependent from difficulty mode entry
            return Global.ObjectMgr.GetCreatureTemplateAddon(GetCreatureTemplate().Entry);
        }

        public bool LoadCreaturesAddon()
        {
            var cainfo = GetCreatureAddon();
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
                if (Convert.ToBoolean(m_unitData.AnimTier & (byte)UnitBytes1Flags.Hover) && !Convert.ToBoolean(GetCreatureTemplate().InhabitType & InhabitType.Air))
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
                    var AdditionalSpellInfo = Global.SpellMgr.GetSpellInfo(id, GetMap().GetDifficultyID());
                    if (AdditionalSpellInfo == null)
                    {
                        Log.outError(LogFilter.Sql, "Creature ({0}) has wrong spell {1} defined in `auras` field.", GetGUID().ToString(), id);
                        continue;
                    }

                    // skip already applied aura
                    if (HasAura(id))
                        continue;

                    AddAura(id, this);
                    Log.outError(LogFilter.Unit, "Spell: {0} added to creature ({1})", id, GetGUID().ToString());
                }
            }
            return true;
        }

        // Send a message to LocalDefense channel for players opposition team in the zone
        public void SendZoneUnderAttackMessage(Player attacker)
        {
            var enemy_team = attacker.GetTeam();

            var packet = new ZoneUnderAttack();
            packet.AreaID = (int)GetAreaId();
            Global.WorldMgr.SendGlobalMessage(packet, null, (enemy_team == Team.Alliance ? Team.Horde : Team.Alliance));
        }

        public void SetInCombatWithZone()
        {
            if (!CanHaveThreatList())
            {
                Log.outError(LogFilter.Unit, "Creature entry {0} call SetInCombatWithZone but creature cannot have threat list.", GetEntry());
                return;
            }

            var map = GetMap();

            if (!map.IsDungeon())
            {
                Log.outError(LogFilter.Unit, "Creature entry {0} call SetInCombatWithZone for map (id: {1}) that isn't an instance.", GetEntry(), map.GetId());
                return;
            }

            var PlList = map.GetPlayers();
            if (PlList.Empty())
                return;

            foreach (var player in PlList)
            {
                if (player.IsGameMaster())
                    continue;

                if (player.IsAlive())
                    EngageWithTarget(player);
            }
        }

        public override bool HasSpell(uint spellId)
        {
            for (byte i = 0; i < SharedConst.MaxCreatureSpells; ++i)
                if (spellId == m_spells[i])
                    return true;

            return false;
        }

        public long GetRespawnTimeEx()
        {
            var now = Time.UnixTime;
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
                var homePos = GetHomePosition();
                homePos.GetPosition(out x, out y, out z, out ori);
                dist = 0;
            }
        }

        bool IsSpawnedOnTransport() { return m_creatureData != null && m_creatureData.spawnPoint.GetMapId() != GetMapId(); }

        public void AllLootRemovedFromCorpse()
        {
            if (loot.loot_type != LootType.Skinning && !IsPet() && GetCreatureTemplate().SkinLootId != 0 && HasLootRecipient())
                if (LootStorage.Skinning.HaveLootFor(GetCreatureTemplate().SkinLootId))
                    AddUnitFlag(UnitFlags.Skinnable);

            var now = Time.UnixTime;
            // Do not reset corpse remove time if corpse is already removed
            if (m_corpseRemoveTime <= now)
                return;

            var decayRate = WorldConfig.GetFloatValue(WorldCfg.RateCorpseDecayLooted);
            // corpse skinnable, but without skinning flag, and then skinned, corpse will despawn next update
            if (loot.loot_type == LootType.Skinning)
                m_corpseRemoveTime = now;
            else
                m_corpseRemoveTime = now + (uint)(m_corpseDelay * decayRate);

            m_respawnTime = Math.Max(m_corpseRemoveTime + m_respawnDelay, m_respawnTime);
        }

        public bool HasScalableLevels()
        {
            var cinfo = GetCreatureTemplate();
            var scaling = cinfo.GetLevelScaling(GetMap().GetDifficultyID());

            return (scaling.MinLevel != 0 && scaling.MaxLevel != 0);
        }

        ulong GetMaxHealthByLevel(uint level)
        {
            var cInfo = GetCreatureTemplate();
            var scaling = cInfo.GetLevelScaling(GetMap().GetDifficultyID());
            var baseHealth = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureHealth, level, cInfo.GetHealthScalingExpansion(), scaling.ContentTuningID, (Class)cInfo.UnitClass);
            
            return (ulong)(baseHealth * cInfo.ModHealth * cInfo.ModHealthExtra);
        }

        public override float GetHealthMultiplierForTarget(WorldObject target)
        {
            if (!HasScalableLevels())
                return 1.0f;

            var levelForTarget = GetLevelForTarget(target);
            if (GetLevel() < levelForTarget)
                return 1.0f;

            return (float)GetMaxHealthByLevel(levelForTarget) / GetCreateHealth();
        }

        float GetBaseDamageForLevel(uint level)
        {
            var cInfo = GetCreatureTemplate();
            var scaling = cInfo.GetLevelScaling(GetMap().GetDifficultyID());
            return Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureAutoAttackDps, level, cInfo.GetHealthScalingExpansion(), scaling.ContentTuningID, (Class)cInfo.UnitClass);
        }

        public override float GetDamageMultiplierForTarget(WorldObject target)
        {
            if (!HasScalableLevels())
                return 1.0f;

            var levelForTarget = GetLevelForTarget(target);

            return GetBaseDamageForLevel(levelForTarget) / GetBaseDamageForLevel(GetLevel());
        }

        float GetBaseArmorForLevel(uint level)
        {
            var cInfo = GetCreatureTemplate();
            var scaling = cInfo.GetLevelScaling(GetMap().GetDifficultyID());
            var baseArmor = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureArmor, level, cInfo.GetHealthScalingExpansion(), scaling.ContentTuningID, (Class)cInfo.UnitClass);
            return baseArmor * cInfo.ModArmor;
        }

        public override float GetArmorMultiplierForTarget(WorldObject target)
        {
            if (!HasScalableLevels())
                return 1.0f;

            var levelForTarget = GetLevelForTarget(target);

            return GetBaseArmorForLevel(levelForTarget) / GetBaseArmorForLevel(GetLevel());
        }

        public override uint GetLevelForTarget(WorldObject target)
        {
            var unitTarget = target.ToUnit();
            if (unitTarget)
            {
                if (IsWorldBoss())
                {
                    var level = (int)(unitTarget.GetLevel() + WorldConfig.GetIntValue(WorldCfg.WorldBossLevelDiff));
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

                    var targetLevelDelta = 0;

                    var playerTarget = target.ToPlayer();
                    if (playerTarget != null)
                    {
                        if (scalingFactionGroup != 0 && CliDB.FactionTemplateStorage.LookupByKey(CliDB.ChrRacesStorage.LookupByKey(playerTarget.GetRace()).FactionID).FactionGroup != scalingFactionGroup)
                            scalingLevelMin = scalingLevelMax;

                        int maxCreatureScalingLevel = playerTarget.m_activePlayerData.MaxCreatureScalingLevel;
                        targetLevelDelta = Math.Min(maxCreatureScalingLevel > 0 ? maxCreatureScalingLevel - targetLevel : 0, playerTarget.m_activePlayerData.ScalingPlayerLevelDelta);
                    }

                    var levelWithDelta = targetLevel + targetLevelDelta;
                    var level = MathFunctions.RoundToInterval(ref levelWithDelta, scalingLevelMin, scalingLevelMax) + scalingLevelDelta;
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
            var creatureData = GetCreatureData();
            if (creatureData != null)
            {
                var scriptId = creatureData.ScriptId;
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

            var ptime = Time.UnixTime;

            if (vCount.lastIncrementTime + vItem.incrtime <= ptime)
            {
                var pProto = Global.ObjectMgr.GetItemTemplate(vItem.item);

                var diff = (uint)((ptime - vCount.lastIncrementTime) / vItem.incrtime);
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
                var new_count = vItem.maxcount > used_count ? vItem.maxcount - used_count : 0;
                m_vendorItemCounts.Add(new VendorItemCount(vItem.item, new_count));
                return new_count;
            }

            var ptime = Time.UnixTime;

            if (vCount.lastIncrementTime + vItem.incrtime <= ptime)
            {
                var pProto = Global.ObjectMgr.GetItemTemplate(vItem.item);

                var diff = (uint)((ptime - vCount.lastIncrementTime) / vItem.incrtime);
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
                var cl = Global.ObjectMgr.GetCreatureLocale(GetEntry());
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
            var range = 0f;

            for (byte i = 0; i < GetPetAutoSpellSize(); ++i)
            {
                var spellID = GetPetAutoSpellOnPos(i);
                if (spellID == 0)
                    continue;

                var spellInfo = Global.SpellMgr.GetSpellInfo(spellID, GetMap().GetDifficultyID());
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

                var myLevel = GetLevelForTarget(target);
                var levelDiff = (int)(targetLevel - myLevel);

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
            var ground = GetFloorZ();

            var isInAir = (MathFunctions.fuzzyGt(GetPositionZMinusOffset(), ground + 0.05f) || MathFunctions.fuzzyLt(GetPositionZMinusOffset(), ground - 0.05f)); // Can be underground too, prevent the falling

            if (GetCreatureTemplate().InhabitType.HasAnyFlag(InhabitType.Air) && isInAir && !IsFalling())
            {
                if (GetCreatureTemplate().InhabitType.HasAnyFlag(InhabitType.Ground))
                    SetCanFly(true);
                else
                    SetDisableGravity(true);
            }
            else
            {
                SetCanFly(false);
                SetDisableGravity(false);
            }

            if (!isInAir)
                SetFall(false);

            SetSwim(GetCreatureTemplate().InhabitType.HasAnyFlag(InhabitType.Water) && IsInWater());
        }

        public override void SetObjectScale(float scale)
        {
            base.SetObjectScale(scale);

            var minfo = Global.ObjectMgr.GetCreatureModelInfo(GetDisplayId());
            if (minfo != null)
            {
                SetBoundingRadius((IsPet() ? 1.0f : minfo.BoundingRadius) * scale);
                SetCombatReach((IsPet() ? SharedConst.DefaultPlayerCombatReach : minfo.CombatReach) * scale);
            }
        }

        public override void SetDisplayId(uint modelId, float displayScale = 1f)
        {
            base.SetDisplayId(modelId, displayScale);

            var minfo = Global.ObjectMgr.GetCreatureModelInfo(modelId);
            if (minfo != null)
            {
                SetBoundingRadius((IsPet() ? 1.0f : minfo.BoundingRadius) * GetObjectScale());
                SetCombatReach((IsPet() ? SharedConst.DefaultPlayerCombatReach : minfo.CombatReach) * GetObjectScale());
            }
        }

        public void SetDisplayFromModel(int modelIdx)
        {
            var model = GetCreatureTemplate().GetModelByIdx(modelIdx);
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

            var spellInfo = focusSpell.GetSpellInfo();

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
            var newTarget = target ? target.GetGUID() : ObjectGuid.Empty;
            if (GetTarget() != newTarget)
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Target), newTarget);

                // here we determine if the (relatively expensive) forced update is worth it, or whether we can afford to wait until the scheduled update tick
                // only require instant update for spells that actually have a visual
                if (spellInfo.GetSpellVisual() != 0 && (focusSpell.GetCastTime() == 0 || // if the spell is instant cast
                   spellInfo.HasAttribute(SpellAttr5.DontTurnDuringCast))) // client gets confused if we attempt to turn at the regularly scheduled update packet
                {
                    var playersNearby = GetPlayerListInGrid(GetVisibilityRange());
                    foreach (Player player in playersNearby)
                    {
                        // only update players that are known to the client (have already been created)
                        if (player.HaveAtClient(this))
                            SendUpdateToPlayer(player);
                    }
                }
            }

            var canTurnDuringCast = !focusSpell.GetSpellInfo().HasAttribute(SpellAttr5.DontTurnDuringCast);
            // Face the target - we need to do this before the unit state is modified for no-turn spells
            if (target)
                SetFacingToObject(target, false);
            else if (!canTurnDuringCast)
            {
                var victim = GetVictim();
                if (victim)
                    SetFacingToObject(victim, false); // ensure server-side orientation is correct at beginning of cast
            }

            if (!canTurnDuringCast)
                AddUnitState(UnitState.CannotTurn);
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

            if (IsPet())// player pets do not use delay system
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Target), m_suppressedTarget);
                if (!m_suppressedTarget.IsEmpty())
                {
                    var objTarget = Global.ObjAccessor.GetWorldObject(this, m_suppressedTarget);
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
                ClearUnitState(UnitState.CannotTurn);

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
        public bool CanWalk()
        {
            return GetCreatureTemplate().InhabitType.HasAnyFlag(InhabitType.Ground);
        }
        public override bool CanSwim()
        {
            return GetCreatureTemplate().InhabitType.HasAnyFlag(InhabitType.Water);
        }
        public override bool CanFly()
        {
            return GetCreatureTemplate().InhabitType.HasAnyFlag(InhabitType.Air);
        }
        public bool IsDungeonBoss() { return (GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.DungeonBoss)); }

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

        public override SpellSchoolMask GetMeleeDamageSchoolMask() { return m_meleeDamageSchoolMask; }
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
                var despawnList = new List<Creature>();

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

                foreach (var despawnCreature in despawnList)
                {
                    despawnCreature.AddObjectToRemoveList();
                }
            }

            var data = Global.ObjectMgr.GetCreatureData(spawnId);
            if (data == null)
            {
                Log.outError(LogFilter.Sql, "Creature (GUID: {0}) not found in table `creature`, can't load. ", spawnId);
                return false;
            }

            m_spawnId = spawnId;
            m_respawnCompatibilityMode = data.spawnGroupData.flags.HasAnyFlag(SpawnGroupFlags.CompatibilityMode);
            m_creatureData = data;
            m_respawnradius = data.spawndist;
            m_respawnDelay = (uint)data.spawntimesecs;
            // Is the creature script objecting to us spawning? If yes, delay by a little bit (then re-check in ::Update)
            if (!m_respawnCompatibilityMode && m_respawnTime == 0 && !Global.ScriptMgr.CanSpawn(spawnId, data.Id, data, map))
            {
                SaveRespawnTime(RandomHelper.URand(4, 7));
                return false;
            }

            if (!Create(map.GenerateLowGuid(HighGuid.Creature), map, data.Id, data.spawnPoint, data, 0, !m_respawnCompatibilityMode))
                return false;

            //We should set first home position, because then AI calls home movement
            SetHomePosition(data.spawnPoint);

            m_deathState = DeathState.Alive;

            m_respawnTime = GetMap().GetCreatureRespawnTime(m_spawnId);

            // Is the creature script objecting to us spawning? If yes, delay by a little bit (then re-check in ::Update)
            if (m_respawnCompatibilityMode && m_respawnTime == 0 && !Global.ScriptMgr.CanSpawn(spawnId, GetEntry(), GetCreatureData(), map))
                m_respawnTime = Time.UnixTime + RandomHelper.URand(4, 7);

            if (m_respawnTime != 0)                          // respawn on UpdateLoadCreatureFromDB
            {
                m_deathState = DeathState.Dead;
                if (CanFly())
                {
                    var tz = map.GetHeight(GetPhaseShift(), data.spawnPoint, true, MapConst.MaxFallDistance);
                    if (data.spawnPoint.GetPositionZ() - tz > 0.1f && GridDefines.IsValidMapCoord(tz))
                        Relocate(data.spawnPoint.GetPositionX(), data.spawnPoint.GetPositionY(), tz);
                }
            }

            SetSpawnHealth();

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

        MovementGeneratorType GetDefaultMovementType() { return DefaultMovementType; }
        public void SetDefaultMovementType(MovementGeneratorType mgt) { DefaultMovementType = mgt; }

        public long GetRespawnTime() { return m_respawnTime; }
        public void SetRespawnTime(uint respawn) { m_respawnTime = respawn != 0 ? Time.UnixTime + respawn : 0; }

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

        public Unit SelectVictim()
        {
            // function provides main threat functionality
            // next-victim-selection algorithm and evade mode are called
            // threat list sorting etc.

            Unit target = null;

            // First checking if we have some taunt on us
            var tauntAuras = GetAuraEffectsByType(AuraType.ModTaunt);
            if (!tauntAuras.Empty())
            {
                var caster = tauntAuras.Last().GetCaster();

                // The last taunt aura caster is alive an we are happy to attack him
                if (caster != null && caster.IsAlive())
                    return GetVictim();
                else if (tauntAuras.Count > 1)
                {
                    // We do not have last taunt aura caster but we have more taunt auras,
                    // so find first available target

                    // Auras are pushed_back, last caster will be on the end
                    for (var i = tauntAuras.Count - 1; i >= 0; i--)
                    {
                        caster = tauntAuras[i].GetCaster();
                        if (caster != null && CanSeeOrDetect(caster, true) && IsValidAttackTarget(caster) && caster.IsInAccessiblePlaceFor(ToCreature()))
                        {
                            target = caster;
                            break;
                        }
                    }
                }
                else
                    target = GetVictim();
            }

            if (CanHaveThreatList())
            {
                if (target == null && !GetThreatManager().IsThreatListEmpty())
                    // No taunt aura or taunt aura caster is dead standard target selection
                    target = GetThreatManager().GetHostilTarget();
            }
            else if (!HasReactState(ReactStates.Passive))
            {
                // We have player pet probably
                target = GetAttackerForHelper();
                if (target == null && IsSummon())
                {
                    var owner = ToTempSummon().GetOwner();
                    if (owner != null)
                    {
                        if (owner.IsInCombat())
                            target = owner.GetAttackerForHelper();
                        if (target == null)
                        {
                            foreach (var unit in owner.m_Controlled)
                            {
                                if (unit.IsInCombat())
                                {
                                    target = unit.GetAttackerForHelper();
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

            if (target != null && _IsTargetAcceptable(target) && CanCreatureAttack(target))
            {
                if (!IsFocusing(null, true))
                    SetInFront(target);
                return target;
            }

            // last case when creature must not go to evade mode:
            // it in combat but attacker not make any damage and not enter to aggro radius to have record in threat list
            // Note: creature does not have targeted movement generator but has attacker in this case
            foreach (var unit in attackerList)
            {
                if (CanCreatureAttack(unit) && !unit.IsTypeId(TypeId.Player)
                    && !unit.ToCreature().HasUnitTypeMask(UnitTypeMask.ControlableGuardian))
                    return null;
            }

            // @todo a vehicle may eat some mob, so mob should not evade
            if (GetVehicle() != null)
                return null;

            // search nearby enemy before enter evade mode
            if (HasReactState(ReactStates.Aggressive))
            {
                target = SelectNearestTargetInAttackDistance(m_CombatDistance != 0 ? m_CombatDistance : SharedConst.AttackDistance);

                if (target != null && _IsTargetAcceptable(target) && CanCreatureAttack(target))
                    return target;
            }

            var iAuras = GetAuraEffectsByType(AuraType.ModInvisibility);
            if (!iAuras.Empty())
            {
                foreach (var aura in iAuras)
                {
                    if (aura.GetBase().IsPermanent())
                    {
                        GetAI().EnterEvadeMode();
                        break;
                    }
                }
                return null;
            }

            // enter in evade mode in other case
            GetAI().EnterEvadeMode(EvadeReason.NoHostiles);
            return null;
        }
    }

    public class VendorItemCount
    {
        public VendorItemCount(uint _item, uint _count)
        {
            itemId = _item;
            count = _count;
            lastIncrementTime = Time.UnixTime;
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
            var victim = Global.ObjAccessor.GetUnit(m_owner, m_victim);
            if (victim != null)
            {
                while (!m_assistants.Empty())
                {
                    var assistant = m_owner.GetMap().GetCreature(m_assistants[0]);
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
        List<ObjectGuid> m_assistants = new List<ObjectGuid>();
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
