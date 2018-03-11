using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavegameSync
{
    public class SaveSpecRepository
    {
        private static SaveSpecRepository singleton = new SaveSpecRepository();

        private Dictionary<string, SaveSpec> saveSpecs = new Dictionary<string, SaveSpec>();

        public static SaveSpecRepository GetRepository()
        {
            return singleton;
        }

        private SaveSpecRepository()
        {
            SaveSpec mohaaSpec = new SaveSpec("Medal of Honor Allied Assault War Chest", new string[]{ "main\\save" });
            saveSpecs.Add(mohaaSpec.GameName, mohaaSpec);
        }

        public SaveSpec GetSaveSpec(string gameName)
        {
            if (saveSpecs.ContainsKey(gameName))
            {
                return saveSpecs[gameName];
            }
            return null;
        }

        public ICollection<SaveSpec> GetAllSaveSpecs()
        {
            return saveSpecs.Values;
        }
    }
}
