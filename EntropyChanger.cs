using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TST.HelperClasses;

namespace TST
{

    public class EntropyChanger
    {
        //Output to terminal setting
        const bool CSOUTPUT = true;
        const string cZIPEXT1 = ".xxx";
        const string cZIPEXT2 = ".gz";
        const string cREMAINDERFILE = "_Remainder.xxx";
        const string cCONTROLEXTENSION = @"_Control_";
        const bool bCompress = false;
        const bool bDeleteWorkingFiles = false;

        public bool Compress { get { return bCompress; } }




        /// <summary>
        /// Loading state from disk
        /// </summary>
        /// <param name="oData"></param>
        /// <param name="iRunCounter"></param>
        /// <returns></returns>
        public bool LoadFiles(ref EntropyChangerData oData, int iRunCounter)
        {
            bool retVal = true;
            try
            {
                //Read Remainder
                if (bCompress)
                {
                    FileOps.Decompress(new FileInfo(oData.Filename + cREMAINDERFILE + cZIPEXT2));
                    File.Delete(oData.Filename + cREMAINDERFILE + cZIPEXT2);
                }
                oData.FileData_IN = FileOps.ReadFile(oData.Filename + cREMAINDERFILE);
                if (bDeleteWorkingFiles) File.Delete(oData.Filename + cREMAINDERFILE);

                //Load CutOff
                if (bCompress)
                {
                    FileOps.Decompress(new FileInfo(oData.Filename + "_CutOff_Run_" + iRunCounter + cZIPEXT1 + cZIPEXT2));
                    File.Delete(oData.Filename + "_CutOff_Run_" + iRunCounter + cZIPEXT1 + cZIPEXT2);
                }
                oData.CutOffBytes = FileOps.ReadFile(oData.Filename + "_CutOff_Run_" + iRunCounter + cZIPEXT1);
                if (bDeleteWorkingFiles) File.Delete(oData.Filename + "_CutOff_Run_" + iRunCounter + cZIPEXT1);

                //Load Control
                if (bCompress)
                {
                    FileOps.Decompress(new FileInfo(oData.Filename + cCONTROLEXTENSION + iRunCounter + cZIPEXT1 + cZIPEXT2));
                    File.Delete(oData.Filename + cCONTROLEXTENSION + iRunCounter + cZIPEXT1 + cZIPEXT2);
                }


                string sTxt = File.ReadAllText(oData.Filename + cCONTROLEXTENSION + iRunCounter + cZIPEXT1);
                string[] sNrs = sTxt.Split(' ');
                CutOffData cd = new CutOffData();
                cd.ListSortedOccurences = new List<int>(256);
                cd.ListTransitionCounters = new List<long>(256);
                int j = 0;
                for (int i = 0; i < 256; i++)
                {
                    int sOut1;
                    long sOut2;
                    Int32.TryParse(sNrs[j++], out sOut1);
                    Int64.TryParse(sNrs[j++], out sOut2);
                    cd.ListSortedOccurences.Add(sOut1);
                    cd.ListTransitionCounters.Add(sOut2);
                }



                //CutOffData cd = FiledObjects.Load<CutOffData>(oData.Filename + cCONTROLEXTENSION + iRunCounter + cZIPEXT1);
                if (bDeleteWorkingFiles) File.Delete(oData.Filename + cCONTROLEXTENSION + iRunCounter + cZIPEXT1);

                oData.ListSortedOccurences = cd.ListSortedOccurences;
                oData.ListTransitionCounters = cd.ListTransitionCounters;

            }
            catch (Exception ex)
            {
                if (CSOUTPUT)
                {
                    Console.WriteLine(ex.Message);
                    retVal = false;
                }
            }
            return retVal;
        }

