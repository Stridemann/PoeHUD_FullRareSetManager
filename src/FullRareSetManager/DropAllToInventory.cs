using System;
using PoeHUD.Plugins;
using PoeHUD.Poe.RemoteMemoryObjects;
using System.Threading;
using System.Windows.Forms;
using FullRareSetManager.Utilities;
using PoeHUD.Controllers;

namespace FullRareSetManager
{
    public class DropAllToInventory
    {
        private readonly Core _plugin;
        private GameController GameController => _plugin.GameController;

        private const int WHILE_DELAY = 5;

        public DropAllToInventory(Core plugin)
        {
            _plugin = plugin;
        }

        public bool SwitchToTab(int tabIndex)
        {
            var latency = (int)GameController.Game.IngameState.CurLatency;
            // We don't want to Switch to a tab that we are already on
            var openLeftPanel = GameController.Game.IngameState.IngameUi.OpenLeftPanel;
            try
            {
                var stashTabToGoTo =
                    GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(tabIndex)
                        .InventoryUiElement;

                if (stashTabToGoTo.IsVisible)
                {
                    return true;
                }
            }
            catch
            {
                // Nothing to see here officer.
            }

            // We want to maximum wait 20 times the Current Latency before giving up in our while loops.
            var maxNumberOfTries = latency * 20 > 2000 ? latency * 20 / WHILE_DELAY : 2000 / WHILE_DELAY;
            var clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;

            if (tabIndex > 30)
            {
                return SwitchToTabViaArrowKeys(tabIndex);
            }

            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;
            try
            {
                // Obs, this method only works with 31 stashtabs on 1920x1080, since you have to scroll at 32 tabs, and the frame stays in place.
                var viewAllTabsButton = GameController.Game.IngameState.ServerData.StashPanel.ViewAllStashButton;

                if (stashPanel.IsVisible && !viewAllTabsButton.IsVisible)
                {
                    // The user doesn't have a view all tabs button, eg. 4 tabs.
                    return SwitchToTabViaArrowKeys(tabIndex);
                }

                var parent = openLeftPanel.Children[2].Children[0].Children[1].Children[3];
                var dropDownTabElements = parent.Children[2];

                var totalStashes = GameController.Game.IngameState.ServerData.StashPanel.TotalStashes;
                if (totalStashes > 30)
                {
                    dropDownTabElements = parent.Children[1];
                }

                if (!dropDownTabElements.IsVisible)
                {
                    var pos = viewAllTabsButton.GetClientRect();
                    
                    Mouse.SetCursorPosAndLeftClick(pos.Center + clickWindowOffset);

                    var brCounter = 0;

                    while (!dropDownTabElements.IsVisible)
                    {
                        Thread.Sleep(WHILE_DELAY);

                        if (brCounter++ <= maxNumberOfTries)
                        {
                            continue;
                        }
                        BasePlugin.LogMessage($"1. Error in SwitchToTab: {tabIndex}.", 5);
                        return false;
                    }

                    if (totalStashes > 30)
                    {
                        // TODO:Zafaar implemented something that allows us to get in contact with the ScrollBar.
                        Mouse.VerticalScroll(true, 5);
                        Thread.Sleep(latency + 50);
                    }
                }

                var tabPos = dropDownTabElements.Children[tabIndex].GetClientRect();

                Mouse.SetCursorPosAndLeftClick(tabPos.Center + clickWindowOffset);
                Thread.Sleep(latency);
            }
            catch (Exception e)
            {
                BasePlugin.LogError($"Error in GoToTab {tabIndex}: {e.Message}", 5);
                return false;
            }

            Inventory stash;

            var counter = 0;

            do
            {
                Thread.Sleep(WHILE_DELAY);
                stash = stashPanel.VisibleStash;

                if (counter++ <= maxNumberOfTries)
                {
                    continue;
                }
                BasePlugin.LogMessage("2. Error opening stash: " + tabIndex, 5);
                return false;
            } while (stash?.VisibleInventoryItems == null);
            return true;
        }

        private bool SwitchToTabViaArrowKeys(int tabIndex)
        {
            var latency = (int)GameController.Game.IngameState.CurLatency;
            var indexOfCurrentVisibleTab = GetIndexOfCurrentVisibleTab();
            var difference = tabIndex - indexOfCurrentVisibleTab;
            var negative = difference < 0;

            for (var i = 0; i < Math.Abs(difference); i++)
            {
                Keyboard.KeyPress(negative ? Keys.Left : Keys.Right);
                Thread.Sleep(latency);
            }

            return true;
        }

        private int GetIndexOfCurrentVisibleTab()
        {
            var openLeftPanel = GameController.Game.IngameState.IngameUi.OpenLeftPanel;
            var totalStashes = GameController.Game.IngameState.ServerData.StashPanel.TotalStashes;

            for (var i = 0; i < totalStashes; i++)
            {
                var stashTabToGoTo = openLeftPanel
                    .Children[2]
                    .Children[0]
                    .Children[1]
                    .Children[1]
                    .Children[i]
                    .Children[0];

                if (stashTabToGoTo.IsVisible)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}