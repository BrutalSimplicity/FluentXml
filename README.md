# FluidXml - XmlDocument Extensions

This package includes extensions to the XmlDocument interface to make it a bit more modern and XLinq-like. XLinq is still my preferred choice for interacting with Xml, but if you've already committed to XmlDocument, then these might be some useful extensions for you.

You might wonder, why not just copy over the tree from XmlDocument to XLinq, which is not a bad idea if your use case can sustain it; however, there are a couple issues with this:

- Using two Xml DOMs is expensive in both memory and speed. They are non-trivial APIs doing large amounts of work. One of the most expensive operations in using a DOM is parsing and loading the markup. If you're using both XmlDocument and XLinq you're paying this cost at least twice.

- Reduced maintainability switching between the different interfaces. Teams tend to know and understand the technologies they use most common. If you're in a situation where you've started using XLinq on a team that predominantly uses XmlDocument, you might be making it more difficult to maintain in the future for other team members unfamiliar with the API. These extensions allow them to continue using the XmlDocument API, but with some added bells and whistles should they so choose.

## Install

You can install the Nuget package using the nuget command line tool. If you're in Visual Studio you can also get it through the Nuget Package Manager tool.

## Examples

### Creating Xml

Let's create some basic Xml.

```csharp
XmlDocument doc = new XmlDocument();

doc.Add("Catalog",
  doc.NewElement("book", new [] { "id", "bk101" },
    doc.NewElement("author", "Gambardella, Matthew"),
    doc.NewElement("title", "XML Developer's Guide"),
    doc.NewElement("genre", "Computer"),
    doc.NewElement("price", "44.95"),
    doc.NewElement("publish_date", "2000-10-01"),
    doc.NewElement("description", "An in-depth look at creating applications with XML.")));

var bookNode = doc.NewElement("book", new[] { "id", "bk102" },
          doc.NewElement("author", "Ralls, Kim"),
          doc.NewElement("title", "Midnight Rain"),
          doc.NewElement("genre", "Fantasy"),
          doc.NewElement("price", "5.95"),
          doc.NewElement("publish_date", "2000-12-16"),
          doc.NewElement("description", "A former architect battles corporate zombies, an evil sorceress, and her own childhood to become queen of the world."));
doc.Add(bookNode);

// or

doc.Add(
  doc.NewElement("book", new[] { "id", "bk103" })
   .Add("author", "Corets, Eva")
   .Add("title", "Maeve Ascendant")
   .Add("genre", "Fantasy")
   .Add("price", "5.95")
   .Add("publish_date", "2000-11-17")
   .Add("description", "After the collapse of a nanotechnology society in England, the young survivors lay the foundation for a new society."));
```

This shows a variety of ways you can use to create Xml. Just note that `NewElement` is only available on an XmlDocument, whereas `Add` is available on any XmlNode.

**Result:**

```xml
<Catalog>
  <book id="bk101">
    <author>Gambardella, Matthew</author>
    <title>XML Developer's Guide</title>
    <genre>Computer</genre>
    <price>44.95</price>
    <publish_date>2000-10-01</publish_date>
    <description>An in-depth look at creating applications with XML.</description>
  </book>
  <book id="bk102">
    <author>Ralls, Kim</author>
    <title>Midnight Rain</title>
    <genre>Fantasy</genre>
    <price>5.95</price>
    <publish_date>2000-12-16</publish_date>
    <description>A former architect battles corporate zombies, an evil sorceress, and her own childhood to become queen of the world.</description>
  </book>
  <book id="bk103">
    <author>Corets, Eva</author>
    <title>Maeve Ascendant</title>
    <genre>Fantasy</genre>
    <price>5.95</price>
    <publish_date>2000-11-17</publish_date>
    <description>After the collapse of a nanotechnology society in England, the young survivors lay the foundation for a new society.</description>
  </book>
</Catalog>
```

### Iterating over Xml

Spitting out all of the element names from the XmlDocument created above.

```csharp
doc.Descendants(XmlNodeType.Element).Select(node => node.Name);
```

**Results:**

```
author 
title 
genre 
price 
publish_date 
description 
book 
author 
title 
genre 
price 
publish_date 
description 
book 
author 
title 
genre 
price 
publish_date 
description 
book 
Catalog 
```

What if I wanted names and text?

