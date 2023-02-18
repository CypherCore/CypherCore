// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(107906)]
public class spell_pri_glyph_of_shadow : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(PriestSpells.SHADOWFORM_VISUAL_WITH_GLYPH, Difficulty.None) != null)
			return false;

		return true;
	}

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.RemoveAura(PriestSpells.SHADOWFORM_VISUAL_WITHOUT_GLYPH);
		caster.CastSpell(caster, PriestSpells.SHADOWFORM_VISUAL_WITH_GLYPH, true);
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.RemoveAura(PriestSpells.SHADOWFORM_VISUAL_WITH_GLYPH);
		caster.CastSpell(caster, PriestSpells.SHADOWFORM_VISUAL_WITHOUT_GLYPH, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}