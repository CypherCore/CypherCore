using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    class TraitMgr
    {
        public static uint COMMIT_COMBAT_TRAIT_CONFIG_CHANGES_SPELL_ID = 384255u;
        public static uint MAX_COMBAT_TRAIT_CONFIGS = 10u;

        static Dictionary<uint, NodeGroup> _traitGroups = new();
        static Dictionary<int, Node> _traitNodes = new();
        static Dictionary<int, SubTree> _traitSubTrees = new();
        static Dictionary<int, Tree> _traitTrees = new();
        static uint[] _skillLinesByClass = new uint[(int)Class.Max];
        static MultiMap<uint, Tree> _traitTreesBySkillLine = new();
        static MultiMap<uint, Tree> _traitTreesByTraitSystem = new();
        static int _configIdGenerator;
        static MultiMap<uint, TraitCurrencySourceRecord> _traitCurrencySourcesByCurrency = new();
        static MultiMap<uint, TraitDefinitionEffectPointsRecord> _traitDefinitionEffectPointModifiers = new();
        static MultiMap<int, TraitTreeLoadoutEntryRecord> _traitTreeLoadoutsByChrSpecialization = new();

        public static void Load()
        {
            _configIdGenerator = int.MaxValue;

            MultiMap<uint, TraitCondRecord> nodeEntryConditions = new();
            foreach (TraitNodeEntryXTraitCondRecord traitNodeEntryXTraitCondEntry in CliDB.TraitNodeEntryXTraitCondStorage.Values)
            {
                TraitCondRecord traitCondEntry = CliDB.TraitCondStorage.LookupByKey(traitNodeEntryXTraitCondEntry.TraitCondID);
                if (traitCondEntry != null)
                    nodeEntryConditions.Add(traitNodeEntryXTraitCondEntry.TraitNodeEntryID, traitCondEntry);
            }

            MultiMap<uint, TraitCostRecord> nodeEntryCosts = new();
            foreach (TraitNodeEntryXTraitCostRecord traitNodeEntryXTraitCostEntry in CliDB.TraitNodeEntryXTraitCostStorage.Values)
            {
                TraitCostRecord traitCostEntry = CliDB.TraitCostStorage.LookupByKey(traitNodeEntryXTraitCostEntry.TraitCostID);
                if (traitCostEntry != null)
                    nodeEntryCosts.Add(traitNodeEntryXTraitCostEntry.TraitNodeEntryID, traitCostEntry);
            }

            MultiMap<uint, TraitCondRecord> nodeGroupConditions = new();
            foreach (TraitNodeGroupXTraitCondRecord traitNodeGroupXTraitCondEntry in CliDB.TraitNodeGroupXTraitCondStorage.Values)
            {
                TraitCondRecord traitCondEntry = CliDB.TraitCondStorage.LookupByKey(traitNodeGroupXTraitCondEntry.TraitCondID);
                if (traitCondEntry != null)
                    nodeGroupConditions.Add(traitNodeGroupXTraitCondEntry.TraitNodeGroupID, traitCondEntry);
            }

            MultiMap<uint, TraitCostRecord> nodeGroupCosts = new();
            foreach (TraitNodeGroupXTraitCostRecord traitNodeGroupXTraitCostEntry in CliDB.TraitNodeGroupXTraitCostStorage.Values)
            {
                TraitCostRecord traitCondEntry = CliDB.TraitCostStorage.LookupByKey(traitNodeGroupXTraitCostEntry.TraitCostID);
                if (traitCondEntry != null)
                    nodeGroupCosts.Add(traitNodeGroupXTraitCostEntry.TraitNodeGroupID, traitCondEntry);
            }

            MultiMap<int, uint> nodeGroups = new();
            foreach (TraitNodeGroupXTraitNodeRecord traitNodeGroupXTraitNodeEntry in CliDB.TraitNodeGroupXTraitNodeStorage.Values)
                nodeGroups.Add(traitNodeGroupXTraitNodeEntry.TraitNodeID, traitNodeGroupXTraitNodeEntry.TraitNodeGroupID);

            MultiMap<uint, TraitCondRecord> nodeConditions = new();
            foreach (TraitNodeXTraitCondRecord traitNodeXTraitCondEntry in CliDB.TraitNodeXTraitCondStorage.Values)
            {
                TraitCondRecord traitCondEntry = CliDB.TraitCondStorage.LookupByKey(traitNodeXTraitCondEntry.TraitCondID);
                if (traitCondEntry != null)
                    nodeConditions.Add(traitNodeXTraitCondEntry.TraitNodeID, traitCondEntry);
            }

            MultiMap<uint, TraitCostRecord> nodeCosts = new();
            foreach (TraitNodeXTraitCostRecord traitNodeXTraitCostEntry in CliDB.TraitNodeXTraitCostStorage.Values)
            {
                TraitCostRecord traitCostEntry = CliDB.TraitCostStorage.LookupByKey(traitNodeXTraitCostEntry.TraitCostID);
                if (traitCostEntry != null)
                    nodeCosts.Add(traitNodeXTraitCostEntry.TraitNodeID, traitCostEntry);
            }

            MultiMap<uint, TraitNodeEntryRecord> nodeEntries = new();
            foreach (TraitNodeXTraitNodeEntryRecord traitNodeXTraitNodeEntryEntry in CliDB.TraitNodeXTraitNodeEntryStorage.Values)
            {
                TraitNodeEntryRecord traitNodeEntryEntry = CliDB.TraitNodeEntryStorage.LookupByKey(traitNodeXTraitNodeEntryEntry.TraitNodeEntryID);
                if (traitNodeEntryEntry != null)
                    nodeEntries.Add(traitNodeXTraitNodeEntryEntry.TraitNodeID, traitNodeEntryEntry);
            }

            MultiMap<uint, TraitCostRecord> treeCosts = new();
            foreach (TraitTreeXTraitCostRecord traitTreeXTraitCostEntry in CliDB.TraitTreeXTraitCostStorage.Values)
            {
                TraitCostRecord traitCostEntry = CliDB.TraitCostStorage.LookupByKey(traitTreeXTraitCostEntry.TraitCostID);
                if (traitCostEntry != null)
                    treeCosts.Add(traitTreeXTraitCostEntry.TraitTreeID, traitCostEntry);
            }

            MultiMap<uint, TraitTreeXTraitCurrencyRecord> treeCurrencies = new();
            foreach (TraitTreeXTraitCurrencyRecord traitTreeXTraitCurrencyEntry in CliDB.TraitTreeXTraitCurrencyStorage.Values)
            {
                if (CliDB.TraitCurrencyStorage.HasRecord((uint)traitTreeXTraitCurrencyEntry.TraitCurrencyID))
                    treeCurrencies.Add(traitTreeXTraitCurrencyEntry.TraitTreeID, traitTreeXTraitCurrencyEntry);
            }

            MultiMap<uint, int> traitTreesIdsByTraitSystem = new();
            foreach (TraitTreeRecord traitTree in CliDB.TraitTreeStorage.Values)
            {
                Tree tree = new();
                tree.Data = traitTree;

                var costs = treeCosts.LookupByKey(traitTree.Id);
                if (costs != null)
                    tree.Costs = costs;

                var currencies = treeCurrencies.LookupByKey(traitTree.Id);
                if (currencies != null)
                {
                    currencies.OrderBy(p => p.Index);
                    tree.Currencies.AddRange(currencies.Select(p => CliDB.TraitCurrencyStorage.LookupByKey(p.TraitCurrencyID)));
                }

                if (traitTree.TraitSystemID != 0)
                {
                    traitTreesIdsByTraitSystem.Add(traitTree.TraitSystemID, (int)traitTree.Id);
                    tree.ConfigType = TraitConfigType.Generic;
                }

                _traitTrees[(int)traitTree.Id] = tree;
            }

            foreach (var (_, traitSubTree) in CliDB.TraitSubTreeStorage)
            {
                SubTree subTree = new();
                subTree.Data = traitSubTree;

                Tree tree = _traitTrees.LookupByKey(traitSubTree.TraitTreeID);
                if (tree != null)
                    tree.SubTrees.Add(subTree);

                _traitSubTrees[(int)traitSubTree.ID] = subTree;
            }

            foreach (var (_, traitNodeGroup) in CliDB.TraitNodeGroupStorage)
            {
                NodeGroup nodeGroup = new();
                nodeGroup.Data = traitNodeGroup;

                var conditions = nodeGroupConditions.LookupByKey(traitNodeGroup.Id);
                if (conditions != null)
                    nodeGroup.Conditions = conditions;

                var costs = nodeGroupCosts.LookupByKey(traitNodeGroup.Id);
                if (costs != null)
                    nodeGroup.Costs = costs;

                _traitGroups[traitNodeGroup.Id] = nodeGroup;
            }

            foreach (TraitNodeRecord traitNode in CliDB.TraitNodeStorage.Values)
            {
                Node node = new();
                node.Data = traitNode;

                Tree tree = _traitTrees.LookupByKey(traitNode.TraitTreeID);
                if (tree != null)
                    tree.Nodes.Add(node);

                foreach (var traitNodeEntry in nodeEntries.LookupByKey(traitNode.Id))
                {
                    NodeEntry entry = new();
                    entry.Data = traitNodeEntry;

                    var conditions = nodeEntryConditions.LookupByKey(traitNodeEntry.Id);
                    if (conditions != null)
                        entry.Conditions = conditions;

                    var costs = nodeEntryCosts.LookupByKey(traitNodeEntry.Id);
                    if (costs != null)
                        entry.Costs = costs;

                    node.Entries.Add(entry);
                }

                foreach (var nodeGroupId in nodeGroups.LookupByKey(traitNode.Id))
                {
                    NodeGroup nodeGroup = _traitGroups.LookupByKey(nodeGroupId);
                    if (nodeGroup == null)
                        continue;

                    nodeGroup.Nodes.Add(node);
                    node.Groups.Add(nodeGroup);
                }

                var conditions1 = nodeConditions.LookupByKey(traitNode.Id);
                if (conditions1 != null)
                    node.Conditions = conditions1;

                var costs1 = nodeCosts.LookupByKey(traitNode.Id);
                if (costs1 != null)
                    node.Costs = costs1;

                SubTree subTree = _traitSubTrees.LookupByKey(traitNode.TraitSubTreeID);
                if (subTree != null)
                {
                    subTree.Nodes.Add(node);

                    foreach (NodeEntry nodeEntry in node.Entries)
                    {
                        foreach (TraitCostRecord cost in nodeEntry.Costs)
                        {
                            TraitCurrencyRecord traitCurrency = CliDB.TraitCurrencyStorage.LookupByKey(cost.TraitCurrencyID);
                            if (traitCurrency != null)
                                subTree.Currencies.Add(traitCurrency);
                        }
                    }

                    foreach (NodeGroup nodeGroup in node.Groups)
                    {
                        foreach (TraitCostRecord cost in nodeGroup.Costs)
                        {
                            TraitCurrencyRecord traitCurrency = CliDB.TraitCurrencyStorage.LookupByKey(cost.TraitCurrencyID);
                            if (traitCurrency != null)
                                subTree.Currencies.Add(traitCurrency);
                        }
                    }

                    foreach (TraitCostRecord cost in node.Costs)
                    {
                        TraitCurrencyRecord traitCurrency = CliDB.TraitCurrencyStorage.LookupByKey(cost.TraitCurrencyID);
                        if (traitCurrency != null)
                            subTree.Currencies.Add(traitCurrency);
                    }

                    Tree tree1 = _traitTrees.LookupByKey(traitNode.TraitTreeID);
                    if (tree1 != null)
                    {
                        foreach (TraitCostRecord cost in tree1.Costs)
                        {
                            TraitCurrencyRecord traitCurrency = CliDB.TraitCurrencyStorage.LookupByKey(cost.TraitCurrencyID);
                            if (traitCurrency != null)
                                subTree.Currencies.Add(traitCurrency);
                        }
                    }
                }

                _traitNodes[(int)traitNode.Id] = node;
            }

            foreach (TraitEdgeRecord traitEdgeEntry in CliDB.TraitEdgeStorage.Values)
            {
                Node left = _traitNodes.LookupByKey(traitEdgeEntry.LeftTraitNodeID);
                Node right = _traitNodes.LookupByKey(traitEdgeEntry.RightTraitNodeID);
                if (left == null || right == null)
                    continue;

                right.ParentNodes.Add(Tuple.Create(left, (TraitEdgeType)traitEdgeEntry.Type));
            }

            foreach (SkillLineXTraitTreeRecord skillLineXTraitTreeEntry in CliDB.SkillLineXTraitTreeStorage.Values)
            {
                Tree tree = _traitTrees.LookupByKey(skillLineXTraitTreeEntry.TraitTreeID);
                if (tree == null)
                    continue;

                SkillLineRecord skillLineEntry = CliDB.SkillLineStorage.LookupByKey(skillLineXTraitTreeEntry.SkillLineID);
                if (skillLineEntry == null)
                    continue;

                _traitTreesBySkillLine.Add(skillLineXTraitTreeEntry.SkillLineID, tree);
                if (skillLineEntry.CategoryID == SkillCategory.Class)
                {
                    foreach (SkillRaceClassInfoRecord skillRaceClassInfo in Global.DB2Mgr.GetSkillRaceClassInfo(skillLineEntry.Id))
                        for (int i = 1; i < (int)Class.Max; ++i)
                            if ((skillRaceClassInfo.ClassMask & (1 << (i - 1))) != 0)
                                _skillLinesByClass[i] = skillLineXTraitTreeEntry.SkillLineID;

                    tree.ConfigType = TraitConfigType.Combat;
                }
                else
                    tree.ConfigType = TraitConfigType.Profession;
            }

            foreach (var (traitSystemId, traitTreeId) in traitTreesIdsByTraitSystem)
            {
                Tree tree = _traitTrees.LookupByKey(traitTreeId);
                if (tree != null)
                    _traitTreesByTraitSystem.Add(traitSystemId, tree);
            }

            foreach (TraitCurrencySourceRecord traitCurrencySource in CliDB.TraitCurrencySourceStorage.Values)
                _traitCurrencySourcesByCurrency.Add(traitCurrencySource.TraitCurrencyID, traitCurrencySource);

            foreach (TraitDefinitionEffectPointsRecord traitDefinitionEffectPoints in CliDB.TraitDefinitionEffectPointsStorage.Values)
                _traitDefinitionEffectPointModifiers.Add(traitDefinitionEffectPoints.TraitDefinitionID, traitDefinitionEffectPoints);

            MultiMap<uint, TraitTreeLoadoutEntryRecord> traitTreeLoadoutEntries = new();
            foreach (TraitTreeLoadoutEntryRecord traitTreeLoadoutEntry in CliDB.TraitTreeLoadoutEntryStorage.Values)
                traitTreeLoadoutEntries[traitTreeLoadoutEntry.TraitTreeLoadoutID].Add(traitTreeLoadoutEntry);

            foreach (TraitTreeLoadoutRecord traitTreeLoadout in CliDB.TraitTreeLoadoutStorage.Values)
            {
                var entries = traitTreeLoadoutEntries.LookupByKey(traitTreeLoadout.Id);
                if (entries != null)
                {
                    entries.Sort((left, right) => { return left.OrderIndex.CompareTo(right.OrderIndex); });
                    // there should be only one loadout per spec, we take last one encountered
                    _traitTreeLoadoutsByChrSpecialization[traitTreeLoadout.ChrSpecializationID] = entries;
                }
            }
        }

        /**
         * Generates new TraitConfig identifier.
         * Because this only needs to be unique for each character we let it overflow
*/
        public static int GenerateNewTraitConfigId()
        {
            if (_configIdGenerator == int.MaxValue)
                _configIdGenerator = 0;

            return ++_configIdGenerator;
        }

        public static TraitConfigType GetConfigTypeForTree(int traitTreeId)
        {
            Tree tree = _traitTrees.LookupByKey(traitTreeId);
            if (tree == null)
                return TraitConfigType.Invalid;

            return tree.ConfigType;
        }

        /**
         * @brief Finds relevant TraitTree identifiers
         * @param traitConfig config data
         * @return Trait tree data
*/
        public static List<Tree> GetTreesForConfig(TraitConfigPacket traitConfig)
        {
            switch (traitConfig.Type)
            {
                case TraitConfigType.Combat:
                    ChrSpecializationRecord chrSpecializationEntry = CliDB.ChrSpecializationStorage.LookupByKey(traitConfig.ChrSpecializationID);
                    if (chrSpecializationEntry != null)
                        return _traitTreesBySkillLine.LookupByKey(_skillLinesByClass[chrSpecializationEntry.ClassID]);
                    break;
                case TraitConfigType.Profession:
                    return _traitTreesBySkillLine.LookupByKey(traitConfig.SkillLineID);
                case TraitConfigType.Generic:
                    return _traitTreesByTraitSystem.LookupByKey(traitConfig.TraitSystemID);
                default:
                    break;
            }

            return null;
        }

        public static bool HasEnoughCurrency(TraitEntryPacket entry, Dictionary<uint, int> currencies)
        {
            int getCurrencyCount(int currencyId)
            {
                return currencies.LookupByKey(currencyId);
            }

            Node node = _traitNodes.LookupByKey(entry.TraitNodeID);
            foreach (NodeGroup group in node.Groups)
                foreach (TraitCostRecord cost in group.Costs)
                    if (getCurrencyCount(cost.TraitCurrencyID) < cost.Amount * entry.Rank)
                        return false;

            var nodeEntryItr = node.Entries.Find(nodeEntry => nodeEntry.Data.Id == entry.TraitNodeEntryID);
            if (nodeEntryItr != null)
                foreach (TraitCostRecord cost in nodeEntryItr.Costs)
                    if (getCurrencyCount(cost.TraitCurrencyID) < cost.Amount * entry.Rank)
                        return false;

            foreach (TraitCostRecord cost in node.Costs)
                if (getCurrencyCount(cost.TraitCurrencyID) < cost.Amount * entry.Rank)
                    return false;

            Tree tree = _traitTrees.LookupByKey(node.Data.TraitTreeID);
            if (tree != null)
                foreach (TraitCostRecord cost in tree.Costs)
                    if (getCurrencyCount(cost.TraitCurrencyID) < cost.Amount * entry.Rank)
                        return false;

            return true;
        }

        public static void TakeCurrencyCost(TraitEntryPacket entry, Dictionary<uint, int> currencies)
        {
            Node node = _traitNodes.LookupByKey(entry.TraitNodeID);
            foreach (NodeGroup group in node.Groups)
                foreach (TraitCostRecord cost in group.Costs)
                    currencies[(uint)cost.TraitCurrencyID] -= cost.Amount * entry.Rank;

            var nodeEntryItr = node.Entries.Find(nodeEntry => nodeEntry.Data.Id == entry.TraitNodeEntryID);
            if (nodeEntryItr != null)
                foreach (TraitCostRecord cost in nodeEntryItr.Costs)
                    currencies[(uint)cost.TraitCurrencyID] -= cost.Amount * entry.Rank;

            foreach (TraitCostRecord cost in node.Costs)
                currencies[(uint)cost.TraitCurrencyID] -= cost.Amount * entry.Rank;

            Tree tree = _traitTrees.LookupByKey(node.Data.TraitTreeID);
            if (tree != null)
                foreach (TraitCostRecord cost in tree.Costs)
                    currencies[(uint)cost.TraitCurrencyID] -= cost.Amount * entry.Rank;
        }

        public static void FillOwnedCurrenciesMap(TraitConfigPacket traitConfig, Player player, Dictionary<uint, int> currencies)
        {
            List<Tree> trees = GetTreesForConfig(traitConfig);
            if (trees == null)
                return;

            bool hasTraitNodeEntry(int traitNodeEntryId)
            {
                return traitConfig.Entries.Any(traitEntry => traitEntry.TraitNodeEntryID == traitNodeEntryId && (traitEntry.Rank > 0 || traitEntry.GrantedRanks > 0));
            }

            foreach (Tree tree in trees)
            {
                foreach (TraitCurrencyRecord currency in tree.Currencies)
                {
                    switch (currency.GetCurrencyType())
                    {
                        case TraitCurrencyType.Gold:
                        {
                            if (!currencies.ContainsKey((int)currency.Id))
                                currencies[currency.Id] = 0;

                            int amount = currencies[currency.Id];
                            if (player.GetMoney() > (ulong)(int.MaxValue - amount))
                                amount = int.MaxValue;
                            else
                                amount += (int)player.GetMoney();
                            break;
                        }
                        case TraitCurrencyType.CurrencyTypesBased:
                            if (!currencies.ContainsKey((int)currency.Id))
                                currencies[currency.Id] = 0;

                            currencies[currency.Id] += (int)player.GetCurrencyQuantity((uint)currency.CurrencyTypesID);
                            break;
                        case TraitCurrencyType.TraitSourced:
                            var currencySources = _traitCurrencySourcesByCurrency.LookupByKey(currency.Id);
                            if (currencySources != null)
                            {
                                foreach (TraitCurrencySourceRecord currencySource in currencySources)
                                {
                                    if (currencySource.QuestID != 0 && !player.IsQuestRewarded(currencySource.QuestID))
                                        continue;

                                    if (currencySource.AchievementID != 0 && !player.HasAchieved(currencySource.AchievementID))
                                        continue;

                                    if (currencySource.PlayerLevel != 0 && player.GetLevel() < currencySource.PlayerLevel)
                                        continue;

                                    if (currencySource.TraitNodeEntryID != 0 && !hasTraitNodeEntry(currencySource.TraitNodeEntryID))
                                        continue;

                                    if (!currencies.ContainsKey(currencySource.TraitCurrencyID))
                                        currencies[currencySource.TraitCurrencyID] = 0;

                                    currencies[currencySource.TraitCurrencyID] += currencySource.Amount;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public static void AddSpentCurrenciesForEntry(TraitEntryPacket entry, Dictionary<int, int> cachedCurrencies, int multiplier)
        {
            Node node = _traitNodes.LookupByKey(entry.TraitNodeID);
            foreach (NodeGroup group in node.Groups)
            {
                foreach (TraitCostRecord cost in group.Costs)
                {
                    if (!cachedCurrencies.ContainsKey(cost.TraitCurrencyID))
                        cachedCurrencies[cost.TraitCurrencyID] = 0;

                    cachedCurrencies[cost.TraitCurrencyID] += cost.Amount * entry.Rank * multiplier;
                }
            }

            var nodeEntryItr = node.Entries.Find(nodeEntry => nodeEntry.Data.Id == entry.TraitNodeEntryID);
            if (nodeEntryItr != null)
            {
                foreach (TraitCostRecord cost in nodeEntryItr.Costs)
                {
                    if (!cachedCurrencies.ContainsKey(cost.TraitCurrencyID))
                        cachedCurrencies[cost.TraitCurrencyID] = 0;

                    cachedCurrencies[cost.TraitCurrencyID] += cost.Amount * entry.Rank * multiplier;
                }
            }

            foreach (TraitCostRecord cost in node.Costs)
            {
                if (!cachedCurrencies.ContainsKey(cost.TraitCurrencyID))
                    cachedCurrencies[cost.TraitCurrencyID] = 0;

                cachedCurrencies[cost.TraitCurrencyID] += cost.Amount * entry.Rank * multiplier;
            }

            Tree tree = _traitTrees.LookupByKey(node.Data.TraitTreeID);
            if (tree != null)
            {
                foreach (TraitCostRecord cost in tree.Costs)
                {
                    if (!cachedCurrencies.ContainsKey(cost.TraitCurrencyID))
                        cachedCurrencies[cost.TraitCurrencyID] = 0;

                    cachedCurrencies[cost.TraitCurrencyID] += cost.Amount * entry.Rank * multiplier;
                }
            }
        }

        public static void FillSpentCurrenciesMap(List<TraitEntryPacket> traitEntries, Dictionary<int, int> cachedCurrencies)
        {
            foreach (TraitEntryPacket entry in traitEntries)
                AddSpentCurrenciesForEntry(entry, cachedCurrencies, 1);
        }

        public static int[] GetClassAndSpecTreeCurrencies(TraitConfigPacket traitConfig)
        {
            int[] currencies = new int[2];

            List<Tree> trees = GetTreesForConfig(traitConfig);
            if (trees != null)
            {
                int destIndex = 0;
                foreach (var tree in trees)
                {
                    if (destIndex >= currencies.Length)
                        break;

                    foreach (var currency in tree.Currencies)
                    {
                        if (destIndex >= currencies.Length)
                            break;

                        currencies[destIndex++] = (int)currency.Id;
                    }
                }
            }

            return currencies;
        }

        public static List<TraitCurrencyRecord> GetSubTreeCurrency(int traitSubTreeId)
        {
            SubTree subTree = _traitSubTrees.LookupByKey(traitSubTreeId);
            if (subTree == null)
                return null;

            return subTree.Currencies;
        }

        public static bool MeetsTraitCondition(TraitConfigPacket traitConfig, Player player, TraitCondRecord condition, ref Dictionary<int, int> cachedCurrencies)
        {
            if (condition.QuestID != 0 && !player.IsQuestRewarded(condition.QuestID))
                return false;

            if (condition.AchievementID != 0 && !player.HasAchieved(condition.AchievementID))
                return false;

            if (condition.SpecSetID != 0)
            {
                uint chrSpecializationId = (uint)player.GetPrimarySpecialization();
                if (traitConfig.Type == TraitConfigType.Combat)
                    chrSpecializationId = (uint)traitConfig.ChrSpecializationID;

                if (!Global.DB2Mgr.IsSpecSetMember(condition.SpecSetID, chrSpecializationId))
                    return false;
            }

            if (condition.TraitCurrencyID != 0 && condition.SpentAmountRequired != 0)
            {
                if (cachedCurrencies == null)
                {
                    cachedCurrencies = new();
                    FillSpentCurrenciesMap(traitConfig.Entries, cachedCurrencies);
                }

                if (condition.TraitNodeGroupID != 0 || condition.TraitNodeID != 0 || condition.TraitNodeEntryID != 0)
                {
                    cachedCurrencies.TryAdd(condition.TraitCurrencyID, 0);
                    if (cachedCurrencies[condition.TraitCurrencyID] < condition.SpentAmountRequired)
                        return false;
                }
            }

            if (condition.RequiredLevel != 0 && player.GetLevel() < condition.RequiredLevel)
                return false;

            return true;
        }

        public static bool NodeMeetsTraitConditions(TraitConfigPacket traitConfig, Node node, uint traitNodeEntryId, Player player, Dictionary<int, int> spentCurrencies)
        {
            var meetsConditions = (bool isSufficient, bool hasFailedConditions) (List<TraitCondRecord> conditions) =>
            {
                bool isSufficient = false;
                bool hasFailedConditions = false;

                foreach (var condition in conditions)
                {
                    if (condition.GetCondType() == TraitConditionType.Available || condition.GetCondType() == TraitConditionType.Visible)
                    {
                        if (MeetsTraitCondition(traitConfig, player, condition, ref spentCurrencies))
                        {
                            if (condition.HasFlag(TraitCondFlags.IsSufficient))
                            {
                                isSufficient = true;
                                break;
                            }
                            continue;
                        }

                        hasFailedConditions = true;
                    }
                }

                return (isSufficient, hasFailedConditions);
            };

            bool IsSufficient;
            bool HasFailedConditions;

            bool hasFailedConditions = false;
            foreach (NodeEntry entry in node.Entries)
            {
                if (entry.Data.Id == traitNodeEntryId)
                {
                    (IsSufficient, HasFailedConditions) = meetsConditions(entry.Conditions);
                    if (IsSufficient)
                        return true;
                    if (HasFailedConditions)
                        hasFailedConditions = true;
                }
            }

            (IsSufficient, HasFailedConditions) = meetsConditions(node.Conditions);
            if (IsSufficient)
                return true;
            else if (HasFailedConditions)
                hasFailedConditions = true;

            foreach (NodeGroup group in node.Groups)
            {
                (IsSufficient, HasFailedConditions) = meetsConditions(group.Conditions);
                if (IsSufficient)
                    return true;
                if (HasFailedConditions)
                    hasFailedConditions = true;
            }

            return !hasFailedConditions;
        }

        public static List<TraitEntry> GetGrantedTraitEntriesForConfig(TraitConfigPacket traitConfig, Player player)
        {
            List<TraitEntry> entries = new();
            var trees = GetTreesForConfig(traitConfig);
            if (trees == null)
                return entries;

            void addGrantedRankToEntry(uint nodeId, NodeEntry entry, int grantedRanks)
            {
                TraitEntry foundTraitEntry = entries.Find(traitEntry => traitEntry.TraitNodeID == nodeId && traitEntry.TraitNodeEntryID == entry.Data.Id);
                if (foundTraitEntry == null)
                {
                    foundTraitEntry = new();
                    foundTraitEntry.TraitNodeID = (int)nodeId;
                    foundTraitEntry.TraitNodeEntryID = (int)entry.Data.Id;
                    foundTraitEntry.Rank = 0;
                    foundTraitEntry.GrantedRanks = 0;
                    entries.Add(foundTraitEntry);
                }
                foundTraitEntry.GrantedRanks += grantedRanks;
                if (foundTraitEntry.GrantedRanks > entry.Data.MaxRanks)
                    foundTraitEntry.GrantedRanks = entry.Data.MaxRanks;
            }

            Dictionary<int, int> cachedCurrencies = null;

            foreach (Tree tree in trees)
            {
                foreach (Node node in tree.Nodes)
                {
                    foreach (NodeEntry entry in node.Entries)
                        foreach (TraitCondRecord condition in entry.Conditions)
                            if (condition.GetCondType() == TraitConditionType.Granted && MeetsTraitCondition(traitConfig, player, condition, ref cachedCurrencies))
                                addGrantedRankToEntry(node.Data.Id, entry, condition.GrantedRanks);

                    foreach (TraitCondRecord condition in node.Conditions)
                        if (condition.GetCondType() == TraitConditionType.Granted && MeetsTraitCondition(traitConfig, player, condition, ref cachedCurrencies))
                            foreach (NodeEntry entry in node.Entries)
                                addGrantedRankToEntry(node.Data.Id, entry, condition.GrantedRanks);

                    foreach (NodeGroup group in node.Groups)
                        foreach (TraitCondRecord condition in group.Conditions)
                            if (condition.GetCondType() == TraitConditionType.Granted && MeetsTraitCondition(traitConfig, player, condition, ref cachedCurrencies))
                                foreach (NodeEntry entry in node.Entries)
                                    addGrantedRankToEntry(node.Data.Id, entry, condition.GrantedRanks);
                }
            }

            return entries;
        }

        public static bool IsValidEntry(TraitEntryPacket traitEntry)
        {
            Node node = _traitNodes.LookupByKey(traitEntry.TraitNodeID);
            if (node == null)
                return false;

            var entryItr = node.Entries.Find(entry => entry.Data.Id == traitEntry.TraitNodeEntryID);
            if (entryItr == null)
                return false;

            if (entryItr.Data.MaxRanks < traitEntry.Rank + traitEntry.GrantedRanks)
                return false;

            return true;
        }

        public static TalentLearnResult ValidateConfig(TraitConfigPacket traitConfig, Player player, bool requireSpendingAllCurrencies = false, bool removeInvalidEntries = false)
        {
            int getNodeEntryCount(int traitNodeId)
            {
                return traitConfig.Entries.Count(traitEntry => traitEntry.TraitNodeID == traitNodeId);
            }

            TraitEntryPacket getNodeEntry(uint traitNodeId, uint traitNodeEntryId)
            {
                return traitConfig.Entries.Find(traitEntry => traitEntry.TraitNodeID == traitNodeId && traitEntry.TraitNodeEntryID == traitNodeEntryId);
            }

            bool isNodeFullyFilled(Node node)
            {
                if (node.Data.GetNodeType() == TraitNodeType.Selection)
                    return node.Entries.Any(nodeEntry =>
                    {
                        TraitEntryPacket traitEntry = getNodeEntry(node.Data.Id, nodeEntry.Data.Id);
                        return traitEntry != null && (traitEntry.Rank + traitEntry.GrantedRanks) == nodeEntry.Data.MaxRanks;
                    });

                return node.Entries.All(nodeEntry =>
                {
                    TraitEntryPacket traitEntry = getNodeEntry(node.Data.Id, nodeEntry.Data.Id);
                    return traitEntry != null && (traitEntry.Rank + traitEntry.GrantedRanks) == nodeEntry.Data.MaxRanks;
                });
            };

            Dictionary<int, int> spentCurrencies = new();
            FillSpentCurrenciesMap(traitConfig.Entries, spentCurrencies);

            var isValidTraitEntry = TalentLearnResult (TraitEntryPacket traitEntry) =>
            {
                if (!IsValidEntry(traitEntry))
                    return TalentLearnResult.FailedUnknown;

                Node node = _traitNodes.LookupByKey(traitEntry.TraitNodeID);
                if (node.Data.GetNodeType() == TraitNodeType.Selection || node.Data.GetNodeType() == TraitNodeType.SubTreeSelection)
                    if (getNodeEntryCount(traitEntry.TraitNodeID) != 1)
                        return TalentLearnResult.FailedUnknown;

                if (!NodeMeetsTraitConditions(traitConfig, node, (uint)traitEntry.TraitNodeEntryID, player, spentCurrencies))
                    return TalentLearnResult.FailedUnknown;

                if (!node.ParentNodes.Empty())
                {
                    bool hasAnyParentTrait = false;
                    foreach (var (parentNode, edgeType) in node.ParentNodes)
                    {
                        if (!isNodeFullyFilled(parentNode))
                        {
                            if (edgeType == TraitEdgeType.RequiredForAvailability)
                                return TalentLearnResult.FailedNotEnoughTalentsInPrimaryTree;

                            continue;
                        }

                        hasAnyParentTrait = true;
                    }

                    if (!hasAnyParentTrait)
                        return TalentLearnResult.FailedNotEnoughTalentsInPrimaryTree;
                }

                return TalentLearnResult.LearnOk;
            };

            for (var i = 0; i != traitConfig.Entries.Count;)
            {
                TalentLearnResult result = isValidTraitEntry(traitConfig.Entries[i]);
                if (result != TalentLearnResult.LearnOk)
                {
                    if (!removeInvalidEntries)
                        return result;

                    AddSpentCurrenciesForEntry(traitConfig.Entries[i], spentCurrencies, -1);

                    if (traitConfig.Entries[i].GrantedRanks == 0  // fully remove entries that don't have granted ranks
                        || traitConfig.Entries[i].Rank == 0)      // ... or entries that do have them and don't have any additional spent ranks (can happen if the same entry is revalidated after first removing all spent ranks)
                        traitConfig.Entries.RemoveAt(i);
                    else
                        traitConfig.Entries[i].Rank = 0;

                    // revalidate entire config - a removed entry will invalidate all other entries that depend on it
                    i = 0;
                }
                else
                    ++i;
            }

            Dictionary<int, SubtreeValidationData> subtrees = new();

            foreach (TraitEntryPacket traitEntry in traitConfig.Entries)
            {
                Node node = _traitNodes.LookupByKey(traitEntry.TraitNodeID);
                var entryItr = node.Entries.Find(nodeEntry => nodeEntry.Data.Id == traitEntry.TraitNodeEntryID);
                Cypher.Assert(entryItr != null);

                if (!subtrees.ContainsKey(entryItr.Data.TraitSubTreeID))
                    subtrees[entryItr.Data.TraitSubTreeID] = new();

                if (node.Data.GetNodeType() == TraitNodeType.SubTreeSelection)
                    subtrees[entryItr.Data.TraitSubTreeID].IsSelected = true;

                if (node.Data.TraitSubTreeID != 0)
                    subtrees[node.Data.TraitSubTreeID].Entries.Add(traitEntry);
            }

            foreach (TraitSubTreeCachePacket subTree in traitConfig.SubTrees)
                subTree.Active = false;

            foreach (var (selectedSubTreeId, data) in subtrees)
            {
                var subtreeDataItr = traitConfig.SubTrees.Find(p => p.TraitSubTreeID == selectedSubTreeId);
                if (subtreeDataItr == null)
                {
                    subtreeDataItr = new TraitSubTreeCachePacket();
                    subtreeDataItr.TraitSubTreeID = selectedSubTreeId;
                    traitConfig.SubTrees.Add(subtreeDataItr);
                }

                subtreeDataItr.Entries = data.Entries;
                subtreeDataItr.Active = data.IsSelected;
            }

            Dictionary<uint, int> grantedCurrencies = new();
            FillOwnedCurrenciesMap(traitConfig, player, grantedCurrencies);

            foreach (var (traitCurrencyId, spentAmount) in spentCurrencies)
            {
                if (CliDB.TraitCurrencyStorage.LookupByKey(traitCurrencyId).GetCurrencyType() != TraitCurrencyType.TraitSourced)
                    continue;

                if (spentAmount == 0)
                    continue;

                int grantedCount = grantedCurrencies.LookupByKey(traitCurrencyId);
                if (grantedCount == 0 || grantedCount < spentAmount)
                    return TalentLearnResult.FailedNotEnoughTalentsInPrimaryTree;

            }

            if (requireSpendingAllCurrencies && traitConfig.Type == TraitConfigType.Combat)
            {
                // client checks only first two currencies for trait tree
                foreach (int traitCurrencyId in GetClassAndSpecTreeCurrencies(traitConfig))
                {
                    int grantedAmount = grantedCurrencies.LookupByKey(traitCurrencyId);
                    if (grantedAmount == 0)
                        continue;

                    int spentAmount = spentCurrencies.LookupByKey(traitCurrencyId);
                    if (spentAmount == 0 || spentAmount != grantedAmount)
                        return TalentLearnResult.UnspentTalentPoints;
                }

                foreach (var (selectedTraitSubTreeId, data) in subtrees)
                {
                    if (!data.IsSelected)
                        continue;

                    foreach (TraitCurrencyRecord subTreeCurrency in GetSubTreeCurrency(selectedTraitSubTreeId))
                    {
                        int grantedAmount = grantedCurrencies.LookupByKey(subTreeCurrency.Id);
                        if (grantedAmount == 0)
                            continue;

                        int spentAmount = spentCurrencies.LookupByKey(subTreeCurrency.Id);
                        if (spentAmount == 0 || spentAmount != grantedAmount)
                            return TalentLearnResult.UnspentTalentPoints;
                    }
                }
            }

            return TalentLearnResult.LearnOk;
        }

        public static bool CanApplyTraitNode(TraitConfig traitConfig, TraitEntry traitEntry)
        {
            Node node = _traitNodes.LookupByKey(traitEntry.TraitNodeID);
            if (node == null)
                return false;

            if (node.Data.TraitSubTreeID != 0)
            {
                var subTreeItr = traitConfig.SubTrees._values.Find(p => p.TraitSubTreeID == node.Data.TraitSubTreeID);
                if (subTreeItr == null || subTreeItr.Active == 0)
                    return false;
            }

            return true;
        }

        public static List<TraitDefinitionEffectPointsRecord> GetTraitDefinitionEffectPointModifiers(int traitDefinitionId)
        {
            return _traitDefinitionEffectPointModifiers.LookupByKey(traitDefinitionId);
        }

        public static void InitializeStarterBuildTraitConfig(TraitConfigPacket traitConfig, Player player)
        {
            traitConfig.Entries.Clear();
            var trees = GetTreesForConfig(traitConfig);
            if (trees == null)
                return;

            foreach (TraitEntry grant in GetGrantedTraitEntriesForConfig(traitConfig, player))
            {
                TraitEntryPacket newEntry = new();
                newEntry.TraitNodeID = grant.TraitNodeID;
                newEntry.TraitNodeEntryID = grant.TraitNodeEntryID;
                newEntry.GrantedRanks = grant.GrantedRanks;
                traitConfig.Entries.Add(newEntry);
            }

            Dictionary<uint, int> currencies = new();
            FillOwnedCurrenciesMap(traitConfig, player, currencies);

            var loadoutEntries = _traitTreeLoadoutsByChrSpecialization.LookupByKey(traitConfig.ChrSpecializationID);
            if (loadoutEntries != null)
            {
                TraitEntryPacket findEntry(TraitConfigPacket config, int traitNodeId, int traitNodeEntryId)
                {
                    return config.Entries.Find(traitEntry => traitEntry.TraitNodeID == traitNodeId && traitEntry.TraitNodeEntryID == traitNodeEntryId);
                }

                foreach (TraitTreeLoadoutEntryRecord loadoutEntry in loadoutEntries)
                {
                    int addedRanks = loadoutEntry.NumPoints;
                    Node node = _traitNodes.LookupByKey(loadoutEntry.SelectedTraitNodeID);

                    TraitEntryPacket newEntry = new();
                    newEntry.TraitNodeID = loadoutEntry.SelectedTraitNodeID;
                    newEntry.TraitNodeEntryID = loadoutEntry.SelectedTraitNodeEntryID;
                    if (newEntry.TraitNodeEntryID == 0)
                        newEntry.TraitNodeEntryID = (int)node.Entries[0].Data.Id;

                    TraitEntryPacket entryInConfig = findEntry(traitConfig, newEntry.TraitNodeID, newEntry.TraitNodeEntryID);

                    if (entryInConfig != null)
                        addedRanks -= entryInConfig.Rank;

                    newEntry.Rank = addedRanks;

                    if (!HasEnoughCurrency(newEntry, currencies))
                        continue;

                    if (entryInConfig != null)
                        entryInConfig.Rank += addedRanks;
                    else
                        traitConfig.Entries.Add(newEntry);

                    TakeCurrencyCost(newEntry, currencies);
                }
            }
        }
    }

    class NodeEntry
    {
        public TraitNodeEntryRecord Data;
        public List<TraitCondRecord> Conditions = new();
        public List<TraitCostRecord> Costs = new();
    }

    class Node
    {
        public TraitNodeRecord Data;
        public List<NodeEntry> Entries = new();
        public List<NodeGroup> Groups = new();
        public List<Tuple<Node, TraitEdgeType>> ParentNodes = new(); // TraitEdge::LeftTraitNodeID
        public List<TraitCondRecord> Conditions = new();
        public List<TraitCostRecord> Costs = new();
    }

    class NodeGroup
    {
        public TraitNodeGroupRecord Data;
        public List<TraitCondRecord> Conditions = new();
        public List<TraitCostRecord> Costs = new();
        public List<Node> Nodes = new();
    }

    class SubTree
    {
        public TraitSubTreeRecord Data;
        public List<Node> Nodes = new();
        public List<TraitCurrencyRecord> Currencies = new();
    }

    class Tree
    {
        public TraitTreeRecord Data;
        public List<Node> Nodes = new();
        public List<TraitCostRecord> Costs = new();
        public List<TraitCurrencyRecord> Currencies = new();
        public List<SubTree> SubTrees = new();
        public TraitConfigType ConfigType;
    }

    class SubtreeValidationData
    {
        public List<TraitEntryPacket> Entries = new();
        public bool IsSelected;
    }
}
