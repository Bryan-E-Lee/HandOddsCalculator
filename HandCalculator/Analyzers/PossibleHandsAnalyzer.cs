using System.Diagnostics;

namespace HandCalculator.Analyzers
{
    internal class PossibleHandsAnalyzer : HandAnalyzer
    {
        public const decimal OffBy1RerollOdds = 1m / 6;
        public const decimal OffBy2RerollOdds = 1m / 18;
        public const decimal OffBy3RerollOdds = 1m / 36;

        public PossibleHandsAnalyzer(HandConfig handConfig) : base(handConfig)
        { }

        private static decimal RerollRatio = 1m / Hand.DieFaces.Count();

        protected override string GetAnalysisString(List<IGrouping<string, Hand>> handGroups, IEnumerable<HandCategory> excludedHands, IEnumerable<HandCategory> includedHands)
        {
            Console.WriteLine($"Analyzing Possible Hands\n===\n");
            if (handGroups.Count() == 0)
            {
                return "No data.";
            }

            var timer = new Stopwatch();
            timer.Start();

            var totalHands = handGroups.Sum(hg => hg.Count());
            ComputeNormalRollHands(handGroups, excludedHands, includedHands);
            ComputeRerolls(handGroups, excludedHands, includedHands, totalHands);

            if (excludedHands.Any() || includedHands.Any())
            {
                var percentage = ExcludeIncludePassHandOdds / (totalHands);
                Console.WriteLine($"{ExcludeIncludePassHandOdds} of {totalHands} hands ({percentage:P4}) after exclusions / inclusions\n");
            }
            timer.Stop();
            return $"{CreateMessage(totalHands, timer)}";
        }

        private static bool ExcludeIncludeHandFilter(Hand hand, IEnumerable<HandCategory> excludedHands, IEnumerable<HandCategory> includedHands)
            => !excludedHands.Any(eh => hand.HandCategories.Contains(eh))
            && (!includedHands.Any() || includedHands.Any(ih => hand.HandCategories.Contains(ih)));

        protected void ComputeNormalRollHands(List<IGrouping<string, Hand>> hands, IEnumerable<HandCategory> excludedHands, IEnumerable<HandCategory> includedHands)
        {
            foreach (var handGroup in hands)
            {
                var hand = handGroup.First();
                if (ComputedIsomorphicKeys.Contains(hand.IsomorphicKey))
                {
                    continue;
                }
                ComputedIsomorphicKeys.Add(hand.IsomorphicKey);

                var categories = GetHandCategories(hand);

                foreach (var category in categories)
                {
                    HandsByCategory[category].AddRange(handGroup.ToList());
                }

                if (ExcludeIncludeHandFilter(hand, excludedHands, includedHands))
                {
                    ExcludeIncludePassHandOdds += handGroup.Count();
                }
            }

            foreach (var category in HandsByCategory.Keys)
            {
                HandOddsByCategory[category] = HandsByCategory[category].Count;
            }
        }

        protected void ComputeRerolls(List<IGrouping<string, Hand>> hands, IEnumerable<HandCategory> excludedHands, IEnumerable<HandCategory> includedHands, int totalHands)
        {
            if (_config.Rerolls <= 0)
            {
                return;
            }

            foreach (var category in HandOddsByCategory.Keys)
            {
                ComputeRerollsForCategory(category, hands, excludedHands, includedHands, totalHands);
            }
        }

        private void ComputeRerollsForCategory(HandCategory category, List<IGrouping<string, Hand>> handGroups, IEnumerable<HandCategory> excludedHands, IEnumerable<HandCategory> includedHands, int totalHands)
        {
            if (category == HandCategory.HighRoll)
            {
                return;
            }

            var handsInCategoryGroups = HandsByCategory[category].GroupBy(h => h.IsomorphicKey).ToList();
            var handsNotInCategoryGroups = GetIsomorphicHandsNotInCategory(category, handGroups);

            foreach (var handNotInCategoryGroup in handsNotInCategoryGroups)
            {
                var handNotInCategory = handNotInCategoryGroup.First();
                var possibleHandGroups = handsInCategoryGroups.Where(hic => hic.First().RerollableKey == handNotInCategory.RerollableKey).ToList();

                foreach (var handInCategoryGroup in possibleHandGroups)
                {
                    var handInCategory = handInCategoryGroup.First();
                    var difference = handNotInCategory.GetRerollDifference(handInCategory);
                    if (difference > _config.Rerolls)
                    {
                        continue;
                    }

                    var odds = GetRerollOdds(difference);
                    if (!odds.HasValue)
                    {
                        continue;
                    }

                    HandOddsByCategory[category] += odds.Value;

                    if (!excludedHands.Contains(category) && !includedHands.Contains(category)) //Excluded hands are treated as "superior", one will not reroll them to obtain a lesser hand
                    {
                        AddExcludeIncludeHandCategoryOdds(new OddsAddRequest
                        {
                            Hand = handInCategory,
                            ExcludedHands = excludedHands,
                            IncludedHands = includedHands,
                            Odds = odds.Value,
                            IsomorphicHands = handsNotInCategoryGroups.Count,
                            TotalHands = totalHands
                        });
                    }
                }
            }
        }

