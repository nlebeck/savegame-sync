using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavegameSync
{
    public class SaveSpec
    {
        public string GameName { get; private set; }

        /// <summary>
        /// A list of file or directory paths, relative to the game's install directory, to include
        /// in the savegame
        /// </summary>
        public string[] SavePaths { get; private set; }

        public SaveSpec(string gameName, string[] savePaths)
        {
            GameName = gameName;
            SavePaths = savePaths;
        }

    }
}
