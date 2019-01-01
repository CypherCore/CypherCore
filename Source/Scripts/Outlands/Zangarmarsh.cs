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
using System.Collections.Generic;

namespace Scripts.Outlands
{
    [Script]
    class npcs_ashyen_and_keleth : CreatureScript
    {
        public npcs_ashyen_and_keleth() : base("npcs_ashyen_and_keleth") { }

        public override bool OnGossipHello(Player player, Creature creature)
        {
            if (player.GetReputationRank(942) > ReputationRank.Neutral)
            {
                if (creature.GetEntry() == NPC_ASHYEN)
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GOSSIP_ITEM_BLESS_ASH, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

                if (creature.GetEntry() == NPC_KELETH)
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GOSSIP_ITEM_BLESS_KEL, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
            }
            player.SEND_GOSSIP_MENU(player.GetGossipTextId(creature), creature.GetGUID());

            return true;
        }

        public override bool OnGossipSelect(Player player, Creature creature, uint sender, uint action)
        {
            player.PlayerTalkClass.ClearMenus();
            if (action == eTradeskill.GossipActionInfoDef + 1)
            {
                creature.SetPowerType(PowerType.Mana);
                creature.SetMaxPower(PowerType.Mana, 200);             //set a "fake" mana value, we can't depend on database doing it in this case
                creature.SetPower(PowerType.Mana, 200);

                if (creature.GetEntry() == NPC_ASHYEN)                //check which Creature we are dealing with
                {
                    uint spell = 0;
                    switch (player.GetReputationRank(942))
                    {                                               //mark of lore
                        case ReputationRank.Friendly:
                            spell = SPELL_BLESS_ASH_FRI;
                            break;
                        case ReputationRank.Honored:
                            spell = SPELL_BLESS_ASH_HON;
                            break;
                        case ReputationRank.Revered:
                            spell = SPELL_BLESS_ASH_REV;
                            break;
                        case ReputationRank.Exalted:
                            spell = SPELL_BLESS_ASH_EXA;
                            break;
                        default:
                            break;
                    }

                    if (spell != 0)
                    {
                        creature.CastSpell(player, spell, true);
                        creature.GetAI().Talk(GOSSIP_REWARD_BLESS);
                    }
                }

                if (creature.GetEntry() == NPC_KELETH)
                {
                    uint spell = 0;
                    switch (player.GetReputationRank(942))         //mark of war
                    {
                        case ReputationRank.Friendly:
                            spell = SPELL_BLESS_KEL_FRI;
                            break;
                        case ReputationRank.Honored:
                            spell = SPELL_BLESS_KEL_HON;
                            break;
                        case ReputationRank.Revered:
                            spell = SPELL_BLESS_KEL_REV;
                            break;
                        case ReputationRank.Exalted:
                            spell = SPELL_BLESS_KEL_EXA;
                            break;
                        default:
                            break;
                    }

                    if (spell != 0)
                    {
                        creature.CastSpell(player, spell, true);
                        creature.GetAI().Talk(GOSSIP_REWARD_BLESS);
                    }
                }
                player.CLOSE_GOSSIP_MENU();
                player.TalkedToCreature(creature.GetEntry(), creature.GetGUID());
            }
            return true;
        }

        const uint GOSSIP_REWARD_BLESS = 0;

        const uint NPC_ASHYEN = 17900;
        const uint NPC_KELETH = 17901;

        const uint SPELL_BLESS_ASH_EXA = 31815;
        const uint SPELL_BLESS_ASH_REV = 31811;
        const uint SPELL_BLESS_ASH_HON = 31810;
        const uint SPELL_BLESS_ASH_FRI = 31808;

        const uint SPELL_BLESS_KEL_EXA = 31814;
        const uint SPELL_BLESS_KEL_REV = 31813;
        const uint SPELL_BLESS_KEL_HON = 31812;
        const uint SPELL_BLESS_KEL_FRI = 31807;

        const string GOSSIP_ITEM_BLESS_ASH = "Grant me your mark, wise ancient.";
        const string GOSSIP_ITEM_BLESS_KEL = "Grant me your mark, mighty ancient.";
    }

    [Script]
    class npc_cooshcoosh : CreatureScript
    {
        public npc_cooshcoosh() : base("npc_cooshcoosh") { }

        class npc_cooshcooshAI : ScriptedAI
        {
            public npc_cooshcooshAI(Creature creature) : base(creature)
            {
                m_uiNormFaction = creature.getFaction();
            }

            uint m_uiNormFaction;

            public override void Reset()
            {
                _events.ScheduleEvent(Event_LightningBolt, 2000);
                if (me.getFaction() != m_uiNormFaction)
                    me.SetFaction(m_uiNormFaction);
            }

            public override void EnterCombat(Unit who) { }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                _events.Update(diff);

                _events.ExecuteEvents(id =>
                {
                    DoCastVictim(SPELL_LIGHTNING_BOLT);
                    _events.ScheduleEvent(Event_LightningBolt, 5000);
                });

                DoMeleeAttackIfReady();
            }
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new npc_cooshcooshAI(creature);
        }

