using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	// 223657 Safeguard
	[SpellScript(223657)]
	public class spell_warr_safeguard : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.RemoveMovementImpairingAuras(true);
		}
	}
}