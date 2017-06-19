/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms
{
    [Script]
    class npc_apprentice_mirveda : CreatureScript
    {
        public npc_apprentice_mirveda() : base("npc_apprentice_mirveda") { }

        class npc_apprentice_mirvedaAI : ScriptedAI
        {
            public npc_apprentice_mirvedaAI(Creature creature)
                : base(creature)
            {
                Summons = new SummonList(me);
            }

            public override void Reset()
            {
                SetCombatMovement(false);
                KillCount = 0;
                PlayerGUID.Clear();
                Summons.DespawnAll();
            }

            public override void sQuestReward(Player player, Quest quest, uint opt)
            {
                if (quest.Id == QUEST_CORRUPTED_SOIL)
                {
                    me.RemoveFlag64(UnitFields.NpcFlags, NPCFlags.QuestGiver);
                    _events.ScheduleEvent(EventTalk, 2000);
                }
            }

            public override void sQuestAccept(Player player, Quest quest)
            {
                if (quest.Id == QUEST_UNEXPECTED_RESULT)
                {
                    me.SetFaction(FactionCombat);
                    me.RemoveFlag64(UnitFields.NpcFlags, NPCFlags.QuestGiver);
                    _events.ScheduleEvent(EventSummon, 1000);
                    PlayerGUID = player.GetGUID();
                }
            }

            public override void EnterCombat(Unit who)
            {
                _events.ScheduleEvent(EventFireball, 1000);
            }

            public override void JustSummoned(Creature summoned)
            {
                // This is the best I can do because AttackStart does nothing
                summoned.GetMotionMaster().MovePoint(1, me.GetPositionX(), me.GetPositionY(), me.GetPositionZ());
                // summoned.AI().AttackStart(me);
                Summons.Summon(summoned);
            }

            public override void SummonedCreatureDies(Creature summoned, Unit who)
            {
                Summons.Despawn(summoned);
                ++KillCount;
            }

            public override void JustDied(Unit killer)
            {
                me.SetFaction(FactionNormal);

                if (!PlayerGUID.IsEmpty())
                {
                    Player player = Global.ObjAccessor.GetPlayer(me, PlayerGUID);
                    if (player)
                        player.FailQuest(QUEST_UNEXPECTED_RESULT);
                }
            }

            public override void UpdateAI(uint diff)
            {
                if (KillCount >= 3 && !PlayerGUID.IsEmpty())
                {
                    Player player = Global.ObjAccessor.GetPlayer(me, PlayerGUID);
                    if (player)
                    {
                        if (player.GetQuestStatus(QUEST_UNEXPECTED_RESULT) == QuestStatus.Incomplete)
                        {
                            player.CompleteQuest(QUEST_UNEXPECTED_RESULT);
                            me.SetFaction(FactionNormal);
                            me.SetFlag64(UnitFields.NpcFlags, NPCFlags.QuestGiver);
                        }
                    }
                }

                _events.Update(diff);
                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case EventTalk:
                            Talk(SayTestSoil);
                            _events.ScheduleEvent(EventAddQuestGiverFlag, 7000);
                            break;
                        case EventAddQuestGiverFlag:
                            me.SetFlag64(UnitFields.NpcFlags, NPCFlags.QuestGiver);
                            break;
                        case EventSummon:
                            me.SummonCreature(NPC_GHARZUL, 8749.505f, -7132.595f, 35.31983f, 3.816502f, TempSummonType.CorpseTimedDespawn, 180000);
                            me.SummonCreature(NPC_ANGERSHADE, 8755.38f, -7131.521f, 35.30957f, 3.816502f, TempSummonType.CorpseTimedDespawn, 180000);
                            me.SummonCreature(NPC_ANGERSHADE, 8753.199f, -7125.975f, 35.31986f, 3.816502f, TempSummonType.CorpseTimedDespawn, 180000);
                            break;
                        case EventFireball:
                            if (UpdateVictim())
                            {
                                DoCastVictim(SpellFireball, true);  // Not casting in combat
                                _events.ScheduleEvent(EventFireball, 3000);
                            }
                            break;
                        default:
                            break;
                    }
                });
                DoMeleeAttackIfReady();
            }

            uint KillCount;
            ObjectGuid PlayerGUID;
            SummonList Summons;
        }

        const uint EventTalk = 1;     // Quest 8487
        const uint EventAddQuestGiverFlag = 2;     // Quest 8487
        const uint EventSummon = 3;     // Quest 8488
        const uint EventFireball = 4;      // Quest 8488

        // Creatures
        const uint NPC_GHARZUL = 15958; // Quest 8488
        const uint NPC_ANGERSHADE = 15656; // Quest 8488

        // Spells
        const uint SpellTestSoil = 29535; // Quest 8487
        const uint SpellFireball = 20811; // Quest 8488

        //Texts
        const uint SayTestSoil = 0;     // Quest 8487

        // Factions
        const uint FactionNormal = 1604;  // Quest 8488
        const uint FactionCombat = 232;   // Quest 8488

        // Quest
        const uint QUEST_CORRUPTED_SOIL = 8487;
        const uint QUEST_UNEXPECTED_RESULT = 8488;

        public override CreatureAI GetAI(Creature creature)
        {
            return new npc_apprentice_mirvedaAI(creature);
        }
    }

    [Script]
    class npc_infused_crystal : CreatureScript
    {
        public npc_infused_crystal() : base("npc_infused_crystal") { }

        class npc_infused_crystalAI : ScriptedAI
        {
            public npc_infused_crystalAI(Creature creature)
                : base(creature)
            {
                SetCombatMovement(false);
            }

            public override void Reset()
            {
                EndTimer = 0;
                Completed = false;
                Progress = false;
                PlayerGUID.Clear();
                WaveTimer = 0;
            }

            public override void MoveInLineOfSight(Unit who)
            {
                if (!Progress && who.IsTypeId(TypeId.Player) && me.IsWithinDistInMap(who, 10.0f))
                {
                    if (who.ToPlayer().GetQuestStatus(QuestPoweringOurDefenses) == QuestStatus.Incomplete)
                    {
                        PlayerGUID = who.GetGUID();
                        WaveTimer = 1000;
                        EndTimer = 60000;
                        Progress = true;
                    }
                }
            }

            public override void JustSummoned(Creature summoned)
            {
                summoned.GetAI().AttackStart(me);
            }

            public override void JustDied(Unit killer)
            {
                if (!PlayerGUID.IsEmpty() && !Completed)
                {
                    Player player = Global.ObjAccessor.GetPlayer(me, PlayerGUID);
                    if (player)
                        player.FailQuest(QuestPoweringOurDefenses);
                }
            }

            public override void UpdateAI(uint diff)
            {
                if (EndTimer < diff && Progress)
                {
                    Talk(Emote);
                    Completed = true;
                    if (!PlayerGUID.IsEmpty())
                    {
                        Player player = Global.ObjAccessor.GetPlayer(me, PlayerGUID);
                        if (player)
                            player.CompleteQuest(QuestPoweringOurDefenses);
                    }

                    me.DealDamage(me, (uint)me.GetHealth(), null, DamageEffectType.Direct, SpellSchoolMask.Normal, null, false);
                    me.RemoveCorpse();
                }
                else EndTimer -= diff;

                if (WaveTimer < diff && !Completed && Progress)
                {
                    uint ran1 = RandomHelper.Rand32() % 8;
                    uint ran2 = RandomHelper.Rand32() % 8;
                    uint ran3 = RandomHelper.Rand32() % 8;
                    me.SummonCreature(NpcEnragedWeaith, SpawnLocations[ran1].X, SpawnLocations[ran1].Y, SpawnLocations[ran1].Z, 0, TempSummonType.TimedOrCorpseDespawn, 10000);
                    me.SummonCreature(NpcEnragedWeaith, SpawnLocations[ran2].X, SpawnLocations[ran2].Y, SpawnLocations[ran2].Z, 0, TempSummonType.TimedOrCorpseDespawn, 10000);
                    me.SummonCreature(NpcEnragedWeaith, SpawnLocations[ran3].X, SpawnLocations[ran3].Y, SpawnLocations[ran3].Z, 0, TempSummonType.TimedOrCorpseDespawn, 10000);
                    WaveTimer = 30000;
                }
                else WaveTimer -= diff;
            }

            uint EndTimer;
            uint WaveTimer;
            bool Completed;
            bool Progress;
            ObjectGuid PlayerGUID;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new npc_infused_crystalAI(creature);
        }

        // Quest
        const uint QuestPoweringOurDefenses = 8490;

        // Says
        const uint Emote = 0;

        // Creatures
        const uint NpcEnragedWeaith = 17086;

        static Vector3[] SpawnLocations =
        {
            new Vector3(8270.68f, -7188.53f, 139.619f),
            new Vector3(8284.27f, -7187.78f, 139.603f),
            new Vector3(8297.43f, -7193.53f, 139.603f),
            new Vector3(8303.5f, -7201.96f, 139.577f),
            new Vector3(8273.22f, -7241.82f, 139.382f),
            new Vector3(8254.89f, -7222.12f, 139.603f),
            new Vector3(8278.51f, -7242.13f, 139.162f),
            new Vector3(8267.97f, -7239.17f, 139.517f)
        };
    }
}
