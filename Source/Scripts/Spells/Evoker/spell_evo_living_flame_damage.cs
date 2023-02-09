using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(361500)]
public class spell_evo_living_flame_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(EvokerSpells.SPELL_EVOKER_ENERGIZING_FLAME, EvokerSpells.SPELL_EVOKER_LIVING_FLAME);
	}

	private void HandleManaRestored(int UnnamedParameter)
	{
		var auraEffect = GetCaster().GetAuraEffect(EvokerSpells.SPELL_EVOKER_ENERGIZING_FLAME, 0);

		if (auraEffect != null)
		{
			var spellInfo = Global.SpellMgr.AssertSpellInfo(EvokerSpells.SPELL_EVOKER_LIVING_FLAME, GetCastDifficulty());

			var cost = spellInfo.CalcPowerCost(PowerType.Mana, false, GetCaster(), GetSpellInfo().GetSchoolMask(), null);

			if (cost == null)
				return;

			var manaRestored = MathFunctions.CalculatePct(cost.Amount, auraEffect.GetAmount());
			GetCaster().ModifyPower(PowerType.Mana, manaRestored);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleManaRestored, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}