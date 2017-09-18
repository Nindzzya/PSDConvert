using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Text;
using PhotoshopFile;
using Endogine.Serialization;

namespace Convert
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Out.WriteLine("Start Program");

            if (args.Length < 2)
            {
                Console.Out.WriteLine("Paramter Error, Extis");
                return;
            }

            Dictionary<string, Color> convertMap = new Dictionary<string, Color>();
            for (int index = 1; index < args.Length; ++index)
            {
                string[] converString = args[index].Split(':');
                if (converString.Length != 2) return;
                convertMap.Add(converString[0].ToUpper(), ColorTranslator.FromHtml(converString[1]));
            }

            WalkDirectoryTree(new DirectoryInfo(args[0]), convertMap);

            Console.Out.WriteLine("Exit Program");
        }

        static void WalkDirectoryTree(DirectoryInfo root, Dictionary<string, Color> convertMap)
        {
            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;

            try
            {
                files = root.GetFiles("*.*");
            }
            catch
            {
            }

            if (files != null)
            {
                foreach (FileInfo fi in files)
                {
                    if (fi.Extension.ToLower() == ".psd")
                        UpdatePsd(fi, convertMap);
                }

                subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    WalkDirectoryTree(dirInfo, convertMap);
                }
            }
        }

        static private void UpdatePsd(FileInfo fileInfo, Dictionary<string, Color> convertMap)
        {
            Console.Out.WriteLine("Start Convert " + fileInfo.FullName);

            var fileStream = new FileStream(fileInfo.FullName, FileMode.Open);
            if (fileStream == null)
            {
                Console.Out.WriteLine("Paramter Error, Skip");
                return;
            }

            var psdFile = new PsdFile(fileStream, new LoadContext());
            fileStream.Close();

            foreach (Layer layr in psdFile.Layers)
            {
                var layName = layr.Name.ToUpper();
                try
                {
                    var findKey = convertMap.Single(x => layName.Contains(x.Key));
                    var color = findKey.Value;

                    var vscg = layr.AdditionalInfo.SingleOrDefault(x => x.Key == "vscg");
                    var solidColor = layr.AdditionalInfo.SingleOrDefault(x => x.Key == "SoCo");
                    //var tysh = layr.AdditionalInfo.SingleOrDefault(x => x.Key == "TySh");
                    if (solidColor == null /*&& tysh == null*/ && vscg == null)
                    {
                        ConsoleColor oldColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Out.WriteLine("Convert Error : " + fileInfo.FullName);
                        Console.ForegroundColor = oldColor;
                        return;
                    }

                    if (vscg != null)
                    {
                        solidColor = vscg;
                    }

                    if (solidColor != null)
                    {
                        PhotoshopFile.PsdDescriptor eff = new PhotoshopFile.PsdDescriptor(CreateReadStream(solidColor));

                        double red = color.R;
                        double green = color.G;
                        double blue = color.B;
                        byte[] redBuffer, greenBuffer, blueBuffer;
                        unsafe
                        {
                            BinaryReverseReader.SwapBytes((byte*)&red, 8);
                            redBuffer = ConvertDoubleToByteArray(red);
                            BinaryReverseReader.SwapBytes((byte*)&green, 8);
                            greenBuffer = ConvertDoubleToByteArray(green);
                            BinaryReverseReader.SwapBytes((byte*)&blue, 8);
                            blueBuffer = ConvertDoubleToByteArray(blue);
                        }

                        long redOffset = 0, greenOffset = 0, blueOffset = 0;
                        GetPSDColorOffset(eff, ref redOffset, ref greenOffset, ref blueOffset);

                        BinaryReverseWriter writer = CreateWriteStream(solidColor);
                        writer.Seek((int)redOffset, SeekOrigin.Begin);
                        writer.Write(redBuffer);
                        writer.Seek((int)greenOffset, SeekOrigin.Begin);
                        writer.Write(greenBuffer);
                        writer.Seek((int)blueOffset, SeekOrigin.Begin);
                        writer.Write(blueBuffer);
                    }
                    //else if (tysh != null)
                    //{
                    //    PhotoshopFile.LayerResources.TypeToolTyShPH6 txt = new PhotoshopFile.LayerResources.TypeToolTyShPH6(CreateReadStream(tysh));
                    //    Dictionary<string, object> d = txt.StylesheetReader.GetStylesheetDataFromLongestRun();
                    //    BinaryReverseWriter writer = CreateWriteStream(tysh);
                    //    //writer.Seek((int)redOffset, SeekOrigin.Begin);
                    //    //writer.Write(redBuffer);
                    //    //writer.Seek((int)greenOffset, SeekOrigin.Begin);
                    //    //writer.Write(greenBuffer);
                    //    //writer.Seek((int)blueOffset, SeekOrigin.Begin);
                    //    //writer.Write(blueBuffer);
                    //}
                }
                catch
                {
                }
            }

            psdFile.Save(fileInfo.DirectoryName + "\\_" + fileInfo.Name, Encoding.Default);
            Console.Out.WriteLine("Convert Finished, Save to " + fileInfo.DirectoryName + "\\_" + fileInfo.Name, Encoding.Default);
        }

        static private BinaryReverseReader CreateReadStream(LayerInfo info)
        {
            RawLayerInfo rawly = (RawLayerInfo)info;
            Stream str = new MemoryStream(rawly.Data);
            BinaryReverseReader brd = new BinaryReverseReader(str);
            if (rawly.Key == "vogk")
            {
                int version = brd.ReadInt32();
                int version1 = brd.ReadInt32();
            }
            else if (rawly.Key == "vstk")
            {
                int vers = brd.ReadInt32();
            }
            else if (rawly.Key == "vscg")
            {
                int v = brd.ReadInt32();
                int v1 = brd.ReadInt32();
            }
            else if (rawly.Key == "lfx2")
            {
                int v = brd.ReadInt32();
                int v1 = brd.ReadInt32();
            }
            else if (rawly.Key == "SoCo")
            {
                int v = brd.ReadInt32();
            }
            return brd;
        }

        static private BinaryReverseWriter CreateWriteStream(LayerInfo info)
        {
            RawLayerInfo rawly = (RawLayerInfo)info;
            Stream stream = new MemoryStream(rawly.Data);
            BinaryReverseWriter writer = new BinaryReverseWriter(stream);
            return writer;
        }

        static private string GetPSDColor(PsdDescriptor descr)
        {
            PsdDescriptor clr = (PsdDescriptor)descr.getObjects()["Clr "];
            Dictionary<string, PsdObject> values = clr.getObjects();
            var valR = (PsdDouble)values["Rd  "];
            var valG = (PsdDouble)values["Grn "];
            var valB = (PsdDouble)values["Bl  "];
            return Color.FromArgb((int)(ReadPSDDouble(GetByteStreamFromDouble(valR.getValue()))), (int)(ReadPSDDouble(GetByteStreamFromDouble(valG.getValue()))), (int)(ReadPSDDouble(GetByteStreamFromDouble(valB.getValue())))).Name;
        }

        static private void GetPSDColorOffset(PsdDescriptor descr, ref long redOffset, ref long greenOffset, ref long blueOffset)
        {
            PsdDescriptor clr = (PsdDescriptor)descr.getObjects()["Clr "];
            Dictionary<string, PsdObject> values = clr.getObjects();
            var valR = (PsdDouble)values["Rd  "];
            var valG = (PsdDouble)values["Grn "];
            var valB = (PsdDouble)values["Bl  "];
            redOffset = valR.offset;
            greenOffset = valG.offset;
            blueOffset = valB.offset;
        }

        static private BinaryReverseReader GetByteStreamFromDouble(double val)
        {
            byte[] barr = ConvertDoubleToByteArray(val);
            Stream st = new MemoryStream(barr);
            BinaryReverseReader rd = new BinaryReverseReader(st);
            return rd;
        }

        static private double ReadPSDDouble(BinaryReverseReader br)
        {

            double val = br.ReadDouble();
            unsafe
            {
                BinaryReverseReader.SwapBytes((byte*)&val, 8);
            }
            return val;
        }

        static private byte[] ConvertDoubleToByteArray(double d)
        {
            return BitConverter.GetBytes(d);
        }
    }
}
