using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace FullRareSetManager.SetParts
{
    public class WeaponItemsSetPart : BaseSetPart
    {
        private StashItem[] _currentSetItems;
        public List<StashItem> OneHandedHighLvlItems = new List<StashItem>();
        public List<StashItem> OneHandedLowLvlItems = new List<StashItem>();
        public List<StashItem> TwoHandedHighLvlItems = new List<StashItem>();
        public List<StashItem> TwoHandedLowLvlItems = new List<StashItem>();

        public WeaponItemsSetPart(string partName) : base(partName)
        {
        }

        public override void AddItem(StashItem item)
        {
            if (item.ItemType == StashItemType.TwoHanded)
            {
                if (item.LowLvl)
                    TwoHandedLowLvlItems.Add(item);
                else
                    TwoHandedHighLvlItems.Add(item);
            }
            else
            {
                if (item.LowLvl)
                    OneHandedLowLvlItems.Add(item);
                else
                    OneHandedHighLvlItems.Add(item);
            }
        }

        public override int LowSetsCount()
        {
            var count = TwoHandedLowLvlItems.Count;

            int oneHandedCount;

            var oneHandedLeft = OneHandedLowLvlItems.Count - OneHandedHighLvlItems.Count; //High and low together

            if (oneHandedLeft <= 0)
                oneHandedCount = OneHandedLowLvlItems.Count;
            else
                oneHandedCount = OneHandedHighLvlItems.Count + oneHandedLeft / 2; //(High & low) + (Low / 2)

            return count + oneHandedCount;
        }

        public override int HighSetsCount()
        {
            return TwoHandedHighLvlItems.Count + OneHandedHighLvlItems.Count / 2;
        }

        public override int TotalSetsCount()
        {
            return TwoHandedLowLvlItems.Count + TwoHandedHighLvlItems.Count +
                   (OneHandedHighLvlItems.Count + OneHandedLowLvlItems.Count) / 2;
        }

        public override string GetInfoString()
        {
            var rezult = "Weapons: " + TotalSetsCount() + " (" + LowSetsCount() + "L / " + HighSetsCount() + "H)";

            var twoHandCount = TwoHandedLowLvlItems.Count + TwoHandedHighLvlItems.Count;

            if (twoHandCount > 0)
            {
                rezult += "\r\n     Two Handed: " + twoHandCount + " (" + TwoHandedLowLvlItems.Count + "L / " +
                          TwoHandedHighLvlItems.Count + "H)";
            }

            var oneHandCount = OneHandedLowLvlItems.Count + OneHandedHighLvlItems.Count;

            if (oneHandCount > 0)
            {
                rezult += "\r\n     One Handed: " + oneHandCount / 2 + " (" + OneHandedLowLvlItems.Count + "L / " +
                          OneHandedHighLvlItems.Count + "H)";
            }

            return rezult;
        }

        public override PrepareItemResult PrepareItemForSet(FullRareSetManagerSettings settings)
        {
            var oneHandedFirst = settings.WeaponTypePriority.Value == "One handed";

            if (!oneHandedFirst)
            {
                if (OneHandedHighLvlItems.Count > 0 && OneHandedHighLvlItems[0].BInPlayerInventory)
                    oneHandedFirst = true;
                else if (OneHandedLowLvlItems.Count > 0 && OneHandedLowLvlItems[0].BInPlayerInventory)
                    oneHandedFirst = true;
            }
            else
            {
                if (TwoHandedHighLvlItems.Count > 0 && TwoHandedHighLvlItems[0].BInPlayerInventory)
                    oneHandedFirst = false;
                else if (TwoHandedLowLvlItems.Count > 0 && TwoHandedLowLvlItems[0].BInPlayerInventory)
                    oneHandedFirst = false;
            }

            var invokeList = new Func<PrepareItemResult>[5];

            if (oneHandedFirst)
            {
                invokeList[0] = Prepahe_OH;
                invokeList[1] = Prepahe_OHOL;
                invokeList[2] = Prepahe_OL;
                invokeList[3] = Prepahe_TH;
                invokeList[4] = Prepahe_TL;
            }
            else
            {
                invokeList[0] = Prepahe_TH;
                invokeList[1] = Prepahe_TL;
                invokeList[2] = Prepahe_OHOL;
                invokeList[3] = Prepahe_OH;
                invokeList[4] = Prepahe_OL;
            }

            var rezults = new List<Tuple<PrepareItemResult, Func<PrepareItemResult>>>();

            foreach (var t in invokeList)
            {
                var result = t();

                if (result != null)
                {
                    var func = t;
                    rezults.Add(new Tuple<PrepareItemResult, Func<PrepareItemResult>>(result, func));
                }
            }

            if (rezults.Count > 0)
            {
                var inPlayerInv = rezults.Find(x => x.Item1.BInPlayerInvent);

                if (inPlayerInv != null)
                {
                    inPlayerInv.Item2();
                    return inPlayerInv.Item1;
                }

                rezults[0].Item2();
                return rezults[0].Item1;
            }

            return null;
        }

        private PrepareItemResult Prepahe_TH()
        {
            if (TwoHandedHighLvlItems.Count < 1)
                return null;

            /*
            if (!TwoHandedHighLvlItems[0].BInPlayerInventory)
            {
                TwoHandedHighLvlItems = TwoHandedHighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                    .ToList();
            }
            */
            _currentSetItems = new[]
            {
                TwoHandedHighLvlItems[0]
            };

            return new PrepareItemResult
            {
                AllowedReplacesCount = LowSetsCount(),
                LowSet = false,
                BInPlayerInvent = _currentSetItems[0].BInPlayerInventory
            };
        }

        private PrepareItemResult Prepahe_OH()
        {
            if (OneHandedHighLvlItems.Count < 2)
                return null;

            /*
            if (!OneHandedHighLvlItems[0].BInPlayerInventory && !OneHandedHighLvlItems[1].BInPlayerInventory)
            {
                OneHandedHighLvlItems = OneHandedHighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                    .ToList();
            }
            else if (OneHandedHighLvlItems[0].BInPlayerInventory && !OneHandedHighLvlItems[1].BInPlayerInventory)
            {
                var first = OneHandedHighLvlItems[0];
                OneHandedHighLvlItems.Remove(first);
                OneHandedHighLvlItems = OneHandedHighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                    .ToList();
                OneHandedHighLvlItems.Insert(0, first);
            }
            */

            _currentSetItems = new[]
            {
                OneHandedHighLvlItems[0],
                OneHandedHighLvlItems[1]
            };

            var inPlayerInvent = _currentSetItems[0].BInPlayerInventory || _currentSetItems[1].BInPlayerInventory;

            return new PrepareItemResult
            {
                AllowedReplacesCount = LowSetsCount(),
                LowSet = false,
                BInPlayerInvent = inPlayerInvent
            };
        }

        private PrepareItemResult Prepahe_OHOL()
        {
            if (OneHandedHighLvlItems.Count < 1 || OneHandedLowLvlItems.Count < 1)
                return null;

            /*
            if (!OneHandedHighLvlItems[0].BInPlayerInventory)
            {
                OneHandedHighLvlItems = OneHandedHighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                    .ToList();
            }

            if (!OneHandedLowLvlItems[0].BInPlayerInventory)
            {
                OneHandedLowLvlItems = OneHandedLowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                    .ToList();
            }
            */

            _currentSetItems = new[]
            {
                OneHandedHighLvlItems[0],
                OneHandedLowLvlItems[0]
            };

            var replCount = TwoHandedLowLvlItems.Count;
            var oneHandedLowCount = OneHandedLowLvlItems.Count - 1;

            int oneHandedCount;

            var oneHandedLeft = oneHandedLowCount - OneHandedHighLvlItems.Count; //High and low together

            if (oneHandedLeft <= 0)
                oneHandedCount = oneHandedLowCount;
            else
                oneHandedCount = OneHandedHighLvlItems.Count + oneHandedLeft / 2; //(High & low) + (Low / 2)

            replCount += oneHandedCount;

            var inPlayerInvent = _currentSetItems[0].BInPlayerInventory || _currentSetItems[1].BInPlayerInventory;

            return new PrepareItemResult
            {
                AllowedReplacesCount = replCount,
                LowSet = true,
                BInPlayerInvent = inPlayerInvent
            };
        }

        private PrepareItemResult Prepahe_TL()
        {
            if (TwoHandedLowLvlItems.Count >= 1)
            {
                /*
                if (!TwoHandedLowLvlItems[0].BInPlayerInventory)
                {
                    TwoHandedLowLvlItems = TwoHandedLowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                        .ToList();
                }
                */
                _currentSetItems = new[]
                {
                    TwoHandedLowLvlItems[0]
                };

                var replCount = LowSetsCount() - 1;

                return new PrepareItemResult
                {
                    AllowedReplacesCount = replCount,
                    LowSet = true,
                    BInPlayerInvent = _currentSetItems[0].BInPlayerInventory
                };
            }

            return null;
        }

        private PrepareItemResult Prepahe_OL()
        {
            if (OneHandedLowLvlItems.Count < 2)
                return null;

            /*
            if (!OneHandedLowLvlItems[0].BInPlayerInventory && !OneHandedLowLvlItems[1].BInPlayerInventory)
            {
                OneHandedLowLvlItems = OneHandedLowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                    .ToList();
            }
            */

            _currentSetItems = new[]
            {
                OneHandedLowLvlItems[0],
                OneHandedLowLvlItems[1]
            };

            var replCount = LowSetsCount() - 2;
            var inPlayerInvent = _currentSetItems[0].BInPlayerInventory || _currentSetItems[1].BInPlayerInventory;

            return new PrepareItemResult
            {
                AllowedReplacesCount = replCount,
                LowSet = true,
                BInPlayerInvent = inPlayerInvent
            };
        }

        public override void DoLowItemReplace()
        {
            if (TwoHandedLowLvlItems.Count >= 1)
            {
                //TwoHandedLowLvlItems = TwoHandedLowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                //    .ToList();
                _currentSetItems = new[]
                {
                    TwoHandedLowLvlItems[0]
                };
            }
            else if (OneHandedHighLvlItems.Count >= 1 && OneHandedLowLvlItems.Count >= 1)
            {
                //OneHandedLowLvlItems = OneHandedLowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                //    .ToList();
                //OneHandedHighLvlItems = OneHandedHighLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                //    .ToList();

                _currentSetItems = new[]
                {
                    OneHandedHighLvlItems[0],
                    OneHandedLowLvlItems[0]
                };
            }
            else if (OneHandedLowLvlItems.Count >= 2)
            {
                //OneHandedLowLvlItems = OneHandedLowLvlItems.OrderByDescending(x => x.InventPosX + x.InventPosY * 12)
                //    .ToList();
                _currentSetItems = new[]
                {
                    OneHandedLowLvlItems[0],
                    OneHandedLowLvlItems[1]
                };
            }
            else
            {
                //DebugPlugin.LogMsg("Something goes wrong: Can't do low lvl item replace on weapons!",10, Color.Red);//TODO
            }
        }

        public override StashItem[] GetPreparedItems()
        {
            return _currentSetItems;
        }

        public override void RemovePreparedItems()
        {
            RemoveItem(_currentSetItems[0]);

            if (_currentSetItems.Length > 1)
                RemoveItem(_currentSetItems[1]);
        }

        private void RemoveItem(StashItem item)
        {
            if (item.LowLvl)
            {
                TwoHandedLowLvlItems.Remove(item);
                OneHandedLowLvlItems.Remove(item);
            }
            else
            {
                TwoHandedHighLvlItems.Remove(item);
                OneHandedHighLvlItems.Remove(item);
            }
        }

        public override int PlayerInventItemsCount()
        {
            return
                TwoHandedHighLvlItems.Count(x => x.BInPlayerInventory) +
                TwoHandedLowLvlItems.Count(x => x.BInPlayerInventory) +
                OneHandedHighLvlItems.Count(x => x.BInPlayerInventory) +
                OneHandedLowLvlItems.Count(x => x.BInPlayerInventory);
        }
    }
}
