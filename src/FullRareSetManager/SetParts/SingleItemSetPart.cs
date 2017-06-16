using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullRareSetManager
{
    public class SingleItemSetPart : BaseSetPart
    {
        public SingleItemSetPart(string partName) : base(partName) { }
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
            return HighLvlItems.Count + LowLvlItems.Count;
        }


        public override int LowSetsCount()
        {
            return LowLvlItems.Count;
        }
        public override int HighSetsCount()
        {
            return HighLvlItems.Count;
        }

        public override string GetInfoString()
        {
            return PartName + ": " + TotalSetsCount() + " (" + LowSetsCount() + "L / " + HighSetsCount() + "H)";
        }


        private StashItem CurrentSetItem;
        public override PrepareItemResult PrepareItemForSet(FullRareSetManager_Settings settings)
        {
            if (HighLvlItems.Count > 0)
            {
                CurrentSetItem = HighLvlItems[0];
                return new PrepareItemResult() { AllowedReplacesCount = LowLvlItems.Count, LowSet = false, bInPlayerInvent = CurrentSetItem.bInPlayerInventory };
            }
            else if (LowLvlItems.Count > 0)
            {
                CurrentSetItem = LowLvlItems[0];
                return new PrepareItemResult() { AllowedReplacesCount = LowLvlItems.Count - 1, LowSet = true, bInPlayerInvent = CurrentSetItem.bInPlayerInventory };
            }
            return new PrepareItemResult();//Code should never get here
        }

        public override void DoLowItemReplace()
        {
            if(LowLvlItems.Count > 0)//Count should be always > 0 here
                CurrentSetItem = LowLvlItems[0];
            else
            {
                PoeHUD.DebugPlug.DebugPlugin.LogMsg("Something goes wrong: Can't do low lvl item replace on " + PartName + "!", 10, SharpDX.Color.Red);
            }
        }

        public override StashItem[] GetPreparedItems()
        {
            return new StashItem[] { CurrentSetItem };
        }
    }
}
