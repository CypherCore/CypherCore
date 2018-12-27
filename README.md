## CypherCore [![Build Status](https://travis-ci.org/CypherCore/CypherCore.svg?branch=master)](https://travis-ci.org/CypherCore/CypherCore) [![Build status](https://ci.appveyor.com/api/projects/status/ge4hjp1h1d28q25j?svg=true)](https://ci.appveyor.com/project/hondacrx/cyphercore)

CypherCore is an open source server project for World of Warcraft written in C#.

The current support game version is: 8.0.1.28153

### Prerequisites
* Visual Studio 2017 with netcore 2.0 [Download](https://www.visualstudio.com/downloads/)
* Mysql Database 5.6 or higher [Download](https://dev.mysql.com/downloads/mysql/)

### Server Setup
* Download and Complie the Extractor [Download](https://github.com/CypherCore/Tools)
* Run all extractors in the wow directory
* Copy all created folders into server directory (ex: C:\CypherCore\Data)
* Make sure Conf files are updated and point the the correct folders and sql user and databases

### Installing the database
* Download the full Trinity Core database (TDB 735.00) [Download](https://github.com/TrinityCore/TrinityCore/releases)
* Extract the sql files into the core sql folder (ex: C:\CypherCore\sql)

### Playing
* Must use Arctium WoW Client Launcher [Download](https://arctium.io/files/?f=15ac71b2f0df3f)
* Must start the launch with the argument --custom 
(make a shortcut and edit shortcut and add argument to target ex: Arctium WoW Client Launcher.exe" --custom)

### Support / General Info
* Check out our channel on Arctium Discord [Here](https://discord.gg/Hac3qn6)
* Check out Trinity Core Wiki as a few steps are the same [Here](https://trinitycore.atlassian.net/wiki/spaces/tc/pages/2130077/Installation+Guide)
