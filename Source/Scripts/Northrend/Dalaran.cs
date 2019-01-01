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
using Framework.Database;
using Game.AI;
using Game.Entities;
using Game.Mails;
using Game.Scripting;
using System.Collections.Generic;

namespace Scripts.Northrend
{
    struct DalaranConst
    {
        //Mageguard
        public const uint SpellTrespasserA = 54028;
        public const uint SpellTrespasserH = 54029;

        public const uint SpellSunreaverDisguiseFemale = 70973;
        public const uint SpellSunreaverDisguiseMale = 70974;
        public const uint SpellSilverConenantDisguiseFemale = 70971;
        public const uint SpellSilverConenantDisguiseMale = 70972;

        public const int NpcAplleboughA = 29547;
        public const int NpcSweetberryH = 29715;

        //Minigob
        public const int SpellManabonked = 61834;
        public const int SpellTeleportVisual = 51347;
        public const int SpellImprovedBlink = 61995;

        public const int EventSelectTarget = 1;
        public const int EventBlink = 2;
        public const int EventDespawnVisual = 3;
        public const int EventDespawn = 4;

        public const int MailMinigobEntry = 264;
        public const int MailDeliverDelayMin = 5 * Time.Minute;
        public const int MailDeliverDelayMax = 15 * Time.Minute;
    }

    [Script]
    class npc_mageguard_dalaran : ScriptedAI
    {
        public npc_mageguard_dalaran(Creature creature) : base(creature)
        {
            creature.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
            creature.ApplySpellImmune(0, SpellImmunity.Damage, (uint)SpellSchools.Normal, true);
            creature.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Magic, true);
        }

        public override void Reset() { }

        public override void EnterCombat(Unit who) { }

        public override void AttackStart(Unit who) { }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!who || !who.IsInWorld || who.GetZoneId() != 4395)
                return;

            if (!me.IsWithinDist(who, 65.0f, false))
                return;

            Player player = who.GetCharmerOrOwnerPlayerOrPlayerItself();

            if (!player || player.IsGameMaster() || player.IsBeingTeleported() ||
                // If player has Disguise aura for quest A Meeting With The Magister or An Audience With The Arcanist, do not teleport it away but let it pass
                player.HasAura(DalaranConst.SpellSunreaverDisguiseFemale) || player.HasAura(DalaranConst.SpellSunreaverDisguiseMale) ||
                player.HasAura(DalaranConst.SpellSilverConenantDisguiseFemale) || player.HasAura(DalaranConst.SpellSilverConenantDisguiseMale))
                return;

            switch (me.GetEntry())
            {
                case 29254:
                    if (player.GetTeam() == Team.Horde)              // Horde unit found in Alliance area
                    {
                        if (GetClosestCreatureWithEntry(me, DalaranConst.NpcAplleboughA, 32.0f))
                        {
                            if (me.isInBackInMap(who, 12.0f))   // In my line of sight, "outdoors", and behind me
                                DoCast(who, DalaranConst.SpellTrespasserA); // Teleport the Horde unit out
                        }
                        else                                      // In my line of sight, and "indoors"
                            DoCast(who, DalaranConst.SpellTrespasserA);     // Teleport the Horde unit out
                    }
                    break;
                case 29255:
                    if (player.GetTeam() == Team.Alliance)           // Alliance unit found in Horde area
                    {
                        if (GetClosestCreatureWithEntry(me, DalaranConst.NpcSweetberryH, 32.0f))
                        {
                            if (me.isInBackInMap(who, 12.0f))   // In my line of sight, "outdoors", and behind me
                                DoCast(who, DalaranConst.SpellTrespasserH); // Teleport the Alliance unit out
                        }
                        else                                      // In my line of sight, and "indoors"
                            DoCast(who, DalaranConst.SpellTrespasserH);     // Teleport the Alliance unit out
                    }
                    break;
            }
            me.SetOrientation(me.GetHomePosition().GetOrientation());
            return;
        }

        public override void UpdateAI(uint diff) { }
    }

    [Script]
    class npc_minigob_manabonk : ScriptedAI
    {
        public npc_minigob_manabonk(Creature creature) : base(creature)
        {
            me.setActive(true);
        }

        public override void Reset()
        {
            me.SetVisible(false);
            _events.ScheduleEvent(DalaranConst.EventSelectTarget, Time.InMilliseconds);
        }

        Player SelectTargetInDalaran()
        {
            List<Player> PlayerInDalaranList = new List<Player>();

            var players = me.GetMap().GetPlayers();
            foreach (var player in players)
            {
                if (player.GetZoneId() == me.GetZoneId() && !player.IsFlying() && !player.IsMounted() && !player.IsGameMaster())
                    PlayerInDalaranList.Add(player);
            }

            if (PlayerInDalaranList.Empty())
                return null;

            return PlayerInDalaranList.SelectRandom();
        }

        void SendMailToPlayer(Player player)
        {
            SQLTransaction trans = new SQLTransaction();
            uint deliverDelay = RandomHelper.URand(DalaranConst.MailDeliverDelayMin, DalaranConst.MailDeliverDelayMax);
            new MailDraft(DalaranConst.MailMinigobEntry, true).SendMailTo(trans, new MailReceiver(player), new MailSender(MailMessageType.Creature, me.GetEntry()), MailCheckMask.None, deliverDelay);
            DB.Characters.CommitTransaction(trans);
        }

        public override void UpdateAI(uint diff)
        {
            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case DalaranConst.EventSelectTarget:
                        me.SetVisible(true);
                        DoCast(me, DalaranConst.SpellTeleportVisual);
                        Player player = SelectTargetInDalaran();
                        if (player)
                        {
                            me.NearTeleportTo(player.GetPositionX(), player.GetPositionY(), player.GetPositionZ(), 0.0f);
                            DoCast(player, DalaranConst.SpellManabonked);
                            SendMailToPlayer(player);
                        }
                        _events.ScheduleEvent(DalaranConst.EventBlink, 3 * Time.InMilliseconds);
                        break;
                    case DalaranConst.EventBlink:
                        {
                            DoCast(me, DalaranConst.SpellImprovedBlink);
                            Position pos = me.GetRandomNearPosition(RandomHelper.FRand(15, 40));
                            me.GetMotionMaster().MovePoint(0, pos.posX, pos.posY, pos.posZ);
                            _events.ScheduleEvent(DalaranConst.EventDespawn, 3 * Time.InMilliseconds);
                            _events.ScheduleEvent(DalaranConst.EventDespawnVisual, (uint)(2.5 * Time.InMilliseconds));
                            break;
                        }
                    case DalaranConst.EventDespawnVisual:
                        DoCast(me, DalaranConst.SpellTeleportVisual);
                        break;
                    case DalaranConst.EventDespawn:
                        me.DespawnOrUnsummon();
                        break;
                    default:
                        break;
                }
            });
        }
    }
}
