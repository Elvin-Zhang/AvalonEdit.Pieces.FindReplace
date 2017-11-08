﻿using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Threading.Tasks;
using System.Threading;
using ICSharpCode.AvalonEdit;

namespace FindReplaceTesting
{
    [TestClass]
    public class UnitTest1
    {

        // here is some info on OnApplyTemplate: https://charlass.wordpress.com/2012/02/17/wpf-onapplytemplate-is-not-getting-called/


        [TestMethod]
        public async Task TestMethod1()
        {
            var result = await wpfTestUtil.Utility.runWithUIThread();

            Assert.IsFalse(result.IsError, $"Exception occured: {result.ex}");
        }


        [TestMethod]
        public async Task TestSearchReplacePanel()
        {
            var result = await wpfTestUtil.Utility.runWithUIThread(new wpfTestUtil.RunOnUIArgs
            {
                RunAfterWindowAvailable = (win, host) =>
                {
                    var editor = host.GetTextEditor();

                    FindReplace.SearchReplacePanel.Install(editor);
                }
            });

            Assert.IsFalse(result.IsError, $"Exception occured: {result.ex}");
        }
    }
}
