using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 99947 - Face Rage
internal class spell_gen_face_rage : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.FaceRage) && spellInfo.GetEffects().Count > 2;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
	{
		GetTarget().RemoveAurasDueToSpell(GetEffectInfo(2).TriggerSpell);
	}
}