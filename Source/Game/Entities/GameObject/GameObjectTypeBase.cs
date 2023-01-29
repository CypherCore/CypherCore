// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    // Base class for GameObject Type specific implementations
    internal class GameObjectTypeBase
    {
        public class CustomCommand
        {
            public virtual void Execute(GameObjectTypeBase type)
            {
            }
        }

        public GameObjectTypeBase(GameObject owner)
        {
            Owner = owner;
        }

        protected GameObject Owner { get; set; }

        public virtual void Update(uint diff)
        {
        }

        public virtual void OnStateChanged(GameObjectState oldState, GameObjectState newState)
        {
        }

        public virtual void OnRelocated()
        {
        }
    }

}