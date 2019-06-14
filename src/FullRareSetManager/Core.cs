﻿namespace FullRareSetManager
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms; 
    using FullRareSetManager.SetParts;
    using FullRareSetManager.Utilities;
    using PoeHUD.Controllers;
    using PoeHUD.Framework;
    using PoeHUD.Hud.Menu;
    using PoeHUD.Hud.Menu.SettingsDrawers;
    using PoeHUD.Hud.Settings;
    using PoeHUD.Models;
    using PoeHUD.Models.Enums;
    using PoeHUD.Models.Interfaces;
    using PoeHUD.Plugins;
    using PoeHUD.Poe;
    using PoeHUD.Poe.Components;
    using PoeHUD.Poe.Elements;
    using PoeHUD.Poe.RemoteMemoryObjects;
    using SharpDX;

    public class Core : BaseSettingsPlugin<FullRareSetManagerSettings>
    {
        public ItemDisplayData[] DisplayData;

        private const int INPUT_DELAY = 15;
        private bool _bDropAllItems;
        private string _currentOpenedStashTabName;
        private string _drawInfoString = string.Empty;
        private Inventory _currentOpenedStashTab;
        private CurrentSetInfo _currentSetData;
        private DropAllToInventory _inventDrop;
        private BaseSetPart[] _itemSetTypes;
        private StashData _sData;

        public Core()
        {
            this.PluginName = "Rare Set Manager";
        }

        public override void Initialise()
        {
            _sData = StashData.Load(this);
            if (_sData == null)
            {
                LogMessage("RareSetManager: Can't load cached items from file StashData.json. Creating new config. Open stash tabs for updating info.", 10);
                _sData = new StashData();
            }

            _inventDrop = new DropAllToInventory(this);
            DisplayData = new ItemDisplayData[8];
            for (var i = 0; i <= 7; i++)
            {
                DisplayData[i] = new ItemDisplayData();
            }

            UpdateItemsSetsInfo();

            Settings.WeaponTypePriority.SetListValues(new List<string> { "Two handed", "One handed" });
            Settings.CalcByFreeSpace.OnValueChanged += UpdateItemsSetsInfo;
            GameController.Area.OnAreaChange += OnAreaChange;
            MenuPlugin.KeyboardMouseEvents.MouseDown += OnMouseEvent;
            API.SubscribePluginEvent("StashUpdate", ExternalUpdateStashes);
        }

        private void ExternalUpdateStashes(object[] args)
        {
            if (!Settings.Enable.Value)
            {
                return;
            }

            Thread.Sleep(70);
            this.UpdateStashes();
            this.UpdatePlayerInventory();
            this.UpdateItemsSetsInfo();
        }

        private void OnAreaChange(AreaController area)
        {
            _currentLabels.Clear();
            _currentAlerts.Clear();
        }

        public override void Render()
        {
            if (!GameController.Game.IngameState.InGame)
            {
                return;
            }

            var needUpdate = UpdatePlayerInventory();
            var ingameState = GameController.Game.IngameState;
            var stashIsVisible = ingameState.ServerData.StashPanel.IsVisible;

            if (stashIsVisible)
            {
                needUpdate = UpdateStashes() || needUpdate;
            }

            if (needUpdate)
            {
                // Thread.Sleep(100);//Wait until item be placed to player invent. There should be some delay
                UpdateItemsSetsInfo();
            }

            if (_bDropAllItems)
            {
                _bDropAllItems = false;
                try
                {
                    DropAllItems();
                }
                catch
                {
                    LogError("There was an error while moving items.", 5);
                }
                finally
                {
                    UpdatePlayerInventory();
                    UpdateItemsSetsInfo();
                }
            }

            if (!_bDropAllItems)
            {
                DrawSetsInfo();
            }

            RenderLabels();

            if (WinApi.IsKeyDown(Settings.DropToInventoryKey))
            {
                if (stashIsVisible && ingameState.IngameUi.InventoryPanel.IsVisible)
                {
                    if (_currentSetData.BSetIsReady)
                    {
                        _bDropAllItems = true;
                    }
                }
                SellSetToVendor();
            }
        }
        /*
        public void OpenNpcTradewindow(Element merchantMenu)
        {
            var gameWindow = GameController.Window.GetWindowRectangle().TopLeft;
            var sellItemsButton = merchantMenu.Children[0].Children[2].Children[23];

            Mouse.SetCursorPosAndLeftClick(sellItemsButton.GetClientRect().Center + gameWindow, Settings.ExtraDelay);
        }
        */
        public void SellSetToVendor(int callCount = 1)
        {
            try
            {
                // Sell to vendor.
                var gameWindow = GameController.Window.GetWindowRectangle().TopLeft;
                var latency = (int)GameController.Game.IngameState.CurLatency;
                var cursorPosPreMoving = Mouse.GetCursorPosition();
                var npcTradingWindowRoot = GameController.Game.IngameState.UIRoot
                    .Children[1]
                    .Children[74];

                if (npcTradingWindowRoot.ChildCount != 4)
                {
                    var merchantMenu = GameController.Game.IngameState.UIRoot.
                     Children[1].
                     Children[18].
                     Children[6];
                    // The vendor sell window is not open, but is in memory (it would've went straigth to catch if that wasn't the case).
                    try
                    {   
                        if (merchantMenu.IsVisible)
                        {
                            var sellItemsButton = merchantMenu.Children[0].Children[2].Children[20];
                            //opening npcTradinWindow
                            Mouse.SetCursorPosAndLeftClick(sellItemsButton.GetClientRect().Center + gameWindow, Settings.ExtraDelay);
                        }
                    }
                    catch
                    {
                        LogMessage("Error: Merchant Window couldn't be opened", 5);
                    }
                    //LogMessage("Error: Merchant Window isn't open", 10);
                }
                var npcTradingWindow = GameController.Game.IngameState.UIRoot
                    .Children[1]
                    .Children[74]
                    .Children[3];
                var playerOfferItems = npcTradingWindow.Children[0];
                const int setItemsCount = 9;
                const int uiButtonsCount = 2;

                ////LogMessage($"Player has put in {playerOfferItems.ChildCount - uiButtonsCount} in the trading window.",3);
                ////LogMessage($"playerOfferItems.Childcount == {playerOfferItems.ChildCount}",3);
                if (playerOfferItems.ChildCount < setItemsCount + uiButtonsCount)
                {
                    for (var i = 0; i < setItemsCount - 1; i++)
                    {
                        var itemType = _itemSetTypes[i];
                        var items = itemType.GetPreparedItems();

                        if (items.Any(item => !item.BInPlayerInventory))
                        {
                            return;
                        }

                        Keyboard.KeyDown(Keys.LControlKey);
                        foreach (var item in items)
                        {
                            var foundItem =
                                GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory]
                                    .VisibleInventoryItems.Find(
                                        x => x.InventPosX == item.InventPosX && x.InventPosY == item.InventPosY);

                            if (foundItem == null)
                            {
                                LogError("FoundItem was null.", 3);
                                return;
                            }

                            Thread.Sleep(INPUT_DELAY);
                            Mouse.SetCursorPosAndLeftClick(foundItem.GetClientRect().Center + gameWindow,
                                Settings.ExtraDelay);
                            Thread.Sleep(latency + Settings.ExtraDelay);
                        }
                    }

                    Keyboard.KeyUp(Keys.LControlKey);
                }

                Thread.Sleep(INPUT_DELAY + Settings.ExtraDelay.Value);
                var npcOfferItems = npcTradingWindow.Children[1];
                foreach (var element in npcOfferItems.Children)
                {
                    var item = element.AsObject<NormalInventoryItem>().Item;
                    if (item.Path == "")
                    {
                        continue;
                    }

                    var itemName = GameController.Files.BaseItemTypes.Translate(item.Path).BaseName;
                    if (itemName == "Chaos Orb" || itemName == "Regal Orb")
                    {
                        continue;
                    }

                    LogMessage($"Npc offered '{itemName}'", 3);
                    if (callCount >= 5)
                    {
                        return;
                    }

                    var delay = INPUT_DELAY + Settings.ExtraDelay.Value;
                    LogMessage($"Trying to sell set again in {delay} ms.", 3);
                    Thread.Sleep(delay);

                    SellSetToVendor(callCount++);

                    return;
                }

                Thread.Sleep(latency + Settings.ExtraDelay);
                var acceptButton = npcTradingWindow.Children[5];
                if (Settings.AutoSell.Value)
                {
                    Mouse.SetCursorPosAndLeftClick(acceptButton.GetClientRect().Center + gameWindow,
                        Settings.ExtraDelay.Value);
                    Thread.Sleep(INPUT_DELAY);
                    Mouse.SetCursorPos(cursorPosPreMoving);
                }
                else
                {
                    Mouse.SetCursorPos(acceptButton.GetClientRect().Center + gameWindow);
                }
            }
            catch
            {
                LogMessage("We hit catch!", 5);
                Thread.Sleep(INPUT_DELAY);
            }
        }

        public void DropAllItems()
        {
            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;
            var stashNames = stashPanel.AllStashNames;
            var gameWindowPos = GameController.Window.GetWindowRectangle();
            var latency = (int)GameController.Game.IngameState.CurLatency + Settings.ExtraDelay;
            var cursorPosPreMoving = Mouse.GetCursorPosition();

            // Iterrate through all the different item types.
            for (var i = 0; i < 8; i++) // Check that we have enough items for any set
            {
                var part = _itemSetTypes[i];
                var items = part.GetPreparedItems();

                Keyboard.KeyDown(Keys.LControlKey);
                Thread.Sleep(INPUT_DELAY);

                try
                {
                    foreach (var curPreparedItem in items)
                    {
                        // If items is already in our inventory, move on.
                        if (curPreparedItem.BInPlayerInventory)
                        {
                            continue;
                        }

                        // Get the index of the item we want to move from stash to inventory.
                        var invIndex = stashNames.IndexOf(curPreparedItem.StashName);

                        // Switch to the tab we want to go to.
                        if (!_inventDrop.SwitchToTab(invIndex, Settings))
                        {
                            // throw new Exception("Can't switch to tab");
                            Keyboard.KeyUp(Keys.LControlKey);
                            return;
                        }

                        Thread.Sleep(latency + Settings.ExtraDelay);
                        
                        // Get the current visible stash tab.
                        _currentOpenedStashTab = stashPanel.VisibleStash;

                        var item = curPreparedItem;
                        var foundItem =
                            _currentOpenedStashTab.VisibleInventoryItems.Find(
                                x => x.InventPosX == item.InventPosX && x.InventPosY == item.InventPosY);
                        var curItemsCount = _currentOpenedStashTab.VisibleInventoryItems.Count;

                        if (foundItem != null)
                        {
                            // If we found the item.
                            Mouse.SetCursorPosAndLeftClick(foundItem.GetClientRect().Center + gameWindowPos.TopLeft,
                                Settings.ExtraDelay);
                            item.BInPlayerInventory = true;
                            Thread.Sleep(latency + 100 + Settings.ExtraDelay);

                            if (_currentOpenedStashTab.VisibleInventoryItems.Count == curItemsCount)
                            {
                                ////LogError("Item was not dropped?? : " + curPreparedItem.ItemName + ", checking again...", 10);
                                Thread.Sleep(200);

                                if (_currentOpenedStashTab.VisibleInventoryItems.Count == curItemsCount)
                                {
                                    LogError("Item was not dropped after additional delay: " + curPreparedItem.ItemName,
                                        5);
                                }
                            }
                        }
                        else
                        {
                            LogError("We couldn't find the item we where looking for.\n" +
                                     $"ItemName: {item.ItemName}.\n" +
                                     $"Inventory Position: ({item.InventPosX},{item.InventPosY})", 5);
                        }

                        ////Thread.Sleep(200);
                        if (!UpdateStashes())
                        {
                            LogError("There was item drop but it don't want to update stash!", 10);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("Error move items: " + ex.Message, 4);
                }

                Keyboard.KeyUp(Keys.LControlKey);
                ////part.RemovePreparedItems();
            }

            UpdatePlayerInventory();
            UpdateItemsSetsInfo();

            Mouse.SetCursorPos(cursorPosPreMoving);
        }

        private void DrawSetsInfo()
        {
            var stash = GameController.Game.IngameState.ServerData.StashPanel;
            var leftPanelOpened = stash.IsVisible;

            if (leftPanelOpened)
            {
                if (_currentSetData.BSetIsReady && _currentOpenedStashTab != null)
                {
                    var visibleInventoryItems = _currentOpenedStashTab.VisibleInventoryItems;

                    if (visibleInventoryItems != null)
                    {
                        var stashTabRect = _currentOpenedStashTab.InventoryUiElement.GetClientRect();

                        var setItemsListRect = new RectangleF(stashTabRect.Right, stashTabRect.Bottom, 270, 240);
                        Graphics.DrawBox(setItemsListRect, new Color(0, 0, 0, 200));
                        Graphics.DrawFrame(setItemsListRect, 2, Color.White);

                        var drawPosX = setItemsListRect.X + 10;
                        var drawPosY = setItemsListRect.Y + 10;

                        Graphics.DrawText("Current " + (_currentSetData.SetType == 1 ? "Chaos" : "Regal") + " set:", 15,
                            new Vector2(drawPosX, drawPosY));

                        drawPosY += 25;

                        for (var i = 0; i < 8; i++)
                        {
                            var part = _itemSetTypes[i];
                            var items = part.GetPreparedItems();

                            foreach (var curPreparedItem in items)
                            {
                                var inInventory = _sData.PlayerInventory.StashTabItems.Contains(curPreparedItem);
                                var curStashOpened = curPreparedItem.StashName == _currentOpenedStashTabName;
                                var color = Color.Gray;

                                if (inInventory)
                                {
                                    color = Color.Green;
                                }
                                else if (curStashOpened)
                                {
                                    color = Color.Yellow;
                                }

                                if (!inInventory && curStashOpened)
                                {
                                    var item = curPreparedItem;
                                    var foundItem =
                                        visibleInventoryItems.Find(x => x.InventPosX == item.InventPosX &&
                                                                        x.InventPosY == item.InventPosY);

                                    if (foundItem != null)
                                    {
                                        Graphics.DrawFrame(foundItem.GetClientRect(), 2, Color.Yellow);
                                    }
                                }

                                Graphics.DrawText(
                                    curPreparedItem.StashName + " (" + curPreparedItem.ItemName + ") " +
                                    (curPreparedItem.itemlvl < 75 ? "L" : "H"), 15, new Vector2(drawPosX, drawPosY), color);
                                drawPosY += 20;
                            }
                        }
                    }
                }
            }

            if (Settings.ShowOnlyWithInventory)
            {
                if (!GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
                {
                    return;
                }
            }

            if (Settings.HideWhenLeftPanelOpened)
            {
                if (leftPanelOpened)
                {
                    return;
                }
            }

            var posX = Settings.PositionX.Value;
            var posY = Settings.PositionY.Value;

            var rect = new RectangleF(posX, posY, 230, 200);
            Graphics.DrawBox(rect, new Color(0, 0, 0, 200));
            Graphics.DrawFrame(rect, 2, Color.White);

            posX += 10;
            posY += 10;
            Graphics.DrawText(_drawInfoString, 15, new Vector2(posX, posY));
        }

        private void UpdateItemsSetsInfo()
        {
            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;
            _currentOpenedStashTab = stashPanel.VisibleStash;

            _currentSetData = new CurrentSetInfo();
            _itemSetTypes = new BaseSetPart[8];
            _itemSetTypes[0] = new WeaponItemsSetPart("Weapons") {ItemCellsSize = 8};
            _itemSetTypes[1] = new SingleItemSetPart("Helmets") {ItemCellsSize = 4};
            _itemSetTypes[2] = new SingleItemSetPart("Body Armors") {ItemCellsSize = 6};
            _itemSetTypes[3] = new SingleItemSetPart("Gloves") {ItemCellsSize = 4};
            _itemSetTypes[4] = new SingleItemSetPart("Boots") {ItemCellsSize = 4};
            _itemSetTypes[5] = new SingleItemSetPart("Belts") {ItemCellsSize = 2};
            _itemSetTypes[6] = new SingleItemSetPart("Amulets") {ItemCellsSize = 1};
            _itemSetTypes[7] = new RingItemsSetPart("Rings") {ItemCellsSize = 1};

            for (var i = 0; i <= 7; i++)
            {
                DisplayData[i].BaseData = _itemSetTypes[i];
            }

            foreach (var item in _sData.PlayerInventory.StashTabItems)
            {
                var index = (int)item.ItemType;

                if (index > 7)
                {
                    index = 0; // Switch One/TwoHanded to 0(weapon)
                }

                var setPart = _itemSetTypes[index];
                item.BInPlayerInventory = true;
                setPart.AddItem(item);
            }

            const int StashCellsCount = 12 * 12;
            /*
            if (_currentOpenedStashTab.InvType == InventoryType.QuadStash)
            {
                StashCellsCount = 24 * 24;
            }
            */
            foreach (var stash in _sData.StashTabs)
            {
                var stashTabItems = stash.Value.StashTabItems;
                foreach (var item in stashTabItems)
                {
                    var index = (int)item.ItemType;
                    if (index > 7)
                    {
                        index = 0; // Switch One/TwoHanded to 0(weapon)
                    }

                    var setPart = _itemSetTypes[index];
                    item.BInPlayerInventory = false;
                    setPart.AddItem(item);
                    setPart.StashTabItemsCount = stashTabItems.Count;
                }
            }
            // Calculate sets:
            _drawInfoString = "";
            var chaosSetMaxCount = 0;
            var regalSetMaxCount = int.MaxValue;
            var minItemsCount = int.MaxValue;
            var maxItemsCount = 0;

            for (var i = 0; i <= 7; i++) // Check that we have enough items for any set
            {
                var setPart = _itemSetTypes[i];
                var low = setPart.LowSetsCount();
                var high = setPart.HighSetsCount();
                var total = setPart.TotalSetsCount();

                if (minItemsCount > total)
                {
                    minItemsCount = total;
                }
                    
                if (maxItemsCount < total)
                {
                    maxItemsCount = total;
                }
                    
                if (regalSetMaxCount > high)
                {
                    regalSetMaxCount = high;
                }
                   
                chaosSetMaxCount += low;
                _drawInfoString += setPart.GetInfoString() + "\r\n";

                var drawInfo = DisplayData[i];
                drawInfo.TotalCount = total;
                drawInfo.TotalLowCount = low;
                drawInfo.TotalHighCount = high;

                if (Settings.CalcByFreeSpace.Value)
                {
                    int totalPossibleStashItemsCount = StashCellsCount / setPart.ItemCellsSize;

                    drawInfo.FreeSpaceCount = totalPossibleStashItemsCount -
                                              (setPart.StashTabItemsCount + setPart.PlayerInventItemsCount());

                    if (drawInfo.FreeSpaceCount < 0)
                    {
                        drawInfo.FreeSpaceCount = 0;
                    }

                    drawInfo.PriorityPercent = (float)drawInfo.FreeSpaceCount / totalPossibleStashItemsCount;
                    if (drawInfo.PriorityPercent > 1)
                    {
                        drawInfo.PriorityPercent = 1;
                    }

                    drawInfo.PriorityPercent = 1 - drawInfo.PriorityPercent;
                }
            }

            if (!Settings.CalcByFreeSpace.Value)
            {
                var maxSets = maxItemsCount;

                if (Settings.MaxSets.Value > 0)
                {
                    maxSets = Settings.MaxSets.Value;
                }

                for (var i = 0; i <= 7; i++)
                {
                    var drawInfo = DisplayData[i];

                    if (drawInfo.TotalCount == 0)
                    {
                        drawInfo.PriorityPercent = 0;
                    }     
                    else
                    {
                        drawInfo.PriorityPercent = (float)drawInfo.TotalCount / maxSets;

                        if (drawInfo.PriorityPercent > 1)
                        {
                            drawInfo.PriorityPercent = 1;
                        }
                    }
                }
            }

            _drawInfoString += "\r\n";

            var chaosSets = Math.Min(minItemsCount, chaosSetMaxCount);

            _drawInfoString += "Chaos sets ready: " + chaosSets;

            if (Settings.ShowRegalSets.Value)
            {
                _drawInfoString += "\r\n";
                _drawInfoString += "Regal sets ready: " + regalSetMaxCount;
            }

            if (chaosSets <= 0 && regalSetMaxCount <= 0)
            {
                return;
            }

            {
                var maxAvailableReplaceCount = 0;
                var replaceIndex = -1;

                var isLowSet = false;
                for (var i = 0; i < 8; i++) // Check that we have enough items for any set
                {
                    var part = _itemSetTypes[i];
                    var prepareResult = part.PrepareItemForSet(Settings);

                    isLowSet = isLowSet || prepareResult.LowSet;

                    if (maxAvailableReplaceCount >= prepareResult.AllowedReplacesCount || prepareResult.BInPlayerInvent)
                    {
                        continue;
                    }

                    maxAvailableReplaceCount = prepareResult.AllowedReplacesCount;
                    replaceIndex = i;
                }

                if (!isLowSet)
                {
                    if (Settings.ShowRegalSets)
                    {
                        _currentSetData.BSetIsReady = true;
                        _currentSetData.SetType = 2;
                        return;
                    }

                    if (maxAvailableReplaceCount == 0)
                    {
                        ////LogMessage("You want to make a regal set anyway? Ok.", 2);
                        _currentSetData.BSetIsReady = true;
                        _currentSetData.SetType = 2;
                        return;
                    }

                    if (replaceIndex != -1)
                    {
                        _itemSetTypes[replaceIndex].DoLowItemReplace();
                        _currentSetData.SetType = 1;
                        _currentSetData.BSetIsReady = true;
                    }
                    else
                    {
                        _currentSetData.BSetIsReady = true;
                        _currentSetData.SetType = 1;
                    }
                }
                else
                {
                    _currentSetData.BSetIsReady = true;
                    _currentSetData.SetType = 1;
                }
            }
        }

        public bool UpdateStashes()
        {
            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;

            if (stashPanel == null)
            {
                LogMessage("ServerData.StashPanel is null", 3);
                return false;
            }

            var needUpdateAllInfo = false;
            _currentOpenedStashTab = null;
            _currentOpenedStashTabName = "";

            for (var i = 0; i < stashPanel.TotalStashes; i++)
            {
                var stashName = stashPanel.GetStashName(i);

                if (Settings.OnlyAllowedStashTabs.Value)
                {
                    if (Settings.AllowedStashTabs.All(x => x.Name != stashName))
                    {
                        continue;
                    }
                }

                var stash = stashPanel.GetStashInventoryByIndex(i);
                var visibleInventoryItems = stash?.VisibleInventoryItems;

                if (visibleInventoryItems == null)
                {
                    continue;
                }
                    
                _currentOpenedStashTab = stash;
                _currentOpenedStashTabName = stashName;

                StashTabData curStashData = null;
                var add = false;

                if (!_sData.StashTabs.TryGetValue(stashName, out curStashData))
                {
                    curStashData = new StashTabData();
                    add = true;
                }

                curStashData.StashTabItems = new List<StashItem>();
                needUpdateAllInfo = true;

                foreach (var invItem in visibleInventoryItems)
                {
                    var item = invItem.Item;
                    var newStashItem = ProcessItem(item);

                    if (newStashItem == null)
                    {
                        if (Settings.ShowRedRectangleAroundIgnoredItems)
                        {
                            Graphics.DrawFrame(invItem.GetClientRect(), 2, Color.Red);
                        }

                        continue;
                    }

                    newStashItem.StashName = stashName;
                    newStashItem.InventPosX = invItem.InventPosX;
                    newStashItem.InventPosY = invItem.InventPosY;
                    newStashItem.BInPlayerInventory = false;
                    curStashData.StashTabItems.Add(newStashItem);
                }

                curStashData.ItemsCount = (int)stash.ItemCount;

                if (add && curStashData.ItemsCount > 0)
                {
                    _sData.StashTabs.Add(stashName, curStashData);
                }
            }

            List<string> workingTabs = null;

            workingTabs = Settings.OnlyAllowedStashTabs.Value
                ? Settings.AllowedStashTabs.Select(x => x.Name).ToList()
                : stashPanel.AllStashNames;

            var keyTabs = _sData.StashTabs.Keys.ToList(); // Delete stashes that doesn't exist
            foreach (var stashNameInUse in keyTabs)
            {
                if (!workingTabs.Contains(stashNameInUse))
                {
                    _sData.StashTabs.Remove(stashNameInUse);
                }
            }

            if (!needUpdateAllInfo)
            {
                return false;
            }

            return true;
        }

        private bool UpdatePlayerInventory()
        {
            if (!GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
            {
                return false;
            }

            var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];

            if (_sData?.PlayerInventory == null)
            {
                return true;
            }

            _sData.PlayerInventory = new StashTabData();

            var invItems = inventory.VisibleInventoryItems;

            if (invItems == null)
            {
                return true;
            }

            foreach (var invItem in invItems)
            {
                var item = invItem.Item;
                var newAddedItem = ProcessItem(item);

                if (newAddedItem == null)
                {
                    continue;
                }

                newAddedItem.InventPosX = invItem.InventPosX;
                newAddedItem.InventPosY = invItem.InventPosY;
                newAddedItem.BInPlayerInventory = true;
                _sData.PlayerInventory.StashTabItems.Add(newAddedItem);
            }

            _sData.PlayerInventory.ItemsCount = (int)inventory.ItemCount;

            return true;
        }

        private long CurPickItemCount = 0;

        private void OnMouseEvent(object sender, MouseEventArgs e)
        {
            try
            {
                if (!Settings.Enable || !GameController.Window.IsForeground() || e.Button != MouseButtons.Left)
                {
                    return;
                }

                Element uiHover = GameController.Game.IngameState.UIHover;
                var HoverItemIcon = uiHover.AsObject<HoverItemIcon>();

                if (HoverItemIcon.ToolTipType == ToolTipType.ItemOnGround)
                {
                    var item = HoverItemIcon.Item;

                    var filteredItemResult = ProcessItem(item);

                    if (filteredItemResult == null)
                    {
                        return;
                    }

                    if (++CurPickItemCount > long.MaxValue)
                    {
                        CurPickItemCount = 0;
                    }

                    Task.Factory.StartNew(async () =>
                    {
                        long curItemPickCount = CurPickItemCount;
                        Stopwatch sw = Stopwatch.StartNew();
                        while (item.IsValid)
                        {
                            await Task.Delay(30);

                            // We want to prevent the item was added more than once
                            if (curItemPickCount != CurPickItemCount)
                            {
                                return;
                            }

                            if (sw.ElapsedMilliseconds <= 10000)
                            {
                                continue;
                            }

                            sw.Stop();
                            break;
                        }

                        // We want to prevent the item was added more than once
                        if (curItemPickCount != CurPickItemCount)
                        {
                            return;
                        }

                        if (!item.IsValid)
                        {
                            filteredItemResult.BInPlayerInventory = true;
                            _sData.PlayerInventory.StashTabItems.Add(filteredItemResult);
                            UpdateItemsSetsInfo();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LogError("OnMouseEvent error: " + ex.Message, 4);
                return;
            }

            return;
        }

        private StashItem ProcessItem(IEntity item)
        {
            try
            {
                if (item == null)
                {
                    return null;
                }

                var mods = item?.GetComponent<Mods>();
                if (mods?.ItemRarity != ItemRarity.Rare)
                {
                    return null;
                }

                var bIdentified = mods.Identified;
                if (bIdentified && !Settings.AllowIdentified)
                {
                    return null;
                }

                if (mods.ItemLevel < 60)
                {
                    return null;
                }

                var newItem = new StashItem
                {
                    BIdentified = bIdentified,
                    itemlvl = mods.ItemLevel
                };

                if (string.IsNullOrEmpty(item.Path))
                {
                    LogError($"Item metadata is empty. Can be fixed by restarting the game", 10);
                    return null;
                }

                if (Settings.IgnoreElderShaper.Value)
                {
                    var baseComp = item.GetComponent<Base>();
                    if (baseComp.isElder || baseComp.isShaper)
                    {
                        return null;
                    }
                }

                var bit = GameController.Files.BaseItemTypes.Translate(item.Path);
                if (bit == null)
                {
                    return null;
                }
                    
                newItem.ItemClass = bit.ClassName;
                newItem.ItemName = bit.BaseName;
                newItem.ItemType = GetStashItemTypeByClassName(newItem.ItemClass);
                if (newItem.ItemType != StashItemType.Undefined)
                {
                    return newItem;
                }
            }
            catch (Exception e)
            {
                LogError($"Error in \"ProcessItem\": {e}", 10);
                return null;
            }

            return null;
        }

        private StashItemType GetStashItemTypeByClassName(string className)
        {
            if (className.StartsWith("Two Hand"))
            {
                return StashItemType.TwoHanded;
            }

            if (className.StartsWith("One Hand") || className.StartsWith("Thrusting One Hand"))
            {
                return StashItemType.OneHanded;
            }

            switch (className)
            {
                case "Bow": return StashItemType.TwoHanded;
                case "Staff": return StashItemType.TwoHanded;
                case "Sceptre": return StashItemType.OneHanded;
                case "Wand": return StashItemType.OneHanded;
                case "Dagger": return StashItemType.OneHanded;
                case "Claw": return StashItemType.OneHanded;
                case "Shield": return StashItemType.OneHanded;

                case "Ring": return StashItemType.Ring;
                case "Amulet": return StashItemType.Amulet;
                case "Belt": return StashItemType.Belt;

                case "Helmet": return StashItemType.Helmet;
                case "Body Armour": return StashItemType.Body;
                case "Boots": return StashItemType.Boots;
                case "Gloves": return StashItemType.Gloves;

                default:
                    return StashItemType.Undefined;
            }
        }

        private CheckboxSettingDrawer AllowedStashTabsRoot;
        private List<StashTabNodeSettingDrawer> StashTabDrawers = new List<StashTabNodeSettingDrawer>();

        public override void InitializeSettingsMenu()
        {
            base.InitializeSettingsMenu();

            AllowedStashTabsRoot =
                new CheckboxSettingDrawer(Settings.OnlyAllowedStashTabs, "Allowed Stash Tabs", GetUniqDrawerId());
            SettingsDrawers.Add(AllowedStashTabsRoot); // Adding checkbox to settings menu drawers

            var addTabButton = new ButtonNode();
            var addTabButtonDrawer = new ButtonSettingDrawer(addTabButton, "Add Stash Tab", GetUniqDrawerId());
            AllowedStashTabsRoot.Children.Add(addTabButtonDrawer);

            addTabButton.OnPressed += delegate
            {
                var newNode = new StashTabNode();
                AddAllowedStashTabNode(newNode);
                Settings.AllowedStashTabs.Add(newNode);
            };

            foreach (var node in Settings.AllowedStashTabs)
            {
                AddAllowedStashTabNode(node);
            }
        }

        private void AddAllowedStashTabNode(StashTabNode node)
        {
            var deleteTabButton = new ButtonNode();
            var deleteTabButtonDrawer = new ButtonSettingDrawer(deleteTabButton, "Delete", GetUniqDrawerId());
            AllowedStashTabsRoot.Children.Insert(AllowedStashTabsRoot.Children.Count - 1, deleteTabButtonDrawer);

            var buttonSameLineDrawer =
                new SameLineSettingDrawer("", GetUniqDrawerId()); // Delete button and stash node should be on same line
            AllowedStashTabsRoot.Children.Insert(AllowedStashTabsRoot.Children.Count - 1, buttonSameLineDrawer);

            var stashNodeDrawer = new StashTabNodeSettingDrawer(node, "", GetUniqDrawerId());
            AllowedStashTabsRoot.Children.Insert(AllowedStashTabsRoot.Children.Count - 1,
                stashNodeDrawer); ////AllowedStashTabsRoot.Children.Count - 1, 

            deleteTabButton.OnPressed += delegate
            {
                // Delete stash node related drawers
                AllowedStashTabsRoot.Children.Remove(deleteTabButtonDrawer);
                AllowedStashTabsRoot.Children.Remove(buttonSameLineDrawer);
                AllowedStashTabsRoot.Children.Remove(stashNodeDrawer);
                Settings.AllowedStashTabs.Remove(stashNodeDrawer.StashNode);
                StashTabController.UnregisterStashNode(stashNodeDrawer
                    .StashNode); // No sense to update data in deleted stash node
            };

            StashTabController.RegisterStashNode(node);
        }

        public override void OnPluginDestroyForHotReload()
        {
            Settings.AllowedStashTabs.ForEach(x =>
                StashTabController.UnregisterStashNode(x)); // Unregistering stash tab nodes (hot reload)

            // Unsubscribing for hot reload or plugin update
            MenuPlugin.KeyboardMouseEvents.MouseDown -= OnMouseEvent;
            GameController.Area.OnAreaChange -= OnAreaChange;
            API.UnsubscribePluginEvent("StashUpdate");
        }

        public override void OnClose()
        {
            if (_sData != null)
            {
                StashData.Save(this, _sData);
            }   
        }

        private struct CurrentSetInfo
        {
            public bool BSetIsReady;
            public int SetType; // 1 - chaos set, 2 - regal set
        }

        /*Rare set classes:

        Two Hand Sword
        Two Hand Axe
        Two Hand Mace
        Bow
        Staff

        One Hand Sword
        One Hand Axe
        One Hand Mace
        Sceptre
        Wand
        Dagger
        Claw

        Ring
        Amulet
        Belt
        Shield
        Helmet
        Body Armour
        Boots
        Gloves
        */
        public class ItemDisplayData
        {
            public BaseSetPart BaseData;
            public float PriorityPercent;
            public int TotalCount;
            public int TotalHighCount;
            public int TotalLowCount;
            public int FreeSpaceCount;
        }

        #region Draw labels

        private readonly Dictionary<EntityWrapper, ItemDisplayData> _currentAlerts =
            new Dictionary<EntityWrapper, ItemDisplayData>();

        private Dictionary<long, LabelOnGround> _currentLabels =
            new Dictionary<long, LabelOnGround>();

        private void RenderLabels()
        {
            if (!Settings.EnableBorders.Value)
            {
                return;
            }

            var shouldUpdate = false;

            var tempCopy = new Dictionary<EntityWrapper, ItemDisplayData>(_currentAlerts);
            var keyValuePairs = tempCopy.AsParallel().Where(x => x.Key != null && x.Key.Address != 0 && x.Key.IsValid)
                .ToList();
            foreach (var kv in keyValuePairs)
            {
                if (DrawBorder(kv.Key.Address, kv.Value) && !shouldUpdate)
                {
                    shouldUpdate = true;
                }
            }

            if (shouldUpdate)
            {
                _currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                    .GroupBy(y => y.ItemOnGround.Address).ToDictionary(y => y.Key, y => y.First());
            }

            if (!Settings.InventBorders.Value)
            {
                return;
            }

            if (!GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
            {
                return;
            }

            var playerInv = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            var visibleInventoryItems = playerInv.VisibleInventoryItems;
            if (visibleInventoryItems == null)
            {
                return;
            }

            foreach (var inventItem in visibleInventoryItems)
            {
                var item = inventItem.Item;
                if (item == null)
                {
                    continue;
                }

                var visitResult = ProcessItem(item);
                if (visitResult == null)
                {
                    continue;
                }

                var index = (int)visitResult.ItemType;
                if (index > 7)
                {
                    index = 0;
                }

                var data = DisplayData[index];
                var rect = inventItem.GetClientRect();

                var borderColor = Color.Lerp(Color.Red, Color.Green, data.PriorityPercent);

                rect.X += 2;
                rect.Y += 2;

                rect.Width -= 4;
                rect.Height -= 4;

                var testRect = new RectangleF(rect.X + 3, rect.Y + 3, 40, 20);

                Graphics.DrawBox(testRect, new Color(10, 10, 10, 230));
                Graphics.DrawFrame(rect, 2, borderColor);

                Graphics.DrawText(
                    Settings.CalcByFreeSpace.Value ? $"{data.FreeSpaceCount}" : $"{data.PriorityPercent:p0}",
                    Settings.TextSize.Value, testRect.TopLeft,
                    Color.White);
            }
        }

        private bool DrawBorder(long entityAddress, ItemDisplayData data)
        {
            var ui = GameController.Game.IngameState.IngameUi;
            var shouldUpdate = false;
            if (_currentLabels.TryGetValue(entityAddress, out var entityLabel))
            {
                if (!entityLabel.IsVisible)
                {
                    return shouldUpdate;
                }

                var rect = entityLabel.Label.GetClientRect();
                if (ui.OpenLeftPanel.IsVisible && ui.OpenLeftPanel.GetClientRect().Intersects(rect) ||
                    ui.OpenRightPanel.IsVisible && ui.OpenRightPanel.GetClientRect().Intersects(rect))
                {
                    return false;
                }

                var incrSize = Settings.BorderOversize.Value;
                if (Settings.BorderAutoResize.Value)
                {
                    incrSize = (int)Lerp(incrSize, 1, data.PriorityPercent);
                }

                rect.X -= incrSize;
                rect.Y -= incrSize;
                rect.Width += incrSize * 2;
                rect.Height += incrSize * 2;

                var borderColor = Color.Lerp(Color.Red, Color.Green, data.PriorityPercent);
                var borderWidth = Settings.BorderWidth.Value;
                if (Settings.BorderAutoResize.Value)
                {
                    borderWidth = (int)Lerp(borderWidth, 1, data.PriorityPercent);
                }

                Graphics.DrawFrame(rect, borderWidth, borderColor);
                if (Settings.TextSize.Value == 0)
                {
                    return shouldUpdate;
                }

                if (Settings.TextOffsetX < 0)
                {
                    rect.X += Settings.TextOffsetX;
                }
                else
                {
                    rect.X += rect.Width * (Settings.TextOffsetX.Value / 10);
                }

                if (Settings.TextOffsetY < 0)
                {
                    rect.Y += Settings.TextOffsetY;
                }
                else
                {
                    rect.Y += rect.Height * (Settings.TextOffsetY.Value / 10);
                }

                Graphics.DrawText(
                    Settings.CalcByFreeSpace.Value ? $"{data.FreeSpaceCount}" : $"{data.PriorityPercent:p0}",
                    Settings.TextSize.Value, rect.TopLeft,
                    Color.White
                );
            }
            else
            {
                shouldUpdate = true;
            }

            return shouldUpdate;
        }

        private float Lerp(float a, float b, float f)
        {
            return a + f * (b - a);
        }

        public override void EntityAdded(EntityWrapper entity)
        {
            if (!Settings.EnableBorders.Value)
            {
                return;
            }

            if (!Settings.Enable || entity == null || GameController.Area.CurrentArea.IsTown ||
                _currentAlerts.ContainsKey(entity) || !entity.HasComponent<WorldItem>())
            {
                return;
            }

            var item = entity.GetComponent<WorldItem>().ItemEntity;
            var visitResult = ProcessItem(item);
            if (visitResult == null)
            {
                return;
            }

            if (Settings.IgnoreOneHanded && visitResult.ItemType == StashItemType.OneHanded)
            {
                visitResult = null;
            }

            if (visitResult == null)
            {
                return;
            }

            var index = (int)visitResult.ItemType;
            if (index > 7)
            {
                index = 0;
            }

            var displData = DisplayData[index];
            _currentAlerts.Add(entity, displData);
        }

        public override void EntityRemoved(EntityWrapper entity)
        {
            if (!Settings.EnableBorders.Value)
            {
                return;
            }

            _currentAlerts.Remove(entity);
            _currentLabels.Remove(entity.Address);
        }

        #endregion
    }
}