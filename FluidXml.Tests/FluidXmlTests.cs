using System.Xml.Linq;
using System.Xml;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XmlDiffLib;

namespace FluidXml.Tests
{
  [TestClass]
  public class FluidXmlTests
  {
    [TestMethod]
    public void InsertXmlTest()
    {
      string expected = "<Root><Item1>1</Item1><InsertBefore>before</InsertBefore><Item2 id=\"test\">2</Item2><Item3>3</Item3><Item4><SubItem1>s1</SubItem1><SubItem2>s2</SubItem2></Item4></Root>";
      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.Add(
        xmlDoc.NewElement("Root")
              .Insert(xmlDoc.NewElement("Item4", xmlDoc.NewElement("SubItem1", "s1"),
                                                 xmlDoc.NewElement("SubItem2", "s2")))
              .Insert("Item3", "3")
              .Insert("Item2", new[] { "id", "test" }, "2")
              .Insert("Item1", "1")
              .InsertBefore("Item2", xmlDoc.NewElement("InsertBefore", "before")));
      Assert.AreEqual(expected, xmlDoc.InnerXml);
    }
    [TestMethod]
    public void AddXmlTest1()
    {
      string expected = "<Root><Item1>1</Item1><Item2 id=\"test\">2</Item2><Item3>3</Item3><Item4><SubItem1>s1</SubItem1><SubItem2>s2</SubItem2></Item4></Root>";
      XmlDocument xmlDoc = new XmlDocument();
      XmlElement elem = xmlDoc.CreateElement("Root");
      elem.Add("Item1", "1")
          .Add("Item2", new[] { "id", "test" }, "2")
          .Add("Item3", "3")
          .Add("Item4",
            xmlDoc.NewElement("SubItem1", "s1"),
            xmlDoc.NewElement("SubItem2", "s2"));
      xmlDoc.AppendChild(elem);
      Assert.AreEqual(expected, xmlDoc.InnerXml);
    }

    [TestMethod]
    public void XmlCreationTest()
    {
      string expected = "<Root><Item1>1</Item1><Item2 id=\"test\">2</Item2><Item3>3</Item3><Item4><SubItem1>s1</SubItem1><SubItem2>s2</SubItem2></Item4></Root>";
      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.Add("Root",
          xmlDoc.NewElement("Item1", "1"),
          xmlDoc.NewElement("Item2", new[] { "id", "test" }, "2"),
          xmlDoc.NewElement("Item3", "3"),
          xmlDoc.NewElement("Item4",
            xmlDoc.NewElement("SubItem1", "s1"),
            xmlDoc.NewElement("SubItem2", "s2")));
      Assert.AreEqual(expected, xmlDoc.InnerXml);
    }

