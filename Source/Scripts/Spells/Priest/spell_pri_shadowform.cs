// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(232698)]
public class spell_pri_shadowform : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SPELL_PRIEST_SHADOWFORM_VISUAL_WITHOUT_GLYPH, PriestSpells.SPELL_PRIEST_SHADOWFORM_VISUAL_WITH_GLYPH);
	}

	private void HandleEffectApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		GetTarget().CastSpell(GetTarget(), GetTarget().HasAura(PriestSpells.SPELL_PRIEST_GLYPH_OF_SHADOW) ? PriestSpells.SPELL_PRIEST_SHADOWFORM_VISUAL_WITH_GLYPH : PriestSpells.SPELL_PRIEST_SHADOWFORM_VISUAL_WITHOUT_GLYPH, true);
	}

	private void HandleEffectRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		GetTarget().RemoveAurasDueToSpell(GetTarget().HasAura(PriestSpells.SPELL_PRIEST_GLYPH_OF_SHADOW) ? PriestSpells.SPELL_PRIEST_SHADOWFORM_VISUAL_WITH_GLYPH : PriestSpells.SPELL_PRIEST_SHADOWFORM_VISUAL_WITHOUT_GLYPH);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.AddPctModifier, AuraEffectHandleModes.RealOrReapplyMask));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.AddPctModifier, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterRemove));
	}
}