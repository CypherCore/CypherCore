// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Scripts.Spells.Quest;

internal struct Misc
{
	//Quests6124 6129
	public static TimeSpan DespawnTime = TimeSpan.FromSeconds(30);

	//HodirsHelm
	public const byte Say1 = 1;
	public const byte Say2 = 2;

	//Acleansingsong
	public const uint AreaIdBittertidelake = 4385;
	public const uint AreaIdRiversheart = 4290;
	public const uint AreaIdWintergraspriver = 4388;

	//Quest12372
	public const uint WhisperOnHitByForceWhisper = 1;

	//BurstAtTheSeams
	public const uint AreaTheBrokenFront = 4507;
	public const uint AreaMordRetharTheDeathGate = 4508;
	public const uint QuestFuelForTheFire = 12690;
}