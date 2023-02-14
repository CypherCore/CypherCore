// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Cryptography.Ed25519.Internal.Ed25519Ref10
{
	internal static partial class FieldOperations
	{
		public static void fe_1(out FieldElement h)
		{
			h = default(FieldElement);
			h.x0 = 1;
		}
	}
}