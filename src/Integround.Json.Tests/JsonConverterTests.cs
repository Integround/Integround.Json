using System;
using System.Xml;
using Xunit;
using Xunit.Extensions;

namespace Integround.Json.Tests
{
    public class JsonConverterTests
    {
        #region Test Json to Xml to Json conversion

        [Theory,
        // Test empty elements:
        InlineData(
            "{}",
            "<Json xmlns:json=\"http://www.integround.com/json\" json:ValueType=\"Object\" />"),
        InlineData(
            "[]",
            "<Json xmlns:json=\"http://www.integround.com/json\" json:ValueType=\"Array\" />"),
        InlineData(
            "{\"Prop1\":[],\"Prop2\":{}}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Prop1 json:ValueType=\"Array\" /><Prop2 json:ValueType=\"Object\" /></Json>"),
        InlineData(
            "[[],{}]",
            "<Json xmlns:json=\"http://www.integround.com/json\" json:ValueType=\"Array\"><Value json:ValueType=\"Array\" /><Value json:ValueType=\"Object\" /></Json>"),

        // Test string values:
        InlineData(
            "{\"Prop1\":\"Contents1\",\"Prop2\":\"Contents2\",\"Prop3\":\"\"}",
            "<Json><Prop1>Contents1</Prop1><Prop2>Contents2</Prop2><Prop3></Prop3></Json>"),
        InlineData(
            "[\"Contents1\",\"Contents2\"]",
             "<Json xmlns:json=\"http://www.integround.com/json\" json:ValueType=\"Array\"><Value>Contents1</Value><Value>Contents2</Value></Json>"),

        // Test numbers:
        InlineData(
            "{\"P1\":0,\"P2\":10,\"P3\":-20,\"P4\":0.0,\"P5\":10.0,\"P6\":-20.0,\"P7\":0.01,\"P8\":10e10,\"P9\":-0.2e12,\"P10\":10E-10,\"P11\":-0.2E-12}",
            "<Json xmlns:json=\"http://www.integround.com/json\">" +
            "<P1 json:ValueType=\"Number\">0</P1>" +
            "<P2 json:ValueType=\"Number\">10</P2>" +
            "<P3 json:ValueType=\"Number\">-20</P3>" +
            "<P4 json:ValueType=\"Number\">0.0</P4>" +
            "<P5 json:ValueType=\"Number\">10.0</P5>" +
            "<P6 json:ValueType=\"Number\">-20.0</P6>" +
            "<P7 json:ValueType=\"Number\">0.01</P7>" +
            "<P8 json:ValueType=\"Number\">10e10</P8>" +
            "<P9 json:ValueType=\"Number\">-0.2e12</P9>" +
            "<P10 json:ValueType=\"Number\">10E-10</P10>" +
            "<P11 json:ValueType=\"Number\">-0.2E-12</P11>" + "</Json>"),

        // Test boolean:
        InlineData(
            "{\"Prop1\":true,\"Prop2\":false}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Prop1 json:ValueType=\"Boolean\">true</Prop1><Prop2 json:ValueType=\"Boolean\">false</Prop2></Json>"),
        InlineData(
            "[true,false]",
             "<Json xmlns:json=\"http://www.integround.com/json\" json:ValueType=\"Array\"><Value json:ValueType=\"Boolean\">true</Value><Value json:ValueType=\"Boolean\">false</Value></Json>"),

        // Test null:
        InlineData(
            "{\"Prop1\":null}",
            "<Json><Prop1 /></Json>"),
        InlineData(
            "[null]",
             "<Json xmlns:json=\"http://www.integround.com/json\" json:ValueType=\"Array\"><Value /></Json>"),

        // Test attributes:
        InlineData(
            "{\"@attribute\":\"attr\",\"@attribute2\":\"\"}",
            "<Json attribute=\"attr\" attribute2=\"\" />"),

        // Tests with namespaces:
        InlineData(
            "{\"@xmlns\":\"http://www.test.com/\",\"@attribute\":\"huuhaa\",\"@attribute2\":\"huuhaa2\",\"Element\":null}",
            "<Json xmlns=\"http://www.test.com/\" attribute=\"huuhaa\" attribute2=\"huuhaa2\"><Element /></Json>"),

        // Test special character escaping:
        InlineData(
            "{\"Prop\":\" \\\" \\n \\t \\b \\f \\r \\\\ \"}",
            "<Json><Prop> \" \n \t &#x8; &#xC; \r \\ </Prop></Json>"),

        // Test XML character escaping:
        InlineData(
            "{\"@attribute\":\" > < & ' \\\" \",\"Root\":\" > < & ' \\\" \"}",
            "<Json attribute=\" &gt; &lt; &amp; ' &quot; \"><Root> &gt; &lt; &amp; ' \" </Root></Json>"),
        ]
        public void TestJsonToXmlToJson(string json, string xml)
        {
            // First convert to Xml:
            var xmlOutput = JsonConverter.ConvertToXml(json);
            Assert.Equal(xml, xmlOutput.InnerXml);

            // Then convert back to Json:
            var jsonOutput = JsonConverter.ConvertFromXml(xmlOutput.DocumentElement);
            Assert.Equal(json, jsonOutput);
        }

