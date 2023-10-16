// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Draenor.ZoneAssaultOnTheDarkPortal
{
    struct SpellIds
    {
        public const uint DarkPortalRunAway = 158985;
    }

    [Script] // 621 - Dark Portal: Run away
    class scene_dark_portal_run_away : SceneScript
    {
        public scene_dark_portal_run_away() : base("scene_dark_portal_run_away") { }

        public override void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            player.RemoveAurasDueToSpell(SpellIds.DarkPortalRunAway);
            PhasingHandler.OnConditionChange(player);
        }
    }

    [Script] // 34420 - The Cost of War
    class quest_the_cost_of_war : QuestScript
    {
        public quest_the_cost_of_war() : base("quest_the_cost_of_war") { }

        public override void OnQuestStatusChange(Player player, Quest quest, QuestStatus oldStatus, QuestStatus newStatus)
        {
            if (newStatus == QuestStatus.None)
            {
                player.RemoveAurasDueToSpell(SpellIds.DarkPortalRunAway);
                PhasingHandler.OnConditionChange(player);
            }
            else if (newStatus == QuestStatus.Incomplete)
            {
                player.CastSpell(player, SpellIds.DarkPortalRunAway, TriggerCastFlags.FullMask);
                PhasingHandler.OnConditionChange(player);
            }
        }
    }
}
