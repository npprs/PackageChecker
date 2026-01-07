using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DependencyCheckerTests
{
    public class ValidateManifestStructureTests
    {
        private string LoadTestJson(
          string filename,
          [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = ""
        )
        {
            string testFileDir = Path.GetDirectoryName(sourceFilePath)!;
            string testDataPath = Path.Combine(testFileDir, "TestData", filename);
            return File.ReadAllText(testDataPath);
        }

        [Test]
        public void Given_ValidManifest_Should_PassValidation()
        {
            // Given
            string json = LoadTestJson("valid_manifest.json");
            var manifest = JsonConvert.DeserializeObject<NoppersDependencyChecker.ManifestData>(json);

            // When
            bool result = NoppersDependencyChecker.ValidateManifestStructure(manifest);

            // Then
            Assert.IsTrue(result);
        }

        [Test]
        public void Given_EmptyLockedField_Should_FailValidation()
        {
            // Given
            string json = LoadTestJson("empty_locked.json");
            var manifest = JsonConvert.DeserializeObject<NoppersDependencyChecker.ManifestData>(json);

            // When
            bool result = NoppersDependencyChecker.ValidateManifestStructure(manifest);

            // Then
            Assert.IsFalse(result);
        }

        [Test]
        public void Given_MissingLockedField_Should_FailValidation()
        {
            // Given
            string json = LoadTestJson("missing_locked.json");
            var manifest = JsonConvert.DeserializeObject<NoppersDependencyChecker.ManifestData>(json);

            // When
            bool result = NoppersDependencyChecker.ValidateManifestStructure(manifest);

            // Then
            Assert.IsFalse(result);
        }

        [Test]
        public void Given_NullVersion_Should_FailValidation()
        {
            // Given
            string json = LoadTestJson("null_version.json");
            var manifest = JsonConvert.DeserializeObject<NoppersDependencyChecker.ManifestData>(json);

            // When
            bool result = NoppersDependencyChecker.ValidateManifestStructure(manifest);

            // Then
            Assert.IsFalse(result);
        }
    }

    public class IsVersionGreaterTests
    {
        [Test]
        public void Given_Poi_CurrentIsGreaterThanNew_Should_ReturnTrue()
        {
            // Given
            string currentVersion = "9.3.63";
            string newVersion = "8.1.166";

            // When
            bool result = NoppersDependencyChecker.IsVersionGreater(currentVersion, newVersion);

            // Then
            Assert.IsTrue(result);
        }

        [Test]
        public void Given_Poi_CurrentIsLessThanNew_Should_ReturnFalse()
        {
            // Given
            string currentVersion = "8.1.166";
            string newVersion = "9.3.63";

            // When
            bool result = NoppersDependencyChecker.IsVersionGreater(currentVersion, newVersion);

            // Then
            Assert.IsFalse(result);
        }

        [Test]
        public void Given_VRCF_CurrentIsGreaterThanNew_Should_ReturnTrue()
        {
            // Given
            string currentVersion = "1.1278.0";
            string newVersion = "1.1271.0";

            // When
            bool result = NoppersDependencyChecker.IsVersionGreater(currentVersion, newVersion);

            // Then
            Assert.IsTrue(result);
        }

        [Test]
        public void Given_VRCF_CurrentIsLessThanNew_Should_ReturnFalse()
        {
            // Given
            string currentVersion = "1.1271.0";
            string newVersion = "1.1278.0";

            // When
            bool result = NoppersDependencyChecker.IsVersionGreater(currentVersion, newVersion);

            // Then
            Assert.IsFalse(result);
        }

        [Test]
        public void Given_MA_CurrentIsGreaterThanNew_Should_ReturnTrue()
        {
            // Given
            string currentVersion = "1.14.4-beta.2";
            string newVersion = "1.14.4-beta.1";

            // When
            bool result = NoppersDependencyChecker.IsVersionGreater(currentVersion, newVersion);

            // Then
            Assert.IsTrue(result);
        }

        [Test]
        public void Given_MA_CurrentIsLessThanNew_Should_ReturnFalse()
        {
            // Given
            string currentVersion = "1.14.4-beta.1";
            string newVersion = "1.14.4-beta.2";

            // When
            bool result = NoppersDependencyChecker.IsVersionGreater(currentVersion, newVersion);

            // Then
            Assert.IsFalse(result);
        }

        [Test]
        public void Given_VersionsAreEqual_Should_ReturnFalse()
        {
            // Given
            string currentVersion = "1.0.0";
            string newVersion = "1.0.0";

            // When
            bool result = NoppersDependencyChecker.IsVersionGreater(currentVersion, newVersion);

            // Then
            Assert.IsFalse(result);
        }
    }

    public class GetLocksDirectoryTests
    {
        [Test]
        public void Should_ReturnValidLocksPath()
        {
            // When
            string result = NoppersDependencyChecker.GetLocksDirectory();

            // Then
            Assert.IsTrue(result.EndsWith("Locks"));
        }
    }

    public class GetLockFilesTests
    {
        private string GetTestLocksDir([System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
        {
            string testFileDir = Path.GetDirectoryName(sourceFilePath)!;
            return Path.Combine(testFileDir, "TestData", "TestLocksDir");
        }

        private string GetTestLocksDirEmpty([System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
        {
            string testFileDir = Path.GetDirectoryName(sourceFilePath)!;
            return Path.Combine(testFileDir, "TestData", "TestLocksDir_Empty");
        }

        private string GetTestLocksDirInvalidManifests([System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
        {
            string testFileDir = Path.GetDirectoryName(sourceFilePath)!;
            return Path.Combine(testFileDir, "TestData", "TestLocksDir_InvalidManifests");
        }

        [Test]
        public void Given_ValidLockFiles_Should_ReturnValidManifests()
        {
            // Given
            string testLocksDir = GetTestLocksDir();

            // When
            var result = NoppersDependencyChecker.GetLockFiles(testLocksDir, NoppersDependencyChecker.GetManifest, NoppersDependencyChecker.ValidateManifestStructure);

            // Then
            Assert.IsTrue(result.Count == 3);
        }

        [Test]
        public void Given_NoLockFiles_Should_ReturnEmptyList()
        {
            // Given
            string testLocksDir = GetTestLocksDirEmpty();

            // When
            var result = NoppersDependencyChecker.GetLockFiles(testLocksDir, NoppersDependencyChecker.GetManifest, NoppersDependencyChecker.ValidateManifestStructure);

            // Then
            Assert.IsTrue(result.Count == 0);
        }

        [Test]
        public void Given_InvalidLockFiles_Should_ReturnEmptyList()
        {
            // Given
            string testLocksDir = GetTestLocksDirInvalidManifests();

            // When
            var result = NoppersDependencyChecker.GetLockFiles(testLocksDir, NoppersDependencyChecker.GetManifest, NoppersDependencyChecker.ValidateManifestStructure);

            // Then
            Assert.IsTrue(result.Count == 1);
        }
    }

    public class MergeLockFilesTests
    {
        private string GetTestLocksDirMerge([System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
        {
            string testFileDir = Path.GetDirectoryName(sourceFilePath);
            return Path.Combine(testFileDir, "TestData", "TestLocksDir_Merge");
        }

        [Test]
        public void Given_MultipleLockFiles_Should_MergeDependencies()
        {
            // Given
            string testLocksDir = GetTestLocksDirMerge();
            var lockFiles = NoppersDependencyChecker.GetLockFiles(testLocksDir, NoppersDependencyChecker.GetManifest, NoppersDependencyChecker.ValidateManifestStructure);

            // When
            var result = NoppersDependencyChecker.MergeLockFiles(lockFiles, NoppersDependencyChecker.IsVersionGreater);

            // Then
            Assert.AreEqual("1.1271.0", result["com.vrcfury.vrcfury"]);
            Assert.AreEqual("3.12.7", result["d4rkpl4y3r.d4rkavataroptimizer"]);
            Assert.AreEqual("1.8.6", result["gogoloco"]);
        }
    }

    public class GetManifestTests
    {
        private string GetTestDataPath(
          string filename,
          [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = ""
        )
        {
            string testFileDir = Path.GetDirectoryName(sourceFilePath)!;
            return Path.Combine(testFileDir, "TestData", filename);
        }

        [Test]
        public void Given_FileDoesNotExist_Should_ReturnNull()
        {
            // Given
            string nonExistentPath = "path/to/nonexistent/file.json";

            // When
            string result = NoppersDependencyChecker.GetManifest(nonExistentPath);

            // Then
            Assert.IsNull(result);
        }

        [Test]
        public void Given_FileExists_Should_ReturnFileContent()
        {
            // Given
            string testFilePath = GetTestDataPath("valid_manifest.json");

            // When
            string result = NoppersDependencyChecker.GetManifest(testFilePath);

            // Then
            Assert.IsTrue(result.Contains("locked"));
        }
    }

    public class ComposeLockFileTests
    {
        [Test]
        public void Given_Selection_Should_ReturnOnlySelected()
        {
            // Given
            var currentPackages = new Dictionary<string, NoppersDependencyChecker.PackageInfo>
            {
                { "package1", new NoppersDependencyChecker.PackageInfo { version = "1.2.3" } },
                { "package2", new NoppersDependencyChecker.PackageInfo { version = "2.3.4" } },
                { "package3", new NoppersDependencyChecker.PackageInfo { version = "5.6.7" } }
            };

            var packageSelection = new Dictionary<string, bool>
            {
                { "package1", true },
                { "package2", false },
                { "package3", true }
            };

            // When
            var result = NoppersDependencyChecker.ComposeLockFile(currentPackages, packageSelection);

            // Then
            Assert.AreEqual("1.2.3", result.locked["package1"].version);
            Assert.AreEqual("5.6.7", result.locked["package3"].version);
        }

        [Test]
        public void Given_NoSelection_Should_ReturnEmpty()
        {
            // Given
            var currentPackages = new Dictionary<string, NoppersDependencyChecker.PackageInfo>
            {
                { "package1", new NoppersDependencyChecker.PackageInfo { version = "1.2.3" } }
            };

            var packageSelection = new Dictionary<string, bool>
            {
                { "package1", false }
            };

            // When
            var result = NoppersDependencyChecker.ComposeLockFile(currentPackages, packageSelection);

            // Then
            Assert.AreEqual(0, result.locked.Count);
        }
    }

    public class UpdateManifestTests
    {
        private string GetTestDataPath(
            string filename,
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = ""
        )
        {
            string testFileDir = Path.GetDirectoryName(sourceFilePath);
            return Path.Combine(testFileDir, "TestData", "TestUpdateManifest", filename);
        }

        [Test]
        public void Given_ValidManifestAndUpdates_Should_UpdateVersions()
        {
            // Given
            string manifestPath = GetTestDataPath("vpm-manifest.json");
            string lockPath = GetTestDataPath("test.lock.json");
            string expectedManifestPath = GetTestDataPath("vpm-manifest-results.json");

            // Get manifest from both files
            string manifestJson = NoppersDependencyChecker.GetManifest(manifestPath);
            string lockJson = NoppersDependencyChecker.GetManifest(lockPath);
            string expectedManifestJson = NoppersDependencyChecker.GetManifest(expectedManifestPath);

            var lockData = JsonConvert.DeserializeObject<NoppersDependencyChecker.ManifestData>(lockJson);

            // Compare lock file to get updates
            var mergedRequirements = NoppersDependencyChecker.MergeLockFiles(
                new List<NoppersDependencyChecker.ManifestData> { lockData },
                NoppersDependencyChecker.IsVersionGreater
            );

            var issues = NoppersDependencyChecker.CompareManifestsWithMerged(
                mergedRequirements,
                manifestJson
            );

            // Mock write function to capture the result
            string capturedContent = null;
            bool writeManifest(string path, string content)
            {
                capturedContent = content;
                return true;
            }

            // When
            bool result = NoppersDependencyChecker.UpdateManifest(
                manifestPath,
                issues,
                NoppersDependencyChecker.GetManifest,
                writeManifest
            );

            // Then
            Assert.AreEqual(expectedManifestJson, capturedContent);
        }
    }
}