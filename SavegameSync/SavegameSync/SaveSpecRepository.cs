using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SavegameSync
{
    public class SaveSpecRepository
    {
        private const string REPOSITORY_FILE_NAME = "SaveSpecRepository.xml";

        private static SaveSpecRepository singleton;

        private Dictionary<string, SaveSpec> saveSpecs = new Dictionary<string, SaveSpec>();

        public static SaveSpecRepository GetRepository()
        {
            if (singleton == null)
            {
                singleton = new SaveSpecRepository();
            }
            return singleton;
        }

        private SaveSpecRepository()
        {
            ParseFromXmlFile(REPOSITORY_FILE_NAME);
        }

        public SaveSpec GetSaveSpec(string gameName)
        {
            if (saveSpecs.ContainsKey(gameName))
            {
                return saveSpecs[gameName];
            }
            else
            {
                throw new SaveSpecMissingException(gameName);
            }
        }

        public ICollection<SaveSpec> GetAllSaveSpecs()
        {
            return saveSpecs.Values;
        }

        private void ParseFromXmlFile(string xmlFileName)
        {
            XmlReader reader;
            try
            {
                reader = XmlReader.Create(xmlFileName);
            }
            catch (FileNotFoundException)
            {
                throw new SaveSpecRepositoryMissingException();
            }

            reader.ReadToNextSibling("saveSpecRepository");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "saveSpec")
                {
                    SaveSpec saveSpec = ParseSaveSpec(reader.ReadSubtree());
                    saveSpecs.Add(saveSpec.GameName, saveSpec);
                }
            }
        }

        private SaveSpec ParseSaveSpec(XmlReader reader)
        {
            string gameName = null;
            List<string> paths = new List<string>();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "name")
                    {
                        gameName = reader.ReadElementContentAsString();
                    }
                    else if (reader.Name == "path")
                    {
                        paths.Add(reader.ReadElementContentAsString());
                    }
                }
            }
            if (gameName == null || paths.Count == 0)
            {
                throw new SaveSpecRepositoryParseException();
            }
            return new SaveSpec(gameName, paths.ToArray());
        }
    }
}
