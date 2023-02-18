// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 5394 - Healing Stream Totem
	[SpellScript(5394)]
	public class spell_sha_healing_stream_totem : SpellScript, ISpellAfterCast
	{
		public void AfterCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (caster.HasAura(ShamanSpells.CARESS_OF_THE_TIDEMOTHER))
			{
				var auraeffx = caster.GetAura(ShamanSpells.CARESS_OF_THE_TIDEMOTHER).GetEffect(0);
				var amount   = auraeffx.GetAmount();
				caster.CastSpell(caster, ShamanSpells.CARESS_OF_THE_TIDEMOTHER_AURA, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, amount));
			}
		}
	}
}