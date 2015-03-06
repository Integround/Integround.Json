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
            "{\"Root\":\"Contents\"}",
            "<Json><Root>Contents</Root></Json>"),
        InlineData(
            "{\"Root\":\"Contents\"}",
            "<Json><Root>Contents</Root></Json>"),
        InlineData(
            "{\"Element1\":\"string\",\"Element2\":23,\"Element3\":{},\"Element4\":true}",
            "<Json><Element1>string</Element1><Element2>23</Element2><Element3 /><Element4>true</Element4></Json>"),
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
            "{\"Prop\":[\"10\",\"20\",\"30\"]}",
            "<Json><Prop>10</Prop><Prop>20</Prop><Prop>30</Prop></Json>"),
        InlineData(
            "{\"Prop\":[{},\"10\",{},{}]}",
            "<Json><Prop /><Prop>10</Prop><Prop /><Prop /></Json>"),
        InlineData(
            "{\"Prop\":[[],\"10\",[10,20,null],[]]}",
            "<Json><Prop /><Prop>10</Prop><Prop><Value>10</Value><Value>20</Value><Value /></Prop><Prop /></Json>"),
        InlineData(
            "{\"Prop\":[{\"Element\":\"Contents\"},\"20\",30,true]}",
            "<Json><Prop><Element>Contents</Element></Prop><Prop>20</Prop><Prop>30</Prop><Prop>true</Prop></Json>"),
        InlineData(
            "[{\"Element\":\"Contents\"},{\"Element2\":\"Contents2\"}]",
            "<Json><Value><Element>Contents</Element></Value><Value><Element2>Contents2</Element2></Value></Json>")
        ]
        public void TestJsonToXml(string input, string expected)
        {
            var xml = JsonConverter.ConvertToXml(input);

            Assert.Equal(expected, xml.InnerXml);
        }

        
        // Test invalid input:
        [Theory,
            InlineData(
            "{",
            "Invalid JSON. Expecting '\"', found EOF."),
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