        private decimal? GetRerollOdds(int difference)
        {
            switch (difference)
            {
                case 1:
                    return OffBy1RerollOdds;
                case 2:
                    return OffBy2RerollOdds;
                case 3:
                    return OffBy3RerollOdds;
                default:
                    return null;
            }
        }

        private class OddsAddRequest
        {
            public required Hand Hand { get; set; }
            public required IEnumerable<HandCategory> ExcludedHands { get; set; }
            public required IEnumerable<HandCategory> IncludedHands { get; set; }
            public decimal Odds { get; set; }
            public int IsomorphicHands { get; set; }
            public int TotalHands { get; set; }
        }

        private void AddExcludeIncludeHandCategoryOdds(OddsAddRequest request)
        {
            if (!request.ExcludedHands.Any() && !request.IncludedHands.Any())
            {
                return;
            }

            if (ExcludeIncludeHandFilter(request.Hand, request.ExcludedHands, request.IncludedHands))
            {
                ExcludeIncludePassHandOdds += request.Odds * request.IsomorphicHands / request.TotalHands;
            }
        }

        private List<IGrouping<string, Hand>> GetIsomorphicHandsNotInCategory(HandCategory category, IEnumerable<IGrouping<string, Hand>> isomorphicHandGroups)
        {
            switch (category.Name)
            {
                case HandCategory.PairName:
                    return isomorphicHandGroups.Where(hg => !hg.First().IsPair).ToList();

                case HandCategory.TwoPairName:
                    return isomorphicHandGroups.Where(hg => !hg.First().IsTwoPair).ToList();

                case HandCategory.TripleName:
                    return isomorphicHandGroups.Where(hg => !hg.First().IsTriple).ToList();

                case HandCategory.SmallStraightName:
                    return isomorphicHandGroups.Where(hg => !hg.First().IsSmallStraight).ToList();

                case HandCategory.FlushName:
                    return isomorphicHandGroups.Where(hg => !hg.First().IsFlush).ToList();

                case HandCategory.FullHouseName:
                    return isomorphicHandGroups.Where(hg => !hg.First().IsFullHouse).ToList();

                case HandCategory.BigStraightName:
                    return isomorphicHandGroups.Where(hg => !hg.First().IsBigStraight).ToList();

                case HandCategory.QuadName:
                    return isomorphicHandGroups.Where(hg => !hg.First().IsQuad).ToList();

                case HandCategory.JackpotName:
                    return isomorphicHandGroups.Where(hg => !hg.First().IsJackpot).ToList();

                default:
                    return isomorphicHandGroups.ToList();
            }
        }


        private List<HandCategory> GetHandCategories(Hand hand)
        {
            if (ComputedEffectives.ContainsKey(hand.EffectiveKey))
            {
                return ComputedEffectives[hand.EffectiveKey];
            }

            var categories = CheckHand(hand).ToList();
            ComputedEffectives.Add(hand.EffectiveKey, categories);

            if (!ComputedNormals.Contains(hand.NormalKey))
            {
                ComputedNormals.Add(hand.NormalKey);
            }
            return categories;
        }

        private List<HandCategory> CheckHand(Hand hand)
        {
            var handCategories = new List<HandCategory>();
            if (hand.IsJackpot)
            {
                handCategories.Add(HandCategory.Jackpot);
            }
            if (hand.IsQuad)
            {
                handCategories.Add(HandCategory.Quad);
            }
            if (hand.IsBigStraight)
            {
                handCategories.Add(HandCategory.BigStraight);
            }
            if (hand.IsFullHouse)
            {
                handCategories.Add(HandCategory.FullHouse);
            }
            if (hand.IsFlush)
            {
                handCategories.Add(HandCategory.Flush);
            }
            if (hand.IsSmallStraight)
            {
                handCategories.Add(HandCategory.SmallStraight);
            }
            if (hand.IsTriple)
            {
                handCategories.Add(HandCategory.Triple);
            }
            if (hand.IsTwoPair)
            {
                handCategories.Add(HandCategory.TwoPair);
            }
            if (hand.IsPair)
            {
                handCategories.Add(HandCategory.Pair);
            }
            handCategories.Add(HandCategory.HighRoll);
            return handCategories;
        }
    }
}
