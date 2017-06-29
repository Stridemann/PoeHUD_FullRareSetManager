using System;
using System.Collections.Generic;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Hud.UI;
using PoeHUD.Poe.RemoteMemoryObjects;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using SharpDX;
using PoeHUD.Framework;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using PoeHUD.Poe.Elements;
using PoeHUD.Controllers;
using PoeHUD.Models.Interfaces;

namespace FullRareSetManager
{
    public class FullRareSetManager : BaseSettingsPlugin<FullRareSetManager_Settings>
    {
        private StashData SData;
        private DropAllToInventory InventDrop;
        public override void Initialise()
        {
            SData = StashData.Load(this);
            InventDrop = new DropAllToInventory(this);

            DisplayData = new ItemDisplayData[8];

            for (int i = 0; i <= 7; i++)
                DisplayData[i] = new ItemDisplayData();

            UpdateItemsSetsInfo();

            GameController.Area.OnAreaChange += OnAreaChange;
        }

        private void OnAreaChange(AreaController area)
        {
            currentLabels.Clear();
            currentAlerts.Clear();
        }
        public ItemDisplayData[] DisplayData;
        #region Draw labels

        private readonly Dictionary<EntityWrapper, ItemDisplayData> currentAlerts = new Dictionary<EntityWrapper, ItemDisplayData>();
        private Dictionary<long, ItemsOnGroundLabelElement> currentLabels = new Dictionary<long, ItemsOnGroundLabelElement>();

        private void RenderLabels()
        {
            if (!Settings.EnableBorders.Value) return;
            bool shouldUpdate = false;

            Dictionary<EntityWrapper, ItemDisplayData> tempCopy = new Dictionary<EntityWrapper, ItemDisplayData>(currentAlerts);
            var keyValuePairs = tempCopy.AsParallel().Where(x => x.Key != null && x.Key.Address != 0 && x.Key.IsValid).ToList();
            foreach (var kv in keyValuePairs)
            {
                if (DrawBorder(kv.Key.Address, kv.Value) && !shouldUpdate)
                {
                    shouldUpdate = true;
                }
            }

            if (shouldUpdate)
            {
                currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                    .GroupBy(y => y.ItemOnGround.Address).ToDictionary(y => y.Key, y => y.First());
            }
        }


