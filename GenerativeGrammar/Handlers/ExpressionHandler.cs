using System.Data;
using System.Text;
using GenerativeGrammar.Exceptions;
using GenerativeGrammar.JsonParser;
using GenerativeGrammar.Model;
using Tree = GenerativeGrammar.Model.Tree;

namespace GenerativeGrammar.Handlers
{
    public class ExpressionHandler
    {
        public List<Npc> Npcs { get; set; }
        public Tree GenerativeTree { get; set; }
        public Log LevelLog { get; set; }

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

        private readonly List<string> _functions = new()
        {
            "MIN", "MAX", "SIZE", "DISTINCT", "TYPE.DamageTaken", "SUM"
        };

        private static ExpressionHandler Instance { get; set; }

        public static ExpressionHandler GetInstance()
        {
            return Instance ??= new ExpressionHandler();
        }

        private ExpressionHandler()
        {
            LevelLog = Log.GetInstance();
            GenerativeTree = new Tree();
            Npcs = new List<Npc>();
        }

        public void HandleAttribute(string attribute)
        {
            if (attribute.Contains('?'))
            {
                attribute = HandleIfStatement(attribute);
            }

            if (string.IsNullOrEmpty(attribute)) return;
            var sides = attribute.Split(" ");
            if (HandleVariable(sides[0]) == null)
            {
                GenerativeTree.Attributes.Add(sides[0], 0);
            }
        
            if (attribute.Contains("<-"))
            {
                GenerativeTree.Attributes[sides[0]] = EvaluateEquation(string.Join(" ", sides.Skip(2)));
            }
            else
            {
                var value = EvaluateEquation(string.Join(" ", sides));
                SetVariable(sides[0], value);
            }
        }
    
        private void SetVariable(string variable, int value)
        {
            if (GenerativeTree.Attributes.ContainsKey(variable)) GenerativeTree.Attributes[variable] = value;
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
            if (CheckForFunction(equation))
            {
                var function = ExtractFunction(equation);
                var result = HandleFunction(function);
                equation = equation.Replace(function, result);
            }
            var sb = new StringBuilder();
            var parts = equation.Trim().Split(" ");
            foreach (var part in parts)
            {
                if (_operands.ContainsKey(part)) sb.Append(_operands[part]);
                else sb.Append(HandleVariable(part));
            }
            
            var table = new DataTable();
            table.Columns.Add("", typeof(int));
            table.Columns[0].Expression = sb.ToString();
            var r = table.NewRow();
            table.Rows.Add(r);
            var equationResult = (int)r[0];
            return equationResult;
        }

        private string ExtractFunction(string equation)
        {
            var function = _functions.Where(f => 
                equation.IndexOf(f, StringComparison.Ordinal) > -1).ToList()[0];
            var startIndex = equation.IndexOf(function, StringComparison.Ordinal);
            var brackets = 1;
            var index = startIndex + function.Length + 1;
            while (brackets > 0)
            {
                switch (equation[index])
                {
                    case '(':
                        brackets += 1;
                        break;
                    case ')':
                        brackets -= 1;
                        break;
                }

                index += 1;
            }

            var endIndex = index - 1;
            return equation.Substring(startIndex, endIndex - startIndex + 1);
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
            condition = HandleImply(condition).ToLower();

            condition = condition.Replace("!", "not")
                .Replace("||", "or")
                .Replace("&&", "and")
                .Replace("==", "=");
            
            
            var table = new DataTable();
            table.Columns.Add("", typeof(bool));
            table.Columns[0].Expression = condition;
            var r = table.NewRow();
            table.Rows.Add(r);
            var conditionResult = (bool)r[0];
            return conditionResult;
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

            var index = 0;
            
            while (index < stringEqualityIndexes.Count)
            {
                string equalityResult;
                if (tokens[stringEqualityIndexes[index]].Equals("=="))
                {
                    equalityResult = tokens[stringEqualityIndexes[index] - 1].
                        Equals(tokens[stringEqualityIndexes[index] + 1]).ToString();
                }
                else
                {
                    equalityResult = (!tokens[stringEqualityIndexes[index] - 1].
                        Equals(tokens[stringEqualityIndexes[index] + 1])).ToString();
                }

                
                var toReplace = tokens[stringEqualityIndexes[index] - 1] + " " + 
                                tokens[stringEqualityIndexes[index]] + " " + tokens[stringEqualityIndexes[index] + 1];
                equation = equation.Replace(toReplace, equalityResult);
                index += 1;
            }
            return equation;
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
                condition = "not (" + tokens[0] + ") or (" + tokens[1] + ")";
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
                var list = (List<dynamic>) HandleVariable(tokens[index + 1], returnCompleteList: true);
                var endIndex = index + 1;

                var startBrackets = HandleConditionBrackets(tokens[startIndex])[0];
                var endBrackets = HandleConditionBrackets(tokens[endIndex])[1];

                string result;
                
                if (variable.GetType() == list.GetType().GetGenericArguments()[0]) 
                {
                    result = negativeFlag ?
                    (!list.Contains(variable)).ToString() : 
                    list.Contains(variable).ToString();
                }
                else
                {
                    result = LookForValueInObjectsList(variable, list, negativeFlag);
                }
                result = startBrackets + result + endBrackets;
            
                var tokensList = tokens.ToList();
                tokensList.RemoveRange(startIndex, endIndex - startIndex + 1);
                tokensList.Insert(startIndex, result);
            
                tokens = tokensList.ToArray();
            }

