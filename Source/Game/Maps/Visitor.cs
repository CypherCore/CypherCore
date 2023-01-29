// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps
{
    public class Visitor
    {
        internal GridMapTypeMask _mask;

        private readonly Notifier _notifier;

        public Visitor(Notifier notifier, GridMapTypeMask mask)
        {
            _notifier = notifier;
            _mask = mask;
        }

        public void Visit(IList<WorldObject> collection)
        {
            _notifier.Visit(collection);
        }

        public void Visit(IList<Creature> creatures)
        {
            _notifier.Visit(creatures);
        }

        public void Visit(IList<AreaTrigger> areatriggers)
        {
            _notifier.Visit(areatriggers);
        }

        public void Visit(IList<SceneObject> sceneObjects)
        {
            _notifier.Visit(sceneObjects);
        }

        public void Visit(IList<Conversation> conversations)
        {
            _notifier.Visit(conversations);
        }

        public void Visit(IList<GameObject> gameobjects)
        {
            _notifier.Visit(gameobjects);
        }

        public void Visit(IList<DynamicObject> dynamicobjects)
        {
            _notifier.Visit(dynamicobjects);
        }

        public void Visit(IList<Corpse> corpses)
        {
            _notifier.Visit(corpses);
        }

        public void Visit(IList<Player> players)
        {
            _notifier.Visit(players);
        }
    }
}