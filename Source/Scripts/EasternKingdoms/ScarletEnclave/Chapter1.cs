/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms
{
    enum UnworthyInitiatePhase
    {
        Chained,
        ToEquip,
        Equiping,
        ToAttack,
        Attacking
    }

    [Script]
    public class npc_unworthy_initiate : ScriptedAI
    {
        public npc_unworthy_initiate(Creature creature) : base(creature)
        {
            me.SetReactState(ReactStates.Passive);
            if (me.GetCurrentEquipmentId() == 0)
                me.SetCurrentEquipmentId((byte)me.GetOriginalEquipmentId());
        }

        public override void Reset()
        {
            anchorGUID.Clear();
            phase = UnworthyInitiatePhase.Chained;
            _events.Reset();
            me.SetFaction(7);
            me.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
            me.SetStandState(UnitStandStateType.Kneel);
            me.LoadEquipment(0, true);
        }

        public override void EnterCombat(Unit who)
        {
            _events.ScheduleEvent(EventIcyTouch, 1000, 1);
            _events.ScheduleEvent(EventPlagueStrike, 3000, 1);
            _events.ScheduleEvent(EventBloodStrike, 2000, 1);
            _events.ScheduleEvent(EventDeathCoil, 5000, 1);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;

            if (id == 1)
            {
                wait_timer = 5000;
                me.CastSpell(me, SpellDKInitateVisual, true);

                Player starter = Global.ObjAccessor.GetPlayer(me, playerGUID);
                if (starter)
                    Global.CreatureTextMgr.SendChat(me, (byte)SayEventAttack, null, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, Team.Other, false, starter);

                phase = UnworthyInitiatePhase.ToAttack;
            }
        }

        public void EventStart(Creature anchor, Player target)
        {
            wait_timer = 5000;
            phase = UnworthyInitiatePhase.ToEquip;

            me.SetStandState(UnitStandStateType.Stand);
            me.RemoveAurasDueToSpell(SpellSoulPrisonChainSelf);
            me.RemoveAurasDueToSpell(SpellSoulPrisonChain);

            float z;
            anchor.GetContactPoint(me, out anchorX, out anchorY, out z, 1.0f);

            playerGUID = target.GetGUID();
            Talk(SayEventStart);
        }

        public override void UpdateAI(uint diff)
        {
            switch (phase)
            {
                case UnworthyInitiatePhase.Chained:
                    if (anchorGUID.IsEmpty())
                    {
                        Creature anchor = me.FindNearestCreature(29521, 30);
                        if (anchor)
                        {
                            anchor.GetAI().SetGUID(me.GetGUID());
                            anchor.CastSpell(me, SpellSoulPrisonChain, true);
                            anchorGUID = anchor.GetGUID();
                        }
                        else
                            Log.outError(LogFilter.Scripts, "npc_unworthy_initiateAI: unable to find anchor!");

                        float dist = 99.0f;
                        GameObject prison = null;

                        for (byte i = 0; i < 12; ++i)
                        {
                            GameObject temp_prison = me.FindNearestGameObject(acherus_soul_prison[i], 30);
                            if (temp_prison)
                            {
                                if (me.IsWithinDist(temp_prison, dist, false))
                                {
                                    dist = me.GetDistance2d(temp_prison);
                                    prison = temp_prison;
                                }
                            }
                        }

                        if (prison)
                            prison.ResetDoorOrButton();
                        else
                            Log.outError(LogFilter.Scripts, "npc_unworthy_initiateAI: unable to find prison!");
                    }
                    break;
                case UnworthyInitiatePhase.ToEquip:
                    if (wait_timer != 0)
                    {
                        if (wait_timer > diff)
                            wait_timer -= diff;
                        else
                        {
                            me.GetMotionMaster().MovePoint(1, anchorX, anchorY, me.GetPositionZ());
                            phase = UnworthyInitiatePhase.Equiping;
                            wait_timer = 0;
                        }
                    }
                    break;
                case UnworthyInitiatePhase.ToAttack:
                    if (wait_timer != 0)
                    {
                        if (wait_timer > diff)
                            wait_timer -= diff;
                        else
                        {
                            me.SetFaction(14);
                            me.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
                            phase = UnworthyInitiatePhase.Attacking;

                            Player target = Global.ObjAccessor.GetPlayer(me, playerGUID);
                            if (target)
                                AttackStart(target);
                            wait_timer = 0;
                        }
                    }
                    break;
                case UnworthyInitiatePhase.Attacking:
                    if (!UpdateVictim())
                        return;

                    _events.Update(diff);
                    _events.ExecuteEvents(eventId =>
                    {
                        switch (eventId)
                        {
                            case EventIcyTouch:
                                DoCastVictim(SpellIcyTouch);
                                _events.DelayEvents(1000, 1);
                                _events.ScheduleEvent(EventIcyTouch, 5000, 1);
                                break;
                            case EventPlagueStrike:
                                DoCastVictim(SpellPlagueStrike);
                                _events.DelayEvents(1000, 1);
                                _events.ScheduleEvent(EventPlagueStrike, 5000, 1);
                                break;
                            case EventBloodStrike:
                                DoCastVictim(SpellBloodStrike);
                                _events.DelayEvents(1000, 1);
                                _events.ScheduleEvent(EventBloodStrike, 5000, 1);
                                break;
                            case EventDeathCoil:
                                DoCastVictim(SpellDeathCoil);
                                _events.DelayEvents(1000, 1);
                                _events.ScheduleEvent(EventDeathCoil, 5000, 1);
                                break;
                        }
                    });

                    DoMeleeAttackIfReady();
                    break;
                default:
                    break;
            }
        }

        ObjectGuid playerGUID;
        UnworthyInitiatePhase phase;
        uint wait_timer;
        float anchorX, anchorY;
        ObjectGuid anchorGUID;

        const uint SpellSoulPrisonChainSelf = 54612;
        const uint SpellSoulPrisonChain = 54613;
        const uint SpellDKInitateVisual = 51519;

        const uint SpellIcyTouch = 52372;
        const uint SpellPlagueStrike = 52373;
        const uint SpellBloodStrike = 52374;
        const uint SpellDeathCoil = 52375;

        const uint SayEventStart = 0;
        const uint SayEventAttack = 1;

        const uint EventIcyTouch = 1;
        const uint EventPlagueStrike = 2;
        const uint EventBloodStrike = 3;
        const uint EventDeathCoil = 4;

        static uint[] acherus_soul_prison = { 191577, 191580, 191581, 191582, 191583, 191584, 191585, 191586, 191587, 191588, 191589, 191590 };
    }

    [Script]
    class npc_unworthy_initiate_anchor : PassiveAI
    {
        public npc_unworthy_initiate_anchor(Creature creature) : base(creature) { }

        public override void SetGUID(ObjectGuid guid, int id)
        {
            if (prisonerGUID.IsEmpty())
                prisonerGUID = guid;
        }

        public override ObjectGuid GetGUID(int id)
        {
            return prisonerGUID;
        }

        ObjectGuid prisonerGUID;
    }

    [Script]
    class go_acherus_soul_prison : GameObjectScript
    {
        public go_acherus_soul_prison() : base("go_acherus_soul_prison") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            Creature anchor = go.FindNearestCreature(29521, 15);
            if (anchor)
            {
                ObjectGuid prisonerGUID = anchor.GetAI().GetGUID();
                if (!prisonerGUID.IsEmpty())
                {
                    Creature prisoner = ObjectAccessor.GetCreature(player, prisonerGUID);
                    if (prisoner)
                        ((npc_unworthy_initiate)prisoner.GetAI()).EventStart(anchor, player);
                }
            }

            return false;
        }
    }

    struct EyeOfAcherus
    {
        public const uint EyeHugeDisplayId = 26320;
        public const uint EyeSmallDisplayId = 25499;

        public const uint SpellEyePhasemask = 70889;
        public const uint SpellEyeVisual = 51892;
        public const uint SpellEyeFlight = 51923;
        public const uint SpellEyeFlightBoost = 51890;
        public const uint SpellEyeControl = 51852;

        public const string SayEyeLaunched = "Eye of Acherus is launched towards its destination.";
        public const string SayEyeUnderControl = "You are now in control of the eye.";

        public static float[] EyeDestination = { 1750.8276f, -5873.788f, 147.2266f };
    }

    [Script]
    class npc_eye_of_acherus : ScriptedAI
    {
        public npc_eye_of_acherus(Creature creature) : base(creature)
        {
            Reset();
        }

        uint startTimer;

        public override void Reset()
        {
            startTimer = 2000;
        }

        public override void AttackStart(Unit u) { }

        public override void MoveInLineOfSight(Unit u) { }

        public override void JustDied(Unit killer)
        {
            Unit charmer = me.GetCharmer();
            if (charmer)
                charmer.RemoveAurasDueToSpell(EyeOfAcherus.SpellEyeControl);
        }

        public override void UpdateAI(uint diff)
        {
            if (me.IsCharmed())
            {
                if (startTimer <= diff)    // fly to start point
                {
                    me.CastSpell(me, EyeOfAcherus.SpellEyePhasemask, true);
                    me.CastSpell(me, EyeOfAcherus.SpellEyeVisual, true);
                    me.CastSpell(me, EyeOfAcherus.SpellEyeFlightBoost, true);
                    me.SetSpeedRate(UnitMoveType.Flight, 4f);

                    me.GetMotionMaster().MovePoint(0, EyeOfAcherus.EyeDestination[0], EyeOfAcherus.EyeDestination[1], EyeOfAcherus.EyeDestination[2]);
                    return;
                }
                else
                    startTimer -= diff;
            }
            else
                me.ForcedDespawn();
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point || id != 0)
                return;

            me.SetDisplayId(EyeOfAcherus.EyeSmallDisplayId);

            me.CastSpell(me, EyeOfAcherus.SpellEyeFlight, true);
            me.Say(EyeOfAcherus.SayEyeUnderControl, Language.Universal);

            if (me.GetCharmer() && me.GetCharmer().IsTypeId(TypeId.Player))
                me.GetCharmer().ToPlayer().SetClientControl(me, true);
        }
    }
}
