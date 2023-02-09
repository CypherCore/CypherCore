using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 63471 -Spawn Blood Pool
internal class spell_spawn_blood_pool : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCaster));
	}

	private void SetDest(ref SpellDestination dest)
	{
		Unit       caster       = GetCaster();
		Position   summonPos    = caster.GetPosition();
		LiquidData liquidStatus = new();

		if (caster.GetMap().GetLiquidStatus(caster.GetPhaseShift(), caster.GetPositionX(), caster.GetPositionY(), caster.GetPositionZ(), LiquidHeaderTypeFlags.AllLiquids, liquidStatus, caster.GetCollisionHeight()) != ZLiquidStatus.NoWater)
			summonPos.posZ = liquidStatus.level;

		dest.Relocate(summonPos);
	}
}