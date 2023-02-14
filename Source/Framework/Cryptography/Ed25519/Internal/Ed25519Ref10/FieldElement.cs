// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Diagnostics.Contracts;

namespace Framework.Cryptography.Ed25519.Internal.Ed25519Ref10
{
    internal struct FieldElement
    {
        internal int x0, x1, x2, x3, x4, x5, x6, x7, x8, x9;

        internal FieldElement(params int[] elements)
        {
            x0 = elements[0];
            x1 = elements[1];
            x2 = elements[2];
            x3 = elements[3];
            x4 = elements[4];
            x5 = elements[5];
            x6 = elements[6];
            x7 = elements[7];
            x8 = elements[8];
            x9 = elements[9];
        }
    }
}
