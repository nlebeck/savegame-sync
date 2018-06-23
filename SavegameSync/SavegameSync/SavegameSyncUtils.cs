using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavegameSync
{
    public static class SavegameSyncUtils
    {
        public static string SerializeDateTime(DateTime dateTime)
        {
            return dateTime.Ticks.ToString();
        }

        public static DateTime DeserializeDateTime(string str)
        {
            long ticks = long.Parse(str);
            DateTime dateTime = new DateTime(ticks);
            return dateTime;
        }

        public static string GetSavegameFileNameFromGuid(Guid guid)
        {
            return guid.ToString() + ".zip";
        }
    }
}
