using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace iCode
{
	public class Project
    {
        private JObject Attributes;

        public string Name;
        public string BundleId;

        public List<string> Frameworks;
        public List<Class> Classes;

        internal Project(string text)
		{
			this.Frameworks = new List<string>();
			this.Classes = new List<Class>();
			this.Attributes = JObject.Parse(text);
			this.Name = this.Attributes["name"].ToString();
			this.BundleId = this.Attributes["package"].ToString();

			foreach (JToken jtoken in this.Attributes["frameworks"])
			{
				this.Frameworks.Add((string)jtoken);
			}

			foreach (JToken classStruct in this.Attributes["classes"])
			{
				this.Classes.Add(new Class(classStruct));
			}
		}

		internal string GetProjectContent
		{
			get
			{
				return this.Attributes.ToString();
			}
		}
	}
}
