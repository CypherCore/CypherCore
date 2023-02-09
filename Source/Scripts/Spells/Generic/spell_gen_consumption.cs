using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 64208 - Consumption
internal class spell_gen_consumption : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamageCalc, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
	}

	private void HandleDamageCalc(uint effIndex)
	{
		var caster = GetCaster().ToCreature();

		if (caster == null)
			return;

		var damage         = 0;
		var createdBySpell = Global.SpellMgr.GetSpellInfo(caster.m_unitData.CreatedBySpell, GetCastDifficulty());

		if (createdBySpell != null)
			damage = createdBySpell.GetEffect(2).CalcValue();

		if (damage != 0)
			SetEffectValue(damage);
	}
}