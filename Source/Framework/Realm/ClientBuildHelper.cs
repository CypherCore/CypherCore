// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Framework.ClientBuild
{
    struct ClientBuildPlatformType
    {
        public static int Windows = "Win".fourcc();
        public static int macOS = "Mac".fourcc();
    }

    struct ClientBuildArch
    {
        public static int x86 = "x86".fourcc();
        public static int x64 = "x64".fourcc();
        public static int Arm32 = "A32".fourcc();
        public static int Arm64 = "A64".fourcc();
        public static int WA32 = "WA32".fourcc();
    }

    struct ClientBuildType
    {
        public static int Retail = "WoW".fourcc();
        public static int RetailChina = "WoWC".fourcc();
        public static int Beta = "WoWB".fourcc();
        public static int BetaRelease = "WoWE".fourcc();
        public static int Ptr = "WoWT".fourcc();
        public static int PtrRelease = "WoWR".fourcc();
    }

    public class ClientBuildHelper
    {
        public static bool IsValid(string platform)
        {
            switch (platform)
            {
                case "Win":
                case "Wn64":
                case "WinA":
                case "Mac":
                case "Mc64":
                case "MacA":
                    return true;
                default:
                    break;
            }

            return false;
        }
    }

    public class ClientBuildVariantId
    {
        public int Platform;
        public int Arch;
        public int Type;
    }

    public class ClientBuildAuthKey
    {
        public static int Size = 16;

        public ClientBuildVariantId Variant;
        public byte[] Key = new byte[Size];
    }

    public class ClientBuildInfo
    {
        public uint Build;
        public uint MajorVersion;
        public uint MinorVersion;
        public uint BugfixVersion;
        public char[] HotfixVersion = new char[4];
        public List<ClientBuildAuthKey> AuthKeys = new();
    }
}
