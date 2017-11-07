using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;

namespace FindReplaceTesting
{
    [TestClass]
    public class UnitTest1
    {
        private (Window win, AvalonTestPieces.TestHost host) SetupHost()
        {
            var host = new AvalonTestPieces.TestHost();
            var win = new Window();
            win.Content = host;

            return (win, host);
        }


        [TestMethod]
        public void TestMethod1()
        {
            var result = SetupHost();
            result.win.ShowDialog();
        }


        [TestMethod]
        public void TestOverlayButton()
        {
            var result = SetupHost();
            var editor = result.host.GetTextEditor();

            var adorner1 = new FindReplace.GenericControlAdorner(editor.TextArea)
            {
                Child = new Button { Content = "Hello World!" }
            };


            result.win.ShowDialog();
        }
    }
}
