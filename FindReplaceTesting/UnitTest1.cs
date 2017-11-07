using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;

namespace FindReplaceTesting
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var host = new AvalonTestPieces.TestHost();
            var win = new Window();
            win.Content = host;
            win.ShowDialog();
        }
    }
}
