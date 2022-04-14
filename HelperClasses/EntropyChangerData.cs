using System.Collections.Generic;

namespace TST.HelperClasses
{
    public class EntropyChangerData
    {
        //Properties
        public string Filename { get; set; }
        public byte[] FileData_IN { get; set; }
        public byte[] FileData_OUT { get; set; }
        public long CutOffPoint { get; set; }
        public byte[] CutOffBytes { get; set; }
        public List<int> ListSortedOccurences { get; set; }
        public List<long> ListTransitionCounters { get; set; }
        public List<long> ListOfDistances { get; set; }

        /// <summary>
        /// Clear part of the contents of object.
        /// </summary>
        public void ClearData()
        {
            FileData_IN = null;
            FileData_OUT = null;
            CutOffPoint = -1;
            CutOffBytes = null;
        }//void:ClearData

    }//class:EntropyChangerData


    public class EntropyChangerDataTST
    {
        //Properties
        public string Filename { get; set; }
        public long[] FileData_IN { get; set; }
        public long[] FileData_OUT { get; set; }
        public long CutOffPoint { get; set; }
        public long[] CutOffBytes { get; set; }
        public List<long> ListSortedOccurences { get; set; }
        public List<long> ListTransitionCounters { get; set; }
        public List<long> ListOfDistances { get; set; }

        /// <summary>
        /// Clear part of the contents of object.
        /// </summary>
        public void ClearData()
        {
            FileData_IN = null;
            FileData_OUT = null;
            CutOffPoint = -1;
            CutOffBytes = null;
        }//void:ClearData

    }//class:EntropyChangerData

}//namespace:TST.HelperClasses
