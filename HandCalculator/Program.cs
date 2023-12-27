// See https://aka.ms/new-console-template for more information

using HandCalculator;
using HandCalculator.Analyzers;

const string InvalidEntry = "\nInvalid Entry\n";

//var rerolls = 0;
//var extraRolls = 0;
var handConfig = new HandConfig
{
    CommunalRolls = 2,
    NormalRolls = 3
};

var primaryCommands = new List<string>
{
    "P - Possible Hands",
    "R - Set Rerolls",
    "O - Set Extra Rolls",
    "E - Set Excluded Hands",
    "I - Set Included Hands",
    "X - Exit"
};

var excludedHands = new List<HandCategory>();
var includedHands = new List<HandCategory>();

var possibleHandAnalyzer = new PossibleHandsAnalyzer(handConfig);
var hands = GenerateHands(7).Select(roll => new Hand(roll, handConfig)).ToList();
while (true)
{
    Console.WriteLine($"\n\nEnter Command:\n{String.Join('\n', primaryCommands)}");
    var response = GetResponse();
    switch (response)
    {
        case "x":
            return;
        case "p":
            RunAnalyzer(possibleHandAnalyzer);
            break;
        case "r":
            SetRerollsProcedure();
            break;
        case "o":
            SetExtraRollsProcedure();
            break;
        case "e":
            SetExcludedHandsProcedure();
            break;
        case "i":
            SetIncludedHandsProcedure();
            break;
        default:
            Console.WriteLine(InvalidEntry);
            break;
    }
}

static List<List<int>> GenerateHands(int numberOfDice)
{
    if (numberOfDice <= 1)
    {
        return Hand.DieFaces
            .Select(val => new List<int> { val })
            .ToList();
    }
    return GenerateHands(numberOfDice - 1)
        .SelectMany(hand => Enumerable.Range(1, 6)
                            .Select(val => new List<int> { val }.Concat(hand).ToList())
                            .ToList()
        )
        .ToList();
}


static string GetResponse() => Console.ReadKey().KeyChar.ToString().ToLower();

void RunAnalyzer(HandAnalyzer handAnalyzer)
{
    handAnalyzer.Reset();
    Console.WriteLine($"\n\n");
    var analysis = handAnalyzer.GetAnalysis(hands, excludedHands, includedHands);
    Console.WriteLine($"{analysis}\n");
}

void SetRerollsProcedure()
{
    while (true)
    {
        Console.WriteLine($"\n\nSet Rerolls (max {handConfig.NormalRolls} / min 0) (X to quit):");
        var response = GetResponse();
        if (response == "x")
        {
            return;
        }
        if (int.TryParse(response, out int rerolls))
        {
            handConfig.Rerolls = Math.Max(0, Math.Min(rerolls, handConfig.NormalRolls));
            Console.WriteLine($"\nRerolls set to {rerolls}");
            return;
        }
        Console.WriteLine(InvalidEntry);
    }
}

void SetExtraRollsProcedure()
{
    while (true)
    {
        Console.WriteLine($"\n\nSet Extra Rolls (max 2 / min 0) (X to quit):");
        var response = GetResponse();
        if (response == "x")
        {
            return;
        }
        if (int.TryParse(response, out int extraRolls))
        {
            handConfig.ExtraRolls = Math.Max(0, Math.Min(extraRolls, 2));
            Console.WriteLine($"\nExtra Rolls set to {handConfig.ExtraRolls}");
            return;
        }
        Console.WriteLine(InvalidEntry);
    }
}

string GetCurrentlyExcludeIncludeHandsText(IEnumerable<HandCategory> collection, string emptyText)
{
    List<string> handNames = new();
    if (!collection?.Any() ?? true)
    {
        return emptyText;
    }

    handNames = collection!.OrderBy(eh => eh.Value).Select(eh => eh.DisplayNameValue).ToList();
    return "\n* " + String.Join("\n* ", handNames);
}

string GetPossibleExcludeIncludeHandsText(IEnumerable<HandCategory> hands, string allText)
{
    if (!hands?.Any() ?? true)
    {
        return "\n* " + String.Join("\n* ", HandCategory.AllHandCategories.Select(hc => hc.DisplayNameValue));
    }
    var possibleNames = HandCategory.AllHandCategories.Where(hc => !hands!.Any(eh => eh.Name == hc.Name)).Select(hc => hc.DisplayNameValue).ToList();
    if (!possibleNames.Any())
    {
        return allText;
    }
    return "\n* " + String.Join("\n* ", possibleNames);
}


void SetExcludedHandsProcedure()
{
    while (true)
    {
        Console.WriteLine($"\n\nCurrently Excluded Hands:");
        Console.WriteLine(GetCurrentlyExcludeIncludeHandsText(excludedHands, "No hands excluded."));
        Console.WriteLine("\nSet Excluded Hands (calculate odds IGNORING hands in this list). Enter hand value to toggle exclusion:");
        Console.WriteLine(GetPossibleExcludeIncludeHandsText(excludedHands, "All hands excluded. Why did you do that?"));
        Console.WriteLine("X - Back");

        var response = GetResponse();
        if (response == "x")
        {
            return;
        }
        if (!int.TryParse(response, out int toggleHandValue))
        {
            Console.WriteLine(InvalidEntry);
            continue;
        }

        var handToExclude = HandCategory.AllHandCategories.FirstOrDefault(hc => hc.Value == toggleHandValue);
        if (handToExclude == null)
        {
            Console.WriteLine(InvalidEntry);
            continue;
        }

        if (excludedHands?.Contains(handToExclude) ?? false)
        {
            excludedHands.Remove(handToExclude);
        }
        else
        {
            excludedHands = excludedHands ?? new();
            excludedHands.Add(handToExclude);
        }
    }
}

void SetIncludedHandsProcedure()
{
    while (true)
    {
        Console.WriteLine($"\n\nCurrently Included Hands:");
        Console.WriteLine(GetCurrentlyExcludeIncludeHandsText(includedHands, "No hands included."));
        Console.WriteLine("\nSet Included Hands (calculate odds REQUIRES hands in this list). Enter hand value to toggle exclusion:");
        Console.WriteLine(GetPossibleExcludeIncludeHandsText(includedHands, "All hands included. Why did you do that?"));
        Console.WriteLine("X - Back");

        var response = GetResponse();
        if (response == "x")
        {
            return;
        }
        if (!int.TryParse(response, out int toggleHandValue))
        {
            Console.WriteLine(InvalidEntry);
            continue;
        }

        var handToInclude = HandCategory.AllHandCategories.FirstOrDefault(hc => hc.Value == toggleHandValue);
        if (handToInclude == null)
        {
            Console.WriteLine(InvalidEntry);
            continue;
        }

        if (includedHands?.Contains(handToInclude) ?? false)
        {
            includedHands.Remove(handToInclude);
        }
        else
        {
            includedHands = includedHands ?? new();
            includedHands.Add(handToInclude);
        }
    }
}