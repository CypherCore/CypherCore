-- Description: Deathknight Spells
-- Copyright Forged Wow LLC
-- Licensed under GPL v3.0 https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE 
-- For Table Hashes:
-- https://docs.google.com/spreadsheets/d/17t9fLLc4WUBtI9CK4DIkVVE7ICC1LMYfZ-kmocT72Mw/edit?usp=sharing 
DELETE FROM spell WHERE ID IN (390259);
INSERT INTO spell
(ID, NameSubtext, Description, AuraDescription, VerifiedBuild)
VALUES(390259, '', 'Dark Transformation also empowers your $?s207349[Dark Arbiter][Gargoyle] and Army of the Dead for $390264d, increasing their damage by $390264s1%.', '', 45745);

-- spell table hash 

DELETE FROM spell_effect WHERE SpellID IN (390259);
INSERT INTO spell_effect
(ID, EffectAura, DifficultyID, EffectIndex, Effect, EffectAmplitude, EffectAttributes, EffectAuraPeriod, EffectBonusCoefficient, EffectChainAmplitude, EffectChainTargets, EffectItemType, EffectMechanic, EffectPointsPerResource, EffectPosFacing, EffectRealPointsPerLevel, EffectTriggerSpell, BonusCoefficientFromAP, PvpMultiplier, Coefficient, Variance, ResourceCoefficient, GroupSizeBasePointsCoefficient, EffectBasePoints, ScalingClass, EffectMiscValue1, EffectMiscValue2, EffectRadiusIndex1, EffectRadiusIndex2, EffectSpellClassMask1, EffectSpellClassMask2, EffectSpellClassMask3, EffectSpellClassMask4, ImplicitTarget1, ImplicitTarget2, SpellID, VerifiedBuild)
VALUES(1026942, 42, 0, 0, 6, 0.0, 0, 0, 0.0, 1.0, 0, 0, 0, 0.0, 0.0, 0.0, 390260, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0, 0, 0, 0, 0, 0, 0, 32, 0, 1, 0, 390259, 45745);

-- spell_effect table hash 4030871717
DELETE FROM `hotfix_data` WHERE `Id`=68943 AND `TableHash`=4030871717;
INSERT INTO `hotfix_data` (`Id`, `UniqueId`, `TableHash`, `RecordId`, `Status`, `VerifiedBuild`) VALUES (68943, 3734412021, 4030871717, 1026942, 1, 45745);

-- These are missing skill line and spellid fields, they cannot be null.

DELETE FROM skill_line_ability WHERE ID IN (23712, 23713, 23714, 23715, 23716, 23717, 23718, 23719, 26341, 31464, 33877, 33898);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 23712, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 23713, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 23714, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 23715, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 23716, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 23717, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 23718, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 23719, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 26341, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 31464, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 33877, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);
INSERT INTO skill_line_ability
(RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, VerifiedBuild)
VALUES(0, NULL, NULL, 33898, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 45745);

-- skill_line_ability table hash 4282664694
DELETE FROM `hotfix_data` WHERE `Id`=68943 AND `TableHash`=4282664694;
INSERT INTO `hotfix_data` (`Id`, `UniqueId`, `TableHash`, `RecordId`, `Status`, `VerifiedBuild`) VALUES
(68943, 3734412021, 4282664694, 23712, 1, 45745),
(68943, 3734412021, 4282664694, 23713, 1, 45745),
(68943, 3734412021, 4282664694, 23714, 1, 45745),
(68943, 3734412021, 4282664694, 23715, 1, 45745),
(68943, 3734412021, 4282664694, 23716, 1, 45745),
(68943, 3734412021, 4282664694, 23717, 1, 45745),
(68943, 3734412021, 4282664694, 23718, 1, 45745),
(68943, 3734412021, 4282664694, 23719, 1, 45745),
(68943, 3734412021, 4282664694, 26341, 1, 45745),
(68943, 3734412021, 4282664694, 31464, 1, 45745),
(68943, 3734412021, 4282664694, 33877, 1, 45745),
(68943, 3734412021, 4282664694, 33898, 1, 45745);
