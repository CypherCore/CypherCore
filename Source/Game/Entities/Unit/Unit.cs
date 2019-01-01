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
using Framework.Dynamic;
using Framework.GameMath;
using Framework.IO;
using Game.AI;
using Game.BattleGrounds;
using Game.Chat;
using Game.Combat;
using Game.DataStorage;
using Game.Maps;
using Game.Movement;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Unit : WorldObject
    {
        public Unit(bool isWorldObject) : base(isWorldObject)
        {
            moveSpline = new MoveSpline();
            i_motionMaster = new MotionMaster(this);
            threatManager = new ThreatManager(this);
            m_unitTypeMask = UnitTypeMask.None;
            m_HostileRefManager = new HostileRefManager(this);
            _spellHistory = new SpellHistory(this);
            m_FollowingRefManager = new RefManager<Unit, TargetedMovementGeneratorBase>();

            objectTypeId = TypeId.Unit;
            objectTypeMask |= TypeMask.Unit;
            m_updateFlag.MovementUpdate = true;

            m_modAttackSpeedPct = new float[] { 1.0f, 1.0f, 1.0f };
            m_deathState = DeathState.Alive;

            for (byte i = 0; i < (int)SpellImmunity.Max; ++i)
                m_spellImmune[i] = new MultiMap<uint, uint>();

            for (byte i = 0; i < (int)UnitMods.End; ++i)
                m_auraModifiersGroup[i] = new float[] { 0.0f, 100.0f, 1.0f, 0.0f, 1.0f };

            m_auraModifiersGroup[(int)UnitMods.DamageOffHand][(int)UnitModifierType.TotalPCT] = 0.5f;

            foreach (AuraType auraType in Enum.GetValues(typeof(AuraType)))
                m_modAuras[auraType] = new List<AuraEffect>();

            for (byte i = 0; i < (int)WeaponAttackType.Max; ++i)
                m_weaponDamage[i] = new float[] { 1.0f, 2.0f };

            if (IsTypeId(TypeId.Player))
            {
                m_modMeleeHitChance = 7.5f;
                m_modRangedHitChance = 7.5f;
                m_modSpellHitChance = 15.0f;
            }
            m_baseSpellCritChance = 5;

            for (byte i = 0; i < (int)SpellSchools.Max; ++i)
                m_threatModifier[i] = 1.0f;

            for (byte i = 0; i < (int)UnitMoveType.Max; ++i)
                m_speed_rate[i] = 1.0f;

            _redirectThreatInfo = new RedirectThreatInfo();
            m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);

            movesplineTimer = new TimeTrackerSmall();
        }

        public override void Dispose()
        {
            // set current spells as deletable
            for (CurrentSpellTypes i = 0; i < CurrentSpellTypes.Max; ++i)
            {
                if (m_currentSpells.ContainsKey(i))
                {
                    if (m_currentSpells[i] != null)
                    {
                        m_currentSpells[i].SetReferencedFromCurrent(false);
                        m_currentSpells[i] = null;
                    }
                }
            }

            m_Events.KillAllEvents(true);

            _DeleteRemovedAuras();

            //i_motionMaster = null;
            m_charmInfo = null;
            moveSpline = null;
            _spellHistory = null;

            /*ASSERT(!m_duringRemoveFromWorld);
            ASSERT(!m_attacking);
            ASSERT(m_attackers.empty());
            ASSERT(m_sharedVision.empty());
            ASSERT(m_Controlled.empty());
            ASSERT(m_appliedAuras.empty());
            ASSERT(m_ownedAuras.empty());
            ASSERT(m_removedAuras.empty());
            ASSERT(m_gameObj.empty());
            ASSERT(m_dynObj.empty());*/

            base.Dispose();
        }

        public override void Update(uint diff)
        {
            // WARNING! Order of execution here is important, do not change.
            // Spells must be processed with event system BEFORE they go to _UpdateSpells.
            m_Events.Update(diff);

            if (!IsInWorld)
                return;

            _UpdateSpells(diff);

            // If this is set during update SetCantProc(false) call is missing somewhere in the code
            // Having this would prevent spells from being proced, so let's crash
            Cypher.Assert(m_procDeep == 0);

            if (CanHaveThreatList() && GetThreatManager().isNeedUpdateToClient(diff))
                SendThreatListUpdate();

            // update combat timer only for players and pets (only pets with PetAI)
            if (IsInCombat() && (IsTypeId(TypeId.Player) || (IsPet() && IsControlledByPlayer())))
            {
                // Check UNIT_STATE_MELEE_ATTACKING or UNIT_STATE_CHASE (without UNIT_STATE_FOLLOW in this case) so pets can reach far away
                // targets without stopping half way there and running off.
                // These flags are reset after target dies or another command is given.
                if (m_HostileRefManager.IsEmpty())
                {
                    // m_CombatTimer set at aura start and it will be freeze until aura removing
                    if (m_CombatTimer <= diff)
                        ClearInCombat();
                    else
                        m_CombatTimer -= diff;
                }
            }

            uint att;
            // not implemented before 3.0.2
            if ((att = getAttackTimer(WeaponAttackType.BaseAttack)) != 0)
                setAttackTimer(WeaponAttackType.BaseAttack, (diff >= att ? 0 : att - diff));
            if ((att = getAttackTimer(WeaponAttackType.RangedAttack)) != 0)
                setAttackTimer(WeaponAttackType.RangedAttack, (diff >= att ? 0 : att - diff));
            if ((att = getAttackTimer(WeaponAttackType.OffAttack)) != 0)
                setAttackTimer(WeaponAttackType.OffAttack, (diff >= att ? 0 : att - diff));

            // update abilities available only for fraction of time
            UpdateReactives(diff);

            if (IsAlive())
            {
                ModifyAuraState(AuraStateType.HealthLess20Percent, HealthBelowPct(20));
                ModifyAuraState(AuraStateType.HealthLess35Percent, HealthBelowPct(35));
                ModifyAuraState(AuraStateType.HealthAbove75Percent, HealthAbovePct(75));
            }

            UpdateSplineMovement(diff);
            GetMotionMaster().UpdateMotion(diff);
            UpdateUnderwaterState(GetMap(), GetPositionX(), GetPositionY(), GetPositionZ());
        }
        void _UpdateSpells(uint diff)
        {
            if (GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null)
                _UpdateAutoRepeatSpell();

            for (CurrentSpellTypes i = 0; i < CurrentSpellTypes.Max; ++i)
            {
                if (GetCurrentSpell(i) != null && m_currentSpells[i].getState() == SpellState.Finished)
                {
                    m_currentSpells[i].SetReferencedFromCurrent(false);
                    m_currentSpells[i] = null;
                }
            }

            foreach (var app in GetOwnedAuras())
            {
                Aura i_aura = app.Value;
                if (i_aura == null)
                    continue;

                i_aura.UpdateOwner(diff, this);
            }

            // remove expired auras - do that after updates(used in scripts?)
            foreach (var pair in GetOwnedAuras())
            {
                if (pair.Value != null && pair.Value.IsExpired())
                    RemoveOwnedAura(pair, AuraRemoveMode.Expire);
            }

            foreach (var aura in m_visibleAurasToUpdate)
                aura.ClientUpdate();

            m_visibleAurasToUpdate.Clear();

            _DeleteRemovedAuras();

            if (!m_gameObj.Empty())
            {
                for (var i = 0; i < m_gameObj.Count; ++i)
                {
                    GameObject go = m_gameObj[i];
                    if (!go.isSpawned())
                    {
                        go.SetOwnerGUID(ObjectGuid.Empty);
                        go.SetRespawnTime(0);
                        go.Delete();
                        m_gameObj.Remove(go);
                    }
                }
            }

            _spellHistory.Update();
        }

        public void HandleEmoteCommand(Emote anim_id)
        {
            EmoteMessage packet = new EmoteMessage();
            packet.Guid = GetGUID();
            packet.EmoteID = (int)anim_id;
            SendMessageToSet(packet, true);
        }
        public void SendDurabilityLoss(Player receiver, uint percent)
        {
            DurabilityDamageDeath packet = new DurabilityDamageDeath();
            packet.Percent = percent;
            receiver.SendPacket(packet);
        }

        public bool IsInDisallowedMountForm()
        {
            return IsDisallowedMountForm(getTransForm(), GetShapeshiftForm(), GetDisplayId());
        }

        public bool IsDisallowedMountForm(uint spellId, ShapeShiftForm form, uint displayId)
        {
            SpellInfo transformSpellInfo = Global.SpellMgr.GetSpellInfo(getTransForm());
            if (transformSpellInfo != null)
                if (transformSpellInfo.HasAttribute(SpellAttr0.CastableWhileMounted))
                    return false;

            if (form != 0)
            {
                SpellShapeshiftFormRecord shapeshift = CliDB.SpellShapeshiftFormStorage.LookupByKey(form);
                if (shapeshift == null)
                    return true;

                if (!shapeshift.Flags.HasAnyFlag(SpellShapeshiftFormFlags.IsNotAShapeshift))
                    return true;
            }
            if (displayId == GetNativeDisplayId())
                return false;

            CreatureDisplayInfoRecord display = CliDB.CreatureDisplayInfoStorage.LookupByKey(displayId);
            if (display == null)
                return true;

            CreatureDisplayInfoExtraRecord displayExtra = CliDB.CreatureDisplayInfoExtraStorage.LookupByKey(display.ExtendedDisplayInfoID);
            if (displayExtra == null)
                return true;

            CreatureModelDataRecord model = CliDB.CreatureModelDataStorage.LookupByKey(display.ModelID);
            ChrRacesRecord race = CliDB.ChrRacesStorage.LookupByKey(displayExtra.DisplayRaceID);

            if (model != null && !Convert.ToBoolean(model.Flags & 0x80))
                if (race != null && !Convert.ToBoolean(race.Flags & 0x4))
                    return true;

            return false;
        }

        public void SendClearTarget()
        {
            BreakTarget breakTarget = new BreakTarget();
            breakTarget.UnitGUID = GetGUID();
            SendMessageToSet(breakTarget, false);
        }
        public virtual bool IsLoading() { return false; }
        public bool IsDuringRemoveFromWorld() { return m_duringRemoveFromWorld; }

        //SharedVision
        public bool HasSharedVision() { return !m_sharedVision.Empty(); }
        public List<Player> GetSharedVisionList() { return m_sharedVision; }

        public void AddPlayerToVision(Player player)
        {
            if (m_sharedVision.Empty())
            {
                setActive(true);
                SetWorldObject(true);
            }
            m_sharedVision.Add(player);
        }

        // only called in Player.SetSeer
        public void RemovePlayerFromVision(Player player)
        {
            m_sharedVision.Remove(player);
            if (m_sharedVision.Empty())
            {
                setActive(false);
                SetWorldObject(false);
            }
        }

        public virtual void Talk(string text, ChatMsg msgType, Language language, float textRange, WorldObject target)
        {
            var builder = new CustomChatTextBuilder(this, msgType, text, language, target);
            var localizer = new LocalizedPacketDo(builder);
            var worker = new PlayerDistWorker(this, textRange, localizer);
            Cell.VisitWorldObjects(this, worker, textRange);
        }

        public virtual void Say(string text, Language language, WorldObject target = null)
        {
            Talk(text, ChatMsg.MonsterSay, language, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
        }

        public virtual void Yell(string text, Language language, WorldObject target = null)
        {
            Talk(text, ChatMsg.MonsterYell, language, WorldConfig.GetFloatValue(WorldCfg.ListenRangeYell), target);
        }

        public virtual void TextEmote(string text, WorldObject target = null, bool isBossEmote = false)
        {
            Talk(text, isBossEmote ? ChatMsg.RaidBossEmote : ChatMsg.MonsterEmote, Language.Universal, WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote), target);
        }

        public virtual void Whisper(string text, Language language, Player target, bool isBossWhisper = false)
        {
            if (!target)
                return;

            LocaleConstant locale = target.GetSession().GetSessionDbLocaleIndex();
            ChatPkt data = new ChatPkt();
            data.Initialize(isBossWhisper ? ChatMsg.RaidBossWhisper : ChatMsg.MonsterWhisper, Language.Universal, this, target, text, 0, "", locale);
            target.SendPacket(data);
        }

        public void Talk(uint textId, ChatMsg msgType, float textRange, WorldObject target)
        {
            if (!CliDB.BroadcastTextStorage.ContainsKey(textId))
            {
                Log.outError(LogFilter.Unit, "Unit.Talk: `broadcast_text` (Id: {0}) was not found", textId);
                return;
            }

            var builder = new BroadcastTextBuilder(this, msgType, textId, GetGender(), target);
            var localizer = new LocalizedPacketDo(builder);
            var worker = new PlayerDistWorker(this, textRange, localizer);
            Cell.VisitWorldObjects(this, worker, textRange);
        }

        public virtual void Say(uint textId, WorldObject target = null)
        {
            Talk(textId, ChatMsg.MonsterSay, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
        }

        public virtual void Yell(uint textId, WorldObject target = null)
        {
            Talk(textId, ChatMsg.MonsterYell, WorldConfig.GetFloatValue(WorldCfg.ListenRangeYell), target);
        }

        public virtual void TextEmote(uint textId, WorldObject target = null, bool isBossEmote = false)
        {
            Talk(textId, isBossEmote ? ChatMsg.RaidBossEmote : ChatMsg.MonsterEmote, WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote), target);
        }

        public virtual void Whisper(uint textId, Player target, bool isBossWhisper = false)
        {
            if (!target)
                return;

            BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(textId);
            if (bct == null)
            {
                Log.outError(LogFilter.Unit, "Unit.Whisper: `broadcast_text` was not {0} found", textId);
                return;
            }

            LocaleConstant locale = target.GetSession().GetSessionDbLocaleIndex();
            ChatPkt data = new ChatPkt();
            data.Initialize(isBossWhisper ? ChatMsg.RaidBossWhisper : ChatMsg.MonsterWhisper, Language.Universal, this, target, Global.DB2Mgr.GetBroadcastTextValue(bct, locale, GetGender()), 0, "", locale);
            target.SendPacket(data);
        }

        public override void UpdateObjectVisibility(bool forced = true)
        {
            if (!forced)
                AddToNotify(NotifyFlags.VisibilityChanged);
            else
            {
                base.UpdateObjectVisibility(true);
                // call MoveInLineOfSight for nearby creatures
                AIRelocationNotifier notifier = new AIRelocationNotifier(this);
                Cell.VisitAllObjects(this, notifier, GetVisibilityRange());
            }
        }

        public override void RemoveFromWorld()
        {
            // cleanup

            if (IsInWorld)
            {
                m_duringRemoveFromWorld = true;
                if (IsVehicle())
                    RemoveVehicleKit(true);

                RemoveCharmAuras();
                RemoveAurasByType(AuraType.BindSight);
                RemoveNotOwnSingleTargetAuras();

                RemoveAllGameObjects();
                RemoveAllDynObjects();
                RemoveAllAreaTriggers();

                ExitVehicle();  // Remove applied auras with SPELL_AURA_CONTROL_VEHICLE
                UnsummonAllTotems();
                RemoveAllControlled();

                RemoveAreaAurasDueToLeaveWorld();

                if (!GetCharmerGUID().IsEmpty())
                {
                    Log.outFatal(LogFilter.Unit, "Unit {0} has charmer guid when removed from world", GetEntry());
                }
                Unit owner = GetOwner();
                if (owner != null)
                {
                    if (owner.m_Controlled.Contains(this))
                    {
                        Log.outFatal(LogFilter.Unit, "Unit {0} is in controlled list of {1} when removed from world", GetEntry(), owner.GetEntry());
                    }
                }

                base.RemoveFromWorld();
                m_duringRemoveFromWorld = false;
            }
        }

        public void CleanupBeforeRemoveFromMap(bool finalCleanup)
        {
            // This needs to be before RemoveFromWorld to make GetCaster() return a valid for aura removal
            InterruptNonMeleeSpells(true);

            if (IsInWorld)
                RemoveFromWorld();

            // A unit may be in removelist and not in world, but it is still in grid
            // and may have some references during delete
            RemoveAllAuras();
            RemoveAllGameObjects();

            if (finalCleanup)
                m_cleanupDone = true;

            m_Events.KillAllEvents(false);                      // non-delatable (currently casted spells) will not deleted now but it will deleted at call in Map.RemoveAllObjectsInRemoveList
            CombatStop();
            DeleteThreatList();
            getHostileRefManager().setOnlineOfflineState(false);
            GetMotionMaster().Clear(false);                    // remove different non-standard movement generators.
        }
        public override void CleanupsBeforeDelete(bool finalCleanup = true)
        {
            CleanupBeforeRemoveFromMap(finalCleanup);

            base.CleanupsBeforeDelete(finalCleanup);
        }

        public void setTransForm(uint spellid) { m_transform = spellid; }
        public uint GetTransForm() { return m_transform; }

        public Vehicle GetVehicleKit() { return m_vehicleKit; }
        public Vehicle GetVehicle() { return m_vehicle; }
        public void SetVehicle(Vehicle vehicle) { m_vehicle = vehicle; }
        public Unit GetVehicleBase()
        {
            return m_vehicle != null ? m_vehicle.GetBase() : null;
        }
        public Creature GetVehicleCreatureBase()
        {
            Unit veh = GetVehicleBase();
            if (veh != null)
            {
                Creature c = veh.ToCreature();
                if (c != null)
                    return c;
            }
            return null;
        }
        public ITransport GetDirectTransport()
        {
            Vehicle veh = GetVehicle();
            if (veh != null)
                return veh;
            return GetTransport();
        }

        public void _RegisterDynObject(DynamicObject dynObj)
        {
            m_dynObj.Add(dynObj);
            if (IsTypeId(TypeId.Unit) && IsAIEnabled)
                ToCreature().GetAI().JustRegisteredDynObject(dynObj);
        }

        public void _UnregisterDynObject(DynamicObject dynObj)
        {
            m_dynObj.Remove(dynObj);
            if (IsTypeId(TypeId.Unit) && IsAIEnabled)
                ToCreature().GetAI().JustUnregisteredDynObject(dynObj);
        }

        public DynamicObject GetDynObject(uint spellId)
        {
            return GetDynObjects(spellId).FirstOrDefault();
        }

        List<DynamicObject> GetDynObjects(uint spellId)
        {
            List<DynamicObject> dynamicobjects = new List<DynamicObject>();
            foreach (var obj in m_dynObj)
                if (obj.GetSpellId() == spellId)
                    dynamicobjects.Add(obj);

            return dynamicobjects;
        }

        public void RemoveDynObject(uint spellId)
        {
            for (var i = 0; i < m_dynObj.Count; ++i)
            {
                var dynObj = m_dynObj[i];
                if (dynObj.GetSpellId() == spellId)
                    dynObj.Remove();
            }
        }

        public void RemoveAllDynObjects()
        {
            while (!m_dynObj.Empty())
                m_dynObj.First().Remove();
        }

        public GameObject GetGameObject(uint spellId)
        {
            return GetGameObjects(spellId).FirstOrDefault();
        }

        List<GameObject> GetGameObjects(uint spellId)
        {
            List<GameObject> gameobjects = new List<GameObject>();
            foreach (var obj in m_gameObj)
                if (obj.GetSpellId() == spellId)
                    gameobjects.Add(obj);

            return gameobjects;
        }

        public void AddGameObject(GameObject gameObj)
        {
            if (gameObj == null || !gameObj.GetOwnerGUID().IsEmpty())
                return;

            m_gameObj.Add(gameObj);
            gameObj.SetOwnerGUID(GetGUID());

            if (gameObj.GetSpellId() != 0)
            {
                SpellInfo createBySpell = Global.SpellMgr.GetSpellInfo(gameObj.GetSpellId());
                // Need disable spell use for owner
                if (createBySpell != null && createBySpell.HasAttribute(SpellAttr0.DisabledWhileActive))
                    // note: item based cooldowns and cooldown spell mods with charges ignored (unknown existing cases)
                    GetSpellHistory().StartCooldown(createBySpell, 0, null, true);
            }

            if (IsTypeId(TypeId.Unit) && ToCreature().IsAIEnabled)
                ToCreature().GetAI().JustSummonedGameobject(gameObj);
        }

        public void RemoveGameObject(GameObject gameObj, bool del)
        {
            if (gameObj == null || gameObj.GetOwnerGUID() != GetGUID())
                return;

            gameObj.SetOwnerGUID(ObjectGuid.Empty);

            for (byte i = 0; i < SharedConst.MaxGameObjectSlot; ++i)
            {
                if (m_ObjectSlot[i] == gameObj.GetGUID())
                {
                    m_ObjectSlot[i].Clear();
                    break;
                }
            }

            // GO created by some spell
            uint spellid = gameObj.GetSpellId();
            if (spellid != 0)
            {
                RemoveAurasDueToSpell(spellid);

                SpellInfo createBySpell = Global.SpellMgr.GetSpellInfo(spellid);
                // Need activate spell use for owner
                if (createBySpell != null && createBySpell.IsCooldownStartedOnEvent())
                    // note: item based cooldowns and cooldown spell mods with charges ignored (unknown existing cases)
                    GetSpellHistory().SendCooldownEvent(createBySpell);
            }

            m_gameObj.Remove(gameObj);

            if (IsTypeId(TypeId.Unit) && ToCreature().IsAIEnabled)
                ToCreature().GetAI().SummonedGameobjectDespawn(gameObj);

            if (del)
            {
                gameObj.SetRespawnTime(0);
                gameObj.Delete();
            }
        }

        public void RemoveGameObject(uint spellid, bool del)
        {
            if (m_gameObj.Empty())
                return;

            foreach (var obj in m_gameObj)
            {
                if (spellid == 0 || obj.GetSpellId() == spellid)
                {
                    obj.SetOwnerGUID(ObjectGuid.Empty);
                    if (del)
                    {
                        obj.SetRespawnTime(0);
                        obj.Delete();
                    }
                }
            }
        }

        public void RemoveAllGameObjects()
        {
            // remove references to unit
            while (!m_gameObj.Empty())
            {
                var obj = m_gameObj.First();
                obj.SetOwnerGUID(ObjectGuid.Empty);
                obj.SetRespawnTime(0);
                obj.Delete();
                m_gameObj.Remove(obj);
            }
        }

        public void _RegisterAreaTrigger(AreaTrigger areaTrigger)
        {
            m_areaTrigger.Add(areaTrigger);
            if (IsTypeId(TypeId.Unit) && IsAIEnabled)
                ToCreature().GetAI().JustRegisteredAreaTrigger(areaTrigger);
        }

        public void _UnregisterAreaTrigger(AreaTrigger areaTrigger)
        {
            m_areaTrigger.Remove(areaTrigger);
            if (IsTypeId(TypeId.Unit) && IsAIEnabled)
                ToCreature().GetAI().JustUnregisteredAreaTrigger(areaTrigger);
        }

        AreaTrigger GetAreaTrigger(uint spellId)
        {
            List<AreaTrigger> areaTriggers = GetAreaTriggers(spellId);
            return areaTriggers.Empty() ? null : areaTriggers[0];
        }

        public List<AreaTrigger> GetAreaTriggers(uint spellId)
        {
            return m_areaTrigger.Where(trigger => trigger.GetSpellId() == spellId).ToList();
        }

        public void RemoveAreaTrigger(uint spellId)
        {
            if (m_areaTrigger.Empty())
                return;

            for (var i = 0; i < m_areaTrigger.Count; ++i)
            {
                AreaTrigger areaTrigger = m_areaTrigger[i];
                if (areaTrigger.GetSpellId() == spellId)
                    areaTrigger.Remove();
            }
        }

        public void RemoveAreaTrigger(AuraEffect aurEff)
        {
            if (m_areaTrigger.Empty())
                return;

            foreach (AreaTrigger areaTrigger in m_areaTrigger)
            {
                if (areaTrigger.GetAuraEffect() == aurEff)
                {
                    areaTrigger.Remove();
                    break; // There can only be one AreaTrigger per AuraEffect
                }
            }
        }

        public void RemoveAllAreaTriggers()
        {
            while (!m_areaTrigger.Empty())
                m_areaTrigger[0].Remove();
        }

        public bool IsVendor() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.Vendor); }
        public bool IsTrainer() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.Trainer); }
        public bool IsQuestGiver() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.QuestGiver); }
        public bool IsGossip() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.Gossip); }
        public bool IsTaxi() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.FlightMaster); }
        public bool IsGuildMaster() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.Petitioner); }
        public bool IsBattleMaster() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.BattleMaster); }
        public bool IsBanker() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.Banker); }
        public bool IsInnkeeper() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.Innkeeper); }
        public bool IsSpiritHealer() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.SpiritHealer); }
        public bool IsSpiritGuide() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.SpiritGuide); }
        public bool IsTabardDesigner() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.TabardDesigner); }
        public bool IsAuctioner() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.Auctioneer); }
        public bool IsArmorer() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.Repair); }
        public bool IsServiceProvider()
        {
            return HasFlag64(UnitFields.NpcFlags,
                NPCFlags.Vendor | NPCFlags.Trainer | NPCFlags.FlightMaster |
                NPCFlags.Petitioner | NPCFlags.BattleMaster | NPCFlags.Banker |
                NPCFlags.Innkeeper | NPCFlags.SpiritHealer |
                NPCFlags.SpiritGuide | NPCFlags.TabardDesigner | NPCFlags.Auctioneer);
        }
        public bool IsSpiritService() { return HasFlag64(UnitFields.NpcFlags, NPCFlags.SpiritHealer | NPCFlags.SpiritGuide); }
        public bool IsCritter() { return GetCreatureType() == CreatureType.Critter; }
        public bool IsInFlight() { return HasUnitState(UnitState.InFlight); }

        public Guardian GetGuardianPet()
        {
            ObjectGuid pet_guid = GetPetGUID();
            if (!pet_guid.IsEmpty())
            {
                Creature pet = ObjectAccessor.GetCreatureOrPetOrVehicle(this, pet_guid);
                if (pet != null)
                    if (pet.HasUnitTypeMask(UnitTypeMask.Guardian))
                        return (Guardian)pet;

                Log.outFatal(LogFilter.Unit, "Unit:GetGuardianPet: Guardian {0} not exist.", pet_guid);
                SetPetGUID(ObjectGuid.Empty);
            }

            return null;
        }

        public Unit SelectNearbyTarget(Unit exclude = null, float dist = SharedConst.NominalMeleeRange)
        {
            List<Unit> targets = new List<Unit>();
            var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(this, this, dist);
            var searcher = new UnitListSearcher(this, targets, u_check);
            Cell.VisitAllObjects(this, searcher, dist);

            // remove current target
            if (GetVictim())
                targets.Remove(GetVictim());

            if (exclude)
                targets.Remove(exclude);

            // remove not LoS targets
            foreach (var unit in targets)
            {
                if (!IsWithinLOSInMap(unit) || unit.IsTotem() || unit.IsSpiritService() || unit.IsCritter())
                    targets.Remove(unit);
            }

            // no appropriate targets
            if (targets.Empty())
                return null;

            // select random
            return targets.SelectRandom();
        }

        public void EnterVehicle(Unit Base, sbyte seatId = -1)
        {
            CastCustomSpell(SharedConst.VehicleSpellRideHardcoded, SpellValueMod.BasePoint0, seatId + 1, Base, TriggerCastFlags.IgnoreCasterMountedOrOnVehicle);
        }

        public void _EnterVehicle(Vehicle vehicle, sbyte seatId, AuraApplication aurApp)
        {
            // Must be called only from aura handler
            Cypher.Assert(aurApp != null);

            if (!IsAlive() || GetVehicleKit() == vehicle || vehicle.GetBase().IsOnVehicle(this))
                return;

            if (m_vehicle != null)
            {
                if (m_vehicle != vehicle)
                {
                    Log.outDebug(LogFilter.Vehicle, "EnterVehicle: {0} exit {1} and enter {2}.", GetEntry(), m_vehicle.GetBase().GetEntry(), vehicle.GetBase().GetEntry());
                    ExitVehicle();
                }
                else if (seatId >= 0 && seatId == GetTransSeat())
                    return;
            }

            if (aurApp.HasRemoveMode())
                return;

            Player player = ToPlayer();
            if (player != null)
            {
                if (vehicle.GetBase().IsTypeId(TypeId.Player) && player.IsInCombat())
                {
                    vehicle.GetBase().RemoveAura(aurApp);
                    return;
                }
            }

            Cypher.Assert(!m_vehicle);
            vehicle.AddPassenger(this, seatId);
        }

        public void ChangeSeat(sbyte seatId, bool next = true)
        {
            if (m_vehicle == null)
                return;

            // Don't change if current and new seat are identical
            if (seatId == GetTransSeat())
                return;

            var seat = (seatId < 0 ? m_vehicle.GetNextEmptySeat(GetTransSeat(), next) : m_vehicle.Seats.LookupByKey(seatId));
            // The second part of the check will only return true if seatId >= 0. @Vehicle.GetNextEmptySeat makes sure of that.
            if (seat == null || !seat.IsEmpty())
                return;

            AuraEffect rideVehicleEffect = null;
            var vehicleAuras = m_vehicle.GetBase().GetAuraEffectsByType(AuraType.ControlVehicle);
            foreach (var eff in vehicleAuras)
            {
                if (eff.GetCasterGUID() != GetGUID())
                    continue;

                // Make sure there is only one ride vehicle aura on target cast by the unit changing seat
                Cypher.Assert(rideVehicleEffect == null);
                rideVehicleEffect = eff;
            }

            // Unit riding a vehicle must always have control vehicle aura on target
            Cypher.Assert(rideVehicleEffect != null);

            rideVehicleEffect.ChangeAmount((seatId < 0 ? GetTransSeat() : seatId) + 1);
        }

        public void ExitVehicle(Position exitPosition = null)
        {
            //! This function can be called at upper level code to initialize an exit from the passenger's side.
            if (m_vehicle == null)
                return;

            GetVehicleBase().RemoveAurasByType(AuraType.ControlVehicle, GetGUID());
            //! The following call would not even be executed successfully as the
            //! SPELL_AURA_CONTROL_VEHICLE unapply handler already calls _ExitVehicle without
            //! specifying an exitposition. The subsequent call below would return on if (!m_vehicle).

            //! To do:
            //! We need to allow SPELL_AURA_CONTROL_VEHICLE unapply handlers in spellscripts
            //! to specify exit coordinates and either store those per passenger, or we need to
            //! init spline movement based on those coordinates in unapply handlers, and
            //! relocate exiting passengers based on Unit.moveSpline data. Either way,
            //! Coming Soon(TM)
        }

        public void _ExitVehicle(Position exitPosition = null)
        {
            // It's possible m_vehicle is NULL, when this function is called indirectly from @VehicleJoinEvent.Abort.
            // In that case it was not possible to add the passenger to the vehicle. The vehicle aura has already been removed
            // from the target in the aforementioned function and we don't need to do anything else at this point.
            if (m_vehicle == null)
                return;

            // This should be done before dismiss, because there may be some aura removal
            Vehicle vehicle = m_vehicle.RemovePassenger(this);

            Player player = ToPlayer();

            // If the player is on mounted duel and exits the mount, he should immediatly lose the duel
            if (player && player.duel != null && player.duel.isMounted)
                player.DuelComplete(DuelCompleteType.Fled);

            SetControlled(false, UnitState.Root);      // SMSG_MOVE_FORCE_UNROOT, ~MOVEMENTFLAG_ROOT

            Position pos;
            if (exitPosition == null)                          // Exit position not specified
                pos = vehicle.GetBase().GetPosition();  // This should use passenger's current position, leaving it as it is now
            // because we calculate positions incorrect (sometimes under map)
            else
                pos = exitPosition;

            AddUnitState(UnitState.Move);

            if (player != null)
                player.SetFallInformation(0, GetPositionZ());

            float height = pos.GetPositionZ();

            MoveSplineInit init = new MoveSplineInit(this);

            // Creatures without inhabit type air should begin falling after exiting the vehicle
            if (IsTypeId(TypeId.Unit) && !ToCreature().CanFly() && height > GetMap().GetWaterOrGroundLevel(GetPhaseShift(), pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), ref height) + 0.1f)
                init.SetFall();

            init.MoveTo(pos.GetPositionX(), pos.GetPositionY(), height, false);
            init.SetFacing(GetOrientation());
            init.SetTransportExit();
            init.Launch();

            if (player != null)
                player.ResummonPetTemporaryUnSummonedIfAny();

            if (vehicle.GetBase().HasUnitTypeMask(UnitTypeMask.Minion) && vehicle.GetBase().IsTypeId(TypeId.Unit))
                if (((Minion)vehicle.GetBase()).GetOwner() == this)
                    vehicle.GetBase().ToCreature().DespawnOrUnsummon();

            if (HasUnitTypeMask(UnitTypeMask.Accessory))
            {
                // Vehicle just died, we die too
                if (vehicle.GetBase().getDeathState() == DeathState.JustDied)
                    setDeathState(DeathState.JustDied);
                // If for other reason we as minion are exiting the vehicle (ejected, master dismounted) - unsummon
                else
                    ToTempSummon().UnSummon(2000); // Approximation
            }
        }

        void SendCancelOrphanSpellVisual(uint id)
        {
            CancelOrphanSpellVisual cancelOrphanSpellVisual = new CancelOrphanSpellVisual();
            cancelOrphanSpellVisual.SpellVisualID = id;
            SendMessageToSet(cancelOrphanSpellVisual, true);
        }

        void SendPlayOrphanSpellVisual(ObjectGuid target, uint spellVisualId, float travelSpeed, bool speedAsTime = false, bool withSourceOrientation = false)
        {
            PlayOrphanSpellVisual playOrphanSpellVisual = new PlayOrphanSpellVisual();
            playOrphanSpellVisual.SourceLocation = GetPosition();
            if (withSourceOrientation)
                playOrphanSpellVisual.SourceRotation = new Vector3(0.0f, 0.0f, GetOrientation());
            playOrphanSpellVisual.Target = target; // exclusive with TargetLocation
            playOrphanSpellVisual.SpellVisualID = spellVisualId;
            playOrphanSpellVisual.TravelSpeed = travelSpeed;
            playOrphanSpellVisual.SpeedAsTime = speedAsTime;
            playOrphanSpellVisual.UnkZero = 0.0f;
            SendMessageToSet(playOrphanSpellVisual, true);
        }

        void SendPlayOrphanSpellVisual(Vector3 targetLocation, uint spellVisualId, float travelSpeed, bool speedAsTime = false, bool withSourceOrientation = false)
        {
            PlayOrphanSpellVisual playOrphanSpellVisual = new PlayOrphanSpellVisual();
            playOrphanSpellVisual.SourceLocation = GetPosition();
            if (withSourceOrientation)
                playOrphanSpellVisual.SourceRotation = new Vector3(0.0f, 0.0f, GetOrientation());
            playOrphanSpellVisual.TargetLocation = targetLocation; // exclusive with Target
            playOrphanSpellVisual.SpellVisualID = spellVisualId;
            playOrphanSpellVisual.TravelSpeed = travelSpeed;
            playOrphanSpellVisual.SpeedAsTime = speedAsTime;
            playOrphanSpellVisual.UnkZero = 0.0f;
            SendMessageToSet(playOrphanSpellVisual, true);
        }

        void SendCancelSpellVisual(uint id)
        {
            CancelSpellVisual cancelSpellVisual = new CancelSpellVisual();
            cancelSpellVisual.Source = GetGUID();
            cancelSpellVisual.SpellVisualID = id;
            SendMessageToSet(cancelSpellVisual, true);
        }

        public void SendPlaySpellVisual(ObjectGuid targetGuid, uint spellVisualId, uint missReason, uint reflectStatus, float travelSpeed, bool speedAsTime = false)
        {
            PlaySpellVisual playSpellVisual = new PlaySpellVisual();
            playSpellVisual.Source = GetGUID();
            playSpellVisual.Target = targetGuid; // exclusive with TargetPosition
            playSpellVisual.SpellVisualID = spellVisualId;
            playSpellVisual.TravelSpeed = travelSpeed;
            playSpellVisual.MissReason = (ushort)missReason;
            playSpellVisual.ReflectStatus = (ushort)reflectStatus;
            playSpellVisual.SpeedAsTime = speedAsTime;
            SendMessageToSet(playSpellVisual, true);
        }

        public void SendPlaySpellVisual(Vector3 targetPosition, float o, uint spellVisualId, uint missReason, uint reflectStatus, float travelSpeed, bool speedAsTime = false)
        {
            PlaySpellVisual playSpellVisual = new PlaySpellVisual();
            playSpellVisual.Source = GetGUID();
            playSpellVisual.TargetPosition = targetPosition; // exclusive with Target
            playSpellVisual.Orientation = o;
            playSpellVisual.SpellVisualID = spellVisualId;
            playSpellVisual.TravelSpeed = travelSpeed;
            playSpellVisual.MissReason = (ushort)missReason;
            playSpellVisual.ReflectStatus = (ushort)reflectStatus;
            playSpellVisual.SpeedAsTime = speedAsTime;
            SendMessageToSet(playSpellVisual, true);
        }

        void SendCancelSpellVisualKit(uint id)
        {
            CancelSpellVisualKit cancelSpellVisualKit = new CancelSpellVisualKit();
            cancelSpellVisualKit.Source = GetGUID();
            cancelSpellVisualKit.SpellVisualKitID = id;
            SendMessageToSet(cancelSpellVisualKit, true);
        }

        public void SendPlaySpellVisualKit(uint id, uint type, uint duration)
        {
            PlaySpellVisualKit playSpellVisualKit = new PlaySpellVisualKit();
            playSpellVisualKit.Unit = GetGUID();
            playSpellVisualKit.KitRecID = id;
            playSpellVisualKit.KitType = type;
            playSpellVisualKit.Duration = duration;
            SendMessageToSet(playSpellVisualKit, true);
        }

        public void UnsummonAllTotems()
        {
            for (byte i = 0; i < SharedConst.MaxSummonSlot; ++i)
            {
                if (m_SummonSlot[i].IsEmpty())
                    continue;

                Creature OldTotem = GetMap().GetCreature(m_SummonSlot[i]);
                if (OldTotem != null)
                    if (OldTotem.IsSummon())
                        OldTotem.ToTempSummon().UnSummon();
            }
        }

        public bool IsOnVehicle(Unit vehicle)
        {
            return m_vehicle != null && m_vehicle == vehicle.GetVehicleKit();
        }

        public UnitAI GetAI() { return i_AI; }
        void SetAI(UnitAI newAI) { i_AI = newAI; }

        public bool isPossessing()
        {
            Unit u = GetCharm();
            if (u != null)
                return u.isPossessed();
            else
                return false;
        }
        public Unit GetCharm()
        {
            ObjectGuid charm_guid = GetCharmGUID();
            if (!charm_guid.IsEmpty())
            {
                Unit pet = Global.ObjAccessor.GetUnit(this, charm_guid);
                if (pet != null)
                    return pet;

                Log.outError(LogFilter.Unit, "Unit.GetCharm: Charmed creature {0} not exist.", charm_guid);
                SetGuidValue(UnitFields.Charm, ObjectGuid.Empty);
            }

            return null;
        }
        public bool IsCharmed() { return !GetCharmerGUID().IsEmpty(); }
        public bool isPossessed() { return HasUnitState(UnitState.Possessed); }

        public HostileRefManager getHostileRefManager() { return m_HostileRefManager; }

        public void OnPhaseChange()
        {
            if (!IsInWorld)
                return;

            if (IsTypeId(TypeId.Unit) || !ToPlayer().GetSession().PlayerLogout())
            {
                HostileRefManager refManager = getHostileRefManager();
                HostileReference refe = refManager.getFirst();

                while (refe != null)
                {
                    Unit unit = refe.GetSource().GetOwner();
                    if (unit != null)
                    {
                        Creature creature = unit.ToCreature();
                        if (creature != null)
                            refManager.setOnlineOfflineState(creature, creature.IsInPhase(this));
                    }

                    refe = refe.next();
                }

                // modify threat lists for new phasemask
                if (!IsTypeId(TypeId.Player))
                {
                    List<HostileReference> threatList = GetThreatManager().getThreatList();
                    List<HostileReference> offlineThreatList = GetThreatManager().getOfflineThreatList();

                    // merge expects sorted lists
                    threatList.Sort();
                    offlineThreatList.Sort();
                    threatList.AddRange(offlineThreatList);

                    foreach (var host in threatList)
                    {
                        Unit unit = host.getTarget();
                        if (unit != null)
                            unit.getHostileRefManager().setOnlineOfflineState(ToCreature(), unit.IsInPhase(this));
                    }
                }
            }
        }

        public uint GetModelForForm(ShapeShiftForm form)
        {
            if (IsTypeId(TypeId.Player))
            {
                Aura artifactAura = GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                if (artifactAura != null)
                {
                    Item artifact = ToPlayer().GetItemByGuid(artifactAura.GetCastItemGUID());
                    if (artifact)
                    {
                        ArtifactAppearanceRecord artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifact.GetModifier(ItemModifier.ArtifactAppearanceId));
                        if (artifactAppearance != null)
                            if ((ShapeShiftForm)artifactAppearance.OverrideShapeshiftFormID == form)
                                return artifactAppearance.OverrideShapeshiftDisplayID;
                    }
                }

                byte hairColor = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId);
                byte skinColor = GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId);

                switch (form)
                {
                    case ShapeShiftForm.CatForm:
                        {
                            if (GetRace() == Race.NightElf)
                            {
                                if (HasAura(210333)) // Glyph of the Feral Chameleon
                                    hairColor = (byte)RandomHelper.URand(0, 10);

                                switch (hairColor)
                                {
                                    case 7: // Violet
                                    case 8:
                                        return 29405;
                                    case 3: // Light Blue
                                        return 29406;
                                    case 0: // Green
                                    case 1: // Light Green
                                    case 2: // Dark Green
                                        return 29407;
                                    case 4: // White
                                        return 29408;
                                    default: // original - Dark Blue
                                        return 892;
                                }
                            }
                            else if (GetRace() == Race.Troll)
                            {
                                if (HasAura(210333)) // Glyph of the Feral Chameleon
                                    hairColor = (byte)RandomHelper.URand(0, 12);

                                switch (hairColor)
                                {
                                    case 0: // Red
                                    case 1:
                                        return 33668;
                                    case 2: // Yellow
                                    case 3:
                                        return 33667;
                                    case 4: // Blue
                                    case 5:
                                    case 6:
                                        return 33666;
                                    case 7: // Purple
                                    case 10:
                                        return 33665;
                                    default: // original - white
                                        return 33669;
                                }
                            }
                            else if (GetRace() == Race.Worgen)
                            {
                                if (HasAura(210333)) // Glyph of the Feral Chameleon
                                    hairColor = (byte)RandomHelper.URand(0, 9);

                                // Male
                                if (GetGender() == Gender.Male)
                                {
                                    switch (skinColor)
                                    {
                                        case 1: // Brown
                                            return 33662;
                                        case 2: // Black
                                        case 7:
                                            return 33661;
                                        case 4: // yellow
                                            return 33664;
                                        case 3: // White
                                        case 5:
                                            return 33663;
                                        default: // original - Gray
                                            return 33660;
                                    }
                                }
                                // Female
                                else
                                {
                                    switch (skinColor)
                                    {
                                        case 5: // Brown
                                        case 6:
                                            return 33662;
                                        case 7: // Black
                                        case 8:
                                            return 33661;
                                        case 3: // yellow
                                        case 4:
                                            return 33664;
                                        case 2: // White
                                            return 33663;
                                        default: // original - Gray
                                            return 33660;
                                    }
                                }
                            }
                            else if (GetRace() == Race.Tauren)
                            {
                                if (HasAura(210333)) // Glyph of the Feral Chameleon
                                    hairColor = (byte)RandomHelper.URand(0, 20);

                                if (GetGender() == Gender.Male)
                                {
                                    switch (skinColor)
                                    {
                                        case 12: // White
                                        case 13:
                                        case 14:
                                        case 18: // Completly White
                                            return 29409;
                                        case 9: // Light Brown
                                        case 10:
                                        case 11:
                                            return 29410;
                                        case 6: // Brown
                                        case 7:
                                        case 8:
                                            return 29411;
                                        case 0: // Dark
                                        case 1:
                                        case 2:
                                        case 3: // Dark Grey
                                        case 4:
                                        case 5:
                                            return 29412;
                                        default: // original - Grey
                                            return 8571;
                                    }
                                }
                                // Female
                                else
                                {
                                    switch (skinColor)
                                    {
                                        case 10: // White
                                            return 29409;
                                        case 6: // Light Brown
                                        case 7:
                                            return 29410;
                                        case 4: // Brown
                                        case 5:
                                            return 29411;
                                        case 0: // Dark
                                        case 1:
                                        case 2:
                                        case 3:
                                            return 29412;
                                        default: // original - Grey
                                            return 8571;
                                    }
                                }
                            }
                            else if (Player.TeamForRace(GetRace()) == Team.Alliance)
                                return 892;
                            else
                                return 8571;
                        }
                    case ShapeShiftForm.BearForm:
                        {
                            if (GetRace() == Race.NightElf)
                            {
                                if (HasAura(210333)) // Glyph of the Feral Chameleon
                                    hairColor = (byte)RandomHelper.URand(0, 8);

                                switch (hairColor)
                                {
                                    case 0: // Green
                                    case 1: // Light Green
                                    case 2: // Dark Green
                                        return 29413; // 29415?
                                    case 6: // Dark Blue
                                        return 29414;
                                    case 4: // White
                                        return 29416;
                                    case 3: // Light Blue
                                        return 29417;
                                    default: // original - Violet
                                        return 29415;
                                }
                            }
                            else if (GetRace() == Race.Troll)
                            {
                                if (HasAura(210333)) // Glyph of the Feral Chameleon
                                    hairColor = (byte)RandomHelper.URand(0, 14);

                                switch (hairColor)
                                {
                                    case 0: // Red
                                    case 1:
                                        return 33657;
                                    case 2: // Yellow
                                    case 3:
                                        return 33659;
                                    case 7: // Purple
                                    case 10:
                                        return 33656;
                                    case 8: // White
                                    case 9:
                                    case 11:
                                    case 12:
                                        return 33658;
                                    default: // original - Blue
                                        return 33655;
                                }
                            }
                            else if (GetRace() == Race.Worgen)
                            {
                                if (HasAura(210333)) // Glyph of the Feral Chameleon
                                    hairColor = (byte)RandomHelper.URand(0, 8);

                                // Male
                                if (GetGender() == Gender.Male)
                                {
                                    switch (skinColor)
                                    {
                                        case 1: // Brown
                                            return 33652;
                                        case 2: // Black
                                        case 7:
                                            return 33651;
                                        case 4: // Yellow
                                            return 33653;
                                        case 3: // White
                                        case 5:
                                            return 33654;
                                        default: // original - Gray
                                            return 33650;
                                    }
                                }
                                // Female
                                else
                                {
                                    switch (skinColor)
                                    {
                                        case 5: // Brown
                                        case 6:
                                            return 33652;
                                        case 7: // Black
                                        case 8:
                                            return 33651;
                                        case 3: // yellow
                                        case 4:
                                            return 33654;
                                        case 2: // White
                                            return 33653;
                                        default: // original - Gray
                                            return 33650;
                                    }
                                }
                            }
                            else if (GetRace() == Race.Tauren)
                            {
                                if (HasAura(210333)) // Glyph of the Feral Chameleon
                                    hairColor = (byte)RandomHelper.URand(0, 20);

                                if (GetGender() == Gender.Male)
                                {
                                    switch (skinColor)
                                    {
                                        case 0: // Dark (Black)
                                        case 1:
                                        case 2:
                                            return 29418;
                                        case 3: // White
                                        case 4:
                                        case 5:
                                        case 12:
                                        case 13:
                                        case 14:
                                            return 29419;
                                        case 9: // Light Brown/Grey
                                        case 10:
                                        case 11:
                                        case 15:
                                        case 16:
                                        case 17:
                                            return 29420;
                                        case 18: // Completly White
                                            return 29421;
                                        default: // original - Brown
                                            return 2289;
                                    }
                                }
                                // Female
                                else
                                {
                                    switch (skinColor)
                                    {
                                        case 0: // Dark (Black)
                                        case 1:
                                            return 29418;
                                        case 2: // White
                                        case 3:
                                            return 29419;
                                        case 6: // Light Brown/Grey
                                        case 7:
                                        case 8:
                                        case 9:
                                            return 29420;
                                        case 10: // Completly White
                                            return 29421;
                                        default: // original - Brown
                                            return 2289;
                                    }
                                }
                            }
                            else if (Player.TeamForRace(GetRace()) == Team.Alliance)
                                return 29415;
                            else
                                return 2289;
                        }
                    case ShapeShiftForm.FlightForm:
                        if (Player.TeamForRace(GetRace()) == Team.Alliance)
                            return 20857;
                        return 20872;
                    case ShapeShiftForm.FlightFormEpic:
                        if (HasAura(219062)) // Glyph of the Sentinel
                        {
                            switch (GetRace())
                            {
                                case Race.NightElf: // Blue
                                    return 64328;
                                case Race.Tauren: // Brown
                                    return 64329;
                                case Race.Worgen: // Purple
                                    return 64330;
                                case Race.Troll: // White
                                    return 64331;
                                default:
                                    break;
                            }
                        }
                        if (Player.TeamForRace(GetRace()) == Team.Alliance)
                            return (GetRace() == Race.Worgen ? 37729 : 21243u);
                        if (GetRace() == Race.Troll)
                            return 37730;
                        return 21244;
                    case ShapeShiftForm.MoonkinForm:
                        switch (GetRace())
                        {
                            case Race.NightElf:
                                return 15374;
                            case Race.Tauren:
                                return 15375;
                            case Race.Worgen:
                                return 37173;
                            case Race.Troll:
                                return 37174;
                            default:
                                break;
                        }
                        break;
                    case ShapeShiftForm.AquaticForm:
                        if (HasAura(114333)) // Glyph of the Orca
                            return 4591;
                        return 2428;
                    case ShapeShiftForm.TravelForm:
                        {
                            if (HasAura(131113)) // Glyph of the Cheetah
                                return 1043;

                            if (HasAura(224122)) // Glyph of the Doe
                                return 70450;

                            switch (GetRace())
                            {
                                case Race.NightElf:
                                case Race.Worgen:
                                    return 40816;
                                case Race.Troll:
                                case Race.Tauren:
                                    return 45339;
                                default:
                                    break;
                            }
                            break;
                        }
                    case ShapeShiftForm.GhostWolf:
                        if (HasAura(58135)) // Glyph of Spectral Wolf
                            return 60247;
                        break;
                }
            }

            uint modelid = 0;
            SpellShapeshiftFormRecord formEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)form);
            if (formEntry != null && formEntry.CreatureDisplayID[0] != 0)
            {
                // Take the alliance modelid as default
                if (!IsTypeId(TypeId.Player))
                    return formEntry.CreatureDisplayID[0];
                else
                {
                    if (Player.TeamForRace(GetRace()) == Team.Alliance)
                        modelid = formEntry.CreatureDisplayID[0];
                    else
                        modelid = formEntry.CreatureDisplayID[1];

                    // If the player is horde but there are no values for the horde modelid - take the alliance modelid
                    if (modelid == 0 && Player.TeamForRace(GetRace()) == Team.Horde)
                        modelid = formEntry.CreatureDisplayID[0];
                }
            }

            return modelid;
        }

        public virtual bool CanUseAttackType(WeaponAttackType attacktype)
        {
            switch (attacktype)
            {
                case WeaponAttackType.BaseAttack:
                    return !HasFlag(UnitFields.Flags, UnitFlags.Disarmed);
                case WeaponAttackType.OffAttack:
                    return !HasFlag(UnitFields.Flags2, UnitFlags2.DisarmOffhand);
                case WeaponAttackType.RangedAttack:
                    return !HasFlag(UnitFields.Flags2, UnitFlags2.DisarmRanged);
                default:
                    return true;
            }
        }

        public Totem ToTotem() { return IsTotem() ? (this as Totem) : null; }
        public TempSummon ToTempSummon() { return IsSummon() ? (this as TempSummon) : null; }
        public virtual void setDeathState(DeathState s)
        {
            // Death state needs to be updated before RemoveAllAurasOnDeath() is called, to prevent entering combat
            m_deathState = s;

            if (s != DeathState.Alive && s != DeathState.JustRespawned)
            {
                CombatStop();
                DeleteThreatList();
                getHostileRefManager().deleteReferences();

                if (IsNonMeleeSpellCast(false))
                    InterruptNonMeleeSpells(false);

                ExitVehicle();                                      // Exit vehicle before calling RemoveAllControlled
                // vehicles use special type of charm that is not removed by the next function
                // triggering an assert
                UnsummonAllTotems();
                RemoveAllControlled();
                RemoveAllAurasOnDeath();
            }

            if (s == DeathState.JustDied)
            {
                ModifyAuraState(AuraStateType.HealthLess20Percent, false);
                ModifyAuraState(AuraStateType.HealthLess35Percent, false);
                // remove aurastates allowing special moves
                ClearAllReactives();
                m_Diminishing.Clear();
                if (IsInWorld)
                {
                    // Only clear MotionMaster for entities that exists in world
                    // Avoids crashes in the following conditions :
                    //  * Using 'call pet' on dead pets
                    //  * Using 'call stabled pet'
                    //  * Logging in with dead pets
                    GetMotionMaster().Clear(false);
                    GetMotionMaster().MoveIdle();
                }
                StopMoving();
                DisableSpline();
                // without this when removing IncreaseMaxHealth aura player may stuck with 1 hp
                // do not why since in IncreaseMaxHealth currenthealth is checked
                SetHealth(0);
                SetPower(GetPowerType(), 0);
                SetUInt32Value(UnitFields.NpcEmotestate, 0);

                // players in instance don't have ZoneScript, but they have InstanceScript
                ZoneScript zoneScript = GetZoneScript() != null ? GetZoneScript() : GetInstanceScript();
                if (zoneScript != null)
                    zoneScript.OnUnitDeath(this);
            }
            else if (s == DeathState.JustRespawned)
                RemoveFlag(UnitFields.Flags, UnitFlags.Skinnable); // clear skinnable for creature and player (at Battleground)
        }

        public bool IsVisible()
        {
            return (m_serverSideVisibility.GetValue(ServerSideVisibilityType.GM) > (uint)AccountTypes.Player) ? false : true;
        }

        public void SetVisible(bool val)
        {
            if (!val)
                m_serverSideVisibility.SetValue(ServerSideVisibilityType.GM, AccountTypes.GameMaster);
            else
                m_serverSideVisibility.SetValue(ServerSideVisibilityType.GM, AccountTypes.Player);

            UpdateObjectVisibility();
        }
        public bool IsMounted() { return HasFlag(UnitFields.Flags, UnitFlags.Mount); }
        public uint GetMountID() { return GetUInt32Value(UnitFields.MountDisplayId); }

        public void SetShapeshiftForm(ShapeShiftForm form)
        {
            SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.ShapeshiftForm, (byte)form);
        }

        public void SetModifierValue(UnitMods unitMod, UnitModifierType modifierType, float value)
        {
            m_auraModifiersGroup[(int)unitMod][(int)modifierType] = value;
        }
        public int CalcSpellDuration(SpellInfo spellProto)
        {
            sbyte comboPoints = (sbyte)(m_playerMovingMe != null ? m_playerMovingMe.GetComboPoints() : 0);

            int minduration = spellProto.GetDuration();
            int maxduration = spellProto.GetMaxDuration();

            int duration;

            if (comboPoints != 0 && minduration != -1 && minduration != maxduration)
                duration = minduration + (maxduration - minduration) * comboPoints / 5;
            else
                duration = minduration;

            return duration;
        }

        public int ModSpellDuration(SpellInfo spellProto, Unit target, int duration, bool positive, uint effectMask)
        {
            // don't mod permanent auras duration
            if (duration < 0)
                return duration;

            // some auras are not affected by duration modifiers
            if (spellProto.HasAttribute(SpellAttr7.IgnoreDurationMods))
                return duration;

            // cut duration only of negative effects
            if (!positive)
            {
                uint mechanic = spellProto.GetSpellMechanicMaskByEffectMask(effectMask);

                int durationMod;
                int durationMod_always = 0;
                int durationMod_not_stack = 0;

                for (byte i = 1; i <= (int)Mechanics.Enraged; ++i)
                {
                    if (!Convert.ToBoolean(mechanic & 1 << i))
                        continue;
                    // Find total mod value (negative bonus)
                    int new_durationMod_always = target.GetTotalAuraModifierByMiscValue(AuraType.MechanicDurationMod, i);
                    // Find max mod (negative bonus)
                    int new_durationMod_not_stack = target.GetMaxNegativeAuraModifierByMiscValue(AuraType.MechanicDurationModNotStack, i);
                    // Check if mods applied before were weaker
                    if (new_durationMod_always < durationMod_always)
                        durationMod_always = new_durationMod_always;
                    if (new_durationMod_not_stack < durationMod_not_stack)
                        durationMod_not_stack = new_durationMod_not_stack;
                }

                // Select strongest negative mod
                if (durationMod_always > durationMod_not_stack)
                    durationMod = durationMod_not_stack;
                else
                    durationMod = durationMod_always;

                if (durationMod != 0)
                    MathFunctions.AddPct(ref duration, durationMod);

                // there are only negative mods currently
                durationMod_always = target.GetTotalAuraModifierByMiscValue(AuraType.ModAuraDurationByDispel, (int)spellProto.Dispel);
                durationMod_not_stack = target.GetMaxNegativeAuraModifierByMiscValue(AuraType.ModAuraDurationByDispelNotStack, (int)spellProto.Dispel);

                durationMod = 0;
                if (durationMod_always > durationMod_not_stack)
                    durationMod += durationMod_not_stack;
                else
                    durationMod += durationMod_always;

                if (durationMod != 0)
                    MathFunctions.AddPct(ref duration, durationMod);
            }
            else
            {
                // else positive mods here, there are no currently
                // when there will be, change GetTotalAuraModifierByMiscValue to GetTotalPositiveAuraModifierByMiscValue

                // Mixology - duration boost
                if (target.IsTypeId(TypeId.Player))
                {
                    if (spellProto.SpellFamilyName == SpellFamilyNames.Potion && (
                       Global.SpellMgr.IsSpellMemberOfSpellGroup(spellProto.Id, SpellGroup.ElixirBattle) ||
                       Global.SpellMgr.IsSpellMemberOfSpellGroup(spellProto.Id, SpellGroup.ElixirGuardian)))
                    {
                        SpellEffectInfo effect = spellProto.GetEffect(Difficulty.None, 0);
                        if (target.HasAura(53042) && effect != null && target.HasSpell(effect.TriggerSpell))
                            duration *= 2;
                    }
                }
            }

            return Math.Max(duration, 0);
        }

        // creates aura application instance and registers it in lists
        // aura application effects are handled separately to prevent aura list corruption
        public AuraApplication _CreateAuraApplication(Aura aura, uint effMask)
        {
            // can't apply aura on unit which is going to be deleted - to not create a memory leak
            Cypher.Assert(!m_cleanupDone);
            // aura musn't be removed
            Cypher.Assert(!aura.IsRemoved());

            // aura mustn't be already applied on target
            Cypher.Assert(!aura.IsAppliedOnTarget(GetGUID()), "Unit._CreateAuraApplication: aura musn't be applied on target");

            SpellInfo aurSpellInfo = aura.GetSpellInfo();
            uint aurId = aurSpellInfo.Id;

            // ghost spell check, allow apply any auras at player loading in ghost mode (will be cleanup after load)
            if (!IsAlive() && !aurSpellInfo.IsDeathPersistent() &&
                (!IsTypeId(TypeId.Player) || !ToPlayer().GetSession().PlayerLoading()))
                return null;

            Unit caster = aura.GetCaster();

            AuraApplication aurApp = new AuraApplication(this, caster, aura, effMask);
            m_appliedAuras.Add(aurId, aurApp);

            if (aurSpellInfo.HasAnyAuraInterruptFlag())
            {
                m_interruptableAuras.Add(aurApp);
                AddInterruptMask(aurSpellInfo.AuraInterruptFlags);
            }

            AuraStateType aState = aura.GetSpellInfo().GetAuraState();
            if (aState != 0)
                m_auraStateAuras.Add(aState, aurApp);

            aura._ApplyForTarget(this, caster, aurApp);
            return aurApp;
        }
        public void AddInterruptMask(uint[] mask)
        {
            for (int i = 0; i < m_interruptMask.Length; ++i)
                m_interruptMask[i] |= mask[i];
        }

        void _UpdateAutoRepeatSpell()
        {
            // check "realtime" interrupts
            // don't cancel spells which are affected by a SPELL_AURA_CAST_WHILE_WALKING effect
            if (((IsTypeId(TypeId.Player) && ToPlayer().isMoving()) || IsNonMeleeSpellCast(false, false, true, GetCurrentSpell(CurrentSpellTypes.AutoRepeat).m_spellInfo.Id == 75)) &&
                !HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, m_currentSpells[CurrentSpellTypes.AutoRepeat].m_spellInfo))
            {
                // cancel wand shoot
                if (m_currentSpells[CurrentSpellTypes.AutoRepeat].m_spellInfo.Id != 75)
                    InterruptSpell(CurrentSpellTypes.AutoRepeat);
                m_AutoRepeatFirstCast = true;
                return;
            }

            // apply delay (Auto Shot (spellID 75) not affected)
            if (m_AutoRepeatFirstCast && getAttackTimer(WeaponAttackType.RangedAttack) < 500 && m_currentSpells[CurrentSpellTypes.AutoRepeat].m_spellInfo.Id != 75)
                setAttackTimer(WeaponAttackType.RangedAttack, 500);
            m_AutoRepeatFirstCast = false;

            // castroutine
            if (isAttackReady(WeaponAttackType.RangedAttack))
            {
                // Check if able to cast
                if (m_currentSpells[CurrentSpellTypes.AutoRepeat].CheckCast(true) != SpellCastResult.SpellCastOk)
                {
                    InterruptSpell(CurrentSpellTypes.AutoRepeat);
                    return;
                }

                // we want to shoot
                Spell spell = new Spell(this, m_currentSpells[CurrentSpellTypes.AutoRepeat].m_spellInfo, TriggerCastFlags.FullMask);
                spell.prepare(m_currentSpells[CurrentSpellTypes.AutoRepeat].m_targets);

                // all went good, reset attack
                resetAttackTimer(WeaponAttackType.RangedAttack);
            }
        }

        public void UpdateDisplayPower()
        {
            PowerType displayPower = PowerType.Mana;
            switch (GetShapeshiftForm())
            {
                case ShapeShiftForm.Ghoul:
                case ShapeShiftForm.CatForm:
                    displayPower = PowerType.Energy;
                    break;
                case ShapeShiftForm.BearForm:
                    displayPower = PowerType.Rage;
                    break;
                case ShapeShiftForm.TravelForm:
                case ShapeShiftForm.GhostWolf:
                    displayPower = PowerType.Mana;
                    break;
                default:
                    {
                        var powerTypeAuras = GetAuraEffectsByType(AuraType.ModPowerDisplay);
                        if (!powerTypeAuras.Empty())
                        {
                            AuraEffect powerTypeAura = powerTypeAuras.First();
                            displayPower = (PowerType)powerTypeAura.GetMiscValue();
                        }
                        else if (GetTypeId() == TypeId.Player)
                        {
                            ChrClassesRecord cEntry = CliDB.ChrClassesStorage.LookupByKey(GetClass());
                            if (cEntry != null && cEntry.DisplayPower < PowerType.Max)
                                displayPower = cEntry.DisplayPower;
                        }
                        else if (GetTypeId() == TypeId.Unit)
                        {
                            Vehicle vehicle = GetVehicleKit();
                            if (vehicle)
                            {
                                PowerDisplayRecord powerDisplay = CliDB.PowerDisplayStorage.LookupByKey(vehicle.GetVehicleInfo().PowerDisplayID[0]);
                                if (powerDisplay != null)
                                    displayPower = (PowerType)powerDisplay.ActualType;
                                else if (GetClass() == Class.Rogue)
                                    displayPower = PowerType.Energy;
                            }
                            else
                            {
                                Pet pet = ToPet();
                                if (pet)
                                {
                                    if (pet.getPetType() == PetType.Hunter) // Hunter pets have focus
                                        displayPower = PowerType.Focus;
                                    else if (pet.IsPetGhoul() || pet.IsPetAbomination()) // DK pets have energy
                                        displayPower = PowerType.Energy;
                                }
                            }
                        }
                        break;
                    }
            }

            SetPowerType(displayPower);
        }

        public FactionTemplateRecord GetFactionTemplateEntry()
        {
            FactionTemplateRecord entry = CliDB.FactionTemplateStorage.LookupByKey(getFaction());
            if (entry == null)
            {
                ObjectGuid guid = ObjectGuid.Empty;                             // prevent repeating spam same faction problem

                if (GetGUID() != guid)
                {
                    Player player = ToPlayer();
                    Creature creature = ToCreature();
                    if (player != null)
                        Log.outError(LogFilter.Unit, "Player {0} has invalid faction (faction template id) #{1}", player.GetName(), getFaction());
                    else if (creature != null)
                        Log.outError(LogFilter.Unit, "Creature (template id: {0}) has invalid faction (faction template id) #{1}", creature.GetCreatureTemplate().Entry, getFaction());
                    else
                        Log.outError(LogFilter.Unit, "Unit (name={0}, type={1}) has invalid faction (faction template id) #{2}", GetName(), GetTypeId(), getFaction());

                    guid = GetGUID();
                }
            }
            return entry;
        }

        public bool IsInFeralForm()
        {
            ShapeShiftForm form = GetShapeshiftForm();
            return form == ShapeShiftForm.CatForm || form == ShapeShiftForm.BearForm || form == ShapeShiftForm.DireBearForm || form == ShapeShiftForm.GhostWolf;
        }
        public bool IsControlledByPlayer() { return m_ControlledByPlayer; }

        public bool IsCharmedOwnedByPlayerOrPlayer() { return GetCharmerOrOwnerOrOwnGUID().IsPlayer(); }

        public void addFollower(FollowerReference pRef)
        {
            m_FollowingRefManager.InsertFirst(pRef);
        }
        public void removeFollower(FollowerReference pRef) { } //nothing to do yet

        public uint GetCreatureTypeMask()
        {
            uint creatureType = (uint)GetCreatureType();
            return (uint)(creatureType >= 1 ? (1 << (int)(creatureType - 1)) : 0);
        }

        public Pet ToPet()
        {
            return IsPet() ? (this as Pet) : null;
        }
        public MotionMaster GetMotionMaster() { return i_motionMaster; }

        public void PlayOneShotAnimKitId(ushort animKitId)
        {
            if (!CliDB.AnimKitStorage.ContainsKey(animKitId))
            {
                Log.outError(LogFilter.Unit, "Unit.PlayOneShotAnimKitId using invalid AnimKit ID: {0}", animKitId);
                return;
            }

            PlayOneShotAnimKit packet = new PlayOneShotAnimKit();
            packet.Unit = GetGUID();
            packet.AnimKitID = animKitId;
            SendMessageToSet(packet, true);
        }

        public void SetAIAnimKitId(ushort animKitId)
        {
            if (_aiAnimKitId == animKitId)
                return;

            if (animKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(animKitId))
                return;

            _aiAnimKitId = animKitId;

            SetAIAnimKit data = new SetAIAnimKit();
            data.Unit = GetGUID();
            data.AnimKitID = animKitId;
            SendMessageToSet(data, true);
        }

        public override ushort GetAIAnimKitId() { return _aiAnimKitId; }

        public void SetMovementAnimKitId(ushort animKitId)
        {
            if (_movementAnimKitId == animKitId)
                return;

            if (animKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(animKitId))
                return;

            _movementAnimKitId = animKitId;

            SetMovementAnimKit data = new SetMovementAnimKit();
            data.Unit = GetGUID();
            data.AnimKitID = animKitId;
            SendMessageToSet(data, true);
        }

        public override ushort GetMovementAnimKitId() { return _movementAnimKitId; }

        public void SetMeleeAnimKitId(ushort animKitId)
        {
            if (_meleeAnimKitId == animKitId)
                return;

            if (animKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(animKitId))
                return;

            _meleeAnimKitId = animKitId;

            SetMeleeAnimKit data = new SetMeleeAnimKit();
            data.Unit = GetGUID();
            data.AnimKitID = animKitId;
            SendMessageToSet(data, true);
        }

        public override ushort GetMeleeAnimKitId() { return _meleeAnimKitId; }

        public uint GetVirtualItemId(int slot)
        {
            if (slot >= SharedConst.MaxEquipmentItems)
                return 0;

            return GetUInt32Value(UnitFields.VirtualItemSlotId + slot * 2);
        }

        public ushort GetVirtualItemAppearanceMod(uint slot)
        {
            if (slot >= SharedConst.MaxEquipmentItems)
                return 0;

            return GetUInt16Value(UnitFields.VirtualItemSlotId + (int)slot * 2 + 1, 0);
        }

        public void SetVirtualItem(int slot, uint itemId, ushort appearanceModId = 0, ushort itemVisual = 0)
        {
            if (slot >= SharedConst.MaxEquipmentItems)
                return;

            SetUInt32Value(UnitFields.VirtualItemSlotId + slot * 2, itemId);
            SetUInt16Value(UnitFields.VirtualItemSlotId + slot * 2 + 1, 0, appearanceModId);
            SetUInt16Value(UnitFields.VirtualItemSlotId + slot * 2 + 1, 1, itemVisual);
        }

        //Unit
        public void SetLevel(uint lvl)
        {
            SetUInt32Value(UnitFields.Level, lvl);

            if (IsTypeId(TypeId.Player))
            {
                Player player = ToPlayer();
                if (player.GetGroup())
                    player.SetGroupUpdateFlag(GroupUpdateFlags.Level);
                Global.WorldMgr.UpdateCharacterInfoLevel(ToPlayer().GetGUID(), (byte)lvl);
            }
        }
        public uint getLevel()
        {
            return GetUInt32Value(UnitFields.Level);
        }

        public override uint GetLevelForTarget(WorldObject target)
        {
            return getLevel();
        }

        public Race GetRace()
        {
            return (Race)GetByteValue(UnitFields.Bytes0, 0);
        }
        public ulong getRaceMask()
        {
            return (1ul << ((int)GetRace() - 1));
        }
        public Class GetClass()
        {
            return (Class)GetByteValue(UnitFields.Bytes0, 1);
        }
        public uint getClassMask()
        {
            return (uint)(1 << ((int)GetClass() - 1));
        }
        public Gender GetGender()
        {
            return (Gender)GetByteValue(UnitFields.Bytes0, 3);
        }

        public void SetNativeDisplayId(uint displayId, float displayScale = 1f)
        {
            SetUInt32Value(UnitFields.NativeDisplayId, displayId);
            SetFloatValue(UnitFields.NativeXDisplayScale, displayScale);
        }
        public virtual void SetDisplayId(uint modelId, float displayScale = 1f)
        {
            SetUInt32Value(UnitFields.DisplayId, modelId);
            SetFloatValue(UnitFields.DisplayScale, displayScale);
            // Set Gender by modelId
            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelInfo(modelId);
            if (minfo != null)
                SetByteValue(UnitFields.Bytes0, 3, (byte)minfo.gender);
        }
        public uint GetNativeDisplayId()
        {
            return GetUInt32Value(UnitFields.NativeDisplayId);
        }
        public float GetNativeDisplayScale()
        {
            return GetFloatValue(UnitFields.NativeXDisplayScale);
        }
        public virtual Unit GetOwner()
        {
            ObjectGuid ownerid = GetOwnerGUID();
            if (!ownerid.IsEmpty())
                return Global.ObjAccessor.GetUnit(this, ownerid);

            return null;
        }
        public virtual float GetFollowAngle() { return MathFunctions.PiOver2; }

        public ObjectGuid GetOwnerGUID() { return GetGuidValue(UnitFields.SummonedBy); }
        public ObjectGuid GetCreatorGUID() { return GetGuidValue(UnitFields.CreatedBy); }
        public ObjectGuid GetMinionGUID() { return GetGuidValue(UnitFields.Summon); }
        public ObjectGuid GetCharmerGUID() { return GetGuidValue(UnitFields.CharmedBy); }
        public ObjectGuid GetCharmGUID() { return GetGuidValue(UnitFields.Charm); }
        public ObjectGuid GetPetGUID() { return m_SummonSlot[0]; }
        public ObjectGuid GetCritterGUID() { return GetGuidValue(UnitFields.Critter); }
        public ObjectGuid GetCharmerOrOwnerGUID()
        {
            return !GetCharmerGUID().IsEmpty() ? GetCharmerGUID() : GetOwnerGUID();
        }
        ObjectGuid GetCharmerOrOwnerOrOwnGUID()
        {
            ObjectGuid guid = GetCharmerOrOwnerGUID();
            if (!guid.IsEmpty())
                return guid;

            return GetGUID();
        }
        public Unit GetCharmer()
        {
            ObjectGuid charmerid = GetCharmerGUID();
            if (!charmerid.IsEmpty())
                return Global.ObjAccessor.GetUnit(this, charmerid);
            return null;
        }
        public Unit GetCharmerOrOwnerOrSelf()
        {
            Unit u = GetCharmerOrOwner();
            if (u != null)
                return u;

            return this;
        }
        public Player GetCharmerOrOwnerPlayerOrPlayerItself()
        {
            ObjectGuid guid = GetCharmerOrOwnerGUID();
            if (guid.IsPlayer())
                return Global.ObjAccessor.FindPlayer(guid);

            return IsTypeId(TypeId.Player) ? ToPlayer() : null;
        }
        public Unit GetCharmerOrOwner()
        {
            return !GetCharmerGUID().IsEmpty() ? GetCharmer() : GetOwner();
        }

        public uint GetChannelSpellId() { return GetUInt32Value(UnitFields.ChannelData);    }
        public void SetChannelSpellId(uint channelSpellId) { SetUInt32Value(UnitFields.ChannelData, channelSpellId); }
        public uint GetChannelSpellXSpellVisualId() { return GetUInt32Value(UnitFields.ChannelData + 1);}
        public void SetChannelSpellXSpellVisualId(uint channelSpellXSpellVisualId) { SetUInt32Value(UnitFields.ChannelData + 1, channelSpellXSpellVisualId); }

        public List<ObjectGuid> GetChannelObjects() { return GetDynamicStructuredValues<ObjectGuid>(UnitDynamicFields.ChannelObjects); }
        public void AddChannelObject(ObjectGuid guid) { AddDynamicStructuredValue(UnitDynamicFields.ChannelObjects, guid); }

        public void SetOwnerGUID(ObjectGuid owner)
        {
            if (GetOwnerGUID() == owner)
                return;

            SetGuidValue(UnitFields.SummonedBy, owner);
            if (owner.IsEmpty())
                return;

            // Update owner dependent fields
            Player player = Global.ObjAccessor.GetPlayer(this, owner);
            if (player == null || !player.HaveAtClient(this)) // if player cannot see this unit yet, he will receive needed data with create object
                return;

            SetFieldNotifyFlag(UpdateFieldFlags.Owner);

            UpdateData udata = new UpdateData(GetMapId());
            UpdateObject packet;
            BuildValuesUpdateBlockForPlayer(udata, player);
            udata.BuildPacket(out packet);
            player.SendPacket(packet);

            RemoveFieldNotifyFlag(UpdateFieldFlags.Owner);
        }
        public void SetCreatorGUID(ObjectGuid creator) { SetGuidValue(UnitFields.CreatedBy, creator); }
        void SetMinionGUID(ObjectGuid guid) { SetGuidValue(UnitFields.Summon, guid); }
        void SetCharmerGUID(ObjectGuid owner) { SetGuidValue(UnitFields.CharmedBy, owner); }
        public void SetPetGUID(ObjectGuid guid) { m_SummonSlot[0] = guid; }
        void SetCritterGUID(ObjectGuid guid) { SetGuidValue(UnitFields.Critter, guid); }

        public uint GetCombatTimer() { return m_CombatTimer; }
        public bool IsPvP() { return HasByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.PvP); }
        public bool IsFFAPvP() { return HasByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.FFAPvp); }

        public ShapeShiftForm GetShapeshiftForm()
        {
            return (ShapeShiftForm)GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.ShapeshiftForm);
        }
        public CreatureType GetCreatureType()
        {
            if (IsTypeId(TypeId.Player))
            {
                ShapeShiftForm form = GetShapeshiftForm();
                var ssEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)form);
                if (ssEntry != null && ssEntry.CreatureType > 0)
                    return (CreatureType)ssEntry.CreatureType;
                else
                    return CreatureType.Humanoid;
            }
            else
                return ToCreature().GetCreatureTemplate().CreatureType;
        }
        Player GetAffectingPlayer()
        {
            if (GetCharmerOrOwnerGUID().IsEmpty())
                return IsTypeId(TypeId.Player) ? ToPlayer() : null;

            Unit owner = GetCharmerOrOwner();
            if (owner != null)
                return owner.GetCharmerOrOwnerPlayerOrPlayerItself();
            return null;
        }

        public void DeMorph()
        {
            SetDisplayId(GetNativeDisplayId());
        }

        public bool IsStopped() { return !HasUnitState(UnitState.Moving); }

        public bool HasUnitTypeMask(UnitTypeMask mask) { return Convert.ToBoolean(mask & m_unitTypeMask); }
        public void AddUnitTypeMask(UnitTypeMask mask) { m_unitTypeMask |= mask; }

        public bool IsAlive() { return m_deathState == DeathState.Alive; }
        public bool IsDying() { return m_deathState == DeathState.JustDied; }
        public bool IsDead() { return (m_deathState == DeathState.Dead || m_deathState == DeathState.Corpse); }
        public bool IsSummon() { return m_unitTypeMask.HasAnyFlag(UnitTypeMask.Summon); }
        public bool IsGuardian() { return m_unitTypeMask.HasAnyFlag(UnitTypeMask.Guardian); }
        public bool IsPet() { return m_unitTypeMask.HasAnyFlag(UnitTypeMask.Pet); }
        public bool IsHunterPet() { return m_unitTypeMask.HasAnyFlag(UnitTypeMask.HunterPet); }
        public bool IsTotem() { return m_unitTypeMask.HasAnyFlag(UnitTypeMask.Totem); }
        public bool IsVehicle() { return m_unitTypeMask.HasAnyFlag(UnitTypeMask.Vehicle); }

        public void AddUnitState(UnitState f)
        {
            m_state |= f;
        }
        public bool HasUnitState(UnitState f)
        {
            return Convert.ToBoolean(m_state & f);
        }
        public void ClearUnitState(UnitState f)
        {
            m_state &= ~f;
        }
        public void SetStandFlags(object flags)
        {
            SetByteFlag(UnitFields.Bytes1, UnitBytes1Offsets.VisFlag, flags);
        }
        public void RemoveStandFlags(object flags)
        {
            RemoveByteFlag(UnitFields.Bytes1, UnitBytes1Offsets.VisFlag, flags);
        }

        public override bool IsAlwaysVisibleFor(WorldObject seer)
        {
            if (base.IsAlwaysVisibleFor(seer))
                return true;

            // Always seen by owner
            ObjectGuid guid = GetCharmerOrOwnerGUID();
            if (!guid.IsEmpty())
                if (seer.GetGUID() == guid)
                    return true;

            Player seerPlayer = seer.ToPlayer();
            if (seerPlayer != null)
            {
                Unit owner = GetOwner();
                if (owner != null)
                {
                    Player ownerPlayer = owner.ToPlayer();
                    if (ownerPlayer)
                        if (ownerPlayer.IsGroupVisibleFor(seerPlayer))
                            return true;
                }
            }

            return false;
        }

        //Faction
        public bool IsNeutralToAll()
        {
            var my_faction = GetFactionTemplateEntry();
            if (my_faction == null || my_faction.Faction == 0)
                return true;

            var raw_faction = CliDB.FactionStorage.LookupByKey(my_faction.Faction);
            if (raw_faction != null && raw_faction.ReputationIndex >= 0)
                return false;

            return my_faction.IsNeutralToAll();
        }
        public bool IsHostileTo(Unit unit)
        {
            return GetReactionTo(unit) <= ReputationRank.Hostile;
        }
        public bool IsFriendlyTo(Unit unit)
        {
            return GetReactionTo(unit) >= ReputationRank.Friendly;
        }
        public ReputationRank GetReactionTo(Unit target)
        {
            // always friendly to self
            if (this == target)
                return ReputationRank.Friendly;

            // always friendly to charmer or owner
            if (GetCharmerOrOwnerOrSelf() == target.GetCharmerOrOwnerOrSelf())
                return ReputationRank.Friendly;

            if (HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable))
            {
                if (target.HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable))
                {
                    Player selfPlayerOwner = GetAffectingPlayer();
                    Player targetPlayerOwner = target.GetAffectingPlayer();

                    if (selfPlayerOwner != null && targetPlayerOwner != null)
                    {
                        // always friendly to other unit controlled by player, or to the player himself
                        if (selfPlayerOwner == targetPlayerOwner)
                            return ReputationRank.Friendly;

                        // duel - always hostile to opponent
                        if (selfPlayerOwner.duel != null && selfPlayerOwner.duel.opponent == targetPlayerOwner && selfPlayerOwner.duel.startTime != 0)
                            return ReputationRank.Hostile;

                        // same group - checks dependant only on our faction - skip FFA_PVP for example
                        if (selfPlayerOwner.IsInRaidWith(targetPlayerOwner))
                            return ReputationRank.Friendly; // return true to allow config option AllowTwoSide.Interaction.Group to work
                    }

                    // check FFA_PVP
                    if (IsFFAPvP() && target.IsFFAPvP())
                        return ReputationRank.Hostile;

                    if (selfPlayerOwner != null)
                    {
                        var targetFactionTemplateEntry = target.GetFactionTemplateEntry();
                        if (targetFactionTemplateEntry != null)
                        {
                            if (!selfPlayerOwner.HasFlag(UnitFields.Flags2, UnitFlags2.IgnoreReputation))
                            {
                                var targetFactionEntry = CliDB.FactionStorage.LookupByKey(targetFactionTemplateEntry.Faction);
                                if (targetFactionEntry != null)
                                {
                                    if (targetFactionEntry.CanHaveReputation())
                                    {
                                        // check contested flags
                                        if (Convert.ToBoolean(targetFactionTemplateEntry.Flags & (uint)FactionTemplateFlags.ContestedGuard)
                                            && selfPlayerOwner.HasFlag(PlayerFields.Flags, PlayerFlags.ContestedPVP))
                                            return ReputationRank.Hostile;

                                        // if faction has reputation, hostile state depends only from AtWar state
                                        if (selfPlayerOwner.GetReputationMgr().IsAtWar(targetFactionEntry))
                                            return ReputationRank.Hostile;
                                        return ReputationRank.Friendly;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // do checks dependant only on our faction
            return GetFactionReactionTo(GetFactionTemplateEntry(), target);
        }
        ReputationRank GetFactionReactionTo(FactionTemplateRecord factionTemplateEntry, Unit target)
        {
            // always neutral when no template entry found
            if (factionTemplateEntry == null)
                return ReputationRank.Neutral;

            var targetFactionTemplateEntry = target.GetFactionTemplateEntry();
            if (targetFactionTemplateEntry == null)
                return ReputationRank.Neutral;

            Player targetPlayerOwner = target.GetAffectingPlayer();
            if (targetPlayerOwner != null)
            {
                // check contested flags
                if (Convert.ToBoolean(factionTemplateEntry.Flags & (uint)FactionTemplateFlags.ContestedGuard)
                    && targetPlayerOwner.HasFlag(PlayerFields.Flags, PlayerFlags.ContestedPVP))
                    return ReputationRank.Hostile;
                ReputationRank repRank = targetPlayerOwner.GetReputationMgr().GetForcedRankIfAny(factionTemplateEntry);
                if (repRank != ReputationRank.None)
                    return repRank;
                if (!target.HasFlag(UnitFields.Flags2, UnitFlags2.IgnoreReputation))
                {
                    var factionEntry = CliDB.FactionStorage.LookupByKey(factionTemplateEntry.Faction);
                    if (factionEntry != null)
                    {
                        if (factionEntry.CanHaveReputation())
                        {
                            // CvP case - check reputation, don't allow state higher than neutral when at war
                            repRank = targetPlayerOwner.GetReputationMgr().GetRank(factionEntry);
                            if (targetPlayerOwner.GetReputationMgr().IsAtWar(factionEntry))
                                repRank = (ReputationRank)Math.Min((int)ReputationRank.Neutral, (int)repRank);
                            return repRank;
                        }
                    }
                }
            }

            // common faction based check
            if (factionTemplateEntry.IsHostileTo(targetFactionTemplateEntry))
                return ReputationRank.Hostile;
            if (factionTemplateEntry.IsFriendlyTo(targetFactionTemplateEntry))
                return ReputationRank.Friendly;
            if (targetFactionTemplateEntry.IsFriendlyTo(factionTemplateEntry))
                return ReputationRank.Friendly;
            if (Convert.ToBoolean(factionTemplateEntry.Flags & (uint)FactionTemplateFlags.HostileByDefault))
                return ReputationRank.Hostile;
            // neutral by default
            return ReputationRank.Neutral;
        }

        public uint getFaction()
        {
            return GetUInt32Value(UnitFields.FactionTemplate);
        }
        public void SetFaction(uint faction)
        {
            SetUInt32Value(UnitFields.FactionTemplate, faction);
        }
        public void RestoreFaction()
        {
            if (IsTypeId(TypeId.Player))
                ToPlayer().setFactionForRace(GetRace());
            else
            {
                if (HasUnitTypeMask(UnitTypeMask.Minion))
                {
                    Unit owner = GetOwner();
                    if (owner)
                    {
                        SetFaction(owner.getFaction());
                        return;
                    }
                }
                CreatureTemplate cinfo = ToCreature().GetCreatureTemplate();
                if (cinfo != null)  // normal creature
                    SetFaction(cinfo.Faction);
            }
        }

        public bool IsInPartyWith(Unit unit)
        {
            if (this == unit)
                return true;

            Unit u1 = GetCharmerOrOwnerOrSelf();
            Unit u2 = unit.GetCharmerOrOwnerOrSelf();
            if (u1 == u2)
                return true;

            if (u1.IsTypeId(TypeId.Player) && u2.IsTypeId(TypeId.Player))
                return u1.ToPlayer().IsInSameGroupWith(u2.ToPlayer());
            else if ((u2.IsTypeId(TypeId.Player) && u1.IsTypeId(TypeId.Unit) && u1.ToCreature().GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)) ||
                (u1.IsTypeId(TypeId.Player) && u2.IsTypeId(TypeId.Unit) && u2.ToCreature().GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)))
                return true;

            // else u1->GetTypeId() == u2->GetTypeId() == TYPEID_UNIT
            return u1.getFaction() == u2.getFaction();
        }

        public bool IsInRaidWith(Unit unit)
        {
            if (this == unit)
                return true;

            Unit u1 = GetCharmerOrOwnerOrSelf();
            Unit u2 = unit.GetCharmerOrOwnerOrSelf();
            if (u1 == u2)
                return true;

            if (u1.IsTypeId(TypeId.Player) && u2.IsTypeId(TypeId.Player))
                return u1.ToPlayer().IsInSameRaidWith(u2.ToPlayer());
            else if ((u2.IsTypeId(TypeId.Player) && u1.IsTypeId(TypeId.Unit) && u1.ToCreature().GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)) ||
                    (u1.IsTypeId(TypeId.Player) && u2.IsTypeId(TypeId.Unit) && u2.ToCreature().GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)))
                return true;

            // else u1->GetTypeId() == u2->GetTypeId() == TYPEID_UNIT
            return u1.getFaction() == u2.getFaction();
        }

        public SheathState GetSheath() { return (SheathState)GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.SheathState); }
        public void SetSheath(SheathState sheathed) { SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.SheathState, (byte)sheathed); }

        public UnitStandStateType GetStandState() { return (UnitStandStateType)GetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.StandState); }

        public bool IsSitState()
        {
            UnitStandStateType s = GetStandState();
            return
                s == UnitStandStateType.SitChair || s == UnitStandStateType.SitLowChair ||
                s == UnitStandStateType.SitMediumChair || s == UnitStandStateType.SitHighChair ||
                s == UnitStandStateType.Sit;
        }

        public bool IsStandState()
        {
            UnitStandStateType s = GetStandState();
            return !IsSitState() && s != UnitStandStateType.Sleep && s != UnitStandStateType.Kneel;
        }

        public void SetStandState(UnitStandStateType state, uint animKitId = 0)
        {
            SetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.StandState, (byte)state);

            if (IsStandState())
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.NotSeated);

            if (IsTypeId(TypeId.Player))
            {
                StandStateUpdate packet = new StandStateUpdate(state, animKitId);
                ToPlayer().SendPacket(packet);
            }
        }

        public uint GetDisplayId() { return GetUInt32Value(UnitFields.DisplayId); }

        public void RestoreDisplayId(bool ignorePositiveAurasPreventingMounting = false)
        {
            AuraEffect handledAura = null;
            // try to receive model from transform auras
            var transforms = GetAuraEffectsByType(AuraType.Transform);
            if (!transforms.Empty())
            {
                // iterate over already applied transform auras - from newest to oldest
                foreach (var eff in transforms)
                {
                    AuraApplication aurApp = eff.GetBase().GetApplicationOfTarget(GetGUID());
                    if (aurApp != null)
                    {
                        if (handledAura == null)
                        {
                            if (!ignorePositiveAurasPreventingMounting)
                                handledAura = eff;
                            else
                            {
                                CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate((uint)eff.GetMiscValue());
                                if (ci != null)
                                    if (!IsDisallowedMountForm(eff.GetId(), ShapeShiftForm.None, ObjectManager.ChooseDisplayId(ci).CreatureDisplayID))
                                        handledAura = eff;
                            }
                        }

                        // prefer negative auras
                        if (!aurApp.IsPositive())
                        {
                            handledAura = eff;
                            break;
                        }
                    }
                }
            }
            uint modelId;
            // transform aura was found
            if (handledAura != null)
                handledAura.HandleEffect(this, AuraEffectHandleModes.SendForClient, true);
            // we've found shapeshift
            else if ((modelId = GetModelForForm(GetShapeshiftForm())) != 0)
            {
                if (!ignorePositiveAurasPreventingMounting || !IsDisallowedMountForm(0, GetShapeshiftForm(), modelId))
                    SetDisplayId(modelId);
                else
                    SetDisplayId(GetNativeDisplayId());
            }
            // no auras found - set modelid to default
            else
                SetDisplayId(GetNativeDisplayId());
        }

        public bool IsDamageReducedByArmor(SpellSchoolMask schoolMask, SpellInfo spellInfo = null, sbyte effIndex = -1)
        {
            // only physical spells damage gets reduced by armor
            if ((schoolMask & SpellSchoolMask.Normal) == 0)
                return false;
            if (spellInfo != null)
            {
                // there are spells with no specific attribute but they have "ignores armor" in tooltip
                if (spellInfo.HasAttribute(SpellCustomAttributes.IgnoreArmor))
                    return false;

                if (effIndex != -1)
                {
                    // bleeding effects are not reduced by armor
                    SpellEffectInfo effect = spellInfo.GetEffect(GetMap().GetDifficultyID(), (uint)effIndex);
                    if (effect != null)
                    {
                        if (effect.ApplyAuraName == AuraType.PeriodicDamage || effect.Effect == SpellEffectName.SchoolDamage)
                            if (spellInfo.GetEffectMechanicMask((byte)effIndex).HasAnyFlag((1u << (int)Mechanics.Bleed)))
                                return false;
                    }
                }
            }
            return true;
        }

        public override void BuildValuesUpdate(UpdateType updatetype, ByteBuffer data, Player target)
        {
            if (target == null)
                return;

            ByteBuffer fieldBuffer = new ByteBuffer();

            uint valCount = valuesCount;

            uint[] flags = UpdateFieldFlags.UnitUpdateFieldFlags;
            uint visibleFlag = UpdateFieldFlags.Public;

            if (target == this)
                visibleFlag |= UpdateFieldFlags.Private;
            else if (IsTypeId(TypeId.Player))
                valCount = (int)PlayerFields.End;

            UpdateMask updateMask = new UpdateMask(valCount);

            Player plr = GetCharmerOrOwnerPlayerOrPlayerItself();
            if (GetOwnerGUID() == target.GetGUID())
                visibleFlag |= UpdateFieldFlags.Owner;

            if (HasFlag(ObjectFields.DynamicFlags, UnitDynFlags.SpecialInfo))
                if (HasAuraTypeWithCaster(AuraType.Empathy, target.GetGUID()))
                    visibleFlag |= UpdateFieldFlags.SpecialInfo;

            if (plr != null && plr.IsInSameRaidWith(target))
                visibleFlag |= UpdateFieldFlags.PartyMember;

            Creature creature = ToCreature();
            for (var index = 0; index < valCount; ++index)
            {
                if (Convert.ToBoolean(_fieldNotifyFlags & flags[index]) ||
                    Convert.ToBoolean((flags[index] & visibleFlag) & UpdateFieldFlags.SpecialInfo) ||
                    ((updatetype == UpdateType.Values ? _changesMask.Get(index) : updateValues[index].UnsignedValue != 0) && flags[index].HasAnyFlag(visibleFlag)) ||
                    (index == (int)UnitFields.AuraState && HasFlag(UnitFields.AuraState, AuraStateType.PerCasterAuraStateMask)))
                {
                    updateMask.SetBit(index);

                    if (index == (int)UnitFields.NpcFlags)
                    {
                        uint appendValue = GetUInt32Value(UnitFields.NpcFlags);

                        if (creature != null)
                            if (!target.CanSeeSpellClickOn(creature))
                                appendValue &= ~(uint)NPCFlags.SpellClick;

                        fieldBuffer.WriteUInt32(appendValue);
                    }
                    else if (index == (int)UnitFields.AuraState)
                    {
                        // Check per caster aura states to not enable using a spell in client if specified aura is not by target
                        fieldBuffer.WriteUInt32(BuildAuraStateUpdateForTarget(target));
                    }
                    // FIXME: Some values at server stored in float format but must be sent to client in public uint format
                    else if ((index >= (int)UnitFields.NegStat && index < (int)UnitFields.NegStat + (int)Stats.Max) ||
                        (index >= (int)UnitFields.PosStat && index < (int)UnitFields.PosStat + (int)Stats.Max))
                    {
                        fieldBuffer.WriteUInt32((uint)GetFloatValue(index));
                    }
                    // Gamemasters should be always able to select units - remove not selectable flag
                    else if (index == (int)UnitFields.Flags)
                    {
                        UnitFlags appendValue = (UnitFlags)updateValues[index].UnsignedValue;
                        if (target.IsGameMaster())
                            appendValue &= ~UnitFlags.NotSelectable;

                        fieldBuffer.WriteUInt32((uint)appendValue);
                    }
                    // use modelid_a if not gm, _h if gm for CREATURE_FLAG_EXTRA_TRIGGER creatures
                    else if (index == (int)UnitFields.DisplayId)
                    {
                        uint displayId = updateValues[index].UnsignedValue;
                        if (creature != null)
                        {
                            CreatureTemplate cinfo = creature.GetCreatureTemplate();

                            // this also applies for transform auras
                            SpellInfo transform = Global.SpellMgr.GetSpellInfo(getTransForm());
                            if (transform != null)
                            {
                                foreach (SpellEffectInfo effect in transform.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
                                {
                                    if (effect != null && effect.IsAura(AuraType.Transform))
                                    {
                                        CreatureTemplate transformInfo = Global.ObjectMgr.GetCreatureTemplate((uint)effect.MiscValue);
                                        if (transformInfo != null)
                                        {
                                            cinfo = transformInfo;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Trigger))
                                if (target.IsGameMaster())
                                    displayId = cinfo.GetFirstVisibleModel().CreatureDisplayID;
                        }

                        fieldBuffer.WriteUInt32(displayId);
                    }
                    // hide lootable animation for unallowed players
                    else if (index == (int)ObjectFields.DynamicFlags)
                    {
                        UnitDynFlags dynamicFlags = (UnitDynFlags)updateValues[index].UnsignedValue & ~UnitDynFlags.Tapped;

                        if (creature != null)
                        {
                            if (creature.hasLootRecipient() && !creature.isTappedBy(target))
                                dynamicFlags |= UnitDynFlags.Tapped;

                            if (!target.isAllowedToLoot(creature))
                                dynamicFlags &= ~UnitDynFlags.Lootable;
                        }

                        // unit UNIT_DYNFLAG_TRACK_UNIT should only be sent to caster of SPELL_AURA_MOD_STALKED auras
                        if (dynamicFlags.HasAnyFlag(UnitDynFlags.TrackUnit))
                            if (!HasAuraTypeWithCaster(AuraType.ModStalked, target.GetGUID()))
                                dynamicFlags &= ~UnitDynFlags.TrackUnit;

                        fieldBuffer.WriteUInt32((uint)dynamicFlags);
                    }
                    // FG: pretend that OTHER players in own group are friendly ("blue")
                    else if (index == (int)UnitFields.Bytes2 || index == (int)UnitFields.FactionTemplate)
                    {
                        if (IsControlledByPlayer() && target != this && WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup) && IsInRaidWith(target))
                        {
                            FactionTemplateRecord ft1 = GetFactionTemplateEntry();
                            FactionTemplateRecord ft2 = target.GetFactionTemplateEntry();
                            if (ft1 != null && ft2 != null && !ft1.IsFriendlyTo(ft2))
                            {
                                if (index == (int)UnitFields.Bytes2)
                                    // Allow targetting opposite faction in party when enabled in config
                                    fieldBuffer.WriteUInt32(updateValues[index].UnsignedValue & ((int)UnitBytes2Flags.Sanctuary << 8)); // this flag is at public byte offset 1 !!
                                else
                                    // pretend that all other HOSTILE players have own faction, to allow follow, heal, rezz (trade wont work)
                                    fieldBuffer.WriteUInt32(target.getFaction());
                            }
                            else
                                fieldBuffer.WriteUInt32(updateValues[index].UnsignedValue);
                        }
                        else
                            fieldBuffer.WriteUInt32(updateValues[index].UnsignedValue);
                    }
                    else
                    {
                        // send in current format (float as float, public uint as uint32)
                        fieldBuffer.WriteUInt32(updateValues[index].UnsignedValue);
                    }
                }
            }

            updateMask.AppendToPacket(data);
            data.WriteBytes(fieldBuffer);
        }

        public override void DestroyForPlayer(Player target)
        {
            Battleground bg = target.GetBattleground();
            if (bg != null)
            {
                if (bg.isArena())
                {
                    DestroyArenaUnit destroyArenaUnit = new DestroyArenaUnit();
                    destroyArenaUnit.Guid = GetGUID();
                    target.SendPacket(destroyArenaUnit);
                }
            }

            base.DestroyForPlayer(target);
        }
    }
}
