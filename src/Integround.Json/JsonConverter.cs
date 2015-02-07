using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Integround.Json
{
    public class JsonConverter
    {
        protected class FormatAttributes
        {
            public const string Namespace = "http://www.integround.com/json";
            public const string NullValue = "null";
            public const string Nullable = "Nullable";
            public const string DataType = "DataType";
            public const string DataTypeArray = "Array";
            public const string DataTypeNumber = "Number";
            public const string DataTypeBoolean = "Boolean";
        }

        public static string ConvertFromXml(XmlElement xml)
        {
            var stringBuilder = new StringBuilder();
            WriteJsonObject(xml, stringBuilder, false, true);

            return stringBuilder.ToString();
        }

        public static XmlDocument ConvertToXml(string json)
        {
            var xml = new XmlDocument();
            var root = xml.CreateElement("Json");
            xml.AppendChild(root);
            using (var reader = new StringReader(json))
            {
                ReadJsonObject(reader, root, xml, true);
            }
            return xml;
        }

        #region JSON -> XML helper methods

        private static char PeekNextChar(TextReader reader)
        {
            char nextChar;
            while (char.IsWhiteSpace(nextChar = (char)reader.Peek()))
            {
                // Clear the white space   
                reader.Read();
            }
            return nextChar;
        }

        private static char ReadNextChar(TextReader reader)
        {
            char nextChar;
            while (char.IsWhiteSpace(nextChar = (char)reader.Read()))
            {
                // Clear the white space   
            }

            return nextChar;
        }

        private static char ReadChar(TextReader reader, char allowedChar)
        {
            var nextChar = ReadNextChar(reader);
            if (nextChar != allowedChar)
                throw new Exception(string.Format("Invalid JSON. Expecting '{0}', found '{1}'.", allowedChar, nextChar));

            return nextChar;
        }

        private static char ReadChar(TextReader reader, char[] allowedChar)
        {
            var nextChar = ReadNextChar(reader);
            if (!allowedChar.Contains(nextChar))
                throw new Exception(string.Format("Invalid JSON. Expecting '{0}', found '{1}'.", string.Join(",", allowedChar), nextChar));

            return nextChar;
        }

        private static string ReadUntil(TextReader reader, char[] delimiters)
        {
            var str = "";
            char readChar;

            while (char.IsWhiteSpace(readChar = (char)reader.Peek()))
            {
                // Clear the white space 
                reader.Read();
            }

            // Read the string before the delimiter char:
            while (!delimiters.Contains(readChar))
            {
                str += (char)reader.Read();
                readChar = (char)reader.Peek();
            }

            return str;
        }

        private static void ReadSingleElement(StringReader reader, XmlNode node, XmlDocument xml)
        {
            var nextChar = PeekNextChar(reader);
            if (nextChar == '{')
            {
                // Another object
                ReadJsonObject(reader, node, xml);
            }
            else if (nextChar == '[')
            {
                // Another object
                ReadJsonObject(reader, node, xml, true);
            }
            else if (nextChar == '"') // string value if expected:
            {
                // Read the starting quote character:
                ReadChar(reader, '"');
                // Read the string value:
                node.InnerText = ReadUntil(reader, new[] { '"' }).TrimEnd();
                // Read the ending quote:
                ReadChar(reader, '"');
            }
            else
            {
                // Read the null, number or boolean value:
                var value = ReadUntil(reader, new[] { ',', ']', '}' }).TrimEnd();

                // Null values are not added to XML:
                if (!string.Equals(value, FormatAttributes.NullValue, StringComparison.InvariantCultureIgnoreCase))
                    node.InnerText = value;
            }
        }
        
        private static void ReadJsonObject(StringReader reader, XmlNode parent, XmlDocument xml, bool isRoot = false)
        {
            var nextChar = PeekNextChar(reader);
            if(nextChar != '{' && nextChar != '[')
                throw new Exception(string.Format("Invalid JSON. Expecting '{{' or '[', found '{0}'.", nextChar));

            // Check if the json starts with an array:
            var isArray = isRoot && nextChar == '[';
            if (!isArray)
            {
                // Read the object-starting curly bracket:
                ReadChar(reader, '{');

                // Check if the object is empty:
                nextChar = PeekNextChar(reader);
                if (nextChar == '}')
                {
                    ReadChar(reader, '}');
                    return;
                }
            }

            while (true)
            {
                // If the json value is an array, do not read the property name but use 'Value' instead
                var propertyName = "Value";
                if (!isArray)
                {
                    // *********************************************
                    // Read the property name
                    // *********************************************
                    ReadChar(reader, '"');
                    propertyName = ReadUntil(reader, new[] {'"'});
                    ReadChar(reader, '"');

                    // *********************************************
                    // Read the colon:
                    // *********************************************
                    ReadChar(reader, ':');
                }

                // *********************************************
                // Read the value:
                // *********************************************
                nextChar = PeekNextChar(reader);
                if (nextChar == '[') // Array
                {
                    ReadChar(reader, '[');

                    // Check if the array is empty:
                    nextChar = PeekNextChar(reader);
                    if (nextChar == ']')
                    {
                        ReadChar(reader, ']');
                    }
                    else
                    {
                        // Read the array items:
                        while (true)
                        {
                            var node = xml.CreateElement(propertyName);
                            ReadSingleElement(reader, node, xml);
                            parent.AppendChild(node);

                            // Read the array item separator:
                            nextChar = ReadChar(reader, new[] {',', ']'});
                            if (nextChar == ']')
                                break;
                        }
                    }
                }
                else
                {
                    // Create a new xml node:
                    XmlNode node;
                    if (propertyName.StartsWith("@"))
                        node = xml.CreateAttribute(propertyName.Substring(1));
                    else
                        node = xml.CreateElement(propertyName);

                    // Read the contents:
                    ReadSingleElement(reader, node, xml);

                    // Add the xml node:
                    if (node.NodeType == XmlNodeType.Attribute)
                    {
                        parent.Attributes.Append((XmlAttribute)node);
                    }
                    else
                    {
                        parent.AppendChild(node);
                    }
                }

                // *********************************************
                // Read the object ending character:
                // *********************************************
                if (isArray)
                    break;

                nextChar = ReadChar(reader, new[] { ',', '}' });
                if (nextChar == '}')
                {
                    break;
                }
            }
        }

        #endregion

        #region XML -> JSON helper methods

        private static void WriteJsonObject(XmlNode node, StringBuilder stringBuilder, bool isArray, bool isRoot = false)
        {
            var attributes = node.Attributes.Cast<XmlNode>().ToList();

            // Get the format attributes:
            var formatAttributes = attributes.Where(n =>
                (n.NamespaceURI == FormatAttributes.Namespace)).ToList();
            var dataType = formatAttributes.Where(a => a.LocalName == FormatAttributes.DataType).Select(a => a.Value).FirstOrDefault();

            // Get the data attributes & elements:
            var children = attributes.Where(n =>
                (n.Value != FormatAttributes.Namespace) &&
                (n.NamespaceURI != FormatAttributes.Namespace)).ToList();
            children.AddRange(node.ChildNodes.Cast<XmlNode>());

            // Omit the element name if this is an array item or the root object:
            if (!isArray && !isRoot)
                stringBuilder.AppendFormat("\"{0}\":", node.LocalName);

            //***********************************
            // Write the element contents:
            //***********************************

            var contentStringBuilder = new StringBuilder();

            // Empty node:
            if (!children.Any())
            {
                var nullable =
                    formatAttributes.Where(a => (a.LocalName == FormatAttributes.Nullable))
                        .Select(a => a.Value)
                        .FirstOrDefault();

                // If json:Nullable != false (default = true), write the null value.
                if (!string.Equals(nullable, "false", StringComparison.InvariantCultureIgnoreCase))
                    stringBuilder.Append(FormatAttributes.NullValue);

                // Else if the data type is not Number/Boolean, write just the quotes.
                else if ((dataType != FormatAttributes.DataTypeNumber) && (dataType != FormatAttributes.DataTypeBoolean))
                    stringBuilder.Append("\"\"");
            }
            // If all child elements are forced to an array:
            else if (dataType == FormatAttributes.DataTypeArray)
            {
                WriteJsonElements(children, dataType, contentStringBuilder, true);
                stringBuilder.AppendFormat("[{0}]", contentStringBuilder);
            }
            // If the child elements have the same name, put the values into an array:
            else if (children.Count > 1
                && children.All(n => n.NodeType == XmlNodeType.Element)
                && children.Skip(1).All(n => n.LocalName.Equals(children[0].LocalName)))
            {
                WriteJsonElements(children, dataType, contentStringBuilder, true);
                stringBuilder.AppendFormat("{{\"{0}\":[{1}]}}", children[0].LocalName, contentStringBuilder);
            }
            else
            {
                WriteJsonElements(children, dataType, contentStringBuilder, false);

                // If this was a complex element, add curly brackets:
                if (children.Count > 1 ||
                    children.First().NodeType != XmlNodeType.Text)
                    stringBuilder.AppendFormat("{{{0}}}", contentStringBuilder);
                else
                    stringBuilder.Append(contentStringBuilder);
            }
        }

        private static void WriteJsonElements(IReadOnlyList<XmlNode> children, string dataType, StringBuilder stringBuilder, bool isArray)
        {
            for (var i = 0; i < children.Count; i++)
            {
                switch (children[i].NodeType)
                {
                    case XmlNodeType.Attribute:
                        stringBuilder.AppendFormat("\"@{0}\":\"{1}\"", children[i].LocalName, children[i].InnerText);
                        break;
                    case XmlNodeType.Text:

                        if ((dataType == FormatAttributes.DataTypeBoolean) ||
                            (dataType == FormatAttributes.DataTypeNumber))
                            stringBuilder.Append(children[i].InnerText);
                        else
                            stringBuilder.AppendFormat("\"{0}\"", children[i].InnerText);
                        break;
                    case XmlNodeType.Element:
                        WriteJsonObject(children[i], stringBuilder, isArray);
                        break;
                }

                // If this was not the last child, add ',':
                if (i != children.Count - 1)
                {
                    stringBuilder.Append(",");
                }
            }
        }

        #endregion
    }
}