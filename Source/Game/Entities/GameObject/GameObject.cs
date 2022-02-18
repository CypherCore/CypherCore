/*
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
using Framework.GameMath;
using Game.AI;
using Game.BattleGrounds;
using Game.Collision;
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
using System.Numerics;

namespace Game.Entities
{
    public class GameObject : WorldObject
    {
        public GameObject() : base(false)
        {
            ObjectTypeMask |= TypeMask.GameObject;
            ObjectTypeId = TypeId.GameObject;

            m_updateFlag.Stationary = true;
            m_updateFlag.Rotation = true;

            m_respawnDelayTime = 300;
            m_despawnDelay = 0;
            m_lootState = LootState.NotReady;
            m_spawnedByDefault = true;

            ResetLootMode(); // restore default loot mode
            StationaryPosition = new Position();

            m_gameObjectData = new GameObjectFieldData();
        }

        public override void Dispose()
        {
            m_AI = null;
            m_model = null;
            if (m_goInfo != null && m_goInfo.type == GameObjectTypes.Transport)
                m_goValue.Transport.StopFrames.Clear();

            base.Dispose();
        }

        public bool AIM_Initialize()
        {
            m_AI = AISelector.SelectGameObjectAI(this);

            if (m_AI == null)
                return false;

            m_AI.InitializeAI();
            return true;
        }

        public string GetAIName()
        {
            GameObjectTemplate got = Global.ObjectMgr.GetGameObjectTemplate(GetEntry());
            if (got != null)
                return got.AIName;

            return "";
        }

        public override void CleanupsBeforeDelete(bool finalCleanup)
        {
            base.CleanupsBeforeDelete(finalCleanup);

            RemoveFromOwner();
        }

        void RemoveFromOwner()
        {
            ObjectGuid ownerGUID = GetOwnerGUID();
            if (ownerGUID.IsEmpty())
                return;

            Unit owner = Global.ObjAccessor.GetUnit(this, ownerGUID);
            if (owner)
            {
                owner.RemoveGameObject(this, false);
                Cypher.Assert(GetOwnerGUID().IsEmpty());
                return;
            }

            // This happens when a mage portal is despawned after the caster changes map (for example using the portal)
            Log.outDebug(LogFilter.Server, "Removed GameObject (GUID: {0} Entry: {1} SpellId: {2} LinkedGO: {3}) that just lost any reference to the owner {4} GO list",
                GetGUID().ToString(), GetGoInfo().entry, m_spellId, GetGoInfo().GetLinkedGameObjectEntry(), ownerGUID.ToString());
            SetOwnerGUID(ObjectGuid.Empty);
        }

        public override void AddToWorld()
        {
            //- Register the gameobject for guid lookup
            if (!IsInWorld)
            {
                if (m_zoneScript != null)
                    m_zoneScript.OnGameObjectCreate(this);

                GetMap().GetObjectsStore().Add(GetGUID(), this);
                if (m_spawnId != 0)
                    GetMap().GetGameObjectBySpawnIdStore().Add(m_spawnId, this);

                // The state can be changed after GameObject.Create but before GameObject.AddToWorld
                bool toggledState = GetGoType() == GameObjectTypes.Chest ? GetLootState() == LootState.Ready : (GetGoState() == GameObjectState.Ready || IsTransport());
                if (m_model != null)
                {
                    Transport trans = ToTransport();
                    if (trans)
                        trans.SetDelayedAddModelToMap();
                    else
                        GetMap().InsertGameObjectModel(m_model);
                }

                EnableCollision(toggledState);
                base.AddToWorld();
            }
        }

        public override void RemoveFromWorld()
        {
            //- Remove the gameobject from the accessor
            if (IsInWorld)
            {
                if (m_zoneScript != null)
                    m_zoneScript.OnGameObjectRemove(this);

                RemoveFromOwner();
                if (m_model != null)
                    if (GetMap().ContainsGameObjectModel(m_model))
                        GetMap().RemoveGameObjectModel(m_model);
                base.RemoveFromWorld();

                if (m_spawnId != 0)
                    GetMap().GetGameObjectBySpawnIdStore().Remove(m_spawnId, this);
                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        public static GameObject CreateGameObject(uint entry, Map map, Position pos, Quaternion rotation, uint animProgress, GameObjectState goState, uint artKit = 0)
        {
            GameObjectTemplate goInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);
            if (goInfo == null)
                return null;

            GameObject go = new();
            if (!go.Create(entry, map, pos, rotation, animProgress, goState, artKit, false, 0))
                return null;

            return go;
        }

        public static GameObject CreateGameObjectFromDB(ulong spawnId, Map map, bool addToMap = true)
        {
            GameObject go = new();
            if (!go.LoadFromDB(spawnId, map, addToMap))
                return null;

            return go;
        }

        bool Create(uint entry, Map map, Position pos, Quaternion rotation, uint animProgress, GameObjectState goState, uint artKit, bool dynamic, ulong spawnid)
        {
            Cypher.Assert(map);
            SetMap(map);

            Relocate(pos);
            StationaryPosition.Relocate(pos);
            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Server, "Gameobject (Spawn id: {0} Entry: {1}) not created. Suggested coordinates isn't valid (X: {2} Y: {3})", GetSpawnId(), entry, pos.GetPositionX(), pos.GetPositionY());
                return false;
            }

            // Set if this object can handle dynamic spawns
            if (!dynamic)
                SetRespawnCompatibilityMode();

            UpdatePositionData();

            SetZoneScript();
            if (m_zoneScript != null)
            {
                entry = m_zoneScript.GetGameObjectEntry(m_spawnId, entry);
                if (entry == 0)
                    return false;
            }

            GameObjectTemplate goInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);
            if (goInfo == null)
            {
                Log.outError(LogFilter.Sql, "Gameobject (Spawn id: {0} Entry: {1}) not created: non-existing entry in `gameobject_template`. Map: {2} (X: {3} Y: {4} Z: {5})", GetSpawnId(), entry, map.GetId(), pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ());
                return false;
            }

            if (goInfo.type == GameObjectTypes.MapObjTransport)
            {
                Log.outError(LogFilter.Sql, "Gameobject (Spawn id: {0} Entry: {1}) not created: gameobject type GAMEOBJECT_TYPE_MAP_OBJ_TRANSPORT cannot be manually created.", GetSpawnId(), entry);
                return false;
            }

            ObjectGuid guid;
            if (goInfo.type != GameObjectTypes.Transport)
                guid = ObjectGuid.Create(HighGuid.GameObject, map.GetId(), goInfo.entry, map.GenerateLowGuid(HighGuid.GameObject));
            else
            {
                guid = ObjectGuid.Create(HighGuid.Transport, map.GenerateLowGuid(HighGuid.Transport));
                m_updateFlag.ServerTime = true;
            }

            _Create(guid);

            m_goInfo = goInfo;
            m_goTemplateAddon = Global.ObjectMgr.GetGameObjectTemplateAddon(entry);

            if (goInfo.type >= GameObjectTypes.Max)
            {
                Log.outError(LogFilter.Sql, "Gameobject (Spawn id: {0} Entry: {1}) not created: non-existing GO type '{2}' in `gameobject_template`. It will crash client if created.", GetSpawnId(), entry, goInfo.type);
                return false;
            }

            SetLocalRotation(rotation.X, rotation.Y, rotation.Z, rotation.W);
            GameObjectAddon gameObjectAddon = Global.ObjectMgr.GetGameObjectAddon(GetSpawnId());

            // For most of gameobjects is (0, 0, 0, 1) quaternion, there are only some transports with not standard rotation
            Quaternion parentRotation = Quaternion.Identity;
            if (gameObjectAddon != null)
                parentRotation = gameObjectAddon.ParentRotation;

            SetParentRotation(parentRotation);

            SetObjectScale(goInfo.size);

            GameObjectOverride goOverride = GetGameObjectOverride();
            if (goOverride != null)
            {
                SetFaction(goOverride.Faction);
                SetFlags(goOverride.Flags);
            }

            if (m_goTemplateAddon != null)
            {
                if (m_goTemplateAddon.WorldEffectID != 0)
                {
                    m_updateFlag.GameObject = true;
                    SetWorldEffectID(m_goTemplateAddon.WorldEffectID);
                }

                if (m_goTemplateAddon.AIAnimKitID != 0)
                    _animKitId = (ushort)m_goTemplateAddon.AIAnimKitID;
            }

            SetEntry(goInfo.entry);

            // set name for logs usage, doesn't affect anything ingame
            SetName(goInfo.name);

            SetDisplayId(goInfo.displayId);

            CreateModel();

            // GAMEOBJECT_BYTES_1, index at 0, 1, 2 and 3
            SetGoType(goInfo.type);
            m_prevGoState = goState;
            SetGoState(goState);
            SetGoArtKit((byte)artKit);

            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.SpawnTrackingStateAnimID), Global.DB2Mgr.GetEmptyAnimStateID());

            switch (goInfo.type)
            {
                case GameObjectTypes.FishingHole:
                    SetGoAnimProgress(animProgress);
                    m_goValue.FishingHole.MaxOpens = RandomHelper.URand(GetGoInfo().FishingHole.minRestock, GetGoInfo().FishingHole.maxRestock);
                    break;
                case GameObjectTypes.DestructibleBuilding:
                    m_goValue.Building.Health = 20000;//goinfo.DestructibleBuilding.intactNumHits + goinfo.DestructibleBuilding.damagedNumHits;
                    m_goValue.Building.MaxHealth = m_goValue.Building.Health;
                    SetGoAnimProgress(255);
                    // yes, even after the updatefield rewrite this garbage hack is still in client
                    SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.ParentRotation), new Quaternion(goInfo.DestructibleBuilding.DestructibleModelRec, 0f, 0f, 0f));
                    break;
                case GameObjectTypes.Transport:
                    m_goValue.Transport.AnimationInfo = Global.TransportMgr.GetTransportAnimInfo(goInfo.entry);
                    m_goValue.Transport.PathProgress = Time.GetMSTime();
                    if (!m_goValue.Transport.AnimationInfo.Path.Empty())
                        m_goValue.Transport.PathProgress -= m_goValue.Transport.PathProgress % GetTransportPeriod();    // align to period
                    m_goValue.Transport.CurrentSeg = 0;
                    m_goValue.Transport.StateUpdateTimer = 0;
                    m_goValue.Transport.StopFrames = new List<uint>();
                    if (goInfo.Transport.Timeto2ndfloor > 0)
                        m_goValue.Transport.StopFrames.Add(goInfo.Transport.Timeto2ndfloor);
                    if (goInfo.Transport.Timeto3rdfloor > 0)
                        m_goValue.Transport.StopFrames.Add(goInfo.Transport.Timeto3rdfloor);
                    if (goInfo.Transport.Timeto4thfloor > 0)
                        m_goValue.Transport.StopFrames.Add(goInfo.Transport.Timeto4thfloor);
                    if (goInfo.Transport.Timeto5thfloor > 0)
                        m_goValue.Transport.StopFrames.Add(goInfo.Transport.Timeto5thfloor);
                    if (goInfo.Transport.Timeto6thfloor > 0)
                        m_goValue.Transport.StopFrames.Add(goInfo.Transport.Timeto6thfloor);
                    if (goInfo.Transport.Timeto7thfloor > 0)
                        m_goValue.Transport.StopFrames.Add(goInfo.Transport.Timeto7thfloor);
                    if (goInfo.Transport.Timeto8thfloor > 0)
                        m_goValue.Transport.StopFrames.Add(goInfo.Transport.Timeto8thfloor);
                    if (goInfo.Transport.Timeto9thfloor > 0)
                        m_goValue.Transport.StopFrames.Add(goInfo.Transport.Timeto9thfloor);
                    if (goInfo.Transport.Timeto10thfloor > 0)
                        m_goValue.Transport.StopFrames.Add(goInfo.Transport.Timeto10thfloor);

                    if (goInfo.Transport.startOpen != 0)
                        SetTransportState(GameObjectState.TransportStopped, goInfo.Transport.startOpen - 1);
                    else
                        SetTransportState(GameObjectState.TransportActive);

                    SetGoAnimProgress(animProgress);
                    break;
                case GameObjectTypes.FishingNode:
                    SetLevel(1);
                    SetGoAnimProgress(255);
                    break;
                case GameObjectTypes.Trap:
                    if (goInfo.Trap.stealthed != 0)
                    {
                        m_stealth.AddFlag(StealthType.Trap);
                        m_stealth.AddValue(StealthType.Trap, 70);
                    }

                    if (goInfo.Trap.stealthAffected != 0)
                    {
                        m_invisibility.AddFlag(InvisibilityType.Trap);
                        m_invisibility.AddValue(InvisibilityType.Trap, 300);
                    }
                    break;
                case GameObjectTypes.PhaseableMo:
                    RemoveFlag((GameObjectFlags)0xF00);
                    AddFlag((GameObjectFlags)((m_goInfo.PhaseableMO.AreaNameSet & 0xF) << 8));
                    break;
                case GameObjectTypes.CapturePoint:
                    SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.SpellVisualID), m_goInfo.CapturePoint.SpellVisual1);
                    break;
                default:
                    SetGoAnimProgress(animProgress);
                    break;
            }

            if (gameObjectAddon != null)
            {
                if (gameObjectAddon.invisibilityValue != 0)
                {
                    m_invisibility.AddFlag(gameObjectAddon.invisibilityType);
                    m_invisibility.AddValue(gameObjectAddon.invisibilityType, gameObjectAddon.invisibilityValue);
                }

                if (gameObjectAddon.WorldEffectID != 0)
                {
                    m_updateFlag.GameObject = true;
                    SetWorldEffectID(gameObjectAddon.WorldEffectID);
                }

                if (gameObjectAddon.AIAnimKitID != 0)
                    _animKitId = (ushort)gameObjectAddon.AIAnimKitID;
            }

            LastUsedScriptID = GetGoInfo().ScriptId;
            AIM_Initialize();

            // Initialize loot duplicate count depending on raid difficulty
            if (map.Is25ManRaid())
                loot.maxDuplicates = 3;

            if (spawnid != 0)
                m_spawnId = spawnid;

            uint linkedEntry = GetGoInfo().GetLinkedGameObjectEntry();
            if (linkedEntry != 0)
            {
                GameObject linkedGo = CreateGameObject(linkedEntry, map, pos, rotation, 255, GameObjectState.Ready);
                if (linkedGo != null)
                {
                    SetLinkedTrap(linkedGo);
                    if (!map.AddToMap(linkedGo))
                        linkedGo.Dispose();
                }
            }

            // Check if GameObject is Infinite
            if (goInfo.IsInfiniteGameObject())
                SetVisibilityDistanceOverride(VisibilityDistanceType.Infinite);

            // Check if GameObject is Gigantic
            if (goInfo.IsGiganticGameObject())
                SetVisibilityDistanceOverride(VisibilityDistanceType.Gigantic);

            // Check if GameObject is Large
            if (goInfo.IsLargeGameObject())
                SetVisibilityDistanceOverride(VisibilityDistanceType.Large);

            return true;
        }

        public override void Update(uint diff)
        {
            m_Events.Update(diff);

            if (GetAI() != null)
                GetAI().UpdateAI(diff);
            else if (!AIM_Initialize())
                Log.outError(LogFilter.Server, "Could not initialize GameObjectAI");

            if (m_despawnDelay != 0)
            {
                if (m_despawnDelay > diff)
                    m_despawnDelay -= diff;
                else
                {
                    m_despawnDelay = 0;
                    DespawnOrUnsummon(TimeSpan.FromMilliseconds(0), m_despawnRespawnTime);
                }
            }

            switch (m_lootState)
            {
                case LootState.NotReady:
                {
                    switch (GetGoType())
                    {
                        case GameObjectTypes.Trap:
                        {
                            // Arming Time for GAMEOBJECT_TYPE_TRAP (6)
                            GameObjectTemplate goInfo = GetGoInfo();

                            // Bombs
                            Unit owner = GetOwner();
                            if (goInfo.Trap.charges == 2)
                                m_cooldownTime = GameTime.GetGameTimeMS() + 10 * Time.InMilliseconds;   // Hardcoded tooltip value
                            else if (owner)
                            {
                                if (owner.IsInCombat())
                                    m_cooldownTime = GameTime.GetGameTimeMS() + goInfo.Trap.startDelay * Time.InMilliseconds;
                            }
                            m_lootState = LootState.Ready;
                            break;
                        }
                        case GameObjectTypes.Transport:
                            if (m_goValue.Transport.AnimationInfo.Path.Empty())
                                break;

                            m_goValue.Transport.PathProgress += diff;

                            if (GetGoState() == GameObjectState.TransportActive)
                            {
                                m_goValue.Transport.PathProgress += diff;
                                /* TODO: Fix movement in unloaded grid - currently GO will just disappear
                                public uint timer = m_goValue.Transport.PathProgress % GetTransportPeriod();
                                TransportAnimationEntry const* node = m_goValue.Transport.AnimationInfo.GetAnimNode(timer);
                                if (node && m_goValue.Transport.CurrentSeg != node.TimeSeg)
                                {
                                m_goValue.Transport.CurrentSeg = node.TimeSeg;

                                G3D.Quat rotation = m_goValue.Transport.AnimationInfo.GetAnimRotation(timer);
                                G3D.Vector3 pos = rotation.toRotationMatrix()
                                G3D.Matrix3.fromEulerAnglesZYX(GetOrientation(), 0.0f, 0.0f)
                                G3D.Vector3(node.X, node.Y, node.Z);

                                pos += G3D.Vector3(GetStationaryX(), GetStationaryY(), GetStationaryZ());

                                G3D.Vector3 src(GetPositionX(), GetPositionY(), GetPositionZ());

                                TC_LOG_DEBUG("misc", "Src: {0} Dest: {1}", src.toString().c_str(), pos.toString().c_str());

                                GetMap().GameObjectRelocation(this, pos.x, pos.y, pos.z, GetOrientation());
                                }
                                */
                                if (!m_goValue.Transport.StopFrames.Empty())
                                {
                                    uint visualStateBefore = (m_goValue.Transport.StateUpdateTimer / 20000) & 1;
                                    m_goValue.Transport.StateUpdateTimer += diff;
                                    uint visualStateAfter = (m_goValue.Transport.StateUpdateTimer / 20000) & 1;
                                    if (visualStateBefore != visualStateAfter)
                                    {
                                        m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.Level);
                                        m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.State);
                                        ForceUpdateFieldChange();
                                    }
                                }
                            }
                            break;
                        case GameObjectTypes.FishingNode:
                        {
                            // fishing code (bobber ready)
                            if (GameTime.GetGameTime() > m_respawnTime - 5)
                            {
                                // splash bobber (bobber ready now)
                                Unit caster = GetOwner();
                                if (caster != null && caster.IsTypeId(TypeId.Player))
                                {
                                    SetGoState(GameObjectState.Active);
                                    SetFlags(GameObjectFlags.NoDespawn);

                                    UpdateData udata = new(caster.GetMapId());
                                    UpdateObject packet;
                                    BuildValuesUpdateBlockForPlayer(udata, caster.ToPlayer());
                                    udata.BuildPacket(out packet);
                                    caster.ToPlayer().SendPacket(packet);

                                    SendCustomAnim(GetGoAnimProgress());
                                }

                                m_lootState = LootState.Ready;                 // can be successfully open with some chance
                            }
                            return;
                        }
                        case GameObjectTypes.Chest:
                            if (m_restockTime > GameTime.GetGameTime())
                                return;
                            // If there is no restock timer, or if the restock timer passed, the chest becomes ready to loot
                            m_restockTime = 0;
                            m_lootState = LootState.Ready;
                            AddToObjectUpdateIfNeeded();
                            break;
                        default:
                            m_lootState = LootState.Ready;                         // for other GOis same switched without delay to GO_READY
                            break;
                    }
                }
                goto case LootState.Ready;
                case LootState.Ready:
                {
                    if (m_respawnCompatibilityMode)
                    {
                        if (m_respawnTime > 0)                          // timer on
                        {
                            long now = GameTime.GetGameTime();
                            if (m_respawnTime <= now)            // timer expired
                            {
                                ObjectGuid dbtableHighGuid = ObjectGuid.Create(HighGuid.GameObject, GetMapId(), GetEntry(), m_spawnId);
                                long linkedRespawntime = GetMap().GetLinkedRespawnTime(dbtableHighGuid);
                                if (linkedRespawntime != 0)             // Can't respawn, the master is dead
                                {
                                    ObjectGuid targetGuid = Global.ObjectMgr.GetLinkedRespawnGuid(dbtableHighGuid);
                                    if (targetGuid == dbtableHighGuid) // if linking self, never respawn (check delayed to next day)
                                        SetRespawnTime(Time.Week);
                                    else
                                        m_respawnTime = (now > linkedRespawntime ? now : linkedRespawntime) + RandomHelper.IRand(5, Time.Minute); // else copy time from master and add a little
                                    SaveRespawnTime();
                                    return;
                                }

                                m_respawnTime = 0;
                                m_SkillupList.Clear();
                                m_usetimes = 0;

                                switch (GetGoType())
                                {
                                    case GameObjectTypes.FishingNode:   //  can't fish now
                                    {
                                        Unit caster = GetOwner();
                                        if (caster != null && caster.IsTypeId(TypeId.Player))
                                        {
                                            caster.ToPlayer().RemoveGameObject(this, false);
                                            caster.ToPlayer().SendPacket(new FishEscaped());
                                        }
                                        // can be delete
                                        m_lootState = LootState.JustDeactivated;
                                        return;
                                    }
                                    case GameObjectTypes.Door:
                                    case GameObjectTypes.Button:
                                        //we need to open doors if they are closed (add there another condition if this code breaks some usage, but it need to be here for Battlegrounds)
                                        if (GetGoState() != GameObjectState.Ready)
                                            ResetDoorOrButton();
                                        break;
                                    case GameObjectTypes.FishingHole:
                                        // Initialize a new max fish count on respawn
                                        m_goValue.FishingHole.MaxOpens = RandomHelper.URand(GetGoInfo().FishingHole.minRestock, GetGoInfo().FishingHole.maxRestock);
                                        break;
                                    default:
                                        break;
                                }

                                if (!m_spawnedByDefault)        // despawn timer
                                {
                                    // can be despawned or destroyed
                                    SetLootState(LootState.JustDeactivated);
                                    return;
                                }

                                // Call AI Reset (required for example in SmartAI to clear one time events)
                                if (GetAI() != null)
                                    GetAI().Reset();

                                // respawn timer
                                uint poolid = GetSpawnId() != 0 ? Global.PoolMgr.IsPartOfAPool<GameObject>(GetSpawnId()) : 0;
                                if (poolid != 0)
                                    Global.PoolMgr.UpdatePool<GameObject>(poolid, GetSpawnId());
                                else
                                    GetMap().AddToMap(this);
                            }
                        }
                    }

                    // Set respawn timer
                    if (!m_respawnCompatibilityMode && m_respawnTime > 0)
                        SaveRespawnTime();

                    if (IsSpawned())
                    {
                        GameObjectTemplate goInfo = GetGoInfo();
                        uint max_charges;
                        if (goInfo.type == GameObjectTypes.Trap)
                        {
                            if (GameTime.GetGameTimeMS() < m_cooldownTime)
                                break;

                            // Type 2 (bomb) does not need to be triggered by a unit and despawns after casting its spell.
                            if (goInfo.Trap.charges == 2)
                            {
                                SetLootState(LootState.Activated);
                                break;
                            }

                            // Type 0 despawns after being triggered, type 1 does not.
                            // @todo This is activation radius. Casting radius must be selected from spell 
                            float radius;
                            if (goInfo.Trap.radius == 0f)
                            {
                                // Battlegroundgameobjects have data2 == 0 && data5 == 3
                                if (goInfo.Trap.cooldown != 3)
                                    break;

                                radius = 3.0f;
                            }
                            else
                                radius = goInfo.Trap.radius / 2.0f;

                            Unit target;
                            // @todo this hack with search required until GO casting not implemented
                            Unit owner = GetOwner();
                            if (owner)
                            {
                                // Hunter trap: Search units which are unfriendly to the trap's owner
                                var checker = new NearestAttackableNoTotemUnitInObjectRangeCheck(this, radius);
                                var searcher = new UnitLastSearcher(this, checker);
                                Cell.VisitAllObjects(this, searcher, radius);
                                target = searcher.GetTarget();
                            }
                            else
                            {
                                // Environmental trap: Any player
                                var check = new AnyPlayerInObjectRangeCheck(this, radius);
                                var searcher = new PlayerSearcher(this, check);
                                Cell.VisitWorldObjects(this, searcher, radius);
                                target = searcher.GetTarget();
                            }

                            if (target)
                                SetLootState(LootState.Activated, target);
                        }
                        else if ((max_charges = goInfo.GetCharges()) != 0)
                        {
                            if (m_usetimes >= max_charges)
                            {
                                m_usetimes = 0;
                                SetLootState(LootState.JustDeactivated);      // can be despawned or destroyed
                            }
                        }
                    }

                    break;
                }
                case LootState.Activated:
                {
                    switch (GetGoType())
                    {
                        case GameObjectTypes.Door:
                        case GameObjectTypes.Button:
                            if (m_cooldownTime != 0 && GameTime.GetGameTimeMS() >= m_cooldownTime)
                                ResetDoorOrButton();
                            break;
                        case GameObjectTypes.Goober:
                            if (GameTime.GetGameTimeMS() >= m_cooldownTime)
                            {
                                RemoveFlag(GameObjectFlags.InUse);

                                SetLootState(LootState.JustDeactivated);
                                m_cooldownTime = 0;
                            }
                            break;
                        case GameObjectTypes.Chest:
                            if (m_groupLootTimer != 0)
                            {
                                if (m_groupLootTimer <= diff)
                                {
                                    Group group = Global.GroupMgr.GetGroupByGUID(lootingGroupLowGUID);
                                    if (group)
                                        group.EndRoll(loot, GetMap());

                                    m_groupLootTimer = 0;
                                    lootingGroupLowGUID.Clear();
                                }
                                else
                                    m_groupLootTimer -= diff;
                            }

                            // Non-consumable chest was partially looted and restock time passed, restock all loot now
                            if (GetGoInfo().Chest.consumable == 0 && GameTime.GetGameTime() >= m_restockTime)
                            {
                                m_restockTime = 0;
                                m_lootState = LootState.Ready;
                                AddToObjectUpdateIfNeeded();
                            }
                            break;
                        case GameObjectTypes.Trap:
                        {
                            GameObjectTemplate goInfo = GetGoInfo();
                            Unit target = Global.ObjAccessor.GetUnit(this, m_lootStateUnitGUID);
                            if (goInfo.Trap.charges == 2 && goInfo.Trap.spell != 0)
                            {
                                //todo NULL target won't work for target type 1
                                CastSpell(null, goInfo.Trap.spell);
                                SetLootState(LootState.JustDeactivated);
                            }
                            else if (target)
                            {
                                // Some traps do not have a spell but should be triggered
                                CastSpellExtraArgs args = new();
                                args.SetOriginalCaster(GetOwnerGUID());
                                if (goInfo.Trap.spell != 0)
                                    CastSpell(target, goInfo.Trap.spell, args);

                                // Template value or 4 seconds
                                m_cooldownTime = (GameTime.GetGameTimeMS() + (goInfo.Trap.cooldown != 0 ? goInfo.Trap.cooldown : 4u)) * Time.InMilliseconds;

                                if (goInfo.Trap.charges == 1)
                                    SetLootState(LootState.JustDeactivated);
                                else if (goInfo.Trap.charges == 0)
                                    SetLootState(LootState.Ready);

                                // Battleground gameobjects have data2 == 0 && data5 == 3
                                if (goInfo.Trap.radius == 0 && goInfo.Trap.cooldown == 3)
                                {
                                    Player player = target.ToPlayer();
                                    if (player)
                                    {
                                        Battleground bg = player.GetBattleground();
                                        if (bg)
                                            bg.HandleTriggerBuff(GetGUID());
                                    }
                                }
                            }
                            break;
                        }
                        default:
                            break;
                    }
                    break;
                }
                case LootState.JustDeactivated:
                {
                    // If nearby linked trap exists, despawn it
                    GameObject linkedTrap = GetLinkedTrap();
                    if (linkedTrap)
                        linkedTrap.DespawnOrUnsummon();

                    //if Gameobject should cast spell, then this, but some GOs (type = 10) should be destroyed
                    if (GetGoType() == GameObjectTypes.Goober)
                    {
                        uint spellId = GetGoInfo().Goober.spell;

                        if (spellId != 0)
                        {
                            foreach (var id in m_unique_users)
                            {
                                // m_unique_users can contain only player GUIDs
                                Player owner = Global.ObjAccessor.GetPlayer(this, id);
                                if (owner != null)
                                    owner.CastSpell(owner, spellId, false);
                            }

                            m_unique_users.Clear();
                            m_usetimes = 0;
                        }

                        // Only goobers with a lock id or a reset time may reset their go state
                        if (GetGoInfo().GetLockId() != 0 || GetGoInfo().GetAutoCloseTime() != 0)
                            SetGoState(GameObjectState.Ready);

                        //any return here in case Battleground traps
                        GameObjectOverride goOverride = GetGameObjectOverride();
                        if (goOverride != null && goOverride.Flags.HasFlag(GameObjectFlags.NoDespawn))
                            return;
                    }

                    loot.Clear();

                    // Do not delete chests or goobers that are not consumed on loot, while still allowing them to despawn when they expire if summoned
                    bool isSummonedAndExpired = (GetOwner() != null || GetSpellId() != 0) && m_respawnTime == 0;
                    if ((GetGoType() == GameObjectTypes.Chest || GetGoType() == GameObjectTypes.Goober) && !GetGoInfo().IsDespawnAtAction() && !isSummonedAndExpired)
                    {
                        if (GetGoType() == GameObjectTypes.Chest && GetGoInfo().Chest.chestRestockTime > 0)
                        {
                            // Start restock timer when the chest is fully looted
                            m_restockTime = GameTime.GetGameTime() + GetGoInfo().Chest.chestRestockTime;
                            SetLootState(LootState.NotReady);
                            AddToObjectUpdateIfNeeded();
                        }
                        else
                            SetLootState(LootState.Ready);
                        UpdateObjectVisibility();
                        return;
                    }
                    else if (!GetOwnerGUID().IsEmpty() || GetSpellId() != 0)
                    {
                        SetRespawnTime(0);
                        Delete();
                        return;
                    }

                    SetLootState(LootState.NotReady);

                    //burning flags in some Battlegrounds, if you find better condition, just add it
                    if (GetGoInfo().IsDespawnAtAction() || GetGoAnimProgress() > 0)
                    {
                        SendGameObjectDespawn();
                        //reset flags
                        GameObjectOverride goOverride = GetGameObjectOverride();
                        if (goOverride != null)
                            SetFlags(goOverride.Flags);
                    }

                    if (m_respawnDelayTime == 0)
                        return;

                    if (!m_spawnedByDefault)
                    {
                        m_respawnTime = 0;

                        if (m_spawnId != 0)
                            DestroyForNearbyPlayers();
                        else
                            Delete();

                        return;
                    }

                    uint respawnDelay = m_respawnDelayTime;
                    uint scalingMode = WorldConfig.GetUIntValue(WorldCfg.RespawnDynamicMode);
                    if (scalingMode != 0)
                        GetMap().ApplyDynamicModeRespawnScaling(this, m_spawnId, ref respawnDelay, scalingMode);
                    m_respawnTime = GameTime.GetGameTime() + respawnDelay;

                    // if option not set then object will be saved at grid unload
                    // Otherwise just save respawn time to map object memory
                    SaveRespawnTime();

                    if (m_respawnCompatibilityMode)
                        DestroyForNearbyPlayers();
                    else
                        AddObjectToRemoveList();

                    break;
                }
            }
        }

        public GameObjectOverride GetGameObjectOverride()
        {
            if (m_spawnId != 0)
            {
                GameObjectOverride goOverride = Global.ObjectMgr.GetGameObjectOverride(m_spawnId);
                if (goOverride != null)
                    return goOverride;
            }

            return m_goTemplateAddon;
        }

        public void Refresh()
        {
            // not refresh despawned not casted GO (despawned casted GO destroyed in all cases anyway)
            if (m_respawnTime > 0 && m_spawnedByDefault)
                return;

            if (IsSpawned())
                GetMap().AddToMap(this);
        }

        public void AddUniqueUse(Player player)
        {
            AddUse();
            m_unique_users.Add(player.GetGUID());
        }

        public void DespawnOrUnsummon(TimeSpan delay = default, TimeSpan forceRespawnTime = default)
        {
            if (delay > TimeSpan.Zero)
            {
                if (m_despawnDelay == 0 || m_despawnDelay > delay.TotalMilliseconds)
                {
                    m_despawnDelay = (uint)delay.TotalMilliseconds;
                    m_despawnRespawnTime = forceRespawnTime;
                }
            }
            else
            {
                if (m_goData != null)
                {
                    uint respawnDelay = (uint)((forceRespawnTime > TimeSpan.Zero) ? forceRespawnTime.TotalSeconds : m_respawnDelayTime);
                    SaveRespawnTime(respawnDelay);
                }
                Delete();
            }
        }

        public void Delete()
        {
            // If nearby linked trap exists, despawn it
            GameObject linkedTrap = GetLinkedTrap();
            if (linkedTrap != null)
                linkedTrap.DespawnOrUnsummon();

            SetLootState(LootState.NotReady);
            RemoveFromOwner();

            SendGameObjectDespawn();

            SetGoState(GameObjectState.Ready);
            GameObjectOverride goOverride = GetGameObjectOverride();
            if (goOverride != null)
                SetFlags(goOverride.Flags);

            uint poolid = GetSpawnId() != 0 ? Global.PoolMgr.IsPartOfAPool<GameObject>(GetSpawnId()) : 0;
            if (poolid != 0)
                Global.PoolMgr.UpdatePool<GameObject>(poolid, GetSpawnId());
            else
                AddObjectToRemoveList();
        }

        public void SendGameObjectDespawn()
        {
            GameObjectDespawn packet = new();
            packet.ObjectGUID = GetGUID();
            SendMessageToSet(packet, true);
        }

        public void GetFishLoot(Loot fishloot, Player loot_owner)
        {
            fishloot.Clear();

            uint zone, subzone;
            uint defaultzone = 1;
            GetZoneAndAreaId(out zone, out subzone);

            // if subzone loot exist use it
            fishloot.FillLoot(subzone, LootStorage.Fishing, loot_owner, true, true);
            if (fishloot.Empty())
            {
                //subzone no result,use zone loot
                fishloot.FillLoot(zone, LootStorage.Fishing, loot_owner, true);
                //use zone 1 as default, somewhere fishing got nothing,becase subzone and zone not set, like Off the coast of Storm Peaks.
                if (fishloot.Empty())
                    fishloot.FillLoot(defaultzone, LootStorage.Fishing, loot_owner, true, true);
            }
        }

        public void GetFishLootJunk(Loot fishloot, Player loot_owner)
        {
            fishloot.Clear();

            uint zone, subzone;
            uint defaultzone = 1;
            GetZoneAndAreaId(out zone, out subzone);

            // if subzone loot exist use it
            fishloot.FillLoot(subzone, LootStorage.Fishing, loot_owner, true, true, LootModes.JunkFish);
            if (fishloot.Empty())  //use this becase if zone or subzone has normal mask drop, then fishloot.FillLoot return true.
            {
                //use zone loot
                fishloot.FillLoot(zone, LootStorage.Fishing, loot_owner, true, true, LootModes.JunkFish);
                if (fishloot.Empty())
                    //use zone 1 as default
                    fishloot.FillLoot(defaultzone, LootStorage.Fishing, loot_owner, true, true, LootModes.JunkFish);
            }
        }

        public void SaveToDB()
        {
            // this should only be used when the gameobject has already been loaded
            // preferably after adding to map, because mapid may not be valid otherwise
            GameObjectData data = Global.ObjectMgr.GetGameObjectData(m_spawnId);
            if (data == null)
            {
                Log.outError(LogFilter.Maps, "GameObject.SaveToDB failed, cannot get gameobject data!");
                return;
            }

            SaveToDB(GetMapId(), data.SpawnDifficulties);
        }

        public void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties)
        {
            GameObjectTemplate goI = GetGoInfo();

            if (goI == null)
                return;

            if (m_spawnId == 0)
                m_spawnId = Global.ObjectMgr.GenerateGameObjectSpawnId();

            // update in loaded data (changing data only in this place)
            GameObjectData data = Global.ObjectMgr.NewOrExistGameObjectData(m_spawnId);

            if (data.SpawnId == 0)
                data.SpawnId = m_spawnId;
            Cypher.Assert(data.SpawnId == m_spawnId);

            data.Id = GetEntry();
            data.MapId = GetMapId();
            data.SpawnPoint.Relocate(this);
            data.rotation = m_localRotation;
            data.spawntimesecs = (int)(m_spawnedByDefault ? m_respawnDelayTime : -m_respawnDelayTime);
            data.animprogress = GetGoAnimProgress();
            data.goState = GetGoState();
            data.SpawnDifficulties = spawnDifficulties;
            data.artKit = (byte)GetGoArtKit();
            if (data.spawnGroupData == null)
                data.spawnGroupData = Global.ObjectMgr.GetDefaultSpawnGroup();

            data.PhaseId = GetDBPhase() > 0 ? (uint)GetDBPhase() : data.PhaseId;
            data.PhaseGroup = GetDBPhase() < 0 ? (uint)-GetDBPhase() : data.PhaseGroup;

            // Update in DB
            byte index = 0;
            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT);
            stmt.AddValue(0, m_spawnId);
            DB.World.Execute(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.INS_GAMEOBJECT);
            stmt.AddValue(index++, m_spawnId);
            stmt.AddValue(index++, GetEntry());
            stmt.AddValue(index++, mapid);
            stmt.AddValue(index++, data.SpawnDifficulties.Empty() ? "" : string.Join(",", data.SpawnDifficulties));
            stmt.AddValue(index++, data.PhaseId);
            stmt.AddValue(index++, data.PhaseGroup);
            stmt.AddValue(index++, GetPositionX());
            stmt.AddValue(index++, GetPositionY());
            stmt.AddValue(index++, GetPositionZ());
            stmt.AddValue(index++, GetOrientation());
            stmt.AddValue(index++, m_localRotation.X);
            stmt.AddValue(index++, m_localRotation.Y);
            stmt.AddValue(index++, m_localRotation.Z);
            stmt.AddValue(index++, m_localRotation.W);
            stmt.AddValue(index++, m_respawnDelayTime);
            stmt.AddValue(index++, GetGoAnimProgress());
            stmt.AddValue(index++, (byte)GetGoState());
            DB.World.Execute(stmt);
        }

        public override bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool unused = true)
        {
            GameObjectData data = Global.ObjectMgr.GetGameObjectData(spawnId);
            if (data == null)
            {
                Log.outError(LogFilter.Maps, "Gameobject (SpawnId: {0}) not found in table `gameobject`, can't load. ", spawnId);
                return false;
            }

            uint entry = data.Id;

            uint animprogress = data.animprogress;
            GameObjectState go_state = data.goState;
            uint artKit = data.artKit;

            m_spawnId = spawnId;
            m_respawnCompatibilityMode = ((data.spawnGroupData.flags & SpawnGroupFlags.CompatibilityMode) != 0);
            if (!Create(entry, map, data.SpawnPoint, data.rotation, animprogress, go_state, artKit, !m_respawnCompatibilityMode, spawnId))
                return false;

            PhasingHandler.InitDbPhaseShift(GetPhaseShift(), data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
            PhasingHandler.InitDbVisibleMapId(GetPhaseShift(), data.terrainSwapMap);

            if (data.spawntimesecs >= 0)
            {
                m_spawnedByDefault = true;

                if (!GetGoInfo().GetDespawnPossibility() && !GetGoInfo().IsDespawnAtAction())
                {
                    AddFlag(GameObjectFlags.NoDespawn);
                    m_respawnDelayTime = 0;
                    m_respawnTime = 0;
                }
                else
                {
                    m_respawnDelayTime = (uint)data.spawntimesecs;
                    m_respawnTime = GetMap().GetGORespawnTime(m_spawnId);

                    // ready to respawn
                    if (m_respawnTime != 0 && m_respawnTime <= GameTime.GetGameTime())
                    {
                        m_respawnTime = 0;
                        GetMap().RemoveRespawnTime(SpawnObjectType.GameObject, m_spawnId);
                    }
                }
            }
            else
            {
                if (!m_respawnCompatibilityMode)
                {
                    Log.outWarn(LogFilter.Sql, $"GameObject {entry} (SpawnID {spawnId}) is not spawned by default, but tries to use a non-hack spawn system. This will not work. Defaulting to compatibility mode.");
                    m_respawnCompatibilityMode = true;
                }

                m_spawnedByDefault = false;
                m_respawnDelayTime = (uint)-data.spawntimesecs;
                m_respawnTime = 0;
            }

            m_goData = data;

            if (addToMap && !GetMap().AddToMap(this))
                return false;

            return true;
        }

        public static bool DeleteFromDB(ulong spawnId)
        {
            GameObjectData data = Global.ObjectMgr.GetGameObjectData(spawnId);
            if (data == null)
                return false;

            SQLTransaction trans = new();

            Global.MapMgr.DoForAllMapsWithMapId(data.MapId, map =>
            {
                // despawn all active objects, and remove their respawns
                List<GameObject> toUnload = new();
                foreach (var creature in map.GetGameObjectBySpawnIdStore().LookupByKey(spawnId))
                    toUnload.Add(creature);

                foreach (GameObject obj in toUnload)
                    map.AddObjectToRemoveList(obj);

                map.RemoveRespawnTime(SpawnObjectType.GameObject, spawnId, trans);
            });

            // delete data from memory
            Global.ObjectMgr.DeleteGameObjectData(spawnId);

            trans = new();

            // ... and the database
            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_EVENT_GAMEOBJECT);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToGO);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToCreature);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToGO);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToGO);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT_ADDON);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            DB.World.CommitTransaction(trans);

            return true;
        }

        public override bool HasQuest(uint questId)
        {
            return Global.ObjectMgr.GetGOQuestRelations(GetEntry()).HasQuest(questId);
        }

        public override bool HasInvolvedQuest(uint questId)
        {
            return Global.ObjectMgr.GetGOQuestInvolvedRelations(GetEntry()).HasQuest(questId);
        }

        public bool IsTransport()
        {
            // If something is marked as a transport, don't transmit an out of range packet for it.
            GameObjectTemplate gInfo = GetGoInfo();
            if (gInfo == null)
                return false;

            return gInfo.type == GameObjectTypes.Transport || gInfo.type == GameObjectTypes.MapObjTransport;
        }

        // is Dynamic transport = non-stop Transport
        public bool IsDynTransport()
        {
            // If something is marked as a transport, don't transmit an out of range packet for it.
            GameObjectTemplate gInfo = GetGoInfo();
            if (gInfo == null)
                return false;

            return gInfo.type == GameObjectTypes.MapObjTransport || (gInfo.type == GameObjectTypes.Transport && m_goValue.Transport.StopFrames.Empty());
        }

        public bool IsDestructibleBuilding()
        {
            GameObjectTemplate gInfo = GetGoInfo();
            if (gInfo == null)
                return false;

            return gInfo.type == GameObjectTypes.DestructibleBuilding;
        }

        public Transport ToTransport() { return GetGoInfo().type == GameObjectTypes.MapObjTransport ? (this as Transport) : null; }

        public void SaveRespawnTime(uint forceDelay = 0)
        {
            if (m_goData != null && (forceDelay != 0 || m_respawnTime > GameTime.GetGameTime()) && m_spawnedByDefault)
            {
                if (m_respawnCompatibilityMode)
                {
                    RespawnInfo ri = new();
                    ri.type = SpawnObjectType.GameObject;
                    ri.spawnId = m_spawnId;
                    ri.respawnTime = m_respawnTime;
                    GetMap().SaveRespawnInfoDB(ri);
                    return;
                }

                long thisRespawnTime = forceDelay != 0 ? GameTime.GetGameTime() + forceDelay : m_respawnTime;
                GetMap().SaveRespawnTime(SpawnObjectType.GameObject, m_spawnId, GetEntry(), thisRespawnTime, GridDefines.ComputeGridCoord(GetPositionX(), GetPositionY()).GetId());
            }
        }

        public override bool IsNeverVisibleFor(WorldObject seer)
        {
            if (base.IsNeverVisibleFor(seer))
                return true;

            if (GetGoType() == GameObjectTypes.SpellFocus && GetGoInfo().SpellFocus.serverOnly == 1)
                return true;

            if (GetDisplayId() == 0)
                return true;

            return false;
        }

        public override bool IsAlwaysVisibleFor(WorldObject seer)
        {
            if (base.IsAlwaysVisibleFor(seer))
                return true;

            if (IsTransport() || IsDestructibleBuilding())
                return true;

            if (seer == null)
                return false;

            // Always seen by owner and friendly units
            ObjectGuid guid = GetOwnerGUID();
            if (!guid.IsEmpty())
            {
                if (seer.GetGUID() == guid)
                    return true;

                Unit owner = GetOwner();
                if (owner != null && seer.IsTypeMask(TypeMask.Unit) && owner.IsFriendlyTo(seer.ToUnit()))
                    return true;
            }

            return false;
        }

        public override bool IsInvisibleDueToDespawn()
        {
            if (base.IsInvisibleDueToDespawn())
                return true;

            // Despawned
            if (!IsSpawned())
                return true;

            return false;
        }

        public void Respawn()
        {
            if (m_spawnedByDefault && m_respawnTime > 0)
            {
                m_respawnTime = GameTime.GetGameTime();
                GetMap().Respawn(SpawnObjectType.GameObject, m_spawnId);
            }
        }

        public bool ActivateToQuest(Player target)
        {
            if (target.HasQuestForGO((int)GetEntry()))
                return true;

            if (!Global.ObjectMgr.IsGameObjectForQuests(GetEntry()))
                return false;

            switch (GetGoType())
            {
                case GameObjectTypes.QuestGiver:
                    QuestGiverStatus questStatus = target.GetQuestDialogStatus(this);
                    if (questStatus != QuestGiverStatus.None && questStatus != QuestGiverStatus.Future)
                        return true;
                    break;
                case GameObjectTypes.Chest:
                {
                    // Chests become inactive while not ready to be looted
                    if (GetLootState() == LootState.NotReady)
                        return false;

                    // scan GO chest with loot including quest items
                    if (LootStorage.Gameobject.HaveQuestLootForPlayer(GetGoInfo().GetLootId(), target))
                    {
                        Battleground bg = target.GetBattleground();
                        if (bg)
                            return bg.CanActivateGO((int)GetEntry(), (uint)target.GetTeam());
                        return true;
                    }
                    break;
                }
                case GameObjectTypes.Generic:
                {
                    if (target.GetQuestStatus(GetGoInfo().Generic.questID) == QuestStatus.Incomplete)
                        return true;
                    break;
                }
                case GameObjectTypes.Goober:
                {
                    if (target.GetQuestStatus(GetGoInfo().Goober.questID) == QuestStatus.Incomplete)
                        return true;
                    break;
                }
                default:
                    break;
            }
            return false;
        }

        public void TriggeringLinkedGameObject(uint trapEntry, Unit target)
        {
            GameObjectTemplate trapInfo = Global.ObjectMgr.GetGameObjectTemplate(trapEntry);
            if (trapInfo == null || trapInfo.type != GameObjectTypes.Trap)
                return;

            SpellInfo trapSpell = Global.SpellMgr.GetSpellInfo(trapInfo.Trap.spell, GetMap().GetDifficultyID());
            if (trapSpell == null)                                          // checked at load already
                return;

            GameObject trapGO = GetLinkedTrap();
            if (trapGO)
                trapGO.CastSpell(target, trapSpell.Id);
        }

        GameObject LookupFishingHoleAround(float range)
        {
            var u_check = new NearestGameObjectFishingHole(this, range);
            var checker = new GameObjectSearcher(this, u_check);

            Cell.VisitGridObjects(this, checker, range);
            return checker.GetTarget();
        }

        public void ResetDoorOrButton()
        {
            if (m_lootState == LootState.Ready || m_lootState == LootState.JustDeactivated)
                return;

            RemoveFlag(GameObjectFlags.InUse);
            SetGoState(m_prevGoState);

            SetLootState(LootState.JustDeactivated);
            m_cooldownTime = 0;
        }

        public void UseDoorOrButton(uint time_to_restore = 0, bool alternative = false, Unit user = null)
        {
            if (m_lootState != LootState.Ready)
                return;

            if (time_to_restore == 0)
                time_to_restore = GetGoInfo().GetAutoCloseTime();

            SwitchDoorOrButton(true, alternative);
            SetLootState(LootState.Activated, user);

            m_cooldownTime = time_to_restore != 0 ? GameTime.GetGameTimeMS() + time_to_restore : 0;
        }

        public void SetGoArtKit(byte kit)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.ArtKit), kit);
            GameObjectData data = Global.ObjectMgr.GetGameObjectData(m_spawnId);
            if (data != null)
                data.artKit = kit;
        }

        public void SetGoArtKit(byte artkit, GameObject go, uint lowguid)
        {
            GameObjectData data = null;
            if (go != null)
            {
                go.SetGoArtKit(artkit);
                data = go.GetGameObjectData();
            }
            else if (lowguid != 0)
                data = Global.ObjectMgr.GetGameObjectData(lowguid);

            if (data != null)
                data.artKit = artkit;
        }

        void SwitchDoorOrButton(bool activate, bool alternative = false)
        {
            if (activate)
                AddFlag(GameObjectFlags.InUse);
            else
                RemoveFlag(GameObjectFlags.InUse);

            if (GetGoState() == GameObjectState.Ready)                      //if closed . open
                SetGoState(alternative ? GameObjectState.Destroyed : GameObjectState.Active);
            else                                                    //if open . close
                SetGoState(GameObjectState.Ready);
        }

        public void Use(Unit user)
        {
            // by default spell caster is user
            Unit spellCaster = user;
            uint spellId = 0;
            bool triggered = false;

            Player playerUser = user.ToPlayer();
            if (playerUser != null)
            {
                if (m_goInfo.GetNoDamageImmune() != 0 && playerUser.HasUnitFlag(UnitFlags.Immune))
                    return;

                if (!m_goInfo.IsUsableMounted())
                    playerUser.RemoveAurasByType(AuraType.Mounted);

                playerUser.PlayerTalkClass.ClearMenus();
                if (GetAI().GossipHello(playerUser))
                    return;
            }

            // If cooldown data present in template
            uint cooldown = GetGoInfo().GetCooldown();
            if (cooldown != 0)
            {
                if (m_cooldownTime > GameTime.GetGameTime())
                    return;

                m_cooldownTime = GameTime.GetGameTimeMS() + cooldown * Time.InMilliseconds;
            }

            switch (GetGoType())
            {
                case GameObjectTypes.Door:                          //0
                case GameObjectTypes.Button:                        //1
                    //doors/buttons never really despawn, only reset to default state/flags
                    UseDoorOrButton(0, false, user);
                    return;
                case GameObjectTypes.QuestGiver:                    //2
                {
                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    player.PrepareGossipMenu(this, GetGoInfo().QuestGiver.gossipID, true);
                    player.SendPreparedGossip(this);
                    return;
                }
                case GameObjectTypes.Trap:                          //6
                {
                    GameObjectTemplate goInfo = GetGoInfo();
                    if (goInfo.Trap.spell != 0)
                        CastSpell(user, goInfo.Trap.spell);

                    m_cooldownTime = GameTime.GetGameTimeMS() + (goInfo.Trap.cooldown != 0 ? goInfo.Trap.cooldown : 4) * Time.InMilliseconds;   // template or 4 seconds

                    if (goInfo.Trap.charges == 1)         // Deactivate after trigger
                        SetLootState(LootState.JustDeactivated);

                    return;
                }
                //Sitting: Wooden bench, chairs enzz
                case GameObjectTypes.Chair:                         //7
                {
                    GameObjectTemplate info = GetGoInfo();
                    if (info == null)
                        return;

                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    if (ChairListSlots.Empty())        // this is called once at first chair use to make list of available slots
                    {
                        if (info.Chair.chairslots > 0)     // sometimes chairs in DB have error in fields and we dont know number of slots
                        {
                            for (uint i = 0; i < info.Chair.chairslots; ++i)
                                ChairListSlots[i] = default; // Last user of current slot set to 0 (none sit here yet)
                        }
                        else
                            ChairListSlots[0] = default;     // error in DB, make one default slot
                    }

                    Player player = user.ToPlayer();

                    // a chair may have n slots. we have to calculate their positions and teleport the player to the nearest one
                    float lowestDist = SharedConst.DefaultVisibilityDistance;

                    uint nearest_slot = 0;
                    float x_lowest = GetPositionX();
                    float y_lowest = GetPositionY();

                    // the object orientation + 1/2 pi
                    // every slot will be on that straight line
                    float orthogonalOrientation = GetOrientation() + MathFunctions.PI * 0.5f;
                    // find nearest slot
                    bool found_free_slot = false;

                    foreach (var slot in ChairListSlots.ToList())
                    {
                        // the distance between this slot and the center of the go - imagine a 1D space
                        float relativeDistance = (info.size * slot.Key) - (info.size * (info.Chair.chairslots - 1) / 2.0f);

                        float x_i = (float)(GetPositionX() + relativeDistance * Math.Cos(orthogonalOrientation));
                        float y_i = (float)(GetPositionY() + relativeDistance * Math.Sin(orthogonalOrientation));

                        if (!slot.Value.IsEmpty())
                        {
                            Player ChairUser = Global.ObjAccessor.GetPlayer(this, slot.Value);
                            if (ChairUser != null)
                                if (ChairUser.IsSitState() && ChairUser.GetStandState() != UnitStandStateType.Sit && ChairUser.GetExactDist2d(x_i, y_i) < 0.1f)
                                    continue;        // This seat is already occupied by ChairUser. NOTE: Not sure if the ChairUser.getStandState() != UNIT_STAND_STATE_SIT check is required.
                                else
                                    ChairListSlots[slot.Key] = default; // This seat is unoccupied.
                            else
                                ChairListSlots[slot.Key] = default;     // The seat may of had an occupant, but they're offline.
                        }

                        found_free_slot = true;

                        // calculate the distance between the player and this slot
                        float thisDistance = player.GetDistance2d(x_i, y_i);

                        if (thisDistance <= lowestDist)
                        {
                            nearest_slot = slot.Key;
                            lowestDist = thisDistance;
                            x_lowest = x_i;
                            y_lowest = y_i;
                        }
                    }

                    if (found_free_slot)
                    {
                        var guid = ChairListSlots.LookupByKey(nearest_slot);
                        if (guid.IsEmpty())
                        {
                            ChairListSlots[nearest_slot] = player.GetGUID(); //this slot in now used by player
                            player.TeleportTo(GetMapId(), x_lowest, y_lowest, GetPositionZ(), GetOrientation(), (TeleportToOptions.NotLeaveTransport | TeleportToOptions.NotLeaveCombat | TeleportToOptions.NotUnSummonPet));
                            player.SetStandState(UnitStandStateType.SitLowChair + (byte)info.Chair.chairheight);
                            return;
                        }
                    }
                    else
                        player.GetSession().SendNotification("There's nowhere left for you to sit.");

                    return;
                }
                //big gun, its a spell/aura
                case GameObjectTypes.Goober:                        //10
                {
                    GameObjectTemplate info = GetGoInfo();

                    if (user.IsTypeId(TypeId.Player))
                    {
                        Player player = user.ToPlayer();

                        if (info.Goober.pageID != 0)                    // show page...
                        {
                            PageTextPkt data = new();
                            data.GameObjectGUID = GetGUID();
                            player.SendPacket(data);
                        }
                        else if (info.Goober.gossipID != 0)
                        {
                            player.PrepareGossipMenu(this, info.Goober.gossipID);
                            player.SendPreparedGossip(this);
                        }

                        if (info.Goober.eventID != 0)
                        {
                            Log.outDebug(LogFilter.Scripts, "Goober ScriptStart id {0} for GO entry {1} (GUID {2}).", info.Goober.eventID, GetEntry(), GetSpawnId());
                            GetMap().ScriptsStart(ScriptsType.Event, info.Goober.eventID, player, this);
                            EventInform(info.Goober.eventID, user);
                        }

                        // possible quest objective for active quests
                        if (info.Goober.questID != 0 && Global.ObjectMgr.GetQuestTemplate(info.Goober.questID) != null)
                        {
                            //Quest require to be active for GO using
                            if (player.GetQuestStatus(info.Goober.questID) != QuestStatus.Incomplete)
                                break;
                        }

                        Group group = player.GetGroup();
                        if (group)
                        {
                            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                            {
                                Player member = refe.GetSource();
                                if (member)
                                    if (member.IsAtGroupRewardDistance(this))
                                        member.KillCreditGO(info.entry, GetGUID());
                            }
                        }
                        else
                            player.KillCreditGO(info.entry, GetGUID());
                    }

                    uint trapEntry = info.Goober.linkedTrap;
                    if (trapEntry != 0)
                        TriggeringLinkedGameObject(trapEntry, user);

                    AddFlag(GameObjectFlags.InUse);
                    SetLootState(LootState.Activated, user);

                    // this appear to be ok, however others exist in addition to this that should have custom (ex: 190510, 188692, 187389)
                    if (info.Goober.customAnim != 0)
                        SendCustomAnim(GetGoAnimProgress());
                    else
                        SetGoState(GameObjectState.Active);

                    m_cooldownTime = GameTime.GetGameTimeMS() + info.GetAutoCloseTime();

                    // cast this spell later if provided
                    spellId = info.Goober.spell;
                    spellCaster = null;

                    break;
                }
                case GameObjectTypes.Camera:                        //13
                {
                    GameObjectTemplate info = GetGoInfo();
                    if (info == null)
                        return;

                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    if (info.Camera._camera != 0)
                        player.SendCinematicStart(info.Camera._camera);

                    if (info.Camera.eventID != 0)
                    {
                        GetMap().ScriptsStart(ScriptsType.Event, info.Camera.eventID, player, this);
                        EventInform(info.Camera.eventID, user);
                    }

                    return;
                }
                //fishing bobber
                case GameObjectTypes.FishingNode:                   //17
                {
                    Player player = user.ToPlayer();
                    if (player == null)
                        return;

                    if (player.GetGUID() != GetOwnerGUID())
                        return;

                    switch (GetLootState())
                    {
                        case LootState.Ready:                              // ready for loot
                        {
                            uint zone, subzone;
                            GetZoneAndAreaId(out zone, out subzone);

                            int zone_skill = Global.ObjectMgr.GetFishingBaseSkillLevel(subzone);
                            if (zone_skill == 0)
                                zone_skill = Global.ObjectMgr.GetFishingBaseSkillLevel(zone);

                            //provide error, no fishable zone or area should be 0
                            if (zone_skill == 0)
                                Log.outError(LogFilter.Sql, "Fishable areaId {0} are not properly defined in `skill_fishing_base_level`.", subzone);

                            int skill = player.GetSkillValue(SkillType.Fishing);

                            int chance;
                            if (skill < zone_skill)
                            {
                                chance = (int)(Math.Pow((double)skill / zone_skill, 2) * 100);
                                if (chance < 1)
                                    chance = 1;
                            }
                            else
                                chance = 100;

                            int roll = RandomHelper.IRand(1, 100);

                            Log.outDebug(LogFilter.Server, "Fishing check (skill: {0} zone min skill: {1} chance {2} roll: {3}", skill, zone_skill, chance, roll);

                            player.UpdateFishingSkill();

                            // @todo find reasonable value for fishing hole search
                            GameObject fishingPool = LookupFishingHoleAround(20.0f + SharedConst.ContactDistance);

                            // If fishing skill is high enough, or if fishing on a pool, send correct loot.
                            // Fishing pools have no skill requirement as of patch 3.3.0 (undocumented change).
                            if (chance >= roll || fishingPool)
                            {
                                // @todo I do not understand this hack. Need some explanation.
                                // prevent removing GO at spell cancel
                                RemoveFromOwner();
                                SetOwnerGUID(player.GetGUID());

                                if (fishingPool)
                                {
                                    fishingPool.Use(player);
                                    SetLootState(LootState.JustDeactivated);
                                }
                                else
                                    player.SendLoot(GetGUID(), LootType.Fishing);
                            }
                            else// If fishing skill is too low, send junk loot.
                                player.SendLoot(GetGUID(), LootType.FishingJunk);
                            break;
                        }
                        case LootState.JustDeactivated:                   // nothing to do, will be deleted at next update
                            break;
                        default:
                        {
                            SetLootState(LootState.JustDeactivated);
                            player.SendPacket(new FishNotHooked());
                            break;
                        }
                    }

                    player.FinishSpell(CurrentSpellTypes.Channeled);
                    return;
                }

                case GameObjectTypes.Ritual:              //18
                {
                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    Unit owner = GetOwner();

                    GameObjectTemplate info = GetGoInfo();

                    // ritual owner is set for GO's without owner (not summoned)
                    if (m_ritualOwner == null && owner == null)
                        m_ritualOwner = player;

                    if (owner != null)
                    {
                        if (!owner.IsTypeId(TypeId.Player))
                            return;

                        // accept only use by player from same group as owner, excluding owner itself (unique use already added in spell effect)
                        if (player == owner.ToPlayer() || (info.Ritual.castersGrouped != 0 && !player.IsInSameRaidWith(owner.ToPlayer())))
                            return;

                        // expect owner to already be channeling, so if not...
                        if (owner.GetCurrentSpell(CurrentSpellTypes.Channeled) == null)
                            return;

                        // in case summoning ritual caster is GO creator
                        spellCaster = owner;
                    }
                    else
                    {
                        if (player != m_ritualOwner && (info.Ritual.castersGrouped != 0 && !player.IsInSameRaidWith(m_ritualOwner)))
                            return;

                        spellCaster = player;
                    }

                    AddUniqueUse(player);

                    if (info.Ritual.animSpell != 0)
                    {
                        player.CastSpell(player, info.Ritual.animSpell, true);

                        // for this case, summoningRitual.spellId is always triggered
                        triggered = true;
                    }

                    // full amount unique participants including original summoner
                    if (GetUniqueUseCount() == info.Ritual.casters)
                    {
                        if (m_ritualOwner != null)
                            spellCaster = m_ritualOwner;

                        spellId = info.Ritual.spell;

                        if (spellId == 62330)                       // GO store nonexistent spell, replace by expected
                        {
                            // spell have reagent and mana cost but it not expected use its
                            // it triggered spell in fact casted at currently channeled GO
                            spellId = 61993;
                            triggered = true;
                        }

                        // Cast casterTargetSpell at a random GO user
                        // on the current DB there is only one gameobject that uses this (Ritual of Doom)
                        // and its required target number is 1 (outter for loop will run once)
                        if (info.Ritual.casterTargetSpell != 0 && info.Ritual.casterTargetSpell != 1) // No idea why this field is a bool in some cases
                            for (uint i = 0; i < info.Ritual.casterTargetSpellTargets; i++)
                            {
                                // m_unique_users can contain only player GUIDs
                                Player target = Global.ObjAccessor.GetPlayer(this, m_unique_users.SelectRandom());
                                if (target != null)
                                    spellCaster.CastSpell(target, info.Ritual.casterTargetSpell, true);
                            }

                        // finish owners spell
                        if (owner != null)
                            owner.FinishSpell(CurrentSpellTypes.Channeled);

                        // can be deleted now, if
                        if (info.Ritual.ritualPersistent == 0)
                            SetLootState(LootState.JustDeactivated);
                        else
                        {
                            // reset ritual for this GO
                            m_ritualOwner = null;
                            m_unique_users.Clear();
                            m_usetimes = 0;
                        }
                    }
                    else
                        return;

                    // go to end function to spell casting
                    break;
                }
                case GameObjectTypes.SpellCaster:                   //22
                {
                    GameObjectTemplate info = GetGoInfo();
                    if (info == null)
                        return;

                    if (info.SpellCaster.partyOnly != 0)
                    {
                        Unit caster = GetOwner();
                        if (caster == null || !caster.IsTypeId(TypeId.Player))
                            return;

                        if (!user.IsTypeId(TypeId.Player) || !user.ToPlayer().IsInSameRaidWith(caster.ToPlayer()))
                            return;
                    }

                    user.RemoveAurasByType(AuraType.Mounted);
                    spellId = info.SpellCaster.spell;

                    AddUse();
                    break;
                }
                case GameObjectTypes.MeetingStone:                  //23
                {
                    GameObjectTemplate info = GetGoInfo();

                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    Player targetPlayer = Global.ObjAccessor.FindPlayer(player.GetTarget());

                    // accept only use by player from same raid as caster, except caster itself
                    if (targetPlayer == null || targetPlayer == player || !targetPlayer.IsInSameRaidWith(player))
                        return;

                    //required lvl checks!
                    var userLevels = Global.DB2Mgr.GetContentTuningData(info.ContentTuningId, player.m_playerData.CtrOptions.GetValue().ContentTuningConditionMask);
                    if (userLevels.HasValue)
                        if (player.GetLevel() < userLevels.Value.MaxLevel)
                            return;

                    var targetLevels = Global.DB2Mgr.GetContentTuningData(info.ContentTuningId, targetPlayer.m_playerData.CtrOptions.GetValue().ContentTuningConditionMask);
                    if (targetLevels.HasValue)
                        if (targetPlayer.GetLevel() < targetLevels.Value.MaxLevel)
                            return;

                    if (info.entry == 194097)
                        spellId = 61994;                            // Ritual of Summoning
                    else
                        spellId = 23598;// 59782;                            // Summoning Stone Effect

                    break;
                }

                case GameObjectTypes.FlagStand:                     // 24
                {
                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    if (player.CanUseBattlegroundObject(this))
                    {
                        // in Battlegroundcheck
                        Battleground bg = player.GetBattleground();
                        if (!bg)
                            return;

                        if (player.GetVehicle() != null)
                            return;

                        player.RemoveAurasByType(AuraType.ModStealth);
                        player.RemoveAurasByType(AuraType.ModInvisibility);
                        // BG flag click
                        // AB:
                        // 15001
                        // 15002
                        // 15003
                        // 15004
                        // 15005
                        bg.EventPlayerClickedOnFlag(player, this);
                        return;                                     //we don;t need to delete flag ... it is despawned!
                    }
                    break;
                }

                case GameObjectTypes.FishingHole:                   // 25
                {
                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    player.SendLoot(GetGUID(), LootType.Fishinghole);
                    player.UpdateCriteria(CriteriaType.CatchFishInFishingHole, GetGoInfo().entry);
                    return;
                }

                case GameObjectTypes.FlagDrop:                      // 26
                {
                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    if (player.CanUseBattlegroundObject(this))
                    {
                        // in Battlegroundcheck
                        Battleground bg = player.GetBattleground();
                        if (!bg)
                            return;

                        if (player.GetVehicle() != null)
                            return;

                        player.RemoveAurasByType(AuraType.ModStealth);
                        player.RemoveAurasByType(AuraType.ModInvisibility);
                        // BG flag dropped
                        // WS:
                        // 179785 - Silverwing Flag
                        // 179786 - Warsong Flag
                        // EotS:
                        // 184142 - Netherstorm Flag
                        GameObjectTemplate info = GetGoInfo();
                        if (info != null)
                        {
                            switch (info.entry)
                            {
                                case 179785:                        // Silverwing Flag
                                case 179786:                        // Warsong Flag
                                    if (bg.GetTypeID(true) == BattlegroundTypeId.WS)
                                        bg.EventPlayerClickedOnFlag(player, this);
                                    break;
                                case 184142:                        // Netherstorm Flag
                                    if (bg.GetTypeID(true) == BattlegroundTypeId.EY)
                                        bg.EventPlayerClickedOnFlag(player, this);
                                    break;
                            }
                        }
                        //this cause to call return, all flags must be deleted here!!
                        spellId = 0;
                        Delete();
                    }
                    break;
                }
                case GameObjectTypes.BarberChair:                  //32
                {
                    GameObjectTemplate info = GetGoInfo();
                    if (info == null)
                        return;

                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    player.SendPacket(new EnableBarberShop());

                    // fallback, will always work
                    player.TeleportTo(GetMapId(), GetPositionX(), GetPositionY(), GetPositionZ(), GetOrientation(), (TeleportToOptions.NotLeaveTransport | TeleportToOptions.NotLeaveCombat | TeleportToOptions.NotUnSummonPet));
                    player.SetStandState((UnitStandStateType.SitLowChair + (byte)info.BarberChair.chairheight), info.BarberChair.SitAnimKit);
                    return;
                }
                case GameObjectTypes.NewFlag:
                {
                    GameObjectTemplate info = GetGoInfo();
                    if (info == null)
                        return;

                    if (!user.IsPlayer())
                        return;

                    spellId = info.NewFlag.pickupSpell;
                    break;
                }
                case GameObjectTypes.ItemForge:
                {
                    GameObjectTemplate info = GetGoInfo();
                    if (info == null)
                        return;

                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();
                    PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(info.ItemForge.conditionID1);
                    if (playerCondition != null)
                        if (!ConditionManager.IsPlayerMeetingCondition(player, playerCondition))
                            return;

                    switch (info.ItemForge.ForgeType)
                    {
                        case 0: // Artifact Forge
                        case 1: // Relic Forge
                        {

                            Aura artifactAura = player.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                            Item item = artifactAura != null ? player.GetItemByGuid(artifactAura.GetCastItemGUID()) : null;
                            if (!item)
                            {
                                player.SendPacket(new DisplayGameError(GameError.MustEquipArtifact));
                                return;
                            }

                            OpenArtifactForge openArtifactForge = new();
                            openArtifactForge.ArtifactGUID = item.GetGUID();
                            openArtifactForge.ForgeGUID = GetGUID();
                            player.SendPacket(openArtifactForge);
                            break;
                        }
                        case 2: // Heart Forge
                        {
                            Item item = player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                            if (!item)
                                return;

                            OpenHeartForge openHeartForge = new();
                            openHeartForge.ForgeGUID = GetGUID();
                            player.SendPacket(openHeartForge);
                            break;
                        }
                        default:
                            break;
                    }
                    break;
                }
                case GameObjectTypes.UILink:
                {
                    Player player = user.ToPlayer();
                    if (!player)
                        return;

                    GameObjectUILink gameObjectUILink = new();
                    gameObjectUILink.ObjectGUID = GetGUID();
                    gameObjectUILink.UILink = (int)GetGoInfo().UILink.UILinkType;
                    player.SendPacket(gameObjectUILink);
                    return;
                }
                default:
                    if (GetGoType() >= GameObjectTypes.Max)
                        Log.outError(LogFilter.Server, "GameObject.Use(): unit (type: {0}, guid: {1}, name: {2}) tries to use object (guid: {3}, entry: {4}, name: {5}) of unknown type ({6})",
                            user.GetTypeId(), user.GetGUID().ToString(), user.GetName(), GetGUID().ToString(), GetEntry(), GetGoInfo().name, GetGoType());
                    break;
            }

            if (spellId == 0)
                return;

            if (!Global.SpellMgr.HasSpellInfo(spellId, GetMap().GetDifficultyID()))
            {
                if (!user.IsTypeId(TypeId.Player) || !Global.OutdoorPvPMgr.HandleCustomSpell(user.ToPlayer(), spellId, this))
                    Log.outError(LogFilter.Server, "WORLD: unknown spell id {0} at use action for gameobject (Entry: {1} GoType: {2})", spellId, GetEntry(), GetGoType());
                else
                    Log.outDebug(LogFilter.Outdoorpvp, "WORLD: {0} non-dbc spell was handled by OutdoorPvP", spellId);
                return;
            }

            Player player1 = user.ToPlayer();
            if (player1)
                Global.OutdoorPvPMgr.HandleCustomSpell(player1, spellId, this);

            if (spellCaster != null)
                spellCaster.CastSpell(user, spellId, triggered);
            else
                CastSpell(user, spellId);
        }

        public void SendCustomAnim(uint anim)
        {
            GameObjectCustomAnim customAnim = new();
            customAnim.ObjectGUID = GetGUID();
            customAnim.CustomAnim = anim;
            SendMessageToSet(customAnim, true);
        }

        public bool IsInRange(float x, float y, float z, float radius)
        {
            GameObjectDisplayInfoRecord info = CliDB.GameObjectDisplayInfoStorage.LookupByKey(m_goInfo.displayId);
            if (info == null)
                return IsWithinDist3d(x, y, z, radius);

            float sinA = (float)Math.Sin(GetOrientation());
            float cosA = (float)Math.Cos(GetOrientation());
            float dx = x - GetPositionX();
            float dy = y - GetPositionY();
            float dz = z - GetPositionZ();
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            //! Check if the distance between the 2 objects is 0, can happen if both objects are on the same position.
            //! The code below this check wont crash if dist is 0 because 0/0 in float operations is valid, and returns infinite
            if (MathFunctions.fuzzyEq(dist, 0.0f))
                return true;

            float sinB = dx / dist;
            float cosB = dy / dist;
            dx = dist * (cosA * cosB + sinA * sinB);
            dy = dist * (cosA * sinB - sinA * cosB);
            return dx < info.GeoBoxMax.X + radius && dx > info.GeoBoxMin.X - radius
                && dy < info.GeoBoxMax.Y + radius && dy > info.GeoBoxMin.Y - radius
                && dz < info.GeoBoxMax.Z + radius && dz > info.GeoBoxMin.Z - radius;
        }

        public void EventInform(uint eventId, WorldObject invoker = null)
        {
            if (eventId == 0)
                return;

            if (GetAI() != null)
                GetAI().EventInform(eventId);

            if (m_zoneScript != null)
                m_zoneScript.ProcessEvent(this, eventId, invoker);

            BattlegroundMap bgMap = GetMap().ToBattlegroundMap();
            if (bgMap)
                if (bgMap.GetBG())
                    bgMap.GetBG().ProcessEvent(this, eventId, invoker);
        }

        public uint GetScriptId()
        {
            GameObjectData gameObjectData = GetGameObjectData();
            if (gameObjectData != null)
            {
                uint scriptId = gameObjectData.ScriptId;
                if (scriptId != 0)
                    return scriptId;
            }

            return GetGoInfo().ScriptId;
        }

        public override string GetName(Locale locale = Locale.enUS)
        {
            if (locale != Locale.enUS)
            {
                GameObjectLocale cl = Global.ObjectMgr.GetGameObjectLocale(GetEntry());
                if (cl != null)
                    if (cl.Name.Length > (int)locale && !cl.Name[(int)locale].IsEmpty())
                        return cl.Name[(int)locale];
            }

            return base.GetName(locale);
        }

        public void UpdatePackedRotation()
        {
            const int PACK_YZ = 1 << 20;
            const int PACK_X = PACK_YZ << 1;

            const int PACK_YZ_MASK = (PACK_YZ << 1) - 1;
            const int PACK_X_MASK = (PACK_X << 1) - 1;

            sbyte w_sign = (sbyte)(m_localRotation.W >= 0.0f ? 1 : -1);
            long x = (int)(m_localRotation.X * PACK_X) * w_sign & PACK_X_MASK;
            long y = (int)(m_localRotation.Y * PACK_YZ) * w_sign & PACK_YZ_MASK;
            long z = (int)(m_localRotation.Z * PACK_YZ) * w_sign & PACK_YZ_MASK;
            m_packedRotation = z | (y << 21) | (x << 42);
        }

        public void SetLocalRotation(float qx, float qy, float qz, float qw)
        {
            Quaternion rotation = new Quaternion(qx, qy, qz, qw);
            rotation = Quaternion.Multiply(rotation, 1.0f / MathF.Sqrt(Quaternion.Dot(rotation, rotation)));

            m_localRotation.X = rotation.X;
            m_localRotation.Y = rotation.Y;
            m_localRotation.Z = rotation.Z;
            m_localRotation.W = rotation.W;
            UpdatePackedRotation();
        }

        public void SetParentRotation(Quaternion rotation)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.ParentRotation), rotation);
        }

        public void SetLocalRotationAngles(float z_rot, float y_rot, float x_rot)
        {
            Quaternion quat = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(z_rot, y_rot, x_rot));
            SetLocalRotation(quat.X, quat.Y, quat.Z, quat.W);
        }

        public Quaternion GetWorldRotation()
        {
            Quaternion localRotation = GetLocalRotation();

            Transport transport = GetTransport();
            if (transport != null)
            {
                Quaternion worldRotation = transport.GetWorldRotation();

                Quaternion worldRotationQuat = new(worldRotation.X, worldRotation.Y, worldRotation.Z, worldRotation.W);
                Quaternion localRotationQuat = new(localRotation.X, localRotation.Y, localRotation.Z, localRotation.W);

                Quaternion resultRotation = localRotationQuat * worldRotationQuat;

                return resultRotation;
            }

            return localRotation;
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nSpawnId: {GetSpawnId()} GoState: {GetGoState()} ScriptId: {GetScriptId()} AIName: {GetAIName()}";
        }
        
        public bool IsAtInteractDistance(Player player, SpellInfo spell = null)
        {
            if (spell != null || (spell = GetSpellForLock(player)) != null)
            {
                float maxRange = spell.GetMaxRange(spell.IsPositive());

                if (GetGoType() == GameObjectTypes.SpellFocus)
                    return maxRange * maxRange >= GetExactDistSq(player);

                if (CliDB.GameObjectDisplayInfoStorage.ContainsKey(GetGoInfo().displayId))
                    return IsAtInteractDistance(player, maxRange);
            }

            return IsAtInteractDistance(player, GetInteractionDistance());
        }

        bool IsAtInteractDistance(Position pos, float radius)
        {
            var displayInfo = CliDB.GameObjectDisplayInfoStorage.LookupByKey(GetGoInfo().displayId);
            if (displayInfo != null)
            {
                float scale = GetObjectScale();

                float minX = displayInfo.GeoBoxMin.X * scale - radius;
                float minY = displayInfo.GeoBoxMin.Y * scale - radius;
                float minZ = displayInfo.GeoBoxMin.Z * scale - radius;
                float maxX = displayInfo.GeoBoxMax.X * scale + radius;
                float maxY = displayInfo.GeoBoxMax.Y * scale + radius;
                float maxZ = displayInfo.GeoBoxMax.Z * scale + radius;

                Quaternion worldRotation = GetLocalRotation();

                //Todo Test this. Needs checked.
                var worldSpaceBox = MathFunctions.toWorldSpace(Matrix4x4.CreateFromQuaternion(worldRotation), new Vector3(GetPositionX(), GetPositionY(), GetPositionZ()), new Box(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ)));
                return worldSpaceBox.Contains(new Vector3(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()));
            }

            return GetExactDist(pos) <= radius;
        }

        public bool IsWithinDistInMap(Player player)
        {
            return IsInMap(player) && IsInPhase(player) && IsAtInteractDistance(player);
        }
        
        public SpellInfo GetSpellForLock(Player player)
        {
            if (!player)
                return null;

            uint lockId = GetGoInfo().GetLockId();
            if (lockId == 0)
                return null;

            var lockEntry = CliDB.LockStorage.LookupByKey(lockId);
            if (lockEntry == null)
                return null;

            for (byte i = 0; i < SharedConst.MaxLockCase; ++i)
            {
                if (lockEntry.LockType[i] == 0)
                    continue;

                if (lockEntry.LockType[i] == (byte)LockKeyType.Spell)
                {
                    SpellInfo spell = Global.SpellMgr.GetSpellInfo((uint)lockEntry.Index[i], GetMap().GetDifficultyID());
                    if (spell != null)
                        return spell;
                }

                if (lockEntry.LockType[i] != (byte)LockKeyType.Skill)
                    break;

                foreach (var playerSpell in player.GetSpellMap())
                {
                    SpellInfo spell = Global.SpellMgr.GetSpellInfo(playerSpell.Key, GetMap().GetDifficultyID());
                    if (spell != null)
                    {
                        foreach (var effect in spell.GetEffects())
                            if (effect.Effect == SpellEffectName.OpenLock && effect.MiscValue == lockEntry.Index[i])
                                if (effect.CalcValue(player) >= lockEntry.Skill[i])
                                    return spell;
                    }
                }
            }

            return null;
        }

        
        public void ModifyHealth(int change, WorldObject attackerOrHealer = null, uint spellId = 0)
        {
            if (m_goValue.Building.MaxHealth == 0 || change == 0)
                return;

            // prevent double destructions of the same object
            if (change < 0 && m_goValue.Building.Health == 0)
                return;

            if (m_goValue.Building.Health + change <= 0)
                m_goValue.Building.Health = 0;
            else if (m_goValue.Building.Health + change >= m_goValue.Building.MaxHealth)
                m_goValue.Building.Health = m_goValue.Building.MaxHealth;
            else
                m_goValue.Building.Health += (uint)change;

            // Set the health bar, value = 255 * healthPct;
            SetGoAnimProgress(m_goValue.Building.Health * 255 / m_goValue.Building.MaxHealth);

            // dealing damage, send packet
            Player player = attackerOrHealer != null ? attackerOrHealer.GetCharmerOrOwnerPlayerOrPlayerItself() : null;
            if (player != null)
            {
                DestructibleBuildingDamage packet = new();
                packet.Caster = attackerOrHealer.GetGUID(); // todo: this can be a GameObject
                packet.Target = GetGUID();
                packet.Damage = -change;
                packet.Owner = player.GetGUID();
                packet.SpellID = spellId;
                player.SendPacket(packet);
            }

            GameObjectDestructibleState newState = GetDestructibleState();

            if (m_goValue.Building.Health == 0)
                newState = GameObjectDestructibleState.Destroyed;
            else if (m_goValue.Building.Health < m_goValue.Building.MaxHealth / 2)
                newState = GameObjectDestructibleState.Damaged;
            else if (m_goValue.Building.Health == m_goValue.Building.MaxHealth)
                newState = GameObjectDestructibleState.Intact;

            if (newState == GetDestructibleState())
                return;

            SetDestructibleState(newState, attackerOrHealer, false);
        }

        public void SetDestructibleState(GameObjectDestructibleState state, WorldObject attackerOrHealer = null, bool setHealth = false)
        {
            // the user calling this must know he is already operating on destructible gameobject
            Cypher.Assert(GetGoType() == GameObjectTypes.DestructibleBuilding);

            switch (state)
            {
                case GameObjectDestructibleState.Intact:
                    RemoveFlag(GameObjectFlags.Damaged | GameObjectFlags.Destroyed);
                    SetDisplayId(m_goInfo.displayId);
                    if (setHealth)
                    {
                        m_goValue.Building.Health = m_goValue.Building.MaxHealth;
                        SetGoAnimProgress(255);
                    }
                    EnableCollision(true);
                    break;
                case GameObjectDestructibleState.Damaged:
                {
                    EventInform(m_goInfo.DestructibleBuilding.DamagedEvent, attackerOrHealer);
                    GetAI().Damaged(attackerOrHealer, m_goInfo.DestructibleBuilding.DamagedEvent);

                    RemoveFlag(GameObjectFlags.Destroyed);
                    AddFlag(GameObjectFlags.Damaged);

                    uint modelId = m_goInfo.displayId;
                    DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(m_goInfo.DestructibleBuilding.DestructibleModelRec);
                    if (modelData != null)
                        if (modelData.State1Wmo != 0)
                            modelId = modelData.State1Wmo;
                    SetDisplayId(modelId);

                    if (setHealth)
                    {
                        m_goValue.Building.Health = 10000;//m_goInfo.DestructibleBuilding.damagedNumHits;
                        uint maxHealth = m_goValue.Building.MaxHealth;
                        // in this case current health is 0 anyway so just prevent crashing here
                        if (maxHealth == 0)
                            maxHealth = 1;
                        SetGoAnimProgress(m_goValue.Building.Health * 255 / maxHealth);
                    }
                    break;
                }
                case GameObjectDestructibleState.Destroyed:
                {
                    EventInform(m_goInfo.DestructibleBuilding.DestroyedEvent, attackerOrHealer);
                    GetAI().Destroyed(attackerOrHealer, m_goInfo.DestructibleBuilding.DestroyedEvent);

                    Player player = attackerOrHealer != null ? attackerOrHealer.GetCharmerOrOwnerPlayerOrPlayerItself() : null;
                    if (player)
                    {
                        Battleground bg = player.GetBattleground();
                        if (bg != null)
                            bg.DestroyGate(player, this);
                    }

                    RemoveFlag(GameObjectFlags.Damaged);
                    AddFlag(GameObjectFlags.Destroyed);

                    uint modelId = m_goInfo.displayId;
                    DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(m_goInfo.DestructibleBuilding.DestructibleModelRec);
                    if (modelData != null)
                        if (modelData.State2Wmo != 0)
                            modelId = modelData.State2Wmo;
                    SetDisplayId(modelId);

                    if (setHealth)
                    {
                        m_goValue.Building.Health = 0;
                        SetGoAnimProgress(0);
                    }
                    EnableCollision(false);
                    break;
                }
                case GameObjectDestructibleState.Rebuilding:
                {
                    EventInform(m_goInfo.DestructibleBuilding.RebuildingEvent, attackerOrHealer);
                    RemoveFlag(GameObjectFlags.Damaged | GameObjectFlags.Destroyed);

                    uint modelId = m_goInfo.displayId;
                    DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(m_goInfo.DestructibleBuilding.DestructibleModelRec);
                    if (modelData != null)
                        if (modelData.State3Wmo != 0)
                            modelId = modelData.State3Wmo;
                    SetDisplayId(modelId);

                    // restores to full health
                    if (setHealth)
                    {
                        m_goValue.Building.Health = m_goValue.Building.MaxHealth;
                        SetGoAnimProgress(255);
                    }
                    EnableCollision(true);
                    break;
                }
            }
        }

        public void SetLootState(LootState state, Unit unit = null)
        {
            m_lootState = state;
            m_lootStateUnitGUID = unit ? unit.GetGUID() : ObjectGuid.Empty;
            GetAI().OnLootStateChanged((uint)state, unit);

            // Start restock timer if the chest is partially looted or not looted at all
            if (GetGoType() == GameObjectTypes.Chest && state == LootState.Activated && GetGoInfo().Chest.chestRestockTime > 0 && m_restockTime == 0)
                m_restockTime = GameTime.GetGameTime() + GetGoInfo().Chest.chestRestockTime;

            // only set collision for doors on SetGoState
            if (GetGoType() == GameObjectTypes.Door)
                return;

            if (m_model != null)
            {
                bool collision = false;
                // Use the current go state
                if ((GetGoState() != GameObjectState.Ready && (state == LootState.Activated || state == LootState.JustDeactivated)) || state == LootState.Ready)
                    collision = !collision;

                EnableCollision(collision);
            }
        }

        public void SetGoState(GameObjectState state)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.State), (sbyte)state);
            if (GetAI() != null)
                GetAI().OnStateChanged(state);
            if (m_model != null && !IsTransport())
            {
                if (!IsInWorld)
                    return;

                // startOpen determines whether we are going to add or remove the LoS on activation
                bool collision = false;
                if (state == GameObjectState.Ready)
                    collision = !collision;

                EnableCollision(collision);
            }
        }

        public virtual uint GetTransportPeriod()
        {
            Cypher.Assert(GetGoInfo().type == GameObjectTypes.Transport);
            if (!m_goValue.Transport.AnimationInfo.Path.Empty())
                return m_goValue.Transport.AnimationInfo.TotalTime;

            return 0;
        }

        public void SetTransportState(GameObjectState state, uint stopFrame = 0)
        {
            if (GetGoState() == state)
                return;

            Cypher.Assert(GetGoInfo().type == GameObjectTypes.Transport);
            Cypher.Assert(state >= GameObjectState.TransportActive);
            if (state == GameObjectState.TransportActive)
            {
                m_goValue.Transport.StateUpdateTimer = 0;
                m_goValue.Transport.PathProgress = Time.GetMSTime();
                if (GetGoState() >= GameObjectState.TransportStopped)
                    m_goValue.Transport.PathProgress += m_goValue.Transport.StopFrames.LookupByIndex(GetGoState() - GameObjectState.TransportStopped);
                SetGoState(GameObjectState.TransportActive);
            }
            else
            {
                Cypher.Assert(stopFrame < m_goValue.Transport.StopFrames.Count);
                m_goValue.Transport.PathProgress = Time.GetMSTime() + m_goValue.Transport.StopFrames[(int)stopFrame];
                SetGoState((GameObjectState)((int)GameObjectState.TransportStopped + stopFrame));
            }
        }

        public void SetDisplayId(uint displayid)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.DisplayID), displayid);
            UpdateModel();
        }

        public byte GetNameSetId()
        {
            switch (GetGoType())
            {
                case GameObjectTypes.DestructibleBuilding:
                    DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(m_goInfo.DestructibleBuilding.DestructibleModelRec);
                    if (modelData != null)
                    {
                        switch (GetDestructibleState())
                        {
                            case GameObjectDestructibleState.Intact:
                                return modelData.State0NameSet;
                            case GameObjectDestructibleState.Damaged:
                                return modelData.State1NameSet;
                            case GameObjectDestructibleState.Destroyed:
                                return modelData.State2NameSet;
                            case GameObjectDestructibleState.Rebuilding:
                                return modelData.State3NameSet;
                            default:
                                break;
                        }
                    }
                    break;
                case GameObjectTypes.GarrisonBuilding:
                case GameObjectTypes.GarrisonPlot:
                case GameObjectTypes.PhaseableMo:
                    var flags = (GameObjectFlags)(uint)m_gameObjectData.Flags;
                    return (byte)(((int)flags >> 8) & 0xF);
                default:
                    break;
            }

            return 0;
        }

        void EnableCollision(bool enable)
        {
            if (m_model == null)
                return;

            m_model.EnableCollision(enable);
        }

        void UpdateModel()
        {
            if (!IsInWorld)
                return;

            if (m_model != null)
                if (GetMap().ContainsGameObjectModel(m_model))
                    GetMap().RemoveGameObjectModel(m_model);

            RemoveFlag(GameObjectFlags.MapObject);
            m_model = null;
            CreateModel();

            if (m_model != null)
                GetMap().InsertGameObjectModel(m_model);
        }

        Player GetLootRecipient()
        {
            if (m_lootRecipient.IsEmpty())
                return null;
            return Global.ObjAccessor.FindPlayer(m_lootRecipient);
        }

        Group GetLootRecipientGroup()
        {
            if (m_lootRecipientGroup.IsEmpty())
                return Global.GroupMgr.GetGroupByGUID(m_lootRecipientGroup);

            return null;
        }

        public void SetLootRecipient(Unit unit, Group group)
        {
            // set the player whose group should receive the right
            // to loot the creature after it dies
            // should be set to null after the loot disappears

            if (unit == null)
            {
                m_lootRecipient.Clear();
                m_lootRecipientGroup = group ? group.GetGUID() : ObjectGuid.Empty;
                return;
            }

            if (!unit.IsTypeId(TypeId.Player) && !unit.IsVehicle())
                return;

            Player player = unit.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (player == null)                                             // normal creature, no player involved
                return;

            m_lootRecipient = player.GetGUID();

            // either get the group from the passed parameter or from unit's one
            Group unitGroup = player.GetGroup();
            if (group)
                m_lootRecipientGroup = group.GetGUID();
            else if (unitGroup)
                m_lootRecipientGroup = unitGroup.GetGUID();
        }

        public bool IsLootAllowedFor(Player player)
        {
            if (m_lootRecipient.IsEmpty() && m_lootRecipientGroup.IsEmpty())
                return true;

            if (player.GetGUID() == m_lootRecipient)
                return true;

            Group playerGroup = player.GetGroup();
            if (!playerGroup || playerGroup != GetLootRecipientGroup()) // if we dont have a group we arent the recipient
                return false;                                           // if go doesnt have group bound it means it was solo killed by someone else

            return true;
        }

        public void SetLinkedTrap(GameObject linkedTrap) { m_linkedTrap = linkedTrap.GetGUID(); }
        public GameObject GetLinkedTrap()
        {
            return ObjectAccessor.GetGameObject(this, m_linkedTrap);
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt8((byte)flags);
            m_objectData.WriteCreate(buffer, flags, this, target);
            m_gameObjectData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.GameObject))
                m_gameObjectData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedGameObjectMask, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedGameObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.GameObject);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.GameObject])
                m_gameObjectData.WriteUpdate(buffer, requestedGameObjectMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_gameObjectData);
            base.ClearUpdateMask(remove);
        }

        public void GetRespawnPosition(out float x, out float y, out float z, out float ori)
        {
            if (m_goData != null)
                m_goData.SpawnPoint.GetPosition(out x, out y, out z, out ori);
            else
                GetPosition(out x, out y, out z, out ori);
        }

        public float GetInteractionDistance()
        {
            switch (GetGoType())
            {
                case GameObjectTypes.AreaDamage:
                    return 0.0f;
                case GameObjectTypes.QuestGiver:
                case GameObjectTypes.Text:
                case GameObjectTypes.FlagStand:
                case GameObjectTypes.FlagDrop:
                case GameObjectTypes.MiniGame:
                    return 5.5555553f;
                case GameObjectTypes.Chair:
                case GameObjectTypes.BarberChair:
                    return 3.0f;
                case GameObjectTypes.FishingNode:
                    return 100.0f;
                case GameObjectTypes.FishingHole:
                    return 20.0f + SharedConst.ContactDistance; // max spell range
                case GameObjectTypes.Camera:
                case GameObjectTypes.MapObject:
                case GameObjectTypes.DungeonDifficulty:
                case GameObjectTypes.DestructibleBuilding:
                case GameObjectTypes.Door:
                    return 5.0f;
                // Following values are not blizzlike
                case GameObjectTypes.GuildBank:
                case GameObjectTypes.Mailbox:
                    // Successful mailbox interaction is rather critical to the client, failing it will start a minute-long cooldown until the next mail query may be executed.
                    // And since movement info update is not sent with mailbox interaction query, server may find the player outside of interaction range. Thus we increase it.
                    return 10.0f; // 5.0f is blizzlike
                default:
                    return SharedConst.InteractionDistance;
            }
        }

        public void UpdateModelPosition()
        {
            if (m_model == null)
                return;

            if (GetMap().ContainsGameObjectModel(m_model))
            {
                GetMap().RemoveGameObjectModel(m_model);
                m_model.UpdatePosition();
                GetMap().InsertGameObjectModel(m_model);
            }
        }

        public void SetAnimKitId(ushort animKitId, bool oneshot)
        {
            if (_animKitId == animKitId)
                return;

            if (animKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(animKitId))
                return;

            if (!oneshot)
                _animKitId = animKitId;
            else
                _animKitId = 0;

            GameObjectActivateAnimKit activateAnimKit = new();
            activateAnimKit.ObjectGUID = GetGUID();
            activateAnimKit.AnimKitID = animKitId;
            activateAnimKit.Maintain = !oneshot;
            SendMessageToSet(activateAnimKit, true);
        }

        void SetSpellVisualId(uint spellVisualId, ObjectGuid activatorGuid = default)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.SpellVisualID), spellVisualId);

            GameObjectPlaySpellVisual packet = new();
            packet.ObjectGUID = GetGUID();
            packet.ActivatorGUID = activatorGuid;
            packet.SpellVisualID = spellVisualId;
            SendMessageToSet(packet, true);
        }

        public override ushort GetAIAnimKitId() { return _animKitId; }

        public uint GetWorldEffectID() { return _worldEffectID; }
        public void SetWorldEffectID(uint worldEffectID) { _worldEffectID = worldEffectID; }

        public GameObjectTemplate GetGoInfo() { return m_goInfo; }
        public GameObjectTemplateAddon GetTemplateAddon() { return m_goTemplateAddon; }
        public GameObjectData GetGameObjectData() { return m_goData; }
        public GameObjectValue GetGoValue() { return m_goValue; }

        public ulong GetSpawnId() { return m_spawnId; }

        public Quaternion GetLocalRotation() { return m_localRotation; }
        public long GetPackedLocalRotation() { return m_packedRotation; }

        public void SetOwnerGUID(ObjectGuid owner)
        {
            // Owner already found and different than expected owner - remove object from old owner
            if (!owner.IsEmpty() && !GetOwnerGUID().IsEmpty() && GetOwnerGUID() != owner)
            {
                Cypher.Assert(false);
            }
            m_spawnedByDefault = false;                     // all object with owner is despawned after delay
            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.CreatedBy), owner);
        }
        public override ObjectGuid GetOwnerGUID() { return m_gameObjectData.CreatedBy; }

        public void SetSpellId(uint id)
        {
            m_spawnedByDefault = false;                     // all summoned object is despawned after delay
            m_spellId = id;
        }
        public uint GetSpellId() { return m_spellId; }

        public long GetRespawnTime() { return m_respawnTime; }
        public long GetRespawnTimeEx()
        {
            long now = GameTime.GetGameTime();
            if (m_respawnTime > now)
                return m_respawnTime;
            else
                return now;
        }

        public void SetRespawnTime(int respawn)
        {
            m_respawnTime = respawn > 0 ? GameTime.GetGameTime() + respawn : 0;
            m_respawnDelayTime = (uint)(respawn > 0 ? respawn : 0);
            if (respawn != 0 && !m_spawnedByDefault)
                UpdateObjectVisibility(true);
        }

        public bool IsSpawned()
        {
            return m_respawnDelayTime == 0 ||
                (m_respawnTime > 0 && !m_spawnedByDefault) ||
                (m_respawnTime == 0 && m_spawnedByDefault);
        }
        public bool IsSpawnedByDefault() { return m_spawnedByDefault; }
        public void SetSpawnedByDefault(bool b) { m_spawnedByDefault = b; }
        public uint GetRespawnDelay() { return m_respawnDelayTime; }

        public bool HasFlag(GameObjectFlags flags) { return (m_gameObjectData.Flags & (uint)flags) != 0; }
        public void AddFlag(GameObjectFlags flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.Flags), (uint)flags); }
        public void RemoveFlag(GameObjectFlags flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.Flags), (uint)flags); }
        public void SetFlags(GameObjectFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.Flags), (uint)flags); }
        public void SetLevel(uint level) { SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.Level), level); }

        public GameObjectTypes GetGoType() { return (GameObjectTypes)(sbyte)m_gameObjectData.TypeID; }
        public void SetGoType(GameObjectTypes type) { SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.TypeID), (sbyte)type); }
        public GameObjectState GetGoState() { return (GameObjectState)(sbyte)m_gameObjectData.State; }
        uint GetGoArtKit() { return m_gameObjectData.ArtKit; }
        byte GetGoAnimProgress() { return m_gameObjectData.PercentHealth; }
        public void SetGoAnimProgress(uint animprogress) { SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.PercentHealth), (byte)animprogress); }

        public LootState GetLootState() { return m_lootState; }
        public LootModes GetLootMode() { return m_LootMode; }
        bool HasLootMode(LootModes lootMode) { return Convert.ToBoolean(m_LootMode & lootMode); }
        void SetLootMode(LootModes lootMode) { m_LootMode = lootMode; }
        void AddLootMode(LootModes lootMode) { m_LootMode |= lootMode; }
        void RemoveLootMode(LootModes lootMode) { m_LootMode &= ~lootMode; }
        void ResetLootMode() { m_LootMode = LootModes.Default; }
        public void SetLootGenerationTime() { m_lootGenerationTime = (uint)GameTime.GetGameTime(); }
        public uint GetLootGenerationTime() { return m_lootGenerationTime; }

        public void AddToSkillupList(ObjectGuid PlayerGuid) { m_SkillupList.Add(PlayerGuid); }
        public bool IsInSkillupList(ObjectGuid PlayerGuid)
        {
            foreach (var i in m_SkillupList)
                if (i == PlayerGuid)
                    return true;

            return false;
        }
        void ClearSkillupList() { m_SkillupList.Clear(); }

        public void AddUse() { ++m_usetimes; }

        public uint GetUseCount() { return m_usetimes; }
        uint GetUniqueUseCount() { return (uint)m_unique_users.Count; }

        bool HasLootRecipient() { return !m_lootRecipient.IsEmpty() || !m_lootRecipientGroup.IsEmpty(); }

        public override uint GetLevelForTarget(WorldObject target)
        {
            Unit owner = GetOwner();
            if (owner != null)
                return owner.GetLevelForTarget(target);

            if (GetGoType() == GameObjectTypes.Trap)
            {
                Player player = target.ToPlayer();
                if (player != null)
                {
                    var userLevels = Global.DB2Mgr.GetContentTuningData(GetGoInfo().ContentTuningId, player.m_playerData.CtrOptions.GetValue().ContentTuningConditionMask);
                    if (userLevels.HasValue)
                        return (byte)Math.Clamp(player.GetLevel(), userLevels.Value.MinLevel, userLevels.Value.MaxLevel);
                }

                Unit targetUnit = target.ToUnit();
                if (targetUnit != null)
                    return targetUnit.GetLevel();
            }

            return 1;
        }

        GameObjectDestructibleState GetDestructibleState()
        {
            if ((m_gameObjectData.Flags & (uint)GameObjectFlags.Destroyed) != 0)
                return GameObjectDestructibleState.Destroyed;
            if ((m_gameObjectData.Flags & (uint)GameObjectFlags.Damaged) != 0)
                return GameObjectDestructibleState.Damaged;
            return GameObjectDestructibleState.Intact;
        }

        public GameObjectAI GetAI() { return m_AI; }

        public T GetAI<T>() where T : GameObjectAI { return (T)m_AI; }

        public uint GetDisplayId() { return m_gameObjectData.DisplayID; }

        public override uint GetFaction() { return m_gameObjectData.FactionTemplate; }
        public override void SetFaction(uint faction) { SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.FactionTemplate), faction); }

        public override float GetStationaryX() { return StationaryPosition.GetPositionX(); }
        public override float GetStationaryY() { return StationaryPosition.GetPositionY(); }
        public override float GetStationaryZ() { return StationaryPosition.GetPositionZ(); }
        public override float GetStationaryO() { return StationaryPosition.GetOrientation(); }

        public void RelocateStationaryPosition(float x, float y, float z, float o) { StationaryPosition.Relocate(x, y, z, o); }

        //! Object distance/size - overridden from Object._IsWithinDist. Needs to take in account proper GO size.
        public override bool _IsWithinDist(WorldObject obj, float dist2compare, bool is3D, bool incOwnRadius, bool incTargetRadius)
        {
            //! Following check does check 3d distance
            return IsInRange(obj.GetPositionX(), obj.GetPositionY(), obj.GetPositionZ(), dist2compare);
        }

        public void CreateModel()
        {
            m_model = GameObjectModel.Create(new GameObjectModelOwnerImpl(this));
            if (m_model != null && m_model.IsMapObject())
                AddFlag(GameObjectFlags.MapObject);
        }

        // There's many places not ready for dynamic spawns. This allows them to live on for now.
        void SetRespawnCompatibilityMode(bool mode = true) { m_respawnCompatibilityMode = mode; }
        public bool GetRespawnCompatibilityMode() { return m_respawnCompatibilityMode; }

        #region Fields
        protected GameObjectFieldData m_gameObjectData;
        protected GameObjectValue m_goValue;
        protected GameObjectTemplate m_goInfo;
        protected GameObjectTemplateAddon m_goTemplateAddon;
        GameObjectData m_goData;
        ulong m_spawnId;
        uint m_spellId;
        long m_respawnTime;                          // (secs) time of next respawn (or despawn if GO have owner()),
        uint m_respawnDelayTime;                     // (secs) if 0 then current GO state no dependent from timer
        uint m_despawnDelay;
        TimeSpan m_despawnRespawnTime;                   // override respawn time after delayed despawn
        LootState m_lootState;
        ObjectGuid m_lootStateUnitGUID;                    // GUID of the unit passed with SetLootState(LootState, Unit*)
        bool m_spawnedByDefault;
        long m_restockTime;
        long m_cooldownTime;                         // used as internal reaction delay time store (not state change reaction).
        // For traps this: spell casting cooldown, for doors/buttons: reset time.

        Player m_ritualOwner;                              // used for GAMEOBJECT_TYPE_SUMMONING_RITUAL where GO is not summoned (no owner)
        List<ObjectGuid> m_unique_users = new();
        uint m_usetimes;

        ObjectGuid m_lootRecipient;
        ObjectGuid m_lootRecipientGroup;
        LootModes m_LootMode;                                  // bitmask, default LOOT_MODE_DEFAULT, determines what loot will be lootable
        uint m_lootGenerationTime;
        public uint m_groupLootTimer;                            // (msecs)timer used for group loot
        public ObjectGuid lootingGroupLowGUID;                         // used to find group which is looting
        long m_packedRotation;
        Quaternion m_localRotation;
        public Position StationaryPosition { get; set; }

        GameObjectAI m_AI;
        bool m_respawnCompatibilityMode;
        ushort _animKitId;
        uint _worldEffectID;

        GameObjectState m_prevGoState;                          // What state to set whenever resetting

        Dictionary<uint, ObjectGuid> ChairListSlots = new();
        List<ObjectGuid> m_SkillupList = new();

        public Loot loot = new();

        public GameObjectModel m_model;

        ObjectGuid m_linkedTrap;
        #endregion
    }

    class GameObjectModelOwnerImpl : GameObjectModelOwnerBase
    {
        public GameObjectModelOwnerImpl(GameObject owner)
        {
            _owner = owner;
        }

        public override bool IsSpawned() { return _owner.IsSpawned(); }
        public override uint GetDisplayId() { return _owner.GetDisplayId(); }
        public override byte GetNameSetId() { return _owner.GetNameSetId(); }
        public override bool IsInPhase(PhaseShift phaseShift) { return _owner.GetPhaseShift().CanSee(phaseShift); }
        public override Vector3 GetPosition() { return new Vector3(_owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ()); }
        public override float GetOrientation() { return _owner.GetOrientation(); }
        public override float GetScale() { return _owner.GetObjectScale(); }

        GameObject _owner;
    }

    public struct GameObjectValue
    {
        public transport Transport;

        public fishinghole FishingHole;

        public building Building;

        //11 GAMEOBJECT_TYPE_TRANSPORT
        public struct transport
        {
            public uint PathProgress;
            public TransportAnimation AnimationInfo;
            public uint CurrentSeg;
            public List<uint> StopFrames;
            public uint StateUpdateTimer;
        }

        //25 GAMEOBJECT_TYPE_FISHINGHOLE
        public struct fishinghole
        {
            public uint MaxOpens;
        }

        //33 GAMEOBJECT_TYPE_DESTRUCTIBLE_BUILDING
        public struct building
        {
            public uint Health;
            public uint MaxHealth;
        }
    }
}