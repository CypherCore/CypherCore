using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(new uint[] { 24858, 102560, 197625 })]
public class aura_dru_astral_form : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(DruidSpells.SPELL_DRUID_GLYPH_OF_STARS);
	}

	private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit target = GetTarget();
		if (target.HasAura(DruidSpells.SPELL_DRUID_GLYPH_OF_STARS))
		{
			target.SetDisplayId(target.GetNativeDisplayId());
			target.AddAura(DruidSpells.SPELL_DRUID_BLUE_COLOR, target);
			target.AddAura(DruidSpells.SPELL_DRUID_SHADOWY_GHOST, target);
			target.CastSpell(target, (uint)Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_GLYPH_OF_STARS, Difficulty.None).GetEffect(0).BasePoints, true);
		}
	}

	private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit target = GetTarget();
		if (target.HasAura(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM) || target.HasAura(DruidSpells.SPELL_DRUID_CHOSEN_OF_ELUNE))
		{
			return;
		}

		target.RemoveAura((uint)Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_GLYPH_OF_STARS, Difficulty.None).GetEffect(0).BasePoints);
		target.RemoveAura(DruidSpells.SPELL_DRUID_BLUE_COLOR);
		target.RemoveAura(DruidSpells.SPELL_DRUID_SHADOWY_GHOST);
	}

	public override void Register()
	{
		switch (ScriptSpellId)
		{
			case 197625:
			case 24858:
				AuraEffects.Add(new EffectApplyHandler(AfterApply, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
				AuraEffects.Add(new EffectApplyHandler(AfterRemove, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
				break;
			case 102560:
				AuraEffects.Add(new EffectApplyHandler(AfterApply, 1, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
				AuraEffects.Add(new EffectApplyHandler(AfterRemove, 1, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
				break;
		}
	}
}