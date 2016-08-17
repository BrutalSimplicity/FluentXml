using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace FluidXml
{
  /// <summary>
  /// In methods where an Xml Document is required, we can use this exception
  /// when a sufficient response is not possible (i.e. having to return a null value)
  /// </summary>
  public class NoXmlDocumentException : Exception
  {
    public NoXmlDocumentException(string message) : base(message) { }
  }

  /// <summary>
  /// XmlDocument Extensions for querying/updating/creating/transforming Nodes
  /// </summary>
  public static class XmlDocExtensions
  {

    /// <summary>
    /// Tries to update the value of an Xml element
    /// <remarks>
    /// If the forceCreate option is used will this create the node underneath the parent,
    /// if the node is not found.
    /// </remarks>
    /// </summary>
    /// <param name="node">Node to search from</param>
    /// <param name="parentNodeXpath">XPath of parent node</param>
    /// <param name="childElementName">Child element name to update</param>
    /// <param name="value">Value to use for update</param>
    /// <param name="forceCreate">If true, will create the element if it is not found</param>
    /// <returns>True if the node is updated or created, false otherwise</returns>
    public static bool TryUpdateElement(this XmlNode node, string parentNodeXpath, string childElementName, string value, bool forceCreate = false)
    {
      if (node == null)
        return false;

      bool result = false;
      try
      {
        XmlNode foundNode = node.SelectSingleNode(parentNodeXpath);
        if (foundNode != null)
        {
          if (foundNode[childElementName] != null)
          {
            foundNode[childElementName].InnerText = value;
            result = true;
          }
          else
          {
            if (forceCreate)
            {
              XmlDocument xmlDoc;
              if (!TryGetXmlDocument(node, out xmlDoc))
                return false;
              XmlNode newNode = (XmlElement)xmlDoc.CreateElement(childElementName);
              newNode.InnerText = value;
              foundNode.AppendChild(newNode);
              result = true;
            }
          }
        }
      }
      catch
      {
        return false;
      }
      return result;
    }

    /// <summary>
    /// Tries to update the value of an Xml element
    /// <remarks>
    /// If the forceCreate option is used will create the node underneath the parent,
    /// if the node is not found.
    /// </remarks>
    /// </summary>
    /// <param name="node">Parent Node</param>
    /// <param name="childElementName">Child element name to update</param>
    /// <param name="value">Value to use for update</param>
    /// <param name="forceCreate">If true, will create the element if it is not found</param>
    /// <returns>True if the node is updated or created, false otherwise</returns>
    public static bool TryUpdateElement(this XmlNode node, string childElementName, string value, bool forceCreate = false)
    {
      if (node == null)
        return false;

      bool result = false;
      try
      {
        if (node[childElementName] != null)
        {
          node[childElementName].InnerText = value;
          result = true;
        }
        else
        {
          if (forceCreate)
          {
            XmlDocument xmlDoc;
            if (!TryGetXmlDocument(node, out xmlDoc))
              return false;
            XmlNode newNode = (XmlElement)xmlDoc.CreateElement(childElementName);
            newNode.InnerText = value;
            node.AppendChild(newNode);
            result = true;
          }
        }
      }
      catch
      {
        return false;
      }
      return result;
    }

    /// <summary>
    /// Creates a new element and adds it to a node. Then
    /// returns the new element chain.
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="tagName">tag name of the node</param>
    /// <param name="value">value of the node</param>
    /// <returns>Itself</returns>
    public static XmlNode Add(this XmlNode xmlNode, string tagName, string value)
    {
      if (xmlNode == null)
        return null;

      XmlDocument xmlDoc;
      if (!TryGetXmlDocument(xmlNode, out xmlDoc))
        throw new NoXmlDocumentException(m_noXmlDocExceptionMessage);
      XmlElement elem = xmlDoc.CreateElement(tagName);
      elem.InnerText = value;
      xmlNode.AppendChild(elem);
      return xmlNode;
    }

    /// <summary>
    /// Creates a new element and adds it to a node. Then
    /// returns the new element chain.
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="tagName"></param>
    /// <param name="attrs">name-value pair of attributes</param>
    /// <param name="value"></param>
    /// <returns>Itself</returns>
    public static XmlNode Add(this XmlNode xmlNode, string tagName, string[] attrs, string value)
    {
      if (xmlNode == null)
        return null;

      XmlDocument xmlDoc;
      if (!TryGetXmlDocument(xmlNode, out xmlDoc))
        throw new NoXmlDocumentException(m_noXmlDocExceptionMessage);
      XmlElement elem = xmlDoc.CreateElement(tagName);
      elem.InnerText = value;

      if (attrs.Length > 0 && attrs.Length % 2 != 0)
        throw new ArgumentException("Attributes array is missing a name-value pair.", "attrs");

      for (int attrIndex = 0; attrIndex < attrs.Length; attrIndex += 2)
      {
        XmlAttribute newAttr = xmlDoc.CreateAttribute(attrs[attrIndex]);
        newAttr.Value = attrs[attrIndex + 1];
        elem.Attributes.Append(newAttr);
      }
      xmlNode.AppendChild(elem);

      return xmlNode;
    }

    /// <summary>
    /// Adds node(s) to a node
    /// </summary>
    /// <param name="attrs">Key-Value pair collection of attributes</param>
    /// <param name="nodes">node(s) to add</param>
    /// <returns>Itself</returns>
    public static XmlNode Add(this XmlNode xmlNode, params XmlNode[] nodes)
    {
      if (xmlNode == null)
        return null;

      var mutNode = xmlNode;
      if (xmlNode.NodeType == XmlNodeType.Document)
      {
        XmlDocument doc;
        if (TryGetXmlDocument(xmlNode, out doc))
          mutNode = doc.DocumentElement;
      }
      foreach (var node in nodes)
        mutNode.AppendChild(node);

      return xmlNode;
    }

    /// <summary>
    /// Adds nodes to an element with tagName and then adds that
    /// node to a parent node, and then returns itself.
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="tagName">Name of the tag</param>
    /// <param name="node">Node to add to the tagName element</param>
    /// <returns>Itself</returns>
    public static XmlNode Add(this XmlNode xmlNode, string tagName, params XmlNode[] nodes)
    {
      if (xmlNode == null)
        return null;

      XmlDocument xmlDoc;
      if (!TryGetXmlDocument(xmlNode, out xmlDoc))
        throw new NoXmlDocumentException(m_noXmlDocExceptionMessage);
      XmlElement elem = xmlDoc.CreateElement(tagName);

      foreach (var node in nodes)
        elem.AppendChild(node);
      xmlNode.AppendChild(elem);

      return xmlNode;
    }

    /// <summary>
    /// Adds nodes to an element with tagName and attributes, then adds that
    /// node to a parent node, and then returns itself.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="xmlNode"></param>
    /// <param name="tagName">Name of the tag</param>
    /// <param name="node">Node to add to the tagName element</param>
    /// <returns>Itself</returns>
    public static XmlNode Add(this XmlNode xmlNode, string tagName, string[] attrs, params XmlNode[] nodes)
    {
      if (xmlNode == null)
        return null;

      XmlDocument xmlDoc;
      if (!TryGetXmlDocument(xmlNode, out xmlDoc))
        throw new NoXmlDocumentException(m_noXmlDocExceptionMessage);
      XmlElement elem = xmlDoc.CreateElement(tagName);

      if (attrs.Length > 0 && attrs.Length % 2 != 0)
        throw new ArgumentException("Attributes array is missing a name-value pair.", "attrs");

      for (int attrIndex = 0; attrIndex < attrs.Length; attrIndex += 2)
      {
        XmlAttribute newAttr = xmlDoc.CreateAttribute(attrs[attrIndex]);
        newAttr.Value = attrs[attrIndex + 1];
        elem.Attributes.Append(newAttr);
      }

      foreach (var node in nodes)
        elem.AppendChild(node);
      xmlNode.AppendChild(elem);

      return xmlNode;
    }

    /// <summary>
    /// Creates a new element and inserts it at the beginning of a node. Then
    /// returns the new element chain.
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="tagName"></param>
    /// <param name="value"></param>
    /// <returns>Itself</returns>
    public static XmlNode Insert(this XmlNode xmlNode, string tagName, string value)
    {
      if (xmlNode == null)
        return null;

      XmlDocument xmlDoc;
      if (!TryGetXmlDocument(xmlNode, out xmlDoc))
        throw new NoXmlDocumentException(m_noXmlDocExceptionMessage);
      XmlElement elem = xmlDoc.CreateElement(tagName);
      elem.InnerText = value;
      xmlNode.PrependChild(elem);
      return xmlNode;
    }

    /// <summary>
    /// Creates a new element and inserts it at the beginning of a node. Then
    /// returns the new element chain.
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="tagName"></param>
    /// <param name="attrs">Pairs of attributes</param>
    /// <param name="value"></param>
    /// <returns>Itself</returns>
    public static XmlNode Insert(this XmlNode xmlNode, string tagName, string[] attrs, string value)
    {
      if (xmlNode == null)
        return null;

      XmlDocument xmlDoc;
      if (!TryGetXmlDocument(xmlNode, out xmlDoc))
        throw new NoXmlDocumentException(m_noXmlDocExceptionMessage);
      XmlElement elem = xmlDoc.CreateElement(tagName);
      elem.InnerText = value;

      if (attrs != null)
        AddElementAttributes(elem, attrs);

      xmlNode.PrependChild(elem);

      return xmlNode;
    }

    /// <summary>
    /// Inserts new nodes at the beginning of a node and then returns itself.
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="nodes">nodes to be inserted</param>
    /// <returns>Itself</returns>
    public static XmlNode Insert(this XmlNode xmlNode, params XmlNode[] nodes)
    {
      if (xmlNode == null)
        return null;

      foreach (var node in nodes)
        xmlNode.PrependChild(node);

      return xmlNode;
    }

    /// <summary>
    /// Adds nodes at the beginning of an element with tagName and then inserts that
    /// node at the beginning of a parent node, and then returns itself.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="xmlNode"></param>
    /// <param name="tagName">Name of the tag</param>
    /// <param name="nodes">Nodes to add to the tagName element</param>
    /// <returns>Itself with new nodes</returns>
    public static XmlNode Insert(this XmlNode xmlNode, string tagName, params XmlNode[] nodes)
    {
      if (xmlNode == null)
        return null;

      XmlDocument xmlDoc;
      if (!TryGetXmlDocument(xmlNode, out xmlDoc))
        throw new NoXmlDocumentException(m_noXmlDocExceptionMessage);
      XmlNode newNode = xmlDoc.CreateElement(tagName);
      foreach (XmlNode node in nodes)
        newNode.AppendChild(node);
      xmlNode.PrependChild(newNode);
      return xmlNode;
    }

    /// <summary>
    /// Adds nodes at the beginning of an element with tagName and then inserts that
    /// node at the beginning of a parent node, and then returns itself.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="xmlNode"></param>
    /// <param name="tagName">Name of the tag</param>
    /// <param name="attrs">element attributes</param>
    /// <param name="nodes">Nodes to add to the tagName element</param>
    /// <returns>Itself with new nodes inserted</returns>
    public static XmlNode Insert(this XmlNode xmlNode, string tagName, string[] attrs, params XmlNode[] nodes)
    {
      if (xmlNode == null)
        return null;

      XmlDocument xmlDoc;
      if (!TryGetXmlDocument(xmlNode, out xmlDoc))
        throw new NoXmlDocumentException(m_noXmlDocExceptionMessage);
      XmlNode newNode = xmlDoc.CreateElement(tagName);
      foreach (XmlNode node in nodes)
        newNode.AppendChild(node);
      xmlNode.PrependChild(newNode);

      if (attrs != null)
        AddElementAttributes(newNode, attrs);

      return xmlNode;
    }

    /// <summary>
    /// Insert node a new node before a child node
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="childName">Name of child to insert node before</param>
    /// <param name="newNode">Node to insert</param>
    /// <returns>Itself with newly inserted node</returns>
    public static XmlNode InsertBefore(this XmlNode xmlNode, string childName, XmlNode newNode)
    {
      if (xmlNode == null)
        return null;

      if (xmlNode[childName] != null)
        xmlNode.InsertBefore(newNode, xmlNode[childName]);
      return xmlNode;
    }

    /// <summary>
    /// Insert a new node after a child node
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="childName">Name of child to insert node after</param>
    /// <param name="newNode">Node to insert</param>
    /// <returns>Itself with newly inserted node</returns>
    public static XmlNode InsertAfter(this XmlNode xmlNode, string childName, XmlNode newNode)
    {
      if (xmlNode == null)
        return null;

      if (xmlNode[childName] != null)
        xmlNode.InsertAfter(newNode, xmlNode[childName]);
      return xmlNode;
    }

    /// <summary>
    /// Returns a new element with given name and value
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="tagName">Name of the element</param>
    /// <param name="tagValue">Value of the element</param>
    /// <returns>New element</returns>
    public static XmlElement NewElement(this XmlDocument doc, string tagName, string tagValue)
    {
      if (doc == null)
        return null;

      var elem = doc.CreateElement(tagName);
      elem.InnerText = tagValue;
      return elem;
    }

    /// <summary>
    /// Returns a new element with given name and nodes
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="tagName">Name of the element</param>
    /// <param name="nodes">Nodes to be added to the element</param>
    /// <returns>New element with given nodes</returns>
    public static XmlElement NewElement(this XmlDocument doc, string tagName, params XmlNode[] nodes)
    {
      if (doc == null)
        return null;

      var elem = doc.CreateElement(tagName);
      foreach (var node in nodes)
        elem.AppendChild(node);
      return elem;
    }

    /// <summary>
    /// Returns a new element with given name and nodes
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="tagName">Name of the element</param>
    /// <param name="attrs">element attributes</param>
    /// <param name="nodes">Nodes to be added to the element</param>
    /// <returns>New element with given attributes and value</returns>
    public static XmlElement NewElement(this XmlDocument doc, string tagName, string[] attrs, string tagValue)
    {
      if (doc == null)
        return null;

      var elem = doc.CreateElement(tagName);
      elem.InnerText = tagValue;

      if (attrs != null)
        AddElementAttributes(elem, attrs);

      return elem;
    }

    /// <summary>
    /// Returns a new element with given name and nodes
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="tagName">Name of the element</param>
    /// /// <param name="attrs">element attributes</param>
    /// <param name="nodes">Nodes to be added to the element</param>
    /// <returns>New element with given nodes</returns>
    public static XmlElement NewElement(this XmlDocument doc, string tagName, string[] attrs, params XmlNode[] nodes)
    {
      if (doc == null)
        return null;

      var elem = doc.CreateElement(tagName);
      foreach (var node in nodes)
        elem.AppendChild(node);

      if (attrs != null)
        AddElementAttributes(elem, attrs);

      return elem;
    }

    /// <summary>
    /// Remove a set of nodes XLinq-like
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns>Number of nodes removed</returns>
    public static int Remove(this IEnumerable<XmlNode> nodes)
    {
      int count = 0;
      foreach (var node in nodes)
      {
        if (node.ParentNode != null)
        {
          node.ParentNode.RemoveChild(node);
          count++;
        }
      }
      return count;
    }

    /// <summary>
    /// Depth-First Post-Order (Document Order) Node Traversal.
    /// Similar to the XLinq Descendants()
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="type">
    /// [Optional] Types of nodes to return ValidTypes: None, Element, Text, Attribute
    /// (Default: XmlNodeType.None)
    /// </param>
    /// <returns>Enumerable set of XmlNodes</returns>
    public static IEnumerable<XmlNode> Descendants(this XmlNode xmlNode, params XmlNodeType[] types)
    {
      Stack<XmlNode> nodeStack = new Stack<XmlNode>();
      Stack<List<XmlNode>> lastVisitedStack = new Stack<List<XmlNode>>();
      XmlNode currNode = (xmlNode is XmlDocument) ? ((XmlDocument)xmlNode).DocumentElement : xmlNode;
      List<XmlNode> lastVisited = new List<XmlNode>();

      if (xmlNode != null)
      {
        while (nodeStack.Count > 0 || currNode != null)
        {
          // Travel down descendants until you can't travel anymore
          if (currNode != null)
          {
            // save state before moving to the next descendant
            nodeStack.Push(currNode);

            // Make sure you move to a descendant you haven't visited yet
            currNode = currNode.ChildNodes.Cast<XmlNode>().Where(n => !lastVisited.Contains(n)).FirstOrDefault();

            // Save the state of all the siblings you've visited
            lastVisitedStack.Push(lastVisited);

            lastVisited = new List<XmlNode>();
          }
          else
          {
            // Check to see if there are any siblings who haven't been visited yet,
            // if so let's visit them first.
            var peekNode = nodeStack.Peek();
            var otherNode = peekNode.ChildNodes.Cast<XmlNode>().Where(n => !lastVisited.Contains(n)).FirstOrDefault();
            if (otherNode != null)
            {
              currNode = otherNode;
            }
            else
            {
              // Visit the node
              if (types.Length > 0 && !types.Contains(XmlNodeType.None))
              {
                if (types.Contains(peekNode.NodeType))
                  yield return peekNode;
                if (types.Contains(XmlNodeType.Attribute))
                {
                  if (peekNode.Attributes != null && peekNode.Attributes.Count > 0)
                    foreach (XmlAttribute attrib in peekNode.Attributes)
                      yield return attrib;
                }
              }
              else  // if no node type pass all nodes
                yield return peekNode;

              // Since we're about to move back up the tree, 
              // we need to restore the previous state
              lastVisited = lastVisitedStack.Pop();

              // Add the visited node to the list of visited
              lastVisited.Add(nodeStack.Pop());
            }
          }
        }
      }
    }

    /// <summary>
    /// Return an IEnumerable over child nodes
    /// </summary>
    /// <param name="node"></param>
    /// <param name="elementName">[Optional] Child Element name to filter by</param>
    /// <returns>Enumerable set of child XmlNodes</returns>
    public static IEnumerable<XmlNode> Elements(this XmlNode node, string elementName = null)
    {
      if (node != null)
      {
        if (string.IsNullOrEmpty(elementName))
        {
          foreach (XmlNode child in node.ChildNodes)
            if (node.NodeType == XmlNodeType.Element)
              yield return child;
        }
        else
        {
          foreach (XmlNode child in node.ChildNodes)
            if (node.NodeType == XmlNodeType.Element && node.LocalName == elementName)
              yield return child;
        }
      }
    }

    /// <summary>
    /// Maybe selects a single node returned by XPath with possible arguments.
    /// e.g. XPathSelectSingle("PersVeh[@id = $id]", "Veh1")
    /// </summary>
    /// <param name="node"></param>
    /// <param name="xpath">XPath</param>
    /// <param name="args">
    /// Arguments to be substituted into the XPath for '$'. 
    /// The $ can be followed by any number of characters and digits (including none). 
    /// Arguments are substituted in order.
    /// </param>
    /// <returns>An XmlNode, or null if not found</returns>
    public static XmlNode XPathSelectSingle(this XmlNode node, string xpath, params string[] args)
    {
      if (node == null)
        return null;

      string queryableXPath = xpath;
      if (args.Length > 0)
      {
        queryableXPath = ReplaceXPathArgs(xpath, args);
      }
      return null;
    }

    /// <summary>
    /// Replaces parameters for XPath arguments
    /// beginning with $
    /// </summary>
    /// <param name="xpath"></param>
    /// <param name="args">arguments to be replaced</param>
    /// <returns>XPath with substituted parameters</returns>
    private static string ReplaceXPathArgs(string xpath, params string[] args)
    {
      // Could I have used a Regular Expression? Sure. I thought this would be
      // faster, considering XPaths are used everywhere!
      // Another alternative would've been to use XsltContext to define arguments, but
      // AFAIK these only work with XPathNavigator.
      string result = xpath;
      if (args.Length > 0)
      {
        // sanitize for args beginning with ' or "
        string[] escapedArgs = args.Select(a => '"' + a.Trim('"', '\'') + '"').ToArray();
        bool skipSingleQuote = false;
        bool skipDoubleQuote = false;
        List<int[]> argIndexes = new List<int[]>();
        for (int charIndex = 0; charIndex < xpath.Length; charIndex++)
        {
          // Skip information embedded in single/double quotes
          if (xpath[charIndex] == '\'' && !skipDoubleQuote)
            skipSingleQuote ^= true;
          if (xpath[charIndex] == '"' && !skipSingleQuote)
            skipDoubleQuote ^= true;
          if (!skipSingleQuote && !skipDoubleQuote)
          {
            // if $ is found, record the start index and the length of the identifier (characters and digits occuring after $)
            if (xpath[charIndex] == '$')
            {
              int[] newIndex = new int[] { charIndex++, 0 };
              int count = 1;
              for (; charIndex < xpath.Length && Char.IsLetterOrDigit(xpath[charIndex]); charIndex++, count++) ;
              newIndex[1] = count;
              argIndexes.Add(newIndex);
            }
          }
        }

        // make sure you have enough arguments for substitution (more args is OK)
        if (args.Length < argIndexes.Count)
          throw new ArgumentException("Number of XPath arguments is greater than the number of paramters", "args");

        // number of characters can't be bigger than the size of the xpath + arguments
        char[] charXPath = new char[xpath.Length + escapedArgs.Sum(a => a.Length)];
        int argIndex = 0;
        int charXPathIndex = 0;

        // drop in the characters until a subsitution point is found
        for (int charIndex = 0; charIndex < xpath.Length; charIndex++)
        {
          // now drop in the argument at the point of substitution
          if (argIndex < argIndexes.Count && argIndexes[argIndex][0] == charIndex)
          {
            for (int i = 0; i < escapedArgs[argIndex].Length; i++)
              charXPath[charXPathIndex++] = escapedArgs[argIndex][i];
            // update the original xpath indexer to the point after the identifier (PersVeh[@id = $id])
            charIndex += argIndexes[argIndex++][1] - 1;                           //                 ^  Point after the identifer
          }
          else
            charXPath[charXPathIndex++] = xpath[charIndex];
        }
        result = new string(charXPath, 0, charXPathIndex);
      }

      return result;
    }

    /// <summary>
    /// Returns an enumerable collection of nodes returned by XPath.
    /// These nodes are cast from the XmlNodeList to an IEnumerable, allowing Linq expressions
    /// to be applied to the nodes.
    /// e.g. XPathSelectMany("//PersVeh[@id = $veh and @RatedDriverRef = $driver]/Coverage", GetVehicleIDString(vehIndex + 1), GetDriverIDString(driverIndex + 1))
    /// </summary>
    /// <param name="node"></param>
    /// <param name="xpath">XPath</param>
    /// <param name="args"></param>
    /// <returns>
    /// Enumerable set of XmlNodes for the given XPath
    /// </returns>
    public static IEnumerable<XmlNode> XPathSelectMany(this XmlNode node, string xpath, params string[] args)
    {
      if (node == null)
        return null;

      string queryableXPath = xpath;
      if (args.Length > 0)
        queryableXPath = ReplaceXPathArgs(xpath, args);
      return node.SelectNodes(queryableXPath).Cast<XmlNode>();
    }

    /// <summary>
    /// Use this method to convert an XmlDocument to an XDocument.
    /// Due to performance implications, this should generally be avoided.
    /// Prefer ToXElement over this method.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="options">Load options</param>
    /// <returns>An XDocument</returns>
    public static XDocument ToXDocument(this XmlDocument document, LoadOptions options = LoadOptions.None)
    {
      if (document == null)
        throw new ArgumentNullException("document");

      using (XmlNodeReader reader = new XmlNodeReader(document))
      {
        return XDocument.Load(reader, options);
      }
    }

    /// <summary>
    /// If the power of XLinq is truly needed, use this method to maybe convert
    /// your XmlElement to an XElement
    /// </summary>
    /// <param name="elem"></param>
    /// <param name="options">Load Options</param>
    /// <returns>Maybe an XElement, or Maybe.Not</returns>
    public static XElement ToXElement(this XmlNode node, LoadOptions options = LoadOptions.None)
    {
      if (node.NodeType != XmlNodeType.Element)
        throw new ArgumentException("Node must be an element type");

      using (XmlNodeReader reader = new XmlNodeReader(node))
      {
        return XElement.Load(reader, options);
      }
    }

    /// <summary>
    /// Covert an XElement to an XmlNode
    /// </summary>
    /// <param name="elem"></param>
    /// <param name="document">The owning XmlDocument for the converted node</param>
    /// <param name="options">Reader Options</param>
    /// <returns>Maybe an XmlNode</returns>
    public static XmlNode ToXmlElement(this XElement elem, XmlDocument document, ReaderOptions options = ReaderOptions.None)
    {
      if (elem == null)
        throw new ArgumentNullException("elem");
      else if (document == null)
        throw new ArgumentNullException("document");

      try
      {
        return document.ReadNode(elem.CreateReader(options));
      }
      catch
      {
        return null;
      }
    }

    private static bool TryGetXmlDocument(XmlNode node, out XmlDocument doc)
    {
      if (node == null)
        throw new ArgumentNullException("node");

      doc = node.OwnerDocument;
      if (doc == null)
      {
        doc = node as XmlDocument;
        if (doc == null)
          return false;
      }
      return true;
    }

    private static void AddElementAttributes(XmlNode node, string[] attrs)
    {
      if (attrs.Length > 0 && attrs.Length % 2 != 0)
        throw new ArgumentException("Attributes array is missing a name-value pair.", "attrs");
      var xmlDoc = node.OwnerDocument;

      for (int attrIndex = 0; attrIndex < attrs.Length; attrIndex += 2)
      {
        XmlAttribute newAttr = xmlDoc.CreateAttribute(attrs[attrIndex]);
        newAttr.Value = attrs[attrIndex + 1];
        node.Attributes.Append(newAttr);
      }
    }

    private static string m_noXmlDocExceptionMessage = "Node doesn't contain a reference to an instance of XmlDocument";
  }
}
