## Forged Core [![Build status](https://ci.appveyor.com/api/projects/status/2wgy97jxy0wfl7ly?svg=true)](https://ci.appveyor.com/project/JBurlison/forgedcore)

Forged Core is an open source branch of CypherCore.
Forge Core is the core of the Forged WoW Server and aims to be the most complete and up to date core.
Forged Core also features a very extencible script loading system for easy additions to all types of gameplay. Scripts can be loaded into the script directory and reloaded during runtime.

[Forged WoW Website](http://forgedwow.gg/)

CypherCore is an open source server project for World of Warcraft written in C#.

The current support game version is: 10.0.5.48001

### Prerequisites
* .NET 7.0 SDK [Download](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
* MariaDB 10.6/Mysql 8 or higher [Download](https://mariadb.org/download/)
* Optional: Visual Studio 2022, Visual Studio Code or Jetbrains Rider

### Server Setup
* ~~Download and Complie the Extractor [Download](https://github.com/CypherCore/Tools)~~ Use TrinityCore extractors for now: [Download](https://ci.appveyor.com/project/DDuarte/trinitycore/branch/master/artifacts)
* Run all extractors in the wow directory
* Copy all created folders into server directory (ex: C:\CypherCore\Data)
* Make sure Conf files are updated and point the the correct folders and sql user and databases

### Installing the database
* Download the full Trinity Core database (TDB 1002.22121) [Download](https://github.com/TrinityCore/TrinityCore/releases)
* Extract the sql files into the core sql folder (ex: C:\CypherCore\sql)

### Playing
* Must use Arctium WoW Client Launcher [Download](https://arctium.io/wow)

### Support / General Info
* Check out our Discord [Here](https://discord.gg/forgedwow)
* Check out Trinity Core Wiki as a few steps are the same [Here](https://trinitycore.atlassian.net/wiki/spaces/tc/pages/2130077/Installation+Guide)

### Legal
* Blizzard, Battle.net, World of Warcraft, and all associated logos and designs are trademarks or registered trademarks of Blizzard Entertainment.
* All other trademarks are the property of their respective owners. This project is **not** affiliated with Blizzard Entertainment or any of their family of sites.
