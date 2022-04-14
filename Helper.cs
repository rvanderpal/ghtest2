using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;


namespace TST
{
    public static class CompressionParam
    {
        //Compression parameters
        public static long OriginalSize { get; set; }
        public static long NewSize { get; set; }

        public static double GetPercentage()
        {
              return (double)NewSize / (double)OriginalSize*100;
        }

    }
    public static class FiledObjects
    {
        public static T Load<T>(string FileName)
        {
            Object rslt;

            if (File.Exists(FileName))
            {
                var xs = new XmlSerializer(typeof(T));

                using (var sr = new StreamReader(FileName))
                {
                    rslt = (T)xs.Deserialize(sr);
                }
                return (T)rslt;
            }
            else
            {
                return default(T);
            }
        }


        public static bool Save<T>(string FileName, Object Obj)
        {
            var xs = new XmlSerializer(typeof(T));
            using (TextWriter sw = new StreamWriter(FileName))
            {
                xs.Serialize(sw, Obj);
            }

            if (File.Exists(FileName))
                return true;
            else return false;
        }
    }


    public class Helper
    {



        public byte[] GetByteArray(string sPath)
        {
            byte[] baseList = null;
            try
            {
                baseList = File.ReadAllBytes(sPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return baseList;
        }


        //public byte[] Encode(List<int> sl)
        //{
        //	//Encode Arithmetic (Range)
        //	const int alphabet_size = 317; //NrOf Symbols

        //	int data_size = sl.Count ();//Size of DataStream

        //	int RANGE_SIZE_IN_BITS = 16; //Int16, 2Byte, 16bit


        //	//Create Int array
        //	int[] data = sl.ToArray ();

        //	//make ranges for data
        //	const int cm_size = alphabet_size + 2;
        //	int[] cm = new int[cm_size];
        //	Auxiliary.makeRanges(ref data, data_size, ref cm, alphabet_size, RANGE_SIZE_IN_BITS);
        //	//ranges are completed


        //	//Calculate Optimal Entropy
        //	double entropy = Auxiliary.calculate_entropy (data, data_size);
        //	//Forecast size
        //	int expected_size = (int)(entropy * (double)(data_size) / 8.0);
        //	//Console Output
        //	Console.WriteLine ("Expected size: {0}", expected_size);

        //	//Encode
        //	DataStorage ds = new DataStorage (expected_size*2);
        //	DateTime TimeEncodeStart = DateTime.Now;

        //	RangeMapper rm_encode = new RangeMapper (RANGE_SIZE_IN_BITS, ref ds);
        //	for (int i = 0; i < data_size; ++i) {


        //		rm_encode.encodeRange (cm [data [i]], cm [data [i] + 1]);
        //	}
        //	rm_encode.encodeRange (cm [alphabet_size], cm [alphabet_size + 1]); //end of data marker
        //	rm_encode.flush ();
        //	DateTime TimeEncodeEnd = DateTime.Now;
        //	TimeSpan encodespan = TimeEncodeEnd - TimeEncodeStart;

        //	Console.WriteLine ("Encoding time {0} miliseconds, actual size {1} bytes.", 
        //		(encodespan.Seconds * 1000 + encodespan.Milliseconds).ToString (),
        //		ds.nSizeTaken.ToString ());
        //	//end encoding

        //	//Write Encoded to File
        //	List<byte> bytes = new List<byte> (ds.nSizeTaken * sizeof(byte));

        //	for (int u = 0; u < ds.nSizeTaken; u++)
        //		bytes.Add (BitConverter.GetBytes (ds.data [u]) [0]);

        //	//File.Delete (@"/Users/richardvdPal/Google Drive/TST/test.zip");
        //	//File.WriteAllBytes (@"/Users/richardvdPal/Google Drive/TST/test.zip", bytes.ToArray ());

        //	//Decode
        //	DateTime TimeDecodeStart = DateTime.Now;
        //	int lookup_size = (1 << RANGE_SIZE_IN_BITS) + 1;
        //	short[] lookup = new short[lookup_size];  
        //	Auxiliary.makeLookupTable (ref cm, cm_size, ref lookup);
        //	ds.SetPointer ();
        //	bool isOK = true;
        //	RangeMapper rm_decode = new RangeMapper (RANGE_SIZE_IN_BITS, ref ds);
        //	rm_decode.init ();
        //	int k = 0;
        //	while (true) {
        //		int midpoint = rm_decode.getMidPoint ();
        //		//next is binary search algorithm that does not need having lookup array
        //		//int index = Auxiliary.findInterval(cm, alphabet_size + 2, midpoint);
        //		//this is lookup table that expedites execution, either of these functions works
        //		int index = lookup [midpoint]; //midpoint presumed being within correct boundaries
        //		if (index == alphabet_size)
        //			break; //end of data marker
        //		if (index != data [k]) {
        //			Console.WriteLine ("Data mismatch {0} element {1}", k, index);
        //			isOK = false;
        //			break;
        //		}
        //		rm_decode.decodeRange (cm [index], cm [index + 1]);
        //		++k;
        //	}
        //	if (k != data_size)
        //		isOK = false;
        //	DateTime TimeDecodeEnd = DateTime.Now;
        //	TimeSpan decodespan = TimeDecodeEnd - TimeDecodeStart;

        //	//Console Output
        //	Console.WriteLine ("Decoding time {0} miliseconds",
        //		(decodespan.Seconds * 1000 + decodespan.Milliseconds).ToString ());
        //	if (isOK) {
        //		Console.WriteLine ("Round trip is OK");
        //	} else {
        //		Console.WriteLine ("Data mismatch");
        //	}
        //	//end decoding

        //	return bytes.ToArray ();

        //}


        //public byte[] Decode(byte[] aDec)
        //{
        //	//Encode Arithmetic (Range)
        //	const int alphabet_size = 319; //NrOf Symbols

        //	int data_size = aDec.Count()-1;//Size of DataStream

        //	int RANGE_SIZE_IN_BITS = 16; //Int16, 2Byte, 16bit


        //	//Create Int array
        //	int[] data = new int[data_size];
        //	for (int b = 0; b < data_size; b++)
        //		data [b] = aDec [b];

        //	DataStorage ds = new DataStorage (data_size*2);
        //	ds.data = aDec;

        //	//make ranges for data
        //	const int cm_size = alphabet_size + 2;
        //	int[] cm = new int[cm_size];
        //	Auxiliary.makeRanges(ref data, data_size, ref cm, alphabet_size, RANGE_SIZE_IN_BITS);
        //	//ranges are completed



        //	List<byte> bytes = new List<byte> (ds.nSizeTaken * sizeof(byte));

        //	//Decode
        //	DateTime TimeDecodeStart = DateTime.Now;
        //	int lookup_size = (1 << RANGE_SIZE_IN_BITS) + 1;
        //	short[] lookup = new short[lookup_size];  
        //	Auxiliary.makeLookupTable (ref cm, cm_size, ref lookup);
        //	ds.SetPointer ();
        //	bool isOK = true;
        //	RangeMapper rm_decode = new RangeMapper (RANGE_SIZE_IN_BITS, ref ds);
        //	rm_decode.init ();
        //	int k = 0;
        //	while (true) {
        //		int midpoint = rm_decode.getMidPoint ();
        //		//next is binary search algorithm that does not need having lookup array
        //		//int index = Auxiliary.findInterval(cm, alphabet_size + 2, midpoint);
        //		//this is lookup table that expedites execution, either of these functions works
        //		int index = lookup [midpoint]; //midpoint presumed being within correct boundaries
        //		if (index == alphabet_size)
        //			break; //end of data marker
        //		if (index != data [k]) {
        //			Console.WriteLine ("Data mismatch {0} element {1}", k, index);
        //			isOK = false;
        //			break;
        //		}
        //		rm_decode.decodeRange (cm [index], cm [index + 1]);
        //		++k;
        //	}
        //	if (k != data_size)
        //		isOK = false;
        //	DateTime TimeDecodeEnd = DateTime.Now;
        //	TimeSpan decodespan = TimeDecodeEnd - TimeDecodeStart;

        //	//Console Output
        //	Console.WriteLine ("Decoding time {0} miliseconds",
        //		(decodespan.Seconds * 1000 + decodespan.Milliseconds).ToString ());
        //	if (isOK) {
        //		Console.WriteLine ("Round trip is OK");
        //	} else {
        //		Console.WriteLine ("Data mismatch");
        //	}
        //	//end decoding

        //	return bytes.ToArray ();
        //}

        public byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public cKillPoint ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            cKillPoint obj = (cKillPoint)binForm.Deserialize(memStream);
            return obj;
        }




    }//class

    [Serializable]
    public class cKillPoint
    {
        public cKillPoint()
        {
            bTransitionCtr = new List<int>();
        }
        public int i
        {
            get;
            set;
        }
        public int j
        {
            get;
            set;
        }
        public int k
        {
            get;
            set;
        }
        public int iPtr
        {
            get;
            set;
        }
        public List<int> bTransitionCtr
        {
            get;
            set;
        }
        public int iC
        {
            get;
            set;
        }
    }//class
}//namespace

