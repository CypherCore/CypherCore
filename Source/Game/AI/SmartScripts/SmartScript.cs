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
            mTargetStorage = new MultiMap<uint, WorldObject>();
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
                SmartEvents eventType = Event.GetEventType();
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

            Unit tempInvoker = GetLastInvoker();
            if (tempInvoker != null)
                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: Invoker: {0} (guidlow: {1})", tempInvoker.GetName(), tempInvoker.GetGUID().ToString());

            switch (e.GetActionType())
            {
                case SmartActions.Talk:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        Creature talker = me;
                        Player targetPlayer = null;
                        Unit talkTarget = null;

                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                if (IsCreature(obj) && !obj.ToCreature().IsPet()) // Prevented sending text to pets.
                                {
                                    if (e.Action.talk.useTalkTarget != 0)
                                        talkTarget = obj.ToCreature();
                                    else
                                        talker = obj.ToCreature();
                                    break;
                                }
                                else if (IsPlayer(obj))
                                {
                                    targetPlayer = obj.ToPlayer();
                                    break;
                                }
                            }
                        }

                        if (!talker)
                            break;

                        mTalkerEntry = talker.GetEntry();
                        mLastTextID = e.Action.talk.textGroupId;
                        mTextTimer = e.Action.talk.duration;

                        if (IsPlayer(GetLastInvoker())) // used for $vars in texts and whisper target
                            talkTarget = GetLastInvoker();
                        else if (targetPlayer != null)
                            talkTarget = targetPlayer;

                        mUseTextTimer = true;
                        Global.CreatureTextMgr.SendChat(talker, (byte)e.Action.talk.textGroupId, talkTarget);
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_TALK: talker: {0} (Guid: {1}), textGuid: {2}",
                            talker.GetName(), talker.GetGUID().ToString(), mTextGUID.ToString());
                        break;
                    }
                case SmartActions.SimpleTalk:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                if (IsCreature(obj))
                                    Global.CreatureTextMgr.SendChat(obj.ToCreature(), (byte)e.Action.talk.textGroupId, IsPlayer(GetLastInvoker()) ? GetLastInvoker() : null);
                                else if (IsPlayer(obj) && me != null)
                                {
                                    Unit templastInvoker = GetLastInvoker();
                                    Global.CreatureTextMgr.SendChat(me, (byte)e.Action.talk.textGroupId, IsPlayer(templastInvoker) ? templastInvoker : null, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, Team.Other, false, obj.ToPlayer());
                                }
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SIMPLE_TALK: talker: {0} (GuidLow: {1}), textGroupId: {2}",
                                    obj.GetName(), obj.GetGUID().ToString(), e.Action.talk.textGroupId);
                            }


                        }
                        break;
                    }
                case SmartActions.PlayEmote:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                if (IsUnit(obj))
                                {
                                    obj.ToUnit().HandleEmoteCommand((Emote)e.Action.emote.emoteId);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_PLAY_EMOTE: target: {0} (GuidLow: {1}), emote: {2}",
                                        obj.GetName(), obj.GetGUID().ToString(), e.Action.emote.emoteId);
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.Sound:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                if (IsUnit(obj))
                                {
                                    obj.PlayDirectSound(e.Action.sound.soundId, e.Action.sound.onlySelf != 0 ? obj.ToPlayer() : null);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SOUND: target: {0} (GuidLow: {1}), sound: {2}, onlyself: {3}",
                                        obj.GetName(), obj.GetGUID().ToString(), e.Action.sound.soundId, e.Action.sound.onlySelf);
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.SetFaction:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                if (IsCreature(obj))
                                {
                                    if (e.Action.faction.factionID != 0)
                                    {
                                        obj.ToCreature().SetFaction(e.Action.faction.factionID);
                                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_FACTION: Creature entry {0}, GuidLow {1} set faction to {2}",
                                            obj.GetEntry(), obj.GetGUID().ToString(), e.Action.faction.factionID);
                                    }
                                    else
                                    {
                                        CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate(obj.ToCreature().GetEntry());
                                        if (ci != null)
                                        {
                                            if (obj.ToCreature().getFaction() != ci.Faction)
                                            {
                                                obj.ToCreature().SetFaction(ci.Faction);
                                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_FACTION: Creature entry {0}, GuidLow {1} set faction to {2}",
                                                    obj.GetEntry(), obj.GetGUID().ToString(), ci.Faction);
                                            }
                                        }
                                    }
                                }
                            }


                        }
                        break;
                    }
                case SmartActions.MorphToEntryOrModel:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (!IsCreature(obj))
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
                                        obj.ToCreature().SetDisplayId(model.CreatureDisplayID, model.DisplayScale);
                                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} set displayid to {2}",
                                            obj.GetEntry(), obj.GetGUID().ToString(), model.CreatureDisplayID);
                                    }
                                }
                                //if no param1, then use value from param2 (modelId)
                                else
                                {
                                    obj.ToCreature().SetDisplayId(e.Action.morphOrMount.model);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} set displayid to {2}",
                                        obj.GetEntry(), obj.GetGUID().ToString(), e.Action.morphOrMount.model);
                                }
                            }
                            else
                            {
                                obj.ToCreature().DeMorph();
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} demorphs.",
                                    obj.GetEntry(), obj.GetGUID().ToString());
                            }
                        }
                        break;
                    }
                case SmartActions.FailQuest:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsPlayer(obj))
                            {
                                obj.ToPlayer().FailQuest(e.Action.quest.questId);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_FAIL_QUEST: Player guidLow {0} fails quest {1}",
                                    obj.GetGUID().ToString(), e.Action.quest.questId);
                            }
                        }


                        break;
                    }
                case SmartActions.OfferQuest:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            Player pTarget = obj.ToPlayer();
                            if (pTarget)
                            {
                                Quest q = Global.ObjectMgr.GetQuestTemplate(e.Action.questOffer.questId);
                                if (q != null)
                                {
                                    if (me && e.Action.questOffer.directAdd == 0)
                                    {
                                        if (pTarget.CanTakeQuest(q, true))
                                        {
                                            WorldSession session = pTarget.GetSession();
                                            if (session)
                                            {
                                                PlayerMenu menu = new PlayerMenu(session);
                                                menu.SendQuestGiverQuestDetails(q, me.GetGUID(), true, false);
                                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction:: SMART_ACTION_OFFER_QUEST: Player {0} - offering quest {1}", pTarget.GetGUID().ToString(), e.Action.questOffer.questId);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        pTarget.AddQuestAndCheckCompletion(q, null);
                                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_ADD_QUEST: Player {0} add quest {1}", pTarget.GetGUID().ToString(), e.Action.questOffer.questId);
                                    }
                                }
                            }
                        }


                        break;
                    }
                case SmartActions.SetReactState:
                    {
                        if (me == null)
                            break;

                        me.SetReactState((ReactStates)e.Action.react.state);
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_REACT_STATE: Creature guidLow {0} set reactstate {1}",
                            me.GetGUID().ToString(), e.Action.react.state);
                        break;
                    }
                case SmartActions.RandomEmote:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        uint[] emotes =
                        {
                            e.Action.randomEmote.emote1,
                            e.Action.randomEmote.emote2,
                            e.Action.randomEmote.emote3,
                            e.Action.randomEmote.emote4,
                            e.Action.randomEmote.emote5,
                            e.Action.randomEmote.emote6,
                        };
                        uint[] temp = new uint[SharedConst.SmartActionParamCount];
                        int count = 0;
                        for (byte i = 0; i < SharedConst.SmartActionParamCount; i++)
                        {
                            if (emotes[i] != 0)
                            {
                                temp[count] = emotes[i];
                                ++count;
                            }
                        }

                        foreach (var obj in targets)
                        {
                            if (IsUnit(obj))
                            {
                                uint emote = temp[RandomHelper.IRand(0, count)];
                                obj.ToUnit().HandleEmoteCommand((Emote)emote);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_RANDOM_EMOTE: Creature guidLow {0} handle random emote {1}",
                                    obj.GetGUID().ToString(), emote);
                            }
                        }


                        break;
                    }
                case SmartActions.ThreatAllPct:
                    {
                        if (me == null)
                            break;

                        var threatList = me.GetThreatManager().getThreatList();
                        foreach (var refe in threatList)
                        {
                            Unit target = Global.ObjAccessor.GetUnit(me, refe.getUnitGuid());
                            if (target != null)
                            {
                                me.GetThreatManager().modifyThreatPercent(target, e.Action.threatPCT.threatINC != 0 ? (int)e.Action.threatPCT.threatINC : -(int)e.Action.threatPCT.threatDEC);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_THREAT_ALL_PCT: Creature guidLow {0} modify threat for unit {1}, value {2}",
                                    me.GetGUID().ToString(), target.GetGUID().ToString(), e.Action.threatPCT.threatINC != 0 ? (int)e.Action.threatPCT.threatINC : -(int)e.Action.threatPCT.threatDEC);
                            }
                        }
                        break;
                    }
                case SmartActions.ThreatSinglePct:
                    {
                        if (me == null)
                            break;

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsUnit(obj))
                            {
                                me.GetThreatManager().modifyThreatPercent(obj.ToUnit(), e.Action.threatPCT.threatINC != 0 ? (int)e.Action.threatPCT.threatINC : -(int)e.Action.threatPCT.threatDEC);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_THREAT_SINGLE_PCT: Creature guidLow {0} modify threat for unit {1}, value {2}",
                                    me.GetGUID().ToString(), obj.GetGUID().ToString(), e.Action.threatPCT.threatINC != 0 ? (int)e.Action.threatPCT.threatINC : -(int)e.Action.threatPCT.threatDEC);
                            }
                        }


                        break;
                    }
                case SmartActions.CallAreaexploredoreventhappens:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            // Special handling for vehicles
                            if (IsUnit(obj))
                            {
                                Vehicle vehicle = obj.ToUnit().GetVehicleKit();
                                if (vehicle != null)
                                    foreach (var seat in vehicle.Seats)
                                    {
                                        Player player = Global.ObjAccessor.FindPlayer(seat.Value.Passenger.Guid);
                                        if (player != null)
                                            player.AreaExploredOrEventHappens(e.Action.quest.questId);
                                    }
                            }

                            if (IsPlayer(obj))
                            {
                                obj.ToPlayer().AreaExploredOrEventHappens(e.Action.quest.questId);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_CALL_AREAEXPLOREDOREVENTHAPPENS: {0} credited quest {1}",
                                    obj.GetGUID().ToString(), e.Action.quest.questId);
                            }
                        }


                        break;
                    }
                case SmartActions.Cast:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (!IsUnit(obj))
                                continue;

                            if (!e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !obj.ToUnit().HasAura(e.Action.cast.spell))
                            {
                                TriggerCastFlags triggerFlag = TriggerCastFlags.None;
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

                                        bool _allowMove = false;
                                        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(e.Action.cast.spell);
                                        var costs = spellInfo.CalcPowerCost(me, spellInfo.GetSchoolMask());
                                        bool hasPower = true;
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

                                        if (me.GetDistance(obj) > spellInfo.GetMaxRange(true) ||
                                            me.GetDistance(obj) < spellInfo.GetMinRange(true) ||
                                            !me.IsWithinLOSInMap(obj) || !hasPower)
                                            _allowMove = true;

                                        ((SmartAI)me.GetAI()).SetCombatMove(_allowMove);
                                    }

                                    me.CastSpell(obj.ToUnit(), e.Action.cast.spell, triggerFlag);
                                }
                                else if (go)
                                    go.CastSpell(obj.ToUnit(), e.Action.cast.spell, triggerFlag);
                                else if (obj != null)
                                    obj.ToUnit().CastSpell(obj.ToUnit(), e.Action.cast.spell);

                                //Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_CAST. Creature {0} casts spell {1} on target {2} with castflags {3}",
                                   // me.GetGUID().ToString(), e.Action.cast.spell, obj.GetGUID().ToString(), e.Action.cast.castFlags);
                            }
                            else
                                Log.outDebug(LogFilter.ScriptsAi, "Spell {0} not casted because it has flag SMARTCAST_AURA_NOT_PRESENT and the target (Guid: {1} Entry: {2} Type: {3}) already has the aura",
                                    e.Action.cast.spell, obj.GetGUID(), obj.GetEntry(), obj.GetTypeId());
                        }
                        break;
                    }
                case SmartActions.InvokerCast:
                    {
                        Unit tempLastInvoker = GetLastInvoker();
                        if (tempLastInvoker == null)
                            break;

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (!IsUnit(obj))
                                continue;

                            if (!e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !obj.ToUnit().HasAura(e.Action.cast.spell))
                            {

                                if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                                    tempLastInvoker.InterruptNonMeleeSpells(false);

                                TriggerCastFlags triggerFlag = TriggerCastFlags.None;
                                if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                                {
                                    if (e.Action.cast.triggerFlags != 0)
                                        triggerFlag = (TriggerCastFlags)e.Action.cast.triggerFlags;
                                    else
                                        triggerFlag = TriggerCastFlags.FullMask;
                                }

                                tempLastInvoker.CastSpell(obj.ToUnit(), e.Action.cast.spell, triggerFlag);

                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_INVOKER_CAST: Invoker {0} casts spell {1} on target {2} with castflags {3}",
                                    tempLastInvoker.GetGUID().ToString(), e.Action.cast.spell, obj.GetGUID().ToString(), e.Action.cast.castFlags);
                            }
                            else
                                Log.outDebug(LogFilter.ScriptsAi, "Spell {0} not cast because it has flag SMARTCAST_AURA_NOT_PRESENT and the target ({1}) already has the aura", e.Action.cast.spell, obj.GetGUID().ToString());
                        }
                        break;
                    }
                case SmartActions.AddAura:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsUnit(obj))
                            {
                                obj.ToUnit().AddAura(e.Action.cast.spell, obj.ToUnit());
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_ADD_AURA: Adding aura {0} to unit {1}",
                                    e.Action.cast.spell, obj.GetGUID().ToString());
                            }
                        }


                        break;
                    }
                case SmartActions.ActivateGobject:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsGameObject(obj))
                            {
                                // Activate
                                obj.ToGameObject().SetLootState(LootState.Ready);
                                obj.ToGameObject().UseDoorOrButton(0, false, unit);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_ACTIVATE_GOBJECT. Gameobject {0} (entry: {1}) activated",
                                    obj.GetGUID().ToString(), obj.GetEntry());
                            }
                        }


                        break;
                    }
                case SmartActions.ResetGobject:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsGameObject(obj))
                            {
                                obj.ToGameObject().ResetDoorOrButton();
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_RESET_GOBJECT. Gameobject {0} (entry: {1}) reset",
                                    obj.GetGUID().ToString(), obj.GetEntry());
                            }
                        }


                        break;
                    }
                case SmartActions.SetEmoteState:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsUnit(obj))
                            {
                                obj.ToUnit().SetUInt32Value(UnitFields.NpcEmotestate, e.Action.emote.emoteId);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_EMOTE_STATE. Unit {0} set emotestate to {1}",
                                    obj.GetGUID().ToString(), e.Action.emote.emoteId);
                            }
                        }


                        break;
                    }
                case SmartActions.SetUnitFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsUnit(obj))
                            {
                                if (e.Action.unitFlag.type == 0)
                                {
                                    obj.ToUnit().SetFlag(UnitFields.Flags, e.Action.unitFlag.flag);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_UNIT_FLAG. Unit {0} added flag {1} to UNIT_FIELD_FLAGS",
                                    obj.GetGUID().ToString(), e.Action.unitFlag.flag);
                                }
                                else
                                {
                                    obj.ToUnit().SetFlag(UnitFields.Flags2, e.Action.unitFlag.flag);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SET_UNIT_FLAG. Unit {0} added flag {1} to UNIT_FIELD_FLAGS_2",
                                    obj.GetGUID().ToString(), e.Action.unitFlag.flag);
                                }
                            }
                        }


                        break;
                    }
                case SmartActions.RemoveUnitFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsUnit(obj))
                            {
                                if (e.Action.unitFlag.type == 0)
                                {
                                    obj.ToUnit().RemoveFlag(UnitFields.Flags2, e.Action.unitFlag.flag);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_REMOVE_UNIT_FLAG. Unit {0} removed flag {1} to UNIT_FIELD_FLAGS",
                                    obj.GetGUID().ToString(), e.Action.unitFlag.flag);
                                }
                                else
                                {
                                    obj.ToUnit().RemoveFlag(UnitFields.Flags2, e.Action.unitFlag.flag);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_REMOVE_UNIT_FLAG. Unit {0} removed flag {1} to UNIT_FIELD_FLAGS_2",
                                    obj.GetGUID().ToString(), e.Action.unitFlag.flag);
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

                        bool move = e.Action.combatMove.move != 0 ? true : false;
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

                        IncPhase((int)e.Action.incEventPhase.inc);
                        DecPhase((int)e.Action.incEventPhase.dec);
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
                        if (e.Action.flee.withEmote != 0)
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

                        if (IsPlayer(unit) && GetBaseObject() != null)
                        {
                            unit.ToPlayer().GroupEventHappens(e.Action.quest.questId, GetBaseObject());
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_CALL_GROUPEVENTHAPPENS: Player {0}, group credit for quest {1}",
                                unit.GetGUID().ToString(), e.Action.quest.questId);
                        }

                        // Special handling for vehicles
                        Vehicle vehicle = unit.GetVehicleKit();
                        if (vehicle != null)
                        {
                            foreach (var seat in vehicle.Seats)
                            {
                                Player player = Global.ObjAccessor.FindPlayer(seat.Value.Passenger.Guid);
                                if (player != null)
                                    player.GroupEventHappens(e.Action.quest.questId, GetBaseObject());
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
                case SmartActions.Removeaurasfromspell:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (!IsUnit(obj))
                                continue;

                            if (e.Action.removeAura.spell == 0)
                                obj.ToUnit().RemoveAllAuras();
                            else
                                obj.ToUnit().RemoveAurasDueToSpell(e.Action.removeAura.spell);

                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_REMOVEAURASFROMSPELL: Unit {0}, spell {1}",
                                obj.GetGUID().ToString(), e.Action.removeAura.spell);
                        }


                        break;
                    }
                case SmartActions.Follow:
                    {
                        if (!IsSmart())
                            break;

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsUnit(obj))
                            {
                                ((SmartAI)me.GetAI()).SetFollow(obj.ToUnit(), e.Action.follow.dist, e.Action.follow.angle, e.Action.follow.credit, e.Action.follow.entry, e.Action.follow.creditType);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_FOLLOW: Creature {0} following target {1}",
                                    me.GetGUID().ToString(), obj.GetGUID().ToString());
                                break;
                            }
                        }


                        break;
                    }
                case SmartActions.RandomPhase:
                    {
                        if (GetBaseObject() == null)
                            break;

                        uint[] phases =
                        {
                            e.Action.randomPhase.phase1,
                            e.Action.randomPhase.phase2,
                            e.Action.randomPhase.phase3,
                            e.Action.randomPhase.phase4,
                            e.Action.randomPhase.phase5,
                            e.Action.randomPhase.phase6,
                        };
                        uint[] temp = new uint[SharedConst.SmartActionParamCount];
                        uint count = 0;
                        for (byte i = 0; i < SharedConst.SmartActionParamCount; i++)
                        {
                            if (phases[i] > 0)
                            {
                                temp[count] = phases[i];
                                ++count;
                            }
                        }

                        uint phase = temp[RandomHelper.IRand(0, (int)count)];
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
                            if (me == null)
                                break;

                            Player player = me.GetLootRecipient();
                            if (player != null)
                            {
                                player.RewardPlayerAndGroupAtEvent(e.Action.killedMonster.creature, player);
                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_CALL_KILLEDMONSTER: Player {0}, Killcredit: {1}",
                                    player.GetGUID().ToString(), e.Action.killedMonster.creature);
                            }
                        }
                        else // Specific target type
                        {
                            List<WorldObject> targets = GetTargets(e, unit);
                            if (targets.Empty())
                                break;

                            foreach (var obj in targets)
                            {
                                if (IsPlayer(obj))
                                {
                                    obj.ToPlayer().KilledMonsterCredit(e.Action.killedMonster.creature);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_CALL_KILLEDMONSTER: Player {0}, Killcredit: {1}",
                                        obj.GetGUID().ToString(), e.Action.killedMonster.creature);
                                }
                                else if (IsUnit(obj)) // Special handling for vehicles
                                {
                                    Vehicle vehicle = obj.ToUnit().GetVehicleKit();
                                    if (vehicle != null)
                                    {
                                        foreach (var seat in vehicle.Seats)
                                        {
                                            Player player = Global.ObjAccessor.FindPlayer(seat.Value.Passenger.Guid);
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
                        WorldObject obj = GetBaseObject();
                        if (obj == null)
                            obj = unit;

                        if (obj == null)
                            break;

                        InstanceScript instance = obj.GetInstanceScript();
                        if (instance == null)
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: Event {0} attempt to set instance data without instance script. EntryOrGuid {1}", e.GetEventType(), e.entryOrGuid);
                            break;
                        }

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        instance.SetGuidData(e.Action.setInstanceData64.field, targets.First().GetGUID());
                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA64: Field: {0}, data: {1}",
                            e.Action.setInstanceData64.field, targets.First().GetGUID());
                        break;
                    }
                case SmartActions.UpdateTemplate:
                    {
                        var targets = GetTargets(e, unit);

                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsCreature(obj))
                                obj.ToCreature().UpdateEntry(e.Action.updateTemplate.creature, null, e.Action.updateTemplate.updateLevel != 0);
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
                        if (me != null)
                        {
                            me.SetInCombatWithZone();
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_SET_IN_COMBAT_WITH_ZONE: Creature {0}", me.GetGUID().ToString());
                        }
                        break;
                    }
                case SmartActions.CallForHelp:
                    {
                        if (me != null)
                        {
                            me.CallForHelp(e.Action.callHelp.range);
                            if (e.Action.callHelp.withEmote != 0)
                            {
                                var builder = new BroadcastTextBuilder(me, ChatMsg.Emote, (uint)BroadcastTextIds.CallForHelp, me.GetGender());
                                Global.CreatureTextMgr.SendChatPacket(me, builder, ChatMsg.MonsterEmote);
                            }
                            Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_CALL_FOR_HELP: Creature {0}", me.GetGUID().ToString());
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
                        var targets = GetTargets(e, unit);

                        if (targets == null)
                            break;

                        foreach (var obj in targets)
                        {
                            if (obj.IsTypeId(TypeId.Unit))
                            {
                                Creature target = obj.ToCreature();
                                if (target)
                                    target.DespawnOrUnsummon(e.Action.forceDespawn.delay, TimeSpan.FromSeconds(e.Action.forceDespawn.respawn));
                            }
                            else if (obj.IsTypeId(TypeId.GameObject))
                            {
                                GameObject goTarget = obj.ToGameObject();
                                if (goTarget)
                                    goTarget.SetRespawnTime((int)e.Action.forceDespawn.delay + 1);
                            }
                        }

                        break;
                    }
                case SmartActions.SetIngamePhaseId:
                    {
                        var targets = GetTargets(e, unit);

                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (e.Action.ingamePhaseId.apply == 1)
                                PhasingHandler.AddPhase(obj, e.Action.ingamePhaseId.id, true);
                            else
                                PhasingHandler.RemovePhase(obj, e.Action.ingamePhaseId.id, true);
                        }

                        break;
                    }
                case SmartActions.SetIngamePhaseGroup:
                    {
                        var targets = GetTargets(e, unit);

                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (e.Action.ingamePhaseGroup.apply == 1)
                                PhasingHandler.AddPhaseGroup(obj, e.Action.ingamePhaseGroup.groupId, true);
                            else
                                PhasingHandler.RemovePhaseGroup(obj, e.Action.ingamePhaseGroup.groupId, true);
                        }

                        break;
                    }
                case SmartActions.MountToEntryOrModel:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (!IsUnit(obj))
                                continue;

                            if (e.Action.morphOrMount.creature != 0 || e.Action.morphOrMount.model != 0)
                            {
                                if (e.Action.morphOrMount.creature > 0)
                                {
                                    CreatureTemplate cInfo = Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature);
                                    if (cInfo != null)
                                        obj.ToUnit().Mount(ObjectManager.ChooseDisplayId(cInfo).CreatureDisplayID);
                                }
                                else
                                    obj.ToUnit().Mount(e.Action.morphOrMount.model);
                            }
                            else
                                obj.ToUnit().Dismount();
                        }


                        break;
                    }
                case SmartActions.SetInvincibilityHpLevel:
                    {
                        if (me == null)
                            break;

                        SmartAI ai = ((SmartAI)me.GetAI());

                        if (ai == null)
                            break;

                        if (e.Action.invincHP.percent != 0)
                            ai.SetInvincibilityHpLevel((uint)me.CountPctFromMaxHealth((int)e.Action.invincHP.percent));
                        else
                            ai.SetInvincibilityHpLevel(e.Action.invincHP.minHP);
                        break;
                    }
                case SmartActions.SetData:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsCreature(obj))
                                obj.ToCreature().GetAI().SetData(e.Action.setData.field, e.Action.setData.data);
                            else if (IsGameObject(obj))
                                obj.ToGameObject().GetAI().SetData(e.Action.setData.field, e.Action.setData.data);
                        }


                        break;
                    }
                case SmartActions.MoveOffset:
                    {
                        var targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                if (!IsCreature(obj))
                                    continue;

                                if (!e.Event.event_flags.HasAnyFlag(SmartEventFlags.WhileCharmed) && IsCharmedCreature(obj))
                                    continue;

                                Position pos = obj.GetPosition();

                                // Use forward/backward/left/right cartesian plane movement
                                float o = pos.GetOrientation();
                                float x = (float)(pos.GetPositionX() + (Math.Cos(o - (Math.PI / 2)) * e.Target.x) + (Math.Cos(o) * e.Target.y));
                                float y = (float)(pos.GetPositionY() + (Math.Sin(o - (Math.PI / 2)) * e.Target.x) + (Math.Sin(o) * e.Target.y));
                                float z = pos.GetPositionZ() + e.Target.z;
                                obj.ToCreature().GetMotionMaster().MovePoint(EventId.SmartRandomPoint, x, y, z);
                            }
                        }
                        break;
                    }
                case SmartActions.SetVisibility:
                    {
                        if (me != null)
                            me.SetVisible(Convert.ToBoolean(e.Action.visibility.state));
                        break;
                    }
                case SmartActions.SetActive:
                    {
                        WorldObject baseObj = GetBaseObject();
                        if (baseObj != null)
                            baseObj.setActive(e.Action.active.state != 0);
                        break;
                    }
                case SmartActions.AttackStart:
                    {
                        if (me == null)
                            break;

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsUnit(obj))
                            {
                                me.GetAI().AttackStart(obj.ToUnit());
                                break;
                            }
                        }


                        break;
                    }
                case SmartActions.SummonCreature:
                    {
                        WorldObject summoner = GetBaseObjectOrUnit(unit);
                        if (!summoner)
                            break;

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            float x, y, z, o;
                            foreach (var obj in targets)
                            {
                                obj.GetPosition(out x, out y, out z, out o);
                                x += e.Target.x;
                                y += e.Target.y;
                                z += e.Target.z;
                                o += e.Target.o;
                                Creature summon = summoner.SummonCreature(e.Action.summonCreature.creature, x, y, z, o, (TempSummonType)e.Action.summonCreature.type, e.Action.summonCreature.duration);
                                if (summon != null)
                                    if (e.Action.summonCreature.attackInvoker != 0)
                                        summon.GetAI().AttackStart(obj.ToUnit());
                            }
                        }

                        if (e.GetTargetType() != SmartTargets.Position)
                            break;

                        Creature summon1 = summoner.SummonCreature(e.Action.summonCreature.creature, e.Target.x, e.Target.y, e.Target.z, e.Target.o, (TempSummonType)e.Action.summonCreature.type, e.Action.summonCreature.duration);
                        if (summon1 != null)
                            if (unit != null && e.Action.summonCreature.attackInvoker != 0)
                                summon1.GetAI().AttackStart(unit);
                        break;
                    }
                case SmartActions.SummonGo:
                    {
                        WorldObject summoner = GetBaseObjectOrUnit(unit);
                        if (!summoner)
                            break;

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                if (!IsUnit(obj))
                                    continue;

                                Position pos = obj.GetPositionWithOffset(new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o));
                                Quaternion rot = Quaternion.fromEulerAnglesZYX(pos.GetOrientation(), 0.0f, 0.0f);
                                summoner.SummonGameObject(e.Action.summonGO.entry, pos, rot, e.Action.summonGO.despawnTime);
                            }
                        }

                        if (e.GetTargetType() != SmartTargets.Position)
                            break;

                        Quaternion rot1 = Quaternion.fromEulerAnglesZYX(e.Target.o, 0.0f, 0.0f);
                        summoner.SummonGameObject(e.Action.summonGO.entry, new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o), rot1, e.Action.summonGO.despawnTime);
                        break;
                    }
                case SmartActions.KillUnit:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (!IsUnit(obj))
                                continue;

                            obj.ToUnit().KillSelf();
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
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (!IsPlayer(obj))
                                continue;

                            obj.ToPlayer().AddItem(e.Action.item.entry, e.Action.item.count);
                        }
                        break;
                    }
                case SmartActions.RemoveItem:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (!IsPlayer(obj))
                                continue;

                            obj.ToPlayer().DestroyItemCount(e.Action.item.entry, e.Action.item.count, true);
                        }
                        break;
                    }
                case SmartActions.StoreTargetList:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        StoreTargetList(targets, e.Action.storeTargets.id);
                        break;
                    }
                case SmartActions.Teleport:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsPlayer(obj))
                                obj.ToPlayer().TeleportTo(e.Action.teleport.mapID, e.Target.x, e.Target.y, e.Target.z, e.Target.o);
                            else if (IsCreature(obj))
                                obj.ToCreature().NearTeleportTo(e.Target.x, e.Target.y, e.Target.z, e.Target.o);
                        }


                        break;
                    }
                case SmartActions.SetFly:
                    {
                        if (!IsSmart())
                            break;

                        ((SmartAI)me.GetAI()).SetFly(e.Action.setFly.fly != 0 ? true : false);
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
                        var targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                if (IsCreature(obj))
                                {
                                    SmartAI ai = (SmartAI)obj.ToCreature().GetAI();
                                    if (ai != null)
                                        ai.GetScript().StoreCounter(e.Action.setCounter.counterId, e.Action.setCounter.value, e.Action.setCounter.reset);
                                    else
                                        Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SET_COUNTER is not using SmartAI, skipping");
                                }
                                else if (IsGameObject(obj))
                                {
                                    SmartGameObjectAI ai = (SmartGameObjectAI)obj.ToGameObject().GetAI();
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

                        bool run = e.Action.wpStart.run != 0 ? true : false;
                        uint entry = e.Action.wpStart.pathID;
                        bool repeat = e.Action.wpStart.repeat != 0 ? true : false;
                        List<WorldObject> targets = GetTargets(e, unit);
                        StoreTargetList(targets, SharedConst.SmartEscortTargets);
                        me.SetReactState((ReactStates)e.Action.wpStart.reactState);
                        ((SmartAI)me.GetAI()).StartPath(run, entry, repeat, unit);

                        uint quest = e.Action.wpStart.quest;
                        uint DespawnTime = e.Action.wpStart.despawnTime;
                        ((SmartAI)me.GetAI()).mEscortQuestID = quest;
                        ((SmartAI)me.GetAI()).SetDespawnTime(DespawnTime);
                        break;
                    }
                case SmartActions.WpPause:
                    {
                        if (!IsSmart())
                            break;

                        uint delay = e.Action.wpPause.delay;
                        ((SmartAI)me.GetAI()).PausePath(delay, e.GetEventType() != SmartEvents.WaypointReached);
                        break;
                    }
                case SmartActions.WpStop:
                    {
                        if (!IsSmart())
                            break;

                        uint DespawnTime = e.Action.wpStop.despawnTime;
                        uint quest = e.Action.wpStop.quest;
                        bool fail = e.Action.wpStop.fail != 0 ? true : false;
                        ((SmartAI)me.GetAI()).StopPath(DespawnTime, quest, fail);
                        break;
                    }
                case SmartActions.WpResume:
                    {
                        if (!IsSmart())
                            break;

                        ((SmartAI)me.GetAI()).ResumePath();
                        break;
                    }
                case SmartActions.SetOrientation:
                    {
                        if (me == null)
                            break;
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (e.GetTargetType() == SmartTargets.Self)
                            me.SetFacingTo((me.GetTransport() ? me.GetTransportHomePosition() : me.GetHomePosition()).GetOrientation());
                        else if (e.GetTargetType() == SmartTargets.Position)
                            me.SetFacingTo(e.Target.o);
                        else if (!targets.Empty())
                        {
                            me.SetFacingToObject(targets.First());
                        }

                        break;
                    }
                case SmartActions.Playmovie:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (!IsPlayer(obj))
                                continue;

                            obj.ToPlayer().SendMovieStart(e.Action.movie.entry);
                        }


                        break;
                    }
                case SmartActions.MoveToPos:
                    {
                        if (!IsSmart())
                            break;

                        WorldObject target = null;

                        if (e.GetTargetType() == SmartTargets.CreatureRange || e.GetTargetType() == SmartTargets.CreatureGuid ||
                            e.GetTargetType() == SmartTargets.CreatureDistance || e.GetTargetType() == SmartTargets.GameobjectRange ||
                            e.GetTargetType() == SmartTargets.GameobjectGuid || e.GetTargetType() == SmartTargets.GameobjectDistance ||
                            e.GetTargetType() == SmartTargets.ClosestCreature || e.GetTargetType() == SmartTargets.ClosestGameobject ||
                            e.GetTargetType() == SmartTargets.OwnerOrSummoner || e.GetTargetType() == SmartTargets.ActionInvoker ||
                            e.GetTargetType() == SmartTargets.ClosestEnemy || e.GetTargetType() == SmartTargets.ClosestFriendly)
                        {
                            List<WorldObject> targets = GetTargets(e, unit);
                            if (targets.Empty())
                                break;

                            target = targets.First();
                        }

                        if (!target)
                        {
                            float x = e.Target.x;
                            float y = e.Target.y;
                            float z = e.Target.z;
                            float o = 0;
                            if (e.Action.moveToPos.transport != 0)
                            {
                                ITransport trans = me.GetDirectTransport();
                                if (trans != null)
                                    trans.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                            }

                            me.GetMotionMaster().MovePoint(e.Action.moveToPos.pointId, x, y, z, e.Action.moveToPos.disablePathfinding == 0);
                        }
                        else
                        {
                            float x, y, z;
                            target.GetPosition(out x, out y, out z);
                            if (e.Action.moveToPos.contactDistance > 0)
                                target.GetContactPoint(me, out x, out y, out z, e.Action.moveToPos.contactDistance);
                            me.GetMotionMaster().MovePoint(e.Action.moveToPos.pointId, x, y, z, e.Action.moveToPos.disablePathfinding == 0);
                        }
                        break;
                    }
                case SmartActions.RespawnTarget:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsCreature(obj))
                                obj.ToCreature().Respawn();
                            else if (IsGameObject(obj))
                                obj.ToGameObject().SetRespawnTime((int)e.Action.respawnTarget.goRespawnTime);
                        }


                        break;
                    }
                case SmartActions.CloseGossip:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsPlayer(obj))
                                obj.ToPlayer().PlayerTalkClass.SendCloseGossip();


                        break;
                    }
                case SmartActions.Equip:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            Creature npc = obj.ToCreature();
                            if (npc != null)
                            {
                                EquipmentItem[] slot = new EquipmentItem[3];
                                sbyte equipId = (sbyte)e.Action.equip.entry;
                                if (equipId != 0)
                                {
                                    EquipmentInfo einfo = Global.ObjectMgr.GetEquipmentInfo(npc.GetEntry(), equipId);
                                    if (einfo == null)
                                    {
                                        Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_EQUIP uses non-existent equipment info id {0} for creature {1}", equipId, npc.GetEntry());
                                        break;
                                    }

                                    npc.SetCurrentEquipmentId((byte)equipId);
                                    slot[0] = einfo.Items[0];
                                    slot[1] = einfo.Items[1];
                                    slot[2] = einfo.Items[2];
                                }
                                else
                                {
                                    slot[0].ItemId = e.Action.equip.slot1;
                                    slot[1].ItemId = e.Action.equip.slot2;
                                    slot[2].ItemId = e.Action.equip.slot3;
                                }
                                if (e.Action.equip.mask == 0 || Convert.ToBoolean(e.Action.equip.mask & 1))
                                    npc.SetVirtualItem(0, slot[0].ItemId, slot[0].AppearanceModId, slot[0].ItemVisual);
                                if (e.Action.equip.mask == 0 || Convert.ToBoolean(e.Action.equip.mask & 2))
                                    npc.SetVirtualItem(1, slot[1].ItemId, slot[1].AppearanceModId, slot[1].ItemVisual);
                                if (e.Action.equip.mask == 0 || Convert.ToBoolean(e.Action.equip.mask & 4))
                                    npc.SetVirtualItem(2, slot[2].ItemId, slot[2].AppearanceModId, slot[2].ItemVisual);
                            }
                        }
                        break;
                    }
                case SmartActions.CreateTimedEvent:
                    {
                        SmartEvent ne = new SmartEvent();
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

                        SmartAction ac = new SmartAction();
                        ac.type = SmartActions.TriggerTimedEvent;
                        ac.timeEvent.id = e.Action.timeEvent.id;

                        SmartScriptHolder ev = new SmartScriptHolder();
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
                    break;
                case SmartActions.RemoveTimedEvent:
                    mRemIDs.Add(e.Action.timeEvent.id);
                    break;
                case SmartActions.OverrideScriptBaseObject:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsCreature(obj))
                            {
                                if (meOrigGUID.IsEmpty() && me)
                                    meOrigGUID = me.GetGUID();
                                if (goOrigGUID.IsEmpty() && go)
                                    goOrigGUID = go.GetGUID();
                                go = null;
                                me = obj.ToCreature();
                                break;
                            }
                            else if (IsGameObject(obj))
                            {
                                if (meOrigGUID.IsEmpty() && me)
                                    meOrigGUID = me.GetGUID();
                                if (goOrigGUID.IsEmpty() && go)
                                    goOrigGUID = go.GetGUID();
                                go = obj.ToGameObject();
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
                        float attackAngle = e.Action.setRangedMovement.angle / 180.0f * MathFunctions.PI;

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                Creature target = obj.ToCreature();
                                if (target != null)
                                    if (IsSmart(target) && target.GetVictim() != null)
                                        if (((SmartAI)target.GetAI()).CanCombatMove())
                                            target.GetMotionMaster().MoveChase(target.GetVictim(), attackDistance, attackAngle);
                            }
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

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                Creature target = obj.ToCreature();
                                GameObject goTarget = obj.ToGameObject();
                                if (target != null)
                                {
                                    if (IsSmart(target))
                                        ((SmartAI)target.GetAI()).SetScript9(e, e.Action.timedActionList.id, GetLastInvoker());
                                }
                                else if (goTarget != null)
                                {
                                    if (IsSmartGO(goTarget))
                                        ((SmartGameObjectAI)goTarget.GetAI()).SetScript9(e, e.Action.timedActionList.id, GetLastInvoker());
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.SetNpcFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsUnit(obj))
                                obj.ToUnit().SetUInt64Value(UnitFields.NpcFlags, e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.AddNpcFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsUnit(obj))
                                obj.ToUnit().SetFlag64(UnitFields.NpcFlags, e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.RemoveNpcFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsUnit(obj))
                                obj.ToUnit().RemoveFlag64(UnitFields.NpcFlags, e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.CrossCast:
                    {
                        List<WorldObject> casters = GetTargets(CreateSmartEvent(SmartEvents.UpdateIc, 0, 0, 0, 0, 0, SmartActions.None, 0, 0, 0, 0, 0, 0, (SmartTargets)e.Action.crossCast.targetType, e.Action.crossCast.targetParam1, e.Action.crossCast.targetParam2, e.Action.crossCast.targetParam3, 0), unit);
                        if (casters.Empty())
                            break;

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in casters)
                        {
                            if (!IsUnit(obj))
                                continue;

                            Unit targetUnit = obj.ToUnit();

                            bool interruptedSpell = false;

                            foreach (var targetObj in targets)
                            {
                                if (!IsUnit(targetObj))
                                    continue;

                                if (!(e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent)) || !targetObj.ToUnit().HasAura(e.Action.crossCast.spell))
                                {
                                    if (!interruptedSpell && e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                                    {
                                        targetUnit.InterruptNonMeleeSpells(false);
                                        interruptedSpell = true;
                                    }

                                    targetUnit.CastSpell(targetObj.ToUnit(), e.Action.crossCast.spell, e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered));
                                }
                                else
                                    Log.outDebug(LogFilter.ScriptsAi, "Spell {0} not cast because it has flag SMARTCAST_AURA_NOT_PRESENT and the target ({1}) already has the aura", e.Action.crossCast.spell, targetObj.GetGUID().ToString());
                            }
                        }
                        break;
                    }
                case SmartActions.CallRandomTimedActionlist:
                    {
                        uint[] actions =
                        {
                            e.Action.randTimedActionList.entry1,
                            e.Action.randTimedActionList.entry2,
                            e.Action.randTimedActionList.entry3,
                            e.Action.randTimedActionList.entry4,
                            e.Action.randTimedActionList.entry5,
                            e.Action.randTimedActionList.entry6,
                        };
                        uint[] temp = new uint[SharedConst.SmartActionParamCount];
                        uint count = 0;
                        for (byte i = 0; i < SharedConst.SmartActionParamCount; i++)
                        {
                            if (actions[i] > 0)
                            {
                                temp[count] = actions[i];
                                ++count;
                            }
                        }

                        uint id = temp[RandomHelper.IRand(0, (int)count)];
                        if (e.GetTargetType() == SmartTargets.None)
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.entryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());
                            break;
                        }

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                Creature target = obj.ToCreature();
                                GameObject goTarget = obj.ToGameObject();
                                if (target != null)
                                {
                                    if (IsSmart(target))
                                        ((SmartAI)target.GetAI()).SetScript9(e, id, GetLastInvoker());
                                }
                                else if (goTarget != null)
                                {
                                    if (IsSmartGO(goTarget))
                                        ((SmartGameObjectAI)goTarget.GetAI()).SetScript9(e, id, GetLastInvoker());
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.CallRandomRangeTimedActionlist:
                    {
                        uint id = RandomHelper.URand(e.Action.randTimedActionList.entry1, e.Action.randTimedActionList.entry2);
                        if (e.GetTargetType() == SmartTargets.None)
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.entryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());
                            break;
                        }

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (!targets.Empty())
                        {
                            foreach (var obj in targets)
                            {
                                Creature target = obj.ToCreature();
                                GameObject goTarget = obj.ToGameObject();
                                if (target != null)
                                {
                                    if (IsSmart(target))
                                        ((SmartAI)target.GetAI()).SetScript9(e, id, GetLastInvoker());
                                }
                                else if (goTarget != null)
                                {
                                    if (IsSmartGO(goTarget))
                                        ((SmartGameObjectAI)goTarget.GetAI()).SetScript9(e, id, GetLastInvoker());
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.ActivateTaxi:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsPlayer(obj))
                                obj.ToPlayer().ActivateTaxiPathTo(e.Action.taxi.id);
                        break;
                    }
                case SmartActions.RandomMove:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

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
                        break;
                    }
                case SmartActions.SetUnitFieldBytes1:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;
                        foreach (var obj in targets)
                            if (IsUnit(obj))
                                obj.ToUnit().SetByteFlag(UnitFields.Bytes1, (byte)e.Action.setunitByte.type, e.Action.setunitByte.byte1);
                        break;
                    }
                case SmartActions.RemoveUnitFieldBytes1:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsUnit(obj))
                                obj.ToUnit().RemoveByteFlag(UnitFields.Bytes1, (byte)e.Action.delunitByte.type, e.Action.delunitByte.byte1);
                        break;
                    }
                case SmartActions.InterruptSpell:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsUnit(obj))
                                obj.ToUnit().InterruptNonMeleeSpells(e.Action.interruptSpellCasting.withDelayed != 0, e.Action.interruptSpellCasting.spell_id, e.Action.interruptSpellCasting.withInstant != 0);
                        break;
                    }
                case SmartActions.SendGoCustomAnim:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsGameObject(obj))
                                obj.ToGameObject().SendCustomAnim(e.Action.sendGoCustomAnim.anim);
                        break;
                    }
                case SmartActions.SetDynamicFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsUnit(obj))
                                obj.ToUnit().SetUInt32Value(ObjectFields.DynamicFlags, e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.AddDynamicFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsUnit(obj))
                                obj.ToUnit().SetFlag(ObjectFields.DynamicFlags, e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.RemoveDynamicFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsUnit(obj))
                                obj.ToUnit().RemoveFlag(ObjectFields.DynamicFlags, e.Action.unitFlag.flag);
                        break;
                    }
                case SmartActions.JumpToPos:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            Creature creature = obj.ToCreature();
                            if (creature != null)
                            {
                                creature.GetMotionMaster().Clear();
                                // @todo add optional jump orientation support?
                                creature.GetMotionMaster().MoveJump(e.Target.x, e.Target.y, e.Target.z, 0.0f, e.Action.jump.speedxy, e.Action.jump.speedz);
                            }
                        }
                        //todo Resume path when reached jump location
                        break;
                    }
                case SmartActions.GoSetLootState:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);

                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsGameObject(obj))
                                obj.ToGameObject().SetLootState((LootState)e.Action.setGoLootState.state);
                        break;
                    }
                case SmartActions.GoSetGoState:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);

                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsGameObject(obj))
                                obj.ToGameObject().SetGoState((GameObjectState)e.Action.goState.state);
                        break;
                    }
                case SmartActions.SendTargetToTarget:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        List<WorldObject> storedTargets = GetTargetList(e.Action.sendTargetToTarget.id);
                        foreach (var obj in targets)
                        {
                            if (IsCreature(obj))
                            {
                                SmartAI ai = (SmartAI)obj.ToCreature().GetAI();
                                if (ai != null)
                                    ai.GetScript().StoreTargetList(new List<WorldObject>(storedTargets), e.Action.sendTargetToTarget.id);   // store a copy of target list
                                else
                                    Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SEND_TARGET_TO_TARGET is not using SmartAI, skipping");
                            }
                            else if (IsGameObject(obj))
                            {
                                SmartGameObjectAI ai = (SmartGameObjectAI)obj.ToGameObject().GetAI();
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
                        if (GetBaseObject() == null)
                            break;

                        Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_SEND_GOSSIP_MENU: gossipMenuId {0}, gossipNpcTextId {1}",
                            e.Action.sendGossipMenu.gossipMenuId, e.Action.sendGossipMenu.gossipNpcTextId);

                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            Player player = obj.ToPlayer();
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
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsCreature(obj))
                            {
                                if (e.GetTargetType() == SmartTargets.Self)
                                    obj.ToCreature().SetHomePosition(me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetOrientation());
                                else if (e.GetTargetType() == SmartTargets.Position)
                                    obj.ToCreature().SetHomePosition(e.Target.x, e.Target.y, e.Target.z, e.Target.o);
                                else if (e.GetTargetType() == SmartTargets.CreatureRange || e.GetTargetType() == SmartTargets.CreatureGuid ||
                                         e.GetTargetType() == SmartTargets.CreatureDistance || e.GetTargetType() == SmartTargets.GameobjectRange ||
                                         e.GetTargetType() == SmartTargets.GameobjectGuid || e.GetTargetType() == SmartTargets.GameobjectDistance ||
                                         e.GetTargetType() == SmartTargets.ClosestCreature || e.GetTargetType() == SmartTargets.ClosestGameobject ||
                                         e.GetTargetType() == SmartTargets.OwnerOrSummoner || e.GetTargetType() == SmartTargets.ActionInvoker ||
                                         e.GetTargetType() == SmartTargets.ClosestEnemy || e.GetTargetType() == SmartTargets.ClosestFriendly)
                                {
                                    obj.ToCreature().SetHomePosition(obj.GetPositionX(), obj.GetPositionY(), obj.GetPositionZ(), obj.GetOrientation());
                                }
                                else
                                    Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SET_HOME_POS is invalid, skipping");
                            }
                        }
                        break;
                    }
                case SmartActions.SetHealthRegen:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsCreature(obj))
                                obj.ToCreature().setRegeneratingHealth(e.Action.setHealthRegen.regenHealth != 0 ? true : false);
                        break;
                    }
                case SmartActions.SetRoot:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsCreature(obj))
                                obj.ToCreature().SetControlled(e.Action.setRoot.root != 0 ? true : false, UnitState.Root);
                        break;
                    }
                case SmartActions.SetGoFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsGameObject(obj))
                                obj.ToGameObject().SetUInt32Value(GameObjectFields.Flags, e.Action.goFlag.flag);
                        break;
                    }
                case SmartActions.AddGoFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsGameObject(obj))
                                obj.ToGameObject().SetFlag(GameObjectFields.Flags, e.Action.goFlag.flag);
                        break;
                    }
                case SmartActions.RemoveGoFlag:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                            if (IsGameObject(obj))
                                obj.ToGameObject().RemoveFlag(GameObjectFields.Flags, e.Action.goFlag.flag);
                        break;
                    }
                case SmartActions.SummonCreatureGroup:
                    {
                        List<TempSummon> summonList;
                        GetBaseObject().SummonCreatureGroup((byte)e.Action.creatureGroup.group, out summonList);

                        foreach (var obj in summonList)
                            if (unit == null && e.Action.creatureGroup.attackInvoker != 0)
                                obj.GetAI().AttackStart(unit);
                        break;
                    }
                case SmartActions.SetPower:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);

                        if (!targets.Empty())
                            foreach (var obj in targets)
                                if (IsUnit(obj))
                                    obj.ToUnit().SetPower((PowerType)e.Action.power.powerType, (int)e.Action.power.newPower);
                        break;
                    }
                case SmartActions.AddPower:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);

                        if (!targets.Empty())
                            foreach (var obj in targets)
                                if (IsUnit(obj))
                                    obj.ToUnit().SetPower((PowerType)e.Action.power.powerType, obj.ToUnit().GetPower((PowerType)e.Action.power.powerType) + (int)e.Action.power.newPower);
                        break;
                    }
                case SmartActions.RemovePower:
                    {
                        List<WorldObject> targets = GetTargets(e, unit);

                        if (!targets.Empty())
                            foreach (var obj in targets)
                                if (IsUnit(obj))
                                    obj.ToUnit().SetPower((PowerType)e.Action.power.powerType, obj.ToUnit().GetPower((PowerType)e.Action.power.powerType) - (int)e.Action.power.newPower);
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
                case SmartActions.RandomSound:
                    {
                        uint[] sounds = new uint[SharedConst.SmartActionParamCount - 1];
                        sounds[0] = e.Action.randomSound.sound1;
                        sounds[1] = e.Action.randomSound.sound2;
                        sounds[2] = e.Action.randomSound.sound3;
                        sounds[3] = e.Action.randomSound.sound4;
                        sounds[4] = e.Action.randomSound.sound5;

                        bool onlySelf = e.Action.randomSound.onlySelf != 0;

                        var targets = GetTargets(e, unit);
                        if (targets != null)
                        {
                            foreach (var obj in targets)
                            {
                                if (IsUnit(obj))
                                {
                                    uint sound = sounds.SelectRandom();
                                    obj.PlayDirectSound(sound, onlySelf ? obj.ToPlayer() : null);
                                    Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction:: SMART_ACTION_RANDOM_SOUND: target: {0} ({1}), sound: {2}, onlyself: {3}",
                                        obj.GetName(), obj.GetGUID().ToString(), sound, onlySelf);
                                }
                            }
                        }
                        break;
                    }
                case SmartActions.SetCorpseDelay:
                    {
                        var targets = GetTargets(e, unit);
                        if (targets == null)
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsCreature(obj))
                                obj.ToCreature().SetCorpseDelay(e.Action.corpseDelay.timer);
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
                case SmartActions.PlayAnimkit:
                    {
                        var targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (var obj in targets)
                        {
                            if (IsCreature(obj))
                            {
                                if (e.Action.animKit.type == 0)
                                    obj.ToCreature().PlayOneShotAnimKitId((ushort)e.Action.animKit.animKit);
                                else if (e.Action.animKit.type == 1)
                                    obj.ToCreature().SetAIAnimKitId((ushort)e.Action.animKit.animKit);
                                else if (e.Action.animKit.type == 2)
                                    obj.ToCreature().SetMeleeAnimKitId((ushort)e.Action.animKit.animKit);
                                else if (e.Action.animKit.type == 3)
                                    obj.ToCreature().SetMovementAnimKitId((ushort)e.Action.animKit.animKit);
                                else
                                {
                                    Log.outError(LogFilter.Sql, "SmartScript: Invalid type for SMART_ACTION_PLAY_ANIMKIT, skipping");
                                    break;
                                }

                                Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction:: SMART_ACTION_PLAY_ANIMKIT: target: {0} ({1}), AnimKit: {2}, Type: {3}",
                                    obj.GetName(), obj.GetGUID().ToString(), e.Action.animKit.animKit, e.Action.animKit.type);
                            }
                        }
                        break;
                    }
                case SmartActions.ScenePlay:
                    {
                        var targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (WorldObject target in targets)
                        {
                            Player playerTarget = target.ToPlayer();
                            if (playerTarget)
                                playerTarget.GetSceneMgr().PlayScene(e.Action.scene.sceneId);
                        }

                        break;
                    }
                case SmartActions.SceneCancel:
                    {
                        var targets = GetTargets(e, unit);
                        if (targets.Empty())
                            break;

                        foreach (WorldObject target in targets)
                        {
                            Player playerTarget = target.ToPlayer();
                            if (playerTarget)
                                playerTarget.GetSceneMgr().CancelSceneBySceneId(e.Action.scene.sceneId);
                        }

                        break;
                    }
                default:
                    Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Unhandled Action type {3}", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                    break;
            }

            if (e.link != 0 && e.link != e.event_id)
            {
                SmartScriptHolder linked = Global.SmartAIMgr.FindLinkedEvent(mEvents, e.link);
                if (linked != null)
                    ProcessEvent(linked, unit, var0, var1, bvar, spell, gob, varString);
                else
                    Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Link Event {3} not found or invalid, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.link);
            }
        }

        void ProcessTimedAction(SmartScriptHolder e, uint min, uint max, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
        {
            if (Global.ConditionMgr.IsObjectMeetingSmartEventConditions(e.entryOrGuid, e.event_id, e.source_type, unit, GetBaseObject()))
                ProcessAction(e, unit, var0, var1, bvar, spell, gob, varString);

            RecalcTimer(e, min, max);
        }

        void InstallTemplate(SmartScriptHolder e)
        {
            if (GetBaseObject() == null)
                return;
            if (mTemplate != 0)
            {
                Log.outError(LogFilter.Sql, "SmartScript.InstallTemplate: Entry {0} SourceType {1} AI Template can not be set more then once, skipped.", e.entryOrGuid, e.GetScriptType());
                return;
            }
            mTemplate = (SmartAITemplate)e.Action.installTtemplate.id;
            switch ((SmartAITemplate)e.Action.installTtemplate.id)
            {
                case SmartAITemplate.Caster:
                    {
                        AddEvent(SmartEvents.UpdateIc, 0, 0, 0, e.Action.installTtemplate.param2, e.Action.installTtemplate.param3, SmartActions.Cast, e.Action.installTtemplate.param1, e.Target.raw.param1, 0, 0, 0, 0, SmartTargets.Victim, 0, 0, 0, 1);
                        AddEvent(SmartEvents.Range, 0, e.Action.installTtemplate.param4, 300, 0, 0, SmartActions.AllowCombatMovement, 1, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 1);
                        AddEvent(SmartEvents.Range, 0, 0, e.Action.installTtemplate.param4 > 10 ? e.Action.installTtemplate.param4 - 10 : 0, 0, 0, SmartActions.AllowCombatMovement, 0, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 1);
                        AddEvent(SmartEvents.ManaPct, 0, e.Action.installTtemplate.param5 - 15 > 100 ? 100 : e.Action.installTtemplate.param5 + 15, 100, 1000, 1000, SmartActions.SetEventPhase, 1, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        AddEvent(SmartEvents.ManaPct, 0, 0, e.Action.installTtemplate.param5, 1000, 1000, SmartActions.SetEventPhase, 0, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        AddEvent(SmartEvents.ManaPct, 0, 0, e.Action.installTtemplate.param5, 1000, 1000, SmartActions.AllowCombatMovement, 1, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        break;
                    }
                case SmartAITemplate.Turret:
                    {
                        AddEvent(SmartEvents.UpdateIc, 0, 0, 0, e.Action.installTtemplate.param2, e.Action.installTtemplate.param3, SmartActions.Cast, e.Action.installTtemplate.param1, e.Target.raw.param1, 0, 0, 0, 0, SmartTargets.Victim, 0, 0, 0, 0);
                        AddEvent(SmartEvents.JustCreated, 0, 0, 0, 0, 0, SmartActions.AllowCombatMovement, 0, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        break;
                    }
                case SmartAITemplate.CagedNPCPart:
                    {
                        if (me == null)
                            return;
                        //store cage as id1
                        AddEvent(SmartEvents.DataSet, 0, 0, 0, 0, 0, SmartActions.StoreTargetList, 1, 0, 0, 0, 0, 0, SmartTargets.ClosestGameobject, e.Action.installTtemplate.param1, 10, 0, 0);

                        //reset(close) cage on hostage(me) respawn
                        AddEvent(SmartEvents.Update, SmartEventFlags.NotRepeatable, 0, 0, 0, 0, SmartActions.ResetGobject, 0, 0, 0, 0, 0, 0, SmartTargets.GameobjectDistance, e.Action.installTtemplate.param1, 5, 0, 0);

                        AddEvent(SmartEvents.DataSet, 0, 0, 0, 0, 0, SmartActions.SetRun, e.Action.installTtemplate.param3, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        AddEvent(SmartEvents.DataSet, 0, 0, 0, 0, 0, SmartActions.SetEventPhase, 1, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);

                        AddEvent(SmartEvents.Update, SmartEventFlags.NotRepeatable, 1000, 1000, 0, 0, SmartActions.MoveOffset, 0, 0, 0, 0, 0, 0, SmartTargets.Self, 0, e.Action.installTtemplate.param4, 0, 1);
                        //phase 1: give quest credit on movepoint reached
                        AddEvent(SmartEvents.Movementinform, 0, (uint)MovementGeneratorType.Point, EventId.SmartRandomPoint, 0, 0, SmartActions.SetData, 0, 0, 0, 0, 0, 0, SmartTargets.Stored, 1, 0, 0, 1);
                        //phase 1: despawn after time on movepoint reached
                        AddEvent(SmartEvents.Movementinform, 0, (uint)MovementGeneratorType.Point, EventId.SmartRandomPoint, 0, 0, SmartActions.ForceDespawn, e.Action.installTtemplate.param2, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 1);

                        if (Global.CreatureTextMgr.TextExist(me.GetEntry(), (byte)e.Action.installTtemplate.param5))
                            AddEvent(SmartEvents.Movementinform, 0, (uint)MovementGeneratorType.Point, EventId.SmartRandomPoint, 0, 0, SmartActions.Talk, e.Action.installTtemplate.param5, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 1);
                        break;
                    }
                case SmartAITemplate.CagedGOPart:
                    {
                        if (go == null)
                            return;
                        //store hostage as id1
                        AddEvent(SmartEvents.GoStateChanged, 0, 2, 0, 0, 0, SmartActions.StoreTargetList, 1, 0, 0, 0, 0, 0, SmartTargets.ClosestCreature, e.Action.installTtemplate.param1, 10, 0, 0);
                        //store invoker as id2
                        AddEvent(SmartEvents.GoStateChanged, 0, 2, 0, 0, 0, SmartActions.StoreTargetList, 2, 0, 0, 0, 0, 0, SmartTargets.None, 0, 0, 0, 0);
                        //signal hostage
                        AddEvent(SmartEvents.GoStateChanged, 0, 2, 0, 0, 0, SmartActions.SetData, 0, 0, 0, 0, 0, 0, SmartTargets.Stored, 1, 0, 0, 0);
                        //when hostage raeched end point, give credit to invoker
                        if (e.Action.installTtemplate.param2 != 0)
                            AddEvent(SmartEvents.DataSet, 0, 0, 0, 0, 0, SmartActions.CallKilledmonster, e.Action.installTtemplate.param1, 0, 0, 0, 0, 0, SmartTargets.Stored, 2, 0, 0, 0);
                        else
                            AddEvent(SmartEvents.GoStateChanged, 0, 2, 0, 0, 0, SmartActions.CallKilledmonster, e.Action.installTtemplate.param1, 0, 0, 0, 0, 0, SmartTargets.Stored, 2, 0, 0, 0);
                        break;
                    }
                default:
                    return;
            }
        }

        void AddEvent(SmartEvents e, SmartEventFlags event_flags, uint event_param1, uint event_param2, uint event_param3, uint event_param4,
            SmartActions action, uint action_param1, uint action_param2, uint action_param3, uint action_param4, uint action_param5, uint action_param6,
            SmartTargets t, uint target_param1, uint target_param2, uint target_param3, uint phaseMask)
        {
            mInstallEvents.Add(CreateSmartEvent(e, event_flags, event_param1, event_param2, event_param3, event_param4, action, action_param1, action_param2, action_param3, action_param4, action_param5, action_param6, t, target_param1, target_param2, target_param3, phaseMask));
        }

        SmartScriptHolder CreateSmartEvent(SmartEvents e, SmartEventFlags event_flags, uint event_param1, uint event_param2, uint event_param3, uint event_param4,
            SmartActions action, uint action_param1, uint action_param2, uint action_param3, uint action_param4, uint action_param5, uint action_param6,
            SmartTargets t, uint target_param1, uint target_param2, uint target_param3, uint phaseMask)
        {
            SmartScriptHolder script = new SmartScriptHolder();
            script.Event.type = e;
            script.Event.raw.param1 = event_param1;
            script.Event.raw.param2 = event_param2;
            script.Event.raw.param3 = event_param3;
            script.Event.raw.param4 = event_param4;
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
            Unit tempLastInvoker = GetLastInvoker();
            if (invoker != null)
                scriptTrigger = invoker;
            else if (tempLastInvoker != null)
                scriptTrigger = tempLastInvoker;

            WorldObject baseObject = GetBaseObject();

            List<WorldObject> l = new List<WorldObject>();
            switch (e.GetTargetType())
            {
                case SmartTargets.Self:
                    if (baseObject != null)
                        l.Add(baseObject);
                    break;
                case SmartTargets.Victim:
                    if (me != null && me.GetVictim() != null)
                        l.Add(me.GetVictim());
                    break;
                case SmartTargets.HostileSecondAggro:
                    if (me != null)
                    {
                        Unit u = me.GetAI().SelectTarget(SelectAggroTarget.TopAggro, 1);
                        if (u != null)
                            l.Add(u);
                    }
                    break;
                case SmartTargets.HostileLastAggro:
                    if (me != null)
                    {
                        Unit u = me.GetAI().SelectTarget(SelectAggroTarget.BottomAggro, 0);
                        if (u != null)
                            l.Add(u);
                    }
                    break;
                case SmartTargets.HostileRandom:
                    if (me != null)
                    {
                        Unit u = me.GetAI().SelectTarget(SelectAggroTarget.Random, 0);
                        if (u != null)
                            l.Add(u);
                    }
                    break;
                case SmartTargets.HostileRandomNotTop:
                    if (me != null)
                    {
                        Unit u = me.GetAI().SelectTarget(SelectAggroTarget.Random, 1);
                        if (u != null)
                            l.Add(u);
                    }
                    break;
                case SmartTargets.None:
                case SmartTargets.ActionInvoker:
                    if (scriptTrigger != null)
                        l.Add(scriptTrigger);
                    break;
                case SmartTargets.ActionInvokerVehicle:
                    if (scriptTrigger != null && scriptTrigger.GetVehicle() != null && scriptTrigger.GetVehicle().GetBase() != null)
                        l.Add(scriptTrigger.GetVehicle().GetBase());
                    break;
                case SmartTargets.InvokerParty:
                    if (scriptTrigger != null)
                    {
                        Player player = scriptTrigger.ToPlayer();
                        if (player != null)
                        {
                            Group group = player.GetGroup();
                            if (group)
                            {
                                for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.next())
                                {
                                    Player member = groupRef.GetSource();
                                    if (member)
                                        l.Add(member);
                                }
                            }
                            // We still add the player to the list if there is no group. If we do
                            // this even if there is a group (thus the else-check), it will add the
                            // same player to the list twice. We don't want that to happen.
                            else
                                l.Add(scriptTrigger);
                        }
                    }
                    break;
                case SmartTargets.CreatureRange:
                    {
                        List<WorldObject> units = GetWorldObjectsInDist(e.Target.unitRange.maxDist);
                        foreach (var obj in units)
                        {
                            if (!IsCreature(obj))
                                continue;

                            if (me != null && me == obj)
                                continue;

                            if ((e.Target.unitRange.creature == 0 || obj.ToCreature().GetEntry() == e.Target.unitRange.creature) && baseObject.IsInRange(obj, e.Target.unitRange.minDist, e.Target.unitRange.maxDist))
                                l.Add(obj);
                        }
                        break;
                    }
                case SmartTargets.CreatureDistance:
                    {
                        List<WorldObject> units = GetWorldObjectsInDist(e.Target.unitDistance.dist);
                        foreach (var obj in units)
                        {
                            if (!IsCreature(obj))
                                continue;

                            if (me != null && me == obj)
                                continue;

                            if (e.Target.unitDistance.creature == 0 || obj.ToCreature().GetEntry() == e.Target.unitDistance.creature)
                                l.Add(obj);
                        }
                        break;
                    }
                case SmartTargets.GameobjectDistance:
                    {
                        List<WorldObject> units = GetWorldObjectsInDist(e.Target.goDistance.dist);
                        foreach (var obj in units)
                        {
                            if (!IsGameObject(obj))
                                continue;

                            if (go != null && go == obj)
                                continue;

                            if (e.Target.goDistance.entry == 0 || obj.ToGameObject().GetEntry() == e.Target.goDistance.entry)
                                l.Add(obj);
                        }
                        break;
                    }
                case SmartTargets.GameobjectRange:
                    {
                        List<WorldObject> units = GetWorldObjectsInDist(e.Target.goRange.maxDist);
                        foreach (var obj in units)
                        {
                            if (!IsGameObject(obj))
                                continue;

                            if (go != null && go == obj)
                                continue;

                            if ((e.Target.goRange.entry == 0 && obj.ToGameObject().GetEntry() == e.Target.goRange.entry) && baseObject.IsInRange(obj, e.Target.goRange.minDist, e.Target.goRange.maxDist))
                                l.Add(obj);
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

                        Creature target = FindCreatureNear(scriptTrigger != null ? scriptTrigger : baseObject, e.Target.unitGUID.dbGuid);
                        if (target)
                            if (target != null && (e.Target.unitGUID.entry == 0 || target.GetEntry() == e.Target.unitGUID.entry))
                                l.Add(target);
                        break;
                    }
                case SmartTargets.GameobjectGuid:
                    {
                        if (scriptTrigger == null && baseObject == null)
                        {
                            Log.outError(LogFilter.Sql, "SMART_TARGET_GAMEOBJECT_GUID can not be used without invoker");
                            break;
                        }

                        GameObject target = FindGameObjectNear(scriptTrigger != null ? scriptTrigger : baseObject, e.Target.goGUID.dbGuid);
                        if (target)
                            if (target != null && (e.Target.goGUID.entry == 0 || target.GetEntry() == e.Target.goGUID.entry))
                                l.Add(target);
                        break;
                    }
                case SmartTargets.PlayerRange:
                    {
                        List<WorldObject> units = GetWorldObjectsInDist(e.Target.playerRange.maxDist);
                        if (!units.Empty() && baseObject != null)
                            foreach (var obj in units)
                                if (IsPlayer(obj) && baseObject.IsInRange(obj, e.Target.playerRange.minDist, e.Target.playerRange.maxDist))
                                    l.Add(obj);

                        break;
                    }
                case SmartTargets.PlayerDistance:
                    {
                        List<WorldObject> units = GetWorldObjectsInDist(e.Target.playerDistance.dist);
                        foreach (var obj in units)
                            if (IsPlayer(obj))
                                l.Add(obj);
                        break;
                    }
                case SmartTargets.Stored:
                    {
                        var list = mTargetStorage.LookupByKey(e.Target.stored.id);
                        if (!list.Empty())
                            l.AddRange(list);

                        return l;
                    }
                case SmartTargets.ClosestCreature:
                    {
                        Creature target = baseObject.FindNearestCreature(e.Target.closest.entry, e.Target.closest.dist != 0 ? e.Target.closest.dist : 100, e.Target.closest.dead == 0);
                        if (target)
                            l.Add(target);
                        break;
                    }
                case SmartTargets.ClosestGameobject:
                    {
                        GameObject target = baseObject.FindNearestGameObject(e.Target.closest.entry, e.Target.closest.dist != 0 ? e.Target.closest.dist : 100);
                        if (target)
                            l.Add(target);
                        break;
                    }
                case SmartTargets.ClosestPlayer:
                    {
                        if (me)
                        {
                            Player target = me.SelectNearestPlayer(e.Target.playerDistance.dist);
                            if (target)
                                l.Add(target);
                        }
                        break;
                    }
                case SmartTargets.OwnerOrSummoner:
                    {
                        if (me != null)
                        {
                            ObjectGuid charmerOrOwnerGuid = me.GetCharmerOrOwnerGUID();
                            if (charmerOrOwnerGuid.IsEmpty())
                            {
                                TempSummon tempSummon = me.ToTempSummon();
                                if (tempSummon)
                                {
                                    Unit summoner = tempSummon.GetSummoner();
                                    if (summoner)
                                        charmerOrOwnerGuid = summoner.GetGUID();
                                }
                            }

                            if (charmerOrOwnerGuid.IsEmpty())
                                charmerOrOwnerGuid = me.GetCreatorGUID();

                            Unit owner = Global.ObjAccessor.GetUnit(me, charmerOrOwnerGuid);
                            if (owner != null)
                                l.Add(owner);
                        }
                        break;
                    }
                case SmartTargets.ThreatList:
                    {
                        if (me != null)
                        {
                            var threatList = me.GetThreatManager().getThreatList();
                            foreach (var i in threatList)
                            {
                                Unit temp = Global.ObjAccessor.GetUnit(me, i.getUnitGuid());
                                if (temp != null)
                                    l.Add(temp);
                            }
                        }
                        break;
                    }
                case SmartTargets.ClosestEnemy:
                    {
                        if (me != null)
                        {
                            Unit target = me.SelectNearestTarget(e.Target.closestAttackable.maxDist);
                            if (target != null)
                                l.Add(target);
                        }

                        break;
                    }
                case SmartTargets.ClosestFriendly:
                    {
                        if (me != null)
                        {
                            Unit target = DoFindClosestFriendlyInRange(e.Target.closestFriendly.maxDist);
                            if (target != null)
                                l.Add(target);
                        }
                        break;
                    }
                case SmartTargets.LootRecipients:
                    {
                        if (me)
                        {
                            Group lootGroup = me.GetLootRecipientGroup();
                            if (lootGroup)
                            {
                                for (GroupReference refe = lootGroup.GetFirstMember(); refe != null; refe = refe.next())
                                {
                                    Player recipient = refe.GetSource();
                                    if (recipient)
                                        l.Add(recipient);
                                }
                            }
                            else
                            {
                                Player recipient = me.GetLootRecipient();
                                if (recipient)
                                    l.Add(recipient);
                            }
                        }
                        break;
                    }
                case SmartTargets.VehicleAccessory:
                    {
                        if (me && me.IsVehicle())
                        {
                            Unit target = me.GetVehicleKit().GetPassenger((sbyte)e.Target.vehicle.seat);
                            if (target)
                                l.Add(target);
                        }
                        break;
                    }
                case SmartTargets.SpellTarget:
                    {
                        if (spellTemplate != null)
                            l.Add(spellTemplate.m_targets.GetUnitTarget());
                        break;
                    }
                case SmartTargets.Position:
                default:
                    break;
            }

            return l;
        }

        List<WorldObject> GetWorldObjectsInDist(float dist)
        {
            List<WorldObject> targets = new List<WorldObject>();
            WorldObject obj = GetBaseObject();
            if (obj)
            {
                var u_check = new AllWorldObjectsInRange(obj, dist);
                var searcher = new WorldObjectListSearcher(obj, targets, u_check);
                Cell.VisitAllObjects(obj, searcher, dist);
            }
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
                    if (me != null && me.IsInCombat())
                        return;
                    ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    break;
                case SmartEvents.UpdateIc:
                    if (me == null || !me.IsInCombat())
                        return;
                    ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                    break;
                case SmartEvents.HealtPct:
                    {
                        if (me == null || !me.IsInCombat() || me.GetMaxHealth() == 0)
                            return;
                        uint perc = (uint)me.GetHealthPct();
                        if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                            return;
                        ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                        break;
                    }
                case SmartEvents.TargetHealthPct:
                    {
                        if (me == null || !me.IsInCombat() || me.GetVictim() == null || me.GetVictim().GetMaxHealth() == 0)
                            return;
                        uint perc = (uint)me.GetVictim().GetHealthPct();
                        if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                            return;
                        ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax, me.GetVictim());
                        break;
                    }
                case SmartEvents.ManaPct:
                    {
                        if (me == null || !me.IsInCombat() || me.GetMaxPower(PowerType.Mana) == 0)
                            return;
                        uint perc = (uint)me.GetPowerPct(PowerType.Mana);
                        if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                            return;
                        ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
                        break;
                    }
                case SmartEvents.TargetManaPct:
                    {
                        if (me == null || !me.IsInCombat() || me.GetVictim() == null || me.GetVictim().GetMaxPower(PowerType.Mana) == 0)
                            return;
                        uint perc = (uint)me.GetVictim().GetPowerPct(PowerType.Mana);
                        if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
                            return;
                        ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax, me.GetVictim());
                        break;
                    }
                case SmartEvents.Range:
                    {
                        if (me == null || !me.IsInCombat() || me.GetVictim() == null)
                            return;

                        if (me.IsInRange(me.GetVictim(), e.Event.minMaxRepeat.min, e.Event.minMaxRepeat.max))
                            ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax, me.GetVictim());
                        break;
                    }
                case SmartEvents.VictimCasting:
                    {
                        if (me == null || !me.IsInCombat())
                            return;

                        Unit victim = me.GetVictim();

                        if (victim == null || !victim.IsNonMeleeSpellCast(false, false, true))
                            return;

                        if (e.Event.targetCasting.spellId > 0)
                        {
                            Spell currSpell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);
                            if (currSpell != null)
                                if (currSpell.m_spellInfo.Id != e.Event.targetCasting.spellId)
                                    return;
                        }

                        ProcessTimedAction(e, e.Event.targetCasting.repeatMin, e.Event.targetCasting.repeatMax, me.GetVictim());
                        break;
                    }
                case SmartEvents.FriendlyHealth:
                    {
                        if (me == null || !me.IsInCombat())
                            return;

                        Unit target = DoSelectLowestHpFriendly(e.Event.friendlyHealth.radius, e.Event.friendlyHealth.hpDeficit);
                        if (target == null || !target.IsInCombat())
                            return;
                        ProcessTimedAction(e, e.Event.friendlyHealth.repeatMin, e.Event.friendlyHealth.repeatMax, target);
                        break;
                    }
                case SmartEvents.FriendlyIsCc:
                    {
                        if (me == null || !me.IsInCombat())
                            return;

                        List<Creature> pList = new List<Creature>();
                        DoFindFriendlyCC(pList, e.Event.friendlyCC.radius);
                        if (pList.Empty())
                            return;
                        ProcessTimedAction(e, e.Event.friendlyCC.repeatMin, e.Event.friendlyCC.repeatMax, pList.First());
                        break;
                    }
                case SmartEvents.FriendlyMissingBuff:
                    {
                        List<Creature> pList = new List<Creature>();
                        DoFindFriendlyMissingBuff(pList, e.Event.missingBuff.radius, e.Event.missingBuff.spell);

                        if (pList.Empty())
                            return;

                        ProcessTimedAction(e, e.Event.missingBuff.repeatMin, e.Event.missingBuff.repeatMax, pList.First());
                        break;
                    }
                case SmartEvents.HasAura:
                    {
                        if (me == null)
                            return;
                        uint count = me.GetAuraCount(e.Event.aura.spell);
                        if ((e.Event.aura.count == 0 && count == 0) || (e.Event.aura.count != 0 && count >= e.Event.aura.count))
                            ProcessTimedAction(e, e.Event.aura.repeatMin, e.Event.aura.repeatMax);
                        break;
                    }
                case SmartEvents.TargetBuffed:
                    {
                        if (me == null || me.GetVictim() == null)
                            return;
                        uint count = me.GetVictim().GetAuraCount(e.Event.aura.spell);
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
                case SmartEvents.GossipHello:
                case SmartEvents.FollowCompleted:
                case SmartEvents.OnSpellclick:
                    ProcessAction(e, unit, var0, var1, bvar, spell, gob);
                    break;
                case SmartEvents.IsBehindTarget:
                    {
                        if (me == null)
                            return;

                        Unit victim = me.GetVictim();
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
                        ProcessAction(e, unit);
                        RecalcTimer(e, e.Event.emote.cooldownMin, e.Event.emote.cooldownMax);
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
                        ProcessAction(e, unit);
                        RecalcTimer(e, e.Event.kill.cooldownMin, e.Event.kill.cooldownMax);
                        break;
                    }
                case SmartEvents.SpellhitTarget:
                case SmartEvents.SpellHit:
                    {
                        if (spell == null)
                            return;
                        if ((e.Event.spellHit.spell == 0 || spell.Id == e.Event.spellHit.spell) &&
                            (e.Event.spellHit.school == 0 || Convert.ToBoolean((uint)spell.SchoolMask & e.Event.spellHit.school)))
                        {
                            ProcessAction(e, unit, 0, 0, bvar, spell);
                            RecalcTimer(e, e.Event.spellHit.cooldownMin, e.Event.spellHit.cooldownMax);
                        }
                        break;
                    }
                case SmartEvents.OocLos:
                    {
                        if (me == null || me.IsInCombat())
                            return;
                        //can trigger if closer than fMaxAllowedRange
                        float range = e.Event.los.maxDist;

                        //if range is ok and we are actually in LOS
                        if (me.IsWithinDistInMap(unit, range) && me.IsWithinLOSInMap(unit))
                        {
                            //if friendly event&&who is not hostile OR hostile event&&who is hostile
                            if ((e.Event.los.noHostile != 0 && !me.IsHostileTo(unit)) ||
                                (e.Event.los.noHostile == 0 && me.IsHostileTo(unit)))
                            {
                                ProcessAction(e, unit);
                                RecalcTimer(e, e.Event.los.cooldownMin, e.Event.los.cooldownMax);
                            }
                        }
                        break;
                    }
                case SmartEvents.IcLos:
                    {
                        if (me == null || !me.IsInCombat())
                            return;
                        //can trigger if closer than fMaxAllowedRange
                        float range = e.Event.los.maxDist;

                        //if range is ok and we are actually in LOS
                        if (me.IsWithinDistInMap(unit, range) && me.IsWithinLOSInMap(unit))
                        {
                            //if friendly event&&who is not hostile OR hostile event&&who is hostile
                            if ((e.Event.los.noHostile != 0 && !me.IsHostileTo(unit)) ||
                                (e.Event.los.noHostile == 0 && me.IsHostileTo(unit)))
                            {
                                ProcessAction(e, unit);
                                RecalcTimer(e, e.Event.los.cooldownMin, e.Event.los.cooldownMax);
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
                        ProcessAction(e, unit);
                        RecalcTimer(e, e.Event.summoned.cooldownMin, e.Event.summoned.cooldownMax);
                        break;
                    }
                case SmartEvents.ReceiveHeal:
                case SmartEvents.Damaged:
                case SmartEvents.DamagedTarget:
                    {
                        if (var0 > e.Event.minMaxRepeat.max || var0 < e.Event.minMaxRepeat.min)
                            return;
                        ProcessAction(e, unit);
                        RecalcTimer(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
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
                        if (me == null || (e.Event.waypoint.pointID != 0 && var0 != e.Event.waypoint.pointID) || (e.Event.waypoint.pathID != 0 && GetPathId() != e.Event.waypoint.pathID))
                            return;
                        ProcessAction(e, unit);
                        break;
                    }
                case SmartEvents.SummonDespawned:
                case SmartEvents.InstancePlayerEnter:
                    {
                        if (e.Event.instancePlayerEnter.team != 0 && var0 != e.Event.instancePlayerEnter.team)
                            return;
                        ProcessAction(e, unit, var0);
                        RecalcTimer(e, e.Event.instancePlayerEnter.cooldownMin, e.Event.instancePlayerEnter.cooldownMax);
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
                        ProcessAction(e, unit, var0, var1);
                        RecalcTimer(e, e.Event.dataSet.cooldownMin, e.Event.dataSet.cooldownMax);
                        break;
                    }
                case SmartEvents.PassengerRemoved:
                case SmartEvents.PassengerBoarded:
                    {
                        if (unit == null)
                            return;
                        ProcessAction(e, unit);
                        RecalcTimer(e, e.Event.minMax.repeatMin, e.Event.minMax.repeatMax);
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
                case SmartEvents.DummyEffect:
                    {
                        if (e.Event.dummy.spell != var0 || e.Event.dummy.effIndex != var1)
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
                case SmartEvents.GoStateChanged:
                    {
                        if (e.Event.goStateChanged.state != var0)
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
                        if (me == null || !me.IsInCombat())
                            return;

                        List<WorldObject> _targets;

                        switch (e.GetTargetType())
                        {
                            case SmartTargets.CreatureRange:
                            case SmartTargets.CreatureGuid:
                            case SmartTargets.CreatureDistance:
                            case SmartTargets.ClosestCreature:
                            case SmartTargets.ClosestPlayer:
                            case SmartTargets.PlayerRange:
                            case SmartTargets.PlayerDistance:
                                _targets = GetTargets(e);
                                break;
                            default:
                                return;
                        }

                        if (_targets == null)
                            return;

                        Unit target = null;

                        foreach (var obj in _targets)
                        {
                            if (IsUnit(obj) && me.IsFriendlyTo(obj.ToUnit()) && obj.ToUnit().IsAlive() && obj.ToUnit().IsInCombat())
                            {
                                uint healthPct = (uint)obj.ToUnit().GetHealthPct();

                                if (healthPct > e.Event.friendlyHealthPct.maxHpPct || healthPct < e.Event.friendlyHealthPct.minHpPct)
                                    continue;

                                target = obj.ToUnit();
                                break;
                            }
                        }

                        if (target == null)
                            return;

                        ProcessTimedAction(e, e.Event.friendlyHealthPct.repeatMin, e.Event.friendlyHealthPct.repeatMax, target);
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
                            List<Creature> list = new List<Creature>();
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
                            List<GameObject> list = new List<GameObject>();
                            me.GetGameObjectListWithEntryInGrid(list, e.Event.distance.entry, e.Event.distance.dist);

                            if (!list.Empty())
                                gameobject = list.FirstOrDefault();
                        }

                        if (gameobject)
                            ProcessTimedAction(e, e.Event.distance.repeat, e.Event.distance.repeat, null, 0, 0, false, null, gameobject);

                        break;
                    }
                case SmartEvents.CounterSet:
                    if (GetCounterId(e.Event.counter.id) != 0 && GetCounterValue(e.Event.counter.id) == e.Event.counter.value)
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

            if (e.GetEventType() == SmartEvents.UpdateIc && (me == null || !me.IsInCombat()))
                return;

            if (e.GetEventType() == SmartEvents.UpdateOoc && (me != null && me.IsInCombat()))//can be used with me=NULL (go script)
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
                    if (me && me.HasUnitState(UnitState.Root | UnitState.Stunned))
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
                    case SmartEvents.HealtPct:
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
            if ((mScriptType == SmartScriptType.Creature || mScriptType == SmartScriptType.GameObject) && GetBaseObject() == null)
                return;

            InstallEvents();//before UpdateTimers

            foreach (var holder in mEvents)
                UpdateTimer(holder, diff);

            if (!mStoredEvents.Empty())
                foreach (var holder in mStoredEvents)
                    UpdateTimer(holder, diff);

            bool needCleanup = true;
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
                {
                    RemoveStoredEvent(id);
                }
            }
            if (mUseTextTimer && me != null)
            {
                if (mTextTimer < diff)
                {
                    uint textID = mLastTextID;
                    mLastTextID = 0;
                    uint entry = mTalkerEntry;
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
            ProcessEventsFor(SmartEvents.OocLos, who);

            if (me == null)
                return;

            if (me.GetVictim() != null)
                return;

            ProcessEventsFor(SmartEvents.IcLos, who);
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
            FriendlyBelowHpPctEntryInRange u_check = new FriendlyBelowHpPctEntryInRange(me, entry, range, minHPDiff, excludeSelf);
            UnitLastSearcher searcher = new UnitLastSearcher(me, u_check);
            Cell.VisitAllObjects(me, searcher, range);

            return searcher.GetTarget();
        }

        void DoFindFriendlyCC(List<Creature> _list, float range)
        {
            if (me == null)
                return;

            var u_check = new FriendlyCCedInRange(me, range);
            var searcher = new CreatureListSearcher(me, _list, u_check);
            Cell.VisitGridObjects(me, searcher, range);
        }

        void DoFindFriendlyMissingBuff(List<Creature> list, float range, uint spellid)
        {
            if (me == null)
                return;

            var u_check = new FriendlyMissingBuffInRange(me, range, spellid);
            var searcher = new CreatureListSearcher(me, list, u_check);
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
            int i = 0;
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

        Unit GetLastInvoker()
        {
            WorldObject lookupRoot = me;
            if (!lookupRoot)
                lookupRoot = go;

            if (lookupRoot)
                return Global.ObjAccessor.GetUnit(lookupRoot, mLastInvoker);

            return Global.ObjAccessor.FindPlayer(mLastInvoker);
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
            return obj;
        }
        WorldObject GetBaseObjectOrUnit(Unit unit)
        {
            WorldObject summoner = GetBaseObject();

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

            Creature creatureObj = obj.ToCreature();
            if (creatureObj)
                return creatureObj.IsCharmed();

            return false;
        }
        bool IsGameObject(WorldObject obj) { return obj != null && obj.IsTypeId(TypeId.GameObject); }

        void StoreTargetList(List<WorldObject> targets, uint id)
        {
            if (targets == null)
                return;

            if (mTargetStorage.ContainsKey(id))
            {
                // check if already stored
                if (mTargetStorage[id] == targets)
                    return;
            }

            mTargetStorage[id] = targets;
        }

        bool IsSmart(Creature c = null)
        {
            bool smart = true;
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
            bool smart = true;
            if (g != null && g.GetAIName() != "SmartGameObjectAI")
                smart = false;

            if (go == null || go.GetAIName() != "SmartGameObjectAI")
                smart = false;
            if (!smart)
                Log.outError(LogFilter.Sql, "SmartScript: Action target GameObject (GUID: {0} Entry: {1}) is not using SmartGameObjectAI, action skipped to prevent crash.", g != null ? g.GetSpawnId() : (go != null ? go.GetSpawnId() : 0), g != null ? g.GetEntry() : (go != null ? go.GetEntry() : 0));

            return smart;
        }

        public List<WorldObject> GetTargetList(uint id)
        {
            var list = mTargetStorage.LookupByKey(id);
            if (!list.Empty())
                return list;
            return null;
        }

        void StoreCounter(uint id, uint value, uint reset)
        {
            if (mCounterList.ContainsKey(id))
            {
                if (reset == 0)
                    value += GetCounterValue(id);
                mCounterList.Remove(id);
            }

            mCounterList.Add(id, value);
            ProcessEventsFor(SmartEvents.CounterSet);
        }

        uint GetCounterId(uint id)
        {
            if (mCounterList.ContainsKey(id))
                return id;
            return 0;
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
                    Creature m = ObjectAccessor.GetCreature(lookupRoot, meOrigGUID);
                    if (m != null)
                    {
                        me = m;
                        go = null;
                    }
                }

                if (!goOrigGUID.IsEmpty())
                {
                    GameObject o = ObjectAccessor.GetGameObject(lookupRoot, goOrigGUID);
                    if (o != null)
                    {
                        me = null;
                        go = o;
                    }
                }
            }
            goOrigGUID.Clear();
            meOrigGUID.Clear();
        }

        void IncPhase(int p = 1)
        {
            if (p >= 0)
                mEventPhase += (uint)p;
            else
                DecPhase(-p);
        }

        void DecPhase(int p = 1)
        {
            if (mEventPhase > p)
                mEventPhase -= (uint)p;
            else
                mEventPhase = 0;
        }
        bool IsInPhase(uint p) { return Convert.ToBoolean((1 << (int)(mEventPhase - 1)) & p); }
        void SetPhase(uint p = 0) { mEventPhase = p; }

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

        MultiMap<uint, WorldObject> mTargetStorage = new MultiMap<uint, WorldObject>();

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

        SmartAITemplate mTemplate;
    }
}
