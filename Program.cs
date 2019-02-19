using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic; 

delegate bool FilePathFunc(string sourcePath);

namespace zip_project
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 3)
            {
                Console.WriteLine("at least 3 command line arguments " + 
                                  "required: args[0] == tarZipFilePath | " +
                                  "required: args[1] == sourceBaseDirectory | " +
                                  "args[2...] = filesToZipPaths!");
                return;
            }
            string tarZipPath = args[0];
            string sourceBaseDirectory = args[1];

            if(sourceBaseDirectory.Equals("None"))
            {
                sourceBaseDirectory = "";
            }

            var sourcePaths = new List<string>(args);
            sourcePaths.RemoveAt(0);
            sourcePaths.RemoveAt(0);

            bool success = zipFiles(tarZipPath, sourceBaseDirectory, sourcePaths);
            Console.WriteLine("zipping files " + (success ? "was successful" : "failed") + "!");
        }

        static bool zipFiles(string tarZipFilePath, string baseDir, List<string> sourcePaths)
        {
            if(String.IsNullOrEmpty(tarZipFilePath))
            {
                Console.WriteLine("zipFiles -> tarZipFilePath is EMPTY!");
                return false;
            }
            var fi = new FileInfo(tarZipFilePath);
            var di = fi.Directory;
            if(fi.Exists && di.Exists)
            {
                bool replaceTarZipFilePath = aksIfUserWantsToReplaceZipFilePath();
                if(replaceTarZipFilePath)
                {
                    File.Delete(tarZipFilePath);
                }else{
                    tarZipFilePath = askForAlternativeFileName(tarZipFilePath);
                }
                fi = new FileInfo(tarZipFilePath);
            }
            if(String.IsNullOrEmpty(tarZipFilePath) || fi.Exists)
            {
                Console.WriteLine("zipFiles -> tarZipFilePath is EMPTY!");
                return false;
            }else if (fi.Exists)
            {
                Console.WriteLine("zipFiles -> tarZipFilePath does already EXIST!");
                return false;
            }

            bool totalSuccess = true;
            using (FileStream newZipFile = new FileStream(tarZipFilePath, FileMode.CreateNew))
            {
                using (ZipArchive archive = new ZipArchive(newZipFile, ZipArchiveMode.Create))
                {
                    FilePathFunc zipDirFunc = (absSourceFilePath) => {
                        string zipTarPath = genZipTargetPath(absSourceFilePath, baseDir);
                        if( !String.IsNullOrEmpty(zipTarPath) )
                        {
                            totalSuccess = zipFileToArchive(absSourceFilePath, zipTarPath, archive) && totalSuccess;
                        }
                        return false;
                    };
                    foreach(var sp in sourcePaths)
                    {
                        // FileInfo(path) tests if path is a file an if path exists
                        // DirectoryInfo(path) tests if path is a dir and if path exists
                        // => if FileInfo(directoryPath).Exists == False!!! (differs e.g. from Qt::FileInfo, and is actually pretty neat!)
                        if(new DirectoryInfo(sp).Exists)
                        {
                            iterateOverFiles(sp, zipDirFunc);
                        }else if(new FileInfo(sp).Exists)
                        {
                            totalSuccess = zipFileToArchive(sp, genZipTargetPath(sp, baseDir), archive) && totalSuccess;
                        }
                    }
                }
            }
            return totalSuccess;
        }

        static string genZipTargetPath(string absSourcePath, string zipBasePath)
        {
            if(String.IsNullOrEmpty(zipBasePath))
            {
                return Path.GetFileName(absSourcePath);
            }
            
            if(absSourcePath.StartsWith(zipBasePath))
            {
                string zipTarPath = absSourcePath.Substring(zipBasePath.Length);
                return trimPathSeparator(zipTarPath);
            }
            return "";
        }

        static void iterateOverFiles(string dirPath, FilePathFunc f)
        {
            try
            {
                var txtFiles = Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories);

                foreach (string currentFile in txtFiles)
                {
                    f(currentFile);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static bool zipFileToArchive(string sourcePath, string tarZipFilePath, ZipArchive archive)
        {
            try
            {
                using (Stream srcFileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        using (Stream fileStreamInZip = archive.CreateEntry(tarZipFilePath).Open())
                        {
                            srcFileStream.CopyTo(fileStreamInZip);
                            return true;
                        }
                    }catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return false;
        }
        static string trimPathSeparator(string tp)
        {
            if(tp.StartsWith(Path.DirectorySeparatorChar))
            {
                tp = tp.Substring(1);
            }
            if(tp.EndsWith(Path.DirectorySeparatorChar))
            {
                tp = tp.Substring(0,tp.Length-1);
            }
            return tp;
        }

        static string askForAlternativeFileName(string p)
        {
            return p;
        }

        static bool aksIfUserWantsToReplaceZipFilePath()
        {
            return true;
        }
    }
}
