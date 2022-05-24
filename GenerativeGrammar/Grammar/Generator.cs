using System.Text.RegularExpressions;
using GenerativeGrammar.NPC;

namespace GenerativeGrammar.Grammar;

public class Generator
{
    public List<NPC.NPC> Npcs { get; set; }
    public Tree GenerativeTree { get; set; }
    public Log LevelLog { get; set; }
    
    public Generator(Tree generativeTree, Log LevelLog)
    {
        GenerativeTree = generativeTree;
        Npcs = new List<NPC.NPC>();
        this.LevelLog = LevelLog;
    }
    
    public void GenerateFromTree(Node node)
    {
        Console.WriteLine(node.Name);
        switch (node.Name)
        {
            case "NPCS":
            case "STATS":
            case "BASE":
            case "NATURE":
            case "EVS":
            case "MOVES":
            case "MOVE":
                foreach (var neighbour in node.ActualNeighbours)
                {
                    GenerateFromTree(GenerativeTree.Nodes.Find(e => e.Name.Equals(neighbour)));
                }
                break;
            case "NPC":
                var npc = new NPC.NPC();
                Npcs.Add(npc);
                foreach (var neighbour in node.ActualNeighbours)
                {
                    var next = neighbour.Split(" : ", 2);
                    if (next.Length == 2)
                    {
                        var attributes = next[1];
                        HandleAttributes(attributes);
                    }

                    GenerateFromTree(GenerativeTree.Nodes.Find(e => e.Name.Equals(next[0].Trim())));
                }
                break;
            case "TYPE": 
            case "PLUS":
            case "MINUS":
            case "CATEGORY":
            case "TARGET":
                GenerateTypes(node);
                break;
            case "NAME":
                break;
            case "TYPES":
            case "AFFIXES":
            case "BONUS":
                break;
            default:
                GenerateValueFromRange(node);
                break;
        }
    }

    private void HandleAttributes(string attributes)
    {
        var attributesList = attributes.Trim().Split(", ");
        foreach (var attribute in attributesList)
        {
            string attr = HandleIfStatement(attribute);
            if (attr.Contains("<-"))
            {
                var parts = attr.Split(" <- ");
                Npcs[^1].Attributes.Add(parts[0].Trim(), int.Parse(parts[1].Trim()));
            }
            else
            {
                var parts = attr.Split(" += ");
                if (GenerativeTree.GlobalVariables.ContainsKey(parts[0].Trim()))
                {
                    GenerativeTree.GlobalVariables[parts[0]] += int.Parse(parts[1].Trim());
                }
                else
                {
                    Npcs[^1].Attributes[parts[0].Trim()] += int.Parse(parts[1].Trim()) ;
                }
            }
            
        }
    }

    private string HandleIfStatement(string attribute)
    {
        var parts = attribute.Split(" ? ");
        if (parts.Length == 2)
        {
            var condition = parts[0];
            var trueStatement = parts[1].Split(" : ")[0].Trim();
            var falseStatement = parts[1].Split(" : ")[1].Trim();
            return trueStatement;
        }
        else
        {
            return parts[0].Trim();
        }
    }

    private int GenerateValueFromRange(Node node)
    {
        var range = node.PossibleNeighbours[0].Trim().Split("..");
        int minimum = HandleValue(range[0].Replace("[", ""));
        int maximum = HandleValue(range[1].Replace("]", ""));
        return new Random().Next(minimum, maximum);
    }

    private int HandleValue(string s)
    {
        
        try
        {
            int value = int.Parse(s.Trim());
            return value;
        }
        catch (FormatException)
        {
            var result = 0;
            var sides = Regex.Split(s, @" \+ | \- ");
            
            foreach (var side in sides)
            {
                if (side.EndsWith(")"))
                {
                    var function = side.Split("(")[0];
                    var variable = side.Split("(")[1].Replace(")", "");
                    if (function.Contains("MIN"))
                    {
                        result += LevelLog.EnemyStats[variable.Split(".")[^1]].Min();
                    }
                    else
                    {
                        result += LevelLog.EnemyStats[variable.Split(".")[^1]].Max();
                    }
                }
                else
                {
                    result += int.Parse(side.Trim());
                }
            }
            return result;
        }
    }

    public string GenerateTypes(Node typeNode)
    {
        Dictionary<string, int> weightedTypesList = new Dictionary<string, int>();
        foreach (var value in typeNode.PossibleNeighbours)
        {
            var sides = value.Trim().Remove(0, 1).Split("] ");
            var weight = int.Parse(sides[0]);
            var type = sides[1].Trim();
            weightedTypesList.Add(type, weight);
        }

        Dictionary<string, int> results = new Dictionary<string, int>();

        int index = PickIndexFromWeightedList(weightedTypesList.Values.ToList());
        string result = weightedTypesList.Keys.ToList()[index];
        return result;
    }

    private int PickIndexFromWeightedList(List<int> weights)
    {
        for (int i = 1; i < weights.Count; i++)
        {
            weights[i] += weights[i - 1];
        }

        double total = weights[^1];
        double value = new Random().NextDouble() * total;
        return BinarySearch(weights, value);
    }

    private int BinarySearch(List<int> weightsList, double value)
    {
        int low = 0;
        int high = weightsList.Count - 1;
        while (low < high)
        {
            int mid = (low + high) / 2;
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