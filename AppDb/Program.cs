using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLAP;

namespace AppDb
{
	class Program
	{
		static void Main(string[] args)
		{
			int exitCode = Parser.RunConsole<ControlVerbs>(args);
			Environment.Exit(exitCode);
		}
	}

	class ControlVerbs
	{
		[Empty, Help]
		public static void Help(string help)
		{
			Console.WriteLine(help);
		}

		[Verb(Description = "Recreate database")]
		public static void Refresh(
			[Aliases("db-location")]
			[Required]
			[Description("JSON appdb file location")]
			[CLAP.Validation.PathExists]
			string dbloc,

			[Required]
			[CLAP.Validation.PathExists]
			[Description("Directory where links will be created. Caution! All existing contents of this directory will be mercilessly obliterated.")]
			string target,

			[Aliases("portable-platform-location")]
			[CLAP.Validation.PathExists]
			[Description("Location of the portable platform. All installed apps will be automatically imported")]
			string pploc,

			[Aliases("additional-links-location")]
			[CLAP.Validation.PathExists]
			[Description("Location of directory with links. Its contents will be copied to target. Non-recursive.")]
			string addloc
			)
		{
			var parent = Newtonsoft.Json.JsonConvert.DeserializeObject<Model.AppModelCollection>(System.IO.File.ReadAllText(dbloc));
			parent.Location = target;
			System.IO.Directory.CreateDirectory(target);

			if (pploc != null)
			{
				Util.importPortableApps(parent, pploc);
			}

			if (addloc != null)
			{
				Util.importDirectoryWithLinks(parent, addloc);
			}

			Action<Model.AppModelCollection> execute = (col) =>
			{
				var dir = new System.IO.DirectoryInfo(col.Location);
				dir.Empty();

				foreach (var model in col.Entries)
				{
					model.LinkLocation = col.Location;

					var link = Windows.ShellLink.FromModel(model);
					link.Save();
				}
			};

			execute(parent);
		}
	}

	static class Util
	{
		public static void Empty(this System.IO.DirectoryInfo directory)
		{
			foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
			foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
		}

		public static void importDirectoryWithLinks(Model.AppModelCollection col, string path)
		{
			var dir = new System.IO.DirectoryInfo(path);

			foreach (var file in dir.EnumerateFiles())
			{
				var link = new Windows.ShellLink(file.FullName);

				col.Entries.Add(new Model.AppModel
				{
					Arguments = link.Arguments,
					Caption = file.Name.Replace(".lnk", ""),
					ExecutablePath = link.TargetPath,
					StartIn = link.WorkingDirectory,
					IconLocation = link.IconLocation
				});
			}
		}

		public static void importPortableApps(Model.AppModelCollection col, string path)
		{
			var dir = new System.IO.DirectoryInfo(path + System.IO.Path.DirectorySeparatorChar + "PortableApps");

			var captionDict = new Dictionary<string, string>
			{
				{ "GoogleChrome", "Chrome" },
				{ "TelegramDesktop", "Telegram" }
			};

			foreach (var appdir in dir.GetDirectories())
			{
				// Special case for skype *sigh*
				if (appdir.Name == "sPortable")
				{
					var entry = new Model.AppModel
					{
						ExecutablePath = @"D:\Apps\Locale Emulator\LEProc.exe",
						Arguments = @"-run ""D:\Apps\PortablePlatform\PortableApps\sPortable\sPortable.exe""",
						Caption = "Skype",
						StartIn = appdir.FullName,
						IconLocation = System.IO.Path.Combine(new string[] { appdir.FullName, "App", "Skype", "Phone", "Skype.exe" }) + ",0"
					};

					col.Entries.Add(entry);
				}
				else
				{
					foreach (var file in appdir.EnumerateFiles())
					{
						if (file.Name.EndsWith("Portable.exe"))
						{
							var entry = new Model.AppModel { ExecutablePath = file.FullName, Caption = file.Name.Replace("Portable.exe", "") };

							if (captionDict.ContainsKey(entry.Caption))
							{
								entry.Caption = captionDict[entry.Caption];
							}

							col.Entries.Add(entry);
						}
					}
				}
			}
		}
	}
}
