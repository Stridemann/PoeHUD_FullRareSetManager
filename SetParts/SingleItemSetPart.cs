using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace FullRareSetManager.SetParts
{
    public class SingleItemSetPart : BaseSetPart
    {
        private StashItem _currentSetItem;
        public List<StashItem> HighLvlItems = new List<StashItem>();
        public List<StashItem> LowLvlItems = new List<StashItem>();

        public SingleItemSetPart(string partName) : base(partName)
        {
        }

        public override void AddItem(StashItem item)
        {
            if (item.LowLvl)
                LowLvlItems.Add(item);
            else
                HighLvlItems.Add(item);
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

        public override PrepareItemResult PrepareItemForSet(FullRareSetManagerSettings settings)
        {
            var lowFirst = LowLvlItems.Count > 0 && LowLvlItems[0].BInPlayerInventory;

            if (lowFirst)
            {
                var result = LowProcess();

                if (result != null)
                    return result;

                result = HighProcess();

                if (result != null)
                    return result;
            }
            else
            {
                var result = HighProcess();

                if (result != null)
                    return result;

                result = LowProcess();

                if (result != null)
                    return result;
            }

            return null;
        }

        private PrepareItemResult HighProcess()
        {
            if (HighLvlItems.Count <= 0)
                return null;

            if (!HighLvlItems[0].BInPlayerInventory)
                HighLvlItems = HighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

            _currentSetItem = HighLvlItems[0];

            return new PrepareItemResult
            {
                AllowedReplacesCount = LowLvlItems.Count,
                LowSet = false,
                BInPlayerInvent = _currentSetItem.BInPlayerInventory
            };
        }

        private PrepareItemResult LowProcess()
        {
            if (LowLvlItems.Count <= 0)
                return null;

            if (!LowLvlItems[0].BInPlayerInventory)
                LowLvlItems = LowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

            _currentSetItem = LowLvlItems[0];

            return new PrepareItemResult
            {
                AllowedReplacesCount = LowLvlItems.Count - 1,
                LowSet = true,
                BInPlayerInvent = _currentSetItem.BInPlayerInventory
            };
        }

        public override void DoLowItemReplace()
        {
            if (LowLvlItems.Count > 0) //should be always > 0 here
            {
                LowLvlItems = LowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();
                _currentSetItem = LowLvlItems[0];
            }
            //else
                //DebugPlugin.LogMsg("Something goes wrong: Can't do low lvl item replace on " + PartName + "!", 10, Color.Red);//TODO
        }

        public override StashItem[] GetPreparedItems()
        {
            return new[] {_currentSetItem};
        }

        public override void RemovePreparedItems()
        {
            if (_currentSetItem.LowLvl)
                LowLvlItems.Remove(_currentSetItem);
            else
                HighLvlItems.Remove(_currentSetItem);
        }

        public override int PlayerInventItemsCount()
        {
            return
                HighLvlItems.Count(x => x.BInPlayerInventory) +
                LowLvlItems.Count(x => x.BInPlayerInventory);
        }
    }
}
