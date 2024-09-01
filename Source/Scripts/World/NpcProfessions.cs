// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World.NpcProfessions
{
    struct SpellIds
    {
        public const uint Goblin = 20222;
        public const uint Gnomish = 20219;

        // EngineeringTrinkets
        public const uint LearnToEverlook = 23490;
        public const uint LearnToGadget = 23491;
        public const uint LearnToArea52 = 36956;
        public const uint LearnToToshley = 36957;

        public const uint ToEverlook = 23486;
        public const uint ToGadget = 23489;
        public const uint ToArea52 = 36954;
        public const uint ToToshley = 36955;
    }

    enum CreatureIds
    {
        Zap = 14742,
        Jhordy = 14743,
        Kablam = 21493,
        Smiles = 21494
    }

    enum GossipIds
    {
        Zap = 6092, // Zap Farflinger
        Jhordy = 6094, // Jhordy Lapforge
        Kablam = 8308, // Kablamm Farflinger
        Smiles = 8306, // Smiles O'Byron
    }

    [Script]
    class npc_engineering_tele_trinket : ScriptedAI
    {
        public npc_engineering_tele_trinket(Creature creature) : base(creature) { }

        bool CanLearn(Player player, uint textId, uint altTextId, SkillType skillId, uint skillValue, uint reqSpellId, uint spellId, ref uint npcTextId)
        {
            bool res = false;
            npcTextId = textId;
            if (player.GetBaseSkillValue(skillId) >= skillValue && player.HasSpell(reqSpellId))
            {
                if (!player.HasSpell(spellId))
                    res = true;
                else
                    npcTextId = altTextId;
            }
            return res;
        }

        public override bool OnGossipHello(Player player)
        {
            uint npcTextId = 0;
            GossipIds gossipItem = 0;
            bool canLearn = false;

            if (player.HasSkill(SkillType.Engineering))
            {
                switch ((CreatureIds)me.GetEntry())
                {
                    case CreatureIds.Zap:
                        canLearn = CanLearn(player, 6092, 0, SkillType.ClassicEngineering, 260, SpellIds.Goblin, SpellIds.ToEverlook, ref npcTextId);
                        if (canLearn)
                            gossipItem = GossipIds.Zap;
                        break;
                    case CreatureIds.Jhordy:
                        canLearn = CanLearn(player, 7251, 7252, SkillType.ClassicEngineering, 260, SpellIds.Gnomish, SpellIds.ToGadget, ref npcTextId);
                        if (canLearn)
                            gossipItem = GossipIds.Jhordy;
                        break;
                    case CreatureIds.Kablam:
                        canLearn = CanLearn(player, 10365, 0, SkillType.OutlandEngineering, 50, SpellIds.Goblin, SpellIds.ToArea52, ref npcTextId);
                        if (canLearn)
                            gossipItem = GossipIds.Kablam;
                        break;
                    case CreatureIds.Smiles:
                        canLearn = CanLearn(player, 10363, 0, SkillType.OutlandEngineering, 50, SpellIds.Gnomish, SpellIds.ToToshley, ref npcTextId);
                        if (canLearn)
                            gossipItem = GossipIds.Kablam;
                        break;
                }
            }

            if (canLearn)
                player.AddGossipItem((uint)gossipItem, 2, me.GetEntry(), eTradeskill.GossipActionInfoDef + 1);

            player.SendGossipMenu(npcTextId != 0 ? npcTextId : player.GetGossipTextId(me), me.GetGUID());
            return true;
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint sender = player.PlayerTalkClass.GetGossipOptionSender(gossipListId);
            if (sender != me.GetEntry())
                return true;

            switch ((CreatureIds)sender)
            {
                case CreatureIds.Zap:
                    player.CastSpell(player, SpellIds.LearnToEverlook, false);
                    break;
                case CreatureIds.Jhordy:
                    player.CastSpell(player, SpellIds.LearnToGadget, false);
                    break;
                case CreatureIds.Kablam:
                    player.CastSpell(player, SpellIds.LearnToArea52, false);
                    break;
                case CreatureIds.Smiles:
                    player.CastSpell(player, SpellIds.LearnToToshley, false);
                    break;
            }

            return false;
        }
    }
}