using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// Flametongue Attack - 10444
	[SpellScript(10444)]
	public class bfa_spell_flametongue_attack_damage : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var flamet = caster.GetAura(ShamanSpells.SPELL_FLAMETONGUE_AURA);

			if (flamet != null)
				SetHitDamage((int)(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.2f));
		}
	}
}