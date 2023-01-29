// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Maps;

namespace Game.Scripting.Interfaces.IWorldState
{
    public interface IWorldStateOnValueChange : IScriptObject
    {
        void OnValueChange(int worldStateId, int oldValue, int newValue, Map map);
    }
}