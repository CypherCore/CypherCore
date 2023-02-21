DELETE FROM world.spell_script_names WHERE ID IN (47482, 47484, 47481, 390270, 207289, 390260, 390259);
INSERT INTO world.spell_script_names
(spell_id, ScriptName)
VALUES(47482, 'spell_dk_ghoul_leap');
INSERT INTO world.spell_script_names
(spell_id, ScriptName)
VALUES(47484, 'spell_dk_ghoul_huddle');
INSERT INTO world.spell_script_names
(spell_id, ScriptName)
VALUES(47481, 'spell_dk_ghoul_gnaw');
INSERT INTO world.spell_script_names
(spell_id, ScriptName)
VALUES(390270, 'spell_dk_coil_of_devastation');
INSERT INTO world.spell_script_names
(spell_id, ScriptName)
VALUES(207289, 'spell_dk_unholy_assault');
INSERT INTO world.spell_script_names
(spell_id, ScriptName)
VALUES(390260, 'spell_dk_commander_of_the_dead_aura');
INSERT INTO world.spell_script_names
(spell_id, ScriptName)
VALUES(390259, 'spell_dk_commander_of_the_dead_aura_proc');

DELETE FROM hotfixes.spell WHERE ID IN (390259);
INSERT INTO hotfixes.spell
(ID, NameSubtext, Description, AuraDescription, VerifiedBuild)
VALUES(390259, '', 'Dark Transformation also empowers your $?s207349[Dark Arbiter][Gargoyle] and Army of the Dead for $390264d, increasing their damage by $390264s1%.', '', 2147483647);

DELETE FROM hotfixes.spell_effect WHERE SpellID IN (390259);
INSERT INTO hotfixes.spell_effect
(ID, EffectAura, DifficultyID, EffectIndex, Effect, EffectAmplitude, EffectAttributes, EffectAuraPeriod, EffectBonusCoefficient, EffectChainAmplitude, EffectChainTargets, EffectItemType, EffectMechanic, EffectPointsPerResource, EffectPosFacing, EffectRealPointsPerLevel, EffectTriggerSpell, BonusCoefficientFromAP, PvpMultiplier, Coefficient, Variance, ResourceCoefficient, GroupSizeBasePointsCoefficient, EffectBasePoints, ScalingClass, EffectMiscValue1, EffectMiscValue2, EffectRadiusIndex1, EffectRadiusIndex2, EffectSpellClassMask1, EffectSpellClassMask2, EffectSpellClassMask3, EffectSpellClassMask4, ImplicitTarget1, ImplicitTarget2, SpellID, VerifiedBuild)
VALUES(1026942, 42, 0, 0, 6, 0.0, 0, 0, 0.0, 1.0, 0, 0, 0, 0.0, 0.0, 0.0, 390260, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0, 0, 0, 0, 0, 0, 0, 32, 0, 1, 0, 390259, 2147483647);

DELETE FROM hotfixes.skill_line_ability WHERE ID IN (23712, 23713, 23714, 23715, 23716, 23717, 23718, 23719, 26341, 31464, 33877, 33898);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 23712, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 23713, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 23714, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 23715, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 23716, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 23717, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 23718, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 23719, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 26341, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 31464, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 33877, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO hotfixes.skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(NULL, NULL, NULL, 33898, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

DELETE FROM world.spell_proc WHERE SpellId IN (390270, 390259);
INSERT INTO world.spell_proc
(SpellId, SchoolMask, SpellFamilyName, SpellFamilyMask0, SpellFamilyMask1, SpellFamilyMask2, SpellFamilyMask3, ProcFlags, ProcFlags2, SpellTypeMask, SpellPhaseMask, HitMask, AttributesMask, DisableEffectsMask, ProcsPerMinute, Chance, Cooldown, Charges)
VALUES(390270, 32, 15, 8192, 2048, 16385, 0, 65536, 4, 0, 2, 0, 2, 0, 0.0, 101.0, 0, 0);
INSERT INTO world.spell_proc
(SpellId, SchoolMask, SpellFamilyName, SpellFamilyMask0, SpellFamilyMask1, SpellFamilyMask2, SpellFamilyMask3, ProcFlags, ProcFlags2, SpellTypeMask, SpellPhaseMask, HitMask, AttributesMask, DisableEffectsMask, ProcsPerMinute, Chance, Cooldown, Charges)
VALUES(390259, 0, 15, 0, 0, 32, 0, 16384, 0, 0, 2, 0, 0, 0, 0.0, 101.0, 0, 0);

