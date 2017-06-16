using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace FullRareSetManager
{
    public class FullRareSetManager_Settings : SettingsBase
    {
        public FullRareSetManager_Settings()
        {
            Enable = true;
            AllowIdentified = false;
            ShowOnlyWithInventory = false;
            HideWhenLeftPanelOpened = false;
            ShowRegalSets = false;
            PositionX = new RangeNode<float>(0.0f, 0.0f, 2000.0f);
            PositionY = new RangeNode<float>(365.0f, 0.0f, 2000.0f);
            WeaponTypePriority = new RangeNode<int>(0, 0, 1);
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
    }
}
