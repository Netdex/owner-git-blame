using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace java_git_blame
{
    class Program
    {
        private const string BLOCK_STRING = "██";

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string cd = args.Length == 1 ? args[0] : Directory.GetCurrentDirectory();

            Console.WriteLine("=== GIT FILE OWNERSHIP (BLAME) ===");
            Console.WriteLine("Repository Location: " + cd + "\n");

            string[] javaFiles = (from item in Directory.GetFiles(cd, "*.*", SearchOption.AllDirectories) where !item.Contains(".git") select item).ToArray();
            Dictionary<string, int> totalContributions = new Dictionary<string, int>();
            int totalLines = 0;
            foreach (string javaFile in javaFiles)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($":: {javaFile}");
                ProcessStartInfo psi = new ProcessStartInfo("git",
                    $"-C \"{cd}\" blame \"{javaFile}\" --line-porcelain -w") // -M -C
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                Process p = Process.Start(psi);

                Dictionary<string, int> fileContributions = new Dictionary<string, int>();
                int fileTotalLines = 0;
                string line;
                while ((line = p.StandardOutput.ReadLine()) != null)
                {
                    if (line.StartsWith("author "))
                    {
                        string user = line.Substring(7);
                        if (!fileContributions.ContainsKey(user))
                            fileContributions[user] = 0;
                        fileContributions[user]++;
                        fileTotalLines++;
                        totalLines++;
                    }
                }

                List<KeyValuePair<string, int>> sortedList = fileContributions.ToList();
                sortedList.Sort((firstPair, nextPair) => nextPair.Value.CompareTo(firstPair.Value));

                foreach (KeyValuePair<string, int> kvp in sortedList)
                {
                    string owner = kvp.Key;
                    if (!totalContributions.ContainsKey(owner))
                        totalContributions[owner] = 0;
                    totalContributions[owner] += fileContributions[owner];

                    double perc = 1.0 * fileContributions[owner] / fileTotalLines;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"{owner,20}: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{perc:P}");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\t{fileContributions[owner] + " line(s)",10}");
                }

                string err = p.StandardError.ReadToEnd();
                if (err.Contains("fatal"))
                {
                    Console.ResetColor();
                    Console.WriteLine("Failed to get git-blame data for current file.");
                    Console.WriteLine(err);
                    return;
                }
            }

            List<KeyValuePair<string, int>> sortedTotalList = totalContributions.ToList();
            sortedTotalList.Sort((firstPair, nextPair) => nextPair.Value.CompareTo(firstPair.Value));

            Console.ResetColor();
            ConsoleColor[] colorPalette = ((ConsoleColor[])Enum.GetValues(typeof(ConsoleColor))).Reverse().ToArray();
            Console.WriteLine("\nTotal Contributions:");
            for (int i = 0; i < sortedTotalList.Count; i++)
            {
                string user = sortedTotalList[i].Key;
                double perc = 1.0 * totalContributions[user] / totalLines;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{user,20}: ");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{perc:P}");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"\t{totalContributions[user] + " line(s)",10}\t");

                Console.ForegroundColor = colorPalette[i];
                Console.BackgroundColor = colorPalette[i];
                Console.Write(BLOCK_STRING);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine("X");
            }
            DrawASCIIPie(
                colorPalette,
                (from item in totalContributions.Values.ToArray() select 1.0 * item / totalLines).ToArray(),
                11, Console.WindowWidth - 50, Math.Max(0, Console.CursorTop - 22));
            Console.ResetColor();
        }

        public static void DrawASCIIPie(ConsoleColor[] colors, double[] perc, int radius, int cx, int cy)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    Console.CursorLeft = cx + radius * 2 + x * 2;
                    Console.CursorTop = cy + radius + y;
                    if (x * x + y * y < radius * radius)
                    {
                        double a = Math.Atan2(y, x) / Math.PI / 2 + .45;
                        Console.ForegroundColor = S(colors, perc, a, 0);
                        Console.BackgroundColor = Console.ForegroundColor;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    Console.Write(BLOCK_STRING);
                }
                Console.WriteLine();
            }
            Console.ResetColor();
        }

        public static ConsoleColor S(ConsoleColor[] k, double[] v, double a, int incdx)
        {
            while (true)
            {
                if (v.Length == incdx)
                    return ConsoleColor.Black;
                if (a < v[incdx])
                    return k[incdx];
                var v1 = v;
                a = a - v1[incdx];
                incdx = incdx + 1;
            }
        }
    }
}
