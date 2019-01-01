/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Network;
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.CollectionItemSetFavorite)]
        void HandleCollectionItemSetFavorite(CollectionItemSetFavorite collectionItemSetFavorite)
        {
            switch (collectionItemSetFavorite.Type)
            {
                case CollectionType.Toybox:
                    GetCollectionMgr().ToySetFavorite(collectionItemSetFavorite.ID, collectionItemSetFavorite.IsFavorite);
                    break;
                case CollectionType.Appearance:
                    {
                        var pair = GetCollectionMgr().HasItemAppearance(collectionItemSetFavorite.ID);
                        if (!pair.Item1 || pair.Item2)
                            return;

                        GetCollectionMgr().SetAppearanceIsFavorite(collectionItemSetFavorite.ID, collectionItemSetFavorite.IsFavorite);
                        break;
                    }
                case CollectionType.TransmogSet:
                    break;
                default:
                    break;
            }
        }
    }
}
