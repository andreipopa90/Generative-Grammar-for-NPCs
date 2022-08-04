using System.Text;
using GenerativeGrammar.Exceptions;
using GenerativeGrammar.JsonParser;
using GenerativeGrammar.Model;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace GenerativeGrammar.Handlers;

public class ExpressionHandler
{
    private List<Npc> Npcs { get; }
    private Tree GenerativeTree { get; }
    private Log LevelLog { get; }
    private readonly Dictionary<string, string> _operands = new() {
        { "OR", "||" },
        { "AND", "&&" },
        { "=", "==" },
        { "NOT", "!" },
        { "=>", "=>" },
        { "+=", "+" },
        { "-=", "-" },
        { "!=", "!=" },
        { "+", "+" },
        { "-", "-" },
        { "*", "*" },
        { "/", "/" },
        { "<=", "<=" },
        { ">=", ">=" },
        { "<", "<" },
        { ">", ">"}
    };
    

    public ExpressionHandler(Log levelLog, Tree generativeTree, List<Npc> npcs)
    {
        LevelLog = levelLog;
        GenerativeTree = generativeTree;
        Npcs = npcs;
    }

    public void HandleAttribute(string attribute)
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

    public int EvaluateEquation(string equation)
    {
        var sb = new StringBuilder();
        var parts = equation.Trim().Split(" ");
        foreach (var part in parts)
        {
            if (_operands.ContainsKey(part)) sb.Append(_operands[part]);
            else sb.Append(HandleVariable(part));
        }
        return CSharpScript.EvaluateAsync<int>(sb.ToString()).Result;
    }

    public bool HandleCondition(string condition)
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
            else
            {
                var variable = HandleVariable(token);
                var brackets = HandleConditionBrackets(token);
                
                result.Add(brackets[0] + variable + brackets[1]);
            }
        }

        condition = string.Join(" ", result);
        condition = HandleStringEquality(condition);
        condition = HandleImply(condition).ToLower().Replace(".equals", ".Equals");
        
        return CSharpScript.EvaluateAsync<bool>(condition).Result;
    }

    private string HandleStringEquality(string equation)
    {
        var tokens = equation.Split(" ");
        var stringEqualityIndexes = new List<int>();
        
        for (var i = 0; i < tokens.Length; i++)
        {
            if ((tokens[i].Equals("==") || tokens[i].Equals("!="))
                && !int.TryParse(tokens[i - 1], out _)
                && !int.TryParse(tokens[i + 1], out _)
                && !bool.TryParse(tokens[i - 1], out _)
                && !bool.TryParse(tokens[i + 1], out _))
            {
                stringEqualityIndexes.Add(i);
            }
        }

        
        foreach (var index in stringEqualityIndexes)
        {
            tokens[index - 1] = "\"" + tokens[index - 1] + "\"";
            tokens[index + 1] = "\"" + tokens[index + 1] + "\")";
            if (tokens[index].Equals("!="))
            {
                tokens[index - 1] = "!" + tokens[index - 1];
            }
            tokens[index] = ".Equals(";
            
        }
        return string.Join(" ", tokens);
    }
    
    /**
     * <summary>
     * Change expressions that use imply, like A => B, to !(A) || (B), where A and B are boolean expressions
     * Assumption taken: there is only one imply used per boolean expression, so A and B have no imply
     * </summary>
     */
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
            string variable;
            if (tokens[index - 1].Equals("NOT"))
            {
                negativeFlag = true;
                variable = HandleVariable(tokens[index - 2])!.ToString()!;
                startIndex = index - 2;
            }
            else
            {
                variable = HandleVariable(tokens[index - 1])!.ToString()!;

                startIndex = index - 1;
            }
            var list = (List<dynamic>) HandleVariable(tokens[index + 1], returnCompleteList: true)!;
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

    public object? HandleVariable(string token, bool returnCompleteList = false)
    {
        var index = -1;
        dynamic result;
        token = CheckForFunction(token);
        
        token = CheckForIndex(token, ref index);
        
        var tokenStart = token.Split(".")[0].Trim();
        
        if (token.StartsWith("\"") || int.TryParse(token, out _) || bool.TryParse(token, out _))
        {
            result = token.Replace("\"", "");
        }
        else if (GenerativeTree.Parameters.Contains(tokenStart))
        {
            var sides = token.Split(".");
            dynamic field = LevelLog.GetType().GetProperty(sides[1])?.GetValue(LevelLog) ?? throw new InvalidOperationException();
            result = sides.Length == 3 ? field[sides[2]] : field;
        }
        else if (GenerativeTree.GlobalVariables.ContainsKey(token))
        {
            result = GenerativeTree.GlobalVariables[token];
        }
        else if (Npcs[^1].Attributes.ContainsKey(token))
        {
            result = Npcs[^1].Attributes[token];
        }
        else if (Npcs[^1].ValuesOfNodes.ContainsKey(token))
        {
            var resultReturned = new List<dynamic>();
            var visited = new List<string>();
            Npcs[^1].GetNodesTerminalValues(token, ref resultReturned, ref visited);
            result = resultReturned;
        }
        else return null!;

        if (result.GetType().IsGenericType &&
            result.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
            return index != -1 ? result[index] : returnCompleteList ? result : result[0];
        return result;
    }

    private static string CheckForIndex(string token, ref int index)
    {
        if (!token.Contains('[')) return token;
        index = int.Parse(token.Replace("]", "").Split("[")[1]);
        token = token.Split("[")[0];

        return token;
    }

    private string CheckForFunction(string token)
    {
        if (!(token.EndsWith(')') && token.Contains('(') && !token.StartsWith('(')))
        {
            token = token.Replace("(", "").Replace(")", "");
        }
        else
        {
            token = HandleFunction(token);
        }

        return token;
    }

    private string HandleFunction(string token)
    {
        var sides = token.Split('(');
        var function = sides[0].Trim();
        var variables = sides[1].Replace(")", "").Trim().Split(", ");
        var results = new List<dynamic>();
        foreach (var v in variables) 
        {
            dynamic variable = HandleVariable(v, returnCompleteList: true)!;
            if (variable == null) return token;
            if (variable.GetType().IsGenericType &&
                variable.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
            {
                for(var i = 0; i < variable.Count; i++)
                    results.Add(variable[i]);
            }
            else
            {
                results.Add(variable);
            }
        }

        return function switch
        {
            "MAX" => results.Max()!.ToString(),
            "MIN" => results.Min()!.ToString(),
            "SIZE" => results.Count.ToString(),
            "DISTINCT" => (results.Distinct().Count() == results.Count).ToString(),
            _ => HandleCustomFunction(function, results)
        };
    }

    private string HandleCustomFunction(string function, List<dynamic> variables)
    {
        if (!function.Equals("TYPE.DamageTaken")) return "-1";
        var type = new JSONReader().ReadTypeChartJson().Find(e => e.Name.Equals(Npcs[^1].ValuesOfNodes["TYPE"][^1]))!;
        var damageTaken = variables.Select(playerType => type.DamageTaken[playerType]).ToList();

        var damageTakenString = string.Join("", damageTaken);
        if (damageTakenString.Equals("01") ||
            damageTakenString.Equals("10") ||
            damageTakenString.Equals("11") ||
            damageTakenString.Equals("1"))
            return "1";
        
        return damageTakenString[..1];
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