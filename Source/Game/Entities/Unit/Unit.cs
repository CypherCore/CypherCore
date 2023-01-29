// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.AI;
using Game.BattleGrounds;
using Game.Chat;
using Game.Combat;
using Game.DataStorage;
using Game.Groups;
using Game.Maps;
using Game.Movement;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IUnit;
using Game.Spells;

namespace Game.Entities
{
    public partial class Unit : WorldObject
    {
        public Unit(bool isWorldObject) : base(isWorldObject)
        {
            MoveSpline = new MoveSpline();
            _iMotionMaster = new MotionMaster(this);
            _combatManager = new CombatManager(this);
            _threatManager = new ThreatManager(this);
            _spellHistory = new SpellHistory(this);

            ObjectTypeId = TypeId.Unit;
            ObjectTypeMask |= TypeMask.Unit;
            UpdateFlag.MovementUpdate = true;

            ModAttackSpeedPct = new float[]
                                 {
                                     1.0f, 1.0f, 1.0f
                                 };

            DeathState = DeathState.Alive;

            for (byte i = 0; i < (int)SpellImmunity.Max; ++i)
                _spellImmune[i] = new MultiMap<uint, uint>();

            for (byte i = 0; i < (int)UnitMods.End; ++i)
            {
                AuraFlatModifiersGroup[i] = new float[(int)UnitModifierFlatType.End];
                AuraFlatModifiersGroup[i][(int)UnitModifierFlatType.Base] = 0.0f;
                AuraFlatModifiersGroup[i][(int)UnitModifierFlatType.BasePCTExcludeCreate] = 100.0f;
                AuraFlatModifiersGroup[i][(int)UnitModifierFlatType.Total] = 0.0f;

                AuraPctModifiersGroup[i] = new float[(int)UnitModifierPctType.End];
                AuraPctModifiersGroup[i][(int)UnitModifierPctType.Base] = 1.0f;
                AuraPctModifiersGroup[i][(int)UnitModifierPctType.Total] = 1.0f;
            }

            AuraPctModifiersGroup[(int)UnitMods.DamageOffHand][(int)UnitModifierPctType.Total] = 0.5f;

            foreach (AuraType auraType in Enum.GetValues(typeof(AuraType)))
                _modAuras[auraType] = new List<AuraEffect>();

            for (byte i = 0; i < (int)WeaponAttackType.Max; ++i)
                WeaponDamage[i] = new float[]
                                   {
                                       1.0f, 2.0f
                                   };

            if (IsTypeId(TypeId.Player))
            {
                ModMeleeHitChance = 7.5f;
                ModRangedHitChance = 7.5f;
                ModSpellHitChance = 15.0f;
            }

            BaseSpellCritChance = 5;

            for (byte i = 0; i < (int)UnitMoveType.Max; ++i)
                SpeedRate[i] = 1.0f;

            ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);

            _splineSyncTimer = new TimeTracker();

            UnitData = new UnitData();
        }

        public override void Dispose()
        {
            // set current spells as deletable
            for (CurrentSpellTypes i = 0; i < CurrentSpellTypes.Max; ++i)
                if (CurrentSpells.ContainsKey(i))
                    if (CurrentSpells[i] != null)
                    {
                        CurrentSpells[i].SetReferencedFromCurrent(false);
                        CurrentSpells[i] = null;
                    }

            Events.KillAllEvents(true);

            _DeleteRemovedAuras();

            //i_motionMaster = null;
            _charmInfo = null;
            MoveSpline = null;
            _spellHistory = null;

            /*ASSERT(!_duringRemoveFromWorld);
			ASSERT(!_attacking);
			ASSERT(_attackers.empty());
			ASSERT(_sharedVision.empty());
			ASSERT(Controlled.empty());
			ASSERT(_appliedAuras.empty());
			ASSERT(_ownedAuras.empty());
			ASSERT(_removedAuras.empty());
			ASSERT(_gameObj.empty());
			ASSERT(DynObj.empty());*/

            base.Dispose();
        }

        public override void Update(uint diff)
        {
            // WARNING! Order of execution here is important, do not change.
            // Spells must be processed with event system BEFORE they go to _UpdateSpells.
            Events.Update(diff);

            if (!IsInWorld)
                return;

            _UpdateSpells(diff);

            base.Update(diff);

            // If this is set during update SetCantProc(false) call is missing somewhere in the code
            // Having this would prevent spells from being proced, so let's crash
            Cypher.Assert(ProcDeep == 0);

            _combatManager.Update(diff);

            _lastDamagedTargetGuid = ObjectGuid.Empty;

            if (_lastExtraAttackSpell != 0)
            {
                while (!_extraAttacksTargets.Empty())
                {
                    var (targetGuid, count) = _extraAttacksTargets.FirstOrDefault();
                    _extraAttacksTargets.Remove(targetGuid);

                    Unit victim = Global.ObjAccessor.GetUnit(this, targetGuid);

                    if (victim != null)
                        HandleProcExtraAttackFor(victim, count);
                }

                _lastExtraAttackSpell = 0;
            }

            bool spellPausesCombatTimer(CurrentSpellTypes type)
            {
                return GetCurrentSpell(type) != null && GetCurrentSpell(type).GetSpellInfo().HasAttribute(SpellAttr6.DelayCombatTimerDuringCast);
            }

            if (!spellPausesCombatTimer(CurrentSpellTypes.Generic) &&
                !spellPausesCombatTimer(CurrentSpellTypes.Channeled))
            {
                uint base_att = GetAttackTimer(WeaponAttackType.BaseAttack);

                if (base_att != 0)
                    SetAttackTimer(WeaponAttackType.BaseAttack, (diff >= base_att ? 0 : base_att - diff));

                uint ranged_att = GetAttackTimer(WeaponAttackType.RangedAttack);

                if (ranged_att != 0)
                    SetAttackTimer(WeaponAttackType.RangedAttack, (diff >= ranged_att ? 0 : ranged_att - diff));

                uint off_att = GetAttackTimer(WeaponAttackType.OffAttack);

                if (off_att != 0)
                    SetAttackTimer(WeaponAttackType.OffAttack, (diff >= off_att ? 0 : off_att - diff));
            }

            // update abilities available only for fraction of Time
            UpdateReactives(diff);

            if (IsAlive())
            {
                ModifyAuraState(AuraStateType.Wounded20Percent, HealthBelowPct(20));
                ModifyAuraState(AuraStateType.Wounded25Percent, HealthBelowPct(25));
                ModifyAuraState(AuraStateType.Wounded35Percent, HealthBelowPct(35));
                ModifyAuraState(AuraStateType.WoundHealth20_80, HealthBelowPct(20) || HealthAbovePct(80));
                ModifyAuraState(AuraStateType.Healthy75Percent, HealthAbovePct(75));
                ModifyAuraState(AuraStateType.WoundHealth35_80, HealthBelowPct(35) || HealthAbovePct(80));
            }

            UpdateSplineMovement(diff);
            GetMotionMaster().Update(diff);

            // Wait with the aura interrupts until we have updated our movement generators and position
            if (IsPlayer())
                InterruptMovementBasedAuras();
            else if (!MoveSpline.Finalized())
                InterruptMovementBasedAuras();

            // All position info based actions have been executed, reset info
            _positionUpdateInfo.Reset();

            if (HasScheduledAIChange() &&
                (!IsPlayer() || (IsCharmed() && GetCharmerGUID().IsCreature())))
                UpdateCharmAI();

            RefreshAI();
        }

        public void HandleEmoteCommand(Emote emoteId, Player target = null, uint[] spellVisualKitIds = null, int sequenceVariation = 0)
        {
            EmoteMessage packet = new();
            packet.Guid = GetGUID();
            packet.EmoteID = (uint)emoteId;

            var emotesEntry = CliDB.EmotesStorage.LookupByKey(emoteId);

            if (emotesEntry != null &&
                spellVisualKitIds != null)
                if (emotesEntry.AnimId == (uint)Anim.MountSpecial ||
                    emotesEntry.AnimId == (uint)Anim.MountSelfSpecial)
                    packet.SpellVisualKitIDs.AddRange(spellVisualKitIds);

            packet.SequenceVariation = sequenceVariation;

            if (target != null)
                target.SendPacket(packet);
            else
                SendMessageToSet(packet, true);
        }

        public void SendDurabilityLoss(Player receiver, uint percent)
        {
            DurabilityDamageDeath packet = new();
            packet.Percent = percent;
            receiver.SendPacket(packet);
        }

        public bool IsInDisallowedMountForm()
        {
            return IsDisallowedMountForm(GetTransformSpell(), GetShapeshiftForm(), GetDisplayId());
        }

        public bool IsDisallowedMountForm(uint spellId, ShapeShiftForm form, uint displayId)
        {
            SpellInfo transformSpellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetMap().GetDifficultyID());

            if (transformSpellInfo != null)
                if (transformSpellInfo.HasAttribute(SpellAttr0.AllowWhileMounted))
                    return false;

            if (form != 0)
            {
                SpellShapeshiftFormRecord shapeshift = CliDB.SpellShapeshiftFormStorage.LookupByKey(form);

                if (shapeshift == null)
                    return true;

                if (!shapeshift.Flags.HasAnyFlag(SpellShapeshiftFormFlags.Stance))
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

            if (model != null &&
                !model.GetFlags().HasFlag(CreatureModelDataFlags.CanMountWhileTransformedAsThis))
                if (race != null &&
                    !race.GetFlags().HasFlag(ChrRacesFlag.CanMount))
                    return true;

            return false;
        }

        public void SendClearTarget()
        {
            BreakTarget breakTarget = new();
            breakTarget.UnitGUID = GetGUID();
            SendMessageToSet(breakTarget, false);
        }

        public virtual bool IsLoading()
        {
            return false;
        }

        public bool IsDuringRemoveFromWorld()
        {
            return _duringRemoveFromWorld;
        }

        //SharedVision
        public bool HasSharedVision()
        {
            return !_sharedVision.Empty();
        }

        public List<Player> GetSharedVisionList()
        {
            return _sharedVision;
        }

        public void AddPlayerToVision(Player player)
        {
            if (_sharedVision.Empty())
            {
                SetActive(true);
                SetWorldObject(true);
            }

            _sharedVision.Add(player);
        }

        // only called in Player.SetSeer
        public void RemovePlayerFromVision(Player player)
        {
            _sharedVision.Remove(player);

            if (_sharedVision.Empty())
            {
                SetActive(false);
                SetWorldObject(false);
            }
        }

        public virtual void Talk(string text, ChatMsg msgType, Language language, float textRange, WorldObject target)
        {
            var builder = new CustomChatTextBuilder(this, msgType, text, language, target);
            var localizer = new LocalizedDo(builder);
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

            Locale locale = target.GetSession().GetSessionDbLocaleIndex();
            ChatPkt data = new();
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
            var localizer = new LocalizedDo(builder);
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

            Locale locale = target.GetSession().GetSessionDbLocaleIndex();
            ChatPkt data = new();
            data.Initialize(isBossWhisper ? ChatMsg.RaidBossWhisper : ChatMsg.MonsterWhisper, Language.Universal, this, target, Global.DB2Mgr.GetBroadcastTextValue(bct, locale, GetGender()), 0, "", locale);
            target.SendPacket(data);
        }

        public override void UpdateObjectVisibility(bool forced = true)
        {
            if (!forced)
            {
                AddToNotify(NotifyFlags.VisibilityChanged);
            }
            else
            {
                base.UpdateObjectVisibility(true);
                // call MoveInLineOfSight for nearby creatures
                AIRelocationNotifier notifier = new(this);
                Cell.VisitAllObjects(this, notifier, GetVisibilityRange());
            }
        }

