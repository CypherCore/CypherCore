// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Spells
{
    public class AuraLoadEffectInfo
    {
        public int[] Amounts = new int[SpellConst.MaxEffects];
        public int[] BaseAmounts = new int[SpellConst.MaxEffects];
    }
}