        public string RebuildFileStep(ref EntropyChangerData oData, int iRunCounter)
        {
            //Need List of distances to start with



            List<int> lstTmp = new List<int>();
            foreach (byte b in oData.FileData_IN)
                lstTmp.Add(b);
            for (int i = 0; i < lstTmp.Count(); i++) if (lstTmp[i] == 0) lstTmp[i] = -1;

            //Get filepointer based on cutoff point
            EntropyChangerData oCOData = new EntropyChangerData();
            oCOData.ListOfDistances = new List<long>();
            foreach (byte b in oData.CutOffBytes) oCOData.ListOfDistances.Add(b);
            oCOData.ListSortedOccurences = oData.ListSortedOccurences;

            //find correct point
            int iNrOfToDelete = oData.FileData_IN.Length - oData.CutOffBytes.Length;
            for (int i = 0; i < 256; i++)
            {

                if (oData.ListSortedOccurences[0] == 0 && i == 0)
                {
                    i++; iNrOfToDelete -= oCOData.ListSortedOccurences.Count(); iNrOfToDelete -= (int)oData.ListTransitionCounters[0];
                    List<long> lt = new List<long>();
                    for (int ggi = 0; ggi < oData.ListTransitionCounters[0]; ggi++)
                    {
                        lt.Add(0);
                    }
                    foreach (int hhi in oCOData.ListOfDistances) lt.Add(hhi);
                    oCOData.ListOfDistances = lt;
                }
                //End

                long t = oData.ListTransitionCounters[i];

                for (int j = 0; j < t; j++)
                {

                    iNrOfToDelete--;
                    oData.ListTransitionCounters[i]--;
                    if (iNrOfToDelete == 0)
                    {
                        i = 255;
                        break;
                    }
                }

            }

            oCOData.ListTransitionCounters = oData.ListTransitionCounters;


            RebuildFileDataToStore(iRunCounter, ref oCOData);

            //Merge
            int iMergePointer = 0;
            for (int im = 0; im < oData.FileData_IN.Length; im++)
            {
                if (oData.FileData_IN[im] == 0)
                {
                    oData.FileData_IN[im] = oCOData.FileData_OUT[iMergePointer];
                    iMergePointer++;
                }
            }

            oData.FileData_OUT = oData.FileData_IN;


            Console.Write("Busy");




            return "NrOf [0]bytes in remainder" + oData.FileData_OUT.Count(x => x == 0);

        }

