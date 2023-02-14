// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 252216 - Tiger Dash
	internal class spell_dru_tiger_dash : SpellScript, ISpellBeforeCast
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.CatForm);
		}

		public void BeforeCast()
		{
			// Change into cat form
			if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.CatForm)
				GetCaster().CastSpell(GetCaster(), DruidSpellIds.CatForm, true);
		}
	}
}