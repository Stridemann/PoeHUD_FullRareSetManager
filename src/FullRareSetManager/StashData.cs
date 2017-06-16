using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FullRareSetManager
{
    public class StashData
    {
        private const string StashDataFile = "StashData.json";
        public Dictionary<string, StashTabData> StashTabs = new Dictionary<string, StashTabData>();
        public StashTabData PlayerInventory = new StashTabData();


        public static StashData Load(FullRareSetManager plugin)
        {
            StashData result = null;
            var dataFileFullPath = plugin.PluginDirectory + "\\" + StashDataFile;

            if (File.Exists(dataFileFullPath))
            {
                string json = File.ReadAllText(dataFileFullPath);
                result = JsonConvert.DeserializeObject<StashData>(json);
            }
            else
            {
                result = new StashData();
                Save(plugin, result);
            }
            return result;
        }

        public static void Save(FullRareSetManager plugin, StashData data)
        {
            try
            {
                var dataFileFullPath = plugin.PluginDirectory + "\\" + StashDataFile;

                var settingsDirName = Path.GetDirectoryName(dataFileFullPath);
                if (!Directory.Exists(settingsDirName))
                    Directory.CreateDirectory(settingsDirName);

                using (var stream = new StreamWriter(File.Create(dataFileFullPath)))
                {
                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    stream.Write(json);
                }
            }
            catch
            {
                PoeHUD.Plugins.BasePlugin.LogError($"Plugin {plugin.PluginName} error save settings!", 3);
            }
        }
    }

    public class StashTabData
    {
        public int ItemsCount;
        public List<StashItem> StashTabItems = new List<StashItem>();
    }

    public class StashItem
    {
        public string StashName;
        public StashItemType ItemType;
        public string ItemClass;
        public string ItemName;
        public bool LowLvl;
        public bool bIdentified;
        public int InventPosX;
        public int InventPosY;
        //public int ItemQuality;//TODO: consider item quality in future
    }


    public enum StashItemType : int
    {
        Undefined = -1,

        Weapon = 0,      
        Helmet = 1,
        Body = 2,
        Gloves = 3,
        Boots = 4,
        Belt = 5,
        Amulet = 6,
        Ring = 7,

        TwoHanded = 8,
        OneHanded = 9,
    }
}
