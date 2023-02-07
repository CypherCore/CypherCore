using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(197492)]
public class aura_dru_restoration_affinity : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private readonly List<uint> LearnedSpells = new List<uint>() { (uint)DruidSpells.SPELL_DRUID_YSERA_GIFT, (uint)DruidSpells.SPELL_DRUID_REJUVENATION, (uint)DruidSpells.SPELL_DRUID_HEALING_TOUCH, (uint)DruidSpells.SPELL_DRUID_SWIFTMEND };

	private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Player target = GetTarget().ToPlayer();
		if (target != null)
		{
			foreach (uint spellId in LearnedSpells)
			{
				target.LearnSpell(spellId, false);
			}
		}
	}

	private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Player target = GetTarget().ToPlayer();
		if (target != null)
		{
			foreach (uint spellId in LearnedSpells)
			{
				target.RemoveSpell(spellId);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}