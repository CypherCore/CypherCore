using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 157980 - Supernova
internal class spell_mage_supernova : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDamage(uint effIndex)
	{
		if (GetExplTargetUnit() == GetHitUnit())
		{
			var damage = GetHitDamage();
			MathFunctions.AddPct(ref damage, GetEffectInfo(0).CalcValue());
			SetHitDamage(damage);
		}
	}
}