using System;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using FullRareSetManager.Utilities;
using SharpDX;

namespace FullRareSetManager
{
    public class DropAllToInventory
    {
        private const int WHILE_DELAY = 5;
        private readonly FullRareSetManagerCore _plugin;

        public DropAllToInventory(FullRareSetManagerCore plugin)
        {
            _plugin = plugin;
        }

        private GameController GameController => _plugin.GameController;

        public bool SwitchToTab(int tabIndex, FullRareSetManagerSettings Settings)
        {
            var latency = (int) GameController.Game.IngameState.CurLatency;

            // We don't want to Switch to a tab that we are already on
            var openLeftPanel = GameController.Game.IngameState.IngameUi.OpenLeftPanel;

            try
            {
                var stashTabToGoTo =
                    GameController.Game.IngameState.IngameUi.StashElement.GetStashInventoryByIndex(tabIndex)
                        .InventoryUIElement;

                if (stashTabToGoTo.IsVisible)
                    return true;
            }
            catch
            {
                // Nothing to see here officer.
            }

            var _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;

            // We want to maximum wait 20 times the Current Latency before giving up in our while loops.
            var maxNumberOfTries = latency * 20 > 2000 ? latency * 20 / WHILE_DELAY : 2000 / WHILE_DELAY;

            if (tabIndex > 30)
                return SwitchToTabViaArrowKeys(tabIndex);

            var stashPanel = GameController.Game.IngameState.IngameUi.StashElement;

            try
            {
                var viewAllTabsButton = GameController.Game.IngameState.IngameUi.StashElement.ViewAllStashButton;

                if (stashPanel.IsVisible && !viewAllTabsButton.IsVisible)
                {
                    // The user doesn't have a view all tabs button, eg. 4 tabs.
                    return SwitchToTabViaArrowKeys(tabIndex);
                }

                var dropDownTabElements = GameController.Game.IngameState.IngameUi.StashElement.ViewAllStashPanel;

                if (!dropDownTabElements.IsVisible)
                {
                    var pos = viewAllTabsButton.GetClientRect();
                    Mouse.SetCursorPosAndLeftClick(pos.Center + _clickWindowOffset, Settings.ExtraDelay);

                    //Thread.Sleep(200);
                    Thread.Sleep(latency + Settings.ExtraDelay);
                    var brCounter = 0;

                    //while (1 == 2 && !dropDownTabElements.IsVisible)
                    //{
                    //    Thread.Sleep(WHILE_DELAY);

                    //    if (brCounter++ <= maxNumberOfTries)
                    //        continue;

                    //    BasePlugin.LogMessage($"1. Error in SwitchToTab: {tabIndex}.", 5);
                    //    return false;
                    //}

                    if (GameController.Game.IngameState.IngameUi.StashElement.TotalStashes > 30)
                    {
                        // TODO:Zafaar implemented something that allows us to get in contact with the ScrollBar.
                        Mouse.VerticalScroll(true, 5);
                        Thread.Sleep(latency + Settings.ExtraDelay);
                    }
                }

                // Dropdown menu have the following children: 0, 1, 2.
                // Where:
                // 0 is the icon (fx. chaos orb).
                // 1 is the name of the tab.
                // 2 is the slider.
                var totalStashes = GameController.Game.IngameState.IngameUi.StashElement.TotalStashes;
                var slider = dropDownTabElements.Children[1].ChildCount == totalStashes;
                var noSlider = dropDownTabElements.Children[2].ChildCount == totalStashes;
                RectangleF tabPos;

                if (slider)
                    tabPos = dropDownTabElements.GetChildAtIndex(1).GetChildAtIndex(tabIndex).GetClientRect();
                else if (noSlider)
                    tabPos = dropDownTabElements.GetChildAtIndex(2).GetChildAtIndex(tabIndex).GetClientRect();
                else
                {
                    
                    //BasePlugin.LogError("Couldn't detect slider/non-slider, contact Preaches [Stashie]", 3);
                    return false;
                }

                Mouse.SetCursorPosAndLeftClick(tabPos.Center + _clickWindowOffset, Settings.ExtraDelay);
                Thread.Sleep(latency + Settings.ExtraDelay);
            }
            catch (Exception e)
            {
                //BasePlugin.LogError($"Error in GoToTab {tabIndex}: {e.Message}", 5);
                return false;
            }

            Inventory stash;

            var counter = 0;

            do
            {
                Thread.Sleep(WHILE_DELAY);
                stash = stashPanel.VisibleStash;

                if (counter++ <= maxNumberOfTries)
                    continue;

                //BasePlugin.LogMessage("2. Error opening stash: " + tabIndex, 5);
                return true;
            } while (stash?.VisibleInventoryItems == null);

            return true;
        }

        private bool SwitchToTabViaArrowKeys(int tabIndex)
        {
            var latency = (int) GameController.Game.IngameState.CurLatency;
            var indexOfCurrentVisibleTab = GameController.Game.IngameState.IngameUi.StashElement.IndexVisibleStash; // GetIndexOfCurrentVisibleTab();
            var difference = tabIndex - indexOfCurrentVisibleTab;
            var negative = difference < 0;

            for (var i = 0; i < Math.Abs(difference); i++)
            {
                Keyboard.KeyPress(negative ? Keys.Left : Keys.Right);
                Thread.Sleep(latency);
            }

            return true;
        }
    }
}
