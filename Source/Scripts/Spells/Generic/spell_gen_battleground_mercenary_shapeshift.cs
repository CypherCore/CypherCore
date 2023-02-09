using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 193970 - Mercenary Shapeshift
internal class spell_gen_battleground_mercenary_shapeshift : AuraScript, IHasAuraEffects
{
	//using OtherFactionRacePriorityList = std::array<Races, 3>;

	private static readonly Dictionary<Race, Race[]> RaceInfo = new()
	                                                            {
		                                                            {
			                                                            Race.Human, new[]
			                                                                        {
				                                                                        Race.Undead, Race.BloodElf
			                                                                        }
		                                                            },
		                                                            {
			                                                            Race.Orc, new[]
			                                                                      {
				                                                                      Race.Dwarf
			                                                                      }
		                                                            },
		                                                            {
			                                                            Race.Dwarf, new[]
			                                                                        {
				                                                                        Race.Orc, Race.Undead, Race.Tauren
			                                                                        }
		                                                            },
		                                                            {
			                                                            Race.NightElf, new[]
			                                                                           {
				                                                                           Race.Troll, Race.BloodElf
			                                                                           }
		                                                            },
		                                                            {
			                                                            Race.Undead, new[]
			                                                                         {
				                                                                         Race.Human
			                                                                         }
		                                                            },
		                                                            {
			                                                            Race.Tauren, new[]
			                                                                         {
				                                                                         Race.Draenei, Race.NightElf
			                                                                         }
		                                                            },
		                                                            {
			                                                            Race.Gnome, new[]
			                                                                        {
				                                                                        Race.Goblin, Race.BloodElf
			                                                                        }
		                                                            },
		                                                            {
			                                                            Race.Troll, new[]
			                                                                        {
				                                                                        Race.NightElf, Race.Human, Race.Draenei
			                                                                        }
		                                                            },
		                                                            {
			                                                            Race.Goblin, new[]
			                                                                         {
				                                                                         Race.Gnome, Race.Dwarf
			                                                                         }
		                                                            },
		                                                            {
			                                                            Race.BloodElf, new[]
			                                                                           {
				                                                                           Race.Human, Race.NightElf
			                                                                           }
		                                                            },
		                                                            {
			                                                            Race.Draenei, new[]
			                                                                          {
				                                                                          Race.Tauren, Race.Orc
			                                                                          }
		                                                            },
		                                                            {
			                                                            Race.Worgen, new[]
			                                                                         {
				                                                                         Race.Troll
			                                                                         }
		                                                            },
		                                                            {
			                                                            Race.PandarenNeutral, new[]
			                                                                                  {
				                                                                                  Race.PandarenNeutral
			                                                                                  }
		                                                            },
		                                                            {
			                                                            Race.PandarenAlliance, new[]
			                                                                                   {
				                                                                                   Race.PandarenHorde, Race.PandarenNeutral
			                                                                                   }
		                                                            },
		                                                            {
			                                                            Race.PandarenHorde, new[]
			                                                                                {
				                                                                                Race.PandarenAlliance, Race.PandarenNeutral
			                                                                                }
		                                                            },
		                                                            {
			                                                            Race.Nightborne, new[]
			                                                                             {
				                                                                             Race.NightElf, Race.Human
			                                                                             }
		                                                            },
		                                                            {
			                                                            Race.HighmountainTauren, new[]
			                                                                                     {
				                                                                                     Race.Draenei, Race.NightElf
			                                                                                     }
		                                                            },
		                                                            {
			                                                            Race.VoidElf, new[]
			                                                                          {
				                                                                          Race.Troll, Race.BloodElf
			                                                                          }
		                                                            },
		                                                            {
			                                                            Race.LightforgedDraenei, new[]
			                                                                                     {
				                                                                                     Race.Tauren, Race.Orc
			                                                                                     }
		                                                            },
		                                                            {
			                                                            Race.ZandalariTroll, new[]
			                                                                                 {
				                                                                                 Race.KulTiran, Race.Human
			                                                                                 }
		                                                            },
		                                                            {
			                                                            Race.KulTiran, new[]
			                                                                           {
				                                                                           Race.ZandalariTroll
			                                                                           }
		                                                            },
		                                                            {
			                                                            Race.DarkIronDwarf, new[]
			                                                                                {
				                                                                                Race.MagharOrc, Race.Orc
			                                                                                }
		                                                            },
		                                                            {
			                                                            Race.Vulpera, new[]
			                                                                          {
				                                                                          Race.MechaGnome, Race.DarkIronDwarf /*Guessed, For Shamans*/
			                                                                          }
		                                                            },
		                                                            {
			                                                            Race.MagharOrc, new[]
			                                                                            {
				                                                                            Race.DarkIronDwarf
			                                                                            }
		                                                            },
		                                                            {
			                                                            Race.MechaGnome, new[]
			                                                                             {
				                                                                             Race.Vulpera
			                                                                             }
		                                                            }
	                                                            };

