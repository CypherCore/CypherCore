// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Cryptography.Ed25519.Internal.Ed25519Ref10
{
	internal static partial class GroupOperations
	{
		public static void ge_precomp_0(out GroupElementPreComp h)
		{
			FieldOperations.fe_1(out h.yplusx);
			FieldOperations.fe_1(out h.yminusx);
			FieldOperations.fe_0(out h.xy2d);
		}
	}
}