    /// <summary>
    /// This test validates Descendants, XPathSelectMany, and Remove.
    /// These are purely test cases, and should not be used without thinking
    /// about the structure of the document to be processed. For example,
    /// some test cases of Descendants() unnecessarily iterate over the 
    /// entire document.
    /// </summary>
    [TestMethod]
    public void TestDescendants()
    {
      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(TestResources.desc1actual);

      // Let's have some fun by making all the number based attributes (i.e. Veh1, Drv1, etc...) 0-based
      foreach (var node in xmlDoc.Descendants(XmlNodeType.Attribute))
        if (char.IsNumber(node.InnerText[node.InnerText.Length - 1])) // check to see if the last character is numeric
          node.InnerText = node.InnerText.Substring(0, node.InnerText.Length - 1) + (node.InnerText[node.InnerText.Length - 1] - '1').ToString();
      XmlDiff diff = new XmlDiff(xmlDoc.InnerXml, TestResources.desc1expected);
      diff.CompareDocuments(new XmlDiffOptions());
      Assert.AreEqual(0, diff.DiffNodeList.Count);

      xmlDoc.LoadXml(TestResources.desc2actual);

      // This time let's remove all empty nodes, and certain partial nodes, while ignoring required nodes
      // linquified!
      (from node in xmlDoc.Descendants(XmlNodeType.Element)
       let ignore = (node.LocalName == "ItemIdInfo" || node.LocalName == "InsuredOrPrincipal")
       let partialNodes = (node.LocalName == "TaxIdentity" && node.ChildNodes.Count < 2) ||
                          (node.LocalName == "QuestionAnswer" && node.ChildNodes.Count < 2)
       where ((!node.HasChildNodes && node.InnerText == string.Empty) || partialNodes) && !ignore
       select node).Remove();

      diff = new XmlDiff(xmlDoc.InnerXml, TestResources.desc2expected);
      diff.CompareDocuments(new XmlDiffOptions() { TwoWayMatch = true });
      Assert.AreEqual(0, diff.DiffNodeList.Count);

      // How about aggregating the options nodes - there is already a method for this
      // see AggregateChildElements. This is just an example of the power of linq
      xmlDoc.LoadXml(TestResources.aggregatetest);
      var groups = (from node in xmlDoc.Descendants(XmlNodeType.Element)
                    where node.LocalName == "Option"
                    group node.ChildNodes by node.ParentNode);
      foreach (var group in groups)
      {
        // Remove the old parent "Option"
        var opts = group.SelectMany(nodes => nodes.Cast<XmlNode>());
        opts.Select(n => n.ParentNode).Remove();

        // Add the child nodes to the aggregate "Option"
        group.Key.Add("Option", opts.ToArray());
      }

      diff = new XmlDiff(xmlDoc.InnerXml, TestResources.aggregateexpected);
      XmlDiffOptions options = new XmlDiffOptions();
      options.TwoWayMatch = true;
      Assert.IsTrue(diff.CompareDocuments(options));

      // Now, let's try removing all CurrentTermAmt nodes under PersVeh
      xmlDoc.LoadXml(TestResources.desc3actual);
      (from coverage in xmlDoc.XPathSelectMany("//PersVeh[@id = $id and @RatedDriverRef = $driver]/Coverage", "1", "1")
       where coverage["CurrentTermAmt"].HasValue()
       select coverage["CurrentTermAmt"]).Remove();
      diff = new XmlDiff(xmlDoc.InnerXml, TestResources.desc3expected);
      Assert.IsTrue(diff.CompareDocuments(new XmlDiffOptions() { TwoWayMatch = true }));

      // Lastly, let's convert all "PerAccident" text to "PerAcc"
      xmlDoc.LoadXml(TestResources.desc4actual);
      (from coverage in xmlDoc.XPathSelectMany("//Coverage")
       from node in coverage.Descendants(XmlNodeType.Text)
       where node.InnerText == "PerAccident"
       select node)
      .ToList()
      .ForEach(n => n.InnerText = "PerAcc");
      diff = new XmlDiff(xmlDoc.InnerXml, TestResources.desc4expected);
      Assert.IsTrue(diff.CompareDocuments(new XmlDiffOptions() { TwoWayMatch = true }));
    }

    /// <summary>
    /// Another test case showing how the XmlDocument extension methods
    /// can be used with linq to extract large portions of information.
    /// Here is how coverage info can be extracted
    /// </summary>
    [TestMethod]
    public void TestXml3()
    {
      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(TestResources.HallmarkResponse);

      var vehicleCoverages = from vehicle in xmlDoc.XPathSelectMany("/ACORD/InsuranceSvcRs/PersAutoPolicyQuoteInqRs/PersAutoLineBusiness/PersVeh")
                             from coverage in vehicle.XPathSelectMany("Coverage")
                             group coverage by vehicle.Attributes["id"] into pair
                             select new
                             {
                               VehId = int.Parse(pair.Key.Value.Remove(0, 1)),
                               Coverages = from coverage in pair
                                           where coverage["CurrentTermAmt"].HasValue()
                                           select new
                                           {
                                             Code = coverage["CoverageCd"].GetValue(),
                                             Limit1 = coverage.XPathSelectSingle("Limit[LimitAppliesToCd='PerPerson']/FormatInteger")
                                                              .GetValueAsInt(
                                                                coverage.XPathSelectSingle("Limit/FormatInteger").GetValueAsInt()),
                                             Limit2 = coverage.XPathSelectSingle("Limit[LimitAppliesToCd='PerAcc']/FormatInteger")
                                                              .GetValueAsInt(
                                                                 coverage.XPathSelectSingle("Limit/FormatInteger").GetValueAsInt()),
                                             Deductible = coverage.XPathSelectSingle("Deductible/FormatInteger")
                                                                  .GetValueAsInt(),
                                             Premium = coverage.XPathSelectSingle("CurrentTermAmt/Amt")
                                                               .GetValueAsDouble()
                                           }
                             };
      var vehicleList = vehicleCoverages.ToList();
      var coverages = vehicleList[0].Coverages.ToList();
      Assert.AreEqual(1, vehicleList[0].VehId);
      Assert.AreEqual("BI", coverages[0].Code);
      Assert.AreEqual(25000, coverages[0].Limit1);
      Assert.AreEqual(50000, coverages[0].Limit2);
      Assert.AreEqual(177.0, coverages[0].Premium);
      Assert.AreEqual(2, vehicleList[1].VehId);
      coverages = vehicleList[1].Coverages.ToList();
      Assert.AreEqual("PD", coverages[1].Code);
      Assert.AreEqual(10000, coverages[1].Limit1);
      Assert.AreEqual(163.0, coverages[1].Premium);
      var pip = coverages.Where(c => c.Code == "PIP").FirstOrDefault();
      Assert.AreEqual(10000, pip.Limit1);
      Assert.AreEqual(10000, pip.Deductible);
      Assert.AreEqual(292.0, pip.Premium);
    }

