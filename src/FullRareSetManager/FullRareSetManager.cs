using System;
using System.Collections.Generic;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Hud.UI;
using PoeHUD.Poe.RemoteMemoryObjects;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using SharpDX;

namespace FullRareSetManager
{
    public class FullRareSetManager : BaseSettingsPlugin<FullRareSetManager_Settings>
    {
        private StashData SData;
        public override void Initialise()
        {
            SData = StashData.Load(this);
            UpdateItemsSetsInfo();
        }

        public override void OnClose()
        {
            if(SData != null)
                StashData.Save(this, SData);
        }

        public override void Render()
        {
            bool needUpdate = UpdatePlayerInventory();

            if (GameController.Game.IngameState.ServerData.StashPanel.IsVisible)
            {
                needUpdate = UpdateStashes() || needUpdate;
            }

            if(needUpdate)
                UpdateItemsSetsInfo();

            DrawSetsInfo();
        }



        private Inventory CurrentOpenedStashTab;
        private string CurrentOpenedStashTabName;

        private void DrawSetsInfo()
        {
            var stash = GameController.Game.IngameState.ServerData.StashPanel;
            var leftPanelOpened = stash.IsVisible;


            if (leftPanelOpened)
            {
                if (bFullSetDrawPrepared && CurrentOpenedStashTab != null)
                {
                    var StashTabRect = CurrentOpenedStashTab.InventoryRootElement.GetClientRect();

                    var setItemsListRect = new RectangleF(StashTabRect.Right, StashTabRect.Bottom, 270, 200);
                    Graphics.DrawBox(setItemsListRect, new Color(0, 0, 0, 200));
                    Graphics.DrawFrame(setItemsListRect, 2, Color.White);

                    float drawPosX = setItemsListRect.X + 10;
                    float drawPosY = setItemsListRect.Y + 10;

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
        private bool bFullSetDrawPrepared = false;

        private void UpdateItemsSetsInfo()
        {
            bFullSetDrawPrepared = false;
            ItemSetTypes = new BaseSetPart[8];

            ItemSetTypes[0] = new WeaponItemsSetPart("Weapons");
            ItemSetTypes[1] = new SingleItemSetPart("Helmets");
            ItemSetTypes[2] = new SingleItemSetPart("Body Armors");
            ItemSetTypes[3] = new SingleItemSetPart("Gloves");
            ItemSetTypes[4] = new SingleItemSetPart("Boots");
            ItemSetTypes[5] = new SingleItemSetPart("Belts");
            ItemSetTypes[6] = new SingleItemSetPart("Amulets");
            ItemSetTypes[7] = new RingItemsSetPart("Rings");

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

            for (int i = 0; i < 8; i++)//Check that we have enough items for any set
            {
                var part = ItemSetTypes[i];

                var low = part.LowSetsCount();
                var high = part.HighSetsCount();
                var total = part.TotalSetsCount();

                if (minItemsCount > total)
                    minItemsCount = total;

                if (regalSetMaxCount > high)
                    regalSetMaxCount = high;

                chaosSetMaxCount += low;
                DrawInfoString += part.GetInfoString() + "\r\n";
            }
            DrawInfoString += "\r\n";

            int chaosSets = Math.Min(minItemsCount, chaosSetMaxCount);

            DrawInfoString += "Chaos sets ready: " + chaosSets;
            DrawInfoString += "\r\n";
            DrawInfoString += "Regal sets ready: " + regalSetMaxCount;

            int maxAvailableReplaceCount = 0;
            int replaceIndex = -1;

            bool isLowSet = false;
            for (int i = 0; i < 8; i++)//Check that we have enough items for any set
            {
                var part = ItemSetTypes[i];
                var prepareResult = part.PrepareItemForSet();

                isLowSet = isLowSet || prepareResult.LowSet;

                if (maxAvailableReplaceCount < prepareResult.AllowedReplacesCount)
                {
                    maxAvailableReplaceCount = prepareResult.AllowedReplacesCount;
                    replaceIndex = i;
                }
            }

            bFullSetDrawPrepared = false;

            if (chaosSets > 0 || regalSetMaxCount > 0)
            {
                if (!isLowSet)
                {
                    if (Settings.ShowRegalSets)
                    {
                        bFullSetDrawPrepared = true;
                        return;
                    }
                    if (maxAvailableReplaceCount == 0)
                    {
                        LogMessage("Something goes wrong (or not?): This is regal set + can't replace with any low iLvl items + allow chaos set", 5);
                        return;
                    }
                    else
                    {
                        ItemSetTypes[replaceIndex].DoLowItemReplace();
                        bFullSetDrawPrepared = true;
                    }
                }
                else
                {
                    bFullSetDrawPrepared = true;
                }
            }
        }



        private bool UpdateStashes()
        {
            var invPanel = GameController.Game.IngameState.ServerData.StashPanel;
            List<string> stashNames = new List<string>();
            bool needUpdateAllInfo = false;
            CurrentOpenedStashTab = null;
            CurrentOpenedStashTabName = "";


            for (int i = 0; i < invPanel.TotalStashes; i++)
            {
                Inventory stash = invPanel.getStashInventory(i);
                string stashName = invPanel.getStashName(i);
                stashNames.Add(stashName);

                if (stash == null || stash.VisibleInventoryItems == null)
                {
                    continue;
                }

                CurrentOpenedStashTab = stash;
                CurrentOpenedStashTabName = stashName;

                StashTabData curStashData = null;

                if (!SData.StashTabs.TryGetValue(stashName, out curStashData))
                {
                    curStashData = new StashTabData();
                    SData.StashTabs.Add(stashName, curStashData);
                }

                if (curStashData.ItemsCount != stash.ItemCount)
                {
                    curStashData.StashTabItems = new List<StashItem>();

                    foreach (var invItem in stash.VisibleInventoryItems)
                    {
                        var item = invItem.Item;
                        var newStashItem = ProcessItem(item, curStashData);

                        if (newStashItem != null)
                        {
                            newStashItem.StashName = stashName;
                            newStashItem.InventPosX = invItem.InventPosX;
                            newStashItem.InventPosY = invItem.InventPosY;
                        }
                    }
                    
                    needUpdateAllInfo = true;
                }
            }


            if (needUpdateAllInfo)
            {
                foreach (var name in stashNames)
                {
                    if (!SData.StashTabs.ContainsKey(name))
                        SData.StashTabs.Remove(name);
                }
                needUpdateAllInfo = false;
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
                    var newAddedItem = ProcessItem(item, SData.PlayerInventory);

                    if(newAddedItem != null)
                    {
                        newAddedItem.InventPosX = invItem.InventPosX;
                        newAddedItem.InventPosY = invItem.InventPosY;
                    }
                }
            }
            return true;
        }

        private StashItem ProcessItem(Entity item, StashTabData data)
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
            else if (className.StartsWith("One Hand"))
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
                data.StashTabItems.Add(newItem);
                return newItem;
            }
            return null;
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

    }
}
