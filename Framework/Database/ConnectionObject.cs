using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Framework.Database
{
	public class ConnectionObject
	{
		public string Host { get; set; }
		public string Port { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string Database { get; set; }
	}
}
