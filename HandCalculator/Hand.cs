namespace HandCalculator
{
    internal class Hand
    {
        public const int DieFaceCount = 6;
        public static readonly IEnumerable<int> DieFaces = Enumerable.Range(1, DieFaceCount);

        public Hand(IEnumerable<int> rolls, HandConfig handConfig)
        {
            AllRolls = rolls.ToList();
            _config = handConfig;
        }

        public List<int> AllRolls { get; private set; }
        public List<int> CommunalRolls => AllRolls.Take(_config.CommunalRolls).ToList();
        public List<int> NormalRolls => AllRolls.Skip(_config.CommunalRolls).Take(_config.NormalRolls).ToList();
        public List<int> ExtraRolls => AllRolls.Skip(_config.CommunalRolls + _config.NormalRolls).Take(_config.ExtraRolls).ToList();
        public List<int> EffectiveRolls => CommunalRolls.Concat(NormalRolls).Concat(ExtraRolls).ToList();
        private readonly HandConfig _config;

        public override int GetHashCode()
        {
            var hash = String.Concat(EffectiveRolls);
            return Convert.ToInt32(hash);
        }

        public override bool Equals(object? obj)
        {
            if (obj is Hand otherHand)
            {
                return otherHand.GetHashCode() == GetHashCode();
            }
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return String.Concat(EffectiveRolls);
        }

        /// <summary>
        /// Used to recognize when two rolls have the same communal dice and, thus, could be rolled into one another.
        /// </summary>
        public string CommunalKey => String.Concat(CommunalRolls.OrderBy(r => r));

        /// <summary>
        /// Used to recognize when recomputing rerolls as rerolls affect normal rolls.
        /// </summary>
        public string NormalKey => String.Concat(NormalRolls.OrderBy(r => r));

        /// <summary>
        /// Uniquely identifies any extra dice rolled.
        /// </summary>
        public string ExtraKey => String.Concat(ExtraRolls.OrderBy(r => r));

        /// <summary>
        /// Uniquely identifies when two hands can be rerolled into one another.
        /// </summary>
        public string RerollableKey => CommunalKey + ExtraKey;

        /// <summary>
        /// Uniquely identifies when two hands are completely isomorphic to each other.
        /// </summary>
        public string IsomorphicKey => CommunalKey + NormalKey + ExtraKey;

        /// <summary>
        /// Used to recognize when recomputing the same roll with a different order.
        /// The roll still counts as a permutation but there is no need to recalculate.
        /// </summary>
        public string EffectiveKey => String.Concat(EffectiveRolls.OrderBy(r => r));

        /// <summary>
        /// Uniquely identifies a complete roll for the input config.
        /// Since all possible rolls including extras are generated initially, this prevents recomputation of those.
        /// </summary>
        public string CompleteUniqueRollKey => String.Concat(AllRolls.Take(_config.CommunalRolls + _config.NormalRolls + _config.ExtraRolls));

        /// <summary>
        /// Determines what counts as a unique roll from the end user's perspective. There are 7776 unique rolls.
        /// </summary>
        public string UniqueRollKey => String.Concat(CommunalRolls.Concat(NormalRolls));

        public int this[int index] => AllRolls.ElementAt(index);

        /// <summary>
        /// Retrieves a number of die face differences between two rolls. Useful for calculating rerolls,
        /// as a hand can be rerolled into another if the number of different faces between them is less
        /// than or equal to the number of rerolls available.
        /// </summary>
        /// <param name="otherNormalRolls"></param>
        /// <returns></returns>
        public int GetRerollDifference(IEnumerable<int> otherNormalRolls)
        {
            var matchedIndices = new List<(int, int)>();
            var otherNormalList = otherNormalRolls.ToList();
            for (var i = 0; i < NormalRolls.Count; i++)
            {
                var roll = NormalRolls[i];
                for (var j = 0; j < otherNormalList.Count; j++)
                {
                    if (matchedIndices.Contains((i, j)))
                    {
                        break;
                    }
                    var otherRoll = otherNormalList[j];
                    if (roll == otherRoll)
                    {
                        matchedIndices.Add((i, j));
                        break;
                    }
                }
            }
            return NormalRolls.Count - matchedIndices.Count;
        }

        public int GetRerollDifference(Hand otherHand) => GetRerollDifference(otherHand.NormalRolls);

        public int NumberOfDuplicates
        {
            get
            {
                var seen = new List<int>();
                var skips = new List<int>();
                var duplicates = 0;
                foreach (var roll in EffectiveRolls)
                {
                    if (seen.Contains(roll) && !skips.Contains(roll))
                    {
                        duplicates++;
                        skips.Add(roll);
                    }
                    else
                    {
                        seen.Add(roll);
                    }
                }
                return duplicates;
            }
        }

        public int MaxMultipleCount
        {
            get
            {
                var seen = new Dictionary<int, int>();
                foreach (var roll in EffectiveRolls)
                {
                    if (seen.ContainsKey(roll))
                    {
                        seen[roll]++;
                    }
                    else
                    {
                        seen.Add(roll, 1);
                    }
                }
                return seen.Values.Max();
            }
        }

        public int LongestStraight
        {
            get
            {
                var hits = new List<int> { 1 };
                var currentHitIndex = 0;
                var testHand = EffectiveRolls.Distinct().OrderBy(roll => roll).ToList();
                for (var index = 1; index < testHand.Count; index++)
                {
                    if (testHand[index] == testHand[index - 1] + 1)
                    {
                        hits[currentHitIndex]++;
                    }
                    else
                    {
                        currentHitIndex++;
                        hits.Add(1);
                    }
                }
                return hits.Max();
            }
        }

        public bool IsPair => NumberOfDuplicates >= 1;

        public bool IsTwoPair => NumberOfDuplicates >= 2;

        public bool IsTriple => MaxMultipleCount >= 3;

        public bool IsSmallStraight => LongestStraight >= 4;

        public bool IsFlush
            => EffectiveRolls.Count(roll => roll % 2 == 0) >= 5
            || EffectiveRolls.Count(roll => roll % 2 == 1) >= 5;

        public bool IsFullHouse
            => NumberOfDuplicates >= 2
            && MaxMultipleCount >= 3;

        public bool IsBigStraight => LongestStraight >= 5;

        public bool IsQuad => MaxMultipleCount >= 4;

        public bool IsJackpot => MaxMultipleCount >= 5;

        public bool IsPairExplicit
            => NumberOfDuplicates == 1
            && MaxMultipleCount == 2;

        public bool IsTwoPairExplicit
            => NumberOfDuplicates == 2
            && MaxMultipleCount == 2;

        public bool IsTripleExplicit
            => NumberOfDuplicates == 1
            && MaxMultipleCount == 3;

        public bool IsSmallStraightExplicit => LongestStraight == 4;

        public bool IsQuadExplicit
            => MaxMultipleCount == 4;

        public bool IsFullHouseExplicit
            => NumberOfDuplicates == 2
            && MaxMultipleCount == 3;

        public List<HandCategory> HandCategories
        {
            get
            {
                var handCategories = new List<HandCategory>();
                if (IsPair)
                {
                    handCategories.Add(HandCategory.Pair);
                }
                if (IsTwoPair)
                {
                    handCategories.Add(HandCategory.TwoPair);
                }
                if (IsTriple)
                {
                    handCategories.Add(HandCategory.Triple);
                }
                if (IsSmallStraight)
                {
                    handCategories.Add(HandCategory.SmallStraight);
                }
                if (IsFlush)
                {
                    handCategories.Add(HandCategory.Flush);
                }
                if (IsFullHouse)
                {
                    handCategories.Add(HandCategory.FullHouse);
                }
                if (IsBigStraight)
                {
                    handCategories.Add(HandCategory.BigStraight);
                }
                if (IsQuad)
                {
                    handCategories.Add(HandCategory.Quad);
                }
                if (IsJackpot)
                {
                    handCategories.Add(HandCategory.Jackpot);
                }

                if (!handCategories.Any())
                {
                    handCategories.Add(HandCategory.HighRoll);
                }
                return handCategories;
            }
        }
    }
}