	private static readonly Dictionary<Race, uint[]> RaceDisplayIds = new()
	                                                                  {
		                                                                  {
			                                                                  Race.Human, new uint[]
			                                                                              {
				                                                                              55239, 55238
			                                                                              }
		                                                                  },
		                                                                  {
			                                                                  Race.Orc, new uint[]
			                                                                            {
				                                                                            55257, 55256
			                                                                            }
		                                                                  },
		                                                                  {
			                                                                  Race.Dwarf, new uint[]
			                                                                              {
				                                                                              55241, 55240
			                                                                              }
		                                                                  },
		                                                                  {
			                                                                  Race.NightElf, new uint[]
			                                                                                 {
				                                                                                 55243, 55242
			                                                                                 }
		                                                                  },
		                                                                  {
			                                                                  Race.Undead, new uint[]
			                                                                               {
				                                                                               55259, 55258
			                                                                               }
		                                                                  },
		                                                                  {
			                                                                  Race.Tauren, new uint[]
			                                                                               {
				                                                                               55261, 55260
			                                                                               }
		                                                                  },
		                                                                  {
			                                                                  Race.Gnome, new uint[]
			                                                                              {
				                                                                              55245, 55244
			                                                                              }
		                                                                  },
		                                                                  {
			                                                                  Race.Troll, new uint[]
			                                                                              {
				                                                                              55263, 55262
			                                                                              }
		                                                                  },
		                                                                  {
			                                                                  Race.Goblin, new uint[]
			                                                                               {
				                                                                               55267, 57244
			                                                                               }
		                                                                  },
		                                                                  {
			                                                                  Race.BloodElf, new uint[]
			                                                                                 {
				                                                                                 55265, 55264
			                                                                                 }
		                                                                  },
		                                                                  {
			                                                                  Race.Draenei, new uint[]
			                                                                                {
				                                                                                55247, 55246
			                                                                                }
		                                                                  },
		                                                                  {
			                                                                  Race.Worgen, new uint[]
			                                                                               {
				                                                                               55255, 55254
			                                                                               }
		                                                                  },
		                                                                  {
			                                                                  Race.PandarenNeutral, new uint[]
			                                                                                        {
				                                                                                        55253, 55252
			                                                                                        }
		                                                                  }, // Not Verified, Might Be Swapped With Race.PandarenHorde
		                                                                  {
			                                                                  Race.PandarenAlliance, new uint[]
			                                                                                         {
				                                                                                         55249, 55248
			                                                                                         }
		                                                                  },
		                                                                  {
			                                                                  Race.PandarenHorde, new uint[]
			                                                                                      {
				                                                                                      55251, 55250
			                                                                                      }
		                                                                  },
		                                                                  {
			                                                                  Race.Nightborne, new uint[]
			                                                                                   {
				                                                                                   82375, 82376
			                                                                                   }
		                                                                  },
		                                                                  {
			                                                                  Race.HighmountainTauren, new uint[]
			                                                                                           {
				                                                                                           82377, 82378
			                                                                                           }
		                                                                  },
		                                                                  {
			                                                                  Race.VoidElf, new uint[]
			                                                                                {
				                                                                                82371, 82372
			                                                                                }
		                                                                  },
		                                                                  {
			                                                                  Race.LightforgedDraenei, new uint[]
			                                                                                           {
				                                                                                           82373, 82374
			                                                                                           }
		                                                                  },
		                                                                  {
			                                                                  Race.ZandalariTroll, new uint[]
			                                                                                       {
				                                                                                       88417, 88416
			                                                                                       }
		                                                                  },
		                                                                  {
			                                                                  Race.KulTiran, new uint[]
			                                                                                 {
				                                                                                 88414, 88413
			                                                                                 }
		                                                                  },
		                                                                  {
			                                                                  Race.DarkIronDwarf, new uint[]
			                                                                                      {
				                                                                                      88409, 88408
			                                                                                      }
		                                                                  },
		                                                                  {
			                                                                  Race.Vulpera, new uint[]
			                                                                                {
				                                                                                94999, 95001
			                                                                                }
		                                                                  },
		                                                                  {
			                                                                  Race.MagharOrc, new uint[]
			                                                                                  {
				                                                                                  88420, 88410
			                                                                                  }
		                                                                  },
		                                                                  {
			                                                                  Race.MechaGnome, new uint[]
			                                                                                   {
				                                                                                   94998, 95000
			                                                                                   }
		                                                                  }
	                                                                  };

