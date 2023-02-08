using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(81262)]
public class spell_dru_efflorescence_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void HandleHeal(AuraEffect UnnamedParameter)
	{
		if (GetCaster() && GetCaster().GetOwner())
		{
			GetCaster().GetOwner().CastSpell(GetCaster().GetPosition(), EfflorescenceSpells.SPELL_DRUID_EFFLORESCENCE_HEAL);

			var playerList = GetCaster().GetPlayerListInGrid(11.2f);
			foreach (var targets in playerList)
			{
				if (GetCaster().GetOwner().HasAura(DruidSpells.SPELL_DRU_SPRING_BLOSSOMS))
				{
					if (!targets.HasAura(DruidSpells.SPELL_DRU_SPRING_BLOSSOMS_HEAL))
					{
						GetCaster().GetOwner().CastSpell(targets, DruidSpells.SPELL_DRU_SPRING_BLOSSOMS_HEAL, true);
					}
				}
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleHeal, 0, AuraType.PeriodicDummy));
	}
}