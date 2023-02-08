using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(118009)]
public class spell_dk_desecrated_ground : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void OnTick(AuraEffect UnnamedParameter)
	{
		if (GetCaster())
		{
			DynamicObject dynObj = GetCaster().GetDynObject(DeathKnightSpells.SPELL_DK_DESECRATED_GROUND);
			if (dynObj != null) 
			{
				if (GetCaster().GetDistance(dynObj) <= 8.0f)
				{
					GetCaster().CastSpell(GetCaster(), DeathKnightSpells.SPELL_DK_DESECRATED_GROUND_IMMUNE, true);
				}
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 1, AuraType.PeriodicDummy));
	}
}