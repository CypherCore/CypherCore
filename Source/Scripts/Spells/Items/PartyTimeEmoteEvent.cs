// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;

namespace Scripts.Spells.Items;

internal class PartyTimeEmoteEvent : BasicEvent
{
	private readonly Player _player;

	public PartyTimeEmoteEvent(Player player)
	{
		_player = player;
	}

	public override bool Execute(ulong time, uint diff)
	{
		if (!_player.HasAura(ItemSpellIds.PartyTime))
			return true;

		if (_player.IsMoving())
			_player.HandleEmoteCommand(RandomHelper.RAND(Emote.OneshotApplaud, Emote.OneshotLaugh, Emote.OneshotCheer, Emote.OneshotChicken));
		else
			_player.HandleEmoteCommand(RandomHelper.RAND(Emote.OneshotApplaud, Emote.OneshotDancespecial, Emote.OneshotLaugh, Emote.OneshotCheer, Emote.OneshotChicken));

		_player.m_Events.AddEventAtOffset(this, TimeSpan.FromSeconds(RandomHelper.RAND(5, 10, 15)));

		return false; // do not delete re-added event in EventProcessor::Update
	}
}