        #endregion

        #region Test Xml to Json conversion

        [Theory,
        InlineData(
            "<Root/>",
            "null"),
        InlineData(
            "<Root>Contents</Root>",
            "\"Contents\""),

        // Test the nullable attribute:
        InlineData(
            "<Root xmlns:json=\"http://www.integround.com/json\"><E1/><E2 json:Nullable=\"false\"/><E3 json:Nullable=\"false\"></E3><E4 json:Nullable=\"true\"/><E5 json:Nullable=\"true\"></E5></Root>",
            "{\"E1\":null,\"E2\":\"\",\"E3\":\"\",\"E4\":null,\"E5\":null}"),

        // Array tests
            // Elements with the same name should be converted to an array:
        InlineData(
            "<Root><P1>10</P1><P1>20</P1><P1><E1>10</E1><E2/></P1></Root>",
            "{\"P1\":[\"10\",\"20\",{\"E1\":\"10\",\"E2\":null}]}"),
        InlineData(
            "<Root><Prop><P1>10</P1><P1>20</P1><P1>30</P1></Prop></Root>",
            "{\"Prop\":{\"P1\":[\"10\",\"20\",\"30\"]}}"),
        ]
        public void TestXmlToJson(string input, string expected)
        {
            var xml = new XmlDocument();
            xml.LoadXml(input);
            var json = JsonConverter.ConvertFromXml(xml.DocumentElement);

            Assert.Equal(expected, json);
        }

        #endregion

        #region Test Json to Xml conversion

        [Theory,

        // Test whitespaces:
        InlineData(
            " \t \n \r { \"E1\" : \"  Contents  Contents  \" \t , \"E2\" :  {  }  , \"E3\" :  [  ] , \"E4\" : [ \"10\" , \"20\" ] } ",
            "<Json xmlns:json=\"http://www.integround.com/json\"><E1>  Contents  Contents  </E1><E2 json:ValueType=\"Object\" /><E3 json:ValueType=\"Array\" /><E4 json:ValueType=\"Array\"><Value>10</Value><Value>20</Value></E4></Json>"),

        // Test special character escaping:
        InlineData(
            "{\"Prop\":\"Content \\/ \\u0054 \"}",
            "<Json><Prop>Content / T </Prop></Json>")
        ]
        public void TestJsonToXml(string input, string expected)
        {
            var xml = JsonConverter.ConvertToXml(input);

            Assert.Equal(expected, xml.InnerXml);
        }

        #endregion

        #region Test invalid Json inputs

