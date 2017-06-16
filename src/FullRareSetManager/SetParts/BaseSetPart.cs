using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullRareSetManager
{
    public abstract class BaseSetPart
    {
        public BaseSetPart(string partName)
        {
            PartName = partName;
        }
        public string PartName;
        public abstract int LowSetsCount();
        public abstract int HighSetsCount();
        public abstract int TotalSetsCount();
        public abstract void AddItem(StashItem item);
        public abstract string GetInfoString();
        
        public abstract PrepareItemResult PrepareItemForSet(FullRareSetManager_Settings settings);
        public abstract void DoLowItemReplace();
        public abstract StashItem[] GetPreparedItems();
    }

    public class PrepareItemResult
    {
        public PrepareItemResult() { }
        public int AllowedReplacesCount;
        public bool LowSet;
        public bool bInPlayerInvent;
    }
}
