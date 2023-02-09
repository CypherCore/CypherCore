using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid
{
	[Script] // 70691 - Item T10 Restoration 4P Bonus
	internal class spell_dru_t10_restoration_4p_bonus : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Load()
		{
			return GetCaster().IsTypeId(TypeId.Player);
		}

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
		}

		private void FilterTargets(List<WorldObject> targets)
		{
			if (!GetCaster().ToPlayer().GetGroup())
			{
				targets.Clear();
				targets.Add(GetCaster());
			}
			else
			{
				targets.Remove(GetExplTargetUnit());
				List<Unit> tempTargets = new();

				foreach (var obj in targets)
					if (obj.IsTypeId(TypeId.Player) &&
					    GetCaster().IsInRaidWith(obj.ToUnit()))
						tempTargets.Add(obj.ToUnit());

				if (tempTargets.Empty())
				{
					targets.Clear();
					FinishCast(SpellCastResult.DontReport);

					return;
				}

				var target = tempTargets.SelectRandom();
				targets.Clear();
				targets.Add(target);
			}
		}
	}
}