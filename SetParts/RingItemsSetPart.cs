using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace FullRareSetManager.SetParts
{
    public class RingItemsSetPart : BaseSetPart
    {
        private StashItem[] _currentSetItems;
        public List<StashItem> HighLvlItems = new List<StashItem>();
        public List<StashItem> LowLvlItems = new List<StashItem>();

        public RingItemsSetPart(string partName) : base(partName)
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
            return (HighLvlItems.Count + LowLvlItems.Count) / 2;
        }

        public override int LowSetsCount()
        {
            var oneHandedLeft = LowLvlItems.Count - HighLvlItems.Count; //High and low together

            if (oneHandedLeft <= 0)
                return LowLvlItems.Count;

            return HighLvlItems.Count + oneHandedLeft / 2; //(High & low) + (Low / 2)
        }

        public override int HighSetsCount()
        {
            return HighLvlItems.Count / 2;
        }

        public override string GetInfoString()
        {
            return PartName + ": " + TotalSetsCount() + " (" + LowSetsCount() + "L / " + HighSetsCount() + "H)";
        }

        public override PrepareItemResult PrepareItemForSet(FullRareSetManagerSettings settings)
        {
            var anyHighInInvent = HighLvlItems.Count >= 1 && HighLvlItems[0].BInPlayerInventory;
            var anyLowInInvent = LowLvlItems.Count >= 1 && LowLvlItems[0].BInPlayerInventory;

            if (anyHighInInvent && anyLowInInvent)
            {
                _currentSetItems = new[]
                {
                    HighLvlItems[0],
                    LowLvlItems[0]
                };

                return new PrepareItemResult
                {
                    AllowedReplacesCount = LowLvlItems.Count - 1,
                    LowSet = true,
                    BInPlayerInvent = true
                };
            }

            if (anyHighInInvent)
            {
                var allHighInInvent = HighLvlItems.Count >= 2 && HighLvlItems[1].BInPlayerInventory;

                if (allHighInInvent)
                {
                    _currentSetItems = new[]
                    {
                        HighLvlItems[0],
                        HighLvlItems[1]
                    };

                    return new PrepareItemResult
                    {
                        AllowedReplacesCount = LowLvlItems.Count,
                        LowSet = false,
                        BInPlayerInvent = true
                    };
                }

                var result = PrepareHigh();

                if (result != null)
                    return result;

                result = PrepareMixedHl();

                if (result != null)
                    return result;
            }
            else if (anyLowInInvent)
            {
                var allLowInInvent = LowLvlItems.Count >= 2 && LowLvlItems[1].BInPlayerInventory;

                if (allLowInInvent)
                {
                    _currentSetItems = new[]
                    {
                        LowLvlItems[0],
                        LowLvlItems[1]
                    };

                    return new PrepareItemResult
                    {
                        AllowedReplacesCount = LowLvlItems.Count - 2,
                        LowSet = true,
                        BInPlayerInvent = true
                    };
                }

                var result = PrepareMixedHl() ?? PrepareLow();

                if (result != null)
                    return result;
            }
            else
            {
                var result = PrepareHigh();

                if (result != null)
                    return result;

                result = PrepareMixedHl();

                if (result != null)
                    return result;

                result = PrepareLow();

                if (result != null)
                    return result;
            }

            return new PrepareItemResult(); //Code should never get here
        }

        private PrepareItemResult PrepareHigh()
        {
            if (HighLvlItems.Count < 2)
                return null;

            var inPlayerInvent = HighLvlItems[0].BInPlayerInventory || HighLvlItems[1].BInPlayerInventory;

            if (!inPlayerInvent)
                HighLvlItems = HighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

            _currentSetItems = new[]
            {
                HighLvlItems[0],
                HighLvlItems[1]
            };

            return new PrepareItemResult
            {
                AllowedReplacesCount = LowLvlItems.Count,
                LowSet = false,
                BInPlayerInvent = false
            };
        }

        private PrepareItemResult PrepareMixedHl()
        {
            if (HighLvlItems.Count < 1 || LowLvlItems.Count < 1)
                return null;

            var inPlayerInvent = HighLvlItems[0].BInPlayerInventory;

            if (!inPlayerInvent)
            {
                HighLvlItems = HighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

                inPlayerInvent = LowLvlItems.Count > 1 && LowLvlItems[1].BInPlayerInventory;

                if (!inPlayerInvent)
                    HighLvlItems = HighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();
            }

            _currentSetItems = new[]
            {
                HighLvlItems[0],
                LowLvlItems[0]
            };

            return new PrepareItemResult
            {
                AllowedReplacesCount = LowLvlItems.Count - 1,
                LowSet = true,
                BInPlayerInvent = false
            };
        }

        private PrepareItemResult PrepareLow()
        {
            if (LowLvlItems.Count < 2)
                return null;

            var inPlayerInvent = LowLvlItems[0].BInPlayerInventory || LowLvlItems[1].BInPlayerInventory;

            if (!inPlayerInvent)
                LowLvlItems = LowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

            _currentSetItems = new[]
            {
                LowLvlItems[0],
                LowLvlItems[1]
            };

            return new PrepareItemResult
            {
                AllowedReplacesCount = LowLvlItems.Count - 2,
                LowSet = true,
                BInPlayerInvent = false
            };
        }

        public override void DoLowItemReplace()
        {
            if (HighLvlItems.Count >= 1 && LowLvlItems.Count >= 1)
            {
                if (!LowLvlItems[0].BInPlayerInventory)
                    LowLvlItems = LowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

                if (!HighLvlItems[0].BInPlayerInventory)
                    HighLvlItems = HighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

                _currentSetItems = new[]
                {
                    HighLvlItems[0],
                    LowLvlItems[0]
                };
            }
            else if (LowLvlItems.Count >= 2)
            {
                if (!LowLvlItems[0].BInPlayerInventory)
                    LowLvlItems = LowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12).ToList();

                _currentSetItems = new[]
                {
                    LowLvlItems[0],
                    LowLvlItems[1]
                };
            }
            //else
                //DebugPlugin.LogMsg("Something goes wrong: Can't do low lvl item replace on rings!", 10, Color.Red);//TODO
        }

        public override StashItem[] GetPreparedItems()
        {
            return _currentSetItems;
        }

        public override void RemovePreparedItems()
        {
            RemoveItem(_currentSetItems[0]);
            RemoveItem(_currentSetItems[1]);
        }

        private void RemoveItem(StashItem item)
        {
            if (item.LowLvl)
                LowLvlItems.Remove(item);
            else
                HighLvlItems.Remove(item);
        }

        public override int PlayerInventItemsCount()
        {
            return
                HighLvlItems.Count(x => x.BInPlayerInventory) +
                LowLvlItems.Count(x => x.BInPlayerInventory);
        }
    }
}
