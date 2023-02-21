-- Copyright Forged Wow LLC
-- Licensed under GPL v3.0 https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE 
DELETE FROM `num_talents_at_level` WHERE `ID` IN (124, 125, 126, 127, 128, 129, 130, 131, 132, 133);

INSERT INTO `num_talents_at_level` (`ID`, `NumTalents`, `NumTalentsDeathKnight`, `NumTalentsDemonHunter`, `VerifiedBuild`) VALUES 
(124, 72, 72, 72, 45745),
(125, 73, 73, 73, 45745),
(126, 74, 74, 74, 45745),
(127, 75, 75, 75, 45745),
(128, 76, 76, 76, 45745),
(129, 77, 77, 77, 45745),
(130, 78, 78, 78, 45745),
(131, 79, 79, 79, 45745),
(132, 80, 80, 80, 45745),
(133, 81, 81, 81, 45745);

DELETE FROM `hotfix_data` WHERE `Id` = 80000 AND `TableHash` = 262173489 AND `VerifiedBuild` = 45745;

INSERT INTO `hotfix_data` (`Id`, `UniqueId`, `TableHash`, `RecordId`, `Status`, `VerifiedBuild`) VALUES 
(80000, 123456789, 262173489, 124, 1, 45745),
(80000, 123456789, 262173489, 125, 1, 45745),
(80000, 123456789, 262173489, 126, 1, 45745),
(80000, 123456789, 262173489, 127, 1, 45745),
(80000, 123456789, 262173489, 128, 1, 45745),
(80000, 123456789, 262173489, 129, 1, 45745),
(80000, 123456789, 262173489, 130, 1, 45745),
(80000, 123456789, 262173489, 131, 1, 45745),
(80000, 123456789, 262173489, 132, 1, 45745),
(80000, 123456789, 262173489, 133, 1, 45745);