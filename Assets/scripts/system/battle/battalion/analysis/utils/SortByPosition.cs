using System.Collections.Generic;
using component.battle.battalion.data_holders;

namespace system.battle.battalion.analysis.utils
{
    public class SortByPosition : IComparer<BattalionInfo>
    {
        public int Compare(BattalionInfo e1, BattalionInfo e2)
        {
            return e2.position.x.CompareTo(e1.position.x);
        }
    }

    public class SortByPositionDesc : IComparer<BattalionInfo>
    {
        public int Compare(BattalionInfo e1, BattalionInfo e2)
        {
            return e1.position.x.CompareTo(e2.position.x);
        }
    }
}