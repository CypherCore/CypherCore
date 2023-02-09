using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(13877)]
public class spell_rogue_blade_flurry_SpellScript : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		if (GetHitUnit() == GetExplTargetUnit())
		{
			SetHitDamage(0);
		}
	}
}