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
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Movement;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms
{
    struct SpellIds
    {
        //Unworthy Initiate
        public const uint SoulPrisonChainSelf = 54612;
        public const uint SoulPrisonChain = 54613;
        public const uint DKInitateVisual = 51519;
        public const uint IcyTouch = 52372;
        public const uint PlagueStrike = 52373;
        public const uint BloodStrike = 52374;
        public const uint DeathCoil = 52375;

        //EyeOfAcherus
        public const uint EyeVisual = 51892;
        public const uint EyeFlightBoost = 51923;
        public const uint EyeFlight = 51890;

        //DeathKnightInitiate
        public const uint Duel = 52996;
        //public const uint SPELL_DUEL_TRIGGERED        = 52990;
        public const uint DuelVictory = 52994;
        public const uint DuelFlag = 52991;
        public const uint Grovel = 7267;

        //DarkRiderOfAcherus
        public const uint DespawnHorse = 51918;

        //SalanarTheHorseman
        public const uint EffectStolenHorse = 52263;
        public const uint DeliverStolenHorse = 52264;
        public const uint CallDarkRider = 52266;
        public const uint EffectOvertake = 52349;
        public const uint RealmOfShadows = 52693;

        //ScarletMinerCart
        public const uint CartCheck = 54173;
        public const uint SummonCart = 52463;
        public const uint SummonMiner = 52464;
        public const uint CartDrag = 52465;
    }

    struct TextIds
    {
        //Unworthy Initiate
        public const uint SayEventStart = 0;
        public const uint SayEventAttack = 1;

        //EyeOfAcherus     
        public const uint TalkMoveStart = 0;
        public const uint TalkControl = 1;

        //DeathKnightInitiate
        public const uint SayDuel = 0;

        //DarkRiderOfAcherus
        public const uint SayDarkRider = 0;

        //SalanarTheHorseman
        public const uint SaySalanar = 0;

        //ScarletMiner
        public const uint SayScarletMiner0 = 0;
        public const uint SayScarletMiner1 = 1;
    }

    struct CreatureIds
    {
        //SalanarTheHorseman
        public const uint DarkRiderOfAcherus = 28654;
        public const uint SalanarInRealmOfShadows = 28788;

        //dkc1_gothik
        public const uint Ghouls = 28845;
        public const uint Ghosts = 28846;

        //ScarletMinerCart
        public const uint Miner = 28841;
    }

    struct EventIds
    {
        //Unworthy Initiate
        public const uint IcyTouch = 1;
        public const uint PlagueStrike = 2;
        public const uint BloodStrike = 3;
        public const uint DeathCoil = 4;
    }

    struct MiscConst
    {
        //Unworthy Initiate
        public static uint[] acherus_soul_prison = { 191577, 191580, 191581, 191582, 191583, 191584, 191585, 191586, 191587, 191588, 191589, 191590 };

        //EyeOfAcherus
        public static Position EyeOFAcherusFallPoint = new Position(2361.21f, -5660.45f, 496.7444f, 0.0f);

        //DeathKnightInitiate
        public static uint QuestDeathChallenge = 12733;
        public static uint FactionHostile = 2068;

        //SalanarTheHorseman
        public static uint GossipSalanarMenu = 9739;
        public static uint GossipSalanarOption = 0;
        public static uint QuestIntoRealmOfShadows = 12687;
    }

    [Script]
    class npc_unworthy_initiate : ScriptedAI
    {
        public npc_unworthy_initiate(Creature creature) : base(creature)
        {
            Initialize();
            me.SetReactState(ReactStates.Passive);
            if (me.GetCurrentEquipmentId() == 0)
                me.SetCurrentEquipmentId((byte)me.GetOriginalEquipmentId());
        }

        void Initialize()
        {
            anchorGUID.Clear();
            phase = UnworthyInitiatePhase.Chained;
        }

        public override void Reset()
        {
            Initialize();
            _events.Reset();
            me.SetFaction(7);
            me.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
            me.SetStandState(UnitStandStateType.Kneel);
            me.LoadEquipment(0, true);
        }

        public override void EnterCombat(Unit who)
        {
            _events.ScheduleEvent(EventIds.IcyTouch, 1000, 1);
            _events.ScheduleEvent(EventIds.PlagueStrike, 3000, 1);
            _events.ScheduleEvent(EventIds.BloodStrike, 2000, 1);
            _events.ScheduleEvent(EventIds.DeathCoil, 5000, 1);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;

            if (id == 1)
            {
                wait_timer = 5000;
                me.LoadEquipment(1);
                me.CastSpell(me, SpellIds.DKInitateVisual, true);

                Player starter = Global.ObjAccessor.GetPlayer(me, playerGUID);
                if (starter)
                    Talk(TextIds.SayEventAttack, starter);

                phase = UnworthyInitiatePhase.ToAttack;
            }
        }

        public void EventStart(Creature anchor, Player target)
        {
            wait_timer = 5000;
            phase = UnworthyInitiatePhase.ToEquip;

            me.SetStandState(UnitStandStateType.Stand);
            me.RemoveAurasDueToSpell(SpellIds.SoulPrisonChainSelf);
            me.RemoveAurasDueToSpell(SpellIds.SoulPrisonChain);

            float z;
            anchor.GetContactPoint(me, out anchorX, out anchorY, out z, 1.0f);

            playerGUID = target.GetGUID();
            Talk(TextIds.SayEventStart);
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
                            anchor.CastSpell(me, SpellIds.SoulPrisonChain, true);
                            anchorGUID = anchor.GetGUID();
                        }
                        else
                            Log.outError(LogFilter.Scripts, "npc_unworthy_initiateAI: unable to find anchor!");

                        float dist = 99.0f;
                        GameObject prison = null;

                        for (byte i = 0; i < 12; ++i)
                        {
                            GameObject temp_prison = me.FindNearestGameObject(MiscConst.acherus_soul_prison[i], 30);
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
                            case EventIds.IcyTouch:
                                DoCastVictim(SpellIds.IcyTouch);
                                _events.DelayEvents(1000, 1);
                                _events.ScheduleEvent(EventIds.IcyTouch, 5000, 1);
                                break;
                            case EventIds.PlagueStrike:
                                DoCastVictim(SpellIds.PlagueStrike);
                                _events.DelayEvents(1000, 1);
                                _events.ScheduleEvent(EventIds.PlagueStrike, 5000, 1);
                                break;
                            case EventIds.BloodStrike:
                                DoCastVictim(SpellIds.BloodStrike);
                                _events.DelayEvents(1000, 1);
                                _events.ScheduleEvent(EventIds.BloodStrike, 5000, 1);
                                break;
                            case EventIds.DeathCoil:
                                DoCastVictim(SpellIds.DeathCoil);
                                _events.DelayEvents(1000, 1);
                                _events.ScheduleEvent(EventIds.DeathCoil, 5000, 1);
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

        enum UnworthyInitiatePhase
        {
            Chained,
            ToEquip,
            Equiping,
            ToAttack,
            Attacking,
        }
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

    [Script]
    class npc_eye_of_acherus : ScriptedAI
    {
        public npc_eye_of_acherus(Creature creature) : base(creature)
        {
            me.SetDisplayFromModel(0);

            Player owner = me.GetCharmerOrOwner().ToPlayer();
            if (owner)
            {
                me.GetCharmInfo().InitPossessCreateSpells();
                owner.SendAutoRepeatCancel(me);
            }

            me.SetReactState(ReactStates.Passive);

            me.GetMotionMaster().MovePoint(1, MiscConst.EyeOFAcherusFallPoint, false);

            MoveSplineInit init = new MoveSplineInit(me);
            init.MoveTo(MiscConst.EyeOFAcherusFallPoint.GetPositionX(), MiscConst.EyeOFAcherusFallPoint.GetPositionY(), MiscConst.EyeOFAcherusFallPoint.GetPositionZ(), false);
            init.SetFall();
            init.Launch();
        }

        public override void OnCharmed(bool apply) { }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType movementType, uint pointId)
        {
            if (movementType == MovementGeneratorType.Waypoint && pointId == 2)
            {
                me.SetSheath(SheathState.Melee);
                me.RemoveAllAuras();

                Player owner = me.GetCharmerOrOwner().ToPlayer();
                if (owner)
                {
                    owner.RemoveAura(SpellIds.EyeFlightBoost);
                    for (UnitMoveType i = 0; i < UnitMoveType.Max; ++i)
                        me.SetSpeedRate(i, owner.GetSpeedRate(i));

                    Talk(TextIds.TalkControl, owner);
                }
                me.SetDisableGravity(false);
                DoCast(me, SpellIds.EyeFlight);
            }

            if (movementType == MovementGeneratorType.Point && pointId == 1)
            {
                me.SetDisableGravity(true);
                me.SetControlled(true, UnitState.Root);
                _scheduler.Schedule(System.TimeSpan.FromSeconds(5), task =>
                {
                    DoCast(me, SpellIds.EyeFlightBoost);

                    me.SetControlled(false, UnitState.Root);

                    Player owner = me.GetCharmerOrOwner().ToPlayer();
                    if (owner)
                    {
                        for (UnitMoveType i = 0; i < UnitMoveType.Max; ++i)
                            me.SetSpeedRate(i, owner.GetSpeedRate(i));
                        Talk(TextIds.TalkMoveStart, owner);
                    }
                    me.GetMotionMaster().MovePath(me.GetEntry() * 100, false);
                });
            }
        }
    }

    [Script]
    class npc_death_knight_initiate : CreatureScript
    {
        public npc_death_knight_initiate() : base("npc_death_knight_initiate") { }

        class npc_death_knight_initiateAI : CombatAI
        {
            public npc_death_knight_initiateAI(Creature creature) : base(creature)
            {
                Initialize();
            }

            void Initialize()
            {
                m_uiDuelerGUID.Clear();
                m_uiDuelTimer = 5000;
                m_bIsDuelInProgress = false;
                lose = false;
            }

            public override void Reset()
            {
                Initialize();

                me.RestoreFaction();
                base.Reset();
                me.SetFlag(UnitFields.Flags, UnitFlags.Unk15);
            }

            public override void SpellHit(Unit pCaster, SpellInfo pSpell)
            {
                if (!m_bIsDuelInProgress && pSpell.Id == SpellIds.Duel)
                {
                    m_uiDuelerGUID = pCaster.GetGUID();
                    Talk(TextIds.SayDuel, pCaster);
                    m_bIsDuelInProgress = true;
                }
            }

            public override void DamageTaken(Unit pDoneBy, ref uint uiDamage)
            {
                if (m_bIsDuelInProgress && pDoneBy.IsControlledByPlayer())
                {
                    if (pDoneBy.GetGUID() != m_uiDuelerGUID && pDoneBy.GetOwnerGUID() != m_uiDuelerGUID) // other players cannot help
                        uiDamage = 0;
                    else if (uiDamage >= me.GetHealth())
                    {
                        uiDamage = 0;

                        if (!lose)
                        {
                            pDoneBy.RemoveGameObject(SpellIds.DuelFlag, true);
                            pDoneBy.AttackStop();
                            me.CastSpell(pDoneBy, SpellIds.DuelVictory, true);
                            lose = true;
                            me.CastSpell(me, SpellIds.Grovel, true);
                            me.RestoreFaction();
                        }
                    }
                }
            }

            public override void UpdateAI(uint uiDiff)
            {
                if (!UpdateVictim())
                {
                    if (m_bIsDuelInProgress)
                    {
                        if (m_uiDuelTimer <= uiDiff)
                        {
                            me.SetFaction(MiscConst.FactionHostile);

                            Unit unit = Global.ObjAccessor.GetUnit(me, m_uiDuelerGUID);
                            if (unit)
                                AttackStart(unit);
                        }
                        else
                            m_uiDuelTimer -= uiDiff;
                    }
                    return;
                }

                if (m_bIsDuelInProgress)
                {
                    if (lose)
                    {
                        if (!me.HasAura(SpellIds.Grovel))
                            EnterEvadeMode();
                        return;
                    }
                    else if (me.GetVictim() && me.GetVictim().IsTypeId(TypeId.Player) && me.GetVictim().HealthBelowPct(10))
                    {
                        me.GetVictim().CastSpell(me.GetVictim(), SpellIds.Grovel, true); // beg
                        me.GetVictim().RemoveGameObject(SpellIds.DuelFlag, true);
                        EnterEvadeMode();
                        return;
                    }
                }

                /// @todo spells

                base.UpdateAI(uiDiff);
            }

            bool lose;
            ObjectGuid m_uiDuelerGUID;
            uint m_uiDuelTimer;
            public bool m_bIsDuelInProgress;
        }

        public override bool OnGossipSelect(Player player, Creature creature, uint sender, uint action)
        {
            ClearGossipMenuFor(player);
            if (action == eTradeskill.GossipActionInfoDef)
            {
                CloseGossipMenuFor(player);

                if (player.IsInCombat() || creature.IsInCombat())
                    return true;

                npc_death_knight_initiateAI pInitiateAI = creature.GetAI<npc_death_knight_initiateAI>();
                if (pInitiateAI != null)
                {
                    if (pInitiateAI.m_bIsDuelInProgress)
                        return true;
                }

                creature.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
                creature.RemoveFlag(UnitFields.Flags, UnitFlags.Unk15);

                player.CastSpell(creature, SpellIds.Duel, false);
                player.CastSpell(player, SpellIds.DuelFlag, true);
            }
            return true;
        }

        public override bool OnGossipHello(Player player, Creature creature)
        {
            if (player.GetQuestStatus(MiscConst.QuestDeathChallenge) == QuestStatus.Incomplete && creature.IsFullHealth())
            {
                if (player.HealthBelowPct(10))
                    return true;

                if (player.IsInCombat() || creature.IsInCombat())
                    return true;

                AddGossipItemFor(player, Player.GetDefaultGossipMenuForSource(creature), 0, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
                SendGossipMenuFor(player, player.GetGossipTextId(creature), creature.GetGUID());
            }
            return true;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new npc_death_knight_initiateAI(creature);
        }
    }

    [Script]
    class npc_dark_rider_of_acherus : ScriptedAI
    {
        public npc_dark_rider_of_acherus(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            PhaseTimer = 4000;
            Phase = 0;
            Intro = false;
            TargetGUID.Clear();
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void UpdateAI(uint diff)
        {
            if (!Intro || TargetGUID.IsEmpty())
                return;

            if (PhaseTimer <= diff)
            {
                switch (Phase)
                {
                    case 0:
                        Talk(TextIds.SayDarkRider);
                        PhaseTimer = 5000;
                        Phase = 1;
                        break;
                    case 1:
                        Unit target = Global.ObjAccessor.GetUnit(me, TargetGUID);
                        if (target)
                            DoCast(target, SpellIds.DespawnHorse, true);
                        PhaseTimer = 3000;
                        Phase = 2;
                        break;
                    case 2:
                        me.SetVisible(false);
                        PhaseTimer = 2000;
                        Phase = 3;
                        break;
                    case 3:
                        me.DespawnOrUnsummon();
                        break;
                    default:
                        break;
                }
            }
            else
                PhaseTimer -= diff;
        }

        public void InitDespawnHorse(Unit who)
        {
            if (!who)
                return;

            TargetGUID = who.GetGUID();
            me.SetWalk(true);
            me.SetSpeedRate(UnitMoveType.Run, 0.4f);
            me.GetMotionMaster().MoveChase(who);
            me.SetTarget(TargetGUID);
            Intro = true;
        }

        uint PhaseTimer;
        uint Phase;
        bool Intro;
        ObjectGuid TargetGUID;
    }

    [Script]
    class npc_salanar_the_horseman : ScriptedAI
    {
        public npc_salanar_the_horseman(Creature creature) : base(creature) { }

        public override void sGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            if (menuId == MiscConst.GossipSalanarMenu && gossipListId == MiscConst.GossipSalanarOption)
            {
                player.CastSpell(player, SpellIds.RealmOfShadows, true);
                player.PlayerTalkClass.SendCloseGossip();
            }
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            if (spell.Id == SpellIds.DeliverStolenHorse)
            {
                if (caster.IsTypeId(TypeId.Unit) && caster.IsVehicle())
                {
                    Unit charmer = caster.GetCharmer();
                    if (charmer)
                    {
                        if (charmer.HasAura(SpellIds.EffectStolenHorse))
                        {
                            charmer.RemoveAurasDueToSpell(SpellIds.EffectStolenHorse);
                            caster.RemoveFlag(UnitFields.NpcFlags, NPCFlags.SpellClick);
                            caster.SetFaction(35);
                            DoCast(caster, SpellIds.CallDarkRider, true);
                            Creature Dark_Rider = me.FindNearestCreature(CreatureIds.DarkRiderOfAcherus, 15);
                            if (Dark_Rider)
                                Dark_Rider.GetAI<npc_dark_rider_of_acherus>().InitDespawnHorse(caster);
                        }
                    }
                }
            }
        }

        public override void MoveInLineOfSight(Unit who)
        {
            base.MoveInLineOfSight(who);

            if (who.IsTypeId(TypeId.Unit) && who.IsVehicle() && me.IsWithinDistInMap(who, 5.0f))
            {
                Unit charmer = who.GetCharmer();
                if (charmer)
                {
                    Player player = charmer.ToPlayer();
                    if (player)
                    {
                        // for quest Into the Realm of Shadows(QUEST_INTO_REALM_OF_SHADOWS)
                        if (me.GetEntry() == CreatureIds.SalanarInRealmOfShadows && player.GetQuestStatus(MiscConst.QuestIntoRealmOfShadows) == QuestStatus.Incomplete)
                        {
                            player.GroupEventHappens(MiscConst.QuestIntoRealmOfShadows, me);
                            Talk(TextIds.SaySalanar);
                            charmer.RemoveAurasDueToSpell(SpellIds.EffectOvertake);
                            Creature creature = who.ToCreature();
                            if (creature)
                            {
                                creature.DespawnOrUnsummon();
                                //creature.Respawn(true);
                            }
                        }

                        player.RemoveAurasDueToSpell(SpellIds.RealmOfShadows);
                    }
                }
            }
        }
    }

    [Script]
    class npc_ros_dark_rider : ScriptedAI
    {
        public npc_ros_dark_rider(Creature creature) : base(creature) { }

        public override void EnterCombat(Unit who)
        {
            me.ExitVehicle();
        }

        public override void Reset()
        {
            Creature deathcharger = me.FindNearestCreature(28782, 30);
            if (!deathcharger)
                return;

            deathcharger.RestoreFaction();
            deathcharger.RemoveFlag(UnitFields.NpcFlags, NPCFlags.SpellClick);
            deathcharger.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            if (!me.GetVehicle() && deathcharger.IsVehicle() && deathcharger.GetVehicleKit().HasEmptySeat(0))
                me.EnterVehicle(deathcharger);
        }

        public override void JustDied(Unit killer)
        {
            Creature deathcharger = me.FindNearestCreature(28782, 30);
            if (!deathcharger)
                return;

            if (killer.IsTypeId(TypeId.Player) && deathcharger.IsTypeId(TypeId.Unit) && deathcharger.IsVehicle())
            {
                deathcharger.SetFlag(UnitFields.NpcFlags, NPCFlags.SpellClick);
                deathcharger.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                deathcharger.SetFaction(2096);
            }
        }
    }

    [Script]
    class npc_dkc1_gothik : ScriptedAI
    {
        public npc_dkc1_gothik(Creature creature) : base(creature) { }

        public override void MoveInLineOfSight(Unit who)
        {
            base.MoveInLineOfSight(who);

            if (who.GetEntry() == CreatureIds.Ghouls && me.IsWithinDistInMap(who, 10.0f))
            {
                Unit owner = who.GetOwner();
                if (owner)
                {
                    Player player = owner.ToPlayer();
                    if (player)
                    {
                        Creature creature = who.ToCreature();
                        if (player.GetQuestStatus(12698) == QuestStatus.Incomplete)
                            creature.CastSpell(owner, 52517, true);

                        /// @todo Creatures must not be removed, but, must instead
                        //      stand next to Gothik and be commanded into the pit
                        //      and dig into the ground.
                        creature.DespawnOrUnsummon();

                        if (player.GetQuestStatus(12698) == QuestStatus.Complete)
                            owner.RemoveAllMinionsByEntry(CreatureIds.Ghosts);
                    }
                }
            }
        }
    }

    [Script]
    class npc_scarlet_ghoul : ScriptedAI
    {
        public npc_scarlet_ghoul(Creature creature) : base(creature)
        {
            // Ghouls should display their Birth Animation
            // Crawling out of the ground
            //DoCast(me, 35177, true);
            //me.MonsterSay("Mommy?", LANG_UNIVERSAL, 0);
            me.SetReactState(ReactStates.Defensive);
        }

        void FindMinions(Unit owner)
        {
            List<TempSummon> MinionList = new List<TempSummon>();
            owner.GetAllMinionsByEntry(MinionList, CreatureIds.Ghouls);

            foreach (TempSummon summon in MinionList)
                if (summon.GetOwnerGUID() == me.GetOwnerGUID())
                    if (summon.IsInCombat() && summon.getAttackerForHelper())
                        AttackStart(summon.getAttackerForHelper());
        }

        public override void UpdateAI(uint diff)
        {
            if (!me.IsInCombat())
            {
                Unit owner = me.GetOwner();
                if (owner)
                {
                    Player plrOwner = owner.ToPlayer();
                    if (plrOwner && plrOwner.IsInCombat())
                    {
                        if (plrOwner.getAttackerForHelper() && plrOwner.getAttackerForHelper().GetEntry() == CreatureIds.Ghosts)
                            AttackStart(plrOwner.getAttackerForHelper());
                        else
                            FindMinions(owner);
                    }
                }
            }

            if (!UpdateVictim() || !me.GetVictim())
                return;

            //ScriptedAI::UpdateAI(diff);
            //Check if we have a current target
            if (me.GetVictim().GetEntry() == CreatureIds.Ghosts)
            {
                if (me.isAttackReady())
                {
                    //If we are within range melee the target
                    if (me.IsWithinMeleeRange(me.GetVictim()))
                    {
                        me.AttackerStateUpdate(me.GetVictim());
                        me.resetAttackTimer();
                    }
                }
            }
        }
    }

    [Script]
    class npc_scarlet_miner_cart : PassiveAI
    {
        public npc_scarlet_miner_cart(Creature creature) : base(creature)
        {
            me.SetDisplayFromModel(0); // Modelid2
        }

        public override void JustSummoned(Creature summon)
        {
            if (summon.GetEntry() == CreatureIds.Miner)
            {
                _minerGUID = summon.GetGUID();
                summon.GetAI().SetGUID(_playerGUID);
            }
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            if (summon.GetEntry() == CreatureIds.Miner)
                _minerGUID.Clear();
        }

        public override void DoAction(int param)
        {
            Creature miner = ObjectAccessor.GetCreature(me, _minerGUID);
            if (miner)
            {
                me.SetWalk(false);

                // Not 100% correct, but movement is smooth. Sometimes miner walks faster
                // than normal, this speed is fast enough to keep up at those times.
                me.SetSpeedRate(UnitMoveType.Run, 1.25f);

                me.GetMotionMaster().MoveFollow(miner, 1.0f, 0);
            }
        }

        public override void PassengerBoarded(Unit who, sbyte seatId, bool apply)
        {
            if (apply)
            {
                _playerGUID = who.GetGUID();
                me.CastSpell((Unit)null, SpellIds.SummonMiner, true);
            }
            else
            {
                _playerGUID.Clear();
                Creature miner = ObjectAccessor.GetCreature(me, _minerGUID);
                if (miner)
                    miner.DespawnOrUnsummon();
            }
        }

        ObjectGuid _minerGUID;
        ObjectGuid _playerGUID;
    }

    [Script]
    class npc_scarlet_miner : npc_escortAI
    {
        public npc_scarlet_miner(Creature creature) : base(creature)
        {
            Initialize();
            me.SetReactState(ReactStates.Passive);
        }

        void Initialize()
        {
            carGUID.Clear();
            IntroTimer = 0;
            IntroPhase = 0;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void IsSummonedBy(Unit summoner)
        {
            carGUID = summoner.GetGUID();
        }

        void InitWaypoint()
        {
            AddWaypoint(1, 2389.03f, -5902.74f, 109.014f, 5000);
            AddWaypoint(2, 2341.812012f, -5900.484863f, 102.619743f);
            AddWaypoint(3, 2306.561279f, -5901.738281f, 91.792419f);
            AddWaypoint(4, 2300.098389f, -5912.618652f, 86.014885f);
            AddWaypoint(5, 2294.142090f, -5927.274414f, 75.316849f);
            AddWaypoint(6, 2286.984375f, -5944.955566f, 63.714966f);
            AddWaypoint(7, 2280.001709f, -5961.186035f, 54.228283f);
            AddWaypoint(8, 2259.389648f, -5974.197754f, 42.359348f);
            AddWaypoint(9, 2242.882812f, -5984.642578f, 32.827850f);
            AddWaypoint(10, 2217.265625f, -6028.959473f, 7.675705f);
            AddWaypoint(11, 2202.595947f, -6061.325684f, 5.882018f);
            AddWaypoint(12, 2188.974609f, -6080.866699f, 3.370027f);

            if (RandomHelper.URand(0, 1) != 0)
            {
                AddWaypoint(13, 2176.483887f, -6110.407227f, 1.855181f);
                AddWaypoint(14, 2172.516602f, -6146.752441f, 1.074235f);
                AddWaypoint(15, 2138.918457f, -6158.920898f, 1.342926f);
                AddWaypoint(16, 2129.866699f, -6174.107910f, 4.380779f);
                AddWaypoint(17, 2117.709473f, -6193.830078f, 13.3542f, 10000);
            }
            else
            {
                AddWaypoint(13, 2184.190186f, -6166.447266f, 0.968877f);
                AddWaypoint(14, 2234.265625f, -6163.741211f, 0.916021f);
                AddWaypoint(15, 2268.071777f, -6158.750977f, 1.822252f);
                AddWaypoint(16, 2270.028320f, -6176.505859f, 6.340538f);
                AddWaypoint(17, 2271.739014f, -6195.401855f, 13.3542f, 10000);
            }
        }

        public override void SetGUID(ObjectGuid guid, int id = 0)
        {
            InitWaypoint();
            Start(false, false, guid);
            SetDespawnAtFar(false);
        }

        public override void WaypointReached(uint waypointId)
        {
            switch (waypointId)
            {
                case 1:
                    {
                        Unit car = ObjectAccessor.GetCreature(me, carGUID);
                        if (car)
                            me.SetFacingToObject(car);
                        Talk(TextIds.SayScarletMiner0);
                        SetRun(true);
                        IntroTimer = 4000;
                        IntroPhase = 1;
                        break;
                    }
                case 17:
                    {
                        Unit car = ObjectAccessor.GetCreature(me, carGUID);
                        if (car)
                        {
                            me.SetFacingToObject(car);
                            car.Relocate(car.GetPositionX(), car.GetPositionY(), me.GetPositionZ() + 1);
                            car.StopMoving();
                            car.RemoveAura(SpellIds.CartDrag);
                        }
                        Talk(TextIds.SayScarletMiner1);
                        break;
                    }
                default:
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (IntroPhase != 0)
            {
                if (IntroTimer <= diff)
                {
                    if (IntroPhase == 1)
                    {
                        Creature car = ObjectAccessor.GetCreature(me, carGUID);
                        if (car)
                            DoCast(car, SpellIds.CartDrag);
                        IntroTimer = 800;
                        IntroPhase = 2;
                    }
                    else
                    {
                        Creature car = ObjectAccessor.GetCreature(me, carGUID);
                        if (car)
                            car.GetAI().DoAction(0);
                        IntroPhase = 0;
                    }
                }
                else
                    IntroTimer -= diff;
            }
            base.UpdateAI(diff);
        }

        uint IntroTimer;
        uint IntroPhase;
        ObjectGuid carGUID;
    }
}
