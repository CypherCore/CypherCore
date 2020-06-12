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
using Framework.Dynamic;
using Framework.GameMath;
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
using Game.Network;

namespace Game.Entities
{
    public partial class Unit : WorldObject
    {
        public Unit(bool isWorldObject) : base(isWorldObject)
        {
            MoveSpline = new MoveSpline();
            i_motionMaster = new MotionMaster(this);
            threatManager = new ThreatManager(this);
            UnitTypeMask = UnitTypeMask.None;
            hostileRefManager = new HostileRefManager(this);
            _spellHistory = new SpellHistory(this);
            m_FollowingRefManager = new RefManager<Unit, ITargetedMovementGeneratorBase>();

            ObjectTypeId = TypeId.Unit;
            ObjectTypeMask |= TypeMask.Unit;
            m_updateFlag.MovementUpdate = true;

            m_modAttackSpeedPct = new float[] { 1.0f, 1.0f, 1.0f };
            m_deathState = DeathState.Alive;

            for (byte i = 0; i < (int)SpellImmunity.Max; ++i)
                m_spellImmune[i] = new MultiMap<uint, uint>();

            for (byte i = 0; i < (int)UnitMods.End; ++i)
            {
                m_auraFlatModifiersGroup[i] = new float[(int)UnitModifierFlatType.End];
                m_auraFlatModifiersGroup[i][(int)UnitModifierFlatType.Base] = 0.0f;
                m_auraFlatModifiersGroup[i][(int)UnitModifierFlatType.BasePCTExcludeCreate] = 100.0f;
                m_auraFlatModifiersGroup[i][(int)UnitModifierFlatType.Total] = 0.0f;

                m_auraPctModifiersGroup[i] = new float[(int)UnitModifierPctType.End];
                m_auraPctModifiersGroup[i][(int)UnitModifierPctType.Base] = 1.0f;
                m_auraPctModifiersGroup[i][(int)UnitModifierPctType.Total] = 1.0f;
            }

            m_auraPctModifiersGroup[(int)UnitMods.DamageOffHand][(int)UnitModifierPctType.Total] = 0.5f;

            foreach (AuraType auraType in Enum.GetValues(typeof(AuraType)))
                m_modAuras[auraType] = new List<AuraEffect>();

            for (byte i = 0; i < (int)WeaponAttackType.Max; ++i)
                m_weaponDamage[i] = new float[] { 1.0f, 2.0f };

            if (IsTypeId(TypeId.Player))
            {
                ModMeleeHitChance = 7.5f;
                ModRangedHitChance = 7.5f;
                ModSpellHitChance = 15.0f;
            }
            BaseSpellCritChance = 5;

            for (byte i = 0; i < (int)SpellSchools.Max; ++i)
                m_threatModifier[i] = 1.0f;

            for (byte i = 0; i < (int)UnitMoveType.Max; ++i)
                m_speed_rate[i] = 1.0f;

            _redirectThreatInfo = new RedirectThreatInfo();
            m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);

            movesplineTimer = new TimeTrackerSmall();

            m_unitData = new UnitData();
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
            MoveSpline = null;
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

            if (CanHaveThreatList() && GetThreatManager().IsNeedUpdateToClient(diff))
                SendThreatListUpdate();

            // update combat timer only for players and pets (only pets with PetAI)
            if (IsInCombat() && (IsTypeId(TypeId.Player) || (IsPet() && IsControlledByPlayer())))
            {
                // Check UNIT_STATE_MELEE_ATTACKING or UNIT_STATE_CHASE (without UNIT_STATE_FOLLOW in this case) so pets can reach far away
                // targets without stopping half way there and running off.
                // These flags are reset after target dies or another command is given.
                if (hostileRefManager.IsEmpty())
                {
                    // m_CombatTimer set at aura start and it will be freeze until aura removing
                    if (combatTimer <= diff)
                        ClearInCombat();
                    else
                        combatTimer -= diff;
                }
            }

