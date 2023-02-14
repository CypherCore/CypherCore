// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IScene;

namespace Scripts.World.SceneScripts
{
    internal struct SpellIds
    {
        public const uint DeathwingSimulator = 201184;
    }

    [Script]
    internal class scene_deathwing_simulator : ScriptObjectAutoAddDBBound, ISceneOnSceneTrigger
    {
        public scene_deathwing_simulator() : base("scene_deathwing_simulator")
        {
        }

        // Called when a player receive trigger from scene
        public void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
        {
            if (triggerName == "Burn Player")
                player.CastSpell(player, SpellIds.DeathwingSimulator, true); // Deathwing Simulator Burn player
        }
    }
}