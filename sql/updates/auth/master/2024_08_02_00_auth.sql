DELETE FROM `build_info` WHERE `build` IN (55666,55792,55793,55818,55824,55846,55933,55939);
INSERT INTO `build_info` (`build`,`majorVersion`,`minorVersion`,`bugfixVersion`,`hotfixVersion`,`winAuthSeed`,`win64AuthSeed`,`mac64AuthSeed`,`winChecksumSeed`,`macChecksumSeed`) VALUES
(55666,11,0,0,NULL,NULL,'F7E5A88E4D3615B652C8B9D76E7F617C',NULL,NULL,NULL),
(55792,11,0,0,NULL,NULL,'C4DECDAA44BC548FF09EF3BB837D2147',NULL,NULL,NULL),
(55793,11,0,0,NULL,NULL,'F9CF3232AD1C38C2028668D5BB64198F',NULL,NULL,NULL),
(55818,11,0,0,NULL,NULL,'903A9B546248F71B16D9D9B06A072C24',NULL,NULL,NULL),
(55824,11,0,0,NULL,NULL,'8A6F13269A2896067A1E88789FB41BA7',NULL,NULL,NULL),
(55846,11,0,0,NULL,NULL,'BB5E1BED705872C226834B95A9E9F8A9',NULL,NULL,NULL),
(55933,11,0,0,NULL,NULL,'C34D42CB351C400D1319D221197CF18D',NULL,NULL,NULL),
(55939,11,0,0,NULL,NULL,'91529F4CE41DE4E54E132660ACDCADC5',NULL,NULL,NULL);

UPDATE `realmlist` SET `gamebuild`=55939 WHERE `gamebuild`=55664;

ALTER TABLE `realmlist` CHANGE `gamebuild` `gamebuild` int unsigned NOT NULL DEFAULT '55939';