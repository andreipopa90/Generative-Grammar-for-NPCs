using GenerativeGrammar.Handlers;
using GenerativeGrammar.JsonParser;
using GenerativeGrammar.Model;
using Random = System.Random;
using Tree = GenerativeGrammar.Model.Tree;

namespace GenerativeGrammar.Grammar
{
    public class Generator
    {
        private readonly string _filePath =
            Path.Combine(@"..", "..", "..", "Grammar", "Grammar.txt");
        public List<Npc> Npcs { get; set; }
        private Tree GenerativeTree { get; set; }
        private Log LevelLog { get; set; }
        private ExpressionHandler Handler { get; set; }
        private WeightedListPicker Picker { get; set; }

        public void StartGeneration(Log levelLog)
        {
            Npcs = new List<Npc>();
            LevelLog = levelLog;
            Handler = ExpressionHandler.GetInstance();
            Picker = new WeightedListPicker(Handler);
            var parser = new Parser(levelLog: LevelLog);
            GenerativeTree = parser.HandleLines(parser.ReadGrammarFile(_filePath).ToList());
            var root = GenerativeTree.Root;
            Handler.Npcs = Npcs;
            Handler.LevelLog = LevelLog;
            Handler.GenerativeTree = GenerativeTree;
            GenerateFromNode(root);
        }

        /**
     * <summary>
     * There are 4 types of node:
     * - Those that have to pick the neighbours
     * - Those that already have neighbours
     * - Terminal nodes
     * - Nodes that use a source
     * </summary>
     */
        private void GenerateFromNode(Node node)
        {
            if (node is {IsTerminalNode: true, IsSourceNode: false})
            {
                HandleTerminalNode(node);
            }
            else if (node.ActualNeighbours.Count == 0 && !node.IsSourceNode)
            {
                DetermineActualNeighbours(node);
            }
            else if (node.IsSourceNode)
            {
                HandleSourceNode(node);
            }
            else
            {
                HandleNodeWithNeighbours(node);
            }

        }

        private bool CheckNodeCondition(Node node)
        {
            if (node.Conditions.Count == 0) return true;
            try
            {
                var result = true;
                foreach (var condition in node.Conditions)
                {
                    result = result && Handler.HandleCondition(condition);
                }

                return result;
            }
            catch (ArgumentOutOfRangeException)
            {
                return true;
            }
        }
    
        private void HandleTerminalNode(Node node)
        {
            var trial = 1;
            do
            {
                // RemoveOnTrial(node, trial);
                if (trial > 1)
                {
                    Npcs[^1].ValuesOfNodes[node.Name].Remove(^1);
                }
                dynamic value = null!;

                if (node.ActualNeighbours.Count == 1)
                {
                    value = GetValueFromRange(node);
                }
                else if (!node.ActualNeighbours.Any())
                {
                    value = Picker.GetWeightedTerminalNode(node);
                    value = HandleNodeAttributes(value);

                    value = ((string) value).StartsWith("\"") ? ((string) value).Replace("\"", "") : value;
                }
            
                // AddNodeToNpc(node, value);
                if (!Npcs[^1].ValuesOfNodes.ContainsKey(node.Name)) 
                    Npcs[^1].ValuesOfNodes.Add(node.Name, new List<dynamic>());
                Npcs[^1].ValuesOfNodes[node.Name].Clear();
                Npcs[^1].ValuesOfNodes[node.Name].Add(value);
                trial++;
            } while (!CheckNodeCondition(node));
        
        }

        private dynamic HandleNodeAttributes(dynamic value)
        {
            var sides = ((string) value).Split(" : ", 2);
            if (sides.Length != 2) return value;
            value = sides[0].Trim();
            var attributes = sides[1].Split(", ");
            foreach (var attribute in attributes)
            {
                Handler.HandleAttribute(attribute);
            }

            return value;
        }

        /**
        * <summary>
        * Assumption: Condition can be added only for the nodes whose next nodes are leafs
        * </summary>
        */
        private void HandleNodeWithNeighbours(Node node)
        {
            var trial = 1;
            do
            {
                var neighbours = node.ActualNeighbours;
                if (trial > 1)
                {
                    Npcs[^1].ValuesOfNodes[node.Name].RemoveAll(e => neighbours.Contains(e));
                    Npcs[^1].ValuesOfNodes =
                        Npcs[^1].ValuesOfNodes.Where(e => !neighbours.Contains(e.Key))
                            .ToDictionary(e => e.Key, e => e.Value);
                }
            
                foreach (var nextNode in neighbours.Select(neighbour => neighbour.Split(" : ", 2)[0]))
                {
                    var next = GenerativeTree.Nodes.Find(n => n.Name.Equals(nextNode));
                    if (next is null || !next.IsTerminalNode) continue;
                    if (!Npcs[^1].ValuesOfNodes.ContainsKey(next.Name))
                        Npcs[^1].ValuesOfNodes.Add(next.Name, new List<dynamic>());
                    Npcs[^1].ValuesOfNodes[next.Name].Add(0);
                }
            
                foreach (var sides in neighbours.Select(neighbour => neighbour.Split(" : ", 2)))
                {
                    HandleNpcCreation(node);
                    if (sides.Length == 2)
                    {
                        SetAttributesFromNode(sides);
                    }

                    var nextNode = GenerativeTree.Nodes.Find(e => e.Name.Equals(sides[0].Trim()));

                    if (!Npcs[^1].ValuesOfNodes.ContainsKey(node.Name)) 
                        Npcs[^1].ValuesOfNodes.Add(node.Name, new List<dynamic>());
                    Npcs[^1].ValuesOfNodes[node.Name].Add(sides[0].Trim());
                    
                    GenerateFromNode(nextNode!);
                }

                trial++;
            } while (!CheckNodeCondition(node));
        }

