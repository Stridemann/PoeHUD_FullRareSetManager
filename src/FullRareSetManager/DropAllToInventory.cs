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

            try
            {

                var gameWindowPos = GameController.Window.GetWindowRectangle();
                // Obs, this method only works with 31 stashtabs on 1920x1080, since you have to scroll at 32 tabs, and the frame stays in place.
                var openLeftPanel = GameController.Game.IngameState.IngameUi.OpenLeftPanel;
                var viewAllTabsButton = GameController.Game.IngameState.UIRoot.Children[1].Children[21].Children[2]
                    .Children[0]
                    .Children[1].Children[2];


                var parent = openLeftPanel.Children[2].Children[0].Children[1].Children[3];
                var element = parent.Children[2];

                if (!element.IsVisible)
                {
                    var pos = viewAllTabsButton.GetClientRect();
                    MouseUtils.LeftMouseClick(pos.Center + gameWindowPos.TopLeft);

                    int brCounter = 0;
                    while (!element.IsVisible)
                    {
                        Thread.Sleep(50);
                        if (++brCounter > 30) break;
                    }
                }

                var tabPos = element.Children[tabIndex].GetClientRect();

                MouseUtils.LeftMouseClick(tabPos.Center + gameWindowPos.TopLeft);
            }
            catch (Exception e)
            {
                BasePlugin.LogError($"Error in GoToTab: {e}", 5);
            }

            Inventory stash = null;
            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;

            int counter = 0;
            do
            {
                Thread.Sleep(50);
                stash = stashPanel.getStashInventory(tabIndex);
                if (++counter > 100) break;
            }
            while (stash == null || stash.VisibleInventoryItems == null);

            return stash;
        }
    }
}
