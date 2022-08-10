using GenerativeGrammar.Handlers;
using GenerativeGrammar.JsonParser;
using GenerativeGrammar.Model;

namespace GenerativeGrammar.Grammar;

public class Generator
{
    public List<Npc> Npcs { get; }
    private Tree GenerativeTree { get; }
    private Log LevelLog { get; }
    private ExpressionHandler? Handler { get; }
    private WeightedListPicker Picker { get; }

    public Generator(Tree generativeTree, Log levelLog)
    {
        GenerativeTree = generativeTree;
        Npcs = new List<Npc>();
        LevelLog = levelLog;
        Handler = ExpressionHandler.GetInstance();
        Handler.Npcs = Npcs;
        Handler.GenerativeTree = GenerativeTree;
        Handler.LevelLog = LevelLog;
        Picker = new WeightedListPicker(Handler);
    }

    public void StartGeneration()
    {
        var root = GenerativeTree.Root;
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
        Console.WriteLine(node.Name);
        if (node.IsTerminalNode && string.IsNullOrEmpty(node.Source))
        {
            HandleTerminalNode(node);
        }
        else if (node.ActualNeighbours.Count == 0 && string.IsNullOrEmpty(node.Source))
        {
            DetermineActualNeighbours(node);
        }
        else if (!string.IsNullOrEmpty(node.Source))
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
            return node.Conditions.
                Aggregate(true, (current, condition) => current && Handler.HandleCondition(condition));
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
            RemoveOnTrial(node, trial);
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
            
            AddNodeToNpc(node, value);
            trial++;
        } while (!CheckNodeCondition(node));
        
    }

    private void AddNodeToNpc(Node node, dynamic value)
    {
        if (!Npcs[^1].ValuesOfNodes.ContainsKey(node.Name))
            Npcs[^1].ValuesOfNodes.Add(node.Name, new List<dynamic>());
        Npcs[^1].ValuesOfNodes[node.Name].Add(value.ToString());
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
            foreach (var sides in neighbours.Select(neighbour => neighbour.Split(" : ", 2)))
            {
                HandleNpcCreation(node);
                if (sides.Length == 2)
                {
                    SetAttributesFromNode(sides);
                }

                var nextNode = GenerativeTree.Nodes.Find(e => e.Name.Equals(sides[0].Trim()));
                AddNodeToNpc(node, nextNode, sides[0]);

                GenerateFromNode(nextNode!);
            }

            trial++;
        } while (!CheckNodeCondition(node));
    }

    private void AddNodeToNpc(Node node, Node? nextNode, string terminalValue)
    {
        if (!Npcs[^1].ValuesOfNodes.ContainsKey(node.Name))
            Npcs[^1].ValuesOfNodes.Add(node.Name, new List<dynamic>());
        if (nextNode is null)
        {
            Npcs[^1].ValuesOfNodes[node.Name].Add(terminalValue);
            return;
        }


        if (!Npcs[^1].ValuesOfNodes.ContainsKey(nextNode.Name))
        {
            Npcs[^1].ValuesOfNodes[node.Name].Add(nextNode.Name);
        }
    }

    private void HandleSourceNode(Node node)
    {
        
        var reader = new JSONReader();
        var file = node.Source.Split(".")[0].Trim();
        var method = reader.GetType().GetMethods().ToList().Find(e => e.Name.ToLower().Contains(file));

        dynamic values = method!.Invoke(reader, null) ?? throw new InvalidOperationException();
        var result = new Dictionary<object, int>();
        for (var i = 0; i < values.Count; i++)
        {
            result.Add(values[i], 1);
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
            RemoveOnTrial(node, trial);
            var pickedNode = Picker.GetObjectFromWeightedList(result);
            pickedNodeName = pickedNode.GetType().GetProperty("Name")?.GetValue(pickedNode)!.ToString() ??
                                 throw new InvalidOperationException();
            AddNodeToNpc(node, pickedNodeName);
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
            result = FilterObjectsFromSource(result, neighbourNode);
        }

        var trial = 1;
        do
        {
            var neighbours = neighbourNodes.Where(e => e != null).Select(e => e!.Name).ToList();
            if (trial > 1)
            {
                Npcs[^1].ValuesOfNodes[node.Name].RemoveAll(e => neighbours.Contains(e));
                foreach (var neighbourNode in neighbourNodes.Where(neighbourNode => neighbourNode != null))
                {
                    Npcs[^1].ValuesOfNodes[node.Name].Remove(neighbourNode!.Name);
                    Npcs[^1].ValuesOfNodes.Remove(neighbourNode.Name);
                }
            }
            var objectPicked = Picker.GetObjectFromWeightedList(result);
            result.Remove(objectPicked);
            foreach (var neighbourNode in neighbourNodes.Where(neighbourNode => neighbourNode != null))
            {
                
                var objectPropertyValue = objectPicked.GetType().GetProperties().ToList()
                        .Find(p => p.Name.ToLower().Contains(neighbourNode!.Name.ToLower()))!
                        .GetValue(objectPicked);
                AddNodeToNpc(node, neighbourNode!.Name);
                AddNodeToNpc(neighbourNode, objectPropertyValue ?? throw new InvalidOperationException());
            }

            trial++;
        } while (!CheckNodeCondition(node));
    }

    private Dictionary<object, int> FilterObjectsFromSource(Dictionary<object, int> result, Node? neighbourNode)
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
        do
        {
            RemoveOnTrial(node, trial); 
            var nextNode = Picker.GetWeightedTerminalNode(node);

            sides = nextNode.Split(" : ", 2);

            neighbours = sides[0].Trim().Split(" ~ ");


            foreach (var neighbour in neighbours)
            {
                var next = GenerativeTree.Nodes.Find(e => e.Name.Equals(neighbour));
                AddNodeToNpc(node, next, neighbour);
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

    private void RemoveOnTrial(Node node, int trial)
    {
        if (trial > 1)
        {
            Npcs[^1].ValuesOfNodes[node.Name].Remove(^1);
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