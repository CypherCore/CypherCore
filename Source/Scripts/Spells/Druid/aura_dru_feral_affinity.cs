using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(202157)]
public class aura_dru_feral_affinity : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private readonly List<uint> LearnedSpells = new List<uint>() { (uint)DruidSpells.SPELL_DRUID_FELINE_SWIFTNESS, (uint)DruidSpells.SPELL_DRUID_SHRED, (uint)DruidSpells.SPELL_DRUID_RAKE, (uint)DruidSpells.SPELL_DRUID_RIP, (uint)DruidSpells.SPELL_DRUID_FEROCIOUS_BITE, (uint)DruidSpells.SPELL_DRUID_SWIPE_CAT };

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
		AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}