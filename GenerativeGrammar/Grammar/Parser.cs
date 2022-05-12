using System;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;

namespace GenerativeGrammar.Grammar
{

	public struct Tree
	{
		public Node root;
		public string name;
	}

	public struct Node
	{
		public string name;
		public List<Node> neighbours;
	}
	
	public class Parser
	{
		private Tree generativeTree;
		public Parser()
		{
			generativeTree = new()
			{
				root = new Node()
			};
		}

		private string[] ReadGrammarFile()
		{
			string[] lines = File.ReadAllLines(Path.Combine(@"..", "..", "..", "Grammar", "Grammar.txt"));
			return lines;
		}

		private void handleLines(string[] lines)
		{
			foreach (var line in lines)
			{
				var sides = line.Split(":=");
				if (sides.Length == 2)
				{
					Node node = new Node();
					node.name = sides[0].Trim();
					List<Node> neighbours = new();
					foreach (var neighbour in sides[1].Trim().Split("|")[0].Split(" "))
					{
						Node n = new Node();
						n.name = neighbour.Trim();
						neighbours.Add(n);
					}
					node.neighbours = neighbours;
					foreach (var n in node.neighbours)
					{
						Console.WriteLine(n.name);
					}
					if (node.name.Contains("NPCS"))
					{
						generativeTree.root = node;
					}
				}
			}
		}

		private List<Node> findNodesWithName(string name)
		{
			Node node = generativeTree.root;
			
			return null;
		}

		static void Main()
		{
			Parser parser = new();
			var lines = parser.ReadGrammarFile();
			parser.handleLines(lines);
		}
	}
}
