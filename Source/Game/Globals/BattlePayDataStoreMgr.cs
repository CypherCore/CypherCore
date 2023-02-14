// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Collections;
using Framework.Database;
using Game.Entities;
using Game.Networking.Packets.Bpay;

namespace Game
{
    public class BattlePayDataStoreMgr : Singleton<BattlePayDataStoreMgr>
    {

        public List<BpayGroup> ProductGroups { get; private set; } = new List<BpayGroup>();
        public List<BpayShop> ShopEntries { get; private set; } = new List<BpayShop>();
        public SortedDictionary<uint, BpayProduct> Products { get; private set; } = new SortedDictionary<uint, BpayProduct>();
        public SortedDictionary<uint, BpayProductInfo> ProductInfos { get; private set; } = new SortedDictionary<uint, BpayProductInfo>();
        public SortedDictionary<uint, BpayDisplayInfo> DisplayInfos { get; private set; } = new SortedDictionary<uint, BpayDisplayInfo>();
        public SortedDictionary<uint, ProductAddon> ProductAddons { get; private set; } = new SortedDictionary<uint, ProductAddon>();

        public void Initialize()
        {
            LoadProductAddons();
            LoadDisplayInfos();
            LoadProduct();
            LoadProductGroups();
            LoadShopEntries();
        }

        public List<BpayProduct> GetProductsOfProductInfo(uint productInfoEntry)
        {
            /*std::vector<BattlePayData::Product> subproducts = {};

            for (auto productInfo : _productInfos)
                if (productInfo.second.Entry == productInfoEntry)
                    for (uint32 productid : productInfo.second.ProductIds)
                    {
                        //TC_LOG_INFO("server.BattlePay", "GetProductsOfProductInfo: found product [{}] at productInfo [{}]", productid, productInfoEntry);
                        subproducts.push_back(*GetProduct(productid));
                    }

            if (subproducts.size() > 0)
                return &subproducts; // warning*/

            Log.outInfo(LogFilter.BattlePay, "GetProductsOfProductInfo failed for productInfoEntry {}", productInfoEntry);
            return null;
        }

        public List<BpayProductItem> GetItemsOfProduct(uint productID)
        {
            foreach (var product in Products)
            {
                if (product.Value.ProductId == productID)
                {
                    return product.Value.Items;
                }
            }

            Log.outInfo(LogFilter.BattlePay, "GetItemsOfProduct failed for productid {}", productID);
            return null;
        }

        public BpayProduct GetProduct(uint productID)
        {
            return Products.GetValueOrDefault(productID);
        }


        // This awesome function returns back the productinfo for all the two types of productid!
        public BpayProductInfo GetProductInfoForProduct(uint productID)
        {
            // Find product by subproduct id (_productInfos.productids) if not found find it by shop productid (_productInfos.productid)
            if (!ProductInfos.TryGetValue(productID, out var prod))
            {
                foreach (var productInfo in ProductInfos)
                    if (productInfo.Value.ProductId == productID)
                        return productInfo.Value;

                Log.outInfo(LogFilter.BattlePay, "GetProductInfoForProduct failed for productID {}", productID);
                return null;
            }

            return prod;
        }

        public BpayDisplayInfo GetDisplayInfo(uint displayInfoEntry)
        {
            return DisplayInfos.GetValueOrDefault(displayInfoEntry);
        }


        // Custom properties for each product (displayinfoEntry, productInfoEntry, shopEntry are the same)
        public ProductAddon GetProductAddon(uint displayInfoEntry)
        {
            return ProductAddons.GetValueOrDefault(displayInfoEntry);
        }

        public uint GetProductGroupId(uint productId)
        {
            foreach (var shop in ShopEntries)
                if (shop.ProductID == productId)
                    return shop.GroupID;

            return 0;
        }

        public bool ProductExist(uint productID)
        {
            if (Products.ContainsKey(productID))
            {
                return true;
            }

            Log.outInfo(LogFilter.BattlePay, "ProductExist failed for productID {}", productID);
            return false;
        }

        public bool DisplayInfoExist(uint displayInfoEntry)
        {
            if (DisplayInfos.ContainsKey(displayInfoEntry))
            {
                return true;
            }

            Log.outInfo(LogFilter.BattlePay, "DisplayInfoExist failed for displayInfoEntry {}", displayInfoEntry);
            return false;
        }

        private void LoadProductAddons()
        {
            Log.outInfo(LogFilter.BattlePay, "Loading Battlepay display info addons ...");
            ProductAddons.Clear();

            var result = DB.World.Query("SELECT DisplayInfoEntry, DisableListing, DisableBuy, NameColorIndex, ScriptName, Comment FROM battlepay_addon");
            if (result == null)
            {
                return;
            }

            do
            {
                var fields = result.GetFields();

                ProductAddon productAddon = new ProductAddon();
                productAddon.DisplayInfoEntry = fields.Read<uint>(0);
                productAddon.DisableListing = fields.Read<byte>(1);
                productAddon.DisableBuy = fields.Read<byte>(2);
                productAddon.NameColorIndex = fields.Read<byte>(3);
                productAddon.ScriptName = fields.Read<string>(4);
                productAddon.Comment = fields.Read<string>(5);
                ProductAddons.Add(fields.Read<uint>(0), productAddon);
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, ">> Loaded {} Battlepay product addons", (ulong)ProductAddons.Count);
        }

