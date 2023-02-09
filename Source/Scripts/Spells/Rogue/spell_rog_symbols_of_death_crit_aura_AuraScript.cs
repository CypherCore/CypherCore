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
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private void HandleAfterProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		Remove();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleAfterProc, 0, AuraType.AddFlatModifier, AuraScriptHookType.AfterProc));
	}
}