        private void HandleSourceNode(Node node)
        {
        
            var reader = new JsonReader();
            var file = node.Source.Split(".")[0].Trim();
            var method = reader.GetType().GetMethods().ToList().Find(e => e.Name.ToLower().Contains(file));

            dynamic values = method!.Invoke(reader, null) ?? throw new InvalidOperationException();
            var result = new Dictionary<object, int>();
            for (var i = 0; i < values.Count; i++)
            {
                result.Add(values[i], node.ActualNeighbours.Count > 0 ? 1 : 0);
            }

            if (node.ActualNeighbours.Count > 0)
            {
                HandleNonTerminalSourceNode(node, result);
            }
            else
            {
                HandleTerminalSourceNode(node, result);
            }

        }

        private void HandleTerminalSourceNode(Node node, Dictionary<object, int> result)
        {
            foreach (var possibleNeighbour in node.PossibleNeighbours)
            {
                var sides = possibleNeighbour.Split(" : ", 2)[0].Split(" ");
                var weight = Handler.EvaluateEquation(sides[0].Trim().Replace("[", "").Replace("]", ""));
                var neighbourName = sides[1].Replace("\"", "").Trim();
                result = result.ToDictionary(e => e.Key,
                    e => e.Key.GetType().GetProperty("Name")!.GetValue(e.Key)!.ToString()!.Equals(neighbourName)
                        ? e.Value + weight
                        : e.Value);
            }

            string pickedNodeName;
            var trial = 1;
            do
            {
                if (trial > 1)
                {
                    Npcs[^1].ValuesOfNodes[node.Name].RemoveAt(Npcs[^1].ValuesOfNodes[node.Name].Count - 1);
                }
                var pickedObject = Picker.GetObjectFromWeightedList(result);
                pickedNodeName = pickedObject.GetType().GetProperty("Name")?.GetValue(pickedObject)!.ToString() ??
                                 throw new InvalidOperationException();
                // AddNodeToNpc(node, pickedNodeName);
                if (!Npcs[^1].ValuesOfNodes.ContainsKey(node.Name))
                    Npcs[^1].ValuesOfNodes.Add(node.Name, new List<dynamic>());
                Npcs[^1].ValuesOfNodes[node.Name].Add(pickedObject);
                trial++;
            } while (!CheckNodeCondition(node));

            var nodeSides = node.PossibleNeighbours.Find(e =>
                e.Contains(pickedNodeName))!.Split(" : ");
        
            if (nodeSides.Length != 2) return;
        
            var attributes = nodeSides[1].Split(", ");
            foreach (var attribute in attributes)
            {
                Handler.HandleAttribute(attribute);
            }
        
        }

        private void HandleNonTerminalSourceNode(Node node, Dictionary<object, int> result)
        {
            var neighbourNodes = node.ActualNeighbours.Select(e => GenerativeTree.Nodes.Find(f => f.Name.Equals(e))).ToList();

            foreach (var neighbourNode in neighbourNodes.Where(neighbourNode => neighbourNode != null))
            {
                if (neighbourNode != null) 
                    result = FilterObjectsFromSource(result, neighbourNode);
            }

            var trial = 1;
            do
            {
                if (trial > 1)
                {
                    Npcs[^1].ValuesOfNodes[node.Name].RemoveAt(Npcs[^1].ValuesOfNodes[node.Name].Count - 1);
                }
                var objectPicked = Picker.GetObjectFromWeightedList(result);
                result.Remove(objectPicked);
                if (!Npcs[^1].ValuesOfNodes.ContainsKey(node.Name))
                    Npcs[^1].ValuesOfNodes.Add(node.Name, new List<dynamic>());
                Npcs[^1].ValuesOfNodes[node.Name].Add(objectPicked);

                trial++;
            } while (!CheckNodeCondition(node));
        }

