## CypherCore [![Build status](https://ci.appveyor.com/api/projects/status/ge4hjp1h1d28q25j?svg=true)](https://ci.appveyor.com/project/hondacrx/cyphercore)

CypherCore is an open source server project for World of Warcraft written in C#.

The current support game version is: 9.2.0.42423

### Prerequisites
* .NET 6.0 SDK [Download](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* MariaDB 10.6 or higher [Download](https://mariadb.org/download/)
* Optional: Visual Studio 2022, Visual Studio Code or Jetbrains Rider

### Server Setup
* ~~Download and Complie the Extractor [Download](https://github.com/CypherCore/Tools)~~ Use TrinityCore extractors for now
* Run all extractors in the wow directory
* Copy all created folders into server directory (ex: C:\CypherCore\Data)
* Make sure Conf files are updated and point the the correct folders and sql user and databases

### Installing the database
* Download the full Trinity Core database (TDB 910.21101) [Download](https://github.com/TrinityCore/TrinityCore/releases)
* Extract the sql files into the core sql folder (ex: C:\CypherCore\sql)

### Playing
* Must use Arctium WoW Client Launcher [Download](https://arctium.io/wow)

### Support / General Info
* Check out our Discord [Here](https://discord.gg/tCx3JbJ5qQ)
* Check out Trinity Core Wiki as a few steps are the same [Here](https://trinitycore.atlassian.net/wiki/spaces/tc/pages/2130077/Installation+Guide)

### Legal
* Blizzard, Battle.net, World of Warcraft, and all associated logos and designs are trademarks or registered trademarks of Blizzard Entertainment.
* All other trademarks are the property of their respective owners. This project is **not** affiliated with Blizzard Entertainment or any of their family of sites.