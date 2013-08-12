using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WixBuilder
{
    /// <summary>
    ///     WiX Content Builder
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args == null)
            {
                return -1;
            }

            if (args.Length < 3)
            {
                return -2;
            }

            List<string> arguments = new List<string>(args);

            string wxsFile = arguments[0];
            string realFolder = arguments[1];
            string wixFolder = arguments[2];
            Uri wixFileFolder = new Uri(Path.GetDirectoryName(wxsFile) + Path.DirectorySeparatorChar);

            Console.WriteLine("Updating WXS file '{0}' at folder '{1}' from '{2}'", wxsFile, wixFolder, realFolder);

            XDocument wxsDocument = XDocument.Load(wxsFile);
            DirectoryInfo realDirectory = new DirectoryInfo(realFolder);
            UniqueCollection<Guid> guids = new UniqueCollection<Guid>();

            if (arguments[3] == "-repalceProduct")
            {
                XAttribute xProductId = wxsDocument.Descendants(XName.Get("Product", wxsDocument.Root.Name.Namespace.NamespaceName)).First().Attribute("Id");
                guids.Add(new Guid(xProductId.Value));
                xProductId.Value = GenerateGuid(guids).ToString().ToUpper();
                arguments.RemoveAt(3);
            }

            XElement pfFiles = wxsDocument.Descendants(XName.Get("Directory", wxsDocument.Root.Name.Namespace.NamespaceName)).First((xElement) =>
                {
                    XAttribute xId = xElement.Attribute("Id");
                    if (xId != null)
                    {
                        return xId.Value == "ProgramFilesFolder";
                    }
                    return false;
                });

            foreach (Guid guid in wxsDocument.Descendants(XName.Get("Component", wxsDocument.Root.Name.Namespace.NamespaceName)).Where((xElement) =>
                {
                    return xElement.Attribute("Guid") != null;
                }).Select((xElement) =>
                {
                    return new Guid(xElement.Attribute("Guid").Value);
                }))
            {
                guids.Add(guid);
            }

            XElement realElement = pfFiles;
            foreach (string filePart in wixFolder.Split('\\'))
            {
                XElement temp = realElement.Elements(XName.Get("Directory", wxsDocument.Root.Name.Namespace.NamespaceName)).FirstOrDefault((xElement) =>
                    {
                        XAttribute xName = xElement.Attribute("Name");
                        if (xName != null)
                        {
                            return xName.Value == filePart;
                        }
                        return false;
                    });
                if (temp == null)
                {
                    temp = new XElement(XName.Get("Directory", wxsDocument.Root.Name.Namespace.NamespaceName), 
                        new XAttribute("Name", filePart), 
                        new XAttribute("Id", filePart.ToUpperInvariant().Replace(' ', '_')));
                    realElement.Add(temp);
                }
                realElement = temp;
            }

            UniqueCollection<string> componentIds = new UniqueCollection<string>();
            foreach (string arg in arguments.Skip(3))
            {
                componentIds.Add(arg);
            }
            ProcessFiles(realElement, realDirectory, guids, componentIds, wixFileFolder);

            XElement xFeature = wxsDocument.Descendants(XName.Get("Feature", wxsDocument.Root.Name.Namespace.NamespaceName)).First();
            xFeature.RemoveNodes();
            foreach (string componentId in componentIds)
            {
                xFeature.Add(new XElement(XName.Get("ComponentRef", wxsDocument.Root.Name.Namespace.NamespaceName), new XAttribute("Id", componentId)));
            }

            wxsDocument.Save(wxsFile);

            Console.WriteLine("Done updating WXS file '{0}' at folder '{1}' from '{2}'", wxsFile, wixFolder, realFolder);

            return 0;
        }

        private static void ProcessFiles(XElement parentElement, DirectoryInfo parentDirectory, UniqueCollection<Guid> guids, UniqueCollection<string> componentIds, Uri wixFileFolder)
        {
            List<XElement> xComponents = new List<XElement>();
            foreach (FileInfo fileInfo in parentDirectory.GetFiles())
            {
                XElement xComponent = parentElement.Elements(XName.Get("Component", parentElement.Document.Root.Name.Namespace.NamespaceName)).FirstOrDefault((component) =>
                    {
                        return component.Element(XName.Get("File", parentElement.Document.Root.Name.Namespace.NamespaceName)).Attribute("Source").Value == GetRelativePath(wixFileFolder, fileInfo);
                    });
                if (xComponent == null)
                {
                    string componentId = GenerateId(componentIds, string.Format(CultureInfo.InvariantCulture, "{0}_{1}Component", parentDirectory.Name, fileInfo.Name));
                    Guid guid = GenerateGuid(guids);
                    xComponent = new XElement(XName.Get("Component", parentElement.Document.Root.Name.Namespace.NamespaceName),
                        new XAttribute("DiskId", "1"),
                        new XAttribute("Id", componentId),
                        new XAttribute("Guid", guid));
                    parentElement.Add(xComponent);
                    xComponent.Add(new XElement(XName.Get("File", parentElement.Document.Root.Name.Namespace.NamespaceName), new XAttribute("Id", fileInfo.Name.ToUpperInvariant()), new XAttribute("Name", fileInfo.Name), new XAttribute("Source", GetRelativePath(wixFileFolder, fileInfo))));
                }
                else
                {
                    componentIds.Add(xComponent.Attribute("Id").Value);
                }
                xComponents.Add(xComponent);
            }
            foreach (XElement xComponent in parentElement.Elements(XName.Get("Component", parentElement.Document.Root.Name.Namespace.NamespaceName)))
            {
                if (xComponents.Contains(xComponent))
                {
                    continue;
                }
                xComponent.Remove();
            }

            foreach (DirectoryInfo directory in parentDirectory.GetDirectories())
            {
                XElement xDirectory = parentElement.Elements(XName.Get("Directory", parentElement.Document.Root.Name.Namespace.NamespaceName)).FirstOrDefault((xElement) =>
                {
                    XAttribute xName = xElement.Attribute("Name");
                    if (xName != null)
                    {
                        return xName.Value == directory.Name;
                    }
                    return false;
                });
                if (xDirectory == null)
                {
                    xDirectory = new XElement(XName.Get("Directory", parentElement.Document.Root.Name.Namespace.NamespaceName),
                        new XAttribute("Name", directory.Name),
                        new XAttribute("Id", directory.Name.ToUpperInvariant().Replace(' ', '_')));
                    parentElement.Add(xDirectory);
                }
                ProcessFiles(xDirectory, directory, guids, componentIds, wixFileFolder);
            }
        }

        private static string GetRelativePath(Uri wixFileFolder, FileInfo fileInfo)
        {
            Uri fileUri = new Uri(fileInfo.FullName);
            Uri relativePath = wixFileFolder.MakeRelativeUri(fileUri);
            return Uri.UnescapeDataString(relativePath.ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private static string GenerateId(UniqueCollection<string> componentIds, string componentBaseId)
        {
            string componentId = componentBaseId.ToUpperInvariant();
            for (int componentIndex = 1; componentIds.Has(componentId); componentIndex++)
            {
                componentId = componentBaseId + componentIndex.ToString(CultureInfo.InvariantCulture);
            }
            componentIds.Add(componentId);
            return componentId;
        }

        private static Guid GenerateGuid(UniqueCollection<Guid> guids)
        {
            Random random = new Random();
            Guid guid = Guid.Empty;
            do
            {
                byte[] bytes = new byte[16];
                random.NextBytes(bytes);
                guid = new Guid(bytes);
            } while (guids.Has(guid));
            guids.Add(guid);
            return guid;
        }
    }
}
