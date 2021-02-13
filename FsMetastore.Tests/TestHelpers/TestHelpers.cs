using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Primitives;
using FsMetastore.Persistence.IO.Change;
using FsMetastore.Persistence.IOC;
using FsMetastore.Persistence.Zipper.Model;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FsMetastore.Tests.TestHelpers
{
    public static class TestHelpers
    {
        public static void MatchPathCaseSensitive(this StringAssertions assertions,
            IZipperItem expected,
            string because = "",
            params object[] becauseArgs)
        {
            if (expected is FileZipperItem file)
            {
                var comparison = PathComparator.CompareFilePaths(assertions.Subject, file.FullPath, StringComparer.Ordinal);
                comparison.Should().Be(ComparisonResult.Same, $"Expected {file.FullPath} but was {assertions.Subject}");
            }
            else if (expected is DirectoryZipperItem dir)
            {
                var comparison = PathComparator.CompareFilePaths(assertions.Subject, dir.FullPath, StringComparer.Ordinal);
                comparison.Should().Be(ComparisonResult.Same, $"Expected {dir.FullPath} but was {assertions.Subject}");
            }
            else if (expected is DriveZipperItem drive)
            {
                var comparison = PathComparator.CompareFilePaths(assertions.Subject, drive.DriveInfo.Name.TrimEnd('\\', '/'), StringComparer.Ordinal);
                comparison.Should().Be(ComparisonResult.Same, $"Expected {drive.DriveInfo.Name} but was {assertions.Subject}");
            }
            else
            {
                Assert.False(true, $"unknown zipper type {expected}");
            }
        }
        
        public static IServiceProvider Create(Action<ServiceCollection> serviceOverrideAction = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceOverrideAction?.Invoke(serviceCollection);

            serviceCollection.AddFsMetastore();
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider;
        }

        private static DirectoryInfo _solutionFolder = null;
        /// <summary>
        /// Assuming that we are running within the source directory somewhere, search the parent folders until
        /// the sln file is found, return the path to folder.
        /// parent folder
        /// 
        /// </summary>
        /// <param name="path"></param>
        private static DirectoryInfo FindSolutionFolder()
        {
            if (_solutionFolder != null)
            {
                return _solutionFolder;
            }

            var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            while (currentDirectory?.Exists == true)
            {
                if (currentDirectory.GetFiles("*.sln").Any())
                {
                    _solutionFolder = currentDirectory;
                    return _solutionFolder;
                }
                currentDirectory = currentDirectory.Parent;
            }

            return null;
        }
        
        public static string GetSolutionFolderToScan()
        {
            return FindSolutionFolder()?.FullName;
        }

        public static string RecreateTestSubFolder(string subFolderName)
        {
            var subFolderPath = GetTestSubFolder(subFolderName);
            RecreateFolder(subFolderPath);
            return subFolderPath;
        }

        public static string GetTestSubFolder(string subFolderName)
        {
            var testOutputFolder = FindTestOutputFolder();
            var subFolderPath = Path.Combine(testOutputFolder, subFolderName);
            return subFolderPath;
        }


        public static long GetDirectorySize(string directoryPath)
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            if (!dirInfo.Exists)
            {
                return -1;
            }

            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(a => a.Length);
        }

        private static string FindTestOutputFolder()
        {
            var solutionFolder = FindSolutionFolder();
            if (solutionFolder == null)
            {
                return null;
            }

            var testOutputFolder = Path.Combine(solutionFolder.FullName, ".testoutput");
            if (!Directory.Exists(testOutputFolder))
            {
                Directory.CreateDirectory(testOutputFolder);
            }

            return testOutputFolder;
        }

        public static void RemoveFileIfExists(string path)
        {
            if(File.Exists(path))
            {
                File.Delete(path);
            }
        }
        
        public static void RemoveFolderIfExists(string path)
        {
            if(Directory.Exists(path))
            {
                Directory.Delete(path);
            }
        }
        
        public static void RecreateFolder(string batchRoot)
        {
            if(Directory.Exists(batchRoot))
            {
                Directory.Delete(batchRoot, true);
            }
            Directory.CreateDirectory(batchRoot);
        }

        
    }
}
