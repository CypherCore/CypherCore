using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_running_wild_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(SharedConst.DisplayIdHiddenMount))
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleMount, 1, AuraType.Mounted, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
	}

	private void HandleMount(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit target = GetTarget();
		PreventDefaultAction();

		target.Mount(SharedConst.DisplayIdHiddenMount, 0, 0);

		// cast speed aura
		MountCapabilityRecord mountCapability = CliDB.MountCapabilityStorage.LookupByKey(aurEff.GetAmount());

		if (mountCapability != null)
			target.CastSpell(target, mountCapability.ModSpellAuraID, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}