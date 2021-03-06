﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Integround.Json.Enums;
using Integround.Json.Models;

namespace Integround.Json
{
    public class JsonConverter
    {
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

        private static int PeekNextChar(TextReader reader, bool ignoreWhiteSpace = true)
        {
            var nextChar = reader.Peek();

            // Read & ignore characters until a non-whitespace character is found 
            // or the end of file if detected: 
            while (ignoreWhiteSpace && (nextChar != -1) && char.IsWhiteSpace((char)nextChar))
            {
                reader.Read();
                nextChar = reader.Peek();
            }

            return nextChar;
        }

        private static int ReadNextChar(TextReader reader, bool ignoreWhiteSpace = false)
        {
            var nextChar = reader.Read();

            // Read characters until a non-whitespace character is found
            // or the end of file if detected: 
            while (ignoreWhiteSpace && (nextChar != -1) && char.IsWhiteSpace((char)nextChar))
            {
                nextChar = reader.Read();
            }

            return nextChar;
        }

        private static char ReadChar(TextReader reader, char allowedChar, bool ignoreWhiteSpace = true)
        {
            return ReadChar(reader, new[] { allowedChar }, ignoreWhiteSpace);
        }

        private static char ReadChar(TextReader reader, char[] allowedChars, bool ignoreWhiteSpace = true)
        {
            var nextCharValue = ReadNextChar(reader, ignoreWhiteSpace);

            // Check if end of file was detected:
            if (nextCharValue == -1)
            {
                var allowedCharsString = string.Join(", ", allowedChars.Select(d => string.Format("'{0}'", d)));
                throw new Exception(string.Format("Invalid JSON. Expected characters: {0}, found EOF.",
                    allowedCharsString));
            }

            // Otherwise convert to a char and handle it:
            var nextChar = (char)nextCharValue;
            if (!allowedChars.Contains(nextChar))
            {
                var allowedCharsString = string.Join(", ", allowedChars.Select(d => string.Format("'{0}'", d)));
                throw new Exception(string.Format("Invalid JSON. Expected characters: {0}, found '{1}'.",
                    allowedCharsString, nextChar));
            }

            return nextChar;
        }

        private static string ReadString(TextReader reader)
        {
            var str = new StringBuilder();
            var nextCharValue = PeekNextChar(reader, false);

            // If the first character was the escape character, read it and get the next char:
            var escapeCharDetected = (nextCharValue == '\\');
            if (escapeCharDetected)
            {
                reader.Read();
                nextCharValue = PeekNextChar(reader, false);
            }

            // Read characters until a ending quote or EOF is found:
            while (nextCharValue != -1)
            {
                // If a quote is read and it's not escaped, stop reading any further:
                if (!escapeCharDetected && (nextCharValue == '"'))
                    break;

                var character = (char)reader.Read();

                if (escapeCharDetected)
                {
                    switch (character)
                    {
                        case 'b':
                            str.Append('\b');
                            break;
                        case 'f':
                            str.Append('\f');
                            break;
                        case 'n':
                            str.Append('\n');
                            break;
                        case 'r':
                            str.Append('\r');
                            break;
                        case 't':
                            str.Append('\t');
                            break;
                        case '"':
                            str.Append(character);
                            break;
                        case '\\':
                            str.Append(character);
                            break;
                        case '/':
                            str.Append(character);
                            break;
                        case 'u':
                            // Read four hex characters:
                            var hexValue = new char[4];
                            if (reader.Read(hexValue, 0, 4) != 4)
                            {
                                throw new Exception("Invalid JSON. Unexpected EOF detected. Expected '\\u' to be followed by four hex digits.");
                            }

                            // Convert the read hex string to an integer value:
                            var hexValueString = new string(hexValue);
                            int characterValue;
                            if (!Int32.TryParse(hexValueString, NumberStyles.HexNumber, null, out characterValue))
                            {
                                throw new Exception(string.Format("Invalid JSON. Expected '\\u' to be followed by four hex digits, found '{0}'.", hexValueString));
                            }

                            str.Append((char)characterValue);
                            break;
                        default:
                            throw new Exception(string.Format("Invalid JSON. \\{0} is not a known special character.", character));
                    }
                }
                else
                {
                    str.Append(character);
                }

                // Move to the next character:
                nextCharValue = reader.Peek();

                // If the next character is the escape character, read it and get the next char:
                escapeCharDetected = (nextCharValue == '\\');
                if (escapeCharDetected)
                {
                    reader.Read();
                    nextCharValue = PeekNextChar(reader, false);
                }
            }

            // Check if end of file was detected:
            if (nextCharValue == -1)
            {
                throw new Exception("Invalid JSON. Unexpected EOF was detected. Expected '\"'.");
            }

            return str.ToString();
        }

