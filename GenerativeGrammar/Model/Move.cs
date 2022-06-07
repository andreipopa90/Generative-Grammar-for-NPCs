using System;

[Serializable]
public class Move
{
	public string Name { get; set; }
	public string Accuracy { get; set; }
	public int BasePower { get; set; }
	public string Category { get; set; }
	public string Target { get; set; }
	public string Type { get; set; }
}
