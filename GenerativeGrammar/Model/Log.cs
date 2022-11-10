namespace GenerativeGrammar.Model
{
    [Serializable]
    public class Log
    {
        private static Log _instance;
        public int CurrentLevel { get; set; }
        public Dictionary<string, List<int>> EnemyStats { get; set; }
        public Dictionary<string, int> PlayerStats { get; set; }
        public bool HasAilments { get; set; }
        public List<string> PlayerTypes { get; set; }
        public string PlayerDefense { get; set; }
        public string PlayerAttack { get; set; }
        public List<string> PlayerMovesUsed { get; set; }
        public static Log GetInstance()
        {
            if (_instance == null) _instance = new Log();
            return _instance;
        }

        private Log()
        {
            EnemyStats = new Dictionary<string, List<int>>
            {
                {"HP", new List<int>()},
                {"ATK", new List<int>()},
                {"DEF", new List<int>()},
                {"SPA", new List<int>()},
                {"SPD", new List<int>()},
                {"SPE", new List<int>()}
            };
            PlayerStats = new Dictionary<string, int>{
                {"HP", 0},
                {"ATK", 0},
                {"DEF", 0},
                {"SPA", 0},
                {"SPD", 0},
                {"SPE", 0}
            };
            HasAilments = false;
            PlayerTypes = new List<string>();
            PlayerDefense = string.Empty;
            PlayerAttack = string.Empty;
            PlayerMovesUsed = new List<string>();
        }
    }
}