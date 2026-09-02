using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace EmmuRpc
{
    internal static class GameNameCatalog
    {
        private const string ResourceName = "EmmuRpc.Resources.GameNames.txt";
        private static readonly List<string> Names = new List<string>();
        private static bool _loaded;

        public static int Count
        {
            get { return Names.Count; }
        }

        public static void Load()
        {
            if (_loaded)
                return;

            lock (Names)
            {
                if (_loaded)
                    return;

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                {
                    if (stream == null)
                        throw new InvalidOperationException("The embedded game-name catalog is missing.");

                    HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string name = line.Trim();
                            if (name.Length > 0 && unique.Add(name))
                                Names.Add(name);
                        }
                    }
                }

                _loaded = true;
            }
        }

        public static List<string> Search(string query, int limit)
        {
            Load();
            if (limit < 1)
                limit = 1;

            string needle = (query ?? String.Empty).Trim();
            List<string> matches = new List<string>(limit);

            if (needle.Length == 0)
            {
                for (int i = 0; i < Names.Count && matches.Count < limit; i++)
                    matches.Add(Names[i]);
                return matches;
            }

            for (int i = 0; i < Names.Count && matches.Count < limit; i++)
            {
                if (Names[i].StartsWith(needle, StringComparison.OrdinalIgnoreCase))
                    matches.Add(Names[i]);
            }

            for (int i = 0; i < Names.Count && matches.Count < limit; i++)
            {
                string name = Names[i];
                if (!name.StartsWith(needle, StringComparison.OrdinalIgnoreCase) &&
                    name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    matches.Add(name);
            }

            return matches;
        }
    }
}
