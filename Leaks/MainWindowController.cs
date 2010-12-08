
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.IO;
using System.Globalization;

namespace Leaks
{
	public class LeakInfo
	{
		internal string Leak;
		internal List<string> CallStack;
		internal long Offset;
	}

	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		#region Constructors

		// Called when created from unmanaged code
		public MainWindowController (IntPtr handle) : base(handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base(coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public MainWindowController () : base("MainWindow")
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		//strongly typed window accessor
		public new MainWindow Window {
			get { return (MainWindow)base.Window; }
		}
		
		public class MethodInfo
		{
			internal string Name;
			internal uint Start;
			internal uint End;
			internal long Length;
		}
		List<MethodInfo> methodInfo = new List<MethodInfo>();

		partial void readAddresses (NSObject sender)
		{
			MonoMac.AppKit.NSOpenPanel p = new MonoMac.AppKit.NSOpenPanel();
			p.AllowsMultipleSelection = false;
			p.CanChooseDirectories = false;
			p.CanChooseFiles = true;
			int ret = p.RunModal();
			if (ret != 0) //NSFileHandlingPanelOKButton)
			{
				monoOutput.StringValue = (p.Urls[0].Path);
			}
			ReadMonoOutput();
			logButton.Enabled = true;
		}
		
		void ReadMonoOutput()
		{
			string[] addressLines = File.ReadAllLines(monoOutput.StringValue);
			
			foreach (string line in addressLines)
			{
				if (line.Contains("converting"))
					continue;
				string[] words = line.Split(' ');
				string methodName = null;
				uint startAddr = 0;
				uint endAddr = 0;
				long length = 0;
				
				for (int i = 0; i < words.Count(); i++)
				{
					if (words[i] == "emitted")
					{
						try
						{
							uint.TryParse(words[i+2].Replace("0x", ""), NumberStyles.AllowHexSpecifier,
							              null, out startAddr);
							if (uint.TryParse(words[i+4].Replace("0x", ""), NumberStyles.AllowHexSpecifier,
							              null, out endAddr) == false)
								endAddr = startAddr + 10;
							
							long.TryParse(words[i+7], out length);
							break;
						}
						catch (Exception er)
						{
							Console.WriteLine("eror on line: {0}", line);
							Console.WriteLine(er);
						}
					}
					else
						methodName += words[i] + " ";
				}
				MethodInfo m = new MethodInfo() 
				{
					Name = methodName.Trim(),
					Start = startAddr,
					End = endAddr,
					Length = length
				};
				methodInfo.Add(m);
			}			
		}
		
		List<LeakInfo> leaks = new List<LeakInfo>();
		
		partial void readLog (NSObject sender)
		{
			MonoMac.AppKit.NSOpenPanel p = new MonoMac.AppKit.NSOpenPanel();
			p.AllowsMultipleSelection = false;
			p.CanChooseDirectories = false;
			p.CanChooseFiles = true;
			int ret = p.RunModal();
			if (ret != 0) //NSFileHandlingPanelOKButton)
			{
				logNumber.StringValue = (p.Urls[0].Path);
			}
			
			ProcessLeaksOutput();
		}
		void ProcessLeaksOutput()
		{
			string[] addressLines = File.ReadAllLines(logNumber.StringValue);
			
			LeakInfo leak = null;
			
			for (int i = 0; i < addressLines.Count(); i++)
			{
				if (addressLines[i].Contains("Leak:"))
				{
					leak = new LeakInfo();
					leak.Leak = addressLines[i];
				}
				else if (addressLines[i].Contains("Call stack:") && leak != null)
				{
					leak.CallStack = new List<string>();
					string callStackLine = addressLines[i];
					string[] methods = callStackLine.Split('|');
					foreach (string method in methods.Skip(1))
					{
						if (method.Contains("0x"))
						{
							uint addr = uint.Parse(method.Replace("0x", "").Trim(), System.Globalization.NumberStyles.AllowHexSpecifier);
							string foundMethod = null;
							foreach (MethodInfo m in methodInfo)
							{
								if (m.Start <= addr && addr <= m.End)
								{
									foundMethod = m.Name;
									leak.Offset = addr - m.Start;
									break;
								}
							}
							if (foundMethod == null)
								leak.CallStack.Add("unknown method: " + method);
							else
								leak.CallStack.Add(foundMethod);
						}
						else
							leak.CallStack.Add(method);
					}
					leaks.Add(leak);
					leak = null;
				}
			}
		}
		
		partial void process (NSObject sender)
		{
			LeaksTableViewDataSource lDS = new LeaksTableViewDataSource(leaks);
			leaksTableView.DataSource = lDS;
			
			LeaksTableViewDelegate leaksDelegate = new LeaksTableViewDelegate(leaksTableView, leaks, callStackTableView);
			leaksTableView.Delegate = leaksDelegate;
			
			leaksTableView.ReloadData();

			CallStackTableViewDataSource callstackDS = new CallStackTableViewDataSource(leaks);
			callStackTableView.DataSource = callstackDS;
		}
		partial void copyCallStack (NSObject sender)
		{
			string stack = string.Join(Environment.NewLine, leaks[LeaksTableViewDelegate.SelectedRow].CallStack.ToArray());
			NSPasteboard pb = NSPasteboard.GeneralPasteboard;
			string[] types = new string[] {NSPasteboard.NSStringPboardType};
			pb.DeclareTypes(types, null);
			pb.SetStringforType(stack, NSPasteboard.NSStringPboardType);
		}
	}
}

