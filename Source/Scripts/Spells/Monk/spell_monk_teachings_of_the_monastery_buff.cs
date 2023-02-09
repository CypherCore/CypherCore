using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(202090)]
public class spell_monk_teachings_of_the_monastery_buff : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_TEACHINGS_OF_THE_MONASTERY_PASSIVE, MonkSpells.SPELL_MONK_BLACKOUT_KICK_TRIGGERED, MonkSpells.SPELL_MONK_BLACKOUT_KICK);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (!GetTarget().HasAura(MonkSpells.SPELL_MONK_TEACHINGS_OF_THE_MONASTERY_PASSIVE))
		{
			return false;
		}

		if (eventInfo.GetSpellInfo().Id != MonkSpells.SPELL_MONK_BLACKOUT_KICK)
		{
			return false;
		}

		return true;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		Aura monasteryBuff = GetAura();
		if (monasteryBuff != null)
		{
			for (byte i = 0; i < monasteryBuff.GetStackAmount(); ++i)
			{
				GetTarget().CastSpell(eventInfo.GetProcTarget(), MonkSpells.SPELL_MONK_BLACKOUT_KICK_TRIGGERED);
			}
			monasteryBuff.Remove();
		}
	}

	public override void Register()
	{

		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}