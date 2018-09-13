using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavegameSync
{
    public abstract class SavegameSyncException : Exception
    {
        public SavegameSyncException()
            : base()
        { }

        public SavegameSyncException(string message)
            : base(message)
        { }

        public SavegameSyncException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    public class SaveSpecMissingException : SavegameSyncException
    {
        public SaveSpecMissingException(string gameName)
            : base(GetMessage(gameName)) { }

        private static string GetMessage(string gameName)
        {
            return $"SaveSpec for \"{gameName}\" not found.";
        }
    }

    public class NotInLocalGameListException : SavegameSyncException
    {
        public NotInLocalGameListException(string gameName)
            : base(GetMessage(gameName)) { }

        private static string GetMessage(string gameName)
        {
            return $"Game \"{gameName}\" not found in local game list.";
        }
    }
}
