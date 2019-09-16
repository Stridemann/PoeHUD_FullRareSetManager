using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FullRareSetManager
{
    public class StashData
    {
        private const string STASH_DATA_FILE = "StashData.json";
        public StashTabData PlayerInventory = new StashTabData();
        public Dictionary<string, StashTabData> StashTabs = new Dictionary<string, StashTabData>();

        public static StashData Load(FullRareSetManagerCore plugin)
        {
            StashData result;
            var dataFileFullPath = plugin.DirectoryFullName + "\\" + STASH_DATA_FILE;

            if (File.Exists(dataFileFullPath))
            {
                var json = File.ReadAllText(dataFileFullPath);

                try
                {
                    result = JsonConvert.DeserializeObject<StashData>(json);
                }
                catch (Exception e)
                {
                    return null;
                }
            }
            else
            {
                result = new StashData();
                Save(plugin, result);
            }

            return result;
        }

        public static void Save(FullRareSetManagerCore plugin, StashData data)
        {
            try
            {
                if (data == null) return;
                var dataFileFullPath = plugin.DirectoryFullName + "\\" + STASH_DATA_FILE;
                var settingsDirName = Path.GetDirectoryName(dataFileFullPath);

                if (!Directory.Exists(settingsDirName))
                    Directory.CreateDirectory(settingsDirName);

                using (var stream = new StreamWriter(File.Create(dataFileFullPath)))
                {
                    var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    stream.Write(json);
                }
            }
            catch
            {
                //PoeHUD.Plugins.BasePlugin.LogError($"Plugin {plugin.PluginName} error save settings!", 3);
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
        public bool BIdentified;
        public int InventPosX;
        public int InventPosY;
        public string ItemClass;
        public string ItemName;
        public StashItemType ItemType;
        public bool LowLvl;
        public string StashName;
        public bool BInPlayerInventory { get; set; }
    }

    public enum StashItemType
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
        OneHanded = 9
    }
}
