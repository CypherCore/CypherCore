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
using System;

namespace Scripts.Kalimdor.ZoneAshenvale
{
    struct CreatureIds
    {
        //RuulSnowhoof
        public const uint ThistlefurUrsa = 3921;
        public const uint ThistlefurTotemic = 3922;
        public const uint ThistlefurPathfinder = 3926;

        //Muglash
        public const uint WrathRider = 3713;
        public const uint WrathSorceress = 3717;
        public const uint WrathRazortail = 3712;
        public const uint WrathPriestess = 3944;
        public const uint WrathMyrmidon = 3711;
        public const uint WrathSeawitch = 3715;
        public const uint Vorsha = 12940;
        public const uint Muglash = 12717;
    }

    struct GameObjectIds
    {
        //RuulSnowhoof
        public const uint Cage = 178147;

        //Muglash
        public const uint NagaBrazier = 178247;

        //KingoftheFoulweald
        public const uint Banner = 178205;
    }

    struct QuestIds
    {
        //RuulSnowhoof
        public const uint FreedomToRuul = 6482;

        //Muglash
        public const uint Vorsha = 6641;
    }

    struct TextIds
    {
        //Muglash
        public const uint SayMugStart1 = 0;
        public const uint SayMugStart2 = 1;
        public const uint SayMugBrazier = 2;
        public const uint SayMugBrazierWait = 3;
        public const uint SayMugOnGuard = 4;
        public const uint SayMugRest = 5;
        public const uint SayMugDone = 6;
        public const uint SayMugGratitude = 7;
        public const uint SayMugPatrol = 8;
        public const uint SayMugReturn = 9;
    }

    struct Misc
    {
        //RuulSnowhoof
        public const uint FactionQuest = 113;
        public static Position[] RuulSnowhoofSummonsCoord =
        {
            new Position(3449.218018f, -587.825073f, 174.978867f, 4.714445f),
            new Position(3446.384521f, -587.830872f, 175.186279f, 4.714445f),
            new Position(3444.218994f, -587.835327f, 175.380600f, 4.714445f),
            new Position(3508.344482f, -492.024261f, 186.929031f, 4.145029f),
            new Position(3506.265625f, -490.531006f, 186.740128f, 4.239277f),
            new Position(3503.682373f, -489.393799f, 186.629684f, 4.349232f)
        };

        //Muglash
        public static Position[] FirstNagaCoord =
        {
            new Position(3603.504150f, 1122.631104f,  1.635f, 0.0f),        // rider
            new Position(3589.293945f, 1148.664063f,  5.565f, 0.0f),        // sorceress
            new Position(3609.925537f, 1168.759521f, -1.168f, 0.0f)         // razortail
        };

        public static Position[] SecondNagaCoord =
        {
            new Position(3609.925537f, 1168.759521f, -1.168f, 0.0f),        // witch
            new Position(3645.652100f, 1139.425415f, 1.322f,  0.0f),        // priest
            new Position(3583.602051f, 1128.405762f, 2.347f,  0.0f)         // myrmidon
        };
        public static Position VorshaCoord = new Position(3633.056885f, 1172.924072f, -5.388f, 0.0f);
    }


    [Script]
    class npc_ruul_snowhoof : npc_escortAI
    {
        public npc_ruul_snowhoof(Creature creature) : base(creature) { }

        public override void Reset()
        {
            GameObject cage = me.FindNearestGameObject(GameObjectIds.Cage, 20);
            if (cage)
                cage.SetGoState(GameObjectState.Ready);
        }

        public override void EnterCombat(Unit who) { }

        public override void JustSummoned(Creature summoned)
        {
            summoned.GetAI().AttackStart(me);
        }

        public override void sQuestAccept(Player player, Quest quest)
        {
            if (quest.Id == QuestIds.FreedomToRuul)
            {
                me.SetFaction(Misc.FactionQuest);
                Start(true, false, player.GetGUID());
            }
        }

        public override void WaypointReached(uint waypointId)
        {
            Player player = GetPlayerForEscort();
            if (!player)
                return;

            switch (waypointId)
            {
                case 0:
                    me.SetUInt32Value(UnitFields.Bytes1, 0);
                    GameObject cage = me.FindNearestGameObject(GameObjectIds.Cage, 20);
                    if (cage)
                        cage.SetGoState(GameObjectState.Active);
                    break;
                case 13:
                    me.SummonCreature(CreatureIds.ThistlefurTotemic, Misc.RuulSnowhoofSummonsCoord[0], TempSummonType.DeadDespawn, 60000);
                    me.SummonCreature(CreatureIds.ThistlefurUrsa, Misc.RuulSnowhoofSummonsCoord[1], TempSummonType.DeadDespawn, 60000);
                    me.SummonCreature(CreatureIds.ThistlefurPathfinder, Misc.RuulSnowhoofSummonsCoord[2], TempSummonType.DeadDespawn, 60000);
                    break;
                case 19:
                    me.SummonCreature(CreatureIds.ThistlefurTotemic, Misc.RuulSnowhoofSummonsCoord[3], TempSummonType.DeadDespawn, 60000);
                    me.SummonCreature(CreatureIds.ThistlefurUrsa, Misc.RuulSnowhoofSummonsCoord[4], TempSummonType.DeadDespawn, 60000);
                    me.SummonCreature(CreatureIds.ThistlefurPathfinder, Misc.RuulSnowhoofSummonsCoord[5], TempSummonType.DeadDespawn, 60000);
                    break;
                case 21:
                    player.GroupEventHappens(QuestIds.FreedomToRuul, me);
                    break;
            }
        }
    }