        public override bool OnGossipHello(Player player, Creature creature)
        {
            if (player.GetQuestStatus(QUEST_CRACK_SKULLS) == QuestStatus.Incomplete)
                player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GOSSIP_COOSH, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);

            player.SEND_GOSSIP_MENU(9441, creature.GetGUID());
            return true;
        }

        public override bool OnGossipSelect(Player player, Creature creature, uint sender, uint action)
        {
            player.PlayerTalkClass.ClearMenus();
            if (action == eTradeskill.GossipActionInfoDef)
            {
                player.CLOSE_GOSSIP_MENU();
                creature.SetFaction(FACTION_HOSTILE_CO);
                creature.GetAI().AttackStart(player);
            }
            return true;
        }

        const uint SPELL_LIGHTNING_BOLT = 9532;
        const uint QUEST_CRACK_SKULLS = 10009;
        const uint FACTION_HOSTILE_CO = 45;
        const int Event_LightningBolt = 1;

        const string GOSSIP_COOSH = "You owe Sim'salabim money. Hand them over or die!";
    }

    [Script]
    class npc_elder_kuruti : CreatureScript
    {
        public npc_elder_kuruti() : base("npc_elder_kuruti") { }

        public override bool OnGossipHello(Player player, Creature creature)
        {
            if (player.GetQuestStatus(9803) == QuestStatus.Incomplete)
                player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GOSSIP_ITEM_KUR1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);

            player.SEND_GOSSIP_MENU(9226, creature.GetGUID());

            return true;
        }

        public override bool OnGossipSelect(Player player, Creature creature, uint sender, uint action)
        {
            player.PlayerTalkClass.ClearMenus();
            switch (action)
            {
                case eTradeskill.GossipActionInfoDef:
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GOSSIP_ITEM_KUR2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                    player.SEND_GOSSIP_MENU(9227, creature.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 1:
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GOSSIP_ITEM_KUR3, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.SEND_GOSSIP_MENU(9229, creature.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    {
                        if (!player.HasItemCount(24573))
                        {
                            List<ItemPosCount> dest = new List<ItemPosCount>();
                            uint itemId = 24573;
                            uint temp;
                            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, 1, out temp);
                            if (msg == InventoryResult.Ok)
                            {
                                player.StoreNewItem(dest, itemId, true);
                            }
                            else
                                player.SendEquipError(msg, null, null, itemId);
                        }
                        player.SEND_GOSSIP_MENU(9231, creature.GetGUID());
                        break;
                    }
            }
            return true;
        }

        const string GOSSIP_ITEM_KUR1 = "Greetings, elder. It is time for your people to end their hostility towards the draenei and their allies.";
        const string GOSSIP_ITEM_KUR2 = "I did not mean to deceive you, elder. The draenei of Telredor thought to approach you in a way that would seem familiar to you.";
        const string GOSSIP_ITEM_KUR3 = "I will tell them. Farewell, elder.";
    }

    [Script]
    class npc_kayra_longmane : CreatureScript
    {
        public npc_kayra_longmane() : base("npc_kayra_longmane") { }

        class npc_kayra_longmaneAI : npc_escortAI
        {
            public npc_kayra_longmaneAI(Creature creature) : base(creature) { }

            public override void Reset() { }

            public override void WaypointReached(uint waypointId)
            {
                Player player = GetPlayerForEscort();
                if (!player)
                    return;

                switch (waypointId)
                {
                    case 4:
                        Talk(SAY_AMBUSH1, player);
                        DoSpawnCreature(NPC_SLAVEBINDER, -10.0f, -5.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOOC, 30000);
                        DoSpawnCreature(NPC_SLAVEBINDER, -8.0f, 5.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOOC, 30000);
                        break;
                    case 5:
                        Talk(SAY_PROGRESS, player);
                        SetRun();
                        break;
                    case 16:
                        Talk(SAY_AMBUSH2, player);
                        DoSpawnCreature(NPC_SLAVEBINDER, -10.0f, -5.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOOC, 30000);
                        DoSpawnCreature(NPC_SLAVEBINDER, -8.0f, 5.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOOC, 30000);
                        break;
                    case 17:
                        SetRun(false);
                        break;
                    case 25:
                        Talk(SAY_END, player);
                        player.GroupEventHappens(QUEST_ESCAPE_FROM, me);
                        break;
                }
            }
        }

        public override bool OnQuestAccept(Player player, Creature creature, Quest quest)
        {
            if (quest.Id == QUEST_ESCAPE_FROM)
            {
                creature.GetAI().Talk(SAY_START, player);

                npc_escortAI pEscortAI = (npc_kayra_longmaneAI)creature.GetAI();
                if (pEscortAI != null)
                    pEscortAI.Start(false, false, player.GetGUID());
            }
            return true;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new npc_kayra_longmaneAI(creature);
        }

        const uint SAY_START = 0;
        const uint SAY_AMBUSH1 = 1;
        const uint SAY_PROGRESS = 2;
        const uint SAY_AMBUSH2 = 3;
        const uint SAY_END = 4;

        const uint QUEST_ESCAPE_FROM = 9752;
        const uint NPC_SLAVEBINDER = 18042;
    }
}
