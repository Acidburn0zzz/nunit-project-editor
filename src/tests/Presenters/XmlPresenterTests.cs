﻿// ***********************************************************************
// Copyright (c) 2010 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using NUnit.Framework;
using NSubstitute;

namespace NUnit.ProjectEditor.Tests.XmlEditor
{
    public class XmlPresenterTests
    {
        private IProjectDocument model;
        private IXmlView view;
        private XmlPresenter presenter;

        private static readonly string initialText = "<NUnitProject/>";
        private static readonly string changedText = "<NUnitProject processModel=\"Separate\"/>";

        [SetUp]
        public void Initialize()
        {
            model = new ProjectDocument();
            model.LoadXml(initialText);
            view = Substitute.For<IXmlView>();
            presenter = new XmlPresenter(model, view);
            presenter.LoadViewFromModel();
        }

        [Test]
        public void ViewIsInitializedCorrectly()
        {
            Assert.AreEqual(initialText, view.Xml.Text);
        }

        [Test]
        public void WhenXmlChangesModelIsUpdated()
        {
            view.Xml.Text = changedText;
            view.Xml.Validated += Raise.Event<ActionDelegate>();
            Assert.AreEqual(changedText, model.XmlText);
        }

        //[Test]
        //public void BadXmlSetsException()
        //{
        //    view.Xml.Text = "<NUnitProject>"; // Missing slash
        //    view.Xml.Validated += Raise.Event<ActionDelegate>();
            
        //}
    }
}
