// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class WorldObjectListSearcher : Notifier
    {
        private readonly ICheck<WorldObject> _check;
        private readonly GridMapTypeMask _mapTypeMask;
        private readonly List<WorldObject> _objects;
        private readonly PhaseShift _phaseShift;

        public WorldObjectListSearcher(WorldObject searcher, List<WorldObject> objects, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
        {
            _mapTypeMask = mapTypeMask;
            _phaseShift = searcher.GetPhaseShift();
            _objects = objects;
            _check = check;
        }

        public override void Visit(IList<Player> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (_check.Invoke(player))
                    _objects.Add(player);
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (_check.Invoke(creature))
                    _objects.Add(creature);
            }
        }

        public override void Visit(IList<Corpse> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Corpse corpse = objs[i];

                if (_check.Invoke(corpse))
                    _objects.Add(corpse);
            }
        }

        public override void Visit(IList<GameObject> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];

                if (_check.Invoke(gameObject))
                    _objects.Add(gameObject);
            }
        }

        public override void Visit(IList<DynamicObject> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];

                if (_check.Invoke(dynamicObject))
                    _objects.Add(dynamicObject);
            }
        }

        public override void Visit(IList<AreaTrigger> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                AreaTrigger areaTrigger = objs[i];

                if (_check.Invoke(areaTrigger))
                    _objects.Add(areaTrigger);
            }
        }

        public override void Visit(IList<SceneObject> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                SceneObject sceneObject = objs[i];

                if (_check.Invoke(sceneObject))
                    _objects.Add(sceneObject);
            }
        }

        public override void Visit(IList<Conversation> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Conversation conversation = objs[i];

                if (_check.Invoke(conversation))
                    _objects.Add(conversation);
            }
        }
    }

}