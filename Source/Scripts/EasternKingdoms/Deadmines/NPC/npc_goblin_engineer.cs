// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(48439)]
    public class npc_goblin_engineer : ScriptedAI
    {
        public npc_goblin_engineer(Creature creature) : base(creature)
        {
            Instance = creature.GetInstanceScript();
        }

        public InstanceScript Instance;

        public override void Reset()
        {
            if (!me)
            {
                return;
            }

            if (me.FindNearestGameObject(DMGameObjects.GO_HEAVY_DOOR, 20.0f))
            {
                me.AddAura(78087, me);
            }
            else
            {
                me.AddAura(57626, me);
            }
        }

    }

}
