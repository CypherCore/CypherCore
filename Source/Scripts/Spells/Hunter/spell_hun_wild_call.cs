using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(185789)]
public class spell_hun_wild_call : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == HunterSpells.SPELL_HUNTER_AUTO_SHOT && (eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0)
			return true;

		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
			if (player.GetSpellHistory().HasCooldown(HunterSpells.SPELL_BARBED_SHOT))
				player.GetSpellHistory().ResetCooldown(HunterSpells.SPELL_BARBED_SHOT, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}