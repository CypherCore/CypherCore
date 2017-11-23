ALTER TABLE `account` CHANGE `sessionkey` `sessionkey` varchar(128) NOT NULL DEFAULT '';

UPDATE `realmlist` SET `gamebuild`=25480 WHERE `gamebuild`=24742;

ALTER TABLE `realmlist` CHANGE `gamebuild` `gamebuild` int(10) unsigned NOT NULL DEFAULT '25480';