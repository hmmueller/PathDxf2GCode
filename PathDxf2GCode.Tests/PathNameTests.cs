namespace de.hmmueller.PathDxf2GCode.Tests;

[TestClass]
public class PathNameTests {
    const string TEST_FILE_PATTERN = "([0-9]+)(?:[.]([0-9A-Z]+))?";
    const string TEST_PATH_PATTERN = "([0-9]+)[.]([0-9A-Z]+)";

    private static PathName AsPathName(string name) => new(name, "TEST.dxf");

    [TestMethod]
    [DataRow("8002", "8002.0")]
    [DataRow("8002", "8002.1")]
    [DataRow("8002", "8002.A")]
    public void EqualPathNamesWithDifferentGroupCounts(string name1, string name2) {
        Assert.IsTrue(PathName.CompareFileNameToPathName(name1, AsPathName(name2), TEST_FILE_PATTERN, TEST_PATH_PATTERN) == 0);
    }

    [TestMethod]
    [DataRow("FILE 8000.1", "8000.1")]
    [DataRow("FILE 8000", "8000.1")]
    [DataRow("FILE 8000-8002", "8000.0")]
    [DataRow("FILE 8000-8002", "8001.0")]
    [DataRow("FILE 8000-8002", "8002.0")]
    [DataRow("FILE 8000-8002", "8000.1")]
    [DataRow("FILE 8000-8002", "8001.99")]
    [DataRow("FILE 8000-8002", "8002.99")]
    [DataRow("FILE 8000.4-8001,8000.1-8001.3", "8000.2")]
    public void FileNameContainsPathName(string fileName, string pathName) {
        Assert.IsTrue(PathModel.Collection.FileNameMatchesPathName(fileName, AsPathName(pathName), TEST_FILE_PATTERN, TEST_PATH_PATTERN));
    }

    [TestMethod]
    [DataRow("FILE 8000.1", "8000.2")]
    [DataRow("FILE 8000", "8001.1")]
    [DataRow("FILE 8001-8003", "8000.0")]
    [DataRow("FILE 8001-8003", "8000.99")]
    [DataRow("FILE 8000-8002,8004", "8003.0")]
    [DataRow("FILE 8000.2-8000.3,8000.5", "8000.4")]
    [DataRow("FILE 8000.0-8000.3,8000.5,8001", "8000.4")]
    [DataRow("FILE 8000.4-8001,8000.1-8000.2", "8000.3")]
    public void FileNameDoesNotContainPathName(string fileName, string pathName) {
        Assert.IsFalse(PathModel.Collection.FileNameMatchesPathName(fileName, AsPathName(pathName), TEST_FILE_PATTERN, TEST_PATH_PATTERN));
    }
}