            return string.Join(" ", tokens);
        }

        private static string LookForValueInObjectsList(string variable, List<object> list, bool negativeFlag)
        {
            return (from listObject in list from property in listObject.GetType().GetProperties() 
                where property.GetValue(listObject)!.ToString()!.Equals(variable) 
                select listObject).Any() ? 
                (true ^ negativeFlag).ToString() : 
                (false ^ negativeFlag).ToString();
        }

        public object HandleVariable(string token, bool returnCompleteList = false)
        {
            var index = -1;
            dynamic result;
            token = CheckAndHandleFunction(token);
        
            token = CheckForIndex(token, ref index);
        
            var tokenStart = token.Split(".")[0].Trim();
        
            if (token.StartsWith("\"") || int.TryParse(token, out _) || bool.TryParse(token, out _))
            {
                result = token.Replace("\"", "");
            }
            else if (GenerativeTree.Parameters.Contains(tokenStart))
            {
                var sides = token.Split(".");
                dynamic field = LevelLog.GetType().GetProperty(sides[1])?.GetValue(LevelLog) ?? 
                                throw new InvalidOperationException();
                result = sides.Length == 3 ? field[sides[2]] : field;
            }
            else if (GenerativeTree.Attributes.ContainsKey(token))
            {
                result = GenerativeTree.Attributes[token];
            }
            else if (Npcs[^1].ValuesOfNodes.ContainsKey(token))
            {
                var resultReturned = new List<dynamic>();
                var visited = new List<string>();
                Npcs[^1].GetNodesTerminalValues(token, ref resultReturned, ref visited);
                result = resultReturned;
            }
            else if (Npcs[^1].ValuesOfNodes.ContainsKey(tokenStart))
            {
                var tokenSides = token.Split(".");
                var propertyName = tokenSides[1].ToLower();
                List<object> nodesValues = Npcs[^1].ValuesOfNodes[tokenStart];
                var resultedValue = nodesValues
                    .Select(n => n.GetType().GetProperties().ToList()
                    .Find(p => p.Name.ToLower().Contains(propertyName))!
                    .GetValue(n)).ToList()[^1];
                result = resultedValue;
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

        private bool CheckForFunction(string line)
        {
            return _functions.Any(e => line.Contains(e));
        }
    
        private string CheckAndHandleFunction(string token)
        {
            token = _functions.Any(e => token.Contains(e)) ? 
                HandleFunction(token) : token.Replace("(", "").Replace(")", "");

            return token;
        }

        private string HandleFunction(string token)
        {
            var sides = token.Split('(');
            var function = sides[0].Trim();
            var variables = sides[1].Replace(")", "").Trim().Split(",");
            var results = new List<dynamic>();
            foreach (var v in variables) 
            {
                dynamic variable = _operands.Keys.Any(token.Contains) ? 
                    EvaluateEquation(v) : 
                    HandleVariable(v, returnCompleteList: true)!;
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
                "MAX" => Convert.ToString(results.Max()),
                "MIN" => Convert.ToString(results.Min()),
                "SIZE" => Convert.ToString(results.Count),
                "DISTINCT" => Convert.ToString(results.Distinct().Count() == results.Count),
                "TYPE.DamageTaken" => HandleDamageTakenFunction(results),
                "SUM" => HandleSumFunction(results, token),
                _ => "-1"
            };
        }

        private string HandleSumFunction(List<object> variable, string token)
        {
            if (!token.Contains(',')) return variable.Sum(v => (int) v).ToString();
            var sumCriterion = variable[^1].ToString();
            variable.RemoveAt(variable.Count - 1);
            var sum = variable.Sum(v => (int) v.GetType()
                .GetProperties()
                .ToList()
                .Find(p => p.Name.ToLower().Equals(sumCriterion?.ToLower()))!
                .GetValue(v)!);
            return sum.ToString();
        }

        private string HandleDamageTakenFunction(IEnumerable<dynamic> variables)
        {
            var type = new JSONReader().ReadTypeChartJson().
                Find(e => e.Name.Equals(Npcs[^1].ValuesOfNodes["TYPE"][^1].Name))!;
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
}