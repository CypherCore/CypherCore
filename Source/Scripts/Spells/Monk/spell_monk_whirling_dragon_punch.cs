using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(152175)]
public class spell_monk_whirling_dragon_punch : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void OnTick(AuraEffect UnnamedParameter)
	{
		if (GetCaster())
		{
			GetCaster().CastSpell(GetCaster(), MonkSpells.SPELL_MONK_WHIRLING_DRAGON_PUNCH_DAMAGE, true);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
	}
}