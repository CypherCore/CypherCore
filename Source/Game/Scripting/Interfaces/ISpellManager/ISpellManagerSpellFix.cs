// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Spells;

namespace Game.Scripting.Interfaces.ISpellManager
{
    /// <summary>
    ///     Applies spell fixes before LoadSpellInfoImmunities, LoadSpellInfoDiminishing, LoadSpellInfoCustomAttributes and LoadSkillLineAbilityMap all have effected the spell. 
    ///     This will override any of those calculations.
    /// </summary>
    public interface ISpellManagerSpellFix
    {
        int[] SpellIds { get; }

        void ApplySpellFix(SpellInfo spellInfo);
    }
}
