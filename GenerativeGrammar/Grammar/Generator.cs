using System.Text.RegularExpressions;
using GenerativeGrammar.NPC;
using Fare;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace GenerativeGrammar.Grammar;

public class Generator
{
    private List<NPC.Npc> Npcs { get; set; }
    private Tree GenerativeTree { get; set; }
    private Log LevelLog { get; set; }
    
    public Generator(Tree generativeTree, Log levelLog)
    {
        GenerativeTree = generativeTree;
        Npcs = new List<NPC.Npc>();
        LevelLog = levelLog;
    }
    
    public void GenerateFromTree(Node node)
    {
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
                    GenerateFromTree(GenerativeTree.Nodes.Find(e => e.Name.Equals(neighbour.Replace("*", ""))));
                }
                break;
            case "NPC":
                GenerateNpc(node);
                break;
            case "TYPE": 
            case "PLUS":
            case "MINUS":
            case "CATEGORY":
            case "TARGET":
                Npcs[^1].Types.Add(GetWeightedTerminalNode(node));
                break;
            case "NAME":
                Npcs[^1].Moves.Add(new Xeger(node.Name).Generate());
                break;
            case "TYPES":
            case "AFFIXES":
            case "BONUS":
                break;
            default:
                if (node.Name.EndsWith("EV")){
                    if (!Npcs[^1].EVS.ContainsKey(node.Name))
                    {
                        Npcs[^1].EVS.Add(node.Name, GetValueFromRange(node));
                    }
                    else
                    {
                        Npcs[^1].EVS[node.Name] = GetValueFromRange(node);
                    }
                }
                else
                {
                    if (!Npcs[^1].BaseStats.ContainsKey(node.Name))
                    {
                        Npcs[^1].BaseStats.Add(node.Name, GetValueFromRange(node));
                    }
                    else
                    {
                        Npcs[^1].BaseStats[node.Name] = GetValueFromRange(node);
                    }
                }

                break;
        }
    }

    private void GenerateNpc(Node node)
    {
        var npc = new NPC.Npc();
        Npcs.Add(npc);
        foreach (var neighbour in node.ActualNeighbours)
        {
            var next = neighbour.Split(" : ", 2);
            if (next.Length == 2)
            {
                var attributes = next[1];
                HandleAttributes(attributes);
            }

            foreach (var attribute in npc.Attributes)
            {
                Console.WriteLine(attribute.Key);
            }
            GenerateFromTree(GenerativeTree.Nodes.Find(e => e.Name.Equals(next[0].Trim())));
        }
    }

    private void HandleAttributes(string attributes)
    {
        var attributesList = attributes.Trim().Split(", ");
        foreach (var attribute in attributesList)
        {
            var attr = HandleIfStatement(attribute);
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
        if (parts.Length != 2) return parts[0].Trim();
        var condition = HandleCondition(parts[0]);
        var trueStatement = parts[1].Split(" : ")[0].Trim();
        var falseStatement = parts[1].Split(" : ")[1].Trim();
        return condition ? trueStatement : falseStatement;
    }

    private bool HandleCondition(string part)
    {
        return CSharpScript.EvaluateAsync<bool>(part).Result;
    }

    private int GetValueFromRange(Node node)
    {
        var range = node.PossibleNeighbours[0].Trim().Split("..");
        var minimum = HandleVariable(range[0].Replace("[", ""));
        var maximum = HandleVariable(range[1].Replace("]", ""));
        return new Random().Next(minimum, maximum);
    }

    private int HandleVariable(string s)
    {
        try
        {
            var value = int.Parse(s.Trim());
            return value;
        }
        catch (FormatException)
        {
            var result = 0;
            var sides = Regex.Split(s, @"\+|\-");
            
            foreach (var side in sides)
            {
                if (side.Trim().EndsWith(")"))
                {
                    HandleAggregateFunction(side);
                    var function = side.Split("(")[0];
                    var variable = side.Split("(")[1].Replace(")", "");
                    if (function.Contains("MIN"))
                    {
                        result += LevelLog.EnemyStats[variable.Split(".")[^1].Trim()].Min();
                    }
                    else if (function.Contains("MAX"))
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

    private int ParseVariable(string variable)
    {
        if (Npcs[^1].Attributes.ContainsKey(variable))
        {
            return Npcs[^1].Attributes[variable];
        } 
        else if (GenerativeTree.GlobalVariables.ContainsKey(variable))
        {
            return GenerativeTree.GlobalVariables[variable];
        }

        return 0;
    }

    private int HandleAggregateFunction(string block)
    {
        var sides = block.Trim().Replace(")", "").Split("(");
        switch (sides[0])
        {
            case "MIN":
                break;
            case "MAX":
                break;
            case "SIZE":
                break;
        }
        return 0;
    }

    private string GetWeightedTerminalNode(Node typeNode)
    {
        var weightedTypesList = new Dictionary<string, int>();
        foreach (var value in typeNode.PossibleNeighbours)
        {
            var sides = value.Trim().Replace("[", "").Split("] ");
            var weight = 0;
            try
            {
                weight = int.Parse(sides[0].Trim());
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