        private static JsonElementValue ReadValue(TextReader reader, char[] delimiters)
        {
            var value = new JsonElementValue();
            var valueString = new StringBuilder();
            var nextCharValue = PeekNextChar(reader);

            // Read characters until an expected delimiter or EOF is found:
            while ((nextCharValue != -1) &&
                   !char.IsWhiteSpace((char)nextCharValue) &&
                   !delimiters.Contains((char)nextCharValue))
            {
                valueString.Append((char)reader.Read());

                // If a boolean or null value was found, stop reading any further.
                // This check should only be made if the lengths match, not after every character.
                if ((valueString.Length == bool.TrueString.Length) ||
                    (valueString.Length == bool.FalseString.Length) ||
                    (valueString.Length == JsonElementFormatAttributes.NullValue.Length))
                {
                    var valueToCheck = valueString.ToString();

                    // If the string is a boolean value:
                    if (string.Equals(valueToCheck, bool.TrueString, StringComparison.InvariantCultureIgnoreCase) ||
                        string.Equals(valueToCheck, bool.FalseString, StringComparison.InvariantCultureIgnoreCase))
                    {
                        value.Value = valueToCheck;
                        value.Type = JsonValueType.Boolean;
                        break;
                    }

                    // If the string is null:
                    if (string.Equals(valueToCheck, JsonElementFormatAttributes.NullValue,
                        StringComparison.InvariantCultureIgnoreCase))
                    {
                        value.Value = valueToCheck;
                        value.Type = JsonValueType.Null;
                        break;
                    }
                }

                nextCharValue = reader.Peek();
            }

            // Check if the string was numerical:
            // Require the numeric string to start with a minus sign '-' or a digit and end with a digit.
            float numericValue;
            var elementValue = valueString.ToString();
            if (float.TryParse(elementValue, NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue) &&
                Char.IsDigit(elementValue.Last()) &&
                (Char.IsDigit(elementValue.First()) ||
                 (valueString.Length > 1) && (valueString[0] == '-') && char.IsDigit(valueString[1])))
            {
                value.Value = elementValue;
                value.Type = JsonValueType.Numeric;
            }

            // Check if end of file was detected.
            // If the value was numeric, the error is not raised here but from the missing delimiter.
            if ((nextCharValue == -1) && (value.Type != JsonValueType.Numeric))
            {
                throw new Exception("Invalid JSON. Unexpected EOF detected. Expected a boolean, numeric or null value.");
            }

            if ((value.Type != JsonValueType.Boolean) &&
                (value.Type != JsonValueType.Null) &&
                (value.Type != JsonValueType.Numeric))
            {
                throw new Exception(string.Format("Invalid JSON. Expected a boolean, numeric or null value, found '{0}'.", valueString));
            }

            return value;
        }

        private static void ReadSingleElement(StringReader reader, XmlNode node, XmlDocument xml, char[] delimiters)
        {
            var nextChar = PeekNextChar(reader);
            if (nextChar == -1)
            {
                throw new Exception("Invalid JSON. Unexpected EOF was detected.");
            }

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
                node.InnerText = ReadString(reader);
                // Read the ending quote:
                ReadChar(reader, '"');
            }
            else
            {

                // Read the null, number or boolean value:
                var value = ReadValue(reader, delimiters);

                if ((value.Type == JsonValueType.Boolean) ||
                    (value.Type == JsonValueType.Numeric))
                {
                    // Add the value type attribute:
                    var attribute = CreateXmlAttribute(xml, node, JsonElementFormatAttributes.ValueType,
                        JsonElementFormatAttributes.Prefix, JsonElementFormatAttributes.Namespace);
                    attribute.Value = (value.Type == JsonValueType.Boolean)
                        ? JsonElementFormatAttributes.ValueTypeBoolean
                        : JsonElementFormatAttributes.ValueTypeNumber;
                }

                // Null values are not added to XML:
                if (value.Type != JsonValueType.Null)
                    node.InnerText = value.Value;
            }
        }

