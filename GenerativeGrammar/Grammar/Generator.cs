using System.Text;
using GenerativeGrammar.NPC;
using Fare;
using GenerativeGrammar.Model;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Newtonsoft.Json;

namespace GenerativeGrammar.Grammar;

public class Generator
{
    private List<Npc> Npcs { get; set; }
    private Tree GenerativeTree { get; set; }
    private Log LevelLog { get; set; }
    private JSONReader Reader { get; set; }

    private readonly Dictionary<string, string> _operands = new() {
        { "OR", "||" },
        { "AND", "&&" },
        { "=", "==" },
        { "NOT", "!" },
        { "=>", "=>" }
    };

    private readonly string[] _mathSymbols = {"+", "-", "/", "*", "<=", ">="};
    
    public Generator(Tree generativeTree, Log levelLog)
    {
        GenerativeTree = generativeTree;
        Npcs = new List<Npc>();
        LevelLog = levelLog;
        Reader = new JSONReader();
    }
    
    public void GenerateFromTree(Node node)
    {
        switch (node.Name)
        {
            case "NPCS":
            case "STATS":
            case "EVS":
            case "MOVES":
                GoToNeighbours(node);
                break;
            case "NPC":
                GenerateNpc(node);
                break;
            case "TYPES":
            case "AFFIXES":
                HandleNodeWithAttribute(node);
                break;
            case "MOVE":
                HandleMove();
                break;
            case "BASE":
                HandleBaseStats();
                break;
            case "NATURE":
                HandleNature();
                break;
            default:
                HandleEVs(node);
                break;
        }
    }

    private void HandleNodeWithAttribute(Node node)
    {
        var neighbours = node.PossibleNeighbours;
        node.PossibleNeighbours = neighbours.Select(e => e.Split(" : ", 2)[0]).ToList();
        var possibleAttributes = neighbours.Select(e => e.Split(" : ", 2)[1]).ToList();
        var actualNeighbour = GetWeightedTerminalNode(node);
        var index = node.PossibleNeighbours.IndexOf(
            node.PossibleNeighbours.Find(e => e.Split("] ")[1].Trim().Equals(actualNeighbour))!);
        HandleAttributes(possibleAttributes[index]);
        var conditionResult = node.Conditions.Aggregate(true, (current, condition) => current 
            && EvaluateCondition(condition));
        if (conditionResult)
        {
            neighbours = actualNeighbour.Split(" ").Select(e => e.Trim()).ToList();
            foreach (var neighbour in neighbours)
            {
                GenerateFromTree(GenerativeTree.Nodes.Find(e => e.Name.Equals(neighbour)));
            }
        }
    }

    private void HandleBaseStats()
    {
        var stats = Reader.ReadBaseStatsJSON().Where(
            e =>
                (!LevelLog.PlayerDefense.Equals("Special") || e.BaseStats["atk"] >= 70) &&
                (!LevelLog.PlayerDefense.Equals("Physical") || e.BaseStats["spa"] >= 70) &&
                (!LevelLog.PlayerAttack.Equals("Special") || e.BaseStats["spd"] >= 70) &&
                (!LevelLog.PlayerAttack.Equals("Physical") || e.BaseStats["def"] >= 70)
            ).ToList();
        Npcs[^1].BaseStats = stats[new Random().Next(0, stats.Count)];
    }

    private void HandleNature()
    {
        var plus = GetWeightedTerminalNode(GenerativeTree.Nodes.Find(e => e.Name.Equals("PLUS")));
        plus = plus.Replace("\"", "");
        var natures = Reader.ReadNatures();
        natures = natures.Where(e => e.Plus.Equals(plus)).ToList();
        Npcs[^1].Nature = natures[new Random().Next(0, natures.Count)];
    }

    private bool IsMoveValid(Move move)
    {
        return Npcs[^1].Types.Contains(move.Type) || move.Target.Equals("self") || move.Category.Equals("Status");
    }

    private void HandleMove()
    {
        var moves = Reader.ReadMovesJSON().Where(IsMoveValid).ToList();
        Move result;
        do
        {
            result = moves[new Random().Next(0, moves.Count)];
        } while (!IsMoveValid(result));
        Npcs[^1].Moves.Add(result);
    }

    private void HandleEVs(Node node)
    {
        var value = GetValueFromRange(node);
        if (!Npcs[^1].EVS.ContainsKey(node.Name))
        {
            Npcs[^1].EVS.Add(node.Name, value);
        }
        else
        {
            Npcs[^1].EVS[node.Name] = value;
        }
    }