        public string RebuildFileStepNew(ref EntropyChangerData oData, int iRunCounter)
        {
            //CutOff Bytes
            //FileData Templated with 1's
            //Goal:
            //Rebuild from CutOff (back to front)
            //Mask over 1 template in file
            //Return

            //Issue nrof template and expected mismatch
            return "";
        }

   
        /// <summary>
        /// Rebuild from distances list
        /// </summary>
        /// <param name="oData"></param>
        /// <returns></returns>
        public bool RebuildFileDataToStore(int iRunCounter, ref EntropyChangerData oData)
        {
            bool retVal = true;

            //try
            //{
            //Determine point to process (ListOfDistances.Count)
            long iNrOffDistancesToTransformToBytes = oData.ListOfDistances.Count;
            //long iFileSize = oData.ListOfDistances.Count();
            // long iFileSize = oData.FileData_IN.Count();

            long iFileSize = 0;
            if (oData.FileData_IN == null)
            {
                if (oData.CutOffBytes != null)
                    iFileSize = oData.ListOfDistances.Count() + oData.CutOffBytes.Length;
                else
                    iFileSize = oData.ListOfDistances.Count();
            }
            else
                iFileSize = oData.FileData_IN.Length;

            //Create placeholder for file
            //byte[] baOutfile = new byte[iFileSize];
            List<int> baOutfile = new List<int>();
            for (int i = 0; i < iFileSize; i++) baOutfile.Add(-1);

            //Assumed the file is sorted correctly and occurences are stored correctly.

            //Loop through sorted byte list
            int iDistancesPointer = 0;
            int iFileDataPointer = 0;

            int ptrList = 0;
            List<long> tmpLst = new List<long>();
            foreach (long l in oData.ListTransitionCounters) tmpLst.Add(l);
            Console.WriteLine("Sum of transition counters {0}", oData.ListTransitionCounters.Sum(x => x));
            //Loop the amount of distances to translate
            for (int i = 0; i < iFileSize; i++)
            {
                while (oData.ListTransitionCounters[ptrList] == 0) ptrList++;

                int iOccurence = oData.ListSortedOccurences[ptrList];

                if (oData.ListOfDistances.Count() > 0)
                    for (int k = 0; k < oData.ListOfDistances[iDistancesPointer]; k++)
                    {
                        iFileDataPointer++;
                        while (baOutfile[iFileDataPointer] != -1)
                            iFileDataPointer++;
                    }

                baOutfile[iFileDataPointer] = (byte)iOccurence;
                iFileDataPointer++;

                //oData.ListTransitionCounters[ptrList]--;
                tmpLst[ptrList]--;

                // if (oData.ListTransitionCounters[ptrList] == 0)
                if (tmpLst[ptrList] == 0)
                {
                    Console.Write(".");
                    ptrList++;
                    iFileDataPointer = 0;

                }

                while (iFileDataPointer < iFileSize && baOutfile[iFileDataPointer] != -1)
                    if (iFileDataPointer < baOutfile.Count() - 1)
                        iFileDataPointer++;
                    else
                        break;

                iDistancesPointer++;

                int iTmp = 0;
                if (iDistancesPointer == oData.ListOfDistances.Count())
                {
                    for (int q = 0; q < baOutfile.Count(); q++)
                        if (baOutfile[q] == -1)
                        {
                            baOutfile[q] = iRunCounter;
                            iTmp++;
                        }

                    Console.WriteLine("-1 to 0: {0}", iTmp);
                    break;
                }
            }

            oData.ClearData();

            byte[] abaOutfile = new byte[iFileSize];
            for (int i = 0; i < baOutfile.Count(); i++) abaOutfile[i] = (byte)baOutfile[i];
            oData.FileData_OUT = abaOutfile;

            //}
            //catch (Exception ex)
            //{
            //    if (CSOUTPUT)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //    retVal = false;

            //}


            return retVal;

        }//bool:RebuildFileDataToStore


     
        /// <summary>
        /// To calculate the relative distances between byte occurrences
        /// </summary>
        /// <param name="oData"></param>
        /// <returns></returns>
        public bool CalculateDistances(int iRunCounter, ref EntropyChangerData oData)
        {
            bool retVal = true;
            //try
            //{
            //Init
            List<Byte> bLstWork = new List<byte>();
            List<long> iLstDistances = new List<long>();
            List<long> bTransitionCtr = new List<long>();
            long bTCtr = 0;

            //Convert byte[] to List<byte>
            foreach (byte b in oData.FileData_IN)
            {
                bLstWork.Add(b);
            }

            //Heuristics
            Dictionary<int, long> dictHeuristics = new Dictionary<int, long>();
            for (int i = 0; i < 256; i++) dictHeuristics.Add(i, 0);
            foreach (byte b in oData.FileData_IN) dictHeuristics[b]++;
            List<int> iLstSorted = null;

            //Sort Low-High
            iLstSorted = dictHeuristics.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();

            //First run (runCounter==0) then 0 at the end else 0 at the beginning
            List<int> iLstSortedCopy = new List<int>(iLstSorted);

            int iZeroLocationInSortedList = iLstSorted.FindIndex(x => x == 0);
            if (iZeroLocationInSortedList == -1) throw new Exception("0 byte not found");

            if (iRunCounter == 0)
            {
                //First run, zero to end.
                if (iLstSorted[iLstSorted.Count - 1] != 0) //Check zero already at end
                {
                    for (int i = iZeroLocationInSortedList; i < iLstSorted.Count - 1; i++)
                    {
                        iLstSorted[i] = iLstSortedCopy[i + 1];
                    }
                    iLstSorted[iLstSorted.Count - 1] = 0;
                }
            }
            else
            {
                //Next run, zero at start. (will be automatic)
                if (iLstSorted[0] != 0)
                {
                    for (int i = 0; i < iZeroLocationInSortedList; i++)
                    {
                        iLstSorted[i + 1] = iLstSortedCopy[i];
                    }
                    iLstSorted[0] = 0;
                }
            }
            oData.ListSortedOccurences = iLstSorted;


            //Change Entropy
            foreach (int j in iLstSorted) //
            {
                //Distance
                int iDistance = 0;

                //Data loop
                for (int i = 0; i < bLstWork.Count(); i++)
                {
                    //Search Next
                    if (j == bLstWork[i])
                    {
                        //Add
                        iLstDistances.Add(iDistance);
                        iDistance = 0;
                        bTCtr++;
                    }
                    else
                        iDistance++;//Next

                }

                //Clean handled byte
                bLstWork.RemoveAll(by => by == j);

                //Add Transition Counter
                bTransitionCtr.Add(bTCtr);//Can be optimized, start +/- 0,i+
                bTCtr = 0;

            }//Change Entropy	
            oData.ListTransitionCounters = bTransitionCtr;

            if (CSOUTPUT)
            {
                //Show some stats about Distances > 255
                Console.WriteLine(">1:{0}", iLstDistances.Count(x => x > 1));
                Console.WriteLine(">3:{0}", iLstDistances.Count(x => x > 3));
                Console.WriteLine(">7:{0}", iLstDistances.Count(x => x > 7));
                Console.WriteLine(">15:{0}", iLstDistances.Count(x => x > 15));
                Console.WriteLine(">31:{0}", iLstDistances.Count(x => x > 31));
                Console.WriteLine(">63:{0}", iLstDistances.Count(x => x > 63));
                Console.WriteLine(">127:{0}", iLstDistances.Count(x => x > 127));
                Console.WriteLine(">255:{0}", iLstDistances.Count(x => x > 255));
                Console.WriteLine(">511:{0}", iLstDistances.Count(x => x > 511));
                Console.WriteLine(">1023:{0}", iLstDistances.Count(x => x > 1023));
                Console.WriteLine(">2047:{0}", iLstDistances.Count(x => x > 2047));
                Console.WriteLine(">4095:{0}", iLstDistances.Count(x => x > 4095));
                Console.WriteLine(">8191:{0}", iLstDistances.Count(x => x > 8191));
            }
            oData.ListOfDistances = iLstDistances;

            //Check if it calculates back to original
            EntropyChangerData oDataCheck = new EntropyChangerData();
            oDataCheck.ListOfDistances = oData.ListOfDistances;
            oDataCheck.FileData_IN = oData.FileData_IN;
            oDataCheck.ListSortedOccurences = oData.ListSortedOccurences;
            oDataCheck.ListTransitionCounters = oData.ListTransitionCounters;
            RebuildFileDataToStore(iRunCounter, ref oDataCheck);

            if (oDataCheck.FileData_OUT != null)
                for (int i = 0; i < oData.FileData_IN.Count(); i++)
                    if (oData.FileData_IN[i] != oDataCheck.FileData_OUT[i])
                        throw new Exception("Check Distances to Original before changing failed");

            //}
            //catch (Exception ex)
            //{
            //    if (CSOUTPUT)
            //        Console.WriteLine(ex.Message);
            //    retVal = false;
            //}
            return retVal;
        }//bool:CalculateDistances