            uint att;
            // not implemented before 3.0.2
            if ((att = GetAttackTimer(WeaponAttackType.BaseAttack)) != 0)
                SetAttackTimer(WeaponAttackType.BaseAttack, (diff >= att ? 0 : att - diff));
            if ((att = GetAttackTimer(WeaponAttackType.RangedAttack)) != 0)
                SetAttackTimer(WeaponAttackType.RangedAttack, (diff >= att ? 0 : att - diff));
            if ((att = GetAttackTimer(WeaponAttackType.OffAttack)) != 0)
                SetAttackTimer(WeaponAttackType.OffAttack, (diff >= att ? 0 : att - diff));

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
                if (GetCurrentSpell(i) != null && m_currentSpells[i].GetState() == SpellState.Finished)
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
                    if (!go.IsSpawned())
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
            return IsDisallowedMountForm(GetTransForm(), GetShapeshiftForm(), GetDisplayId());
        }

        public bool IsDisallowedMountForm(uint spellId, ShapeShiftForm form, uint displayId)
        {
            SpellInfo transformSpellInfo = Global.SpellMgr.GetSpellInfo(GetTransForm());
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
                SetActive(true);
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
                SetActive(false);
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
            GetHostileRefManager().DeleteReferences();
        }
        public override void CleanupsBeforeDelete(bool finalCleanup = true)
        {
            CleanupBeforeRemoveFromMap(finalCleanup);

            base.CleanupsBeforeDelete(finalCleanup);
        }

        public void SetTransForm(uint spellid) { m_transform = spellid; }
        public uint GetTransForm() { return m_transform; }

        public Vehicle GetVehicleKit() { return VehicleKit; }
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

