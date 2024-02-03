using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AppDb.Windows
{
	class ShellLink : IWshRuntimeLibrary.IWshShortcut
	{
		protected static IWshRuntimeLibrary.WshShellClass shellObj = new IWshRuntimeLibrary.WshShellClass();
		protected IWshRuntimeLibrary.IWshShortcut linkObj;

		public ShellLink(string path)
		{
			this.Load(path);
		}

		public void Load(string path)
		{
			this.linkObj = shellObj.CreateShortcut(path) as IWshRuntimeLibrary.IWshShortcut;
		}

		public string Arguments
		{
			get
			{
				return this.linkObj.Arguments;
			}
			set
			{
				this.linkObj.Arguments = value;
			}
		}

		public string Description
		{
			get
			{
				return this.linkObj.Description;
			}
			set
			{
				this.linkObj.Description = value;
			}
		}

		public string FullName
		{
			get
			{
				return this.linkObj.FullName;
			}
		}

		public string Name
		{
			get
			{
				return System.IO.Path.GetFileNameWithoutExtension(this.FullName);
			}
		}

		public string Hotkey
		{
			get
			{
				return this.linkObj.Hotkey;
			}
			set
			{
				this.linkObj.Hotkey = value;
			}
		}

		public string IconLocation
		{
			get
			{
				return this.linkObj.IconLocation;
			}
			set
			{
				this.linkObj.IconLocation = value;
			}
		}

		public string RelativePath
		{
			set
			{
				this.linkObj.RelativePath = value;
			}
		}

		public void Save()
		{
			this.linkObj.Save();
		}

		public string TargetPath
		{
			get
			{
				return this.linkObj.TargetPath;
			}
			set
			{
				this.linkObj.TargetPath = value;
			}
		}

		public int WindowStyle
		{
			get
			{
				return this.linkObj.WindowStyle;
			}
			set
			{
				this.linkObj.WindowStyle = value;
			}
		}

		public string WorkingDirectory
		{
			get
			{
				return this.linkObj.WorkingDirectory;
			}
			set
			{
				this.linkObj.WorkingDirectory = value;
			}
		}

		public static ShellLink FromModel(Model.AppModel model)
		{
			var obj = new ShellLink(model.LinkLocation + System.IO.Path.DirectorySeparatorChar + model.Caption + ".lnk");
			obj.WorkingDirectory = model.StartIn;
			obj.Arguments = model.Arguments;
			obj.TargetPath = model.ExecutablePath;
			obj.IconLocation = model.IconLocation;

			return obj;
		}
	}
}
