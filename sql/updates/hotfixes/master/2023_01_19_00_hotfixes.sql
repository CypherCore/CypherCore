
DROP TABLE IF EXISTS `char_base_info`;
CREATE TABLE IF NOT EXISTS `char_base_info` (
  `ID` int unsigned NOT NULL DEFAULT '0',
  `RaceID` tinyint NOT NULL DEFAULT '0',
  `ClassID` tinyint NOT NULL DEFAULT '0',
  `FactionXferId` int NOT NULL DEFAULT '0',
  `VerifiedBuild` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`,`VerifiedBuild`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
