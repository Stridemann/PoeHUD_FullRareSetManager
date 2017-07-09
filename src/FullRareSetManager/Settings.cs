using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using System.Windows.Forms;

namespace FullRareSetManager
{
    public class FullRareSetManagerSettings : SettingsBase
    {
        public FullRareSetManagerSettings()
        {
            Enable = false;
            AllowIdentified = false;
            ShowOnlyWithInventory = false;
            HideWhenLeftPanelOpened = false;
            ShowRegalSets = false;
            PositionX = new RangeNode<float>(0.0f, 0.0f, 2000.0f);
            PositionY = new RangeNode<float>(365.0f, 0.0f, 2000.0f);
            WeaponTypePriority = new ListNode {Value = "Two handed"};
            DropToInventoryKey = Keys.F5;

            EnableBorders = false;
            InventBorders = false;
            BorderWidth = new RangeNode<int>(5, 1, 15);
            BorderAutoResize = false;
            BorderOversize = new RangeNode<int>(5, 0, 15);
            TextSize = new RangeNode<int>(20, 0, 30);
            TextOffsetX = new RangeNode<float>(0, -50, 12);
            TextOffsetY = new RangeNode<float>(-20, -50, 12);
            IgnoreOneHanded = false;
            MaxSets = new RangeNode<int>(0, 0, 30);
            CalcByFreeSpace = false;
        }

        [Menu("Position X")]
        public RangeNode<float> PositionX { get; set; }
        [Menu("Position Y")]
        public RangeNode<float> PositionY { get; set; }

        [Menu("Allow Identified Items")]
        public ToggleNode AllowIdentified { get; set; }

        [Menu("Show only with inventory")]
        public ToggleNode ShowOnlyWithInventory { get; set; }

        [Menu("Hide when left panel opened")]
        public ToggleNode HideWhenLeftPanelOpened { get; set; }
        [Menu("Show Regal sets")]
        public ToggleNode ShowRegalSets { get; set; }

        [Menu("Priority", "Weapon prepare priority in list of set items. If you have 1-handed and 2-handed weapons- it will consider this option.")]
        public ListNode WeaponTypePriority { get; set; }

        [Menu("Max Collecting Sets (0 disable)", "Amount of sets you going to collect. It will display lower pick priority if amount of item are more than this value.")]
        public RangeNode<int> MaxSets { get; set; }

        [Menu("Drop To Invent Key")]
        public HotkeyNode DropToInventoryKey { get; set; }


        [Menu("Items Lables Borders", 0)]
        public ToggleNode EnableBorders { get; set; }

        [Menu("Inventory Borders")]
        public ToggleNode InventBorders { get; set; }

        [Menu("Borders Width", 1, 0)]
        public RangeNode<int> BorderWidth { get; set; }
        [Menu("Borders Oversize", 2, 0)]
        public RangeNode<int> BorderOversize { get; set; }
        [Menu("Resize Borders accord. to Pick Priority", "That will change borders width, oversize depending on pick priority.", 3, 0)]
        public ToggleNode BorderAutoResize { get; set; }

        [Menu("Text Size", 4, 0)]
        public RangeNode<int> TextSize { get; set; }

        [Menu("Text Offset X", 5, 0)]
        public RangeNode<float> TextOffsetX { get; set; }
        [Menu("Text Offset Y", 6, 0)]
        public RangeNode<float> TextOffsetY { get; set; }

        [Menu("Don't Higlight One Handed", 7, 0)]
        public ToggleNode IgnoreOneHanded { get; set; }

        [Menu("Separate stash tabs for each item type", "Pick priority will be calculated by free space in stash tab. Free space will be calculated for each item stash tab.")]
        public ToggleNode CalcByFreeSpace { get; set; }
    }
}
