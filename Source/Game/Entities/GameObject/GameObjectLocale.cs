// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;

namespace Game.Entities
{
    public class GameObjectLocale
    {
        public StringArray CastBarCaption { get; set; } = new((int)Locale.Total);
        public StringArray Name { get; set; } = new((int)Locale.Total);
        public StringArray Unk1 { get; set; } = new((int)Locale.Total);
    }
}