```csharp
doc.Descendants(XmlNodeType.Element, XmlNodeType.Text)
   .Select(node => node.NodeType == XmlNodeType.Text ? node.Value : node.Name);
```

**Results (Abbreviated):**

```
Gambardella, Matthew 
author 
XML Developer's Guide 
title 
Computer 
genre 
44.95 
price 
2000-10-01 
publish_date 
An in-depth look at creating applications with XML. 
description 
book 
Ralls, Kim 
author 
Midnight Rain 
...
```


What if I wanted to remove all `publish_date` elements?

```csharp
(from node in doc.Descendants(XmlNodeType.Element)
 where node.Name == "publish_date"
 select node).Remove();
```

**Results:**

```xml
<Catalog>
  <book id="bk101">
    <author>Gambardella, Matthew</author>
    <title>XML Developer's Guide</title>
    <genre>Computer</genre>
    <price>44.95</price>
    <description>An in-depth look at creating applications with XML.</description>
  </book>
  <book id="bk102">
    <author>Ralls, Kim</author>
    <title>Midnight Rain</title>
    <genre>Fantasy</genre>
    <price>5.95</price>
    <description>A former architect battles corporate zombies, an evil sorceress, and her own childhood to become queen of the world.</description>
  </book>
  <book id="bk103">
    <author>Corets, Eva</author>
    <title>Maeve Ascendant</title>
    <genre>Fantasy</genre>
    <price>5.95</price>
    <description>After the collapse of a nanotechnology society in England, the young survivors lay the foundation for a new society.</description>
  </book>
</Catalog>
```

### Using XPath

Now, let's try to find only books whose price is less than $10.00

```csharp
var nodes = from node in doc.XPathSelectMany("/Catalog/book")
            where double.Parse(node["price"].InnerText) < 10
            select node;
```

```xml
<book id="bk102">
   <author>Ralls, Kim</author>
   <title>Midnight Rain</title>
   <genre>Fantasy</genre>
   <price>5.95</price>
   <publish_date>2000-12-16</publish_date>
   <description>A former architect battles corporate zombies, an evil sorceress, and her own childhood to become queen of the world.</description>
</book> 
﻿<book id="bk103">
   <author>Corets, Eva</author>
   <title>Maeve Ascendant</title>
   <genre>Fantasy</genre>
   <price>5.95</price>
   <publish_date>2000-11-17</publish_date>
   <description>After the collapse of a nanotechnology society in England, the young survivors lay the foundation for a new society.</description>
</book> 
```

#### XPath With Arguments

Let's take the same example, but let the XPath engine handle our filtering. The syntax for an argument is the `$` symbol with any alphanumeric characters after it. The regular expression is probably something like: `$[a-zA-Z0-9]*`. However, this also ignores `$` embedded in quotes.

```csharp
var nodes = doc.XPathSelectMany("/Catalog/book[price < $value]", "10");
```

**Results:**

```xml
<book id="bk102">
   <author>Ralls, Kim</author>
   <title>Midnight Rain</title>
   <genre>Fantasy</genre>
   <price>5.95</price>
   <publish_date>2000-12-16</publish_date>
   <description>A former architect battles corporate zombies, an evil sorceress, and her own childhood to become queen of the world.</description>
</book> 
﻿<book id="bk103">
   <author>Corets, Eva</author>
   <title>Maeve Ascendant</title>
   <genre>Fantasy</genre>
   <price>5.95</price>
   <publish_date>2000-11-17</publish_date>
   <description>After the collapse of a nanotechnology society in England, the young survivors lay the foundation for a new society.</description>
</book> 
```

Let's add one more condition to the XPath to show how the argument replacement works. This time we'll find all book(s) with a price less than $10 and a publish date before 2000-12-01.

```csharp

// In XPath 1.0 we can't work directly with dates, but given the format we can use some trickeration to get around it
var nodes = doc.XPathSelectMany("/Catalog/book[price < $value and number(translate(publish_date, '-', '')) < $date]", 
                  "10", "20001201");
```


**Result:**

```xml
<book id="bk103">
   <author>Corets, Eva</author>
   <title>Maeve Ascendant</title>
   <genre>Fantasy</genre>
   <price>5.95</price>
   <publish_date>2000-11-17</publish_date>
   <description>After the collapse of a nanotechnology society in England, the young survivors lay the foundation for a new society.</description>
</book>
```