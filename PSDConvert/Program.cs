using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Text;

namespace PSDConvert
{
    class Program
    {
        static Photoshop.Application PhotoshopApplication = new Photoshop.Application();

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

            DirectoryInfo root = new DirectoryInfo(args[0]);
            string saveFolder = root.Parent.FullName + "\\ConvertOutPut";
            Directory.CreateDirectory(saveFolder);
            WalkDirectoryTree(root, convertMap, saveFolder);

            Console.Out.WriteLine("Exit Program");

        }

        static void WalkDirectoryTree(DirectoryInfo root, Dictionary<string, Color> convertMap, string saveFolder)
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

            saveFolder = saveFolder + "\\" + root.Name;
            Directory.CreateDirectory(saveFolder);

            if (files != null)
            {
                foreach (FileInfo fi in files)
                {
                    if (fi.Extension.ToLower() == ".psd")
                        UpdatePsd(fi, convertMap, saveFolder);
                }

                subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    WalkDirectoryTree(dirInfo, convertMap, saveFolder);
                }
            }
        }

        static private void UpdatePsd(FileInfo fileInfo, Dictionary<string, Color> convertMap, string saveFolder)
        {
            Console.Out.WriteLine("Start Convert " + fileInfo.FullName);

            Photoshop.Document psdDocument = PhotoshopApplication.Open(fileInfo.FullName);
            Photoshop.ArtLayers layers = psdDocument.ArtLayers;
            for (int index = 0; index < layers.Count; ++index)
            {
                try
                {
                    Photoshop.ArtLayer layer = layers[index + 1];
                    var layName = layer.Name.ToUpper();
                    var findKey = convertMap.Single(x => layName.Contains(x.Key));
                    var color = findKey.Value;

                    switch (layer.LayerType)
                    {
                        case Photoshop.PsLayerType.psArtLayer:
                            {
                                switch (layer.Kind)
                                {
                                    case Photoshop.PsLayerKind.psSolidFillLayer:
                                        {
                                            PhotoshopApplication.ActiveDocument.ActiveLayer = layer;
                                            UpdateSolidFillColor(color);
                                        }
                                        break;
                                    case Photoshop.PsLayerKind.psTextLayer:
                                        {
                                            PhotoshopApplication.ActiveDocument.ActiveLayer = layer;
                                            Photoshop.TextItem text = layer.TextItem;
                                            text.Color.RGB.HexValue = color.R.ToString("X2").ToLower() + color.G.ToString("X2").ToLower() + color.B.ToString("X2").ToLower();
                                            break;
                                        }
                                    default:
                                        {
                                            ConsoleColor oldColor = Console.ForegroundColor;
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Out.WriteLine("Convert Error : Layer Name - " + layName + "   LayerKind - " + layer.Kind);
                                            Console.ForegroundColor = oldColor;
                                            break;
                                        }
                                }
                            }
                            break;
                        default:
                            {
                                throw new Exception();
                            }
                    }
                }
                catch
                {
                }
            }

            psdDocument.SaveAs(saveFolder + "\\" + fileInfo.Name);
            Console.Out.WriteLine("Convert Finished, Save to " + saveFolder + "\\" + fileInfo.Name);
        }

        static private void UpdateSolidFillColor(Color color)
        {
            Photoshop.ActionDescriptor descriptor = new Photoshop.ActionDescriptor();
            Photoshop.ActionReference reference = new Photoshop.ActionReference();
            reference.PutEnumerated(PhotoshopApplication.StringIDToTypeID("contentLayer"), PhotoshopApplication.CharIDToTypeID("Ordn"), PhotoshopApplication.CharIDToTypeID("Trgt"));
            descriptor.PutReference(PhotoshopApplication.CharIDToTypeID("null"), reference);
            Photoshop.ActionDescriptor fillDescriptor = new Photoshop.ActionDescriptor();
            Photoshop.ActionDescriptor colorDescriptor = new Photoshop.ActionDescriptor();
            colorDescriptor.PutDouble(PhotoshopApplication.CharIDToTypeID("Rd  "), color.R);
            colorDescriptor.PutDouble(PhotoshopApplication.CharIDToTypeID("Grn "), color.G);
            colorDescriptor.PutDouble(PhotoshopApplication.CharIDToTypeID("Bl  "), color.B);
            fillDescriptor.PutObject(PhotoshopApplication.CharIDToTypeID("Clr "), PhotoshopApplication.CharIDToTypeID("RGBC"), colorDescriptor);
            descriptor.PutObject(PhotoshopApplication.CharIDToTypeID("T   "), PhotoshopApplication.StringIDToTypeID("solidColorLayer"), fillDescriptor);
            PhotoshopApplication.ExecuteAction(PhotoshopApplication.CharIDToTypeID("setd"), descriptor);
        }

        static private void GetSolidFillColor()
        {
            Photoshop.ActionReference reference = new Photoshop.ActionReference();
            reference.PutEnumerated(PhotoshopApplication.StringIDToTypeID("contentLayer"), PhotoshopApplication.CharIDToTypeID("Ordn"), PhotoshopApplication.CharIDToTypeID("Trgt"));
            Photoshop.ActionDescriptor descriptor = PhotoshopApplication.ExecuteActionGet(reference);
            Photoshop.ActionList actionList = descriptor.GetList(PhotoshopApplication.CharIDToTypeID("Adjs"));
            Photoshop.ActionDescriptor solidColorLayer = actionList.GetObjectValue(0);
            Photoshop.ActionDescriptor color = solidColorLayer.GetObjectValue(PhotoshopApplication.CharIDToTypeID("Clr "));
            double red = color.GetDouble(PhotoshopApplication.CharIDToTypeID("Rd  "));
            double green = color.GetDouble(PhotoshopApplication.CharIDToTypeID("Grn "));
            double blue = color.GetDouble(PhotoshopApplication.CharIDToTypeID("Bl  "));
        }
    }
}
