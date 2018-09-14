using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using XelberaSW.SpORM.Utilities;

namespace XelberaSW.SpORM.Tests
{
    [TestClass]
    public class XmlHelpersTests
    {
        [TestMethod]
        public void Test1()
        {
            var ser = new CustomXmlSerializer();

            var obj = new
            {
                A = 0,
                B = "hello!",
                C = new
                {
                    Ca = "Hello!",
                    Cb = Guid.Empty,
                    Cc = DateTime.Now
                }
            };

            var a = ser.Serialize(obj, "Param");
        }

        private static object CreateObjectForTest()
        {
            return new SerializationTest_obj
            {
                A = new object[]
                {
                    5,
                    2,
                    3,
                    "SomeString",
                    new SerializationTest_A
                    {
                        B = new SerializationTest_B
                        {
                            C = "AAA",
                            D = 15.5
                        }
                    }
                },
                B = new SerializationTest_B
                {
                    C = "xxxx",
                    D = -15.2233
                },
                C = new SerializationTest_C()
            };
        }

        private const int PERFORMANCE_TEST_COUNT = 300000;

        [TestMethod]
        public void Test2()
        {
            var sb = new StringBuilder();
            using (new PerformanceCheck(nameof(CustomXmlSerializer)))
            {
                var ser = new CustomXmlSerializer();
                for (var i = 0; i < PERFORMANCE_TEST_COUNT; i++)
                {
                    var obj = CreateObjectForTest();

                    var a = ser.Serialize(obj, "Param");

                    var str = a.ToString();
                    sb.Clear();
                    sb.AppendLine(str);
                }
            }

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void Test3()
        {
            var sb = new StringBuilder();

            using (new PerformanceCheck(nameof(XmlSerializer)))
            {
                var ser = new XmlSerializer(typeof(SerializationTest_obj), new[]
                {
                    typeof(SerializationTest_A)
                });
                for (var i = 0; i < PERFORMANCE_TEST_COUNT; i++)
                {
                    var obj = CreateObjectForTest();

                    var stringWriter = new StringWriter();
                    ser.Serialize(stringWriter, obj);

                    var str = stringWriter.ToString();
                    sb.Clear();
                    sb.AppendLine(str);
                }
            }

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void Test4()
        {
            var ser = new CustomXmlSerializer();

            var obj = new ATEEventLogEntry
            {
                EventName = "Hello, world!",
                Caller = "Moscow calling",
                Recipient = "USA",
                LinkedId = "global link",
                EventDate = DateTimeOffset.Now,
                Data = new
                {
                    A = 0,
                    B = "hello!",
                    C = new
                    {
                        Ca = "Hello!",
                        Cb = Guid.Empty,
                        Cc = DateTime.Now
                    }
                }
            };

            var a = ser.Serialize(obj, "Param");
        }
    }

    [XmlRoot("Param")]
    public class SerializationTest_obj
    {
        public object[] A { get; set; } = new object[]
        {
            5,2,3,new SerializationTest_A()
        };

        public SerializationTest_B B { get; set; }
        public SerializationTest_C C { get; set; }
    }

    public class SerializationTest_A
    {
        public SerializationTest_B B { get; set; } = new SerializationTest_B();
    }

    public class SerializationTest_B
    {
        [XmlAttribute]
        public string C { get; set; } = "AAA";
        [XmlAttribute]
        public double D { get; set; } = 15.5;
    }


    public class SerializationTest_C
    {
        [XmlAttribute]
        public Guid Guid { get; set; } = Guid.NewGuid();
        [XmlAttribute]
        public DateTime Now { get; set; } = DateTime.Now;
    }

    public class ATEEventLogEntry
    {
        public string EventName { get; set; }
        public string Caller { get; set; }
        public string Recipient { get; set; }
        public string LinkedId { get; set; }
        public DateTimeOffset EventDate { get; set; }
        public object Data { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
