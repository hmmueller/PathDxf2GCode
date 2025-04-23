namespace de.hmmueller.PathDxf2GCode.Tests;

[TestClass]
public class OptionsTests {
    [TestMethod]
    public void HCreatesNoOptions() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Assert.IsNull(Options.Create(["/h"], messages));
    }

    [TestMethod]
    public void QuestionMarkCreatesNoOptions() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Assert.IsNull(Options.Create(["-?"], messages));
    }

    [TestMethod]
    public void TestInvalidOption() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Assert.IsNull(Options.Create(["/x"], messages));
    }

    [TestMethod]
    public void TestEmptyOption() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Assert.IsNull(Options.Create(["-"], messages));
    }

    [TestMethod]
    public void CheckDoubleParams() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Options? o = Options.Create(["A", "/v1.1", "/f2,2", "/s3.3", "Z"], messages);
        Assert.IsFalse(messages.Errors.Any(), string.Join("\r\n", messages.Errors));
        Assert.AreEqual(1.1, o!.GlobalSweepRate_mmpmin, 1e-4);
        Assert.AreEqual(2.2, o.GlobalFeedRate_mmpmin, 1e-4);
        CollectionAssert.AreEqual(new[] { "A", "Z" }, o.DxfFilePaths.ToArray());
    }

    [TestMethod]
    public void CheckDoubleParamsWithSpace() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Options? o = Options.Create(["-f", "1.1", "-v", "2,2", "/s", "3.3",], messages);
        Assert.IsFalse(messages.Errors.Any(), string.Join("\r\n", messages.Errors));
        Assert.AreEqual(1.1, o!.GlobalFeedRate_mmpmin, 1e-4);
        Assert.AreEqual(2.2, o.GlobalSweepRate_mmpmin, 1e-4);
    }

    [TestMethod]
    public void CheckStringParams() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Options? o = Options.Create(["/dDIR1", "/p([0-9]+)", "/dDIR2", "/f1", "/v1", "/s1"], messages);
        Assert.IsFalse(messages.Errors.Any(), string.Join("\r\n", messages.Errors));
        Assert.AreEqual("([0-9]+)", o!.PathNamePattern);
        CollectionAssert.AreEqual(new[] { "LOCAL", "DIR1", "DIR2" }, o.DirAndSearchDirectories("LOCAL").ToArray());
    }

    [TestMethod]
    public void CheckStringParamsWithSpace() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Options? o = Options.Create(["-d", "DIR1", "-p", "([0-9]+)", "-d", "DIR2", "-f1", "-v1", "/s1"], messages);
        Assert.IsFalse(messages.Errors.Any(), string.Join("\r\n", messages.Errors));
        Assert.AreEqual("([0-9]+)", o!.PathNamePattern);
        CollectionAssert.AreEqual(new[] { "LOCAL", "DIR1", "DIR2" }, o.DirAndSearchDirectories("LOCAL").ToArray());
    }

    [TestMethod]
    public void CheckMissingStringParam() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Assert.IsNull(Options.Create(["/d", "DIR1", "/p"], messages));
    }

    [TestMethod]
    public void COptionSetsCheckFlag() {
        using StringWriter sw = new();
        MessageHandlerForEntities messages = new(sw);
        Assert.IsTrue(Options.Create(["/v1", "/f1", "/s1", "/c"], messages)!.CheckModels);
    }
}
