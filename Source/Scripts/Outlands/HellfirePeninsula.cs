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
using Game.PvP;

namespace Scripts.Outlands
{
    struct Aeranas
    {
        public const uint SaySummon = 0;
        public const uint SayFree = 1;

        public const uint FactionHostile = 16;
        public const uint FactionFriendly = 35;

        public const uint SpellEncelopingWinds = 15535;
        public const uint SpellShock = 12553;
    }

    [Script]
    class npc_aeranas : ScriptedAI
    {
        public npc_aeranas(Creature creature) : base(creature) { }

        public override void Reset()
        {
            faction_Timer = 8000;
            envelopingWinds_Timer = 9000;
            shock_Timer = 5000;

            me.RemoveFlag64(UnitFields.NpcFlags, NPCFlags.QuestGiver);
            me.SetFaction(Aeranas.FactionFriendly);

            Talk(Aeranas.SaySummon);
        }

        public override void UpdateAI(uint diff)
        {
            if (faction_Timer != 0)
            {
                if (faction_Timer <= diff)
                {
                    me.SetFaction(Aeranas.FactionHostile);
                    faction_Timer = 0;
                }
                else
                    faction_Timer -= diff;
            }

            if (!UpdateVictim())
                return;

            if (HealthBelowPct(30))
            {
                me.SetFaction(Aeranas.FactionFriendly);
                me.SetFlag64(UnitFields.NpcFlags, NPCFlags.QuestGiver);
                me.RemoveAllAuras();
                me.DeleteThreatList();
                me.CombatStop(true);
                Talk(Aeranas.SayFree);
                return;
            }

            if (shock_Timer <= diff)
            {
                DoCastVictim(Aeranas.SpellShock);
                shock_Timer = 10000;
            }
            else
                shock_Timer -= diff;

            if (envelopingWinds_Timer <= diff)
            {
                DoCastVictim(Aeranas.SpellEncelopingWinds);
                envelopingWinds_Timer = 25000;
            }
            else
                envelopingWinds_Timer -= diff;

            DoMeleeAttackIfReady();
        }

        uint faction_Timer;
        uint envelopingWinds_Timer;
        uint shock_Timer;
    }

    struct AncestralWolf
    {
        public const uint EmoteWoldLiftHead = 0;
        public const uint EmoteWolfHowl = 1;
        public const uint SayWolfWelcome = 2;

        public const uint SpellAncestralWoldBuff = 29981;

        public const uint NpcRyga = 17123;
    }

    [Script]
    class npc_ancestral_wolf : npc_escortAI
    {
        public npc_ancestral_wolf(Creature creature) : base(creature)
        {
            if (creature.GetOwner() && creature.GetOwner().IsTypeId(TypeId.Player))
                Start(false, false, creature.GetOwner().GetGUID());
            else
                Log.outError(LogFilter.Scripts, "Scripts: npc_ancestral_wolf can not obtain owner or owner is not a player.");

            creature.SetSpeed(UnitMoveType.Walk, 1.5f);
            Reset();
        }

        public override void Reset()
        {
            ryga = null;
            DoCast(me, AncestralWolf.SpellAncestralWoldBuff, true);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!ryga && who.GetEntry() == AncestralWolf.NpcRyga && me.IsWithinDistInMap(who, 15.0f))
            {
                Creature temp = who.ToCreature();
                if (temp)
                    ryga = temp;
            }

            base.MoveInLineOfSight(who);
        }

        public override void WaypointReached(uint waypointId)
        {
            switch (waypointId)
            {
                case 0:
                    Talk(AncestralWolf.EmoteWoldLiftHead);
                    break;
                case 2:
                    Talk(AncestralWolf.EmoteWolfHowl);
                    break;
                case 50:
                    if (ryga && ryga.IsAlive() && !ryga.IsInCombat())
                        ryga.GetAI().Talk(AncestralWolf.SayWolfWelcome);
                    break;
            }
        }

