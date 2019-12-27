using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace iCode.Projects
{
	public class Project
	{
		private JObject _attributes;

		public string Name;
		public string BundleId;
		public string Path;

		public List<string> Frameworks;
		public List<Class> Classes;

		internal Project(string path)
		{
			this.Frameworks = new List<string>();
			this.Classes = new List<Class>();
			this._attributes = JObject.Parse(File.ReadAllText(path));
			this.Name = this._attributes["name"].ToString();
			this.BundleId = this._attributes["package"].ToString();
			this.Path = System.IO.Path.GetDirectoryName(path);

			foreach (JToken jtoken in this._attributes["frameworks"])
			{
				this.Frameworks.Add((string)jtoken);
			}

			foreach (JToken classStruct in this._attributes["classes"])
			{
				this.Classes.Add(new Class(classStruct));
			}
		}

		internal string GetProjectContent
		{
			get
			{
				return this._attributes.ToString();
			}
		}
	}
}