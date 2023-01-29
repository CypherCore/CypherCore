// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{

    namespace GameObjectType
    {
        internal class SetTransportAutoCycleBetweenStopFrames : GameObjectTypeBase.CustomCommand
        {
            private readonly bool _on;

            public SetTransportAutoCycleBetweenStopFrames(bool on)
            {
                _on = on;
            }

            public override void Execute(GameObjectTypeBase type)
            {
                Transport transport = (Transport)type;

                transport?.SetAutoCycleBetweenStopFrames(_on);
            }
        }
    }
}