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
		public string Category { get; set; } = "appdb-OtherApps";

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

	class ChocoAppModel
	{
		public string Caption { get; set; } = null;
		public string FileName { get; set; } = null;

		public AppModel Promote()
		{
			var choco = Environment.GetEnvironmentVariable("ChocolateyInstall", EnvironmentVariableTarget.User);

			return new AppModel { ExecutablePath = System.IO.Path.Combine(choco, "bin", FileName), Caption = Caption, Category = "appdb-ChocolateyApps" };
		}
	}

	class AppModelCollection
	{
		public string TargetLocation { get; set; } = null;
		public string PortablePlatformLocation { get; set; } = null;
		public List<string> FavoriteApps { get; set; } = new List<string>();
		public List<AppModel> Entries { get; set; } = new List<AppModel>();
		public List<string[]> AutomaticCaptionSubst { get; set; } = new List<string[]>();

		public void AddEntry(AppModel app)
		{
			if (Entries.Where(x => x.Caption == app.Caption).Count() > 0)
			{
				Console.WriteLine("NOTICE: [" + app.Caption + "] already exists, ignoring " + app.ExecutablePath);
			}
			else
			{
				Entries.Add(app);
			}
		}
	}
}