        public bool HasNpcFlag(NPCFlags flags) { return (m_unitData.NpcFlags[0] & (uint)flags) != 0; }
        public void AddNpcFlag(NPCFlags flags) { SetUpdateFieldFlagValue(ref m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.NpcFlags, 0), (uint)flags); }
        public void RemoveNpcFlag(NPCFlags flags) { RemoveUpdateFieldFlagValue(ref m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.NpcFlags, 0), (uint)flags); }
        public void SetNpcFlags(NPCFlags flags) { SetUpdateFieldValue(ref m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.NpcFlags, 0), (uint)flags); }
        public bool HasNpcFlag2(NPCFlags2 flags) { return (m_unitData.NpcFlags[1] & (uint)flags) != 0; }
        public void AddNpcFlag2(NPCFlags2 flags) { SetUpdateFieldFlagValue(ref m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.NpcFlags, 1), (uint)flags); }
        public void RemoveNpcFlag2(NPCFlags2 flags) { RemoveUpdateFieldFlagValue(ref m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.NpcFlags, 1), (uint)flags); }
        public void SetNpcFlags2(NPCFlags2 flags) { SetUpdateFieldValue(ref m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.NpcFlags, 1), (uint)flags); }

        public bool IsVendor() { return HasNpcFlag(NPCFlags.Vendor); }
        public bool IsTrainer() { return HasNpcFlag(NPCFlags.Trainer); }
        public bool IsQuestGiver() { return HasNpcFlag(NPCFlags.QuestGiver); }
        public bool IsGossip() { return HasNpcFlag(NPCFlags.Gossip); }
        public bool IsTaxi() { return HasNpcFlag(NPCFlags.FlightMaster); }
        public bool IsGuildMaster() { return HasNpcFlag(NPCFlags.Petitioner); }
        public bool IsBattleMaster() { return HasNpcFlag(NPCFlags.BattleMaster); }
        public bool IsBanker() { return HasNpcFlag(NPCFlags.Banker); }
        public bool IsInnkeeper() { return HasNpcFlag(NPCFlags.Innkeeper); }
        public bool IsSpiritHealer() { return HasNpcFlag(NPCFlags.SpiritHealer); }
        public bool IsSpiritGuide() { return HasNpcFlag(NPCFlags.SpiritGuide); }
        public bool IsTabardDesigner() { return HasNpcFlag(NPCFlags.TabardDesigner); }
        public bool IsAuctioner() { return HasNpcFlag(NPCFlags.Auctioneer); }
        public bool IsArmorer() { return HasNpcFlag(NPCFlags.Repair); }
        public bool IsServiceProvider()
        {
            return HasNpcFlag(NPCFlags.Vendor | NPCFlags.Trainer | NPCFlags.FlightMaster |
                NPCFlags.Petitioner | NPCFlags.BattleMaster | NPCFlags.Banker |
                NPCFlags.Innkeeper | NPCFlags.SpiritHealer |
                NPCFlags.SpiritGuide | NPCFlags.TabardDesigner | NPCFlags.Auctioneer);
        }
        public bool IsSpiritService() { return HasNpcFlag(NPCFlags.SpiritHealer | NPCFlags.SpiritGuide); }
        public bool IsCritter() { return GetCreatureType() == CreatureType.Critter; }
        public bool IsInFlight() { return HasUnitState(UnitState.InFlight); }

        public void SetHoverHeight(float hoverHeight) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.HoverHeight), hoverHeight); }

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
                if (vehicle.GetBase().GetDeathState() == DeathState.JustDied)
                    SetDeathState(DeathState.JustDied);
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
            playOrphanSpellVisual.LaunchDelay = 0.0f;
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
            playOrphanSpellVisual.LaunchDelay = 0.0f;
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

        public void SendPlaySpellVisual(Vector3 targetPosition, float launchDelay, uint spellVisualId, uint missReason, uint reflectStatus, float travelSpeed, bool speedAsTime = false)
        {
            PlaySpellVisual playSpellVisual = new PlaySpellVisual();
            playSpellVisual.Source = GetGUID();
            playSpellVisual.TargetPosition = targetPosition; // exclusive with Target
            playSpellVisual.LaunchDelay = launchDelay;
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

        public bool IsPossessing()
        {
            Unit u = GetCharm();
            if (u != null)
                return u.IsPossessed();
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
                SetCharmGUID(ObjectGuid.Empty);
            }

            return null;
        }
        public bool IsCharmed() { return !GetCharmerGUID().IsEmpty(); }
        public bool IsPossessed() { return HasUnitState(UnitState.Possessed); }

        public HostileRefManager GetHostileRefManager() { return hostileRefManager; }

        public void OnPhaseChange()
        {
            if (!IsInWorld)
                return;

            if (IsTypeId(TypeId.Unit) || !ToPlayer().GetSession().PlayerLogout())
            {
                HostileRefManager refManager = GetHostileRefManager();
                HostileReference refe = refManager.GetFirst();

                while (refe != null)
                {
                    Unit unit = refe.GetSource().GetOwner();
                    if (unit != null)
                    {
                        Creature creature = unit.ToCreature();
                        if (creature != null)
                            refManager.SetOnlineOfflineState(creature, creature.IsInPhase(this));
                    }

                    refe = refe.Next();
                }

                // modify threat lists for new phasemask
                if (!IsTypeId(TypeId.Player))
                {
                    List<HostileReference> threatList = GetThreatManager().GetThreatList();
                    List<HostileReference> offlineThreatList = GetThreatManager().GetOfflineThreatList();

                    // merge expects sorted lists
                    threatList.Sort();
                    offlineThreatList.Sort();
                    threatList.AddRange(offlineThreatList);

                    foreach (var host in threatList)
                    {
                        Unit unit = host.GetTarget();
                        if (unit != null)
                            unit.GetHostileRefManager().SetOnlineOfflineState(ToCreature(), unit.IsInPhase(this));
                    }
                }
            }
        }

        public uint GetModelForForm(ShapeShiftForm form, uint spellId)
        {
            // Hardcoded cases
            switch (spellId)
            {
                case 7090: // Bear Form
                    return 29414;
                case 35200: // Roc Form
                    return 4877;
                default:
                    break;
            }

            Player thisPlayer = ToPlayer();
            if (thisPlayer != null)
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

                byte hairColor = thisPlayer.m_playerData.HairColorID;
                byte skinColor = thisPlayer.m_playerData.SkinID;

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
                                    skinColor = (byte)RandomHelper.URand(0, 9);

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
                                    skinColor = (byte)RandomHelper.URand(0, 20);

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
                                    skinColor = (byte)RandomHelper.URand(0, 8);

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
                                    skinColor = (byte)RandomHelper.URand(0, 20);

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

        public Totem ToTotem() { return IsTotem() ? (this as Totem) : null; }
        public TempSummon ToTempSummon() { return IsSummon() ? (this as TempSummon) : null; }
        public virtual void SetDeathState(DeathState s)
        {
            // Death state needs to be updated before RemoveAllAurasOnDeath() is called, to prevent entering combat
            m_deathState = s;

            if (s != DeathState.Alive && s != DeathState.JustRespawned)
            {
                CombatStop();
                DeleteThreatList();
                GetHostileRefManager().DeleteReferences();

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
                SetEmoteState(Emote.OneshotNone);

                // players in instance don't have ZoneScript, but they have InstanceScript
                ZoneScript zoneScript = GetZoneScript() != null ? GetZoneScript() : GetInstanceScript();
                if (zoneScript != null)
                    zoneScript.OnUnitDeath(this);
            }
            else if (s == DeathState.JustRespawned)
                RemoveUnitFlag(UnitFlags.Skinnable); // clear skinnable for creature and player (at Battleground)
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

        public bool IsMagnet()
        {
            // Grounding Totem
            if (m_unitData.CreatedBySpell == 8177) /// @todo: find a more generic solution
                return true;

            return false;
        }

        public void SetShapeshiftForm(ShapeShiftForm form)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ShapeshiftForm), (byte)form);
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
            SpellInfo autoRepeatSpellInfo = m_currentSpells[CurrentSpellTypes.AutoRepeat].m_spellInfo;

            // check "realtime" interrupts
            // don't cancel spells which are affected by a SPELL_AURA_CAST_WHILE_WALKING effect
            if (((IsTypeId(TypeId.Player) && ToPlayer().IsMoving()) || IsNonMeleeSpellCast(false, false, true, autoRepeatSpellInfo.Id == 75)) &&
                !HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, autoRepeatSpellInfo))
            {
                // cancel wand shoot
                if (autoRepeatSpellInfo.Id != 75)
                    InterruptSpell(CurrentSpellTypes.AutoRepeat);
                m_AutoRepeatFirstCast = true;
                return;
            }

            // apply delay (Auto Shot (spellID 75) not affected)
            if (m_AutoRepeatFirstCast && GetAttackTimer(WeaponAttackType.RangedAttack) < 500 && autoRepeatSpellInfo.Id != 75)
                SetAttackTimer(WeaponAttackType.RangedAttack, 500);
            m_AutoRepeatFirstCast = false;

            // castroutine
            if (IsAttackReady(WeaponAttackType.RangedAttack))
            {
                // Check if able to cast
                SpellCastResult result = m_currentSpells[CurrentSpellTypes.AutoRepeat].CheckCast(true);
                if (result != SpellCastResult.SpellCastOk)
                {
                    if (autoRepeatSpellInfo.Id != 75)
                        InterruptSpell(CurrentSpellTypes.AutoRepeat);
                    else if (GetTypeId() == TypeId.Player)
                        Spell.SendCastResult(ToPlayer(), autoRepeatSpellInfo, m_currentSpells[CurrentSpellTypes.AutoRepeat].m_SpellVisual, m_currentSpells[CurrentSpellTypes.AutoRepeat].m_castId, result);

                    return;
                }

                // we want to shoot
                Spell spell = new Spell(this, autoRepeatSpellInfo, TriggerCastFlags.FullMask);
                spell.Prepare(m_currentSpells[CurrentSpellTypes.AutoRepeat].m_targets);

                // all went good, reset attack
                ResetAttackTimer(WeaponAttackType.RangedAttack);
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
                                    if (pet.GetPetType() == PetType.Hunter) // Hunter pets have focus
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
            FactionTemplateRecord entry = CliDB.FactionTemplateStorage.LookupByKey(GetFaction());
            if (entry == null)
            {
                Player player = ToPlayer();
                if (player != null)
                    Log.outError(LogFilter.Unit, "Player {0} has invalid faction (faction template id) #{1}", player.GetName(), GetFaction());
                else
                {
                    Creature creature = ToCreature();
                    if (creature != null)
                        Log.outError(LogFilter.Unit, "Creature (template id: {0}) has invalid faction (faction template id) #{1}", creature.GetCreatureTemplate().Entry, GetFaction());
                    else
                        Log.outError(LogFilter.Unit, "Unit (name={0}, type={1}) has invalid faction (faction template id) #{2}", GetName(), GetTypeId(), GetFaction());
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

        public void AddFollower(FollowerReference pRef)
        {
            m_FollowingRefManager.InsertFirst(pRef);
        }
        public void RemoveFollower(FollowerReference pRef) { } //nothing to do yet

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

            return m_unitData.VirtualItems[slot].ItemID;
        }

        public ushort GetVirtualItemAppearanceMod(uint slot)
        {
            if (slot >= SharedConst.MaxEquipmentItems)
                return 0;

            return m_unitData.VirtualItems[(int)slot].ItemAppearanceModID;
        }

        public void SetVirtualItem(uint slot, uint itemId, ushort appearanceModId = 0, ushort itemVisual = 0)
        {
            if (slot >= SharedConst.MaxEquipmentItems)
                return;

            var virtualItemField = m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.VirtualItems, (int)slot);
            SetUpdateFieldValue(virtualItemField.ModifyValue((VisibleItem visibleItem) => visibleItem.ItemID), itemId);
            SetUpdateFieldValue(virtualItemField.ModifyValue((VisibleItem visibleItem) => visibleItem.ItemAppearanceModID), appearanceModId);
            SetUpdateFieldValue(virtualItemField.ModifyValue((VisibleItem visibleItem) => visibleItem.ItemVisual), itemVisual);
        }

        //Unit
        public void SetLevel(uint lvl)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Level), lvl);

            Player player = ToPlayer();
            if (player != null)
            {
                if (player.GetGroup())
                    player.SetGroupUpdateFlag(GroupUpdateFlags.Level);

                Global.CharacterCacheStorage.UpdateCharacterLevel(ToPlayer().GetGUID(), (byte)lvl);
            }
        }
        public uint GetLevel() { return m_unitData.Level; }
        public override uint GetLevelForTarget(WorldObject target) { return GetLevel(); }

        public Race GetRace() { return (Race)(byte)m_unitData.Race; }
        public void SetRace(Race race) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Race), (byte)race); }
        public long GetRaceMask() { return 1L << ((int)GetRace() - 1); }
        public Class GetClass() { return (Class)(byte)m_unitData.ClassId; }
        public void SetClass(Class classId) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ClassId), (byte)classId); }
        public uint GetClassMask() { return (uint)(1 << ((int)GetClass() - 1)); }
        public Gender GetGender() { return (Gender)(byte)m_unitData.Sex; }
        public void SetGender(Gender sex) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Sex), (byte)sex); }

        public uint GetDisplayId() { return m_unitData.DisplayID; }
        public virtual void SetDisplayId(uint modelId, float displayScale = 1f)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.DisplayID), modelId);
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.DisplayScale), displayScale);
            // Set Gender by modelId
            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelInfo(modelId);
            if (minfo != null)
                SetGender((Gender)minfo.gender);
        }
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

            var shapeshiftAura = GetAuraEffectsByType(AuraType.ModShapeshift);

            // transform aura was found
            if (handledAura != null)
            {
                handledAura.HandleEffect(this, AuraEffectHandleModes.SendForClient, true);
                return;
            }
            // we've found shapeshift
            else if (!shapeshiftAura.Empty()) // we've found shapeshift
            {
                // only one such aura possible at a time
                uint modelId = GetModelForForm(GetShapeshiftForm(), shapeshiftAura[0].GetId());
                if (modelId != 0)
                {
                    if (!ignorePositiveAurasPreventingMounting || !IsDisallowedMountForm(0, GetShapeshiftForm(), modelId))
                        SetDisplayId(modelId);
                    else
                        SetDisplayId(GetNativeDisplayId());
                    return;
                }
            }
            // no auras found - set modelid to default
            SetDisplayId(GetNativeDisplayId());
        }
        public uint GetNativeDisplayId() { return m_unitData.NativeDisplayID; }
        public void SetNativeDisplayId(uint displayId, float displayScale = 1f)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.NativeDisplayID), displayId);
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.NativeXDisplayScale), displayScale);
        }
        public float GetNativeDisplayScale() { return m_unitData.NativeXDisplayScale; }

        public bool IsMounted()
        {
            return HasUnitFlag(UnitFlags.Mount);
        }
        public uint GetMountDisplayId() { return m_unitData.MountDisplayID; }
        public void SetMountDisplayId(uint mountDisplayId) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.MountDisplayID), mountDisplayId); }

        public virtual Unit GetOwner()
        {
            ObjectGuid ownerid = GetOwnerGUID();
            if (!ownerid.IsEmpty())
                return Global.ObjAccessor.GetUnit(this, ownerid);

            return null;
        }
        public virtual float GetFollowAngle() { return MathFunctions.PiOver2; }

        public ObjectGuid GetOwnerGUID() { return m_unitData.SummonedBy; }
        public void SetOwnerGUID(ObjectGuid owner)
        {
            if (GetOwnerGUID() == owner)
                return;

            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.SummonedBy), owner);
            if (owner.IsEmpty())
                return;

            // Update owner dependent fields
            Player player = Global.ObjAccessor.GetPlayer(this, owner);
            if (player == null || !player.HaveAtClient(this)) // if player cannot see this unit yet, he will receive needed data with create object
                return;

            UpdateData udata = new UpdateData(GetMapId());
            UpdateObject packet;
            BuildValuesUpdateBlockForPlayerWithFlag(udata, UpdateFieldFlag.Owner, player);
            udata.BuildPacket(out packet);
            player.SendPacket(packet);
        }
        public ObjectGuid GetCreatorGUID() { return m_unitData.CreatedBy; }
        public void SetCreatorGUID(ObjectGuid creator) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.CreatedBy), creator); }
        public ObjectGuid GetMinionGUID() { return m_unitData.Summon; }
        public void SetMinionGUID(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Summon), guid); }
        public ObjectGuid GetCharmerGUID() { return m_unitData.CharmedBy; }
        public void SetCharmerGUID(ObjectGuid owner) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.CharmedBy), owner); }
        public ObjectGuid GetCharmGUID() { return m_unitData.Charm; }
        public void SetCharmGUID(ObjectGuid charm) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Charm), charm); }
        public ObjectGuid GetPetGUID() { return m_SummonSlot[0]; }
        public void SetPetGUID(ObjectGuid guid) { m_SummonSlot[0] = guid; }
        public ObjectGuid GetCritterGUID() { return m_unitData.Critter; }
        public void SetCritterGUID(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Critter), guid); }
        public ObjectGuid GetBattlePetCompanionGUID() { return m_unitData.BattlePetCompanionGUID; }
        public void SetBattlePetCompanionGUID(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.BattlePetCompanionGUID), guid); }
        public ObjectGuid GetCharmerOrOwnerGUID()
        {
            return !GetCharmerGUID().IsEmpty() ? GetCharmerGUID() : GetOwnerGUID();
        }
        public ObjectGuid GetCharmerOrOwnerOrOwnGUID()
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

        public bool HasUnitFlag(UnitFlags flags) { return (m_unitData.Flags & (uint)flags) != 0; }
        public void AddUnitFlag(UnitFlags flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Flags), (uint)flags); }
        public void RemoveUnitFlag(UnitFlags flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Flags), (uint)flags); }
        public void SetUnitFlags(UnitFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Flags), (uint)flags); }
        public bool HasUnitFlag2(UnitFlags2 flags) { return (m_unitData.Flags2 & (uint)flags) != 0; }
        public void AddUnitFlag2(UnitFlags2 flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Flags2), (uint)flags); }
        public void RemoveUnitFlag2(UnitFlags2 flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Flags2), (uint)flags); }
        public void SetUnitFlags2(UnitFlags2 flags) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Flags2), (uint)flags); }
        public bool HasUnitFlag3(UnitFlags3 flags) { return (m_unitData.Flags3 & (uint)flags) != 0; }
        public void AddUnitFlag3(UnitFlags3 flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Flags3), (uint)flags); }
        public void RemoveUnitFlag3(UnitFlags3 flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Flags3), (uint)flags); }
        public void SetUnitFlags3(UnitFlags3 flags) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Flags3), (uint)flags); }

        public void SetCreatedBySpell(uint spellId) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.CreatedBySpell), spellId); }

        public Emote GetEmoteState() { return (Emote)(int)m_unitData.EmoteState; }
        public void SetEmoteState(Emote emote) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.EmoteState), (int)emote); }

        public SheathState GetSheath() { return (SheathState)(byte)m_unitData.SheatheState; }
        public void SetSheath(SheathState sheathed) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.SheatheState), (byte)sheathed); }

        public uint GetCombatTimer() { return combatTimer; }
        public UnitPVPStateFlags GetPvpFlags() { return (UnitPVPStateFlags)(byte)m_unitData.PvpFlags; }
        public bool HasPvpFlag(UnitPVPStateFlags flags) { return (m_unitData.PvpFlags & (uint)flags) != 0; }
        public void AddPvpFlag(UnitPVPStateFlags flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PvpFlags), (byte)flags); }
        public void RemovePvpFlag(UnitPVPStateFlags flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PvpFlags), (byte)flags); }
        public void SetPvpFlags(UnitPVPStateFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PvpFlags), (byte)flags); }
        public bool IsInSanctuary() { return HasPvpFlag(UnitPVPStateFlags.Sanctuary); }
        public bool IsPvP() { return HasPvpFlag(UnitPVPStateFlags.PvP); }
        public bool IsFFAPvP() { return HasPvpFlag(UnitPVPStateFlags.FFAPvp); }

        public UnitPetFlags GetPetFlags()
        {
            return (UnitPetFlags)(byte)m_unitData.PetFlags;
        }
        public bool HasPetFlag(UnitPetFlags flags) { return (m_unitData.PetFlags & (byte)flags) != 0; }
        public void AddPetFlag(UnitPetFlags flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PetFlags), (byte)flags); }
        public void RemovePetFlag(UnitPetFlags flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PetFlags), (byte)flags); }
        public void SetPetFlags(UnitPetFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PetFlags), (byte)flags); }

        public void SetPetNumberForClient(uint petNumber) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PetNumber), petNumber); }
        public void SetPetNameTimestamp(uint timestamp) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PetNameTimestamp), timestamp); }

        public ShapeShiftForm GetShapeshiftForm() { return (ShapeShiftForm)(byte)m_unitData.ShapeshiftForm; }
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

        public bool HasUnitTypeMask(UnitTypeMask mask) { return Convert.ToBoolean(mask & UnitTypeMask); }
        public void AddUnitTypeMask(UnitTypeMask mask) { UnitTypeMask |= mask; }

        public bool IsAlive() { return m_deathState == DeathState.Alive; }
        public bool IsDying() { return m_deathState == DeathState.JustDied; }
        public bool IsDead() { return (m_deathState == DeathState.Dead || m_deathState == DeathState.Corpse); }
        public bool IsSummon() { return UnitTypeMask.HasAnyFlag(UnitTypeMask.Summon); }
        public bool IsGuardian() { return UnitTypeMask.HasAnyFlag(UnitTypeMask.Guardian); }
        public bool IsPet() { return UnitTypeMask.HasAnyFlag(UnitTypeMask.Pet); }
        public bool IsHunterPet() { return UnitTypeMask.HasAnyFlag(UnitTypeMask.HunterPet); }
        public bool IsTotem() { return UnitTypeMask.HasAnyFlag(UnitTypeMask.Totem); }
        public bool IsVehicle() { return UnitTypeMask.HasAnyFlag(UnitTypeMask.Vehicle); }

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

            if (HasUnitFlag(UnitFlags.PvpAttackable))
            {
                if (target.HasUnitFlag(UnitFlags.PvpAttackable))
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
                            if (!selfPlayerOwner.HasUnitFlag2(UnitFlags2.IgnoreReputation))
                            {
                                var targetFactionEntry = CliDB.FactionStorage.LookupByKey(targetFactionTemplateEntry.Faction);
                                if (targetFactionEntry != null)
                                {
                                    if (targetFactionEntry.CanHaveReputation())
                                    {
                                        // check contested flags
                                        if (Convert.ToBoolean(targetFactionTemplateEntry.Flags & (uint)FactionTemplateFlags.ContestedGuard)
                                            && selfPlayerOwner.HasPlayerFlag(PlayerFlags.ContestedPVP))
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
                    && targetPlayerOwner.HasPlayerFlag(PlayerFlags.ContestedPVP))
                    return ReputationRank.Hostile;
                ReputationRank repRank = targetPlayerOwner.GetReputationMgr().GetForcedRankIfAny(factionTemplateEntry);
                if (repRank != ReputationRank.None)
                    return repRank;
                if (!target.HasUnitFlag2(UnitFlags2.IgnoreReputation))
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

        public uint GetFaction() { return m_unitData.FactionTemplate; }
        public void SetFaction(uint faction) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.FactionTemplate), faction); }

        public void RestoreFaction()
        {
            if (IsTypeId(TypeId.Player))
                ToPlayer().SetFactionForRace(GetRace());
            else
            {
                if (HasUnitTypeMask(UnitTypeMask.Minion))
                {
                    Unit owner = GetOwner();
                    if (owner)
                    {
                        SetFaction(owner.GetFaction());
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

            return u1.GetTypeId() == TypeId.Unit && u2.GetTypeId() == TypeId.Unit && u1.GetFaction() == u2.GetFaction();
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

            // else u1.GetTypeId() == u2.GetTypeId() == TYPEID_UNIT
            return u1.GetFaction() == u2.GetFaction();
        }

        public UnitStandStateType GetStandState() { return (UnitStandStateType)(byte)m_unitData.StandState; }
        public void AddVisFlags(UnitVisFlags flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.VisFlags), (byte)flags); }
        public void RemoveVisFlags(UnitVisFlags flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.VisFlags), (byte)flags); }
        public void SetVisFlags(UnitVisFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.VisFlags), (byte)flags); }

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
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.StandState), (byte)state);

            if (IsStandState())
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.NotSeated);

            if (IsTypeId(TypeId.Player))
            {
                StandStateUpdate packet = new StandStateUpdate(state, animKitId);
                ToPlayer().SendPacket(packet);
            }
        }

        public void SetAnimTier(UnitBytes1Flags animTier, bool notifyClient)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.AnimTier), (byte)animTier);

            if (notifyClient)
            {
                SetAnimTier setAnimTier = new SetAnimTier();
                setAnimTier.Unit = GetGUID();
                setAnimTier.Tier = (int)animTier;
                SendMessageToSet(setAnimTier, true);
            }
        }

        public uint GetChannelSpellId() { return ((UnitChannel)m_unitData.ChannelData).SpellID; }
        public void SetChannelSpellId(uint channelSpellId)
        {
            SetUpdateFieldValue(ref m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ChannelData)._value.SpellID, channelSpellId);
        }
        public uint GetChannelSpellXSpellVisualId() { return ((UnitChannel)m_unitData.ChannelData).SpellXSpellVisualID; }
        public void SetChannelSpellXSpellVisualId(uint channelSpellXSpellVisualId)
        {
            UnitChannel unitChannel = m_unitData.ModifyValue(m_unitData.ChannelData);
            SetUpdateFieldValue(ref unitChannel.SpellXSpellVisualID, channelSpellXSpellVisualId);
        }
        public void AddChannelObject(ObjectGuid guid) { AddDynamicUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ChannelObjects), guid); }
        public void SetChannelObject(int slot, ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ChannelObjects, slot), guid); }
        public void ClearChannelObjects() { ClearDynamicUpdateFieldValues(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ChannelObjects)); }

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

        public override UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
        {
            UpdateFieldFlag flags = UpdateFieldFlag.None;
            if (target == this || GetOwnerGUID() == target.GetGUID())
                flags |= UpdateFieldFlag.Owner;

            if (HasDynamicFlag(UnitDynFlags.SpecialInfo))
                if (HasAuraTypeWithCaster(AuraType.Empathy, target.GetGUID()))
                    flags |= UpdateFieldFlag.Empath;

            return flags;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new WorldPacket();

            buffer.WriteUInt8((byte)flags);
            m_objectData.WriteCreate(buffer, flags, this, target);
            m_unitData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new WorldPacket();

            buffer.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.Unit))
                m_unitData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            UpdateMask valuesMask = new UpdateMask(14);
            valuesMask.Set((int)TypeId.Unit);

            WorldPacket buffer = new WorldPacket();

            UpdateMask mask = new UpdateMask(191);
            m_unitData.AppendAllowedFieldsMaskForFlag(mask, flags);
            m_unitData.WriteUpdate(buffer, mask, true, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteUInt32(valuesMask.GetBlock(0));
            data.WriteBytes(buffer);
        }

        public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedUnitMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new UpdateMask((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            m_unitData.FilterDisallowedFieldsMaskForFlag(requestedUnitMask, flags);
            if (requestedUnitMask.IsAnySet())
                valuesMask.Set((int)TypeId.Unit);

            WorldPacket buffer = new WorldPacket();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Unit])
                m_unitData.WriteUpdate(buffer, requestedUnitMask, true, this, target);

            WorldPacket buffer1 = new WorldPacket();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_unitData);
            base.ClearUpdateMask(remove);
        }

        public override void DestroyForPlayer(Player target)
        {
            Battleground bg = target.GetBattleground();
            if (bg != null)
            {
                if (bg.IsArena())
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