        private void LoadProductGroups()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading Battlepay product groups ...");
            ProductGroups.Clear();

            var result = DB.World.Query("SELECT Entry, GroupId, IconFileDataID, DisplayType, Ordering, Unk, Name, Description FROM battlepay_group");
            if (result == null)
            {
                return;
            }

            do
            {
                var fields = result.GetFields();

                BpayGroup productGroup = new BpayGroup();
                productGroup.Entry = fields.Read<uint>(0);
                productGroup.GroupId = fields.Read<uint>(1);
                productGroup.IconFileDataID = fields.Read<uint>(2);
                productGroup.DisplayType = fields.Read<byte>(3);
                productGroup.Ordering = fields.Read<uint>(4);
                productGroup.Unk = fields.Read<uint>(5);
                productGroup.Name = fields.Read<string>(6);
                productGroup.Description = fields.Read<string>(7);
                ProductGroups.Add(productGroup);
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, ">> Loaded {} Battlepay product groups", (ulong)ProductGroups.Count);
        }

        private void LoadProduct()
        {
            Log.outInfo(LogFilter.ServerLoading,  "Loading Battlepay products ...");
            Products.Clear();
            ProductInfos.Clear();

            // Product Info
            var result = DB.World.Query("SELECT Entry, ProductId, NormalPriceFixedPoint, CurrentPriceFixedPoint, ProductIds, Unk1, Unk2, UnkInts, Unk3, ChoiceType FROM battlepay_productinfo");
            if (result == null)
            {
                return;
            }

            do
            {
                var fields = result.GetFields();

                BpayProductInfo productInfo = new BpayProductInfo();
                productInfo.Entry = fields.Read<uint>(0);
                productInfo.ProductId = fields.Read<uint>(1);
                productInfo.NormalPriceFixedPoint = fields.Read<uint>(2);
                productInfo.CurrentPriceFixedPoint = fields.Read<uint>(3);
                StringArray subproducts_stream = new StringArray(fields.Read<string>(4), ',');
                foreach (string subproduct in subproducts_stream)
                {
                    if (uint.TryParse(subproduct, out uint productId))
                        productInfo.ProductIds.Add(productId); // another cool flux stuff: multiple subproducts can be added in one column
                }
                productInfo.Unk1 = fields.Read<uint>(5);
                productInfo.Unk2 = fields.Read<uint>(6);
                productInfo.UnkInts.Add(fields.Read<uint>(7));
                productInfo.Unk3 = fields.Read<uint>(8);
                productInfo.ChoiceType = fields.Read<uint>(9);

                // we copy store the info for every product - some product info is the same for multiple products
                foreach (uint subproductid in productInfo.ProductIds)
                {
                    ProductInfos.Add(subproductid, productInfo);
                }
            } while (result.NextRow());

            // Product
            result = DB.World.Query("SELECT Entry, ProductId, Type, Flags, Unk1, DisplayId, ItemId, Unk4, Unk5, Unk6, Unk7, Unk8, Unk9, UnkString, UnkBit, UnkBits, Name FROM battlepay_product");
            if (result == null)
            {
                return;
            }

            do
            {
                var fields = result.GetFields();

                BpayProduct product = new BpayProduct();
                product.Entry = fields.Read<uint>(0);
                product.ProductId = fields.Read<uint>(1);
                product.Type = fields.Read<byte>(2);
                product.Flags = fields.Read<uint>(3);
                product.Unk1 = fields.Read<uint>(4);
                product.DisplayId = fields.Read<uint>(5);
                product.ItemId = fields.Read<uint>(6);
                product.Unk4 = fields.Read<uint>(7);
                product.Unk5 = fields.Read<uint>(8);
                product.Unk6 = fields.Read<uint>(9);
                product.Unk7 = fields.Read<uint>(10);
                product.Unk8 = fields.Read<uint>(11);
                product.Unk9 = fields.Read<uint>(12);
                product.UnkString = fields.Read<string>(13);
                product.UnkBit = fields.Read<bool>(14);
                product.UnkBits = fields.Read<uint>(15);
                product.Name = fields.Read<string>(16); // unused in packets but useful in other ways

                Products.Add(fields.Read<uint>(1), product);
            } while (result.NextRow());

            // Product Items
            result = DB.World.Query("SELECT ID, UnkByte, ItemID, Quantity, UnkInt1, UnkInt2, IsPet, PetResult, Display FROM battlepay_item");

            if (result == null)
            {
                return;
            }

            do
            {
                var fields = result.GetFields();

                var productID = fields.Read<uint>(1);

                if (!Products.ContainsKey(productID))
                    continue;

                BpayProductItem productItem = new BpayProductItem();

                productItem.ItemID = fields.Read<uint>(2); 

                if (Global.ObjectMgr.GetItemTemplate(productItem.ItemID) != null)
                    continue;

                productItem.Entry = fields.Read<uint>(0);
                productItem.ID = productID;
                productItem.UnkByte = fields.Read<byte>(2);
                productItem.ItemID = fields.Read<uint>(3);
                productItem.Quantity = fields.Read<uint>(4);
                productItem.UnkInt1 = fields.Read<uint>(5);
                productItem.UnkInt2 = fields.Read<uint>(6);
                productItem.IsPet = fields.Read<bool>(7);
                productItem.PetResult = fields.Read<uint>(8);
                Products[productID].Items.Add(productItem);
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading,  ">> Loaded {} Battlepay product infos and {} Battlepay products", (ulong)ProductInfos.Count, (ulong)Products.Count);
        }

