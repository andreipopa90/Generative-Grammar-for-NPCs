using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class JSONReader
{

	private readonly string PathRoot = Path.Combine(@"..", "..", "..", "Model", "JsonParser");

	public List<Move> ReadMovesJSON()
	{
		List<Move> Moves;
		StreamReader reader = new(Path.Combine(PathRoot, "moves.json"));
		string json = reader.ReadToEnd();
		Moves = JsonConvert.DeserializeObject<List<Move>>(json);
		reader.Close();

		return Moves;
	}

	public List<BaseStat> ReadBaseStatsJSON()
	{
		List<BaseStat> BaseStats;
		StreamReader reader = new(Path.Combine(PathRoot, "pokedex.json"));
		string json = reader.ReadToEnd();
		BaseStats = JsonConvert.DeserializeObject<List<BaseStat>>(json);
		reader.Close();

		return BaseStats;
	}

	public List<Type> ReadTypeChart()
	{
		List<Type> TypeChart;
		StreamReader reader = new(Path.Combine(PathRoot, "typechart.json"));
		string json = reader.ReadToEnd();
		TypeChart = JsonConvert.DeserializeObject<List<Type>>(json);
		reader.Close();

		return TypeChart;
	}

	public List<Nature> ReadNatures()
	{
		List<Nature> Natures;
		StreamReader reader = new(Path.Combine(PathRoot, "natures.json"));
		string json = reader.ReadToEnd();
		Natures = JsonConvert.DeserializeObject<List<Nature>>(json);
		reader.Close();

		return Natures;
	}
}
