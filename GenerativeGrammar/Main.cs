// See https://aka.ms/new-console-template for more information

using System.Text;
using GenerativeGrammar.Grammar;
using GenerativeGrammar.JsonParser;
using GenerativeGrammar.Model;

void PrintLogs(ref Log levelLog)
{
    var result = new StringBuilder();
    result.Append("Level: ").Append(levelLog.CurrentLevel).AppendLine();
    result.Append("Player Attributes").AppendLine();
    result.Append("Types: ").Append(string.Join(", ", levelLog.PlayerTypes)).AppendLine();
    result.Append("HP: ").Append(levelLog.PlayerStats["HP"]).AppendLine();
    result.Append("ATK: ").Append(levelLog.PlayerStats["ATK"]).AppendLine();
    result.Append("DEF: ").Append(levelLog.PlayerStats["DEF"]).AppendLine();
    result.Append("SPA: ").Append(levelLog.PlayerStats["SPA"]).AppendLine();
    result.Append("SPD: ").Append(levelLog.PlayerStats["SPD"]).AppendLine();
    result.Append("SPE: ").Append(levelLog.PlayerStats["SPE"]).AppendLine();
    result.Append("Main Defence Type: ").Append(levelLog.PlayerDefense).AppendLine();
    result.Append("Main Attack Type: ").Append(levelLog.PlayerAttack).AppendLine();
    result.Append("Inflicted Status Ailments: ").Append(levelLog.HasAilments ? "Yes" : "No").AppendLine();

    result.Append("Enemies Attributes").AppendLine();
    result.Append("HP: ").Append(string.Join(", ", levelLog.EnemyStats["HP"])).AppendLine();
    result.Append("ATK: ").Append(string.Join(", ", levelLog.EnemyStats["ATK"])).AppendLine();
    result.Append("DEF: ").Append(string.Join(", ", levelLog.EnemyStats["DEF"])).AppendLine();
    result.Append("SPA: ").Append(string.Join(", ", levelLog.EnemyStats["SPA"])).AppendLine();
    result.Append("SPD: ").Append(string.Join(", ", levelLog.EnemyStats["SPD"])).AppendLine();
    result.Append("SPE: ").Append(string.Join(", ", levelLog.EnemyStats["SPE"])).AppendLine();

    Console.WriteLine(result);
}

void NpcsToUnits(ref Generator generator)
{
    var enemiesBase = new List<BaseStat>();
    var enemiesNature = new List<Nature>();
    var enemiesMoves = new List<List<Move>>();
    var enemiesAffixes = new List<List<string>>();
    var enemiesEvs = new List<Dictionary<string, int>>();
    var enemiesTypes = new List<List<Type>>();
    foreach (var npc in generator.Npcs.Select(npcStructure => npcStructure.ValuesOfNodes))
    {
        enemiesBase.Add(npc["BASE"][0]);
        enemiesNature.Add(npc["NATURE"][0]);
        var affixes = new List<string>();
        if (npc.TryGetValue("AFFIX", out var value))
        {
            affixes = value.Cast<string>().ToList();
        }

        enemiesAffixes.Add(affixes);
        enemiesMoves.Add(npc["MOVE"].Cast<Move>().ToList());
        var evs = new Dictionary<string, int>()
        {
            {"hp", npc["HPEV"][0]},
            {"atk", npc["ATKEV"][0]},
            {"def", npc["DEFEV"][0]},
            {"spa", npc["SPAEV"][0]},
            {"spd", npc["SPDEV"][0]},
            {"spe", npc["SPEEV"][0]}
        };
        enemiesEvs.Add(evs);
        enemiesTypes.Add(npc["TYPE"].Cast<Type>().ToList());
    }

    PrintEnemies(ref enemiesBase, ref enemiesNature, ref enemiesMoves, ref enemiesAffixes, ref enemiesEvs, 
        ref enemiesTypes);
}

