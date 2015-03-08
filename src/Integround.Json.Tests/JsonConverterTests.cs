using System;
using System.Xml;
using Xunit;
using Xunit.Extensions;

namespace Integround.Json.Tests
{
    public class JsonConverterTests
    {
        [Theory,
        InlineData(
            "<Root/>",
            "null"),
        InlineData(
            "<Root>Contents</Root>",
            "\"Contents\""),
        InlineData(
            "<Root><Element/></Root>",
            "{\"Element\":null}"),
        InlineData(
            "<Root xmlns:json=\"http://www.integround.com/json\"><Element/><Element2/><Element3 json:Nullable=\"false\"/></Root>",
            "{\"Element\":null,\"Element2\":null,\"Element3\":\"\"}"),
        InlineData(
            "<Root><Element>Contents</Element><Element2>Contents2</Element2></Root>",
            "{\"Element\":\"Contents\",\"Element2\":\"Contents2\"}"),

        // Array tests:
        InlineData(
            "<Root><Prop>10</Prop><Prop>20</Prop><Prop>30</Prop></Root>",
            "{\"Prop\":[\"10\",\"20\",\"30\"]}"),
        InlineData(
            "<Root><P/><P>null</P><P>30</P></Root>",
            "{\"P\":[null,\"null\",\"30\"]}"),
        InlineData(
            "<Root><Prop><Element1>10</Element1><Element2/></Prop><Prop>20</Prop><Prop>30</Prop></Root>",
            "{\"Prop\":[{\"Element1\":\"10\",\"Element2\":null},\"20\",\"30\"]}"),
        InlineData( // Force the root element to be an array:
            "<Root xmlns:json=\"http://www.integround.com/json\" json:DataType=\"Array\"><P1>10</P1><P2>20</P2><P3>30</P3></Root>",
            "[\"10\",\"20\",\"30\"]"),
        InlineData( // Force the root element to be an array:
            "<Root xmlns:json=\"http://www.integround.com/json\" json:DataType=\"Array\"><P1><E1>10</E1><E2/></P1><P2>20</P2><P3>30</P3></Root>",
            "[{\"E1\":\"10\",\"E2\":null},\"20\",\"30\"]"),
        InlineData( // Force the child element to be an array:
            "<Root xmlns:json=\"http://www.integround.com/json\"><Prop json:DataType=\"Array\"><P1>10</P1><P2>20</P2><P3>30</P3></Prop></Root>",
            "{\"Prop\":[\"10\",\"20\",\"30\"]}"),

        // Attribute tests:
        InlineData(
            "<Root attribute=\"huuhaa\"/>",
            "{\"@attribute\":\"huuhaa\"}"),
        InlineData(
            "<Root attribute=\"huuhaa\" attribute2=\"huuhaa2\"><Element/></Root>",
            "{\"@attribute\":\"huuhaa\",\"@attribute2\":\"huuhaa2\",\"Element\":null}"),

        // Tests with namespaces:
        InlineData(
            "<Root xmlns=\"http://www.test.com/\" attribute=\"huuhaa\" attribute2=\"huuhaa2\"><Element/></Root>",
            "{\"@xmlns\":\"http://www.test.com/\",\"@attribute\":\"huuhaa\",\"@attribute2\":\"huuhaa2\",\"Element\":null}"),

        // Tests with data types:
        InlineData(
            "<Root xmlns:json=\"http://www.integround.com/json\">" +
            "<E1>Contents</E1>" +
            "<E2 json:DataType=\"String\">Contents2</E2>" +
            "<E3 json:DataType=\"Number\">13</E3>" +
            "<E4 json:DataType=\"Boolean\">true</E4>" +
            "</Root>",
            "{\"E1\":\"Contents\",\"E2\":\"Contents2\",\"E3\":13,\"E4\":true}")
        ]
        public void TestXmlToJson(string input, string expected)
        {
            var xml = new XmlDocument();
            xml.LoadXml(input);
            var json = JsonConverter.ConvertFromXml(xml.DocumentElement);

            Assert.Equal(expected, json);
        }

