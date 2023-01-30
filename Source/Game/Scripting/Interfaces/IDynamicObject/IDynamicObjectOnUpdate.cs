// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Scripting.Interfaces.IDynamicObject
{
    public interface IDynamicObjectOnUpdate : IScriptObject
    {
        void OnUpdate(DynamicObject obj, uint diff);
    }
}