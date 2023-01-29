// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps
{
    public class Notifier
    {
        public virtual void Visit(IList<WorldObject> objs)
        {
        }

        public virtual void Visit(IList<Creature> objs)
        {
        }

        public virtual void Visit(IList<AreaTrigger> objs)
        {
        }

        public virtual void Visit(IList<SceneObject> objs)
        {
        }

        public virtual void Visit(IList<Conversation> objs)
        {
        }

        public virtual void Visit(IList<GameObject> objs)
        {
        }

        public virtual void Visit(IList<DynamicObject> objs)
        {
        }

        public virtual void Visit(IList<Corpse> objs)
        {
        }

        public virtual void Visit(IList<Player> objs)
        {
        }

        public void CreatureUnitRelocationWorker(Creature c, Unit u)
        {
            if (!u.IsAlive() ||
                !c.IsAlive() ||
                c == u ||
                u.IsInFlight())
                return;

            if (!c.HasUnitState(UnitState.Sightless))
            {
                if (c.IsAIEnabled() &&
                    c.CanSeeOrDetect(u, false, true))
                {
                    c.GetAI().MoveInLineOfSight_Safe(u);
                }
                else
                {
                    if (u.IsTypeId(TypeId.Player) &&
                        u.HasStealthAura() &&
                        c.IsAIEnabled() &&
                        c.CanSeeOrDetect(u, false, true, true))
                        c.GetAI().TriggerAlert(u);
                }
            }
        }
    }
}