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
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace NUnit.ProjectEditor
{
    public class ProjectDocument : IProjectDocument
    {
        #region Static Fields

        /// <summary>
        /// Used to generate default names for projects
        /// </summary>
        private static int projectSeed = 0;

        /// <summary>
        /// The extension used for test projects
        /// </summary>
        private static readonly string nunitExtension = ".nunit";

        #endregion

        private enum ProjectUpdateState
        {
            NoChanges,
            XmlTextHasChanges,
            XmlDocHasChanges
        }

        #region Instance Fields

        /// <summary>
        /// The original text from which the doc was loaded.
        /// Updated from the doc when the xml view is displayed
        /// and from the view when the user edits it.
        /// </summary>
        string xmlText;

        /// <summary>
        /// The XmlDocument representing the loaded doc. It
        /// is generated from the text when the doc is loaded
        /// unless an exception is thrown. It is modified as the
        /// user makes changes.
        /// </summary>
        XmlDocument xmlDoc;

        /// <summary>
        /// An exception thrown when trying to build the xml
        /// document from the xml text.
        /// </summary>
        Exception exception;

        /// <summary>
        /// Path to the file storing this doc
        /// </summary>
        private string projectPath;

        /// <summry>
        /// True if the Xml Document has been changed
        /// </summary>
        private ProjectUpdateState projectUpdateState;

        /// <summary>
        /// True if the doc has been changed and not yet saved
        /// </summary>
        private bool hasUnsavedChanges;

        #endregion

        #region Constructors

        public ProjectDocument() : this(GenerateProjectName()) { }

        public ProjectDocument(string projectPath)
        {
            this.xmlDoc = new XmlDocument();
            this.projectPath = Path.GetFullPath(projectPath);

            xmlDoc.NodeChanged += new XmlNodeChangedEventHandler(xmlDoc_Changed);
            xmlDoc.NodeInserted += new XmlNodeChangedEventHandler(xmlDoc_Changed);
            xmlDoc.NodeRemoved += new XmlNodeChangedEventHandler(xmlDoc_Changed);
        }

        #endregion

        #region IProjectDocument Members

        #region Events

        public event ActionDelegate ProjectCreated;
        public event ActionDelegate ProjectClosed;
        public event ActionDelegate ProjectChanged;

        #endregion

        #region Properties

        /// <summary>
        /// The name of the doc.
        /// </summary>
        public string Name
        {
            get { return Path.GetFileNameWithoutExtension(projectPath); }
        }

        /// <summary>
        /// Gets or sets the path to which a doc will be saved.
        /// </summary>
        public string ProjectPath
        {
            get { return projectPath; }
            set
            {
                string newProjectPath = Path.GetFullPath(value);
                if (newProjectPath != projectPath)
                {
                    projectPath = newProjectPath;
                }
            }
        }

        /// <summary>
        /// The top-level (NUnitProject) node
        /// </summary>
        public XmlNode RootNode
        {
            get { return xmlDoc.FirstChild; }
        }

        public bool HasUnsavedChanges
        {
            get { return hasUnsavedChanges; }
        }

        public bool IsValid
        {
            get { return exception == null; }
        }

        #endregion

        #region Methods

        public void CreateNewProject()
        {
            this.xmlText = "<NUnitProject />";

            UpdateXmlDocFromXmlText();
            hasUnsavedChanges = false;

            if (ProjectCreated != null)
                ProjectCreated();
        }

        public void OpenProject(string fileName)
        {
            StreamReader rdr = new StreamReader(fileName);
            this.xmlText = rdr.ReadToEnd();
            rdr.Close();

            this.projectPath = Path.GetFullPath(fileName);
            UpdateXmlDocFromXmlText();

            if (ProjectCreated != null)
                ProjectCreated();

            hasUnsavedChanges = false;
        }

        public void CloseProject()
        {
            if (ProjectClosed != null)
                ProjectClosed();
        }

        public void SaveProject()
        {
            XmlTextWriter writer = new XmlTextWriter(
                ProjectPathFromFile(projectPath),
                System.Text.Encoding.UTF8);
            writer.Formatting = Formatting.Indented;

            xmlDoc.WriteTo(writer);
            writer.Close();

            hasUnsavedChanges = false;
        }

        public void SaveProject(string fileName)
        {
            projectPath = fileName;
            SaveProject();
        }

        public void SynchronizeModel()
        {
            switch (this.projectUpdateState)
            {
                case ProjectUpdateState.XmlTextHasChanges:
                    UpdateXmlDocFromXmlText();
                    break;

                case ProjectUpdateState.XmlDocHasChanges:
                    UpdateXmlTextFromXmlDoc();
                    break;
            }
        }

        #region Load Methods

        public void Load()
        {
            StreamReader rdr = new StreamReader(this.projectPath);
            this.xmlText = rdr.ReadToEnd();
            rdr.Close();

            LoadXml(this.xmlText);

            this.hasUnsavedChanges = false;
        }

        public void LoadXml(string xmlText)
        {
            try
            {
                this.xmlText = xmlText;
                this.xmlDoc.LoadXml(xmlText);

                if (RootNode.Name != "NUnitProject")
                    throw new ProjectFormatException("Top level element must be <NUnitProject...>.");
            }
            catch (ProjectFormatException)
            {
                throw;
            }
            catch (XmlException e)
            {
                throw new ProjectFormatException(e.Message, e.LineNumber, e.LinePosition);
            }
            catch (Exception e)
            {
                // TODO: Figure out line numbers
                throw new ProjectFormatException(e.Message);
            }
        }

        #endregion

        #region Save methods

        public void Save()
        {
            xmlText = this.ToXml();

            using (StreamWriter writer = new StreamWriter(ProjectPathFromFile(projectPath), false, System.Text.Encoding.UTF8))
            {
                writer.Write(xmlText);
            }

            hasUnsavedChanges = false;
        }

        public void Save(string fileName)
        {
            this.projectPath = Path.GetFullPath(fileName);
            Save();
        }

        public string ToXml()
        {
            StringWriter buffer = new StringWriter();

            using (XmlTextWriter writer = new XmlTextWriter(buffer))
            {
                writer.Formatting = Formatting.Indented;
                xmlDoc.WriteTo(writer);
            }

            return buffer.ToString();
        }

        #endregion

        #endregion

        #endregion

        #region IXmlModel Members

        public string XmlText
        {
            get { return xmlText; }
            set
            {
                xmlText = value;
                projectUpdateState = ProjectUpdateState.XmlTextHasChanges;
            }
        }

        public Exception Exception
        {
            get { return exception; }
            set
            {
                exception = value;
                projectUpdateState = ProjectUpdateState.XmlTextHasChanges;
            }
        }

        #endregion

        #region Event Handlers

        void xmlDoc_Changed(object sender, XmlNodeChangedEventArgs e)
        {
            hasUnsavedChanges = true;
            projectUpdateState = ProjectUpdateState.XmlDocHasChanges;

            if (this.ProjectChanged != null)
                ProjectChanged();
        }

        #endregion

        #region Private Properties and Helper Methods

        private string DefaultBasePath
        {
            get { return Path.GetDirectoryName(projectPath); }
        }

        public static bool IsProjectFile(string path)
        {
            return Path.GetExtension(path) == nunitExtension;
        }

        private static string ProjectPathFromFile(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path) + nunitExtension;
            return Path.Combine(Path.GetDirectoryName(path), fileName);
        }

        private static string GenerateProjectName()
        {
            return string.Format("Project{0}", ++projectSeed);
        }

        private void UpdateXmlDocFromXmlText()
        {
            try
            {
                LoadXml(xmlText);
                exception = null;
                projectUpdateState = ProjectUpdateState.NoChanges;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
        }

        private void UpdateXmlTextFromXmlDoc()
        {
            xmlText = this.ToXml();
            projectUpdateState = ProjectUpdateState.NoChanges;
        }

        #endregion
    }
}