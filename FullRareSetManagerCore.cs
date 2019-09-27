using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using FullRareSetManager.SetParts;
using FullRareSetManager.Utilities;
using ImGuiNET;
using SharpDX;

namespace FullRareSetManager
{
    public class FullRareSetManagerCore : BaseSettingsPlugin<FullRareSetManagerSettings>
    {
        private const int INPUT_DELAY = 15;
        private bool _bDropAllItems;
        private Inventory _currentOpenedStashTab;
        private string _currentOpenedStashTabName;
        private CurrentSetInfo _currentSetData;
        private string _drawInfoString = "";
        private DropAllToInventory _inventDrop;
        private BaseSetPart[] _itemSetTypes;
        private StashData _sData;
        public ItemDisplayData[] DisplayData;
        private bool _allowScanTabs = true;

        public override void ReceiveEvent(string eventId, object args)
        {
            if (!Settings.Enable.Value) return;

            if (eventId == "stashie_start_drop_items")
            {
                _allowScanTabs = false;
            }
            else if (eventId == "stashie_stop_drop_items")
            {
                _allowScanTabs = true;
            }
            else if (eventId == "stashie_finish_drop_items_to_stash_tab")
            {
                UpdateStashes();
                UpdatePlayerInventory();
                UpdateItemsSetsInfo();
            }
        }

        public override bool Initialise()
        {
            Input.RegisterKey(Settings.DropToInventoryKey.Value);
            _sData = StashData.Load(this);

            if (_sData == null)
            {
                LogMessage(
                    "RareSetManager: Can't load cached items from file StashData.json. Creating new config. Open stash tabs for updating info. Tell to developer if this happen often enough.",
                    10);

                _sData = new StashData();
            }

            _inventDrop = new DropAllToInventory(this);

            DisplayData = new ItemDisplayData[8];

            for (var i = 0; i <= 7; i++)
            {
                DisplayData[i] = new ItemDisplayData();
            }

            UpdateItemsSetsInfo();

            Settings.WeaponTypePriority.SetListValues(new List<string> {"Two handed", "One handed"});

            Settings.CalcByFreeSpace.OnValueChanged += delegate { UpdateItemsSetsInfo(); };

            //WorldItemsController.OnEntityAdded += args => EntityAdded(args.Entity);
            //WorldItemsController.OnEntityRemoved += args => EntityRemoved(args.Entity);
            //WorldItemsController.OnItemPicked += WorldItemsControllerOnOnItemPicked;
            return true;
        }

        public override void EntityAdded(Entity entity)
        {
            if (!Settings.EnableBorders.Value)
                return;

            if (entity.Type != EntityType.WorldItem)
                return;

            if (!Settings.Enable || GameController.Area.CurrentArea.IsTown ||
                _currentAlerts.ContainsKey(entity))
                return;

            var item = entity.GetComponent<WorldItem>().ItemEntity;

            var visitResult = ProcessItem(item);

            if (visitResult == null)
                return;

            if (Settings.IgnoreOneHanded && visitResult.ItemType == StashItemType.OneHanded)
                visitResult = null;

            if (visitResult == null)
                return;

            var index = (int) visitResult.ItemType;

            if (index > 7)
                index = 0;

            var displData = DisplayData[index];

            _currentAlerts.Add(entity, displData);
        }

        public override void EntityRemoved(Entity entity)
        {
            if (!Settings.EnableBorders.Value)
                return;

            if (entity.Type != EntityType.WorldItem)
                return;

            if (Vector2.Distance(entity.GridPos, GameController.Player.GridPos) < 10)
            {
                //item picked by player?
                var wi = entity.GetComponent<WorldItem>();
                var filteredItemResult = ProcessItem(wi.ItemEntity);

                if (filteredItemResult == null)
                    return;
                filteredItemResult.BInPlayerInventory = true;
                _sData.PlayerInventory.StashTabItems.Add(filteredItemResult);
                UpdateItemsSetsInfo();
            }

            _currentAlerts.Remove(entity);
            _currentLabels.Remove(entity.Address);
        }

        public override void AreaChange(AreaInstance area)
        {
            _currentLabels.Clear();
            _currentAlerts.Clear();
        }

