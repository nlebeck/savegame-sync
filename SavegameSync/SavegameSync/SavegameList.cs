using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SavegameSync
{
    /// <summary>
    /// Stores a queue of savegame entries for each game. Each savegame entry is
    /// a (guid, timestamp) pair.
    /// </summary>
    public class SavegameList
    {
        private class GameEntry
        {
            public string GameName { get; set; }
            public Queue<Tuple<string, string>> saves { get; set; }

            public GameEntry() {
                saves = new Queue<Tuple<string, string>>();
            }
        }

        private Dictionary<string, Queue<Tuple<string, string>>> gameEntries =
            new Dictionary<string, Queue<Tuple<string, string>>>();

        public SavegameList(Stream stream)
        {
            stream.Position = 0;
            StreamReader streamReader = new StreamReader(stream);
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                string[] lineSplit = line.Split(new char[] { '\t' });
                string gameName = lineSplit[0];
                gameEntries[gameName] = new Queue<Tuple<string, string>>();
                int index = 1;
                while (index < lineSplit.Length)
                {
                    string saveGuid = lineSplit[index];
                    index++;
                    string saveTimestamp = lineSplit[index];
                    index++;
                    gameEntries[gameName].Enqueue(new Tuple<string, string>(saveGuid, saveTimestamp));
                }
            }
            streamReader.Close();
        }

        public SavegameList() { }

        public void WriteToStream(Stream stream)
        {
            StreamWriter streamWriter = new StreamWriter(stream);
            foreach (string entry in gameEntries.Keys)
            {
                streamWriter.Write(entry);
                foreach (Tuple<string, string> save in gameEntries[entry])
                {
                    streamWriter.Write("\t" + save.Item1 + "\t" + save.Item2);
                }
                streamWriter.WriteLine();
            }
            streamWriter.Flush();
        }

        public void AddSave(string gameName, string saveGuid, string saveTimestamp)
        {
            if (!gameEntries.ContainsKey(gameName))
            {
                gameEntries[gameName] = new Queue<Tuple<string, string>>();
            }
            if (gameEntries[gameName].Count >= MainWindow.SavesPerGame)
            {
                gameEntries[gameName].Dequeue();
            }
            gameEntries[gameName].Enqueue(new Tuple<string, string>(saveGuid, saveTimestamp));
        }

        public List<Tuple<string, string>> ReadSaves(string gameName)
        {
            if (!gameEntries.ContainsKey(gameName))
            {
                return new List<Tuple<string, string>>();
            }
            return new List<Tuple<string, string>>(gameEntries[gameName]);
        }

        public List<string> GetGames()
        {
            return new List<string>(gameEntries.Keys);
        }

        public void DebugPrintSaves(string gameName)
        {
            List<Tuple<string, string>> saves = ReadSaves(gameName);
            Debug.Write(gameName);
            foreach (Tuple<string, string> save in saves)
            {
                Debug.Write(" " + save.Item1 + " " + save.Item2);
            }
            Debug.WriteLine("");
        }

        public void DebugPrintGames()
        {
            Debug.Write("Games:");
            foreach (string gameName in GetGames())
            {
                Debug.Write(" " + gameName);
            }
            Debug.WriteLine("");
        }
    }
}
