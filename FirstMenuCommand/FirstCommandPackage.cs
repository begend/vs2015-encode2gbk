//------------------------------------------------------------------------------
// <copyright file="FirstCommandPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

using EnvDTE;
using EnvDTE80;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;

namespace FirstMenuCommand
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    //[ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(FirstCommandPackage.PackageGuidString)]
    //[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class FirstCommandPackage : Package
    {
        /// <summary>
        /// FirstCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "f8564b3a-f371-4722-b5e2-945cd29589c1";

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstCommand"/> class.
        /// </summary>
        public FirstCommandPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        private string srcRootPath;
        private string destRootPath;

        private DocumentEvents documentEvents;


        protected override void Initialize()
        {
            Debug.WriteLine("begin in my initial");
            //FirstCommand.Initialize(this);
            base.Initialize();

            Debug.WriteLine("mid in my initial");
            var dte = GetService(typeof(DTE)) as DTE2;

            documentEvents = dte.Events.DocumentEvents; //must not be local variable
            documentEvents.DocumentSaved += documentEvents_DocumentSaved_Gbk;

            parseXml();
            Debug.WriteLine("end in my initial");
        }

        void parseXml()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("c:\\vsConf.xml");
            XmlNode root = xmlDoc.SelectSingleNode("info");
            XmlNodeList nodeList = root.ChildNodes;
            foreach (XmlNode xn in nodeList)
            {
                if (xn.Name.Equals("from"))
                {
                    this.srcRootPath = xn.InnerText;
                }

                if (xn.Name.Equals("to"))
                {
                    this.destRootPath = xn.InnerText;
                }
            }
        }

        void documentEvents_DocumentSaved(Document document)
        {
            Debug.WriteLine("in my save");

            var srcPath = document.FullName;
            var fileName = document.Name;
            var path = document.Path;

            try
            {
                File.WriteAllLines(srcPath, File.ReadAllLines(srcPath, Encoding.GetEncoding("utf-8")),
                Encoding.GetEncoding("gb18030"));

                File.WriteAllLines(srcPath + "gbk", File.ReadAllLines(srcPath, Encoding.GetEncoding("utf-8")),
                                Encoding.GetEncoding("gb18030"));
            }
            catch (DecoderFallbackException)
            {
                File.WriteAllLines(srcPath + "exp.gbk", File.ReadAllLines(srcPath, Encoding.GetEncoding("utf-8")),
                    Encoding.GetEncoding("gb18030"));
                File.WriteAllLines(srcPath + "exp.utf", File.ReadAllLines(srcPath, Encoding.GetEncoding("gb18030")),
                    Encoding.GetEncoding("utf-8"));
            }
;
        }

        void documentEvents_DocumentSaved_Gbk(Document document)
        {
            if (document.Kind != "{8E7B96A8-E33D-11D0-A6D5-00C04FB67F6A}")
            {
                // then it's not a text file
                return;
            }

            var path = document.FullName;

            var stream = new FileStream(path, FileMode.Open);
            var reader = new StreamReader(stream, Encoding.GetEncoding("utf-8"), true);
            reader.Read();

            if (reader.CurrentEncoding == Encoding.GetEncoding("gb2312"))
            {
                stream.Close();
                return;
            }

            string text;

            try
            {
                stream.Position = 0;
                reader = new StreamReader(stream, Encoding.GetEncoding("utf-8"));
                text = reader.ReadToEnd();
                stream.Close();

                string url = "http://127.0.0.1:12345/hello";
                //HttpPost(url, text);

                File.WriteAllText(path, text, Encoding.GetEncoding("gb2312"));
            }
            catch (DecoderFallbackException)
            {
                stream.Position = 0;
                reader = new StreamReader(stream, Encoding.Default);
                text = reader.ReadToEnd();
                stream.Close();
                File.WriteAllText(path, text, Encoding.GetEncoding("gb2312"));
            }


        }

        string HttpPost(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = Encoding.UTF8.GetByteCount(postDataStr);
            Stream myRequestStream = request.GetRequestStream();
            StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
            myStreamWriter.Write(postDataStr);
            myStreamWriter.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("gb2312"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        #endregion
    }


}
