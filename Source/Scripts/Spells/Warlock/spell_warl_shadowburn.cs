using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(17877)]
	public class spell_warl_shadowburn : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			if (GetCaster())
			{
				var removeMode = GetTargetApplication().GetRemoveMode();

				if (removeMode == AuraRemoveMode.Death)
					GetCaster().SetPower(PowerType.SoulShards, GetCaster().GetPower(PowerType.SoulShards) + 50);
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
		}
	}
}