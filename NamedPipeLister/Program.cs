using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamedPipeLister
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			string[] prevPipes = null;

			while (true)
			{
				string[] namedPipes = Directory.GetFiles(@"\\.\pipe\").Where(p => p.StartsWith(@"\\.\pipe\Unity")).ToArray();
				if (prevPipes == null || !namedPipes.SequenceEqual(prevPipes))
				{
					foreach (string pipe in namedPipes)
						Console.WriteLine(pipe);
					Console.WriteLine();
					prevPipes = namedPipes;
				}

				await Task.Delay(500);
			}
		}
	}
}
