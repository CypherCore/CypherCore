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
using Framework.Dynamic;
using Game.AI;
using Game.DataStorage;
using Game.Groups;
using Game.Loots;
using Game.Maps;
using Game.Network.Packets;
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
            m_defaultMovementType = MovementGeneratorType.Idle;
            m_regenHealth = true;
            m_meleeDamageSchoolMask = SpellSchoolMask.Normal;

            m_regenTimer = SharedConst.CreatureRegenInterval;
            valuesCount = (int)UnitFields.End;
            _dynamicValuesCount = (int)UnitDynamicFields.End;

            m_SightDistance = SharedConst.SightRangeUnit;

            ResetLootMode(); // restore default loot mode

            m_homePosition = new WorldLocation();
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
            DestroyForNearbyPlayers();
            if (IsAlive())
                setDeathState(DeathState.JustDied);
            RemoveCorpse(false);
        }

        public void SearchFormation()
        {
            if (IsSummon())
                return;

            ulong lowguid = GetSpawnId();
            if (lowguid == 0)
                return;

            var frmdata = FormationMgr.CreatureGroupMap.LookupByKey(lowguid);
            if (frmdata != null)
                FormationMgr.AddCreatureToGroup(frmdata.leaderGUID, this);
        }

        public void RemoveCorpse(bool setSpawnTime = true)
        {
            if (getDeathState() != DeathState.Corpse)
                return;

            m_corpseRemoveTime = Time.UnixTime;
            setDeathState(DeathState.Dead);
            RemoveAllAuras();
            UpdateObjectVisibility();
            loot.clear();
            uint respawnDelay = m_respawnDelay;
            if (IsAIEnabled)
                GetAI().CorpseRemoved(respawnDelay);

            // Should get removed later, just keep "compatibility" with scripts
            if (setSpawnTime)
                m_respawnTime = Time.UnixTime + respawnDelay;

            // if corpse was removed during falling, the falling will continue and override relocation to respawn position
            if (IsFalling())
                StopMoving();

            float x, y, z, o;
            GetRespawnPosition(out x, out y, out z, out o);
            SetHomePosition(x, y, z, o);
            GetMap().CreatureRelocation(this, x, y, z, o);
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
            CreatureTemplate cinfo = null;
            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(GetMap().GetDifficultyID());
            while (cinfo == null && difficultyEntry != null)
            {
                int idx = CreatureTemplate.DifficultyIDToDifficultyEntryIndex(difficultyEntry.Id);
                if (idx == -1)
                    break;

                if (normalInfo.DifficultyEntry[idx] != 0)
                {
                    cinfo = Global.ObjectMgr.GetCreatureTemplate(normalInfo.DifficultyEntry[idx]);
                    break;
                }

                if (difficultyEntry.FallbackDifficultyID == 0)
                    break;

                difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficultyEntry.FallbackDifficultyID);
            }

            if (cinfo == null)
                cinfo = normalInfo;

            // Initialize loot duplicate count depending on raid difficulty
            if (GetMap().Is25ManRaid())
                loot.maxDuplicates = 3;

            SetEntry(entry);                                        // normal entry always
            m_creatureInfo = cinfo;                                 // map mode related always

            // equal to player Race field, but creature does not have race
            SetByteValue(UnitFields.Bytes0, 0, 0);

            SetByteValue(UnitFields.Bytes0, 1, (byte)cinfo.UnitClass);

            // Cancel load if no model defined
            if (cinfo.GetFirstValidModel() == null)
            {
                Log.outError(LogFilter.Sql, "Creature (Entry: {0}) has no model defined in table `creature_template`, can't load. ", entry);
                return false;
            }

            CreatureModel model = ObjectManager.ChooseDisplayId(cinfo, data);
            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelRandomGender(ref model, cinfo);
            if (minfo == null)                                             // Cancel load if no model defined
            {
                Log.outError(LogFilter.Sql, "Creature (Entry: {0}) has invalid model {1} defined in table `creature_template`, can't load.", entry, model.CreatureDisplayID);
                return false;
            }

            SetDisplayId(model.CreatureDisplayID, model.DisplayScale);
            SetNativeDisplayId(model.CreatureDisplayID, model.DisplayScale);
            SetByteValue(UnitFields.Bytes0, 3, (byte)minfo.gender);

            // Load creature equipment
            if (data == null || data.equipmentId == 0)
                LoadEquipment(); // use default equipment (if available)
            else if(data != null && data.equipmentId != 0)                // override, 0 means no equipment
            {
                m_originalEquipmentId = (sbyte)data.equipmentId;
                LoadEquipment(data.equipmentId);
            }

            SetName(normalInfo.Name);                              // at normal entry always

            SetFloatValue(UnitFields.ModCastSpeed, 1.0f);
            SetFloatValue(UnitFields.ModCastHaste, 1.0f);
            SetFloatValue(UnitFields.ModHaste, 1.0f);
            SetFloatValue(UnitFields.ModRangedHaste, 1.0f);
            SetFloatValue(UnitFields.ModHasteRegen, 1.0f);
            SetFloatValue(UnitFields.ModTimeRate, 1.0f);

            SetSpeedRate(UnitMoveType.Walk, cinfo.SpeedWalk);
            SetSpeedRate(UnitMoveType.Run, cinfo.SpeedRun);
            SetSpeedRate(UnitMoveType.Swim, 1.0f);      // using 1.0 rate
            SetSpeedRate(UnitMoveType.Flight, 1.0f);    // using 1.0 rate

            SetObjectScale(cinfo.Scale);

            SetFloatValue(UnitFields.HoverHeight, cinfo.HoverHeight);

            // checked at loading
            m_defaultMovementType = (MovementGeneratorType)cinfo.MovementType;
            if (m_respawnradius == 0 && m_defaultMovementType == MovementGeneratorType.Random)
                m_defaultMovementType = MovementGeneratorType.Idle;

            for (byte i = 0; i < SharedConst.MaxCreatureSpells; ++i)
                m_spells[i] = GetCreatureTemplate().Spells[i];

            return true;
        }

        public bool UpdateEntry(uint entry, CreatureData data = null, bool updateLevel = true)
        {
            if (!InitEntry(entry, data))
                return false;

            CreatureTemplate cInfo = GetCreatureTemplate();

            m_regenHealth = cInfo.RegenHealth;

            // creatures always have melee weapon ready if any unless specified otherwise
            if (GetCreatureAddon() == null)
                SetSheath(SheathState.Melee);

            SetFaction(cInfo.Faction);

            ulong npcFlags;
            uint unitFlags, unitFlags2, unitFlags3, dynamicFlags;
            ObjectManager.ChooseCreatureFlags(cInfo, out npcFlags, out unitFlags, out unitFlags2, out unitFlags3, out dynamicFlags, data);

            if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Worldevent))
                SetUInt64Value(UnitFields.NpcFlags, npcFlags | Global.GameEventMgr.GetNPCFlag(this));
            else
                SetUInt64Value(UnitFields.NpcFlags, npcFlags);

            SetUInt32Value(UnitFields.Flags, unitFlags);
            SetUInt32Value(UnitFields.Flags2, unitFlags2);
            SetUInt32Value(UnitFields.Flags3, unitFlags3);

            SetUInt32Value(ObjectFields.DynamicFlags, dynamicFlags);

            SetUInt32Value(UnitFields.StateAnimId, (uint)CliDB.AnimationDataStorage.Count);

            RemoveFlag(UnitFields.Flags, UnitFlags.InCombat);

            SetBaseAttackTime(WeaponAttackType.BaseAttack, cInfo.BaseAttackTime);
            SetBaseAttackTime(WeaponAttackType.OffAttack, cInfo.BaseAttackTime);
            SetBaseAttackTime(WeaponAttackType.RangedAttack, cInfo.RangeAttackTime);

            if (updateLevel)
                SelectLevel();
            else
                UpdateLevelDependantStats(); // We still re-initialize level dependant stats on entry update

            SetMeleeDamageSchool((SpellSchools)cInfo.DmgSchool);
            SetModifierValue(UnitMods.ResistanceHoly, UnitModifierType.BaseValue, cInfo.Resistance[(int)SpellSchools.Holy]);
            SetModifierValue(UnitMods.ResistanceFire, UnitModifierType.BaseValue, cInfo.Resistance[(int)SpellSchools.Fire]);
            SetModifierValue(UnitMods.ResistanceNature, UnitModifierType.BaseValue, cInfo.Resistance[(int)SpellSchools.Nature]);
            SetModifierValue(UnitMods.ResistanceFrost, UnitModifierType.BaseValue, cInfo.Resistance[(int)SpellSchools.Frost]);
            SetModifierValue(UnitMods.ResistanceShadow, UnitModifierType.BaseValue, cInfo.Resistance[(int)SpellSchools.Shadow]);
            SetModifierValue(UnitMods.ResistanceArcane, UnitModifierType.BaseValue, cInfo.Resistance[(int)SpellSchools.Arcane]);

            SetCanModifyStats(true);
            UpdateAllStats();

            // checked and error show at loading templates
            var factionTemplate = CliDB.FactionTemplateStorage.LookupByKey(cInfo.Faction);
            if (factionTemplate != null)
            {
                if (Convert.ToBoolean(factionTemplate.Flags & (uint)FactionTemplateFlags.PVP))
                    SetPvP(true);
                else
                    SetPvP(false);
            }

            // updates spell bars for vehicles and set player's faction - should be called here, to overwrite faction that is set from the new template
            if (IsVehicle())
            {
                Player owner = GetCharmerOrOwnerPlayerOrPlayerItself();
                if (owner != null) // this check comes in case we don't have a player
                {
                    SetFaction(owner.getFaction()); // vehicles should have same as owner faction
                    owner.VehicleSpellInitialize();
                }
            }

            // trigger creature is always not selectable and can not be attacked
            if (IsTrigger())
                SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);

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
            return true;
        }

        public override void Update(uint diff)
        {
            if (IsAIEnabled && TriggerJustRespawned)
            {
                TriggerJustRespawned = false;
                GetAI().JustRespawned();
                if (m_vehicleKit != null)
                    m_vehicleKit.Reset();
            }

            UpdateMovementFlags();

            switch (m_deathState)
            {
                case DeathState.JustRespawned:
                case DeathState.JustDied:
                    Log.outError(LogFilter.Unit, "Creature ({0}) in wrong state: {2}", GetGUID().ToString(), m_deathState);
                    break;
                case DeathState.Dead:
                    {
                        long now = Time.UnixTime;
                        if (m_respawnTime <= now)
                        {
                            // First check if there are any scripts that object to us respawning
                            if (!Global.ScriptMgr.CanSpawn(GetSpawnId(), GetEntry(), GetCreatureTemplate(), GetCreatureData(), GetMap()))
                                break; // Will be rechecked on next Update call

                            ObjectGuid dbtableHighGuid = ObjectGuid.Create(HighGuid.Creature, GetMapId(), GetEntry(), m_spawnId);
                            long linkedRespawntime = GetMap().GetLinkedRespawnTime(dbtableHighGuid);
                            if (linkedRespawntime == 0)             // Can respawn
                                Respawn();
                            else                                // the master is dead
                            {
                                ObjectGuid targetGuid = Global.ObjectMgr.GetLinkedRespawnGuid(dbtableHighGuid);
                                if (targetGuid == dbtableHighGuid) // if linking self, never respawn (check delayed to next day)
                                    SetRespawnTime(Time.Day);
                                else
                                    m_respawnTime = (now > linkedRespawntime ? now : linkedRespawntime) + RandomHelper.IRand(5, Time.Minute); // else copy time from master and add a little
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
                                group.EndRoll(loot);
                            m_groupLootTimer = 0;
                            lootingGroupLowGUID.Clear();
                        }
                        else m_groupLootTimer -= diff;
                    }
                    else if (m_corpseRemoveTime <= Time.UnixTime)
                    {
                        RemoveCorpse(false);
                        Log.outDebug(LogFilter.Unit, "Removing corpse... {0} ", GetUInt32Value(ObjectFields.Entry));
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
                            WorldObject objTarget = Global.ObjAccessor.GetWorldObject(this, m_suppressedTarget);
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
                            Unit charmer = Global.ObjAccessor.GetUnit(this, LastCharmerGUID);
                            if (charmer)
                                if (CanStartAttack(charmer, true))
                                    i_AI.AttackStart(charmer);
                        }

                        LastCharmerGUID.Clear();
                    }

                    // periodic check to see if the creature has passed an evade boundary
                    if (IsAIEnabled && !IsInEvadeMode() && IsInCombat())
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
                    if (m_combatPulseDelay > 0 && IsInCombat() && GetMap().IsDungeon())
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
                                {
                                    if (CanHaveThreatList())
                                        AddThreat(player, 0.0f);
                                    SetInCombatWith(player);
                                    player.SetInCombatWith(this);
                                }
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

                    if (m_regenTimer > 0)
                    {
                        if (diff >= m_regenTimer)
                            m_regenTimer = 0;
                        else
                            m_regenTimer -= diff;
                    }

                    if (m_regenTimer == 0)
                    {
                        bool bInCombat = IsInCombat() && (!GetVictim() ||                                        // if IsInCombat() is true and this has no victim
                                                          !GetVictim().GetCharmerOrOwnerPlayerOrPlayerItself() ||                // or the victim/owner/charmer is not a player
                                                          !GetVictim().GetCharmerOrOwnerPlayerOrPlayerItself().IsGameMaster()); // or the victim/owner/charmer is not a GameMaster

                        if (!IsInEvadeMode() && (!bInCombat || IsPolymorphed() || CanNotReachTarget())) // regenerate health if not in combat or if polymorphed
                            RegenerateHealth();

                        if (HasFlag(UnitFields.Flags2, UnitFlags2.RegeneratePower))
                        {
                            if (GetPowerType() == PowerType.Energy)
                                Regenerate(PowerType.Energy);
                            else
                                RegenerateMana();
                        }
                        m_regenTimer = SharedConst.CreatureRegenInterval;
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
            Global.ScriptMgr.OnCreatureUpdate(this, diff);

        }

        void RegenerateMana()
        {
            int curValue = GetPower(PowerType.Mana);
            int maxValue = GetMaxPower(PowerType.Mana);

            if (curValue >= maxValue)
                return;

            int addvalue = 0;

            // Combat and any controlled creature
            if (IsInCombat() || !GetCharmerOrOwnerGUID().IsEmpty())
            {
                float ManaIncreaseRate = WorldConfig.GetFloatValue(WorldCfg.RatePowerMana);
                addvalue = (int)((27.0f / 5.0f + 17.0f) * ManaIncreaseRate);
            }
            else
                addvalue = maxValue / 3;

            // Apply modifiers (if any).
            addvalue *= (int)GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerRegenPercent, (int)PowerType.Mana);
            addvalue += GetTotalAuraModifierByMiscValue(AuraType.ModPowerRegen, (int)PowerType.Mana) * SharedConst.CreatureRegenInterval / (5 * Time.InMilliseconds);

            ModifyPower(PowerType.Mana, addvalue);
        }

        void RegenerateHealth()
        {
            if (!isRegeneratingHealth())
                return;

            ulong curValue = GetHealth();
            ulong maxValue = GetMaxHealth();

            if (curValue >= maxValue)
                return;

            long addvalue = 0;

            // Not only pet, but any controlled creature
            if (!GetCharmerOrOwnerGUID().IsEmpty())
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

        public void Regenerate(PowerType power)
        {
            int curValue = GetPower(power);
            int maxValue = GetMaxPower(power);

            if (curValue >= maxValue)
                return;

            float addvalue = 0.0f;

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
                default:
                    return;
            }

            // Apply modifiers (if any).
            addvalue *= GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerRegenPercent, (int)power);
            addvalue += GetTotalAuraModifierByMiscValue(AuraType.ModPowerRegen, (int)power) * (IsHunterPet() ? SharedConst.PetFocusRegenInterval : SharedConst.CreatureRegenInterval) / (5 * Time.InMilliseconds);

            ModifyPower(power, (int)addvalue);
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
            else if (m_formation.getLeader() == this)
            {
                m_formation.FormationReset(false);
                GetMotionMaster().Initialize();
            }
            else if (m_formation.isFormed())
                GetMotionMaster().MoveIdle(); //wait the order of leader
            else
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

            Creature creature = new Creature();
            if (!creature.Create(lowGuid, map, entry, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation(), null, vehId))
                return null;

            return creature;
        }

        public static Creature CreateCreatureFromDB(ulong spawnId, Map map, bool addToMap = true, bool allowDuplicate = false)
        {
            Creature creature = new Creature();
            if (!creature.LoadCreatureFromDB(spawnId, map, addToMap, allowDuplicate))
                return null;

            return creature;
        }

        public bool Create(ulong guidlow, Map map, uint entry, float x, float y, float z, float ang, CreatureData data, uint vehId)
        {
            SetMap(map);

            if (data != null)
            {
                PhasingHandler.InitDbPhaseShift(GetPhaseShift(), data.phaseUseFlags, data.phaseId, data.phaseGroup);
                PhasingHandler.InitDbVisibleMapId(GetPhaseShift(), data.terrainSwapMap);
            }

            CreatureTemplate cinfo = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (cinfo == null)
            {
                Log.outError(LogFilter.Sql, "Creature.Create: creature template (guidlow: {0}, entry: {1}) does not exist.", guidlow, entry);
                return false;
            }

            //! Relocate before CreateFromProto, to initialize coords and allow
            //! returning correct zone id for selecting OutdoorPvP/Battlefield script
            Relocate(x, y, z, ang);

            // Check if the position is valid before calling CreateFromProto(), otherwise we might add Auras to Creatures at
            // invalid position, triggering a crash about Auras not removed in the destructor
            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Unit, "Creature.Create: given coordinates for creature (guidlow {0}, entry {1}) are not valid (X: {2}, Y: {3}, Z: {4}, O: {5})", guidlow, entry, x, y, z, ang);
                return false;
            }

            if (!CreateFromProto(guidlow, entry, data, vehId))
                return false;

            cinfo = GetCreatureTemplate(); // might be different than initially requested
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
                z += GetFloatValue(UnitFields.HoverHeight);

                //! Relocate again with updated Z coord
                Relocate(x, y, z, ang);
            }

            CreatureModel display = new CreatureModel(GetNativeDisplayId(), GetNativeDisplayScale(), 1.0f);
            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelRandomGender(ref display, cinfo);
            if (minfo != null && !IsTotem())                               // Cancel load if no model defined or if totem
            {
                SetDisplayId(display.CreatureDisplayID, display.DisplayScale);
                SetNativeDisplayId(display.CreatureDisplayID, display.DisplayScale);
                SetByteValue(UnitFields.Bytes0, 3, (byte)minfo.gender);
            }

            LastUsedScriptID = GetScriptId();

            // TODO: Replace with spell, handle from DB
            if (IsSpiritHealer() || IsSpiritGuide())
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

        public bool isCanInteractWithBattleMaster(Player player, bool msg)
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
            return player.getLevel() >= 15 && player.GetClass() == GetCreatureTemplate().TrainerClass;
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

        public void StartPickPocketRefillTimer()
        {
            _pickpocketLootRestore = Time.UnixTime + WorldConfig.GetIntValue(WorldCfg.CreaturePickpocketRefill);
        }
        public void ResetPickPocketRefillTimer() { _pickpocketLootRestore = 0; }
        public bool CanGeneratePickPocketLoot() { return _pickpocketLootRestore <= Time.UnixTime; }
        public void SetSkinner(ObjectGuid guid) { _skinner = guid; }
        public ObjectGuid GetSkinner() { return _skinner; } // Returns the player who skinned this creature    

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

        public void SetLootRecipient(Unit unit)
        {
            // set the player whose group should receive the right
            // to loot the creature after it dies
            // should be set to NULL after the loot disappears

            if (unit == null)
            {
                m_lootRecipient.Clear();
                m_lootRecipientGroup.Clear();
                RemoveFlag(ObjectFields.DynamicFlags, UnitDynFlags.Lootable | UnitDynFlags.Tapped);
                return;
            }

            if (!unit.IsTypeId(TypeId.Player) && !unit.IsVehicle())
                return;

            Player player = unit.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (player == null)                                             // normal creature, no player involved
                return;

            m_lootRecipient = player.GetGUID();
            Group group = player.GetGroup();
            if (group)
                m_lootRecipientGroup = group.GetGUID();

            SetFlag(ObjectFields.DynamicFlags, UnitDynFlags.Tapped);
        }

        public bool isTappedBy(Player player)
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
            ulong npcflag = GetUInt64Value(UnitFields.NpcFlags);
            uint unitFlags = GetUInt32Value(UnitFields.Flags);
            uint unitFlags2 = GetUInt32Value(UnitFields.Flags2);
            uint unitFlags3 = GetUInt32Value(UnitFields.Flags3);
            uint dynamicflags = GetUInt32Value(ObjectFields.DynamicFlags);

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

                if (dynamicflags == cinfo.DynamicFlags)
                    dynamicflags = 0;
            }

            // data.guid = guid must not be updated at save
            data.id = GetEntry();
            data.mapid = (ushort)mapid;
            data.displayid = displayId;
            data.equipmentId = GetCurrentEquipmentId();
            data.posX = GetPositionX();
            data.posY = GetPositionY();
            data.posZ = GetPositionZMinusOffset();
            data.orientation = GetOrientation();
            data.spawntimesecs = m_respawnDelay;
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
            data.dynamicflags = dynamicflags;

            data.phaseId = GetDBPhase() > 0 ? (uint)GetDBPhase() : data.phaseId;
            data.phaseGroup = GetDBPhase() < 0 ? (uint)-GetDBPhase() : data.phaseGroup;

            // update in DB
            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_CREATURE);
            stmt.AddValue(0, m_spawnId);
            trans.Append(stmt);

            byte index = 0;

            stmt = DB.World.GetPreparedStatement(WorldStatements.INS_CREATURE);
            stmt.AddValue(index++, m_spawnId);
            stmt.AddValue(index++, GetEntry());
            stmt.AddValue(index++, mapid);
            stmt.AddValue(index++, string.Join(",", data.spawnDifficulties));
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
            CreatureTemplate cInfo = GetCreatureTemplate();

            // level
            byte minlevel = (byte)Math.Min(cInfo.Maxlevel, cInfo.Minlevel);
            byte maxlevel = (byte)Math.Max(cInfo.Maxlevel, cInfo.Minlevel);
            byte level = (byte)(minlevel == maxlevel ? minlevel : RandomHelper.URand(minlevel, maxlevel));
            SetLevel(level);

            if (HasScalableLevels())
            {
                SetUInt32Value(UnitFields.ScalingLevelMin, cInfo.levelScaling.Value.MinLevel);
                SetUInt32Value(UnitFields.ScalingLevelMax, cInfo.levelScaling.Value.MaxLevel);

                int mindelta = Math.Min(cInfo.levelScaling.Value.DeltaLevelMax, cInfo.levelScaling.Value.DeltaLevelMin);
                int maxdelta = Math.Max(cInfo.levelScaling.Value.DeltaLevelMax, cInfo.levelScaling.Value.DeltaLevelMin);
                int delta = mindelta == maxdelta ? mindelta : RandomHelper.IRand(mindelta, maxdelta);

                SetInt32Value(UnitFields.ScalingLevelDelta, delta);
            }

            UpdateLevelDependantStats();
        }

        void UpdateLevelDependantStats()
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureEliteType rank = IsPet() ? 0 : cInfo.Rank;
            CreatureBaseStats stats = Global.ObjectMgr.GetCreatureBaseStats(getLevel(), cInfo.UnitClass);

            // health
            float healthmod = _GetHealthMod(rank);

            uint basehp = stats.GenerateHealth(cInfo);
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

            SetModifierValue(UnitMods.Health, UnitModifierType.BaseValue, health);

            //Damage
            float basedamage = stats.GenerateBaseDamage(cInfo);
            float weaponBaseMinDamage = basedamage;
            float weaponBaseMaxDamage = basedamage * 1.5f;

            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

            SetBaseWeaponDamage(WeaponAttackType.OffAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
            SetBaseWeaponDamage(WeaponAttackType.OffAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

            SetBaseWeaponDamage(WeaponAttackType.RangedAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
            SetBaseWeaponDamage(WeaponAttackType.RangedAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

            SetModifierValue(UnitMods.AttackPower, UnitModifierType.BaseValue, stats.AttackPower);
            SetModifierValue(UnitMods.AttackPowerRanged, UnitModifierType.BaseValue, stats.RangedAttackPower);

            float armor = stats.GenerateArmor(cInfo); // @todo Why is this treated as uint32 when it's a float?
            SetModifierValue(UnitMods.Armor, UnitModifierType.BaseValue, armor);
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
            if (m_creatureData == null)
                return;

            ulong curhealth;

            if (!m_regenHealth)
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

        public override bool hasQuest(uint quest_id)
        {
            var qr = Global.ObjectMgr.GetCreatureQuestRelationBounds(GetEntry());
            foreach (var id in qr)
            {
                if (id == quest_id)
                    return true;
            }
            return false;
        }

        public override bool hasInvolvedQuest(uint quest_id)
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

            GetMap().RemoveCreatureRespawnTime(m_spawnId);
            Global.ObjectMgr.DeleteCreatureData(m_spawnId);

            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_CREATURE);
            stmt.AddValue(0, m_spawnId);
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
            if (IsAIEnabled && GetAI().CanSeeAlways(obj))
                return true;

            return false;
        }

        public bool CanStartAttack(Unit who, bool force)
        {
            if (IsCivilian())
                return false;

            // This set of checks is should be done only for creatures
            if ((HasFlag(UnitFields.Flags, UnitFlags.ImmuneToNpc) && !who.IsTypeId(TypeId.Player))                                // flag is valid only for non player characters
                || (HasFlag(UnitFields.Flags, UnitFlags.ImmuneToPc) && who.IsTypeId(TypeId.Player))                               // immune to PC and target is a player, return false
                || (who.GetOwner() && who.GetOwner().IsTypeId(TypeId.Player) && HasFlag(UnitFields.Flags, UnitFlags.ImmuneToPc))) // player pets are immune to pc as well
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

                if (who.IsInCombat() && IsWithinDist(who, SharedConst.AttackDistance))
                {
                    Unit victim = who.getAttackerForHelper();
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

        public override void setDeathState(DeathState s)
        {
            base.setDeathState(s);

            if (s == DeathState.JustDied)
            {
                m_corpseRemoveTime = Time.UnixTime + m_corpseDelay;
                m_respawnTime = Time.UnixTime + m_respawnDelay + m_corpseDelay;

                // always save boss respawn time at death to prevent crash cheating
                if (WorldConfig.GetBoolValue(WorldCfg.SaveRespawnTimeImmediately) || isWorldBoss())
                    SaveRespawnTime();

                ReleaseFocus(null, false);               // remove spellcast focus
                DoNotReacquireTarget(); // cancel delayed re-target
                SetTarget(ObjectGuid.Empty); // drop target - dead mobs shouldn't ever target things

                SetUInt64Value(UnitFields.NpcFlags, (ulong)NPCFlags.None);

                SetUInt32Value(UnitFields.MountDisplayId, 0); // if creature is mounted on a virtual mount, remove it at death

                setActive(false);

                if (HasSearchedAssistance())
                {
                    SetNoSearchAssistance(false);
                    UpdateSpeed(UnitMoveType.Run);
                }

                //Dismiss group if is leader
                if (m_formation != null && m_formation.getLeader() == this)
                    m_formation.FormationReset(true);

                if ((CanFly() || IsFlying()))
                    GetMotionMaster().MoveFall();

                base.setDeathState(DeathState.Corpse);
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

                ClearUnitState(UnitState.AllState & ~UnitState.IgnorePathfinding);

                if (!IsPet())
                {
                    CreatureData creatureData = GetCreatureData();
                    CreatureTemplate cInfo = GetCreatureTemplate();

                    ulong npcFlags;
                    uint unitFlags, unitFlags2, unitFlags3, dynamicFlags;
                    ObjectManager.ChooseCreatureFlags(cInfo, out npcFlags, out unitFlags, out unitFlags2, out unitFlags3, out dynamicFlags, creatureData);

                    if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Worldevent))
                        SetUInt64Value(UnitFields.NpcFlags, npcFlags | Global.GameEventMgr.GetNPCFlag(this));
                    else
                        SetUInt64Value(UnitFields.NpcFlags, npcFlags);

                    SetUInt32Value(UnitFields.Flags, unitFlags);
                    SetUInt32Value(UnitFields.Flags2, unitFlags2);
                    SetUInt32Value(UnitFields.Flags3, unitFlags3);
                    SetUInt32Value(ObjectFields.DynamicFlags, dynamicFlags);

                    RemoveFlag(UnitFields.Flags, UnitFlags.InCombat);

                    SetMeleeDamageSchool((SpellSchools)cInfo.DmgSchool);
                }

                InitializeMovementAI();
                base.setDeathState(DeathState.Alive);
                LoadCreaturesAddon();
            }
        }

        public void Respawn(bool force = false)
        {
            DestroyForNearbyPlayers();

            if (force)
            {
                if (IsAlive())
                    setDeathState(DeathState.JustDied);
                else if (getDeathState() != DeathState.Corpse)
                    setDeathState(DeathState.Corpse);
            }

            RemoveCorpse(false);

            if (getDeathState() == DeathState.Dead)
            {
                if (m_spawnId != 0)
                    GetMap().RemoveCreatureRespawnTime(m_spawnId);

                Log.outDebug(LogFilter.Unit, "Respawning creature {0} ({1})", GetName(), GetGUID().ToString());
                m_respawnTime = 0;
                ResetPickPocketRefillTimer();
                loot.clear();

                if (m_originalEntry != GetEntry())
                    UpdateEntry(m_originalEntry);
                else
                    SelectLevel();

                setDeathState(DeathState.JustRespawned);

                CreatureModel display = new CreatureModel(GetNativeDisplayId(), GetNativeDisplayScale(), 1.0f);
                CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelRandomGender(ref display, GetCreatureTemplate());
                if (minfo != null)                                             // Cancel load if no model defined
                {
                    SetDisplayId(display.CreatureDisplayID, display.DisplayScale);
                    SetNativeDisplayId(display.CreatureDisplayID, display.DisplayScale);
                    SetByteValue(UnitFields.Bytes0, 3, (byte)minfo.gender);
                }

                GetMotionMaster().InitDefault();
                //Re-initialize reactstate that could be altered by movementgenerators
                InitializeReactState();

                //Call AI respawn virtual function
                if (IsAIEnabled)
                {
                    GetAI().Reset();
                    TriggerJustRespawned = true;//delay event to next tick so all creatures are created on the map before processing
                }

                uint poolid = GetSpawnId() != 0 ? Global.PoolMgr.IsPartOfAPool<Creature>(GetSpawnId()) : 0;
                if (poolid != 0)
                    Global.PoolMgr.UpdatePool<Creature>(poolid, GetSpawnId());
            }

            UpdateObjectVisibility();
        }

        public void ForcedDespawn(uint timeMSToDespawn = 0, TimeSpan forceRespawnTimer = default(TimeSpan))
        {
            if (timeMSToDespawn != 0)
            {
                ForcedDespawnDelayEvent pEvent = new ForcedDespawnDelayEvent(this, forceRespawnTimer);

                m_Events.AddEvent(pEvent, m_Events.CalculateTime(timeMSToDespawn));
                return;
            }

            if (forceRespawnTimer > TimeSpan.Zero)
            {
                if (IsAlive())
                {
                    uint respawnDelay = m_respawnDelay;
                    uint corpseDelay = m_corpseDelay;
                    m_respawnDelay = (uint)forceRespawnTimer.TotalSeconds;
                    m_corpseDelay = 0;
                    setDeathState(DeathState.JustDied);

                    m_respawnDelay = respawnDelay;
                    m_corpseDelay = corpseDelay;
                }
                else
                {
                    m_corpseRemoveTime = Time.UnixTime;
                    m_respawnTime = Time.UnixTime + (long)forceRespawnTimer.TotalMilliseconds;
                }

            }
            else
            {
                if (IsAlive())
                    setDeathState(DeathState.JustDied);
            }

            RemoveCorpse(false);
        }

        public void DespawnOrUnsummon(TimeSpan time, TimeSpan forceRespawnTimer = default(TimeSpan)) { DespawnOrUnsummon((uint)time.TotalMilliseconds, forceRespawnTimer); }

        public void DespawnOrUnsummon(uint msTimeToDespawn = 0, TimeSpan forceRespawnTimer = default(TimeSpan))
        {
            TempSummon summon = ToTempSummon();
            if (summon != null)
                summon.UnSummon(msTimeToDespawn);
            else
                ForcedDespawn(msTimeToDespawn, forceRespawnTimer);
        }

        public override bool IsImmunedToSpell(SpellInfo spellInfo, Unit caster)
        {
            if (spellInfo == null)
                return false;

            // Creature is immune to main mechanic of the spell
            if (Convert.ToBoolean(GetCreatureTemplate().MechanicImmuneMask & (1 << ((int)spellInfo.Mechanic - 1))))
                return true;

            // This check must be done instead of 'if (GetCreatureTemplate().MechanicImmuneMask & (1 << (spellInfo.Mechanic - 1)))' for not break
            // the check of mechanic immunity on DB (tested) because GetCreatureTemplate().MechanicImmuneMask and m_spellImmune[IMMUNITY_MECHANIC] don't have same data.
            bool immunedToAllEffects = true;
            foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
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
            SpellEffectInfo effect = spellInfo.GetEffect(GetMap().GetDifficultyID(), index);
            if (effect == null)
                return true;
            if (Convert.ToBoolean(GetCreatureTemplate().MechanicImmuneMask & (1 << ((int)effect.Mechanic - 1))))
                return true;

            if (GetCreatureTemplate().CreatureType == CreatureType.Mechanical && effect.Effect == SpellEffectName.Heal)
                return true;

            return base.IsImmunedToSpellEffect(spellInfo, index, caster);
        }

        public bool isElite()
        {
            if (IsPet())
                return false;

            var rank = GetCreatureTemplate().Rank;
            return rank != CreatureEliteType.Elite && rank != CreatureEliteType.RareElite;
        }

        public bool isWorldBoss()
        {
            if (IsPet())
                return false;

            return Convert.ToBoolean(GetCreatureTemplate().TypeFlags & CreatureTypeFlags.BossMob);
        }

        public SpellInfo reachWithSpellAttack(Unit victim)
        {
            if (victim == null)
                return null;

            for (uint i = 0; i < SharedConst.MaxCreatureSpells; ++i)
            {
                if (m_spells[i] == 0)
                    continue;
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(m_spells[i]);
                if (spellInfo == null)
                {
                    Log.outError(LogFilter.Unit, "WORLD: unknown spell id {0}", m_spells[i]);
                    continue;
                }

                bool bcontinue = true;
                foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
                {
                    if (effect != null && ((effect.Effect == SpellEffectName.SchoolDamage) || (effect.Effect == SpellEffectName.Instakill)
                        || (effect.Effect == SpellEffectName.EnvironmentalDamage) || (effect.Effect == SpellEffectName.HealthLeech)))
                    {
                        bcontinue = false;
                        break;
                    }
                }
                if (bcontinue)
                    continue;

                var costs = spellInfo.CalcPowerCost(this, spellInfo.SchoolMask);
                var m = costs.Find(cost => cost.Power == PowerType.Mana);
                if (m != null)
                    if (m.Amount > GetPower(PowerType.Mana))
                        continue;

                float range = spellInfo.GetMaxRange(false);
                float minrange = spellInfo.GetMinRange(false);
                float dist = GetDistance(victim);
                if (dist > range || dist < minrange)
                    continue;
                if (spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence) && HasFlag(UnitFields.Flags, UnitFlags.Silenced))
                    continue;
                if (spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Pacify) && HasFlag(UnitFields.Flags, UnitFlags.Pacified))
                    continue;
                return spellInfo;
            }
            return null;
        }

        SpellInfo reachWithSpellCure(Unit victim)
        {
            if (victim == null)
                return null;

            for (uint i = 0; i < SharedConst.MaxCreatureSpells; ++i)
            {
                if (m_spells[i] == 0)
                    continue;
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(m_spells[i]);
                if (spellInfo == null)
                {
                    Log.outError(LogFilter.Unit, "WORLD: unknown spell id {0}", m_spells[i]);
                    continue;
                }

                bool bcontinue = true;
                foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
                {
                    if (effect != null && effect.Effect == SpellEffectName.Heal)
                    {
                        bcontinue = false;
                        break;
                    }
                }
                if (bcontinue)
                    continue;

                var costs = spellInfo.CalcPowerCost(this, spellInfo.SchoolMask);
                var m = costs.Find(cost => cost.Power == PowerType.Mana);
                if (m != null)
                    if (m.Amount > GetPower(PowerType.Mana))
                        continue;

                float range = spellInfo.GetMaxRange(true);
                float minrange = spellInfo.GetMinRange(true);
                float dist = GetDistance(victim);

                if (dist > range || dist < minrange)
                    continue;
                if (spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence) && HasFlag(UnitFields.Flags, UnitFlags.Silenced))
                    continue;
                if (spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Pacify) && HasFlag(UnitFields.Flags, UnitFlags.Pacified))
                    continue;
                return spellInfo;
            }
            return null;
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

        public Player SelectNearestPlayer(float distance)
        {
            var checker = new NearestPlayerInObjectRangeCheck(this, distance);
            var searcher = new PlayerLastSearcher(this, checker);
            Cell.VisitAllObjects(this, searcher, distance);
            return searcher.GetTarget();
        }

        public void SendAIReaction(AiReaction reactionType)
        {
            AIReaction packet = new AIReaction();

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
                    List<Creature> assistList = new List<Creature>();

                    var u_check = new AnyAssistCreatureInRangeCheck(this, GetVictim(), radius);
                    var searcher = new CreatureListSearcher(this, assistList, u_check);
                    Cell.VisitGridObjects(this, searcher, radius);

                    if (!assistList.Empty())
                    {
                        AssistDelayEvent e = new AssistDelayEvent(GetVictim().GetGUID(), this);
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
            if (IsInEvadeMode())
                return false;

            // is it true?
            if (!HasReactState(ReactStates.Aggressive))
                return false;

            // we don't need help from zombies :)
            if (!IsAlive())
                return false;

            // we don't need help from non-combatant ;)
            if (IsCivilian())
                return false;

            if (HasFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable | UnitFlags.ImmuneToPc))
                return false;

            // skip fighting creature
            if (IsInCombat())
                return false;

            // only free creature
            if (!GetCharmerOrOwnerGUID().IsEmpty())
                return false;

            // only from same creature faction
            if (checkfaction)
            {
                if (getFaction() != u.getFaction())
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
            if (IsFriendlyTo(target) || !target.isTargetableForAttack(false)
                || (m_vehicle != null && (IsOnVehicle(target) || m_vehicle.GetBase().IsOnVehicle(target))))
                return false;

            if (target.HasUnitState(UnitState.Died))
            {
                // guards can detect fake death
                if (IsGuard() && target.HasFlag(UnitFields.Flags2, UnitFlags2.FeignDeath))
                    return true;
                else
                    return false;
            }

            Unit myVictim = getAttackerForHelper();
            Unit targetVictim = target.getAttackerForHelper();

            // if I'm already fighting target, or I'm hostile towards the target, the target is acceptable
            if (myVictim == target || targetVictim == this || IsHostileTo(target))
                return true;

            // if the target's victim is friendly, and the target is neutral, the target is acceptable
            if (targetVictim != null && IsFriendlyTo(targetVictim))
                return true;

            // if the target's victim is not friendly, or the target is friendly, the target is not acceptable
            return false;
        }

        public override void SaveRespawnTime()
        {
            if (IsSummon() || m_spawnId == 0 || (m_creatureData != null && !m_creatureData.dbData))
                return;

            GetMap().SaveCreatureRespawnTime(m_spawnId, m_respawnTime);
        }

        public bool CanCreatureAttack(Unit victim, bool force = true)
        {
            if (!victim.IsInMap(this))
                return false;

            if (!IsValidAttackTarget(victim))
                return false;

            if (!victim.isInAccessiblePlaceFor(this))
                return false;

            if (IsAIEnabled && !GetAI().CanAIAttack(victim))
                return false;

            if (GetMap().IsDungeon())
                return true;

            // if the mob is actively being damaged, do not reset due to distance unless it's a world boss
            if (!isWorldBoss())
                if (Time.UnixTime - GetLastDamagedTime() <= SharedConst.MaxAggroResetTime)
                    return true;

            //Use AttackDistance in distance check if threat radius is lower. This prevents creature bounce in and out of combat every update tick.
            float dist = Math.Max(GetAttackDistance(victim), (WorldConfig.GetFloatValue(WorldCfg.ThreatRadius) + m_CombatDistance));

            Unit unit = GetCharmerOrOwner();
            if (unit != null)
                return victim.IsWithinDist(unit, dist);
            else
                return victim.IsInDist(m_homePosition, dist);
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

                SetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.StandState, (byte)(cainfo.bytes1 & 0xFF));
                //SetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.PetTalent, (byte)((cainfo.bytes1 >> 8) & 0xFF));
                SetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.PetTalents, 0);
                SetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.VisFlag, (byte)((cainfo.bytes1 >> 16) & 0xFF));
                SetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.AnimTier, (byte)((cainfo.bytes1 >> 24) & 0xFF));

                //! Suspected correlation between UNIT_FIELD_BYTES_1, offset 3, value 0x2:
                //! If no inhabittype_fly (if no MovementFlag_DisableGravity or MovementFlag_CanFly flag found in sniffs)
                //! Check using InhabitType as movement flags are assigned dynamically
                //! basing on whether the creature is in air or not
                //! Set MovementFlag_Hover. Otherwise do nothing.
                if (Convert.ToBoolean(GetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.AnimTier) & (byte)UnitBytes1Flags.Hover) && !Convert.ToBoolean(GetCreatureTemplate().InhabitType & InhabitType.Air))
                    AddUnitMovementFlag(MovementFlag.Hover);
            }

            if (cainfo.bytes2 != 0)
            {
                // 0 SheathState
                // 1 PvpFlags
                // 2 PetFlags           Pet only, so always 0 for default creature
                // 3 ShapeshiftForm     Must be determined/set by shapeshift spell/aura

                SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.SheathState, (byte)(cainfo.bytes2 & 0xFF));
                //SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, uint8((cainfo->bytes2 >> 8) & 0xFF));
                //SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PetFlags, uint8((cainfo->bytes2 >> 16) & 0xFF));
                SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PetFlags, 0);
                //SetByteValue(UnitFields.Bytes2, UNIT_BYTES_2_OFFSET_SHAPESHIFT_FORM, uint8((cainfo->bytes2 >> 24) & 0xFF));
                SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.ShapeshiftForm, 0);
            }

            if (cainfo.emote != 0)
                SetUInt32Value(UnitFields.NpcEmotestate, cainfo.emote);

            SetAIAnimKitId(cainfo.aiAnimKit);
            SetMovementAnimKitId(cainfo.movementAnimKit);
            SetMeleeAnimKitId(cainfo.meleeAnimKit);

            // Check if visibility distance different
            if (cainfo.visibilityDistanceType != VisibilityDistanceType.Normal)
                SetVisibilityDistanceOverride(cainfo.visibilityDistanceType);

            //Load Path
            if (cainfo.path_id != 0)
                m_path_id = cainfo.path_id;

            if (cainfo.auras != null)
            {
                foreach (var id in cainfo.auras)
                {
                    SpellInfo AdditionalSpellInfo = Global.SpellMgr.GetSpellInfo(id);
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
            Team enemy_team = attacker.GetTeam();

            ZoneUnderAttack packet = new ZoneUnderAttack();
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

            Map map = GetMap();

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
                {
                    SetInCombatWith(player);
                    player.SetInCombatWith(this);
                    AddThreat(player, 0.0f);
                }

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
            long now = Time.UnixTime;
            if (m_respawnTime > now)
                return m_respawnTime;
            else
                return now;
        }

        public void GetRespawnPosition(out float x, out float y, out float z)
        {
            if (m_spawnId != 0)
            {
                CreatureData data = Global.ObjectMgr.GetCreatureData(GetSpawnId());
                if (data != null)
                {
                    x = data.posX;
                    y = data.posY;
                    z = data.posZ;
                    return;
                }
            }

            GetPosition(out x, out y, out z);
        }
        public void GetRespawnPosition(out float x, out float y, out float z, out float ori)
        {
            if (m_spawnId != 0)
            {
                CreatureData data = Global.ObjectMgr.GetCreatureData(GetSpawnId());
                if (data != null)
                {
                    x = data.posX;
                    y = data.posY;
                    z = data.posZ;
                    ori = data.orientation;

                    return;
                }
            }

            GetPosition(out x, out y, out z, out ori);
        }
        public void GetRespawnPosition(out float x, out float y, out float z, out float ori, out float dist)
        {
            if (m_spawnId != 0)
            {
                CreatureData data = Global.ObjectMgr.GetCreatureData(GetSpawnId());
                if (data != null)
                {
                    x = data.posX;
                    y = data.posY;
                    z = data.posZ;
                    ori = data.orientation;
                    dist = data.spawndist;

                    return;
                }
            }

            GetPosition(out x, out y, out z, out ori);
            dist = 0;
        }

        public void AllLootRemovedFromCorpse()
        {
            if (loot.loot_type != LootType.Skinning && !IsPet() && GetCreatureTemplate().SkinLootId != 0 && hasLootRecipient())
                if (LootStorage.Skinning.HaveLootFor(GetCreatureTemplate().SkinLootId))
                    SetFlag(UnitFields.Flags, UnitFlags.Skinnable);

            long now = Time.UnixTime;
            // Do not reset corpse remove time if corpse is already removed
            if (m_corpseRemoveTime <= now)
                return;

            float decayRate = WorldConfig.GetFloatValue(WorldCfg.RateCorpseDecayLooted);
            // corpse skinnable, but without skinning flag, and then skinned, corpse will despawn next update
            if (loot.loot_type == LootType.Skinning)
                m_corpseRemoveTime = now;
            else
                m_corpseRemoveTime = now + (uint)(m_corpseDelay * decayRate);

            m_respawnTime = m_corpseRemoveTime + m_respawnDelay;
        }

        public bool HasScalableLevels()
        {
            CreatureTemplate cinfo = GetCreatureTemplate();
            return cinfo.levelScaling.HasValue;
        }

        ulong GetMaxHealthByLevel(uint level)
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureBaseStats stats = Global.ObjectMgr.GetCreatureBaseStats(level, cInfo.UnitClass);
            return stats.GenerateHealth(cInfo);
        }

        public override float GetHealthMultiplierForTarget(WorldObject target)
        {
            if (!HasScalableLevels())
                return 1.0f;

            uint levelForTarget = GetLevelForTarget(target);
            if (getLevel() < levelForTarget)
                return 1.0f;

            return (float)GetMaxHealthByLevel(levelForTarget) / GetCreateHealth();
        }

        float GetBaseDamageForLevel(uint level)
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureBaseStats stats = Global.ObjectMgr.GetCreatureBaseStats(level, cInfo.UnitClass);
            return stats.GenerateBaseDamage(cInfo);
        }

        public override float GetDamageMultiplierForTarget(WorldObject target)
        {
            if (!HasScalableLevels())
                return 1.0f;

            uint levelForTarget = GetLevelForTarget(target);

            return GetBaseDamageForLevel(levelForTarget) / GetBaseDamageForLevel(getLevel());
        }

        float GetBaseArmorForLevel(uint level)
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureBaseStats stats = Global.ObjectMgr.GetCreatureBaseStats(level, cInfo.UnitClass);
            return stats.GenerateArmor(cInfo);
        }

        public override float GetArmorMultiplierForTarget(WorldObject target)
        {
            if (!HasScalableLevels())
                return 1.0f;

            uint levelForTarget = GetLevelForTarget(target);

            return GetBaseArmorForLevel(levelForTarget) / GetBaseArmorForLevel(getLevel());
        }

        public override uint GetLevelForTarget(WorldObject target)
        {
            Unit unitTarget = target.ToUnit();
            if (unitTarget)
            {
                if (isWorldBoss())
                {
                    int level = (int)(unitTarget.getLevel() + WorldConfig.GetIntValue(WorldCfg.WorldBossLevelDiff));
                    return (uint)MathFunctions.RoundToInterval(ref level, 1u, 255u);
                }

                // If this creature should scale level, adapt level depending of target level
                // between UNIT_FIELD_SCALING_LEVEL_MIN and UNIT_FIELD_SCALING_LEVEL_MAX
                if (HasScalableLevels())
                {
                    int targetLevelWithDelta = ((int)unitTarget.getLevel() + GetInt32Value(UnitFields.ScalingLevelDelta));

                    if (target.IsPlayer())
                        targetLevelWithDelta += target.GetInt32Value(ActivePlayerFields.ScalingPlayerLevelDelta);

                    return (uint)MathFunctions.RoundToInterval(ref targetLevelWithDelta, GetInt32Value(UnitFields.ScalingLevelMin), GetInt32Value(UnitFields.ScalingLevelMax));
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
                return creatureData.ScriptId;

            return Global.ObjectMgr.GetCreatureTemplate(GetEntry()).ScriptID;
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

            long ptime = Time.UnixTime;

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

            long ptime = Time.UnixTime;

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

        public override string GetName(LocaleConstant locale_idx = LocaleConstant.enUS)
        {
            if (locale_idx != LocaleConstant.enUS)
            {
                CreatureLocale cl = Global.ObjectMgr.GetCreatureLocale(GetEntry());
                if (cl != null)
                {
                    if (cl.Name.Length > (byte)locale_idx && !string.IsNullOrEmpty(cl.Name[(byte)locale_idx]))
                        return cl.Name[(byte)locale_idx];
                }
            }

            return base.GetName(locale_idx);
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
            float range = SharedConst.MeleeRange;

            for (byte i = 0; i < GetPetAutoSpellSize(); ++i)
            {
                uint spellID = GetPetAutoSpellOnPos(i);
                if (spellID == 0)
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellID);
                if (spellInfo != null)
                {
                    if (spellInfo.GetRecoveryTime() == 0  // No cooldown
                        && spellInfo.RangeEntry.Id != 1 /*Self*/ && spellInfo.RangeEntry.Id != 2 /*Combat Range*/
                        && spellInfo.GetMinRange() > range)
                        range = spellInfo.GetMinRange();
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

        public void SetPosition(float x, float y, float z, float o)
        {
            // prevent crash when a bad coord is sent by the client
            if (!GridDefines.IsValidMapCoord(x, y, z, o))
            {
                Log.outDebug(LogFilter.Unit, "Creature.SetPosition({0}, {1}, {2}) .. bad coordinates!", x, y, z);
                return;
            }

            GetMap().CreatureRelocation(ToCreature(), x, y, z, o);
            if (IsVehicle())
                GetVehicleKit().RelocatePassengers();
        }

        public bool IsDungeonBoss()
        {
            CreatureTemplate cinfo = Global.ObjectMgr.GetCreatureTemplate(GetEntry());
            return cinfo != null && (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.DungeonBoss));
        }

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
            float ground = GetMap().GetHeight(GetPhaseShift(), GetPositionX(), GetPositionY(), GetPositionZMinusOffset());

            bool isInAir = (MathFunctions.fuzzyGt(GetPositionZMinusOffset(), ground + 0.05f) || MathFunctions.fuzzyLt(GetPositionZMinusOffset(), ground - 0.05f)); // Can be underground too, prevent the falling

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

            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelInfo(GetDisplayId());
            if (minfo != null)
            {
                SetFloatValue(UnitFields.BoundingRadius, (IsPet() ? 1.0f : minfo.BoundingRadius) * scale);
                SetFloatValue(UnitFields.CombatReach, (IsPet() ? SharedConst.DefaultCombatReach : minfo.CombatReach) * scale);
            }
        }

        public override void SetDisplayId(uint modelId, float displayScale = 1f)
        {
            base.SetDisplayId(modelId, displayScale);

            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelInfo(modelId);
            if (minfo != null)
            {
                SetFloatValue(UnitFields.BoundingRadius, (IsPet() ? 1.0f : minfo.BoundingRadius) * GetObjectScale());
                SetFloatValue(UnitFields.CombatReach, (IsPet() ? SharedConst.DefaultCombatReach : minfo.CombatReach) * GetObjectScale());
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
                SetGuidValue(UnitFields.Target, guid);
        }

        public void FocusTarget(Spell focusSpell, WorldObject target)
        {
            // already focused
            if (_focusSpell != null)
                return;

            // don't use spell focus for vehicle spells
            if (focusSpell.GetSpellInfo().HasAura(Difficulty.None, AuraType.ControlVehicle))
                return;

            if ((!target || target == this) && focusSpell.GetCastTime() == 0) // instant cast, untargeted (or self-targeted) spell doesn't need any facing updates
                return;

            // store pre-cast values for target and orientation (used to later restore)
            if (!IsFocusing(null, true))
            { // only overwrite these fields if we aren't transitioning from one spell focus to another
                m_suppressedTarget = GetGuidValue(UnitFields.Target);
                m_suppressedOrientation = GetOrientation();
            }

            _focusSpell = focusSpell;

            // set target, then force send update packet to players if it changed to provide appropriate facing
            ObjectGuid newTarget = target ? target.GetGUID() : ObjectGuid.Empty;
            if (GetGuidValue(UnitFields.Target) != newTarget)
            {
                SetGuidValue(UnitFields.Target, newTarget);

                // here we determine if the (relatively expensive) forced update is worth it, or whether we can afford to wait until the scheduled update tick
                // only require instant update for spells that actually have a visual
                if (focusSpell.GetSpellInfo().GetSpellVisual() != 0 && (focusSpell.GetCastTime() == 0 || // if the spell is instant cast
                   focusSpell.GetSpellInfo().HasAttribute(SpellAttr5.DontTurnDuringCast))) // client gets confused if we attempt to turn at the regularly scheduled update packet
                {
                    List<Player> playersNearby = GetPlayerListInGrid(GetVisibilityRange());
                    foreach (var player in playersNearby)
                    {
                        // only update players that are known to the client (have already been created)
                        if (player.HaveAtClient(this))
                            SendUpdateToPlayer(player);
                    }
                }
            }

            bool canTurnDuringCast = !focusSpell.GetSpellInfo().HasAttribute(SpellAttr5.DontTurnDuringCast);
            // Face the target - we need to do this before the unit state is modified for no-turn spells
            if (target)
                SetFacingToObject(target);
            else if (!canTurnDuringCast)
            {
                Unit victim = GetVictim();
                if (victim)
                    SetFacingToObject(victim); // ensure server-side orientation is correct at beginning of cast
            }

            if (!canTurnDuringCast)
                AddUnitState(UnitState.CannotTurn);
        }

        public bool IsFocusing(Spell focusSpell = null, bool withDelay = false)
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
                SetGuidValue(UnitFields.Target, m_suppressedTarget);
                if (!m_suppressedTarget.IsEmpty())
                {
                    WorldObject objTarget = Global.ObjAccessor.GetWorldObject(this, m_suppressedTarget);
                    if (objTarget)
                        SetFacingToObject(objTarget);
                }
                else
                    SetFacingTo(m_suppressedOrientation);
            }
            else
                // tell the creature that it should reacquire its actual target after the delay expires (this is handled in ::Update)
                // player pets don't need to do this, as they automatically reacquire their target on focus release
                MustReacquireTarget();

            if (_focusSpell.GetSpellInfo().HasAttribute(SpellAttr5.DontTurnDuringCast))
                ClearUnitState(UnitState.CannotTurn);

            _focusSpell = null;
            _focusDelay = (!IsPet() && withDelay) ? Time.GetMSTime() : 0; // don't allow re-target right away to prevent visual bugs
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

        public new CreatureAI GetAI()
        {
            return (CreatureAI)i_AI;
        }

        public T GetAI<T>() where T : UnitAI
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

        public override bool LoadFromDB(ulong spawnId, Map map)
        {
            return LoadCreatureFromDB(spawnId, map, false, false);
        }

        public bool LoadCreatureFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate)
        {
            if (!allowDuplicate)
            {
                // If an alive instance of this spawnId is already found, skip creation
                // If only dead instance(s) exist, despawn them and spawn a new (maybe also dead) version
                var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(spawnId);
                List<Creature> despawnList = new List<Creature>();

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
                Log.outError(LogFilter.Sql, "Creature (GUID: {0}) not found in table `creature`, can't load. ", spawnId);
                return false;
            }

            m_spawnId = spawnId;
            m_creatureData = data;
            if (!Create(map.GenerateLowGuid(HighGuid.Creature), map, data.id, data.posX, data.posY, data.posZ, data.orientation, data, 0))
                return false;

            //We should set first home position, because then AI calls home movement
            SetHomePosition(data.posX, data.posY, data.posZ, data.orientation);

            m_respawnradius = data.spawndist;

            m_respawnDelay = data.spawntimesecs;
            m_deathState = DeathState.Alive;

            m_respawnTime = GetMap().GetCreatureRespawnTime(m_spawnId);

            // Is the creature script objecting to us spawning? If yes, delay by one second (then re-check in ::Update)
            if (m_respawnTime == 0 && !Global.ScriptMgr.CanSpawn(spawnId, GetEntry(), GetCreatureTemplate(), GetCreatureData(), map))
                m_respawnTime = Time.UnixTime + 1;

            if (m_respawnTime != 0)                          // respawn on Update
            {
                m_deathState = DeathState.Dead;
                if (CanFly())
                {
                    float tz = map.GetHeight(GetPhaseShift(), data.posX, data.posY, data.posZ, true, MapConst.MaxFallDistance);
                    if (data.posZ - tz > 0.1f && GridDefines.IsValidMapCoord(tz))
                        Relocate(data.posX, data.posY, tz);
                }
            }

            SetSpawnHealth();

            m_defaultMovementType = (MovementGeneratorType)data.movementType;

            loot.SetGUID(ObjectGuid.Create(HighGuid.LootObject, data.mapid, data.id, GetMap().GenerateLowGuid(HighGuid.LootObject)));

            if (addToMap && !GetMap().AddToMap(this))
                return false;
            return true;
        }

        public bool hasLootRecipient() { return !m_lootRecipient.IsEmpty() || !m_lootRecipientGroup.IsEmpty(); }

        public LootModes GetLootMode() { return m_LootMode; }
        public bool HasLootMode(LootModes lootMode) { return Convert.ToBoolean(m_LootMode & lootMode); }
        public void SetLootMode(LootModes lootMode) { m_LootMode = lootMode; }
        public void AddLootMode(LootModes lootMode) { m_LootMode |= lootMode; }
        public void RemoveLootMode(LootModes lootMode) { m_LootMode &= ~lootMode; }
        public void ResetLootMode() { m_LootMode = LootModes.Default; }

        public void SetNoCallAssistance(bool val) { m_AlreadyCallAssistance = val; }
        public void SetNoSearchAssistance(bool val) { m_AlreadySearchedAssistance = val; }
        public bool HasSearchedAssistance() { return m_AlreadySearchedAssistance; }

        MovementGeneratorType GetDefaultMovementType() { return m_defaultMovementType; }
        public void SetDefaultMovementType(MovementGeneratorType mgt) { m_defaultMovementType = mgt; }

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

        bool isRegeneratingHealth() { return m_regenHealth; }
        public void setRegeneratingHealth(bool regenHealth) { m_regenHealth = regenHealth; }

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

        public uint GetWaypointPath() { return m_path_id; }
        public void LoadPath(uint pathid) { m_path_id = pathid; }

        public uint GetCurrentWaypointID() { return m_waypointID; }
        public void UpdateWaypointID(uint wpID) { m_waypointID = wpID; }

        public CreatureGroup GetFormation() { return m_formation; }
        public void SetFormation(CreatureGroup formation) { m_formation = formation; }

        void SetDisableReputationGain(bool disable) { DisableReputationGain = disable; }
        public bool IsReputationGainDisabled() { return DisableReputationGain; }
        public bool IsDamageEnoughForLootingAndReward() { return m_creatureInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoPlayerDamageReq) || m_PlayerDamageReq == 0; }

        public void ResetPlayerDamageReq() { m_PlayerDamageReq = (uint)(GetHealth() / 2); }

        public uint GetOriginalEntry()
        {
            return m_originalEntry;
        }
        void SetOriginalEntry(uint entry)
        {
            m_originalEntry = entry;
        }

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
                Unit caster = tauntAuras.Last().GetCaster();

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
                        if (caster != null && CanSeeOrDetect(caster, true) && IsValidAttackTarget(caster) && caster.isInAccessiblePlaceFor(ToCreature()))
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
                if (target == null && !GetThreatManager().isThreatListEmpty())
                    // No taunt aura or taunt aura caster is dead standard target selection
                    target = GetThreatManager().getHostilTarget();
            }
            else if (!HasReactState(ReactStates.Passive))
            {
                // We have player pet probably
                target = getAttackerForHelper();
                if (target == null && IsSummon())
                {
                    Unit owner = ToTempSummon().GetOwner();
                    if (owner != null)
                    {
                        if (owner.IsInCombat())
                            target = owner.getAttackerForHelper();
                        if (target == null)
                        {
                            foreach (var unit in owner.m_Controlled)
                            {
                                if (unit.IsInCombat())
                                {
                                    target = unit.getAttackerForHelper();
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
                if (!IsFocusing())
                    SetInFront(target);
                return target;
            }

            // last case when creature must not go to evade mode:
            // it in combat but attacker not make any damage and not enter to aggro radius to have record in threat list
            // for example at owner command to pet attack some far away creature
            // Note: creature does not have targeted movement generator but has attacker in this case
            foreach (var unit in attackerList)
            {
                if (!CanCreatureAttack(unit) && !unit.IsTypeId(TypeId.Player)
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
                        assistant.CombatStart(victim);
                        if (assistant.IsAIEnabled)
                            assistant.GetAI().AttackStart(victim);
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
        public ForcedDespawnDelayEvent(Creature owner, TimeSpan respawnTimer = default(TimeSpan))
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
