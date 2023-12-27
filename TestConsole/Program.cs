using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using TransmittalCreator.Services;

namespace TestConsole
{
	public class Params
	{
		public string Pc3 { get; set; }
		public string Pmp { get; set; }
		public string[] StampName { get; set; }
	}

	class Program
	{
		static void Main(string[] args)
		{

			string[] ams =
			{
				"salat", "kub", "ddewrwer", "atlas", "kub", "sdfvafasdf", "ubk", "edfwefwqerf", "dwer",
				"dwer", "dwer", "dwer", "dwer", "dwer", "buk", "atals", "bku"
			};




			var anagrams = ams.Select(s => new string(s.OrderBy(x => x).ToArray()));
			
			const string filepath = @"conf.json";

			// Чтение файла
			var json = File.ReadAllText(filepath);

			// Десериализация строки в объект
			
			var data = JsonSerializer.Deserialize<Params>(json);



			Console.WriteLine("Число анаграмм: " );
			Console.ReadKey();

			//Rectangle rec1 = new Rectangle("1",-109.4765, 3.5459, 2.5);
			//Rectangle rec2 = new Rectangle("2",-105.1077, 3.0005, 2.5);
			//Rectangle rec3 = new Rectangle("3",-100.3877, 3.11, 2.5);
			//Rectangle rec4 = new Rectangle("4",2796.555, 936.048, 2.5);
			//Rectangle rec5 = new Rectangle("5",3787.9363, 936.048, 2.5);

			//List<Rectangle> rectangles = new List<Rectangle>{rec1,rec2,rec3,rec4,rec5};

			//var sortedRec = rectangles.OrderByDescending(y => y.Y).ToList();



			//Circle figure = new Circle(30, 50, 60);
			//Circle clonedFigure=figure.Clone() as Circle;
			//Circle clonedFigure2=figure.DeepCopy() as Circle;
			//figure.Point.X = 100; // изменяем координаты начальной фигуры
			//figure.GetInfo(); // figure.Point.X = 100
			//clonedFigure.GetInfo(); // clonedFigure.Point.X = 100
			//clonedFigure2.GetInfo();

			//string dir = @"C:\Users\yusufzhon.marasulov\Desktop";
			//DirectoryInfo directoryInfo = new DirectoryInfo(dir);
			//string[] allfiles = Directory.GetFiles(dir, "*.pdf", SearchOption.AllDirectories);
			//string[] allDirs = Directory.GetDirectories(@"C:\Users\yusufzhon.marasulov\Desktop");

			//string tree = ScanFolder(directoryInfo);

			//Console.WriteLine("Done");
			//Console.ReadLine();
		}

		static bool isAnagram(string word1, string word2)
		{
			if (word1.Length != word2.Length)
			{
				return false;
			}
			else
			{
				for (int i = 0; i < word2.Length; i++)
				{
					int indexOfCommonLetter = word1.IndexOf(word2[i]);
					if (indexOfCommonLetter != -1) //it means that there's no common letters
					{
						word1 = word1.Remove(indexOfCommonLetter, 1);
					}
					else
					{
						return false;
					}
				}

				return true; //if program comes so far - there's only common letters
			}
		}

		static string ScanFolder(DirectoryInfo directory, string indentation = "\t", int maxLevel = -1, int deep = 0)
		{
			StringBuilder builder = new StringBuilder();

			builder.AppendLine(string.Concat(Enumerable.Repeat(indentation, deep)) + directory.Name);

			if (maxLevel == -1 || maxLevel < deep)
			{
				foreach (var subdirectory in directory.GetDirectories())
					builder.Append(ScanFolder(subdirectory, indentation, maxLevel, deep + 1));
			}

			foreach (var file in directory.GetFiles())
				builder.AppendLine(string.Concat(Enumerable.Repeat(indentation, deep + 1)) + file.Name);

			return builder.ToString();
		}


		public static void DirectorySearch(string dir)
		{
			try
			{
				foreach (string f in Directory.GetFiles(dir))
				{
					Console.WriteLine(Path.GetFileName(f));
				}
				foreach (string d in Directory.GetDirectories(dir))
				{
					Console.WriteLine(Path.GetFileName(d));
					DirectorySearch(d);
				}
			}
			catch (System.Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
		
		public static double ConvertToDouble(string Value)
		{
			if (Value == null)
			{
				return 0;
			}
			else
			{
				double OutVal;
				double.TryParse(Value, out OutVal);

				if (double.IsNaN(OutVal) || double.IsInfinity(OutVal))
				{
					return 0;
				}
				return OutVal;
			}
		}

		public static string GetWithIn(string str)
		{
			string rez = "";

			Regex pattern =
				new Regex(@"\d{1,}?\.[00]{1,2}",
					RegexOptions.Compiled |
					RegexOptions.Singleline);

			foreach (Match m in pattern.Matches(str))
				if (m.Success)
				{
					rez = m.Groups["val"].Value;

				}

			return rez;
		}
	}

	
}