	private static readonly List<uint> RacialSkills = new();
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		foreach (var (race, otherRaces) in RaceInfo)
		{
			if (!CliDB.ChrRacesStorage.ContainsKey(race))
				return false;

			foreach (var otherRace in otherRaces)
				if (!CliDB.ChrRacesStorage.ContainsKey(otherRace))
					return false;
		}

		foreach (var (race, displayIds) in RaceDisplayIds)
		{
			if (!CliDB.ChrRacesStorage.ContainsKey(race))
				return false;

			foreach (var displayId in displayIds)
				if (CliDB.CreatureDisplayInfoStorage.ContainsKey(displayId))
					return false;
		}

		RacialSkills.Clear();

		foreach (var skillLine in CliDB.SkillLineStorage.Values)
			if (skillLine.GetFlags().HasFlag(SkillLineFlags.RacialForThePurposeOfTemporaryRaceChange))
				RacialSkills.Add(skillLine.Id);

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.SendForClientMask, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private static Race GetReplacementRace(Race nativeRace, Class playerClass)
	{
		var otherRaces = RaceInfo.LookupByKey(nativeRace);

		if (otherRaces != null)
			foreach (var race in otherRaces)
				if (Global.ObjectMgr.GetPlayerInfo(race, playerClass) != null)
					return race;

		return Race.None;
	}

	private static uint GetDisplayIdForRace(Race race, Gender gender)
	{
		var displayIds = RaceDisplayIds.LookupByKey(race);

		if (displayIds != null)
			return displayIds[(int)gender];

		return 0;
	}

	private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var owner            = GetUnitOwner();
		var otherFactionRace = GetReplacementRace(owner.GetRace(), owner.GetClass());

		if (otherFactionRace == Race.None)
			return;

		var displayId = GetDisplayIdForRace(otherFactionRace, owner.GetNativeGender());

		if (displayId != 0)
			owner.SetDisplayId(displayId);

		if (mode.HasFlag(AuraEffectHandleModes.Real))
			UpdateRacials(owner.GetRace(), otherFactionRace);
	}

	private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var owner            = GetUnitOwner();
		var otherFactionRace = GetReplacementRace(owner.GetRace(), owner.GetClass());

		if (otherFactionRace == Race.None)
			return;

		UpdateRacials(otherFactionRace, owner.GetRace());
	}

	private void UpdateRacials(Race oldRace, Race newRace)
	{
		var player = GetUnitOwner().ToPlayer();

		if (player == null)
			return;

		foreach (var racialSkillId in RacialSkills)
		{
			if (Global.DB2Mgr.GetSkillRaceClassInfo(racialSkillId, oldRace, player.GetClass()) != null)
			{
				var skillLineAbilities = Global.DB2Mgr.GetSkillLineAbilitiesBySkill(racialSkillId);

				if (skillLineAbilities != null)
					foreach (var ability in skillLineAbilities)
						player.RemoveSpell(ability.Spell, false, false);
			}

			if (Global.DB2Mgr.GetSkillRaceClassInfo(racialSkillId, newRace, player.GetClass()) != null)
				player.LearnSkillRewardedSpells(racialSkillId, player.GetMaxSkillValueForLevel(), newRace);
		}
	}
}