using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(54845)]
public class spell_dru_glyph_of_starfire_proc : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		GetTarget().CastSpell(eventInfo.GetProcTarget(), DruidSpells.SPELL_DRUID_GLYPH_OF_STARFIRE, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}