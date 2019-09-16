namespace FullRareSetManager.SetParts
{
    public abstract class BaseSetPart
    {
        public int ItemCellsSize = 1;
        public string PartName;
        public int StashTabItemsCount = 0;

        protected BaseSetPart(string partName)
        {
            PartName = partName;
        }

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
    }

    public class PrepareItemResult
    {
        public int AllowedReplacesCount;
        public bool BInPlayerInvent;
        public bool LowSet;
    }
}
