using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(48020)] // 48020 - Demonic Circle: Teleport
	internal class spell_warl_demonic_circle_teleport : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleTeleport, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		}

		private void HandleTeleport(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			var player = GetTarget().ToPlayer();

			if (player)
			{
				var circle = player.GetGameObject(WarlockSpells.DEMONIC_CIRCLE_SUMMON);

				if (circle)
				{
					player.NearTeleportTo(circle.GetPositionX(), circle.GetPositionY(), circle.GetPositionZ(), circle.GetOrientation());
					player.RemoveMovementImpairingAuras(false);
				}
			}
		}
	}
}