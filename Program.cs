using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Lomont.Compression;
using TST.HelperClasses;

namespace TST
{
    class MainClass
    {

        #region Constants
        private const string cBASEFILE = @"c:\tmp\33232.zip";
        private const string cCONTROLEXTENSION = @"_Control_";
        private const string cREMAINDERFILE = cBASEFILE + "_Remainder.xxx";
        private const string cZIPEXT = ".gz";
        #endregion


        /// <summary>
        /// MAIN
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            #region DemoFileCreator

            //byte[] tFile = new byte[512];
            //for (int i = 0; i < 256; i++)
            //{
            //    tFile[i] = (byte)i;
            //}
            //for (int i = 255; i < 512; i++)
            //{
            //    tFile[i] = (byte)i;
            //}
            //FileOps.WriteFile(ref tFile, @"c:\tmp\256.zip");
            #endregion

            EntropyChangerData oData = new EntropyChangerData();
            oData.Filename = cBASEFILE;

            //Read File
            byte[] bArrOriginalFile = FileOps.ReadFile(oData.Filename);




            List<byte> tmpLst = new List<byte>();
            foreach (byte b in bArrOriginalFile) if (b < 5) tmpLst.Add(b);

            byte[] bArr = new byte[tmpLst.Count];
            for (int i = 0; i < tmpLst.Count; i++) bArr[i] = tmpLst[i];
            bArrOriginalFile = bArr;

            FileOps.WriteFile(ref bArr, @"c:\tmp\tmp_2202.zip_x"); ;





            Console.WriteLine("Original file, nrof bytes :" + bArrOriginalFile.Count());
            CompressionParam.OriginalSize = bArrOriginalFile.Length;
            //Main

            //Nr Of Loops
            int iRun = 1;
            EntropyChanger ec = new EntropyChanger();
            oData.FileData_IN = bArrOriginalFile;



            for (int iRunCounter = 0; iRunCounter < iRun; iRunCounter++)
            {

                Console.WriteLine("Start run {0} of {1}", iRunCounter, iRun);
                Console.WriteLine("NrOf Zero Bytes: {0}", oData.FileData_IN.Count(x => x == 0));

                ec.ChangeEntropy(iRunCounter, ref oData, true);
                Console.WriteLine("FileData_IN Count {0}={1}", iRunCounter, oData.FileData_IN.Count(x => x == iRunCounter));
                ec.RebuildFileDataToStore(iRunCounter, ref oData);
                Console.WriteLine("FileData_OUT Count {0}={1}", iRunCounter, oData.FileData_OUT.Count(x => x == iRunCounter));

                //To Repeat, Just swap IN en Out, this is a full clean to be sure.
                EntropyChangerData oDataTmp = new EntropyChangerData();
                oDataTmp.FileData_IN = oData.FileData_OUT;
                oData = new EntropyChangerData();
                oData = oDataTmp;
                oData.Filename = cBASEFILE;
                Console.WriteLine("End of Loop.");

                FileInfo fi = null;
                if (ec.Compress)
                    fi = new FileInfo(cBASEFILE + cCONTROLEXTENSION + iRunCounter + ".xxx.gz");
                else
                    fi = new FileInfo(cBASEFILE + cCONTROLEXTENSION + iRunCounter + ".xxx");


                CompressionParam.NewSize += fi.Length;
                if (ec.Compress)
                    fi = new FileInfo(cBASEFILE + "_CutOff_Run_" + iRunCounter + ".xxx.gz");
                else
                    fi = new FileInfo(cBASEFILE + "_CutOff_Run_" + iRunCounter + ".xxx");

                CompressionParam.NewSize += fi.Length;
                fi = null;
            }

            Console.WriteLine("Save Remainder");

            byte[] s = oData.FileData_IN;

            FileOps.WriteFile(ref s, cREMAINDERFILE);
            if (ec.Compress)
            {
                FileOps.Compress(new FileInfo(cREMAINDERFILE));
                File.Delete(cREMAINDERFILE);
            }
            Console.WriteLine("Remainder Saved.");
            if (ec.Compress)
                CompressionParam.NewSize += new FileInfo(cREMAINDERFILE + ".gz").Length;
            else
                CompressionParam.NewSize += new FileInfo(cREMAINDERFILE).Length;



            Console.ReadKey();

            Console.WriteLine("Restruct");

            EntropyChangerData oRestoreFile = new EntropyChangerData();
            oRestoreFile.Filename = cBASEFILE;


            for (int iRunCounter = iRun - 1; iRunCounter >= 0; iRunCounter--)
            {
                ec.LoadFiles(ref oRestoreFile, iRunCounter);
                Console.WriteLine("Found {0} as {1} is expected", oRestoreFile.FileData_IN.Count(x => x == iRunCounter), oRestoreFile.CutOffBytes.Count());

                ec.RebuildFileStep(ref oRestoreFile, iRunCounter);

                Console.WriteLine("Save Stepped back remainder");

                s = oRestoreFile.FileData_OUT;

                FileOps.WriteFile(ref s, cBASEFILE + "_Restored.zip");

                Console.WriteLine("Saved to Disk");



                oRestoreFile.FileData_IN = oRestoreFile.FileData_OUT;




            }


            //Check restored versus original
            bool bResult = true;
            for (int i = 0; i < bArrOriginalFile.Length; i++)
            {

                if (bArrOriginalFile[i] != oRestoreFile.FileData_OUT[i])
                {
                    bResult = false;
                    break;
                }
            }
            if (!bResult) Console.WriteLine("ERROR"); else Console.WriteLine("OK");

            Console.WriteLine("Compression Result: {0}%", CompressionParam.GetPercentage().ToString("N2"));
            Console.ReadKey();
        }

    }
}