        public bool CalculateDistancesTST(int iRunCounter, ref EntropyChangerDataTST oData)
        {
            bool retVal = true;

            //Init
            List<long> bLstWork = new List<long>();
            List<long> iLstDistances = new List<long>();
            List<long> bTransitionCtr = new List<long>();
            long bTCtr = 0;

            //Convert byte[] to List<byte>
            foreach (long b in oData.FileData_IN)
            {
                bLstWork.Add(b);
            }


            //Heuristics
            Dictionary<long, long> dictHeuristics = new Dictionary<long, long>();
            foreach (int i in oData.FileData_IN.Select(x => x).Distinct()) dictHeuristics.Add(i, 0);
            foreach (long b in oData.FileData_IN) dictHeuristics[b]++;
            List<long> iLstSorted = dictHeuristics.OrderBy(x => x.Value).Select(x => x.Key).ToList();
            oData.ListSortedOccurences = iLstSorted;


            //Change Entropy
            foreach (int j in iLstSorted) //
            {
                //Distance
                int iDistance = 0;

                //Data loop
                for (int i = 0; i < bLstWork.Count(); i++)
                {
                    //Search Next
                    if (j == bLstWork[i])
                    {
                        //Add
                        iLstDistances.Add(iDistance);
                        iDistance = 0;
                        bTCtr++;
                    }
                    else
                        iDistance++;//Next

                }

                //Clean handled byte
                bLstWork.RemoveAll(by => by == j);

                //Add Transition Counter
                bTransitionCtr.Add(bTCtr);//Can be optimized, start +/- 0,i+
                bTCtr = 0;

            }//Change Entropy	
            oData.ListTransitionCounters = bTransitionCtr;

            if (CSOUTPUT)
            {
                //Show some stats about Distances > 255
                Console.WriteLine(">1:{0}", iLstDistances.Count(x => x > 1));
                Console.WriteLine(">3:{0}", iLstDistances.Count(x => x > 3));
                Console.WriteLine(">7:{0}", iLstDistances.Count(x => x > 7));
                Console.WriteLine(">15:{0}", iLstDistances.Count(x => x > 15));
                Console.WriteLine(">31:{0}", iLstDistances.Count(x => x > 31));
                Console.WriteLine(">63:{0}", iLstDistances.Count(x => x > 63));
                Console.WriteLine(">127:{0}", iLstDistances.Count(x => x > 127));
                Console.WriteLine(">255:{0}", iLstDistances.Count(x => x > 255));
                Console.WriteLine(">511:{0}", iLstDistances.Count(x => x > 511));
                Console.WriteLine(">1023:{0}", iLstDistances.Count(x => x > 1023));
                Console.WriteLine(">2047:{0}", iLstDistances.Count(x => x > 2047));
                Console.WriteLine(">4095:{0}", iLstDistances.Count(x => x > 4095));
                Console.WriteLine(">8191:{0}", iLstDistances.Count(x => x > 8191));
            }
            oData.ListOfDistances = iLstDistances;

            return retVal;
        }//bool:CalculateDistances