        private Dictionary<object, int> FilterObjectsFromSource(Dictionary<object, int> result, Node neighbourNode)
        {
            switch (neighbourNode!.ActualNeighbours.Count)
            {
                case > 0 when neighbourNode.ActualNeighbours[0].Contains(".."):
                {
                    result = FilterNodesByRange(result, neighbourNode);
                    break;
                }
                case 0:
                {
                    var weights = GetWeightsOfValues(neighbourNode);

                    result = UpdateWeightsOfNodes(result, weights, neighbourNode);

                    break;
                }
            }

            return result;
        }

        private Dictionary<object, int> FilterNodesByRange(Dictionary<object, int> result, Node neighbourNode)
        {
            var range = GetRangesForValue(neighbourNode);
            result = result.Where(e =>
                    (int) e.Key.GetType().GetProperties().ToList()
                        .Find(f => f.Name.ToUpper().Contains(neighbourNode.Name))!.GetValue(e.Key)! >=
                    range[0] &&
                    (int) e.Key.GetType().GetProperties().ToList()
                        .Find(f => f.Name.ToUpper().Contains(neighbourNode.Name))!.GetValue(e.Key)! <=
                    range[1])
                .ToDictionary(e => e.Key, e => e.Value);
            return result;
        }

        private static Dictionary<object, int> UpdateWeightsOfNodes(Dictionary<object, int> result, Dictionary<string, int> weights, Node neighbourNode)
        {
            foreach (var weight in weights)
            {
                result = result.
                    ToDictionary(e => e.Key, 
                        e => e.Key.GetType().GetProperties().ToList()
                            .Find(p => p.Name.ToUpper().Contains(neighbourNode.Name))!.GetValue(e.Key)!
                            .Equals(weight.Key.Replace("\"", ""))
                            ? e.Value + weight.Value
                            : e.Value);
            }

            return result;
        }

        private Dictionary<string, int> GetWeightsOfValues(Node neighbourNode)
        {
            return neighbourNode.PossibleNeighbours.
                Select(value => value.Replace("[", "").Split("] ")).
                ToDictionary(sides => sides[1], sides => int.Parse(Handler.HandleVariable(sides[0])!.ToString()!));
        }

        private List<int> GetRangesForValue(Node neighbourNode)
        {
            var result = new List<int>();
            var values = neighbourNode.ActualNeighbours[0].Replace("[", "").Replace("]", "").Split("..");
            var min = int.Parse(Handler.EvaluateEquation(values[0]).ToString());
            var max = int.Parse(Handler.EvaluateEquation(values[1]).ToString());
            result.Add(min);
            result.Add(max);
            return result;
        }

        private void DetermineActualNeighbours(Node node)
        {
            string[] sides;
            string[] neighbours;
            var trial = 1;
            var amountAdded = 0;
            do
            {
                if (trial > 1)
                {
                    Npcs[^1].ValuesOfNodes[node.Name]
                        .RemoveRange(Npcs[^1].ValuesOfNodes[node.Name].Count - amountAdded, amountAdded);
                }
                // RemoveOnTrial(node, trial); 
                var nextNode = Picker.GetWeightedTerminalNode(node);

                sides = nextNode.Split(" : ", 2);

                neighbours = sides[0].Trim().Split(" ~ ");
                amountAdded = neighbours.Length;

                foreach (var neighbour in neighbours)
                {
                    var next = GenerativeTree.Nodes.Find(e => e.Name.Equals(neighbour));
                    if (!Npcs[^1].ValuesOfNodes.ContainsKey(node.Name))
                        Npcs[^1].ValuesOfNodes.Add(node.Name, new List<dynamic>());
                    if (next?.Name != null) Npcs[^1].ValuesOfNodes[node.Name].Add(next.Name);
                    // AddNodeToNpc(node, next, neighbour);
                }

                trial++;
            } while (!CheckNodeCondition(node));

            if (sides.Length == 2)
            {
                var attributes = sides[1].Trim().Split(", ");
                foreach (var attribute in attributes)
                {
                    Handler.HandleAttribute(attribute);
                }
            }

            foreach (var neighbour in neighbours)
            {
                var next = GenerativeTree.Nodes.Find(e => e.Name.Equals(neighbour));
                if (next is null) continue;
                GenerateFromNode(next);
            }
        
        }

        private void SetAttributesFromNode(IReadOnlyList<string> sides)
        {
            var attributes = sides[1].Split(", ");
            foreach (var attribute in attributes)
            {
                Handler.HandleAttribute(attribute.Trim());
            }
        }

        private void HandleNpcCreation(Node node)
        {
            if (!GenerativeTree.Root.Name.Equals(node.Name)) return;
        
            var npc = new Npc();
            Npcs.Add(npc);
        }

        /**
        * Parse Range for EVs
        */
        private int GetValueFromRange(Node node)
        {
            var range = node.PossibleNeighbours[0].Trim().Split("..");
            var minimum = Handler.EvaluateEquation(range[0].Replace("[", ""));
            var maximum = Handler.EvaluateEquation(range[1].Replace("]", ""));
            return new Random().Next(minimum, maximum + 1);
        }
    }
}