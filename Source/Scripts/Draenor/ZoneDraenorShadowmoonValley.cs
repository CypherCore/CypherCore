// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Draenor.ZoneDraenorShadowmoonValley
{
    // 79243 - Baros Alexston
    struct MiscConst
    {
        // Quest
        public const uint QuestEstablishYourGarrison = 34586;

        // Gossip
        public const uint GossipOptionEstablishGarrison = 0;

        // Text
        public const uint SayStartConstruction = 0;

        // Spells
        public const uint SpellQuest34586Killcredit = 161033;
        public const uint SpellCreateGarrisonShadowmoonValleyAlliance = 156020;
        public const uint SpellDespawnAllSummonsGarrisonIntroOnly = 160938;
    }


    [Script]
    class npc_baros_alexston : ScriptedAI
    {
        Position GarrisonLevelOneCreationPlayerPosition = new(1904.58f, 312.906f, 88.9542f, 4.303615f);

        public npc_baros_alexston(Creature creature) : base(creature) { }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            if (gossipListId == MiscConst.GossipOptionEstablishGarrison)
            {
                player.CloseGossipMenu();
                player.CastSpell(player, MiscConst.SpellQuest34586Killcredit, true);
                player.CastSpell(player, MiscConst.SpellCreateGarrisonShadowmoonValleyAlliance, true);
                player.CastSpell(player, MiscConst.SpellDespawnAllSummonsGarrisonIntroOnly, true);
                player.NearTeleportTo(GarrisonLevelOneCreationPlayerPosition);

                PhasingHandler.OnConditionChange(player);
            }

            return true;
        }

        public override void OnQuestAccept(Player player, Quest quest)
        {
            if (quest.Id == MiscConst.QuestEstablishYourGarrison)
                Talk(MiscConst.SayStartConstruction, player);
        }
    }
}

