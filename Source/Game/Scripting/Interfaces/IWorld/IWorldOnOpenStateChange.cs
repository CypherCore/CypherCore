// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Scripting.Interfaces.IWorld
{
    public interface IWorldOnOpenStateChange : IScriptObject
    {
        void OnOpenStateChange(bool open);
    }
}