using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Warrior
{
	// 202603 Into the Fray
	// MiscId - 5839
	[Script]
	public class at_into_the_fray : AreaTriggerAI
	{
		public at_into_the_fray(AreaTrigger areatrigger) : base(areatrigger)
		{
		}

		public override void OnUpdate(uint diff)
		{
			var caster = at.GetCaster();

			if (caster == null)
				return;

			var timer = at.VariableStorage.GetValue<uint>("_timer", 0) + diff;

			if (timer >= 250)
			{
				at.VariableStorage.Set<int>("_timer", 0);
				var count = (uint)(at.GetInsideUnits().Count - 1);

				if (count != 0)
				{
					if (!caster.HasAura(WarriorSpells.INTO_THE_FRAY))
						caster.CastSpell(caster, WarriorSpells.INTO_THE_FRAY, true);

					var itf = caster.GetAura(WarriorSpells.INTO_THE_FRAY);

					if (itf != null)
						itf.SetStackAmount((byte)Math.Min(itf.CalcMaxStackAmount(), count));
				}
				else
				{
					caster.RemoveAurasDueToSpell(WarriorSpells.INTO_THE_FRAY);
				}
			}
			else
			{
				at.VariableStorage.Set("_timer", timer);
			}
		}
	}
}