        public override void AddToWorld()
        {
            base.AddToWorld();
            _iMotionMaster.AddToWorld();

            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnterWorld);
        }

        public override void RemoveFromWorld()
        {
            // cleanup

            if (IsInWorld)
            {
                _duringRemoveFromWorld = true;
                UnitAI ai = GetAI();

                ai?.OnDespawn();

                if (IsVehicle())
                    RemoveVehicleKit(true);

                RemoveCharmAuras();
                RemoveAurasByType(AuraType.BindSight);
                RemoveNotOwnSingleTargetAuras();
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.LeaveWorld);

                RemoveAllGameObjects();
                RemoveAllDynObjects();
                RemoveAllAreaTriggers();

                ExitVehicle(); // Remove applied Auras with SPELL_AURA_CONTROL_VEHICLE
                UnsummonAllTotems();
                RemoveAllControlled();

                RemoveAreaAurasDueToLeaveWorld();

                RemoveAllFollowers();

                if (IsCharmed())
                    RemoveCharmedBy(null);

                Cypher.Assert(GetCharmedGUID().IsEmpty(), $"Unit {GetEntry()} has charmed Guid when removed from world");
                Cypher.Assert(GetCharmerGUID().IsEmpty(), $"Unit {GetEntry()} has charmer Guid when removed from world");

                Unit owner = GetOwner();

                if (owner != null)
                    if (owner.Controlled.Contains(this))
                        Log.outFatal(LogFilter.Unit, "Unit {0} is in controlled list of {1} when removed from world", GetEntry(), owner.GetEntry());

                base.RemoveFromWorld();
                _duringRemoveFromWorld = false;
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
                _cleanupDone = true;

            Events.KillAllEvents(false); // non-delatable (currently casted spells) will not deleted now but it will deleted at call in Map.RemoveAllObjectsInRemoveList
            CombatStop();
        }

        public override void CleanupsBeforeDelete(bool finalCleanup = true)
        {
            CleanupBeforeRemoveFromMap(finalCleanup);

            base.CleanupsBeforeDelete(finalCleanup);
        }

        public void SetTransformSpell(uint spellid)
        {
            _transformSpell = spellid;
        }

        public uint GetTransformSpell()
        {
            return _transformSpell;
        }

        public Vehicle GetVehicleKit()
        {
            return VehicleKit;
        }

        public Vehicle GetVehicle()
        {
            return Vehicle;
        }

        public void SetVehicle(Vehicle vehicle)
        {
            Vehicle = vehicle;
        }

        public Unit GetVehicleBase()
        {
            return Vehicle?.GetBase();
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
            DynObj.Add(dynObj);

            if (IsTypeId(TypeId.Unit) &&
                IsAIEnabled())
                ToCreature().GetAI().JustRegisteredDynObject(dynObj);
        }

        public void _UnregisterDynObject(DynamicObject dynObj)
        {
            DynObj.Remove(dynObj);

            if (IsTypeId(TypeId.Unit) &&
                IsAIEnabled())
                ToCreature().GetAI().JustUnregisteredDynObject(dynObj);
        }

        public DynamicObject GetDynObject(uint spellId)
        {
            return GetDynObjects(spellId).FirstOrDefault();
        }

        public void RemoveDynObject(uint spellId)
        {
            for (var i = 0; i < DynObj.Count; ++i)
            {
                var dynObj = DynObj[i];

                if (dynObj.GetSpellId() == spellId)
                    dynObj.Remove();
            }
        }

        public void RemoveAllDynObjects()
        {
            while (!DynObj.Empty())
                DynObj.First().Remove();
        }

        public GameObject GetGameObject(uint spellId)
        {
            return GetGameObjects(spellId).FirstOrDefault();
        }

        public void AddGameObject(GameObject gameObj)
        {
            if (gameObj == null ||
                !gameObj.GetOwnerGUID().IsEmpty())
                return;

            GameObj.Add(gameObj);
            gameObj.SetOwnerGUID(GetGUID());

            if (gameObj.GetSpellId() != 0)
            {
                SpellInfo createBySpell = Global.SpellMgr.GetSpellInfo(gameObj.GetSpellId(), GetMap().GetDifficultyID());

                // Need disable spell use for owner
                if (createBySpell != null &&
                    createBySpell.IsCooldownStartedOnEvent())
                    // note: Item based cooldowns and cooldown spell mods with charges ignored (unknown existing cases)
                    GetSpellHistory().StartCooldown(createBySpell, 0, null, true);
            }

            if (IsTypeId(TypeId.Unit) &&
                ToCreature().IsAIEnabled())
                ToCreature().GetAI().JustSummonedGameobject(gameObj);
        }

        public void RemoveGameObject(GameObject gameObj, bool del)
        {
            if (gameObj == null ||
                gameObj.GetOwnerGUID() != GetGUID())
                return;

            gameObj.SetOwnerGUID(ObjectGuid.Empty);

            for (byte i = 0; i < SharedConst.MaxGameObjectSlot; ++i)
                if (ObjectSlot[i] == gameObj.GetGUID())
                {
                    ObjectSlot[i].Clear();

                    break;
                }

            // GO created by some spell
            uint spellid = gameObj.GetSpellId();

            if (spellid != 0)
            {
                RemoveAurasDueToSpell(spellid);

                SpellInfo createBySpell = Global.SpellMgr.GetSpellInfo(spellid, GetMap().GetDifficultyID());

                // Need activate spell use for owner
                if (createBySpell != null &&
                    createBySpell.IsCooldownStartedOnEvent())
                    // note: Item based cooldowns and cooldown spell mods with charges ignored (unknown existing cases)
                    GetSpellHistory().SendCooldownEvent(createBySpell);
            }

            GameObj.Remove(gameObj);

            if (IsTypeId(TypeId.Unit) &&
                ToCreature().IsAIEnabled())
                ToCreature().GetAI().SummonedGameobjectDespawn(gameObj);

            if (del)
            {
                gameObj.SetRespawnTime(0);
                gameObj.Delete();
            }
        }

        public void RemoveGameObject(uint spellid, bool del)
        {
            if (GameObj.Empty())
                return;

            for (var i = 0; i < GameObj.Count; ++i)
            {
                var obj = GameObj[i];

                if (spellid == 0 ||
                    obj.GetSpellId() == spellid)
                {
                    obj.SetOwnerGUID(ObjectGuid.Empty);

                    if (del)
                    {
                        obj.SetRespawnTime(0);
                        obj.Delete();
                    }

                    GameObj.Remove(obj);
                }
            }
        }

        public void RemoveAllGameObjects()
        {
            // remove references to unit
            while (!GameObj.Empty())
            {
                var obj = GameObj.First();
                obj.SetOwnerGUID(ObjectGuid.Empty);
                obj.SetRespawnTime(0);
                obj.Delete();
                GameObj.Remove(obj);
            }
        }

        public void _RegisterAreaTrigger(AreaTrigger areaTrigger)
        {
            _areaTrigger.Add(areaTrigger);

            if (IsTypeId(TypeId.Unit) &&
                IsAIEnabled())
                ToCreature().GetAI().JustRegisteredAreaTrigger(areaTrigger);
        }

        public void _UnregisterAreaTrigger(AreaTrigger areaTrigger)
        {
            _areaTrigger.Remove(areaTrigger);

            if (IsTypeId(TypeId.Unit) &&
                IsAIEnabled())
                ToCreature().GetAI().JustUnregisteredAreaTrigger(areaTrigger);
        }

        public AreaTrigger GetAreaTrigger(uint spellId)
        {
            List<AreaTrigger> areaTriggers = GetAreaTriggers(spellId);

            return areaTriggers.Empty() ? null : areaTriggers[0];
        }

        public List<AreaTrigger> GetAreaTriggers(uint spellId)
        {
            return _areaTrigger.Where(trigger => trigger.GetSpellId() == spellId).ToList();
        }

        public void RemoveAreaTrigger(uint spellId)
        {
            if (_areaTrigger.Empty())
                return;

            for (var i = 0; i < _areaTrigger.Count; ++i)
            {
                AreaTrigger areaTrigger = _areaTrigger[i];

                if (areaTrigger.GetSpellId() == spellId)
                    areaTrigger.Remove();
            }
        }

        public void RemoveAreaTrigger(AuraEffect aurEff)
        {
            if (_areaTrigger.Empty())
                return;

            foreach (AreaTrigger areaTrigger in _areaTrigger)
                if (areaTrigger.GetAuraEffect() == aurEff)
                {
                    areaTrigger.Remove();

                    break; // There can only be one AreaTrigger per AuraEffect
                }
        }

        public void RemoveAllAreaTriggers()
        {
            while (!_areaTrigger.Empty())
                _areaTrigger[0].Remove();
        }

        public NPCFlags GetNpcFlags()
        {
            return (NPCFlags)UnitData.NpcFlags[0];
        }

        public bool HasNpcFlag(NPCFlags flags)
        {
            return (UnitData.NpcFlags[0] & (uint)flags) != 0;
        }

        public void SetNpcFlag(NPCFlags flags)
        {
            SetUpdateFieldFlagValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 0), (uint)flags);
        }

        public void RemoveNpcFlag(NPCFlags flags)
        {
            RemoveUpdateFieldFlagValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 0), (uint)flags);
        }

        public void ReplaceAllNpcFlags(NPCFlags flags)
        {
            SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 0), (uint)flags);
        }

        public NPCFlags2 GetNpcFlags2()
        {
            return (NPCFlags2)UnitData.NpcFlags[1];
        }

        public bool HasNpcFlag2(NPCFlags2 flags)
        {
            return (UnitData.NpcFlags[1] & (uint)flags) != 0;
        }

        public void SetNpcFlag2(NPCFlags2 flags)
        {
            SetUpdateFieldFlagValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 1), (uint)flags);
        }

        public void RemoveNpcFlag2(NPCFlags2 flags)
        {
            RemoveUpdateFieldFlagValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 1), (uint)flags);
        }

        public void ReplaceAllNpcFlags2(NPCFlags2 flags)
        {
            SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 1), (uint)flags);
        }

        public bool IsVendor()
        {
            return HasNpcFlag(NPCFlags.Vendor);
        }

        public bool IsTrainer()
        {
            return HasNpcFlag(NPCFlags.Trainer);
        }

        public bool IsQuestGiver()
        {
            return HasNpcFlag(NPCFlags.QuestGiver);
        }

        public bool IsGossip()
        {
            return HasNpcFlag(NPCFlags.Gossip);
        }

        public bool IsTaxi()
        {
            return HasNpcFlag(NPCFlags.FlightMaster);
        }

        public bool IsGuildMaster()
        {
            return HasNpcFlag(NPCFlags.Petitioner);
        }

        public bool IsBattleMaster()
        {
            return HasNpcFlag(NPCFlags.BattleMaster);
        }

        public bool IsBanker()
        {
            return HasNpcFlag(NPCFlags.Banker);
        }

        public bool IsInnkeeper()
        {
            return HasNpcFlag(NPCFlags.Innkeeper);
        }

        public bool IsSpiritHealer()
        {
            return HasNpcFlag(NPCFlags.SpiritHealer);
        }

        public bool IsSpiritGuide()
        {
            return HasNpcFlag(NPCFlags.SpiritGuide);
        }

        public bool IsTabardDesigner()
        {
            return HasNpcFlag(NPCFlags.TabardDesigner);
        }

        public bool IsAuctioner()
        {
            return HasNpcFlag(NPCFlags.Auctioneer);
        }

        public bool IsArmorer()
        {
            return HasNpcFlag(NPCFlags.Repair);
        }

        public bool IsWildBattlePet()
        {
            return HasNpcFlag(NPCFlags.WildBattlePet);
        }

        public bool IsServiceProvider()
        {
            return HasNpcFlag(NPCFlags.Vendor |
                              NPCFlags.Trainer |
                              NPCFlags.FlightMaster |
                              NPCFlags.Petitioner |
                              NPCFlags.BattleMaster |
                              NPCFlags.Banker |
                              NPCFlags.Innkeeper |
                              NPCFlags.SpiritHealer |
                              NPCFlags.SpiritGuide |
                              NPCFlags.TabardDesigner |
                              NPCFlags.Auctioneer);
        }

        public bool IsSpiritService()
        {
            return HasNpcFlag(NPCFlags.SpiritHealer | NPCFlags.SpiritGuide);
        }

        public bool IsCritter()
        {
            return GetCreatureType() == CreatureType.Critter;
        }

        public bool IsInFlight()
        {
            return HasUnitState(UnitState.InFlight);
        }

        public bool IsContestedGuard()
        {
            var entry = GetFactionTemplateEntry();

            if (entry != null)
                return entry.IsContestedGuardFaction();

            return false;
        }

        public void SetHoverHeight(float hoverHeight)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.HoverHeight), hoverHeight);
        }

        public override float GetCollisionHeight()
        {
            float scaleMod = GetObjectScale(); // 99% sure about this

            if (IsMounted())
            {
                var mountDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(GetMountDisplayId());

                if (mountDisplayInfo != null)
                {
                    var mountModelData = CliDB.CreatureModelDataStorage.LookupByKey(mountDisplayInfo.ModelID);

                    if (mountModelData != null)
                    {
                        var displayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(GetNativeDisplayId());
                        var modelData = CliDB.CreatureModelDataStorage.LookupByKey(displayInfo.ModelID);
                        float collisionHeight = scaleMod * ((mountModelData.MountHeight * mountDisplayInfo.CreatureModelScale) + (modelData.CollisionHeight * modelData.ModelScale * displayInfo.CreatureModelScale * 0.5f));

                        return collisionHeight == 0.0f ? MapConst.DefaultCollesionHeight : collisionHeight;
                    }
                }
            }

            //! Dismounting case - use basic default model _data
            var defaultDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(GetNativeDisplayId());
            var defaultModelData = CliDB.CreatureModelDataStorage.LookupByKey(defaultDisplayInfo.ModelID);

            float collisionHeight1 = scaleMod * defaultModelData.CollisionHeight * defaultModelData.ModelScale * defaultDisplayInfo.CreatureModelScale;

            return collisionHeight1 == 0.0f ? MapConst.DefaultCollesionHeight : collisionHeight1;
        }

        public override string GetDebugInfo()
        {
            string str = $"{base.GetDebugInfo()}\nIsAIEnabled: {IsAIEnabled()} DeathState: {GetDeathState()} UnitMovementFlags: {GetUnitMovementFlags()} UnitMovementFlags2: {GetUnitMovementFlags2()} Class: {GetClass()}\n" +
                         $" {(MoveSpline != null ? MoveSpline.ToString() : "Movespline: <none>\n")} GetCharmedGUID(): {GetCharmedGUID()}\nGetCharmerGUID(): {GetCharmerGUID()}\n{(GetVehicleKit() != null ? GetVehicleKit().GetDebugInfo() : "No vehicle kit")}\n" +
                         $"m_Controlled size: {Controlled.Count}";

            int controlledCount = 0;

            foreach (Unit controlled in Controlled)
            {
                ++controlledCount;
                str += $"\nm_Controlled {controlledCount} : {controlled.GetGUID()}";
            }

            return str;
        }

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
            List<Unit> targets = new();
            var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(this, this, dist);
            var searcher = new UnitListSearcher(this, targets, u_check);
            Cell.VisitAllObjects(this, searcher, dist);

            // remove current Target
            if (GetVictim())
                targets.Remove(GetVictim());

            if (exclude)
                targets.Remove(exclude);

            // remove not LoS targets
            foreach (var unit in targets)
                if (!IsWithinLOSInMap(unit) ||
                    unit.IsTotem() ||
                    unit.IsSpiritService() ||
                    unit.IsCritter())
                    targets.Remove(unit);

            // no appropriate targets
            if (targets.Empty())
                return null;

            // select random
            return targets.SelectRandom();
        }

        public void EnterVehicle(Unit baseUnit, sbyte seatId = -1)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle);
            args.AddSpellMod(SpellValueMod.BasePoint0, seatId + 1);
            CastSpell(baseUnit, SharedConst.VehicleSpellRideHardcoded, args);
        }

        public void _EnterVehicle(Vehicle vehicle, sbyte seatId, AuraApplication aurApp)
        {
            // Must be called only from aura handler
            Cypher.Assert(aurApp != null);

            if (!IsAlive() ||
                GetVehicleKit() == vehicle ||
                vehicle.GetBase().IsOnVehicle(this))
                return;

            if (Vehicle != null)
            {
                if (Vehicle != vehicle)
                {
                    Log.outDebug(LogFilter.Vehicle, "EnterVehicle: {0} exit {1} and enter {2}.", GetEntry(), Vehicle.GetBase().GetEntry(), vehicle.GetBase().GetEntry());
                    ExitVehicle();
                }
                else if (seatId >= 0 &&
                         seatId == GetTransSeat())
                {
                    return;
                }
                else
                {
                    //Exit the current vehicle because unit will reenter in a new Seat.
                    Vehicle.GetBase().RemoveAurasByType(AuraType.ControlVehicle, GetGUID(), aurApp.GetBase());
                }
            }

            if (aurApp.HasRemoveMode())
                return;

            Player player = ToPlayer();

            if (player != null)
            {
                if (vehicle.GetBase().IsTypeId(TypeId.Player) &&
                    player.IsInCombat())
                {
                    vehicle.GetBase().RemoveAura(aurApp);

                    return;
                }

                if (vehicle.GetBase().IsCreature())
                {
                    // If a player entered a vehicle that is part of a formation, remove it from said formation
                    CreatureGroup creatureGroup = vehicle.GetBase().ToCreature().GetFormation();

                    creatureGroup?.RemoveMember(vehicle.GetBase().ToCreature());
                }
            }

            Cypher.Assert(!Vehicle);
            vehicle.AddVehiclePassenger(this, seatId);
        }

        public void ChangeSeat(sbyte seatId, bool next = true)
        {
            if (Vehicle == null)
                return;

            // Don't change if current and new Seat are identical
            if (seatId == GetTransSeat())
                return;

            var seat = (seatId < 0 ? Vehicle.GetNextEmptySeat(GetTransSeat(), next) : Vehicle.Seats.LookupByKey(seatId));

            // The second part of the check will only return true if seatId >= 0. @Vehicle.GetNextEmptySeat makes sure of that.
            if (seat == null ||
                !seat.IsEmpty())
                return;

            AuraEffect rideVehicleEffect = null;
            var vehicleAuras = Vehicle.GetBase().GetAuraEffectsByType(AuraType.ControlVehicle);

            foreach (var eff in vehicleAuras)
            {
                if (eff.GetCasterGUID() != GetGUID())
                    continue;

                // Make sure there is only one ride vehicle aura on Target cast by the unit changing Seat
                Cypher.Assert(rideVehicleEffect == null);
                rideVehicleEffect = eff;
            }

            // Unit riding a vehicle must always have control vehicle aura on Target
            Cypher.Assert(rideVehicleEffect != null);

            rideVehicleEffect.ChangeAmount((seatId < 0 ? GetTransSeat() : seatId) + 1);
        }

        public virtual void ExitVehicle(Position exitPosition = null)
        {
            //! This function can be called at upper level code to initialize an exit from the passenger's side.
            if (Vehicle == null)
                return;

            GetVehicleBase().RemoveAurasByType(AuraType.ControlVehicle, GetGUID());
            //! The following call would not even be executed successfully as the
            //! SPELL_AURA_CONTROL_VEHICLE unapply handler already calls _ExitVehicle without
            //! specifying an exitposition. The subsequent call below would return on if (!_vehicle).

            //! To do:
            //! We need to allow SPELL_AURA_CONTROL_VEHICLE unapply handlers in spellscripts
            //! to specify exit coordinates and either store those per passenger, or we need to
            //! init spline movement based on those coordinates in unapply handlers, and
            //! relocate exiting passengers based on Unit.moveSpline _data. Either way,
            //! Coming Soon(TM)
        }

        public void _ExitVehicle(Position exitPosition = null)
        {
            // It's possible _vehicle is NULL, when this function is called indirectly from @VehicleJoinEvent.Abort.
            // In that case it was not possible to add the passenger to the vehicle. The vehicle aura has already been removed
            // from the Target in the aforementioned function and we don't need to do anything else at this point.
            if (Vehicle == null)
                return;

            // This should be done before dismiss, because there may be some aura removal
            VehicleSeatAddon seatAddon = Vehicle.GetSeatAddonForSeatOfPassenger(this);
            Vehicle vehicle = (Vehicle)Vehicle.RemovePassenger(this);

            if (vehicle == null)
            {
                Log.outError(LogFilter.Vehicle, $"RemovePassenger() couldn't remove current unit from vehicle. Debug info: {GetDebugInfo()}");

                return;
            }

            Player player = ToPlayer();

            // If the player is on mounted Duel and exits the Mount, he should immediatly lose the Duel
            if (player &&
                player.Duel != null &&
                player.Duel.IsMounted)
                player.DuelComplete(DuelCompleteType.Fled);

            SetControlled(false, UnitState.Root); // SMSG_MOVE_FORCE_UNROOT, ~MOVEMENTFLAG_ROOT

            AddUnitState(UnitState.Move);

            player?.SetFallInformation(0, GetPositionZ());

            Position pos;

            // If we ask for a specific exit position, use that one. Otherwise allow scripts to modify it
            if (exitPosition != null)
            {
                pos = exitPosition;
            }
            else
            {
                // Set exit position to vehicle position and use the current orientation
                pos = vehicle.GetBase().GetPosition();
                pos.SetOrientation(GetOrientation());

                // Change exit position based on Seat entry addon _data
                if (seatAddon != null)
                {
                    if (seatAddon.ExitParameter == VehicleExitParameters.VehicleExitParamOffset)
                        pos.RelocateOffset(new Position(seatAddon.ExitParameterX, seatAddon.ExitParameterY, seatAddon.ExitParameterZ, seatAddon.ExitParameterO));
                    else if (seatAddon.ExitParameter == VehicleExitParameters.VehicleExitParamDest)
                        pos.Relocate(new Position(seatAddon.ExitParameterX, seatAddon.ExitParameterY, seatAddon.ExitParameterZ, seatAddon.ExitParameterO));
                }
            }

            var initializer = (MoveSplineInit init) =>
                              {
                                  float height = pos.GetPositionZ() + vehicle.GetBase().GetCollisionHeight();

                                  // Creatures without inhabit Type air should begin falling after exiting the vehicle
                                  if (IsTypeId(TypeId.Unit) &&
                                      !CanFly() &&
                                      height > GetMap().GetWaterOrGroundLevel(GetPhaseShift(), pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ() + vehicle.GetBase().GetCollisionHeight(), ref height))
                                      init.SetFall();

                                  init.MoveTo(pos.GetPositionX(), pos.GetPositionY(), height, false);
                                  init.SetFacing(pos.GetOrientation());
                                  init.SetTransportExit();
                              };

            GetMotionMaster().LaunchMoveSpline(initializer, EventId.VehicleExit, MovementGeneratorPriority.Highest);

            player?.ResummonPetTemporaryUnSummonedIfAny();

            if (vehicle.GetBase().HasUnitTypeMask(UnitTypeMask.Minion) &&
                vehicle.GetBase().IsTypeId(TypeId.Unit))
                if (((Minion)vehicle.GetBase()).GetOwner() == this)
                    vehicle.GetBase().ToCreature().DespawnOrUnsummon(vehicle.GetDespawnDelay());

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

        public void UnsummonAllTotems()
        {
            for (byte i = 0; i < SharedConst.MaxSummonSlot; ++i)
            {
                if (SummonSlot[i].IsEmpty())
                    continue;

                Creature OldTotem = GetMap().GetCreature(SummonSlot[i]);

                if (OldTotem != null)
                    if (OldTotem.IsSummon())
                        OldTotem.ToTempSummon().UnSummon();
            }
        }

        public bool IsOnVehicle(Unit vehicle)
        {
            return Vehicle != null && Vehicle == vehicle.GetVehicleKit();
        }

        public bool IsAIEnabled()
        {
            return IAi != null;
        }

        public virtual UnitAI GetAI()
        {
            return IAi;
        }

        public UnitAI GetTopAI()
        {
            return IAIs.Count == 0 ? null : IAIs.Peek();
        }

        public void AIUpdateTick(uint diff)
        {
            UnitAI ai = GetAI();

            if (ai != null)
            {
                _aiLocked = true;
                ai.UpdateAI(diff);
                _aiLocked = false;
            }
        }

        public void PushAI(UnitAI newAI)
        {
            IAIs.Push(newAI);
        }

        public void SetAI(UnitAI newAI)
        {
            PushAI(newAI);
            RefreshAI();
        }

        public bool PopAI()
        {
            if (IAIs.Count != 0)
            {
                IAIs.Pop();

                return true;
            }
            else
            {
                return false;
            }
        }

        public void RefreshAI()
        {
            Cypher.Assert(!_aiLocked, "Tried to change current AI during UpdateAI()");

            if (IAIs.Count == 0)
                IAi = null;
            else
                IAi = IAIs.Peek();
        }

        public void ScheduleAIChange()
        {
            bool charmed = IsCharmed();

            if (charmed)
            {
                PushAI(GetScheduledChangeAI());
            }
            else
            {
                RestoreDisabledAI();
                PushAI(GetScheduledChangeAI()); //This could actually be PopAI() to get the previous AI but it's required atm to trigger UpdateCharmAI()
            }
        }

        public bool IsPossessedByPlayer()
        {
            return HasUnitState(UnitState.Possessed) && GetCharmerGUID().IsPlayer();
        }

        public bool IsPossessing()
        {
            Unit u = GetCharmed();

            if (u != null)
                return u.IsPossessed();
            else
                return false;
        }

        public bool IsCharmed()
        {
            return !GetCharmerGUID().IsEmpty();
        }

        public bool IsPossessed()
        {
            return HasUnitState(UnitState.Possessed);
        }

        public virtual void OnPhaseChange()
        {
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
                case 24858: // Moonkin Form
                    {
                        if (HasAura(114301)) // Glyph of Stars
                            return 0;

                        break;
                    }
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

                    if (artifact != null)
                    {
                        ArtifactAppearanceRecord artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifact.GetModifier(ItemModifier.ArtifactAppearanceId));

                        if (artifactAppearance != null)
                            if ((ShapeShiftForm)artifactAppearance.OverrideShapeshiftFormID == form)
                                return artifactAppearance.OverrideShapeshiftDisplayID;
                    }
                }

                ShapeshiftFormModelData formModelData = Global.DB2Mgr.GetShapeshiftFormModelData(GetRace(), thisPlayer.GetNativeGender(), form);

                if (formModelData != null)
                {
                    bool useRandom = false;

                    switch (form)
                    {
                        case ShapeShiftForm.CatForm:
                            useRandom = HasAura(210333);

                            break; // Glyph of the Feral Chameleon
                        case ShapeShiftForm.TravelForm:
                            useRandom = HasAura(344336);

                            break; // Glyph of the Swift Chameleon
                        case ShapeShiftForm.AquaticForm:
                            useRandom = HasAura(344338);

                            break; // Glyph of the Aquatic Chameleon
                        case ShapeShiftForm.BearForm:
                            useRandom = HasAura(107059);

                            break; // Glyph of the Ursol Chameleon
                        case ShapeShiftForm.FlightFormEpic:
                        case ShapeShiftForm.FlightForm:
                            useRandom = HasAura(344342);

                            break; // Glyph of the Aerial Chameleon
                        default:
                            break;
                    }

                    if (useRandom)
                    {
                        List<uint> displayIds = new();

                        for (var i = 0; i < formModelData.Choices.Count; ++i)
                        {
                            ChrCustomizationDisplayInfoRecord displayInfo = formModelData.Displays[i];

                            if (displayInfo != null)
                            {
                                ChrCustomizationReqRecord choiceReq = CliDB.ChrCustomizationReqStorage.LookupByKey(formModelData.Choices[i].ChrCustomizationReqID);

                                if (choiceReq == null ||
                                    thisPlayer.GetSession().MeetsChrCustomizationReq(choiceReq, GetClass(), false, thisPlayer.PlayerData.Customizations))
                                    displayIds.Add(displayInfo.DisplayID);
                            }
                        }

                        if (!displayIds.Empty())
                            return displayIds.SelectRandom();
                    }
                    else
                    {
                        uint formChoice = thisPlayer.GetCustomizationChoice(formModelData.OptionID);

                        if (formChoice != 0)
                        {
                            var choiceIndex = formModelData.Choices.FindIndex(choice => { return choice.Id == formChoice; });

                            if (choiceIndex != -1)
                            {
                                ChrCustomizationDisplayInfoRecord displayInfo = formModelData.Displays[choiceIndex];

                                if (displayInfo != null)
                                    return displayInfo.DisplayID;
                            }
                        }
                    }
                }

                switch (form)
                {
                    case ShapeShiftForm.GhostWolf:
                        if (HasAura(58135)) // Glyph of Spectral Wolf
                            return 60247;

                        break;
                    default:
                        break;
                }
            }

            uint modelid = 0;
            SpellShapeshiftFormRecord formEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey(form);

            if (formEntry != null &&
                formEntry.CreatureDisplayID[0] != 0)
            {
                // Take the alliance modelid as default
                if (GetTypeId() != TypeId.Player)
                {
                    return formEntry.CreatureDisplayID[0];
                }
                else
                {
                    if (Player.TeamForRace(GetRace()) == Team.Alliance)
                        modelid = formEntry.CreatureDisplayID[0];
                    else
                        modelid = formEntry.CreatureDisplayID[1];

                    // If the player is horde but there are no values for the horde modelid - take the alliance modelid
                    if (modelid == 0 &&
                        Player.TeamForRace(GetRace()) == Team.Horde)
                        modelid = formEntry.CreatureDisplayID[0];
                }
            }

            return modelid;
        }

        public Totem ToTotem()
        {
            return IsTotem() ? (this as Totem) : null;
        }

        public TempSummon ToTempSummon()
        {
            return IsSummon() ? (this as TempSummon) : null;
        }

        public virtual void SetDeathState(DeathState s)
        {
            // Death State needs to be updated before RemoveAllAurasOnDeath() is called, to prevent entering combat
            DeathState = s;

            bool isOnVehicle = GetVehicle() != null;

            if (s != DeathState.Alive &&
                s != DeathState.JustRespawned)
            {
                CombatStop();

                if (IsNonMeleeSpellCast(false))
                    InterruptNonMeleeSpells(false);

                ExitVehicle(); // Exit vehicle before calling RemoveAllControlled
                               // vehicles use special Type of charm that is not removed by the next function
                               // triggering an assert
                UnsummonAllTotems();
                RemoveAllControlled();
                RemoveAllAurasOnDeath();
            }

            if (s == DeathState.JustDied)
            {
                // remove aurastates allowing special moves
                ClearAllReactives();
                _diminishing.Clear();

                // Don't clear the movement if the Unit was on a vehicle as we are exiting now
                if (!isOnVehicle)
                    if (GetMotionMaster().StopOnDeath())
                        DisableSpline();

                // without this when removing IncreaseMaxHealth aura player may stuck with 1 hp
                // do not why since in IncreaseMaxHealth currenthealth is checked
                SetHealth(0);
                SetPower(GetPowerType(), 0);
                SetEmoteState(Emote.OneshotNone);

                // players in instance don't have ZoneScript, but they have InstanceScript
                ZoneScript zoneScript = GetZoneScript() != null ? GetZoneScript() : GetInstanceScript();

                zoneScript?.OnUnitDeath(this);
            }
            else if (s == DeathState.JustRespawned)
            {
                RemoveUnitFlag(UnitFlags.Skinnable); // clear skinnable for creature and player (at Battleground)
            }
        }

        public bool IsVisible()
        {
            return ServerSideVisibility.GetValue(ServerSideVisibilityType.GM) <= (uint)AccountTypes.Player;
        }

        public void SetVisible(bool val)
        {
            if (!val)
                ServerSideVisibility.SetValue(ServerSideVisibilityType.GM, AccountTypes.GameMaster);
            else
                ServerSideVisibility.SetValue(ServerSideVisibilityType.GM, AccountTypes.Player);

            UpdateObjectVisibility();
        }

        public bool IsMagnet()
        {
            // Grounding Totem
            if (UnitData.CreatedBySpell == 8177) /// @todo: find a more generic solution
				return true;

            return false;
        }

        public void SetShapeshiftForm(ShapeShiftForm form)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ShapeshiftForm), (byte)form);
        }

        // creates aura application instance and registers it in lists
        // aura application effects are handled separately to prevent aura list corruption
        public AuraApplication _CreateAuraApplication(Aura aura, uint effMask)
        {
            // can't apply aura on unit which is going to be deleted - to not create a memory leak
            Cypher.Assert(!_cleanupDone);

            // just return if the aura has been already removed
            // this can happen if OnEffectHitTarget() script hook killed the unit or the aura owner (which can be different)
            if (aura.IsRemoved())
            {
                Log.outError(LogFilter.Spells, $"Unit::_CreateAuraApplication() called with a removed aura. Check if OnEffectHitTarget() is triggering any spell with apply aura effect (that's not allowed!)\nUnit: {GetDebugInfo()}\nAura: {aura.GetDebugInfo()}");

                return null;
            }

            // aura mustn't be already applied on Target
            Cypher.Assert(!aura.IsAppliedOnTarget(GetGUID()), "Unit._CreateAuraApplication: aura musn't be applied on Target");

            SpellInfo aurSpellInfo = aura.GetSpellInfo();
            uint aurId = aurSpellInfo.Id;

            // ghost spell check, allow apply any Auras at player loading in ghost mode (will be cleanup after load)
            if (!IsAlive() &&
                !aurSpellInfo.IsDeathPersistent() &&
                (!IsTypeId(TypeId.Player) || !ToPlayer().GetSession().PlayerLoading()))
                return null;

            Unit caster = aura.GetCaster();

            AuraApplication aurApp = new(this, caster, aura, effMask);
            _appliedAuras.Add(aurId, aurApp);

            if (aurSpellInfo.HasAnyAuraInterruptFlag())
            {
                _interruptableAuras.Add(aurApp);
                AddInterruptMask(aurSpellInfo.AuraInterruptFlags, aurSpellInfo.AuraInterruptFlags2);
            }

            AuraStateType aState = aura.GetSpellInfo().GetAuraState();

            if (aState != 0)
                _auraStateAuras.Add(aState, aurApp);

            aura._ApplyForTarget(this, caster, aurApp);

            return aurApp;
        }

        public void AddInterruptMask(SpellAuraInterruptFlags flags, SpellAuraInterruptFlags2 flags2)
        {
            _interruptMask |= flags;
            _interruptMask2 |= flags2;
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

                            if (cEntry != null &&
                                cEntry.DisplayPower < PowerType.Max)
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
                                    else if (pet.IsPetGhoul() ||
                                             pet.IsPetAbomination()) // DK pets have energy
                                        displayPower = PowerType.Energy;
                                }
                            }
                        }

                        break;
                    }
            }

            SetPowerType(displayPower);
        }

        public void SetSheath(SheathState sheathed)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.SheatheState), (byte)sheathed);

            if (sheathed == SheathState.Unarmed)
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Sheathing);
        }

        public bool IsInFeralForm()
        {
            ShapeShiftForm form = GetShapeshiftForm();

            return form == ShapeShiftForm.CatForm || form == ShapeShiftForm.BearForm || form == ShapeShiftForm.DireBearForm || form == ShapeShiftForm.GhostWolf;
        }

        public bool IsControlledByPlayer()
        {
            return ControlledByPlayer;
        }

        public bool IsCharmedOwnedByPlayerOrPlayer()
        {
            return GetCharmerOrOwnerOrOwnGUID().IsPlayer();
        }

        public void FollowerAdded(AbstractFollower f)
        {
            _followingMe.Add(f);
        }

        public void FollowerRemoved(AbstractFollower f)
        {
            _followingMe.Remove(f);
        }

        public uint GetCreatureTypeMask()
        {
            uint creatureType = (uint)GetCreatureType();

            return (uint)(creatureType >= 1 ? (1 << (int)(creatureType - 1)) : 0);
        }

        public Pet ToPet()
        {
            return IsPet() ? (this as Pet) : null;
        }

        public MotionMaster GetMotionMaster()
        {
            return _iMotionMaster;
        }

        public void PlayOneShotAnimKitId(ushort animKitId)
        {
            if (!CliDB.AnimKitStorage.ContainsKey(animKitId))
            {
                Log.outError(LogFilter.Unit, "Unit.PlayOneShotAnimKitId using invalid AnimKit ID: {0}", animKitId);

                return;
            }

            PlayOneShotAnimKit packet = new();
            packet.Unit = GetGUID();
            packet.AnimKitID = animKitId;
            SendMessageToSet(packet, true);
        }

        public void SetAIAnimKitId(ushort animKitId)
        {
            if (_aiAnimKitId == animKitId)
                return;

            if (animKitId != 0 &&
                !CliDB.AnimKitStorage.ContainsKey(animKitId))
                return;

            _aiAnimKitId = animKitId;

            SetAIAnimKit data = new();
            data.Unit = GetGUID();
            data.AnimKitID = animKitId;
            SendMessageToSet(data, true);
        }

        public override ushort GetAIAnimKitId()
        {
            return _aiAnimKitId;
        }

        public void SetMovementAnimKitId(ushort animKitId)
        {
            if (_movementAnimKitId == animKitId)
                return;

            if (animKitId != 0 &&
                !CliDB.AnimKitStorage.ContainsKey(animKitId))
                return;

            _movementAnimKitId = animKitId;

            SetMovementAnimKit data = new();
            data.Unit = GetGUID();
            data.AnimKitID = animKitId;
            SendMessageToSet(data, true);
        }

        public override ushort GetMovementAnimKitId()
        {
            return _movementAnimKitId;
        }

        public void SetMeleeAnimKitId(ushort animKitId)
        {
            if (_meleeAnimKitId == animKitId)
                return;

            if (animKitId != 0 &&
                !CliDB.AnimKitStorage.ContainsKey(animKitId))
                return;

            _meleeAnimKitId = animKitId;

            SetMeleeAnimKit data = new();
            data.Unit = GetGUID();
            data.AnimKitID = animKitId;
            SendMessageToSet(data, true);
        }

        public override ushort GetMeleeAnimKitId()
        {
            return _meleeAnimKitId;
        }

        public uint GetVirtualItemId(int slot)
        {
            if (slot >= SharedConst.MaxEquipmentItems)
                return 0;

            return UnitData.VirtualItems[slot].ItemID;
        }

        public ushort GetVirtualItemAppearanceMod(uint slot)
        {
            if (slot >= SharedConst.MaxEquipmentItems)
                return 0;

            return UnitData.VirtualItems[(int)slot].ItemAppearanceModID;
        }

        public void SetVirtualItem(uint slot, uint itemId, ushort appearanceModId = 0, ushort itemVisual = 0)
        {
            if (slot >= SharedConst.MaxEquipmentItems)
                return;

            var virtualItemField = Values.ModifyValue(UnitData).ModifyValue(UnitData.VirtualItems, (int)slot);
            SetUpdateFieldValue(virtualItemField.ModifyValue(virtualItemField.ItemID), itemId);
            SetUpdateFieldValue(virtualItemField.ModifyValue(virtualItemField.ItemAppearanceModID), appearanceModId);
            SetUpdateFieldValue(virtualItemField.ModifyValue(virtualItemField.ItemVisual), itemVisual);
        }

        //Unit
        public void SetLevel(uint lvl, bool sendUpdate = true)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Level), lvl);

            if (!sendUpdate)
                return;

            Player player = ToPlayer();

            if (player != null)
            {
                if (player.GetGroup())
                    player.SetGroupUpdateFlag(GroupUpdateFlags.Level);

                Global.CharacterCacheStorage.UpdateCharacterLevel(ToPlayer().GetGUID(), (byte)lvl);
            }
        }

        public uint GetLevel()
        {
            return UnitData.Level;
        }

        public override uint GetLevelForTarget(WorldObject target)
        {
            return GetLevel();
        }

        public Race GetRace()
        {
            return (Race)UnitData.Race.Value;
        }

        public void SetRace(Race race)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Race), (byte)race);
        }

        public Class GetClass()
        {
            return (Class)UnitData.ClassId.Value;
        }

        public void SetClass(Class classId)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ClassId), (byte)classId);
        }

        public uint GetClassMask()
        {
            return (uint)(1 << ((int)GetClass() - 1));
        }

        public Gender GetGender()
        {
            return (Gender)UnitData.Sex.Value;
        }

        public void SetGender(Gender sex)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Sex), (byte)sex);
        }

        public virtual Gender GetNativeGender()
        {
            return GetGender();
        }

        public virtual void SetNativeGender(Gender gender)
        {
            SetGender(gender);
        }

        public void RecalculateObjectScale()
        {
            int scaleAuras = GetTotalAuraModifier(AuraType.ModScale) + GetTotalAuraModifier(AuraType.ModScale2);
            float scale = GetNativeObjectScale() + MathFunctions.CalculatePct(1.0f, scaleAuras);
            float scaleMin = IsPlayer() ? 0.1f : 0.01f;
            SetObjectScale(Math.Max(scale, scaleMin));
        }

        public virtual float GetNativeObjectScale()
        {
            return 1.0f;
        }

        public uint GetDisplayId()
        {
            return UnitData.DisplayID;
        }

        public virtual void SetDisplayId(uint modelId, float displayScale = 1f)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.DisplayID), modelId);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.DisplayScale), displayScale);
            // Set Gender by modelId
            CreatureModelInfo minfo = Global.ObjectMgr.GetCreatureModelInfo(modelId);

            if (minfo != null)
                SetGender((Gender)minfo.Gender);
        }

        public void RestoreDisplayId(bool ignorePositiveAurasPreventingMounting = false)
        {
            AuraEffect handledAura = null;
            // try to receive model from transform Auras
            var transforms = GetAuraEffectsByType(AuraType.Transform);

            if (!transforms.Empty())
                // iterate over already applied transform Auras - from newest to oldest
                foreach (var eff in transforms)
                {
                    AuraApplication aurApp = eff.GetBase().GetApplicationOfTarget(GetGUID());

                    if (aurApp != null)
                    {
                        if (handledAura == null)
                        {
                            if (!ignorePositiveAurasPreventingMounting)
                            {
                                handledAura = eff;
                            }
                            else
                            {
                                CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate((uint)eff.GetMiscValue());

                                if (ci != null)
                                    if (!IsDisallowedMountForm(eff.GetId(), ShapeShiftForm.None, ObjectManager.ChooseDisplayId(ci).CreatureDisplayID))
                                        handledAura = eff;
                            }
                        }

                        // prefer negative Auras
                        if (!aurApp.IsPositive())
                        {
                            handledAura = eff;

                            break;
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
                // only one such aura possible at a Time
                uint modelId = GetModelForForm(GetShapeshiftForm(), shapeshiftAura[0].GetId());

                if (modelId != 0)
                {
                    if (!ignorePositiveAurasPreventingMounting ||
                        !IsDisallowedMountForm(0, GetShapeshiftForm(), modelId))
                        SetDisplayId(modelId);
                    else
                        SetDisplayId(GetNativeDisplayId());

                    return;
                }
            }

            // no Auras found - set modelid to default
            SetDisplayId(GetNativeDisplayId());
        }

        public uint GetNativeDisplayId()
        {
            return UnitData.NativeDisplayID;
        }

        public void SetNativeDisplayId(uint displayId, float displayScale = 1f)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.NativeDisplayID), displayId);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.NativeXDisplayScale), displayScale);
        }

        public float GetNativeDisplayScale()
        {
            return UnitData.NativeXDisplayScale;
        }

        public bool IsMounted()
        {
            return HasUnitFlag(UnitFlags.Mount);
        }

        public uint GetMountDisplayId()
        {
            return UnitData.MountDisplayID;
        }

        public void SetMountDisplayId(uint mountDisplayId)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MountDisplayID), mountDisplayId);
        }

        public void SetCosmeticMountDisplayId(uint mountDisplayId)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.CosmeticMountDisplayID), mountDisplayId);
        }

        public virtual float GetFollowAngle()
        {
            return MathFunctions.PiOver2;
        }

        public override ObjectGuid GetOwnerGUID()
        {
            return UnitData.SummonedBy;
        }

        public void SetOwnerGUID(ObjectGuid owner)
        {
            if (GetOwnerGUID() == owner)
                return;

            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.SummonedBy), owner);

            if (owner.IsEmpty())
                return;

            // Update owner dependent fields
            Player player = Global.ObjAccessor.GetPlayer(this, owner);

            if (player == null ||
                !player.HaveAtClient(this)) // if player cannot see this unit yet, he will receive needed _data with create object
                return;

            UpdateData udata = new(GetMapId());
            UpdateObject packet;
            BuildValuesUpdateBlockForPlayerWithFlag(udata, UpdateFieldFlag.Owner, player);
            udata.BuildPacket(out packet);
            player.SendPacket(packet);
        }

        public ObjectGuid GetCreatorGUID()
        {
            return UnitData.CreatedBy;
        }

        public void SetCreatorGUID(ObjectGuid creator)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.CreatedBy), creator);
        }

        public ObjectGuid GetMinionGUID()
        {
            return UnitData.Summon;
        }

        public void SetMinionGUID(ObjectGuid guid)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Summon), guid);
        }

        public ObjectGuid GetPetGUID()
        {
            return SummonSlot[0];
        }

        public void SetPetGUID(ObjectGuid guid)
        {
            SummonSlot[0] = guid;
        }

        public ObjectGuid GetCritterGUID()
        {
            return UnitData.Critter;
        }

        public void SetCritterGUID(ObjectGuid guid)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Critter), guid);
        }

        public ObjectGuid GetBattlePetCompanionGUID()
        {
            return UnitData.BattlePetCompanionGUID;
        }

        public void SetBattlePetCompanionGUID(ObjectGuid guid)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BattlePetCompanionGUID), guid);
        }

        public ObjectGuid GetDemonCreatorGUID()
        {
            return UnitData.DemonCreator;
        }

        public void SetDemonCreatorGUID(ObjectGuid guid)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.DemonCreator), guid);
        }

        public ObjectGuid GetCharmerGUID()
        {
            return UnitData.CharmedBy;
        }

        public Unit GetCharmer()
        {
            return _charmer;
        }

        public ObjectGuid GetCharmedGUID()
        {
            return UnitData.Charm;
        }

        public Unit GetCharmed()
        {
            return _charmed;
        }

        public override ObjectGuid GetCharmerOrOwnerGUID()
        {
            return IsCharmed() ? GetCharmerGUID() : GetOwnerGUID();
        }

        public override Unit GetCharmerOrOwner()
        {
            return IsCharmed() ? GetCharmer() : GetOwner();
        }

        public uint GetBattlePetCompanionNameTimestamp()
        {
            return UnitData.BattlePetCompanionNameTimestamp;
        }

        public void SetBattlePetCompanionNameTimestamp(uint timestamp)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BattlePetCompanionNameTimestamp), timestamp);
        }

        public uint GetBattlePetCompanionExperience()
        {
            return UnitData.BattlePetCompanionExperience;
        }

        public void SetBattlePetCompanionExperience(uint experience)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BattlePetCompanionExperience), experience);
        }

        public uint GetWildBattlePetLevel()
        {
            return UnitData.WildBattlePetLevel;
        }

        public void SetWildBattlePetLevel(uint wildBattlePetLevel)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.WildBattlePetLevel), wildBattlePetLevel);
        }

        public bool HasUnitFlag(UnitFlags flags)
        {
            return (UnitData.Flags & (uint)flags) != 0;
        }

        public void SetUnitFlag(UnitFlags flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags), (uint)flags);
        }

        public void RemoveUnitFlag(UnitFlags flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags), (uint)flags);
        }

        public void ReplaceAllUnitFlags(UnitFlags flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags), (uint)flags);
        }

        public bool HasUnitFlag2(UnitFlags2 flags)
        {
            return (UnitData.Flags2 & (uint)flags) != 0;
        }

        public void SetUnitFlag2(UnitFlags2 flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags2), (uint)flags);
        }

        public void RemoveUnitFlag2(UnitFlags2 flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags2), (uint)flags);
        }

        public void ReplaceAllUnitFlags2(UnitFlags2 flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags2), (uint)flags);
        }

        public bool HasUnitFlag3(UnitFlags3 flags)
        {
            return (UnitData.Flags3 & (uint)flags) != 0;
        }

        public void SetUnitFlag3(UnitFlags3 flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags3), (uint)flags);
        }

        public void RemoveUnitFlag3(UnitFlags3 flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags3), (uint)flags);
        }

        public void ReplaceAllUnitFlags3(UnitFlags3 flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags3), (uint)flags);
        }

        public UnitDynFlags GetDynamicFlags()
        {
            return (UnitDynFlags)(uint)ObjectData.DynamicFlags;
        }

        public bool HasDynamicFlag(UnitDynFlags flag)
        {
            return (ObjectData.DynamicFlags & (uint)flag) != 0;
        }

        public void SetDynamicFlag(UnitDynFlags flag)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
        }

        public void RemoveDynamicFlag(UnitDynFlags flag)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
        }

        public void ReplaceAllDynamicFlags(UnitDynFlags flag)
        {
            SetUpdateFieldValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
        }

        public void SetCreatedBySpell(uint spellId)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.CreatedBySpell), spellId);
        }

        public void SetNameplateAttachToGUID(ObjectGuid guid)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.NameplateAttachToGUID), guid);
        }

        public Emote GetEmoteState()
        {
            return (Emote)(int)UnitData.EmoteState;
        }

        public void SetEmoteState(Emote emote)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.EmoteState), (int)emote);
        }

        public SheathState GetSheath()
        {
            return (SheathState)(byte)UnitData.SheatheState;
        }

        public UnitPVPStateFlags GetPvpFlags()
        {
            return (UnitPVPStateFlags)(byte)UnitData.PvpFlags;
        }

        public bool HasPvpFlag(UnitPVPStateFlags flags)
        {
            return (UnitData.PvpFlags & (uint)flags) != 0;
        }

        public void SetPvpFlag(UnitPVPStateFlags flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PvpFlags), (byte)flags);
        }

        public void RemovePvpFlag(UnitPVPStateFlags flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PvpFlags), (byte)flags);
        }

        public void ReplaceAllPvpFlags(UnitPVPStateFlags flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PvpFlags), (byte)flags);
        }

        public bool IsInSanctuary()
        {
            return HasPvpFlag(UnitPVPStateFlags.Sanctuary);
        }

        public bool IsPvP()
        {
            return HasPvpFlag(UnitPVPStateFlags.PvP);
        }

        public bool IsFFAPvP()
        {
            return HasPvpFlag(UnitPVPStateFlags.FFAPvp);
        }

        public UnitPetFlags GetPetFlags()
        {
            return (UnitPetFlags)(byte)UnitData.PetFlags;
        }

        public bool HasPetFlag(UnitPetFlags flags)
        {
            return (UnitData.PetFlags & (byte)flags) != 0;
        }

        public void SetPetFlag(UnitPetFlags flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetFlags), (byte)flags);
        }

        public void RemovePetFlag(UnitPetFlags flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetFlags), (byte)flags);
        }

        public void ReplaceAllPetFlags(UnitPetFlags flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetFlags), (byte)flags);
        }

        public void SetPetNumberForClient(uint petNumber)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetNumber), petNumber);
        }

        public void SetPetNameTimestamp(uint timestamp)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetNameTimestamp), timestamp);
        }

        public ShapeShiftForm GetShapeshiftForm()
        {
            return (ShapeShiftForm)(byte)UnitData.ShapeshiftForm;
        }

        public CreatureType GetCreatureType()
        {
            if (IsTypeId(TypeId.Player))
            {
                ShapeShiftForm form = GetShapeshiftForm();
                var ssEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)form);

                if (ssEntry != null &&
                    ssEntry.CreatureType > 0)
                {
                    return (CreatureType)ssEntry.CreatureType;
                }
                else
                {
                    var raceEntry = CliDB.ChrRacesStorage.LookupByKey(GetRace());

                    return (CreatureType)raceEntry.CreatureType;
                }
            }
            else
            {
                return ToCreature().GetCreatureTemplate().CreatureType;
            }
        }

        public void DeMorph()
        {
            SetDisplayId(GetNativeDisplayId());
        }

        public bool HasUnitTypeMask(UnitTypeMask mask)
        {
            return Convert.ToBoolean(mask & UnitTypeMask);
        }

        public void AddUnitTypeMask(UnitTypeMask mask)
        {
            UnitTypeMask |= mask;
        }

        public bool IsAlive()
        {
            return DeathState == DeathState.Alive;
        }

        public bool IsDying()
        {
            return DeathState == DeathState.JustDied;
        }

        public bool IsDead()
        {
            return (DeathState == DeathState.Dead || DeathState == DeathState.Corpse);
        }

        public bool IsSummon()
        {
            return UnitTypeMask.HasAnyFlag(UnitTypeMask.Summon);
        }

        public bool IsGuardian()
        {
            return UnitTypeMask.HasAnyFlag(UnitTypeMask.Guardian);
        }

        public bool IsPet()
        {
            return UnitTypeMask.HasAnyFlag(UnitTypeMask.Pet);
        }

        public bool IsHunterPet()
        {
            return UnitTypeMask.HasAnyFlag(UnitTypeMask.HunterPet);
        }

        public bool IsTotem()
        {
            return UnitTypeMask.HasAnyFlag(UnitTypeMask.Totem);
        }

        public bool IsVehicle()
        {
            return UnitTypeMask.HasAnyFlag(UnitTypeMask.Vehicle);
        }

        public void AddUnitState(UnitState f)
        {
            _state |= f;
        }

        public bool HasUnitState(UnitState f)
        {
            return _state.HasAnyFlag(f);
        }

        public void ClearUnitState(UnitState f)
        {
            _state &= ~f;
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

        public override uint GetFaction()
        {
            return UnitData.FactionTemplate;
        }

        public override void SetFaction(uint faction)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.FactionTemplate), faction);
        }

        public void RestoreFaction()
        {
            if (HasAuraType(AuraType.ModFaction))
            {
                SetFaction((uint)GetAuraEffectsByType(AuraType.ModFaction).LastOrDefault().GetMiscValue());

                return;
            }

            if (IsTypeId(TypeId.Player))
            {
                ToPlayer().SetFactionForRace(GetRace());
            }
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

                if (cinfo != null) // normal creature
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

            if (u1.IsTypeId(TypeId.Player) &&
                u2.IsTypeId(TypeId.Player))
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

            if (u1.IsTypeId(TypeId.Player) &&
                u2.IsTypeId(TypeId.Player))
                return u1.ToPlayer().IsInSameRaidWith(u2.ToPlayer());
            else if ((u2.IsTypeId(TypeId.Player) && u1.IsTypeId(TypeId.Unit) && u1.ToCreature().GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)) ||
                     (u1.IsTypeId(TypeId.Player) && u2.IsTypeId(TypeId.Unit) && u2.ToCreature().GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)))
                return true;

            // else u1.GetTypeId() == u2.GetTypeId() == TYPEID_UNIT
            return u1.GetFaction() == u2.GetFaction();
        }

        public UnitStandStateType GetStandState()
        {
            return (UnitStandStateType)(byte)UnitData.StandState;
        }

        public void SetVisFlag(UnitVisFlags flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.VisFlags), (byte)flags);
        }

        public void RemoveVisFlag(UnitVisFlags flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.VisFlags), (byte)flags);
        }

        public void ReplaceAllVisFlags(UnitVisFlags flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.VisFlags), (byte)flags);
        }

        public bool IsSitState()
        {
            UnitStandStateType s = GetStandState();

            return
                s == UnitStandStateType.SitChair ||
                s == UnitStandStateType.SitLowChair ||
                s == UnitStandStateType.SitMediumChair ||
                s == UnitStandStateType.SitHighChair ||
                s == UnitStandStateType.Sit;
        }

        public bool IsStandState()
        {
            UnitStandStateType s = GetStandState();

            return !IsSitState() && s != UnitStandStateType.Sleep && s != UnitStandStateType.Kneel;
        }

        public void SetStandState(UnitStandStateType state, uint animKitId = 0)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.StandState), (byte)state);

            if (IsStandState())
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Standing);

            if (IsTypeId(TypeId.Player))
            {
                StandStateUpdate packet = new(state, animKitId);
                ToPlayer().SendPacket(packet);
            }
        }

        public AnimTier GetAnimTier()
        {
            return (AnimTier)(byte)UnitData.AnimTier;
        }

        public void SetAnimTier(AnimTier animTier, bool notifyClient = true)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AnimTier), (byte)animTier);

            if (notifyClient)
            {
                SetAnimTier setAnimTier = new();
                setAnimTier.Unit = GetGUID();
                setAnimTier.Tier = (int)animTier;
                SendMessageToSet(setAnimTier, true);
            }
        }

        public uint GetChannelSpellId()
        {
            return ((UnitChannel)UnitData.ChannelData).SpellID;
        }

        public void SetChannelSpellId(uint channelSpellId)
        {
            SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelData).Value.SpellID, channelSpellId);
        }

        public uint GetChannelSpellXSpellVisualId()
        {
            return UnitData.ChannelData.GetValue().SpellVisual.SpellXSpellVisualID;
        }

        public uint GetChannelScriptVisualId()
        {
            return UnitData.ChannelData.GetValue().SpellVisual.ScriptVisualID;
        }

        public void SetChannelVisual(SpellCastVisualField channelVisual)
        {
            UnitChannel unitChannel = Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelData);
            SetUpdateFieldValue(ref unitChannel.SpellVisual, channelVisual);
        }

        public void AddChannelObject(ObjectGuid guid)
        {
            AddDynamicUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelObjects), guid);
        }

        public void SetChannelObject(int slot, ObjectGuid guid)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelObjects, slot), guid);
        }

        public void ClearChannelObjects()
        {
            ClearDynamicUpdateFieldValues(Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelObjects));
        }

        public void RemoveChannelObject(ObjectGuid guid)
        {
            int index = UnitData.ChannelObjects.FindIndex(guid);

            if (index >= 0)
                RemoveDynamicUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelObjects), index);
        }

        public static bool IsDamageReducedByArmor(SpellSchoolMask schoolMask, SpellInfo spellInfo = null)
        {
            // only physical spells Damage gets reduced by armor
            if ((schoolMask & SpellSchoolMask.Normal) == 0)
                return false;

            return spellInfo == null || !spellInfo.HasAttribute(SpellCustomAttributes.IgnoreArmor);
        }

        public override UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
        {
            UpdateFieldFlag flags = UpdateFieldFlag.None;

            if (target == this ||
                GetOwnerGUID() == target.GetGUID())
                flags |= UpdateFieldFlag.Owner;

            if (HasDynamicFlag(UnitDynFlags.SpecialInfo))
                if (HasAuraTypeWithCaster(AuraType.Empathy, target.GetGUID()))
                    flags |= UpdateFieldFlag.Empath;

            return flags;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt8((byte)flags);
            ObjectData.WriteCreate(buffer, flags, this, target);
            UnitData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

            if (Values.HasChanged(TypeId.Object))
                ObjectData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.Unit))
                UnitData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            UpdateMask valuesMask = new(14);
            valuesMask.Set((int)TypeId.Unit);

            WorldPacket buffer = new();

            UpdateMask mask = new(194);
            UnitData.AppendAllowedFieldsMaskForFlag(mask, flags);
            UnitData.WriteUpdate(buffer, mask, true, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteUInt32(valuesMask.GetBlock(0));
            data.WriteBytes(buffer);
        }

        public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedUnitMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);

            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            UnitData.FilterDisallowedFieldsMaskForFlag(requestedUnitMask, flags);

            if (requestedUnitMask.IsAnySet())
                valuesMask.Set((int)TypeId.Unit);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Unit])
                UnitData.WriteUpdate(buffer, requestedUnitMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            Values.ClearChangesMask(UnitData);
            base.ClearUpdateMask(remove);
        }

        public override void DestroyForPlayer(Player target)
        {
            Battleground bg = target.GetBattleground();

            if (bg != null)
                if (bg.IsArena())
                {
                    DestroyArenaUnit destroyArenaUnit = new();
                    destroyArenaUnit.Guid = GetGUID();
                    target.SendPacket(destroyArenaUnit);
                }

            base.DestroyForPlayer(target);
        }

        public bool CanDualWield()
        {
            return CanDualWieldWep;
        }

        public virtual void SetCanDualWield(bool value)
        {
            CanDualWieldWep = value;
        }

        public DeathState GetDeathState()
        {
            return DeathState;
        }

        public bool HaveOffhandWeapon()
        {
            if (IsTypeId(TypeId.Player))
                return ToPlayer().GetWeaponForAttack(WeaponAttackType.OffAttack, true) != null;
            else
                return CanDualWieldWep;
        }

        public static void DealDamageMods(Unit attacker, Unit victim, ref uint damage)
        {
            if (victim == null ||
                !victim.IsAlive() ||
                victim.HasUnitState(UnitState.InFlight) ||
                (victim.IsTypeId(TypeId.Unit) && victim.ToCreature().IsInEvadeMode()))
                damage = 0;
        }

        public static void DealDamageMods(Unit attacker, Unit victim, ref uint damage, ref uint absorb)
        {
            if (victim == null ||
                !victim.IsAlive() ||
                victim.HasUnitState(UnitState.InFlight) ||
                (victim.IsTypeId(TypeId.Unit) && victim.ToCreature().IsEvadingAttacks()))
            {
                absorb += damage;
                damage = 0;

                return;
            }

            if (attacker != null)
                damage = (uint)(damage * attacker.GetDamageMultiplierForTarget(victim));
        }

        public static uint DealDamage(Unit attacker, Unit victim, uint damage, CleanDamage cleanDamage = null, DamageEffectType damagetype = DamageEffectType.Direct, SpellSchoolMask damageSchoolMask = SpellSchoolMask.Normal, SpellInfo spellProto = null, bool durabilityLoss = true)
        {
            UnitAI victimAI = victim.GetAI();

            victimAI?.DamageTaken(attacker, ref damage, damagetype, spellProto);

            UnitAI attackerAI = attacker ? attacker.GetAI() : null;

            attackerAI?.DamageDealt(victim, ref damage, damagetype);

            // Hook for OnDamage Event
            Global.ScriptMgr.ForEach<IUnitOnDamage>(p => p.OnDamage(attacker, victim, ref damage));

            // Signal to pets that their owner was attacked - except when DOT.
            if (attacker != victim &&
                damagetype != DamageEffectType.DOT)
                foreach (Unit controlled in victim.Controlled)
                {
                    Creature cControlled = controlled.ToCreature();

                    if (cControlled != null)
                    {
                        CreatureAI controlledAI = cControlled.GetAI();

                        controlledAI?.OwnerAttackedBy(attacker);
                    }
                }

            Player player = victim.ToPlayer();

            if (player != null &&
                player.GetCommandStatus(PlayerCommandStates.God))
                return 0;

            if (damagetype != DamageEffectType.NoDamage)
            {
                // interrupting Auras with SpellAuraInterruptFlags.Damage before checking !Damage (absorbed Damage breaks that Type of Auras)
                if (spellProto != null)
                {
                    if (!spellProto.HasAttribute(SpellAttr4.ReactiveDamageProc))
                        victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Damage, spellProto);
                }
                else
                {
                    victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Damage);
                }

                if (damage == 0 &&
                    damagetype != DamageEffectType.DOT &&
                    cleanDamage != null &&
                    cleanDamage.AbsorbedDamage != 0)
                    if (victim != attacker &&
                        victim.IsPlayer())
                    {
                        Spell spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);

                        if (spell != null)
                            if (spell.GetState() == SpellState.Preparing &&
                                spell._spellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamageAbsorb))
                                victim.InterruptNonMeleeSpells(false);
                    }

                // We're going to call functions which can modify content of the list during iteration over it's elements
                // Let's copy the list so we can prevent iterator invalidation
                var vCopyDamageCopy = victim.GetAuraEffectsByType(AuraType.ShareDamagePct);

                // copy Damage to casters of this aura
                foreach (var aura in vCopyDamageCopy)
                {
                    // Check if aura was removed during iteration - we don't need to work on such Auras
                    if (!aura.GetBase().IsAppliedOnTarget(victim.GetGUID()))
                        continue;

                    // check Damage school mask
                    if ((aura.GetMiscValue() & (int)damageSchoolMask) == 0)
                        continue;

                    Unit shareDamageTarget = aura.GetCaster();

                    if (shareDamageTarget == null)
                        continue;

                    SpellInfo spell = aura.GetSpellInfo();

                    uint share = MathFunctions.CalculatePct(damage, aura.GetAmount());

                    // @todo check packets if Damage is done by victim, or by Attacker of victim
                    DealDamageMods(attacker, shareDamageTarget, ref share);
                    DealDamage(attacker, shareDamageTarget, share, null, DamageEffectType.NoDamage, spell.GetSchoolMask(), spell, false);
                }
            }

            // Rage from Damage made (only from direct weapon Damage)
            if (attacker != null &&
                cleanDamage != null &&
                (cleanDamage.AttackType == WeaponAttackType.BaseAttack || cleanDamage.AttackType == WeaponAttackType.OffAttack) &&
                damagetype == DamageEffectType.Direct &&
                attacker != victim &&
                attacker.GetPowerType() == PowerType.Rage)
            {
                uint rage = (uint)(attacker.GetBaseAttackTime(cleanDamage.AttackType) / 1000.0f * 1.75f);

                if (cleanDamage.AttackType == WeaponAttackType.OffAttack)
                    rage /= 2;

                attacker.RewardRage(rage);
            }

            if (damage == 0)
                return 0;

            uint health = (uint)victim.GetHealth();

            // Duel ends when player has 1 or less hp
            bool duel_hasEnded = false;
            bool duel_wasMounted = false;

            if (victim.IsPlayer() &&
                victim.ToPlayer().Duel != null &&
                damage >= (health - 1))
            {
                if (!attacker)
                    return 0;

                // prevent kill only if killed in Duel and killed by opponent or opponent controlled creature
                if (victim.ToPlayer().Duel.Opponent == attacker.GetControllingPlayer())
                    damage = health - 1;

                duel_hasEnded = true;
            }
            else if (victim.IsVehicle() &&
                     damage >= (health - 1) &&
                     victim.GetCharmer() != null &&
                     victim.GetCharmer().IsTypeId(TypeId.Player))
            {
                Player victimRider = victim.GetCharmer().ToPlayer();

                if (victimRider != null &&
                    victimRider.Duel != null &&
                    victimRider.Duel.IsMounted)
                {
                    if (!attacker)
                        return 0;

                    // prevent kill only if killed in Duel and killed by opponent or opponent controlled creature
                    if (victimRider.Duel.Opponent == attacker.GetControllingPlayer())
                        damage = health - 1;

                    duel_wasMounted = true;
                    duel_hasEnded = true;
                }
            }

            if (attacker != null &&
                attacker != victim)
            {
                Player killer = attacker.ToPlayer();

                if (killer != null)
                {
                    // in bg, Count dmg if victim is also a player
                    if (victim.IsPlayer())
                    {
                        Battleground bg = killer.GetBattleground();

                        bg?.UpdatePlayerScore(killer, ScoreType.DamageDone, damage);
                    }

                    killer.UpdateCriteria(CriteriaType.DamageDealt, health > damage ? damage : health, 0, 0, victim);
                    killer.UpdateCriteria(CriteriaType.HighestDamageDone, damage);
                }
            }

            if (victim.IsPlayer())
                victim.ToPlayer().UpdateCriteria(CriteriaType.HighestDamageTaken, damage);

            if (attacker != null)
                damage = (uint)(damage / victim.GetHealthMultiplierForTarget(attacker));

            if (victim.GetTypeId() != TypeId.Player &&
                (!victim.IsControlledByPlayer() || victim.IsVehicle()))
            {
                victim.ToCreature().SetTappedBy(attacker);

                if (attacker == null ||
                    attacker.IsControlledByPlayer())
                    victim.ToCreature().LowerPlayerDamageReq(health < damage ? health : damage);
            }

            bool killed = false;
            bool skipSettingDeathState = false;

            if (health <= damage)
            {
                killed = true;

                if (victim.IsPlayer() &&
                    victim != attacker)
                    victim.ToPlayer().UpdateCriteria(CriteriaType.TotalDamageTaken, health);

                if (damagetype != DamageEffectType.NoDamage &&
                    damagetype != DamageEffectType.Self &&
                    victim.HasAuraType(AuraType.SchoolAbsorbOverkill))
                {
                    var vAbsorbOverkill = victim.GetAuraEffectsByType(AuraType.SchoolAbsorbOverkill);
                    DamageInfo damageInfo = new(attacker, victim, damage, spellProto, damageSchoolMask, damagetype, cleanDamage != null ? cleanDamage.AttackType : WeaponAttackType.BaseAttack);

                    foreach (var absorbAurEff in vAbsorbOverkill)
                    {
                        Aura baseAura = absorbAurEff.GetBase();
                        AuraApplication aurApp = baseAura.GetApplicationOfTarget(victim.GetGUID());

                        if (aurApp == null)
                            continue;

                        if ((absorbAurEff.GetMiscValue() & (int)damageInfo.GetSchoolMask()) == 0)
                            continue;

                        // cannot Absorb over limit
                        if (damage >= victim.CountPctFromMaxHealth(100 + absorbAurEff.GetMiscValueB()))
                            continue;

                        // get amount which can be still absorbed by the aura
                        int currentAbsorb = absorbAurEff.GetAmount();

                        // aura with infinite Absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
                        if (currentAbsorb < 0)
                            currentAbsorb = 0;

                        uint tempAbsorb = (uint)currentAbsorb;

                        // This aura Type is used both by Spirit of Redemption (death not really prevented, must grant all credit immediately) and Cheat Death (death prevented)
                        // repurpose PreventDefaultAction for this
                        bool deathFullyPrevented = false;

                        absorbAurEff.GetBase().CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref deathFullyPrevented);
                        currentAbsorb = (int)tempAbsorb;

                        // Absorb must be smaller than the Damage itself
                        currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, (int)damageInfo.GetDamage());
                        damageInfo.AbsorbDamage((uint)currentAbsorb);

                        if (deathFullyPrevented)
                            killed = false;

                        skipSettingDeathState = true;

                        if (currentAbsorb != 0)
                        {
                            SpellAbsorbLog absorbLog = new();
                            absorbLog.Attacker = attacker != null ? attacker.GetGUID() : ObjectGuid.Empty;
                            absorbLog.Victim = victim.GetGUID();
                            absorbLog.Caster = baseAura.GetCasterGUID();
                            absorbLog.AbsorbedSpellID = spellProto != null ? spellProto.Id : 0;
                            absorbLog.AbsorbSpellID = baseAura.GetId();
                            absorbLog.Absorbed = currentAbsorb;
                            absorbLog.OriginalDamage = damageInfo.GetOriginalDamage();
                            absorbLog.LogData.Initialize(victim);
                            victim.SendCombatLogMessage(absorbLog);
                        }
                    }

                    damage = damageInfo.GetDamage();
                }
            }

            if (spellProto != null &&
                spellProto.HasAttribute(SpellAttr3.NoDurabilityLoss))
                durabilityLoss = false;

            if (killed)
            {
                Kill(attacker, victim, durabilityLoss, skipSettingDeathState);
            }
            else
            {
                if (victim.IsTypeId(TypeId.Player))
                    victim.ToPlayer().UpdateCriteria(CriteriaType.TotalDamageTaken, damage);

                victim.ModifyHealth(-(int)damage);

                if (damagetype == DamageEffectType.Direct ||
                    damagetype == DamageEffectType.SpellDirect)
                    victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.NonPeriodicDamage, spellProto);

                if (!victim.IsTypeId(TypeId.Player))
                {
                    // Part of Evade mechanics. DoT's and Thorns / Retribution Aura do not contribute to this
                    if (damagetype != DamageEffectType.DOT &&
                        damage > 0 &&
                        !victim.GetOwnerGUID().IsPlayer() &&
                        (spellProto == null || !spellProto.HasAura(AuraType.DamageShield)))
                        victim.ToCreature().SetLastDamagedTime(GameTime.GetGameTime() + SharedConst.MaxAggroResetTime);

                    if (attacker != null &&
                        (spellProto == null || !spellProto.HasAttribute(SpellAttr4.NoHarmfulThreat)))
                        victim.GetThreatManager().AddThreat(attacker, damage, spellProto);
                }
                else // victim is a player
                {
                    // random durability for items (HIT TAKEN)
                    if (durabilityLoss && WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossDamage) > RandomHelper.randChance())
                    {
                        byte slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
                        victim.ToPlayer().DurabilityPointLossForEquipSlot(slot);
                    }
                }

                if (attacker != null &&
                    attacker.IsPlayer())
                    // random durability for items (HIT DONE)
                    if (durabilityLoss && RandomHelper.randChance(WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossDamage)))
                    {
                        byte slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
                        attacker.ToPlayer().DurabilityPointLossForEquipSlot(slot);
                    }

                if (damagetype != DamageEffectType.NoDamage &&
                    damagetype != DamageEffectType.DOT)
                {
                    if (victim != attacker &&
                        (spellProto == null || !(spellProto.HasAttribute(SpellAttr6.NoPushback) || spellProto.HasAttribute(SpellAttr7.NoPushbackOnDamage) || spellProto.HasAttribute(SpellAttr3.TreatAsPeriodic))))
                    {
                        Spell spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);

                        if (spell != null)
                            if (spell.GetState() == SpellState.Preparing)
                            {
                                bool isCastInterrupted()
                                {
                                    if (damage == 0)
                                        return spell._spellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.ZeroDamageCancels);

                                    if (victim.IsPlayer() &&
                                        spell._spellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamageCancelsPlayerOnly))
                                        return true;

                                    if (spell._spellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamageCancels))
                                        return true;

                                    return false;
                                }

                                ;

                                bool isCastDelayed()
                                {
                                    if (damage == 0)
                                        return false;

                                    if (victim.IsPlayer() &&
                                        spell._spellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamagePushbackPlayerOnly))
                                        return true;

                                    if (spell._spellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamagePushback))
                                        return true;

                                    return false;
                                }

                                if (isCastInterrupted())
                                    victim.InterruptNonMeleeSpells(false);
                                else if (isCastDelayed())
                                    spell.Delayed();
                            }
                    }

                    if (damage != 0 &&
                        victim.IsPlayer())
                    {
                        Spell spell1 = victim.GetCurrentSpell(CurrentSpellTypes.Channeled);

                        if (spell1 != null)
                            if (spell1.GetState() == SpellState.Casting &&
                                spell1._spellInfo.HasChannelInterruptFlag(SpellAuraInterruptFlags.DamageChannelDuration))
                                spell1.DelayedChannel();
                    }
                }

                // last Damage from Duel opponent
                if (duel_hasEnded)
                {
                    Player he = duel_wasMounted ? victim.GetCharmer().ToPlayer() : victim.ToPlayer();

                    Cypher.Assert(he && he.Duel != null);

                    if (duel_wasMounted) // In this case victim==Mount
                        victim.SetHealth(1);
                    else
                        he.SetHealth(1);

                    he.Duel.Opponent.CombatStopWithPets(true);
                    he.CombatStopWithPets(true);

                    he.CastSpell(he, 7267, true); // beg
                    he.DuelComplete(DuelCompleteType.Won);
                }
            }

            // make player victims stand up automatically
            if (victim.GetStandState() != 0 &&
                victim.IsPlayer())
                victim.SetStandState(UnitStandStateType.Stand);

            return damage;
        }

        public long ModifyHealth(long dVal)
        {
            long gain = 0;

            if (dVal == 0)
                return 0;

            long curHealth = (long)GetHealth();

            long val = dVal + curHealth;

            if (val <= 0)
            {
                SetHealth(0);

                return -curHealth;
            }

            long maxHealth = (long)GetMaxHealth();

            if (val < maxHealth)
            {
                SetHealth((ulong)val);
                gain = val - curHealth;
            }
            else if (curHealth != maxHealth)
            {
                SetHealth((ulong)maxHealth);
                gain = maxHealth - curHealth;
            }

            if (dVal < 0)
            {
                HealthUpdate packet = new();
                packet.Guid = GetGUID();
                packet.Health = (long)GetHealth();

                Player player = GetCharmerOrOwnerPlayerOrPlayerItself();

                if (player)
                    player.SendPacket(packet);
            }

            return gain;
        }

        public long GetHealthGain(long dVal)
        {
            long gain = 0;

            if (dVal == 0)
                return 0;

            long curHealth = (long)GetHealth();

            long val = dVal + curHealth;

            if (val <= 0)
                return -curHealth;

            long maxHealth = (long)GetMaxHealth();

            if (val < maxHealth)
                gain = dVal;
            else if (curHealth != maxHealth)
                gain = maxHealth - curHealth;

            return gain;
        }

        public bool IsImmuneToAll()
        {
            return IsImmuneToPC() && IsImmuneToNPC();
        }

        public void SetImmuneToAll(bool apply, bool keepCombat)
        {
            if (apply)
            {
                SetUnitFlag(UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
                ValidateAttackersAndOwnTarget();

                if (!keepCombat)
                    _combatManager.EndAllCombat();
            }
            else
            {
                RemoveUnitFlag(UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
            }
        }

        public virtual void SetImmuneToAll(bool apply)
        {
            SetImmuneToAll(apply, false);
        }

        public bool IsImmuneToPC()
        {
            return HasUnitFlag(UnitFlags.ImmuneToPc);
        }

        public void SetImmuneToPC(bool apply, bool keepCombat)
        {
            if (apply)
            {
                SetUnitFlag(UnitFlags.ImmuneToPc);
                ValidateAttackersAndOwnTarget();

                if (!keepCombat)
                {
                    List<CombatReference> toEnd = new();

                    foreach (var pair in _combatManager.GetPvECombatRefs())
                        if (pair.Value.GetOther(this).HasUnitFlag(UnitFlags.PlayerControlled))
                            toEnd.Add(pair.Value);

                    foreach (var pair in _combatManager.GetPvPCombatRefs())
                        if (pair.Value.GetOther(this).HasUnitFlag(UnitFlags.PlayerControlled))
                            toEnd.Add(pair.Value);

                    foreach (CombatReference refe in toEnd)
                        refe.EndCombat();
                }
            }
            else
            {
                RemoveUnitFlag(UnitFlags.ImmuneToPc);
            }
        }

        public virtual void SetImmuneToPC(bool apply)
        {
            SetImmuneToPC(apply, false);
        }

        public bool IsImmuneToNPC()
        {
            return HasUnitFlag(UnitFlags.ImmuneToNpc);
        }

        public void SetImmuneToNPC(bool apply, bool keepCombat)
        {
            if (apply)
            {
                SetUnitFlag(UnitFlags.ImmuneToNpc);
                ValidateAttackersAndOwnTarget();

                if (!keepCombat)
                {
                    List<CombatReference> toEnd = new();

                    foreach (var pair in _combatManager.GetPvECombatRefs())
                        if (!pair.Value.GetOther(this).HasUnitFlag(UnitFlags.PlayerControlled))
                            toEnd.Add(pair.Value);

                    foreach (var pair in _combatManager.GetPvPCombatRefs())
                        if (!pair.Value.GetOther(this).HasUnitFlag(UnitFlags.PlayerControlled))
                            toEnd.Add(pair.Value);

                    foreach (CombatReference refe in toEnd)
                        refe.EndCombat();
                }
            }
            else
            {
                RemoveUnitFlag(UnitFlags.ImmuneToNpc);
            }
        }

        public virtual void SetImmuneToNPC(bool apply)
        {
            SetImmuneToNPC(apply, false);
        }

        public virtual float GetBlockPercent(uint attackerLevel)
        {
            return 30.0f;
        }

        public void RewardRage(uint baseRage)
        {
            float addRage = baseRage;

            // talent who gave more rage on attack
            MathFunctions.AddPct(ref addRage, GetTotalAuraModifier(AuraType.ModRageFromDamageDealt));

            addRage *= WorldConfig.GetFloatValue(WorldCfg.RatePowerRageIncome);

            ModifyPower(PowerType.Rage, (int)(addRage * 10));
        }

        public float GetPPMProcChance(uint WeaponSpeed, float PPM, SpellInfo spellProto)
        {
            // proc per minute chance calculation
            if (PPM <= 0)
                return 0.0f;

            // Apply chance modifer aura
            if (spellProto != null)
            {
                Player modOwner = GetSpellModOwner();

                modOwner?.ApplySpellMod(spellProto, SpellModOp.ProcFrequency, ref PPM);
            }

            return (float)Math.Floor((WeaponSpeed * PPM) / 600.0f); // result is chance in percents (probability = Speed_in_sec * (PPM / 60))
        }

        public Unit GetNextRandomRaidMemberOrPet(float radius)
        {
            Player player = null;

            if (IsTypeId(TypeId.Player))
                player = ToPlayer();
            // Should we enable this also for charmed units?
            else if (IsTypeId(TypeId.Unit) &&
                     IsPet())
                player = GetOwner().ToPlayer();

            if (player == null)
                return null;

            Group group = player.GetGroup();

            // When there is no group check pet presence
            if (!group)
            {
                // We are pet now, return owner
                if (player != this)
                    return IsWithinDistInMap(player, radius) ? player : null;

                Unit pet = GetGuardianPet();

                // No pet, no group, nothing to return
                if (pet == null)
                    return null;

                // We are owner now, return pet
                return IsWithinDistInMap(pet, radius) ? pet : null;
            }

            List<Unit> nearMembers = new();
            // reserve place for players and pets because resizing vector every unit push is unefficient (vector is reallocated then)

            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player target = refe.GetSource();

                if (target)
                {
                    // IsHostileTo check Duel and controlled by enemy
                    if (target != this &&
                        IsWithinDistInMap(target, radius) &&
                        target.IsAlive() &&
                        !IsHostileTo(target))
                        nearMembers.Add(target);

                    // Push player's pet to vector
                    Unit pet = target.GetGuardianPet();

                    if (pet)
                        if (pet != this &&
                            IsWithinDistInMap(pet, radius) &&
                            pet.IsAlive() &&
                            !IsHostileTo(pet))
                            nearMembers.Add(pet);
                }
            }

            if (nearMembers.Empty())
                return null;

            int randTarget = RandomHelper.IRand(0, nearMembers.Count - 1);

            return nearMembers[randTarget];
        }

        public uint GetComboPoints()
        {
            return (uint)GetPower(PowerType.ComboPoints);
        }

        public void AddComboPoints(sbyte count, Spell spell = null)
        {
            if (count == 0)
                return;

            sbyte comboPoints = (sbyte)(spell != null ? spell._comboPointGain : GetPower(PowerType.ComboPoints));

            comboPoints += count;

            if (comboPoints > 5)
                comboPoints = 5;
            else if (comboPoints < 0)
                comboPoints = 0;

            if (!spell)
                SetPower(PowerType.ComboPoints, comboPoints);
            else
                spell._comboPointGain = comboPoints;
        }

        public void ClearComboPoints()
        {
            SetPower(PowerType.ComboPoints, 0);
        }

        public void ClearAllReactives()
        {
            for (ReactiveType i = 0; i < ReactiveType.Max; ++i)
                _reactiveTimer[i] = 0;

            if (HasAuraState(AuraStateType.Defensive))
                ModifyAuraState(AuraStateType.Defensive, false);

            if (HasAuraState(AuraStateType.Defensive2))
                ModifyAuraState(AuraStateType.Defensive2, false);
        }

        public virtual void SetPvP(bool state)
        {
            if (state)
                SetPvpFlag(UnitPVPStateFlags.PvP);
            else
                RemovePvpFlag(UnitPVPStateFlags.PvP);
        }

        public static void CalcAbsorbResist(DamageInfo damageInfo, Spell spell = null)
        {
            if (!damageInfo.GetVictim() ||
                !damageInfo.GetVictim().IsAlive() ||
                damageInfo.GetDamage() == 0)
                return;

            uint resistedDamage = CalcSpellResistedDamage(damageInfo.GetAttacker(), damageInfo.GetVictim(), damageInfo.GetDamage(), damageInfo.GetSchoolMask(), damageInfo.GetSpellInfo());

            // Ignore Absorption Auras
            float auraAbsorbMod = 0f;

            Unit attacker = damageInfo.GetAttacker();

            if (attacker != null)
                auraAbsorbMod = attacker.GetMaxPositiveAuraModifierByMiscMask(AuraType.ModTargetAbsorbSchool, (uint)damageInfo.GetSchoolMask());

            MathFunctions.RoundToInterval(ref auraAbsorbMod, 0.0f, 100.0f);

            int absorbIgnoringDamage = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), auraAbsorbMod);

            spell?.CallScriptOnResistAbsorbCalculateHandlers(damageInfo, ref resistedDamage, ref absorbIgnoringDamage);

            damageInfo.ResistDamage(resistedDamage);

            // We're going to call functions which can modify content of the list during iteration over it's elements
            // Let's copy the list so we can prevent iterator invalidation
            var vSchoolAbsorbCopy = damageInfo.GetVictim().GetAuraEffectsByType(AuraType.SchoolAbsorb);
            vSchoolAbsorbCopy.Sort(new AbsorbAuraOrderPred());

            // Absorb without mana cost
            for (var i = 0; i < vSchoolAbsorbCopy.Count && (damageInfo.GetDamage() > 0); ++i)
            {
                var absorbAurEff = vSchoolAbsorbCopy[i];

                // Check if aura was removed during iteration - we don't need to work on such Auras
                AuraApplication aurApp = absorbAurEff.GetBase().GetApplicationOfTarget(damageInfo.GetVictim().GetGUID());

                if (aurApp == null)
                    continue;

                if ((absorbAurEff.GetMiscValue() & (int)damageInfo.GetSchoolMask()) == 0)
                    continue;

                // get amount which can be still absorbed by the aura
                int currentAbsorb = absorbAurEff.GetAmount();

                // aura with infinite Absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
                if (currentAbsorb < 0)
                    currentAbsorb = 0;

                if (!absorbAurEff.GetSpellInfo().HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
                    damageInfo.ModifyDamage(-absorbIgnoringDamage);

                uint tempAbsorb = (uint)currentAbsorb;

                bool defaultPrevented = false;

                absorbAurEff.GetBase().CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref defaultPrevented);
                currentAbsorb = (int)tempAbsorb;

                if (!defaultPrevented)
                {
                    // Absorb must be smaller than the Damage itself
                    currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.GetDamage());

                    damageInfo.AbsorbDamage((uint)currentAbsorb);

                    tempAbsorb = (uint)currentAbsorb;
                    absorbAurEff.GetBase().CallScriptEffectAfterAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb);

                    // Check if our aura is using amount to Count heal
                    if (absorbAurEff.GetAmount() >= 0)
                    {
                        // Reduce shield amount
                        absorbAurEff.ChangeAmount(absorbAurEff.GetAmount() - currentAbsorb);

                        // Aura cannot Absorb anything more - remove it
                        if (absorbAurEff.GetAmount() <= 0)
                            absorbAurEff.GetBase().Remove(AuraRemoveMode.EnemySpell);
                    }
                }

                if (!absorbAurEff.GetSpellInfo().HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
                    damageInfo.ModifyDamage(absorbIgnoringDamage);

                if (currentAbsorb != 0)
                {
                    SpellAbsorbLog absorbLog = new();
                    absorbLog.Attacker = damageInfo.GetAttacker() != null ? damageInfo.GetAttacker().GetGUID() : ObjectGuid.Empty;
                    absorbLog.Victim = damageInfo.GetVictim().GetGUID();
                    absorbLog.Caster = absorbAurEff.GetBase().GetCasterGUID();
                    absorbLog.AbsorbedSpellID = damageInfo.GetSpellInfo() != null ? damageInfo.GetSpellInfo().Id : 0;
                    absorbLog.AbsorbSpellID = absorbAurEff.GetId();
                    absorbLog.Absorbed = currentAbsorb;
                    absorbLog.OriginalDamage = damageInfo.GetOriginalDamage();
                    absorbLog.LogData.Initialize(damageInfo.GetVictim());
                    damageInfo.GetVictim().SendCombatLogMessage(absorbLog);
                }
            }

            // Absorb by mana cost
            var vManaShieldCopy = damageInfo.GetVictim().GetAuraEffectsByType(AuraType.ManaShield);

            foreach (var absorbAurEff in vManaShieldCopy)
            {
                if (damageInfo.GetDamage() == 0)
                    break;

                // Check if aura was removed during iteration - we don't need to work on such Auras
                AuraApplication aurApp = absorbAurEff.GetBase().GetApplicationOfTarget(damageInfo.GetVictim().GetGUID());

                if (aurApp == null)
                    continue;

                // check Damage school mask
                if (!Convert.ToBoolean(absorbAurEff.GetMiscValue() & (int)damageInfo.GetSchoolMask()))
                    continue;

                // get amount which can be still absorbed by the aura
                int currentAbsorb = absorbAurEff.GetAmount();

                // aura with infinite Absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
                if (currentAbsorb < 0)
                    currentAbsorb = 0;

                if (!absorbAurEff.GetSpellInfo().HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
                    damageInfo.ModifyDamage(-absorbIgnoringDamage);

                uint tempAbsorb = (uint)currentAbsorb;

                bool defaultPrevented = false;

                absorbAurEff.GetBase().CallScriptEffectManaShieldHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref defaultPrevented);
                currentAbsorb = (int)tempAbsorb;

                if (!defaultPrevented)
                {
                    // Absorb must be smaller than the Damage itself
                    currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.GetDamage());

                    int manaReduction = currentAbsorb;

                    // lower Absorb amount by talents
                    float manaMultiplier = absorbAurEff.GetSpellEffectInfo().CalcValueMultiplier(absorbAurEff.GetCaster());

                    if (manaMultiplier != 0)
                        manaReduction = (int)(manaReduction * manaMultiplier);

                    int manaTaken = -damageInfo.GetVictim().ModifyPower(PowerType.Mana, -manaReduction);

                    // take case when mana has ended up into account
                    currentAbsorb = currentAbsorb != 0 ? (currentAbsorb * (manaTaken / manaReduction)) : 0;

                    damageInfo.AbsorbDamage((uint)currentAbsorb);

                    tempAbsorb = (uint)currentAbsorb;
                    absorbAurEff.GetBase().CallScriptEffectAfterManaShieldHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb);

                    // Check if our aura is using amount to Count Damage
                    if (absorbAurEff.GetAmount() >= 0)
                    {
                        absorbAurEff.ChangeAmount(absorbAurEff.GetAmount() - currentAbsorb);

                        if ((absorbAurEff.GetAmount() <= 0))
                            absorbAurEff.GetBase().Remove(AuraRemoveMode.EnemySpell);
                    }
                }

                if (!absorbAurEff.GetSpellInfo().HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
                    damageInfo.ModifyDamage(absorbIgnoringDamage);

                if (currentAbsorb != 0)
                {
                    SpellAbsorbLog absorbLog = new();
                    absorbLog.Attacker = damageInfo.GetAttacker() != null ? damageInfo.GetAttacker().GetGUID() : ObjectGuid.Empty;
                    absorbLog.Victim = damageInfo.GetVictim().GetGUID();
                    absorbLog.Caster = absorbAurEff.GetBase().GetCasterGUID();
                    absorbLog.AbsorbedSpellID = damageInfo.GetSpellInfo() != null ? damageInfo.GetSpellInfo().Id : 0;
                    absorbLog.AbsorbSpellID = absorbAurEff.GetId();
                    absorbLog.Absorbed = currentAbsorb;
                    absorbLog.OriginalDamage = damageInfo.GetOriginalDamage();
                    absorbLog.LogData.Initialize(damageInfo.GetVictim());
                    damageInfo.GetVictim().SendCombatLogMessage(absorbLog);
                }
            }

            // split Damage Auras - only when not damaging self
            if (damageInfo.GetVictim() != damageInfo.GetAttacker())
            {
                // We're going to call functions which can modify content of the list during iteration over it's elements
                // Let's copy the list so we can prevent iterator invalidation
                var vSplitDamagePctCopy = damageInfo.GetVictim().GetAuraEffectsByType(AuraType.SplitDamagePct);

                foreach (var itr in vSplitDamagePctCopy)
                {
                    if (damageInfo.GetDamage() == 0)
                        break;

                    // Check if aura was removed during iteration - we don't need to work on such Auras
                    AuraApplication aurApp = itr.GetBase().GetApplicationOfTarget(damageInfo.GetVictim().GetGUID());

                    if (aurApp == null)
                        continue;

                    // check Damage school mask
                    if (!Convert.ToBoolean(itr.GetMiscValue() & (int)damageInfo.GetSchoolMask()))
                        continue;

                    // Damage can be splitted only if aura has an alive caster
                    Unit caster = itr.GetCaster();

                    if (!caster ||
                        (caster == damageInfo.GetVictim()) ||
                        !caster.IsInWorld ||
                        !caster.IsAlive())
                        continue;

                    uint splitDamage = MathFunctions.CalculatePct(damageInfo.GetDamage(), itr.GetAmount());

                    itr.GetBase().CallScriptEffectSplitHandlers(itr, aurApp, damageInfo, splitDamage);

                    // Absorb must be smaller than the Damage itself
                    splitDamage = MathFunctions.RoundToInterval(ref splitDamage, 0, damageInfo.GetDamage());

                    damageInfo.AbsorbDamage(splitDamage);

                    // check if caster is immune to Damage
                    if (caster.IsImmunedToDamage(damageInfo.GetSchoolMask()))
                    {
                        damageInfo.GetVictim().SendSpellMiss(caster, itr.GetSpellInfo().Id, SpellMissInfo.Immune);

                        continue;
                    }

                    uint split_absorb = 0;
                    DealDamageMods(damageInfo.GetAttacker(), caster, ref splitDamage, ref split_absorb);

                    SpellNonMeleeDamage log = new(damageInfo.GetAttacker(), caster, itr.GetSpellInfo(), itr.GetBase().GetSpellVisual(), damageInfo.GetSchoolMask(), itr.GetBase().GetCastId());
                    CleanDamage cleanDamage = new(splitDamage, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);
                    DealDamage(damageInfo.GetAttacker(), caster, splitDamage, cleanDamage, DamageEffectType.Direct, damageInfo.GetSchoolMask(), itr.GetSpellInfo(), false);
                    log.Damage = splitDamage;
                    log.OriginalDamage = splitDamage;
                    log.Absorb = split_absorb;
                    caster.SendSpellNonMeleeDamageLog(log);

                    // break 'Fear' and similar Auras
                    ProcSkillsAndAuras(damageInfo.GetAttacker(), caster, new ProcFlagsInit(ProcFlags.None), new ProcFlagsInit(ProcFlags.TakeHarmfulSpell), ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, ProcFlagsHit.None, null, damageInfo, null);
                }
            }
        }

        public static void CalcHealAbsorb(HealInfo healInfo)
        {
            if (healInfo.GetHeal() == 0)
                return;

            // Need remove expired Auras after
            bool existExpired = false;

            // Absorb without mana cost
            var vHealAbsorb = healInfo.GetTarget().GetAuraEffectsByType(AuraType.SchoolHealAbsorb);

            for (var i = 0; i < vHealAbsorb.Count && healInfo.GetHeal() > 0; ++i)
            {
                AuraEffect absorbAurEff = vHealAbsorb[i];
                // Check if aura was removed during iteration - we don't need to work on such Auras
                AuraApplication aurApp = absorbAurEff.GetBase().GetApplicationOfTarget(healInfo.GetTarget().GetGUID());

                if (aurApp == null)
                    continue;

                if ((absorbAurEff.GetMiscValue() & (int)healInfo.GetSchoolMask()) == 0)
                    continue;

                // get amount which can be still absorbed by the aura
                int currentAbsorb = absorbAurEff.GetAmount();

                // aura with infinite Absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
                if (currentAbsorb < 0)
                    currentAbsorb = 0;

                uint tempAbsorb = (uint)currentAbsorb;

                bool defaultPrevented = false;

                absorbAurEff.GetBase().CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, healInfo, ref tempAbsorb, ref defaultPrevented);
                currentAbsorb = (int)tempAbsorb;

                if (!defaultPrevented)
                {
                    // Absorb must be smaller than the heal itself
                    currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, healInfo.GetHeal());

                    healInfo.AbsorbHeal((uint)currentAbsorb);

                    tempAbsorb = (uint)currentAbsorb;
                    absorbAurEff.GetBase().CallScriptEffectAfterAbsorbHandlers(absorbAurEff, aurApp, healInfo, ref tempAbsorb);

                    // Check if our aura is using amount to Count heal
                    if (absorbAurEff.GetAmount() >= 0)
                    {
                        // Reduce shield amount
                        absorbAurEff.ChangeAmount(absorbAurEff.GetAmount() - currentAbsorb);

                        // Aura cannot Absorb anything more - remove it
                        if (absorbAurEff.GetAmount() <= 0)
                            existExpired = true;
                    }
                }

                if (currentAbsorb != 0)
                {
                    SpellHealAbsorbLog absorbLog = new();
                    absorbLog.Healer = healInfo.GetHealer() ? healInfo.GetHealer().GetGUID() : ObjectGuid.Empty;
                    absorbLog.Target = healInfo.GetTarget().GetGUID();
                    absorbLog.AbsorbCaster = absorbAurEff.GetBase().GetCasterGUID();
                    absorbLog.AbsorbedSpellID = (int)(healInfo.GetSpellInfo() != null ? healInfo.GetSpellInfo().Id : 0);
                    absorbLog.AbsorbSpellID = (int)absorbAurEff.GetId();
                    absorbLog.Absorbed = currentAbsorb;
                    absorbLog.OriginalHeal = (int)healInfo.GetOriginalHeal();
                    healInfo.GetTarget().SendMessageToSet(absorbLog, true);
                }
            }

            // Remove all expired Absorb Auras
            if (existExpired)
                for (var i = 0; i < vHealAbsorb.Count;)
                {
                    AuraEffect auraEff = vHealAbsorb[i];
                    ++i;

                    if (auraEff.GetAmount() <= 0)
                    {
                        uint removedAuras = healInfo.GetTarget()._removedAurasCount;
                        auraEff.GetBase().Remove(AuraRemoveMode.EnemySpell);

                        if (removedAuras + 1 < healInfo.GetTarget()._removedAurasCount)
                            i = 0;
                    }
                }
        }

        public static uint CalcArmorReducedDamage(Unit attacker, Unit victim, uint damage, SpellInfo spellInfo, WeaponAttackType attackType = WeaponAttackType.Max, uint attackerLevel = 0)
        {
            float armor = victim.GetArmor();

            if (attacker != null)
            {
                armor *= victim.GetArmorMultiplierForTarget(attacker);

                // bypass enemy armor by SPELL_AURA_BYPASS_ARMOR_FOR_CASTER
                int armorBypassPct = 0;
                var reductionAuras = victim.GetAuraEffectsByType(AuraType.BypassArmorForCaster);

                foreach (var eff in reductionAuras)
                    if (eff.GetCasterGUID() == attacker.GetGUID())
                        armorBypassPct += eff.GetAmount();

                armor = MathFunctions.CalculatePct(armor, 100 - Math.Min(armorBypassPct, 100));

                // Ignore enemy armor by SPELL_AURA_MOD_TARGET_RESISTANCE aura
                armor += attacker.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)SpellSchoolMask.Normal);

                if (spellInfo != null)
                {
                    Player modOwner = attacker.GetSpellModOwner();

                    modOwner?.ApplySpellMod(spellInfo, SpellModOp.TargetResistance, ref armor);
                }

                var resIgnoreAuras = attacker.GetAuraEffectsByType(AuraType.ModIgnoreTargetResist);

                foreach (var eff in resIgnoreAuras)
                    if (eff.GetMiscValue().HasAnyFlag((int)SpellSchoolMask.Normal) &&
                        eff.IsAffectingSpell(spellInfo))
                        armor = (float)Math.Floor(MathFunctions.AddPct(ref armor, -eff.GetAmount()));

                // Apply Player CR_ARMOR_PENETRATION rating
                if (attacker.IsPlayer())
                {
                    float arpPct = attacker.ToPlayer().GetRatingBonusValue(CombatRating.ArmorPenetration);

                    // no more than 100%
                    MathFunctions.RoundToInterval(ref arpPct, 0.0f, 100.0f);

                    float maxArmorPen;

                    if (victim.GetLevelForTarget(attacker) < 60)
                        maxArmorPen = 400 + 85 * victim.GetLevelForTarget(attacker);
                    else
                        maxArmorPen = 400 + 85 * victim.GetLevelForTarget(attacker) + 4.5f * 85 * (victim.GetLevelForTarget(attacker) - 59);

                    // Cap armor penetration to this number
                    maxArmorPen = Math.Min((armor + maxArmorPen) / 3.0f, armor);
                    // Figure out how much armor do we ignore
                    armor -= MathFunctions.CalculatePct(maxArmorPen, arpPct);
                }
            }

            if (MathFunctions.fuzzyLe(armor, 0.0f))
                return damage;

            Class attackerClass = Class.Warrior;

            if (attacker != null)
            {
                attackerLevel = attacker.GetLevelForTarget(victim);
                attackerClass = attacker.GetClass();
            }

            // Expansion and ContentTuningID necessary? Does Player get a ContentTuningID too ?
            float armorConstant = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, attackerLevel, -2, 0, attackerClass);

            if ((armor + armorConstant) == 0)
                return damage;

            float mitigation = Math.Min(armor / (armor + armorConstant), 0.85f);

            return (uint)Math.Max(damage * (1.0f - mitigation), 0.0f);
        }

        public uint MeleeDamageBonusDone(Unit victim, uint damage, WeaponAttackType attType, DamageEffectType damagetype, SpellInfo spellProto = null, SpellEffectInfo spellEffectInfo = null, SpellSchoolMask damageSchoolMask = SpellSchoolMask.Normal)
        {
            if (victim == null ||
                damage == 0)
                return 0;

            uint creatureTypeMask = victim.GetCreatureTypeMask();

            // Done fixed Damage bonus Auras
            int DoneFlatBenefit = 0;

            // ..done
            DoneFlatBenefit += GetTotalAuraModifierByMiscMask(AuraType.ModDamageDoneCreature, (int)creatureTypeMask);

            // ..done
            // SPELL_AURA_MOD_DAMAGE_DONE included in weapon Damage

            // ..done (base at attack power for marked Target and base at attack power for creature Type)
            int APbonus = 0;

            if (attType == WeaponAttackType.RangedAttack)
            {
                APbonus += victim.GetTotalAuraModifier(AuraType.RangedAttackPowerAttackerBonus);

                // ..done (base at attack power and creature Type)
                APbonus += GetTotalAuraModifierByMiscMask(AuraType.ModRangedAttackPowerVersus, (int)creatureTypeMask);
            }
            else
            {
                APbonus += victim.GetTotalAuraModifier(AuraType.MeleeAttackPowerAttackerBonus);

                // ..done (base at attack power and creature Type)
                APbonus += GetTotalAuraModifierByMiscMask(AuraType.ModMeleeAttackPowerVersus, (int)creatureTypeMask);
            }

            if (APbonus != 0) // Can be negative
            {
                bool normalized = spellProto != null && spellProto.HasEffect(SpellEffectName.NormalizedWeaponDmg);
                DoneFlatBenefit += (int)(APbonus / 3.5f * GetAPMultiplier(attType, normalized));
            }

            // Done total percent Damage Auras
            float DoneTotalMod = 1.0f;

            SpellSchoolMask schoolMask = spellProto != null ? spellProto.GetSchoolMask() : damageSchoolMask;

            if ((schoolMask & SpellSchoolMask.Normal) == 0)
                // Some spells don't benefit from pct done mods
                // mods for SPELL_SCHOOL_MASK_NORMAL are already factored in base melee Damage calculation
                if (spellProto == null ||
                    !spellProto.HasAttribute(SpellAttr6.IgnoreCasterDamageModifiers))
                {
                    float maxModDamagePercentSchool = 0.0f;
                    Player thisPlayer = ToPlayer();

                    if (thisPlayer != null)
                    {
                        for (var i = SpellSchools.Holy; i < SpellSchools.Max; ++i)
                            if (Convert.ToBoolean((int)schoolMask & (1 << (int)i)))
                                maxModDamagePercentSchool = Math.Max(maxModDamagePercentSchool, thisPlayer.ActivePlayerData.ModDamageDonePercent[(int)i]);
                    }
                    else
                    {
                        maxModDamagePercentSchool = GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentDone, (uint)schoolMask);
                    }

                    DoneTotalMod *= maxModDamagePercentSchool;
                }

            if (spellProto == null)
                // melee attack
                foreach (AuraEffect autoAttackDamage in GetAuraEffectsByType(AuraType.ModAutoAttackDamage))
                    MathFunctions.AddPct(ref DoneTotalMod, autoAttackDamage.GetAmount());

            DoneTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamageDoneVersus, creatureTypeMask);

            // bonus against aurastate
            DoneTotalMod *= GetTotalAuraMultiplier(AuraType.ModDamageDoneVersusAurastate,
                                                   aurEff =>
                                                   {
                                                       if (victim.HasAuraState((AuraStateType)aurEff.GetMiscValue()))
                                                           return true;

                                                       return false;
                                                   });

            // Add SPELL_AURA_MOD_DAMAGE_DONE_FOR_MECHANIC percent bonus
            if (spellEffectInfo != null &&
                spellEffectInfo.Mechanic != 0)
                MathFunctions.AddPct(ref DoneTotalMod, GetTotalAuraModifierByMiscValue(AuraType.ModDamageDoneForMechanic, (int)spellEffectInfo.Mechanic));
            else if (spellProto != null &&
                     spellProto.Mechanic != 0)
                MathFunctions.AddPct(ref DoneTotalMod, GetTotalAuraModifierByMiscValue(AuraType.ModDamageDoneForMechanic, (int)spellProto.Mechanic));

            float damageF = damage;

            // apply spellmod to Done Damage
            if (spellProto != null)
            {
                Player modOwner = GetSpellModOwner();

                modOwner?.ApplySpellMod(spellProto, damagetype == DamageEffectType.DOT ? SpellModOp.PeriodicHealingAndDamage : SpellModOp.HealingAndDamage, ref damageF);
            }

            damageF = (damageF + DoneFlatBenefit) * DoneTotalMod;

            // bonus result can be negative
            return (uint)Math.Max(damageF, 0.0f);
        }

        public uint MeleeDamageBonusTaken(Unit attacker, uint pdamage, WeaponAttackType attType, DamageEffectType damagetype, SpellInfo spellProto = null, SpellSchoolMask damageSchoolMask = SpellSchoolMask.Normal)
        {
            if (pdamage == 0)
                return 0;

            int TakenFlatBenefit = 0;

            // ..taken
            TakenFlatBenefit += GetTotalAuraModifierByMiscMask(AuraType.ModDamageTaken, (int)attacker.GetMeleeDamageSchoolMask());

            if (attType != WeaponAttackType.RangedAttack)
                TakenFlatBenefit += GetTotalAuraModifier(AuraType.ModMeleeDamageTaken);
            else
                TakenFlatBenefit += GetTotalAuraModifier(AuraType.ModRangedDamageTaken);

            if ((TakenFlatBenefit < 0) &&
                (pdamage < -TakenFlatBenefit))
                return 0;

            // Taken total percent Damage Auras
            float TakenTotalMod = 1.0f;

            // ..taken
            TakenTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentTaken, (uint)attacker.GetMeleeDamageSchoolMask());

            // .. taken pct (special attacks)
            if (spellProto != null)
            {
                // From caster spells
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSchoolMaskDamageFromCaster, aurEff => { return aurEff.GetCasterGUID() == attacker.GetGUID() && (aurEff.GetMiscValue() & (int)spellProto.GetSchoolMask()) != 0; });

                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSpellDamageFromCaster, aurEff => { return aurEff.GetCasterGUID() == attacker.GetGUID() && aurEff.IsAffectingSpell(spellProto); });

                // Mod Damage from spell mechanic
                ulong mechanicMask = spellProto.GetAllEffectsMechanicMask();

                // Shred, Maul - "Effects which increase Bleed Damage also increase Shred Damage"
                if (spellProto.SpellFamilyName == SpellFamilyNames.Druid &&
                    spellProto.SpellFamilyFlags[0].HasAnyFlag(0x00008800u))
                    mechanicMask |= (1 << (int)Mechanics.Bleed);

                if (mechanicMask != 0)
                    TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMechanicDamageTakenPercent,
                                                            aurEff =>
                                                            {
                                                                if ((mechanicMask & (1ul << (aurEff.GetMiscValue()))) != 0)
                                                                    return true;

                                                                return false;
                                                            });

                if (damagetype == DamageEffectType.DOT)
                    TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModPeriodicDamageTaken, aurEff => (aurEff.GetMiscValue() & (uint)spellProto.GetSchoolMask()) != 0);
            }
            else // melee attack
            {
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMeleeDamageFromCaster, aurEff => { return aurEff.GetCasterGUID() == attacker.GetGUID(); });
            }

            AuraEffect cheatDeath = GetAuraEffect(45182, 0);

            if (cheatDeath != null)
                MathFunctions.AddPct(ref TakenTotalMod, cheatDeath.GetAmount());

            if (attType != WeaponAttackType.RangedAttack)
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMeleeDamageTakenPct);
            else
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModRangedDamageTakenPct);

            // Versatility
            Player modOwner = GetSpellModOwner();

            if (modOwner)
            {
                // only 50% of SPELL_AURA_MOD_VERSATILITY for Damage reduction
                float versaBonus = modOwner.GetTotalAuraModifier(AuraType.ModVersatility) / 2.0f;
                MathFunctions.AddPct(ref TakenTotalMod, -(modOwner.GetRatingBonusValue(CombatRating.VersatilityDamageTaken) + versaBonus));
            }

            // Sanctified Wrath (bypass Damage reduction)
            if (TakenTotalMod < 1.0f)
            {
                SpellSchoolMask attackSchoolMask = spellProto != null ? spellProto.GetSchoolMask() : damageSchoolMask;

                float damageReduction = 1.0f - TakenTotalMod;
                var casterIgnoreResist = attacker.GetAuraEffectsByType(AuraType.ModIgnoreTargetResist);

                foreach (AuraEffect aurEff in casterIgnoreResist)
                {
                    if ((aurEff.GetMiscValue() & (int)attackSchoolMask) == 0)
                        continue;

                    MathFunctions.AddPct(ref damageReduction, -aurEff.GetAmount());
                }

                TakenTotalMod = 1.0f - damageReduction;
            }

            float tmpDamage = (float)(pdamage + TakenFlatBenefit) * TakenTotalMod;

            return (uint)Math.Max(tmpDamage, 0.0f);
        }

        public virtual SpellSchoolMask GetMeleeDamageSchoolMask(WeaponAttackType attackType = WeaponAttackType.BaseAttack)
        {
            return SpellSchoolMask.None;
        }

        public virtual void UpdateDamageDoneMods(WeaponAttackType attackType, int skipEnchantSlot = -1)
        {
            UnitMods unitMod = attackType switch
            {
                WeaponAttackType.BaseAttack => UnitMods.DamageMainHand,
                WeaponAttackType.OffAttack => UnitMods.DamageOffHand,
                WeaponAttackType.RangedAttack => UnitMods.DamageRanged,
                _ => throw new NotImplementedException()
            };

            float amount = GetTotalAuraModifier(AuraType.ModDamageDone,
                                                aurEff =>
                                                {
                                                    if ((aurEff.GetMiscValue() & (int)SpellSchoolMask.Normal) == 0)
                                                        return false;

                                                    return CheckAttackFitToAuraRequirement(attackType, aurEff);
                                                });

            SetStatFlatModifier(unitMod, UnitModifierFlatType.Total, amount);
        }

        public void UpdateAllDamageDoneMods()
        {
            for (var attackType = WeaponAttackType.BaseAttack; attackType < WeaponAttackType.Max; ++attackType)
                UpdateDamageDoneMods(attackType);
        }

        public void UpdateDamagePctDoneMods(WeaponAttackType attackType)
        {
            (UnitMods unitMod, float factor) = attackType switch
            {
                WeaponAttackType.BaseAttack => (UnitMods.DamageMainHand, 1.0f),
                WeaponAttackType.OffAttack => (UnitMods.DamageOffHand, 0.5f),
                WeaponAttackType.RangedAttack => (UnitMods.DamageRanged, 1.0f),
                _ => throw new NotImplementedException()
            };

            factor *= GetTotalAuraMultiplier(AuraType.ModDamagePercentDone,
                                             aurEff =>
                                             {
                                                 if (!aurEff.GetMiscValue().HasAnyFlag((int)SpellSchoolMask.Normal))
                                                     return false;

                                                 return CheckAttackFitToAuraRequirement(attackType, aurEff);
                                             });

            if (attackType == WeaponAttackType.OffAttack)
                factor *= GetTotalAuraMultiplier(AuraType.ModOffhandDamagePct, auraEffect => CheckAttackFitToAuraRequirement(attackType, auraEffect));

            SetStatPctModifier(unitMod, UnitModifierPctType.Total, factor);
        }

        public void UpdateAllDamagePctDoneMods()
        {
            for (var attackType = WeaponAttackType.BaseAttack; attackType < WeaponAttackType.Max; ++attackType)
                UpdateDamagePctDoneMods(attackType);
        }

        public CombatManager GetCombatManager()
        {
            return _combatManager;
        }

        // Exposes the threat manager directly - be careful when interfacing with this
        // As a general rule of thumb, any unit pointer MUST be null checked BEFORE passing it to threatmanager methods
        // threatmanager will NOT null check your pointers for you - misuse = crash
        public ThreatManager GetThreatManager()
        {
            return _threatManager;
        }

        private void _UpdateSpells(uint diff)
        {
            _spellHistory.Update();

            if (GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null)
                _UpdateAutoRepeatSpell();

            for (CurrentSpellTypes i = 0; i < CurrentSpellTypes.Max; ++i)
                if (GetCurrentSpell(i) != null &&
                    CurrentSpells[i].GetState() == SpellState.Finished)
                {
                    CurrentSpells[i].SetReferencedFromCurrent(false);
                    CurrentSpells[i] = null;
                }

            foreach (var app in GetOwnedAuras())
            {
                Aura i_aura = app.Value;

                if (i_aura == null)
                    continue;

                i_aura.UpdateOwner(diff, this);
            }

            // remove expired Auras - do that after updates(used in scripts?)
            foreach (var pair in GetOwnedAuras())
                if (pair.Value != null &&
                    pair.Value.IsExpired())
                    RemoveOwnedAura(pair, AuraRemoveMode.Expire);

            foreach (var aura in _visibleAurasToUpdate)
                aura.ClientUpdate();

            _visibleAurasToUpdate.Clear();

            _DeleteRemovedAuras();

            if (!GameObj.Empty())
                for (var i = 0; i < GameObj.Count; ++i)
                {
                    GameObject go = GameObj[i];

                    if (!go.IsSpawned())
                    {
                        go.SetOwnerGUID(ObjectGuid.Empty);
                        go.SetRespawnTime(0);
                        go.Delete();
                        GameObj.Remove(go);
                    }
                }
        }

        private Unit GetVehicleRoot()
        {
            Unit vehicleRoot = GetVehicleBase();

            if (!vehicleRoot)
                return null;

            for (; ; )
            {
                if (!vehicleRoot.GetVehicleBase())
                    return vehicleRoot;

                vehicleRoot = vehicleRoot.GetVehicleBase();
            }
        }

        private List<DynamicObject> GetDynObjects(uint spellId)
        {
            List<DynamicObject> dynamicobjects = new();

            foreach (var obj in DynObj)
                if (obj.GetSpellId() == spellId)
                    dynamicobjects.Add(obj);

            return dynamicobjects;
        }

        private List<GameObject> GetGameObjects(uint spellId)
        {
            List<GameObject> gameobjects = new();

            foreach (var obj in GameObj)
                if (obj.GetSpellId() == spellId)
                    gameobjects.Add(obj);

            return gameobjects;
        }

        private void CancelSpellMissiles(uint spellId, bool reverseMissile = false)
        {
            bool hasMissile = false;

            foreach (var pair in Events.GetEvents())
            {
                Spell spell = Spell.ExtractSpellFromEvent(pair.Value);

                if (spell != null)
                    if (spell.GetSpellInfo().Id == spellId)
                    {
                        pair.Value.ScheduleAbort();
                        hasMissile = true;
                    }
            }

            if (hasMissile)
            {
                MissileCancel packet = new();
                packet.OwnerGUID = GetGUID();
                packet.SpellID = spellId;
                packet.Reverse = reverseMissile;
                SendMessageToSet(packet, false);
            }
        }

        private void RestoreDisabledAI()
        {
            // Keep popping the stack until we either reach the bottom or find a valid AI
            while (PopAI())
                if (GetTopAI() != null &&
                    GetTopAI() is not ScheduledChangeAI)
                    return;
        }

        private UnitAI GetScheduledChangeAI()
        {
            Creature creature = ToCreature();

            if (creature != null)
                return new ScheduledChangeAI(creature);
            else
                return null;
        }

        private bool HasScheduledAIChange()
        {
            UnitAI ai = GetAI();

            if (ai != null)
                return ai is ScheduledChangeAI;
            else
                return true;
        }

        private void RemoveAllFollowers()
        {
            while (!_followingMe.Empty())
                _followingMe[0].SetTarget(null);
        }

        private bool HasInterruptFlag(SpellAuraInterruptFlags flags)
        {
            return _interruptMask.HasAnyFlag(flags);
        }

        private bool HasInterruptFlag(SpellAuraInterruptFlags2 flags)
        {
            return _interruptMask2.HasAnyFlag(flags);
        }

        private void _UpdateAutoRepeatSpell()
        {
            SpellInfo autoRepeatSpellInfo = CurrentSpells[CurrentSpellTypes.AutoRepeat]._spellInfo;

            // check "realtime" interrupts
            // don't cancel spells which are affected by a SPELL_AURA_CAST_WHILE_WALKING effect
            if ((IsMoving() && GetCurrentSpell(CurrentSpellTypes.AutoRepeat).CheckMovement() != SpellCastResult.SpellCastOk) ||
                IsNonMeleeSpellCast(false, false, true, autoRepeatSpellInfo.Id == 75))
            {
                // cancel wand shoot
                if (autoRepeatSpellInfo.Id != 75)
                    InterruptSpell(CurrentSpellTypes.AutoRepeat);

                return;
            }

            // castroutine
            if (IsAttackReady(WeaponAttackType.RangedAttack) &&
                GetCurrentSpell(CurrentSpellTypes.AutoRepeat).GetState() != SpellState.Preparing)
            {
                // Check if able to cast
                SpellCastResult result = CurrentSpells[CurrentSpellTypes.AutoRepeat].CheckCast(true);

                if (result != SpellCastResult.SpellCastOk)
                {
                    if (autoRepeatSpellInfo.Id != 75)
                        InterruptSpell(CurrentSpellTypes.AutoRepeat);
                    else if (GetTypeId() == TypeId.Player)
                        Spell.SendCastResult(ToPlayer(), autoRepeatSpellInfo, CurrentSpells[CurrentSpellTypes.AutoRepeat]._SpellVisual, CurrentSpells[CurrentSpellTypes.AutoRepeat]._castId, result);

                    return;
                }

                // we want to shoot
                Spell spell = new(this, autoRepeatSpellInfo, TriggerCastFlags.IgnoreGCD);
                spell.Prepare(CurrentSpells[CurrentSpellTypes.AutoRepeat]._targets);
            }
        }

        private uint GetCosmeticMountDisplayId()
        {
            return UnitData.CosmeticMountDisplayID;
        }

        private Player GetControllingPlayer()
        {
            ObjectGuid guid = GetCharmerOrOwnerGUID();

            if (!guid.IsEmpty())
            {
                Unit master = Global.ObjAccessor.GetUnit(this, guid);

                if (master != null)
                    return master.GetControllingPlayer();

                return null;
            }
            else
            {
                return ToPlayer();
            }
        }

        private void StartReactiveTimer(ReactiveType reactive)
        {
            _reactiveTimer[reactive] = 4000;
        }

        private void DealMeleeDamage(CalcDamageInfo damageInfo, bool durabilityLoss)
        {
            Unit victim = damageInfo.Target;

            if (!victim.IsAlive() ||
                victim.HasUnitState(UnitState.InFlight) ||
                (victim.IsTypeId(TypeId.Unit) && victim.ToCreature().IsEvadingAttacks()))
                return;

            if (damageInfo.TargetState == VictimState.Parry &&
                (!victim.IsCreature() || victim.ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoParryHasten)))
            {
                // Get attack timers
                float offtime = victim.GetAttackTimer(WeaponAttackType.OffAttack);
                float basetime = victim.GetAttackTimer(WeaponAttackType.BaseAttack);

                // Reduce attack Time
                if (victim.HaveOffhandWeapon() &&
                    offtime < basetime)
                {
                    float percent20 = victim.GetBaseAttackTime(WeaponAttackType.OffAttack) * 0.20f;
                    float percent60 = 3.0f * percent20;

                    if (offtime > percent20 &&
                        offtime <= percent60)
                    {
                        victim.SetAttackTimer(WeaponAttackType.OffAttack, (uint)percent20);
                    }
                    else if (offtime > percent60)
                    {
                        offtime -= 2.0f * percent20;
                        victim.SetAttackTimer(WeaponAttackType.OffAttack, (uint)offtime);
                    }
                }
                else
                {
                    float percent20 = victim.GetBaseAttackTime(WeaponAttackType.BaseAttack) * 0.20f;
                    float percent60 = 3.0f * percent20;

                    if (basetime > percent20 &&
                        basetime <= percent60)
                    {
                        victim.SetAttackTimer(WeaponAttackType.BaseAttack, (uint)percent20);
                    }
                    else if (basetime > percent60)
                    {
                        basetime -= 2.0f * percent20;
                        victim.SetAttackTimer(WeaponAttackType.BaseAttack, (uint)basetime);
                    }
                }
            }

            // Call default DealDamage
            CleanDamage cleanDamage = new(damageInfo.CleanDamage, damageInfo.Absorb, damageInfo.AttackType, damageInfo.HitOutCome);
            DealDamage(this, victim, damageInfo.Damage, cleanDamage, DamageEffectType.Direct, (SpellSchoolMask)damageInfo.DamageSchoolMask, null, durabilityLoss);

            // If this is a creature and it attacks from behind it has a probability to daze it's victim
            if ((damageInfo.HitOutCome == MeleeHitOutcome.Crit || damageInfo.HitOutCome == MeleeHitOutcome.Crushing || damageInfo.HitOutCome == MeleeHitOutcome.Normal || damageInfo.HitOutCome == MeleeHitOutcome.Glancing) &&
                !IsTypeId(TypeId.Player) &&
                !ToCreature().IsControlledByPlayer() &&
                !victim.HasInArc(MathFunctions.PI, this) &&
                (victim.IsTypeId(TypeId.Player) || !victim.ToCreature().IsWorldBoss()) &&
                !victim.IsVehicle())
            {
                // 20% base chance
                float chance = 20.0f;

                // there is a newbie protection, at level 10 just 7% base chance; assuming linear function
                if (victim.GetLevel() < 30)
                    chance = 0.65f * victim.GetLevelForTarget(this) + 0.5f;

                uint victimDefense = victim.GetMaxSkillValueForLevel(this);
                uint attackerMeleeSkill = GetMaxSkillValueForLevel();

                chance *= attackerMeleeSkill / (float)victimDefense * 0.16f;

                // -probability is between 0% and 40%
                MathFunctions.RoundToInterval(ref chance, 0.0f, 40.0f);

                if (RandomHelper.randChance(chance))
                    CastSpell(victim, 1604, true);
            }

            if (IsTypeId(TypeId.Player))
            {
                DamageInfo dmgInfo = new(damageInfo);
                ToPlayer().CastItemCombatSpell(dmgInfo);
            }

            // Do effect if any Damage done to Target
            if (damageInfo.Damage != 0)
            {
                // We're going to call functions which can modify content of the list during iteration over it's elements
                // Let's copy the list so we can prevent iterator invalidation
                var vDamageShieldsCopy = victim.GetAuraEffectsByType(AuraType.DamageShield);

                foreach (var dmgShield in vDamageShieldsCopy)
                {
                    SpellInfo spellInfo = dmgShield.GetSpellInfo();

                    // Damage shield can be resisted...
                    var missInfo = victim.SpellHitResult(this, spellInfo, false);

                    if (missInfo != SpellMissInfo.None)
                    {
                        victim.SendSpellMiss(this, spellInfo.Id, missInfo);

                        continue;
                    }

                    // ...or immuned
                    if (IsImmunedToDamage(spellInfo))
                    {
                        victim.SendSpellDamageImmune(this, spellInfo.Id, false);

                        continue;
                    }

                    uint damage = (uint)dmgShield.GetAmount();
                    Unit caster = dmgShield.GetCaster();

                    if (caster)
                    {
                        damage = caster.SpellDamageBonusDone(this, spellInfo, damage, DamageEffectType.SpellDirect, dmgShield.GetSpellEffectInfo());
                        damage = SpellDamageBonusTaken(caster, spellInfo, damage, DamageEffectType.SpellDirect);
                    }

                    DamageInfo damageInfo1 = new(this, victim, damage, spellInfo, spellInfo.GetSchoolMask(), DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
                    CalcAbsorbResist(damageInfo1);
                    damage = damageInfo1.GetDamage();

                    DealDamageMods(victim, this, ref damage);

                    SpellDamageShield damageShield = new();
                    damageShield.Attacker = victim.GetGUID();
                    damageShield.Defender = GetGUID();
                    damageShield.SpellID = spellInfo.Id;
                    damageShield.TotalDamage = damage;
                    damageShield.OriginalDamage = (int)damageInfo.OriginalDamage;
                    damageShield.OverKill = (uint)Math.Max(damage - GetHealth(), 0);
                    damageShield.SchoolMask = (uint)spellInfo.SchoolMask;
                    damageShield.LogAbsorbed = damageInfo1.GetAbsorb();

                    DealDamage(victim, this, damage, null, DamageEffectType.SpellDirect, spellInfo.GetSchoolMask(), spellInfo, true);
                    damageShield.LogData.Initialize(this);

                    victim.SendCombatLogMessage(damageShield);
                }
            }
        }

        private void TriggerOnHealthChangeAuras(ulong oldVal, ulong newVal)
        {
            foreach (AuraEffect effect in GetAuraEffectsByType(AuraType.TriggerSpellOnHealthPct))
            {
                int triggerHealthPct = effect.GetAmount();
                uint triggerSpell = effect.GetSpellEffectInfo().TriggerSpell;
                ulong threshold = CountPctFromMaxHealth(triggerHealthPct);

                switch ((AuraTriggerOnHealthChangeDirection)effect.GetMiscValue())
                {
                    case AuraTriggerOnHealthChangeDirection.Above:
                        if (newVal < threshold ||
                            oldVal > threshold)
                            continue;

                        break;
                    case AuraTriggerOnHealthChangeDirection.Below:
                        if (newVal > threshold ||
                            oldVal < threshold)
                            continue;

                        break;
                    default:
                        break;
                }

                CastSpell(this, triggerSpell, new CastSpellExtraArgs(effect));
            }
        }

        private void UpdateReactives(uint p_time)
        {
            for (ReactiveType reactive = 0; reactive < ReactiveType.Max; ++reactive)
            {
                if (!_reactiveTimer.ContainsKey(reactive))
                    continue;

                if (_reactiveTimer[reactive] <= p_time)
                {
                    _reactiveTimer[reactive] = 0;

                    switch (reactive)
                    {
                        case ReactiveType.Defense:
                            if (HasAuraState(AuraStateType.Defensive))
                                ModifyAuraState(AuraStateType.Defensive, false);

                            break;
                        case ReactiveType.Defense2:
                            if (HasAuraState(AuraStateType.Defensive2))
                                ModifyAuraState(AuraStateType.Defensive2, false);

                            break;
                    }
                }
                else
                {
                    _reactiveTimer[reactive] -= p_time;
                }
            }
        }

        private void GainSpellComboPoints(sbyte count)
        {
            if (count == 0)
                return;

            sbyte cp = (sbyte)GetPower(PowerType.ComboPoints);

            cp += count;

            if (cp > 5) cp = 5;
            else if (cp < 0) cp = 0;

            SetPower(PowerType.ComboPoints, cp);
        }

        private static uint CalcSpellResistedDamage(Unit attacker, Unit victim, uint damage, SpellSchoolMask schoolMask, SpellInfo spellInfo)
        {
            // Magic Damage, check for resists
            if (!Convert.ToBoolean(schoolMask & SpellSchoolMask.Magic))
                return 0;

            // Npcs can have holy resistance
            if (schoolMask.HasAnyFlag(SpellSchoolMask.Holy) &&
                victim.GetTypeId() != TypeId.Unit)
                return 0;

            float averageResist = CalculateAverageResistReduction(attacker, schoolMask, victim, spellInfo);

            float[] discreteResistProbability = new float[11];

            if (averageResist <= 0.1f)
            {
                discreteResistProbability[0] = 1.0f - 7.5f * averageResist;
                discreteResistProbability[1] = 5.0f * averageResist;
                discreteResistProbability[2] = 2.5f * averageResist;
            }
            else
            {
                for (uint i = 0; i < 11; ++i)
                    discreteResistProbability[i] = Math.Max(0.5f - 2.5f * Math.Abs(0.1f * i - averageResist), 0.0f);
            }

            float roll = (float)RandomHelper.NextDouble();
            float probabilitySum = 0.0f;

            uint resistance = 0;

            for (; resistance < 11; ++resistance)
                if (roll < (probabilitySum += discreteResistProbability[resistance]))
                    break;

            float damageResisted = damage * resistance / 10f;

            if (damageResisted > 0.0f) // if any Damage was resisted
            {
                int ignoredResistance = 0;

                if (attacker != null)
                    ignoredResistance += attacker.GetTotalAuraModifierByMiscMask(AuraType.ModIgnoreTargetResist, (int)schoolMask);

                ignoredResistance = Math.Min(ignoredResistance, 100);
                MathFunctions.ApplyPct(ref damageResisted, 100 - ignoredResistance);

                // Spells with melee and magic school mask, decide whether resistance or armor Absorb is higher
                if (spellInfo != null &&
                    spellInfo.HasAttribute(SpellCustomAttributes.SchoolmaskNormalWithMagic))
                {
                    uint damageAfterArmor = CalcArmorReducedDamage(attacker, victim, damage, spellInfo, spellInfo.GetAttackType());
                    float armorReduction = damage - damageAfterArmor;

                    // pick the lower one, the weakest resistance counts
                    damageResisted = Math.Min(damageResisted, armorReduction);
                }
            }

            damageResisted = Math.Max(damageResisted, 0.0f);

            return (uint)damageResisted;
        }

        private static float CalculateAverageResistReduction(WorldObject caster, SpellSchoolMask schoolMask, Unit victim, SpellInfo spellInfo = null)
        {
            float victimResistance = victim.GetResistance(schoolMask);

            if (caster != null)
            {
                // pets inherit 100% of masters penetration
                Player player = caster.GetSpellModOwner();

                if (player != null)
                {
                    victimResistance += player.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)schoolMask);
                    victimResistance -= player.GetSpellPenetrationItemMod();
                }
                else
                {
                    Unit unitCaster = caster.ToUnit();

                    if (unitCaster != null)
                        victimResistance += unitCaster.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)schoolMask);
                }
            }

            // holy resistance exists in pve and comes from level difference, ignore template values
            if (schoolMask.HasAnyFlag(SpellSchoolMask.Holy))
                victimResistance = 0.0f;

            // Chaos Bolt exception, ignore all Target resistances (unknown attribute?)
            if (spellInfo != null &&
                spellInfo.SpellFamilyName == SpellFamilyNames.Warlock &&
                spellInfo.Id == 116858)
                victimResistance = 0.0f;

            victimResistance = Math.Max(victimResistance, 0.0f);

            // level-based resistance does not apply to binary spells, and cannot be overcome by spell penetration
            // gameobject caster -- should it have level based resistance?
            if (caster != null &&
                !caster.IsGameObject() &&
                (spellInfo == null || !spellInfo.HasAttribute(SpellCustomAttributes.BinarySpell)))
                victimResistance += Math.Max(((float)victim.GetLevelForTarget(caster) - (float)caster.GetLevelForTarget(victim)) * 5.0f, 0.0f);

            uint bossLevel = 83;
            float bossResistanceConstant = 510.0f;
            uint level = caster ? victim.GetLevelForTarget(caster) : victim.GetLevel();
            float resistanceConstant;

            if (level == bossLevel)
                resistanceConstant = bossResistanceConstant;
            else
                resistanceConstant = level * 5.0f;

            return victimResistance / (victimResistance + resistanceConstant);
        }

        private bool IsBlockCritical()
        {
            if (RandomHelper.randChance(GetTotalAuraModifier(AuraType.ModBlockCritChance)))
                return true;

            return false;
        }
    }
}