using System;
using System.Collections.Generic;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Hud.UI;
using PoeHUD.Poe.RemoteMemoryObjects;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using SharpDX;
using System.Threading;
using PoeHUD.Controllers;
using Utils;

namespace FullRareSetManager
{
    public class DropAllToInventory
    {
        private FullRareSetManager Plugin;

        private GameController GameController { get { return Plugin.GameController; } }

        public DropAllToInventory(FullRareSetManager plugin)
        {
            Plugin = plugin;
        }

        public Inventory GoToTab(int tabIndex)
        {
            if (tabIndex > 30)
            {
                BasePlugin.LogError(
                    $"WARNING (can be ignored): {tabIndex}. tab requested, using old method since it's greater than 30 which requires scrolling!\n\tHint, it's suggested to use tabs under stashTabIndex 30.",
                    5);
                return null;
            }

            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;

            try
            {
                var viewAllTabsButton = stashPanel.ViewAllStashButton;

                var openLeftPanel = GameController.Game.IngameState.IngameUi.OpenLeftPanel;
                var parent = openLeftPanel.Children[2].Children[0].Children[1].Children[3];
                var dropDownTabElements = parent.Children[2];

                var totalStashes = stashPanel.TotalStashes;
                if (totalStashes > 30)
                {
                    // If the number of stashes is greater than 30, then parent.Children[1] becomes the ScrollBar
                    // and the DropDownElements becomes parent.Children[2]
                    dropDownTabElements = parent.Children[1];
                }

                var gameWindowPos = GameController.Window.GetWindowRectangle();

                if (!dropDownTabElements.IsVisible)
                {
                    var pos = viewAllTabsButton.GetClientRect();
                    MouseUtils.LeftMouseClick(pos.Center + gameWindowPos.TopLeft);

                    int brCounter = 0;
                    while (!dropDownTabElements.IsVisible)
                    {
                        Thread.Sleep(50);
                        if (++brCounter > 30) break;
                    }
                }

                var tabPos = dropDownTabElements.Children[tabIndex].GetClientRect();

                MouseUtils.LeftMouseClick(tabPos.Center + gameWindowPos.TopLeft);
            }
            catch (Exception e)
            {
                BasePlugin.LogError($"Error in GoToTab: {e.Message}", 5);
            }

            Inventory stash = null;
       

            int counter = 0;
            do
            {
                Thread.Sleep(50);
                stash = stashPanel.GetStashInventoryByIndex(tabIndex);
                if (++counter > 100) break;
            }
            while (stash == null || stash.VisibleInventoryItems == null);

            return stash;
        }
    }
}
