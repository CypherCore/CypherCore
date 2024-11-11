// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
            SetVignette(0);

            base.CleanupsBeforeDelete(finalCleanup);

            RemoveFromOwner();
        }

        void RemoveFromOwner()
        {
            ObjectGuid ownerGUID = GetOwnerGUID();
            if (ownerGUID.IsEmpty())
                return;

            Unit owner = Global.ObjAccessor.GetUnit(this, ownerGUID);
            if (owner != null)
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
                    if (trans != null)
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

                // If linked trap exists, despawn it
                GameObject linkedTrap = GetLinkedTrap();
                if (linkedTrap != null)
                    linkedTrap.DespawnOrUnsummon();

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
            Cypher.Assert(map != null);
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
                ReplaceAllFlags(goOverride.Flags);
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
            SetGoArtKit(artKit);

            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.SpawnTrackingStateAnimID), Global.DB2Mgr.GetEmptyAnimStateID());

            switch (goInfo.type)
            {
                case GameObjectTypes.FishingHole:
                    SetGoAnimProgress(animProgress);
                    m_goValue.FishingHole.MaxOpens = RandomHelper.URand(GetGoInfo().FishingHole.minRestock, GetGoInfo().FishingHole.maxRestock);
                    break;
                case GameObjectTypes.DestructibleBuilding:
                    m_goValue.Building.DestructibleHitpoint = Global.ObjectMgr.GetDestructibleHitpoint(GetGoInfo().DestructibleBuilding.HealthRec);
                    m_goValue.Building.Health = m_goValue.Building.DestructibleHitpoint != null ? m_goValue.Building.DestructibleHitpoint.GetMaxHealth() : 0;
                    SetGoAnimProgress(255);

                    // yes, even after the updatefield rewrite this garbage hack is still in client
                    SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.ParentRotation), new Quaternion(goInfo.DestructibleBuilding.DestructibleModelRec, 0f, 0f, 0f));
                    break;
                case GameObjectTypes.Transport:
                    m_goTypeImpl = new GameObjectType.Transport(this);

                    if (goInfo.Transport.startOpen != 0)
                        SetGoState(GameObjectState.TransportStopped);
                    else
                        SetGoState(GameObjectState.TransportActive);

                    SetGoAnimProgress(animProgress);
                    SetActive(true);
                    break;
                case GameObjectTypes.FishingNode:
                    SetLevel(0);
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
                case GameObjectTypes.ControlZone:
                    m_goTypeImpl = new ControlZone(this);
                    SetActive(true);
                    break;
                case GameObjectTypes.NewFlag:
                    m_goTypeImpl = new GameObjectType.NewFlag(this);
                    if (map.Instanceable())
                        SetActive(true);
                    break;
                case GameObjectTypes.NewFlagDrop:
                    if (map.Instanceable())
                        SetActive(true);
                    break;
                case GameObjectTypes.PhaseableMo:
                    RemoveFlag((GameObjectFlags)0xF00);
                    SetFlag((GameObjectFlags)((m_goInfo.PhaseableMO.AreaNameSet & 0xF) << 8));
                    break;
                case GameObjectTypes.CapturePoint:
                    SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.SpellVisualID), m_goInfo.CapturePoint.SpellVisual1);
                    m_goValue.CapturePoint.AssaultTimer = 0;
                    m_goValue.CapturePoint.LastTeamCapture = BattleGroundTeamId.Neutral;
                    m_goValue.CapturePoint.State = BattlegroundCapturePointState.Neutral;
                    UpdateCapturePoint();
                    if (map.Instanceable())
                        SetActive(true);
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

            uint vignetteId = GetGoInfo().GetSpawnVignette();
            if (vignetteId != 0)
                SetVignette(vignetteId);

            LastUsedScriptID = GetGoInfo().ScriptId;

            m_stringIds[(int)StringIdType.Template] = goInfo.StringId;

            AIM_Initialize();

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
            base.Update(diff);

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

            if (m_goTypeImpl != null)
                m_goTypeImpl.Update(diff);

            if (m_perPlayerState != null)
            {
                foreach (var (guid, playerState) in m_perPlayerState.ToList())
                {
                    if (playerState.ValidUntil > GameTime.GetSystemTime())
                        continue;

                    Player seer = Global.ObjAccessor.GetPlayer(this, guid);
                    bool needsStateUpdate = playerState.State != GetGoState();
                    bool despawned = playerState.Despawned;

                    m_perPlayerState.Remove(guid);

                    if (seer != null)
                    {
                        if (despawned)
                        {
                            seer.UpdateVisibilityOf(this);
                        }
                        else if (needsStateUpdate)
                        {
                            ObjectFieldData objMask = new();
                            GameObjectFieldData goMask = new();
                            goMask.MarkChanged(m_gameObjectData.State);

                            UpdateData udata = new(GetMapId());
                            BuildValuesUpdateForPlayerWithMask(udata, objMask.GetUpdateMask(), goMask.GetUpdateMask(), seer);
                            udata.BuildPacket(out UpdateObject packet);
                            seer.SendPacket(packet);
                        }
                    }
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
                            else if (owner != null)
                            {
                                if (owner.IsInCombat())
                                    m_cooldownTime = GameTime.GetGameTimeMS() + goInfo.Trap.startDelay * Time.InMilliseconds;
                            }
                            m_lootState = LootState.Ready;
                            break;
                        }
                        case GameObjectTypes.FishingNode:
                        {
                            // fishing code (bobber ready)
                            if (GameTime.GetGameTime() > m_respawnTime - 5)
                            {
                                // splash bobber (bobber ready now)
                                Unit caster = GetOwner();
                                if (caster != null && caster.IsTypeId(TypeId.Player))
                                    SendCustomAnim(0);

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
                            ClearLoot();
                            UpdateDynamicFlagsForNearbyPlayers();
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
                                uint poolid = GetGameObjectData() != null ? GetGameObjectData().poolId : 0;
                                if (poolid != 0)
                                    Global.PoolMgr.UpdatePool<GameObject>(GetMap().GetPoolData(), poolid, GetSpawnId());
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
                            float radius = goInfo.Trap.radius / 2.0f; // this division seems to date back to when the field was called diameter, don't think it is still relevant.
                            if (radius == 0f)
                                break;

                            Unit target;
                            // @todo this hack with search required until GO casting not implemented
                            if (GetOwner() != null || goInfo.Trap.Checkallunits != 0)
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

                            if (target != null)
                                SetLootState(LootState.Activated, target);
                        }
                        else if (goInfo.type == GameObjectTypes.CapturePoint)
                        {
                            bool hordeCapturing = m_goValue.CapturePoint.State == BattlegroundCapturePointState.ContestedHorde;
                            bool allianceCapturing = m_goValue.CapturePoint.State == BattlegroundCapturePointState.ContestedAlliance;
                            if (hordeCapturing || allianceCapturing)
                            {
                                if (m_goValue.CapturePoint.AssaultTimer <= diff)
                                {
                                    m_goValue.CapturePoint.State = hordeCapturing ? BattlegroundCapturePointState.HordeCaptured : BattlegroundCapturePointState.AllianceCaptured;
                                    if (hordeCapturing)
                                    {
                                        m_goValue.CapturePoint.State = BattlegroundCapturePointState.HordeCaptured;
                                        BattlegroundMap map = GetMap().ToBattlegroundMap();
                                        if (map != null)
                                        {
                                            Battleground bg = map.GetBG();
                                            if (bg != null)
                                            {
                                                if (goInfo.CapturePoint.CaptureEventHorde != 0)
                                                    GameEvents.Trigger(goInfo.CapturePoint.CaptureEventHorde, this, this);
                                                bg.SendBroadcastText(GetGoInfo().CapturePoint.CaptureBroadcastHorde, ChatMsg.BgSystemHorde);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        m_goValue.CapturePoint.State = BattlegroundCapturePointState.AllianceCaptured;
                                        BattlegroundMap map = GetMap().ToBattlegroundMap();
                                        if (map != null)
                                        {
                                            Battleground bg = map.GetBG();
                                            if (bg != null)
                                            {
                                                if (goInfo.CapturePoint.CaptureEventAlliance != 0)
                                                    GameEvents.Trigger(goInfo.CapturePoint.CaptureEventAlliance, this, this);
                                                bg.SendBroadcastText(GetGoInfo().CapturePoint.CaptureBroadcastAlliance, ChatMsg.BgSystemAlliance);
                                            }
                                        }
                                    }

                                    m_goValue.CapturePoint.LastTeamCapture = hordeCapturing ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance;
                                    UpdateCapturePoint();
                                }
                                else
                                    m_goValue.CapturePoint.AssaultTimer -= diff;
                            }
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
                            loot?.Update();

                            // Non-consumable chest was partially looted and restock time passed, restock all loot now
                            if (GetGoInfo().Chest.consumable == 0 && m_restockTime != 0 && GameTime.GetGameTime() >= m_restockTime)
                            {
                                m_restockTime = 0;
                                m_lootState = LootState.Ready;
                                ClearLoot();
                                UpdateDynamicFlagsForNearbyPlayers();
                            }

                            foreach (var (_, loot) in m_personalLoot)
                                loot.Update();

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
                            else if (target != null)
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
                    if (linkedTrap != null)
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

                    ClearLoot();

                    // Do not delete chests or goobers that are not consumed on loot, while still allowing them to despawn when they expire if summoned
                    bool isSummonedAndExpired = (GetOwner() != null || GetSpellId() != 0) && m_respawnTime == 0;
                    if ((GetGoType() == GameObjectTypes.Chest || GetGoType() == GameObjectTypes.Goober) && !GetGoInfo().IsDespawnAtAction() && !isSummonedAndExpired)
                    {
                        if (GetGoType() == GameObjectTypes.Chest && GetGoInfo().Chest.chestRestockTime > 0)
                        {
                            // Start restock timer when the chest is fully looted
                            m_restockTime = GameTime.GetGameTime() + GetGoInfo().Chest.chestRestockTime;
                            SetLootState(LootState.NotReady);
                            UpdateDynamicFlagsForNearbyPlayers();
                        }
                        else
                            SetLootState(LootState.Ready);
                        UpdateObjectVisibility();
                        return;
                    }
                    else if (!GetOwnerGUID().IsEmpty() || GetSpellId() != 0)
                    {
                        SetRespawnTime(0);

                        if (GetGoType() == GameObjectTypes.NewFlagDrop)
                        {
                            GameObject go = GetMap().GetGameObject(GetOwnerGUID());
                            go?.HandleCustomTypeCommand(new GameObjectType.SetNewFlagState(FlagState.InBase, null));
                        }

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
                            ReplaceAllFlags(goOverride.Flags);
                    }

                    if (m_respawnDelayTime == 0)
                        return;

                    if (!m_spawnedByDefault)
                    {
                        m_respawnTime = 0;

                        if (m_spawnId != 0)
                            UpdateObjectVisibilityOnDestroy();
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
                        UpdateObjectVisibilityOnDestroy();
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

        void DespawnForPlayer(Player seer, TimeSpan respawnTime)
        {
            PerPlayerState perPlayerState = GetOrCreatePerPlayerStates(seer.GetGUID());
            perPlayerState.ValidUntil = GameTime.GetSystemTime() + respawnTime;
            perPlayerState.Despawned = true;
            seer.UpdateVisibilityOf(this);
        }

        public void Delete()
        {
            SetLootState(LootState.NotReady);
            RemoveFromOwner();

            if (m_goInfo.type == GameObjectTypes.CapturePoint)
                SendMessageToSet(new CapturePointRemoved(GetGUID()), true);

            SendGameObjectDespawn();

            if (m_goInfo.type != GameObjectTypes.Transport)
                SetGoState(GameObjectState.Ready);

            GameObjectOverride goOverride = GetGameObjectOverride();
            if (goOverride != null)
                ReplaceAllFlags(goOverride.Flags);

            uint poolid = GetGameObjectData() != null ? GetGameObjectData().poolId : 0;
            if (m_respawnCompatibilityMode && poolid != 0)
                Global.PoolMgr.UpdatePool<GameObject>(GetMap().GetPoolData(), poolid, GetSpawnId());
            else
                AddObjectToRemoveList();
        }

        public void SendGameObjectDespawn()
        {
            GameObjectDespawn packet = new();
            packet.ObjectGUID = GetGUID();
            SendMessageToSet(packet, true);
        }

        public Loot GetFishLoot(Player lootOwner)
        {
            uint defaultzone = 1;

            Loot fishLoot = new(GetMap(), GetGUID(), LootType.Fishing, null);

            uint areaId = GetAreaId();
            ItemContext itemContext = ItemBonusMgr.GetContextForPlayer(GetMap().GetMapDifficulty(), lootOwner);
            AreaTableRecord areaEntry;
            while ((areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId)) != null)
            {
                fishLoot.FillLoot(areaId, LootStorage.Fishing, lootOwner, true, true, LootModes.Default, itemContext);
                if (!fishLoot.IsLooted())
                    break;

                areaId = areaEntry.ParentAreaID;
            }

            if (fishLoot.IsLooted())
                fishLoot.FillLoot(defaultzone, LootStorage.Fishing, lootOwner, true, true, LootModes.Default, itemContext);

            return fishLoot;
        }

        public Loot GetFishLootJunk(Player lootOwner)
        {
            uint defaultzone = 1;

            Loot fishLoot = new(GetMap(), GetGUID(), LootType.FishingJunk, null);

            uint areaId = GetAreaId();
            ItemContext itemContext = ItemBonusMgr.GetContextForPlayer(GetMap().GetMapDifficulty(), lootOwner);
            AreaTableRecord areaEntry;
            while ((areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId)) != null)
            {
                fishLoot.FillLoot(areaId, LootStorage.Fishing, lootOwner, true, true, LootModes.JunkFish, itemContext);
                if (!fishLoot.IsLooted())
                    break;

                areaId = areaEntry.ParentAreaID;
            }

            if (fishLoot.IsLooted())
                fishLoot.FillLoot(defaultzone, LootStorage.Fishing, lootOwner, true, true, LootModes.JunkFish, itemContext);

            return fishLoot;
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

            uint mapId = GetMapId();
            ITransport transport = GetTransport();
            if (transport != null)
                if (transport.GetMapIdForSpawning() >= 0)
                    mapId = (uint)transport.GetMapIdForSpawning();

            SaveToDB(mapId, data.SpawnDifficulties);
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
            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT);
            stmt.AddValue(0, m_spawnId);
            DB.World.Execute(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.INS_GAMEOBJECT);
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
                    SetFlag(GameObjectFlags.NoDespawn);
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

            m_stringIds[(int)StringIdType.Spawn] = data.StringId;

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
            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_EVENT_GAMEOBJECT);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToGO);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToCreature);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToGO);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToGO);
            trans.Append(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT_ADDON);
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

            return gInfo.type == GameObjectTypes.MapObjTransport || gInfo.type == GameObjectTypes.Transport;
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

        public override bool IsNeverVisibleFor(WorldObject seer, bool allowServersideObjects = false)
        {
            if (base.IsNeverVisibleFor(seer))
                return true;

            if (GetGoInfo().GetServerOnly() != 0 && !allowServersideObjects)
                return true;

            if (GetDisplayId() == 0 && GetGoInfo().IsDisplayMandatory())
                return true;

            if (m_goTypeImpl != null)
                return m_goTypeImpl.IsNeverVisibleFor(seer, allowServersideObjects);

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
                if (owner != null && seer.IsUnit() && owner.IsFriendlyTo(seer.ToUnit()))
                    return true;
            }

            return false;
        }

        public override bool IsInvisibleDueToDespawn(WorldObject seer)
        {
            if (base.IsInvisibleDueToDespawn(seer))
                return true;

            // Despawned
            if (!IsSpawned())
                return true;

            if (m_perPlayerState != null)
            {
                PerPlayerState state = m_perPlayerState.LookupByKey(seer.GetGUID());
                if (state != null && state.Despawned)
                    return true;
            }

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

        public bool HasConditionalInteraction()
        {
            if (GetGoInfo().GetQuestID() != 0)
                return true;

            if (GetGoType() != GameObjectTypes.AuraGenerator && GetGoInfo().GetConditionID1() != 0)
                return true;

            if (Global.ObjectMgr.IsGameObjectForQuests(GetEntry()))
                return true;

            return false;
        }

        public bool CanActivateForPlayer(Player target)
        {
            if (!MeetsInteractCondition(target))
                return false;

            if (!ActivateToQuest(target))
                return false;

            return true;
        }

        public bool ActivateToQuest(Player target)
        {
            if (target.HasQuestForGO((int)GetEntry()))
                return true;

            if (!Global.ObjectMgr.IsGameObjectForQuests(GetEntry()))
                return true;

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
                    if (target.GetQuestStatus(GetGoInfo().Chest.questID) == QuestStatus.Incomplete
                        || LootStorage.Gameobject.HaveQuestLootForPlayer(GetGoInfo().Chest.chestLoot, target)
                        || LootStorage.Gameobject.HaveQuestLootForPlayer(GetGoInfo().Chest.chestPersonalLoot, target)
                        || LootStorage.Gameobject.HaveQuestLootForPlayer(GetGoInfo().Chest.chestPushLoot, target))
                    {
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
                case GameObjectTypes.SpellFocus:
                {
                    if (target.GetQuestStatus(GetGoInfo().SpellFocus.questID) == QuestStatus.Incomplete)
                        return true;
                    break;
                }
                case GameObjectTypes.Goober:
                {
                    if (target.GetQuestStatus(GetGoInfo().Goober.questID) == QuestStatus.Incomplete)
                        return true;
                    break;
                }
                case GameObjectTypes.GatheringNode:
                {
                    if (LootStorage.Gameobject.HaveQuestLootForPlayer(GetGoInfo().GatheringNode.chestLoot, target))
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
            if (trapGO != null)
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

        public void ActivateObject(GameObjectActions action, int param, WorldObject spellCaster = null, uint spellId = 0, int effectIndex = -1)
        {
            Unit unitCaster = spellCaster != null ? spellCaster.ToUnit() : null;

            switch (action)
            {
                case GameObjectActions.None:
                    Log.outFatal(LogFilter.Spells, $"Spell {spellId} has action type NONE in effect {effectIndex}");
                    break;
                case GameObjectActions.AnimateCustom0:
                case GameObjectActions.AnimateCustom1:
                case GameObjectActions.AnimateCustom2:
                case GameObjectActions.AnimateCustom3:
                    SendCustomAnim((uint)(action - GameObjectActions.AnimateCustom0));
                    break;
                case GameObjectActions.Disturb: // What's the difference with Open?
                    if (unitCaster != null)
                        Use(unitCaster);
                    break;
                case GameObjectActions.Unlock:
                    RemoveFlag(GameObjectFlags.Locked);
                    break;
                case GameObjectActions.Lock:
                    SetFlag(GameObjectFlags.Locked);
                    break;
                case GameObjectActions.Open:
                    if (unitCaster != null)
                        Use(unitCaster);
                    break;
                case GameObjectActions.OpenAndUnlock:
                    if (unitCaster != null)
                        UseDoorOrButton(0, false, unitCaster);
                    RemoveFlag(GameObjectFlags.Locked);
                    break;
                case GameObjectActions.Close:
                    ResetDoorOrButton();
                    break;
                case GameObjectActions.ToggleOpen:
                    // No use cases, implementation unknown
                    break;
                case GameObjectActions.Destroy:
                    if (unitCaster != null)
                        UseDoorOrButton(0, true, unitCaster);
                    break;
                case GameObjectActions.Rebuild:
                    ResetDoorOrButton();
                    break;
                case GameObjectActions.Creation:
                    // No use cases, implementation unknown
                    break;
                case GameObjectActions.Despawn:
                    DespawnOrUnsummon();
                    break;
                case GameObjectActions.MakeInert:
                    SetFlag(GameObjectFlags.NotSelectable);
                    break;
                case GameObjectActions.MakeActive:
                    RemoveFlag(GameObjectFlags.NotSelectable);
                    break;
                case GameObjectActions.CloseAndLock:
                    ResetDoorOrButton();
                    SetFlag(GameObjectFlags.Locked);
                    break;
                case GameObjectActions.UseArtKit0:
                case GameObjectActions.UseArtKit1:
                case GameObjectActions.UseArtKit2:
                case GameObjectActions.UseArtKit3:
                case GameObjectActions.UseArtKit4:
                {
                    GameObjectTemplateAddon templateAddon = GetTemplateAddon();

                    uint artKitIndex = action != GameObjectActions.UseArtKit4 ? (uint)(action - GameObjectActions.UseArtKit0) : 4;

                    uint artKitValue = 0;
                    if (templateAddon != null)
                        artKitValue = templateAddon.ArtKits[artKitIndex];

                    if (artKitValue == 0)
                        Log.outError(LogFilter.Sql, $"GameObject {GetEntry()} hit by spell {spellId} needs `artkit{artKitIndex}` in `gameobject_template_addon`");
                    else
                        SetGoArtKit(artKitValue);

                    break;
                }
                case GameObjectActions.GoTo1stFloor:
                case GameObjectActions.GoTo2ndFloor:
                case GameObjectActions.GoTo3rdFloor:
                case GameObjectActions.GoTo4thFloor:
                case GameObjectActions.GoTo5thFloor:
                case GameObjectActions.GoTo6thFloor:
                case GameObjectActions.GoTo7thFloor:
                case GameObjectActions.GoTo8thFloor:
                case GameObjectActions.GoTo9thFloor:
                case GameObjectActions.GoTo10thFloor:
                    if (GetGoType() == GameObjectTypes.Transport)
                        SetGoState((GameObjectState)action);
                    else
                        Log.outError(LogFilter.Spells, $"Spell {spellId} targeted non-transport gameobject for transport only action \"Go to Floor\" {action} in effect {effectIndex}");
                    break;
                case GameObjectActions.PlayAnimKit:
                    SetAnimKitId((ushort)param, false);
                    break;
                case GameObjectActions.OpenAndPlayAnimKit:
                    if (unitCaster != null)
                        UseDoorOrButton(0, false, unitCaster);
                    SetAnimKitId((ushort)param, false);
                    break;
                case GameObjectActions.CloseAndPlayAnimKit:
                    ResetDoorOrButton();
                    SetAnimKitId((ushort)param, false);
                    break;
                case GameObjectActions.PlayOneShotAnimKit:
                    SetAnimKitId((ushort)param, true);
                    break;
                case GameObjectActions.StopAnimKit:
                    SetAnimKitId(0, false);
                    break;
                case GameObjectActions.OpenAndStopAnimKit:
                    if (unitCaster != null)
                        UseDoorOrButton(0, false, unitCaster);
                    SetAnimKitId(0, false);
                    break;
                case GameObjectActions.CloseAndStopAnimKit:
                    ResetDoorOrButton();
                    SetAnimKitId(0, false);
                    break;
                case GameObjectActions.PlaySpellVisual:
                    SetSpellVisualId((uint)param, spellCaster.GetGUID());
                    break;
                case GameObjectActions.StopSpellVisual:
                    SetSpellVisualId(0);
                    break;
                default:
                    Log.outError(LogFilter.Spells, $"Spell {spellId} has unhandled action {action} in effect {effectIndex}");
                    break;
            }

            // Apply side effects of type
            if (m_goTypeImpl != null)
                m_goTypeImpl.ActivateObject(action, param, spellCaster, spellId, effectIndex);
        }

        public void SetGoArtKit(uint kit)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.ArtKit), kit);
            GameObjectData data = Global.ObjectMgr.GetGameObjectData(m_spawnId);
            if (data != null)
                data.artKit = kit;
        }

        public void SetGoArtKit(uint artkit, GameObject go, uint lowguid)
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
                SetFlag(GameObjectFlags.InUse);
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
                if (GetAI().OnGossipHello(playerUser))
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
                case GameObjectTypes.Chest:                         //3
                {
                    Player player = user.ToPlayer();
                    if (player == null)
                        return;

                    GameObjectTemplate info = GetGoInfo();
                    if (loot == null && info.GetLootId() != 0)
                    {
                        if (info.GetLootId() != 0)
                        {
                            Group group = player.GetGroup();
                            bool groupRules = group != null && info.Chest.usegrouplootrules != 0;

                            loot = new Loot(GetMap(), GetGUID(), LootType.Chest, groupRules ? group : null);
                            loot.SetDungeonEncounterId(info.Chest.DungeonEncounter);
                            loot.FillLoot(info.GetLootId(), LootStorage.Gameobject, player, !groupRules, false, GetLootMode(), ItemBonusMgr.GetContextForPlayer(GetMap().GetMapDifficulty(), player));

                            if (GetLootMode() > 0)
                            {
                                GameObjectTemplateAddon addon = GetTemplateAddon();
                                if (addon != null)
                                    loot.GenerateMoneyLoot(addon.Mingold, addon.Maxgold);
                            }
                        }

                        /// @todo possible must be moved to loot release (in different from linked triggering)
                        if (info.Chest.triggeredEvent != 0)
                            GameEvents.Trigger(info.Chest.triggeredEvent, player, this);

                        // triggering linked GO
                        uint trapEntry = info.Chest.linkedTrap;
                        if (trapEntry != 0)
                            TriggeringLinkedGameObject(trapEntry, player);
                    }
                    else if (!m_personalLoot.ContainsKey(player.GetGUID()))
                    {
                        if (info.Chest.chestPersonalLoot != 0)
                        {
                            GameObjectTemplateAddon addon = GetTemplateAddon();
                            if (info.Chest.DungeonEncounter != 0)
                            {
                                List<Player> tappers = new();
                                foreach (ObjectGuid tapperGuid in GetTapList())
                                {
                                    Player tapper = Global.ObjAccessor.GetPlayer(this, tapperGuid);
                                    if (tapper != null)
                                        tappers.Add(tapper);
                                }

                                if (tappers.Empty())
                                    tappers.Add(player);

                                m_personalLoot = LootManager.GenerateDungeonEncounterPersonalLoot(info.Chest.DungeonEncounter, info.Chest.chestPersonalLoot,
                                    LootStorage.Gameobject, LootType.Chest, this, addon != null ? addon.Mingold : 0, addon != null ? addon.Maxgold : 0,
                                    (ushort)GetLootMode(), GetMap().GetMapDifficulty(), tappers);
                            }
                            else
                            {
                                Loot loot = new(GetMap(), GetGUID(), LootType.Chest, null);
                                m_personalLoot[player.GetGUID()] = loot;

                                loot.SetDungeonEncounterId(info.Chest.DungeonEncounter);
                                loot.FillLoot(info.Chest.chestPersonalLoot, LootStorage.Gameobject, player, true, false, GetLootMode(), ItemBonusMgr.GetContextForPlayer(GetMap().GetMapDifficulty(), player));

                                if (GetLootMode() > 0 && addon != null)
                                    loot.GenerateMoneyLoot(addon.Mingold, addon.Maxgold);
                            }
                        }
                    }

                    if (!m_unique_users.Contains(player.GetGUID()) && info.GetLootId() == 0)
                    {
                        if (info.Chest.chestPushLoot != 0)
                        {
                            Loot pushLoot = new(GetMap(), GetGUID(), LootType.Chest, null);
                            pushLoot.FillLoot(info.Chest.chestPushLoot, LootStorage.Gameobject, player, true, false, GetLootMode(), ItemBonusMgr.GetContextForPlayer(GetMap().GetMapDifficulty(), player));
                            pushLoot.AutoStore(player, ItemConst.NullBag, ItemConst.NullSlot);
                        }

                        if (info.Chest.triggeredEvent != 0)
                            GameEvents.Trigger(info.Chest.triggeredEvent, player, this);

                        // triggering linked GO
                        uint trapEntry = info.Chest.linkedTrap;
                        if (trapEntry != 0)
                            TriggeringLinkedGameObject(trapEntry, player);

                        AddUniqueUse(player);
                    }

                    if (GetLootState() != LootState.Activated)
                        SetLootState(LootState.Activated, player);

                    // Send loot
                    Loot playerLoot = GetLootForPlayer(player);
                    if (playerLoot != null)
                        player.SendLoot(playerLoot);
                    break;
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

                    foreach (var (slot, sittingUnit) in ChairListSlots.ToList())
                    {
                        // the distance between this slot and the center of the go - imagine a 1D space
                        float relativeDistance = (info.size * slot) - (info.size * (info.Chair.chairslots - 1) / 2.0f);

                        float x_i = (float)(GetPositionX() + relativeDistance * Math.Cos(orthogonalOrientation));
                        float y_i = (float)(GetPositionY() + relativeDistance * Math.Sin(orthogonalOrientation));

                        if (!sittingUnit.IsEmpty())
                        {
                            Unit chairUser = Global.ObjAccessor.GetUnit(this, sittingUnit);
                            if (chairUser != null)
                            {
                                if (chairUser.IsSitState() && chairUser.GetStandState() != UnitStandStateType.Sit && chairUser.GetExactDist2d(x_i, y_i) < 0.1f)
                                    continue;        // This seat is already occupied by ChairUser. NOTE: Not sure if the ChairUser.getStandState() != UNIT_STAND_STATE_SIT check is required.

                                ChairListSlots[slot].Clear(); // This seat is unoccupied.
                            }
                            else
                                ChairListSlots[slot].Clear();     // The seat may of had an occupant, but they're offline.
                        }

                        found_free_slot = true;

                        // calculate the distance between the player and this slot
                        float thisDistance = user.GetDistance2d(x_i, y_i);

                        if (thisDistance <= lowestDist)
                        {
                            nearest_slot = slot;
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
                            ChairListSlots[nearest_slot] = user.GetGUID(); //this slot in now used by player
                            user.NearTeleportTo(x_lowest, y_lowest, GetPositionZ(), GetOrientation());
                            user.SetStandState(UnitStandStateType.SitLowChair + (byte)info.Chair.chairheight);
                            if (info.Chair.triggeredEvent != 0)
                                GameEvents.Trigger(info.Chair.triggeredEvent, user, this);
                            return;
                        }
                    }

                    return;
                }
                case GameObjectTypes.SpellFocus:                   //8
                {
                    // triggering linked GO
                    uint trapEntry = GetGoInfo().SpellFocus.linkedTrap;
                    if (trapEntry != 0)
                        TriggeringLinkedGameObject(trapEntry, user);
                    break;
                }
                //big gun, its a spell/aura
                case GameObjectTypes.Goober:                        //10
                {
                    GameObjectTemplate info = GetGoInfo();
                    Player player = user.ToPlayer();

                    if (player != null)
                    {
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
                            GameEvents.Trigger(info.Goober.eventID, player, this);
                        }

                        // possible quest objective for active quests
                        if (info.Goober.questID != 0 && Global.ObjectMgr.GetQuestTemplate(info.Goober.questID) != null)
                        {
                            //Quest require to be active for GO using
                            if (player.GetQuestStatus(info.Goober.questID) != QuestStatus.Incomplete)
                                break;
                        }

                        Group group = player.GetGroup();
                        if (group != null)
                        {
                            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                            {
                                Player member = refe.GetSource();
                                if (member != null)
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

                    if (info.Goober.AllowMultiInteract != 0 && player != null)
                    {
                        if (info.IsDespawnAtAction())
                            DespawnForPlayer(player, TimeSpan.FromSeconds(m_respawnDelayTime));
                        else
                            SetGoStateFor(GameObjectState.Active, player);
                    }
                    else
                    {
                        SetFlag(GameObjectFlags.InUse);
                        SetLootState(LootState.Activated, user);

                        // this appear to be ok, however others exist in addition to this that should have custom (ex: 190510, 188692, 187389)
                        if (info.Goober.customAnim != 0)
                            SendCustomAnim(GetGoAnimProgress());
                        else
                            SetGoState(GameObjectState.Active);

                        m_cooldownTime = GameTime.GetGameTimeMS() + info.GetAutoCloseTime();
                    }

                    // cast this spell later if provided
                    spellId = info.Goober.spell;
                    if (info.Goober.playerCast == 0)
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
                        GameEvents.Trigger(info.Camera.eventID, player, this);

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
                            SetLootState(LootState.Activated, player);

                            SetGoState(GameObjectState.Active);
                            ReplaceAllFlags(GameObjectFlags.InMultiUse);

                            SendUpdateToPlayer(player);

                            AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(GetAreaId());
                            if (areaEntry == null)
                            {
                                Log.outError(LogFilter.GameObject, $"Gameobject '{GetEntry()}' ({GetGUID()}) spawned in unknown area (x: {GetPositionX()} y: {GetPositionY()} z: {GetPositionZ()} map: {GetMapId()})");
                                break;
                            }

                            // Update the correct fishing skill according to the area's ContentTuning
                            ContentTuningRecord areaContentTuning = Global.DB2Mgr.GetContentTuningForArea(areaEntry);
                            if (areaContentTuning == null)
                                break;

                            player.UpdateFishingSkill(areaContentTuning.ExpansionID);

                            // Send loot
                            int areaFishingLevel = Global.ObjectMgr.GetFishingBaseSkillLevel(areaEntry);

                            uint playerFishingSkill = player.GetProfessionSkillForExp(SkillType.Fishing, areaContentTuning.ExpansionID);
                            int playerFishingLevel = player.GetSkillValue(playerFishingSkill);

                            int roll = RandomHelper.IRand(1, 100);
                            int chance = 100;
                            if (playerFishingLevel < areaFishingLevel)
                            {
                                chance = (int)Math.Pow((double)playerFishingLevel / areaFishingLevel, 2) * 100;
                                if (chance < 1)
                                    chance = 1;
                            }

                            Log.outDebug(LogFilter.Misc, $"Fishing check (skill {playerFishingSkill} level: {playerFishingLevel} area skill level: {areaFishingLevel} chance {chance} roll: {roll}");

                            // @todo find reasonable value for fishing hole search
                            GameObject fishingPool = LookupFishingHoleAround(20.0f + SharedConst.ContactDistance);

                            // If fishing skill is high enough, or if fishing on a pool, send correct loot.
                            // Fishing pools have no skill requirement as of patch 3.3.0 (undocumented change).
                            if (chance >= roll || fishingPool != null)
                            {
                                // @todo I do not understand this hack. Need some explanation.
                                // prevent removing GO at spell cancel
                                RemoveFromOwner();
                                SetOwnerGUID(player.GetGUID());

                                if (fishingPool != null)
                                {
                                    fishingPool.Use(player);
                                    SetLootState(LootState.JustDeactivated);
                                }
                                else
                                {
                                    loot = GetFishLoot(player);
                                    player.SendLoot(loot);
                                }
                            }
                            else// If fishing skill is too low, send junk loot.
                            {
                                loot = GetFishLootJunk(player);
                                player.SendLoot(loot);
                            }
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
                        if (player.GetVehicle() != null)
                            return;

                        if (HasFlag(GameObjectFlags.InUse))
                            return;

                        if (!MeetsInteractCondition(player))
                            return;

                        player.RemoveAurasByType(AuraType.ModStealth);
                        player.RemoveAurasByType(AuraType.ModInvisibility);
                        spellId = GetGoInfo().FlagStand.pickupSpell;
                        spellCaster = null;
                    }
                    break;
                }
                case GameObjectTypes.FishingHole:                   // 25
                {
                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    Loot loot = new(GetMap(), GetGUID(), LootType.Fishinghole, null);
                    loot.FillLoot(GetGoInfo().GetLootId(), LootStorage.Gameobject, player, true, false, LootModes.Default, ItemBonusMgr.GetContextForPlayer(GetMap().GetMapDifficulty(), player));
                    m_personalLoot[player.GetGUID()] = loot;

                    player.SendLoot(loot);
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
                        if (info != null && info.FlagDrop.eventID != 0)
                            GameEvents.Trigger(info.FlagDrop.eventID, player, this);
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

                    EnableBarberShop enableBarberShop = new();
                    enableBarberShop.CustomizationScope = (byte)info.BarberChair.CustomizationScope;
                    player.SendPacket(enableBarberShop);

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

                    Player player = user.ToPlayer();
                    if (player == null)
                        return;

                    if (!player.CanUseBattlegroundObject(this))
                        return;

                    GameObjectType.NewFlag newFlag = (GameObjectType.NewFlag)m_goTypeImpl;
                    if (newFlag == null)
                        return;

                    if (newFlag.GetState() != FlagState.InBase)
                        return;

                    spellId = info.NewFlag.pickupSpell;
                    spellCaster = null;
                    break;
                }
                case GameObjectTypes.NewFlagDrop:
                {
                    GameObjectTemplate info = GetGoInfo();
                    if (info == null)
                        return;

                    if (!user.IsPlayer())
                        return;

                    if (!user.IsAlive())
                        return;

                    GameObject owner = GetMap().GetGameObject(GetOwnerGUID());
                    if (owner != null)
                    {
                        if (owner.GetGoType() == GameObjectTypes.NewFlag)
                        {
                            GameObjectType.NewFlag newFlag = (GameObjectType.NewFlag)owner.m_goTypeImpl;
                            if (newFlag == null)
                                return;

                            if (newFlag.GetState() != FlagState.Dropped)
                                return;

                            // friendly with enemy flag means you're taking it
                            bool defenderInteract = !owner.IsFriendlyTo(user);
                            if (defenderInteract && owner.GetGoInfo().NewFlag.ReturnonDefenderInteract != 0)
                            {
                                Delete();
                                owner.HandleCustomTypeCommand(new GameObjectType.SetNewFlagState(FlagState.InBase, user.ToPlayer()));
                                return;
                            }
                            else
                            {
                                // we let the owner cast the spell for now
                                // so that caster guid is set correctly
                                SpellCastResult result = owner.CastSpell(user, owner.GetGoInfo().NewFlag.pickupSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
                                if (result == SpellCastResult.SpellCastOk)
                                {
                                    Delete();
                                    owner.HandleCustomTypeCommand(new GameObjectType.SetNewFlagState(FlagState.Taken, user.ToPlayer()));
                                    return;
                                }
                            }
                        }
                    }

                    Delete();
                    return;
                }
                case GameObjectTypes.CapturePoint:
                {
                    Player player = user.ToPlayer();
                    if (player == null)
                        return;

                    AssaultCapturePoint(player);
                    return;
                }
                case GameObjectTypes.ItemForge:
                {
                    GameObjectTemplate info = GetGoInfo();
                    if (info == null)
                        return;

                    if (!user.IsTypeId(TypeId.Player))
                        return;

                    Player player = user.ToPlayer();

                    if (!MeetsInteractCondition(player))
                        return;

                    switch (info.ItemForge.ForgeType)
                    {
                        case 0: // Artifact Forge
                        case 1: // Relic Forge
                        {

                            Aura artifactAura = player.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                            Item item = artifactAura != null ? player.GetItemByGuid(artifactAura.GetCastItemGUID()) : null;
                            if (item == null)
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
                            if (item == null)
                                return;

                            GameObjectInteraction openHeartForge = new();
                            openHeartForge.ObjectGUID = GetGUID();
                            openHeartForge.InteractionType = PlayerInteractionType.AzeriteForge;
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
                    if (player == null)
                        return;

                    GameObjectInteraction gameObjectUILink = new();
                    gameObjectUILink.ObjectGUID = GetGUID();
                    switch (GetGoInfo().UILink.UILinkType)
                    {
                        case 0:
                            gameObjectUILink.InteractionType = PlayerInteractionType.AdventureJournal;
                            break;
                        case 1:
                            gameObjectUILink.InteractionType = PlayerInteractionType.ObliterumForge;
                            break;
                        case 2:
                            gameObjectUILink.InteractionType = PlayerInteractionType.ScrappingMachine;
                            break;
                        case 3:
                            gameObjectUILink.InteractionType = PlayerInteractionType.ItemInteraction;
                            break;
                        default:
                            break;
                    }
                    player.SendPacket(gameObjectUILink);
                    return;
                }
                case GameObjectTypes.GatheringNode:                //50
                {
                    Player player = user.ToPlayer();
                    if (player == null)
                        return;

                    GameObjectTemplate info = GetGoInfo();
                    if (!m_personalLoot.ContainsKey(player.GetGUID()))
                    {
                        if (info.GatheringNode.chestLoot != 0)
                        {
                            Loot newLoot = new(GetMap(), GetGUID(), LootType.Chest, null);
                            m_personalLoot[player.GetGUID()] = newLoot;

                            newLoot.FillLoot(info.GatheringNode.chestLoot, LootStorage.Gameobject, player, true, false, GetLootMode(), ItemBonusMgr.GetContextForPlayer(GetMap().GetMapDifficulty(), player));
                        }

                        if (info.GatheringNode.triggeredEvent != 0)
                            GameEvents.Trigger(info.GatheringNode.triggeredEvent, player, this);

                        // triggering linked GO
                        uint trapEntry = info.GatheringNode.linkedTrap;
                        if (trapEntry != 0)
                            TriggeringLinkedGameObject(trapEntry, player);

                        if (info.GatheringNode.xpDifficulty != 0 && info.GatheringNode.xpDifficulty < 10)
                        {
                            QuestXPRecord questXp = CliDB.QuestXPStorage.LookupByKey(player.GetLevel());
                            if (questXp != null)
                            {
                                uint xp = Quest.RoundXPValue(questXp.Difficulty[info.GatheringNode.xpDifficulty]);
                                if (xp != 0)
                                    player.GiveXP(xp, null);
                            }
                        }

                        spellId = info.GatheringNode.spell;
                    }

                    if (m_personalLoot.Count >= info.GatheringNode.MaxNumberofLoots)
                    {
                        SetGoState(GameObjectState.Active);
                        SetDynamicFlag(GameObjectDynamicLowFlags.NoInterract);
                    }

                    if (GetLootState() != LootState.Activated)
                    {
                        SetLootState(LootState.Activated, player);
                        if (info.GatheringNode.ObjectDespawnDelay != 0)
                            DespawnOrUnsummon(TimeSpan.FromSeconds(info.GatheringNode.ObjectDespawnDelay));
                    }

                    // Send loot
                    Loot loot = GetLootForPlayer(player);
                    if (loot != null)
                        player.SendLoot(loot);
                    break;
                }
                default:
                    if (GetGoType() >= GameObjectTypes.Max)
                        Log.outError(LogFilter.Server, "GameObject.Use(): unit (type: {0}, guid: {1}, name: {2}) tries to use object (guid: {3}, entry: {4}, name: {5}) of unknown type ({6})",
                            user.GetTypeId(), user.GetGUID().ToString(), user.GetName(), GetGUID().ToString(), GetEntry(), GetGoInfo().name, GetGoType());
                    break;
            }

            if (m_vignette != null)
            {
                Player player = user.ToPlayer();
                if (player != null)
                {
                    Quest reward = Global.ObjectMgr.GetQuestTemplate((uint)m_vignette.Data.RewardQuestID);
                    if (reward != null && !player.GetQuestRewardStatus((uint)m_vignette.Data.RewardQuestID))
                        player.RewardQuest(reward, LootItemType.Item, 0, this, false);

                    if (m_vignette.Data.VisibleTrackingQuestID != 0)
                        player.SetRewardedQuest(m_vignette.Data.VisibleTrackingQuestID);
                }

                // only unregister it from visibility (need to keep vignette for other gameobject users in case its usable by multiple players
                // to flag their quest completion
                if (GetGoInfo().ClearObjectVignetteonOpening())
                    Vignettes.Remove(m_vignette, this);
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
            if (player1 != null)
                Global.OutdoorPvPMgr.HandleCustomSpell(player1, spellId, this);

            if (spellCaster != null)
                spellCaster.CastSpell(user, spellId, triggered);
            else
            {
                SpellCastResult castResult = CastSpell(user, spellId);
                if (castResult == SpellCastResult.Success)
                {
                    switch (GetGoType())
                    {
                        case GameObjectTypes.NewFlag:
                            HandleCustomTypeCommand(new GameObjectType.SetNewFlagState(FlagState.Taken, user.ToPlayer()));
                            break;
                        case GameObjectTypes.FlagStand:
                            SetFlag(GameObjectFlags.InUse);
                            ZoneScript zonescript = GetZoneScript();
                            if (zonescript != null)
                                zonescript.OnFlagTaken(this, user?.ToPlayer());

                            Delete();
                            break;
                        default:
                            break;
                    }
                }
            }
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

        public void InheritStringIds(GameObject parent)
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

        public void SetScriptStringId(string id)
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
            Quaternion rotation = new(qx, qy, qz, qw);
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

            Transport transport = GetTransport<Transport>();
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

                Quaternion worldRotation = GetWorldRotation();

                //Todo Test this. Needs checked.
                var worldSpaceBox = MathFunctions.toWorldSpace(worldRotation.ToMatrix(), new Vector3(GetPositionX(), GetPositionY(), GetPositionZ()), new Box(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ)));
                return worldSpaceBox.Contains(new Vector3(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()));
            }

            return GetExactDist(pos) <= radius;
        }

        public bool IsWithinDistInMap(Player player)
        {
            return IsInMap(player) && InSamePhase(player) && IsAtInteractDistance(player);
        }

        public SpellInfo GetSpellForLock(Player player)
        {
            if (player == null)
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
            if (m_goValue.Building.DestructibleHitpoint == null || change == 0)
                return;

            // prevent double destructions of the same object
            if (change < 0 && m_goValue.Building.Health == 0)
                return;

            if (m_goValue.Building.Health + change <= 0)
                m_goValue.Building.Health = 0;
            else if (m_goValue.Building.Health + change >= m_goValue.Building.DestructibleHitpoint.GetMaxHealth())
                m_goValue.Building.Health = m_goValue.Building.DestructibleHitpoint.GetMaxHealth();
            else
                m_goValue.Building.Health += (uint)change;

            // Set the health bar, value = 255 * healthPct;
            SetGoAnimProgress(m_goValue.Building.Health * 255 / m_goValue.Building.DestructibleHitpoint.GetMaxHealth());

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

            if (change < 0 && GetGoInfo().DestructibleBuilding.DamageEvent != 0)
                GameEvents.Trigger(GetGoInfo().DestructibleBuilding.DamageEvent, attackerOrHealer, this);

            GameObjectDestructibleState newState = GetDestructibleState();

            if (m_goValue.Building.Health == 0)
                newState = GameObjectDestructibleState.Destroyed;
            else if (m_goValue.Building.Health < m_goValue.Building.DestructibleHitpoint.DamagedNumHits)
                newState = GameObjectDestructibleState.Damaged;
            else if (m_goValue.Building.Health == m_goValue.Building.DestructibleHitpoint.GetMaxHealth())
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
                    if (setHealth && m_goValue.Building.DestructibleHitpoint != null)
                    {
                        m_goValue.Building.Health = m_goValue.Building.DestructibleHitpoint.GetMaxHealth();
                        SetGoAnimProgress(255);
                    }
                    EnableCollision(true);
                    break;
                case GameObjectDestructibleState.Damaged:
                {
                    if (GetGoInfo().DestructibleBuilding.DamagedEvent != 0 && attackerOrHealer != null)
                        GameEvents.Trigger(GetGoInfo().DestructibleBuilding.DamagedEvent, attackerOrHealer, this);
                    GetAI().Damaged(attackerOrHealer, m_goInfo.DestructibleBuilding.DamagedEvent);

                    RemoveFlag(GameObjectFlags.Destroyed);
                    SetFlag(GameObjectFlags.Damaged);

                    uint modelId = m_goInfo.displayId;
                    DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(m_goInfo.DestructibleBuilding.DestructibleModelRec);
                    if (modelData != null)
                        if (modelData.State1Wmo != 0)
                            modelId = modelData.State1Wmo;

                    SetDisplayId(modelId);

                    if (setHealth && m_goValue.Building.DestructibleHitpoint != null)
                    {
                        m_goValue.Building.Health = m_goValue.Building.DestructibleHitpoint.DamagedNumHits;
                        uint maxHealth = m_goValue.Building.DestructibleHitpoint.GetMaxHealth();
                        // in this case current health is 0 anyway so just prevent crashing here
                        if (maxHealth == 0)
                            maxHealth = 1;
                        SetGoAnimProgress(m_goValue.Building.Health * 255 / maxHealth);
                    }
                    break;
                }
                case GameObjectDestructibleState.Destroyed:
                {
                    if (GetGoInfo().DestructibleBuilding.DestroyedEvent != 0 && attackerOrHealer != null)
                        GameEvents.Trigger(GetGoInfo().DestructibleBuilding.DestroyedEvent, attackerOrHealer, this);
                    GetAI().Destroyed(attackerOrHealer, m_goInfo.DestructibleBuilding.DestroyedEvent);

                    RemoveFlag(GameObjectFlags.Damaged);
                    SetFlag(GameObjectFlags.Destroyed);

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
                    if (GetGoInfo().DestructibleBuilding.RebuildingEvent != 0 && attackerOrHealer != null)
                        GameEvents.Trigger(GetGoInfo().DestructibleBuilding.RebuildingEvent, attackerOrHealer, this);
                    RemoveFlag(GameObjectFlags.Damaged | GameObjectFlags.Destroyed);

                    uint modelId = m_goInfo.displayId;
                    DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(m_goInfo.DestructibleBuilding.DestructibleModelRec);
                    if (modelData != null)
                        if (modelData.State3Wmo != 0)
                            modelId = modelData.State3Wmo;
                    SetDisplayId(modelId);

                    // restores to full health
                    if (setHealth & m_goValue.Building.DestructibleHitpoint != null)
                    {
                        m_goValue.Building.Health = m_goValue.Building.DestructibleHitpoint.GetMaxHealth();
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
            m_lootStateUnitGUID = unit != null ? unit.GetGUID() : ObjectGuid.Empty;
            GetAI().OnLootStateChanged((uint)state, unit);

            // Start restock timer if the chest is partially looted or not looted at all
            if (GetGoType() == GameObjectTypes.Chest && state == LootState.Activated && GetGoInfo().Chest.chestRestockTime > 0 && m_restockTime == 0 && loot != null && loot.IsChanged())
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

        void ClearLoot()
        {
            // Unlink loot objects from this GameObject before destroying to avoid accessing freed memory from Loot destructor
            loot = null;
            m_personalLoot.Clear();
            m_unique_users.Clear();
            m_usetimes = 0;
        }

        public bool IsFullyLooted()
        {
            if (loot != null && !loot.IsLooted())
                return false;

            foreach (var (_, loot) in m_personalLoot)
                if (!loot.IsLooted())
                    return false;

            return true;
        }

        public void OnLootRelease(Player looter)
        {
            switch (GetGoType())
            {
                case GameObjectTypes.Chest:
                {
                    GameObjectTemplate goInfo = GetGoInfo();
                    if (goInfo.Chest.consumable == 0 && goInfo.Chest.chestPersonalLoot != 0)
                    {
                        DespawnForPlayer(looter, goInfo.Chest.chestRestockTime != 0
                            ? TimeSpan.FromSeconds(goInfo.Chest.chestRestockTime)
                            : TimeSpan.FromSeconds(m_respawnDelayTime)); // not hiding this object permanently to prevent infinite growth of m_perPlayerState
                                                                         // while also maintaining some sort of cheater protection (not getting rid of entries on logout)
                    }
                    break;
                }
                case GameObjectTypes.GatheringNode:
                {
                    SetGoStateFor(GameObjectState.Active, looter);

                    ObjectFieldData objMask = new();
                    GameObjectFieldData goMask = new();
                    objMask.MarkChanged(objMask.DynamicFlags);

                    UpdateData udata = new(GetMapId());
                    BuildValuesUpdateForPlayerWithMask(udata, objMask.GetUpdateMask(), goMask.GetUpdateMask(), looter);
                    udata.BuildPacket(out UpdateObject packet);
                    looter.SendPacket(packet);
                    break;
                }
            }
        }

        public void SetGoState(GameObjectState state)
        {
            GameObjectState oldState = GetGoState();
            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.State), (sbyte)state);
            if (GetAI() != null)
                GetAI().OnStateChanged(state);

            if (m_goTypeImpl != null)
                m_goTypeImpl.OnStateChanged(oldState, state);

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

        public GameObjectState GetGoStateFor(ObjectGuid viewer)
        {
            if (m_perPlayerState != null)
            {
                PerPlayerState state = m_perPlayerState.LookupByKey(viewer);
                if (state != null && state.State.HasValue)
                    return state.State.Value;
            }

            return GetGoState();
        }

        void SetGoStateFor(GameObjectState state, Player viewer)
        {
            PerPlayerState perPlayerState = GetOrCreatePerPlayerStates(viewer.GetGUID());
            perPlayerState.ValidUntil = GameTime.GetSystemTime() + TimeSpan.FromSeconds(m_respawnDelayTime);
            perPlayerState.State = state;

            GameObjectSetStateLocal setStateLocal = new();
            setStateLocal.ObjectGUID = GetGUID();
            setStateLocal.State = (byte)state;
            viewer.SendPacket(setStateLocal);
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

        public bool IsLootAllowedFor(Player player)
        {
            Loot loot = GetLootForPlayer(player);
            if (loot != null) // check only if loot was already generated
            {
                if (loot.IsLooted()) // nothing to loot or everything looted.
                    return false;
                if (!loot.HasAllowedLooter(GetGUID()) || (!loot.HasItemForAll() && !loot.HasItemFor(player))) // no loot in chest for this player
                    return false;
            }

            if (HasLootRecipient())
                return m_tapList.Contains(player.GetGUID());                                      // if go doesnt have group bound it means it was solo killed by someone else

            return true;
        }

        public override Loot GetLootForPlayer(Player player)
        {
            if (m_personalLoot.Empty())
                return loot;

            return m_personalLoot.LookupByKey(player.GetGUID());
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

        public List<uint> GetPauseTimes()
        {
            if (m_goTypeImpl is GameObjectType.Transport transport)
                return transport.GetPauseTimes();

            return null;
        }

        public void SetPathProgressForClient(float progress)
        {
            DoWithSuppressingObjectUpdates(() =>
            {
                ObjectFieldData dynflagMask = new();
                dynflagMask.MarkChanged(m_objectData.DynamicFlags);
                bool marked = (m_objectData.GetUpdateMask() & dynflagMask.GetUpdateMask()).IsAnySet();

                uint dynamicFlags = (uint)GetDynamicFlags();
                dynamicFlags &= 0xFFFF; // remove high bits
                dynamicFlags |= (uint)(progress * 65535.0f) << 16;
                ReplaceAllDynamicFlags((GameObjectDynamicLowFlags)dynamicFlags);

                if (!marked)
                    m_objectData.ClearChanged(m_objectData.DynamicFlags);
            });
        }

        public void GetRespawnPosition(out float x, out float y, out float z, out float ori)
        {
            if (m_goData != null)
                m_goData.SpawnPoint.GetPosition(out x, out y, out z, out ori);
            else
                GetPosition(out x, out y, out z, out ori);
        }

        public ITransport ToTransportBase()
        {
            switch (GetGoType())
            {
                case GameObjectTypes.Transport:
                    return (GameObjectType.Transport)m_goTypeImpl;
                case GameObjectTypes.MapObjTransport:
                    return (Transport)this;
                default:
                    break;
            }

            return null;
        }

        public void AfterRelocation()
        {
            UpdateModelPosition();
            UpdatePositionData();
            if (m_goTypeImpl != null)
                m_goTypeImpl.OnRelocated();

            // TODO: on heartbeat
            if (m_vignette != null)
                Vignettes.Update(m_vignette, this);

            UpdateObjectVisibility(false);
        }

        public float GetInteractionDistance()
        {
            if (GetGoInfo().GetInteractRadiusOverride() != 0)
                return (float)GetGoInfo().GetInteractRadiusOverride() / 100.0f;

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

        public override VignetteData GetVignette() { return m_vignette; }

        public void SetVignette(uint vignetteId)
        {
            if (m_vignette != null)
            {
                if (m_vignette.Data.ID == vignetteId)
                    return;

                Vignettes.Remove(m_vignette, this);
                m_vignette = null;
            }

            VignetteRecord vignette = CliDB.VignetteStorage.LookupByKey(vignetteId);
            if (vignette != null)
                m_vignette = Vignettes.Create(vignette, this);
        }

        public void SetSpellVisualId(uint spellVisualId, ObjectGuid activatorGuid = default)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.SpellVisualID), spellVisualId);

            GameObjectPlaySpellVisual packet = new();
            packet.ObjectGUID = GetGUID();
            packet.ActivatorGUID = activatorGuid;
            packet.SpellVisualID = spellVisualId;
            SendMessageToSet(packet, true);
        }

        public void AssaultCapturePoint(Player player)
        {
            if (!CanInteractWithCapturePoint(player))
                return;

            GameObjectAI ai = GetAI();
            if (ai != null)
                if (ai.OnCapturePointAssaulted(player))
                    return;

            // only supported in battlegrounds
            Battleground battleground = null;
            BattlegroundMap map = GetMap().ToBattlegroundMap();
            if (map != null)
            {
                Battleground bg = map.GetBG();
                if (bg != null)
                    battleground = bg;
            }

            if (battleground == null)
                return;

            // Cancel current timer
            m_goValue.CapturePoint.AssaultTimer = 0;

            if (player.GetBGTeam() == Team.Horde)
            {
                if (m_goValue.CapturePoint.LastTeamCapture == BattleGroundTeamId.Horde)
                {
                    // defended. capture instantly.
                    m_goValue.CapturePoint.State = BattlegroundCapturePointState.HordeCaptured;
                    battleground.SendBroadcastText(GetGoInfo().CapturePoint.DefendedBroadcastHorde, ChatMsg.BgSystemHorde, player);
                    UpdateCapturePoint();
                    if (GetGoInfo().CapturePoint.DefendedEventHorde != 0)
                        GameEvents.Trigger(GetGoInfo().CapturePoint.DefendedEventHorde, player, this);
                    return;
                }

                switch (m_goValue.CapturePoint.State)
                {
                    case BattlegroundCapturePointState.Neutral:
                    case BattlegroundCapturePointState.AllianceCaptured:
                    case BattlegroundCapturePointState.ContestedAlliance:
                        m_goValue.CapturePoint.State = BattlegroundCapturePointState.ContestedHorde;
                        battleground.SendBroadcastText(GetGoInfo().CapturePoint.AssaultBroadcastHorde, ChatMsg.BgSystemHorde, player);
                        UpdateCapturePoint();
                        if (GetGoInfo().CapturePoint.ContestedEventHorde != 0)
                            GameEvents.Trigger(GetGoInfo().CapturePoint.ContestedEventHorde, player, this);
                        m_goValue.CapturePoint.AssaultTimer = GetGoInfo().CapturePoint.CaptureTime;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (m_goValue.CapturePoint.LastTeamCapture == BattleGroundTeamId.Alliance)
                {
                    // defended. capture instantly.
                    m_goValue.CapturePoint.State = BattlegroundCapturePointState.AllianceCaptured;
                    battleground.SendBroadcastText(GetGoInfo().CapturePoint.DefendedBroadcastAlliance, ChatMsg.BgSystemAlliance, player);
                    UpdateCapturePoint();
                    if (GetGoInfo().CapturePoint.DefendedEventAlliance != 0)
                        GameEvents.Trigger(GetGoInfo().CapturePoint.DefendedEventAlliance, player, this);
                    return;
                }

                switch (m_goValue.CapturePoint.State)
                {
                    case BattlegroundCapturePointState.Neutral:
                    case BattlegroundCapturePointState.HordeCaptured:
                    case BattlegroundCapturePointState.ContestedHorde:
                        m_goValue.CapturePoint.State = BattlegroundCapturePointState.ContestedAlliance;
                        battleground.SendBroadcastText(GetGoInfo().CapturePoint.AssaultBroadcastAlliance, ChatMsg.BgSystemAlliance, player);
                        UpdateCapturePoint();
                        if (GetGoInfo().CapturePoint.ContestedEventAlliance != 0)
                            GameEvents.Trigger(GetGoInfo().CapturePoint.ContestedEventAlliance, player, this);
                        m_goValue.CapturePoint.AssaultTimer = GetGoInfo().CapturePoint.CaptureTime;
                        break;
                    default:
                        break;
                }
            }
        }

        void UpdateCapturePoint()
        {
            if (GetGoType() != GameObjectTypes.CapturePoint)
                return;

            GameObjectAI ai = GetAI();
            if (ai != null)
                if (ai.OnCapturePointUpdated(m_goValue.CapturePoint.State))
                    return;

            uint spellVisualId = 0;
            uint customAnim = 0;

            switch (m_goValue.CapturePoint.State)
            {
                case BattlegroundCapturePointState.Neutral:
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual1;
                    break;
                case BattlegroundCapturePointState.ContestedHorde:
                    customAnim = 1;
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual2;
                    break;
                case BattlegroundCapturePointState.ContestedAlliance:
                    customAnim = 2;
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual3;
                    break;
                case BattlegroundCapturePointState.HordeCaptured:
                    customAnim = 3;
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual4;
                    break;
                case BattlegroundCapturePointState.AllianceCaptured:
                    customAnim = 4;
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual5;
                    break;
                default:
                    break;
            }

            if (customAnim != 0)
                SendCustomAnim(customAnim);

            SetSpellVisualId(spellVisualId);
            UpdateDynamicFlagsForNearbyPlayers();

            BattlegroundMap map = GetMap().ToBattlegroundMap();
            if (map != null)
            {
                Battleground bg = map.GetBG();
                if (bg != null)
                {
                    UpdateCapturePoint packet = new();
                    packet.CapturePointInfo.State = m_goValue.CapturePoint.State;
                    packet.CapturePointInfo.Pos = GetPosition();
                    packet.CapturePointInfo.Guid = GetGUID();
                    packet.CapturePointInfo.CaptureTotalDuration = TimeSpan.FromMilliseconds(GetGoInfo().CapturePoint.CaptureTime);
                    packet.CapturePointInfo.CaptureTime = m_goValue.CapturePoint.AssaultTimer;
                    bg.SendPacketToAll(packet);
                    bg.UpdateWorldState((int)GetGoInfo().CapturePoint.worldState1, (byte)m_goValue.CapturePoint.State);
                }
            }

            GetMap().UpdateSpawnGroupConditions();
        }

        public bool CanInteractWithCapturePoint(Player target)
        {
            if (m_goInfo.type != GameObjectTypes.CapturePoint)
                return false;

            if (m_goValue.CapturePoint.State == BattlegroundCapturePointState.Neutral)
                return true;

            if (target.GetBGTeam() == Team.Horde)
            {
                return m_goValue.CapturePoint.State == BattlegroundCapturePointState.ContestedAlliance
                    || m_goValue.CapturePoint.State == BattlegroundCapturePointState.AllianceCaptured;
            }

            // For Alliance players
            return m_goValue.CapturePoint.State == BattlegroundCapturePointState.ContestedHorde
                || m_goValue.CapturePoint.State == BattlegroundCapturePointState.HordeCaptured;
        }

        public FlagState GetFlagState()
        {
            if (GetGoType() != GameObjectTypes.NewFlag)
                return 0;

            if (m_goTypeImpl is not GameObjectType.NewFlag newFlag)
                return 0;

            return newFlag.GetState();
        }

        public ObjectGuid GetFlagCarrierGUID()
        {
            if (GetGoType() != GameObjectTypes.NewFlag)
                return ObjectGuid.Empty;

            if (m_goTypeImpl is not GameObjectType.NewFlag newFlag)
                return ObjectGuid.Empty;

            return newFlag.GetCarrierGUID();
        }

        public long GetFlagTakenFromBaseTime()
        {
            if (GetGoType() != GameObjectTypes.NewFlag)
                return 0;

            if (m_goTypeImpl is not GameObjectType.NewFlag newFlag)
                return 0;

            return newFlag.GetTakenFromBaseTime();
        }

        public List<ObjectGuid> GetInsidePlayers()
        {
            if (m_goTypeImpl is ControlZone controlZone)
                return controlZone.GetInsidePlayers();

            return null;
        }

        public bool MeetsInteractCondition(Player user)
        {
            return ConditionManager.IsPlayerMeetingCondition(user, m_goInfo.GetConditionID1());
        }

        PerPlayerState GetOrCreatePerPlayerStates(ObjectGuid guid)
        {
            if (m_perPlayerState == null)
                m_perPlayerState = new();

            if (!m_perPlayerState.ContainsKey(guid))
                m_perPlayerState[guid] = new();

            return m_perPlayerState[guid];
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

        public override ObjectGuid GetCreatorGUID() { return m_gameObjectData.CreatedBy; }
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
        public void SetFlag(GameObjectFlags flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.Flags), (uint)flags); }
        public void RemoveFlag(GameObjectFlags flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.Flags), (uint)flags); }
        public void ReplaceAllFlags(GameObjectFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.Flags), (uint)flags); }
        public void SetLevel(uint level) { SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.Level), level); }

        public GameObjectDynamicLowFlags GetDynamicFlags() { return (GameObjectDynamicLowFlags)(uint)m_objectData.DynamicFlags; }
        public bool HasDynamicFlag(GameObjectDynamicLowFlags flag) { return (m_objectData.DynamicFlags & (uint)flag) != 0; }
        public void SetDynamicFlag(GameObjectDynamicLowFlags flag) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_objectData).ModifyValue(m_objectData.DynamicFlags), (uint)flag); }
        public void RemoveDynamicFlag(GameObjectDynamicLowFlags flag) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_objectData).ModifyValue(m_objectData.DynamicFlags), (uint)flag); }
        public void ReplaceAllDynamicFlags(GameObjectDynamicLowFlags flag) { SetUpdateFieldValue(m_values.ModifyValue(m_objectData).ModifyValue(m_objectData.DynamicFlags), (uint)flag); }

        public GameObjectTypes GetGoType() { return (GameObjectTypes)(sbyte)m_gameObjectData.TypeID; }
        public void SetGoType(GameObjectTypes type) { SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.TypeID), (sbyte)type); }
        public GameObjectState GetGoState() { return (GameObjectState)(sbyte)m_gameObjectData.State; }
        public uint GetGoArtKit() { return m_gameObjectData.ArtKit; }
        public byte GetGoAnimProgress() { return m_gameObjectData.PercentHealth; }
        public void SetGoAnimProgress(uint animprogress) { SetUpdateFieldValue(m_values.ModifyValue(m_gameObjectData).ModifyValue(m_gameObjectData.PercentHealth), (byte)animprogress); }

        public LootState GetLootState() { return m_lootState; }
        public LootModes GetLootMode() { return m_LootMode; }
        public bool HasLootMode(LootModes lootMode) { return Convert.ToBoolean(m_LootMode & lootMode); }
        public void SetLootMode(LootModes lootMode) { m_LootMode = lootMode; }
        public void AddLootMode(LootModes lootMode) { m_LootMode |= lootMode; }
        public void RemoveLootMode(LootModes lootMode) { m_LootMode &= ~lootMode; }
        public void ResetLootMode() { m_LootMode = LootModes.Default; }

        public void AddToSkillupList(ObjectGuid PlayerGuid) { m_SkillupList.Add(PlayerGuid); }
        public bool IsInSkillupList(ObjectGuid PlayerGuid)
        {
            foreach (var i in m_SkillupList)
                if (i == PlayerGuid)
                    return true;

            return false;
        }
        public void ClearSkillupList() { m_SkillupList.Clear(); }

        public void AddUse() { ++m_usetimes; }

        public uint GetUseCount() { return m_usetimes; }
        public uint GetUniqueUseCount() { return (uint)m_unique_users.Count; }

        public List<ObjectGuid> GetTapList() { return m_tapList; }
        public void SetTapList(List<ObjectGuid> tapList) { m_tapList = tapList; }

        public bool HasLootRecipient() { return !m_tapList.Empty(); }

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
        public Position GetStationaryPosition() { return StationaryPosition; }

        public void RelocateStationaryPosition(float x, float y, float z, float o) { StationaryPosition.Relocate(x, y, z, o); }

        //! Object distance/size - overridden from Object._IsWithinDist. Needs to take in account proper GO size.
        public override bool _IsWithinDist(WorldObject obj, float dist2compare, bool is3D, bool incOwnRadius, bool incTargetRadius)
        {
            //! Following check does check 3d distance
            return IsInRange(obj.GetPositionX(), obj.GetPositionY(), obj.GetPositionZ(), dist2compare);
        }

        void UpdateDynamicFlagsForNearbyPlayers()
        {
            m_values.ModifyValue(m_objectData).ModifyValue(m_objectData.DynamicFlags);
            AddToObjectUpdateIfNeeded();
        }

        public void HandleCustomTypeCommand(GameObjectTypeBase.CustomCommand command)
        {
            if (m_goTypeImpl != null)
                command.Execute(m_goTypeImpl);
        }

        public void CreateModel()
        {
            m_model = GameObjectModel.Create(new GameObjectModelOwnerImpl(this));
            if (m_model != null)
            {
                if (m_model.IsMapObject())
                    SetFlag(GameObjectFlags.MapObject);

                if (GetGoType() == GameObjectTypes.Door)
                    m_model.DisableLosBlocking(GetGoInfo().Door.NotLOSBlocking != 0);
            }
        }

        // There's many places not ready for dynamic spawns. This allows them to live on for now.
        void SetRespawnCompatibilityMode(bool mode = true) { m_respawnCompatibilityMode = mode; }
        public bool GetRespawnCompatibilityMode() { return m_respawnCompatibilityMode; }

        #region Fields
        public GameObjectFieldData m_gameObjectData;
        GameObjectTypeBase m_goTypeImpl;
        protected GameObjectValue m_goValue; // TODO: replace with m_goTypeImpl
        string[] m_stringIds = new string[3];
        string m_scriptStringId;
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

        List<ObjectGuid> m_tapList = new();
        LootModes m_LootMode;                                  // bitmask, default LOOT_MODE_DEFAULT, determines what loot will be lootable
        long m_packedRotation;
        Quaternion m_localRotation;
        public Position StationaryPosition { get; set; }

        GameObjectAI m_AI;
        bool m_respawnCompatibilityMode;
        ushort _animKitId;
        uint _worldEffectID;

        VignetteData m_vignette;

        Dictionary<ObjectGuid, PerPlayerState> m_perPlayerState;

        GameObjectState m_prevGoState;                          // What state to set whenever resetting

        Dictionary<uint, ObjectGuid> ChairListSlots = new();
        List<ObjectGuid> m_SkillupList = new();

        public Loot loot;
        Dictionary<ObjectGuid, Loot> m_personalLoot = new();

        public GameObjectModel m_model;

        ObjectGuid m_linkedTrap;
        #endregion

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            public GameObject Owner;
            public ObjectFieldData ObjectMask = new();
            public GameObjectFieldData GameObjectMask = new();

            public ValuesUpdateForPlayerWithMaskSender(GameObject owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), GameObjectMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
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
        public override Quaternion GetRotation() { return new Quaternion(_owner.GetLocalRotation().X, _owner.GetLocalRotation().Y, _owner.GetLocalRotation().Z, _owner.GetLocalRotation().W); }
        public override float GetScale() { return _owner.GetObjectScale(); }

        GameObject _owner;
    }

    // Base class for GameObject type specific implementations
    public class GameObjectTypeBase(GameObject owner)
    {
        protected GameObject _owner = owner;

        public virtual void Update(uint diff) { }
        public virtual void OnStateChanged(GameObjectState oldState, GameObjectState newState) { }
        public virtual void OnRelocated() { }
        public virtual bool IsNeverVisibleFor(WorldObject seer, bool allowServersideObjects) { return false; }
        public virtual void ActivateObject(GameObjectActions action, int param, WorldObject spellCaster = null, uint spellId = 0, int effectIndex = -1) { }

        public class CustomCommand
        {
            public virtual void Execute(GameObjectTypeBase type) { }
        }
    }

    public struct GameObjectValue
    {
        public transport Transport;

        public fishinghole FishingHole;

        public building Building;

        public capturePoint CapturePoint;

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
            public DestructibleHitpoint DestructibleHitpoint;
        }
        //42 GAMEOBJECT_TYPE_CAPTURE_POINT
        public struct capturePoint
        {
            public int LastTeamCapture;
            public BattlegroundCapturePointState State;
            public uint AssaultTimer;
        }
    }

    namespace GameObjectType
    {
        //11 GAMEOBJECT_TYPE_TRANSPORT
        class Transport : GameObjectTypeBase, ITransport
        {
            TransportAnimation _animationInfo;
            uint _pathProgress;
            uint _stateChangeTime;
            uint _stateChangeProgress;
            List<uint> _stopFrames = new();
            bool _autoCycleBetweenStopFrames;
            TimeTracker _positionUpdateTimer = new();
            List<WorldObject> _passengers = new();

            static TimeSpan PositionUpdateInterval = TimeSpan.FromMilliseconds(50);

            public Transport(GameObject owner) : base(owner)
            {
                _animationInfo = Global.TransportMgr.GetTransportAnimInfo(owner.GetGoInfo().entry);
                _pathProgress = GameTime.GetGameTimeMS() % GetTransportPeriod();
                _stateChangeTime = GameTime.GetGameTimeMS();
                _stateChangeProgress = _pathProgress;

                GameObjectTemplate goInfo = _owner.GetGoInfo();
                if (goInfo.Transport.Timeto2ndfloor > 0)
                {
                    _stopFrames.Add(goInfo.Transport.Timeto2ndfloor);
                    if (goInfo.Transport.Timeto3rdfloor > 0)
                    {
                        _stopFrames.Add(goInfo.Transport.Timeto3rdfloor);
                        if (goInfo.Transport.Timeto4thfloor > 0)
                        {
                            _stopFrames.Add(goInfo.Transport.Timeto4thfloor);
                            if (goInfo.Transport.Timeto5thfloor > 0)
                            {
                                _stopFrames.Add(goInfo.Transport.Timeto5thfloor);
                                if (goInfo.Transport.Timeto6thfloor > 0)
                                {
                                    _stopFrames.Add(goInfo.Transport.Timeto6thfloor);
                                    if (goInfo.Transport.Timeto7thfloor > 0)
                                    {
                                        _stopFrames.Add(goInfo.Transport.Timeto7thfloor);
                                        if (goInfo.Transport.Timeto8thfloor > 0)
                                        {
                                            _stopFrames.Add(goInfo.Transport.Timeto8thfloor);
                                            if (goInfo.Transport.Timeto9thfloor > 0)
                                            {
                                                _stopFrames.Add(goInfo.Transport.Timeto9thfloor);
                                                if (goInfo.Transport.Timeto10thfloor > 0)
                                                    _stopFrames.Add(goInfo.Transport.Timeto10thfloor);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!_stopFrames.Empty())
                {
                    _pathProgress = 0;
                    _stateChangeProgress = 0;
                }

                _positionUpdateTimer.Reset(PositionUpdateInterval);
            }

            public override void Update(uint diff)
            {
                if (_animationInfo == null)
                    return;

                _positionUpdateTimer.Update(diff);
                if (!_positionUpdateTimer.Passed())
                    return;

                _positionUpdateTimer.Reset(PositionUpdateInterval);

                uint now = GameTime.GetGameTimeMS();
                uint period = GetTransportPeriod();
                uint newProgress;
                if (_stopFrames.Empty())
                    newProgress = now % period;
                else
                {
                    int stopTargetTime;
                    if (_owner.GetGoState() == GameObjectState.TransportActive)
                        stopTargetTime = 0;
                    else
                        stopTargetTime = (int)(_stopFrames[_owner.GetGoState() - GameObjectState.TransportStopped]);

                    if (now < _owner.m_gameObjectData.Level)
                    {
                        int timeToStop = (int)(_owner.m_gameObjectData.Level - _stateChangeTime);
                        float stopSourcePathPct = (float)_stateChangeProgress / (float)period;
                        float stopTargetPathPct = (float)stopTargetTime / (float)period;
                        float timeSinceStopProgressPct = (float)(now - _stateChangeTime) / (float)timeToStop;

                        float progressPct;
                        if (!_owner.HasDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement))
                        {
                            if (_owner.GetGoState() == GameObjectState.TransportActive)
                                stopTargetPathPct = 1.0f;

                            float pathPctBetweenStops = stopTargetPathPct - stopSourcePathPct;
                            if (pathPctBetweenStops < 0.0f)
                                pathPctBetweenStops += 1.0f;

                            progressPct = pathPctBetweenStops * timeSinceStopProgressPct + stopSourcePathPct;
                            if (progressPct > 1.0f)
                                progressPct = -1.0f;
                        }
                        else
                        {
                            float pathPctBetweenStops = stopSourcePathPct - stopTargetPathPct;
                            if (pathPctBetweenStops < 0.0f)
                                pathPctBetweenStops += 1.0f;

                            progressPct = stopSourcePathPct - pathPctBetweenStops * timeSinceStopProgressPct;
                            if (progressPct < 0.0f)
                                progressPct += 1.0f;
                        }

                        newProgress = (uint)((float)period * progressPct) % period;
                    }
                    else
                        newProgress = (uint)stopTargetTime;

                    if (newProgress == stopTargetTime && newProgress != _pathProgress)
                    {
                        uint eventId;
                        switch (_owner.GetGoState() - GameObjectState.TransportActive)
                        {
                            case 0:
                                eventId = _owner.GetGoInfo().Transport.Reached1stfloor;
                                break;
                            case 1:
                                eventId = _owner.GetGoInfo().Transport.Reached2ndfloor;
                                break;
                            case 2:
                                eventId = _owner.GetGoInfo().Transport.Reached3rdfloor;
                                break;
                            case 3:
                                eventId = _owner.GetGoInfo().Transport.Reached4thfloor;
                                break;
                            case 4:
                                eventId = _owner.GetGoInfo().Transport.Reached5thfloor;
                                break;
                            case 5:
                                eventId = _owner.GetGoInfo().Transport.Reached6thfloor;
                                break;
                            case 6:
                                eventId = _owner.GetGoInfo().Transport.Reached7thfloor;
                                break;
                            case 7:
                                eventId = _owner.GetGoInfo().Transport.Reached8thfloor;
                                break;
                            case 8:
                                eventId = _owner.GetGoInfo().Transport.Reached9thfloor;
                                break;
                            case 9:
                                eventId = _owner.GetGoInfo().Transport.Reached10thfloor;
                                break;
                            default:
                                eventId = 0u;
                                break;
                        }

                        if (eventId != 0)
                            GameEvents.Trigger(eventId, _owner, _owner);

                        if (_autoCycleBetweenStopFrames)
                        {
                            GameObjectState currentState = _owner.GetGoState();
                            GameObjectState newState;
                            if (currentState == GameObjectState.TransportActive)
                                newState = GameObjectState.TransportStopped;
                            else if (currentState - GameObjectState.TransportActive == _stopFrames.Count)
                                newState = currentState - 1;
                            else if (_owner.HasDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement))
                                newState = currentState - 1;
                            else
                                newState = currentState + 1;

                            _owner.SetGoState(newState);
                        }
                    }
                }

                if (_pathProgress == newProgress)
                    return;

                _pathProgress = newProgress;

                TransportAnimationRecord oldAnimation = _animationInfo.GetPrevAnimNode(newProgress);
                TransportAnimationRecord newAnimation = _animationInfo.GetNextAnimNode(newProgress);
                if (oldAnimation != null && newAnimation != null)
                {
                    Matrix4x4 pathRotation = new Quaternion(_owner.m_gameObjectData.ParentRotation.GetValue().X, _owner.m_gameObjectData.ParentRotation.GetValue().Y,
                        _owner.m_gameObjectData.ParentRotation.GetValue().Z, _owner.m_gameObjectData.ParentRotation.GetValue().W).ToMatrix();

                    Vector3 prev = new(oldAnimation.Pos.X, oldAnimation.Pos.Y, oldAnimation.Pos.Z);
                    Vector3 next = new(newAnimation.Pos.X, newAnimation.Pos.Y, newAnimation.Pos.Z);

                    Vector3 dst = next;
                    if (prev != next)
                    {
                        float animProgress = (float)(newProgress - oldAnimation.TimeIndex) / (float)(newAnimation.TimeIndex - oldAnimation.TimeIndex);

                        dst = pathRotation.Multiply(Vector3.Lerp(prev, next, animProgress));
                    }

                    dst = pathRotation.Multiply(dst);
                    dst += _owner.GetStationaryPosition();

                    _owner.GetMap().GameObjectRelocation(_owner, dst.X, dst.Y, dst.Z, _owner.GetOrientation());
                }

                TransportRotationRecord oldRotation = _animationInfo.GetPrevAnimRotation(newProgress);
                TransportRotationRecord newRotation = _animationInfo.GetNextAnimRotation(newProgress);
                if (oldRotation != null && newRotation != null)
                {
                    Quaternion prev = new(oldRotation.Rot[0], oldRotation.Rot[1], oldRotation.Rot[2], oldRotation.Rot[3]);
                    Quaternion next = new(newRotation.Rot[0], newRotation.Rot[1], newRotation.Rot[2], newRotation.Rot[3]);

                    Quaternion rotation = next;

                    if (prev != next)
                    {
                        float animProgress = (float)(newProgress - oldRotation.TimeIndex) / (float)(newRotation.TimeIndex - oldRotation.TimeIndex);

                        rotation = Quaternion.Lerp(prev, next, animProgress);
                    }

                    _owner.SetLocalRotation(rotation.X, rotation.Y, rotation.Z, rotation.W);
                    _owner.UpdateModelPosition();
                }

                // update progress marker for client
                _owner.SetPathProgressForClient((float)_pathProgress / (float)period);
            }

            public override void OnStateChanged(GameObjectState oldState, GameObjectState newState)
            {
                Cypher.Assert(newState >= GameObjectState.TransportActive);

                // transports without stop frames just keep animating in state 24
                if (_stopFrames.Empty())
                {
                    if (newState != GameObjectState.TransportActive)
                        _owner.SetGoState(GameObjectState.TransportActive);
                    return;
                }

                uint stopPathProgress = 0;

                if (newState != GameObjectState.TransportActive)
                {
                    Cypher.Assert(newState < (GameObjectState)(GameObjectState.TransportStopped + 9));
                    int stopFrame = (int)(newState - GameObjectState.TransportStopped);
                    Cypher.Assert(stopFrame < _stopFrames.Count);
                    stopPathProgress = _stopFrames[stopFrame];
                }

                _stateChangeTime = GameTime.GetGameTimeMS();
                _stateChangeProgress = _pathProgress;
                uint timeToStop = (uint)Math.Abs(_pathProgress - stopPathProgress);
                _owner.SetLevel(GameTime.GetGameTimeMS() + timeToStop);
                _owner.SetPathProgressForClient((float)_pathProgress / (float)GetTransportPeriod());

                if (oldState == GameObjectState.Active || oldState == newState)
                {
                    // initialization
                    if (_pathProgress > stopPathProgress)
                        _owner.SetDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
                    else
                        _owner.RemoveDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);

                    return;
                }

                int pauseTimesCount = _stopFrames.Count;
                int newToOldStateDelta = newState - oldState;
                if (newToOldStateDelta < 0)
                    newToOldStateDelta += pauseTimesCount + 1;

                int oldToNewStateDelta = oldState - newState;
                if (oldToNewStateDelta < 0)
                    oldToNewStateDelta += pauseTimesCount + 1;

                // this additional check is neccessary because client doesn't check dynamic flags on progress update
                // instead it multiplies progress from dynamicflags field by -1 and then compares that against 0
                // when calculating path progress while we simply check the flag if (!_owner.HasDynamicFlag(GO_DYNFLAG_LO_INVERTED_MOVEMENT))
                bool isAtStartOfPath = _stateChangeProgress == 0;

                if (oldToNewStateDelta < newToOldStateDelta && !isAtStartOfPath)
                    _owner.SetDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
                else
                    _owner.RemoveDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
            }

            public override void OnRelocated()
            {
                UpdatePassengerPositions();
            }

            public void UpdatePassengerPositions()
            {
                foreach (WorldObject passenger in _passengers)
                {
                    passenger.m_movementInfo.transport.pos.GetPosition(out float x, out float y, out float z, out float o);
                    CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                    ITransport.UpdatePassengerPosition(this, _owner.GetMap(), passenger, x, y, z, o, true);
                }
            }

            public uint GetTransportPeriod()
            {
                if (_animationInfo != null)
                    return _animationInfo.TotalTime;

                return 1;
            }

            public List<uint> GetPauseTimes()
            {
                return _stopFrames;
            }

            public ObjectGuid GetTransportGUID() { return _owner.GetGUID(); }

            public float GetTransportOrientation() { return _owner.GetOrientation(); }

            public void AddPassenger(WorldObject passenger)
            {
                if (!_owner.IsInWorld)
                    return;

                if (!_passengers.Contains(passenger))
                {
                    _passengers.Add(passenger);
                    passenger.SetTransport(this);
                    passenger.m_movementInfo.transport.guid = GetTransportGUID();
                    Log.outDebug(LogFilter.Transport, $"Object {passenger.GetName()} boarded transport {_owner.GetName()}.");
                }
            }

            public ITransport RemovePassenger(WorldObject passenger)
            {
                if (_passengers.Remove(passenger))
                {
                    passenger.SetTransport(null);
                    passenger.m_movementInfo.transport.Reset();
                    Log.outDebug(LogFilter.Transport, $"Object {passenger.GetName()} removed from transport {_owner.GetName()}.");

                    Player plr = passenger.ToPlayer();
                    if (plr != null)
                        plr.SetFallInformation(0, plr.GetPositionZ());
                }

                return this;
            }

            public void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o)
            {
                ITransport.CalculatePassengerPosition(ref x, ref y, ref z, ref o, _owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ(), _owner.GetOrientation());
            }

            public void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o)
            {
                ITransport.CalculatePassengerOffset(ref x, ref y, ref z, ref o, _owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ(), _owner.GetOrientation());
            }

            public int GetMapIdForSpawning()
            {
                return _owner.GetGoInfo().Transport.SpawnMap;
            }

            public void SetAutoCycleBetweenStopFrames(bool on)
            {
                _autoCycleBetweenStopFrames = on;
            }
        }

        class SetTransportAutoCycleBetweenStopFrames : GameObjectTypeBase.CustomCommand
        {
            bool _on;

            public SetTransportAutoCycleBetweenStopFrames(bool on)
            {
                _on = on;
            }

            public override void Execute(GameObjectTypeBase type)
            {
                Transport transport = (Transport)type;
                if (transport != null)
                    transport.SetAutoCycleBetweenStopFrames(_on);
            }
        }

        class NewFlag : GameObjectTypeBase
        {
            FlagState _state;
            long _respawnTime;
            ObjectGuid _carrierGUID;
            long _takenFromBaseTime;

            public NewFlag(GameObject owner) : base(owner)
            {
                _state = FlagState.InBase;
            }

            public void SetState(FlagState newState, Player player)
            {
                if (_state == newState)
                    return;

                FlagState oldState = _state;
                _state = newState;

                if (player != null && newState == FlagState.Taken)
                    _carrierGUID = player.GetGUID();
                else
                    _carrierGUID = ObjectGuid.Empty;

                if (newState == FlagState.Taken && oldState == FlagState.InBase)
                    _takenFromBaseTime = GameTime.GetGameTime();
                else if (newState == FlagState.InBase || newState == FlagState.Respawning)
                    _takenFromBaseTime = 0;

                _owner.UpdateObjectVisibility();

                if (newState == FlagState.Respawning)
                    _respawnTime = GameTime.GetGameTimeMS() + _owner.GetGoInfo().NewFlag.RespawnTime;
                else
                    _respawnTime = 0;

                ZoneScript zoneScript = _owner.GetZoneScript();
                if (zoneScript != null)
                    zoneScript.OnFlagStateChange(_owner, oldState, _state, player);
            }

            public override void Update(uint diff)
            {
                if (_state == FlagState.Respawning && GameTime.GetGameTimeMS() >= _respawnTime)
                    SetState(FlagState.InBase, null);
            }

            public override bool IsNeverVisibleFor(WorldObject seer, bool allowServersideObjects)
            {
                return _state != FlagState.InBase;
            }

            public FlagState GetState() { return _state; }
            public ObjectGuid GetCarrierGUID() { return _carrierGUID; }
            public long GetTakenFromBaseTime() { return _takenFromBaseTime; }
        }

        public class SetNewFlagState : GameObjectTypeBase.CustomCommand
        {
            FlagState _state;
            Player _player;

            public SetNewFlagState(FlagState state, Player player)
            {
                _state = state;
                _player = player;
            }

            public override void Execute(GameObjectTypeBase type)
            {
                if (type is NewFlag newFlag)
                    newFlag.SetState(_state, _player);
            }
        }
    }

    public class PerPlayerState
    {
        public DateTime ValidUntil = DateTime.MinValue;
        public GameObjectState? State;
        public bool Despawned;
    }

    class ControlZone : GameObjectTypeBase
    {
        TimeSpan _heartbeatRate;
        TimeTracker _heartbeatTracker;
        List<ObjectGuid> _insidePlayers = new();
        int _previousTeamId;
        float _value;
        bool _contestedTriggered;

        public ControlZone(GameObject owner) : base(owner)
        {
            _value = owner.GetGoInfo().ControlZone.startingValue;


            if (owner.GetMap().Instanceable())
                _heartbeatRate = TimeSpan.FromSeconds(1);
            else if (owner.GetGoInfo().ControlZone.FrequentHeartbeat != 0)
                _heartbeatRate = TimeSpan.FromSeconds(2.5);
            else
                _heartbeatRate = TimeSpan.FromSeconds(5);

            _heartbeatTracker = new(_heartbeatRate);
            _previousTeamId = GetControllingTeam();
            _contestedTriggered = false;
        }

        public override void Update(uint diff)
        {
            if (_owner.HasFlag(GameObjectFlags.NotSelectable))
                return;

            _heartbeatTracker.Update(diff);
            if (_heartbeatTracker.Passed())
            {
                _heartbeatTracker.Reset(_heartbeatRate);
                HandleHeartbeat();
            }
        }

        int GetControllingTeam()
        {
            if (_value < GetMaxHordeValue())
                return BattleGroundTeamId.Horde;

            if (_value > GetMinAllianceValue())
                return BattleGroundTeamId.Alliance;

            return BattleGroundTeamId.Neutral;
        }

        public List<ObjectGuid> GetInsidePlayers() { return _insidePlayers; }

        public override void ActivateObject(GameObjectActions action, int param, WorldObject spellCaster, uint spellId, int effectIndex)
        {
            switch (action)
            {
                case GameObjectActions.MakeInert:
                    foreach (ObjectGuid guid in _insidePlayers)
                    {
                        Player player = Global.ObjAccessor.GetPlayer(_owner, guid);
                        if (player != null)
                            player.SendUpdateWorldState(_owner.GetGoInfo().ControlZone.worldState1, 0);
                    }

                    _insidePlayers.Clear();
                    break;
                default:
                    break;
            }
        }

        public void SetValue(float value)
        {
            _value = MathFunctions.RoundToInterval(ref value, 0.0f, 100.0f);
        }

        void HandleHeartbeat()
        {
            // update player list inside control zone
            List<Player> targetList = new();
            SearchTargets(targetList);

            int oldControllingTeam = GetControllingTeam();
            float pointsGained = CalculatePointsPerSecond(targetList) * (float)_heartbeatRate.TotalMilliseconds / 1000.0f;
            if (pointsGained == 0)
                return;

            int oldRoundedValue = (int)_value;
            SetValue(_value + pointsGained);
            int roundedValue = (int)_value;
            if (oldRoundedValue == roundedValue)
                return;

            int newControllingTeam = GetControllingTeam();
            int assaultingTeam = pointsGained > 0 ? BattleGroundTeamId.Alliance : BattleGroundTeamId.Horde;

            if (oldControllingTeam != newControllingTeam)
                _contestedTriggered = false;

            if (oldControllingTeam != BattleGroundTeamId.Alliance && newControllingTeam == BattleGroundTeamId.Alliance)
                TriggerEvent(_owner.GetGoInfo().ControlZone.ProgressEventAlliance);
            else if (oldControllingTeam != BattleGroundTeamId.Horde && newControllingTeam == BattleGroundTeamId.Horde)
                TriggerEvent(_owner.GetGoInfo().ControlZone.ProgressEventHorde);
            else if (oldControllingTeam == BattleGroundTeamId.Horde && newControllingTeam == BattleGroundTeamId.Neutral)
                TriggerEvent(_owner.GetGoInfo().ControlZone.NeutralEventHorde);
            else if (oldControllingTeam == BattleGroundTeamId.Alliance && newControllingTeam == BattleGroundTeamId.Neutral)
                TriggerEvent(_owner.GetGoInfo().ControlZone.NeutralEventAlliance);

            if (roundedValue == 100 && newControllingTeam == BattleGroundTeamId.Alliance && assaultingTeam == BattleGroundTeamId.Alliance)
                TriggerEvent(_owner.GetGoInfo().ControlZone.CaptureEventAlliance);
            else if (roundedValue == 0 && newControllingTeam == BattleGroundTeamId.Horde && assaultingTeam == BattleGroundTeamId.Horde)
                TriggerEvent(_owner.GetGoInfo().ControlZone.CaptureEventHorde);

            if (oldRoundedValue == 100 && assaultingTeam == BattleGroundTeamId.Horde && !_contestedTriggered)
            {
                TriggerEvent(_owner.GetGoInfo().ControlZone.ContestedEventHorde);
                _contestedTriggered = true;
            }
            else if (oldRoundedValue == 0 && assaultingTeam == BattleGroundTeamId.Alliance && !_contestedTriggered)
            {
                TriggerEvent(_owner.GetGoInfo().ControlZone.ContestedEventAlliance);
                _contestedTriggered = true;
            }

            foreach (Player player in targetList)
                player.SendUpdateWorldState(_owner.GetGoInfo().ControlZone.worldstate2, (uint)roundedValue);
        }

        void SearchTargets(List<Player> targetList)
        {
            AnyUnitInObjectRangeCheck check = new(_owner, _owner.GetGoInfo().ControlZone.radius, true);
            PlayerListSearcher searcher = new(_owner, targetList, check);
            Cell.VisitWorldObjects(_owner, searcher, _owner.GetGoInfo().ControlZone.radius);
            HandleUnitEnterExit(targetList);
        }

        float CalculatePointsPerSecond(List<Player> targetList)
        {
            int hordePlayers = 0;
            int alliancePlayers = 0;

            foreach (Player player in targetList)
            {
                if (!player.IsOutdoorPvPActive())
                    continue;

                if (player.GetTeam() == Team.Horde)
                    hordePlayers--;
                else
                    alliancePlayers++;
            }

            sbyte factionCoefficient = 0; // alliance superiority = 1; horde superiority = -1

            if (alliancePlayers > hordePlayers)
                factionCoefficient = 1;
            else if (hordePlayers > alliancePlayers)
                factionCoefficient = -1;

            float timeNeeded = CalculateTimeNeeded(hordePlayers, alliancePlayers);
            if (timeNeeded == 0.0f)
                return 0.0f;

            return 100.0f / timeNeeded * factionCoefficient;
        }

        float CalculateTimeNeeded(int hordePlayers, int alliancePlayers)
        {
            uint uncontestedTime = _owner.GetGoInfo().ControlZone.UncontestedTime;
            uint delta = (uint)Math.Abs(alliancePlayers - hordePlayers);
            uint minSuperiority = _owner.GetGoInfo().ControlZone.minSuperiority;

            if (delta < minSuperiority)
                return 0;

            // return the uncontested time if controlzone is not contested
            if (uncontestedTime != 0 && (hordePlayers == 0 || alliancePlayers == 0))
                return uncontestedTime;

            uint minTime = _owner.GetGoInfo().ControlZone.minTime;
            uint maxTime = _owner.GetGoInfo().ControlZone.maxTime;
            uint maxSuperiority = _owner.GetGoInfo().ControlZone.maxSuperiority;

            float slope = (minTime - maxTime) / MathF.Max(maxSuperiority - minSuperiority, 1);
            float intercept = maxTime - slope * minSuperiority;
            return slope * delta + intercept;
        }

        void HandleUnitEnterExit(List<Player> newTargetList)
        {
            List<ObjectGuid> exitPlayers = new(_insidePlayers);

            List<Player> enteringPlayers = new();

            foreach (Player unit in newTargetList)
            {
                if (!exitPlayers.Remove(unit.GetGUID()))
                    enteringPlayers.Add(unit);

                _insidePlayers.Add(unit.GetGUID());
            }

            foreach (Player player in enteringPlayers)
            {
                player.SendUpdateWorldState(_owner.GetGoInfo().ControlZone.worldState1, 1);
                player.SendUpdateWorldState(_owner.GetGoInfo().ControlZone.worldstate2, (uint)_value);
                player.SendUpdateWorldState(_owner.GetGoInfo().ControlZone.worldstate3, _owner.GetGoInfo().ControlZone.neutralPercent);
            }

            foreach (ObjectGuid exitPlayerGuid in exitPlayers)
            {
                Player player = Global.ObjAccessor.GetPlayer(_owner, exitPlayerGuid);
                if (player != null)
                    player.SendUpdateWorldState(_owner.GetGoInfo().ControlZone.worldState1, 0);
            }
        }

        float GetMaxHordeValue()
        {
            // ex: if neutralPercent is 40; then 0 - 30 is Horde Controlled
            return 50.0f - _owner.GetGoInfo().ControlZone.neutralPercent / 2.0f;
        }

        float GetMinAllianceValue()
        {
            // ex: if neutralPercent is 40; then 70 - 100 is Alliance Controlled
            return 50.0f + _owner.GetGoInfo().ControlZone.neutralPercent / 2.0f;
        }

        void TriggerEvent(uint eventId)
        {
            if (eventId <= 0)
                return;

            _owner.GetMap().UpdateSpawnGroupConditions();
            GameEvents.Trigger(eventId, _owner, null);
        }

        public uint GetStartingValue()
        {
            return _owner.GetGoInfo().ControlZone.startingValue;
        }
    }

    class SetControlZoneValue : GameObjectTypeBase.CustomCommand
    {
        uint? _value;

        public SetControlZoneValue(uint? value = null)
        {
            _value = value;
        }

        public override void Execute(GameObjectTypeBase type)
        {
            if (type is ControlZone controlZone)
            {
                uint value = controlZone.GetStartingValue();
                if (_value.HasValue)
                    value = _value.Value;

                controlZone.SetValue(value);
            }
        }
    }
}