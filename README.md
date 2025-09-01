## CypherCore [![Build status](https://ci.appveyor.com/api/projects/status/ge4hjp1h1d28q25j?svg=true)](https://ci.appveyor.com/project/hondacrx/cyphercore)

CypherCore is an open source server project for World of Warcraft written in C#.

The current support game version is: 11.2.0.62876

### Prerequisites
* .NET 8.0 SDK [Download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* MariaDB 10.6 or higher [Download](https://mariadb.org/download/)
* Optional: Visual Studio 2022, Visual Studio Code or Jetbrains Rider

### Server Setup
* ~~Download and Complie the Extractor [Download](https://github.com/CypherCore/Tools)~~ Use TrinityCore extractors for now: [Download](https://ci.appveyor.com/project/DDuarte/trinitycore/branch/master/artifacts)
* Run all extractors in the wow directory
* Copy all created folders into server directory (ex: C:\CypherCore\Data)
* Make sure Conf files are updated and point the the correct folders and sql user and databases

### Installing the database
* Download the full Trinity Core database (TDB 1120.25081) [Download](https://github.com/TrinityCore/TrinityCore/releases)
* Extract the sql files into the core sql folder (ex: C:\CypherCore\sql)

### Playing
* Must use Arctium WoW Client Launcher [Download](https://arctium.io/wow)

### Support / General Info
* Check out our Discord [Here](https://discord.gg/tCx3JbJ5qQ)
* Check out Trinity Core Wiki as a few steps are the same [Here](https://trinitycore.info)

### Legal
* Blizzard, Battle.net, World of Warcraft, and all associated logos and designs are trademarks or registered trademarks of Blizzard Entertainment.
* All other trademarks are the property of their respective owners. This project is **not** affiliated with Blizzard Entertainment or any of their family of sites.
