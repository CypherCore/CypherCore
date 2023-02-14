// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Database
{
    public static class DB
    {
        public static LoginDatabase Login = new();
        public static CharacterDatabase Characters = new();
        public static WorldDatabase World = new();
        public static HotfixDatabase Hotfix = new();
    }
}