        public override void Render()
        {
            if (!GameController.Game.IngameState.InGame) return;

            if (!_allowScanTabs)
                return;

            var needUpdate = UpdatePlayerInventory();
            var IngameState = GameController.Game.IngameState;
            var stashIsVisible = IngameState.IngameUi.StashElement.IsVisible;
            
            if (stashIsVisible)
                needUpdate = UpdateStashes() || needUpdate;

            if (needUpdate)
            {
                //Thread.Sleep(100);//Wait until item be placed to player invent. There should be some delay
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
                DrawSetsInfo();

            RenderLabels();

            if (Settings.DropToInventoryKey.PressedOnce())
            {
                if (stashIsVisible && IngameState.IngameUi.InventoryPanel.IsVisible)
                {
                    if (_currentSetData.BSetIsReady)
                        _bDropAllItems = true;
                }

                SellSetToVendor();
            }
        }

        public void SellSetToVendor(int callCount = 1)
        {
            try
            {
                // Sell to vendor.
                var gameWindow = GameController.Window.GetWindowRectangle().TopLeft;
                var latency = (int) GameController.Game.IngameState.CurLatency;

                var npcTradingWindow = GameController.Game.IngameState.IngameUi.SellWindow;

                if (!npcTradingWindow.IsVisible)
                {
                    // The vendor sell window is not open, but is in memory (it would've went straigth to catch if that wasn't the case).
                    LogMessage("Error: npcTradingWindow is not visible (opened)!", 5);
                }

                var playerOfferItems = npcTradingWindow.Children[0];
                const int setItemsCount = 9;
                const int uiButtonsCount = 2;

                LogMessage($"Player has put in {playerOfferItems.ChildCount - uiButtonsCount} in the trading window.",
                    3);

                if (playerOfferItems.ChildCount < setItemsCount + uiButtonsCount)
                {
                    for (var i = 0; i < 8; i++)
                    {
                        var itemType = _itemSetTypes[i];
                        var items = itemType.GetPreparedItems();

                        if (items.Any(item => !item.BInPlayerInventory))
                            return;

                        Keyboard.KeyDown(Keys.LControlKey);

                        foreach (var item in items)
                        {
                            var foundItem =
                                GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory]
                                    .VisibleInventoryItems.FirstOrDefault(x => x.InventPosX == item.InventPosX && x.InventPosY == item.InventPosY);

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
                var npcOfferItems = npcTradingWindow.OtherOffer;

                foreach (var element in npcOfferItems.Children)
                {
                    var item = element.AsObject<NormalInventoryItem>().Item;

                    if (string.IsNullOrEmpty(item.Metadata))
                        continue;

                    var itemName = GameController.Files.BaseItemTypes.Translate(item.Metadata).BaseName;
                    if (itemName == "Chaos Orb" || itemName == "Regal Orb") continue;
                    LogMessage($"Npc offered '{itemName}'", 3);
                    if (callCount >= 5) return;
                    var delay = INPUT_DELAY + Settings.ExtraDelay.Value;
                    LogMessage($"Trying to sell set again in {delay} ms.", 3);
                    Thread.Sleep(delay);

                    //SellSetToVendor(callCount++);

                    return;
                }

                Thread.Sleep(latency + Settings.ExtraDelay);
                var acceptButton = npcTradingWindow.AcceptButton;
                Settings.SetsAmountStatistics++;
                Settings.SetsAmountStatisticsText = $"Total sets sold to vendor: {Settings.SetsAmountStatistics}";

                if (Settings.AutoSell.Value)
                {
                    Mouse.SetCursorPosAndLeftClick(acceptButton.GetClientRect().Center + gameWindow,
                        Settings.ExtraDelay.Value);
                }
                else
                    Mouse.SetCursorPos(acceptButton.GetClientRect().Center + gameWindow);
            }
            catch
            {
                LogMessage("We hit catch!", 3);
                Keyboard.KeyUp(Keys.LControlKey);
                Thread.Sleep(INPUT_DELAY);

                // We are not talking to a vendor.
            }
        }

        public void DropAllItems()
        {
            var stashPanel = GameController.IngameState.IngameUi.StashElement;
            var stashNames = stashPanel.AllStashNames;
            var gameWindowPos = GameController.Window.GetWindowRectangle();
            var latency = (int) GameController.Game.IngameState.CurLatency + Settings.ExtraDelay;
            var cursorPosPreMoving = Mouse.GetCursorPosition();

            // Iterrate through all the different item types.
            for (var i = 0; i < 8; i++) //Check that we have enough items for any set
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
                            continue;

                        // Get the index of the item we want to move from stash to inventory.
                        var invIndex = stashNames.IndexOf(curPreparedItem.StashName);

                        // Switch to the tab we want to go to.
                        if (!_inventDrop.SwitchToTab(invIndex, Settings))
                        {
                            //throw new Exception("Can't switch to tab");
                            Keyboard.KeyUp(Keys.LControlKey);
                            return;
                        }

                        Thread.Sleep(latency + Settings.ExtraDelay);

                        // Get the current visible stash tab.
                        _currentOpenedStashTab = stashPanel.VisibleStash;

                        var item = curPreparedItem;

                        var foundItem =
                            _currentOpenedStashTab.VisibleInventoryItems.FirstOrDefault(
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
                                //LogError("Item was not dropped?? : " + curPreparedItem.ItemName + ", checking again...", 10);
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

                        //Thread.Sleep(200);
                        if (!UpdateStashes())
                            LogError("There was item drop but it don't want to update stash!", 10);
                    }
                }
                catch (Exception ex)
                {
                    LogError("Error move items: " + ex.Message, 4);
                }

                Keyboard.KeyUp(Keys.LControlKey);

                //part.RemovePreparedItems();
            }

            UpdatePlayerInventory();
            UpdateItemsSetsInfo();

            Mouse.SetCursorPos(cursorPosPreMoving);
        }

        private void DrawSetsInfo()
        {
            var stash = GameController.IngameState.IngameUi.StashElement;
            var leftPanelOpened = stash.IsVisible;

            if (leftPanelOpened)
            {
                if (_currentSetData.BSetIsReady && _currentOpenedStashTab != null)
                {
                    var visibleInventoryItems = _currentOpenedStashTab.VisibleInventoryItems;

                    if (visibleInventoryItems != null)
                    {
                        var stashTabRect = _currentOpenedStashTab.InventoryUIElement.GetClientRect();

                        var setItemsListRect = new RectangleF(stashTabRect.Right, stashTabRect.Bottom, 270, 240);
                        Graphics.DrawBox(setItemsListRect, new Color(0, 0, 0, 200));
                        Graphics.DrawFrame(setItemsListRect, Color.White, 2);

                        var drawPosX = setItemsListRect.X + 10;
                        var drawPosY = setItemsListRect.Y + 10;

                        Graphics.DrawText("Current " + (_currentSetData.SetType == 1 ? "Chaos" : "Regal") + " set:", new Vector2(drawPosX, drawPosY),
                            Color.White, 15);

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
                                    color = Color.Green;
                                else if (curStashOpened)
                                    color = Color.Yellow;

                                if (!inInventory && curStashOpened)
                                {
                                    var item = curPreparedItem;

                                    var foundItem =
                                        visibleInventoryItems.FirstOrDefault(x => x.InventPosX == item.InventPosX && x.InventPosY == item.InventPosY);

                                    if (foundItem != null)
                                        Graphics.DrawFrame(foundItem.GetClientRect(), Color.Yellow, 2);
                                }

                                Graphics.DrawText(
                                    curPreparedItem.StashName + " (" + curPreparedItem.ItemName + ") " +
                                    (curPreparedItem.LowLvl ? "L" : "H"), new Vector2(drawPosX, drawPosY), color, 15);

                                drawPosY += 20;
                            }
                        }
                    }
                }
            }

