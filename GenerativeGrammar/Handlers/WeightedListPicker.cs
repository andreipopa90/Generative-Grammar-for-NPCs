using GenerativeGrammar.Model;

namespace GenerativeGrammar.Handlers;

public class WeightedListPicker
{

    private ExpressionHandler Handler;
    public WeightedListPicker(ExpressionHandler handler)
    {
        Handler = handler;
    }

    public object GetObjectFromWeightedList(Dictionary<object, int> list)
    {
        // var sortedDict = list.OrderBy(e => e.Value).ToDictionary(e => e.Key, e=> e.Value);
        var index = PickIndexFromWeightedList(list.Values.ToList());
        var result = list.Keys.ToList()[index];
        return result;
    }

    public string GetWeightedTerminalNode(Node node)
    {
        var weightedTypesList = new Dictionary<string, int>();
        foreach (var sides in node.PossibleNeighbours.Select(value => value.Trim().Replace("[", "").Split("] ")))
        {
            int weight;
            try
            {
                weight = int.Parse(Handler.EvaluateEquation(sides[0].Trim()).ToString());
            }
            catch (FormatException)
            {
                weight = 1;
            }
            var type = sides[1].Trim();
            weightedTypesList.Add(type, weight);
        }
        
        
        
        var index = PickIndexFromWeightedList(weightedTypesList.Values.ToList());
        var result = weightedTypesList.Keys.ToList()[index];
        return result;
    }

    private static int PickIndexFromWeightedList(List<int> weights)
    {
        weights = weights.Select(e => e < 0 ? 0 : e).ToList();
        for (var i = 1; i < weights.Count; i++)
        {
            weights[i] += weights[i - 1];
        }

        double total = weights[^1];
        var value = new Random().NextDouble() * total;
        return BinarySearch(weights, value);
    }

    private static int BinarySearch(IReadOnlyList<int> weightsList, double value)
    {
        var low = 0;
        var high = weightsList.Count - 1;
        while (low < high)
        {
            var mid = (low + high) / 2;
            if (value < weightsList[mid])
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }
}