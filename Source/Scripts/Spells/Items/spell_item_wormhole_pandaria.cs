using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 126755 - Wormhole: Pandaria
internal class spell_item_wormhole_pandaria : SpellScript, IHasSpellEffects
{
	private readonly uint[] WormholeTargetLocations =
	{
		ItemSpellIds.Wormholepandariaisleofreckoning, ItemSpellIds.Wormholepandariakunlaiunderwater, ItemSpellIds.Wormholepandariasravess, ItemSpellIds.Wormholepandariarikkitunvillage, ItemSpellIds.Wormholepandariazanvesstree, ItemSpellIds.Wormholepandariaanglerswharf, ItemSpellIds.Wormholepandariacranestatue, ItemSpellIds.Wormholepandariaemperorsomen, ItemSpellIds.Wormholepandariawhitepetallake
	};

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(WormholeTargetLocations);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleTeleport, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleTeleport(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var spellId = WormholeTargetLocations.SelectRandom();
		GetCaster().CastSpell(GetHitUnit(), spellId, true);
	}
}