        private void LoadShopEntries()
        {
            Log.outInfo(LogFilter.ServerLoading,  "Loading Battlepay shop entries ...");
            ShopEntries.Clear();

            var result = DB.World.Query("SELECT Entry, EntryID, GroupID, ProductID, Ordering, VasServiceType, StoreDeliveryType FROM battlepay_shop");

            if (result == null)
            {
                return;
            }

            do
            {
                var fields = result.GetFields();

                BpayShop shopEntry = new BpayShop();
                shopEntry.Entry = fields.Read<uint>(0);
                shopEntry.EntryId = fields.Read<uint>(1);
                shopEntry.GroupID = fields.Read<uint>(2);
                shopEntry.ProductID = fields.Read<uint>(3);
                shopEntry.Ordering = fields.Read<uint>(4);
                shopEntry.VasServiceType = fields.Read<uint>(5);
                shopEntry.StoreDeliveryType = fields.Read<byte>(6);
                ShopEntries.Add(shopEntry);
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading,  ">> Loaded {} Battlepay shop entries", (ulong)ShopEntries.Count);
        }

        private void LoadDisplayInfos()
        {
            Log.outInfo(LogFilter.ServerLoading,  "Loading Battlepay display info ...");
            DisplayInfos.Clear();

            var result = DB.World.Query("SELECT Entry, CreatureDisplayID, VisualID, Name1, Name2, Name3, Name4, Name5, Name6, Name7, Flags, Unk1, Unk2, Unk3, UnkInt1, UnkInt2, UnkInt3 FROM battlepay_displayinfo");
            if (result == null)
            {
                return;
            }

            do
            {
                var fields = result.GetFields();

                BpayDisplayInfo displayInfo = new BpayDisplayInfo();
                displayInfo.Entry = fields.Read<uint>(0);
                displayInfo.CreatureDisplayID = fields.Read<uint>(1);
                displayInfo.VisualID = fields.Read<uint>(2);
                displayInfo.Name1 = fields.Read<string>(3);
                displayInfo.Name2 = fields.Read<string>(4);
                displayInfo.Name3 = fields.Read<string>(5);
                displayInfo.Name4 = fields.Read<string>(6);
                displayInfo.Name5 = fields.Read<string>(7);
                displayInfo.Name6 = fields.Read<string>(8);
                displayInfo.Name7 = fields.Read<string>(9);
                displayInfo.Flags = fields.Read<uint>(10);
                displayInfo.Unk1 = fields.Read<uint>(11);
                displayInfo.Unk2 = fields.Read<uint>(12);
                displayInfo.Unk3 = fields.Read<uint>(13);
                displayInfo.UnkInt1 = fields.Read<uint>(14);
                displayInfo.UnkInt2 = fields.Read<uint>(15);
                displayInfo.UnkInt3 = fields.Read<uint>(16);
                DisplayInfos.Add(fields.Read<uint>(0), displayInfo);
            } while (result.NextRow());

            result = DB.World.Query("SELECT Entry, DisplayId, VisualId, Unk, Name, DisplayInfoEntry FROM battlepay_visual");
            if (result == null)
            {
                return;
            }

            int visualCounter = 0;

            do
            {
                var fields = result.GetFields();

                visualCounter++;

                BpayVisual visualInfo = new BpayVisual();
                visualInfo.Entry = fields.Read<uint>(0);
                visualInfo.DisplayId = fields.Read<uint>(1);
                visualInfo.VisualId = fields.Read<uint>(2);
                visualInfo.Unk = fields.Read<uint>(3);
                visualInfo.Name = fields.Read<string>(4);
                visualInfo.DisplayInfoEntry = fields.Read<uint>(5);

                if (!DisplayInfos.TryGetValue(visualInfo.DisplayInfoEntry, out var bpayDisplayInfo))
                    continue;

                bpayDisplayInfo.Visuals.Add(visualInfo);
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading,  ">> Loaded {} Battlepay display info with {} visual.", (ulong)DisplayInfos.Count, visualCounter);
        }
    }
}