        Creature ryga;
    }

    [Script]
    class npc_wounded_blood_elf : npc_escortAI
    {
        public npc_wounded_blood_elf(Creature creature) : base(creature) { }

        public override void Reset() { }

        public override void EnterCombat(Unit who)
        {
            if (HasEscortState(eEscortState.Escorting))
                Talk(SAY_ELF_AGGRO);
        }

        public override void JustSummoned(Creature summoned)
        {
            summoned.GetAI().AttackStart(me);
        }

        public override void sQuestAccept(Player player, Quest quest)
        {
            if (quest.Id == QUEST_ROAD_TO_FALCON_WATCH)
            {
                me.SetFaction(FACTION_FALCON_WATCH_QUEST);
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
                    Talk(SAY_ELF_START, player);
                    break;
                case 9:
                    Talk(SAY_ELF_SUMMON1, player);
                    // Spawn two Haal'eshi Talonguard
                    DoSpawnCreature(NPC_HAALESHI_TALONGUARD, -15, -15, 0, 0, TempSummonType.TimedDespawnOOC, 5000);
                    DoSpawnCreature(NPC_HAALESHI_TALONGUARD, -17, -17, 0, 0, TempSummonType.TimedDespawnOOC, 5000);
                    break;
                case 13:
                    Talk(SAY_ELF_RESTING, player);
                    break;
                case 14:
                    Talk(SAY_ELF_SUMMON2, player);
                    // Spawn two Haal'eshi Windwalker
                    DoSpawnCreature(NPC_HAALESHI_WINDWALKER, -15, -15, 0, 0, TempSummonType.TimedDespawnOOC, 5000);
                    DoSpawnCreature(NPC_HAALESHI_WINDWALKER, -17, -17, 0, 0, TempSummonType.TimedDespawnOOC, 5000);
                    break;
                case 27:
                    Talk(SAY_ELF_COMPLETE, player);
                    // Award quest credit
                    player.GroupEventHappens(QUEST_ROAD_TO_FALCON_WATCH, me);
                    break;
            }
        }

        const uint SAY_ELF_START = 0;
        const uint SAY_ELF_SUMMON1 = 1;
        const uint SAY_ELF_RESTING = 2;
        const uint SAY_ELF_SUMMON2 = 3;
        const uint SAY_ELF_COMPLETE = 4;
        const uint SAY_ELF_AGGRO = 5;
        const uint QUEST_ROAD_TO_FALCON_WATCH = 9375;
        const uint NPC_HAALESHI_WINDWALKER = 16966;
        const uint NPC_HAALESHI_TALONGUARD = 16967;
        const uint FACTION_FALCON_WATCH_QUEST = 775;
    }


    [Script]
    class npc_fel_guard_hound : ScriptedAI
    {
        public npc_fel_guard_hound(Creature creature) : base(creature) { }

        public override void Reset()
        {
            checkTimer = 5000; //check for creature every 5 sec
            helboarGUID.Clear();
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point || id != 1)
                return;

            Creature helboar = me.GetMap().GetCreature(helboarGUID);
            if (helboar)
            {
                helboar.RemoveCorpse();
                DoCast(SPELL_SUMMON_POO);

                Player owner = me.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (owner)
                    me.GetMotionMaster().MoveFollow(owner, 0.0f, 0.0f);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (checkTimer <= diff)
            {
                Creature helboar = me.FindNearestCreature(NPC_DERANGED_HELBOAR, 10.0f, false);
                if (helboar)
                {
                    if (helboar.GetGUID() != helboarGUID && me.GetMotionMaster().GetCurrentMovementGeneratorType() != MovementGeneratorType.Point && !me.FindCurrentSpellBySpellId(SPELL_SUMMON_POO))
                    {
                        helboarGUID = helboar.GetGUID();
                        me.GetMotionMaster().MovePoint(1, helboar.GetPositionX(), helboar.GetPositionY(), helboar.GetPositionZ());
                    }
                }
                checkTimer = 5000;
            }
            else checkTimer -= diff;

            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        uint checkTimer;
        ObjectGuid helboarGUID;

        const uint SPELL_SUMMON_POO = 37688;
        const uint NPC_DERANGED_HELBOAR = 16863;
    }
}

