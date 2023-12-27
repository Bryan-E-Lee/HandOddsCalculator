namespace HandCalculator
{
    internal class HandCategory : IComparable<HandCategory>
    {
        protected HandCategory(string name, int value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; private set; }
        public int Value { get; private set; }

        public string DisplayNameValue => $"{Name} - {Value}";

        public static implicit operator string(HandCategory handCategory) => handCategory.Name;
        public static implicit operator HandCategory(string name) => CategoriesByName[name];

        public static implicit operator int(HandCategory handCategory) => handCategory.Value;
        public static implicit operator HandCategory(int value) => CategoriesByValue[value];

        private static readonly List<string> HandCategoryNames = new List<string>
        {
            "High Roll",
            "Pair",
            "Two Pair",
            "Triple",
            "Small Straight",
            "Flush",
            "Full House",
            "Big Straight",
            "Quad",
            "Jackpot"
        };

        public static readonly List<HandCategory> AllHandCategories = HandCategoryNames.Select((name, index) => new HandCategory(name, index)).ToList();

        private static readonly Dictionary<string, HandCategory> CategoriesByName = AllHandCategories.ToDictionary(category => category.Name, category => category);
        private static readonly Dictionary<int, HandCategory> CategoriesByValue = AllHandCategories.ToDictionary(category => category.Value, category => category);

        public const string HighRollName = "High Roll";
        public static HandCategory HighRoll => CategoriesByName[HighRollName];

        public const string PairName = "Pair";
        public static HandCategory Pair => CategoriesByName[PairName];

        public const string TwoPairName = "Two Pair";
        public static HandCategory TwoPair => CategoriesByName[TwoPairName];

        public const string SmallStraightName = "Small Straight";
        public static HandCategory SmallStraight => CategoriesByName[SmallStraightName];

        public const string TripleName = "Triple";
        public static HandCategory Triple => CategoriesByName[TripleName];

        public const string FlushName = "Flush";
        public static HandCategory Flush => CategoriesByName[FlushName];

        public const string FullHouseName = "Full House";
        public static HandCategory FullHouse => CategoriesByName[FullHouseName];

        public const string BigStraightName = "Big Straight";
        public static HandCategory BigStraight => CategoriesByName[BigStraightName];

        public const string QuadName = "Quad";
        public static HandCategory Quad => CategoriesByName[QuadName];

        public const string JackpotName = "Jackpot";
        public static HandCategory Jackpot => CategoriesByName[JackpotName];

        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => Name;
        public override bool Equals(object? obj)
        {
            if (obj is not HandCategory otherCategory)
            {
                return base.Equals(obj);
            }
            return Value == otherCategory.Value;
        }

        public int CompareTo(HandCategory? other) => Value.CompareTo(other?.Value);
    }
}
