using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace TST
{
    public static class FileOps
    {


        /// <summary>
        /// Reads file to bytearra
        /// </summary>
        /// <param name="sPath">UNC path to file</param>
        /// <returns></returns>
        public static byte[] ReadFile(string sPath)
        {
            byte[] aBytes = null;
            try
            {
                aBytes = File.ReadAllBytes(sPath);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            return aBytes;
        }//ReadFile

        public static bool WriteFile(ref byte[] aBytes, string sPath)
        {
            try
            {
                File.WriteAllBytes(sPath, aBytes);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return false;
            }
        }//WriteFile

        public static void Compress(FileInfo fileToCompress)
        {
            using (FileStream originalFileStream = fileToCompress.OpenRead())
            {
                if ((File.GetAttributes(fileToCompress.FullName) &
                   FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
                {
                    using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".gz"))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                           CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);

                        }
                    }
                    FileInfo info = new FileInfo(@"C:\tmp\" +  fileToCompress.Name + ".gz");
                    Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
                    fileToCompress.Name, fileToCompress.Length.ToString(), info.Length.ToString());
                }

            }

        }

        public static void Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
            }
        }

    }
}