            if (Settings.ShowOnlyWithInventory)
            {
                if (!GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
                    return;
            }

            if (Settings.HideWhenLeftPanelOpened)
            {
                if (leftPanelOpened)
                    return;
            }

            var posX = Settings.PositionX.Value;
            var posY = Settings.PositionY.Value;

            var rect = new RectangleF(posX, posY, 230, 200);
            Graphics.DrawBox(rect, new Color(0, 0, 0, 200));
            Graphics.DrawFrame(rect, Color.White, 2);

            posX += 10;
            posY += 10;
            Graphics.DrawText(_drawInfoString, new Vector2(posX, posY), Color.White, 15);
        }

        private void UpdateItemsSetsInfo()
        {
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
                var index = (int) item.ItemType;

                if (index > 7)
                    index = 0; // Switch One/TwoHanded to 0(weapon)

                var setPart = _itemSetTypes[index];
                item.BInPlayerInventory = true;
                setPart.AddItem(item);
            }

            const int StashCellsCount = 12 * 12;

            foreach (var stash in _sData.StashTabs)
            {
                var stashTabItems = stash.Value.StashTabItems;

                foreach (var item in stashTabItems)
                {
                    var index = (int) item.ItemType;

                    if (index > 7)
                        index = 0; // Switch One/TwoHanded to 0(weapon)

                    var setPart = _itemSetTypes[index];
                    item.BInPlayerInventory = false;
                    setPart.AddItem(item);
                    setPart.StashTabItemsCount = stashTabItems.Count;
                }
            }

