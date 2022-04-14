using System;
using System.Collections.Generic;

namespace TST.HelperClasses
{
    /// <summary>
    /// Serializable Data class to use in Object to Disk Writing
    /// </summary>
    [Serializable]
    public class CutOffData
    {
        public List<int> ListSortedOccurences { get; set; }
        public List<long> ListTransitionCounters { get; set; }
    }//class:CutOffData

    [Serializable]
    public class CutOffDataTST
    {
        public List<long> ListSortedOccurences { get; set; }
        public List<long> ListTransitionCounters { get; set; }
    }//class:CutOffData
}//namespace:TST.Helperclasses
