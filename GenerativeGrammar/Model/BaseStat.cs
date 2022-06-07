using System;
using System.Collections.Generic;

[Serializable]
public class BaseStat
{
	public string Name { get; set; }
	public Dictionary<string, int> BaseStats { get; set; }
	public List<string> Types { get; set; }
}