    [TestMethod]
    public void ToXDocumentTest()
    {
      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(TestResources.HallmarkResponse);
      XDocument xDoc = xmlDoc.ToXDocument();
      Assert.AreEqual(xmlDoc.DocumentElement.OuterXml, xDoc.ToString(SaveOptions.DisableFormatting));
    }

    /// <summary>
    /// Tests ToXElement and ToXmlElement
    /// </summary>
    [TestMethod]
    public void ToXElementXmlElementTest()
    {
      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(TestResources.HallmarkResponse);
      var xmlNode = xmlDoc.XPathSelectSingle("//PersVeh/Coverage[CoverageCd='BI']");
      XElement xElem = xmlNode.ToXElement();
      Assert.AreEqual(xmlNode.OuterXml, xElem.ToString(SaveOptions.DisableFormatting));
      (from elem in xElem.Elements("Limit")
       where elem.Element("LimitAppliesToCd").Value == "PerAcc"
       select elem)
      .ToList()
      .ForEach(e => e.Value = "PerAccident");
      xmlNode = xElem.ToXmlElement(xmlDoc);
      Assert.AreEqual(xmlNode.OuterXml, xElem.ToString(SaveOptions.DisableFormatting));
    }

    [TestMethod]
    public void ElementsTest()
    {
      string xml = "<Root><Item1>1</Item1><Item2 id=\"test\">2</Item2><Item3>3</Item3><Item4><SubItem1>s1</SubItem1><SubItem2>s2</SubItem2></Item4></Root>";
      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(xml);
      int count = 1;
      foreach (var node in xmlDoc.DocumentElement.Elements())
        Assert.AreEqual("Item" + count++, node.LocalName);

      xml = "<Root><Item>1</Item><Item id=\"test\">2</Item><Item>3</Item><Item>4</Item><Random>Test</Random></Root>";
      xmlDoc.LoadXml(xml);
      count = 1;
      foreach (var node in xmlDoc.DocumentElement.Elements("Item"))
        Assert.AreEqual(count++, node.InnerText);
    }

    /// <summary>
    ///A test for TryUpdateElement
    ///</summary>
    [TestMethod()]
    public void TryUpdateElementTest()
    {
      XmlDocument actualXmlDoc = new XmlDocument();
      actualXmlDoc.LoadXml(TestResources.updatenodetest);
      XmlDocument expectedXmlDoc = new XmlDocument();
      expectedXmlDoc.LoadXml(TestResources.updatenodeexpected);

      XmlNode insNode = actualXmlDoc.SelectSingleNode("ACORD/InsuranceSvcRq/PersAutoPolicyQuoteInqRq/PersAutoLineBusiness/PersDriver[@id = 'D1']");
      Assert.IsTrue(insNode.TryUpdateElement("DriverInfo/DriversLicense", "StateProvCd", "IT"));

      Assert.IsFalse(insNode.TryUpdateElement("DriverInfo/DriversLicense", "LicenseTypeCd", "Permit"));

      Assert.IsTrue(actualXmlDoc.TryUpdateElement("ACORD/InsuranceSvcRq/PersAutoPolicyQuoteInqRq/PersAutoLineBusiness/PersDriver[@id = 'D1']/DriverInfo/DriversLicense", "CountryCd", "CA", true));

      XmlDiff diff = new XmlDiff(actualXmlDoc.InnerXml, expectedXmlDoc.InnerXml);
      XmlDiffOptions options = new XmlDiffOptions();
      options.TwoWayMatch = true;
      Assert.IsTrue(diff.CompareDocuments(options));

      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(TestResources.alliedreqxml);
      var node = xmlDoc.SelectSingleNode("//HomeLineBusiness/Dwell/Coverage[CoverageCd=\"DWELL\"]/Limit");
      Assert.IsTrue(node.TryUpdateElement("FormatInteger", "150000"));
      Assert.AreEqual("150000", xmlDoc.XPathSelectSingle("//HomeLineBusiness/Dwell/Coverage[CoverageCd='DWELL']/Limit/FormatInteger").GetValue());
    }
  }
}
