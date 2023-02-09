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

			if (caster.HasAura(ShamanSpells.SPELL_SHAMAN_CARESS_OF_THE_TIDEMOTHER))
			{
				var auraeffx = caster.GetAura(ShamanSpells.SPELL_SHAMAN_CARESS_OF_THE_TIDEMOTHER).GetEffect(0);
				var amount   = auraeffx.GetAmount();
				caster.CastSpell(caster, ShamanSpells.SPELL_SHAMAN_CARESS_OF_THE_TIDEMOTHER_AURA, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, amount));
			}
		}
	}
}