    [Script]
    public class npc_muglash : npc_escortAI
    {
        public npc_muglash(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            eventTimer = 10000;
            waveId = 0;
            _isBrazierExtinguished = false;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void EnterCombat(Unit who)
        {
            Player player = GetPlayerForEscort();
            if (player)
            {
                if (HasEscortState(eEscortState.Paused))
                {
                    if (Convert.ToBoolean(RandomHelper.URand(0, 1)))
                        Talk(TextIds.SayMugOnGuard, player);
                    return;
                }
            }
        }

        public override void JustDied(Unit killer)
        {
            if (HasEscortState(eEscortState.Escorting))
            {
                Player player = GetPlayerForEscort();
                if (player)
                    player.FailQuest(QuestIds.Vorsha);
            }
        }

        public override void JustSummoned(Creature summoned)
        {
            summoned.GetAI().AttackStart(me);
        }

        public override void sQuestAccept(Player player, Quest quest)
        {
            if (quest.Id == QuestIds.Vorsha)
            {
                Talk(TextIds.SayMugStart1);
                me.SetFaction(Misc.FactionQuest);
                Start(true, false, player.GetGUID());
            }
        }

        public override void WaypointReached(uint waypointId)
        {
            Player player = GetPlayerForEscort();
            if (player)
            {
                switch (waypointId)
                {
                    case 0:
                        Talk(TextIds.SayMugStart2, player);
                        break;
                    case 24:
                        Talk(TextIds.SayMugBrazier, player);

                        GameObject go = GetClosestGameObjectWithEntry(me, GameObjectIds.NagaBrazier, SharedConst.InteractionDistance * 2);
                        if (go)
                        {
                            go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                            SetEscortPaused(true);
                        }
                        break;
                    case 25:
                        Talk(TextIds.SayMugGratitude);
                        player.GroupEventHappens(QuestIds.Vorsha, me);
                        break;
                    case 26:
                        Talk(TextIds.SayMugPatrol);
                        break;
                    case 27:
                        Talk(TextIds.SayMugReturn);
                        break;
                }
            }
        }

        void DoWaveSummon()
        {
            switch (waveId)
            {
                case 1:
                    me.SummonCreature(CreatureIds.WrathRider, Misc.FirstNagaCoord[0], TempSummonType.TimedDespawnOOC, 60000);
                    me.SummonCreature(CreatureIds.WrathSorceress, Misc.FirstNagaCoord[1], TempSummonType.TimedDespawnOOC, 60000);
                    me.SummonCreature(CreatureIds.WrathRazortail, Misc.FirstNagaCoord[2], TempSummonType.TimedDespawnOOC, 60000);
                    break;
                case 2:
                    me.SummonCreature(CreatureIds.WrathPriestess, Misc.SecondNagaCoord[0], TempSummonType.TimedDespawnOOC, 60000);
                    me.SummonCreature(CreatureIds.WrathMyrmidon, Misc.SecondNagaCoord[1], TempSummonType.TimedDespawnOOC, 60000);
                    me.SummonCreature(CreatureIds.WrathSeawitch, Misc.SecondNagaCoord[2], TempSummonType.TimedDespawnOOC, 60000);
                    break;
                case 3:
                    me.SummonCreature(CreatureIds.Vorsha, Misc.VorshaCoord, TempSummonType.TimedDespawnOOC, 60000);
                    break;
                case 4:
                    SetEscortPaused(false);
                    Talk(TextIds.SayMugDone);
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            base.UpdateAI(diff);

            if (!me.GetVictim())
            {
                if (HasEscortState(eEscortState.Paused) && _isBrazierExtinguished)
                {
                    if (eventTimer < diff)
                    {
                        ++waveId;
                        DoWaveSummon();
                        eventTimer = 10000;
                    }
                    else
                        eventTimer -= diff;
                }
                return;
            }
            DoMeleeAttackIfReady();
        }

        uint eventTimer;
        byte waveId;

        public bool _isBrazierExtinguished { get; set; }
    }

    [Script]
    class go_naga_brazier : GameObjectScript
    {
        public go_naga_brazier() : base("go_naga_brazier") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            Creature creature = ScriptedAI.GetClosestCreatureWithEntry(go, CreatureIds.Muglash, SharedConst.InteractionDistance * 2);
            if (creature)
            {
                npc_muglash pEscortAI = creature.GetAI<npc_muglash>();
                if (pEscortAI != null)
                {
                    creature.GetAI().Talk(TextIds.SayMugBrazierWait);

                    pEscortAI._isBrazierExtinguished = true;
                    return false;
                }
            }

            return true;
        }
    }

    [Script]
    class spell_destroy_karangs_banner : SpellScript
    {
        void HandleAfterCast()
        {
            GameObject banner = GetCaster().FindNearestGameObject(GameObjectIds.Banner, GetSpellInfo().GetMaxRange(true));
            if (banner)
                banner.Delete();
        }

        public override void Register()
        {
            AfterCast.Add(new CastHandler(HandleAfterCast));
        }
    }
}
