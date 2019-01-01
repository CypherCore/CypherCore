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

using Game;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World
{
    struct SceneSpells
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
            if (triggerName == "BURN PLAYER")
                player.CastSpell(player, SceneSpells.DeathwingSimulator, true); // Deathwing Simulator Burn player
        }
    }
}
