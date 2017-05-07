using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppDb.Model
{
	class AppModel
	{
		public string LinkLocation { get; set; } = null;
		public string ExecutablePath { get; set; } = null;
		public string Arguments { get; set; } = "";
		public string Caption { get; set; } = null;

		private string _startIn = null;
		public string StartIn
		{
			get
			{
				return _startIn ?? System.IO.Path.GetDirectoryName(ExecutablePath);
			}
			set
			{
				_startIn = value;
			}
		}

		private string _iconLocation = null;
		public string IconLocation
		{
			get
			{
				return _iconLocation ?? ExecutablePath + ",0";
			}
			set
			{
				_iconLocation = value;
			}
		}
	}

	class AppModelCollection
	{
		public string Location { get; set; } = null;
		public List<AppModel> Entries { get; set; } = new List<AppModel>();
	}
}