        /// <summary>
        /// Change Entropy by creating Entrooy Data object
        /// </summary>
        /// <param name="iRunCounter"></param>
        /// <param name="oData"></param>
        /// <param name="bSave"></param>
        /// <returns></returns>
        public bool ChangeEntropy(int iRunCounter, ref EntropyChangerData oData, bool bSave)
        {
            bool retVal = true;
            try
            {
                //Calculate Distance
                CalculateDistances(iRunCounter, ref oData);

                //Delete CutOff from ListOfDistances, return as byte Array.
                byte[] iArray = GetCutOffObject(ref oData);
                oData.CutOffBytes = iArray;
                //oData.CutOffPoint = oData.FileData_IN.Count() - oData.CutOffBytes.Length;

                //Save CutOff Bytes and Control Data
                SaveCutOffObject(iRunCounter, ref oData);
                SaveCutOffData(iRunCounter, ref oData);
            }
            catch (Exception ex)
            {
                if (CSOUTPUT)
                {
                    Console.WriteLine(ex.Message);
                }
                retVal = false;
            }
            return retVal;
        }//bool:ChangeEntropy

        /// <summary>
        /// Determine CutOff Point, CutOff to return in byte array, list of distances to decrease according to cutOff.
        /// </summary>
        /// <param name="oData"></param>
        /// <returns></returns>
        public byte[] GetCutOffObject(ref EntropyChangerData oData)
        {
            const int CiCutOff = 255;
            byte[] iArray = null;
            try
            {
                //Find index > 255, plus 1, cut off
                int i255 = oData.ListOfDistances.FindLastIndex(x => x > CiCutOff) + 1;
                long res = 0;
                long it = oData.FileData_IN.Length - i255;
                int ifs = oData.FileData_IN.Length;
                for (int j = 255; j >= 0; j--)
                {
                    it -= oData.ListTransitionCounters[j];
                    if (it < 0)
                    {
                        it = j + 1;
                        break;
                    }

                }
                for (int i = (int)it; i < 256; i++)
                    res += oData.ListTransitionCounters[i];

                i255 = oData.FileData_IN.Length - (int)res;



                //Create list to cut off
                List<long> iLstCutOff = oData.ListOfDistances.GetRange(i255, oData.ListOfDistances.Count - i255);

                //Cut off iLstRemCutOffainder part
                oData.ListOfDistances.RemoveRange(i255, oData.ListOfDistances.Count - i255);

                //No integers > 255 allowed
                if (iLstCutOff.FindIndex(x => x > 255) != -1)
                    throw new Exception("Error in CutOff List, integer > 255");

                //Convert to Array
                iArray = new byte[iLstCutOff.Count()];
                for (int ops = 0; ops < iLstCutOff.Count(); ops++)
                {
                    iArray[ops] = (byte)iLstCutOff[ops];
                }
                // oData.CutOffPoint = iArray.Count();
                oData.CutOffBytes = iArray;
            }
            catch (Exception ex)
            {
                if (CSOUTPUT)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return iArray;
        }

       
        /// <summary>
        /// Writes distances in 2 power list
        /// </summary>
        /// <param name="oData"></param>
        public void ShowStatistics(ref EntropyChangerData oData)
        {
            try
            {

                //Show some stats about Distances > 255
                Console.WriteLine(">1:{0}", oData.ListOfDistances.Count(x => x > 1));
                Console.WriteLine(">3:{0}", oData.ListOfDistances.Count(x => x > 3));
                Console.WriteLine(">7:{0}", oData.ListOfDistances.Count(x => x > 7));
                Console.WriteLine(">15:{0}", oData.ListOfDistances.Count(x => x > 15));
                Console.WriteLine(">31:{0}", oData.ListOfDistances.Count(x => x > 31));
                Console.WriteLine(">63:{0}", oData.ListOfDistances.Count(x => x > 63));
                Console.WriteLine(">127:{0}", oData.ListOfDistances.Count(x => x > 127));
                Console.WriteLine(">255:{0}", oData.ListOfDistances.Count(x => x > 255));
                Console.WriteLine(">511:{0}", oData.ListOfDistances.Count(x => x > 511));
                Console.WriteLine(">1023:{0}", oData.ListOfDistances.Count(x => x > 1023));
                Console.WriteLine(">2047:{0}", oData.ListOfDistances.Count(x => x > 2047));
                Console.WriteLine(">4095:{0}", oData.ListOfDistances.Count(x => x > 4095));
                Console.WriteLine(">8191:{0}", oData.ListOfDistances.Count(x => x > 8191));

            }
            catch (Exception ex)
            {
                if (CSOUTPUT)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }//void:ShowStatistics

        /// <summary>
        /// Saving bytestream of distances from main distances list
        /// </summary>
        /// <param name="cd"></param>
        /// <returns></returns>
        public bool SaveCutOffObject(int iRunCounter, ref EntropyChangerData oData)
        {
            bool retVal = true;
            try
            {

                string sFileNameCutOff = oData.Filename + "_CutOff_Run_" + iRunCounter + cZIPEXT1;
                byte[] bArray = oData.CutOffBytes;

                //Write CutOff, Compress, Delete original
                FileOps.WriteFile(ref bArray, sFileNameCutOff);
                if (bCompress)
                {
                    FileOps.Compress(new FileInfo(sFileNameCutOff));
                    File.Delete(sFileNameCutOff);
                }

            }
            catch (Exception ex)
            {
                if (CSOUTPUT)
                {
                    Console.WriteLine(ex.Message);
                }
                retVal = false;
            }
            return retVal;
        }//bool:SaveCutOffObject



        /// <summary>
        /// Saving lists of occurences and byte sort as an object.
        /// Can be made more efficient by adding numbers (under 1k)
        /// </summary>
        /// <param name="oData"></param>
        /// <returns></returns>
        public bool SaveCutOffData(int iRunCounter, ref EntropyChangerData oData)
        {
            bool retVal = true;
            try
            {
                CutOffData cd = new CutOffData();
                cd.ListSortedOccurences = oData.ListSortedOccurences;
                cd.ListTransitionCounters = oData.ListTransitionCounters;

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 256; i++)
                {
                    sb.Append(cd.ListSortedOccurences[i]);
                    sb.Append(" ");
                    sb.Append(cd.ListTransitionCounters[i]);
                    if (i<255)
                     sb.Append(" ");
                }

                File.WriteAllText(oData.Filename + "_Control_" + iRunCounter + cZIPEXT1, sb.ToString());


                if (bCompress)
                {
                    FileOps.Compress(new FileInfo(oData.Filename + "_Control_" + iRunCounter + cZIPEXT1));
                    File.Delete(oData.Filename + "_Control_" + iRunCounter + cZIPEXT1);
                }
            }
            catch (Exception ex)
            {
                if (CSOUTPUT)
                {
                    Console.WriteLine(ex.Message);
                    retVal = false;

                }
            }
            return retVal;
        }//bool:SaveCutOffData

        public bool SaveCutOffDataSaved(int iRunCounter, ref EntropyChangerData oData)
        {
            bool retVal = true;
            try
            {
                CutOffData cd = new CutOffData();
                cd.ListSortedOccurences = oData.ListSortedOccurences;
                cd.ListTransitionCounters = oData.ListTransitionCounters;

                if (!FiledObjects.Save<CutOffData>(oData.Filename + "_Control_" + iRunCounter + cZIPEXT1, cd))
                    throw new Exception("Object not saved to disk.");
                if (bCompress)
                {
                    FileOps.Compress(new FileInfo(oData.Filename + "_Control_" + iRunCounter + cZIPEXT1));
                    File.Delete(oData.Filename + "_Control_" + iRunCounter + cZIPEXT1);
                }
            }
            catch (Exception ex)
            {
                if (CSOUTPUT)
                {
                    Console.WriteLine(ex.Message);
                    retVal = false;

                }
            }
            return retVal;
        }//bool:SaveCutOffData


      


    }//class:EntropyChanger
}//namespace:TST

