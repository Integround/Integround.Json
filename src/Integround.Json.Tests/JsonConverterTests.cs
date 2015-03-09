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
        InlineData(
            "{\"Root\":null}",
            "<Json><Root /></Json>"),
        InlineData(
            "{\"Root\":\"Contents\"}",
            "<Json><Root>Contents</Root></Json>"),
        InlineData(
            "{\"Element1\":\"string\",\"Element2\":23,\"Element3\":null,\"Element4\":true}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Element1>string</Element1><Element2 json:DataType=\"Number\">23</Element2><Element3 /><Element4 json:DataType=\"Boolean\">true</Element4></Json>"),
        InlineData(
            "{\"Element1\":\"\",\"Element2\":null}",
            "<Json><Element1></Element1><Element2 /></Json>"),

        // Array tests:
        InlineData(
            "[]",
            "<Json xmlns:json=\"http://www.integround.com/json\" json:DataType=\"Array\" />"),
        InlineData(
            "[10,\"20\"]",
            "<Json xmlns:json=\"http://www.integround.com/json\" json:DataType=\"Array\"><Value json:DataType=\"Number\">10</Value><Value>20</Value></Json>"),
        InlineData(
            "{\"Prop\":[]}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Prop json:DataType=\"Array\" /></Json>"),
        InlineData(
            "{\"Prop\":[\"10\",20,null,\"null\",true,false,[]]}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Prop json:DataType=\"Array\"><Value>10</Value><Value json:DataType=\"Number\">20</Value><Value /><Value>null</Value><Value json:DataType=\"Boolean\">true</Value><Value json:DataType=\"Boolean\">false</Value><Value json:DataType=\"Array\" /></Prop></Json>"),
        InlineData(
            "{\"Prop\":[{\"Element1\":\"10\",\"Element2\":null},\"20\",\"30\"]}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Prop json:DataType=\"Array\"><Value><Element1>10</Element1><Element2 /></Value><Value>20</Value><Value>30</Value></Prop></Json>"),

        // Attribute tests:
        InlineData(
            "{\"@attribute\":\"huuhaa\"}",
            "<Json attribute=\"huuhaa\" />"),
        InlineData(
            "{\"@attribute\":\"huuhaa\",\"@attribute2\":\"huuhaa2\",\"Element1\":\"Contents\",\"Element2\":null}",
            "<Json attribute=\"huuhaa\" attribute2=\"huuhaa2\"><Element1>Contents</Element1><Element2 /></Json>"),

        // Tests with namespaces:
        InlineData(
            "{\"@xmlns\":\"http://www.test.com/\",\"@attribute\":\"huuhaa\",\"@attribute2\":\"huuhaa2\",\"Element\":null}",
            "<Json xmlns=\"http://www.test.com/\" attribute=\"huuhaa\" attribute2=\"huuhaa2\"><Element /></Json>"),

        // Test special character escaping:
        InlineData(
            "{\"Prop\":[\"Content \\\"String\\\"\",\"Multiline\\nString\\tValue\",\"Others: \\b \\f \\r \\\\ \"]}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Prop json:DataType=\"Array\"><Value>Content \"String\"</Value><Value>Multiline\nString\tValue</Value><Value>Others: &#x8; &#xC; \r \\ </Value></Prop></Json>"),

        // Test XML character escaping:
        InlineData(
            "{\"@attribute\":\"><&'\\\"\",\"Root\":\"><&'\\\"\"}",
            "<Json attribute=\"&gt;&lt;&amp;'&quot;\"><Root>&gt;&lt;&amp;'\"</Root></Json>"),

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
            "<Root xmlns:json=\"http://www.integround.com/json\"><Element1/><Element2 json:Nullable=\"false\"/><Element3 json:Nullable=\"false\"></Element3></Root>",
            "{\"Element1\":null,\"Element2\":\"\",\"Element3\":\"\"}"),

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

        // Test empty objects:
        InlineData(
            "{}",
            "<Json />"),
        InlineData(
            "{\"Prop\":[{},\"10\",null,[]]}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Prop json:DataType=\"Array\"><Value /><Value>10</Value><Value /><Value json:DataType=\"Array\" /></Prop></Json>"),

        // Test whitespaces:
        InlineData(
            " \t \n \r { \"Element1\" : \"  Contents  Contents  \" \t , \"Element2\" :  {  }  , \"Element3\" :  [  ] , \"Element4\" : [ \"10\" , \"20\" ] } ",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Element1>  Contents  Contents  </Element1><Element2 /><Element3 json:DataType=\"Array\" /><Element4 json:DataType=\"Array\"><Value>10</Value><Value>20</Value></Element4></Json>"),

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
            "Invalid JSON. Unexpected EOF detected. Expected a boolean, numeric or null value."),
        InlineData(
            "{\"prop\": abc}",
            "Invalid JSON. Expected a boolean, numeric or null value, found 'abc'."),
        InlineData(
            "{\"pr operty \": true}",
            "Invalid JSON. Property name cannot contain whitespace ('pr operty ')."),

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
