namespace GenerativeGrammar.Model;

public class Npc
{
    public BaseStat BASE { get; set; }
    public Dictionary<string, int> EVS { get; set; }
    public List<string> TYPES { get; set; }
    public Nature NATURE { get; set; }
    public List<Move> MOVES { get; set; }
    public List<string> AFFIXES { get; set; }
    public Dictionary<string, int> Attributes { get; set; }
    public Dictionary<string, List<string>> ValuesOfNodes { get; set; }

    public Npc()
    {
        BASE = new BaseStat();
        EVS = new Dictionary<string, int>();
        TYPES = new List<string>();
        NATURE = new Nature();
        MOVES = new List<Move>();
        AFFIXES = new List<string>();
        Attributes = new Dictionary<string, int>();
        ValuesOfNodes = new Dictionary<string, List<string>>();
    }
}