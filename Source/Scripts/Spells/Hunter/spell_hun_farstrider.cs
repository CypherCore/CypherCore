using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(199523)]
public class spell_hun_farstrider : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if ((eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0)
		{
			return true;
		}

		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		PreventDefaultAction();

		Player player = GetCaster().ToPlayer();
		if (player != null)
		{
			if (player.HasSpell(HunterSpells.SPELL_HUNTER_DISENGAGE))
			{
				player.GetSpellHistory().ResetCooldown(HunterSpells.SPELL_HUNTER_DISENGAGE, true);
			}

			if (player.HasSpell(HunterSpells.SPELL_HUNTER_HARPOON))
			{
				player.GetSpellHistory().ResetCooldown(HunterSpells.SPELL_HUNTER_DISENGAGE, true);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}