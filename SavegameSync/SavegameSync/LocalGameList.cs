using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SavegameSync
{
    public class LocalGameList
    {
        private Dictionary<string, string> gameInstallDirs = new Dictionary<string, string>();

        public LocalGameList(Stream stream)
        {
            stream.Position = 0;
            StreamReader streamReader = new StreamReader(stream);
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                string[] lineSplit = line.Split('\t');
                string gameName = lineSplit[0];
                string installDir = lineSplit[1];
                gameInstallDirs.Add(gameName, installDir);
            }
            streamReader.Close();
        }

        public void WriteToStream(Stream stream)
        {
            StreamWriter streamWriter = new StreamWriter(stream);
            foreach (string gameName in gameInstallDirs.Keys)
            {
                streamWriter.Write($"{gameName}\t{gameInstallDirs[gameName]}\n");
            }
            streamWriter.Flush();
        }

        public void AddGame(string gameName, string installDir)
        {
            gameInstallDirs.Add(gameName, installDir);
        }

        public string GetInstallDir(string gameName)
        {
            if (!gameInstallDirs.ContainsKey(gameName))
            {
                return null;
            }
            return gameInstallDirs[gameName];
        }

        public bool ContainsGame(string gameName)
        {
            return (gameInstallDirs.ContainsKey(gameName));
        }

        public void DebugPrintGames()
        {
            Debug.Write("Installed games: ");
            foreach (string gameName in gameInstallDirs.Keys)
            {
                Debug.Write($" {gameName} {gameInstallDirs[gameName]}");
            }
            Debug.WriteLine("");
        }
    }
}
