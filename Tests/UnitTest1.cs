using NUnit.Framework;
using xlator;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var testString = "Hello, this is a test Realm string with MongoDB and who Realm knows what else?";

            var result = IgnoreList.ReplaceIgnoreWords(testString);

            Assert.AreEqual("Hello, this is a test NOXLATERealmNOXLATE string with NOXLATEMongoDBNOXLATE and who NOXLATERealmNOXLATE knows what else?", result);

            Assert.AreEqual("Hello, this is a test Realm string with MongoDB and who Realm knows what else?",
                IgnoreList.ReAaddIgnoreWords(result));

        }
    }
}