using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullRareSetManager
{
    public class RingItemsSetPart : BaseSetPart
    {
        public RingItemsSetPart(string partName) : base(partName) { }
        public List<StashItem> HighLvlItems = new List<StashItem>();
        public List<StashItem> LowLvlItems = new List<StashItem>();


        public override void AddItem(StashItem item)
        {
            if (item.LowLvl)
            {
                LowLvlItems.Add(item);
            }
            else
            {
                HighLvlItems.Add(item);
            }
        }

        public override int TotalSetsCount()
        {
            return (HighLvlItems.Count + LowLvlItems.Count) / 2;
        }


        public override int LowSetsCount()
        {
            int oneHandedLeft = LowLvlItems.Count - HighLvlItems.Count;//High and low together

            if (oneHandedLeft <= 0)
                return LowLvlItems.Count;
            else
                return HighLvlItems.Count + oneHandedLeft / 2;//(High & low) + (Low / 2)
        }
        public override int HighSetsCount()
        {
            return HighLvlItems.Count;
        }

        public override string GetInfoString()
        {
            return PartName + ": " + TotalSetsCount() + " (" + LowSetsCount() + "L / " + HighSetsCount() + "H)";
        }



        private StashItem[] CurrentSetItems;
        public override PrepareItemResult PrepareItemForSet(FullRareSetManager_Settings settings)
        {
            if (HighLvlItems.Count >= 2)
            {
                var inPlayerInvent = HighLvlItems[0].bInPlayerInventory || HighLvlItems[1].bInPlayerInventory;

                if(!inPlayerInvent)
                    HighLvlItems = HighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();


                CurrentSetItems = new StashItem[]
                {
                    HighLvlItems[0],
                    HighLvlItems[1]
                };
               
                return new PrepareItemResult() { AllowedReplacesCount = LowLvlItems.Count, LowSet = false, bInPlayerInvent = inPlayerInvent };
            }
            else if (HighLvlItems.Count >= 1 && LowLvlItems.Count >= 1)
            {
                var inPlayerInvent = HighLvlItems[0].bInPlayerInventory;

                if (!inPlayerInvent)
                {
                    HighLvlItems = HighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

                    inPlayerInvent = LowLvlItems[1].bInPlayerInventory;

                    if (!inPlayerInvent)
                        HighLvlItems = HighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();
                }

                CurrentSetItems = new StashItem[]
                {
                    HighLvlItems[0],
                    LowLvlItems[0]
                };
              
                return new PrepareItemResult() { AllowedReplacesCount = LowLvlItems.Count - 1, LowSet = true, bInPlayerInvent = inPlayerInvent };
            }
            else if (LowLvlItems.Count >= 2)
            {
                var inPlayerInvent = LowLvlItems[0].bInPlayerInventory || LowLvlItems[1].bInPlayerInventory;

                if (!inPlayerInvent)
                    LowLvlItems = LowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

                CurrentSetItems = new StashItem[]
                {
                    LowLvlItems[0],
                    LowLvlItems[1]
                };
           
                return new PrepareItemResult() { AllowedReplacesCount = LowLvlItems.Count - 2, LowSet = true, bInPlayerInvent = inPlayerInvent };
            }
            return new PrepareItemResult();//Code should never get here
        }

        public override void DoLowItemReplace()
        {
            if (HighLvlItems.Count >= 1 && LowLvlItems.Count >= 1)
            {
                LowLvlItems = LowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();
                HighLvlItems = HighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();
                CurrentSetItems = new StashItem[]
                {
                    HighLvlItems[0],
                    LowLvlItems[0]
                };
            }
            else if (LowLvlItems.Count >= 2)
            {
                LowLvlItems = LowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();
                CurrentSetItems = new StashItem[]
                {
                    LowLvlItems[0],
                    LowLvlItems[1]
                };
            }
            else
            {
                PoeHUD.DebugPlug.DebugPlugin.LogMsg("Something goes wrong: Can't do low lvl item replace on rings!", 10, SharpDX.Color.Red);
            }
        }

        public override StashItem[] GetPreparedItems()
        {
            return CurrentSetItems;
        }

        public override void RemovePreparedItems()
        {
            RemoveItem(CurrentSetItems[0]);
            RemoveItem(CurrentSetItems[1]);
        }

        private void RemoveItem(StashItem item)
        {
            if (item.LowLvl)
            {
                LowLvlItems.Remove(item);
            }
            else
            {
                HighLvlItems.Remove(item);
            }
        }
    }
}
