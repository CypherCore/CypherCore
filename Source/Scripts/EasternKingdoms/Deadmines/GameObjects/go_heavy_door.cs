// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.GameObjects
{
    [GameObjectScript(DMGameObjects.GO_HEAVY_DOOR)]
    public class go_heavy_door : GameObjectAI
    {
        public go_heavy_door(GameObject go) : base(go)
        {
        }

        public void MoveNearCreature(GameObject me, uint entry, uint ragne)
        {
            if (me == null)
            {
                return;
            }

            var creature_list = me.GetCreatureListWithEntryInGrid(entry, ragne);

            creature_list.Sort(new ObjectDistanceOrderPred(me));
            foreach (var creature in creature_list)
            {
                if (creature && creature.IsAlive() && creature.GetTypeId() == TypeId.Unit && creature.HasAura(78087))
                {
                    creature.GetMotionMaster().MoveCharge(me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), 5.0f);
                    creature.DespawnOrUnsummon(TimeSpan.FromMilliseconds(3000));
                    creature.GetAI().Talk(0);
                }
            }
        }

        public override bool OnGossipHello(Player player)
        {
            if (me == null || player == null)
            {
                return false;
            }

            MoveNearCreature(me, 48439, 50);
            MoveNearCreature(me, 48280, 50);

            return true;
        }
    }
}