        [Theory,
        InlineData(
            "abc",
            "Invalid JSON. Expecting '{' or '[', found 'a'."),

        // Test invalid objects:
        InlineData(
            "{",
            "Invalid JSON. Expected characters: '\"', found EOF."),
        InlineData(
            "{ ab",
            "Invalid JSON. Expected characters: '\"', found 'a'."),
        InlineData(
            "{\"dfg",
            "Invalid JSON. Unexpected EOF was detected. Expected '\"'."),
        InlineData(
            "{\"\"",
            "Invalid JSON. Property name cannot be empty."),
        InlineData(
            "{\"prop\"",
            "Invalid JSON. Expected characters: ':', found EOF."),
        InlineData(
            "{\"prop\":",
            "Invalid JSON. Unexpected EOF was detected."),
        InlineData(
            "{\"prop\":,",
            "Invalid JSON. Expected a boolean, numeric or null value, found ''."),
        InlineData(
            "{\"prop\":}",
            "Invalid JSON. Expected a boolean, numeric or null value, found ''."),
        InlineData(
            "{\"prop\":]",
            "Invalid JSON. Unexpected EOF detected. Expected a boolean, numeric or null value."),
        InlineData(
            "{\"prop\":true]",
            "Invalid JSON. Expected characters: ',', '}', found ']'."),
        InlineData(
            "{\"prop\": a",
            "Invalid JSON. Unexpected EOF detected. Expected a boolean, numeric or null value."),
        InlineData(
            "{\"prop\": 123.4",
            "Invalid JSON. Expected characters: ',', '}', found EOF."),
        InlineData(
            "{\"prop\": abc}",
            "Invalid JSON. Expected a boolean, numeric or null value, found 'abc'."),
        InlineData(
            "{\"pr operty \": true}",
            "Invalid JSON. Property name cannot contain whitespace ('pr operty ')."),

        // Test invalid number:
        InlineData(
            "{\"prop\":.0}",
            "Invalid JSON. Expected a boolean, numeric or null value, found '.0'."),
        InlineData(
            "{\"prop\":0.}",
            "Invalid JSON. Expected a boolean, numeric or null value, found '0.'."),
        InlineData(
            "{\"prop\":-.1}",
            "Invalid JSON. Expected a boolean, numeric or null value, found '-.1'."),

        // Test invalid arrays:
        InlineData(
            "[",
            "Invalid JSON. Unexpected EOF was detected."),
        InlineData(
            "[ abc",
            "Invalid JSON. Unexpected EOF detected. Expected a boolean, numeric or null value."),
        InlineData(
            "[ true",
            "Invalid JSON. Expected characters: ',', ']', found EOF."),
        InlineData(
            "[ \"\"",
            "Invalid JSON. Expected characters: ',', ']', found EOF."),
        InlineData(
            "[ 12",
            "Invalid JSON. Expected characters: ',', ']', found EOF."),
        InlineData(
            "[ 12 45",
            "Invalid JSON. Expected characters: ',', ']', found '4'."),
        InlineData(
            "[ 12. 45",
            "Invalid JSON. Expected a boolean, numeric or null value, found '12.'."),
        InlineData(
            "[ \"",
            "Invalid JSON. Unexpected EOF was detected. Expected '\"'."),
        InlineData(
            "[   {   ",
            "Invalid JSON. Expected characters: '\"', found EOF."),

        // Test invalid escaping:
        InlineData(
             "{\"Prop\": \"Value\\String\"}",
            "Invalid JSON. \\S is not a known special character."),
        InlineData(
             "{\"Prop\": \"\\\"}",
            "Invalid JSON. Unexpected EOF was detected. Expected '\"'."),
        InlineData(
             "{\"Prop\": \"\\u\" }",
            "Invalid JSON. Unexpected EOF detected. Expected '\\u' to be followed by four hex digits."),
        InlineData(
             "{\"Prop\": \"\\uaaas\" }",
            "Invalid JSON. Expected '\\u' to be followed by four hex digits, found 'aaas'."),

        // Check invalid characters in property names:
        InlineData(
             "{\"Prop<\": \"Value\" }",
            "Constructing the XML was unsuccessful: The '<' character, hexadecimal value 0x3C, cannot be included in a name.")
        ]
        public void TestJsonToXmlError(string input, string expectedError)
        {
            try
            {
                JsonConverter.ConvertToXml(input);
                Assert.True(false, "JsonToXml should throw an exception.");
            }
            catch (Exception ex)
            {
                Assert.Equal(expectedError, ex.Message);
            }
        }

        #endregion
    }
}