void PrintEnemies(ref List<BaseStat> baseStats, ref List<Nature> natures, ref List<List<Move>> moves, 
    ref List<List<string>> affixes, ref List<Dictionary<string, int>> evs, ref List<List<Type>> types)
{
    for (var i = 0; i < baseStats.Count; i++)
    {
        var enemyData = new StringBuilder();
        enemyData.Append("Name: ").Append(baseStats[i].Name).AppendLine();
        enemyData.Append("TYPES: ").Append(string.Join(", ", types[i].Select(t => t.Name).ToList())).AppendLine();
        enemyData.Append("Nature: ").Append(natures[i].Name).Append(" (").Append(natures[i].Plus).Append(", ").
            Append(natures[i].Minus).Append(')').AppendLine();
        enemyData.Append("HP: ").Append(baseStats[i].Hp).Append(" + ").Append(evs[i]["hp"]).
            Append(" (EV Value)").AppendLine();
        enemyData.Append("ATK: ").Append(baseStats[i].Atk).Append(" + ").Append(evs[i]["atk"]).Append(" (EV Value)").
            AppendLine();
        enemyData.Append("DEF: ").Append(baseStats[i].Def).Append(" + ").Append(evs[i]["def"]).Append(" (EV Value)").
            AppendLine();
        enemyData.Append("SPA: ").Append(baseStats[i].Spa).Append(" + ").Append(evs[i]["spa"]).Append(" (EV Value)").
            AppendLine();
        enemyData.Append("SPD: ").Append(baseStats[i].Spd).Append(" + ").Append(evs[i]["spd"]).Append(" (EV Value)").
            AppendLine();
        enemyData.Append("SPE: ").Append(baseStats[i].Spe).Append(" + ").Append(evs[i]["spe"]).Append(" (EV Value)").
            AppendLine();
        enemyData.Append("Moves: ").Append(string.Join(", ", moves[i].Select(m => m.Name).ToList())).AppendLine();
        enemyData.Append("Moves Category: ").Append(string.Join(", ", moves[i].Select(m => m.Category).ToList())).AppendLine();
        enemyData.Append("Moves Power: ").Append(string.Join(", ", moves[i].Select(m => m.BasePower).ToList())).AppendLine();
        enemyData.Append("Moves Target: ").Append(string.Join(", ", moves[i].Select(m => m.Target).ToList())).AppendLine();
        enemyData.Append("Moves Type: ").Append(string.Join(", ", moves[i].Select(m => m.MoveType).ToList())).AppendLine();
        enemyData.Append("Affixes: ").Append(string.Join(", ", affixes[i])).AppendLine();
        Console.WriteLine(enemyData);
    }
}

void AddPlayerStats(BaseStat playerStats, ref Log levelLog)
{
    levelLog.PlayerStats["HP"] = playerStats.Hp;
    levelLog.PlayerStats["ATK"] = playerStats.Atk;
    levelLog.PlayerStats["DEF"] = playerStats.Def;
    levelLog.PlayerStats["SPA"] = playerStats.Spa;
    levelLog.PlayerStats["SPD"] = playerStats.Spd;
    levelLog.PlayerStats["SPE"] = playerStats.Spe;
}

void AddEnemyStats(List<BaseStat> enemyStats, ref Log levelLog)
{
    foreach (var enemyStat in enemyStats)
    {
        levelLog.EnemyStats["HP"].Add(enemyStat.Hp);
        levelLog.EnemyStats["ATK"].Add(enemyStat.Atk);
        levelLog.EnemyStats["DEF"].Add(enemyStat.Def);
        levelLog.EnemyStats["SPA"].Add(enemyStat.Spa);
        levelLog.EnemyStats["SPD"].Add(enemyStat.Spd);
        levelLog.EnemyStats["SPE"].Add(enemyStat.Spe);
    }
}

void ScenarioOne(ref JsonReader reader, ref List<BaseStat> baseStats)
{

    var monster = reader.ReadBaseStatsJson().Find(b => b.KeyName.Equals("blaziken")) ?? null;
    var levelLogs = Log.GetInstance();
    levelLogs.CurrentLevel = 1;
    if (monster == null) return;
    AddPlayerStats(monster, ref levelLogs);
    levelLogs.PlayerTypes = monster.Types;


    var enemiesList = new List<BaseStat>();
    for (var i = 0; i < 2; i++)
    {
        var randomNumber = new Random().
            Next(baseStats.Count * (levelLogs.CurrentLevel - 1) / 10, baseStats.Count * levelLogs.CurrentLevel / 10);
        if (baseStats[randomNumber].Clone() is BaseStat enemyBase) 
            enemiesList.Add(enemyBase);
    }
    AddEnemyStats(enemiesList, ref levelLogs);
    levelLogs.HasAilments = false;
    levelLogs.PlayerDefense = monster.Def > monster.Spd ? "Physical" : "Special";
    levelLogs.PlayerAttack = monster.Atk > monster.Spa ? "Physical" : "Special";
    PrintLogs(ref levelLogs);
    var generator = new Generator();
    generator.StartGeneration(levelLogs);
    NpcsToUnits(ref generator);
}

void Main()
{
    var reader = new JsonReader();
    var baseStats = reader.ReadBaseStatsJson();
    ScenarioOne(ref reader, ref baseStats);
}

Main();