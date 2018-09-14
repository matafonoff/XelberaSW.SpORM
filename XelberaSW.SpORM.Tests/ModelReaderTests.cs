using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XelberaSW.SpORM.Internal.Readers;
using XelberaSW.SpORM.Tests.TestInternals.Wrappers;

namespace XelberaSW.SpORM.Tests
{
    [TestClass]
    public class ModelReaderTests
    {
        class A
        {
            public string StringValue { get; set; }

            [IgnoreDataMember]
            public string IgnoreValue { get; set; }

            public int Int32Value { get; set; }
            public Guid GuidValue { get; set; }
            public DateTime DateTimeValue { get; set; }
            public DateTimeOffset DateTimeOffsetValue { get; set; }
        }

        [TestMethod]
        public void SingleModelReaderTest()
        {
            var modelReader = new SingleModelReader<A>();

            var a = new A
            {
                StringValue = "Hello, World!",
                IgnoreValue = "This value MUST be ignored",
                Int32Value = 1234,
                GuidValue = Guid.NewGuid(),
                DateTimeValue = DateTime.Now,
                DateTimeOffsetValue = DateTimeOffset.Now
            };
            var wrapper = new DataRecordWrapper<A>(a);

            var b = modelReader.Read(wrapper);
            CheckObjects(a, b);
        }

        [TestMethod]
        public void DatasetReaderTest()
        {
            var datasetReader = new MultipleModelsReader<A>();

            var sourceItems = GetObjectsOfTypeA();

            var dataset = new DataReaderWrapper<A>(sourceItems);

            var items = datasetReader.Read(dataset).ToList();

            Assert.AreEqual(items.Count, sourceItems.Length);

            for (var i = 0; i < items.Count; i++)
            {
                CheckObjects(sourceItems[i], items[i]);
            }
        }

        private A[] GetObjectsOfTypeA()
        {
            return new[]
            {
                CreateObject("first"),
                CreateObject("second"),
                CreateObject("third")
            };
        }

        private static void CheckObjects(A initialObject, A dynamicallyCreated)
        {
            Assert.IsNotNull(initialObject);
            Assert.IsNotNull(dynamicallyCreated);

            Assert.AreNotSame(initialObject, dynamicallyCreated);

            Assert.AreEqual(dynamicallyCreated.StringValue, initialObject.StringValue);
            Assert.AreEqual(dynamicallyCreated.Int32Value, initialObject.Int32Value);
            Assert.AreEqual(dynamicallyCreated.GuidValue, initialObject.GuidValue);
            Assert.AreEqual(dynamicallyCreated.DateTimeValue, initialObject.DateTimeValue);
            Assert.AreEqual(dynamicallyCreated.DateTimeOffsetValue, initialObject.DateTimeOffsetValue);

            Assert.IsTrue(!string.IsNullOrWhiteSpace(initialObject.IgnoreValue));
            Assert.AreEqual(dynamicallyCreated.IgnoreValue, default(string));
        }

        private A CreateObject(string text)
        {
            return new A
            {
                StringValue = text,
                IgnoreValue = "This value MUST be ignored",
                Int32Value = text.GetHashCode(),
                GuidValue = Guid.NewGuid(),
                DateTimeValue = DateTime.Now,
                DateTimeOffsetValue = DateTimeOffset.Now
            };
        }

        private IEnumerable<IEnumerable<object>> GetDataSet()
        {
            yield return GetObjectsOfTypeA();
            yield return GetObjectsOfTypeB();
            yield return GetObjectsOfTypeC();
        }

        private IEnumerable<object> GetObjectsOfTypeC()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<object> GetObjectsOfTypeB()
        {
            throw new NotImplementedException();
        }
    }
}
