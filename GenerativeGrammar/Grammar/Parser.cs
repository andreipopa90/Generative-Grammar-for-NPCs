using System;
using System.IO;
using System.Reflection;

namespace GenerativeGrammar.Grammar
{
	public class Parser
	{
		public Parser()
		{
		}

		public void ReadGrammarFile()
		{
			string[] lines = File.ReadAllLines(Path.Combine(@"..", "..", "..", "Grammar", "Grammar.txt"));
			foreach (var line in lines)
			{
				Console.WriteLine(line);
			}
		}

		static void Main()
		{
			Parser parser = new();
			parser.ReadGrammarFile();
		}
	}
}
