﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    public interface IPlayerOnChargeRecoveryTimeStart : IScriptObject, IClassRescriction
    {
        void OnChargeRecoveryTimeStart(Player player, uint chargeCategoryId, ref int chargeRecoveryTime);
    }
}