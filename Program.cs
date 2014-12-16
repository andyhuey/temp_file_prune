/*
 * temp_file_prune/Program.cs
 * This program will prune old temp files from a directory tree, given input parameters.
 * Uses DotNetZip (http://dotnetzip.codeplex.com)
 * 
 * ajh 2012-10-26: migrate to .Net 3.5 & add zipping capability
 * ajh 2006-02-01
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zip;

namespace temp_file_prune
{
    class Program
    {

        static string sRootDir;
        static int iDaysToKeep;
        static string sParamFile = @"prune_params.txt";
        static ArrayList sDirList;
        static int iTotDirs;
        static bool bDelFiles;
        static bool bZipFiles;

        static void usage()
        {
            System.Console.WriteLine("Usage:");
            System.Console.WriteLine("temp_file_prune /t - test run");
            System.Console.WriteLine("temp_file_prune /y - delete files");
            System.Console.WriteLine("temp_file_prune /z - zip files");
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                usage();
                return;
            }

            string sRunType = args[0];
            switch (sRunType)
            {
                case "/t":
                    bDelFiles = false;
                    bZipFiles = false;
                    break;
                case "/y":
                    bDelFiles = true;
                    bZipFiles = false;
                    break;
                case "/z":
                    bDelFiles = true;
                    bZipFiles = true;
                    break;
                default:
                    usage();
                    return;
            }

            sDirList = new ArrayList();
            int iTotLines = 0;
            iTotDirs = 0;

            // read the param file
            try
            {
                // The using statement also closes the StreamReader.
                using (StreamReader sr = new StreamReader(sParamFile))
                {
                    string sLine;
                    while ((sLine = sr.ReadLine()) != null)
                    {
                        // skip blank lines
                        if (sLine.Trim().Length < 1) continue;
                        // skip comments
                        if (sLine[0] == '#') continue;
                        // first line is root dir
                        switch (iTotLines)
                        {
                            case 0:
                                sRootDir = sLine;
                                break;
                            case 1:
                                iDaysToKeep = Int32.Parse(sLine);
                                break;
                            default:
                                sDirList.Add(sLine);
                                iTotDirs++;
                                break;
                        } // switch
                        iTotLines++;
                    } // while
                } // using
            } // try
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The parameter file could not be read: ");
                Console.WriteLine(e.Message);
            } // catch

            Console.WriteLine(string.Format("Root directory: {0}", sRootDir));
            Console.WriteLine(string.Format("Days to keep: {0}", iDaysToKeep));
            Console.WriteLine(string.Format("Number of directories: {0}", iTotDirs));

            foreach (string s in sDirList)
                ProcessDir(sRootDir + s);

            //Console.Write("Press enter:");
            //Console.ReadLine(); // for debugging

        } // Main

        static void ProcessDir(string sDirSpec)
        {
            string sDir, sFileSpec;
            int iSplit = sDirSpec.LastIndexOf(@"\");
            sDir = sDirSpec.Substring(0, iSplit);
            sFileSpec = sDirSpec.Substring(iSplit + 1);
            Console.WriteLine(
                string.Format("Processing directory [{0}] spec [{1}]...", sDir, sFileSpec));

            try
            {
                DateTime cutoffDate = DateTime.Now.AddDays(-iDaysToKeep);
                string[] sFiles = Directory.GetFiles(sDir, sFileSpec);

                if (bZipFiles)
                {
                    string zipFileName = Path.Combine(sDir, string.Format("pruned-{0:yyyy-MM-dd}.zip", DateTime.Today));
                    ZipFile zf = null;
                    if (File.Exists(zipFileName))
                    {
                        Console.WriteLine("Updating zip file {0}", Path.GetFileName(zipFileName));
                        zf = ZipFile.Read(zipFileName);
                    }
                    else
                    {
                        Console.WriteLine("Creating zip file {0}", Path.GetFileName(zipFileName));
                        zf = new ZipFile(zipFileName);
                    }
                    using (zf)
                    {
                        foreach (string sMyFile in sFiles)
                        {
                            FileInfo fi = new FileInfo(sMyFile);
                            if (DateTime.Compare(fi.LastWriteTime, cutoffDate) < 0)
                            {
                                if (zf[fi.Name] != null)
                                    zf.UpdateFile(fi.FullName, ".");
                                else
                                    zf.AddFile(fi.FullName, ".");
                            }
                        }
                        if (zf.Count > 0)
                        {
                            zf.Save();  //zipFileName);
                            Console.WriteLine("Saved {0} files to zip file {1}", zf.Count, Path.GetFileName(zipFileName));
                        }
                    }
                }

                foreach (string sMyFile in sFiles)
                {
                    FileInfo fi = new FileInfo(sMyFile);
                    if (DateTime.Compare(fi.LastWriteTime, cutoffDate) < 0)
                    {
                        if (bDelFiles)
                            fi.Delete();
                        Console.WriteLine(string.Format("\tDelete file: {0}", sMyFile));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }

        }
    }
}
