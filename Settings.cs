using System.Collections.Generic;
using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;

namespace FullRareSetManager
{
    public class FullRareSetManagerSettings : ISettings
    {
        public List<int> AllowedStashTabs = new List<int>();

        public FullRareSetManagerSettings()
        {
            Enable = new ToggleNode(false);
            AllowIdentified = new ToggleNode(false);
            ShowOnlyWithInventory = new ToggleNode(false);
            HideWhenLeftPanelOpened = new ToggleNode(false);
            ShowRegalSets = new ToggleNode(false);
            PositionX = new RangeNode<float>(0.0f, 0.0f, 2000.0f);
            PositionY = new RangeNode<float>(365.0f, 0.0f, 2000.0f);
            WeaponTypePriority = new ListNode {Value = "Two handed"};
            DropToInventoryKey = Keys.F5;
            ExtraDelay = new RangeNode<int>(50, 0, 2000);

            EnableBorders = new ToggleNode(false);
            InventBorders = new ToggleNode(false);
            BorderWidth = new RangeNode<int>(5, 1, 15);
            BorderAutoResize = new ToggleNode(false);
            BorderOversize = new RangeNode<int>(5, 0, 15);
            TextSize = new RangeNode<int>(20, 0, 30);
            TextOffsetX = new RangeNode<float>(0, -50, 12);
            TextOffsetY = new RangeNode<float>(-20, -50, 12);
            IgnoreOneHanded = new ToggleNode(false);
            MaxSets = new RangeNode<int>(0, 0, 30);
            CalcByFreeSpace = new ToggleNode(false);

            AutoSell = new ToggleNode(true);
        }

        [Menu("", "Registering after using DropToInventoryKey to NPC trade inventory")]
        public TextNode SetsAmountStatisticsText { get; set; } = "Total sets sold to vendor: N/A";
        public int SetsAmountStatistics { get; set; }
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
        [Menu("Priority",
            "Weapon prepare priority in list of set items. If you have 1-handed and 2-handed weapons- it will consider this option.")]
        public ListNode WeaponTypePriority { get; set; }
        [Menu("Max Collecting Sets (0 disable)",
            "Amount of sets you going to collect. It will display lower pick priority if amount of item are more than this value.")]
        public RangeNode<int> MaxSets { get; set; }
        [Menu("Drop To Invent Key", "It will also drop items to NPC trade window inventory")]
        public HotkeyNode DropToInventoryKey { get; set; }
        [Menu("Extra Click Delay")]
        public RangeNode<int> ExtraDelay { get; set; }
        [Menu("Items Lables Borders", 0)]
        public ToggleNode EnableBorders { get; set; }
        [Menu("Inventory Borders")]
        public ToggleNode InventBorders { get; set; }
        [Menu("Borders Width", 1, 0)]
        public RangeNode<int> BorderWidth { get; set; }
        [Menu("Borders Oversize", 2, 0)]
        public RangeNode<int> BorderOversize { get; set; }
        [Menu("Resize Borders accord. to Pick Priority",
            "That will change borders width, oversize depending on pick priority.", 3, 0)]
        public ToggleNode BorderAutoResize { get; set; }
        [Menu("Text Size", 4, 0)]
        public RangeNode<int> TextSize { get; set; }
        [Menu("Text Offset X", 5, 0)]
        public RangeNode<float> TextOffsetX { get; set; }
        [Menu("Text Offset Y", 6, 0)]
        public RangeNode<float> TextOffsetY { get; set; }
        [Menu("Don't Higlight One Handed", 7, 0)]
        public ToggleNode IgnoreOneHanded { get; set; }
        [Menu("Separate stash tabs for each item type",
            "Pick priority will be calculated by free space in stash tab. Free space will be calculated for each item stash tab.")]
        public ToggleNode CalcByFreeSpace { get; set; }
        [Menu("Ignore Elder/Shaper items")]
        public ToggleNode IgnoreElderShaper { get; set; } = new ToggleNode(true);
        [Menu("Show Red Rectangle Around Ignored Items")]
        public ToggleNode ShowRedRectangleAroundIgnoredItems { get; set; } = new ToggleNode(true);
        [Menu("Auto sell on keypress at vendor?")]
        public ToggleNode AutoSell { get; set; }
        public ToggleNode Enable { get; set; }

        [Menu("Only Allowed Stash Tabs", "Define stash tabs manually to ignore other tabs")]
        public ToggleNode OnlyAllowedStashTabs { get; set; } = new ToggleNode(false);
    }
}
