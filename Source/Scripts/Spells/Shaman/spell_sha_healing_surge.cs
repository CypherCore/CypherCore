using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// 188070 Healing Surge
	[SpellScript(188070)]
	public class spell_sha_healing_surge : SpellScript, IHasSpellEffects, ISpellCalculateCastTime
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public int CalcCastTime(int castTime)
		{
			var requiredMaelstrom = GetEffectInfo(2).BasePoints;

			if (GetCaster().GetPower(PowerType.Maelstrom) >= requiredMaelstrom)
			{
				castTime    = 0;
				_takenPower = requiredMaelstrom;
			}

			return castTime;
		}

		private void HandleEnergize(uint UnnamedParameter)
		{
			SetEffectValue(-_takenPower);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleEnergize, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
		}

		private int _takenPower = 0;
	}
}