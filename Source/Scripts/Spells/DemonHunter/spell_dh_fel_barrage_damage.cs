using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(211052)]
public class spell_dh_fel_barrage_damage : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var chargesUsed = GetSpellValue().EffectBasePoints[0];
		var dmg         = GetHitDamage();
		SetHitDamage((float)(dmg * chargesUsed) / 5.0f);
	}
}