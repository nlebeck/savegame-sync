using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SavegameSync
{
    /// <summary>
    /// Stores a queue of savegame entries for each game. Each savegame entry is
    /// a (guid, timestamp) pair.
    /// </summary>
    public class SavegameList
    {
        private Dictionary<string, Queue<Tuple<Guid, DateTime>>> gameEntries =
            new Dictionary<string, Queue<Tuple<Guid, DateTime>>>();

        public async Task ReadFromStream(Stream stream)
        {
            stream.Position = 0;
            StreamReader streamReader = new StreamReader(stream);
            while (!streamReader.EndOfStream)
            {
                string line = await streamReader.ReadLineAsync();
                string[] lineSplit = line.Split('\t');
                string gameName = lineSplit[0];
                gameEntries[gameName] = new Queue<Tuple<Guid, DateTime>>();
                int index = 1;
                while (index < lineSplit.Length)
                {
                    Guid saveGuid = Guid.Parse(lineSplit[index]);
                    index++;
                    DateTime saveTimestamp = SavegameSyncUtils.DeserializeDateTime(lineSplit[index]);
                    index++;
                    gameEntries[gameName].Enqueue(new Tuple<Guid, DateTime>(saveGuid, saveTimestamp));
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
                foreach (Tuple<Guid, DateTime> save in gameEntries[entry])
                {
                    await streamWriter.WriteAsync($"\t{save.Item1.ToString()}\t{SavegameSyncUtils.SerializeDateTime(save.Item2)}");
                }
                await streamWriter.WriteLineAsync();
            }
            await streamWriter.FlushAsync();
        }

        public void AddSave(string gameName, Guid saveGuid, DateTime saveTimestamp)
        {
            if (!gameEntries.ContainsKey(gameName))
            {
                gameEntries[gameName] = new Queue<Tuple<Guid, DateTime>>();
            }
            if (gameEntries[gameName].Count >= SavegameSyncEngine.SavesPerGame)
            {
                gameEntries[gameName].Dequeue();
            }
            gameEntries[gameName].Enqueue(new Tuple<Guid, DateTime>(saveGuid, saveTimestamp));
        }

        public List<Tuple<Guid, DateTime>> ReadSaves(string gameName)
        {
            if (!gameEntries.ContainsKey(gameName))
            {
                return new List<Tuple<Guid, DateTime>>();
            }
            return new List<Tuple<Guid, DateTime>>(gameEntries[gameName]);
        }

        public List<string> GetGames()
        {
            return new List<string>(gameEntries.Keys);
        }

        public void DebugPrintSaves(string gameName)
        {
            List<Tuple<Guid, DateTime>> saves = ReadSaves(gameName);
            Debug.Write(gameName);
            foreach (Tuple<Guid, DateTime> save in saves)
            {
                Debug.Write($" ({save.Item1},{save.Item2})");
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
