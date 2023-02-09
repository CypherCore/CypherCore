using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// Flametongue - 193796
	[SpellScript(193796)]
	public class bfa_spell_flametongue : SpellScript, ISpellOnHit
	{
		public override bool Load()
		{
			return GetCaster().IsPlayer();
		}

		public void OnHit()
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			if (caster.HasAura(ShamanSpells.SPELL_SEARING_ASSAULT_TALENT))
				caster.CastSpell(target, ShamanSpells.SPELL_SEARING_ASSULAT_TALENT_PROC, true);
		}
	}
}