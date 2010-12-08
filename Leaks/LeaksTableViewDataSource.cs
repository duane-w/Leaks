using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Leaks
{
	public class LeaksTableViewDataSource : NSTableViewDataSource
	{
		public List<LeakInfo> Leaks;
		
		public LeaksTableViewDataSource (List<LeakInfo> list)
		{
			Leaks = list;
		}
		
		public override NSObject GetObjectValue (NSTableView tableView, NSTableColumn tableColumn, int row)
		{
			return new NSString(Leaks[row].Leak);
		}
		
		public override int GetRowCount (NSTableView tableView)
		{
			return Leaks.Count();
		}
	}
	
	public class LeaksTableViewDelegate : NSTableViewDelegate
	{
		NSTableView View;
		public List<LeakInfo> Leaks;
		NSTableView CallstackView;
		public static int SelectedRow = -1;
		
		public LeaksTableViewDelegate(NSTableView v, List<LeakInfo> list, NSTableView callstack)
		{
			View = v;
			Leaks = list;
			CallstackView = callstack;
		}
		
		public override void SelectionDidChange (NSNotification notification)
		{
			SelectedRow = View.SelectedRow;
			CallstackView.ReloadData();
		}
	}
	
	public class CallStackTableViewDataSource : NSTableViewDataSource
	{
		public List<LeakInfo> Leaks;
		
		public CallStackTableViewDataSource (List<LeakInfo> list)
		{
			Leaks = list;
		}
		
		public override NSObject GetObjectValue (NSTableView tableView, NSTableColumn tableColumn, int row)
		{
			if (LeaksTableViewDelegate.SelectedRow == -1)
				return null;
			return new NSString(Leaks[LeaksTableViewDelegate.SelectedRow].CallStack[row]);
		}
		
		public override int GetRowCount (NSTableView tableView)
		{
			if (LeaksTableViewDelegate.SelectedRow == -1)
				return 0;
			
			return Leaks[LeaksTableViewDelegate.SelectedRow].CallStack.Count;
		}
	}
}

