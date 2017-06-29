using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using System.Windows.Forms;
using SharpDX;

namespace FullRareSetManager
{
    public class FullRareSetManager_Settings : SettingsBase
    {
        public FullRareSetManager_Settings()
        {
            Enable = false;
            AllowIdentified = false;
            ShowOnlyWithInventory = false;
            HideWhenLeftPanelOpened = false;
            ShowRegalSets = false;
            PositionX = new RangeNode<float>(0.0f, 0.0f, 2000.0f);
            PositionY = new RangeNode<float>(365.0f, 0.0f, 2000.0f);
            WeaponTypePriority = new RangeNode<int>(0, 0, 1);
            DropToInventoryKey = Keys.F5;
            EnableBorders = false;

            BorderWidth = new RangeNode<int>(5, 1, 15);
            BorderAutoResize = false;
            BorderOversize = new RangeNode<int>(5, 0, 15);
            TextSize = new RangeNode<int>(20, 0, 30);
            TextOffsetX = new RangeNode<float>(0, -50, 12);
            TextOffsetY = new RangeNode<float>(-20, -50, 12);
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

        [Menu("WeaponPreparePriority: TwoHand<->OneHand")]
        public RangeNode<int> WeaponTypePriority { get; set; }

        [Menu("Drop To Invent Key")]
        public HotkeyNode DropToInventoryKey { get; set; }


        [Menu("Item Lable borders", 0)]
        public ToggleNode EnableBorders { get; set; }

        [Menu("Borders Width", 1, 0)]
        public RangeNode<int> BorderWidth { get; set; }
        [Menu("Borders Oversize", 2, 0)]
        public RangeNode<int> BorderOversize { get; set; }
        [Menu("Resize Borders accord. to Pick Priority", 3, 0)]
        public ToggleNode BorderAutoResize { get; set; }

        [Menu("Text Size", 4, 0)]
        public RangeNode<int> TextSize { get; set; }

        [Menu("Text Offset X", 5, 0)]
        public RangeNode<float> TextOffsetX { get; set; }
        [Menu("Text Offset Y", 6, 0)]
        public RangeNode<float> TextOffsetY { get; set; }
    }
}