        [Theory,
        InlineData(
            "{}",
            "<Json />"),
        InlineData(
            "  {  }  ",
            "<Json />"),
        InlineData(
            "{\"Root\":\"Contents\"}",
            "<Json><Root>Contents</Root></Json>"),
        InlineData(
            " { \"Root\" : \"  Contents  Contents  \" } ",
            "<Json><Root>  Contents  Contents  </Root></Json>"),
        InlineData(
            "{\"Element1\":\"string\",\"Element2\":23,\"Element3\":{},\"Element4\":true}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Element1>string</Element1><Element2 json:DataType=\"Number\">23</Element2><Element3 /><Element4 json:DataType=\"Boolean\">true</Element4></Json>"),
        InlineData(
            "{\"Element1\":\"\",\"Element2\":null}",
            "<Json><Element1></Element1><Element2 /></Json>"),

        // Attributes:
        InlineData(
            "{\"@attribute1\":\"attr1\",\"@attribute2\":\"attr2\",\"Element\":\"contents\"}",
            "<Json attribute1=\"attr1\" attribute2=\"attr2\"><Element>contents</Element></Json>"),

        // Array tests:
        InlineData(
            "[]",
            "<Json />"),
        InlineData(
            "  [  ]  ",
            "<Json />"),
        InlineData(
            "{\"Prop\":[\"10\",\"20\",\"30\"]}",
            "<Json><Prop>10</Prop><Prop>20</Prop><Prop>30</Prop></Json>"),
        InlineData(
            " { \"Prop\" : [ \"10\" , \"20\" , \"30\" ] } ",
            "<Json><Prop>10</Prop><Prop>20</Prop><Prop>30</Prop></Json>"),
        InlineData(
            "{\"Prop\":[{},\"10\",{},{}]}",
            "<Json><Prop /><Prop>10</Prop><Prop /><Prop /></Json>"),
        InlineData(
            "{\"Prop\":[[],\"10\",[10,20,null],[]]}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Prop /><Prop>10</Prop><Prop><Value json:DataType=\"Number\">10</Value><Value json:DataType=\"Number\">20</Value><Value /></Prop><Prop /></Json>"),
        InlineData(
            "{\"Prop\":[{\"Element\":\"Contents\"},\"20\",30,true]}",
            "<Json xmlns:json=\"http://www.integround.com/json\"><Prop><Element>Contents</Element></Prop><Prop>20</Prop><Prop json:DataType=\"Number\">30</Prop><Prop json:DataType=\"Boolean\">true</Prop></Json>"),
        InlineData(
            "[{\"Element\":\"Contents\"},{\"Element2\":\"Contents2\"}]",
            "<Json><Value><Element>Contents</Element></Value><Value><Element2>Contents2</Element2></Value></Json>"),

        // Test special character escaping:
        InlineData(
            "{\"Prop\":[\"Content \\\"String\\\"\",\"Multiline\\nString\\tValue\",\"Others: \\b \\f \\r \\\\ \\/ \\u0054 \"]}",
            "<Json><Prop>Content \"String\"</Prop><Prop>Multiline\nString\tValue</Prop><Prop>Others: &#x8; &#xC; \r \\ / T </Prop></Json>"),
        
        // Test XML character escaping:
        InlineData(
            "{\"Root\":\"><&'\\\"\",\"@attribute\":\"><&'\\\"\"}",
            "<Json attribute=\"&gt;&lt;&amp;'&quot;\"><Root>&gt;&lt;&amp;'\"</Root></Json>"),
        ]
        public void TestJsonToXml(string input, string expected)
        {
            var xml = JsonConverter.ConvertToXml(input);

            Assert.Equal(expected, xml.InnerXml);
        }


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
    }
}
