using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Web_Works
{
    class Program
    {
        private static int completed = 0;
        private static int failed = 0;
        private static int success = 0;
        private static int total = 0;
        private static int counter = 1;
        private static string output = "";
        private static object locker = new object();
        private static int[] positions = null;
        private static int maxDegree = 8;
        private static int seconds = 0;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                HashSet<string> items = new HashSet<string>();

                foreach (string arg in args)
                {
                    if (Uri.IsWellFormedUriString(arg, UriKind.Absolute))
                    {
                        items.Add(arg);
                    }
                }

                BeginDownload(items);
                
                Environment.Exit(0);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;

            string help = "Commands:\n" +
            "[help]         - Displays a list of commands.\n" +
            "[delay]        - Sets delay in seconds between each download.\n" +
            "[exit]         - Closes the application.\n" +
            "[add]          - Adds URL to list.\n" +
            "[list]         - Displays all URL entries.\n" +
            "[clear]        - Removes all URL entries.\n" +
            "[path]         - Sets the output path for downloads.\n" +
            "[degree]       - Sets the maximum number of parallelism.";

            Console.Title = "Web Works 1.0";

            List<string> title = new List<string>
            {
                @"                                                       ",
                @" __          __  _      __          __        _        ",
                @" \ \        / / | |     \ \        / /       | |       ",
                @"  \ \  /\  / /__| |__    \ \  /\  / /__  _ __| | _____ ",
                @"   \ \/  \/ / _ \ '_ \    \ \/  \/ / _ \| '__| |/ / __|",
                @"    \  /\  /  __/ |_) |    \  /\  / (_) | |  |   <\__ \",
                @"     \/  \/ \___|_.__/      \/  \/ \___/|_|  |_|\_\___/",
                @"                                                       ",
                @"-------------------------------------------------------",
                @"                                                       ",
                @"     < A website downloading tool by Terry Zheng. >    ",
                @"                                                       ",
                @"-------------------------------------------------------"
            };

            foreach (string line in title)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine();

            HashSet<string> URLs = new HashSet<string>();

            while (true)
            {
                Console.Write("> ");

                string[] command = Console.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                switch (command[0])
                {
                    case "exit":
                        if (!AssertArguments(command, 1))
                        {
                            break;
                        }

                        Environment.Exit(0);
                        break;

                    case "help":
                        if (!AssertArguments(command, 1))
                        {
                            break;
                        }

                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine(help);
                        Console.WriteLine();
                        Console.WriteLine();
                        break;

                    case "start":
                        if (!AssertArguments(command, 1))
                        {
                            break;
                        }

                        if (URLs.Count == 0)
                        {
                            LogError("Error: No URLs in list.");
                            break;
                        }

                        completed = 0;
                        success = 0;
                        failed = 0;
                        total = URLs.Count;
                        counter = 1;
                        positions = new int[1 + total];

                        Console.WriteLine();
                        Console.WriteLine();

                        positions[0] = Console.CursorTop + 3;
                        for (int i = 1; i < positions.Length; ++i)
                        {
                            positions[i] = positions[0] + i + 3;
                        }
 
                        Console.WriteLine("Operation Begin:     {0}", DateTime.Now.ToString());
                        Console.WriteLine("Total Downloads:     {0}", total);
                        Update();

                        BeginDownload(URLs);

                        Console.SetCursorPosition(0, positions.Last() + 1);
                        Console.WriteLine();
                        Console.WriteLine("Operation End:       {0}", DateTime.Now.ToString());
                        Console.WriteLine();
                        Console.WriteLine();
                        break;

                    case "add":
                        if (!AssertArguments(command, 2))
                        {
                            break;
                        }

                        if (URLs.Count > 999998)
                        {
                            LogError("Error: Too many URLs.");
                            break;
                        }

                        if (!Uri.IsWellFormedUriString(command[1], UriKind.Absolute))
                        {
                            LogError("Error: Invalid URL format.");
                            break;
                        }

                        URLs.Add(command[1]);
                        break;

                    case "clear":
                        if (!AssertArguments(command, 1))
                        {
                            break;
                        }

                        URLs.Clear();
                        break;

                    case "path":
                        string[] path = string.Join(" ", command).Split(new char[] { '"' }, StringSplitOptions.RemoveEmptyEntries);

                        if (path.Length != 2)
                        {
                            LogError("Error: Path must be surrounded with quotes.");
                            break;
                        }

                        if (!Directory.Exists(path[1]))
                        {
                            LogError("Error: Output folder does not exist.");
                            break;
                        }

                        output = path[1];

                        break;

                    case "list":
                        if (!AssertArguments(command, 1))
                        {
                            break;
                        }

                        Console.WriteLine();
                        Console.WriteLine();

                        if (URLs.Count == 0)
                        {
                            Console.WriteLine("<Empty>");
                            Console.WriteLine();
                            Console.WriteLine();
                            break;
                        }

                        foreach (string item in URLs)
                        {
                            Console.WriteLine(item);
                        }

                        Console.WriteLine();
                        Console.WriteLine();
                        break;

                    case "degree":
                        if (!AssertArguments(command, 2))
                        {
                            break;
                        }

                        int result = -1;
                        if (!int.TryParse(command[1], out result) || result < 1 || result > 20)
                        {
                            LogError("Error: Maximum degree must be a number between 1 and 20");
                            break;
                        }

                        maxDegree = result;
                        break;

                    case "delay":
                        if (!AssertArguments(command, 2))
                        {
                            break;
                        }

                        int delay = -1;
                        if (!int.TryParse(command[1], out delay) || delay < 0 || delay > 1000)
                        {
                            LogError("Error: Delay must be a number between 0 and 1000.");
                            break;
                        }

                        seconds = delay;
                        break;

                    default:
                        LogError("Error: Unknown command.");
                        break;
                }
            }
        }

        private static bool AssertArguments(string[] command, int number)
        {
            if (command.Length != number)
            {
                LogError("Error: Invalid number of arguments.");
                return false;
            }
            return true;
        }

        private static void BeginDownload(HashSet<string> items)
        {
            int number = 1;

            if (seconds > 0 || maxDegree == 1)
            {
                foreach (string location in items)
                {
                    Download(location, number++);

                    Thread.Sleep(seconds * 1000);
                }

                return;
            }

            Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = maxDegree }, i => {
                Download(i, number++);
            });
        }

        private static void LogError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ForegroundColor = ConsoleColor.Cyan;
        }

        private static void Update()
        {
            Console.SetCursorPosition(0, positions[0]);
            Console.WriteLine("Completed:           {0}", completed);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success:             {0}", success);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed:              {0}", failed);
            Console.ForegroundColor = ConsoleColor.Cyan;
        }

        private static void Download(string URL, int number)
        {
            try
            {
                lock (locker)
                {
                    Console.SetCursorPosition(0, positions[number]);
                    Console.WriteLine("URL {0} Status:{1}  {2} Bytes Fetched", number, "      ".Substring(0, 7 - number.ToString().Length), 0);
                }

                long total = 0;
                using (StreamReader reader = new StreamReader(HttpWebRequest.Create(URL).GetResponse().GetResponseStream()))
                {
                    Uri location = new Uri(URL);
                    string name = location.Host + ".html";

                    if (File.Exists(name))
                    {
                        name = string.Format("{0} - {1}.html", Path.GetFileNameWithoutExtension(name), counter);
                        Interlocked.Increment(ref counter);
                    }

                    if (!string.IsNullOrEmpty(output))
                    {
                        name = Path.Combine(output, name);
                    }

                    using (StreamWriter writer = new StreamWriter(name))
                    {
                        while (!reader.EndOfStream)
                        {
                            string current = reader.ReadLine();

                            total += current.Length;

                            lock (locker)
                            {
                                Console.SetCursorPosition(0, positions[number]);
                                Console.WriteLine("URL {0} Status:{1}  {2} Bytes Fetched", number, "      ".Substring(0, 7 - number.ToString().Length), total * 8);
                            }

                            writer.Write(current);
                        }
                    }
                }
                Interlocked.Increment(ref success);
            }
            catch
            {
                lock (locker)
                {
                    Console.SetCursorPosition(0, positions[number]);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("URL {0} Status:{1}  Failed                    ", number, "      ".Substring(0, 7 - number.ToString().Length));
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }

                Interlocked.Increment(ref failed);
            }
            finally
            {
                Interlocked.Increment(ref completed);
                
                lock(locker)
                {
                    Update();
                }
            }
        }
    }
}