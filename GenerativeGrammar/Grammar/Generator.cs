using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using GenerativeGrammar.Exceptions;
using GenerativeGrammar.NPC;
using GenerativeGrammar.Model;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Newtonsoft.Json;

namespace GenerativeGrammar.Grammar;

public class Generator
{
    private List<Model.NPC> Npcs { get; set; }
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
        Npcs = new List<Model.NPC>();
        LevelLog = levelLog;
        Reader = new JSONReader();
    }

    public void GenerateFromTree(Node node)
    {
        if (!node.ActualNeighbours.Any())
        {
            DetermineActualNeighbours(node);
        }
        else if (!string.IsNullOrEmpty(node.Source))
        {
            HandleNodeFromSource(node);
        }
        else if (node.IsLeafNode)
        {
            HandleLeafNode(node);
        }
        else
        {
            HandleNodeWithNeighbours(node);
        }
        
    }

    private void HandleLeafNode(Node node)
    {
        dynamic value = null!;

        if (node.ActualNeighbours.Count == 1)
        {
            value = GetValueFromRange(node);
        } else if (!node.ActualNeighbours.Any())

        {
            value = GetWeightedTerminalNode(node);
        }
        
        if (Npcs[^1].ValuesOfNodes.ContainsKey(node.Name)) Npcs[^1].ValuesOfNodes[node.Name].Add(value);
        else
        {
            Npcs[^1].ValuesOfNodes[node.Name] = new List<string> { value };
        }
    }

    private void HandleNodeWithNeighbours(Node node)
    {
        if (GenerativeTree.Root.Name.Equals(node.Name))
        {
            var npc = new Model.NPC();
            Npcs.Add(npc);
        }
        foreach (var neighbour in node.ActualNeighbours)
        {
            var sides = neighbour.Split(" : ", 2);
            if (sides.Length == 2)
            {
                var attributes = sides[1].Split(", ");
                foreach (var attribute in attributes)
                {
                    HandleAttribute(attribute.Trim());
                }
                
            }
            var nextNode = GenerativeTree.Nodes.Find(e => e.Name.Equals(sides[0].Trim()));
            if (nextNode.Equals(default)) continue;
            GenerateFromTree(nextNode);
        }
    }

    private void HandleNodeFromSource(Node node)
    {
        var reader = new JSONReader();
        var file = node.Source.Split(".")[0].Trim();
        var method = reader.GetType().GetMethods().ToList().Find(e => e.Name.ToLower().Contains(file));
        
        //? conditional access - method might be null
        //! suppress nullable warning
        dynamic values= method?.Invoke(reader, Array.Empty<object>())!;
        
        //Just pick random from list for now
        var valueOfNode = values[new Random().Next(0, values.Count)];
        if (Npcs[^1].ValuesOfNodes.ContainsKey(node.Name))
        {
            Npcs[^1].ValuesOfNodes[node.Name].Add(valueOfNode.ToString() ?? string.Empty);
        }
        else
        {
            Npcs[^1].ValuesOfNodes[node.Name] = new List<string> { valueOfNode.ToString() ?? string.Empty };
        }
    }

    private void DetermineActualNeighbours(Node node)
    {
        var nextNode = GetWeightedTerminalNode(node);
        var sides = nextNode.Split(":", 2);
        if (sides.Length == 2)
        {
            var attributes = sides[1].Trim().Split(", ");
            foreach (var attribute in attributes)
            {
                HandleAttribute(attribute);
            }
        }
        var neighbours = sides[0].Trim();
        var conditionResult = node.Conditions.Aggregate(true, (current, condition) => current && HandleCondition(condition));
        foreach (var neighbour in neighbours)
        {
            var next = GenerativeTree.Nodes.Find(e => e.Name.Equals(neighbour));
            if (next.Equals(default)) continue;
            GenerateFromTree(next);
        }
    }

    private void HandleAttribute(string attribute)
    {
        if (attribute.Contains('?'))
        {
            attribute = HandleIfStatement(attribute);
        }

        if (string.IsNullOrEmpty(attribute)) return;
        var sides = attribute.Split(" ");
        if (HandleVariable(sides[0]) == null) Npcs[^1].Attributes.Add(sides[0], 0);
        if (attribute.Contains("<-"))
        {
            Npcs[^1].Attributes[sides[0]] = EvaluateEquation(string.Join(" ", sides.Skip(2)));
        }
        else
        {
            var value = EvaluateEquation(string.Join(" ", sides));
            SetVariable(sides[0], value);
        }
    }

    private void SetVariable(string variable, int value)
    {
        if (GenerativeTree.GlobalVariables.ContainsKey(variable)) GenerativeTree.GlobalVariables[variable] = value;
        else if (Npcs[^1].Attributes.ContainsKey(variable)) Npcs[^1].Attributes[variable] = value;
        else
        {
            throw new NonExistentVariableException(variable);
        }
    }


    private string HandleIfStatement(string attribute)
    {
        var parts = attribute.Split(" ? ");
        if (parts.Length == 1) return parts[0].Trim();
        var condition = HandleCondition(parts[0]);
        var statements = parts[1].Split(" : ");
        var trueStatement = statements[0].Trim();
        if (statements.Length != 2) return condition ? trueStatement : string.Empty;
        var falseStatement = statements[1].Trim();
        return condition ? trueStatement : falseStatement;

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

    private string GetWeightedTerminalNode(Node typeNode)
    {
        var weightedTypesList = new Dictionary<string, int>();
        foreach (var sides in typeNode.PossibleNeighbours.Select(value => value.Trim().Replace("[", "").Split("] ")))
        {
            int weight;
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

    private bool HandleCondition(string condition)
    {
        condition = HandleIn(condition);
        var tokens = condition.Split(" ");
        var result = new List<string>();
        foreach (var token in tokens)
        {
            if (_operands.ContainsKey(token))
            {
                result.Add(_operands[token]);
            }
            else if (_mathSymbols.Contains(token)) result.Add(token);
            else
            {
                Console.WriteLine(token);
                var variable = HandleVariable(token);
                var brackets = HandleConditionBrackets(token);
                
                result.Add(brackets[0] + variable + brackets[1]);
            }
        }

        condition = HandleImply(string.Join(" ", result)).ToLower();
        Console.WriteLine(condition);
        return CSharpScript.EvaluateAsync<bool>(condition).Result;
    }

    private string HandleImply(string condition)
    {
        //Assume there is only one implication per condition
        var tokens = condition.Split(" => ", 2);
        if (tokens.Length == 2)
        {
            condition = "!(" + tokens[0] + ") || (" + tokens[1] + ")";
        }

        return condition;
    }

    private string HandleIn(string condition)
    {
        condition = condition.Trim();
        var tokens = condition.Split(" ");
        while (tokens.Contains("IN"))
        {
            var negativeFlag = false;
            var index = Array.IndexOf(tokens, "IN");
            int startIndex;
            if (!tokens[index].Equals("IN")) continue;
            string variable;
            if (tokens[index - 1].Equals("NOT"))
            {
                negativeFlag = true;
                variable = HandleVariable(tokens[index - 2]).ToString() ?? string.Empty;
                startIndex = index - 2;
            }
            else
            {
                variable = HandleVariable(tokens[index - 1]).ToString() ?? string.Empty;
                startIndex = index - 1;
            }

            var list = (List<string>) HandleVariable(tokens[index + 1]);
            var endIndex = index + 1;

            var startBrackets = HandleConditionBrackets(tokens[startIndex])[0];
            var endBrackets = HandleConditionBrackets(tokens[endIndex])[1];
            
            var result = negativeFlag ? (!list.Contains(variable)).ToString() : list.Contains(variable).ToString();
            result = startBrackets + result + endBrackets;
            
            var tokensList = tokens.ToList();
            tokensList.RemoveRange(startIndex, endIndex - startIndex + 1);
            tokensList.Insert(startIndex, result);
            
            tokens = tokensList.ToArray();
        }

        return string.Join(" ", tokens);
    }

    private object HandleVariable(string token)
    {
        var index = -1;
        dynamic result;
        if (!(token.EndsWith(')') && token.Contains('(') && !token.StartsWith('(')))
            token = token.Replace("(", "").Replace(")", "");
        if (token.Contains('['))
        {
            index = int.Parse(token.Replace("]", "").Split("[")[1]);
            token = token.Split("[")[0];
        }
        
        if (token.StartsWith("\"") || int.TryParse(token, out _) || bool.TryParse(token, out _))
        {
            result = token.Replace("\"", "");
        }

        else if (token.StartsWith("LOGS."))
        {
            var sides = token.Split(".");
            dynamic field = LevelLog.GetType().GetProperty(sides[1])?.GetValue(LevelLog) ?? throw new InvalidOperationException();
            result = sides.Length == 3 ? field[sides[2]] : field;
        }
        
        else if (GenerativeTree.GlobalVariables.ContainsKey(token)) result = GenerativeTree.GlobalVariables[token];
        
        else if (Npcs[^1].Attributes.ContainsKey(token)) result = Npcs[^1].Attributes[token];
        else if (Npcs[^1].ValuesOfNodes.ContainsKey(token)) result = Npcs[^1].ValuesOfNodes[token];
        else return null!;
        
        return index != -1 ? result[index] : result;
    }

    private string[] HandleConditionBrackets(string token)
    {
        var startBracket = new StringBuilder();
        var endBracket = new StringBuilder();
        foreach (var character in token)
        {
            switch (character)
            {
                case '(':
                    startBracket.Append('(');
                    break;
                case ')':
                    endBracket.Append(')');
                    break;
            }
        }

        var result = new[] {startBracket.ToString(), endBracket.ToString()};
        return result;
    }
}