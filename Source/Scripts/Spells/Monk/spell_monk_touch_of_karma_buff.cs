using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(125174)]
public class spell_monk_touch_of_karma_buff : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_TOUCH_OF_KARMA);
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		foreach (var aurApp in caster.GetAppliedAuras().LookupByKey(MonkSpells.SPELL_MONK_TOUCH_OF_KARMA))
		{
			var targetAura = aurApp.GetBase();

			if (targetAura != null)
				targetAura.Remove();
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}