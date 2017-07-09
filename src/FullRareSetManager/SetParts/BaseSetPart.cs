namespace FullRareSetManager.SetParts
{
    public abstract class BaseSetPart
    {
        protected BaseSetPart(string partName)
        {
            PartName = partName;
        }

        public string PartName;
        public abstract int LowSetsCount();
        public abstract int HighSetsCount();
        public abstract int TotalSetsCount();
        public abstract void AddItem(StashItem item);
        public abstract string GetInfoString();
        public abstract int PlayerInventItemsCount();
        public abstract PrepareItemResult PrepareItemForSet(FullRareSetManagerSettings settings);
        public abstract void DoLowItemReplace();
        public abstract StashItem[] GetPreparedItems();
        public abstract void RemovePreparedItems();

        public int StashTabItemsCount = 0;
        public int ItemCellsSize = 1;
    }

    public class PrepareItemResult
    {
        public int AllowedReplacesCount;
        public bool LowSet;
        public bool BInPlayerInvent;
    }
}