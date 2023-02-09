using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 170377 - Earthen Rage (Proc Aura)
internal class spell_sha_earthen_rage_proc_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.EarthenRagePassive, ShamanSpells.EarthenRageDamage);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		PreventDefaultAction();
		var aura = GetCaster().GetAura(ShamanSpells.EarthenRagePassive);

		if (aura != null)
		{
			var earthen_rage_script = aura.GetScript<spell_sha_earthen_rage_passive>();

			if (earthen_rage_script != null)
			{
				var procTarget = Global.ObjAccessor.GetUnit(GetCaster(), earthen_rage_script.GetProcTargetGuid());

				if (procTarget)
					GetTarget().CastSpell(procTarget, ShamanSpells.EarthenRageDamage, true);
			}
		}
	}
}