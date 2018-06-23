using Microsoft.VisualStudio.TestTools.UnitTesting;
using SavegameSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavegameSyncTest
{
    [TestClass]
    public class SavegameSyncUtilsTest
    {
        [TestMethod]
        public void TestDateTimeSerialization()
        {
            DateTime dateTime1 = DateTime.UtcNow;
            Assert.AreEqual(dateTime1, SavegameSyncUtils.DeserializeDateTime(SavegameSyncUtils.SerializeDateTime(dateTime1)));

            DateTime dateTime2 = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(SavegameSyncUtils.SerializeDateTime(dateTime2), "0");
            Assert.AreEqual(dateTime2, SavegameSyncUtils.DeserializeDateTime("0"));

            DateTime dateTime3 = new DateTime(1533, 10, 27, 10, 42, 55, DateTimeKind.Utc);
            Assert.AreEqual(dateTime3, SavegameSyncUtils.DeserializeDateTime(SavegameSyncUtils.SerializeDateTime(dateTime3)));
        }
    }
}