            //Calculate sets:
            _drawInfoString = "";
            var chaosSetMaxCount = 0;

            var regalSetMaxCount = int.MaxValue;
            var minItemsCount = int.MaxValue;
            var maxItemsCount = 0;

            for (var i = 0; i <= 7; i++) //Check that we have enough items for any set
            {
                var setPart = _itemSetTypes[i];

                var low = setPart.LowSetsCount();
                var high = setPart.HighSetsCount();
                var total = setPart.TotalSetsCount();

                if (minItemsCount > total)
                    minItemsCount = total;

                if (maxItemsCount < total)
                    maxItemsCount = total;

                if (regalSetMaxCount > high)
                    regalSetMaxCount = high;

                chaosSetMaxCount += low;
                _drawInfoString += setPart.GetInfoString() + "\r\n";

                var drawInfo = DisplayData[i];
                drawInfo.TotalCount = total;
                drawInfo.TotalLowCount = low;
                drawInfo.TotalHighCount = high;

                if (Settings.CalcByFreeSpace.Value)
                {
                    var totalPossibleStashItemsCount = StashCellsCount / setPart.ItemCellsSize;

                    drawInfo.FreeSpaceCount = totalPossibleStashItemsCount -
                                              (setPart.StashTabItemsCount + setPart.PlayerInventItemsCount());

                    if (drawInfo.FreeSpaceCount < 0)
                        drawInfo.FreeSpaceCount = 0;

                    drawInfo.PriorityPercent = (float) drawInfo.FreeSpaceCount / totalPossibleStashItemsCount;

                    if (drawInfo.PriorityPercent > 1)
                        drawInfo.PriorityPercent = 1;

                    drawInfo.PriorityPercent = 1 - drawInfo.PriorityPercent;
                }
            }

