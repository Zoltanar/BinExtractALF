using System.Reflection;
using BinExtractALF;

namespace BinExtractALFTest
{
    /// <summary>
    /// This class checks that BinExtractALF gets same number of archives and file entries (and same names of files) as exs4alf.
    /// It does not attempt to extract.
    /// </summary>
    [TestClass]
    public class exs4alfTests
    {
        [TestMethod]
        public void ALM_SYS4INI()
        {
            var fileName = "SYS4INI.BIN";
            Test(fileName, 5, 23094);
        }

        [TestMethod]
        public void ALM_APPEND01()
        {
            var fileName = "APPEND01.AAI";
            Test(fileName, 1, 502);
        }

        private void Test(string fileName, int expectedArchives, int expectedFileCount)
        {
            GetResources(fileName, out var fileStream, out var expectedFiles);
            var archivesHeader = Processing.GetFileInformation(fileStream, out var archiveEntries, out var filesHeader, out var fileEntries);
            Assert.AreEqual(expectedArchives, (int)archivesHeader.entry_count, $"Expected {expectedArchives} archives, got {archivesHeader.entry_count}");
            Assert.AreEqual(expectedArchives, archiveEntries.Length, $"Expected {expectedArchives} archives, got {archiveEntries.Length}");
            Assert.AreEqual(expectedFileCount, (int)filesHeader.entry_count, $"Expected {expectedFileCount} files, got {filesHeader.entry_count}");
            Assert.AreEqual(expectedFileCount, fileEntries.Length, $"Expected {expectedFileCount} files, got {fileEntries.Length}");
            var actualFiles = fileEntries.Select(e => $"{Path.GetFileNameWithoutExtension(archiveEntries[e.ArchiveIndex])}\\{e.FileName}").ToArray();
            CollectionAssert.AreEquivalent(expectedFiles, actualFiles,$"Actual files did not match expected files.");
        }

        private void GetResources(string name, out Stream stream, out string[] listOfFiles)
        {
            var fullName = $"{nameof(BinExtractALFTest)}.Files.{name}";
            stream = Assembly.GetAssembly(typeof(exs4alfTests))!.GetManifestResourceStream(fullName)!;
            var lines = new List<string>();
            using var listStream = Assembly.GetAssembly(typeof(exs4alfTests))!.GetManifestResourceStream(fullName + ".dir.txt")!;
            using var reader = new StreamReader(listStream);
            while (reader.ReadLine() is { } line) lines.Add(line);
            listOfFiles = lines.ToArray();
        }
    }
}