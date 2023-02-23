// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// Spirit link
	[SpellScript(98021)]
	public class spell_sha_spirit_link : SpellScript, ISpellOnHit
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Load()
		{
			averagePercentage = 0.0f;

			return true;
		}

		private void FilterTargets(List<WorldObject> unitList)
		{
			uint targetCount = 0;

			for (var itr = unitList.GetEnumerator(); itr.MoveNext();)
			{
				var target = itr.Current.ToUnit();

				if (target != null)
				{
					targets[target.GetGUID()] =  target.GetHealthPct();
					averagePercentage         += target.GetHealthPct();
					++targetCount;
				}
			}

			averagePercentage /= targetCount;
		}

		public void OnHit()
		{
			var target = GetHitUnit();

			if (target != null)
			{
				if (!targets.ContainsKey(target.GetGUID()))
					return;

				var bp0        = 0.0f;
				var bp1        = 0.0f;
				var percentage = targets[target.GetGUID()];
				var currentHp  = target.CountPctFromMaxHealth((int)percentage);
				var desiredHp  = target.CountPctFromMaxHealth((int)averagePercentage);

				if (desiredHp > currentHp)
					bp1 = desiredHp - currentHp;
				else
					bp0 = currentHp - desiredHp;

				var args = new CastSpellExtraArgs();

				GetCaster()
					.CastSpell(target,
					           98021,
					           new CastSpellExtraArgs(TriggerCastFlags.None)
						           .AddSpellMod(SpellValueMod.BasePoint0, (int)bp0)
						           .AddSpellMod(SpellValueMod.BasePoint1, (int)bp1));
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitCasterAreaRaid));
		}

		private readonly SortedDictionary<ObjectGuid, double> targets = new();
		private double averagePercentage;
	}
}