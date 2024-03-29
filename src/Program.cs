using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLAP;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

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

	class ProcessResult
	{
		public string Output { get; set; }
		public string Error { get; set; }
		public int ExitCode { get; set; }
	}

	class ControlVerbs
	{
		[Empty, Help]
		public static void Help(string help)
		{
			Console.WriteLine(help);
		}

		[Verb]
		public static void Version()
		{
			Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);
		}

		[Verb(Description = "Recreate database")]
		public static void Refresh(
			[Aliases("db-location")]
			[Required]
			[Description("JSON appdb file location")]
			[CLAP.Validation.PathExists]
			string dbloc,

			[Aliases("additional-links-location")]
			[CLAP.Validation.PathExists]
			[Description("Location of directory with links. Its contents will be copied to target. Non-recursive.")]
			string addloc
			)
		{
			var parent = Newtonsoft.Json.JsonConvert.DeserializeObject<Model.AppModelCollection>(System.IO.File.ReadAllText(dbloc));
			parent.TargetLocation = Environment.ExpandEnvironmentVariables(parent.TargetLocation);
			parent.PortablePlatformLocation = Environment.ExpandEnvironmentVariables(parent.PortablePlatformLocation);

			if (parent.PortablePlatformLocation != null)
			{
				Util.importPortableApps(parent, parent.PortablePlatformLocation);
			}

			if (addloc != null)
			{
				Util.importDirectoryWithLinks(parent, addloc);
			}

			Util.importChocolateyApps(parent);

			Action<Model.AppModelCollection> prepare = (col) =>
			{
				foreach (var model in col.Entries)
				{
					if (col.FavoriteApps.Any(x => x == System.IO.Path.Combine(model.Category, model.Caption)))
					{
						model.Category = "appdb-FavoriteApps";
					}

					// Windows 10/11 Start menu fails to show icon if environment variables are used in .lnk *facepalm*
					if (model.ExecutablePath != null) model.ExecutablePath = Environment.ExpandEnvironmentVariables(model.ExecutablePath);
					if (model.IconLocation != null) model.IconLocation = Environment.ExpandEnvironmentVariables(model.IconLocation);
					if (model.StartIn != null) model.StartIn = Environment.ExpandEnvironmentVariables(model.StartIn);

					model.LinkLocation = Environment.ExpandEnvironmentVariables(System.IO.Path.Combine(col.TargetLocation, model.Category));
				}

				var locations = col.Entries.Select(x => x.LinkLocation).Distinct();

				foreach (var location in locations)
				{
					if (!System.IO.Directory.Exists(location))
					{
						System.IO.Directory.CreateDirectory(location);
					}

					new System.IO.DirectoryInfo(location).Empty();
				}
			};

			Action<Model.AppModelCollection> execute = (col) =>
			{
				foreach (var model in col.Entries)
				{		
					var link = Windows.ShellLink.FromModel(model);
					link.Save();
				}
			};

			prepare(parent);
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

				col.AddEntry(new Model.AppModel
				{
					Arguments = link.Arguments,
					Caption = file.Name.Replace(".lnk", ""),
					ExecutablePath = link.TargetPath,
					StartIn = link.WorkingDirectory,
					IconLocation = link.IconLocation,
				});
			}
		}

		static ProcessResult runCommand(string filename, string args)
		{
			Process process = new Process();
			process.StartInfo.FileName = filename;
			process.StartInfo.Arguments = args;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.Start();
			
			process.WaitForExit();

			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();

			return new ProcessResult { Output = output, Error = error, ExitCode = process.ExitCode };
		}

		public static void importChocolateyApps(Model.AppModelCollection col)
		{
			var choco = Environment.GetEnvironmentVariable("ChocolateyInstall", EnvironmentVariableTarget.User);
			var chocoBin = System.IO.Path.Combine(choco, "bin");

			foreach (var filename in System.IO.Directory.EnumerateFiles(chocoBin))
			{
				var file = new System.IO.FileInfo(filename);

				if (file.Extension == ".exe")
				{
					var result = runCommand(filename, "--shimgen-noop");

					if (result.Error.Length > 0 || result.ExitCode != -1)
					{
						Console.WriteLine("WARNING: cannot check gui shimgen for [" + filename + "], exit code " + result.ExitCode);
						Console.WriteLine(result.Output);
						Console.Error.WriteLine(result.Error);
					}
					else
					{
						string[] lines = result.Output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

						bool isGui = lines
							.Where(x => x.StartsWith("  is gui? "))
							.Select(x => x.Replace("  is gui? ", ""))
							.Select(x => bool.Parse(x))
							.FirstOrDefault();

						if (isGui)
						{
							string caption = file.Name.Replace(".exe", "");

							var subst = col.AutomaticCaptionSubst.Where(x => x[0] == caption).ToList();

							if (subst.Count > 0)
							{
								caption = subst[0][1];
							}

							if (caption != "")
							{
								col.AddEntry(new Model.ChocoAppModel { Caption = caption, FileName = file.Name }.Promote());
							}
						}
					}
				}
			}
		}

		public static void importPortableApps(Model.AppModelCollection col, string path)
		{
			var dir = new System.IO.DirectoryInfo(path + System.IO.Path.DirectorySeparatorChar + "PortableApps");

			foreach (var appdir in dir.GetDirectories())
			{

				foreach (var file in appdir.EnumerateFiles())
				{
					if (file.Name.EndsWith("Portable.exe"))
					{
						var entry = new Model.AppModel { ExecutablePath = file.FullName, Category = "appdb-PortableApps", Caption = file.Name.Replace("Portable.exe", "") };

						var subst = col.AutomaticCaptionSubst.Where(x => x[0] == entry.Caption).ToList();

						if (subst.Count > 0)
						{
							entry.Caption = subst[0][1];
						}

						if (entry.Caption != "")
						{
							col.AddEntry(entry);
						}
					}
				}
			}
		}
	}
}
