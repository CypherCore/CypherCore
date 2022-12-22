--
-- Table structure for table `talent_tab`
--

DROP TABLE IF EXISTS `talent_tab`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `talent_tab` (
  `ID` int(10) unsigned NOT NULL DEFAULT '0',
  `Name` text,
  `BackgroundFile` text,
  `OrderIndex` int(11) NOT NULL DEFAULT '0',
  `RaceMask` int(11) NOT NULL DEFAULT '0',
  `ClassMask` int(11) NOT NULL DEFAULT '0',
  `PetTalentMask` int(11) NOT NULL DEFAULT '0',
  `SpellIconID` int(11) NOT NULL DEFAULT '0',
  `VerifiedBuild` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`,`VerifiedBuild`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `talent_tab`
--

LOCK TABLES `talent_tab` WRITE;
/*!40000 ALTER TABLE `talent_tab` DISABLE KEYS */;
/*!40000 ALTER TABLE `talent_tab` ENABLE KEYS */;
UNLOCK TABLES;
