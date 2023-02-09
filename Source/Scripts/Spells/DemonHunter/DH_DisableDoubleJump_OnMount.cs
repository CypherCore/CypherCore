using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script]
public class DH_DisableDoubleJump_OnMount : ScriptObjectAutoAdd, IPlayerOnSpellCast
{
	public Class PlayerClass => Class.DemonHunter;

	public DH_DisableDoubleJump_OnMount() : base("DH_DisableDoubleJump_OnMount")
	{
	}

	public void OnSpellCast(Player player, Spell spell, bool skipCheck)
	{
		if (player.GetClass() == Class.DemonHunter && player.HasAura(DemonHunterSpells.SPELL_DH_DOUBLE_JUMP) && spell.GetSpellInfo().GetEffect(0).ApplyAuraName == AuraType.Mounted)
			player.SetCanDoubleJump(false);
	}

	public void OnUpdate(Player player, uint diff)
	{
		if (player.GetClass() == Class.DemonHunter && player.HasAura(DemonHunterSpells.SPELL_DH_DOUBLE_JUMP) && !player.IsMounted() && !player.HasExtraUnitMovementFlag(MovementFlag2.CanDoubleJump))
			player.SetCanDoubleJump(true);
	}
}