// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World.SceneScripts
{
    struct SpellIds
    {
        public const uint DeathwingSimulator = 201184;
    }

    [Script]
    class scene_deathwing_simulator : SceneScript
    {
        public scene_deathwing_simulator() : base("scene_deathwing_simulator") { }

        // Called when a player receive trigger from scene
        public override void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
        {
            if (triggerName == "Burn Player")
                player.CastSpell(player, SpellIds.DeathwingSimulator, true); // Deathwing Simulator Burn player
        }
    }
}