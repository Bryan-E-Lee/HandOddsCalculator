using System.Diagnostics;

namespace HandCalculator.Analyzers
{
    internal abstract class HandAnalyzer
    {
        public HandAnalyzer(HandConfig handConfig)
        {
            _config = handConfig;
        }

        protected readonly HashSet<string> ComputedNormals = new();
        protected readonly HashSet<string> ComputedIsomorphicKeys = new();
        protected readonly Dictionary<string, List<HandCategory>> ComputedEffectives = new();
        protected readonly Dictionary<HandCategory, decimal> RerollProbabilities = new();
        protected decimal ExcludeIncludePassHandOdds = 0m;
        public int NumberOfCommunalRolls { get; set; } = 2;

        protected static Dictionary<HandCategory, decimal> CreateFreshHandCategories() => new()
        {
            { HandCategory.HighRoll, 0m },
            { HandCategory.Pair, 0m },
            { HandCategory.TwoPair, 0m },
            { HandCategory.SmallStraight, 0m },
            { HandCategory.Triple, 0m },
            { HandCategory.Flush, 0m },
            { HandCategory.FullHouse, 0m },
            { HandCategory.BigStraight, 0m },
            { HandCategory.Quad, 0m },
            { HandCategory.Jackpot, 0m }
        };

        protected static Dictionary<HandCategory, List<Hand>> CreateFreshHandsByCategory() => new()
        {
            { HandCategory.HighRoll, new List<Hand>() },
            { HandCategory.Pair, new List<Hand>() },
            { HandCategory.TwoPair, new List<Hand>() },
            { HandCategory.SmallStraight, new List<Hand>() },
            { HandCategory.Triple, new List<Hand>() },
            { HandCategory.Flush, new List<Hand>() },
            { HandCategory.FullHouse, new List<Hand>() },
            { HandCategory.BigStraight, new List<Hand>() },
            { HandCategory.Quad, new List<Hand>() },
            { HandCategory.Jackpot, new List<Hand>() }
        };

        protected readonly HandConfig _config;
        protected Dictionary<HandCategory, decimal> HandOddsByCategory { get; private set; } = CreateFreshHandCategories();
        protected Dictionary<HandCategory, List<Hand>> HandsByCategory { get; private set; } = CreateFreshHandsByCategory();


        protected virtual string CreateMessage(decimal totalHands, Stopwatch? stoppedTimer = null)
        {
            var messages = HandOddsByCategory.Keys
                .OrderBy(category => category)
                .Select(category =>
                {
                    var handsInCategory = HandOddsByCategory[category];
                    RerollProbabilities.TryGetValue(category, out decimal rerollsInCategory);
                    var percent = (handsInCategory + rerollsInCategory) / totalHands;
                    return $"{category} - {decimal.Round(handsInCategory,4)} / {totalHands} ({percent:P4})";
                })
                .ToList();
            if (stoppedTimer != null)
            {
                var time = new TimeSpan(stoppedTimer.ElapsedTicks);
                messages.Add($"\nThat took me about {time}.");
            }
            return String.Join('\n', messages);
        }

        protected abstract string GetAnalysisString(List<IGrouping<string, Hand>> hands, IEnumerable<HandCategory> excludedHands, IEnumerable<HandCategory> includedHands);

        public virtual void Reset()
        {
            ExcludeIncludePassHandOdds = 0;

            ComputedNormals.Clear();
            ComputedIsomorphicKeys.Clear();
            ComputedEffectives.Clear();
            RerollProbabilities.Clear();
            HandOddsByCategory = CreateFreshHandCategories();
            HandsByCategory = CreateFreshHandsByCategory();
        }
        public string GetAnalysis(IEnumerable<Hand> hands, IEnumerable<HandCategory>? excludedHands, IEnumerable<HandCategory>? includedHands)
        {
            Reset();
            excludedHands = excludedHands ?? new List<HandCategory>();
            includedHands = includedHands ?? new List<HandCategory>();
            var handGroups = GetUniqueHandGroupsToProcess(hands);
            return GetAnalysisString(handGroups, excludedHands, includedHands);
        }

        /// <summary>
        /// Retrieves hands that are to be processed based on the parameters of the config. The unique roll key prevents superfluous dice from being considered.
        /// </summary>
        /// <param name="hands"></param>
        /// <returns></returns>
        private List<IGrouping<string, Hand>> GetUniqueHandGroupsToProcess(IEnumerable<Hand> hands)
            => hands.GroupBy(h => h.CompleteUniqueRollKey).Select(g => g.First()).GroupBy(h => h.IsomorphicKey).ToList();
    }
}
