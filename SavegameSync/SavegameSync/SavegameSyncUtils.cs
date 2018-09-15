using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavegameSync
{
    public static class SavegameSyncUtils
    {

        public delegate Task Operation();

        public static async Task RunWithChecks(Operation op)
        {
            try
            {
                await op();
            }
            catch (SavegameSyncException se)
            {
                string dialogText = "Error encountered while performing operation: " + se.Message;
                InformationDialog dialog = new InformationDialog(dialogText);
                dialog.ShowDialog();
            }
        }

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
