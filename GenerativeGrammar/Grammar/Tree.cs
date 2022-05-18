namespace GenerativeGrammar.Grammar;

public struct Tree
{
    public string Name { get; set; }
    public List<Node> Nodes { get; init; }
    public Dictionary<string, int> GlobalVariables { get; set; }

    public override string ToString()
    {
        var result = "";
        foreach (var node in Nodes)
        {
            result += node.Name + "[";
            foreach (var n in node.PossibleNeighbours)
            {
                result += n + ", ";
            }

            result = result.Remove(result.Length - 2);
            result += "] A[";
            if (node.ActualNeighbours.Count > 0)
            {
                foreach (var n in node.ActualNeighbours)
                {
                    result += n.Trim() + ", ";
                }
            }
            result = result.Remove(result.Length - 2);
            result += "]\n";
        }

        return result;
    }
}