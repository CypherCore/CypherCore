// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Game.AI;
using Game.DataStorage;
using Game.Groups;
using Game.Loots;
using Game.Maps;
using Game.Networking;
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

        public void SignalFormationMovement()
        {
            if (m_formation == null)
                return;

            if (!m_formation.IsLeader(this))
                return;

            m_formation.LeaderStartedMoving();
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
                _loot = null;
                uint respawnDelay = m_respawnDelay;
                CreatureAI ai = GetAI();
                if (ai != null)
                    ai.CorpseRemoved(respawnDelay);

                if (destroyForNearbyPlayers)
                    UpdateObjectVisibilityOnDestroy();

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
                CreatureAI ai = GetAI();
                if (ai != null)
                    ai.CorpseRemoved(m_respawnDelay);

                // In case this is called directly and normal respawn timer not set
                // Since this timer will be longer than the already present time it
                // will be ignored if the correct place added a respawn timer
                if (setSpawnTime)
                {
                    uint respawnDelay = m_respawnDelay;
                    m_respawnTime = Math.Max(GameTime.GetGameTime() + respawnDelay, m_respawnTime);

                    SaveRespawnTime();
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
            CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (creatureInfo == null)
            {
                Log.outError(LogFilter.Sql, "Creature.InitEntry creature entry {0} does not exist.", entry);
                return false;
            }

            m_creatureInfo = creatureInfo;
            SetEntry(entry);
            m_creatureDifficulty = creatureInfo.GetDifficulty(!IsPet() ? GetMap().GetDifficultyID() : Difficulty.None);

            // equal to player Race field, but creature does not have race
            SetRace(0);
            SetClass((Class)creatureInfo.UnitClass);

            // Cancel load if no model defined
            if (creatureInfo.GetFirstValidModel() == null)
            {
                Log.outError(LogFilter.Sql, "Creature (Entry: {0}) has no model defined in table `creature_template`, can't load. ", entry);
                return false;
            }

            CreatureModel model = ObjectManager.ChooseDisplayId(creatureInfo, data);
            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelRandomGender(ref model, creatureInfo);
            if (minfo == null)                                             // Cancel load if no model defined
            {
                Log.outError(LogFilter.Sql, "Creature (Entry: {0}) has invalid model {1} defined in table `creature_template_model`, can't load.", entry, model.CreatureDisplayID);
                return false;
            }

            SetDisplayId(model.CreatureDisplayID, true);

            // Load creature equipment
            if (data == null)
                LoadEquipment();  // use default equipment (if available) for summons
            else if (data.equipmentId == 0)
                LoadEquipment(0); // 0 means no equipment for creature table
            else
            {
                m_originalEquipmentId = data.equipmentId;
                LoadEquipment(data.equipmentId);
            }

            SetName(creatureInfo.Name);                              // at normal entry always

            SetModCastingSpeed(1.0f);
            SetModSpellHaste(1.0f);
            SetModHaste(1.0f);
            SetModRangedHaste(1.0f);
            SetModHasteRegen(1.0f);
            SetModTimeRate(1.0f);
            SetSpellEmpowerStage(-1);

            SetSpeedRate(UnitMoveType.Walk, creatureInfo.SpeedWalk);
            SetSpeedRate(UnitMoveType.Run, creatureInfo.SpeedRun);
            SetSpeedRate(UnitMoveType.Swim, 1.0f);      // using 1.0 rate
            SetSpeedRate(UnitMoveType.Flight, 1.0f);    // using 1.0 rate

            SetObjectScale(GetNativeObjectScale());

            SetCanDualWield(creatureInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.UseOffhandAttack));

            // checked at loading
            DefaultMovementType = (MovementGeneratorType)(data != null ? data.movementType : creatureInfo.MovementType);
            if (m_wanderDistance == 0 && DefaultMovementType == MovementGeneratorType.Random)
                DefaultMovementType = MovementGeneratorType.Idle;

            for (byte i = 0; i < SharedConst.MaxCreatureSpells; ++i)
                m_spells[i] = GetCreatureTemplate().Spells[i];

            CreatureStaticFlagsHolder staticFlags = GenerateStaticFlags(m_creatureDifficulty, GetSpawnId(), GetMap().GetDifficultyID());
            ApplyAllStaticFlags(staticFlags);

            _staticFlags.ApplyFlag(CreatureStaticFlags.NoXp, creatureInfo.CreatureType == CreatureType.Critter || IsPet() || IsTotem() || creatureInfo.FlagsExtra.HasFlag(CreatureFlagsExtra.NoXP));

            // TODO: migrate these in DB
            _staticFlags.ApplyFlag(CreatureStaticFlags2.AllowMountedCombat, GetCreatureDifficulty().TypeFlags.HasFlag(CreatureTypeFlags.AllowMountedCombat));
            SetIgnoreFeignDeath(creatureInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.IgnoreFeighDeath));
            SetInteractionAllowedInCombat(GetCreatureDifficulty().TypeFlags.HasAnyFlag(CreatureTypeFlags.AllowInteractionWhileInCombat));
            SetTreatAsRaidUnit(GetCreatureDifficulty().TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit));

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

            ObjectManager.ChooseCreatureFlags(cInfo, out ulong npcFlags, out uint unitFlags, out uint unitFlags2, out uint unitFlags3, _staticFlags, data);

            if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Worldevent))
                npcFlags |= Global.GameEventMgr.GetNPCFlag(this);

            if (IsVendor() && !npcFlags.HasAnyFlag((ulong)NPCFlags.VendorMask))
                SetVendor(NPCFlags.VendorMask, false);

            ReplaceAllNpcFlags((NPCFlags)(npcFlags & 0xFFFFFFFF));
            ReplaceAllNpcFlags2((NPCFlags2)(npcFlags >> 32));

            if (npcFlags.HasAnyFlag((ulong)NPCFlags.VendorMask))
                SetVendor((NPCFlags)npcFlags & NPCFlags.VendorMask, true);

            SetPetitioner(npcFlags.HasAnyFlag((ulong)NPCFlags.Petitioner));

            // if unit is in combat, keep this flag
            unitFlags &= ~(uint)UnitFlags.InCombat;
            if (IsInCombat())
                unitFlags |= (uint)UnitFlags.InCombat;

            ReplaceAllUnitFlags((UnitFlags)unitFlags);
            ReplaceAllUnitFlags2((UnitFlags2)unitFlags2);
            ReplaceAllUnitFlags3((UnitFlags3)unitFlags3);

            ReplaceAllDynamicFlags(UnitDynFlags.None);

            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.StateWorldEffectsQuestObjectiveID), data != null ? data.spawnTrackingQuestObjectiveId : 0);

            SetCanDualWield(cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.UseOffhandAttack));

            SetBaseAttackTime(WeaponAttackType.BaseAttack, cInfo.BaseAttackTime);
            SetBaseAttackTime(WeaponAttackType.OffAttack, cInfo.BaseAttackTime);
            SetBaseAttackTime(WeaponAttackType.RangedAttack, cInfo.RangeAttackTime);

            if (updateLevel)
                SelectLevel();
            else if (!IsGuardian())
            {
                ulong previousHealth = GetHealth();
                UpdateLevelDependantStats(); // We still re-initialize level dependant stats on entry update
                if (previousHealth > 0)
                    SetHealth(previousHealth);
            }

            // Do not update guardian stats here - they are handled in Guardian::InitStatsForLevel()
            if (!IsGuardian())
            {
                SetMeleeDamageSchool((SpellSchools)cInfo.DmgSchool);
                SetStatFlatModifier(UnitMods.ResistanceHoly, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Holy]);
                SetStatFlatModifier(UnitMods.ResistanceFire, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Fire]);
                SetStatFlatModifier(UnitMods.ResistanceNature, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Nature]);
                SetStatFlatModifier(UnitMods.ResistanceFrost, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Frost]);
                SetStatFlatModifier(UnitMods.ResistanceShadow, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Shadow]);
                SetStatFlatModifier(UnitMods.ResistanceArcane, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Arcane]);

                SetCanModifyStats(true);
                UpdateAllStats();
            }

            // checked and error show at loading templates
            var factionTemplate = CliDB.FactionTemplateStorage.LookupByKey(cInfo.Faction);
            if (factionTemplate != null)
            {
                SetPvP(factionTemplate.HasFlag(FactionTemplateFlags.PVP));
                if (IsTaxi())
                {
                    uint taxiNodesId = Global.ObjectMgr.GetNearestTaxiNode(GetPositionX(), GetPositionY(), GetPositionZ(), GetMapId(),
                        (factionTemplate.FactionGroup & (byte)FactionMasks.Alliance) != 0 ? Team.Alliance : Team.Horde);
                    SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.TaxiNodesID), (int)taxiNodesId);
                }
            }

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
                SetUninteractible(true);

            if (HasNpcFlag(NPCFlags.SpellClick))
                InitializeInteractSpellId();

            InitializeReactState();

            if (Convert.ToBoolean(cInfo.FlagsExtra & CreatureFlagsExtra.NoTaunt))
            {
                ApplySpellImmune(0, SpellImmunity.State, AuraType.ModTaunt, true);
                ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.AttackMe, true);
            }

            SetIsCombatDisallowed(cInfo.FlagsExtra.HasFlag(CreatureFlagsExtra.CannotEnterCombat));

            InitializeMovementCapabilities();

            LoadCreaturesAddon();
            LoadCreaturesSparringHealth(true);
            LoadTemplateImmunities(cInfo.CreatureImmunitiesId);
            GetThreatManager().EvaluateSuppressed();

            //We must update last scriptId or it looks like we reloaded a script, breaking some things such as gossip temporarily
            LastUsedScriptID = GetScriptId();

            m_stringIds[(int)StringIdType.Template] = cInfo.StringId;

            return true;
        }

        /// <summary>
        /// Draws data from m_creatureDifficulty and spawn/difficulty based override data and returns a CreatureStaticFlagsHolder value which contains the data of both
        /// </summary>
        /// <param name="creatureDifficulty"></param>
        /// <param name="spawnId"></param>
        /// <param name="difficultyId"></param>
        /// <returns></returns>
        CreatureStaticFlagsHolder GenerateStaticFlags(CreatureDifficulty creatureDifficulty, ulong spawnId, Difficulty difficultyId)
        {
            CreatureStaticFlagsOverride staticFlagsOverride = Global.ObjectMgr.GetCreatureStaticFlagsOverride(spawnId, difficultyId);
            if (staticFlagsOverride == null)
                return creatureDifficulty.StaticFlags;

            return new CreatureStaticFlagsHolder(
                staticFlagsOverride.StaticFlags1.GetValueOrDefault(creatureDifficulty.StaticFlags.GetFlags()),
                staticFlagsOverride.StaticFlags2.GetValueOrDefault(creatureDifficulty.StaticFlags.GetFlags2()),
                staticFlagsOverride.StaticFlags3.GetValueOrDefault(creatureDifficulty.StaticFlags.GetFlags3()),
                staticFlagsOverride.StaticFlags4.GetValueOrDefault(creatureDifficulty.StaticFlags.GetFlags4()),
                staticFlagsOverride.StaticFlags5.GetValueOrDefault(creatureDifficulty.StaticFlags.GetFlags5()),
                staticFlagsOverride.StaticFlags6.GetValueOrDefault(creatureDifficulty.StaticFlags.GetFlags6()),
                staticFlagsOverride.StaticFlags7.GetValueOrDefault(creatureDifficulty.StaticFlags.GetFlags7()),
                staticFlagsOverride.StaticFlags8.GetValueOrDefault(creatureDifficulty.StaticFlags.GetFlags8()));
        }

        void ApplyAllStaticFlags(CreatureStaticFlagsHolder flags)
        {
            _staticFlags = flags;

            // Apply all other side effects of flag changes
            m_updateFlag.NoBirthAnim = flags.HasFlag(CreatureStaticFlags4.NoBirthAnim);
        }

        public override void Update(uint diff)
        {
            if (IsAIEnabled() && triggerJustAppeared && m_deathState != DeathState.Dead)
            {
                if (IsAreaSpiritHealer() && !IsAreaSpiritHealerIndividual())
                    CastSpell(null, BattlegroundConst.SpellSpiritHealChannelAoE, false);

                if (m_respawnCompatibilityMode && VehicleKit != null)
                    VehicleKit.Reset();

                triggerJustAppeared = false;
                GetAI().JustAppeared();
            }

            UpdateMovementCapabilities();

            GetThreatManager().Update(diff);

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

                    if (IsEngaged())
                        AIUpdateTick(diff);

                    _loot?.Update();

                    foreach (var (playerOwner, loot) in m_personalLoot)
                        loot.Update();

                    if (m_corpseRemoveTime <= GameTime.GetGameTime())
                    {
                        RemoveCorpse(false);
                        Log.outDebug(LogFilter.Unit, "Removing corpse... {0} ", GetEntry());
                    }
                    break;
                case DeathState.Alive:
                    base.Update(diff);

                    if (!IsAlive())
                        break;

                    if (_spellFocusInfo.Delay != 0)
                    {
                        if (_spellFocusInfo.Delay <= diff)
                            ReacquireSpellFocusTarget();
                        else
                            _spellFocusInfo.Delay -= diff;
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

                    AIUpdateTick(diff);

                    DoMeleeAttackIfReady();

                    // creature can be dead after UpdateAI call
                    // CORPSE/DEAD state will processed at next tick (in other case death timer will be updated unexpectedly)
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
                        if (!IsInEvadeMode())
                        {
                            // regenerate health if not in combat or if polymorphed)
                            if (!IsEngaged() || IsPolymorphed())
                                RegenerateHealth();
                            else if (CanNotReachTarget())
                            {
                                // regenerate health if cannot reach the target and the setting is set to do so.
                                // this allows to disable the health regen of raid bosses if pathfinding has issues for whatever reason
                                if (WorldConfig.GetBoolValue(WorldCfg.RegenHpCannotReachTargetInRaid) || !GetMap().IsRaid())
                                {
                                    RegenerateHealth();
                                    Log.outDebug(LogFilter.Unit, $"RegenerateHealth() enabled because Creature cannot reach the target. Detail: {GetDebugInfo()}");
                                }
                                else
                                    Log.outDebug(LogFilter.Unit, $"RegenerateHealth() disabled even if the Creature cannot reach the target. Detail: {GetDebugInfo()}");
                            }
                        }

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

        public override void Heartbeat()
        {
            base.Heartbeat();

            // Creatures with CREATURE_STATIC_FLAG_2_FORCE_PARTY_MEMBERS_INTO_COMBAT periodically force party members into combat
            ForcePartyMembersIntoCombat();

            // creatures should only attack surroundings initially after heartbeat has passed or until attacked
            if (!_aggroGracePeriodExpired)
            {
                _aggroGracePeriodExpired = true;

                // trigger MoveInLineOfSight
                CreatureAggroGracePeriodExpiredNotifier notifier = new(this);
                Cell.VisitAllObjects(this, notifier, GetVisibilityRange());
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
            if (GetVictim() == null)
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

                if (creature == null)
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
                PhasingHandler.InitDbPhaseShift(GetPhaseShift(), data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
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

            CreatureDifficulty creatureDifficulty = cinfo.GetDifficulty(GetMap().GetDifficultyID());

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

            {
                // area/zone id is needed immediately for ZoneScript::GetCreatureEntry hook before it is known which creature template to load (no model/scale available yet)
                PositionFullTerrainStatus terrainStatus = new();
                GetMap().GetFullTerrainStatusForPosition(GetPhaseShift(), GetPositionX(), GetPositionY(), GetPositionZ(), terrainStatus);
                ProcessPositionDataChanged(terrainStatus);
            }

            // Allow players to see those units while dead, do it here (mayby altered by addon auras)
            if (creatureDifficulty.TypeFlags.HasAnyFlag(CreatureTypeFlags.VisibleToGhosts))
                m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive | GhostVisibilityType.Ghost);

            if (!CreateFromProto(guidlow, entry, data, vehId))
                return false;

            cinfo = GetCreatureTemplate(); // might be different than initially requested
            if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.DungeonBoss) && map.IsDungeon())
                m_respawnDelay = 0; // special value, prevents respawn for dungeon bosses unless overridden

            switch (GetCreatureClassification())
            {
                case CreatureClassifications.Elite:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayElite);
                    break;
                case CreatureClassifications.RareElite:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayRareelite);
                    break;
                case CreatureClassifications.Obsolete:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayObsolete);
                    break;
                case CreatureClassifications.Rare:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayRare);
                    break;
                case CreatureClassifications.Trivial:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayTrivial);
                    break;
                case CreatureClassifications.MinusMob:
                    m_corpseDelay = WorldConfig.GetUIntValue(WorldCfg.CorpseDecayMinusMob);
                    break;
            }

            LoadCreaturesAddon();
            LoadCreaturesSparringHealth(true);

            //! Need to be called after LoadCreaturesAddon - MOVEMENTFLAG_HOVER is set there
            posZ += GetHoverOffset();

            LastUsedScriptID = GetScriptId();

            if (IsSpiritHealer() || IsAreaSpiritHealer() || GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.GhostVisibility))
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
                if (target == null && IsSummon())
                {
                    Unit owner = ToTempSummon().GetOwner();
                    if (owner != null)
                    {
                        if (owner.IsInCombat())
                            target = owner.GetAttackerForHelper();
                        if (target == null)
                        {
                            foreach (var itr in owner.m_Controlled)
                            {
                                if (itr.IsInCombat())
                                {
                                    target = itr.GetAttackerForHelper();
                                    if (target != null)
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
                if (!HasSpellFocus())
                    SetInFront(target);
                return target;
            }

            /// @todo a vehicle may eat some mob, so mob should not evade
            if (GetVehicle() != null)
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
            if (IsTotem() || IsTrigger() || IsCritter() || IsSpiritService() || _staticFlags.HasFlag(CreatureStaticFlags.IgnoreCombat))
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
            return !_staticFlags.HasFlag(CreatureStaticFlags.NoXp);
        }

        public void SetCanGiveExperience(bool xpEnabled) { _staticFlags.ApplyFlag(CreatureStaticFlags.NoXp, !xpEnabled); }

        public override bool IsEngaged()
        {
            CreatureAI ai = GetAI();
            if (ai != null)
                return ai.IsEngaged();
            return false;
        }

        public override void AtEngage(Unit target)
        {
            base.AtEngage(target);

            _aggroGracePeriodExpired = true;

            GetThreatManager().ResetUpdateTimer();

            if (!HasFlag(CreatureStaticFlags2.AllowMountedCombat))
                Dismount();

            if (IsPet() || IsGuardian()) // update pets' speed for catchup OOC speed
            {
                UpdateSpeed(UnitMoveType.Run);
                UpdateSpeed(UnitMoveType.Swim);
                UpdateSpeed(UnitMoveType.Flight);
            }

            MovementGeneratorType movetype = GetMotionMaster().GetCurrentMovementGeneratorType();
            if (movetype == MovementGeneratorType.Waypoint || movetype == MovementGeneratorType.Point || (IsAIEnabled() && GetAI().IsEscorted()))
            {
                SetHomePosition(GetPosition());
                // if its a vehicle, set the home positon of every creature passenger at engage
                // so that they are in combat range if hostile
                Vehicle vehicle = GetVehicleKit();
                if (vehicle != null)
                {
                    foreach (var (_, seat) in vehicle.Seats)
                    {
                        Unit passenger = Global.ObjAccessor.GetUnit(this, seat.Passenger.Guid);
                        if (passenger != null)
                        {
                            Creature creature = passenger.ToCreature();
                            if (creature != null)
                                creature.SetHomePosition(GetPosition());
                        }
                    }
                }
            }

            CreatureAI ai = GetAI();
            if (ai != null)
                ai.JustEngagedWith(target);

            CreatureGroup formation = GetFormation();
            if (formation != null)
                formation.MemberEngagingTarget(this, target);

            // Creatures with CREATURE_STATIC_FLAG_2_FORCE_PARTY_MEMBERS_INTO_COMBAT periodically force party members into combat
            ForcePartyMembersIntoCombat();
        }

        public override void AtDisengage()
        {
            base.AtDisengage();

            ClearUnitState(UnitState.AttackPlayer);
            if (IsAlive() && HasDynamicFlag(UnitDynFlags.Tapped))
                RemoveDynamicFlag(UnitDynFlags.Tapped);

            if (IsPet() || IsGuardian()) // update pets' speed for catchup OOC speed
            {
                UpdateSpeed(UnitMoveType.Run);
                UpdateSpeed(UnitMoveType.Swim);
                UpdateSpeed(UnitMoveType.Flight);
            }
        }

        void ForcePartyMembersIntoCombat()
        {
            if (!_staticFlags.HasFlag(CreatureStaticFlags2.ForcePartyMembersIntoCombat) || !IsEngaged())
                return;

            List<Group> partiesToForceIntoCombat = new();
            foreach (var (_, combatReference) in GetCombatManager().GetPvECombatRefs())
            {
                if (combatReference.IsSuppressedFor(this))
                    continue;

                Player player = combatReference.GetOther(this)?.ToPlayer();
                if (player == null || player.IsGameMaster())
                    continue;

                Group group = player.GetGroup();
                if (group != null)
                    partiesToForceIntoCombat.Add(group);
            }

            foreach (Group partyToForceIntoCombat in partiesToForceIntoCombat)
            {
                for (GroupReference refe = partyToForceIntoCombat.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player player = refe.GetSource();
                    if (player == null || !player.IsInWorld || player.GetMap() != GetMap() || player.IsGameMaster())
                        continue;

                    EngageWithTarget(player);
                }
            }
        }

        public bool IsEscorted()
        {
            CreatureAI ai = GetAI();
            if (ai != null)
                return ai.IsEscorted();

            return false;
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nAIName: {GetAIName()} ScriptName: {GetScriptName()} WaypointPath: {GetWaypointPathId()} SpawnId: {GetSpawnId()}";
        }

        public override void ExitVehicle(Position exitPosition = null)
        {
            base.ExitVehicle();

            // if the creature exits a vehicle, set it's home position to the
            // exited position so it won't run away (home) and evade if it's hostile
            SetHomePosition(GetPosition());
        }

        public void SummonGraveyardTeleporter()
        {
            if (!IsAreaSpiritHealer())
                return;

            uint npcEntry = GetFaction() == (uint)FactionTemplates.AllianceGeneric ? 26350 : 26351u;

            // maybe NPC is summoned with these spells:
            // ID - 24237 Summon Alliance Graveyard Teleporter (SERVERSIDE)
            // ID - 46894 Summon Horde Graveyard Teleporter (SERVERSIDE)
            SummonCreature(npcEntry, GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromSeconds(1), 0, 0);
        }

        void InitializeInteractSpellId()
        {
            var clickBounds = Global.ObjectMgr.GetSpellClickInfoMapBounds(GetEntry());
            // Set InteractSpellID if there is only one row in npc_spellclick_spells in db for this creature
            if (clickBounds.Count == 1)
                SetInteractSpellId((int)clickBounds[0].spellId);
            else
                SetInteractSpellId(0);
        }

        public bool HasFlag(CreatureStaticFlags flag) { return _staticFlags.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags2 flag) { return _staticFlags.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags3 flag) { return _staticFlags.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags4 flag) { return _staticFlags.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags5 flag) { return _staticFlags.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags6 flag) { return _staticFlags.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags7 flag) { return _staticFlags.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags8 flag) { return _staticFlags.HasFlag(flag); }

        public uint GetGossipMenuId()
        {
            return _gossipMenuId;
        }

        public void SetGossipMenuId(uint gossipMenuId)
        {
            _gossipMenuId = gossipMenuId;
        }

        public uint GetTrainerId()
        {
            if (_trainerId.HasValue)
                return _trainerId.Value;

            return Global.ObjectMgr.GetCreatureDefaultTrainer(GetEntry());
        }

        public void SetTrainerId(uint? trainerId)
        {
            _trainerId = trainerId;
        }

        public override bool IsMovementPreventedByCasting()
        {
            if (!base.IsMovementPreventedByCasting() && !HasSpellFocus())
                return false;

            return true;
        }

        public void StartPickPocketRefillTimer()
        {
            _pickpocketLootRestore = GameTime.GetGameTime() + WorldConfig.GetIntValue(WorldCfg.CreaturePickpocketRefill);
        }

        public void ResetPickPocketRefillTimer() { _pickpocketLootRestore = 0; }

        public bool CanGeneratePickPocketLoot() { return _pickpocketLootRestore <= GameTime.GetGameTime(); }

        public uint GetLootId()
        {
            if (_lootId.HasValue)
                return _lootId.Value;

            return GetCreatureDifficulty().LootID;
        }

        public void SetLootId(uint? lootId)
        {
            _lootId = lootId;
        }

        public void SetDontClearTapListOnEvade(bool dontClear)
        {
            // only temporary summons are allowed to not clear their tap list
            if (m_spawnId == 0)
                m_dontClearTapListOnEvade = dontClear;
        }

        public bool IsTapListNotClearedOnEvade() { return m_dontClearTapListOnEvade; }

        public void SetTappedBy(Unit unit, bool withGroup = true)
        {
            // set the player whose group should receive the right
            // to loot the creature after it dies
            // should be set to NULL after the loot disappears

            if (unit == null)
            {
                m_tapList.Clear();
                RemoveDynamicFlag(UnitDynFlags.Lootable | UnitDynFlags.Tapped);
                return;
            }

            if (m_tapList.Count >= SharedConst.CreatureTappersSoftCap)
                return;

            if (!unit.IsTypeId(TypeId.Player) && !unit.IsVehicle())
                return;

            Player player = unit.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (player == null)                                             // normal creature, no player involved
                return;

            m_tapList.Add(player.GetGUID());
            if (withGroup)
            {
                Group group = player.GetGroup();
                if (group != null)
                    for (var itr = group.GetFirstMember(); itr != null; itr = itr.Next())
                        if (GetMap().IsRaid() || group.SameSubGroup(player, itr.GetSource()))
                            m_tapList.Add(itr.GetSource().GetGUID());
            }

            if (m_tapList.Count >= SharedConst.CreatureTappersSoftCap)
                SetDynamicFlag(UnitDynFlags.Tapped);
        }

        public bool IsTappedBy(Player player)
        {
            return m_tapList.Contains(player.GetGUID());
        }

        public override Loot GetLootForPlayer(Player player)
        {
            if (m_personalLoot.Empty())
                return _loot;

            var loot = m_personalLoot.LookupByKey(player.GetGUID());
            if (loot != null)
                return loot;

            return null;
        }

        public bool IsFullyLooted()
        {
            if (_loot != null && !_loot.IsLooted())
                return false;

            foreach (var (_, loot) in m_personalLoot)
                if (!loot.IsLooted())
                    return false;

            return true;
        }

        public bool IsSkinnedBy(Player player)
        {
            Loot loot = GetLootForPlayer(player);
            if (loot != null)
                return loot.loot_type == LootType.Skinning;

            return false;
        }

        public HashSet<ObjectGuid> GetTapList() { return m_tapList; }
        public void SetTapList(HashSet<ObjectGuid> tapList) { m_tapList = tapList; }
        public bool HasLootRecipient() { return !m_tapList.Empty(); }

        public bool CanHaveLoot() { return !_staticFlags.HasFlag(CreatureStaticFlags.NoLoot); }

        public void SetCanHaveLoot(bool canHaveLoot) { _staticFlags.ApplyFlag(CreatureStaticFlags.NoLoot, !canHaveLoot); }

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

            uint mapId = GetMapId();
            ITransport transport = GetTransport();
            if (transport != null)
                if (transport.GetMapIdForSpawning() >= 0)
                    mapId = (uint)transport.GetMapIdForSpawning();

            SaveToDB(mapId, data.SpawnDifficulties);
        }

        public virtual void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties)
        {
            // update in loaded data
            if (m_spawnId == 0)
                m_spawnId = Global.ObjectMgr.GenerateCreatureSpawnId();

            CreatureData data = Global.ObjectMgr.NewOrExistCreatureData(m_spawnId);

            uint displayId = GetNativeDisplayId();
            ulong spawnNpcFlags = ((ulong)GetNpcFlags2() << 32) | (uint)GetNpcFlags();
            ulong? npcflag = null;
            uint? unitFlags = null;
            uint? unitFlags2 = null;
            uint? unitFlags3 = null;

            // check if it's a custom model and if not, use 0 for displayId
            CreatureTemplate cinfo = GetCreatureTemplate();
            if (cinfo != null)
            {
                foreach (CreatureModel model in cinfo.Models)
                    if (displayId != 0 && displayId == model.CreatureDisplayID)
                        displayId = 0;

                if (spawnNpcFlags != cinfo.Npcflag)
                    npcflag = spawnNpcFlags;

                if (m_unitData.Flags == (uint)cinfo.UnitFlags)
                    unitFlags = m_unitData.Flags;

                if (m_unitData.Flags2 == cinfo.UnitFlags2)
                    unitFlags2 = m_unitData.Flags2;

                if (m_unitData.Flags3 == cinfo.UnitFlags3)
                    unitFlags3 = m_unitData.Flags3;
            }

            if (data.SpawnId == 0)
                data.SpawnId = m_spawnId;
            Cypher.Assert(data.SpawnId == m_spawnId);

            data.Id = GetEntry();
            if (displayId != 0)
                data.display = new(displayId, SharedConst.DefaultPlayerDisplayScale, 1.0f);
            else
                data.display = null;
            data.equipmentId = (sbyte)GetCurrentEquipmentId();

            if (GetTransport() == null)
            {
                data.MapId = GetMapId();
                data.SpawnPoint.Relocate(this);
            }
            else
            {
                data.MapId = mapid;
                data.SpawnPoint.Relocate(GetTransOffsetX(), GetTransOffsetY(), GetTransOffsetZ(), GetTransOffsetO());
            }

            data.spawntimesecs = (int)m_respawnDelay;
            // prevent add data integrity problems
            data.WanderDistance = GetDefaultMovementType() == MovementGeneratorType.Idle ? 0.0f : m_wanderDistance;
            data.currentwaypoint = 0;
            data.curHealthPct = (uint)GetHealthPct();
            // prevent add data integrity problems
            data.movementType = (byte)(m_wanderDistance == 0 && GetDefaultMovementType() == MovementGeneratorType.Random
                ? MovementGeneratorType.Idle : GetDefaultMovementType());
            data.SpawnDifficulties = spawnDifficulties;
            data.npcflag = npcflag;
            data.unit_flags = unitFlags;
            data.unit_flags2 = unitFlags2;
            data.unit_flags3 = unitFlags3;
            if (data.spawnGroupData == null)
                data.spawnGroupData = Global.ObjectMgr.GetDefaultSpawnGroup();

            data.PhaseId = GetDBPhase() > 0 ? (uint)GetDBPhase() : data.PhaseId;
            data.PhaseGroup = GetDBPhase() < 0 ? (uint)-GetDBPhase() : data.PhaseGroup;

            // update in DB
            SQLTransaction trans = new();

            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_CREATURE);
            stmt.AddValue(0, m_spawnId);
            trans.Append(stmt);

            byte index = 0;

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.INS_CREATURE);
            stmt.AddValue(index++, m_spawnId);
            stmt.AddValue(index++, GetEntry());
            stmt.AddValue(index++, mapid);
            stmt.AddValue(index++, data.SpawnDifficulties.Empty() ? "" : string.Join(',', data.SpawnDifficulties));
            stmt.AddValue(index++, data.PhaseId);
            stmt.AddValue(index++, data.PhaseGroup);
            stmt.AddValue(index++, displayId);
            stmt.AddValue(index++, GetCurrentEquipmentId());
            stmt.AddValue(index++, GetPositionX());
            stmt.AddValue(index++, GetPositionY());
            stmt.AddValue(index++, GetPositionZ());
            stmt.AddValue(index++, GetOrientation());
            stmt.AddValue(index++, m_respawnDelay);
            stmt.AddValue(index++, m_wanderDistance);
            stmt.AddValue(index++, 0);
            stmt.AddValue(index++, (uint)GetHealthPct());
            stmt.AddValue(index++, (byte)GetDefaultMovementType());
            if (npcflag.HasValue)
                stmt.AddValue(index++, npcflag.Value);
            else
                stmt.AddNull(index++);

            if (unitFlags.HasValue)
                stmt.AddValue(index++, unitFlags.Value);
            else
                stmt.AddNull(index++);

            if (unitFlags2.HasValue)
                stmt.AddValue(index++, unitFlags2.Value);
            else
                stmt.AddNull(index++);

            if (unitFlags3.HasValue)
                stmt.AddValue(index++, unitFlags3.Value);
            else
                stmt.AddNull(index++);
            trans.Append(stmt);

            DB.World.CommitTransaction(trans);
        }

        public void SelectLevel()
        {
            // Level
            ApplyLevelScaling();
            int levelWithDelta = m_unitData.ScalingLevelMax + m_unitData.ScalingLevelDelta;
            byte level = (byte)MathFunctions.RoundToInterval(ref levelWithDelta, 1, SharedConst.StrongMaxLevel);
            SetLevel(level);

            UpdateLevelDependantStats();
        }

        public void UpdateLevelDependantStats()
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureClassifications classification = IsPet() ? CreatureClassifications.Normal : cInfo.Classification;
            uint level = GetLevel();
            CreatureBaseStats stats = Global.ObjectMgr.GetCreatureBaseStats(level, cInfo.UnitClass);

            // health
            float healthmod = GetHealthMod(classification);

            uint basehp = (uint)GetMaxHealthByLevel(level);
            uint health = (uint)(basehp * healthmod);

            SetCreateHealth(health);
            SetMaxHealth(health);
            SetHealth(health);
            ResetPlayerDamageReq();

            SetStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Base, health);

            // mana
            PowerType powerType = CalculateDisplayPowerType();
            SetCreateMana(stats.BaseMana);
            SetStatPctModifier(UnitMods.PowerStart + (int)powerType, UnitModifierPctType.Base, GetCreatureDifficulty().ManaModifier);
            SetPowerType(powerType, true, true);

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

            float armor = GetBaseArmorForLevel(level);
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

        public float GetHealthMod(CreatureClassifications classification)
        {
            switch (classification)                                           // define rates for each elite rank
            {
                case CreatureClassifications.Normal:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureHpNormal);
                case CreatureClassifications.Elite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureHpElite);
                case CreatureClassifications.RareElite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureHpRareelite);
                case CreatureClassifications.Obsolete:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureHpObsolete);
                case CreatureClassifications.Rare:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureHpRare);
                case CreatureClassifications.Trivial:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureHpTrivial);
                case CreatureClassifications.MinusMob:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureHpMinusmob);
                default:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureHpElite);
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

        public static float GetDamageMod(CreatureClassifications classification)
        {
            switch (classification)
            {
                case CreatureClassifications.Normal:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureDamageNormal);
                case CreatureClassifications.Elite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureDamageElite);
                case CreatureClassifications.RareElite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureDamageRareelite);
                case CreatureClassifications.Obsolete:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureDamageObsolete);
                case CreatureClassifications.Rare:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureDamageRare);
                case CreatureClassifications.Trivial:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureDamageTrivial);
                case CreatureClassifications.MinusMob:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureDamageMinusmob);
                default:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureDamageElite);
            }
        }

        public float GetSpellDamageMod(CreatureClassifications classification)
        {
            switch (classification)                                           // define rates for each elite rank
            {
                case CreatureClassifications.Normal:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureSpelldamageNormal);
                case CreatureClassifications.Elite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureSpelldamageElite);
                case CreatureClassifications.RareElite:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureSpelldamageRareelite);
                case CreatureClassifications.Obsolete:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureSpelldamageObsolete);
                case CreatureClassifications.Rare:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureSpelldamageRare);
                case CreatureClassifications.Trivial:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureSpelldamageTrivial);
                case CreatureClassifications.MinusMob:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureSpelldamageMinusmob);
                default:
                    return WorldConfig.GetFloatValue(WorldCfg.RateCreatureSpelldamageElite);
            }
        }

        float GetSparringHealthPct() { return _sparringHealthPct; }

        void SetInteractSpellId(int interactSpellId) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.InteractSpellID), interactSpellId); }

        public bool IsAggroGracePeriodExpired() { return _aggroGracePeriodExpired; }

        public void OverrideSparringHealthPct(List<float> healthPct)
        {
            _sparringHealthPct = healthPct.SelectRandom();
        }

        public uint CalculateDamageForSparring(Unit attacker, uint damage)
        {
            if (GetSparringHealthPct() == 0)
                return damage;

            if (attacker == null)
                return damage;

            if (!attacker.IsCreature() || attacker.IsCharmedOwnedByPlayerOrPlayer() || IsCharmedOwnedByPlayerOrPlayer())
                return damage;

            if (GetHealthPct() <= GetSparringHealthPct())
                return 0;

            uint sparringHealth = (uint)(GetMaxHealth() * GetSparringHealthPct() / 100);
            if (GetHealth() - damage <= sparringHealth)
                return (uint)(GetHealth() - sparringHealth);

            if (damage >= GetHealth())
                return (uint)(GetHealth() - 1);

            return damage;
        }

        public bool ShouldFakeDamageFrom(Unit attacker)
        {
            if (GetSparringHealthPct() == 0)
                return false;

            if (attacker == null)
                return false;

            if (!attacker.IsCreature())
                return false;

            if (attacker.IsCharmedOwnedByPlayerOrPlayer() || IsCharmedOwnedByPlayerOrPlayer())
                return false;

            if (GetHealthPct() > GetSparringHealthPct())
                return false;

            return true;
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
                if (CreateVehicleKit(vehId, entry, true))
                    UpdateDisplayPower();

            if (!IsPet())
            {
                uint vignetteId = GetCreatureTemplate().VignetteID;
                if (vignetteId != 0)
                    SetVignette(vignetteId);
            }

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
            SetHealth(CountPctFromMaxHealth(m_creatureData != null ? (int)m_creatureData.curHealthPct : 100));
            SetInitialPowerValue(GetPowerType());
        }

        public override bool HasQuest(uint questId)
        {
            return Global.ObjectMgr.GetCreatureQuestRelations(GetEntry()).HasQuest(questId);
        }

        public override bool HasInvolvedQuest(uint questId)
        {
            return Global.ObjectMgr.GetCreatureQuestInvolvedRelations(GetEntry()).HasQuest(questId);
        }

        public static bool DeleteFromDB(ulong spawnId)
        {
            CreatureData data = Global.ObjectMgr.GetCreatureData(spawnId);
            if (data == null)
                return false;

            SQLTransaction trans = new();

            Global.MapMgr.DoForAllMapsWithMapId(data.MapId, map =>
            {
                // despawn all active creatures, and remove their respawns
                List<Creature> toUnload = new();
                foreach (var creature in map.GetCreatureBySpawnIdStore().LookupByKey(spawnId))
                    toUnload.Add(creature);

                foreach (Creature creature in toUnload)
                    map.AddObjectToRemoveList(creature);

                map.RemoveRespawnTime(SpawnObjectType.Creature, spawnId, trans);
            });

            // delete data from memory ...
            Global.ObjectMgr.DeleteCreatureData(spawnId);

            DB.Characters.CommitTransaction(trans);

            // ... and the database
            trans = new();

            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_CREATURE);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_SPAWNGROUP_MEMBER);
            stmt.AddValue(0, (byte)SpawnObjectType.Creature);
            stmt.AddValue(1, spawnId);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_CREATURE_ADDON);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_GAME_EVENT_CREATURE);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_GAME_EVENT_MODEL_EQUIP);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToCreature);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToGO);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToCreature);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToCreature);
            trans.Append(stmt);

            DB.World.CommitTransaction(trans);

            return true;
        }

        public override bool IsInvisibleDueToDespawn(WorldObject seer)
        {
            if (base.IsInvisibleDueToDespawn(seer))
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

                if (IsNeutralToAll() || !IsWithinDistInMap(who, GetAttackDistance(who) + m_CombatDistance))
                    return false;
            }

            if (!CanCreatureAttack(who, force))
                return false;

            return IsWithinLOSInMap(who);
        }

        public float GetAttackDistance(Unit player)
        {
            float aggroRate = WorldConfig.GetFloatValue(WorldCfg.RateCreatureAggro);
            if (aggroRate == 0)
                return 0.0f;

            // WoW Wiki: the minimum radius seems to be 5 yards, while the maximum range is 45 yards
            float maxRadius = 45.0f * aggroRate;
            float minRadius = 5.0f * aggroRate;

            int expansionMaxLevel = (int)Global.ObjectMgr.GetMaxLevelForExpansion((Expansion)GetCreatureTemplate().RequiredExpansion);
            int playerLevel = (int)player.GetLevelForTarget(this);
            int creatureLevel = (int)GetLevelForTarget(player);
            int levelDifference = creatureLevel - playerLevel;

            // The aggro radius for creatures with equal level as the player is 20 yards.
            // The combatreach should not get taken into account for the distance so we drop it from the range (see Supremus as expample)
            float baseAggroDistance = 20.0f - GetCombatReach();

            // + - 1 yard for each level difference between player and creature
            float aggroRadius = baseAggroDistance + (float)levelDifference;

            // detect range auras
            if ((creatureLevel + 5) <= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
            {
                aggroRadius += GetTotalAuraModifier(AuraType.ModDetectRange);
                aggroRadius += player.GetTotalAuraModifier(AuraType.ModDetectedRange);
            }

            // The aggro range of creatures with higher levels than the total player level for the expansion should get the maxlevel treatment
            // This makes sure that creatures such as bosses wont have a bigger aggro range than the rest of the npc's
            // The following code is used for blizzlike behaviour such as skippable bosses
            if (creatureLevel > expansionMaxLevel)
                aggroRadius = baseAggroDistance + (float)(expansionMaxLevel - playerLevel);

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

                SaveRespawnTime();

                ReleaseSpellFocus(null, false);               // remove spellcast focus
                DoNotReacquireSpellFocusTarget();  // cancel delayed re-target
                SetTarget(ObjectGuid.Empty);      // drop target - dead mobs shouldn't ever target things

                ReplaceAllNpcFlags(NPCFlags.None);
                ReplaceAllNpcFlags2(NPCFlags2.None);

                SetMountDisplayId(0); // if creature is mounted on a virtual mount, remove it at death

                SetActive(false);
                SetNoSearchAssistance(false);

                //Dismiss group if is leader
                if (m_formation != null)
                {
                    if (m_formation.GetLeader() == this)
                        m_formation.FormationReset(true);

                    ZoneScript script = GetZoneScript();
                    if (script != null && !m_formation.HasAliveMembers())
                        script.OnCreatureGroupDepleted(m_formation);
                }

                bool needsFalling = (IsFlying() || IsHovering()) && !IsUnderWater() && !HasUnitState(UnitState.Root);
                SetHover(false, false);
                SetDisableGravity(false, false);

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

                SetTappedBy(null);
                ResetPlayerDamageReq();

                SetCannotReachTarget(false);
                UpdateMovementCapabilities();

                ClearUnitState(UnitState.AllErasable);

                if (!IsPet())
                {
                    CreatureData creatureData = GetCreatureData();
                    CreatureTemplate cInfo = GetCreatureTemplate();

                    ObjectManager.ChooseCreatureFlags(cInfo, out ulong npcFlags, out uint unitFlags, out uint unitFlags2, out uint unitFlags3, _staticFlags, creatureData);

                    if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Worldevent))
                        npcFlags |= Global.GameEventMgr.GetNPCFlag(this);

                    ReplaceAllNpcFlags((NPCFlags)(npcFlags & 0xFFFFFFFF));
                    ReplaceAllNpcFlags2((NPCFlags2)(npcFlags >> 32));

                    ReplaceAllUnitFlags((UnitFlags)unitFlags);
                    ReplaceAllUnitFlags2((UnitFlags2)unitFlags2);
                    ReplaceAllUnitFlags3((UnitFlags3)unitFlags3);
                    ReplaceAllDynamicFlags(UnitDynFlags.None);

                    RemoveUnitFlag(UnitFlags.InCombat);

                    SetMeleeDamageSchool((SpellSchools)cInfo.DmgSchool);

                    uint vignetteId = cInfo.VignetteID;
                    if (vignetteId != 0)
                        SetVignette(vignetteId);
                }

                InitializeMovementAI();
                base.SetDeathState(DeathState.Alive);
                LoadCreaturesAddon();
                LoadCreaturesSparringHealth();
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
                UpdateObjectVisibilityOnDestroy();
                RemoveCorpse(false, false);

                if (GetDeathState() == DeathState.Dead)
                {
                    Log.outDebug(LogFilter.Unit, "Respawning creature {0} ({1})", GetName(), GetGUID().ToString());
                    m_respawnTime = 0;
                    ResetPickPocketRefillTimer();
                    _loot = null;

                    if (m_originalEntry != GetEntry())
                        UpdateEntry(m_originalEntry);

                    SelectLevel();

                    SetDeathState(DeathState.JustRespawned);

                    CreatureModel display = new(GetNativeDisplayId(), GetNativeDisplayScale(), 1.0f);
                    if (Global.ObjectMgr.GetCreatureModelRandomGender(ref display, GetCreatureTemplate()) != null)
                        SetDisplayId(display.CreatureDisplayID, true);

                    GetMotionMaster().InitializeDefault();

                    //Re-initialize reactstate that could be altered by movementgenerators
                    InitializeReactState();

                    UnitAI ai = GetAI();
                    if (ai != null) // reset the AI to be sure no dirty or uninitialized values will be used till next tick
                        ai.Reset();

                    triggerJustAppeared = true;
                    _aggroGracePeriodExpired = false;

                    uint poolid = GetCreatureData() != null ? GetCreatureData().poolId : 0;
                    if (poolid != 0)
                        Global.PoolMgr.UpdatePool<Creature>(GetMap().GetPoolData(), poolid, GetSpawnId());
                }
                UpdateObjectVisibility();
            }
            else
            {
                if (m_spawnId != 0)
                    GetMap().Respawn(SpawnObjectType.Creature, m_spawnId);
            }

            Log.outDebug(LogFilter.Unit, $"Respawning creature {GetName()} ({GetGUID()})");
        }

        public void ForcedDespawn(uint timeMSToDespawn = 0, TimeSpan forceRespawnTimer = default)
        {
            if (timeMSToDespawn != 0)
            {
                m_Events.AddEvent(new ForcedDespawnDelayEvent(this, forceRespawnTimer), m_Events.CalculateTime(TimeSpan.FromMilliseconds(timeMSToDespawn)));
                return;
            }

            if (m_respawnCompatibilityMode)
            {
                uint corpseDelay = GetCorpseDelay();
                uint respawnDelay = GetRespawnDelay();

                // do it before killing creature
                UpdateObjectVisibilityOnDestroy();

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

        public void DespawnOrUnsummon(TimeSpan msTimeToDespawn = default, TimeSpan forceRespawnTimer = default)
        {
            TempSummon summon = ToTempSummon();
            if (summon != null)
                summon.UnSummon((uint)msTimeToDespawn.TotalMilliseconds);
            else
                ForcedDespawn((uint)msTimeToDespawn.TotalMilliseconds, forceRespawnTimer);
        }

        public void LoadTemplateImmunities(int creatureImmunitiesId)
        {
            // uint32 max used for "spell id", the immunity system will not perform SpellInfo checks against invalid spells
            // used so we know which immunities were loaded from template
            uint placeholderSpellId = uint.MaxValue;

            void applyCreatureImmunities(CreatureImmunities immunities, bool apply)
            {
                for (var i = 0; i < immunities.School.Count; ++i)
                    if (immunities.School[i])
                        ApplySpellImmune(placeholderSpellId, SpellImmunity.School, 1u << i, apply);

                for (int i = 0; i < immunities.DispelType.Count; ++i)
                    if (immunities.DispelType[i])
                        ApplySpellImmune(placeholderSpellId, SpellImmunity.Dispel, (uint)i, apply);

                for (var i = 0; i < immunities.Mechanic.Count; ++i)
                    if (immunities.Mechanic[i])
                        ApplySpellImmune(placeholderSpellId, SpellImmunity.Mechanic, (uint)i, apply);

                foreach (SpellEffectName effect in immunities.Effect)
                    ApplySpellImmune(placeholderSpellId, SpellImmunity.Effect, effect, apply);

                foreach (AuraType aura in immunities.Aura)
                    ApplySpellImmune(placeholderSpellId, SpellImmunity.State, aura, apply);

                if (immunities.Other != SpellOtherImmunity.None)
                    ApplySpellImmune(placeholderSpellId, SpellImmunity.Other, (byte)immunities.Other, apply);
            }

            // unapply template immunities (in case we're updating entry)
            CreatureImmunities immunities = Global.SpellMgr.GetCreatureImmunities(_creatureImmunitiesId);
            if (immunities != null)
                applyCreatureImmunities(immunities, false);

            // apply new immunities
            immunities = Global.SpellMgr.GetCreatureImmunities(creatureImmunitiesId);
            if (immunities != null)
            {
                _creatureImmunitiesId = creatureImmunitiesId;
                applyCreatureImmunities(immunities, true);
            }
            else
                _creatureImmunitiesId = 0;
        }

        public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, WorldObject caster, bool requireImmunityPurgesEffectAttribute = false)
        {
            if (GetCreatureTemplate().CreatureType == CreatureType.Mechanical && spellEffectInfo.IsEffect(SpellEffectName.Heal))
                return true;

            return base.IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster, requireImmunityPurgesEffectAttribute);
        }

        public bool IsElite()
        {
            if (IsPet())
                return false;

            return HasClassification(CreatureClassifications.Elite) || HasClassification(CreatureClassifications.RareElite);
        }

        public bool IsWorldBoss()
        {
            if (IsPet())
                return false;

            return GetCreatureDifficulty().TypeFlags.HasFlag(CreatureTypeFlags.BossMob);
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
                        m_Events.AddEvent(e, m_Events.CalculateTime(TimeSpan.FromMilliseconds(WorldConfig.GetUIntValue(WorldCfg.CreatureFamilyAssistanceDelay))));
                    }
                }
            }
        }

        public void CallForHelp(float radius)
        {
            if (radius <= 0.0f || !IsEngaged() || !IsAlive() || IsPet() || IsCharmed())
                return;

            Unit target = GetThreatManager().GetCurrentVictim();
            if (target == null)
                target = GetThreatManager().GetAnyTarget();
            if (target == null)
                target = GetCombatManager().GetAnyTarget();

            if (target == null)
            {
                Log.outError(LogFilter.Unit, $"Creature {GetEntry()} ({GetName()}) trying to call for help without being in combat.");
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

            if (HasUnitFlag(UnitFlags.NonAttackable) || IsImmuneToNPC() || IsUninteractible())
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
                // some creatures can detect fake death
                if (IsIgnoringFeignDeath() && target.HasUnitFlag2(UnitFlags2.FeignDeath))
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

        public void SaveRespawnTime(uint forceDelay = 0)
        {
            if (IsSummon() || m_spawnId == 0 || (m_creatureData != null && !m_creatureData.dbData))
                return;

            if (m_respawnCompatibilityMode)
            {
                RespawnInfo ri = new();
                ri.type = SpawnObjectType.Creature;
                ri.spawnId = m_spawnId;
                ri.respawnTime = m_respawnTime;
                GetMap().SaveRespawnInfoDB(ri);
                return;
            }

            long thisRespawnTime = forceDelay != 0 ? GameTime.GetGameTime() + forceDelay : m_respawnTime;
            GetMap().SaveRespawnTime(SpawnObjectType.Creature, m_spawnId, GetEntry(), thisRespawnTime, GridDefines.ComputeGridCoord(GetHomePosition().GetPositionX(), GetHomePosition().GetPositionY()).GetId());
        }

        public bool CanCreatureAttack(Unit victim, bool force = true)
        {
            if (!victim.IsInMap(this))
                return false;

            if (!IsValidAttackTarget(victim))
                return false;

            if (!victim.IsInAccessiblePlaceFor(this))
                return false;

            CreatureAI ai = GetAI();
            if (ai != null)
                if (!ai.CanAIAttack(victim))
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
                if (CanFly())
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
            return Global.ObjectMgr.GetCreatureTemplateAddon(GetEntry());
        }

        public bool LoadCreaturesAddon()
        {
            CreatureAddon creatureAddon = GetCreatureAddon();
            if (creatureAddon == null)
                return false;

            uint mountDisplayId = _defaultMountDisplayIdOverride.GetValueOrDefault(creatureAddon.mount);
            if (mountDisplayId != 0)
                Mount(creatureAddon.mount);

            SetStandState((UnitStandStateType)creatureAddon.standState);
            ReplaceAllVisFlags((UnitVisFlags)creatureAddon.visFlags);
            SetAnimTier((AnimTier)creatureAddon.animTier, false);

            SetSheath((SheathState)creatureAddon.sheathState);
            ReplaceAllPvpFlags((UnitPVPStateFlags)creatureAddon.pvpFlags);

            // These fields must only be handled by core internals and must not be modified via scripts/DB dat
            ReplaceAllPetFlags(UnitPetFlags.None);
            SetShapeshiftForm(ShapeShiftForm.None);

            if (creatureAddon.emote != 0)
                SetEmoteState((Emote)creatureAddon.emote);

            SetAIAnimKitId(creatureAddon.aiAnimKit);
            SetMovementAnimKitId(creatureAddon.movementAnimKit);
            SetMeleeAnimKitId(creatureAddon.meleeAnimKit);

            // Check if visibility distance different
            if (creatureAddon.visibilityDistanceType != VisibilityDistanceType.Normal)
                SetVisibilityDistanceOverride(creatureAddon.visibilityDistanceType);

            //Load Path
            if (creatureAddon.PathId != 0)
                _waypointPathId = creatureAddon.PathId;

            if (creatureAddon.auras != null)
            {
                foreach (var id in creatureAddon.auras)
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

        public void LoadCreaturesSparringHealth(bool force = false)
        {
            var templateValues = Global.ObjectMgr.GetCreatureTemplateSparringValues(GetCreatureTemplate().Entry);
            if (force || !templateValues.Empty())
            {
                if (templateValues.Contains(_sparringHealthPct)) // only re-randomize sparring value if it was loaded from template (not when set to custom value from script)
                    _sparringHealthPct = templateValues.SelectRandom();
            }
        }

        // Send a message to LocalDefense channel for players opposition team in the zone
        public void SendZoneUnderAttackMessage(Player attacker)
        {
            ZoneUnderAttack packet = new();
            packet.AreaID = (int)GetAreaId();
            packet.Write();

            Team enemyTeam = attacker.GetTeam();
            if (enemyTeam != Team.Alliance)
                Global.WorldMgr.SendGlobalMessage(packet, null, Team.Alliance);
            if (enemyTeam != Team.Horde)
                Global.WorldMgr.SendGlobalMessage(packet, null, Team.Horde);
        }

        public void SetCanMelee(bool canMelee, bool fleeFromMelee = false)
        {
            bool wasFleeingFromMelee = HasFlag(CreatureStaticFlags.NoMeleeFlee);

            _staticFlags.ApplyFlag(CreatureStaticFlags.NoMeleeFlee, !canMelee && fleeFromMelee);
            _staticFlags.ApplyFlag(CreatureStaticFlags4.NoMeleeApproach, !canMelee && !fleeFromMelee);

            if (wasFleeingFromMelee == HasFlag(CreatureStaticFlags.NoMeleeFlee))
                return;

            Unit victim = GetVictim();
            if (victim == null)
                return;

            var currentMovement = GetMotionMaster().GetCurrentMovementGenerator();
            if (currentMovement == null)
                return;

            var canChangeMovement = new Func<bool>(() =>
            {
                if (wasFleeingFromMelee)
                    return currentMovement.GetMovementGeneratorType() == MovementGeneratorType.Fleeing && !HasUnitFlag(UnitFlags.Fleeing);

                return currentMovement.GetMovementGeneratorType() == MovementGeneratorType.Chase;
            })();

            if (!canChangeMovement)
                return;

            GetMotionMaster().Remove(currentMovement);
            StartDefaultCombatMovement(victim);
        }

        public void StartDefaultCombatMovement(Unit victim, float? range = null, float? angle = null)
        {
            if (!HasFlag(CreatureStaticFlags.NoMeleeFlee) || IsSummon())
                GetMotionMaster().MoveChase(victim, range.GetValueOrDefault(0), angle.GetValueOrDefault(0));
            else
                GetMotionMaster().MoveFleeing(victim);
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
                m_creatureData.SpawnPoint.GetPosition(out x, out y, out z, out ori);
                dist = m_creatureData.WanderDistance;
            }
            else
            {
                Position homePos = GetHomePosition();
                homePos.GetPosition(out x, out y, out z, out ori);
                dist = 0;
            }
        }

        bool IsSpawnedOnTransport() { return m_creatureData != null && m_creatureData.MapId != GetMapId(); }

        public void InitializeMovementCapabilities()
        {
            SetHover(GetMovementTemplate().IsHoverInitiallyEnabled());
            SetDisableGravity(IsFloating());
            SetControlled(IsSessile(), UnitState.Root);

            // If an amphibious creatures was swimming while engaged, disable swimming again
            if (IsAmphibious() && !_staticFlags.HasFlag(CreatureStaticFlags.CanSwim))
                RemoveUnitFlag(UnitFlags.CanSwim);

            UpdateMovementCapabilities();
        }

        public void UpdateMovementCapabilities()
        {
            // Do not update movement flags if creature is controlled by a player (charm/vehicle)
            if (m_playerMovingMe != null)
                return;

            // Set the movement flags if the creature is in that mode. (Only fly if actually in air, only swim if in water, etc)
            float ground = GetFloorZ();

            bool isInAir = (MathFunctions.fuzzyGt(GetPositionZ(), ground + GetHoverOffset() + MapConst.GroundHeightTolerance) || MathFunctions.fuzzyLt(GetPositionZ(), ground - MapConst.GroundHeightTolerance)); // Can be underground too, prevent the falling
            if (!isInAir)
                SetFall(false);

            // Some Amphibious creatures toggle swimming while engaged
            if (IsAmphibious() && !HasUnitFlag(UnitFlags.CantSwim) && !HasUnitFlag(UnitFlags.CanSwim) && IsEngaged())
                if (!IsSwimPrevented() || (GetVictim() != null && !GetVictim().IsOnOceanFloor()))
                    SetUnitFlag(UnitFlags.CanSwim);

            SetSwim(IsInWater() && CanSwim());
        }

        public CreatureMovementData GetMovementTemplate()
        {
            CreatureMovementData movementOverride = Global.ObjectMgr.GetCreatureMovementOverride(m_spawnId);
            if (movementOverride != null)
                return movementOverride;

            return GetCreatureTemplate().Movement;
        }

        public override bool CanSwim()
        {
            if (base.CanSwim())
                return true;

            if (IsPet())
                return true;

            return false;
        }

        public override bool CanEnterWater() { return CanSwim() || IsAmphibious(); }

        public void AllLootRemovedFromCorpse()
        {
            long now = GameTime.GetGameTime();
            // Do not reset corpse remove time if corpse is already removed
            if (m_corpseRemoveTime <= now)
                return;

            // Scripts can choose to ignore RATE_CORPSE_DECAY_LOOTED by calling SetCorpseDelay(timer, true)
            float decayRate = m_ignoreCorpseDecayRatio ? 1.0f : WorldConfig.GetFloatValue(WorldCfg.RateCorpseDecayLooted);

            // corpse skinnable, but without skinning flag, and then skinned, corpse will despawn next update
            bool isFullySkinned()
            {
                if (_loot != null && _loot.loot_type == LootType.Skinning && _loot.IsLooted())
                    return true;

                bool hasSkinningLoot = false;
                foreach (var (_, loot) in m_personalLoot)
                {
                    if (loot.loot_type == LootType.Skinning)
                    {
                        if (!loot.IsLooted())
                            return false;
                        hasSkinningLoot = true;
                    }
                }

                return hasSkinningLoot;
            }

            if (isFullySkinned())
                m_corpseRemoveTime = now;
            else
                m_corpseRemoveTime = now + (uint)(m_corpseDelay * decayRate);

            m_respawnTime = Math.Max(m_corpseRemoveTime + m_respawnDelay, m_respawnTime);
        }

        public override void SetInteractionAllowedWhileHostile(bool interactionAllowed)
        {
            _staticFlags.ApplyFlag(CreatureStaticFlags5.InteractWhileHostile, interactionAllowed);
            base.SetInteractionAllowedWhileHostile(interactionAllowed);
        }

        public override void SetInteractionAllowedInCombat(bool interactionAllowed)
        {
            _staticFlags.ApplyFlag(CreatureStaticFlags3.AllowInteractionWhileInCombat, interactionAllowed);
            base.SetInteractionAllowedInCombat(interactionAllowed);
        }

        public override void UpdateNearbyPlayersInteractions()
        {
            base.UpdateNearbyPlayersInteractions();

            // If as a result of npcflag updates we stop seeing UNIT_NPC_FLAG_QUESTGIVER then
            // we must also send SMSG_QUEST_GIVER_STATUS_MULTIPLE because client will not request it automatically
            if (IsQuestGiver())
            {
                var sender = (Player receiver) => receiver.PlayerTalkClass.SendQuestGiverStatus(receiver.GetQuestDialogStatus(this), GetGUID());

                MessageDistDeliverer notifier = new(this, sender, GetVisibilityRange());
                Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
            }
        }

        public bool HasScalableLevels()
        {
            return m_unitData.ContentTuningID != 0;
        }

        public void ApplyLevelScaling()
        {
            CreatureDifficulty creatureDifficulty = GetCreatureDifficulty();
            var levels = Global.DB2Mgr.GetContentTuningData(creatureDifficulty.ContentTuningID, 0);
            if (levels.HasValue)
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ScalingLevelMin), levels.Value.MinLevel);
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ScalingLevelMax), levels.Value.MaxLevel);
            }

            int mindelta = Math.Min(creatureDifficulty.DeltaLevelMax, creatureDifficulty.DeltaLevelMin);
            int maxdelta = Math.Max(creatureDifficulty.DeltaLevelMax, creatureDifficulty.DeltaLevelMin);
            int delta = mindelta == maxdelta ? mindelta : RandomHelper.IRand(mindelta, maxdelta);

            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ScalingLevelDelta), delta);
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ContentTuningID), creatureDifficulty.ContentTuningID);
        }

        ulong GetMaxHealthByLevel(uint level)
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureDifficulty creatureDifficulty = GetCreatureDifficulty();
            float baseHealth = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureHealth, level, creatureDifficulty.GetHealthScalingExpansion(), creatureDifficulty.ContentTuningID, (Class)cInfo.UnitClass, 0);

            return (ulong)Math.Max(baseHealth * creatureDifficulty.HealthModifier, 1.0f);
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

        public float GetBaseDamageForLevel(uint level)
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            CreatureDifficulty creatureDifficulty = GetCreatureDifficulty();
            return Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureAutoAttackDps, level, creatureDifficulty.GetHealthScalingExpansion(), creatureDifficulty.ContentTuningID, (Class)cInfo.UnitClass, 0);
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
            CreatureDifficulty creatureDifficulty = GetCreatureDifficulty();
            float baseArmor = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureArmor, level, creatureDifficulty.GetHealthScalingExpansion(), creatureDifficulty.ContentTuningID, (Class)cInfo.UnitClass, 0);
            return baseArmor * creatureDifficulty.ArmorModifier;
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
            if (unitTarget != null)
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
                    byte scalingFactionGroup = m_unitData.ScalingFactionGroup;
                    uint targetLevel = unitTarget.m_unitData.EffectiveLevel;
                    if (targetLevel == 0)
                        targetLevel = unitTarget.GetLevel();

                    uint targetLevelDelta = 0;

                    Player playerTarget = target.ToPlayer();
                    if (playerTarget != null)
                    {
                        if (scalingFactionGroup != 0 && CliDB.FactionTemplateStorage.LookupByKey(CliDB.ChrRacesStorage.LookupByKey(playerTarget.GetRace()).FactionID).FactionGroup != scalingFactionGroup)
                            scalingLevelMin = scalingLevelMax;

                        int maxCreatureScalingLevel = playerTarget.m_activePlayerData.MaxCreatureScalingLevel;
                        targetLevelDelta = (uint)Math.Min(maxCreatureScalingLevel > 0 ? maxCreatureScalingLevel - targetLevel : 0, playerTarget.m_activePlayerData.ScalingPlayerLevelDelta);
                    }

                    int levelWithDelta = (int)(targetLevel + targetLevelDelta);
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

        public void InheritStringIds(Creature parent)
        {
            // copy references to stringIds from template and spawn
            m_stringIds = parent.m_stringIds;

            // then copy script stringId, not just its reference
            SetScriptStringId(parent.GetStringId(StringIdType.Script));
        }

        public bool HasStringId(string id)
        {
            return m_stringIds.Contains(id);
        }

        void SetScriptStringId(string id)
        {
            if (!id.IsEmpty())
            {
                m_scriptStringId = id;
                m_stringIds[(int)StringIdType.Script] = m_scriptStringId;
            }
            else
            {
                m_scriptStringId = null;
                m_stringIds[(int)StringIdType.Script] = null;
            }
        }

        public string GetStringId(StringIdType type) { return m_stringIds[(int)type]; }

        public override SpawnTrackingStateData GetSpawnTrackingStateDataForPlayer(Player player)
        {
            if (player == null)
                return null;

            CreatureData data = GetCreatureData();
            if (data != null)
            {
                if (data.spawnTrackingQuestObjectiveId != 0 && data.spawnTrackingData != null)
                {
                    SpawnTrackingState state = player.GetSpawnTrackingStateByObjective(data.spawnTrackingData.SpawnTrackingId, data.spawnTrackingQuestObjectiveId);
                    return data.spawnTrackingStates[(int)state];
                }
            }

            return null;
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

        public void SetVendor(NPCFlags flags, bool apply)
        {
            flags &= NPCFlags.VendorMask;
            VendorDataTypeFlags vendorFlags = (VendorDataTypeFlags)((uint)flags >> 7);
            if (apply)
            {
                if (m_vendorData == null)
                {
                    m_entityFragments.Add(EntityFragment.FVendor_C, IsInWorld);
                    m_vendorData = new();
                }

                SetNpcFlag(flags);
                SetUpdateFieldFlagValue(m_values.ModifyValue(m_vendorData).ModifyValue(m_vendorData.Flags), (int)vendorFlags);
            }
            else if (m_vendorData != null)
            {
                RemoveNpcFlag(flags);
                RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_vendorData).ModifyValue(m_vendorData.Flags), (int)vendorFlags);
                if (m_vendorData.Flags == 0)
                {
                    m_values.ModifyValue(m_vendorData);
                    AddToObjectUpdateIfNeeded();
                    m_vendorData = null;
                    m_entityFragments.Remove(EntityFragment.FVendor_C);
                }
            }
        }

        void SetPetitioner(bool apply)
        {
            if (apply)
            {
                if (m_vendorData == null)
                {
                    m_entityFragments.Add(EntityFragment.FVendor_C, IsInWorld);
                    m_vendorData = new();
                }

                SetNpcFlag(NPCFlags.Petitioner);
                SetUpdateFieldFlagValue(m_values.ModifyValue(m_vendorData).ModifyValue(m_vendorData.Flags), (int)VendorDataTypeFlags.Petition);
            }
            else if (m_vendorData != null)
            {
                RemoveNpcFlag(NPCFlags.Petitioner);
                RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_vendorData).ModifyValue(m_vendorData.Flags), (int)VendorDataTypeFlags.Petition);
                if (m_vendorData.Flags == 0)
                {
                    m_values.ModifyValue(m_vendorData);
                    AddToObjectUpdateIfNeeded();
                    m_vendorData = null;
                    m_entityFragments.Remove(EntityFragment.FVendor_C);
                }
            }
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

        public bool HasLabel(int cretureLabel)
        {
            return GetLabels().Contains(cretureLabel);
        }

        public List<int> GetLabels()
        {
            return Global.DB2Mgr.GetCreatureLabels(GetCreatureDifficulty().CreatureDifficultyID);
        }

        public virtual int GetPetAutoSpellSize()
        {
            return SharedConst.MaxSpellCharm;
        }

        public virtual uint GetPetAutoSpellOnPos(byte pos)
        {
            if (pos >= SharedConst.MaxSpellCharm || GetCharmInfo() == null || GetCharmInfo().GetCharmSpell(pos).GetActiveState() != ActiveStates.Enabled)
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
                    if (spellInfo.GetRecoveryTime() == 0 && spellInfo.RangeEntry != null && spellInfo.RangeEntry.Id != 1 /*Self*/ && spellInfo.RangeEntry.Id != 2 /*Combat Range*/ && spellInfo.GetMaxRange() > range)
                        range = spellInfo.GetMaxRange();
                }
            }

            return range;
        }

        bool CanNotReachTarget() { return m_cannotReachTarget; }

        public void SetCannotReachTarget(bool cannotReach)
        {
            if (cannotReach == m_cannotReachTarget)
                return;

            m_cannotReachTarget = cannotReach;
            m_cannotReachTimer = 0;

            if (cannotReach)
                Log.outDebug(LogFilter.Unit, $"Creature::SetCannotReachTarget() called with true. Details: {GetDebugInfo()}");
        }

        public bool IsIgnoringChaseRange()
        {
            return _staticFlags.HasFlag(CreatureStaticFlags6.AlwaysStandOnTopOfTarget);
        }

        public void SetIgnoreChaseRange(bool ignoreChaseRange)
        {
            _staticFlags.ApplyFlag(CreatureStaticFlags6.AlwaysStandOnTopOfTarget, ignoreChaseRange);
        }

        public void SetDefaultMount(uint? mountCreatureDisplayId)
        {
            if (mountCreatureDisplayId.HasValue && !CliDB.CreatureDisplayInfoStorage.HasRecord(mountCreatureDisplayId.Value))
                mountCreatureDisplayId = null;

            _defaultMountDisplayIdOverride = mountCreatureDisplayId;
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

        public Unit SelectNearestHostileUnitInAggroRange(bool useLOS = false, bool ignoreCivilians = false)
        {
            // Selects nearest hostile target within creature's aggro range. Used primarily by
            //  pets set to aggressive. Will not return neutral or friendly targets
            var u_check = new NearestHostileUnitInAggroRangeCheck(this, useLOS, ignoreCivilians);
            var searcher = new UnitSearcher(this, u_check);
            Cell.VisitGridObjects(this, searcher, SharedConst.MaxAggroRadius);
            return searcher.GetTarget();
        }

        public override float GetNativeObjectScale()
        {
            return GetCreatureTemplate().Scale;
        }

        public override void SetObjectScale(float scale)
        {
            base.SetObjectScale(scale);

            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelInfo(GetDisplayId());
            if (minfo != null)
            {
                SetBoundingRadius((IsPet() ? 1.0f : minfo.BoundingRadius) * scale * GetDisplayScale());
                SetCombatReach((IsPet() ? SharedConst.DefaultPlayerCombatReach : minfo.CombatReach) * scale * GetDisplayScale());
            }
        }

        public override void SetDisplayId(uint modelId, bool setNative = false)
        {
            base.SetDisplayId(modelId, setNative);

            CreatureModelInfo modelInfo = Global.ObjectMgr.GetCreatureModelInfo(modelId);
            if (modelInfo != null)
            {
                SetBoundingRadius((IsPet() ? 1.0f : modelInfo.BoundingRadius) * GetObjectScale() * GetDisplayScale());
                SetCombatReach((IsPet() ? SharedConst.DefaultPlayerCombatReach : modelInfo.CombatReach) * GetObjectScale() * GetDisplayScale());
            }
        }

        public void SetDisplayFromModel(int modelIdx)
        {
            CreatureModel model = GetCreatureTemplate().GetModelByIdx(modelIdx);
            if (model != null)
                SetDisplayId(model.CreatureDisplayID);
        }

        public override void SetTarget(ObjectGuid guid)
        {
            if (HasSpellFocus())
                _spellFocusInfo.Target = guid;
            else
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Target), guid);
        }

        public void SetSpellFocus(Spell focusSpell, WorldObject target)
        {
            // Pointer validation and checking for a already existing focus
            if (_spellFocusInfo.Spell != null || focusSpell == null)
                return;

            // Prevent dead / feign death creatures from setting a focus target
            if (!IsAlive() || HasUnitFlag2(UnitFlags2.FeignDeath) || HasAuraType(AuraType.FeignDeath))
                return;

            // Don't allow stunned creatures to set a focus target
            if (HasUnitFlag(UnitFlags.Stunned))
                return;

            // some spells shouldn't track targets
            if (focusSpell.IsFocusDisabled())
                return;

            SpellInfo spellInfo = focusSpell.GetSpellInfo();

            // don't use spell focus for vehicle spells
            if (spellInfo.HasAura(AuraType.ControlVehicle))
                return;

            // instant non-channeled casts and non-target spells don't need facing updates
            if (target == null && (focusSpell.GetCastTime() == 0 && !spellInfo.IsChanneled()))
                return;

            // store pre-cast values for target and orientation (used to later restore)
            if (_spellFocusInfo.Delay == 0)
            { // only overwrite these fields if we aren't transitioning from one spell focus to another
                _spellFocusInfo.Target = GetTarget();
                _spellFocusInfo.Orientation = GetOrientation();
            }
            else // don't automatically reacquire target for the previous spellcast
                _spellFocusInfo.Delay = 0;

            _spellFocusInfo.Spell = focusSpell;

            bool noTurnDuringCast = spellInfo.HasAttribute(SpellAttr5.AiDoesntFaceTarget);
            bool turnDisabled = CannotTurn();
            // set target, then force send update packet to players if it changed to provide appropriate facing
            ObjectGuid newTarget = (target != null && !noTurnDuringCast && !turnDisabled) ? target.GetGUID() : ObjectGuid.Empty;
            if (GetTarget() != newTarget)
                SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Target), newTarget);

            // If we are not allowed to turn during cast but have a focus target, face the target
            if (!turnDisabled && noTurnDuringCast && target != null)
                SetFacingToObject(target, false);

            if (!noTurnDuringCast)
                AddUnitState(UnitState.Focusing);
        }

        public override bool HasSpellFocus(Spell focusSpell = null)
        {
            if (IsDead()) // dead creatures cannot focus
            {
                if (_spellFocusInfo.Spell != null || _spellFocusInfo.Delay != 0)
                    Log.outWarn(LogFilter.Unit, $"Creature '{GetName()}' (entry {GetEntry()}) has spell focus (spell id {(_spellFocusInfo.Spell != null ? _spellFocusInfo.Spell.GetSpellInfo().Id : 0)}, delay {_spellFocusInfo.Delay}ms) despite being dead.");

                return false;
            }

            if (focusSpell != null)
                return focusSpell == _spellFocusInfo.Spell;
            else
                return _spellFocusInfo.Spell != null || _spellFocusInfo.Delay != 0;
        }

        public void ReleaseSpellFocus(Spell focusSpell = null, bool withDelay = true)
        {
            if (_spellFocusInfo.Spell == null)
                return;

            // focused to something else
            if (focusSpell != null && focusSpell != _spellFocusInfo.Spell)
                return;

            if (_spellFocusInfo.Spell.GetSpellInfo().HasAttribute(SpellAttr5.AiDoesntFaceTarget))
                ClearUnitState(UnitState.Focusing);

            if (IsPet()) // player pets do not use delay system
            {
                if (!CannotTurn())
                    ReacquireSpellFocusTarget();
            }
            else // don't allow re-target right away to prevent visual bugs
                _spellFocusInfo.Delay = withDelay ? 1000 : 1u;

            _spellFocusInfo.Spell = null;
        }

        void ReacquireSpellFocusTarget()
        {
            if (!HasSpellFocus())
            {
                Log.outError(LogFilter.Unit, $"Creature::ReacquireSpellFocusTarget() being called with HasSpellFocus() returning false. {GetDebugInfo()}");
                return;
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Target), _spellFocusInfo.Target);

            if (!CannotTurn())
            {
                if (!_spellFocusInfo.Target.IsEmpty())
                {
                    WorldObject objTarget = Global.ObjAccessor.GetWorldObject(this, _spellFocusInfo.Target);
                    if (objTarget != null)
                        SetFacingToObject(objTarget, false);
                }
                else
                    SetFacingTo(_spellFocusInfo.Orientation, false);
            }

            _spellFocusInfo.Delay = 0;
        }

        public void DoNotReacquireSpellFocusTarget()
        {
            _spellFocusInfo.Delay = 0;
            _spellFocusInfo.Spell = null;
        }

        public ulong GetSpawnId() { return m_spawnId; }

        public void SetCorpseDelay(uint delay, bool ignoreCorpseDecayRatio = false)
        {
            m_corpseDelay = delay;
            if (ignoreCorpseDecayRatio)
                m_ignoreCorpseDecayRatio = true;
        }
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

        // Returns true if CREATURE_STATIC_FLAG_AQUATIC is set which strictly binds the creature to liquids
        public bool IsAquatic() { return _staticFlags.HasFlag(CreatureStaticFlags.Aquatic); }

        // Returns true if CREATURE_STATIC_FLAG_AMPHIBIOUS is set which allows a creature to enter and leave liquids while sticking to the ocean floor. These creatures will become able to swim when engaged
        public bool IsAmphibious() { return _staticFlags.HasFlag(CreatureStaticFlags.Amphibious); }

        // Returns true if CREATURE_STATIC_FLAG_FLOATING is set which is  disabling the gravity of the creature on spawn and reset
        public bool IsFloating() { return _staticFlags.HasFlag(CreatureStaticFlags.Floating); }
        public void SetFloating(bool floating) { _staticFlags.ApplyFlag(CreatureStaticFlags.Floating, floating); SetDisableGravity(floating); }

        // Returns true if CREATURE_STATIC_FLAG_SESSILE is set which permanently roots the creature in place
        public bool IsSessile() { return _staticFlags.HasFlag(CreatureStaticFlags.Sessile); }
        public void SetSessile(bool sessile) { _staticFlags.ApplyFlag(CreatureStaticFlags.Sessile, sessile); SetControlled(sessile, UnitState.Root); }

        // Returns true if CREATURE_STATIC_FLAG_3_CANNOT_PENETRATE_WATER is set which does not allow the creature to go below liquid surfaces
        public bool CannotPenetrateWater() { return _staticFlags.HasFlag(CreatureStaticFlags3.CannotPenetrateWater); }
        public void SetCannotPenetrateWater(bool cannotPenetrateWater) { _staticFlags.ApplyFlag(CreatureStaticFlags3.CannotPenetrateWater, cannotPenetrateWater); }

        // Returns true if CREATURE_STATIC_FLAG_3_CANNOT_SWIM is set which prevents 'Amphibious' creatures from swimming when engaged
        public bool IsSwimDisabled() { return _staticFlags.HasFlag(CreatureStaticFlags3.CannotSwim); }

        // Returns true if CREATURE_STATIC_FLAG_4_PREVENT_SWIM is set which prevents 'Amphibious' creatures from swimming when engaged until the victim is no longer on the ocean floor
        public bool IsSwimPrevented() { return _staticFlags.HasFlag(CreatureStaticFlags4.PreventSwim); }

        public override bool CanFly() { return IsFlying() || HasUnitMovementFlag(MovementFlag.CanFly); }

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

        public bool IsThreatFeedbackDisabled() { return _staticFlags.HasFlag(CreatureStaticFlags3.NoThreatFeedback); }
        public void SetNoThreatFeedback(bool noThreatFeedback) { _staticFlags.ApplyFlag(CreatureStaticFlags3.NoThreatFeedback, noThreatFeedback); }

        public void SetUnkillable(bool unkillable) { _staticFlags.ApplyFlag(CreatureStaticFlags.Unkillable, unkillable); }

        public bool IsInEvadeMode() { return HasUnitState(UnitState.Evade); }
        public bool IsEvadingAttacks() { return IsInEvadeMode() || CanNotReachTarget(); }

        public bool IsStateRestoredOnEvade() { return !HasFlag(CreatureStaticFlags5.NoLeavecombatStateRestore); }
        public void SetRestoreStateOnEvade(bool restoreOnEvade) { _staticFlags.ApplyFlag(CreatureStaticFlags5.NoLeavecombatStateRestore, !restoreOnEvade); }

        public override CreatureAI GetAI()
        {
            return (CreatureAI)i_AI;
        }

        public new T GetAI<T>() where T : CreatureAI
        {
            return (T)i_AI;
        }

        public override SpellSchoolMask GetMeleeDamageSchoolMask(WeaponAttackType attackType = WeaponAttackType.BaseAttack) { return m_meleeDamageSchoolMask; }
        public void SetMeleeDamageSchool(SpellSchools school) { m_meleeDamageSchoolMask = (SpellSchoolMask)(1 << (int)school); }

        public bool CanMelee() { return !_staticFlags.HasFlag(CreatureStaticFlags.NoMeleeFlee) && !_staticFlags.HasFlag(CreatureStaticFlags4.NoMeleeApproach); }

        public bool CanIgnoreLineOfSightWhenCastingOnMe() { return _staticFlags.HasFlag(CreatureStaticFlags4.IgnoreLosWhenCastingOnMe); }

        public bool IsTreatedAsRaidUnit() { return _staticFlags.HasFlag(CreatureStaticFlags4.TreatAsRaidUnitForHelpfulSpells); }
        public void SetTreatAsRaidUnit(bool treatAsRaidUnit) { _staticFlags.ApplyFlag(CreatureStaticFlags4.TreatAsRaidUnitForHelpfulSpells, treatAsRaidUnit); }

        public sbyte GetOriginalEquipmentId() { return m_originalEquipmentId; }
        public byte GetCurrentEquipmentId() { return m_equipmentId; }
        public void SetCurrentEquipmentId(byte id) { m_equipmentId = id; }

        public CreatureTemplate GetCreatureTemplate() { return m_creatureInfo; }
        public CreatureData GetCreatureData() { return m_creatureData; }
        public CreatureDifficulty GetCreatureDifficulty() { return m_creatureDifficulty; }

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
            m_wanderDistance = data.WanderDistance;
            m_respawnDelay = (uint)data.spawntimesecs;

            if (!Create(map.GenerateLowGuid(HighGuid.Creature), map, data.Id, data.SpawnPoint, data, 0, !m_respawnCompatibilityMode))
                return false;

            //We should set first home position, because then AI calls home movement
            SetHomePosition(this);

            m_deathState = DeathState.Alive;

            m_respawnTime = GetMap().GetCreatureRespawnTime(m_spawnId);

            if (m_respawnTime == 0 && !map.IsSpawnGroupActive(data.spawnGroupData.groupId))
            {
                if (!m_respawnCompatibilityMode)
                {
                    // @todo pools need fixing! this is just a temporary thing, but they violate dynspawn principles
                    if (data.poolId == 0)
                    {
                        Log.outError(LogFilter.Unit, $"Creature (SpawnID {spawnId}) trying to load in inactive spawn group '{data.spawnGroupData.name}':\n{GetDebugInfo()}");
                        return false;
                    }
                }

                m_respawnTime = GameTime.GetGameTime() + RandomHelper.URand(4, 7);
            }

            if (m_respawnTime != 0)
            {
                if (!m_respawnCompatibilityMode)
                {
                    // @todo same as above
                    if (data.poolId == 0)
                    {
                        Log.outError(LogFilter.Unit, $"Creature (SpawnID {spawnId}) trying to load despite a respawn timer in progress:\n{GetDebugInfo()}");
                        return false;
                    }
                }
                else
                {
                    // compatibility mode creatures will be respawned in ::Update()
                    m_deathState = DeathState.Dead;
                }

                if (CanFly())
                {
                    float tz = map.GetHeight(GetPhaseShift(), data.SpawnPoint, true, MapConst.MaxFallDistance);
                    if (data.SpawnPoint.GetPositionZ() - tz > 0.1f && GridDefines.IsValidMapCoord(tz))
                        Relocate(data.SpawnPoint.GetPositionX(), data.SpawnPoint.GetPositionY(), tz);
                }
            }

            SetSpawnHealth();

            SelectWildBattlePetLevel();

            // checked at creature_template loading
            DefaultMovementType = (MovementGeneratorType)data.movementType;

            m_stringIds[(int)StringIdType.Spawn] = data.StringId;

            if (addToMap && !GetMap().AddToMap(this))
                return false;
            return true;
        }

        public LootModes GetLootMode() { return m_LootMode; }
        public bool HasLootMode(LootModes lootMode) { return Convert.ToBoolean(m_LootMode & lootMode); }
        public void SetLootMode(LootModes lootMode) { m_LootMode = lootMode; }
        public void AddLootMode(LootModes lootMode) { m_LootMode |= lootMode; }
        public void RemoveLootMode(LootModes lootMode) { m_LootMode &= ~lootMode; }
        public void ResetLootMode() { m_LootMode = LootModes.Default; }

        public void SetNoCallAssistance(bool val) { m_AlreadyCallAssistance = val; }
        public void SetNoSearchAssistance(bool val) { m_AlreadySearchedAssistance = val; }
        public bool HasSearchedAssistance() { return m_AlreadySearchedAssistance; }
        public bool IsIgnoringFeignDeath() { return _staticFlags.HasFlag(CreatureStaticFlags2.IgnoreFeignDeath); }
        public void SetIgnoreFeignDeath(bool ignoreFeignDeath) { _staticFlags.ApplyFlag(CreatureStaticFlags2.IgnoreFeignDeath, ignoreFeignDeath); }
        public bool IsIgnoringSanctuarySpellEffect() { return _staticFlags.HasFlag(CreatureStaticFlags2.IgnoreSanctuary); }
        public void SetIgnoreSanctuarySpellEffect(bool ignoreSanctuary) { _staticFlags.ApplyFlag(CreatureStaticFlags2.IgnoreSanctuary, ignoreSanctuary); }

        public override MovementGeneratorType GetDefaultMovementType() { return DefaultMovementType; }
        public void SetDefaultMovementType(MovementGeneratorType mgt) { DefaultMovementType = mgt; }

        public CreatureClassifications GetCreatureClassification() { return GetCreatureTemplate().Classification; }
        public bool HasClassification(CreatureClassifications classification) { return GetCreatureTemplate().Classification == classification; }

        public long GetRespawnTime() { return m_respawnTime; }
        public void SetRespawnTime(uint respawn) { m_respawnTime = respawn != 0 ? GameTime.GetGameTime() + respawn : 0; }

        public uint GetRespawnDelay() { return m_respawnDelay; }
        public void SetRespawnDelay(uint delay) { m_respawnDelay = delay; }

        public float GetWanderDistance() { return m_wanderDistance; }
        public void SetWanderDistance(float dist) { m_wanderDistance = dist; }

        public void DoImmediateBoundaryCheck() { m_boundaryCheckTime = 0; }

        bool CanRegenerateHealth() { return !_staticFlags.HasFlag(CreatureStaticFlags5.NoHealthRegen) && _regenerateHealth; }
        public void SetRegenerateHealth(bool value) { _staticFlags.ApplyFlag(CreatureStaticFlags5.NoHealthRegen, !value); }

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

        public uint GetWaypointPathId() { return _waypointPathId; }
        public void LoadPath(uint pathid) { _waypointPathId = pathid; }

        public (uint nodeId, uint pathId) GetCurrentWaypointInfo() { return _currentWaypointNodeInfo; }
        public void UpdateCurrentWaypointInfo(uint nodeId, uint pathId) { _currentWaypointNodeInfo = (nodeId, pathId); }

        public CreatureGroup GetFormation() { return m_formation; }
        public void SetFormation(CreatureGroup formation) { m_formation = formation; }

        void SetDisableReputationGain(bool disable) { DisableReputationGain = disable; }
        public bool IsReputationGainDisabled() { return DisableReputationGain; }

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

        public override void BuildValuesCreate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            m_objectData.WriteCreate(data, flags, this, target);
            m_unitData.WriteCreate(data, flags, this, target);

            if (m_vendorData != null)
            {
                if (EntityFragmentsHolder.IsIndirectFragment(EntityFragment.FVendor_C))
                    data.WriteUInt8(1);  // IndirectFragmentActive: FVendor_C

                m_vendorData.WriteCreate(data, flags, this, target);
            }
        }

        public override void BuildValuesUpdate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            if ((m_entityFragments.ContentsChangedMask & m_entityFragments.GetUpdateMaskFor(EntityFragment.CGObject)) != 0)
            {
                data.WriteUInt32(m_values.GetChangedObjectTypeMask());

                if (m_values.HasChanged(TypeId.Object))
                    m_objectData.WriteUpdate(data, flags, this, target);

                if (m_values.HasChanged(TypeId.Unit))
                    m_unitData.WriteUpdate(data, flags, this, target);
            }

            if (m_vendorData != null && (m_entityFragments.ContentsChangedMask & m_entityFragments.GetUpdateMaskFor(EntityFragment.FVendor_C)) != 0)
                m_vendorData.WriteUpdate(data, flags, this, target);
        }

        public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);
            valuesMask.Set((int)TypeId.Unit);

            data.WriteUInt32(valuesMask.GetBlock(0));

            UpdateMask mask = m_unitData.GetStaticUpdateMask();
            m_unitData.AppendAllowedFieldsMaskForFlag(mask, flags);
            m_unitData.WriteUpdate(data, mask, true, this, target);
        }

        public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedUnitMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            UpdateMask unitMask = requestedUnitMask;
            m_unitData.FilterDisallowedFieldsMaskForFlag(unitMask, flags);
            if (unitMask.IsAnySet())
                valuesMask.Set((int)TypeId.Unit);

            WorldPacket buffer = new();
            BuildEntityFragmentsForValuesUpdateForPlayerWithMask(buffer, flags);
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Unit])
                m_unitData.WriteUpdate(buffer, requestedUnitMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_vendorData);
            base.ClearUpdateMask(remove);
        }

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            Creature Owner;
            ObjectFieldData ObjectMask = new();
            UnitData UnitMask = new();

            public ValuesUpdateForPlayerWithMaskSender(Creature owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), UnitMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
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
            m_owner.DespawnOrUnsummon(TimeSpan.Zero, m_respawnTimer);    // since we are here, we are not TempSummon as object type cannot change during runtime
            return true;
        }

        Creature m_owner;
        TimeSpan m_respawnTimer;
    }
}
