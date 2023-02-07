// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 783 - Travel Form (dummy)
    internal class spell_dru_travel_form_dummy : SpellScript, ISpellCheckCast
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(DruidSpellIds.FormAquaticPassive, DruidSpellIds.FormAquatic, DruidSpellIds.FormStag);
        }

        public SpellCastResult CheckCast()
        {
            Player player = GetCaster().ToPlayer();

            if (!player)
                return SpellCastResult.CustomError;

            uint spellId = (player.HasSpell(DruidSpellIds.FormAquaticPassive) && player.IsInWater()) ? DruidSpellIds.FormAquatic : DruidSpellIds.FormStag;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetCastDifficulty());

            return spellInfo.CheckLocation(player.GetMapId(), player.GetZoneId(), player.GetAreaId(), player);
        }
    }
}