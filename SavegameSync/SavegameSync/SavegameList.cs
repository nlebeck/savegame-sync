using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SavegameSync
{
    public struct SavegameEntry
    {
        public Guid Guid { get; private set; }
        public DateTime Timestamp { get; private set; }

        public SavegameEntry(Guid guid, DateTime timestamp)
        {
            Guid = guid;
            Timestamp = timestamp;
        }

        public string Serialize()
        {
            return String.Format("{0},{1}", Guid.ToString(), SavegameSyncUtils.SerializeDateTime(Timestamp));
        }

        public static SavegameEntry Deserialize(string str)
        {
            string[] stringSplit = str.Split(',');
            Guid guid = Guid.Parse(stringSplit[0]);
            DateTime timestamp = SavegameSyncUtils.DeserializeDateTime(stringSplit[1]);
            return new SavegameEntry(guid, timestamp);
        }
    }

    /// <summary>
    /// Stores a queue of savegame entries for each game, indexed by game name.
    /// </summary>
    public class SavegameList
    {
        private Dictionary<string, Queue<SavegameEntry>> gameEntries =
            new Dictionary<string, Queue<SavegameEntry>>();

        public async Task ReadFromStream(Stream stream)
        {
            stream.Position = 0;
            StreamReader streamReader = new StreamReader(stream);
            while (!streamReader.EndOfStream)
            {
                string line = await streamReader.ReadLineAsync();
                string[] lineSplit = line.Split('\t');
                string gameName = lineSplit[0];
                gameEntries[gameName] = new Queue<SavegameEntry>();
                for (int i = 1; i < lineSplit.Length; i++)
                {
                    SavegameEntry entry = SavegameEntry.Deserialize(lineSplit[i]);
                    gameEntries[gameName].Enqueue(entry);
                }
            }
            streamReader.Close();
        }

        public async Task WriteToStream(Stream stream)
        {
            StreamWriter streamWriter = new StreamWriter(stream);
            foreach (string entry in gameEntries.Keys)
            {
                await streamWriter.WriteAsync(entry);
                foreach (SavegameEntry save in gameEntries[entry])
                {
                    await streamWriter.WriteAsync($"\t{save.Serialize()}");
                }
                await streamWriter.WriteLineAsync();
            }
            await streamWriter.FlushAsync();
        }

        public void AddSave(string gameName, Guid saveGuid, DateTime saveTimestamp)
        {
            if (!gameEntries.ContainsKey(gameName))
            {
                gameEntries[gameName] = new Queue<SavegameEntry>();
            }
            if (gameEntries[gameName].Count >= SavegameSyncEngine.SavesPerGame)
            {
                gameEntries[gameName].Dequeue();
            }
            gameEntries[gameName].Enqueue(new SavegameEntry(saveGuid, saveTimestamp));
        }

        public List<SavegameEntry> ReadSaves(string gameName)
        {
            if (!gameEntries.ContainsKey(gameName))
            {
                return new List<SavegameEntry>();
            }
            return new List<SavegameEntry>(gameEntries[gameName]);
        }

        public List<string> GetGames()
        {
            return new List<string>(gameEntries.Keys);
        }

        public void DebugPrintSaves(string gameName)
        {
            List<SavegameEntry> saves = ReadSaves(gameName);
            Debug.Write(gameName);
            foreach (SavegameEntry save in saves)
            {
                Debug.Write($" ({save.Guid},{save.Timestamp})");
            }
            Debug.WriteLine("");
        }

        public void DebugPrintGames()
        {
            Debug.Write("Games:");
            foreach (string gameName in GetGames())
            {
                Debug.Write($" {gameName}");
            }
            Debug.WriteLine("");
        }
    }
}
