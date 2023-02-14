// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(227151)]
public class spell_rog_symbols_of_death_crit_aura_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void HandleAfterProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		Remove();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleAfterProc, 0, AuraType.AddFlatModifier, AuraScriptHookType.EffectAfterProc));
	}
}