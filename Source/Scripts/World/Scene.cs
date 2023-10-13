// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World.Achievements
{
    [Script]
    class scene_deathwing_simulator : SceneScript
    {
        const uint SpellDeathwingSimulator = 201184;

        public scene_deathwing_simulator() : base("scene_deathwing_simulator") { }

        // Called when a player receive trigger from scene
        public override void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
        {
            if (triggerName == "Burn Player")
                player.CastSpell(player, SpellDeathwingSimulator, true); // Deathwing Simulator Burn player
        }
    }
}

