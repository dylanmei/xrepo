﻿using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace XPack.Scenarios.TestSupport
{
    public class ProjectBuilder
    {
        private readonly TestEnvironment _environment;
        private readonly string _assemblyName;
        private string _serializedProject;
        
        public ProjectBuilder(string assemblyName, TestEnvironment environment)
        {
            _assemblyName = assemblyName;
            _environment = environment;
            using(var reader = new StreamReader(this.GetType().Assembly.GetManifestResourceStream("XPack.Scenarios.TestSupport.ClassLibrary.csproj.template")))
            {
                _serializedProject = reader.ReadToEnd();
                _serializedProject = _serializedProject.Replace("$AssemblyName$", assemblyName);
                _serializedProject = _serializedProject.Replace("$RootNamespace$", assemblyName);
            }
        }

        public string AssemblyName
        {
            get { return _assemblyName; }
        }

        public string FullPath
        {
            get { return _environment.ResolveProjectPath(Path.Combine(_assemblyName, _assemblyName + ".csproj")); }
        }

        public string Build()
        {
            var logger = new CapturingLogger(LoggerVerbosity.Minimal);
            if(!Project.Build(logger))
                throw new ApplicationException("Build failed");

            return logger.ToString();
        }

        public void AddReference(string assemblyName)
        {
            Project.AddItem("Reference", assemblyName);
            Project.Save();
        }

        private Project _project;
        private Project Project
        {
            get
            {
                if(_project == null)
                {
                    _environment.EnsureDirectoryExists(_environment.XPackConfigDir);
                    var buildProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        {"XPackConfigDir", _environment.XPackConfigDir},
                        {"DisableGlobalXPack", "true"}
                    };

                    var projectDirectory = Path.GetDirectoryName(FullPath);
                    _environment.EnsureDirectoryExists(projectDirectory);

                    File.WriteAllText(FullPath, _serializedProject);
                    _project = new Project(FullPath, buildProperties, null);
                    var xPackImportPath = Path.Combine(_environment.Root, "XPack.Build.targets");
                    _project.Xml.AddImport(xPackImportPath);
                    _project.Xml.Save();
                }

                return _project;
            }
        }
    }
}