        private bool DrawBorder(long entityAddress, ItemDisplayData data)
        {
            IngameUIElements ui = GameController.Game.IngameState.IngameUi;
            ItemsOnGroundLabelElement entityLabel;
            bool shouldUpdate = false;
            if (currentLabels.TryGetValue(entityAddress, out entityLabel))
            {
                if (entityLabel.IsVisible)
                {
                    RectangleF rect = entityLabel.Label.GetClientRect();
                    if ((ui.OpenLeftPanel.IsVisible && ui.OpenLeftPanel.GetClientRect().Intersects(rect)) ||
                        (ui.OpenRightPanel.IsVisible && ui.OpenRightPanel.GetClientRect().Intersects(rect)))
                    {
                        return false;
                    }
                    int incrSize = Settings.BorderOversize.Value;

                    if (Settings.BorderAutoResize.Value)
                        incrSize = (int)Lerp(incrSize, 1, data.PriorityScale);

                    rect.X -= incrSize;
                    rect.Y -= incrSize;

                    rect.Width += incrSize * 2;
                    rect.Height += incrSize * 2;

                    Color borderColor = Color.Lerp(Color.Red, Color.Green, data.PriorityScale);


                    var borderWidth = Settings.BorderWidth.Value;

                    if (Settings.BorderAutoResize.Value)
                        borderWidth = (int)Lerp(borderWidth, 1, data.PriorityScale);

                    Graphics.DrawFrame(rect, borderWidth, borderColor);

                    if (Settings.TextSize.Value != 0)
                    {
                        if (Settings.TextOffsetX < 0)
                            rect.X += Settings.TextOffsetX;
                        else
                            rect.X += rect.Width * (Settings.TextOffsetX.Value / 10);

                        if (Settings.TextOffsetY < 0)
                            rect.Y += Settings.TextOffsetY;
                        else
                            rect.Y += rect.Height * (Settings.TextOffsetY.Value / 10);

                        Graphics.DrawText(data.PriorityPercent + "%", Settings.TextSize.Value, rect.TopLeft, Color.White);
                    }
                }
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
            if (!Settings.EnableBorders.Value) return;

            if (Settings.Enable && entity != null && !GameController.Area.CurrentArea.IsTown
                && !currentAlerts.ContainsKey(entity) && entity.HasComponent<WorldItem>())
            {
                Entity item = entity.GetComponent<WorldItem>().ItemEntity;

                var visitResult = ProcessItem(item);

                if(visitResult != null)
                {
                    int index = (int)visitResult.ItemType;

                    if (index > 7)
                        index = 0;

                    var setPart = DisplayData[index];

                    currentAlerts.Add(entity, setPart);
                }
               
            }
        }

        public override void EntityRemoved(EntityWrapper entity)
        {
            if (!Settings.EnableBorders.Value) return;

            currentAlerts.Remove(entity);
            currentLabels.Remove(entity.Address);
        }

        #endregion



        public override void OnClose()
        {
            if(SData != null)
                StashData.Save(this, SData);
        }

        public override void Render()
        {
            bool needUpdate = UpdatePlayerInventory();
            var ingameState = GameController.Game.IngameState;
            var stashIsVisible = ingameState.ServerData.StashPanel.IsVisible;
            if (stashIsVisible)
            {
                needUpdate = UpdateStashes() || needUpdate;
            }

            if (needUpdate)
            {
                //Thread.Sleep(100);//Wait until item be placed to player invent. There should be some delay
                UpdateItemsSetsInfo();
            }


            var viewAllTabsButton = ingameState.UIRoot.Children[1].Children[21].Children[2]
                    .Children[0]
                    .Children[1].Children[2];

            //Graphics.DrawFrame(viewAllTabsButton.GetClientRect(), 2, Color.Red);


            if (bDropAllItems)
            {
                bDropAllItems = false;
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


            if (WinApi.IsKeyDown(Settings.DropToInventoryKey))
            {
                if(stashIsVisible && ingameState.IngameUi.InventoryPanel.IsVisible)
                {
                    if (CurrentSetData.bSetIsReady)
                    {
                        bDropAllItems = true;
                    }
                }
            }

            if (!bDropAllItems)
            {
                DrawSetsInfo();
            }

            RenderLabels();
        }

        private bool bDropAllItems = false;

        public void DropAllItems()
        {
            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;
            var stashNames = stashPanel.AllStashNames;
            int currentTab = -1;
            var gameWindowPos = GameController.Window.GetWindowRectangle();

            for (int i = 0; i < 8; i++)//Check that we have enough items for any set
            {
                var part = ItemSetTypes[i];
                var items = part.GetPreparedItems();

                for (int j = 0; j < items.Length; j++)
                {
                    var curPreparedItem = items[j];
                    if (curPreparedItem.bInPlayerInventory) continue;
                    var invIndex = stashNames.IndexOf(curPreparedItem.StashName);
                  
                    if (currentTab != invIndex)
                    {
                        currentTab = invIndex;
                        CurrentOpenedStashTab = InventDrop.GoToTab(invIndex);
                    }
                    
                    var foundItem = CurrentOpenedStashTab.VisibleInventoryItems.Find(x => x.InventPosX == curPreparedItem.InventPosX && x.InventPosY == curPreparedItem.InventPosY);

                    if (foundItem != null)
                    {
                        Utils.VirtualKeyboard.KeyDown(Keys.LControlKey);
                        Utils.MouseUtils.LeftMouseClick(foundItem.GetClientRect().Center + gameWindowPos.TopLeft);
                        Utils.VirtualKeyboard.KeyUp(Keys.LControlKey);
                        Thread.Sleep(100);
                    }
                    UpdateStashes();
                }
                part.RemovePreparedItems();
            }
            UpdatePlayerInventory();
            UpdateItemsSetsInfo();
        }
            



        private Inventory CurrentOpenedStashTab;
        private string CurrentOpenedStashTabName;

        private void DrawSetsInfo()
        {
            var stash = GameController.Game.IngameState.ServerData.StashPanel;
            var leftPanelOpened = stash.IsVisible;


            if (leftPanelOpened)
            {
                if (CurrentSetData.bSetIsReady && CurrentOpenedStashTab != null)
                {
                    var StashTabRect = CurrentOpenedStashTab.InventoryRootElement.GetClientRect();

                    var setItemsListRect = new RectangleF(StashTabRect.Right, StashTabRect.Bottom, 270, 240);
                    Graphics.DrawBox(setItemsListRect, new Color(0, 0, 0, 200));
                    Graphics.DrawFrame(setItemsListRect, 2, Color.White);

                    float drawPosX = setItemsListRect.X + 10;
                    float drawPosY = setItemsListRect.Y + 10;

                    //

                    Graphics.DrawText("Current " + (CurrentSetData.SetType == 1 ? "Chaos" : "Regal") + " set:", 15, new Vector2(drawPosX, drawPosY));

                    drawPosY += 25;

                    for (int i = 0; i < 8; i++)//Check that we have enough items for any set
                    {
                        var part = ItemSetTypes[i];
                        var items = part.GetPreparedItems();

                        for (int j = 0; j < items.Length; j++)
                        {
                            var curPreparedItem = items[j];

                            bool inInventory = SData.PlayerInventory.StashTabItems.Contains(curPreparedItem);
                            bool curStashOpened = curPreparedItem.StashName == CurrentOpenedStashTabName;
                            var color = Color.Gray;

                            if(inInventory)
                                color = Color.Green;
                            else if(curStashOpened)
                                color = Color.Yellow;

                            if (!inInventory && curStashOpened)
                            {
                                var foundItem = CurrentOpenedStashTab.VisibleInventoryItems.Find(x => x.InventPosX == curPreparedItem.InventPosX && x.InventPosY == curPreparedItem.InventPosY);

                                if (foundItem != null)
                                {
                                    Graphics.DrawFrame(foundItem.GetClientRect(), 2, Color.Yellow);
                                }
                            }

                            Graphics.DrawText(curPreparedItem.StashName + " (" + curPreparedItem.ItemName + ") " + (curPreparedItem.LowLvl ? "L" : "H"), 15, new Vector2(drawPosX, drawPosY), color);
                            drawPosY += 20;
                        }

                     
                    }
                }
            }

            if (Settings.ShowOnlyWithInventory)
            {
                if (!GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible) return;
            }

            if(Settings.HideWhenLeftPanelOpened)
            {
             
                if (leftPanelOpened) return;
            }

            var posX = Settings.PositionX.Value;
            var posY = Settings.PositionY.Value;

            var rect = new RectangleF(posX, posY, 230, 200);
            Graphics.DrawBox(rect, new Color(0, 0, 0, 200));
            Graphics.DrawFrame(rect, 2, Color.White);


            posX += 10;
            posY += 10;
            Graphics.DrawText(DrawInfoString, 15, new Vector2(posX, posY));
        }


        private BaseSetPart[] ItemSetTypes;
        private string DrawInfoString = "";
        private CurrentSetInfo CurrentSetData;

        private void UpdateItemsSetsInfo()
        {
            CurrentSetData = new CurrentSetInfo();
            ItemSetTypes = new BaseSetPart[8];
            ItemSetTypes[0] = new WeaponItemsSetPart("Weapons");
            ItemSetTypes[1] = new SingleItemSetPart("Helmets");
            ItemSetTypes[2] = new SingleItemSetPart("Body Armors");
            ItemSetTypes[3] = new SingleItemSetPart("Gloves");
            ItemSetTypes[4] = new SingleItemSetPart("Boots");
            ItemSetTypes[5] = new SingleItemSetPart("Belts");
            ItemSetTypes[6] = new SingleItemSetPart("Amulets");
            ItemSetTypes[7] = new RingItemsSetPart("Rings");

            for (int i = 0; i <= 7; i++)
                DisplayData[i].BaseData = ItemSetTypes[i];

            foreach (var item in SData.PlayerInventory.StashTabItems)
            {
                int index = (int)item.ItemType;

                if (index > 7)
                    index = 0; // Switch One/TwoHanded to 0(weapon)

                var setPart = ItemSetTypes[index];
                setPart.AddItem(item);
            }

            foreach (var stash in SData.StashTabs)
            {
                foreach (var item in stash.Value.StashTabItems)
                {
                    int index = (int)item.ItemType;

                    if (index > 7)
                        index = 0; // Switch One/TwoHanded to 0(weapon)

                    var setPart = ItemSetTypes[index];
                    setPart.AddItem(item);
                }
            }

            //Calculate sets:
            DrawInfoString = "";
            int chaosSetMaxCount = 0;

            int regalSetMaxCount = int.MaxValue;
            int minItemsCount = int.MaxValue;
            int maxItemsCount = 0;
            int totalAllItemsCount = 0;

            for (int i = 0; i <= 7; i++)//Check that we have enough items for any set
            {
                var part = ItemSetTypes[i];

                var low = part.LowSetsCount();
                var high = part.HighSetsCount();
                var total = part.TotalSetsCount();

                totalAllItemsCount += total;

                if (minItemsCount > total)
                    minItemsCount = total;

                if (maxItemsCount < total)
                    maxItemsCount = total;

                if (regalSetMaxCount > high)
                    regalSetMaxCount = high;

                chaosSetMaxCount += low;
                DrawInfoString += part.GetInfoString() + "\r\n";

                var drawInfo = DisplayData[i];
                drawInfo.TotalCount = total;
                drawInfo.TotalLowCount = low;
                drawInfo.TotalHighCount = high;
            }

            for (int i = 0; i <= 7; i++)
            {
                var drawInfo = DisplayData[i];
                drawInfo.PriorityScale = (float)drawInfo.TotalCount / maxItemsCount;// - drawInfo.TotalCount
                drawInfo.PriorityPercent = (int)(drawInfo.PriorityScale * 100);
            }

            DrawInfoString += "\r\n";

            int chaosSets = Math.Min(minItemsCount, chaosSetMaxCount);

            DrawInfoString += "Chaos sets ready: " + chaosSets;
            DrawInfoString += "\r\n";
            DrawInfoString += "Regal sets ready: " + regalSetMaxCount;

            if (chaosSets > 0 || regalSetMaxCount > 0)
            {
                int maxAvailableReplaceCount = 0;
                int replaceIndex = -1;

                bool isLowSet = false;
                for (int i = 0; i < 8; i++)//Check that we have enough items for any set
                {
                    var part = ItemSetTypes[i];
                    var prepareResult = part.PrepareItemForSet(Settings);

                    isLowSet = isLowSet || prepareResult.LowSet;

                    if (maxAvailableReplaceCount < prepareResult.AllowedReplacesCount && !prepareResult.bInPlayerInvent)
                    {
                        maxAvailableReplaceCount = prepareResult.AllowedReplacesCount;
                        replaceIndex = i;
                    }
                }

                if (!isLowSet)
                {
                    if (Settings.ShowRegalSets)
                    {
                        CurrentSetData.bSetIsReady = true;
                        CurrentSetData.SetType = 2;
                        return;
                    }
                    if (maxAvailableReplaceCount == 0)
                    {
                        LogMessage("You want to make a regal set anyway? Ok.", 2);
                        CurrentSetData.bSetIsReady = true;
                        CurrentSetData.SetType = 2;
                        return;
                    }
                    else if(replaceIndex != -1)
                    {
                        ItemSetTypes[replaceIndex].DoLowItemReplace();
                        CurrentSetData.SetType = 1;
                        CurrentSetData.bSetIsReady = true;
                    }
                    else
                    {
                        CurrentSetData.bSetIsReady = true;
                        CurrentSetData.SetType = 1;
                        return;
                    }
                }
                else
                {
                    CurrentSetData.bSetIsReady = true;
                    CurrentSetData.SetType = 1;
                }
            }
        }



        private bool UpdateStashes()
        {
            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;
            List<string> stashNames = new List<string>();
            bool needUpdateAllInfo = false;
            CurrentOpenedStashTab = null;
            CurrentOpenedStashTabName = "";


            for (int i = 0; i < stashPanel.TotalStashes; i++)
            {
                Inventory stash = stashPanel.GetStashInventoryByIndex(i);

                string stashName = stashPanel.GetStashName(i);
                stashNames.Add(stashName);

           
                if (stash == null || stash.VisibleInventoryItems == null)
                {
                    continue;
                }

                var visibleInventoryItems = stash.VisibleInventoryItems;

                CurrentOpenedStashTab = stash;
                CurrentOpenedStashTabName = stashName;

                StashTabData curStashData = null;

                bool add = false;
                if (!SData.StashTabs.TryGetValue(stashName, out curStashData))
                {
                    curStashData = new StashTabData();
                    add = true;
                }

                if (stash.ItemCount != visibleInventoryItems.Count) continue;

                if (curStashData.ItemsCount != stash.ItemCount)
                {
                    curStashData.StashTabItems = new List<StashItem>();
                    needUpdateAllInfo = true;
                    foreach (var invItem in visibleInventoryItems)
                    {
                        var item = invItem.Item;
                        var newStashItem = ProcessItem(item);

                        if (newStashItem != null)
                        {
                            curStashData.StashTabItems.Add(newStashItem);
                            newStashItem.StashName = stashName;
                            newStashItem.InventPosX = invItem.InventPosX;
                            newStashItem.InventPosY = invItem.InventPosY;
                        }
                    }
                    curStashData.ItemsCount = (int)stash.ItemCount;
                }

                if(add && curStashData.ItemsCount > 0)
                {
                    SData.StashTabs.Add(stashName, curStashData);
                }
            }


            if (needUpdateAllInfo)
            {
                foreach (var name in stashNames)//Delete stashes that doesn't exist
                {
                    if (!SData.StashTabs.ContainsKey(name))
                        SData.StashTabs.Remove(name);
                }

                return true;
            }
            return false;
        }


        private bool UpdatePlayerInventory()
        {
            if (!GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible) return false;
            var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[PoeHUD.Models.Enums.InventoryIndex.PlayerInventory];

            if (SData.PlayerInventory.ItemsCount == inventory.ItemCount) return false;

            SData.PlayerInventory = new StashTabData();

            var invItems = inventory.VisibleInventoryItems;

            if(invItems != null)
            {
                foreach(var invItem in invItems)
                {
                    var item = invItem.Item;
                    var newAddedItem = ProcessItem(item);

                    if(newAddedItem != null)
                    {
                        SData.PlayerInventory.StashTabItems.Add(newAddedItem);
                        newAddedItem.InventPosX = invItem.InventPosX;
                        newAddedItem.InventPosY = invItem.InventPosY;
                        newAddedItem.bInPlayerInventory = true;
                    }
                }
                SData.PlayerInventory.ItemsCount = (int)inventory.ItemCount;
            }
          
            return true;
        }

        private StashItem ProcessItem(Entity item)
        {
            if (item == null) return null;

            var mods = item.GetComponent<Mods>();

            if (mods.ItemRarity != PoeHUD.Models.Enums.ItemRarity.Rare) return null;

            var bIdentified = mods.Identified;
            if (bIdentified && !Settings.AllowIdentified) return null;

            if (mods.ItemLevel < 60) return null;

            StashItem newItem = new StashItem();
            newItem.bIdentified = bIdentified;
            newItem.LowLvl = mods.ItemLevel < 75;



            BaseItemType bit = GameController.Files.BaseItemTypes.Translate(item.Path);

            var className = bit.ClassName;

            newItem.ItemClass = className;
            newItem.ItemName = bit.BaseName;

            if (className.StartsWith("Two Hand"))
            {
                newItem.ItemType = StashItemType.TwoHanded;
            }
            else if (className.StartsWith("One Hand") || className.StartsWith("Thrusting One Hand"))
            {
                newItem.ItemType = StashItemType.OneHanded;
            }
            else
            {
                switch (className)
                {
                    case "Bow": newItem.ItemType = StashItemType.TwoHanded; break;
                    case "Staff": newItem.ItemType = StashItemType.TwoHanded; break;
                    case "Sceptre": newItem.ItemType = StashItemType.OneHanded; break;
                    case "Wand": newItem.ItemType = StashItemType.OneHanded; break;
                    case "Dagger": newItem.ItemType = StashItemType.OneHanded; break;
                    case "Claw": newItem.ItemType = StashItemType.OneHanded; break;
                    case "Shield": newItem.ItemType = StashItemType.OneHanded; break;

                    case "Ring": newItem.ItemType = StashItemType.Ring; break;
                    case "Amulet": newItem.ItemType = StashItemType.Amulet; break;
                    case "Belt": newItem.ItemType = StashItemType.Belt; break;

                    case "Helmet": newItem.ItemType = StashItemType.Helmet; break;
                    case "Body Armour": newItem.ItemType = StashItemType.Body; break;
                    case "Boots": newItem.ItemType = StashItemType.Boots; break;
                    case "Gloves": newItem.ItemType = StashItemType.Gloves; break;

                    default:
                        newItem.ItemType = StashItemType.Undefined;
                        break;
                }
            }

            if (newItem.ItemType != StashItemType.Undefined)
            {
                return newItem;
            }
            
            return null;
        }

        private struct CurrentSetInfo
        {
            public bool bSetIsReady;
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
            public int TotalCount;
            public int TotalLowCount;
            public int TotalHighCount;
            public float PriorityScale;
            public float PriorityPercent;
        }
    }
}
