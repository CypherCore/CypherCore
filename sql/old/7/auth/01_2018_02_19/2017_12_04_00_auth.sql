UPDATE `realmlist` SET `gamebuild`=25549 WHERE `gamebuild`=25480;

ALTER TABLE `realmlist` CHANGE `gamebuild` `gamebuild` int(10) unsigned NOT NULL DEFAULT '25549';