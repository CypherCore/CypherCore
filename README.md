CypherCore is an open source server project for World of Warcraft written in C#.

The current support game version is: 3.4.0.47168

### Prerequisites
* .NET 6.0 SDK [Download](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* MariaDB 10.6 or higher [Download](https://mariadb.org/download/)
* Optional: Visual Studio 2022, Visual Studio Code or Jetbrains Rider

### Server Setup
* ~~Download and Complie the Extractor [Download](https://github.com/CypherCore/Tools)~~ Use TrinityCore extractors for now: [Download](https://github.com/TrinityCore/TrinityCore/tree/wotlk_classic)
* Run all extractors in the wow directory
* Copy all created folders into server directory (ex: C:\CypherCore\Data)
* Make sure Conf files are updated and point the the correct folders and sql user and databases

### Installing the database
* Download the full Trinity Core database (TDB_full_world_927.22082_2022_08_21) [Download](https://github.com/TrinityCore/TrinityCore/releases)
* Extract the sql files (full and updates) into the core sql folder (ex: C:\CypherCore\sql)

### Playing
* Must use Arctium WoW Client Launcher [Download](https://arctium.io/wow)
* Create link with next parameters (example for Windows): "<path>\World of Warcraft Classic\Arctium WoW Launcher.exe" --version=Classic
* Modify your "<path>\World of Warcraft\_classic_\WTF\Config.wtf"  ->  SET portal "127.0.0.1"

### Support / General Info
* Check out our Discord [Here](https://discord.gg/tCx3JbJ5qQ)
* Check out Trinity Core Wiki as a few steps are the same [Here](https://trinitycore.atlassian.net/wiki/spaces/tc/pages/2130077/Installation+Guide)
* The project is currently under development and a lot of things have not been implemented. Updated according to updates in the appropriate branch of [TrinityCore](https://github.com/TrinityCore/TrinityCore/tree/wotlk_classic)

### Legal
* Blizzard, Battle.net, World of Warcraft, and all associated logos and designs are trademarks or registered trademarks of Blizzard Entertainment.
* All other trademarks are the property of their respective owners. This project is **not** affiliated with Blizzard Entertainment or any of their family of sites.