            if (!Settings.CalcByFreeSpace.Value)
            {
                var maxSets = maxItemsCount;

                if (Settings.MaxSets.Value > 0)
                    maxSets = Settings.MaxSets.Value;

                for (var i = 0; i <= 7; i++)
                {
                    var drawInfo = DisplayData[i];

                    if (drawInfo.TotalCount == 0)
                        drawInfo.PriorityPercent = 0;
                    else
                    {
                        drawInfo.PriorityPercent = (float) drawInfo.TotalCount / maxSets;

                        if (drawInfo.PriorityPercent > 1)
                            drawInfo.PriorityPercent = 1;
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
                return;

            {
                var maxAvailableReplaceCount = 0;
                var replaceIndex = -1;

                var isLowSet = false;

                for (var i = 0; i < 8; i++) //Check that we have enough items for any set
                {
                    var part = _itemSetTypes[i];
                    var prepareResult = part.PrepareItemForSet(Settings);

                    isLowSet = isLowSet || prepareResult.LowSet;

                    if (maxAvailableReplaceCount >= prepareResult.AllowedReplacesCount || prepareResult.BInPlayerInvent)
                        continue;

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
                        //LogMessage("You want to make a regal set anyway? Ok.", 2);
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
            var stashPanel = GameController.IngameState.IngameUi.StashElement;

            if (stashPanel == null)
            {
                LogMessage("ServerData.StashPanel is null", 3);
                return false;
            }

            var needUpdateAllInfo = false;
            _currentOpenedStashTabName = "";
            _currentOpenedStashTab = stashPanel.VisibleStash;

            if (_currentOpenedStashTab == null)
                return false;

            for (var i = 0; i < stashPanel.TotalStashes; i++)
            {
                var stashName = stashPanel.GetStashName(i);

                if (Settings.OnlyAllowedStashTabs.Value)
                {
                    if (!Settings.AllowedStashTabs.Contains(i))
                        continue;
                }

                var stash = stashPanel.GetStashInventoryByIndex(i);

                var visibleInventoryItems = stash?.VisibleInventoryItems;

                if (visibleInventoryItems == null)
                    continue;

                if (_currentOpenedStashTab.Address == stash.Address)
                    _currentOpenedStashTabName = stashName;

                var add = false;

                if (!_sData.StashTabs.TryGetValue(stashName, out var curStashData))
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
                            Graphics.DrawFrame(invItem.GetClientRect(), Color.Red, 2);

                        continue;
                    }

                    newStashItem.StashName = stashName;
                    newStashItem.InventPosX = invItem.InventPosX;
                    newStashItem.InventPosY = invItem.InventPosY;
                    newStashItem.BInPlayerInventory = false;
                    curStashData.StashTabItems.Add(newStashItem);
                }

                curStashData.ItemsCount = (int) stash.ItemCount;

                if (add && curStashData.ItemsCount > 0)
                    _sData.StashTabs.Add(stashName, curStashData);
            }

            if (!needUpdateAllInfo)
                return false;

            return true;
        }

        private bool UpdatePlayerInventory()
        {
            if (!GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
                return false;

            var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];

            if (_sData?.PlayerInventory == null)
                return true;

            _sData.PlayerInventory = new StashTabData();

            var invItems = inventory.VisibleInventoryItems;

            if (invItems == null) return true;

            foreach (var invItem in invItems)
            {
                var item = invItem.Item;
                var newAddedItem = ProcessItem(item);

                if (newAddedItem == null) continue;
                newAddedItem.InventPosX = invItem.InventPosX;
                newAddedItem.InventPosY = invItem.InventPosY;
                newAddedItem.BInPlayerInventory = true;
                _sData.PlayerInventory.StashTabItems.Add(newAddedItem);
            }

            _sData.PlayerInventory.ItemsCount = (int) inventory.ItemCount;

            return true;
        }

        private StashItem ProcessItem(Entity item)
        {
            try
            {
                if (item == null) return null;

                var mods = item?.GetComponent<Mods>();

                if (mods?.ItemRarity != ItemRarity.Rare)
                    return null;

                var bIdentified = mods.Identified;

                if (bIdentified && !Settings.AllowIdentified)
                    return null;

                if (mods.ItemLevel < 60)
                    return null;

                var newItem = new StashItem
                {
                    BIdentified = bIdentified,
                    LowLvl = mods.ItemLevel < 75
                };

                if (string.IsNullOrEmpty(item.Metadata))
                {
                    LogError("Item metadata is empty. Can be fixed by restarting the game", 10);
                    return null;
                }

                if (Settings.IgnoreElderShaper.Value)
                {
                    var baseComp = item.GetComponent<Base>();

                    if (baseComp.isElder || baseComp.isShaper)
                        return null;
                }

                var bit = GameController.Files.BaseItemTypes.Translate(item.Metadata);

                if (bit == null)
                    return null;

                newItem.ItemClass = bit.ClassName;
                newItem.ItemName = bit.BaseName;
                newItem.ItemType = GetStashItemTypeByClassName(newItem.ItemClass);

                if (newItem.ItemType != StashItemType.Undefined)
                    return newItem;
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
                return StashItemType.TwoHanded;

            if (className.StartsWith("One Hand") || className.StartsWith("Thrusting One Hand"))
                return StashItemType.OneHanded;

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

        public override void DrawSettings()
        {
            base.DrawSettings();
            var stashPanel = GameController.Game.IngameState.IngameUi.StashElement;
            var realNames = stashPanel.AllStashNames;

            var uniqId = 0;

            if (ImGui.Button($"Add##{uniqId++}"))
            {
                Settings.AllowedStashTabs.Add(-1);
            }

            for (var i = 0; i < Settings.AllowedStashTabs.Count; i++)
            {
                var value = Settings.AllowedStashTabs[i];

                if (ImGui.Combo(value < realNames.Count && value >= 0 ? realNames[value] : "??", ref value, realNames.ToArray(), realNames.Count))
                {
                    Settings.AllowedStashTabs[i] = value;
                }

                ImGui.SameLine();

                if (ImGui.Button($"Remove##{uniqId++}"))
                {
                    Settings.AllowedStashTabs.RemoveAt(i);
                    i--;
                }
            }
        }

        public override void OnClose()
        {
            if (_sData != null)
                StashData.Save(this, _sData);
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
            public int FreeSpaceCount;
            public float PriorityPercent;
            public int TotalCount;
            public int TotalHighCount;
            public int TotalLowCount;
        }

        #region Draw labels

        private readonly Dictionary<Entity, ItemDisplayData> _currentAlerts =
            new Dictionary<Entity, ItemDisplayData>();

        private Dictionary<long, LabelOnGround> _currentLabels =
            new Dictionary<long, LabelOnGround>();

        private void RenderLabels()
        {
            if (!Settings.EnableBorders.Value)
                return;

            var shouldUpdate = false;

            var tempCopy = new Dictionary<Entity, ItemDisplayData>(_currentAlerts);

            var keyValuePairs = tempCopy.AsParallel().Where(x => x.Key != null && x.Key.Address != 0 && x.Key.IsValid)
                .ToList();

            foreach (var kv in keyValuePairs)
            {
                if (DrawBorder(kv.Key.Address, kv.Value) && !shouldUpdate)
                    shouldUpdate = true;
            }

            if (shouldUpdate)
            {
                _currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                    .Where(y => y?.ItemOnGround != null).GroupBy(y => y.ItemOnGround.Address).ToDictionary(y => y.Key, y => y.First());
            }

            if (!Settings.InventBorders.Value)
                return;

            if (!GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
                return;

            var playerInv = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            var visibleInventoryItems = playerInv.VisibleInventoryItems;

            if (visibleInventoryItems == null)
                return;

            foreach (var inventItem in visibleInventoryItems)
            {
                var item = inventItem.Item;

                if (item == null)
                    continue;

                var visitResult = ProcessItem(item);

                if (visitResult == null) continue;
                var index = (int) visitResult.ItemType;

                if (index > 7)
                    index = 0;

                var data = DisplayData[index];
                var rect = inventItem.GetClientRect();

                var borderColor = Color.Lerp(Color.Red, Color.Green, data.PriorityPercent);

                rect.X += 2;
                rect.Y += 2;

                rect.Width -= 4;
                rect.Height -= 4;

                var testRect = new RectangleF(rect.X + 3, rect.Y + 3, 40, 20);

                Graphics.DrawBox(testRect, new Color(10, 10, 10, 230));
                Graphics.DrawFrame(rect, borderColor, 2);

                Graphics.DrawText(
                    Settings.CalcByFreeSpace.Value ? $"{data.FreeSpaceCount}" : $"{data.PriorityPercent:p0}", testRect.TopLeft,
                    Color.White,
                    Settings.TextSize.Value);
            }
        }

        private bool DrawBorder(long entityAddress, ItemDisplayData data)
        {
            var ui = GameController.Game.IngameState.IngameUi;
            var shouldUpdate = false;

            if (_currentLabels.TryGetValue(entityAddress, out var entityLabel))
            {
                if (!entityLabel.IsVisible) return shouldUpdate;

                var rect = entityLabel.Label.GetClientRect();

                if (ui.OpenLeftPanel.IsVisible && ui.OpenLeftPanel.GetClientRect().Intersects(rect) ||
                    ui.OpenRightPanel.IsVisible && ui.OpenRightPanel.GetClientRect().Intersects(rect))
                    return false;

                var incrSize = Settings.BorderOversize.Value;

                if (Settings.BorderAutoResize.Value)
                    incrSize = (int) Lerp(incrSize, 1, data.PriorityPercent);

                rect.X -= incrSize;
                rect.Y -= incrSize;

                rect.Width += incrSize * 2;
                rect.Height += incrSize * 2;

                var borderColor = Color.Lerp(Color.Red, Color.Green, data.PriorityPercent);

                var borderWidth = Settings.BorderWidth.Value;

                if (Settings.BorderAutoResize.Value)
                    borderWidth = (int) Lerp(borderWidth, 1, data.PriorityPercent);

                Graphics.DrawFrame(rect, borderColor, borderWidth);

                if (Settings.TextSize.Value == 0) return shouldUpdate;

                if (Settings.TextOffsetX < 0)
                    rect.X += Settings.TextOffsetX;
                else
                    rect.X += rect.Width * (Settings.TextOffsetX.Value / 10);

                if (Settings.TextOffsetY < 0)
                    rect.Y += Settings.TextOffsetY;
                else
                    rect.Y += rect.Height * (Settings.TextOffsetY.Value / 10);

                Graphics.DrawText(
                    Settings.CalcByFreeSpace.Value ? $"{data.FreeSpaceCount}" : $"{data.PriorityPercent:p0}", rect.TopLeft,
                    Color.White,
                    Settings.TextSize.Value
                );
            }
            else
                shouldUpdate = true;

            return shouldUpdate;
        }

        private float Lerp(float a, float b, float f)
        {
            return a + f * (b - a);
        }

        #endregion
    }
}
