using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(116888)]
public class spell_dk_purgatory : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Player _player = GetTarget().ToPlayer();
		if (_player != null)
		{
			AuraRemoveMode removeMode = GetTargetApplication().GetRemoveMode();
			if (removeMode == AuraRemoveMode.Expire)
			{
				_player.CastSpell(_player, DeathKnightSpells.SPELL_DK_PURGATORY_INSTAKILL, true);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.SchoolHealAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}