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
using Framework.GameMath;
using Game.Chat;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Misc;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    public class SmartScript
    {
        public SmartScript()
        {
            go = null;
            me = null;
            trigger = null;
            mEventPhase = 0;
            mPathId = 0;
            mTextTimer = 0;
            mLastTextID = 0;
            mTextGUID = ObjectGuid.Empty;
            mUseTextTimer = false;
            mTalkerEntry = 0;
            mTemplate = SmartAITemplate.Basic;
            meOrigGUID = ObjectGuid.Empty;
            goOrigGUID = ObjectGuid.Empty;
            mLastInvoker = ObjectGuid.Empty;
            mScriptType = SmartScriptType.Creature;
        }

        public void OnReset()
        {
            ResetBaseObject();
            foreach (var holder in mEvents)
            {
                if (!holder.Event.event_flags.HasAnyFlag(SmartEventFlags.DontReset))
                {
                    InitTimer(holder);
                    holder.runOnce = false;
                }
            }
            ProcessEventsFor(SmartEvents.Reset);
            mLastInvoker = ObjectGuid.Empty;
        }

        public void ProcessEventsFor(SmartEvents e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
        {
            foreach (var Event in mEvents)
            {
                var eventType = Event.GetEventType();
                if (eventType == SmartEvents.Link)//special handling
                    continue;

                if (eventType == e)
                    if (Global.ConditionMgr.IsObjectMeetingSmartEventConditions(Event.entryOrGuid, Event.event_id, Event.source_type, unit, GetBaseObject()))
                        ProcessEvent(Event, unit, var0, var1, bvar, spell, gob, varString);
            }
        }

        void ProcessAction(SmartScriptHolder e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
        {
            //calc random
            if (e.GetEventType() != SmartEvents.Link && e.Event.event_chance < 100 && e.Event.event_chance != 0)
            {
                if (RandomHelper.randChance(e.Event.event_chance))
                    return;
            }
            e.runOnce = true;//used for repeat check

            if (unit != null)
                mLastInvoker = unit.GetGUID();

            var tempInvoker = GetLastInvoker();
            if (tempInvoker != null)
                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: Invoker: {0} (guidlow: {1})", tempInvoker.GetName(), tempInvoker.GetGUID().ToString());

            var targets = GetTargets(e, unit);

            switch (e.GetActionType())
            {
                case SmartActions.Talk:
                    {
                        var talker = e.Target.type == 0 ? me : null;
                        Unit talkTarget = null;

                        foreach (var target in targets)
                        {
                            if (IsCreature(target) && !target.ToCreature().IsPet()) // Prevented sending text to pets.
                            {
                                if (e.Action.talk.useTalkTarget != 0)
                                {
                                    talker = me;
                                    talkTarget = target.ToCreature();
                                }
                                else
                                    talker = target.ToCreature();
                                break;
                            }
                            else if (IsPlayer(target))
                            {
                                talker = me;
                                talkTarget = target.ToPlayer();
                                break;
                            }
                        }

                        if (talkTarget == null)
                            talkTarget = GetLastInvoker();

                        if (talker == null)
                            break;

                        mTalkerEntry = talker.GetEntry();
                        mLastTextID = e.Action.talk.textGroupId;
                        mTextTimer = e.Action.talk.duration;

                        mUseTextTimer = true;
                        Global.CreatureTextMgr.SendChat(talker, (byte)e.Action.talk.textGroupId, talkTarget);
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_TALK: talker: {0} (Guid: {1}), textGuid: {2}",
                            talker.GetName(), talker.GetGUID().ToString(), mTextGUID.ToString());
                        break;
                    }
                case SmartActions.SimpleTalk:
                    {
                        foreach (var target in targets)
                        {
                            if (IsCreature(target))
                                Global.CreatureTextMgr.SendChat(target.ToCreature(), (byte)e.Action.talk.textGroupId, IsPlayer(GetLastInvoker()) ? GetLastInvoker() : null);
                            else if (IsPlayer(target) && me != null)
                            {
                                var templastInvoker = GetLastInvoker();
                                Global.CreatureTextMgr.SendChat(me, (byte)e.Action.talk.textGroupId, IsPlayer(templastInvoker) ? templastInvoker : null, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, Team.Other, false, target.ToPlayer());
                            }
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SIMPLE_TALK: talker: {0} (GuidLow: {1}), textGroupId: {2}",
                                target.GetName(), target.GetGUID().ToString(), e.Action.talk.textGroupId);
                        }
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
                                if (e.Action.faction.factionID != 0)
                                {
                                    target.ToCreature().SetFaction(e.Action.faction.factionID);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_FACTION: Creature entry {0}, GuidLow {1} set faction to {2}",
                                        target.GetEntry(), target.GetGUID().ToString(), e.Action.faction.factionID);
                                }
                                else
                                {
                                    var ci = Global.ObjectMgr.GetCreatureTemplate(target.ToCreature().GetEntry());
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
                                    var ci = Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature);
                                    if (ci != null)
                                    {
                                        var model = ObjectManager.ChooseDisplayId(ci);
                                        target.ToCreature().SetDisplayId(model.CreatureDisplayID, model.DisplayScale);
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
                            var player = target.ToPlayer();
                            if (player)
                            {
                                var quest = Global.ObjectMgr.GetQuestTemplate(e.Action.questOffer.questId);
                                if (quest != null)
                                {
                                    if (me && e.Action.questOffer.directAdd == 0)
                                    {
                                        if (player.CanTakeQuest(quest, true))
                                        {
                                            var session = player.GetSession();
                                            if (session)
                                            {
                                                var menu = new PlayerMenu(session);
                                                menu.SendQuestGiverQuestDetails(quest, me.GetGUID(), true, false);
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
                        var emotes = new List<uint>();
                        var randomEmote = e.Action.randomEmote;
                        foreach (var id in new[] { randomEmote.emote1, randomEmote.emote2, randomEmote.emote3, randomEmote.emote4, randomEmote.emote5, randomEmote.emote6, })
                            if (id != 0)
                                emotes.Add(id);

                        foreach (var target in targets)
                        {
                            if (IsUnit(target))
                            {
                                var emote = emotes.SelectRandom();
                                target.ToUnit().HandleEmoteCommand((Emote)emote);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_RANDOM_EMOTE: Creature guidLow {0} handle random emote {1}",
                                    target.GetGUID().ToString(), emote);
                            }
                        }
                        break;
                    }
                case SmartActions.ThreatAllPct:
                    {
                        if (me == null)
                            break;

                        foreach (var refe in me.GetThreatManager().GetThreatList())
                        {
                            refe.AddThreatPercent(Math.Max(-100, (int)(e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC)));
                            Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_THREAT_ALL_PCT: Creature {me.GetGUID()} modify threat for {refe.GetTarget().GetGUID()}, value {e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC}");
                        }
                        break;
                    }
                case SmartActions.ThreatSinglePct:
                    {
                        if (me == null)
                            break;

                        foreach (var target in targets)
                        {
                            if (IsUnit(target))
                            {
                                me.GetThreatManager().ModifyThreatByPercent(target.ToUnit(), Math.Max(-100, (int)(e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC)));
                                Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_THREAT_SINGLE_PCT: Creature {me.GetGUID()} modify threat for {target.GetGUID()}, value {e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC}");
                            }
                        }
                        break;
                    }
                case SmartActions.CallAreaexploredoreventhappens:
                    {
                        foreach (var target in targets)
                        {
                            // Special handling for vehicles
                            if (IsUnit(target))
                            {
                                var vehicle = target.ToUnit().GetVehicleKit();
                                if (vehicle != null)
                                {
                                    foreach (var seat in vehicle.Seats)
                                    {
                                        var player = Global.ObjAccessor.GetPlayer(target, seat.Value.Passenger.Guid);
                                        if (player != null)
                                            player.AreaExploredOrEventHappens(e.Action.quest.questId);
                                    }
                                }
                            }

                            if (IsPlayer(target))
                            {
                                target.ToPlayer().AreaExploredOrEventHappens(e.Action.quest.questId);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_CALL_AREAEXPLOREDOREVENTHAPPENS: {0} credited quest {1}",
                                    target.GetGUID().ToString(), e.Action.quest.questId);
                            }
                        }
                        break;
                    }
                case SmartActions.Cast:
                    {
                        if (e.Action.cast.targetsLimit > 0 && targets.Count > e.Action.cast.targetsLimit)
                            targets.RandomResize(e.Action.cast.targetsLimit);

                        foreach (var target in targets)
                        {
                            if (go != null)
                                go.CastSpell(target.ToUnit(), e.Action.cast.spell);

                            if (!IsUnit(target))
                                continue;

                            if (!e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !target.ToUnit().HasAura(e.Action.cast.spell))
                            {
                                var triggerFlag = TriggerCastFlags.None;
                                if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                                {
                                    if (e.Action.cast.triggerFlags != 0)
                                        triggerFlag = (TriggerCastFlags)e.Action.cast.triggerFlags;
                                    else
                                        triggerFlag = TriggerCastFlags.FullMask;
                                }

                                if (me)
                                {
                                    if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                                        me.InterruptNonMeleeSpells(false);

                                    if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.CombatMove))
                                    {
                                        // If cast flag SMARTCAST_COMBAT_MOVE is set combat movement will not be allowed
                                        // unless target is outside spell range, out of mana, or LOS.

                                        var allowMove = false;
                                        var spellInfo = Global.SpellMgr.GetSpellInfo(e.Action.cast.spell, me.GetMap().GetDifficultyID());
                                        var costs = spellInfo.CalcPowerCost(me, spellInfo.GetSchoolMask());
                                        var hasPower = true;
                                        foreach (var cost in costs)
                                        {
                                            if (cost.Power == PowerType.Health)
                                            {
                                                if (me.GetHealth() <= (uint)cost.Amount)
                                                {
                                                    hasPower = false;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                if (me.GetPower(cost.Power) < cost.Amount)
                                                {
                                                    hasPower = false;
                                                    break;
                                                }
                                            }
                                        }

                                        if (me.GetDistance(target) > spellInfo.GetMaxRange(true) ||
                                            me.GetDistance(target) < spellInfo.GetMinRange(true) ||
                                            !me.IsWithinLOSInMap(target) || !hasPower)
                                            allowMove = true;

                                        ((SmartAI)me.GetAI()).SetCombatMove(allowMove);
                                    }

                                    me.CastSpell(target.ToUnit(), e.Action.cast.spell, triggerFlag);
                                }
                                else if (go)
                                    go.CastSpell(target.ToUnit(), e.Action.cast.spell, triggerFlag);
                                else if (target != null)
                                    target.ToUnit().CastSpell(target.ToUnit(), e.Action.cast.spell);
                            }
                            else
                                Log.outDebug(LogFilter.ScriptsAi, "Spell {0} not casted because it has flag SMARTCAST_AURA_NOT_PRESENT and the target (Guid: {1} Entry: {2} Type: {3}) already has the aura",
                                    e.Action.cast.spell, target.GetGUID(), target.GetEntry(), target.GetTypeId());
                        }
                        break;
                    }
                case SmartActions.InvokerCast:
                    {
                        var tempLastInvoker = GetLastInvoker(unit);
                        if (tempLastInvoker == null)
                            break;

                        if (targets.Empty())
                            break;

                        if (e.Action.cast.targetsLimit > 0 && targets.Count > e.Action.cast.targetsLimit)
                            targets.RandomResize(e.Action.cast.targetsLimit);

                        foreach (var target in targets)
                        {
                            if (!IsUnit(target))
                                continue;

                            if (!e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !target.ToUnit().HasAura(e.Action.cast.spell))
                            {

                                if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                                    tempLastInvoker.InterruptNonMeleeSpells(false);

                                var triggerFlag = TriggerCastFlags.None;
                                if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                                {
                                    if (e.Action.cast.triggerFlags != 0)
                                        triggerFlag = (TriggerCastFlags)e.Action.cast.triggerFlags;
                                    else
                                        triggerFlag = TriggerCastFlags.FullMask;
                                }

                                tempLastInvoker.CastSpell(target.ToUnit(), e.Action.cast.spell, triggerFlag);

                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_INVOKER_CAST: Invoker {0} casts spell {1} on target {2} with castflags {3}",
                                    tempLastInvoker.GetGUID().ToString(), e.Action.cast.spell, target.GetGUID().ToString(), e.Action.cast.castFlags);
                            }
                            else
                                Log.outDebug(LogFilter.ScriptsAi, "Spell {0} not cast because it has flag SMARTCAST_AURA_NOT_PRESENT and the target ({1}) already has the aura", e.Action.cast.spell, target.GetGUID().ToString());
                        }
                        break;
                    }
                case SmartActions.AddAura:
                    {
                        foreach (var target in targets)
                        {
                            if (IsUnit(target))
                            {
                                target.ToUnit().AddAura(e.Action.cast.spell, target.ToUnit());
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_ADD_AURA: Adding aura {0} to unit {1}",
                                    e.Action.cast.spell, target.GetGUID().ToString());
                            }
                        }
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
                case SmartActions.SetUnitFlag:
                    {
                        foreach (var target in targets)
                        {
                            if (IsUnit(target))
                            {
                                if (e.Action.unitFlag.type == 0)
                                {
                                    target.ToUnit().AddUnitFlag((UnitFlags)e.Action.unitFlag.flag);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_UNIT_FLAG. Unit {0} added flag {1} to UNIT_FIELD_FLAGS",
                                    target.GetGUID().ToString(), e.Action.unitFlag.flag);
                                }
                                else
                                {
                                    target.ToUnit().AddUnitFlag2((UnitFlags2)e.Action.unitFlag.flag);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_UNIT_FLAG. Unit {0} added flag {1} to UNIT_FIELD_FLAGS_2",
                                    target.GetGUID().ToString(), e.Action.unitFlag.flag);
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.RemoveUnitFlag:
                    {
                        foreach (var target in targets)
                        {
                            if (IsUnit(target))
                            {
                                if (e.Action.unitFlag.type == 0)
                                {
                                    target.ToUnit().RemoveUnitFlag((UnitFlags)e.Action.unitFlag.flag);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_REMOVE_UNIT_FLAG. Unit {0} removed flag {1} to UNIT_FIELD_FLAGS",
                                    target.GetGUID().ToString(), e.Action.unitFlag.flag);
                                }
                                else
                                {
                                    target.ToUnit().RemoveUnitFlag2((UnitFlags2)e.Action.unitFlag.flag);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_REMOVE_UNIT_FLAG. Unit {0} removed flag {1} to UNIT_FIELD_FLAGS_2",
                                    target.GetGUID().ToString(), e.Action.unitFlag.flag);
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.AutoAttack:
                    {
                        if (!IsSmart())
                            break;

                        ((SmartAI)me.GetAI()).SetAutoAttack(e.Action.autoAttack.attack != 0 ? true : false);
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_AUTO_ATTACK: Creature: {0} bool on = {1}",
                            me.GetGUID().ToString(), e.Action.autoAttack.attack);
                        break;
                    }
                case SmartActions.AllowCombatMovement:
                    {
                        if (!IsSmart())
                            break;

                        var move = e.Action.combatMove.move != 0 ? true : false;
                        ((SmartAI)me.GetAI()).SetCombatMove(move);
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_ALLOW_COMBAT_MOVEMENT: Creature {0} bool on = {1}",
                            me.GetGUID().ToString(), e.Action.combatMove.move);
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
                        if (me == null)
                            break;

                        me.GetAI().EnterEvadeMode();
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_EVADE: Creature {0} EnterEvadeMode", me.GetGUID().ToString());
                        break;
                    }
                case SmartActions.FleeForAssist:
                    {
                        if (!me)
                            break;

                        me.DoFleeToGetAssistance();
                        if (e.Action.fleeAssist.withEmote != 0)
                        {
                            var builder = new BroadcastTextBuilder(me, ChatMsg.MonsterEmote, (uint)BroadcastTextIds.FleeForAssist, me.GetGender());
                            Global.CreatureTextMgr.SendChatPacket(me, builder, ChatMsg.Emote);
                        }
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_FLEE_FOR_ASSIST: Creature {0} DoFleeToGetAssistance", me.GetGUID().ToString());
                        break;
                    }
                case SmartActions.CallGroupeventhappens:
                    {
                        if (unit == null)
                            break;

                        // If invoker was pet or charm
                        var playerCharmed = unit.GetCharmerOrOwnerPlayerOrPlayerItself();
                        if (playerCharmed && GetBaseObject() != null)
                        {
                            playerCharmed.GroupEventHappens(e.Action.quest.questId, GetBaseObject());
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_CALL_GROUPEVENTHAPPENS: Player {0}, group credit for quest {1}",
                                unit.GetGUID().ToString(), e.Action.quest.questId);
                        }

                        // Special handling for vehicles
                        var vehicle = unit.GetVehicleKit();
                        if (vehicle != null)
                        {
                            foreach (var seat in vehicle.Seats)
                            {
                                var player1 = Global.ObjAccessor.GetPlayer(unit, seat.Value.Passenger.Guid);
                                if (player1 != null)
                                    player1.GroupEventHappens(e.Action.quest.questId, GetBaseObject());
                            }
                        }
                        break;
                    }
                case SmartActions.CombatStop:
                    {
                        if (!me)
                            break;

                        me.CombatStop(true);
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_COMBAT_STOP: {0} CombatStop", me.GetGUID().ToString());
                        break;
                    }
                case SmartActions.RemoveAurasFromSpell:
                    {
                        foreach (var target in targets)
                        {
                            if (!IsUnit(target))
                                continue;

                            if (e.Action.removeAura.spell == 0)
                                target.ToUnit().RemoveAllAuras();
                            else
                                target.ToUnit().RemoveAurasDueToSpell(e.Action.removeAura.spell);

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
                            ((SmartAI)me.GetAI()).StopFollow(false);
                            break;
                        }

                        foreach (var target in targets)
                        {
                            if (IsUnit(target))
                            {
                                var angle = e.Action.follow.angle > 6 ? (e.Action.follow.angle * (float)Math.PI / 180.0f) : e.Action.follow.angle;
                                ((SmartAI)me.GetAI()).SetFollow(target.ToUnit(), e.Action.follow.dist + 0.1f, angle, e.Action.follow.credit, e.Action.follow.entry, e.Action.follow.creditType);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_FOLLOW: Creature {0} following target {1}",
                                    me.GetGUID().ToString(), target.GetGUID().ToString());
                                break;
                            }
                        }
                        break;
                    }
                case SmartActions.RandomPhase:
                    {
                        if (GetBaseObject() == null)
                            break;

                        var phases = new List<uint>();
                        var randomPhase = e.Action.randomPhase;
                        foreach (var id in new[] { randomPhase.phase1, randomPhase.phase2, randomPhase.phase3, randomPhase.phase4, randomPhase.phase5, randomPhase.phase6 })
                            if (id != 0)
                                phases.Add(id);

                        var phase = phases.SelectRandom();
                        SetPhase(phase);
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_RANDOM_PHASE: Creature {0} sets event phase to {1}",
                            GetBaseObject().GetGUID().ToString(), phase);
                        break;
                    }
                case SmartActions.RandomPhaseRange:
                    {
                        if (GetBaseObject() == null)
                            break;

                        var phase = RandomHelper.URand(e.Action.randomPhaseRange.phaseMin, e.Action.randomPhaseRange.phaseMax);
                        SetPhase(phase);
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_RANDOM_PHASE_RANGE: Creature {0} sets event phase to {1}",
                            GetBaseObject().GetGUID().ToString(), phase);
                        break;
                    }
                case SmartActions.CallKilledmonster:
                    {
                        if (e.Target.type == SmartTargets.None || e.Target.type == SmartTargets.Self) // Loot recipient and his group members
                        {
                            if (me == null)
                                break;

                            var player = me.GetLootRecipient();
                            if (player != null)
                            {
                                player.RewardPlayerAndGroupAtEvent(e.Action.killedMonster.creature, player);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_CALL_KILLEDMONSTER: Player {0}, Killcredit: {1}",
                                    player.GetGUID().ToString(), e.Action.killedMonster.creature);
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
                                    var vehicle = target.ToUnit().GetVehicleKit();
                                    if (vehicle != null)
                                    {
                                        foreach (var seat in vehicle.Seats)
                                        {
                                            var player = Global.ObjAccessor.GetPlayer(target, seat.Value.Passenger.Guid);
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
                        var obj = GetBaseObject();
                        if (obj == null)
                            obj = unit;

                        if (obj == null)
                            break;

                        var instance = obj.GetInstanceScript();
                        if (instance == null)
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: Event {0} attempt to set instance data without instance script. EntryOrGuid {1}", e.GetEventType(), e.entryOrGuid);
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
                        var obj = GetBaseObject();
                        if (obj == null)
                            obj = unit;

                        if (obj == null)
                            break;

                        var instance = obj.GetInstanceScript();
                        if (instance == null)
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: Event {0} attempt to set instance data without instance script. EntryOrGuid {1}", e.GetEventType(), e.entryOrGuid);
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
                                target.ToCreature().UpdateEntry(e.Action.updateTemplate.creature, target.ToCreature().GetCreatureData(), e.Action.updateTemplate.updateLevel != 0);
                        break;
                    }
                case SmartActions.Die:
                    {
                        if (me != null && !me.IsDead())
                        {
                            me.KillSelf();
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_DIE: Creature {0}", me.GetGUID().ToString());
                        }
                        break;
                    }
                case SmartActions.SetInCombatWithZone:
                    {
                        foreach (var target in targets)
                        {
                            if (IsCreature(target))
                            {
                                me.SetInCombatWithZone();
                                Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_SET_IN_COMBAT_WITH_ZONE: Creature: {me.GetGUID()}, Target: {target.GetGUID()}");
                            }
                        }

                        break;
                    }
                case SmartActions.CallForHelp:
                    {
                        foreach (var target in targets)
                        {
                            if (IsCreature(target))
                            {
                                target.ToCreature().CallForHelp(e.Action.callHelp.range);
                                if (e.Action.callHelp.withEmote != 0)
                                {
                                    var builder = new BroadcastTextBuilder(me, ChatMsg.Emote, (uint)BroadcastTextIds.CallForHelp, me.GetGender());
                                    Global.CreatureTextMgr.SendChatPacket(me, builder, ChatMsg.MonsterEmote);
                                }
                                Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_CALL_FOR_HELP: Creature: {me.GetGUID()}, Target: {target.GetGUID()}");
                            }
                        }
                        break;
                    }
                case SmartActions.SetSheath:
                    {
                        if (me != null)
                        {
                            me.SetSheath((SheathState)e.Action.setSheath.sheath);
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_SET_SHEATH: Creature {0}, State: {1}",
                                me.GetGUID().ToString(), e.Action.setSheath.sheath);
                        }
                        break;
                    }
                case SmartActions.ForceDespawn:
                    {
                        // there should be at least a world update tick before despawn, to avoid breaking linked actions
                        var respawnDelay = Math.Max(e.Action.forceDespawn.delay, 1u);

                        foreach (var target in targets)
                        {
                            var creature = target.ToCreature();
                            if (creature != null)
                            {
                                var smartAI = creature.GetAI();
                                if (smartAI != null && smartAI is SmartAI)
                                {
                                    ((SmartAI)smartAI).SetDespawnTime(respawnDelay);
                                    ((SmartAI)smartAI).StartDespawn();
                                }
                                else
                                    creature.DespawnOrUnsummon(respawnDelay);
                            }
                            else
                            {
                                var go = target.ToGameObject();
                                if (go != null)
                                    go.SetRespawnTime((int)respawnDelay);
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
                                    var cInfo = Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature);
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
                                var ai = (SmartAI)me.GetAI();
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
                            if (IsCreature(target))
                                target.ToCreature().GetAI().SetData(e.Action.setData.field, e.Action.setData.data);
                            else if (IsGameObject(target))
                                target.ToGameObject().GetAI().SetData(e.Action.setData.field, e.Action.setData.data);
                        }
                        break;
                    }
                case SmartActions.MoveOffset:
                    {
                        foreach (var target in targets)
                        {
                            if (!IsCreature(target))
                                continue;

                            if (!e.Event.event_flags.HasAnyFlag(SmartEventFlags.WhileCharmed) && IsCharmedCreature(target))
                                continue;

                            var pos = target.GetPosition();

                            // Use forward/backward/left/right cartesian plane movement
                            var o = pos.GetOrientation();
                            var x = (float)(pos.GetPositionX() + (Math.Cos(o - (Math.PI / 2)) * e.Target.x) + (Math.Cos(o) * e.Target.y));
                            var y = (float)(pos.GetPositionY() + (Math.Sin(o - (Math.PI / 2)) * e.Target.x) + (Math.Sin(o) * e.Target.y));
                            var z = pos.GetPositionZ() + e.Target.z;
                            target.ToCreature().GetMotionMaster().MovePoint(EventId.SmartRandomPoint, x, y, z);
                        }
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
                        if (me == null)
                            break;

                        if (targets.Empty())
                            break;

                        var target = targets.SelectRandom().ToUnit();
                        if (target != null)
                            me.GetAI().AttackStart(target);

                        break;
                    }
                case SmartActions.SummonCreature:
                    {
                        var flags = (SmartActionSummonCreatureFlags)e.Action.summonCreature.flags;
                        var preferUnit = flags.HasAnyFlag(SmartActionSummonCreatureFlags.PreferUnit);
                        var summoner = preferUnit ? unit : GetBaseObjectOrUnit(unit);
                        if (summoner == null)
                            break;

                        var personalSpawn = flags.HasAnyFlag(SmartActionSummonCreatureFlags.PersonalSpawn);

                        float x, y, z, o;
                        foreach (var target in targets)
                        {
                            target.GetPosition(out x, out y, out z, out o);
                            x += e.Target.x;
                            y += e.Target.y;
                            z += e.Target.z;
                            o += e.Target.o;
                            Creature summon = summoner.SummonCreature(e.Action.summonCreature.creature, x, y, z, o, (TempSummonType)e.Action.summonCreature.type, e.Action.summonCreature.duration, personalSpawn);
                            if (summon != null)
                                if (e.Action.summonCreature.attackInvoker != 0)
                                    summon.GetAI().AttackStart(target.ToUnit());
                        }

                        if (e.GetTargetType() != SmartTargets.Position)
                            break;

                        Creature summon1 = summoner.SummonCreature(e.Action.summonCreature.creature, e.Target.x, e.Target.y, e.Target.z, e.Target.o, (TempSummonType)e.Action.summonCreature.type, e.Action.summonCreature.duration, personalSpawn);
                        if (summon1 != null)
                            if (unit != null && e.Action.summonCreature.attackInvoker != 0)
                                summon1.GetAI().AttackStart(unit);
                        break;
                    }
                case SmartActions.SummonGo:
                    {
                        var summoner = GetBaseObjectOrUnit(unit);
                        if (!summoner)
                            break;

                        foreach (var target in targets)
                        {
                            var pos = target.GetPositionWithOffset(new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o));
                            summoner.SummonGameObject(e.Action.summonGO.entry, pos, Quaternion.fromEulerAnglesZYX(pos.GetOrientation(), 0.0f, 0.0f), e.Action.summonGO.despawnTime);
                        }

                        if (e.GetTargetType() != SmartTargets.Position)
                            break;

                        summoner.SummonGameObject(e.Action.summonGO.entry, new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o), Quaternion.fromEulerAnglesZYX(e.Target.o, 0.0f, 0.0f), e.Action.summonGO.despawnTime);
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
                case SmartActions.InstallAiTemplate:
                    {
                        InstallTemplate(e);
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
                        if (!IsSmart())
                            break;

                        ((SmartAI)me.GetAI()).SetDisableGravity(e.Action.setDisableGravity.disable != 0);
                        break;
                    }
                case SmartActions.SetCanFly:
                    {
                        if (!IsSmart())
                            break;

                        ((SmartAI)me.GetAI()).SetCanFly(e.Action.setFly.fly != 0);
                        break;
                    }
                case SmartActions.SetRun:
                    {
                        if (!IsSmart())
                            break;

                        ((SmartAI)me.GetAI()).SetRun(e.Action.setRun.run != 0 ? true : false);
                        break;
                    }
                case SmartActions.SetSwim:
                    {
                        if (!IsSmart())
                            break;

                        ((SmartAI)me.GetAI()).SetSwim(e.Action.setSwim.swim != 0 ? true : false);
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
                                    var ai = (SmartAI)target.ToCreature().GetAI();
                                    if (ai != null)
                                        ai.GetScript().StoreCounter(e.Action.setCounter.counterId, e.Action.setCounter.value, e.Action.setCounter.reset);
                                    else
                                        Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SET_COUNTER is not using SmartAI, skipping");
                                }
                                else if (IsGameObject(target))
                                {
                                    var ai = (SmartGameObjectAI)target.ToGameObject().GetAI();
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

                        var run = e.Action.wpStart.run != 0;
                        var entry = e.Action.wpStart.pathID;
                        var repeat = e.Action.wpStart.repeat != 0;

                        foreach (var target in targets)
                        {
                            if (IsPlayer(target))
                            {
                                StoreTargetList(targets, SharedConst.SmartEscortTargets);
                                break;
                            }
                        }

                        me.SetReactState((ReactStates)e.Action.wpStart.reactState);
                        ((SmartAI)me.GetAI()).StartPath(run, entry, repeat, unit);

                        var quest = e.Action.wpStart.quest;
                        var DespawnTime = e.Action.wpStart.despawnTime;
                        ((SmartAI)me.GetAI()).mEscortQuestID = quest;
                        ((SmartAI)me.GetAI()).SetDespawnTime(DespawnTime);
                        break;
                    }
                case SmartActions.WpPause:
                    {
                        if (!IsSmart())
                            break;

                        var delay = e.Action.wpPause.delay;
                        ((SmartAI)me.GetAI()).PausePath(delay, e.GetEventType() != SmartEvents.WaypointReached);
                        break;
                    }
                case SmartActions.WpStop:
                    {
                        if (!IsSmart())
                            break;

                        var DespawnTime = e.Action.wpStop.despawnTime;
                        var quest = e.Action.wpStop.quest;
                        var fail = e.Action.wpStop.fail != 0 ? true : false;
                        ((SmartAI)me.GetAI()).StopPath(DespawnTime, quest, fail);
                        break;
                    }
                case SmartActions.WpResume:
                    {
                        if (!IsSmart())
                            break;

                        ((SmartAI)me.GetAI()).SetWPPauseTimer(0);
                        break;
                    }
                case SmartActions.SetOrientation:
                    {
                        if (me == null)
                            break;

                        if (e.GetTargetType() == SmartTargets.Self)
                            me.SetFacingTo((me.GetTransport() ? me.GetTransportHomePosition() : me.GetHomePosition()).GetOrientation());
                        else if (e.GetTargetType() == SmartTargets.Position)
                            me.SetFacingTo(e.Target.o);
                        else if (!targets.Empty())
                            me.SetFacingToObject(targets.First());

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

                        /*if (e.GetTargetType() == SmartTargets.CreatureRange || e.GetTargetType() == SmartTargets.CreatureGuid ||
                            e.GetTargetType() == SmartTargets.CreatureDistance || e.GetTargetType() == SmartTargets.GameobjectRange ||
                            e.GetTargetType() == SmartTargets.GameobjectGuid || e.GetTargetType() == SmartTargets.GameobjectDistance ||
                            e.GetTargetType() == SmartTargets.ClosestCreature || e.GetTargetType() == SmartTargets.ClosestGameobject ||
                            e.GetTargetType() == SmartTargets.OwnerOrSummoner || e.GetTargetType() == SmartTargets.ActionInvoker ||
                            e.GetTargetType() == SmartTargets.ClosestEnemy || e.GetTargetType() == SmartTargets.ClosestFriendly)*/
                        {
                            // we want to move to random element
                            if (!targets.Empty())
                                target = targets.SelectRandom();
                        }

                        if (target == null)
                        {
                            var dest = new Position(e.Target.x, e.Target.y, e.Target.z);
                            if (e.Action.moveToPos.transport != 0)
                            {
                                var trans = me.GetDirectTransport();
                                if (trans != null)
                                    trans.CalculatePassengerPosition(ref dest.posX, ref dest.posY, ref dest.posZ, ref dest.Orientation);
                            }

                            me.GetMotionMaster().MovePoint(e.Action.moveToPos.pointId, dest, e.Action.moveToPos.disablePathfinding == 0);
                        }
                        else
                        {
                            float x, y, z;
                            target.GetPosition(out x, out y, out z);
                            if (e.Action.moveToPos.contactDistance > 0)
                                target.GetContactPoint(me, out x, out y, out z, e.Action.moveToPos.contactDistance);
                            me.GetMotionMaster().MovePoint(e.Action.moveToPos.pointId, x + e.Target.x, y + e.Target.y, z + e.Target.z, e.Action.moveToPos.disablePathfinding == 0);
                        }
                        break;
                    }
                case SmartActions.RespawnTarget:
                    {
                        foreach (var target in targets)
                        {
                            if (IsCreature(target))
                                target.ToCreature().Respawn();
                            else if (IsGameObject(target))
                            {
                                // do not modify respawndelay of already spawned gameobjects
                                if (target.ToGameObject().IsSpawnedByDefault())
                                    target.ToGameObject().Respawn();
                                else
                                    target.ToGameObject().SetRespawnTime((int)e.Action.respawnTarget.goRespawnTime);
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
                            var npc = target.ToCreature();
                            if (npc != null)
                            {
                                var slot = new EquipmentItem[SharedConst.MaxEquipmentItems];
                                var equipId = (sbyte)e.Action.equip.entry;
                                if (equipId != 0)
                                {
                                    var eInfo = Global.ObjectMgr.GetEquipmentInfo(npc.GetEntry(), equipId);
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

                                for (var i = 0; i < SharedConst.MaxEquipmentItems; ++i)
                                    if (e.Action.equip.mask == 0 || (e.Action.equip.mask & (1 << i)) != 0)
                                        npc.SetVirtualItem(0, slot[i].ItemId, slot[i].AppearanceModId, slot[i].ItemVisual);
                            }
                        }
                        break;
                    }
                case SmartActions.CreateTimedEvent:
                    {
                        var ne = new SmartEvent();
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

                        var ac = new SmartAction();
                        ac.type = SmartActions.TriggerTimedEvent;
                        ac.timeEvent.id = e.Action.timeEvent.id;

                        var ev = new SmartScriptHolder();
                        ev.Event = ne;
                        ev.event_id = e.Action.timeEvent.id;
                        ev.Target = e.Target;
                        ev.Action = ac;
                        InitTimer(ev);
                        mStoredEvents.Add(ev);
                        break;
                    }
                case SmartActions.TriggerTimedEvent:
                    ProcessEventsFor(SmartEvents.TimedEventTriggered, null, e.Action.timeEvent.id);

                    // remove this event if not repeatable
                    if (e.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable))
                        mRemIDs.Add(e.Action.timeEvent.id);
                    break;
                case SmartActions.RemoveTimedEvent:
                    mRemIDs.Add(e.Action.timeEvent.id);
                    break;
                case SmartActions.OverrideScriptBaseObject:
                    {
                        foreach (var target in targets)
                        {
                            if (IsCreature(target))
                            {
                                if (meOrigGUID.IsEmpty() && me)
                                    meOrigGUID = me.GetGUID();
                                if (goOrigGUID.IsEmpty() && go)
                                    goOrigGUID = go.GetGUID();
                                go = null;
                                me = target.ToCreature();
                                break;
                            }
                            else if (IsGameObject(target))
                            {
                                if (meOrigGUID.IsEmpty() && me)
                                    meOrigGUID = me.GetGUID();
                                if (goOrigGUID.IsEmpty() && go)
                                    goOrigGUID = go.GetGUID();
                                go = target.ToGameObject();
                                me = null;
                                break;
                            }
                        }
                        break;
                    }
                case SmartActions.ResetScriptBaseObject:
                    ResetBaseObject();
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
                        var attackAngle = e.Action.setRangedMovement.angle / 180.0f * MathFunctions.PI;

                        foreach (var target in targets)
                        {
                            var creature = target.ToCreature();
                            if (creature != null)
                                if (IsSmart(creature) && creature.GetVictim() != null)
                                    if (((SmartAI)creature.GetAI()).CanCombatMove())
                                        creature.GetMotionMaster().MoveChase(creature.GetVictim(), attackDistance, attackAngle);
                        }
                        break;
                    }
                case SmartActions.CallTimedActionlist:
                    {
                        if (e.GetTargetType() == SmartTargets.None)
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.entryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());
                            break;
                        }

                        foreach (var target in targets)
                        {
                            var creature = target.ToCreature();
                            if (creature != null)
                            {
                                if (IsSmart(creature))
                                    creature.GetAI<SmartAI>().SetScript9(e, e.Action.timedActionList.id, GetLastInvoker());
                            }
                            else
                            {
                                var go = target.ToGameObject();
                                if (go != null)
                                {
                                    if (IsSmartGO(go))
                                        go.GetAI<SmartGameObjectAI>().SetScript9(e, e.Action.timedActionList.id, GetLastInvoker());
                                }
                                else
                                {
                                    var areaTriggerTarget = target.ToAreaTrigger();
                                    if (areaTriggerTarget != null)
                                    {
                                        var atSAI = areaTriggerTarget.GetAI<SmartAreaTriggerAI>();
                                        if (atSAI != null)
                                            atSAI.SetScript9(e, e.Action.timedActionList.id, GetLastInvoker());
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
                                target.ToUnit().SetNpcFlags((NPCFlags)e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.AddNpcFlag:
                    {
                        foreach (var target in targets)
                            if (IsUnit(target))
                                target.ToUnit().AddNpcFlag((NPCFlags)e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.RemoveNpcFlag:
                    {
                        foreach (var target in targets)
                            if (IsUnit(target))
                                target.ToUnit().RemoveNpcFlag((NPCFlags)e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.CrossCast:
                    {
                        if (targets.Empty())
                            break;

                        var casters = GetTargets(CreateSmartEvent(SmartEvents.UpdateIc, 0, 0, 0, 0, 0, 0, SmartActions.None, 0, 0, 0, 0, 0, 0, (SmartTargets)e.Action.crossCast.targetType, e.Action.crossCast.targetParam1, e.Action.crossCast.targetParam2, e.Action.crossCast.targetParam3, 0), unit);
                        foreach (var caster in casters)
                        {
                            if (!IsUnit(caster))
                                continue;

                            var casterUnit = caster.ToUnit();
                            var interruptedSpell = false;

                            foreach (var target in targets)
                            {
                                if (!IsUnit(target))
                                    continue;

                                if (!(e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent)) || !target.ToUnit().HasAura(e.Action.crossCast.spell))
                                {
                                    if (!interruptedSpell && e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                                    {
                                        casterUnit.InterruptNonMeleeSpells(false);
                                        interruptedSpell = true;
                                    }

                                    casterUnit.CastSpell(target.ToUnit(), e.Action.crossCast.spell, e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered));
                                }
                                else
                                    Log.outDebug(LogFilter.ScriptsAi, "Spell {0} not cast because it has flag SMARTCAST_AURA_NOT_PRESENT and the target ({1}) already has the aura", e.Action.crossCast.spell, target.GetGUID().ToString());
                            }
                        }
                        break;
                    }
                case SmartActions.CallRandomTimedActionlist:
                    {
                        var actionLists = new List<uint>();
                        var randTimedActionList = e.Action.randTimedActionList;
                        foreach (var id in new[] { randTimedActionList.actionList1, randTimedActionList.actionList2, randTimedActionList.actionList3, randTimedActionList.actionList4, randTimedActionList.actionList5, randTimedActionList.actionList6 })
                            if (id != 0)
                                actionLists.Add(id);

                        if (e.GetTargetType() == SmartTargets.None)
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.entryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());
                            break;
                        }

                        var randomId = actionLists.SelectRandom();
                        foreach (var target in targets)
                        {
                            var creature = target.ToCreature();
                            if (creature != null)
                            {
                                if (IsSmart(creature))
                                    creature.GetAI<SmartAI>().SetScript9(e, randomId, GetLastInvoker());
                            }
                            else
                            {
                                var go = target.ToGameObject();
                                if (go != null)
                                {
                                    if (IsSmartGO(go))
                                        go.GetAI<SmartGameObjectAI>().SetScript9(e, randomId, GetLastInvoker());
                                }
                                else
                                {
                                    var areaTriggerTarget = target.ToAreaTrigger();
                                    if (areaTriggerTarget != null)
                                    {
                                        var atSAI = areaTriggerTarget.GetAI<SmartAreaTriggerAI>();
                                        if (atSAI != null)
                                            atSAI.SetScript9(e, randomId, GetLastInvoker());
                                    }
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.CallRandomRangeTimedActionlist:
                    {
                        uint id = 0;// RandomHelper.URand(e.Action.randTimedActionList.actionLists[0], e.Action.randTimedActionList.actionLists[1]);
                        if (e.GetTargetType() == SmartTargets.None)
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.entryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());
                            break;
                        }

                        foreach (var target in targets)
                        {
                            var creature = target.ToCreature();
                            if (creature != null)
                            {
                                if (IsSmart(creature))
                                    creature.GetAI<SmartAI>().SetScript9(e, id, GetLastInvoker());
                            }
                            else
                            {
                                var go = target.ToGameObject();
                                if (go != null)
                                {
                                    if (IsSmartGO(go))
                                        go.GetAI<SmartGameObjectAI>().SetScript9(e, id, GetLastInvoker());
                                }
                                else
                                {
                                    var areaTriggerTarget = target.ToAreaTrigger();
                                    if (areaTriggerTarget != null)
                                    {
                                        var atSAI = areaTriggerTarget.GetAI<SmartAreaTriggerAI>();
                                        if (atSAI != null)
                                            atSAI.SetScript9(e, id, GetLastInvoker());
                                    }
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.ActivateTaxi:
                    {
                        foreach (var target in targets)
                            if (IsPlayer(target))
                                target.ToPlayer().ActivateTaxiPathTo(e.Action.taxi.id);
                        break;
                    }
                case SmartActions.RandomMove:
                    {
                        var foundTarget = false;

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

                        if (!foundTarget && me != null && IsCreature(me))
                        {
                            if (e.Action.moveRandom.distance != 0)
                                me.GetMotionMaster().MoveRandom(e.Action.moveRandom.distance);
                            else
                                me.GetMotionMaster().MoveIdle();
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
                                        target.ToUnit().AddVisFlags((UnitVisFlags)e.Action.setunitByte.byte1);
                                        break;
                                    case 3:
                                        // this is totally wrong to maintain compatibility with existing scripts
                                        // TODO: fix with animtier overhaul
                                        target.ToUnit().SetAnimTier((UnitBytes1Flags)(target.ToUnit().m_unitData.AnimTier | e.Action.setunitByte.byte1), false);
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
                                        target.ToUnit().RemoveVisFlags((UnitVisFlags)e.Action.setunitByte.byte1);
                                        break;
                                    case 3:
                                        target.ToUnit().SetAnimTier((UnitBytes1Flags)(target.ToUnit().m_unitData.AnimTier & ~e.Action.setunitByte.byte1), false);
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
                case SmartActions.SendGoCustomAnim:
                    {
                        foreach (var target in targets)
                            if (IsGameObject(target))
                                target.ToGameObject().SendCustomAnim(e.Action.sendGoCustomAnim.anim);
                        break;
                    }
                case SmartActions.SetDynamicFlag:
                    {
                        foreach (var target in targets)
                            if (IsUnit(target))
                                target.ToUnit().SetDynamicFlags((UnitDynFlags)e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.AddDynamicFlag:
                    {
                        foreach (var target in targets)
                            if (IsUnit(target))
                                target.ToUnit().AddDynamicFlag((UnitDynFlags)e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.RemoveDynamicFlag:
                    {
                        foreach (var target in targets)
                            if (IsUnit(target))
                                target.ToUnit().RemoveDynamicFlag((UnitDynFlags)e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.JumpToPos:
                    {
                        foreach (var target in targets)
                        {
                            var creature = target.ToCreature();
                            if (creature != null)
                                creature.GetMotionMaster().MoveJump(e.Target.x, e.Target.y, e.Target.z, 0.0f, e.Action.jump.speedxy, e.Action.jump.speedz);
                        }
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
                        var baseObject = GetBaseObject();
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
                                var ai = (SmartAI)target.ToCreature().GetAI();
                                if (ai != null)
                                    ai.GetScript().StoreTargetList(new List<WorldObject>(storedTargets), e.Action.sendTargetToTarget.id);   // store a copy of target list
                                else
                                    Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SEND_TARGET_TO_TARGET is not using SmartAI, skipping");
                            }
                            else if (IsGameObject(target))
                            {
                                var ai = (SmartGameObjectAI)target.ToGameObject().GetAI();
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
                        if (me)
                            ((SmartAI)me.GetAI()).SetGossipReturn(true);
                        else if (go)
                            ((SmartGameObjectAI)go.GetAI()).SetGossipReturn(true);

                        foreach (var target in targets)
                        {
                            var player = target.ToPlayer();
                            if (player != null)
                            {
                                if (e.Action.sendGossipMenu.gossipMenuId != 0)
                                    player.PrepareGossipMenu(GetBaseObject(), e.Action.sendGossipMenu.gossipMenuId, true);
                                else
                                    player.PlayerTalkClass.ClearMenus();

                                player.PlayerTalkClass.SendGossipMenu(e.Action.sendGossipMenu.gossipNpcTextId, GetBaseObject().GetGUID());
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
                                    target.ToCreature().SetHomePosition(me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetOrientation());
                                else if (e.GetTargetType() == SmartTargets.Position)
                                    target.ToCreature().SetHomePosition(e.Target.x, e.Target.y, e.Target.z, e.Target.o);
                                else if (e.GetTargetType() == SmartTargets.CreatureRange || e.GetTargetType() == SmartTargets.CreatureGuid ||
                                         e.GetTargetType() == SmartTargets.CreatureDistance || e.GetTargetType() == SmartTargets.GameobjectRange ||
                                         e.GetTargetType() == SmartTargets.GameobjectGuid || e.GetTargetType() == SmartTargets.GameobjectDistance ||
                                         e.GetTargetType() == SmartTargets.ClosestCreature || e.GetTargetType() == SmartTargets.ClosestGameobject ||
                                         e.GetTargetType() == SmartTargets.OwnerOrSummoner || e.GetTargetType() == SmartTargets.ActionInvoker ||
                                         e.GetTargetType() == SmartTargets.ClosestEnemy || e.GetTargetType() == SmartTargets.ClosestFriendly)
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
                                target.ToCreature().SetRegenerateHealth(e.Action.setHealthRegen.regenHealth != 0 ? true : false);
                        break;
                    }
                case SmartActions.SetRoot:
                    {
                        foreach (var target in targets)
                            if (IsCreature(target))
                                target.ToCreature().SetControlled(e.Action.setRoot.root != 0 ? true : false, UnitState.Root);
                        break;
                    }
                case SmartActions.SetGoFlag:
                    {
                        foreach (var target in targets)
                            if (IsGameObject(target))
                                target.ToGameObject().SetFlags((GameObjectFlags)e.Action.goFlag.flag);
                        break;
                    }
                case SmartActions.AddGoFlag:
                    {
                        foreach (var target in targets)
                            if (IsGameObject(target))
                                target.ToGameObject().AddFlag((GameObjectFlags)e.Action.goFlag.flag);
                        break;
                    }
                case SmartActions.RemoveGoFlag:
                    {
                        foreach (var target in targets)
                            if (IsGameObject(target))
                                target.ToGameObject().RemoveFlag((GameObjectFlags)e.Action.goFlag.flag);
                        break;
                    }
                case SmartActions.SummonCreatureGroup:
                    {
                        GetBaseObject().SummonCreatureGroup((byte)e.Action.creatureGroup.group, out var summonList);

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
                        var eventId = (ushort)e.Action.gameEventStop.id;
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
                        var eventId = (ushort)e.Action.gameEventStart.id;
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
                        var waypoints = new List<uint>();
                        var closestWaypointFromList = e.Action.closestWaypointFromList;
                        foreach (var id in new[] { closestWaypointFromList.wp1, closestWaypointFromList.wp2, closestWaypointFromList.wp3, closestWaypointFromList.wp4, closestWaypointFromList.wp5, closestWaypointFromList.wp6 })
                            if (id != 0)
                                waypoints.Add(id);

                        var distanceToClosest = float.MaxValue;
                        uint closestPathId = 0;
                        uint closestWaypointId = 0;

                        foreach (var target in targets)
                        {
                            var creature = target.ToCreature();
                            if (creature != null)
                            {
                                if (IsSmart(creature))
                                {
                                    foreach (var pathId in waypoints)
                                    {
                                        var path = Global.SmartAIMgr.GetPath(pathId);
                                        if (path == null || path.nodes.Empty())
                                            continue;

                                        foreach (var waypoint in path.nodes)
                                        {
                                            var distToThisPath = creature.GetDistance(waypoint.x, waypoint.y, waypoint.z);
                                            if (distToThisPath < distanceToClosest)
                                            {
                                                distanceToClosest = distToThisPath;
                                                closestPathId = pathId;
                                                closestWaypointId = waypoint.id;
                                            }
                                        }
                                    }

                                    if (closestPathId != 0)
                                        ((SmartAI)creature.GetAI()).StartPath(false, closestPathId, true, null, closestWaypointId);
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.RandomSound:
                    {
                        var sounds = new List<uint>();
                        var randomSound = e.Action.randomSound;
                        foreach (var id in new[] { randomSound.sound1, randomSound.sound2, randomSound.sound3, randomSound.sound4 })
                            if (id != 0)
                                sounds.Add(id);

                        var onlySelf = e.Action.randomSound.onlySelf != 0;

                        foreach (var target in targets)
                        {
                            if (IsUnit(target))
                            {
                                var sound = sounds.SelectRandom();
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
                                target.ToCreature().SetCorpseDelay(e.Action.corpseDelay.timer);
                        break;
                    }
                case SmartActions.SpawnSpawngroup:
                    {
                        if (e.Action.groupSpawn.minDelay == 0 && e.Action.groupSpawn.maxDelay == 0)
                        {
                            var ignoreRespawn = ((e.Action.groupSpawn.spawnflags & (uint)SmartAiSpawnFlags.IgnoreRespawn) != 0);
                            var force = ((e.Action.groupSpawn.spawnflags & (uint)SmartAiSpawnFlags.ForceSpawn) != 0);

                            // Instant spawn
                            GetBaseObject().GetMap().SpawnGroupSpawn(e.Action.groupSpawn.groupId, ignoreRespawn, force);
                        }
                        else
                        {
                            // Delayed spawn (use values from parameter to schedule event to call us back
                            var ne = new SmartEvent();
                            ne.type = SmartEvents.Update;
                            ne.event_chance = 100;

                            ne.minMaxRepeat.min = e.Action.groupSpawn.minDelay;
                            ne.minMaxRepeat.max = e.Action.groupSpawn.maxDelay;
                            ne.minMaxRepeat.repeatMin = 0;
                            ne.minMaxRepeat.repeatMax = 0;

                            ne.event_flags = 0;
                            ne.event_flags |= SmartEventFlags.NotRepeatable;

                            var ac = new SmartAction();
                            ac.type = SmartActions.SpawnSpawngroup;
                            ac.groupSpawn.groupId = e.Action.groupSpawn.groupId;
                            ac.groupSpawn.minDelay = 0;
                            ac.groupSpawn.maxDelay = 0;
                            ac.groupSpawn.spawnflags = e.Action.groupSpawn.spawnflags;
                            ac.timeEvent.id = e.Action.timeEvent.id;

                            var ev = new SmartScriptHolder();
                            ev.Event = ne;
                            ev.event_id = e.event_id;
                            ev.Target = e.Target;
                            ev.Action = ac;
                            InitTimer(ev);
                            mStoredEvents.Add(ev);
                        }
                        break;
                    }
                case SmartActions.DespawnSpawngroup:
                    {
                        if (e.Action.groupSpawn.minDelay == 0 && e.Action.groupSpawn.maxDelay == 0)
                        {
                            var deleteRespawnTimes = ((e.Action.groupSpawn.spawnflags & (uint)SmartAiSpawnFlags.NosaveRespawn) != 0);

                            // Instant spawn
                            GetBaseObject().GetMap().SpawnGroupSpawn(e.Action.groupSpawn.groupId, deleteRespawnTimes);
                        }
                        else
                        {
                            // Delayed spawn (use values from parameter to schedule event to call us back
                            var ne = new SmartEvent();
                            ne.type = SmartEvents.Update;
                            ne.event_chance = 100;

                            ne.minMaxRepeat.min = e.Action.groupSpawn.minDelay;
                            ne.minMaxRepeat.max = e.Action.groupSpawn.maxDelay;
                            ne.minMaxRepeat.repeatMin = 0;
                            ne.minMaxRepeat.repeatMax = 0;

                            ne.event_flags = 0;
                            ne.event_flags |= SmartEventFlags.NotRepeatable;

                            var ac = new SmartAction();
                            ac.type = SmartActions.DespawnSpawngroup;
                            ac.groupSpawn.groupId = e.Action.groupSpawn.groupId;
                            ac.groupSpawn.minDelay = 0;
                            ac.groupSpawn.maxDelay = 0;
                            ac.groupSpawn.spawnflags = e.Action.groupSpawn.spawnflags;
                            ac.timeEvent.id = e.Action.timeEvent.id;

                            var ev = new SmartScriptHolder();
                            ev.Event = ne;
                            ev.event_id = e.event_id;
                            ev.Target = e.Target;
                            ev.Action = ac;
                            InitTimer(ev);
                            mStoredEvents.Add(ev);
                        }
                        break;
                    }
                case SmartActions.DisableEvade:
                    {
                        if (!IsSmart())
                            break;

                        ((SmartAI)me.GetAI()).SetEvadeDisabled(e.Action.disableEvade.disable != 0);
                        break;
                    }
                case SmartActions.RemoveAurasByType: // can be used to exit vehicle for example
                    {
                        foreach (var target in targets)
                            if (IsUnit(target))
                                target.ToUnit().RemoveAurasByType((AuraType)e.Action.auraType.type);

                        break;
                    }
                case SmartActions.SetSightDist:
                    {
                        foreach (var target in targets)
                            if (IsCreature(target))
                                target.ToCreature().m_SightDistance = e.Action.sightDistance.dist;

                        break;
                    }
                case SmartActions.Flee:
                    {
                        foreach (var target in targets)
                            if (IsCreature(target))
                                target.ToCreature().GetMotionMaster().MoveFleeing(me, e.Action.flee.fleeTime);

                        break;
                    }
                case SmartActions.AddThreat:
                    {
                        foreach (var target in targets)
                            if (IsUnit(target))
                                me.GetThreatManager().AddThreat(target.ToUnit(), (float)(e.Action.threatPCT.threatINC - (float)e.Action.threatPCT.threatDEC), null, true, true);

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
                        var eventId = RandomHelper.URand(e.Action.randomTimedEvent.minId, e.Action.randomTimedEvent.maxId);
                        ProcessEventsFor(SmartEvents.TimedEventTriggered, null, eventId);
                        break;
                    }

                case SmartActions.RemoveAllGameobjects:
                    {
                        foreach (var target in targets)
                            if (IsUnit(target))
                                target.ToUnit().RemoveAllGameObjects();

                        break;
                    }
                case SmartActions.StopMotion:
                    {
                        foreach (var target in targets)
                        {
                            if (IsUnit(target))
                            {
                                if (e.Action.stopMotion.stopMovement != 0)
                                    target.ToUnit().StopMoving();
                                if (e.Action.stopMotion.movementExpired != 0)
                                    target.ToUnit().GetMotionMaster().MovementExpired();
                            }
                        }

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
                                else
                                {
                                    Log.outError(LogFilter.Sql, "SmartScript: Invalid type for SMART_ACTION_PLAY_ANIMKIT, skipping");
                                    break;
                                }

                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction:: SMART_ACTION_PLAY_ANIMKIT: target: {0} ({1}), AnimKit: {2}, Type: {3}",
                                    target.GetName(), target.GetGUID().ToString(), e.Action.animKit.animKit, e.Action.animKit.type);
                            }
                        }
                        break;
                    }
                case SmartActions.ScenePlay:
                    {
                        foreach (var target in targets)
                        {
                            var playerTarget = target.ToPlayer();
                            if (playerTarget)
                                playerTarget.GetSceneMgr().PlayScene(e.Action.scene.sceneId);
                        }

                        break;
                    }
                case SmartActions.SceneCancel:
                    {
                        foreach (var target in targets)
                        {
                            var playerTarget = target.ToPlayer();
                            if (playerTarget)
                                playerTarget.GetSceneMgr().CancelSceneBySceneId(e.Action.scene.sceneId);
                        }

                        break;
                    }
                case SmartActions.SetMovementSpeed:
                    {
                        var speedInteger = e.Action.movementSpeed.speedInteger;
                        var speedFraction = e.Action.movementSpeed.speedFraction;
                        var speed = (float)((float)speedInteger + (float)speedFraction / Math.Pow(10, Math.Floor(Math.Log10((float)(speedFraction != 0 ? speedFraction : 1)) + 1)));

                        foreach (var target in targets)
                            if (IsCreature(target))
                                me.SetSpeed((UnitMoveType)e.Action.movementSpeed.movementType, speed);

                        break;
                    }
                case SmartActions.PlaySpellVisualKit:
                    {
                        foreach (var target in targets)
                        {
                            if (IsUnit(target))
                            {
                                target.ToUnit().SendPlaySpellVisualKit(e.Action.spellVisualKit.spellVisualKitId, e.Action.spellVisualKit.kitType, e.Action.spellVisualKit.duration);
                                Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction:: SMART_ACTION_PLAY_SPELL_VISUAL_KIT: target: {target.GetName()} ({target.GetGUID()}), SpellVisualKit: {e.Action.spellVisualKit.spellVisualKitId}");
                            }
                        }
                        break;
                    }
                case SmartActions.CreateConversation:
                    {
                        var baseObject = GetBaseObject();
                        if (baseObject != null)
                        {
                            foreach (var target in targets)
                            {
                                var playerTarget = target.ToPlayer();
                                if (playerTarget != null)
                                {
                                    var conversation = Conversation.CreateConversation(e.Action.conversation.id, playerTarget,
                                        playerTarget, new List<ObjectGuid>() { playerTarget.GetGUID() }, null);
                                    if (!conversation)
                                        Log.outWarn(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_CREATE_CONVERSATION: id {e.Action.conversation.id}, baseObject {baseObject.GetName()}, target {playerTarget.GetName()} - failed to create");
                                }
                            }
                        }

                        break;
                    }
                default:
                    Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Unhandled Action type {3}", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                    break;
            }

            if (e.link != 0 && e.link != e.event_id)
            {
                var linked = Global.SmartAIMgr.FindLinkedEvent(mEvents, e.link);
                if (linked != null)
                    ProcessEvent(linked, unit, var0, var1, bvar, spell, gob, varString);
                else
                    Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Link Event {3} not found or invalid, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.link);
            }
        }

        void ProcessTimedAction(SmartScriptHolder e, uint min, uint max, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
        {
            // We may want to execute action rarely and because of this if condition is not fulfilled the action will be rechecked in a long time
            if (Global.ConditionMgr.IsObjectMeetingSmartEventConditions(e.entryOrGuid, e.event_id, e.source_type, unit, GetBaseObject()))
            {
                RecalcTimer(e, min, max);
                ProcessAction(e, unit, var0, var1, bvar, spell, gob, varString);
            }
            else
                RecalcTimer(e, Math.Min(min, 5000), Math.Min(min, 5000));
        }

        void InstallTemplate(SmartScriptHolder e)
        {
            if (GetBaseObject() == null)
                return;

            if (mTemplate != SmartAITemplate.Basic)
            {
                Log.outError(LogFilter.Sql, "SmartScript.InstallTemplate: Entry {0} SourceType {1} AI Template can not be set more then once, skipped.", e.entryOrGuid, e.GetScriptType());
                return;
            }

            mTemplate = (SmartAITemplate)e.Action.installTtemplate.id;
            switch ((SmartAITemplate)e.Action.installTtemplate.id)
            {
                case SmartAITemplate.Caster:
                    {
                        AddEvent(SmartEvents.UpdateIc, 0, 0, 0, e.Action.installTtemplate.param2, e.Action.installTtemplate.param3, 0, SmartActions.Cast, e.Action.installTtemplate.param1, e.Target.raw.param1, 0, 0, 0, 0, SmartTargets.Victim, 0, 0, 0, 1);
                        AddEvent(SmartEvents.Range, 0, e.Action.installTtemplate.param4, 300, 0, 0, 0, SmartActions.AllowCombatMovement, 1, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 1);
                        AddEvent(SmartEvents.Range, 0, 0, e.Action.installTtemplate.param4 > 10 ? e.Action.installTtemplate.param4 - 10 : 0, 0, 0, 0, SmartActions.AllowCombatMovement, 0, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 1);
                        AddEvent(SmartEvents.ManaPct, 0, e.Action.installTtemplate.param5 - 15 > 100 ? 100 : e.Action.installTtemplate.param5 + 15, 100, 1000, 1000, 0, SmartActions.SetEventPhase, 1, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        AddEvent(SmartEvents.ManaPct, 0, 0, e.Action.installTtemplate.param5, 1000, 1000, 0, SmartActions.SetEventPhase, 0, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        AddEvent(SmartEvents.ManaPct, 0, 0, e.Action.installTtemplate.param5, 1000, 1000, 0, SmartActions.AllowCombatMovement, 1, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        break;
                    }
                case SmartAITemplate.Turret:
                    {
                        AddEvent(SmartEvents.UpdateIc, 0, 0, 0, e.Action.installTtemplate.param2, e.Action.installTtemplate.param3, 0, SmartActions.Cast, e.Action.installTtemplate.param1, e.Target.raw.param1, 0, 0, 0, 0, SmartTargets.Victim, 0, 0, 0, 0);
                        AddEvent(SmartEvents.JustCreated, 0, 0, 0, 0, 0, 0, SmartActions.AllowCombatMovement, 0, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        break;
                    }
                case SmartAITemplate.CagedNPCPart:
                    {
                        if (me == null)
                            return;
                        //store cage as id1
                        AddEvent(SmartEvents.DataSet, 0, 0, 0, 0, 0, 0, SmartActions.StoreTargetList, 1, 0, 0, 0, 0, 0, SmartTargets.ClosestGameobject, e.Action.installTtemplate.param1, 10, 0, 0);

                        //reset(close) cage on hostage(me) respawn
                        AddEvent(SmartEvents.Update, SmartEventFlags.NotRepeatable, 0, 0, 0, 0, 0, SmartActions.ResetGobject, 0, 0, 0, 0, 0, 0, SmartTargets.GameobjectDistance, e.Action.installTtemplate.param1, 5, 0, 0);

                        AddEvent(SmartEvents.DataSet, 0, 0, 0, 0, 0, 0, SmartActions.SetRun, e.Action.installTtemplate.param3, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        AddEvent(SmartEvents.DataSet, 0, 0, 0, 0, 0, 0, SmartActions.SetEventPhase, 1, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);

                        AddEvent(SmartEvents.Update, SmartEventFlags.NotRepeatable, 1000, 1000, 0, 0, 0, SmartActions.MoveOffset, 0, 0, 0, 0, 0, 0, SmartTargets.Self, 0, e.Action.installTtemplate.param4, 0, 1);
                        //phase 1: give quest credit on movepoint reached
                        AddEvent(SmartEvents.Movementinform, 0, (uint)MovementGeneratorType.Point, EventId.SmartRandomPoint, 0, 0, 0, SmartActions.SetData, 0, 0, 0, 0, 0, 0, SmartTargets.Stored, 1, 0, 0, 1);
                        //phase 1: despawn after time on movepoint reached
                        AddEvent(SmartEvents.Movementinform, 0, (uint)MovementGeneratorType.Point, EventId.SmartRandomPoint, 0, 0, 0, SmartActions.ForceDespawn, e.Action.installTtemplate.param2, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 1);

                        if (Global.CreatureTextMgr.TextExist(me.GetEntry(), (byte)e.Action.installTtemplate.param5))
                            AddEvent(SmartEvents.Movementinform, 0, (uint)MovementGeneratorType.Point, EventId.SmartRandomPoint, 0, 0, 0, SmartActions.Talk, e.Action.installTtemplate.param5, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 1);
                        break;
                    }
                case SmartAITemplate.CagedGOPart:
                    {
                        if (go == null)
                            return;
                        //store hostage as id1
                        AddEvent(SmartEvents.GoLootStateChanged, 0, 2, 0, 0, 0, 0, SmartActions.StoreTargetList, 1, 0, 0, 0, 0, 0, SmartTargets.ClosestCreature, e.Action.installTtemplate.param1, 10, 0, 0);
                        //store invoker as id2
                        AddEvent(SmartEvents.GoLootStateChanged, 0, 2, 0, 0, 0, 0, SmartActions.StoreTargetList, 2, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        //signal hostage
                        AddEvent(SmartEvents.GoLootStateChanged, 0, 2, 0, 0, 0, 0, SmartActions.SetData, 0, 0, 0, 0, 0, 0, SmartTargets.Stored, 1, 0, 0, 0);
                        //when hostage raeched end point, give credit to invoker
                        if (e.Action.installTtemplate.param2 != 0)
                            AddEvent(SmartEvents.DataSet, 0, 0, 0, 0, 0, 0, SmartActions.CallKilledmonster, e.Action.installTtemplate.param1, 0, 0, 0, 0, 0, SmartTargets.Stored, 2, 0, 0, 0);
                        else
                            AddEvent(SmartEvents.GoLootStateChanged, 0, 2, 0, 0, 0, 0, SmartActions.CallKilledmonster, e.Action.installTtemplate.param1, 0, 0, 0, 0, 0, SmartTargets.Stored, 2, 0, 0, 0);
                        break;
                    }
                default:
                    return;
            }
        }

        void AddEvent(SmartEvents e, SmartEventFlags event_flags, uint event_param1, uint event_param2, uint event_param3, uint event_param4, uint event_param5,
            SmartActions action, uint action_param1, uint action_param2, uint action_param3, uint action_param4, uint action_param5, uint action_param6,
            SmartTargets t, uint target_param1, uint target_param2, uint target_param3, uint phaseMask)
        {
            mInstallEvents.Add(CreateSmartEvent(e, event_flags, event_param1, event_param2, event_param3, event_param4, event_param5, action, action_param1, action_param2, action_param3, action_param4, action_param5, action_param6, t, target_param1, target_param2, target_param3, phaseMask));
        }

        SmartScriptHolder CreateSmartEvent(SmartEvents e, SmartEventFlags event_flags, uint event_param1, uint event_param2, uint event_param3, uint event_param4, uint event_param5,
            SmartActions action, uint action_param1, uint action_param2, uint action_param3, uint action_param4, uint action_param5, uint action_param6,
            SmartTargets t, uint target_param1, uint target_param2, uint target_param3, uint phaseMask)
        {
            var script = new SmartScriptHolder();
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

            script.Target.type = t;
            script.Target.raw.param1 = target_param1;
            script.Target.raw.param2 = target_param2;
            script.Target.raw.param3 = target_param3;

            script.source_type = SmartScriptType.Creature;
            InitTimer(script);
            return script;
        }

        List<WorldObject> GetTargets(SmartScriptHolder e, Unit invoker = null)
        {
            Unit scriptTrigger = null;
            var tempLastInvoker = GetLastInvoker();
            if (invoker != null)
                scriptTrigger = invoker;
            else if (tempLastInvoker != null)
                scriptTrigger = tempLastInvoker;

            var baseObject = GetBaseObject();

            var targets = new List<WorldObject>();
            switch (e.GetTargetType())
            {
                case SmartTargets.Self:
                    if (baseObject != null)
                        targets.Add(baseObject);
                    break;
                case SmartTargets.Victim:
                    if (me != null && me.GetVictim() != null)
                        targets.Add(me.GetVictim());
                    break;
                case SmartTargets.HostileSecondAggro:
                    if (me != null)
                    {
                        if (e.Target.hostilRandom.powerType != 0)
                        {
                            var u = me.GetAI().SelectTarget(SelectAggroTarget.MaxThreat, 1, new PowerUsersSelector(me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));
                            if (u != null)
                                targets.Add(u);
                        }
                        else
                        {
                            var u = me.GetAI().SelectTarget(SelectAggroTarget.MaxThreat, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);
                            if (u != null)
                                targets.Add(u);
                        }
                    }
                    break;
                case SmartTargets.HostileLastAggro:
                    if (me != null)
                    {
                        if (e.Target.hostilRandom.powerType != 0)
                        {
                            var u = me.GetAI().SelectTarget(SelectAggroTarget.MinThreat, 1, new PowerUsersSelector(me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));
                            if (u != null)
                                targets.Add(u);
                        }
                        else
                        {
                            var u = me.GetAI().SelectTarget(SelectAggroTarget.MinThreat, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);
                            if (u != null)
                                targets.Add(u);
                        }
                    }
                    break;
                case SmartTargets.HostileRandom:
                    if (me != null)
                    {
                        if (e.Target.hostilRandom.powerType != 0)
                        {
                            var u = me.GetAI().SelectTarget(SelectAggroTarget.Random, 1, new PowerUsersSelector(me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));
                            if (u != null)
                                targets.Add(u);
                        }
                        else
                        {
                            var u = me.GetAI().SelectTarget(SelectAggroTarget.Random, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);
                            if (u != null)
                                targets.Add(u);
                        }
                    }
                    break;
                case SmartTargets.HostileRandomNotTop:
                    if (me != null)
                    {
                        if (e.Target.hostilRandom.powerType != 0)
                        {
                            var u = me.GetAI().SelectTarget(SelectAggroTarget.Random, 1, new PowerUsersSelector(me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));
                            if (u != null)
                                targets.Add(u);
                        }
                        else
                        {
                            var u = me.GetAI().SelectTarget(SelectAggroTarget.Random, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);
                            if (u != null)
                                targets.Add(u);
                        }
                    }
                    break;
                case SmartTargets.Farthest:
                    if (me)
                    {
                        var u = me.GetAI().SelectTarget(SelectAggroTarget.MaxDistance, 0, new FarthestTargetSelector(me, (float)e.Target.farthest.maxDist, e.Target.farthest.playerOnly != 0, e.Target.farthest.isInLos != 0));
                        if (u != null)
                            targets.Add(u);
                    }
                    break;
                case SmartTargets.ActionInvoker:
                    if (scriptTrigger != null)
                        targets.Add(scriptTrigger);
                    break;
                case SmartTargets.ActionInvokerVehicle:
                    if (scriptTrigger != null && scriptTrigger.GetVehicle() != null && scriptTrigger.GetVehicle().GetBase() != null)
                        targets.Add(scriptTrigger.GetVehicle().GetBase());
                    break;
                case SmartTargets.InvokerParty:
                    if (scriptTrigger != null)
                    {
                        var player = scriptTrigger.ToPlayer();
                        if (player != null)
                        {
                            var group = player.GetGroup();
                            if (group)
                            {
                                for (var groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                                {
                                    var member = groupRef.GetSource();
                                    if (member)
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
                        var units = GetWorldObjectsInDist(e.Target.unitRange.maxDist);
                        foreach (var obj in units)
                        {
                            if (!IsCreature(obj))
                                continue;

                            if (me != null && me == obj)
                                continue;

                            if ((e.Target.unitRange.creature == 0 || obj.ToCreature().GetEntry() == e.Target.unitRange.creature) && baseObject.IsInRange(obj, e.Target.unitRange.minDist, e.Target.unitRange.maxDist))
                                targets.Add(obj);
                        }
                        break;
                    }
                case SmartTargets.CreatureDistance:
                    {
                        var units = GetWorldObjectsInDist(e.Target.unitDistance.dist);
                        foreach (var obj in units)
                        {
                            if (!IsCreature(obj))
                                continue;

                            if (me != null && me == obj)
                                continue;

                            if (e.Target.unitDistance.creature == 0 || obj.ToCreature().GetEntry() == e.Target.unitDistance.creature)
                                targets.Add(obj);
                        }
                        break;
                    }
                case SmartTargets.GameobjectDistance:
                    {
                        var units = GetWorldObjectsInDist(e.Target.goDistance.dist);
                        foreach (var obj in units)
                        {
                            if (!IsGameObject(obj))
                                continue;

                            if (go != null && go == obj)
                                continue;

                            if (e.Target.goDistance.entry == 0 || obj.ToGameObject().GetEntry() == e.Target.goDistance.entry)
                                targets.Add(obj);
                        }
                        break;
                    }
                case SmartTargets.GameobjectRange:
                    {
                        var units = GetWorldObjectsInDist(e.Target.goRange.maxDist);
                        foreach (var obj in units)
                        {
                            if (!IsGameObject(obj))
                                continue;

                            if (go != null && go == obj)
                                continue;

                            if ((e.Target.goRange.entry == 0 && obj.ToGameObject().GetEntry() == e.Target.goRange.entry) && baseObject.IsInRange(obj, e.Target.goRange.minDist, e.Target.goRange.maxDist))
                                targets.Add(obj);
                        }
                        break;
                    }
                case SmartTargets.CreatureGuid:
                    {
                        if (scriptTrigger == null && baseObject == null)
                        {
                            Log.outError(LogFilter.Sql, "SMART_TARGET_CREATURE_GUID can not be used without invoker");
                            break;
                        }

                        var target = FindCreatureNear(scriptTrigger != null ? scriptTrigger : baseObject, e.Target.unitGUID.dbGuid);
                        if (target)
                            if (target != null && (e.Target.unitGUID.entry == 0 || target.GetEntry() == e.Target.unitGUID.entry))
                                targets.Add(target);
                        break;
                    }
                case SmartTargets.GameobjectGuid:
                    {
                        if (scriptTrigger == null && baseObject == null)
                        {
                            Log.outError(LogFilter.Sql, "SMART_TARGET_GAMEOBJECT_GUID can not be used without invoker");
                            break;
                        }

                        var target = FindGameObjectNear(scriptTrigger != null ? scriptTrigger : baseObject, e.Target.goGUID.dbGuid);
                        if (target)
                            if (target != null && (e.Target.goGUID.entry == 0 || target.GetEntry() == e.Target.goGUID.entry))
                                targets.Add(target);
                        break;
                    }
                case SmartTargets.PlayerRange:
                    {
                        var units = GetWorldObjectsInDist(e.Target.playerRange.maxDist);
                        if (!units.Empty() && baseObject != null)
                            foreach (var obj in units)
                                if (IsPlayer(obj) && baseObject.IsInRange(obj, e.Target.playerRange.minDist, e.Target.playerRange.maxDist))
                                    targets.Add(obj);

                        break;
                    }
                case SmartTargets.PlayerDistance:
                    {
                        var units = GetWorldObjectsInDist(e.Target.playerDistance.dist);
                        foreach (var obj in units)
                            if (IsPlayer(obj))
                                targets.Add(obj);
                        break;
                    }
                case SmartTargets.Stored:
                    {
                        if (baseObject == null)
                            baseObject = scriptTrigger;

                        if (baseObject != null)
                        {
                            var stored = GetStoredTargetList(e.Target.stored.id, baseObject);
                            if (!stored.Empty())
                                targets.AddRange(stored);
                        }

                        break;
                    }
                case SmartTargets.ClosestCreature:
                    {
                        var target = baseObject.FindNearestCreature(e.Target.closest.entry, e.Target.closest.dist != 0 ? e.Target.closest.dist : 100, e.Target.closest.dead == 0);
                        if (target)
                            targets.Add(target);
                        break;
                    }
                case SmartTargets.ClosestGameobject:
                    {
                        var target = baseObject.FindNearestGameObject(e.Target.closest.entry, e.Target.closest.dist != 0 ? e.Target.closest.dist : 100);
                        if (target)
                            targets.Add(target);
                        break;
                    }
                case SmartTargets.ClosestPlayer:
                    {
                        var obj = GetBaseObject();
                        if (obj != null)
                        {
                            var target = obj.SelectNearestPlayer(e.Target.playerDistance.dist);
                            if (target)
                                targets.Add(target);
                        }
                        break;
                    }
                case SmartTargets.OwnerOrSummoner:
                    {
                        if (me != null)
                        {
                            var charmerOrOwnerGuid = me.GetCharmerOrOwnerGUID();
                            if (charmerOrOwnerGuid.IsEmpty())
                            {
                                var tempSummon = me.ToTempSummon();
                                if (tempSummon)
                                {
                                    var summoner = tempSummon.GetSummoner();
                                    if (summoner)
                                        charmerOrOwnerGuid = summoner.GetGUID();
                                }
                            }

                            if (charmerOrOwnerGuid.IsEmpty())
                                charmerOrOwnerGuid = me.GetCreatorGUID();

                            var owner = Global.ObjAccessor.GetUnit(me, charmerOrOwnerGuid);
                            if (owner != null)
                                targets.Add(owner);
                        }
                        else if (go != null)
                        {
                            var owner = Global.ObjAccessor.GetUnit(go, go.GetOwnerGUID());
                            if (owner)
                                targets.Add(owner);
                        }

                        // Get owner of owner
                        if (e.Target.owner.useCharmerOrOwner != 0 && !targets.Empty())
                        {
                            var owner = targets.First().ToUnit();
                            targets.Clear();

                            var unitBase = Global.ObjAccessor.GetUnit(owner, owner.GetCharmerOrOwnerGUID());
                            if (unitBase != null)
                                targets.Add(unitBase);
                        }
                        break;
                    }
                case SmartTargets.ThreatList:
                    {
                        if (me != null && me.CanHaveThreatList())
                        {
                            var threatList = me.GetThreatManager().GetThreatList();
                            foreach (var refe in me.GetThreatManager().GetThreatList())
                                if (e.Target.hostilRandom.maxDist == 0 || me.IsWithinCombatRange(refe.GetTarget(), e.Target.hostilRandom.maxDist))
                                    targets.Add(refe.GetTarget());
                        }
                        break;
                    }
                case SmartTargets.ClosestEnemy:
                    {
                        if (me != null)
                        {
                            var target = me.SelectNearestTarget(e.Target.closestAttackable.maxDist);
                            if (target != null)
                                targets.Add(target);
                        }

                        break;
                    }
                case SmartTargets.ClosestFriendly:
                    {
                        if (me != null)
                        {
                            var target = DoFindClosestFriendlyInRange(e.Target.closestFriendly.maxDist);
                            if (target != null)
                                targets.Add(target);
                        }
                        break;
                    }
                case SmartTargets.LootRecipients:
                    {
                        if (me)
                        {
                            var lootGroup = me.GetLootRecipientGroup();
                            if (lootGroup)
                            {
                                for (var refe = lootGroup.GetFirstMember(); refe != null; refe = refe.Next())
                                {
                                    var recipient = refe.GetSource();
                                    if (recipient)
                                        if (recipient.IsInMap(me))
                                            targets.Add(recipient);
                                }
                            }
                            else
                            {
                                var recipient = me.GetLootRecipient();
                                if (recipient)
                                    targets.Add(recipient);
                            }
                        }
                        break;
                    }
                case SmartTargets.VehicleAccessory:
                    {
                        if (me && me.IsVehicle())
                        {
                            var target = me.GetVehicleKit().GetPassenger((sbyte)e.Target.vehicle.seat);
                            if (target)
                                targets.Add(target);
                        }
                        break;
                    }
                case SmartTargets.SpellTarget:
                    {
                        if (spellTemplate != null)
                            targets.Add(spellTemplate.m_targets.GetUnitTarget());
                        break;
                    }
                case SmartTargets.Position:
                default:
                    break;
            }

            return targets;
        }

        List<WorldObject> GetWorldObjectsInDist(float dist)
        {
            var targets = new List<WorldObject>();
            var obj = GetBaseObject();
            if (obj == null)
                return targets;

            var u_check = new AllWorldObjectsInRange(obj, dist);
            var searcher = new WorldObjectListSearcher(obj, targets, u_check);
            Cell.VisitAllObjects(obj, searcher, dist);
            return targets;
        }

        void ProcessEvent(SmartScriptHolder e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
        {
            if (!e.active && e.GetEventType() != SmartEvents.Link)
                return;

            if ((e.Event.event_phase_mask != 0 && !IsInPhase(e.Event.event_phase_mask)) || (e.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) && e.runOnce))
                return;

            if (!e.Event.event_flags.HasAnyFlag(SmartEventFlags.WhileCharmed) && IsCharmedCreature(me))
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
                    if (me != null && me.IsEngaged())
                        return;
                    ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    break;
                case SmartEvents.UpdateIc:
                    if (me == null || !me.IsEngaged())
                        return;
                    ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    break;
                case SmartEvents.HealthPct:
                    {
                        if (me == null || !me.IsEngaged() || me.GetMaxHealth() == 0)
                            return;
                        var perc = (uint)me.GetHealthPct();
                        if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                            return;
                        ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                        break;
                    }
                case SmartEvents.TargetHealthPct:
                    {
                        if (me == null || !me.IsEngaged() || me.GetVictim() == null || me.GetVictim().GetMaxHealth() == 0)
                            return;
                        var perc = (uint)me.GetVictim().GetHealthPct();
                        if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                            return;
                        ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax, me.GetVictim());
                        break;
                    }
                case SmartEvents.ManaPct:
                    {
                        if (me == null || !me.IsEngaged() || me.GetMaxPower(PowerType.Mana) == 0)
                            return;
                        var perc = (uint)me.GetPowerPct(PowerType.Mana);
                        if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                            return;
                        ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                        break;
                    }
                case SmartEvents.TargetManaPct:
                    {
                        if (me == null || !me.IsEngaged() || me.GetVictim() == null || me.GetVictim().GetMaxPower(PowerType.Mana) == 0)
                            return;
                        var perc = (uint)me.GetVictim().GetPowerPct(PowerType.Mana);
                        if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                            return;
                        ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax, me.GetVictim());
                        break;
                    }
                case SmartEvents.Range:
                    {
                        if (me == null || !me.IsEngaged() || me.GetVictim() == null)
                            return;

                        if (me.IsInRange(me.GetVictim(), e.Event.minMaxRepeat.min, e.Event.minMaxRepeat.max))
                            ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax, me.GetVictim());
                        else // make it predictable
                            RecalcTimer(e, 500, 500);
                        break;
                    }
                case SmartEvents.VictimCasting:
                    {
                        if (me == null || !me.IsEngaged())
                            return;

                        var victim = me.GetVictim();

                        if (victim == null || !victim.IsNonMeleeSpellCast(false, false, true))
                            return;

                        if (e.Event.targetCasting.spellId > 0)
                        {
                            var currSpell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);
                            if (currSpell != null)
                                if (currSpell.m_spellInfo.Id != e.Event.targetCasting.spellId)
                                    return;
                        }

                        ProcessTimedAction(e, e.Event.targetCasting.repeatMin, e.Event.targetCasting.repeatMax, me.GetVictim());
                        break;
                    }
                case SmartEvents.FriendlyHealth:
                    {
                        if (me == null || !me.IsEngaged())
                            return;

                        var target = DoSelectLowestHpFriendly(e.Event.friendlyHealth.radius, e.Event.friendlyHealth.hpDeficit);
                        if (target == null || !target.IsInCombat())
                        {
                            // if there are at least two same npcs, they will perform the same action immediately even if this is useless...
                            RecalcTimer(e, 1000, 3000);
                            return;
                        }

                        ProcessTimedAction(e, e.Event.friendlyHealth.repeatMin, e.Event.friendlyHealth.repeatMax, target);
                        break;
                    }
                case SmartEvents.FriendlyIsCc:
                    {
                        if (me == null || !me.IsEngaged())
                            return;

                        var creatures = new List<Creature>();
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
                        var creatures = new List<Creature>();
                        DoFindFriendlyMissingBuff(creatures, e.Event.missingBuff.radius, e.Event.missingBuff.spell);

                        if (creatures.Empty())
                            return;

                        ProcessTimedAction(e, e.Event.missingBuff.repeatMin, e.Event.missingBuff.repeatMax, creatures.SelectRandom());
                        break;
                    }
                case SmartEvents.HasAura:
                    {
                        if (me == null)
                            return;
                        var count = me.GetAuraCount(e.Event.aura.spell);
                        if ((e.Event.aura.count == 0 && count == 0) || (e.Event.aura.count != 0 && count >= e.Event.aura.count))
                            ProcessTimedAction(e, e.Event.aura.repeatMin, e.Event.aura.repeatMax);
                        break;
                    }
                case SmartEvents.TargetBuffed:
                    {
                        if (me == null || me.GetVictim() == null)
                            return;
                        var count = me.GetVictim().GetAuraCount(e.Event.aura.spell);
                        if (count < e.Event.aura.count)
                            return;
                        ProcessTimedAction(e, e.Event.aura.repeatMin, e.Event.aura.repeatMax);
                        break;
                    }
                case SmartEvents.Charmed:
                    {
                        if (bvar == (e.Event.charm.onRemove != 1))
                            ProcessAction(e, unit, var0, var1, bvar, spell, gob);
                        break;
                    }
                //no params
                case SmartEvents.Aggro:
                case SmartEvents.Death:
                case SmartEvents.Evade:
                case SmartEvents.ReachedHome:
                case SmartEvents.CharmedTarget:
                case SmartEvents.CorpseRemoved:
                case SmartEvents.AiInit:
                case SmartEvents.TransportAddplayer:
                case SmartEvents.TransportRemovePlayer:
                case SmartEvents.QuestAccepted:
                case SmartEvents.QuestObjCompletion:
                case SmartEvents.QuestCompletion:
                case SmartEvents.QuestRewarded:
                case SmartEvents.QuestFail:
                case SmartEvents.JustSummoned:
                case SmartEvents.Reset:
                case SmartEvents.JustCreated:
                case SmartEvents.FollowCompleted:
                case SmartEvents.OnSpellclick:
                    ProcessAction(e, unit, var0, var1, bvar, spell, gob);
                    break;
                case SmartEvents.GossipHello:
                    {
                        if (e.Event.gossipHello.noReportUse != 0 && var0 != 0)
                            return;
                        ProcessAction(e, unit, var0, var1, bvar, spell, gob);
                        break;
                    }
                case SmartEvents.IsBehindTarget:
                    {
                        if (me == null)
                            return;

                        var victim = me.GetVictim();
                        if (victim != null)
                        {
                            if (!victim.HasInArc(MathFunctions.PI, me))
                                ProcessTimedAction(e, e.Event.behindTarget.cooldownMin, e.Event.behindTarget.cooldownMax, victim);
                        }
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
                        if (me == null || unit == null)
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
                            ProcessAction(e, unit, 0, 0, bvar, spell);
                        }
                        break;
                    }
                case SmartEvents.OocLos:
                    {
                        if (me == null || me.IsEngaged())
                            return;
                        //can trigger if closer than fMaxAllowedRange
                        float range = e.Event.los.maxDist;

                        //if range is ok and we are actually in LOS
                        if (me.IsWithinDistInMap(unit, range) && me.IsWithinLOSInMap(unit))
                        {
                            //if friendly event&&who is not hostile OR hostile event&&who is hostile
                            if ((e.Event.los.noHostile != 0 && !me.IsHostileTo(unit)) || (e.Event.los.noHostile == 0 && me.IsHostileTo(unit)))
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
                        if (me == null || !me.IsEngaged())
                            return;
                        //can trigger if closer than fMaxAllowedRange
                        float range = e.Event.los.maxDist;

                        //if range is ok and we are actually in LOS
                        if (me.IsWithinDistInMap(unit, range) && me.IsWithinLOSInMap(unit))
                        {
                            //if friendly event&&who is not hostile OR hostile event&&who is hostile
                            if ((e.Event.los.noHostile != 0 && !me.IsHostileTo(unit)) || (e.Event.los.noHostile == 0 && me.IsHostileTo(unit)))
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
                        if ((e.Event.movementInform.type != 0 && var0 != e.Event.movementInform.type) || (e.Event.movementInform.id != 0 && var1 != e.Event.movementInform.id))
                            return;
                        ProcessAction(e, unit, var0, var1);
                        break;
                    }
                case SmartEvents.TransportRelocate:
                case SmartEvents.WaypointStart:
                    {
                        if (e.Event.waypoint.pathID != 0 && var0 != e.Event.waypoint.pathID)
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
                        if (me == null || (e.Event.waypoint.pointID != 0 && var0 != e.Event.waypoint.pointID) || (e.Event.waypoint.pathID != 0 && var1 != e.Event.waypoint.pathID))
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
                case SmartEvents.AreatriggerOntrigger:
                    {
                        if (e.Event.areatrigger.id != 0 && var0 != e.Event.areatrigger.id)
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
                case SmartEvents.PhaseChange:
                    {
                        if (!IsInPhase(e.Event.eventPhaseChange.phasemask))
                            return;

                        ProcessAction(e, GetLastInvoker());
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
                        if (me == null || !me.IsEngaged())
                            return;

                        List<WorldObject> targets;

                        switch (e.GetTargetType())
                        {
                            case SmartTargets.CreatureRange:
                            case SmartTargets.CreatureGuid:
                            case SmartTargets.CreatureDistance:
                            case SmartTargets.ClosestCreature:
                            case SmartTargets.ClosestPlayer:
                            case SmartTargets.PlayerRange:
                            case SmartTargets.PlayerDistance:
                                targets = GetTargets(e);
                                break;
                            default:
                                return;
                        }
                        
                        if (targets == null)
                            return;

                        Unit unitTarget = null;
                        foreach (var target in targets)
                        {
                            if (IsUnit(target) && me.IsFriendlyTo(target.ToUnit()) && target.ToUnit().IsAlive() && target.ToUnit().IsInCombat())
                            {
                                var healthPct = (uint)target.ToUnit().GetHealthPct();

                                if (healthPct > e.Event.friendlyHealthPct.maxHpPct || healthPct < e.Event.friendlyHealthPct.minHpPct)
                                    continue;

                                unitTarget = target.ToUnit();
                                break;
                            }
                        }

                        if (unitTarget == null)
                            return;

                        ProcessTimedAction(e, e.Event.friendlyHealthPct.repeatMin, e.Event.friendlyHealthPct.repeatMax, unitTarget);
                        break;
                    }
                case SmartEvents.DistanceCreature:
                    {
                        if (!me)
                            return;

                        Creature creature = null;

                        if (e.Event.distance.guid != 0)
                        {
                            creature = FindCreatureNear(me, e.Event.distance.guid);

                            if (!creature)
                                return;

                            if (!me.IsInRange(creature, 0, e.Event.distance.dist))
                                return;
                        }
                        else if (e.Event.distance.entry != 0)
                        {
                            var list = new List<Creature>();
                            me.GetCreatureListWithEntryInGrid(list, e.Event.distance.entry, e.Event.distance.dist);

                            if (!list.Empty())
                                creature = list.FirstOrDefault();
                        }

                        if (creature)
                            ProcessTimedAction(e, e.Event.distance.repeat, e.Event.distance.repeat, creature);

                        break;
                    }
                case SmartEvents.DistanceGameobject:
                    {
                        if (!me)
                            return;

                        GameObject gameobject = null;

                        if (e.Event.distance.guid != 0)
                        {
                            gameobject = FindGameObjectNear(me, e.Event.distance.guid);

                            if (!gameobject)
                                return;

                            if (!me.IsInRange(gameobject, 0, e.Event.distance.dist))
                                return;
                        }
                        else if (e.Event.distance.entry != 0)
                        {
                            var list = new List<GameObject>();
                            me.GetGameObjectListWithEntryInGrid(list, e.Event.distance.entry, e.Event.distance.dist);

                            if (!list.Empty())
                                gameobject = list.FirstOrDefault();
                        }

                        if (gameobject)
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
                case SmartEvents.SpellEffectHit:
                    ProcessAction(e, unit, var0);
                    break;
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
                    e.active = true;
                    break;
            }
        }

        void RecalcTimer(SmartScriptHolder e, uint min, uint max)
        {
            if (e.entryOrGuid == 15294 && e.timer != 0)
            {
                Log.outError(LogFilter.Server, "Called RecalcTimer");
            }
            // min/max was checked at loading!
            e.timer = RandomHelper.URand(min, max);
            e.active = e.timer == 0;
        }

        void UpdateTimer(SmartScriptHolder e, uint diff)
        {
            if (e.GetEventType() == SmartEvents.Link)
                return;

            if (e.Event.event_phase_mask != 0 && !IsInPhase(e.Event.event_phase_mask))
                return;

            if (e.GetEventType() == SmartEvents.UpdateIc && (me == null || !me.IsEngaged()))
                return;

            if (e.GetEventType() == SmartEvents.UpdateOoc && (me != null && me.IsEngaged()))//can be used with me=NULL (go script)
                return;

            if (e.timer < diff)
            {
                // delay spell cast event if another spell is being casted
                if (e.GetActionType() == SmartActions.Cast)
                {
                    if (!Convert.ToBoolean(e.Action.cast.castFlags & (uint)SmartCastFlags.InterruptPrevious))
                    {
                        if (me != null && me.HasUnitState(UnitState.Casting))
                        {
                            e.timer = 1;
                            return;
                        }
                    }
                }

                // Delay flee for assist event if stunned or rooted
                if (e.GetActionType() == SmartActions.FleeForAssist)
                {
                    if (me && me.HasUnitState(UnitState.Root | UnitState.LostControl))
                    {
                        e.timer = 1;
                        return;
                    }
                }

                e.active = true;//activate events with cooldown
                switch (e.GetEventType())//process ONLY timed events
                {
                    case SmartEvents.Update:
                    case SmartEvents.UpdateIc:
                    case SmartEvents.UpdateOoc:
                    case SmartEvents.HealthPct:
                    case SmartEvents.TargetHealthPct:
                    case SmartEvents.ManaPct:
                    case SmartEvents.TargetManaPct:
                    case SmartEvents.Range:
                    case SmartEvents.VictimCasting:
                    case SmartEvents.FriendlyHealth:
                    case SmartEvents.FriendlyIsCc:
                    case SmartEvents.FriendlyMissingBuff:
                    case SmartEvents.HasAura:
                    case SmartEvents.TargetBuffed:
                    case SmartEvents.IsBehindTarget:
                    case SmartEvents.FriendlyHealthPCT:
                    case SmartEvents.DistanceCreature:
                    case SmartEvents.DistanceGameobject:
                        {
                            ProcessEvent(e);
                            if (e.GetScriptType() == SmartScriptType.TimedActionlist)
                            {
                                e.enableTimed = false;//disable event if it is in an ActionList and was processed once
                                foreach (var holder in mTimedActionList)
                                {
                                    //find the first event which is not the current one and enable it
                                    if (holder.event_id > e.event_id)
                                    {
                                        holder.enableTimed = true;
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                }
            }
            else
            {
                e.timer -= diff;
                if (e.entryOrGuid == 15294 && me.GetGUID().GetCounter() == 55039 && e.timer != 0)
                {
                    Log.outError(LogFilter.Server, "Called UpdateTimer: reduce timer: e.timer: {0}, diff: {1}  current time: {2}", e.timer, diff, Time.GetMSTime());
                }
            }

        }

        bool CheckTimer(SmartScriptHolder e)
        {
            return e.active;
        }

        void InstallEvents()
        {
            if (!mInstallEvents.Empty())
            {
                foreach (var holder in mInstallEvents)
                    mEvents.Add(holder);//must be before UpdateTimers

                mInstallEvents.Clear();
            }
        }

        public void OnUpdate(uint diff)
        {
            if ((mScriptType == SmartScriptType.Creature || mScriptType == SmartScriptType.GameObject
                || mScriptType == SmartScriptType.AreaTriggerEntity || mScriptType == SmartScriptType.AreaTriggerEntityServerside)
                && !GetBaseObject())
                return;

            InstallEvents();//before UpdateTimers

            foreach (var holder in mEvents)
                UpdateTimer(holder, diff);

            if (!mStoredEvents.Empty())
                foreach (var holder in mStoredEvents)
                    UpdateTimer(holder, diff);

            var needCleanup = true;
            if (!mTimedActionList.Empty())
            {
                foreach (var holder in mTimedActionList)
                {
                    if (holder.enableTimed)
                    {
                        UpdateTimer(holder, diff);
                        needCleanup = false;
                    }
                }
            }
            if (needCleanup)
                mTimedActionList.Clear();

            if (!mRemIDs.Empty())
            {
                foreach (var id in mRemIDs)
                    RemoveStoredEvent(id);

                mRemIDs.Clear();
            }
            if (mUseTextTimer && me != null)
            {
                if (mTextTimer < diff)
                {
                    var textID = mLastTextID;
                    mLastTextID = 0;
                    var entry = mTalkerEntry;
                    mTalkerEntry = 0;
                    mTextTimer = 0;
                    mUseTextTimer = false;
                    ProcessEventsFor(SmartEvents.TextOver, null, textID, entry);
                }
                else mTextTimer -= diff;
            }
        }

        void FillScript(List<SmartScriptHolder> e, WorldObject obj, AreaTriggerRecord at, SceneTemplate scene, Spell spell = null)
        {
            if (e.Empty())
            {
                if (obj != null)
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript: EventMap for Entry {0} is empty but is using SmartScript.", obj.GetEntry());
                if (at != null)
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript: EventMap for AreaTrigger {0} is empty but is using SmartScript.", at.Id);
                if (scene != null)
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript: EventMap for SceneId {0} is empty but is using SmartScript.", scene.SceneId);
                if (spell != null)
                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript: EventMap for SpellId {0} is empty but is using SmartScript.", spell.GetSpellInfo().Id);
                return;
            }
            foreach (var holder in e)
            {
                if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.DifficultyAll))//if has instance flag add only if in it
                {
                    if (obj != null && obj.GetMap().IsDungeon())
                    {
                        // TODO: fix it for new maps and difficulties
                        switch (obj.GetMap().GetDifficultyID())
                        {
                            case Difficulty.Normal:
                            case Difficulty.Raid10N:
                                if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.Difficulty0))
                                    mEvents.Add(holder);
                                break;
                            case Difficulty.Heroic:
                            case Difficulty.Raid25N:
                                if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.Difficulty1))
                                    mEvents.Add(holder);
                                break;
                            case Difficulty.Raid10HC:
                                if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.Difficulty2))
                                    mEvents.Add(holder);
                                break;
                            case Difficulty.Raid25HC:
                                if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.Difficulty3))
                                    mEvents.Add(holder);
                                break;
                            default:
                                break;
                        }
                    }
                    continue;
                }
                mEvents.Add(holder);//NOTE: 'world(0)' events still get processed in ANY instance mode
            }
        }

        void GetScript()
        {
            List<SmartScriptHolder> e;
            if (me != null)
            {
                e = Global.SmartAIMgr.GetScript(-((int)me.GetSpawnId()), mScriptType);
                if (e.Empty())
                    e = Global.SmartAIMgr.GetScript((int)me.GetEntry(), mScriptType);
                FillScript(e, me, null, null);
            }
            else if (go != null)
            {
                e = Global.SmartAIMgr.GetScript(-((int)go.GetSpawnId()), mScriptType);
                if (e.Empty())
                    e = Global.SmartAIMgr.GetScript((int)go.GetEntry(), mScriptType);
                FillScript(e, go, null, null);
            }
            else if (trigger != null)
            {
                e = Global.SmartAIMgr.GetScript((int)trigger.Id, mScriptType);
                FillScript(e, null, trigger, null);
            }
            else if (areaTrigger != null)
            {
                e = Global.SmartAIMgr.GetScript((int)areaTrigger.GetEntry(), mScriptType);
                FillScript(e, areaTrigger, null, null);
            }
            else if (sceneTemplate != null)
            {
                e = Global.SmartAIMgr.GetScript((int)sceneTemplate.SceneId, mScriptType);
                FillScript(e, null, null, sceneTemplate);
            }
            else if (spellTemplate != null)
            {
                e = Global.SmartAIMgr.GetScript((int)spellTemplate.GetSpellInfo().Id, mScriptType);
                FillScript(e, null, null, null, spellTemplate);
            }
        }

        public void OnInitialize(WorldObject obj)
        {
            if (obj != null)
            {
                switch (obj.GetTypeId())
                {
                    case TypeId.Unit:
                        mScriptType = SmartScriptType.Creature;
                        me = obj.ToCreature();
                        Log.outDebug(LogFilter.Scripts, "SmartScript.OnInitialize: source is Creature {0}", me.GetEntry());
                        break;
                    case TypeId.GameObject:
                        mScriptType = SmartScriptType.GameObject;
                        go = obj.ToGameObject();
                        Log.outDebug(LogFilter.Scripts, "SmartScript.OnInitialize: source is GameObject {0}", go.GetEntry());
                        break;
                    case TypeId.AreaTrigger:
                        areaTrigger = obj.ToAreaTrigger();
                        mScriptType = areaTrigger.IsServerSide() ? SmartScriptType.AreaTriggerEntityServerside : SmartScriptType.AreaTriggerEntity;
                        Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.OnInitialize: source is AreaTrigger {areaTrigger.GetEntry()}, IsServerSide {areaTrigger.IsServerSide()}");
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

            foreach (var holder in mEvents)
                InitTimer(holder);//calculate timers for first time use

            ProcessEventsFor(SmartEvents.AiInit);
            InstallEvents();
            ProcessEventsFor(SmartEvents.JustCreated);
        }

        public void OnInitialize(AreaTriggerRecord at)
        {
            if (at != null)
            {
                mScriptType = SmartScriptType.AreaTrigger;
                trigger = at;
                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.OnInitialize: AreaTrigger {0}", trigger.Id);
            }
            else
            {
                Log.outError(LogFilter.ScriptsAi, "SmartScript.OnInitialize: !WARNING! Initialized AreaTrigger is Null.");
                return;
            }

            GetScript();//load copy of script

            foreach (var holder in mEvents)
                InitTimer(holder);//calculate timers for first time use

            InstallEvents();
        }

        public void OnInitialize(SceneTemplate scene)
        {
            if (scene != null)
            {
                mScriptType = SmartScriptType.Scene;
                sceneTemplate = scene;
                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.OnInitialize: Scene with id {0}", scene.SceneId);
            }
            else
            {
                Log.outError(LogFilter.ScriptsAi, "SmartScript.OnInitialize: !WARNING! Initialized Scene is Null.");
                return;
            }

            GetScript();//load copy of script

            foreach (var holder in mEvents)
                InitTimer(holder);//calculate timers for first time use

            InstallEvents();
        }

        public void OnInitialize(Spell spell)
        {
            if (spell != null)
            {
                mScriptType = SmartScriptType.Spell;
                spellTemplate = spell;
                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.OnInitialize: Spell id {0}", spell.GetSpellInfo().Id);
            }
            else
            {
                Log.outError(LogFilter.ScriptsAi, "SmartScript.OnInitialize: !WARNING! Initialized Spell is Null.");
                return;
            }

            GetScript();//load copy of script

            foreach (var holder in mEvents)
                InitTimer(holder);//calculate timers for first time use

            InstallEvents();
        }

        public void OnMoveInLineOfSight(Unit who)
        {
            if (me == null)
                return;

            ProcessEventsFor(me.IsEngaged() ? SmartEvents.IcLos : SmartEvents.OocLos, who);
        }

        Unit DoSelectLowestHpFriendly(float range, uint MinHPDiff)
        {
            if (!me)
                return null;

            var u_check = new MostHPMissingInRange<Unit>(me, range, MinHPDiff);
            var searcher = new UnitLastSearcher(me, u_check);
            Cell.VisitGridObjects(me, searcher, range);
            return searcher.GetTarget();
        }

        Unit DoSelectBelowHpPctFriendlyWithEntry(uint entry, float range, byte minHPDiff = 1, bool excludeSelf = true)
        {
            var u_check = new FriendlyBelowHpPctEntryInRange(me, entry, range, minHPDiff, excludeSelf);
            var searcher = new UnitLastSearcher(me, u_check);
            Cell.VisitAllObjects(me, searcher, range);

            return searcher.GetTarget();
        }

        void DoFindFriendlyCC(List<Creature> creatures, float range)
        {
            if (me == null)
                return;

            var u_check = new FriendlyCCedInRange(me, range);
            var searcher = new CreatureListSearcher(me, creatures, u_check);
            Cell.VisitGridObjects(me, searcher, range);
        }

        void DoFindFriendlyMissingBuff(List<Creature> creatures, float range, uint spellid)
        {
            if (me == null)
                return;

            var u_check = new FriendlyMissingBuffInRange(me, range, spellid);
            var searcher = new CreatureListSearcher(me, creatures, u_check);
            Cell.VisitGridObjects(me, searcher, range);
        }

        Unit DoFindClosestFriendlyInRange(float range)
        {
            if (!me)
                return null;

            var u_check = new AnyFriendlyUnitInObjectRangeCheck(me, me, range);
            var searcher = new UnitLastSearcher(me, u_check);
            Cell.VisitAllObjects(me, searcher, range);
            return searcher.GetTarget();
        }

        public void SetScript9(SmartScriptHolder e, uint entry)
        {
            mTimedActionList.Clear();
            mTimedActionList = Global.SmartAIMgr.GetScript((int)entry, SmartScriptType.TimedActionlist);
            if (mTimedActionList.Empty())
                return;
            var i = 0;
            foreach (var holder in mTimedActionList.ToList())
            {
                if (i++ == 0)
                {
                    holder.enableTimed = true;//enable processing only for the first action
                }
                else holder.enableTimed = false;

                if (e.Action.timedActionList.timerType == 1)
                    holder.Event.type = SmartEvents.UpdateIc;
                else if (e.Action.timedActionList.timerType > 1)
                    holder.Event.type = SmartEvents.Update;
                InitTimer(holder);
            }
        }

        Unit GetLastInvoker(Unit invoker = null)
        {
            // Look for invoker only on map of base object... Prevents multithreaded crashes
            var baseObject = GetBaseObject();
            if (baseObject != null)
                return Global.ObjAccessor.GetUnit(baseObject, mLastInvoker);
            // used for area triggers invoker cast
            else if (invoker != null)
                return Global.ObjAccessor.GetUnit(invoker, mLastInvoker);

            return null;
        }

        public void SetPathId(uint id) { mPathId = id; }
        public uint GetPathId() { return mPathId; }
        WorldObject GetBaseObject()
        {
            WorldObject obj = null;
            if (me != null)
                obj = me;
            else if (go != null)
                obj = go;
            else if (areaTrigger != null)
                obj = areaTrigger;

            return obj;
        }
        WorldObject GetBaseObjectOrUnit(Unit unit)
        {
            var summoner = GetBaseObject();

            if (!summoner && unit)
                return unit;

            return summoner;
        }

        bool IsUnit(WorldObject obj) { return obj != null && (obj.IsTypeId(TypeId.Unit) || obj.IsTypeId(TypeId.Player)); }
        public bool IsPlayer(WorldObject obj) { return obj != null && obj.IsTypeId(TypeId.Player); }
        bool IsCreature(WorldObject obj) { return obj != null && obj.IsTypeId(TypeId.Unit); }
        static bool IsCharmedCreature(WorldObject obj)
        {
            if (!obj)
                return false;

            var creatureObj = obj.ToCreature();
            if (creatureObj)
                return creatureObj.IsCharmed();

            return false;
        }
        bool IsGameObject(WorldObject obj) { return obj != null && obj.IsTypeId(TypeId.GameObject); }

        void StoreTargetList(List<WorldObject> targets, uint id)
        {
            // insert or replace
            _storedTargets.Remove(id);
            _storedTargets.Add(id, new ObjectGuidList(targets));
        }

        public List<WorldObject> GetStoredTargetList(uint id, WorldObject obj)
        {
            var list = _storedTargets.LookupByKey(id);
            if (list != null)
                return list.GetObjectList(obj);

            return null;
        }
        
        bool IsSmart(Creature c = null)
        {
            var smart = true;
            if (c != null && c.GetAIName() != "SmartAI")
                smart = false;

            if (me == null || me.GetAIName() != "SmartAI")
                smart = false;

            if (!smart)
                Log.outError(LogFilter.Sql, "SmartScript: Action target Creature (GUID: {0} Entry: {1}) is not using SmartAI, action skipped to prevent crash.", c != null ? c.GetSpawnId() : (me != null ? me.GetSpawnId() : 0), c != null ? c.GetEntry() : (me != null ? me.GetEntry() : 0));

            return smart;
        }

        bool IsSmartGO(GameObject g = null)
        {
            var smart = true;
            if (g != null && g.GetAIName() != "SmartGameObjectAI")
                smart = false;

            if (go == null || go.GetAIName() != "SmartGameObjectAI")
                smart = false;
            if (!smart)
                Log.outError(LogFilter.Sql, "SmartScript: Action target GameObject (GUID: {0} Entry: {1}) is not using SmartGameObjectAI, action skipped to prevent crash.", g != null ? g.GetSpawnId() : (go != null ? go.GetSpawnId() : 0), g != null ? g.GetEntry() : (go != null ? go.GetEntry() : 0));

            return smart;
        }

        void StoreCounter(uint id, uint value, uint reset)
        {
            if (mCounterList.ContainsKey(id))
            {
                if (reset == 0)
                    mCounterList[id] += value;
                else
                    mCounterList[id] = value;
            }
            else
                mCounterList.Add(id, value);

            ProcessEventsFor(SmartEvents.CounterSet, null, id);
        }

        uint GetCounterValue(uint id)
        {
            if (mCounterList.ContainsKey(id))
                return mCounterList[id];
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
            WorldObject lookupRoot = me;
            if (!lookupRoot)
                lookupRoot = go;

            if (lookupRoot)
            {
                if (!meOrigGUID.IsEmpty())
                {
                    var m = ObjectAccessor.GetCreature(lookupRoot, meOrigGUID);
                    if (m != null)
                    {
                        me = m;
                        go = null;
                        areaTrigger = null;
                    }
                }

                if (!goOrigGUID.IsEmpty())
                {
                    var o = ObjectAccessor.GetGameObject(lookupRoot, goOrigGUID);
                    if (o != null)
                    {
                        me = null;
                        go = o;
                        areaTrigger = null;
                    }
                }
            }
            goOrigGUID.Clear();
            meOrigGUID.Clear();
        }

        void IncPhase(uint p)
        {
            // protect phase from overflowing
            SetPhase(Math.Min((uint)SmartPhase.Phase12, mEventPhase + p));
        }

        void DecPhase(uint p)
        {
            if (p >= mEventPhase)
                SetPhase(0);
            else
                SetPhase(mEventPhase - p);
        }
        void SetPhase(uint p)
        {
            var oldPhase = mEventPhase;

            mEventPhase = p;

            if (oldPhase != mEventPhase)
                ProcessEventsFor(SmartEvents.PhaseChange);
        }
        bool IsInPhase(uint p)
        {
            if (mEventPhase == 0)
                return false;

            return ((1 << (int)(mEventPhase - 1)) & p) != 0;
        }

        void RemoveStoredEvent(uint id)
        {
            if (!mStoredEvents.Empty())
            {
                foreach (var holder in mStoredEvents)
                {
                    if (holder.event_id == id)
                    {
                        mStoredEvents.Remove(holder);
                        return;
                    }
                }
            }
        }

        public ObjectGuid mLastInvoker;

        Dictionary<uint, uint> mCounterList = new Dictionary<uint, uint>();

        List<SmartScriptHolder> mEvents = new List<SmartScriptHolder>();
        List<SmartScriptHolder> mInstallEvents = new List<SmartScriptHolder>();
        List<SmartScriptHolder> mTimedActionList = new List<SmartScriptHolder>();
        Creature me;
        ObjectGuid meOrigGUID;
        GameObject go;
        ObjectGuid goOrigGUID;
        AreaTriggerRecord trigger;
        AreaTrigger areaTrigger;
        SceneTemplate sceneTemplate;
        Spell spellTemplate;
        SmartScriptType mScriptType;
        uint mEventPhase;

        uint mPathId;
        List<SmartScriptHolder> mStoredEvents = new List<SmartScriptHolder>();
        List<uint> mRemIDs = new List<uint>();

        uint mTextTimer;
        uint mLastTextID;
        ObjectGuid mTextGUID;
        uint mTalkerEntry;
        bool mUseTextTimer;

        Dictionary<uint, ObjectGuidList> _storedTargets = new Dictionary<uint, ObjectGuidList>();

        SmartAITemplate mTemplate;
    }

    class ObjectGuidList
    {
        List<ObjectGuid> _guidList = new List<ObjectGuid>();
        List<WorldObject> _objectList = new List<WorldObject>();

        public ObjectGuidList(List<WorldObject> objectList)
        {
            _objectList = objectList;
            foreach (var obj in _objectList)
                _guidList.Add(obj.GetGUID());
        }

        public List<WorldObject> GetObjectList(WorldObject obj)
        {
            UpdateObjects(obj);
            return _objectList;
        }

        //sanitize vector using _guidVector
        void UpdateObjects(WorldObject obj)
        {
            _objectList.Clear();

            foreach (var guid in _guidList)
            {
                var newObj = Global.ObjAccessor.GetWorldObject(obj, guid);
                if (newObj != null)
                    _objectList.Add(newObj);
            }
        }
    }
}
