using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace iCode
{
	public class Class
    {
        internal readonly JObject Attributes;
        public readonly string Filename;
        public readonly List<string> CompilerFlags;

        internal Class(JToken classStruct)
		{
            Attributes = (Newtonsoft.Json.Linq.JObject) classStruct;
			this.CompilerFlags = new List<string>();
			this.Filename = classStruct["filename"].ToString();
			foreach (JToken jtoken in classStruct["flags"])
			{
				this.CompilerFlags.Add((string)jtoken);
			}
		}
	}
}
