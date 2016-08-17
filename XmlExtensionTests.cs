using System;
using System.Xml.Linq;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ITC.Insurance.DataT.New;
using XmlDiffLib;
using ITC.Utilities;
using ITC.Utilities.New;
using MaybeNull;
using ITC.Insurance.DataTransformation;
using ITC.Insurance.AU;

namespace ITC.Insurance.DataT.New.Test
{
  [TestClass]
  public class XmlExtensionTests
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
       where coverage.GetChild("CurrentTermAmt").HasValue
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
                               VehId = ITCConvert.ToInt32(pair.Key.Value.Remove(0, 1), 0),
                               Coverages = from coverage in pair
                                           where coverage.GetChild("CurrentTermAmt").HasValue
                                           select new
                                           {
                                             Code = coverage.GetChild("CoverageCd").Select(n => n.InnerText).Else(string.Empty),
                                             Limit1 = coverage.XPathSelectSingle("Limit[LimitAppliesToCd='PerPerson']/FormatInteger")
                                                              .Select(n => ITCConvert.ToInt32(n.InnerText, ITCConstants.InvalidNum))
                                                              .Else(coverage.XPathSelectSingle("Limit[1]/FormatInteger")
                                                                            .Select(n => ITCConvert.ToInt32(n.InnerText, ITCConstants.InvalidNum))
                                                                            .Else(ITCConstants.InvalidNum)),
                                             Limit2 = coverage.XPathSelectSingle("Limit[LimitAppliesToCd='PerAcc']/FormatInteger")
                                                              .Select(n => ITCConvert.ToInt32(n.InnerText, ITCConstants.InvalidNum))
                                                              .Else(ITCConstants.InvalidNum),
                                             Deductible = coverage.XPathSelectSingle("Deductible/FormatInteger")
                                                                  .Select(n => ITCConvert.ToInt32(n.InnerText, ITCConstants.InvalidNum))
                                                                  .Else(ITCConstants.InvalidNum),
                                             Premium = coverage.XPathSelectSingle("CurrentTermAmt/Amt")
                                                               .Select(n => ITCConvert.ToDouble(n.InnerText, 0.0))
                                                               .Else(ITCConstants.InvalidNum)
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
      var xmlNode = xmlDoc.XPathSelectSingle("//PersVeh/Coverage[CoverageCd='BI']").Single();
      XElement xElem = xmlNode.ToXElement().Single();
      Assert.AreEqual(xmlNode.OuterXml, xElem.ToString(SaveOptions.DisableFormatting));
      (from elem in xElem.Elements("Limit")
       where elem.Element("LimitAppliesToCd").Value == "PerAcc"
       select elem)
      .ToList()
      .ForEach(e => e.Value = "PerAccident");
      xmlNode = xElem.ToXmlElement(xmlDoc).Single();
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
    /// TEsts various Xml Extension methods
    /// </summary>
    [TestMethod]
    public void TestCoverageCreation()
    {
      string[] validCoverages = new string[]
      {
        "BI",
        "PD",
        "COLL",
        "PIP",
        "COMP",
        "UM",
        "UMPD",
        "CRA",
      };

      AUPolicy policy = new AUPolicy();
      policy.Insured = new AUDriver();
      TT2AUBridge bridge = new TT2AUBridge(TestResources.TX_b74e03f8Policy, policy);
      bridge.ImportPolicyInfo();

      XmlDocument xmlDocExpected = new XmlDocument();
      XmlDocument xmlDocActual = new XmlDocument();
      xmlDocExpected.LoadXml(TestResources.AspenExpected);
      xmlDocActual.LoadXml(TestResources.AspenExpected);
      AcordXmlAUBridge xbridge = new AcordXmlAUBridge();
      xbridge.XmlDoc = xmlDocActual;

      // Remove all coverage nodes so that we can test we create
      // the same xml
      (from veh in xmlDocActual.XPathSelectMany("//PersVeh")
       from cov in veh.XPathSelectMany("Coverage")
       select cov).Remove();

      #region Large Coverage Branch
      for (int i = 0; i < policy.NumOfCars; i++)
      {
        var car = policy.Cars[i];
        var node = xbridge.GetVehicleNode(i);
        if (!(node is Empty))
        {
          foreach (var coverage in validCoverages)
          {
            switch (coverage)
            {
              case "BI":
                {
                  if (car.LiabBI)
                  {
                    node.Add(
                      xbridge.CreateCoverage(AcordXmlConstantsClass.Coverages.LiabBI,
                        new Limit[]
                        {
                          xbridge.CreateLimit(car.LiabLimits1*1000, AcordXmlConstantsClass.LimitAppliesTo.PerPerson),
                          xbridge.CreateLimit(car.LiabLimits2*1000, AcordXmlConstantsClass.LimitAppliesTo.PerAccident)
                        }));
                  }
                  break;
                }
              case "PD":
                {
                  if (car.LiabPD)
                  {
                    node.Add(
                      xbridge.CreateCoverage("PD",
                        new Limit[]
                        {
                          xbridge.CreateLimit(car.LiabLimits3*1000, AcordXmlConstantsClass.LimitAppliesTo.PropertyDamage)
                        }));
                  }
                  break;
                }
              case "COLL":
                {
                  if (car.Comp)
                  {
                    node.Add(
                      xbridge.CreateCoverage("COLL",
                        new Deductible[]
                        {
                          xbridge.CreateDeductible(car.CollDed, AcordXmlConstantsClass.DeductibleType.PerClaim)
                        },
                        new Option[]
                        {
                          // This is only here to mimic how the core creates this section.
                          xbridge.CreateOption("Misc1", xbridge.AcordXmlConstants.MICollTypeStrings[IndexLib.GetStringIndex(car.CollType, AUConstants.MICollTypeChars, 0)])
                        }));
                  }
                  break;
                }
              case "PIP":
                {
                  if (car.PIP)
                  {
                    node.Add(
                      xbridge.CreateCoverage("PIP",
                        new Limit[]
                        {
                          xbridge.CreateLimit(car.PIPLimit, AcordXmlConstantsClass.LimitAppliesTo.PerPerson)
                        },
                        new Option[]
                        {
                          xbridge.CreateOption("Misc2", xbridge.AcordXmlConstants.StackedStrings[Convert.ToInt32(car.CoStackedPIP)])
                        }));
                  }
                  break;
                }
              case "COMP":
                {
                  if (car.Comp)
                  {
                    node.Add(
                      xbridge.CreateCoverage("COMP",
                        new Deductible[]
                        {
                          xbridge.CreateDeductible(car.CompDed, AcordXmlConstantsClass.DeductibleType.PerClaim)
                        }));
                  }
                  break;
                }
              case "UM":
                {
                  if (car.UninsBI)
                  {
                    node.Add(
                      xbridge.CreateCoverage("UM",
                        new Limit[]
                          {
                            xbridge.CreateLimit(car.UninsBILimits1*1000, AcordXmlConstantsClass.LimitAppliesTo.PerPerson),
                            xbridge.CreateLimit(car.UninsBILimits2*1000, AcordXmlConstantsClass.LimitAppliesTo.PerAccident)
                          },
                        new Option[]
                          {
                            xbridge.CreateOption("Option 1", xbridge.AcordXmlConstants.StackedStrings[Convert.ToInt32(car.CoStackedUM)])
                          }));
                  }
                  break;
                }
              case "UMPD":
                {
                  if (car.UninsPD)
                  {
                    node.Add(
                      xbridge.CreateCoverage("UMPD",
                        new Limit[]
                        {
                          xbridge.CreateLimit(car.UninsPDLimit*1000, AcordXmlConstantsClass.LimitAppliesTo.PerAccident)
                        },
                        new Deductible[]
                        {
                          xbridge.CreateDeductible(car.UninsPDDed, AcordXmlConstantsClass.DeductibleType.PerClaim)
                        }));
                  }
                  break;
                }
              case "CRA":
                {
                  // This is always written for some reason
                  node.Add(xbridge.CreateCoverage("CRA"));
                  break;
                }
              default:
                break;
            }
          }
        }
      }
      #endregion

      XmlDiff diff = new XmlDiff(xmlDocExpected.InnerXml, xmlDocActual.InnerXml);
      Assert.IsTrue(diff.CompareDocuments(new XmlDiffOptions() { TwoWayMatch = true }));
    }


    [TestMethod]
    public void AddCoverageTest()
    {
      XmlDocument xmlDoc = new XmlDocument();
      AcordXmlAUBridge bridge = new AcordXmlAUBridge();
      xmlDoc = bridge.XmlDoc;
      string expected = "<Root><Coverage><CoverageCd>COMP</CoverageCd><CoverageDesc>ComprehensiveCoverage</CoverageDesc><Deductible><FormatInteger>500</FormatInteger><DeductibleTypeCd>CL</DeductibleTypeCd></Deductible><CurrentTermAmt><Amt>0</Amt></CurrentTermAmt></Coverage></Root>";

      // These are just test cases
      // Use CreateCoverage(coverageCd, coverageDescription, ...) instead
      var elem =
        xmlDoc.NewElement("Root",
          bridge.CreateCoverage(AcordXmlConstantsClass.Coverages.Comp,
                  new Deductible[]
                  {
                    bridge.CreateDeductible(500, AcordXmlConstantsClass.DeductibleType.PerClaim)
                  })
                .InsertAfter("CoverageCd", xmlDoc.NewElement("CoverageDesc", "ComprehensiveCoverage"))
                .Add("CurrentTermAmt",
                      xmlDoc.NewElement("Amt", "0")));
      bridge.XmlDoc.Add(elem);
      Assert.AreEqual(expected, bridge.XmlDoc.InnerXml);

      xmlDoc = new XmlDocument();
      bridge.XmlDoc = xmlDoc;
      bool someCondition = false;
      xmlDoc.Add(
        xmlDoc.NewElement("Root",
          bridge.CreateCoverage(AcordXmlConstantsClass.Coverages.Comp, "ComprehensiveCoverage",
                  new Limit[]
                  {
                    null, // nulls are ignored
                    null,
                  },
                  new Deductible[]
                  {
                    bridge.CreateDeductible(500, AcordXmlConstantsClass.DeductibleType.PerClaim)
                  },
                  new Option[]
                  {
                    (someCondition) ? bridge.CreateOption("Misc1", "NA") : null
                  })
                .Add("CurrentTermAmt",
                  xmlDoc.NewElement("Amt", "0"))));
      Assert.AreEqual(expected, xmlDoc.InnerXml);
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
      Assert.AreEqual("150000", xmlDoc.XPathSelectSingle("//HomeLineBusiness/Dwell/Coverage[CoverageCd='DWELL']/Limit/FormatInteger").Select(n => n.InnerText));
    }
  }
}
