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
        public override PrepareItemResult PrepareItemForSet()
        {
            if (HighLvlItems.Count >= 2)
            {
                CurrentSetItems = new StashItem[]
                {
                    HighLvlItems[0],
                    HighLvlItems[1]
                };

                return new PrepareItemResult() { AllowedReplacesCount = LowLvlItems.Count, LowSet = false };
            }
            else if (HighLvlItems.Count >= 1 && LowLvlItems.Count >= 1)
            {
                CurrentSetItems = new StashItem[]
                {
                    HighLvlItems[0],
                    LowLvlItems[0]
                };

                return new PrepareItemResult() { AllowedReplacesCount = LowLvlItems.Count - 1, LowSet = true };
            }
            else if (LowLvlItems.Count >= 2)
            {
                CurrentSetItems = new StashItem[]
                {
                    LowLvlItems[0],
                    LowLvlItems[1]
                };
                
                return new PrepareItemResult() { AllowedReplacesCount = LowLvlItems.Count - 2, LowSet = true };
            }
            return new PrepareItemResult();//Code should never get here
        }

        public override void DoLowItemReplace()
        {
            if (HighLvlItems.Count >= 1 && LowLvlItems.Count >= 1)
            {
                CurrentSetItems = new StashItem[]
                {
                    HighLvlItems[0],
                    LowLvlItems[0]
                };
            }
            else if (LowLvlItems.Count >= 2)
            {
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
    }
}