    private void GoToNeighbours(Node node)
    {
        foreach (var neighbour in node.ActualNeighbours)
        {
            GenerateFromTree(GenerativeTree.Nodes.Find(e => e.Name.Equals(neighbour.Replace("*", ""))));
        }
    }

    private void GenerateNpc(Node node)
    {
        var npc = new Npc();
        Npcs.Add(npc);
        foreach (var next in node.ActualNeighbours.Select(neighbour => neighbour.Split(" : ", 2)))
        {
            if (next.Length == 2)
            {
                var attributes = next[1];
                HandleAttributes(attributes);
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
        var condition = EvaluateCondition(parts[0]);
        var statements = parts[1].Split(" : ");
        var trueStatement = statements[0].Trim();
        if (statements.Length != 2) return condition ? trueStatement : "";
        var falseStatement = statements[1].Trim();
        return condition ? trueStatement : falseStatement;

    }

    private bool EvaluateCondition(string condition)
    {
        var parts = condition.Split(" ").ToList();

        for (var i = 0; i < parts.Count; i++)
        {
            if (_operands.ContainsKey(parts[i]))
            {
                parts[i] = _operands[parts[i]];
            }
            else if (!_mathSymbols.Contains(parts[i]))
            {
                parts[i] = HandleVariable(parts[i]).ToString();
            }
        }

        var result = string.Join(" ", parts);
        if (!result.Contains("=>")) return CSharpScript.EvaluateAsync<bool>(result).Result;
        
        var sb = new StringBuilder();
        sb.Append("!(");
        var sides = result.Split(" => ");
        sb.Append(sides[0]).Append(") || (");
        sb.Append(sides[1]).Append(')');
        result = sb.ToString();
        return CSharpScript.EvaluateAsync<bool>(result).Result;
    }

    /**
     * Parse Range for EVs
     */
    private int GetValueFromRange(Node node)
    {
        var range = node.PossibleNeighbours[0].Trim().Split("..");
        var minimum = EvaluateEquation(range[0].Replace("[", ""));
        var maximum = EvaluateEquation(range[1].Replace("]", ""));
        return new Random().Next(minimum, maximum);
    }

    private int EvaluateEquation(string equation)
    {
        var sb = new StringBuilder();
        var parts = equation.Trim().Split(" ");
        foreach (var part in parts)
        {
            if (_mathSymbols.Contains(part)) sb.Append(part);
            else sb.Append(HandleVariable(part));
        }
        return CSharpScript.EvaluateAsync<int>(sb.ToString()).Result;
    }
    
    private int HandleVariable(string variableString)
    {
        Console.WriteLine(variableString);
        try
        {
            return int.Parse(variableString);
        }
        catch (FormatException)
        {
            var parts = variableString.Split("(");
            if (parts.Length == 1)
            {
                var result = ParseVariable(parts[0]);
                return int.Parse(result.ToString()!);
            }

            switch (parts[0])
            {
                case "MIN":
                    var variables = (List<int>)ParseVariable(parts[1].Replace(")", "").Trim());
                    return variables.AsQueryable().Min();
                case "MAX":
                    variables = (List<int>)ParseVariable(parts[1].Replace(")", "").Trim());
                    return variables.AsQueryable().Max();
                case "TYPE.DamageTaken":
                    // Need json files for this
                    return 1;
            }

            if (Npcs[^1].Attributes.ContainsKey(variableString)) return Npcs[^1].Attributes[variableString];
            
            return -1;
        }
    }

    private object ParseVariable(string variableString)
    {
        var parts = variableString.Trim().Split(".");
        if (parts.Length == 1)
        {
            if (Npcs[^1].Attributes.ContainsKey(parts[0].Trim())) 
                return Npcs[^1].Attributes[parts[0].Trim()];
            if (GenerativeTree.GlobalVariables.ContainsKey(parts[0].Trim()))
                return GenerativeTree.GlobalVariables[parts[0].Trim()];
        }
        else
        {
            switch (parts[1])
            {
                case "EnemyBase":
                    return LevelLog.EnemyStats[parts[2].Trim()];
                case "PlayerTypes":
                    return LevelLog.PlayerTypes;
                case "HasAilment":
                    return LevelLog.HasAilments;
                case "PlayerDefense":
                    return LevelLog.PlayerDefense;
                case "PlayerAttack":
                    return LevelLog.PlayerAttack;
            }
        }

        return variableString;
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