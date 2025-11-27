/*
    GtaImg - A library for reading and manipulating GTA IMG archive files.
    Copyright (c) 2025 Vaibhav Pandey <contact@vaibhavpandey.com>

    Licensed under the MIT License. See LICENSE file in the project root for full license information.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GtaImg;
using NUnit.Framework;

namespace GtaImg.Tests
{
    [TestFixture]
    public class IMGArchiveTests
    {
        /// <summary>
        /// Test data for GTA IMG archives.
        /// Each entry contains: Path, ExpectedVersion, ExpectedMinEntries, Game name
        /// </summary>
        private static readonly object[] ArchiveTestCases = new object[]
        {
            new object[] 
            { 
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto 3\models\gta3.img",
                IMGArchive.IMGVersion.VER1,
                3000,  // Minimum expected entries
                "GTA III"
            },
            new object[] 
            { 
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto Vice City\models\gta3.img",
                IMGArchive.IMGVersion.VER1,
                6000,
                "GTA Vice City"
            },
            new object[] 
            { 
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto San Andreas\models\gta3.img",
                IMGArchive.IMGVersion.VER2,
                16000,
                "GTA San Andreas"
            }
        };

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_ShouldOpen_WithCorrectVersion(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            // Skip if file doesn't exist
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                Assert.That(archive.Version, Is.EqualTo(expectedVersion), string.Format("{0} should be {1}", gameName, expectedVersion));
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_ShouldHaveEntries_AboveMinimum(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                Assert.That(archive.EntryCount, Is.GreaterThanOrEqualTo(minEntries), 
                    string.Format("{0} should have at least {1} entries", gameName, minEntries));
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void GuessVersion_ShouldDetectCorrectly(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            IMGArchive.IMGVersion detectedVersion = IMGArchive.GuessIMGVersion(path);

            Assert.That(detectedVersion, Is.EqualTo(expectedVersion), 
                string.Format("{0} version detection should match", gameName));
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_ShouldEnumerateAllEntries(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                List<IMGEntry> entries = archive.Entries.ToList();

                Assert.That(entries, Has.Count.EqualTo(archive.EntryCount), 
                    string.Format("{0} enumeration count should match EntryCount", gameName));
                
                Assert.That(entries, Has.All.Matches<IMGEntry>(e => !string.IsNullOrEmpty(e.Name)), 
                    "All entries should have non-empty names");
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_EntriesShouldHaveValidOffsets(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                List<IMGEntry> entries = archive.Entries.ToList();

                // Entries should be sorted by offset
                for (int i = 1; i < entries.Count; i++)
                {
                    Assert.That(entries[i].Offset, Is.GreaterThanOrEqualTo(entries[i - 1].Offset),
                        string.Format("Entry at index {0} should have offset >= previous entry", i));
                }

                // All entries should have non-zero size
                Assert.That(entries.Where(e => e.Size > 0).Count(), Is.GreaterThan(entries.Count * 0.99),
                    "At least 99% of entries should have non-zero size");
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_ShouldFindPlayerDff(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                IMGEntry? entry = archive.GetEntryByName("player.dff");

                Assert.That(entry, Is.Not.Null, string.Format("{0} should contain player.dff", gameName));
                Assert.That(entry.Value.Size, Is.GreaterThan(0), "player.dff should have non-zero size");
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_SearchShouldBeCaseInsensitive(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                IMGEntry? lower = archive.GetEntryByName("player.dff");
                IMGEntry? upper = archive.GetEntryByName("PLAYER.DFF");
                IMGEntry? mixed = archive.GetEntryByName("Player.DFF");

                Assert.That(lower, Is.Not.Null, "Should find player.dff (lowercase)");
                Assert.That(upper, Is.Not.Null, "Should find PLAYER.DFF (uppercase)");
                Assert.That(mixed, Is.Not.Null, "Should find Player.DFF (mixed case)");

                Assert.That(lower.Value.Offset, Is.EqualTo(upper.Value.Offset), 
                    "All case variants should return same entry");
                Assert.That(lower.Value.Offset, Is.EqualTo(mixed.Value.Offset), 
                    "All case variants should return same entry");
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_ShouldReturnNullForNonexistentEntry(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                IMGEntry? entry = archive.GetEntryByName("this_file_definitely_does_not_exist_12345.xyz");

                Assert.That(entry, Is.Null, "Non-existent entry should return null");
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_ShouldReadDffWithValidHeader(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                // Find first DFF file
                IMGEntry dffEntry = default(IMGEntry);
                bool found = false;
                foreach (IMGEntry e in archive.Entries)
                {
                    if (e.Name.EndsWith(".dff", StringComparison.OrdinalIgnoreCase) && e.Size > 0)
                    {
                        dffEntry = e;
                        found = true;
                        break;
                    }
                }

                Assume.That(found, "Archive should contain at least one DFF file");

                byte[] data = archive.ReadEntryData(dffEntry);

                Assert.That(data, Is.Not.Null, "Should be able to read entry data");
                Assert.That(data.Length, Is.GreaterThan(0), "Data should not be empty");
                
                // RenderWare DFF magic: 0x10 (clump chunk)
                Assert.That(data[0], Is.EqualTo(0x10), "DFF should start with RenderWare clump magic (0x10)");
                Assert.That(data[1], Is.EqualTo(0x00), "DFF header byte 2");
                Assert.That(data[2], Is.EqualTo(0x00), "DFF header byte 3");
                Assert.That(data[3], Is.EqualTo(0x00), "DFF header byte 4");
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_ShouldReadTxdWithValidHeader(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                // Find first TXD file
                IMGEntry txdEntry = default(IMGEntry);
                bool found = false;
                foreach (IMGEntry e in archive.Entries)
                {
                    if (e.Name.EndsWith(".txd", StringComparison.OrdinalIgnoreCase) && e.Size > 0)
                    {
                        txdEntry = e;
                        found = true;
                        break;
                    }
                }

                Assume.That(found, "Archive should contain at least one TXD file");

                byte[] data = archive.ReadEntryData(txdEntry);

                Assert.That(data, Is.Not.Null, "Should be able to read entry data");
                Assert.That(data.Length, Is.GreaterThan(0), "Data should not be empty");
                
                // RenderWare TXD magic: 0x16 (texture dictionary chunk)
                Assert.That(data[0], Is.EqualTo(0x16), "TXD should start with RenderWare texture dictionary magic (0x16)");
                Assert.That(data[1], Is.EqualTo(0x00), "TXD header byte 2");
                Assert.That(data[2], Is.EqualTo(0x00), "TXD header byte 3");
                Assert.That(data[3], Is.EqualTo(0x00), "TXD header byte 4");
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_OpenEntryShouldReturnBoundedStream(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                IMGEntry? entry = archive.GetEntryByName("player.dff");
                Assume.That(entry, Is.Not.Null);

                using (Stream stream = archive.OpenEntry(entry.Value))
                {
                    Assert.That(stream, Is.Not.Null, "OpenEntry should return a stream");
                    Assert.That(stream.CanRead, Is.True, "Stream should be readable");
                    Assert.That(stream.Length, Is.EqualTo(entry.Value.SizeInBytes), 
                        "Stream length should match entry size");

                    // Read and verify we get the right amount
                    byte[] buffer = new byte[stream.Length];
                    int totalRead = 0;
                    int read;
                    while ((read = stream.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
                    {
                        totalRead += read;
                    }

                    Assert.That(totalRead, Is.EqualTo(entry.Value.SizeInBytes), 
                        "Should read exactly the entry size");
                }
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_ContainsEntryShouldWork(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                Assert.That(archive.ContainsEntry("player.dff"), Is.True, "Should contain player.dff");
                Assert.That(archive.ContainsEntry("PLAYER.DFF"), Is.True, "Should contain PLAYER.DFF (case insensitive)");
                Assert.That(archive.ContainsEntry("nonexistent.xyz"), Is.False, "Should not contain nonexistent file");
            }
        }

        [TestCaseSource(nameof(ArchiveTestCases))]
        public void Archive_SizeShouldBeReasonable(string path, IMGArchive.IMGVersion expectedVersion, int minEntries, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                FileInfo fileInfo = new FileInfo(path);
                long actualFileSize = fileInfo.Length;
                long reportedSize = IMGArchive.BlocksToBytes(archive.Size);

                // For VER1, Size property includes DIR file size estimate, so it can exceed IMG file
                // For VER2, the reported size should match the actual file size closely
                if (expectedVersion == IMGArchive.IMGVersion.VER2)
                {
                    Assert.That(reportedSize, Is.LessThanOrEqualTo(actualFileSize + IMGArchive.BlockSize),
                        "Reported size should not exceed actual file size significantly");
                }
                
                // Size should always be positive and reasonable
                Assert.That(archive.Size, Is.GreaterThan(0), "Archive size should be positive");
                Assert.That(archive.EntryCount, Is.GreaterThan(0), "Archive should have entries");
            }
        }
    }

    [TestFixture]
    public class IMGArchiveFileTypeTests
    {
        private static readonly object[] FileTypeTestCases = new object[]
        {
            new object[] 
            { 
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto 3\models\gta3.img",
                new string[] { ".dff", ".txd" },
                "GTA III"
            },
            new object[] 
            { 
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto Vice City\models\gta3.img",
                new string[] { ".dff", ".txd", ".col", ".ifp" },
                "GTA Vice City"
            },
            new object[] 
            { 
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto San Andreas\models\gta3.img",
                new string[] { ".dff", ".txd", ".col", ".ifp", ".ipl" },
                "GTA San Andreas"
            }
        };

        [TestCaseSource(nameof(FileTypeTestCases))]
        public void Archive_ShouldContainExpectedFileTypes(string path, string[] expectedExtensions, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                HashSet<string> extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (IMGEntry entry in archive.Entries)
                {
                    string ext = Path.GetExtension(entry.Name);
                    if (!string.IsNullOrEmpty(ext))
                    {
                        extensions.Add(ext.ToLowerInvariant());
                    }
                }

                foreach (string expectedExt in expectedExtensions)
                {
                    Assert.That(extensions.Contains(expectedExt), Is.True, 
                        string.Format("{0} should contain {1} files", gameName, expectedExt));
                }
            }
        }
    }

    [TestFixture]
    public class IMGArchiveVehicleTests
    {
        /// <summary>
        /// Test for game-specific vehicles
        /// </summary>
        private static readonly object[] VehicleTestCases = new object[]
        {
            new object[] 
            { 
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto 3\models\gta3.img",
                new string[] { "kuruma.dff", "taxi.dff", "police.dff", "infernus.dff" },
                new string[] { "admiral.dff" }, // Admiral is VC/SA only
                "GTA III"
            },
            new object[] 
            { 
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto Vice City\models\gta3.img",
                new string[] { "admiral.dff", "taxi.dff", "police.dff", "infernus.dff" },
                new string[0],
                "GTA Vice City"
            },
            new object[] 
            { 
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto San Andreas\models\gta3.img",
                new string[] { "admiral.dff", "bullet.dff", "hydra.dff" },
                new string[0],
                "GTA San Andreas"
            }
        };

        [TestCaseSource(nameof(VehicleTestCases))]
        public void Archive_ShouldContainExpectedVehicles(string path, string[] expectedVehicles, string[] unexpectedVehicles, string gameName)
        {
            Assume.That(File.Exists(path), string.Format("Archive not found: {0}", path));

            using (IMGArchive archive = new IMGArchive(path))
            {
                foreach (string vehicle in expectedVehicles)
                {
                    Assert.That(archive.ContainsEntry(vehicle), Is.True, 
                        string.Format("{0} should contain {1}", gameName, vehicle));
                }

                foreach (string vehicle in unexpectedVehicles)
                {
                    Assert.That(archive.ContainsEntry(vehicle), Is.False, 
                        string.Format("{0} should NOT contain {1}", gameName, vehicle));
                }
            }
        }
    }
}
