using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SavegameSync;

namespace SavegameSyncTest
{
    [TestClass]
    public class FileUtilsTest
    {
        [TestMethod]
        public void TestGetParentDirectoryPath()
        {
            Assert.AreEqual("C:\\MyFavoriteGame\\Saves", FileUtils.GetParentDirectoryPath("C:\\MyFavoriteGame\\Saves\\save1.sgm"));
            Assert.AreEqual("C:\\MyFavoriteGame\\Saves", FileUtils.GetParentDirectoryPath("C:\\MyFavoriteGame\\Saves\\\\save1.sgm"));
            Assert.AreEqual("C:\\MyFavoriteGame\\\\Saves", FileUtils.GetParentDirectoryPath("C:\\MyFavoriteGame\\\\Saves\\\\save1.sgm"));
            Assert.AreEqual("C:\\MyFavoriteGame", FileUtils.GetParentDirectoryPath("C:\\MyFavoriteGame\\Saves"));
            Assert.AreEqual("C:\\MyFavoriteGame", FileUtils.GetParentDirectoryPath("C:\\MyFavoriteGame\\Saves\\"));
        }
    }
}
