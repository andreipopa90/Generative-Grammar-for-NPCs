namespace GenerativeGrammar.Model;

public class NPC
{
    public Dictionary<string, int> Attributes { get; set; }

    public Dictionary<string, List<dynamic>> ValuesOfNodes { get; set; }

    public NPC()
    {
        Attributes = new Dictionary<string, int>();
        ValuesOfNodes = new Dictionary<string, List<dynamic>>();
    }
}