// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Chat;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Misc;
using Game.Movement;
using Game.Scripting.v2;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.AI
{
    public class SmartScript
    {
        // Max number of nested ProcessEventsFor() calls to avoid infinite loops
        const uint MaxNestedEvents = 10;

        public ObjectGuid LastInvoker;

        Dictionary<uint, uint> _counterList = new();

        List<SmartScriptHolder> _events = new();
        List<SmartScriptHolder> _installEvents = new();
        List<SmartScriptHolder> _timedActionList = new();
        ObjectGuid mTimedActionListInvoker;
        ActionBase mTimedActionWaitEvent;
        Creature _me;
        ObjectGuid _meOrigGUID;
        GameObject _go;
        ObjectGuid _goOrigGUID;
        Player _player;
        AreaTriggerRecord _trigger;
        AreaTrigger _areaTrigger;
        SceneTemplate _sceneTemplate;
        Quest _quest;
        uint _eventId;
        SmartScriptType _scriptType;
        uint _eventPhase;

        uint _pathId;
        List<SmartScriptHolder> _storedEvents = new();
        List<uint> _remIDs = new();

        uint _textTimer;
        uint _lastTextID;
        ObjectGuid _textGUID;
        uint _talkerEntry;
        bool _useTextTimer;
        uint _currentPriority;
        bool _eventSortingRequired;
        uint _nestedEventsCounter;
        SmartEventFlags _allEventFlags;

        Dictionary<uint, ObjectGuidList> _storedTargets = new();

        public SmartScript()
        {
            _go = null;
            _me = null;
            _trigger = null;
            _eventPhase = 0;
            _pathId = 0;
            _textTimer = 0;
            _lastTextID = 0;
            _textGUID = ObjectGuid.Empty;
            _useTextTimer = false;
            _talkerEntry = 0;
            _meOrigGUID = ObjectGuid.Empty;
            _goOrigGUID = ObjectGuid.Empty;
            LastInvoker = ObjectGuid.Empty;
            _scriptType = SmartScriptType.Creature;
        }

        public void OnReset()
        {
            ResetBaseObject();
            foreach (var holder in _events)
            {
                if (!holder.Event.event_flags.HasAnyFlag(SmartEventFlags.DontReset))
                {
                    InitTimer(holder);
                    holder.RunOnce = false;
                }

                if (holder.Priority != SmartScriptHolder.DefaultPriority)
                {
                    holder.Priority = SmartScriptHolder.DefaultPriority;
                    _eventSortingRequired = true;
                }
            }

            ProcessEventsFor(SmartEvents.Reset);
            LastInvoker.Clear();
        }

        public void ProcessEventsFor(SmartEvents e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
        {
            _nestedEventsCounter++;

            // Allow only a fixed number of nested ProcessEventsFor calls
            if (_nestedEventsCounter > MaxNestedEvents)
            {
                Log.outWarn(LogFilter.ScriptsAi, $"SmartScript::ProcessEventsFor: reached the limit of max allowed nested ProcessEventsFor() calls with event {e}, skipping!\n{GetBaseObject().GetDebugInfo()}");
            }
            else
            {
                foreach (var Event in _events)
                {
                    SmartEvents eventType = Event.GetEventType();
                    if (eventType == SmartEvents.Link)//special handling
                        continue;

                    if (eventType == e)
                        if (Global.ConditionMgr.IsObjectMeetingSmartEventConditions(Event.EntryOrGuid, Event.EventId, Event.SourceType, unit, GetBaseObject()))
                            ProcessEvent(Event, unit, var0, var1, bvar, spell, gob, varString);
                }
            }

            --_nestedEventsCounter;
        }

        void ProcessAction(SmartScriptHolder e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
        {
            e.RunOnce = true; //used for repeat check

            //calc random
            if (e.GetEventType() != SmartEvents.Link && e.Event.event_chance < 100 && e.Event.event_chance != 0 && !e.Event.event_flags.HasFlag(SmartEventFlags.TempIgnoreChanceRoll))
            {
                if (RandomHelper.randChance(e.Event.event_chance))
                    return;
            }

            // Remove SMART_EVENT_FLAG_TEMP_IGNORE_CHANCE_ROLL flag after processing roll chances as it's not needed anymore
            e.Event.event_flags &= ~SmartEventFlags.TempIgnoreChanceRoll;

            if (unit != null)
                LastInvoker = unit.GetGUID();

            Unit tempInvoker = GetLastInvoker();
            if (tempInvoker != null)
                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: Invoker: {0} (guidlow: {1})", tempInvoker.GetName(), tempInvoker.GetGUID().ToString());

            var targets = GetTargets(e, unit != null ? unit : gob);

            switch (e.GetActionType())
            {
                case SmartActions.Talk:
                {
                    Creature talker = e.Target.type == 0 ? _me : null;
                    Unit talkTarget = null;

                    foreach (var target in targets)
                    {
                        if (IsCreature(target) && !target.ToCreature().IsPet()) // Prevented sending text to pets.
                        {
                            if (e.Action.talk.useTalkTarget != 0)
                            {
                                talker = _me;
                                talkTarget = target.ToCreature();
                            }
                            else
                                talker = target.ToCreature();
                            break;
                        }
                        else if (IsPlayer(target))
                        {
                            talker = _me;
                            talkTarget = target.ToPlayer();
                            break;
                        }
                    }

                    if (talkTarget == null)
                        talkTarget = GetLastInvoker();

                    if (talker == null)
                        break;

                    _talkerEntry = talker.GetEntry();
                    _lastTextID = e.Action.talk.textGroupId;
                    _textTimer = e.Action.talk.duration;
                    _useTextTimer = true;
                    uint duration = Global.CreatureTextMgr.SendChat(talker, (byte)e.Action.talk.textGroupId, talkTarget);
                    mTimedActionWaitEvent = CreateTimedActionListWaitEventFor<WaitAction>(e, [GameTime.Now() + TimeSpan.FromMilliseconds(duration)]);
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_TALK: talker: {0} (Guid: {1}), textGuid: {2}",
                        talker.GetName(), talker.GetGUID().ToString(), _textGUID.ToString());
                    break;
                }
                case SmartActions.SimpleTalk:
                {
                    uint duration = 0;
                    foreach (var target in targets)
                    {
                        if (IsCreature(target))
                            duration = Math.Max(Global.CreatureTextMgr.SendChat(target.ToCreature(), (byte)e.Action.simpleTalk.textGroupId, IsPlayer(GetLastInvoker()) ? GetLastInvoker() : null), duration);
                        else if (IsPlayer(target) && _me != null)
                        {
                            Unit templastInvoker = GetLastInvoker();
                            duration = Math.Max(Global.CreatureTextMgr.SendChat(_me, (byte)e.Action.simpleTalk.textGroupId, IsPlayer(templastInvoker) ? templastInvoker : null, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, SoundKitPlayType.Normal, Team.Other, false, target.ToPlayer()), duration);
                        }
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SIMPLE_TALK: talker: {0} (GuidLow: {1}), textGroupId: {2}",
                            target.GetName(), target.GetGUID().ToString(), e.Action.simpleTalk.textGroupId);
                    }
                    mTimedActionWaitEvent = CreateTimedActionListWaitEventFor<WaitAction>(e, [GameTime.Now() + TimeSpan.FromMilliseconds(duration)]);
                    break;
                }
                case SmartActions.PlayEmote:
                {
                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            target.ToUnit().HandleEmoteCommand((Emote)e.Action.emote.emoteId);
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_PLAY_EMOTE: target: {0} (GuidLow: {1}), emote: {2}",
                                target.GetName(), target.GetGUID().ToString(), e.Action.emote.emoteId);
                        }
                    }
                    break;
                }
                case SmartActions.Sound:
                {
                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            if (e.Action.sound.distance == 1)
                                target.PlayDistanceSound(e.Action.sound.soundId, e.Action.sound.onlySelf != 0 ? target.ToPlayer() : null);
                            else
                                target.PlayDirectSound(e.Action.sound.soundId, e.Action.sound.onlySelf != 0 ? target.ToPlayer() : null, e.Action.sound.keyBroadcastTextId);

                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SOUND: target: {0} (GuidLow: {1}), sound: {2}, onlyself: {3}",
                                target.GetName(), target.GetGUID().ToString(), e.Action.sound.soundId, e.Action.sound.onlySelf);
                        }
                    }
                    break;
                }
                case SmartActions.SetFaction:
                {
                    foreach (var target in targets)
                    {
                        if (IsCreature(target))
                        {
                            if (e.Action.faction.factionId != 0)
                            {
                                target.ToCreature().SetFaction(e.Action.faction.factionId);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_FACTION: Creature entry {0}, GuidLow {1} set faction to {2}",
                                    target.GetEntry(), target.GetGUID().ToString(), e.Action.faction.factionId);
                            }
                            else
                            {
                                CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate(target.ToCreature().GetEntry());
                                if (ci != null)
                                {
                                    if (target.ToCreature().GetFaction() != ci.Faction)
                                    {
                                        target.ToCreature().SetFaction(ci.Faction);
                                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_FACTION: Creature entry {0}, GuidLow {1} set faction to {2}",
                                            target.GetEntry(), target.GetGUID().ToString(), ci.Faction);
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
                case SmartActions.MorphToEntryOrModel:
                {
                    foreach (var target in targets)
                    {
                        if (!IsCreature(target))
                            continue;

                        if (e.Action.morphOrMount.creature != 0 || e.Action.morphOrMount.model != 0)
                        {
                            //set model based on entry from creature_template
                            if (e.Action.morphOrMount.creature != 0)
                            {
                                CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature);
                                if (ci != null)
                                {
                                    CreatureModel model = ObjectManager.ChooseDisplayId(ci);
                                    target.ToCreature().SetDisplayId(model.CreatureDisplayID);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} set displayid to {2}",
                                        target.GetEntry(), target.GetGUID().ToString(), model.CreatureDisplayID);
                                }
                            }
                            //if no param1, then use value from param2 (modelId)
                            else
                            {
                                target.ToCreature().SetDisplayId(e.Action.morphOrMount.model);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} set displayid to {2}",
                                    target.GetEntry(), target.GetGUID().ToString(), e.Action.morphOrMount.model);
                            }
                        }
                        else
                        {
                            target.ToCreature().DeMorph();
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} demorphs.",
                                target.GetEntry(), target.GetGUID().ToString());
                        }
                    }
                    break;
                }
                case SmartActions.FailQuest:
                {
                    foreach (var target in targets)
                    {
                        if (IsPlayer(target))
                        {
                            target.ToPlayer().FailQuest(e.Action.quest.questId);
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_FAIL_QUEST: Player guidLow {0} fails quest {1}",
                                target.GetGUID().ToString(), e.Action.quest.questId);
                        }
                    }
                    break;
                }
                case SmartActions.OfferQuest:
                {
                    foreach (var target in targets)
                    {
                        Player player = target.ToPlayer();
                        if (player != null)
                        {
                            Quest quest = Global.ObjectMgr.GetQuestTemplate(e.Action.questOffer.questId);
                            if (quest != null)
                            {
                                if (_me != null && e.Action.questOffer.directAdd == 0)
                                {
                                    if (player.CanTakeQuest(quest, true))
                                    {
                                        WorldSession session = player.GetSession();
                                        if (session != null)
                                        {
                                            PlayerMenu menu = new(session);
                                            menu.SendQuestGiverQuestDetails(quest, _me.GetGUID(), true, false);
                                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction:: SMART_ACTION_OFFER_QUEST: Player {0} - offering quest {1}", player.GetGUID().ToString(), e.Action.questOffer.questId);
                                        }
                                    }
                                }
                                else
                                {
                                    player.AddQuestAndCheckCompletion(quest, null);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_ADD_QUEST: Player {0} add quest {1}", player.GetGUID().ToString(), e.Action.questOffer.questId);
                                }
                            }
                        }
                    }
                    break;
                }
                case SmartActions.SetReactState:
                {
                    foreach (var target in targets)
                    {
                        if (!IsCreature(target))
                            continue;

                        target.ToCreature().SetReactState((ReactStates)e.Action.react.state);
                    }
                    break;
                }
                case SmartActions.RandomEmote:
                {
                    List<uint> emotes = new();
                    var randomEmote = e.Action.randomEmote;
                    foreach (var id in new[] { randomEmote.emote1, randomEmote.emote2, randomEmote.emote3, randomEmote.emote4, randomEmote.emote5, randomEmote.emote6, })
                        if (id != 0)
                            emotes.Add(id);

                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            uint emote = emotes.SelectRandom();
                            target.ToUnit().HandleEmoteCommand((Emote)emote);
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_RANDOM_EMOTE: Creature guidLow {0} handle random emote {1}",
                                target.GetGUID().ToString(), emote);
                        }
                    }
                    break;
                }
                case SmartActions.ThreatAllPct:
                {
                    if (_me == null)
                        break;

                    foreach (var refe in _me.GetThreatManager().GetModifiableThreatList())
                    {
                        refe.ModifyThreatByPercent(Math.Max(-100, (int)(e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC)));
                        Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_THREAT_ALL_PCT: Creature {_me.GetGUID()} modify threat for {refe.GetVictim().GetGUID()}, value {e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC}");
                    }
                    break;
                }
                case SmartActions.ThreatSinglePct:
                {
                    if (_me == null)
                        break;

                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            _me.GetThreatManager().ModifyThreatByPercent(target.ToUnit(), Math.Max(-100, (int)(e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC)));
                            Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_THREAT_SINGLE_PCT: Creature {_me.GetGUID()} modify threat for {target.GetGUID()}, value {e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC}");
                        }
                    }
                    break;
                }
                case SmartActions.Cast:
                {
                    if (targets.Empty())
                        break;

                    if (e.Action.cast.targetsLimit > 0 && targets.Count > e.Action.cast.targetsLimit)
                        targets.RandomResize(e.Action.cast.targetsLimit);

                    bool failedSpellCast = false;
                    bool successfulSpellCast = false;

                    CastSpellExtraArgs args = new();
                    if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                    {
                        if (e.Action.cast.triggerFlags != 0)
                            args.TriggerFlags = (TriggerCastFlags)e.Action.cast.triggerFlags;
                        else
                            args.TriggerFlags = TriggerCastFlags.FullMask;
                    }

                    MultiActionResult<SpellCastResult> waitEvent = CreateTimedActionListWaitEventFor<MultiActionResult<SpellCastResult>>(e);

                    foreach (WorldObject target in targets)
                    {
                        if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) && (!target.IsUnit() || target.ToUnit().HasAura(e.Action.cast.spell)))
                        {
                            Log.outDebug(LogFilter.ScriptsAi, $"Spell {e.Action.cast.spell} not cast because it has flag SMARTCAST_AURA_NOT_PRESENT and the target ({target.GetGUID()}) already has the aura");
                            continue;
                        }

                        if (waitEvent != null)
                        {
                            args.SetScriptResult(ActionResult<SpellCastResult>.GetResultSetter(waitEvent.CreateAndGetResult()));
                            args.SetScriptWaitsForSpellHit(e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.WaitForHit));
                        }

                        SpellCastResult result = SpellCastResult.BadTargets;
                        if (_me != null)
                        {
                            if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                                _me.InterruptNonMeleeSpells(false);

                            result = _me.CastSpell(target, e.Action.cast.spell, args);
                        }
                        else if (_go != null)
                            result = _go.CastSpell(target, e.Action.cast.spell, args);

                        bool spellCastFailed = result != SpellCastResult.SpellCastOk && result != SpellCastResult.SpellInProgress;
                        if (_me != null && e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.CombatMove))
                        {
                            // If cast flag SMARTCAST_COMBAT_MOVE is set combat movement will not be allowed unless target is outside spell range, out of mana, or LOS.
                            _me.GetAI<SmartAI>().SetCombatMove(spellCastFailed, true);
                        }

                        if (spellCastFailed)
                            failedSpellCast = true;
                        else
                            successfulSpellCast = true;

                        Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction:: SMART_ACTION_CAST:: {(_me != null ? _me.GetGUID() : _go.GetGUID())} casts spell {e.Action.cast.spell} on target {target.GetGUID()} with castflags {e.Action.cast.castFlags}");
                    }

                    if (waitEvent != null && !waitEvent.Results.Empty())
                        mTimedActionWaitEvent = waitEvent;

                    // If there is at least 1 failed cast and no successful casts at all, retry again on next loop
                    if (failedSpellCast && !successfulSpellCast)
                    {
                        RetryLater(e, true);
                        // Don't execute linked events
                        return;
                    }

                    break;
                }
                case SmartActions.SelfCast:
                {
                    if (targets.Empty())
                        break;

                    if (e.Action.cast.targetsLimit != 0)
                        targets.RandomResize(e.Action.cast.targetsLimit);

                    MultiActionResult<SpellCastResult> waitEvent = CreateTimedActionListWaitEventFor<MultiActionResult<SpellCastResult>>(e);

                    CastSpellExtraArgs args = new();
                    if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                    {
                        if (e.Action.cast.triggerFlags != 0)
                            args.TriggerFlags = (TriggerCastFlags)e.Action.cast.triggerFlags;
                        else
                            args.TriggerFlags = TriggerCastFlags.FullMask;
                    }

                    foreach (WorldObject target in targets)
                    {
                        if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) && (!target.IsUnit() || target.ToUnit().HasAura(e.Action.cast.spell)))
                            continue;

                        if (waitEvent != null)
                        {
                            args.SetScriptResult(ActionResult<SpellCastResult>.GetResultSetter(waitEvent.CreateAndGetResult()));
                            args.SetScriptWaitsForSpellHit(e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.WaitForHit));
                        }

                        if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious) && target.IsUnit())
                            target.ToUnit().InterruptNonMeleeSpells(false);

                        target.CastSpell(target, e.Action.cast.spell, args);
                    }

                    if (waitEvent != null && !waitEvent.Results.Empty())
                        mTimedActionWaitEvent = waitEvent;
                    break;
                }
                case SmartActions.InvokerCast:
                {
                    Unit tempLastInvoker = GetLastInvoker(unit);
                    if (tempLastInvoker == null)
                        break;

                    if (targets.Empty())
                        break;

                    if (e.Action.cast.targetsLimit != 0)
                        targets.RandomResize(e.Action.cast.targetsLimit);

                    CastSpellExtraArgs args = new();
                    if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                    {
                        if (e.Action.cast.triggerFlags != 0)
                            args.TriggerFlags = (TriggerCastFlags)e.Action.cast.triggerFlags;
                        else
                            args.TriggerFlags = TriggerCastFlags.FullMask;
                    }

                    MultiActionResult<SpellCastResult> waitEvent = CreateTimedActionListWaitEventFor<MultiActionResult<SpellCastResult>>(e);

                    foreach (var target in targets)
                    {
                        if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) && (!target.IsUnit() || target.ToUnit().HasAura(e.Action.cast.spell)))
                        {
                            Log.outDebug(LogFilter.ScriptsAi, $"Spell {e.Action.cast.spell} not cast because it has flag SMARTCAST_AURA_NOT_PRESENT and the target ({target.GetGUID()}) already has the aura");
                            continue;
                        }

                        if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                            tempLastInvoker.InterruptNonMeleeSpells(false);

                        if (waitEvent != null)
                        {
                            args.SetScriptResult(ActionResult<SpellCastResult>.GetResultSetter(waitEvent.CreateAndGetResult()));
                            args.SetScriptWaitsForSpellHit(e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.WaitForHit));
                        }

                        tempLastInvoker.CastSpell(target, e.Action.cast.spell, args);
                        Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction:: SMART_ACTION_INVOKER_CAST: Invoker {tempLastInvoker.GetGUID()} casts spell {e.Action.cast.spell} on target {target.GetGUID()} with castflags {e.Action.cast.castFlags}");
                    }

                    if (waitEvent != null && !waitEvent.Results.Empty())
                        mTimedActionWaitEvent = waitEvent;
                    break;
                }
                case SmartActions.ActivateGobject:
                {
                    foreach (var target in targets)
                    {
                        if (IsGameObject(target))
                        {
                            // Activate
                            target.ToGameObject().SetLootState(LootState.Ready);
                            target.ToGameObject().UseDoorOrButton(0, false, unit);
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_ACTIVATE_GOBJECT. Gameobject {0} (entry: {1}) activated",
                                target.GetGUID().ToString(), target.GetEntry());
                        }
                    }
                    break;
                }
                case SmartActions.ResetGobject:
                {
                    foreach (var target in targets)
                    {
                        if (IsGameObject(target))
                        {
                            target.ToGameObject().ResetDoorOrButton();
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_RESET_GOBJECT. Gameobject {0} (entry: {1}) reset",
                                target.GetGUID().ToString(), target.GetEntry());
                        }
                    }
                    break;
                }
                case SmartActions.SetEmoteState:
                {
                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            target.ToUnit().SetEmoteState((Emote)e.Action.emote.emoteId);
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_EMOTE_STATE. Unit {0} set emotestate to {1}",
                                target.GetGUID().ToString(), e.Action.emote.emoteId);
                        }
                    }
                    break;
                }
                case SmartActions.AutoAttack:
                {
                    _me.SetCanMelee(e.Action.autoAttack.attack != 0);
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_AUTO_ATTACK: Creature: {0} bool on = {1}",
                        _me.GetGUID().ToString(), e.Action.autoAttack.attack);
                    break;
                }
                case SmartActions.AllowCombatMovement:
                {
                    if (!IsSmart())
                        break;

                    bool move = e.Action.combatMove.move != 0;
                    ((SmartAI)_me.GetAI()).SetCombatMove(move);
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_ALLOW_COMBAT_MOVEMENT: Creature {0} bool on = {1}",
                        _me.GetGUID().ToString(), e.Action.combatMove.move);
                    break;
                }
                case SmartActions.SetEventPhase:
                {
                    if (GetBaseObject() == null)
                        break;

                    SetPhase(e.Action.setEventPhase.phase);
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_EVENT_PHASE: Creature {0} set event phase {1}",
                        GetBaseObject().GetGUID().ToString(), e.Action.setEventPhase.phase);
                    break;
                }
                case SmartActions.IncEventPhase:
                {
                    if (GetBaseObject() == null)
                        break;

                    IncPhase(e.Action.incEventPhase.inc);
                    DecPhase(e.Action.incEventPhase.dec);
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_INC_EVENT_PHASE: Creature {0} inc event phase by {1}, " +
                        "decrease by {2}", GetBaseObject().GetGUID().ToString(), e.Action.incEventPhase.inc, e.Action.incEventPhase.dec);
                    break;
                }
                case SmartActions.Evade:
                {
                    if (_me == null)
                        break;

                    // Reset home position to respawn position if specified in the parameters
                    if (e.Action.evade.toRespawnPosition == 0)
                    {
                        _me.GetRespawnPosition(out float homeX, out float homeY, out float homeZ, out float homeO);
                        _me.SetHomePosition(homeX, homeY, homeZ, homeO);
                    }

                    _me.GetAI().EnterEvadeMode();
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_EVADE: Creature {0} EnterEvadeMode", _me.GetGUID().ToString());
                    break;
                }
                case SmartActions.FleeForAssist:
                {
                    if (_me == null)
                        break;

                    _me.DoFleeToGetAssistance();
                    if (e.Action.fleeAssist.withEmote != 0)
                    {
                        var builder = new BroadcastTextBuilder(_me, ChatMsg.MonsterEmote, (uint)BroadcastTextIds.FleeForAssist, _me.GetGender());
                        Global.CreatureTextMgr.SendChatPacket(_me, builder, ChatMsg.Emote);
                    }
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_FLEE_FOR_ASSIST: Creature {0} DoFleeToGetAssistance", _me.GetGUID().ToString());
                    break;
                }
                case SmartActions.CombatStop:
                {
                    if (_me == null)
                        break;

                    _me.CombatStop(true);
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_COMBAT_STOP: {0} CombatStop", _me.GetGUID().ToString());
                    break;
                }
                case SmartActions.RemoveAurasFromSpell:
                {
                    foreach (var target in targets)
                    {
                        if (!IsUnit(target))
                            continue;

                        if (e.Action.removeAura.spell != 0)
                        {
                            ObjectGuid casterGUID = default;
                            if (e.Action.removeAura.onlyOwnedAuras != 0)
                            {
                                if (_me == null)
                                    break;
                                casterGUID = _me.GetGUID();
                            }

                            if (e.Action.removeAura.charges != 0)
                            {
                                Aura aur = target.ToUnit().GetAura(e.Action.removeAura.spell, casterGUID);
                                if (aur != null)
                                    aur.ModCharges(-(int)e.Action.removeAura.charges, AuraRemoveMode.Expire);
                            }
                            target.ToUnit().RemoveAurasDueToSpell(e.Action.removeAura.spell);
                        }
                        else
                            target.ToUnit().RemoveAllAuras();

                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_REMOVEAURASFROMSPELL: Unit {0}, spell {1}",
                            target.GetGUID().ToString(), e.Action.removeAura.spell);
                    }
                    break;
                }
                case SmartActions.Follow:
                {
                    if (!IsSmart())
                        break;

                    if (targets.Empty())
                    {
                        ((SmartAI)_me.GetAI()).StopFollow(false);
                        break;
                    }

                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            float angle = e.Action.follow.angle > 6 ? (e.Action.follow.angle * (float)Math.PI / 180.0f) : e.Action.follow.angle;
                            ((SmartAI)_me.GetAI()).SetFollow(target.ToUnit(), e.Action.follow.dist + 0.1f, angle, e.Action.follow.credit, e.Action.follow.entry, e.Action.follow.creditType);
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_FOLLOW: Creature {0} following target {1}",
                                _me.GetGUID().ToString(), target.GetGUID().ToString());
                            break;
                        }
                    }
                    break;
                }
                case SmartActions.RandomPhase:
                {
                    if (GetBaseObject() == null)
                        break;

                    List<uint> phases = new();
                    var randomPhase = e.Action.randomPhase;
                    foreach (var id in new[] { randomPhase.phase1, randomPhase.phase2, randomPhase.phase3, randomPhase.phase4, randomPhase.phase5, randomPhase.phase6 })
                        if (id != 0)
                            phases.Add(id);

                    uint phase = phases.SelectRandom();
                    SetPhase(phase);
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_RANDOM_PHASE: Creature {0} sets event phase to {1}",
                        GetBaseObject().GetGUID().ToString(), phase);
                    break;
                }
                case SmartActions.RandomPhaseRange:
                {
                    if (GetBaseObject() == null)
                        break;

                    uint phase = RandomHelper.URand(e.Action.randomPhaseRange.phaseMin, e.Action.randomPhaseRange.phaseMax);
                    SetPhase(phase);
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_RANDOM_PHASE_RANGE: Creature {0} sets event phase to {1}",
                        GetBaseObject().GetGUID().ToString(), phase);
                    break;
                }
                case SmartActions.CallKilledmonster:
                {
                    if (e.Target.type == SmartTargets.None || e.Target.type == SmartTargets.Self) // Loot recipient and his group members
                    {
                        if (_me == null)
                            break;

                        foreach (ObjectGuid tapperGuid in _me.GetTapList())
                        {
                            Player tapper = Global.ObjAccessor.GetPlayer(_me, tapperGuid);
                            if (tapper != null)
                            {
                                tapper.KilledMonsterCredit(e.Action.killedMonster.creature, _me.GetGUID());
                                Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction: SMART_ACTION_CALL_KILLEDMONSTER: Player {tapper.GetGUID()}, Killcredit: {e.Action.killedMonster.creature}");
                            }
                        }
                    }
                    else // Specific target type
                    {
                        foreach (var target in targets)
                        {
                            if (IsPlayer(target))
                            {
                                target.ToPlayer().KilledMonsterCredit(e.Action.killedMonster.creature);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_CALL_KILLEDMONSTER: Player {0}, Killcredit: {1}",
                                    target.GetGUID().ToString(), e.Action.killedMonster.creature);
                            }
                            else if (IsUnit(target)) // Special handling for vehicles
                            {
                                Vehicle vehicle = target.ToUnit().GetVehicleKit();
                                if (vehicle != null)
                                {
                                    foreach (var seat in vehicle.Seats)
                                    {
                                        Player player = Global.ObjAccessor.GetPlayer(target, seat.Value.Passenger.Guid);
                                        if (player != null)
                                            player.KilledMonsterCredit(e.Action.killedMonster.creature);
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
                case SmartActions.SetInstData:
                {
                    WorldObject obj = GetBaseObject();
                    if (obj == null)
                        obj = unit;

                    if (obj == null)
                        break;

                    InstanceScript instance = obj.GetInstanceScript();
                    if (instance == null)
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: Event {0} attempt to set instance data without instance script. EntryOrGuid {1}", e.GetEventType(), e.EntryOrGuid);
                        break;
                    }

                    switch (e.Action.setInstanceData.type)
                    {
                        case 0:
                            instance.SetData(e.Action.setInstanceData.field, e.Action.setInstanceData.data);
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA: SetData Field: {0}, data: {1}",
                                e.Action.setInstanceData.field, e.Action.setInstanceData.data);
                            break;
                        case 1:
                            instance.SetBossState(e.Action.setInstanceData.field, (EncounterState)e.Action.setInstanceData.data);
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA: SetBossState BossId: {0}, State: {1} ({2})",
                                e.Action.setInstanceData.field, e.Action.setInstanceData.data, (EncounterState)e.Action.setInstanceData.data);
                            break;
                        default: // Static analysis
                            break;
                    }
                    break;
                }
                case SmartActions.SetInstData64:
                {
                    WorldObject obj = GetBaseObject();
                    if (obj == null)
                        obj = unit;

                    if (obj == null)
                        break;

                    InstanceScript instance = obj.GetInstanceScript();
                    if (instance == null)
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: Event {0} attempt to set instance data without instance script. EntryOrGuid {1}", e.GetEventType(), e.EntryOrGuid);
                        break;
                    }

                    if (targets.Empty())
                        break;

                    instance.SetGuidData(e.Action.setInstanceData64.field, targets.First().GetGUID());
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA64: Field: {0}, data: {1}",
                        e.Action.setInstanceData64.field, targets.First().GetGUID());
                    break;
                }
                case SmartActions.UpdateTemplate:
                {
                    foreach (var target in targets)
                        if (IsCreature(target))
                            target.ToCreature().UpdateEntry(e.Action.updateTemplate.creature, null, e.Action.updateTemplate.updateLevel != 0);
                    break;
                }
                case SmartActions.Die:
                {
                    if (_me != null && !_me.IsDead())
                    {
                        _me.KillSelf();
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_DIE: Creature {0}", _me.GetGUID().ToString());
                    }
                    break;
                }
                case SmartActions.SetInCombatWithZone:
                {
                    if (_me != null && _me.IsAIEnabled())
                    {
                        _me.GetAI().DoZoneInCombat();
                        Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_SET_IN_COMBAT_WITH_ZONE: Creature: {_me.GetGUID()}");
                    }

                    break;
                }
                case SmartActions.CallForHelp:
                {
                    if (_me != null)
                    {
                        _me.CallForHelp(e.Action.callHelp.range);
                        if (e.Action.callHelp.withEmote != 0)
                        {
                            var builder = new BroadcastTextBuilder(_me, ChatMsg.Emote, (uint)BroadcastTextIds.CallForHelp, _me.GetGender());
                            Global.CreatureTextMgr.SendChatPacket(_me, builder, ChatMsg.MonsterEmote);
                        }
                        Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_CALL_FOR_HELP: Creature: {_me.GetGUID()}");
                    }
                    break;
                }
                case SmartActions.SetSheath:
                {
                    if (_me != null)
                    {
                        _me.SetSheath((SheathState)e.Action.setSheath.sheath);
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_SET_SHEATH: Creature {0}, State: {1}",
                            _me.GetGUID().ToString(), e.Action.setSheath.sheath);
                    }
                    break;
                }
                case SmartActions.ForceDespawn:
                {
                    // there should be at least a world update tick before despawn, to avoid breaking linked actions
                    TimeSpan despawnDelay = TimeSpan.FromMilliseconds(e.Action.forceDespawn.delay);
                    if (despawnDelay <= TimeSpan.Zero)
                        despawnDelay = TimeSpan.FromMilliseconds(1);

                    TimeSpan forceRespawnTimer = TimeSpan.FromSeconds(e.Action.forceDespawn.forceRespawnTimer);

                    foreach (var target in targets)
                    {
                        Creature creature = target.ToCreature();
                        if (creature != null)
                            creature.DespawnOrUnsummon(despawnDelay, forceRespawnTimer);
                        else
                        {
                            GameObject go = target.ToGameObject();
                            if (go != null)
                                go.DespawnOrUnsummon(despawnDelay, forceRespawnTimer);
                        }
                    }
                    break;
                }
                case SmartActions.SetIngamePhaseId:
                {
                    foreach (var target in targets)
                    {
                        if (e.Action.ingamePhaseId.apply == 1)
                            PhasingHandler.AddPhase(target, e.Action.ingamePhaseId.id, true);
                        else
                            PhasingHandler.RemovePhase(target, e.Action.ingamePhaseId.id, true);
                    }
                    break;
                }
                case SmartActions.SetIngamePhaseGroup:
                {
                    foreach (var target in targets)
                    {
                        if (e.Action.ingamePhaseGroup.apply == 1)
                            PhasingHandler.AddPhaseGroup(target, e.Action.ingamePhaseGroup.groupId, true);
                        else
                            PhasingHandler.RemovePhaseGroup(target, e.Action.ingamePhaseGroup.groupId, true);
                    }
                    break;
                }
                case SmartActions.MountToEntryOrModel:
                {
                    foreach (var target in targets)
                    {
                        if (!IsUnit(target))
                            continue;

                        if (e.Action.morphOrMount.creature != 0 || e.Action.morphOrMount.model != 0)
                        {
                            if (e.Action.morphOrMount.creature > 0)
                            {
                                CreatureTemplate cInfo = Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature);
                                if (cInfo != null)
                                    target.ToUnit().Mount(ObjectManager.ChooseDisplayId(cInfo).CreatureDisplayID);
                            }
                            else
                                target.ToUnit().Mount(e.Action.morphOrMount.model);
                        }
                        else
                            target.ToUnit().Dismount();
                    }
                    break;
                }
                case SmartActions.SetInvincibilityHpLevel:
                {
                    foreach (var target in targets)
                    {
                        if (IsCreature(target))
                        {
                            SmartAI ai = (SmartAI)_me.GetAI();
                            if (ai == null)
                                continue;

                            if (e.Action.invincHP.percent != 0)
                                ai.SetInvincibilityHpLevel((uint)target.ToCreature().CountPctFromMaxHealth((int)e.Action.invincHP.percent));
                            else
                                ai.SetInvincibilityHpLevel(e.Action.invincHP.minHP);
                        }
                    }
                    break;
                }
                case SmartActions.SetData:
                {
                    foreach (var target in targets)
                    {
                        Creature cTarget = target.ToCreature();
                        if (cTarget != null)
                        {
                            CreatureAI ai = cTarget.GetAI();
                            if (IsSmart(cTarget, true))
                                ((SmartAI)ai).SetData(e.Action.setData.field, e.Action.setData.data, _me);
                            else
                                ai.SetData(e.Action.setData.field, e.Action.setData.data);
                        }
                        else
                        {
                            GameObject oTarget = target.ToGameObject();
                            if (oTarget != null)
                            {
                                GameObjectAI ai = oTarget.GetAI();
                                if (IsSmart(oTarget, true))
                                    ((SmartGameObjectAI)ai).SetData(e.Action.setData.field, e.Action.setData.data, _me);
                                else
                                    ai.SetData(e.Action.setData.field, e.Action.setData.data);
                            }
                        }
                    }
                    break;
                }
                case SmartActions.AttackStop:
                {
                    foreach (var target in targets)
                    {
                        var unitTarget = target.ToUnit();
                        if (unitTarget != null)
                            unitTarget.AttackStop();
                    }
                    break;
                }
                case SmartActions.MoveOffset:
                {
                    MultiActionResult<MovementStopReason> waitEvent = CreateTimedActionListWaitEventFor<MultiActionResult<MovementStopReason>>(e);

                    foreach (var target in targets)
                    {
                        if (!IsCreature(target))
                            continue;

                        if (!e.Event.event_flags.HasAnyFlag(SmartEventFlags.WhileCharmed) && IsCharmedCreature(target))
                            continue;

                        Position pos = target.GetPosition();

                        // Use forward/backward/left/right cartesian plane movement
                        float o = pos.GetOrientation();
                        float x = (float)(pos.GetPositionX() + (Math.Cos(o - (Math.PI / 2)) * e.Target.x) + (Math.Cos(o) * e.Target.y));
                        float y = (float)(pos.GetPositionY() + (Math.Sin(o - (Math.PI / 2)) * e.Target.x) + (Math.Sin(o) * e.Target.y));
                        float z = pos.GetPositionZ() + e.Target.z;

                        ActionResultSetter<MovementStopReason> scriptResult = null;
                        if (waitEvent != null)
                            scriptResult = ActionResult<MovementStopReason>.GetResultSetter(waitEvent.CreateAndGetResult());

                        target.ToCreature().GetMotionMaster().MovePoint(e.Action.moveOffset.PointId, x, y, z, true, null, null, MovementWalkRunSpeedSelectionMode.Default, null, scriptResult);
                    }

                    if (waitEvent != null && !waitEvent.Results.Empty())
                        mTimedActionWaitEvent = waitEvent;
                    break;
                }
                case SmartActions.SetVisibility:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().SetVisible(e.Action.visibility.state != 0);

                    break;
                }
                case SmartActions.SetActive:
                {
                    foreach (var target in targets)
                        target.SetActive(e.Action.active.state != 0);

                    break;
                }
                case SmartActions.AttackStart:
                {
                    if (_me == null)
                        break;

                    if (targets.Empty())
                        break;

                    Unit target = targets.SelectRandom().ToUnit();
                    if (target != null)
                        _me.GetAI().AttackStart(target);

                    break;
                }
                case SmartActions.SummonCreature:
                {
                    SmartActionSummonCreatureFlags flags = (SmartActionSummonCreatureFlags)e.Action.summonCreature.flags;
                    bool preferUnit = flags.HasAnyFlag(SmartActionSummonCreatureFlags.PreferUnit);
                    WorldObject summoner = preferUnit ? unit : GetBaseObjectOrUnitInvoker(unit);
                    if (summoner == null)
                        break;

                    ObjectGuid privateObjectOwner = ObjectGuid.Empty;
                    if (flags.HasAnyFlag(SmartActionSummonCreatureFlags.PersonalSpawn))
                        privateObjectOwner = summoner.IsPrivateObject() ? summoner.GetPrivateObjectOwner() : summoner.GetGUID();
                    uint spawnsCount = Math.Max(e.Action.summonCreature.count, 1u);

                    float x, y, z, o;
                    foreach (var target in targets)
                    {
                        target.GetPosition(out x, out y, out z, out o);
                        x += e.Target.x;
                        y += e.Target.y;
                        z += e.Target.z;
                        o += e.Target.o;
                        for (uint counter = 0; counter < spawnsCount; counter++)
                        {
                            Creature summon = summoner.SummonCreature(e.Action.summonCreature.creature, x, y, z, o, (TempSummonType)e.Action.summonCreature.type, TimeSpan.FromMilliseconds(e.Action.summonCreature.duration), privateObjectOwner);
                            if (summon != null)
                                if (e.Action.summonCreature.attackInvoker != 0)
                                    summon.GetAI().AttackStart(target.ToUnit());
                        }
                    }

                    if (e.GetTargetType() != SmartTargets.Position)
                        break;

                    for (uint counter = 0; counter < spawnsCount; counter++)
                    {
                        Creature summon = summoner.SummonCreature(e.Action.summonCreature.creature, e.Target.x, e.Target.y, e.Target.z, e.Target.o, (TempSummonType)e.Action.summonCreature.type, TimeSpan.FromMilliseconds(e.Action.summonCreature.duration), privateObjectOwner);
                        if (summon != null)
                            if (unit != null && e.Action.summonCreature.attackInvoker != 0)
                                summon.GetAI().AttackStart(unit);
                    }
                    break;
                }
                case SmartActions.SummonGo:
                {
                    WorldObject summoner = GetBaseObjectOrUnitInvoker(unit);
                    if (summoner == null)
                        break;

                    foreach (var target in targets)
                    {
                        Position pos = target.GetPositionWithOffset(new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o));
                        Quaternion rot = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.GetOrientation(), 0f, 0f));
                        summoner.SummonGameObject(e.Action.summonGO.entry, pos, rot, TimeSpan.FromSeconds(e.Action.summonGO.despawnTime), (GameObjectSummonType)e.Action.summonGO.summonType);
                    }

                    if (e.GetTargetType() != SmartTargets.Position)
                        break;

                    Quaternion _rot = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(e.Target.o, 0f, 0f));
                    summoner.SummonGameObject(e.Action.summonGO.entry, new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o), _rot, TimeSpan.FromSeconds(e.Action.summonGO.despawnTime), (GameObjectSummonType)e.Action.summonGO.summonType);
                    break;
                }
                case SmartActions.KillUnit:
                {
                    foreach (var target in targets)
                    {
                        if (!IsUnit(target))
                            continue;

                        target.ToUnit().KillSelf();
                    }
                    break;
                }
                case SmartActions.AddItem:
                {
                    foreach (var target in targets)
                    {
                        if (!IsPlayer(target))
                            continue;

                        target.ToPlayer().AddItem(e.Action.item.entry, e.Action.item.count);
                    }
                    break;
                }
                case SmartActions.RemoveItem:
                {
                    foreach (var target in targets)
                    {
                        if (!IsPlayer(target))
                            continue;

                        target.ToPlayer().DestroyItemCount(e.Action.item.entry, e.Action.item.count, true);
                    }
                    break;
                }
                case SmartActions.StoreTargetList:
                {
                    StoreTargetList(targets, e.Action.storeTargets.id);
                    break;
                }
                case SmartActions.Teleport:
                {
                    foreach (var target in targets)
                    {
                        if (IsPlayer(target))
                            target.ToPlayer().TeleportTo(e.Action.teleport.mapID, e.Target.x, e.Target.y, e.Target.z, e.Target.o);
                        else if (IsCreature(target))
                            target.ToCreature().NearTeleportTo(e.Target.x, e.Target.y, e.Target.z, e.Target.o);
                    }
                    break;
                }
                case SmartActions.SetDisableGravity:
                {
                    foreach (WorldObject target in targets)
                        if (IsCreature(target))
                            target.ToCreature().SetFloating(e.Action.setDisableGravity.disable != 0);
                    break;
                }
                case SmartActions.SetRun:
                {
                    if (!IsSmart())
                        break;

                    ((SmartAI)_me.GetAI()).SetRun(e.Action.setRun.run != 0);
                    break;
                }
                case SmartActions.SetCounter:
                {
                    if (!targets.Empty())
                    {
                        foreach (var target in targets)
                        {
                            if (IsCreature(target))
                            {
                                SmartAI ai = (SmartAI)target.ToCreature().GetAI();
                                if (ai != null)
                                    ai.GetScript().StoreCounter(e.Action.setCounter.counterId, e.Action.setCounter.value, e.Action.setCounter.reset);
                                else
                                    Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SET_COUNTER is not using SmartAI, skipping");
                            }
                            else if (IsGameObject(target))
                            {
                                SmartGameObjectAI ai = (SmartGameObjectAI)target.ToGameObject().GetAI();
                                if (ai != null)
                                    ai.GetScript().StoreCounter(e.Action.setCounter.counterId, e.Action.setCounter.value, e.Action.setCounter.reset);
                                else
                                    Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SET_COUNTER is not using SmartGameObjectAI, skipping");
                            }
                        }
                    }
                    else
                        StoreCounter(e.Action.setCounter.counterId, e.Action.setCounter.value, e.Action.setCounter.reset);

                    break;
                }
                case SmartActions.WpStart:
                {
                    if (!IsSmart())
                        break;

                    uint entry = e.Action.wpStart.pathID;
                    bool repeat = e.Action.wpStart.repeat != 0;

                    foreach (var target in targets)
                    {
                        if (IsPlayer(target))
                        {
                            StoreTargetList(targets, SharedConst.SmartEscortTargets);
                            break;
                        }
                    }

                    ActionResult<MovementStopReason> waitEvent = CreateTimedActionListWaitEventFor<MovementStopReason, ActionResult>(e);
                    ActionResultSetter<MovementStopReason> scriptResult = null;
                    if (waitEvent != null)
                        scriptResult = ActionResult<MovementStopReason>.GetResultSetter(waitEvent);

                    _me.GetAI<SmartAI>().StartPath(entry, repeat, unit, 0, scriptResult);
                    mTimedActionWaitEvent = waitEvent;

                    uint quest = e.Action.wpStart.quest;
                    uint DespawnTime = e.Action.wpStart.despawnTime;
                    _me.GetAI<SmartAI>().EscortQuestID = quest;
                    _me.GetAI<SmartAI>().SetDespawnTime(DespawnTime);
                    break;
                }
                case SmartActions.WpPause:
                {
                    if (!IsSmart())
                        break;

                    uint delay = e.Action.wpPause.delay;
                    ((SmartAI)_me.GetAI()).PausePath(delay, true);
                    break;
                }
                case SmartActions.WpStop:
                {
                    if (!IsSmart())
                        break;

                    uint DespawnTime = e.Action.wpStop.despawnTime;
                    uint quest = e.Action.wpStop.quest;
                    bool fail = e.Action.wpStop.fail != 0;
                    ((SmartAI)_me.GetAI()).StopPath(DespawnTime, quest, fail);
                    break;
                }
                case SmartActions.WpResume:
                {
                    if (!IsSmart())
                        break;

                    // Set the timer to 1 ms so the path will be resumed on next update loop
                    if (_me.GetAI<SmartAI>().CanResumePath())
                        _me.GetAI<SmartAI>().SetWPPauseTimer(1);
                    break;
                }
                case SmartActions.SetOrientation:
                {
                    if (_me == null)
                        break;

                    if (e.GetTargetType() == SmartTargets.Self)
                        _me.SetFacingTo((_me.GetTransport() != null ? _me.GetTransportHomePosition() : _me.GetHomePosition()).GetOrientation());
                    else if (e.GetTargetType() == SmartTargets.Position)
                        _me.SetFacingTo(e.Target.o);
                    else if (!targets.Empty())
                        _me.SetFacingToObject(targets.First());

                    break;
                }
                case SmartActions.Playmovie:
                {
                    foreach (var target in targets)
                    {
                        if (!IsPlayer(target))
                            continue;

                        target.ToPlayer().SendMovieStart(e.Action.movie.entry);
                    }
                    break;
                }
                case SmartActions.MoveToPos:
                {
                    if (!IsSmart())
                        break;

                    WorldObject target = null;

                    // we want to move to random element
                    if (!targets.Empty())
                        target = targets.SelectRandom();

                    ActionResult<MovementStopReason> waitEvent = CreateTimedActionListWaitEventFor<MovementStopReason, ActionResult>(e);
                    ActionResultSetter<MovementStopReason> scriptResult = null;
                    if (waitEvent != null)
                        scriptResult = ActionResult<MovementStopReason>.GetResultSetter(waitEvent);

                    if (target != null)
                    {
                        float x, y, z;
                        target.GetPosition(out x, out y, out z);
                        if (e.Action.moveToPos.contactDistance > 0)
                            target.GetContactPoint(_me, out x, out y, out z, e.Action.moveToPos.contactDistance);
                        _me.GetMotionMaster().MovePoint(e.Action.moveToPos.pointId, x + e.Target.x, y + e.Target.y, z + e.Target.z, e.Action.moveToPos.disablePathfinding == 0);
                        mTimedActionWaitEvent = waitEvent;
                    }

                    if (e.GetTargetType() != SmartTargets.Position)
                        break;

                    Position dest = new(e.Target.x, e.Target.y, e.Target.z);
                    if (e.Action.moveToPos.transport != 0)
                    {
                        var trans = _me.GetDirectTransport();
                        if (trans != null)
                            trans.CalculatePassengerPosition(ref dest.posX, ref dest.posY, ref dest.posZ, ref dest.Orientation);
                    }

                    _me.GetMotionMaster().MovePoint(e.Action.moveToPos.pointId, dest, e.Action.moveToPos.disablePathfinding == 0, null, null, MovementWalkRunSpeedSelectionMode.Default, null, scriptResult);
                    mTimedActionWaitEvent = waitEvent;
                    break;
                }
                case SmartActions.EnableTempGobj:
                {
                    foreach (var target in targets)
                    {
                        if (IsCreature(target))
                            Log.outWarn(LogFilter.Sql, $"Invalid creature target '{target.GetName()}' (entry {target.GetEntry()}, spawnId {target.ToCreature().GetSpawnId()}) specified for SMART_ACTION_ENABLE_TEMP_GOBJ");
                        else if (IsGameObject(target))
                        {
                            if (target.ToGameObject().IsSpawnedByDefault())
                                Log.outWarn(LogFilter.Sql, $"Invalid gameobject target '{target.GetName()}' (entry {target.GetEntry()}, spawnId {target.ToGameObject().GetSpawnId()}) for SMART_ACTION_ENABLE_TEMP_GOBJ - the object is spawned by default");
                            else
                                target.ToGameObject().SetRespawnTime((int)e.Action.enableTempGO.duration);
                        }
                    }
                    break;
                }
                case SmartActions.CloseGossip:
                {
                    foreach (var target in targets)
                        if (IsPlayer(target))
                            target.ToPlayer().PlayerTalkClass.SendCloseGossip();
                    break;
                }
                case SmartActions.Equip:
                {
                    foreach (var target in targets)
                    {
                        Creature npc = target.ToCreature();
                        if (npc != null)
                        {
                            EquipmentItem[] slot = new EquipmentItem[SharedConst.MaxEquipmentItems];
                            sbyte equipId = (sbyte)e.Action.equip.entry;
                            if (equipId != 0)
                            {
                                EquipmentInfo eInfo = Global.ObjectMgr.GetEquipmentInfo(npc.GetEntry(), equipId);
                                if (eInfo == null)
                                {
                                    Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_EQUIP uses non-existent equipment info id {0} for creature {1}", equipId, npc.GetEntry());
                                    break;
                                }

                                npc.SetCurrentEquipmentId((byte)equipId);
                                Array.Copy(eInfo.Items, slot, SharedConst.MaxEquipmentItems);
                            }
                            else
                            {
                                slot[0].ItemId = e.Action.equip.slot1;
                                slot[1].ItemId = e.Action.equip.slot2;
                                slot[2].ItemId = e.Action.equip.slot3;
                            }

                            for (uint i = 0; i < SharedConst.MaxEquipmentItems; ++i)
                                if (e.Action.equip.mask == 0 || (e.Action.equip.mask & (1 << (int)i)) != 0)
                                    npc.SetVirtualItem(i, slot[i].ItemId, slot[i].AppearanceModId, slot[i].ItemVisual);
                        }
                    }
                    break;
                }
                case SmartActions.CreateTimedEvent:
                {
                    SmartEvent ne = new();
                    ne.type = SmartEvents.Update;
                    ne.event_chance = e.Action.timeEvent.chance;
                    if (ne.event_chance == 0)
                        ne.event_chance = 100;

                    ne.minMaxRepeat.min = e.Action.timeEvent.min;
                    ne.minMaxRepeat.max = e.Action.timeEvent.max;
                    ne.minMaxRepeat.repeatMin = e.Action.timeEvent.repeatMin;
                    ne.minMaxRepeat.repeatMax = e.Action.timeEvent.repeatMax;

                    ne.event_flags = 0;
                    if (ne.minMaxRepeat.repeatMin == 0 && ne.minMaxRepeat.repeatMax == 0)
                        ne.event_flags |= SmartEventFlags.NotRepeatable;

                    SmartAction ac = new();
                    ac.type = SmartActions.TriggerTimedEvent;
                    ac.timeEvent.id = e.Action.timeEvent.id;

                    SmartScriptHolder ev = new();
                    ev.Event = ne;
                    ev.EventId = e.Action.timeEvent.id;
                    ev.Target = e.Target;
                    ev.Action = ac;
                    InitTimer(ev);
                    _storedEvents.Add(ev);
                    break;
                }
                case SmartActions.TriggerTimedEvent:
                    ProcessEventsFor(SmartEvents.TimedEventTriggered, null, e.Action.timeEvent.id);

                    // remove this event if not repeatable
                    if (e.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable))
                        _remIDs.Add(e.Action.timeEvent.id);
                    break;
                case SmartActions.RemoveTimedEvent:
                    _remIDs.Add(e.Action.timeEvent.id);
                    break;
                case SmartActions.CallScriptReset:
                    SetPhase(0);
                    OnReset();
                    break;
                case SmartActions.SetRangedMovement:
                {
                    if (!IsSmart())
                        break;

                    float attackDistance = e.Action.setRangedMovement.distance;
                    float attackAngle = e.Action.setRangedMovement.angle / 180.0f * MathFunctions.PI;

                    foreach (var target in targets)
                    {
                        Creature creature = target.ToCreature();
                        if (creature != null)
                            if (IsSmart(creature) && creature.GetVictim() != null)
                                if (((SmartAI)creature.GetAI()).CanCombatMove())
                                    creature.StartDefaultCombatMovement(creature.GetVictim(), attackDistance, attackAngle);
                    }
                    break;
                }
                case SmartActions.CallTimedActionlist:
                {
                    if (e.GetTargetType() == SmartTargets.None)
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.EntryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());
                        break;
                    }

                    foreach (var target in targets)
                    {
                        Creature creature = target.ToCreature();
                        if (creature != null)
                        {
                            if (IsSmart(creature))
                                creature.GetAI<SmartAI>().SetTimedActionList(e, e.Action.timedActionList.id, GetLastInvoker());
                        }
                        else
                        {
                            GameObject go = target.ToGameObject();
                            if (go != null)
                            {
                                if (IsSmart(go))
                                    go.GetAI<SmartGameObjectAI>().SetTimedActionList(e, e.Action.timedActionList.id, GetLastInvoker());
                            }
                            else
                            {
                                AreaTrigger areaTriggerTarget = target.ToAreaTrigger();
                                if (areaTriggerTarget != null)
                                {
                                    SmartAreaTriggerAI atSAI = areaTriggerTarget.GetAI<SmartAreaTriggerAI>();
                                    if (atSAI != null)
                                        atSAI.SetTimedActionList(e, e.Action.timedActionList.id, GetLastInvoker());
                                }
                            }
                        }
                    }
                    break;
                }
                case SmartActions.SetNpcFlag:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().ReplaceAllNpcFlags((NPCFlags)e.Action.flag.flag);
                    break;
                }
                case SmartActions.AddNpcFlag:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().SetNpcFlag((NPCFlags)e.Action.flag.flag);
                    break;
                }
                case SmartActions.RemoveNpcFlag:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().RemoveNpcFlag((NPCFlags)e.Action.flag.flag);
                    break;
                }
                case SmartActions.CrossCast:
                {
                    if (targets.Empty())
                        break;

                    List<WorldObject> casters = GetTargets(CreateSmartEvent(SmartEvents.UpdateIc, 0, 0, 0, 0, 0, 0, SmartActions.None, 0, 0, 0, 0, 0, 0, 0, (SmartTargets)e.Action.crossCast.targetType, e.Action.crossCast.targetParam1, e.Action.crossCast.targetParam2, e.Action.crossCast.targetParam3, e.Action.crossCast.targetParam4, e.Action.param_string, 0), unit);

                    MultiActionResult<SpellCastResult> waitEvent = CreateTimedActionListWaitEventFor<MultiActionResult<SpellCastResult>>(e);

                    CastSpellExtraArgs args = new();
                    if (e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                        args.TriggerFlags = TriggerCastFlags.FullMask;

                    foreach (var caster in casters)
                    {
                        Unit casterUnit = caster.ToUnit();
                        if (casterUnit == null)
                            continue;

                        bool interruptedSpell = false;

                        foreach (var target in targets)
                        {
                            if (e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) && (!target.IsUnit() || target.ToUnit().HasAura(e.Action.crossCast.spell)))
                            {
                                Log.outDebug(LogFilter.ScriptsAi, $"Spell {e.Action.crossCast.spell} not cast because it has flag SMARTCAST_AURA_NOT_PRESENT and the target ({target.GetGUID()}) already has the aura");
                                continue;
                            }

                            if (!interruptedSpell && e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                            {
                                casterUnit.InterruptNonMeleeSpells(false);
                                interruptedSpell = true;
                            }

                            if (waitEvent != null)
                            {
                                args.SetScriptResult(ActionResult<SpellCastResult>.GetResultSetter(waitEvent.CreateAndGetResult()));
                                args.SetScriptWaitsForSpellHit(e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.WaitForHit));
                            }

                            casterUnit.CastSpell(target, e.Action.crossCast.spell, args);
                        }
                    }

                    if (waitEvent != null && !waitEvent.Results.Empty())
                        mTimedActionWaitEvent = waitEvent;
                    break;
                }
                case SmartActions.CallRandomTimedActionlist:
                {
                    List<uint> actionLists = new();
                    var randTimedActionList = e.Action.randTimedActionList;
                    foreach (var id in new[] { randTimedActionList.actionList1, randTimedActionList.actionList2, randTimedActionList.actionList3, randTimedActionList.actionList4, randTimedActionList.actionList5, randTimedActionList.actionList6 })
                        if (id != 0)
                            actionLists.Add(id);

                    if (e.GetTargetType() == SmartTargets.None)
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.EntryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());
                        break;
                    }

                    uint randomId = actionLists.SelectRandom();
                    foreach (var target in targets)
                    {
                        Creature creature = target.ToCreature();
                        if (creature != null)
                        {
                            if (IsSmart(creature))
                                creature.GetAI<SmartAI>().SetTimedActionList(e, randomId, GetLastInvoker());
                        }
                        else
                        {
                            GameObject go = target.ToGameObject();
                            if (go != null)
                            {
                                if (IsSmart(go))
                                    go.GetAI<SmartGameObjectAI>().SetTimedActionList(e, randomId, GetLastInvoker());
                            }
                            else
                            {
                                AreaTrigger areaTriggerTarget = target.ToAreaTrigger();
                                if (areaTriggerTarget != null)
                                {
                                    SmartAreaTriggerAI atSAI = areaTriggerTarget.GetAI<SmartAreaTriggerAI>();
                                    if (atSAI != null)
                                        atSAI.SetTimedActionList(e, randomId, GetLastInvoker());
                                }
                            }
                        }
                    }
                    break;
                }
                case SmartActions.CallRandomRangeTimedActionlist:
                {
                    uint id = RandomHelper.URand(e.Action.randRangeTimedActionList.idMin, e.Action.randRangeTimedActionList.idMax);
                    if (e.GetTargetType() == SmartTargets.None)
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.EntryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());
                        break;
                    }

                    foreach (var target in targets)
                    {
                        Creature creature = target.ToCreature();
                        if (creature != null)
                        {
                            if (IsSmart(creature))
                                creature.GetAI<SmartAI>().SetTimedActionList(e, id, GetLastInvoker());
                        }
                        else
                        {
                            GameObject go = target.ToGameObject();
                            if (go != null)
                            {
                                if (IsSmart(go))
                                    go.GetAI<SmartGameObjectAI>().SetTimedActionList(e, id, GetLastInvoker());
                            }
                            else
                            {
                                AreaTrigger areaTriggerTarget = target.ToAreaTrigger();
                                if (areaTriggerTarget != null)
                                {
                                    SmartAreaTriggerAI atSAI = areaTriggerTarget.GetAI<SmartAreaTriggerAI>();
                                    if (atSAI != null)
                                        atSAI.SetTimedActionList(e, id, GetLastInvoker());
                                }
                            }
                        }
                    }
                    break;
                }
                case SmartActions.ActivateTaxi:
                {
                    MultiActionResult<MovementStopReason> waitEvent = CreateTimedActionListWaitEventFor<MultiActionResult<MovementStopReason>>(e);

                    foreach (var target in targets)
                    {
                        if (IsPlayer(target))
                        {
                            ActionResultSetter<MovementStopReason> scriptResult = null;
                            if (waitEvent != null)
                                scriptResult = ActionResult<MovementStopReason>.GetResultSetter(waitEvent.CreateAndGetResult());

                            if (!target.ToPlayer().ActivateTaxiPathTo(e.Action.taxi.id, 0, null, scriptResult))
                                if (scriptResult != null)
                                    scriptResult.SetResult(MovementStopReason.Interrupted);
                        }
                    }

                    if (waitEvent != null && !waitEvent.Results.Empty())
                        mTimedActionWaitEvent = waitEvent;
                    break;
                }
                case SmartActions.RandomMove:
                {
                    bool foundTarget = false;

                    foreach (var obj in targets)
                    {
                        if (IsCreature(obj))
                        {
                            if (e.Action.moveRandom.distance != 0)
                                obj.ToCreature().GetMotionMaster().MoveRandom(e.Action.moveRandom.distance);
                            else
                                obj.ToCreature().GetMotionMaster().MoveIdle();
                        }
                    }

                    if (!foundTarget && _me != null && IsCreature(_me))
                    {
                        if (e.Action.moveRandom.distance != 0)
                            _me.GetMotionMaster().MoveRandom(e.Action.moveRandom.distance);
                        else
                            _me.GetMotionMaster().MoveIdle();
                    }
                    break;
                }
                case SmartActions.SetUnitFieldBytes1:
                {
                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            switch (e.Action.setunitByte.type)
                            {
                                case 0:
                                    target.ToUnit().SetStandState((UnitStandStateType)e.Action.setunitByte.byte1);
                                    break;
                                case 1:
                                    // pet talent points
                                    break;
                                case 2:
                                    target.ToUnit().SetVisFlag((UnitVisFlags)e.Action.setunitByte.byte1);
                                    break;
                                case 3:
                                    target.ToUnit().SetAnimTier((AnimTier)e.Action.setunitByte.byte1);
                                    break;
                            }
                        }
                    }
                    break;
                }
                case SmartActions.RemoveUnitFieldBytes1:
                {
                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            switch (e.Action.setunitByte.type)
                            {
                                case 0:
                                    target.ToUnit().SetStandState(UnitStandStateType.Stand);
                                    break;
                                case 1:
                                    // pet talent points
                                    break;
                                case 2:
                                    target.ToUnit().RemoveVisFlag((UnitVisFlags)e.Action.setunitByte.byte1);
                                    break;
                                case 3:
                                    target.ToUnit().SetAnimTier(AnimTier.Ground);
                                    break;
                            }
                        }
                    }
                    break;
                }
                case SmartActions.InterruptSpell:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().InterruptNonMeleeSpells(e.Action.interruptSpellCasting.withDelayed != 0, e.Action.interruptSpellCasting.spell_id, e.Action.interruptSpellCasting.withInstant != 0);
                    break;
                }
                case SmartActions.AddDynamicFlag:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().SetDynamicFlag((UnitDynFlags)e.Action.flag.flag);
                    break;
                }
                case SmartActions.RemoveDynamicFlag:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().RemoveDynamicFlag((UnitDynFlags)e.Action.flag.flag);
                    break;
                }
                case SmartActions.JumpToPos:
                {
                    WorldObject target = null;

                    if (!targets.Empty())
                        target = targets.SelectRandom();

                    Position pos = new(e.Target.x, e.Target.y, e.Target.z);
                    if (target != null)
                    {
                        float x, y, z;
                        target.GetPosition(out x, out y, out z);
                        if (e.Action.jump.ContactDistance > 0)
                            target.GetContactPoint(_me, out x, out y, out z, e.Action.jump.ContactDistance);
                        pos = new Position(x + e.Target.x, y + e.Target.y, z + e.Target.z);
                    }
                    else if (e.GetTargetType() != SmartTargets.Position)
                        break;

                    ActionResult<MovementStopReason> waitEvent = CreateTimedActionListWaitEventFor<MovementStopReason, ActionResult>(e);
                    ActionResultSetter<MovementStopReason> actionResultSetter = null;
                    if (waitEvent != null)
                        actionResultSetter = ActionResult<MovementStopReason>.GetResultSetter(waitEvent);

                    if (e.Action.jump.Gravity != 0 || e.Action.jump.UseDefaultGravity != 0)
                    {
                        float gravity = e.Action.jump.UseDefaultGravity != 0 ? (float)MotionMaster.gravity : e.Action.jump.Gravity;
                        _me.GetMotionMaster().MoveJumpWithGravity(pos, e.Action.jump.SpeedXY, gravity, e.Action.jump.PointId, null, false, null, null, actionResultSetter);
                    }
                    else
                        _me.GetMotionMaster().MoveJump(pos, e.Action.jump.SpeedXY, e.Action.jump.SpeedZ, e.Action.jump.PointId, null, false, null, null, actionResultSetter);

                    mTimedActionWaitEvent = waitEvent;
                    break;
                }
                case SmartActions.GoSetLootState:
                {
                    foreach (var target in targets)
                        if (IsGameObject(target))
                            target.ToGameObject().SetLootState((LootState)e.Action.setGoLootState.state);
                    break;
                }
                case SmartActions.GoSetGoState:
                {
                    foreach (var target in targets)
                        if (IsGameObject(target))
                            target.ToGameObject().SetGoState((GameObjectState)e.Action.goState.state);
                    break;
                }
                case SmartActions.SendTargetToTarget:
                {
                    WorldObject baseObject = GetBaseObject();
                    if (baseObject == null)
                        baseObject = unit;

                    if (baseObject == null)
                        break;

                    var storedTargets = GetStoredTargetList(e.Action.sendTargetToTarget.id, baseObject);
                    if (storedTargets == null)
                        break;

                    foreach (var target in targets)
                    {
                        if (IsCreature(target))
                        {
                            SmartAI ai = (SmartAI)target.ToCreature().GetAI();
                            if (ai != null)
                                ai.GetScript().StoreTargetList(new List<WorldObject>(storedTargets), e.Action.sendTargetToTarget.id);   // store a copy of target list
                            else
                                Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SEND_TARGET_TO_TARGET is not using SmartAI, skipping");
                        }
                        else if (IsGameObject(target))
                        {
                            SmartGameObjectAI ai = (SmartGameObjectAI)target.ToGameObject().GetAI();
                            if (ai != null)
                                ai.GetScript().StoreTargetList(new List<WorldObject>(storedTargets), e.Action.sendTargetToTarget.id);   // store a copy of target list
                            else
                                Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SEND_TARGET_TO_TARGET is not using SmartGameObjectAI, skipping");
                        }
                    }
                    break;
                }
                case SmartActions.SendGossipMenu:
                {
                    if (GetBaseObject() == null || !IsSmart())
                        break;

                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SEND_GOSSIP_MENU: gossipMenuId {0}, gossipNpcTextId {1}",
                        e.Action.sendGossipMenu.gossipMenuId, e.Action.sendGossipMenu.gossipNpcTextId);

                    // override default gossip
                    if (_me != null)
                        ((SmartAI)_me.GetAI()).SetGossipReturn(true);
                    else if (_go != null)
                        ((SmartGameObjectAI)_go.GetAI()).SetGossipReturn(true);

                    foreach (var target in targets)
                    {
                        Player player = target.ToPlayer();
                        if (player != null)
                        {
                            if (e.Action.sendGossipMenu.gossipMenuId != 0)
                                player.PrepareGossipMenu(GetBaseObject(), e.Action.sendGossipMenu.gossipMenuId, true);
                            else
                                player.PlayerTalkClass.ClearMenus();

                            uint gossipNpcTextId = e.Action.sendGossipMenu.gossipNpcTextId;
                            if (gossipNpcTextId == 0)
                                gossipNpcTextId = player.GetGossipTextId(e.Action.sendGossipMenu.gossipMenuId, GetBaseObject());

                            player.PlayerTalkClass.SendGossipMenu(gossipNpcTextId, GetBaseObject().GetGUID());
                        }
                    }
                    break;
                }
                case SmartActions.SetHomePos:
                {
                    foreach (var target in targets)
                    {
                        if (IsCreature(target))
                        {
                            if (e.GetTargetType() == SmartTargets.Self)
                                target.ToCreature().SetHomePosition(_me.GetPositionX(), _me.GetPositionY(), _me.GetPositionZ(), _me.GetOrientation());
                            else if (e.GetTargetType() == SmartTargets.Position)
                                target.ToCreature().SetHomePosition(e.Target.x, e.Target.y, e.Target.z, e.Target.o);
                            else if (e.GetTargetType() == SmartTargets.CreatureRange || e.GetTargetType() == SmartTargets.CreatureGuid ||
                                     e.GetTargetType() == SmartTargets.CreatureDistance || e.GetTargetType() == SmartTargets.GameobjectRange ||
                                     e.GetTargetType() == SmartTargets.GameobjectGuid || e.GetTargetType() == SmartTargets.GameobjectDistance ||
                                     e.GetTargetType() == SmartTargets.ClosestCreature || e.GetTargetType() == SmartTargets.ClosestGameobject ||
                                     e.GetTargetType() == SmartTargets.OwnerOrSummoner || e.GetTargetType() == SmartTargets.ActionInvoker ||
                                     e.GetTargetType() == SmartTargets.ClosestEnemy || e.GetTargetType() == SmartTargets.ClosestFriendly ||
                                     e.GetTargetType() == SmartTargets.ClosestUnspawnedGameobject)
                            {
                                target.ToCreature().SetHomePosition(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), target.GetOrientation());
                            }
                            else
                                Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SET_HOME_POS is invalid, skipping");
                        }
                    }
                    break;
                }
                case SmartActions.SetHealthRegen:
                {
                    foreach (var target in targets)
                        if (IsCreature(target))
                            target.ToCreature().SetRegenerateHealth(e.Action.setHealthRegen.regenHealth != 0);
                    break;
                }
                case SmartActions.SetRoot:
                {
                    foreach (var target in targets)
                        if (IsCreature(target))
                            target.ToCreature().SetSessile(e.Action.setRoot.root != 0);
                    break;
                }
                case SmartActions.SummonCreatureGroup:
                {
                    GetBaseObject().SummonCreatureGroup((byte)e.Action.creatureGroup.group, out List<TempSummon> summonList);

                    foreach (var summon in summonList)
                        if (unit == null && e.Action.creatureGroup.attackInvoker != 0)
                            summon.GetAI().AttackStart(unit);
                    break;
                }
                case SmartActions.SetPower:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().SetPower((PowerType)e.Action.power.powerType, (int)e.Action.power.newPower);
                    break;
                }
                case SmartActions.AddPower:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().SetPower((PowerType)e.Action.power.powerType, target.ToUnit().GetPower((PowerType)e.Action.power.powerType) + (int)e.Action.power.newPower);
                    break;
                }
                case SmartActions.RemovePower:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().SetPower((PowerType)e.Action.power.powerType, target.ToUnit().GetPower((PowerType)e.Action.power.powerType) - (int)e.Action.power.newPower);
                    break;
                }
                case SmartActions.GameEventStop:
                {
                    ushort eventId = (ushort)e.Action.gameEventStop.id;
                    if (!Global.GameEventMgr.IsActiveEvent(eventId))
                    {
                        Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: At case SMART_ACTION_GAME_EVENT_STOP, inactive event (id: {0})", eventId);
                        break;
                    }
                    Global.GameEventMgr.StopEvent(eventId, true);
                    break;
                }
                case SmartActions.GameEventStart:
                {
                    ushort eventId = (ushort)e.Action.gameEventStart.id;
                    if (Global.GameEventMgr.IsActiveEvent(eventId))
                    {
                        Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: At case SMART_ACTION_GAME_EVENT_START, already activated event (id: {0})", eventId);
                        break;
                    }
                    Global.GameEventMgr.StartEvent(eventId, true);
                    break;
                }
                case SmartActions.StartClosestWaypoint:
                {
                    MultiActionResult<MovementStopReason> waitEvent = CreateTimedActionListWaitEventFor<MultiActionResult<MovementStopReason>>(e);

                    float distanceToClosest = float.MaxValue;
                    uint closestPathId = 0;
                    uint closestWaypointId = 0;

                    foreach (var target in targets)
                    {
                        Creature creature = target.ToCreature();
                        if (creature != null)
                        {
                            if (IsSmart(creature))
                            {
                                var closestWaypointFromList = e.Action.closestWaypointFromList;
                                foreach (uint pathId in new[] { closestWaypointFromList.wp1, closestWaypointFromList.wp2, closestWaypointFromList.wp3, closestWaypointFromList.wp4, closestWaypointFromList.wp5, closestWaypointFromList.wp6 })
                                {
                                    WaypointPath path = Global.WaypointMgr.GetPath(pathId);
                                    if (path == null || path.Nodes.Empty())
                                        continue;
                                    foreach (var waypoint in path.Nodes)
                                    {
                                        float distanceToThisNode = creature.GetDistance(waypoint.X, waypoint.Y, waypoint.Z);
                                        if (distanceToThisNode < distanceToClosest)
                                        {
                                            distanceToClosest = distanceToThisNode;
                                            closestPathId = pathId;
                                            closestWaypointId = waypoint.Id;
                                        }
                                    }
                                }

                                if (closestPathId != 0)
                                {
                                    ActionResultSetter<MovementStopReason> actionResultSetter = null;
                                    if (waitEvent != null)
                                        actionResultSetter = ActionResult<MovementStopReason>.GetResultSetter(waitEvent.CreateAndGetResult());

                                    creature.GetAI<SmartAI>().StartPath(closestPathId, true, null, closestWaypointId, actionResultSetter);
                                }
                            }
                        }
                    }

                    if (waitEvent != null && !waitEvent.Results.Empty())
                        mTimedActionWaitEvent = waitEvent;
                    break;
                }
                case SmartActions.RandomSound:
                {
                    List<uint> sounds = new();
                    var randomSound = e.Action.randomSound;
                    foreach (var id in new[] { randomSound.sound1, randomSound.sound2, randomSound.sound3, randomSound.sound4 })
                        if (id != 0)
                            sounds.Add(id);

                    bool onlySelf = e.Action.randomSound.onlySelf != 0;

                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            uint sound = sounds.SelectRandom();
                            if (e.Action.randomSound.distance == 1)
                                target.PlayDistanceSound(sound, onlySelf ? target.ToPlayer() : null);
                            else
                                target.PlayDirectSound(sound, onlySelf ? target.ToPlayer() : null);

                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction:: SMART_ACTION_RANDOM_SOUND: target: {0} ({1}), sound: {2}, onlyself: {3}",
                                target.GetName(), target.GetGUID().ToString(), sound, onlySelf);
                        }
                    }
                    break;
                }
                case SmartActions.SetCorpseDelay:
                {
                    foreach (var target in targets)
                        if (IsCreature(target))
                            target.ToCreature().SetCorpseDelay(e.Action.corpseDelay.timer, e.Action.corpseDelay.includeDecayRatio == 0);
                    break;
                }
                case SmartActions.SpawnSpawngroup:
                {
                    if (e.Action.groupSpawn.minDelay == 0 && e.Action.groupSpawn.maxDelay == 0)
                    {
                        bool ignoreRespawn = ((e.Action.groupSpawn.spawnflags & (uint)SmartAiSpawnFlags.IgnoreRespawn) != 0);
                        bool force = ((e.Action.groupSpawn.spawnflags & (uint)SmartAiSpawnFlags.ForceSpawn) != 0);

                        // Instant spawn
                        GetBaseObject().GetMap().SpawnGroupSpawn(e.Action.groupSpawn.groupId, ignoreRespawn, force);
                    }
                    else
                    {
                        // Delayed spawn (use values from parameter to schedule event to call us back
                        SmartEvent ne = new();
                        ne.type = SmartEvents.Update;
                        ne.event_chance = 100;

                        ne.minMaxRepeat.min = e.Action.groupSpawn.minDelay;
                        ne.minMaxRepeat.max = e.Action.groupSpawn.maxDelay;
                        ne.minMaxRepeat.repeatMin = 0;
                        ne.minMaxRepeat.repeatMax = 0;

                        ne.event_flags = 0;
                        ne.event_flags |= SmartEventFlags.NotRepeatable;

                        SmartAction ac = new();
                        ac.type = SmartActions.SpawnSpawngroup;
                        ac.groupSpawn.groupId = e.Action.groupSpawn.groupId;
                        ac.groupSpawn.minDelay = 0;
                        ac.groupSpawn.maxDelay = 0;
                        ac.groupSpawn.spawnflags = e.Action.groupSpawn.spawnflags;
                        ac.timeEvent.id = e.Action.timeEvent.id;

                        SmartScriptHolder ev = new();
                        ev.Event = ne;
                        ev.EventId = e.EventId;
                        ev.Target = e.Target;
                        ev.Action = ac;
                        InitTimer(ev);
                        _storedEvents.Add(ev);
                    }
                    break;
                }
                case SmartActions.DespawnSpawngroup:
                {
                    if (e.Action.groupSpawn.minDelay == 0 && e.Action.groupSpawn.maxDelay == 0)
                    {
                        bool deleteRespawnTimes = ((e.Action.groupSpawn.spawnflags & (uint)SmartAiSpawnFlags.NosaveRespawn) != 0);

                        // Instant spawn
                        GetBaseObject().GetMap().SpawnGroupSpawn(e.Action.groupSpawn.groupId, deleteRespawnTimes);
                    }
                    else
                    {
                        // Delayed spawn (use values from parameter to schedule event to call us back
                        SmartEvent ne = new();
                        ne.type = SmartEvents.Update;
                        ne.event_chance = 100;

                        ne.minMaxRepeat.min = e.Action.groupSpawn.minDelay;
                        ne.minMaxRepeat.max = e.Action.groupSpawn.maxDelay;
                        ne.minMaxRepeat.repeatMin = 0;
                        ne.minMaxRepeat.repeatMax = 0;

                        ne.event_flags = 0;
                        ne.event_flags |= SmartEventFlags.NotRepeatable;

                        SmartAction ac = new();
                        ac.type = SmartActions.DespawnSpawngroup;
                        ac.groupSpawn.groupId = e.Action.groupSpawn.groupId;
                        ac.groupSpawn.minDelay = 0;
                        ac.groupSpawn.maxDelay = 0;
                        ac.groupSpawn.spawnflags = e.Action.groupSpawn.spawnflags;
                        ac.timeEvent.id = e.Action.timeEvent.id;

                        SmartScriptHolder ev = new();
                        ev.Event = ne;
                        ev.EventId = e.EventId;
                        ev.Target = e.Target;
                        ev.Action = ac;
                        InitTimer(ev);
                        _storedEvents.Add(ev);
                    }
                    break;
                }
                case SmartActions.DisableEvade:
                {
                    if (!IsSmart())
                        break;

                    ((SmartAI)_me.GetAI()).SetEvadeDisabled(e.Action.disableEvade.disable != 0);
                    break;
                }
                case SmartActions.AddThreat:
                {
                    if (!_me.CanHaveThreatList())
                        break;

                    foreach (var target in targets)
                        if (IsUnit(target))
                            _me.GetThreatManager().AddThreat(target.ToUnit(), (float)(e.Action.threat.threatINC - (float)e.Action.threat.threatDEC), null, true, true);

                    break;
                }
                case SmartActions.LoadEquipment:
                {
                    foreach (var target in targets)
                        if (IsCreature(target))
                            target.ToCreature().LoadEquipment((int)e.Action.loadEquipment.id, e.Action.loadEquipment.force != 0);

                    break;
                }
                case SmartActions.TriggerRandomTimedEvent:
                {
                    uint eventId = RandomHelper.URand(e.Action.randomTimedEvent.minId, e.Action.randomTimedEvent.maxId);
                    ProcessEventsFor(SmartEvents.TimedEventTriggered, null, eventId);
                    break;
                }
                case SmartActions.PauseMovement:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().PauseMovement(e.Action.pauseMovement.pauseTimer, (MovementSlot)e.Action.pauseMovement.movementSlot, e.Action.pauseMovement.force != 0);

                    break;
                }
                case SmartActions.RespawnBySpawnId:
                {
                    Map map = null;
                    WorldObject obj = GetBaseObject();
                    if (obj != null)
                        map = obj.GetMap();
                    else if (!targets.Empty())
                        map = targets.First().GetMap();

                    if (map != null)
                        map.Respawn((SpawnObjectType)e.Action.respawnData.spawnType, e.Action.respawnData.spawnId);
                    else
                        Log.outError(LogFilter.Sql, $"SmartScript.ProcessAction: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()}, Event {e.EventId} - tries to respawn by spawnId but does not provide a map");
                    break;
                }
                case SmartActions.PlayAnimkit:
                {
                    foreach (var target in targets)
                    {
                        if (IsCreature(target))
                        {
                            if (e.Action.animKit.type == 0)
                                target.ToCreature().PlayOneShotAnimKitId((ushort)e.Action.animKit.animKit);
                            else if (e.Action.animKit.type == 1)
                                target.ToCreature().SetAIAnimKitId((ushort)e.Action.animKit.animKit);
                            else if (e.Action.animKit.type == 2)
                                target.ToCreature().SetMeleeAnimKitId((ushort)e.Action.animKit.animKit);
                            else if (e.Action.animKit.type == 3)
                                target.ToCreature().SetMovementAnimKitId((ushort)e.Action.animKit.animKit);

                            Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction:: SMART_ACTION_PLAY_ANIMKIT: target: {target.GetName()} ({target.GetGUID()}), AnimKit: {e.Action.animKit.animKit}, Type: {e.Action.animKit.type}");
                        }
                        else if (IsGameObject(target))
                        {
                            switch (e.Action.animKit.type)
                            {
                                case 0:
                                    target.ToGameObject().SetAnimKitId((ushort)e.Action.animKit.animKit, true);
                                    break;
                                case 1:
                                    target.ToGameObject().SetAnimKitId((ushort)e.Action.animKit.animKit, false);
                                    break;
                                default:
                                    break;
                            }

                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction:: SMART_ACTION_PLAY_ANIMKIT: target: {0} ({1}), AnimKit: {2}, Type: {3}", target.GetName(), target.GetGUID().ToString(), e.Action.animKit.animKit, e.Action.animKit.type);
                        }
                    }
                    break;
                }
                case SmartActions.ScenePlay:
                {
                    foreach (var target in targets)
                    {
                        Player playerTarget = target.ToPlayer();
                        if (playerTarget != null)
                            playerTarget.GetSceneMgr().PlayScene(e.Action.scene.sceneId);
                    }

                    break;
                }
                case SmartActions.SceneCancel:
                {
                    foreach (var target in targets)
                    {
                        Player playerTarget = target.ToPlayer();
                        if (playerTarget != null)
                            playerTarget.GetSceneMgr().CancelSceneBySceneId(e.Action.scene.sceneId);
                    }

                    break;
                }
                case SmartActions.PlayCinematic:
                {
                    foreach (var target in targets)
                    {
                        if (!IsPlayer(target))
                            continue;

                        target.ToPlayer().SendCinematicStart(e.Action.cinematic.entry);
                    }
                    break;
                }
                case SmartActions.SetMovementSpeed:
                {
                    uint speedInteger = e.Action.movementSpeed.speedInteger;
                    uint speedFraction = e.Action.movementSpeed.speedFraction;
                    float speed = (float)((float)speedInteger + (float)speedFraction / Math.Pow(10, Math.Floor(Math.Log10((float)(speedFraction != 0 ? speedFraction : 1)) + 1)));

                    foreach (var target in targets)
                        if (IsCreature(target))
                            target.ToCreature().SetSpeed((UnitMoveType)e.Action.movementSpeed.movementType, speed);

                    break;
                }
                case SmartActions.PlaySpellVisualKit:
                {
                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            target.ToUnit().SendPlaySpellVisualKit(e.Action.spellVisualKit.spellVisualKitId, e.Action.spellVisualKit.kitType,
                                e.Action.spellVisualKit.duration);

                            Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction:: SMART_ACTION_PLAY_SPELL_VISUAL_KIT: target: {target.GetName()} ({target.GetGUID()}), SpellVisualKit: {e.Action.spellVisualKit.spellVisualKitId}");
                        }
                    }

                    break;
                }
                case SmartActions.OverrideLight:
                {
                    WorldObject obj = GetBaseObject();
                    if (obj != null)
                    {
                        obj.GetMap().SetZoneOverrideLight(e.Action.overrideLight.zoneId, e.Action.overrideLight.areaLightId, e.Action.overrideLight.overrideLightId, TimeSpan.FromMilliseconds(e.Action.overrideLight.transitionMilliseconds));
                        Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction: SMART_ACTION_OVERRIDE_LIGHT: {obj.GetGUID()} sets zone override light (zoneId: {e.Action.overrideLight.zoneId}, " +
                            $"areaLightId: {e.Action.overrideLight.areaLightId}, overrideLightId: {e.Action.overrideLight.overrideLightId}, transitionMilliseconds: {e.Action.overrideLight.transitionMilliseconds})");
                    }
                    break;
                }
                case SmartActions.OverrideWeather:
                {
                    WorldObject obj = GetBaseObject();
                    if (obj != null)
                    {
                        obj.GetMap().SetZoneWeather(e.Action.overrideWeather.zoneId, (WeatherState)e.Action.overrideWeather.weatherId, e.Action.overrideWeather.intensity);
                        Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction: SMART_ACTION_OVERRIDE_WEATHER: {obj.GetGUID()} sets zone weather (zoneId: {e.Action.overrideWeather.zoneId}, " +
                            $"weatherId: {e.Action.overrideWeather.weatherId}, intensity: {e.Action.overrideWeather.intensity})");
                    }
                    break;
                }
                case SmartActions.SetHover:
                {
                    foreach (WorldObject target in targets)
                        if (IsUnit(target))
                            target.ToUnit().SetHover(e.Action.setHover.enable != 0);
                    break;
                }
                case SmartActions.SetHealthPct:
                {
                    foreach (var target in targets)
                    {
                        Unit targetUnit = target.ToUnit();
                        if (targetUnit != null)
                            targetUnit.SetHealth(targetUnit.CountPctFromMaxHealth((int)e.Action.setHealthPct.percent));
                    }

                    break;
                }
                case SmartActions.CreateConversation:
                {
                    WorldObject baseObject = GetBaseObject();

                    foreach (WorldObject target in targets)
                    {
                        Player playerTarget = target.ToPlayer();
                        if (playerTarget != null)
                        {
                            Conversation conversation = Conversation.CreateConversation(e.Action.conversation.id, playerTarget,
                                playerTarget, playerTarget.GetGUID(), null);
                            if (conversation == null)
                                Log.outWarn(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_CREATE_CONVERSATION: id {e.Action.conversation.id}, baseObject {baseObject?.GetName()}, target {playerTarget.GetName()} - failed to create");
                        }
                    }

                    break;
                }
                case SmartActions.SetImmunePC:
                {
                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            if (e.Action.setImmunePC.immunePC != 0)
                                target.ToUnit().SetUnitFlag(UnitFlags.ImmuneToPc);
                            else
                                target.ToUnit().RemoveUnitFlag(UnitFlags.ImmuneToPc);
                        }
                    }
                    break;
                }
                case SmartActions.SetImmuneNPC:
                {
                    foreach (var target in targets)
                    {
                        if (IsUnit(target))
                        {
                            if (e.Action.setImmuneNPC.immuneNPC != 0)
                                target.ToUnit().SetUnitFlag(UnitFlags.ImmuneToNpc);
                            else
                                target.ToUnit().RemoveUnitFlag(UnitFlags.ImmuneToNpc);
                        }
                    }
                    break;
                }
                case SmartActions.SetUninteractible:
                {
                    foreach (var target in targets)
                        if (IsUnit(target))
                            target.ToUnit().SetUninteractible(e.Action.setUninteractible.uninteractible != 0);
                    break;
                }
                case SmartActions.ActivateGameobject:
                {
                    foreach (WorldObject target in targets)
                    {
                        GameObject targetGo = target.ToGameObject();
                        if (targetGo != null)
                            targetGo.ActivateObject((GameObjectActions)e.Action.activateGameObject.gameObjectAction, (int)e.Action.activateGameObject.param, GetBaseObject());
                    }
                    break;
                }
                case SmartActions.AddToStoredTargetList:
                {
                    if (!targets.Empty())
                        AddToStoredTargetList(targets, e.Action.addToStoredTargets.id);
                    else
                    {
                        WorldObject baseObject = GetBaseObject();
                        Log.outWarn(LogFilter.ScriptsAi, $"SmartScript::ProcessAction:: SMART_ACTION_ADD_TO_STORED_TARGET_LIST: var {e.Action.addToStoredTargets.id}, baseObject {(baseObject == null ? "" : baseObject.GetName())}, event {e.EventId} - tried to add no targets to stored target list");
                    }
                    break;
                }
                case SmartActions.BecomePersonalCloneForPlayer:
                {
                    WorldObject baseObject = GetBaseObject();

                    void doCreatePersonalClone(Position position, Player privateObjectOwner)
                    {
                        Creature summon = GetBaseObject().SummonPersonalClone(position, (TempSummonType)e.Action.becomePersonalClone.type, TimeSpan.FromMilliseconds(e.Action.becomePersonalClone.duration), 0, 0, privateObjectOwner);
                        if (summon != null)
                            if (IsSmart(summon))
                                ((SmartAI)summon.GetAI()).SetTimedActionList(e, (uint)e.EntryOrGuid, privateObjectOwner, e.EventId + 1);
                    }

                    // if target is position then targets container was empty
                    if (e.GetTargetType() != SmartTargets.Position)
                    {
                        foreach (WorldObject target in targets)
                        {
                            Player playerTarget = target?.ToPlayer();
                            if (playerTarget != null)
                                doCreatePersonalClone(baseObject.GetPosition(), playerTarget);
                        }
                    }
                    else
                    {
                        Player invoker = GetLastInvoker()?.ToPlayer();
                        if (invoker != null)
                            doCreatePersonalClone(new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o), invoker);
                    }

                    // action list will continue on personal clones
                    _timedActionList.RemoveAll(script => { return script.EventId > e.EventId; });
                    break;
                }
                case SmartActions.TriggerGameEvent:
                {
                    WorldObject sourceObject = GetBaseObjectOrUnitInvoker(unit);
                    foreach (WorldObject target in targets)
                    {
                        if (e.Action.triggerGameEvent.useSaiTargetAsGameEventSource != 0)
                            GameEvents.Trigger(e.Action.triggerGameEvent.eventId, target, sourceObject);
                        else
                            GameEvents.Trigger(e.Action.triggerGameEvent.eventId, sourceObject, target);
                    }

                    break;
                }
                case SmartActions.DoAction:
                {
                    foreach (WorldObject target in targets)
                    {
                        Unit unitTarget = target?.ToUnit();
                        if (unitTarget != null)
                            unitTarget.GetAI()?.DoAction((int)e.Action.doAction.actionId);
                        else
                        {
                            GameObject goTarget = target?.ToGameObject();
                            if (goTarget != null)
                                goTarget.GetAI()?.DoAction((int)e.Action.doAction.actionId);
                        }
                    }

                    break;
                }
                case SmartActions.CompleteQuest:
                {
                    uint questId = e.Action.quest.questId;
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                    if (quest == null)
                        break;

                    foreach (WorldObject target in targets)
                    {
                        Player player = target?.ToPlayer();
                        if (player == null)
                            continue;

                        QuestStatus questStatus = player.GetQuestStatus(questId);
                        if (questStatus == QuestStatus.Rewarded)
                            continue;

                        if (quest.HasFlag(QuestFlags.CompletionEvent) || quest.HasFlag(QuestFlags.CompletionAreaTrigger))
                        {
                            if (questStatus == QuestStatus.Incomplete)
                                player.AreaExploredOrEventHappens(questId);
                        }
                        else if (quest.HasFlag(QuestFlags.TrackingEvent)) // Check if the quest is used as a serverside flag
                            player.CompleteQuest(questId);
                    }

                    break;
                }
                case SmartActions.CreditQuestObjectiveTalkTo:
                {
                    if (_me == null)
                        break;

                    foreach (WorldObject target in targets)
                    {
                        Player player = target?.ToPlayer();
                        if (player == null)
                            continue;

                        player.TalkedToCreature(_me.GetEntry(), _me.GetGUID());
                    }
                    break;
                }
                default:
                    Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Unhandled Action type {3}", e.EntryOrGuid, e.GetScriptType(), e.EventId, e.GetActionType());
                    break;
            }

            if (e.Link != 0 && e.Link != e.EventId)
            {
                SmartScriptHolder linked = Global.SmartAIMgr.FindLinkedEvent(_events, e.Link);
                if (linked != null)
                    ProcessEvent(linked, unit, var0, var1, bvar, spell, gob, varString);
                else
                    Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Link Event {3} not found or invalid, skipped.", e.EntryOrGuid, e.GetScriptType(), e.EventId, e.Link);
            }
        }

        void ProcessTimedAction(SmartScriptHolder e, uint min, uint max, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
        {
            // We may want to execute action rarely and because of this if condition is not fulfilled the action will be rechecked in a long time
            if (Global.ConditionMgr.IsObjectMeetingSmartEventConditions(e.EntryOrGuid, e.EventId, e.SourceType, unit, GetBaseObject()))
            {
                RecalcTimer(e, min, max);
                ProcessAction(e, unit, var0, var1, bvar, spell, gob, varString);
            }
            else
                RecalcTimer(e, Math.Min(min, 5000), Math.Min(min, 5000));
        }

        SmartScriptHolder CreateSmartEvent(SmartEvents e, SmartEventFlags event_flags, uint event_param1, uint event_param2, uint event_param3, uint event_param4, uint event_param5,
            SmartActions action, uint action_param1, uint action_param2, uint action_param3, uint action_param4, uint action_param5, uint action_param6, uint action_param7,
            SmartTargets t, uint target_param1, uint target_param2, uint target_param3, uint target_param4, string targetParamString, uint phaseMask)
        {
            SmartScriptHolder script = new();
            script.Event.type = e;
            script.Event.raw.param1 = event_param1;
            script.Event.raw.param2 = event_param2;
            script.Event.raw.param3 = event_param3;
            script.Event.raw.param4 = event_param4;
            script.Event.raw.param5 = event_param5;
            script.Event.event_phase_mask = phaseMask;
            script.Event.event_flags = event_flags;
            script.Event.event_chance = 100;

            script.Action.type = action;
            script.Action.raw.param1 = action_param1;
            script.Action.raw.param2 = action_param2;
            script.Action.raw.param3 = action_param3;
            script.Action.raw.param4 = action_param4;
            script.Action.raw.param5 = action_param5;
            script.Action.raw.param6 = action_param6;
            script.Action.raw.param7 = action_param7;

            script.Target.type = t;
            script.Target.raw.param1 = target_param1;
            script.Target.raw.param2 = target_param2;
            script.Target.raw.param3 = target_param3;
            script.Target.raw.param4 = target_param4;
            script.Target.param_string = targetParamString;

            script.SourceType = SmartScriptType.Creature;
            InitTimer(script);
            return script;
        }

        List<WorldObject> GetTargets(SmartScriptHolder e, WorldObject invoker = null)
        {
            WorldObject scriptTrigger = null;
            if (invoker != null)
                scriptTrigger = invoker;
            else
            {
                Unit tempLastInvoker = GetLastInvoker();
                if (tempLastInvoker != null)
                    scriptTrigger = tempLastInvoker;
            }

            WorldObject baseObject = GetBaseObject();

            List<WorldObject> targets = new();
            switch (e.GetTargetType())
            {
                case SmartTargets.Self:
                    if (baseObject != null)
                        targets.Add(baseObject);
                    break;
                case SmartTargets.Victim:
                    if (_me != null && _me.GetVictim() != null)
                        targets.Add(_me.GetVictim());
                    break;
                case SmartTargets.HostileSecondAggro:
                    if (_me != null)
                    {
                        if (e.Target.hostilRandom.powerType != 0)
                        {
                            Unit u = _me.GetAI().SelectTarget(SelectTargetMethod.MaxThreat, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));
                            if (u != null)
                                targets.Add(u);
                        }
                        else
                        {
                            Unit u = _me.GetAI().SelectTarget(SelectTargetMethod.MaxThreat, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);
                            if (u != null)
                                targets.Add(u);
                        }
                    }
                    break;
                case SmartTargets.HostileLastAggro:
                    if (_me != null)
                    {
                        if (e.Target.hostilRandom.powerType != 0)
                        {
                            Unit u = _me.GetAI().SelectTarget(SelectTargetMethod.MinThreat, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));
                            if (u != null)
                                targets.Add(u);
                        }
                        else
                        {
                            Unit u = _me.GetAI().SelectTarget(SelectTargetMethod.MinThreat, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);
                            if (u != null)
                                targets.Add(u);
                        }
                    }
                    break;
                case SmartTargets.HostileRandom:
                    if (_me != null)
                    {
                        if (e.Target.hostilRandom.powerType != 0)
                        {
                            Unit u = _me.GetAI().SelectTarget(SelectTargetMethod.Random, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));
                            if (u != null)
                                targets.Add(u);
                        }
                        else
                        {
                            Unit u = _me.GetAI().SelectTarget(SelectTargetMethod.Random, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);
                            if (u != null)
                                targets.Add(u);
                        }
                    }
                    break;
                case SmartTargets.HostileRandomNotTop:
                    if (_me != null)
                    {
                        if (e.Target.hostilRandom.powerType != 0)
                        {
                            Unit u = _me.GetAI().SelectTarget(SelectTargetMethod.Random, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));
                            if (u != null)
                                targets.Add(u);
                        }
                        else
                        {
                            Unit u = _me.GetAI().SelectTarget(SelectTargetMethod.Random, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);
                            if (u != null)
                                targets.Add(u);
                        }
                    }
                    break;
                case SmartTargets.Farthest:
                    if (_me != null)
                    {
                        Unit u = _me.GetAI().SelectTarget(SelectTargetMethod.MaxDistance, 0, new FarthestTargetSelector(_me, (float)e.Target.farthest.maxDist, e.Target.farthest.playerOnly != 0, e.Target.farthest.isInLos != 0));
                        if (u != null)
                            targets.Add(u);
                    }
                    break;
                case SmartTargets.ActionInvoker:
                    if (scriptTrigger != null)
                        targets.Add(scriptTrigger);
                    break;
                case SmartTargets.ActionInvokerVehicle:
                    if (scriptTrigger != null && scriptTrigger.ToUnit()?.GetVehicle() != null && scriptTrigger.ToUnit().GetVehicle().GetBase() != null)
                        targets.Add(scriptTrigger.ToUnit().GetVehicle().GetBase());
                    break;
                case SmartTargets.InvokerParty:
                    if (scriptTrigger != null)
                    {
                        Player player = scriptTrigger.ToPlayer();
                        if (player != null)
                        {
                            Group group = player.GetGroup();
                            if (group != null)
                            {
                                for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                                {
                                    Player member = groupRef.GetSource();
                                    if (member != null)
                                        if (member.IsInMap(player))
                                            targets.Add(member);
                                }
                            }
                            // We still add the player to the list if there is no group. If we do
                            // this even if there is a group (thus the else-check), it will add the
                            // same player to the list twice. We don't want that to happen.
                            else
                                targets.Add(scriptTrigger);
                        }
                    }
                    break;
                case SmartTargets.CreatureRange:
                {
                    WorldObject refObj = baseObject;
                    if (refObj == null)
                        refObj = scriptTrigger;

                    if (refObj == null)
                    {
                        Log.outError(LogFilter.Sql, $"SMART_TARGET_CREATURE_RANGE: {e} is missing base object or invoker.");
                        break;
                    }

                    List<Creature> creatures = refObj.GetCreatureListWithOptionsInGrid(e.Target.unitRange.maxDist,
                        new FindCreatureOptions() { CreatureId = e.Target.unitRange.creature != 0 ? e.Target.unitRange.creature : null, StringId = !e.Target.param_string.IsEmpty() ? e.Target.param_string : null, });
                    targets.AddRange(creatures.Where(target => !refObj.IsWithinDist(target, e.Target.unitRange.minDist)));

                    if (e.Target.unitRange.maxSize != 0)
                        targets.RandomResize(e.Target.unitRange.maxSize);
                    break;
                }
                case SmartTargets.CreatureDistance:
                {
                    if (baseObject == null)
                        break;

                    List<Creature> creatures = baseObject.GetCreatureListWithOptionsInGrid(e.Target.unitDistance.dist,
                        new FindCreatureOptions() { CreatureId = e.Target.unitDistance.creature != 0 ? e.Target.unitDistance.creature : null, StringId = !e.Target.param_string.IsEmpty() ? e.Target.param_string : null });

                    targets.Clear();
                    targets.AddRange(creatures);

                    if (e.Target.unitDistance.maxSize != 0)
                        targets.RandomResize(e.Target.unitDistance.maxSize);
                    break;
                }
                case SmartTargets.GameobjectRange:
                {
                    WorldObject refObj = baseObject;
                    if (refObj == null)
                        refObj = scriptTrigger;

                    if (refObj == null)
                    {
                        Log.outError(LogFilter.Sql, $"SMART_TARGET_GAMEOBJECT_RANGE: {e} is missing base object or invoker.");
                        break;
                    }

                    List<GameObject> gameObjects = refObj.GetGameObjectListWithOptionsInGrid(e.Target.goRange.maxDist,
                        new FindGameObjectOptions() { GameObjectId = e.Target.goRange.entry != 0 ? e.Target.goRange.entry : null, StringId = !e.Target.param_string.IsEmpty() ? e.Target.param_string : null });
                    targets.AddRange(gameObjects.Where(target => !refObj.IsWithinDist(target, e.Target.goRange.minDist)));

                    if (e.Target.goRange.maxSize != 0)
                        targets.RandomResize(e.Target.goRange.maxSize);
                    break;
                }
                case SmartTargets.GameobjectDistance:
                {
                    if (baseObject == null)
                        break;

                    List<GameObject> gameObjects = baseObject.GetGameObjectListWithOptionsInGrid(e.Target.goDistance.dist,
                        new FindGameObjectOptions() { GameObjectId = e.Target.goDistance.entry != 0 ? e.Target.goDistance.entry : null, StringId = !e.Target.param_string.IsEmpty() ? e.Target.param_string : null });

                    targets.Clear();
                    targets.AddRange(gameObjects);

                    if (e.Target.goDistance.maxSize != 0)
                        targets.RandomResize(e.Target.goDistance.maxSize);
                    break;
                }
                case SmartTargets.CreatureGuid:
                {
                    if (scriptTrigger == null && baseObject == null)
                    {
                        Log.outError(LogFilter.Sql, $"SMART_TARGET_CREATURE_GUID {e} can not be used without invoker");
                        break;
                    }

                    Creature target = FindCreatureNear(scriptTrigger != null ? scriptTrigger : baseObject, e.Target.unitGUID.dbGuid);
                    if (target != null)
                        if (target != null && (e.Target.unitGUID.entry == 0 || target.GetEntry() == e.Target.unitGUID.entry))
                            targets.Add(target);
                    break;
                }
                case SmartTargets.GameobjectGuid:
                {
                    if (scriptTrigger == null && baseObject == null)
                    {
                        Log.outError(LogFilter.Sql, $"SMART_TARGET_GAMEOBJECT_GUID {e} can not be used without invoker");
                        break;
                    }

                    GameObject target = FindGameObjectNear(scriptTrigger != null ? scriptTrigger : baseObject, e.Target.goGUID.dbGuid);
                    if (target != null)
                        if (target != null && (e.Target.goGUID.entry == 0 || target.GetEntry() == e.Target.goGUID.entry))
                            targets.Add(target);
                    break;
                }
                case SmartTargets.PlayerRange:
                {
                    if (baseObject == null)
                        break;

                    List<Player> players = baseObject.GetPlayerListInGrid(e.Target.playerRange.maxDist);
                    targets.AddRange(players.Where(target => !baseObject.IsWithinDist(target, e.Target.playerRange.minDist)));

                    break;
                }
                case SmartTargets.PlayerDistance:
                {
                    if (baseObject == null)
                        break;

                    List<Player> players = baseObject.GetPlayerListInGrid(e.Target.playerDistance.dist);
                    targets.AddRange(players);
                    break;
                }
                case SmartTargets.Stored:
                {
                    WorldObject refObj = baseObject;
                    if (refObj == null)
                        refObj = scriptTrigger;

                    if (refObj == null)
                    {
                        Log.outError(LogFilter.Sql, $"SMART_TARGET_STORED: {e} is missing base object or invoker.");
                        break;
                    }

                    var stored = GetStoredTargetList(e.Target.stored.id, refObj);
                    if (stored != null)
                        targets.AddRange(stored);

                    break;
                }
                case SmartTargets.ClosestCreature:
                {
                    WorldObject refObj = baseObject;
                    if (refObj == null)
                        refObj = scriptTrigger;

                    if (refObj == null)
                    {
                        Log.outError(LogFilter.Sql, $"SMART_TARGET_CLOSEST_CREATURE: {e} is missing base object or invoker.");
                        break;
                    }

                    Creature target = refObj.FindNearestCreatureWithOptions(e.Target.unitClosest.dist != 0 ? e.Target.unitClosest.dist : 100,
                        new FindCreatureOptions() { CreatureId = e.Target.unitClosest.entry, StringId = !e.Target.param_string.IsEmpty() ? e.Target.param_string : null, IsAlive = (FindCreatureAliveState)e.Target.unitClosest.findCreatureAliveState });

                    if (target != null)
                        targets.Add(target);
                    break;
                }
                case SmartTargets.ClosestGameobject:
                {
                    WorldObject refObj = baseObject;
                    if (refObj == null)
                        refObj = scriptTrigger;

                    if (refObj == null)
                    {
                        Log.outError(LogFilter.Sql, $"SMART_TARGET_CLOSEST_GAMEOBJECT: {e} is missing base object or invoker.");
                        break;
                    }

                    GameObject target = refObj.FindNearestGameObjectWithOptions(e.Target.goClosest.dist != 0 ? e.Target.goClosest.dist : 100,
                        new FindGameObjectOptions() { GameObjectId = e.Target.goClosest.entry, StringId = !e.Target.param_string.IsEmpty() ? e.Target.param_string : null });

                    if (target != null)
                        targets.Add(target);
                    break;
                }
                case SmartTargets.ClosestPlayer:
                {
                    WorldObject refObj = baseObject;
                    if (refObj == null)
                        refObj = scriptTrigger;

                    if (refObj == null)
                    {
                        Log.outError(LogFilter.Sql, $"SMART_TARGET_CLOSEST_PLAYER: {e} is missing base object or invoker.");
                        break;
                    }

                    Player target = refObj.SelectNearestPlayer(e.Target.playerDistance.dist);
                    if (target != null)
                        targets.Add(target);
                    break;
                }
                case SmartTargets.OwnerOrSummoner:
                {
                    if (_me != null)
                    {
                        ObjectGuid charmerOrOwnerGuid = _me.GetCharmerOrOwnerGUID();
                        if (charmerOrOwnerGuid.IsEmpty())
                        {
                            TempSummon tempSummon = _me.ToTempSummon();
                            if (tempSummon != null)
                            {
                                WorldObject summoner = tempSummon.GetSummoner();
                                if (summoner != null)
                                    charmerOrOwnerGuid = summoner.GetGUID();
                            }
                        }

                        if (charmerOrOwnerGuid.IsEmpty())
                            charmerOrOwnerGuid = _me.GetCreatorGUID();

                        WorldObject owner = Global.ObjAccessor.GetWorldObject(_me, charmerOrOwnerGuid);
                        if (owner != null)
                            targets.Add(owner);
                    }
                    else if (_go != null)
                    {
                        Unit owner = Global.ObjAccessor.GetUnit(_go, _go.GetOwnerGUID());
                        if (owner != null)
                            targets.Add(owner);
                    }

                    // Get owner of owner
                    if (e.Target.owner.useCharmerOrOwner != 0 && !targets.Empty())
                    {
                        WorldObject owner = targets.First();
                        targets.Clear();

                        Unit unitBase = Global.ObjAccessor.GetUnit(owner, owner.GetCharmerOrOwnerGUID());
                        if (unitBase != null)
                            targets.Add(unitBase);
                    }
                    break;
                }
                case SmartTargets.ThreatList:
                {
                    if (_me != null && _me.CanHaveThreatList())
                    {
                        foreach (var refe in _me.GetThreatManager().GetSortedThreatList())
                            if (e.Target.threatList.maxDist == 0 || _me.IsWithinCombatRange(refe.GetVictim(), e.Target.threatList.maxDist))
                                targets.Add(refe.GetVictim());
                    }
                    break;
                }
                case SmartTargets.ClosestEnemy:
                {
                    if (_me != null)
                    {
                        Unit target = _me.SelectNearestTarget(e.Target.closestAttackable.maxDist);
                        if (target != null)
                            targets.Add(target);
                    }

                    break;
                }
                case SmartTargets.ClosestFriendly:
                {
                    if (_me != null)
                    {
                        Unit target = DoFindClosestFriendlyInRange(e.Target.closestFriendly.maxDist);
                        if (target != null)
                            targets.Add(target);
                    }
                    break;
                }
                case SmartTargets.LootRecipients:
                {
                    if (_me != null)
                    {
                        foreach (ObjectGuid tapperGuid in _me.GetTapList())
                        {
                            Player tapper = Global.ObjAccessor.GetPlayer(_me, tapperGuid);
                            if (tapper != null)
                                targets.Add(tapper);
                        }
                    }
                    break;
                }
                case SmartTargets.VehiclePassenger:
                {
                    if (_me != null && _me.IsVehicle())
                    {
                        foreach (var pair in _me.GetVehicleKit().Seats)
                        {
                            if (e.Target.vehicle.seatMask == 0 || (e.Target.vehicle.seatMask & (1 << pair.Key)) != 0)
                            {
                                Unit u = Global.ObjAccessor.GetUnit(_me, pair.Value.Passenger.Guid);
                                if (u != null)
                                    targets.Add(u);
                            }
                        }
                    }
                    break;
                }
                case SmartTargets.ClosestUnspawnedGameobject:
                {
                    GameObject target = baseObject.FindNearestUnspawnedGameObject(e.Target.goClosest.entry, (float)(e.Target.goClosest.dist != 0 ? e.Target.goClosest.dist : 100));
                    if (target != null)
                        targets.Add(target);
                    break;
                }
                case SmartTargets.Position:
                default:
                    break;
            }

            return targets;
        }

        void ProcessEvent(SmartScriptHolder e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
        {
            if (!e.Active && e.GetEventType() != SmartEvents.Link)
                return;

            if ((e.Event.event_phase_mask != 0 && !IsInPhase(e.Event.event_phase_mask)) || (e.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) && e.RunOnce))
                return;

            if (!e.Event.event_flags.HasAnyFlag(SmartEventFlags.WhileCharmed) && IsCharmedCreature(_me))
                return;

            switch (e.GetEventType())
            {
                case SmartEvents.Link://special handling
                    ProcessAction(e, unit, var0, var1, bvar, spell, gob);
                    break;
                //called from Update tick
                case SmartEvents.Update:
                    ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    break;
                case SmartEvents.UpdateOoc:
                    if (_me != null && _me.IsEngaged())
                        return;
                    ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    break;
                case SmartEvents.UpdateIc:
                    if (_me == null || !_me.IsEngaged())
                        return;
                    ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    break;
                case SmartEvents.HealthPct:
                {
                    if (_me == null || !_me.IsEngaged() || _me.GetMaxHealth() == 0)
                        return;
                    uint perc = (uint)_me.GetHealthPct();
                    if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                        return;
                    ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    break;
                }
                case SmartEvents.ManaPct:
                {
                    if (_me == null || !_me.IsEngaged() || _me.GetMaxPower(PowerType.Mana) == 0)
                        return;
                    uint perc = (uint)_me.GetPowerPct(PowerType.Mana);
                    if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                        return;
                    ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    break;
                }
                case SmartEvents.Range:
                {
                    if (_me == null || !_me.IsEngaged() || _me.GetVictim() == null)
                        return;

                    if (_me.IsInRange(_me.GetVictim(), e.Event.minMaxRepeat.min, e.Event.minMaxRepeat.max))
                        ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax, _me.GetVictim());
                    else // make it predictable
                        RecalcTimer(e, 500, 500);
                    break;
                }
                case SmartEvents.VictimCasting:
                {
                    if (_me == null || !_me.IsEngaged())
                        return;

                    Unit victim = _me.GetVictim();

                    if (victim == null || !victim.IsNonMeleeSpellCast(false, false, true))
                        return;

                    if (e.Event.targetCasting.spellId > 0)
                    {
                        Spell currSpell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);
                        if (currSpell != null)
                            if (currSpell.m_spellInfo.Id != e.Event.targetCasting.spellId)
                                return;
                    }

                    ProcessTimedAction(e, e.Event.targetCasting.repeatMin, e.Event.targetCasting.repeatMax, _me.GetVictim());
                    break;
                }
                case SmartEvents.FriendlyIsCc:
                {
                    if (_me == null || !_me.IsEngaged())
                        return;

                    List<Creature> creatures = new();
                    DoFindFriendlyCC(creatures, e.Event.friendlyCC.radius);
                    if (creatures.Empty())
                    {
                        // if there are at least two same npcs, they will perform the same action immediately even if this is useless...
                        RecalcTimer(e, 1000, 3000);
                        return;
                    }

                    ProcessTimedAction(e, e.Event.friendlyCC.repeatMin, e.Event.friendlyCC.repeatMax, creatures.First());
                    break;
                }
                case SmartEvents.FriendlyMissingBuff:
                {
                    List<Creature> creatures = new();
                    DoFindFriendlyMissingBuff(creatures, e.Event.missingBuff.radius, e.Event.missingBuff.spell);

                    if (creatures.Empty())
                        return;

                    ProcessTimedAction(e, e.Event.missingBuff.repeatMin, e.Event.missingBuff.repeatMax, creatures.SelectRandom());
                    break;
                }
                case SmartEvents.HasAura:
                {
                    if (_me == null)
                        return;
                    uint count = _me.GetAuraCount(e.Event.aura.spell);
                    if ((e.Event.aura.count == 0 && count == 0) || (e.Event.aura.count != 0 && count >= e.Event.aura.count))
                        ProcessTimedAction(e, e.Event.aura.repeatMin, e.Event.aura.repeatMax);
                    break;
                }
                case SmartEvents.TargetBuffed:
                {
                    if (_me == null || _me.GetVictim() == null)
                        return;
                    uint count = _me.GetVictim().GetAuraCount(e.Event.aura.spell);
                    if (count < e.Event.aura.count)
                        return;
                    ProcessTimedAction(e, e.Event.aura.repeatMin, e.Event.aura.repeatMax, _me.GetVictim());
                    break;
                }
                case SmartEvents.Charmed:
                {
                    if (bvar == (e.Event.charm.onRemove != 1))
                        ProcessAction(e, unit, var0, var1, bvar, spell, gob);
                    break;
                }
                case SmartEvents.QuestAccepted:
                case SmartEvents.QuestCompletion:
                case SmartEvents.QuestFail:
                case SmartEvents.QuestRewarded:
                {
                    ProcessAction(e, unit);
                    break;
                }
                case SmartEvents.QuestObjCompletion:
                {
                    if (var0 == (e.Event.questObjective.id))
                        ProcessAction(e, unit);
                    break;
                }
                //no params
                case SmartEvents.Aggro:
                case SmartEvents.Death:
                case SmartEvents.Evade:
                case SmartEvents.ReachedHome:
                case SmartEvents.Reset:
                case SmartEvents.CorpseRemoved:
                case SmartEvents.AiInit:
                case SmartEvents.TransportAddplayer:
                case SmartEvents.TransportRemovePlayer:
                case SmartEvents.AreatriggerEnter:
                case SmartEvents.JustSummoned:
                case SmartEvents.JustCreated:
                case SmartEvents.FollowCompleted:
                case SmartEvents.OnSpellclick:
                case SmartEvents.OnDespawn:
                case SmartEvents.SendEventTrigger:
                case SmartEvents.AreatriggerExit:
                    ProcessAction(e, unit, var0, var1, bvar, spell, gob);
                    break;
                case SmartEvents.GossipHello:
                {
                    switch (e.Event.gossipHello.filter)
                    {
                        case 0:
                            // no filter set, always execute action
                            break;
                        case 1:
                            // OnGossipHello only filter set, skip action if OnReportUse
                            if (var0 != 0)
                                return;
                            break;
                        case 2:
                            // OnReportUse only filter set, skip action if OnGossipHello
                            if (var0 == 0)
                                return;
                            break;
                        default:
                            // Ignore any other value
                            break;
                    }

                    ProcessAction(e, unit, var0, var1, bvar, spell, gob);
                    break;
                }
                case SmartEvents.ReceiveEmote:
                    if (e.Event.emote.emoteId == var0)
                    {
                        RecalcTimer(e, e.Event.emote.cooldownMin, e.Event.emote.cooldownMax);
                        ProcessAction(e, unit);
                    }
                    break;
                case SmartEvents.Kill:
                {
                    if (_me == null || unit == null)
                        return;
                    if (e.Event.kill.playerOnly != 0 && !unit.IsTypeId(TypeId.Player))
                        return;
                    if (e.Event.kill.creature != 0 && unit.GetEntry() != e.Event.kill.creature)
                        return;
                    RecalcTimer(e, e.Event.kill.cooldownMin, e.Event.kill.cooldownMax);
                    ProcessAction(e, unit);
                    break;
                }
                case SmartEvents.SpellHitTarget:
                case SmartEvents.SpellHit:
                {
                    if (spell == null)
                        return;
                    if ((e.Event.spellHit.spell == 0 || spell.Id == e.Event.spellHit.spell) &&
                        (e.Event.spellHit.school == 0 || Convert.ToBoolean((uint)spell.SchoolMask & e.Event.spellHit.school)))
                    {
                        RecalcTimer(e, e.Event.spellHit.cooldownMin, e.Event.spellHit.cooldownMax);
                        ProcessAction(e, unit, 0, 0, bvar, spell, gob);
                    }
                    break;
                }
                case SmartEvents.OnSpellCast:
                case SmartEvents.OnSpellFailed:
                case SmartEvents.OnSpellStart:
                {
                    if (spell == null)
                        return;

                    if (spell.Id != e.Event.spellCast.spell)
                        return;

                    RecalcTimer(e, e.Event.spellCast.cooldownMin, e.Event.spellCast.cooldownMax);
                    ProcessAction(e, null, 0, 0, bvar, spell);
                    break;
                }
                case SmartEvents.OocLos:
                {
                    if (_me == null || _me.IsEngaged())
                        return;
                    //can trigger if closer than fMaxAllowedRange
                    float range = e.Event.los.maxDist;

                    //if range is ok and we are actually in LOS
                    if (_me.IsWithinDistInMap(unit, range) && _me.IsWithinLOSInMap(unit))
                    {
                        LOSHostilityMode hostilityMode = (LOSHostilityMode)e.Event.los.hostilityMode;
                        //if friendly event&&who is not hostile OR hostile event&&who is hostile
                        if ((hostilityMode == LOSHostilityMode.Any) || (hostilityMode == LOSHostilityMode.NotHostile && !_me.IsHostileTo(unit)) || (hostilityMode == LOSHostilityMode.Hostile && _me.IsHostileTo(unit)))
                        {
                            if (e.Event.los.playerOnly != 0 && !unit.IsTypeId(TypeId.Player))
                                return;

                            RecalcTimer(e, e.Event.los.cooldownMin, e.Event.los.cooldownMax);
                            ProcessAction(e, unit);
                        }
                    }
                    break;
                }
                case SmartEvents.IcLos:
                {
                    if (_me == null || !_me.IsEngaged())
                        return;
                    //can trigger if closer than fMaxAllowedRange
                    float range = e.Event.los.maxDist;

                    //if range is ok and we are actually in LOS
                    if (_me.IsWithinDistInMap(unit, range) && _me.IsWithinLOSInMap(unit))
                    {
                        LOSHostilityMode hostilityMode = (LOSHostilityMode)e.Event.los.hostilityMode;
                        //if friendly event&&who is not hostile OR hostile event&&who is hostile
                        if ((hostilityMode == LOSHostilityMode.Any) || (hostilityMode == LOSHostilityMode.NotHostile && !_me.IsHostileTo(unit)) || (hostilityMode == LOSHostilityMode.Hostile && _me.IsHostileTo(unit)))
                        {
                            if (e.Event.los.playerOnly != 0 && !unit.IsTypeId(TypeId.Player))
                                return;

                            RecalcTimer(e, e.Event.los.cooldownMin, e.Event.los.cooldownMax);
                            ProcessAction(e, unit);
                        }
                    }
                    break;
                }
                case SmartEvents.Respawn:
                {
                    if (GetBaseObject() == null)
                        return;
                    if (e.Event.respawn.type == (uint)SmartRespawnCondition.Map && GetBaseObject().GetMapId() != e.Event.respawn.map)
                        return;
                    if (e.Event.respawn.type == (uint)SmartRespawnCondition.Area && GetBaseObject().GetZoneId() != e.Event.respawn.area)
                        return;
                    ProcessAction(e);
                    break;
                }
                case SmartEvents.SummonedUnit:
                case SmartEvents.SummonedUnitDies:
                {
                    if (!IsCreature(unit))
                        return;
                    if (e.Event.summoned.creature != 0 && unit.GetEntry() != e.Event.summoned.creature)
                        return;
                    RecalcTimer(e, e.Event.summoned.cooldownMin, e.Event.summoned.cooldownMax);
                    ProcessAction(e, unit);
                    break;
                }
                case SmartEvents.ReceiveHeal:
                case SmartEvents.Damaged:
                case SmartEvents.DamagedTarget:
                {
                    if (var0 > e.Event.minMaxRepeat.max || var0 < e.Event.minMaxRepeat.min)
                        return;
                    RecalcTimer(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    ProcessAction(e, unit);
                    break;
                }
                case SmartEvents.Movementinform:
                {
                    if ((e.Event.movementInform.type != 0 && var0 != e.Event.movementInform.type) || (e.Event.movementInform.id != 0xFFFFFFFF && var1 != e.Event.movementInform.id))
                        return;
                    ProcessAction(e, unit, var0, var1);
                    break;
                }
                case SmartEvents.TransportRelocate:
                {
                    if (e.Event.transportRelocate.pointID != 0 && var0 != e.Event.transportRelocate.pointID)
                        return;
                    ProcessAction(e, unit, var0);
                    break;
                }
                case SmartEvents.WaypointReached:
                case SmartEvents.WaypointResumed:
                case SmartEvents.WaypointPaused:
                case SmartEvents.WaypointStopped:
                case SmartEvents.WaypointEnded:
                {
                    if (_me == null || (e.Event.waypoint.pointID != 0xFFFFFFFF && var0 != e.Event.waypoint.pointID) || (e.Event.waypoint.pathID != 0 && var1 != e.Event.waypoint.pathID))
                        return;
                    ProcessAction(e, unit);
                    break;
                }
                case SmartEvents.SummonDespawned:
                {
                    if (e.Event.summoned.creature != 0 && e.Event.summoned.creature != var0)
                        return;
                    RecalcTimer(e, e.Event.summoned.cooldownMin, e.Event.summoned.cooldownMax);
                    ProcessAction(e, unit, var0);
                    break;
                }
                case SmartEvents.InstancePlayerEnter:
                {
                    if (e.Event.instancePlayerEnter.team != 0 && var0 != e.Event.instancePlayerEnter.team)
                        return;
                    RecalcTimer(e, e.Event.instancePlayerEnter.cooldownMin, e.Event.instancePlayerEnter.cooldownMax);
                    ProcessAction(e, unit, var0);
                    break;
                }
                case SmartEvents.AcceptedQuest:
                case SmartEvents.RewardQuest:
                {
                    if (e.Event.quest.questId != 0 && var0 != e.Event.quest.questId)
                        return;
                    RecalcTimer(e, e.Event.quest.cooldownMin, e.Event.quest.cooldownMax);
                    ProcessAction(e, unit, var0);
                    break;
                }
                case SmartEvents.TransportAddcreature:
                {
                    if (e.Event.transportAddCreature.creature != 0 && var0 != e.Event.transportAddCreature.creature)
                        return;
                    ProcessAction(e, unit, var0);
                    break;
                }
                case SmartEvents.TextOver:
                {
                    if (var0 != e.Event.textOver.textGroupID || (e.Event.textOver.creatureEntry != 0 && e.Event.textOver.creatureEntry != var1))
                        return;
                    ProcessAction(e, unit, var0);
                    break;
                }
                case SmartEvents.DataSet:
                {
                    if (e.Event.dataSet.id != var0 || e.Event.dataSet.value != var1)
                        return;
                    RecalcTimer(e, e.Event.dataSet.cooldownMin, e.Event.dataSet.cooldownMax);
                    ProcessAction(e, unit, var0, var1);
                    break;
                }
                case SmartEvents.PassengerRemoved:
                case SmartEvents.PassengerBoarded:
                {
                    if (unit == null)
                        return;
                    RecalcTimer(e, e.Event.minMax.repeatMin, e.Event.minMax.repeatMax);
                    ProcessAction(e, unit);
                    break;
                }
                case SmartEvents.TimedEventTriggered:
                {
                    if (e.Event.timedEvent.id == var0)
                        ProcessAction(e, unit);
                    break;
                }
                case SmartEvents.GossipSelect:
                {
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript: Gossip Select:  menu {0} action {1}", var0, var1);//little help for scripters
                    if (e.Event.gossip.sender != var0 || e.Event.gossip.action != var1)
                        return;
                    ProcessAction(e, unit, var0, var1);
                    break;
                }
                case SmartEvents.GameEventStart:
                case SmartEvents.GameEventEnd:
                {
                    if (e.Event.gameEvent.gameEventId != var0)
                        return;
                    ProcessAction(e, null, var0);
                    break;
                }
                case SmartEvents.GoLootStateChanged:
                {
                    if (e.Event.goLootStateChanged.lootState != var0)
                        return;
                    ProcessAction(e, unit, var0, var1);
                    break;
                }
                case SmartEvents.GoEventInform:
                {
                    if (e.Event.eventInform.eventId != var0)
                        return;
                    ProcessAction(e, null, var0);
                    break;
                }
                case SmartEvents.ActionDone:
                {
                    if (e.Event.doAction.eventId != var0)
                        return;
                    ProcessAction(e, unit, var0);
                    break;
                }
                case SmartEvents.FriendlyHealthPCT:
                {
                    if (_me == null || !_me.IsEngaged())
                        return;

                    Unit unitTarget = null;

                    switch (e.GetTargetType())
                    {
                        case SmartTargets.CreatureRange:
                        case SmartTargets.CreatureGuid:
                        case SmartTargets.CreatureDistance:
                        case SmartTargets.ClosestCreature:
                        case SmartTargets.ClosestPlayer:
                        case SmartTargets.PlayerRange:
                        case SmartTargets.PlayerDistance:
                        {
                            var targets = GetTargets(e);
                            foreach (WorldObject target in targets)
                            {
                                if (IsUnit(target) && _me.IsFriendlyTo(target.ToUnit()) && target.ToUnit().IsAlive() && target.ToUnit().IsInCombat())
                                {
                                    uint healthPct = (uint)target.ToUnit().GetHealthPct();
                                    if (healthPct > e.Event.friendlyHealthPct.maxHpPct || healthPct < e.Event.friendlyHealthPct.minHpPct)
                                        continue;

                                    unitTarget = target.ToUnit();
                                    break;
                                }
                            }
                        }
                        break;
                        case SmartTargets.ActionInvoker:
                            unitTarget = DoSelectLowestHpPercentFriendly((float)e.Event.friendlyHealthPct.radius, e.Event.friendlyHealthPct.minHpPct, e.Event.friendlyHealthPct.maxHpPct);
                            break;
                        default:
                            return;
                    }

                    if (unitTarget == null)
                        return;

                    ProcessTimedAction(e, e.Event.friendlyHealthPct.repeatMin, e.Event.friendlyHealthPct.repeatMax, unitTarget);
                    break;
                }
                case SmartEvents.DistanceCreature:
                {
                    if (_me == null)
                        return;

                    Creature creature = null;

                    if (e.Event.distance.guid != 0)
                    {
                        creature = FindCreatureNear(_me, e.Event.distance.guid);

                        if (creature == null)
                            return;

                        if (!_me.IsInRange(creature, 0, e.Event.distance.dist))
                            return;
                    }
                    else if (e.Event.distance.entry != 0)
                    {
                        List<Creature> list = _me.GetCreatureListWithEntryInGrid(e.Event.distance.entry, e.Event.distance.dist);

                        if (!list.Empty())
                            creature = list.FirstOrDefault();
                    }

                    if (creature != null)
                        ProcessTimedAction(e, e.Event.distance.repeat, e.Event.distance.repeat, creature);

                    break;
                }
                case SmartEvents.DistanceGameobject:
                {
                    if (_me == null)
                        return;

                    GameObject gameobject = null;

                    if (e.Event.distance.guid != 0)
                    {
                        gameobject = FindGameObjectNear(_me, e.Event.distance.guid);

                        if (gameobject == null)
                            return;

                        if (!_me.IsInRange(gameobject, 0, e.Event.distance.dist))
                            return;
                    }
                    else if (e.Event.distance.entry != 0)
                    {
                        List<GameObject> list = _me.GetGameObjectListWithEntryInGrid(e.Event.distance.entry, e.Event.distance.dist);

                        if (!list.Empty())
                            gameobject = list.FirstOrDefault();
                    }

                    if (gameobject != null)
                        ProcessTimedAction(e, e.Event.distance.repeat, e.Event.distance.repeat, null, 0, 0, false, null, gameobject);

                    break;
                }
                case SmartEvents.CounterSet:
                    if (e.Event.counter.id != var0 || GetCounterValue(e.Event.counter.id) != e.Event.counter.value)
                        return;

                    ProcessTimedAction(e, e.Event.counter.cooldownMin, e.Event.counter.cooldownMax);
                    break;
                case SmartEvents.SceneStart:
                case SmartEvents.SceneCancel:
                case SmartEvents.SceneComplete:
                {
                    ProcessAction(e, unit);
                    break;
                }
                case SmartEvents.SceneTrigger:
                {
                    if (e.Event.param_string != varString)
                        return;

                    ProcessAction(e, unit, var0, 0, false, null, null, varString);
                    break;
                }
                default:
                    Log.outError(LogFilter.Sql, "SmartScript.ProcessEvent: Unhandled Event type {0}", e.GetEventType());
                    break;
            }
        }

        void InitTimer(SmartScriptHolder e)
        {
            switch (e.GetEventType())
            {
                //set only events which have initial timers
                case SmartEvents.Update:
                case SmartEvents.UpdateIc:
                case SmartEvents.UpdateOoc:
                    RecalcTimer(e, e.Event.minMaxRepeat.min, e.Event.minMaxRepeat.max);
                    break;
                case SmartEvents.DistanceCreature:
                case SmartEvents.DistanceGameobject:
                    RecalcTimer(e, e.Event.distance.repeat, e.Event.distance.repeat);
                    break;
                default:
                    e.Active = true;
                    break;
            }
        }

        void RecalcTimer(SmartScriptHolder e, uint min, uint max)
        {
            if (e.EntryOrGuid == 15294 && e.Timer != 0)
            {
                Log.outError(LogFilter.Server, "Called RecalcTimer");
            }
            // min/max was checked at loading!
            e.Timer = RandomHelper.URand(min, max);
            e.Active = e.Timer == 0;
        }

        void UpdateTimer(SmartScriptHolder e, uint diff)
        {
            if (e.GetEventType() == SmartEvents.Link)
                return;

            if (e.Event.event_phase_mask != 0 && !IsInPhase(e.Event.event_phase_mask))
                return;

            if (e.GetEventType() == SmartEvents.UpdateIc && (_me == null || !_me.IsEngaged()))
                return;

            if (e.GetEventType() == SmartEvents.UpdateOoc && (_me != null && _me.IsEngaged()))//can be used with me=NULL (go script)
                return;

            if (e.GetScriptType() == SmartScriptType.TimedActionlist && mTimedActionWaitEvent != null && !mTimedActionWaitEvent.IsReady())
                return;

            if (e.Timer < diff)
            {
                // delay spell cast event if another spell is being casted
                if (e.GetActionType() == SmartActions.Cast)
                {
                    if (!Convert.ToBoolean(e.Action.cast.castFlags & (uint)SmartCastFlags.InterruptPrevious))
                    {
                        if (_me != null && _me.HasUnitState(UnitState.Casting))
                        {
                            RaisePriority(e);
                            return;
                        }
                    }
                }

                // Delay flee for assist event if stunned or rooted
                if (e.GetActionType() == SmartActions.FleeForAssist)
                {
                    if (_me != null && _me.HasUnitState(UnitState.Root | UnitState.LostControl))
                    {
                        e.Timer = 1;
                        return;
                    }
                }

                e.Active = true;//activate events with cooldown
                switch (e.GetEventType())//process ONLY timed events
                {
                    case SmartEvents.Update:
                    case SmartEvents.UpdateIc:
                    case SmartEvents.UpdateOoc:
                    case SmartEvents.HealthPct:
                    case SmartEvents.ManaPct:
                    case SmartEvents.Range:
                    case SmartEvents.VictimCasting:
                    case SmartEvents.FriendlyIsCc:
                    case SmartEvents.FriendlyMissingBuff:
                    case SmartEvents.HasAura:
                    case SmartEvents.TargetBuffed:
                    case SmartEvents.FriendlyHealthPCT:
                    case SmartEvents.DistanceCreature:
                    case SmartEvents.DistanceGameobject:
                    {
                        if (e.GetScriptType() == SmartScriptType.TimedActionlist)
                        {
                            Unit invoker = null;
                            if (_me != null && !mTimedActionListInvoker.IsEmpty())
                                invoker = Global.ObjAccessor.GetUnit(_me, mTimedActionListInvoker);
                            ProcessEvent(e, invoker);
                            e.EnableTimed = false;//disable event if it is in an ActionList and was processed once
                            foreach (var holder in _timedActionList)
                            {
                                //find the first event which is not the current one and enable it
                                if (holder.EventId > e.EventId)
                                {
                                    holder.EnableTimed = true;
                                    break;
                                }
                            }
                        }
                        else
                            ProcessEvent(e);
                        break;
                    }
                }

                if (e.Priority != SmartScriptHolder.DefaultPriority)
                {
                    // Reset priority to default one only if the event hasn't been rescheduled again to next loop
                    if (e.Timer > 1)
                    {
                        // Re-sort events if this was moved to the top of the queue
                        _eventSortingRequired = true;
                        // Reset priority to default one
                        e.Priority = SmartScriptHolder.DefaultPriority;
                    }
                }
            }
            else
            {
                e.Timer -= diff;
                if (e.EntryOrGuid == 15294 && _me.GetGUID().GetCounter() == 55039 && e.Timer != 0)
                {
                    Log.outError(LogFilter.Server, "Called UpdateTimer: reduce timer: e.timer: {0}, diff: {1}  current time: {2}", e.Timer, diff, Time.GetMSTime());
                }
            }

        }

        public bool CheckTimer(SmartScriptHolder e)
        {
            return e.Active;
        }

        void InstallEvents()
        {
            if (!_installEvents.Empty())
            {
                foreach (var holder in _installEvents)
                    _events.Add(holder);//must be before UpdateTimers

                _installEvents.Clear();
            }
        }

        public void OnUpdate(uint diff)
        {
            if ((_scriptType == SmartScriptType.Creature || _scriptType == SmartScriptType.GameObject || _scriptType == SmartScriptType.AreaTriggerEntity || _scriptType == SmartScriptType.AreaTriggerEntityCustom) && GetBaseObject() == null)
                return;

            if (_me != null && _me.IsInEvadeMode())
            {
                // Check if the timed action list finished and clear it if so.
                // This is required by SMART_ACTION_CALL_TIMED_ACTIONLIST failing if mTimedActionList is not empty.
                if (!_timedActionList.Empty())
                {
                    bool needCleanup1 = true;
                    foreach (SmartScriptHolder scriptholder in _timedActionList)
                    {
                        if (scriptholder.EnableTimed)
                            needCleanup1 = false;
                    }

                    if (needCleanup1)
                        _timedActionList.Clear();
                }

                return;
            }

            InstallEvents();//before UpdateTimers

            if (_eventSortingRequired)
            {
                SortEvents(_events);
                _eventSortingRequired = false;
            }

            foreach (var holder in _events)
                UpdateTimer(holder, diff);

            if (!_storedEvents.Empty())
                foreach (var holder in _storedEvents)
                    UpdateTimer(holder, diff);

            bool needCleanup = true;
            if (!_timedActionList.Empty())
            {
                for (int i = 0; i < _timedActionList.Count; ++i)
                {
                    SmartScriptHolder scriptHolder = _timedActionList[i];
                    if (scriptHolder.EnableTimed)
                    {
                        UpdateTimer(scriptHolder, diff);
                        needCleanup = false;
                    }
                }
            }

            if (needCleanup)
                _timedActionList.Clear();

            if (!_remIDs.Empty())
            {
                foreach (var id in _remIDs)
                    RemoveStoredEvent(id);

                _remIDs.Clear();
            }
            if (_useTextTimer && _me != null)
            {
                if (_textTimer < diff)
                {
                    uint textID = _lastTextID;
                    _lastTextID = 0;
                    uint entry = _talkerEntry;
                    _talkerEntry = 0;
                    _textTimer = 0;
                    _useTextTimer = false;
                    ProcessEventsFor(SmartEvents.TextOver, null, textID, entry);
                }
                else _textTimer -= diff;
            }
        }

        void SortEvents(List<SmartScriptHolder> events)
        {
            events.Sort();
        }

        void RaisePriority(SmartScriptHolder e)
        {
            e.Timer = 1;
            // Change priority only if it's set to default, otherwise keep the current order of events
            if (e.Priority == SmartScriptHolder.DefaultPriority)
            {
                e.Priority = _currentPriority++;
                _eventSortingRequired = true;
            }
        }

        void RetryLater(SmartScriptHolder e, bool ignoreChanceRoll = false)
        {
            RaisePriority(e);

            // This allows to retry the action later without rolling again the chance roll (which might fail and end up not executing the action)
            if (ignoreChanceRoll)
                e.Event.event_flags |= SmartEventFlags.TempIgnoreChanceRoll;

            e.RunOnce = false;
        }

        void FillScript(List<SmartScriptHolder> e, WorldObject obj, AreaTriggerRecord at, SceneTemplate scene, Quest quest, uint eventId = 0)
        {
            if (e.Empty())
            {
                if (obj != null)
                    Log.outDebug(LogFilter.ScriptsAi, $"SmartScript: EventMap for Entry {obj.GetEntry()} is empty but is using SmartScript.");
                if (at != null)
                    Log.outDebug(LogFilter.ScriptsAi, $"SmartScript: EventMap for AreaTrigger {at.Id} is empty but is using SmartScript.");
                if (scene != null)
                    Log.outDebug(LogFilter.ScriptsAi, $"SmartScript: EventMap for SceneId {scene.SceneId} is empty but is using SmartScript.");
                if (quest != null)
                    Log.outDebug(LogFilter.ScriptsAi, $"SmartScript: EventMap for Quest {quest.Id} is empty but is using SmartScript.");
                if (eventId != 0)
                    Log.outDebug(LogFilter.ScriptsAi, $"SmartScript: EventMap for Event {eventId} is empty but is using SmartScript.");
                return;
            }

            foreach (var scriptholder in e)
            {
                if (obj != null && !scriptholder.Difficulties.Empty())
                {
                    bool foundValidDifficulty = false;
                    foreach (Difficulty difficulty in scriptholder.Difficulties)
                    {
                        if (difficulty == obj.GetMap().GetDifficultyID())
                        {
                            foundValidDifficulty = true;
                            break;
                        }
                    }

                    if (!foundValidDifficulty)
                        continue;
                }

                _allEventFlags |= scriptholder.Event.event_flags;
                _events.Add(scriptholder);
            }
        }

        void GetScript()
        {
            List<SmartScriptHolder> e;
            // We must use script type to avoid ambiguities
            switch (_scriptType)
            {
                case SmartScriptType.Creature:
                    e = Global.SmartAIMgr.GetScript(-(int)_me.GetSpawnId(), _scriptType);
                    if (e.Empty())
                        e = Global.SmartAIMgr.GetScript((int)_me.GetEntry(), _scriptType);
                    FillScript(e, _me, null, null, null, 0);
                    break;
                case SmartScriptType.GameObject:
                    e = Global.SmartAIMgr.GetScript(-(int)_go.GetSpawnId(), _scriptType);
                    if (e.Empty())
                        e = Global.SmartAIMgr.GetScript((int)_go.GetEntry(), _scriptType);
                    FillScript(e, _go, null, null, null, 0);
                    break;
                case SmartScriptType.AreaTriggerEntity:
                case SmartScriptType.AreaTriggerEntityCustom:
                    e = Global.SmartAIMgr.GetScript((int)_areaTrigger.GetEntry(), _scriptType);
                    FillScript(e, _areaTrigger, null, null, null, 0);
                    break;
                case SmartScriptType.AreaTrigger:
                    e = Global.SmartAIMgr.GetScript((int)_trigger.Id, _scriptType);
                    FillScript(e, null, _trigger, null, null, 0);
                    break;
                case SmartScriptType.Scene:
                    e = Global.SmartAIMgr.GetScript((int)_sceneTemplate.SceneId, _scriptType);
                    FillScript(e, null, null, _sceneTemplate, null, 0);
                    break;
                case SmartScriptType.Quest:
                    e = Global.SmartAIMgr.GetScript((int)_quest.Id, _scriptType);
                    FillScript(e, null, null, null, _quest, 0);
                    break;
                case SmartScriptType.Event:
                    e = Global.SmartAIMgr.GetScript((int)_eventId, _scriptType);
                    FillScript(e, null, null, null, null, _eventId);
                    break;
                default:
                    break;
            }
        }

        public void OnInitialize(WorldObject obj, AreaTriggerRecord at = null, SceneTemplate scene = null, Quest qst = null, uint eventId = 0)
        {
            if (at != null)
            {
                _scriptType = SmartScriptType.AreaTrigger;
                _trigger = at;
                _player = obj.ToPlayer();

                if (_player == null)
                {
                    Log.outError(LogFilter.Misc, $"SmartScript::OnInitialize: source is AreaTrigger with id {_trigger.Id}, missing trigger player");
                    return;
                }

                Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is AreaTrigger with id {_trigger.Id}, triggered by player {_player.GetGUID()}");
            }
            else if (scene != null)
            {
                _scriptType = SmartScriptType.Scene;
                _sceneTemplate = scene;
                _player = obj.ToPlayer();

                if (_player == null)
                {
                    Log.outError(LogFilter.Misc, $"SmartScript::OnInitialize: source is Scene with id {_sceneTemplate.SceneId}, missing trigger player");
                    return;
                }

                Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is Scene with id {_sceneTemplate.SceneId}, triggered by player {_player.GetGUID()}");
            }
            else if (qst != null)
            {
                _scriptType = SmartScriptType.Quest;
                _quest = qst;
                _player = obj.ToPlayer();

                if (_player == null)
                {
                    Log.outError(LogFilter.Misc, $"SmartScript::OnInitialize: source is Quest with id {qst.Id}, missing trigger player");
                    return;
                }

                Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is Quest with id {qst.Id}, triggered by player {_player.GetGUID()}");
            }
            else if (eventId != 0)
            {
                _scriptType = SmartScriptType.Event;
                _eventId = eventId;

                if (obj.IsPlayer())
                {
                    _player = obj.ToPlayer();
                    Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is Event {_eventId}, triggered by player {_player.GetGUID()}");
                }
                else if (obj.IsCreature())
                {
                    _me = obj.ToCreature();
                    Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is Event {_eventId}, triggered by creature {_me.GetEntry()}");
                }
                else if (obj.IsGameObject())
                {
                    _go = obj.ToGameObject();
                    Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is Event {_eventId}, triggered by gameobject {_go.GetEntry()}");
                }
                else
                {
                    Log.outError(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is Event {_eventId}, missing trigger WorldObject");
                    return;
                }
            }
            else if (obj != null) // Handle object based scripts
            {
                switch (obj.GetTypeId())
                {
                    case TypeId.Unit:
                        _scriptType = SmartScriptType.Creature;
                        _me = obj.ToCreature();
                        Log.outDebug(LogFilter.Scripts, $"SmartScript.OnInitialize: source is Creature {_me.GetEntry()}");
                        break;
                    case TypeId.GameObject:
                        _scriptType = SmartScriptType.GameObject;
                        _go = obj.ToGameObject();
                        Log.outDebug(LogFilter.Scripts, $"SmartScript.OnInitialize: source is GameObject {_go.GetEntry()}");
                        break;
                    case TypeId.AreaTrigger:
                        _areaTrigger = obj.ToAreaTrigger();
                        _scriptType = _areaTrigger.IsCustom() ? SmartScriptType.AreaTriggerEntityCustom : SmartScriptType.AreaTriggerEntity;
                        Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.OnInitialize: source is AreaTrigger {_areaTrigger.GetEntry()}, IsCustom {_areaTrigger.IsCustom()}");
                        break;
                    default:
                        Log.outError(LogFilter.Scripts, "SmartScript.OnInitialize: Unhandled TypeID !WARNING!");
                        return;
                }
            }
            else
            {
                Log.outError(LogFilter.ScriptsAi, "SmartScript.OnInitialize: !WARNING! Initialized WorldObject is Null.");
                return;
            }

            GetScript();//load copy of script

            foreach (var holder in _events)
                InitTimer(holder);//calculate timers for first time use

            ProcessEventsFor(SmartEvents.AiInit);
            InstallEvents();
            ProcessEventsFor(SmartEvents.JustCreated);
            _counterList.Clear();
        }

        public void OnMoveInLineOfSight(Unit who)
        {
            if (_me == null)
                return;

            ProcessEventsFor(_me.IsEngaged() ? SmartEvents.IcLos : SmartEvents.OocLos, who);
        }

        Unit DoSelectLowestHpFriendly(float range, uint MinHPDiff)
        {
            if (_me == null)
                return null;

            var u_check = new MostHPMissingInRange<Unit>(_me, range, MinHPDiff);
            var searcher = new UnitLastSearcher(_me, u_check);
            Cell.VisitGridObjects(_me, searcher, range);
            return searcher.GetTarget();
        }

        public Unit DoSelectBelowHpPctFriendlyWithEntry(uint entry, float range, byte minHPDiff = 1, bool excludeSelf = true)
        {
            FriendlyBelowHpPctEntryInRange u_check = new(_me, entry, range, minHPDiff, excludeSelf);
            UnitLastSearcher searcher = new(_me, u_check);
            Cell.VisitAllObjects(_me, searcher, range);

            return searcher.GetTarget();
        }

        Unit DoSelectLowestHpPercentFriendly(float range, uint minHpPct, uint maxHpPct)
        {
            if (_me == null)
                return null;

            MostHPPercentMissingInRange u_check = new(_me, range, minHpPct, maxHpPct);
            UnitLastSearcher searcher = new(_me, u_check);
            Cell.VisitGridObjects(_me, searcher, range);
            return searcher.GetTarget();
        }

        void DoFindFriendlyCC(List<Creature> creatures, float range)
        {
            if (_me == null)
                return;

            var u_check = new FriendlyCCedInRange(_me, range);
            var searcher = new CreatureListSearcher(_me, creatures, u_check);
            Cell.VisitGridObjects(_me, searcher, range);
        }

        void DoFindFriendlyMissingBuff(List<Creature> creatures, float range, uint spellid)
        {
            if (_me == null)
                return;

            var u_check = new FriendlyMissingBuffInRange(_me, range, spellid);
            var searcher = new CreatureListSearcher(_me, creatures, u_check);
            Cell.VisitGridObjects(_me, searcher, range);
        }

        Unit DoFindClosestFriendlyInRange(float range)
        {
            if (_me == null)
                return null;

            var u_check = new AnyFriendlyUnitInObjectRangeCheck(_me, _me, range);
            var searcher = new UnitLastSearcher(_me, u_check);
            Cell.VisitAllObjects(_me, searcher, range);
            return searcher.GetTarget();
        }

        public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker, uint startFromEventId = 0)
        {
            // Do NOT allow to start a new actionlist if a previous one is already running, unless explicitly allowed. We need to always finish the current actionlist
            if (e.GetActionType() == SmartActions.CallTimedActionlist && e.Action.timedActionList.allowOverride == 0 && !_timedActionList.Empty())
                return;

            _timedActionList.Clear();
            _timedActionList = Global.SmartAIMgr.GetScript((int)entry, SmartScriptType.TimedActionlist);
            if (_timedActionList.Empty())
                return;

            _timedActionList.RemoveAll(script => { return script.EventId < startFromEventId; });

            mTimedActionListInvoker = invoker != null ? invoker.GetGUID() : ObjectGuid.Empty;
            for (var i = 0; i < _timedActionList.Count; ++i)
            {
                var scriptHolder = _timedActionList[i];
                scriptHolder.EnableTimed = i == 0;//enable processing only for the first action

                if (e.Action.timedActionList.timerType == 0)
                    scriptHolder.Event.type = SmartEvents.UpdateOoc;
                else if (e.Action.timedActionList.timerType == 1)
                    scriptHolder.Event.type = SmartEvents.UpdateIc;
                else if (e.Action.timedActionList.timerType > 1)
                    scriptHolder.Event.type = SmartEvents.Update;

                InitTimer(scriptHolder);
            }
        }

        Unit GetLastInvoker(Unit invoker = null)
        {
            // Look for invoker only on map of base object... Prevents multithreaded crashes
            WorldObject baseObject = GetBaseObject();
            if (baseObject != null)
                return Global.ObjAccessor.GetUnit(baseObject, LastInvoker);
            // used for area triggers invoker cast
            else if (invoker != null)
                return Global.ObjAccessor.GetUnit(invoker, LastInvoker);

            return null;
        }

        public void SetPathId(uint id) { _pathId = id; }

        public uint GetPathId() { return _pathId; }

        WorldObject GetBaseObject()
        {
            WorldObject obj = null;
            if (_me != null)
                obj = _me;
            else if (_go != null)
                obj = _go;
            else if (_areaTrigger != null)
                obj = _areaTrigger;
            else if (_player != null)
                obj = _player;

            return obj;
        }

        WorldObject GetBaseObjectOrUnitInvoker(Unit invoker)
        {
            return GetBaseObject() ?? invoker;
        }

        public bool HasAnyEventWithFlag(SmartEventFlags flag) { return _allEventFlags.HasAnyFlag(flag); }

        public bool IsUnit(WorldObject obj) { return obj != null && (obj.IsTypeId(TypeId.Unit) || obj.IsTypeId(TypeId.Player)); }

        public bool IsPlayer(WorldObject obj) { return obj != null && obj.IsTypeId(TypeId.Player); }

        public bool IsCreature(WorldObject obj) { return obj != null && obj.IsTypeId(TypeId.Unit); }

        public bool IsCharmedCreature(WorldObject obj)
        {
            if (obj == null)
                return false;

            Creature creatureObj = obj.ToCreature();
            if (creatureObj != null)
                return creatureObj.IsCharmed();

            return false;
        }

        public bool IsGameObject(WorldObject obj) { return obj != null && obj.IsTypeId(TypeId.GameObject); }

        bool IsSmart(Creature creature, bool silent = false)
        {
            if (creature == null)
                return false;

            bool smart = true;
            if (creature.GetAI() is not SmartAI || creature.GetAI<SmartAI>() == null)
                smart = false;

            if (!smart && !silent)
                Log.outError(LogFilter.Sql, "SmartScript: Action target Creature (GUID: {0} Entry: {1}) is not using SmartAI, action skipped to prevent crash.", creature != null ? creature.GetSpawnId() : (_me != null ? _me.GetSpawnId() : 0), creature != null ? creature.GetEntry() : (_me != null ? _me.GetEntry() : 0));

            return smart;
        }

        bool IsSmart(GameObject gameObject, bool silent = false)
        {
            if (gameObject == null)
                return false;

            bool smart = true;
            if (gameObject.GetAI<SmartGameObjectAI>() == null)
                smart = false;

            if (!smart && !silent)
                Log.outError(LogFilter.Sql, "SmartScript: Action target GameObject (GUID: {0} Entry: {1}) is not using SmartGameObjectAI, action skipped to prevent crash.", gameObject != null ? gameObject.GetSpawnId() : (_go != null ? _go.GetSpawnId() : 0), gameObject != null ? gameObject.GetEntry() : (_go != null ? _go.GetEntry() : 0));

            return smart;
        }

        bool IsSmart(bool silent = false)
        {
            if (_me != null)
                return IsSmart(_me, silent);

            if (_go != null)
                return IsSmart(_go, silent);

            return false;
        }

        void StoreTargetList(List<WorldObject> targets, uint id)
        {
            // insert or replace
            _storedTargets.Remove(id);
            _storedTargets.Add(id, new ObjectGuidList(targets));
        }

        void AddToStoredTargetList(List<WorldObject> targets, uint id)
        {
            var inserted = _storedTargets.TryAdd(id, new ObjectGuidList(targets));
            if (!inserted)
                foreach (WorldObject obj in targets)
                    _storedTargets[id].AddGuid(obj.GetGUID());
        }

        public List<WorldObject> GetStoredTargetList(uint id, WorldObject obj)
        {
            var list = _storedTargets.LookupByKey(id);
            if (list != null)
                return list.GetObjectList(obj);

            return null;
        }

        void StoreCounter(uint id, uint value, uint reset)
        {
            if (_counterList.ContainsKey(id))
            {
                if (reset == 0)
                    _counterList[id] += value;
                else
                    _counterList[id] = value;
            }
            else
                _counterList.Add(id, value);

            ProcessEventsFor(SmartEvents.CounterSet, null, id);
        }

        uint GetCounterValue(uint id)
        {
            if (_counterList.ContainsKey(id))
                return _counterList[id];
            return 0;
        }

        GameObject FindGameObjectNear(WorldObject searchObject, ulong guid)
        {
            var bounds = searchObject.GetMap().GetGameObjectBySpawnIdStore().LookupByKey(guid);
            if (bounds.Empty())
                return null;

            return bounds[0];
        }

        Creature FindCreatureNear(WorldObject searchObject, ulong guid)
        {
            var bounds = searchObject.GetMap().GetCreatureBySpawnIdStore().LookupByKey(guid);
            if (bounds.Empty())
                return null;

            var foundCreature = bounds.Find(creature => creature.IsAlive());

            return foundCreature ?? bounds[0];
        }

        void ResetBaseObject()
        {
            WorldObject lookupRoot = _me;
            if (lookupRoot == null)
                lookupRoot = _go;

            if (lookupRoot != null)
            {
                if (!_meOrigGUID.IsEmpty())
                {
                    Creature m = ObjectAccessor.GetCreature(lookupRoot, _meOrigGUID);
                    if (m != null)
                    {
                        _me = m;
                        _go = null;
                        _areaTrigger = null;
                        _player = null;
                    }
                }

                if (!_goOrigGUID.IsEmpty())
                {
                    GameObject o = ObjectAccessor.GetGameObject(lookupRoot, _goOrigGUID);
                    if (o != null)
                    {
                        _me = null;
                        _go = o;
                        _areaTrigger = null;
                        _player = null;
                    }
                }
            }
            _goOrigGUID.Clear();
            _meOrigGUID.Clear();
        }

        void IncPhase(uint p)
        {
            // protect phase from overflowing
            SetPhase(Math.Min((uint)SmartPhase.Phase12, _eventPhase + p));
        }

        void DecPhase(uint p)
        {
            if (p >= _eventPhase)
                SetPhase(0);
            else
                SetPhase(_eventPhase - p);
        }
        void SetPhase(uint p)
        {
            _eventPhase = p;
        }
        bool IsInPhase(uint p)
        {
            if (_eventPhase == 0)
                return false;

            return ((1 << (int)(_eventPhase - 1)) & p) != 0;
        }

        void RemoveStoredEvent(uint id)
        {
            if (!_storedEvents.Empty())
            {
                foreach (var holder in _storedEvents)
                {
                    if (holder.EventId == id)
                    {
                        _storedEvents.Remove(holder);
                        return;
                    }
                }
            }
        }

        //template<typename Result, typename ConcreteActionImpl = Scripting::v2::ActionResult<Result>, typename...Args>
        public static ActionResult<Result> CreateTimedActionListWaitEventFor<Result, ConcreteActionImpl>(SmartScriptHolder e, object[] args = null) where ConcreteActionImpl : ActionBase
        {
            if (e.GetScriptType() != SmartScriptType.TimedActionlist)
                return null;

            if (!e.Event.event_flags.HasFlag(SmartEventFlags.ActionlistWaits))
                return null;

            return (ActionResult<Result>)Activator.CreateInstance(typeof(ActionResult<Result>), args);
        }

        public static ConcreteActionImpl CreateTimedActionListWaitEventFor<ConcreteActionImpl>(SmartScriptHolder e, object[] args = null) where ConcreteActionImpl : ActionBase
        {
            if (e.GetScriptType() != SmartScriptType.TimedActionlist)
                return null;

            if (!e.Event.event_flags.HasFlag(SmartEventFlags.ActionlistWaits))
                return null;

            return (ConcreteActionImpl)Activator.CreateInstance(typeof(ConcreteActionImpl), args);
        }
    }

    class ObjectGuidList
    {
        List<ObjectGuid> _guidList = new();
        List<WorldObject> _objectList = new();

        public ObjectGuidList(List<WorldObject> objectList)
        {
            _objectList = objectList;
            foreach (WorldObject obj in _objectList)
                _guidList.Add(obj.GetGUID());
        }

        public List<WorldObject> GetObjectList(WorldObject obj)
        {
            UpdateObjects(obj);
            return _objectList;
        }

        public void AddGuid(ObjectGuid guid) { _guidList.Add(guid); }

        //sanitize vector using _guidVector
        void UpdateObjects(WorldObject obj)
        {
            _objectList.Clear();

            foreach (ObjectGuid guid in _guidList)
            {
                WorldObject newObj = Global.ObjAccessor.GetWorldObject(obj, guid);
                if (newObj != null)
                    _objectList.Add(newObj);
            }
        }
    }
}
