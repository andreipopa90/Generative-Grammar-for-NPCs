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
			string[] lines = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"", "Grammar", "Grammar.txt"));
			Console.Write(lines);
		}

		static void Main()
		{
			Parser parser = new();
			parser.ReadGrammarFile();
			Console.WriteLine("Hello, world!");
		}
	}
}
