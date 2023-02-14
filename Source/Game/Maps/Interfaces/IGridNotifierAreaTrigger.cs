// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Interfaces
{
    public interface IGridNotifierAreaTrigger : IGridNotifier
    {
        void Visit(IList<AreaTrigger> objs);
    }
}