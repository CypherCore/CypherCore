// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(703)]
public class spell_rog_garrote_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(RogueSpells.SPELL_ROGUE_THUGGEE);
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
			return;

		var caster = GetAura().GetCaster();

		if (caster == null)
			return;

		if (!caster.HasAura(RogueSpells.SPELL_ROGUE_THUGGEE))
			return;

		caster.GetSpellHistory().ResetCooldown(RogueSpells.SPELL_ROGUE_GARROTE_DOT, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}