        private static void ReadJsonObject(StringReader reader, XmlNode parent, XmlDocument xml, bool isRoot = false)
        {
            var nextChar = PeekNextChar(reader);
            if (nextChar != '{' && nextChar != '[')
            {
                throw new Exception(string.Format("Invalid JSON. Expecting '{{' or '[', found '{0}'.", (char)nextChar));
            }

            // Check if the json starts with an array:
            var isRootArray = isRoot && nextChar == '[';
            if (!isRootArray)
            {
                // Read the object-starting curly bracket:
                ReadChar(reader, '{');

                // Check if the object is empty:
                nextChar = PeekNextChar(reader);
                if (nextChar == '}')
                {
                    // Add the value type attribute:
                    var attribute = CreateXmlAttribute(xml, parent, JsonElementFormatAttributes.ValueType,
                        JsonElementFormatAttributes.Prefix, JsonElementFormatAttributes.Namespace);
                    attribute.Value = JsonElementFormatAttributes.ValueTypeObject;

                    ReadChar(reader, '}');
                    return;
                }
            }

            while (true)
            {
                // If the json value is an array, do not read the property name but use 'Value' instead
                var propertyName = "Value";
                if (!isRootArray)
                {
                    // *********************************************
                    // Read the property name
                    // *********************************************
                    ReadChar(reader, '"');
                    propertyName = ReadString(reader);

                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        throw new Exception("Invalid JSON. Property name cannot be empty.");
                    }
                    if (propertyName.Any(Char.IsWhiteSpace))
                    {
                        throw new Exception(string.Format("Invalid JSON. Property name cannot contain whitespace ('{0}').", propertyName));
                    }

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

                    // If this is a root-level array and it is empty, add the value type attribute to the parent node:
                    if ((nextChar == ']') && isRootArray)
                    {
                        var attribute = CreateXmlAttribute(xml, parent,
                            JsonElementFormatAttributes.ValueType,
                            JsonElementFormatAttributes.Prefix,
                            JsonElementFormatAttributes.Namespace);
                        attribute.Value = JsonElementFormatAttributes.ValueTypeArray;
                    }
                    else
                    {
                        var arrayDelimiters = new[] { ',', ']' };

                        var node = parent;

                        // If this a root-level array, create a new parent node:
                        if (!isRootArray)
                            node = CreateXmlElement(xml, parent, propertyName);

                        var attribute = CreateXmlAttribute(xml, node,
                            JsonElementFormatAttributes.ValueType,
                            JsonElementFormatAttributes.Prefix,
                            JsonElementFormatAttributes.Namespace);
                        attribute.Value = JsonElementFormatAttributes.ValueTypeArray;

                        // Read the array items:
                        while (nextChar != ']')
                        {
                            // Create the XML node & add it to the document:
                            var arrayNode = CreateXmlElement(xml, node, "Value");

                            ReadSingleElement(reader, arrayNode, xml, arrayDelimiters);

                            nextChar = PeekNextChar(reader);

                            if (nextChar != ']')
                                ReadChar(reader, arrayDelimiters);
                        }
                    }

                    ReadChar(reader, ']');
                }
                else
                {
                    // Create the XML node & add it to the document:
                    var node = propertyName.StartsWith("@")
                        ? CreateXmlAttribute(xml, parent, propertyName.Substring(1))
                        : CreateXmlElement(xml, parent, propertyName);

                    // Read the node contents:
                    ReadSingleElement(reader, node, xml, new[] { ',', '}' });
                }

                // *********************************************
                // Read the object ending character:
                // *********************************************
                if (isRootArray)
                    break;

                nextChar = ReadChar(reader, new[] { ',', '}' });
                if (nextChar == '}')
                {
                    break;
                }
            }
        }

        private static XmlNode CreateXmlElement(XmlDocument xml, XmlNode parent, string name)
        {
            XmlNode node;
            try
            {
                node = xml.CreateElement(name);
                parent.AppendChild(node);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Constructing the XML was unsuccessful: {0}", ex.Message));
            }

            return node;
        }

        private static XmlNode CreateXmlAttribute(XmlDocument xml, XmlNode parent, string name, string prefix = null, string namespaceUri = null)
        {
            XmlNode node;
            try
            {
                if (!string.IsNullOrWhiteSpace(prefix) && (namespaceUri != null))
                {
                    // Add the namespace declaration to the root element:
                    xml.DocumentElement.SetAttribute(string.Format("xmlns:{0}",
                        JsonElementFormatAttributes.Prefix),
                        JsonElementFormatAttributes.Namespace);
                    node = xml.CreateAttribute(prefix, name, namespaceUri);
                }
                else
                {
                    node = xml.CreateAttribute(name);
                }

                parent.Attributes.Append((XmlAttribute)node);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Constructing the XML was unsuccessful: {0}", ex.Message));
            }

            return node;
        }

        #endregion

        #region XML -> JSON helper methods

        private static void WriteJsonObject(XmlNode node, StringBuilder stringBuilder, bool isArray, bool isRoot = false)
        {
            var attributes = node.Attributes.Cast<XmlNode>().ToList();

            // Get the format attributes:
            var formatAttributes = attributes
                .Where(n => string.Equals(n.NamespaceURI, JsonElementFormatAttributes.Namespace))
                .ToList();
            var valueType = formatAttributes
                .Where(a => string.Equals(a.LocalName, JsonElementFormatAttributes.ValueType))
                .Select(a => a.Value)
                .FirstOrDefault();

            // Get the data attributes & elements:
            var children = attributes
                .Where(n =>
                    !string.Equals(n.Value, JsonElementFormatAttributes.Namespace) &&
                    !string.Equals(n.NamespaceURI, JsonElementFormatAttributes.Namespace))
                .ToList();
            children.AddRange(node.ChildNodes.Cast<XmlNode>());

            // Omit the element name if this is an array item or the root object:
            if (!isArray && !isRoot)
                stringBuilder.AppendFormat("\"{0}\":", EscapeJsonString(node.LocalName));

            //***********************************
            // Write the element contents:
            //***********************************

            var contentStringBuilder = new StringBuilder();

            // Empty node:
            if (!children.Any())
            {
                var nullable =
                    formatAttributes.Where(a => string.Equals(a.LocalName, JsonElementFormatAttributes.Nullable))
                        .Select(a => a.Value)
                        .FirstOrDefault();

                // If this was an object, write empty object:
                if (string.Equals(valueType, JsonElementFormatAttributes.ValueTypeObject))
                    stringBuilder.Append("{}");

                // Else if this was an array, write empty array:
                else if (string.Equals(valueType, JsonElementFormatAttributes.ValueTypeArray))
                    stringBuilder.Append("[]");

                // Else if json:Nullable != false (default = true), write the null value.
                else if (!string.Equals(nullable, "false", StringComparison.InvariantCultureIgnoreCase))
                    stringBuilder.Append(JsonElementFormatAttributes.NullValue);

                // Else if the value type is not Number/Boolean, write just the quotes.
                else if (!string.Equals(valueType, JsonElementFormatAttributes.ValueTypeNumber) &&
                    !string.Equals(valueType, JsonElementFormatAttributes.ValueTypeBoolean))
                    stringBuilder.Append("\"\"");
            }

            // If all child elements are forced to an array:
            else if (string.Equals(valueType, JsonElementFormatAttributes.ValueTypeArray))
            {
                WriteJsonElements(children, valueType, contentStringBuilder, true);
                stringBuilder.AppendFormat("[{0}]", contentStringBuilder);
            }
            // If the child elements have the same name, put the values into an array:
            else if (children.Count > 1
                && children.All(n => n.NodeType == XmlNodeType.Element)
                && children.Skip(1).All(n => n.LocalName.Equals(children[0].LocalName)))
            {
                WriteJsonElements(children, valueType, contentStringBuilder, true);
                stringBuilder.AppendFormat("{{\"{0}\":[{1}]}}",
                    EscapeJsonString(children[0].LocalName),
                    contentStringBuilder);
            }
            else
            {
                WriteJsonElements(children, valueType, contentStringBuilder, false);

                // If this was a complex element, add curly brackets:
                if (children.Count > 1 ||
                    children.First().NodeType != XmlNodeType.Text)
                    stringBuilder.AppendFormat("{{{0}}}", contentStringBuilder);
                else
                    stringBuilder.Append(contentStringBuilder);
            }
        }

        private static void WriteJsonElements(IReadOnlyList<XmlNode> children, string valueType, StringBuilder stringBuilder, bool isArray)
        {
            for (var i = 0; i < children.Count; i++)
            {
                switch (children[i].NodeType)
                {
                    case XmlNodeType.Attribute:
                        stringBuilder.AppendFormat("\"@{0}\":\"{1}\"",
                            EscapeJsonString(children[i].LocalName),
                            EscapeJsonString(children[i].InnerText));
                        break;
                    case XmlNodeType.Text:

                        if (string.Equals(valueType, JsonElementFormatAttributes.ValueTypeBoolean) ||
                            string.Equals(valueType, JsonElementFormatAttributes.ValueTypeNumber))
                            stringBuilder.Append(children[i].InnerText);
                        else
                            stringBuilder.AppendFormat("\"{0}\"", EscapeJsonString(children[i].InnerText));
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

        private static string EscapeJsonString(string input)
        {
            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .Replace("\b", "\\b");
        }
        #endregion
    }
}