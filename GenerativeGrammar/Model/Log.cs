namespace GenerativeGrammar.NPC;

public class Log
{
    public Dictionary<string, List<int>> EnemyStats { get; set; }
    public bool HasAilments { get; set; }

    public Log()
    {
        EnemyStats = new Dictionary<string, List<int>>();
        EnemyStats.Add("HP", new List<int>{1});
        EnemyStats.Add("ATK", new List<int>{1});
        EnemyStats.Add("DEF", new List<int>{1});
        EnemyStats.Add("SPA", new List<int>{1});
        EnemyStats.Add("SPD", new List<int>{1});
        EnemyStats.Add("SPE", new List<int>{1});
        HasAilments = false;
    }
}