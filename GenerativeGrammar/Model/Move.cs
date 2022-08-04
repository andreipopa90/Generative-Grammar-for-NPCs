using System;

[Serializable]
public class Move
{
	public string Name { get; set; }
	public int Accuracy { get; set; }
	public int BasePower { get; set; }
	public string Category { get; set; }
	public string Target { get; set; }
	public string MoveType { get; set; }
	
}
