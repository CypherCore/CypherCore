// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Maps;

namespace Game.Scripting.Interfaces.IMap
{
    public interface IMapOnDestroy<T> : IScriptObject where T : Map
    {
        void OnDestroy(T map);
    }
}