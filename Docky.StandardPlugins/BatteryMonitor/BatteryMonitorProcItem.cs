//  
//  Copyright (C) 2009 GNOME Do
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using Cairo;
using Gdk;
using GLib;
using NDesk.DBus;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Services;

namespace BatteryMonitor
{
	public class BatteryMonitorProcItem : AbstractDockItem
	{
		const string BattBasePath = "/proc/acpi/battery";
		const string BattInfoPath = "info";
		const string BattStatePath = "state";
		
		const string BottomSvg  = "battery_bottom.svg";
		const string InsideSvg  = "battery_inside_{0}.svg";
		const string PluggedSvg = "battery_plugged.svg";
		const string TopSvg     = "battery_top.svg";
		
		int max_capacity;
		int current_capacity;
		uint timer;
		
		Regex number_regex;
		
		double Capacity {
			get {
				return (double) current_capacity / max_capacity;
			}
		}
		
		int RoundedCapacity {
			get {
				return (int) (Math.Round (Capacity, 1) * 100);
			}
		}
		
		public override string UniqueID ()
		{
			return "BatteryMonitor";
		}
		
		public BatteryMonitorProcItem (AbstractDockItemProvider owner)
		{
			Owner = owner;
			
			DockServices.System.BatteryStateChanged += HandleBatteryStateChanged;
			number_regex = new Regex ("[0-9]+");
			
			UpdateBattStat ();
			
			timer = GLib.Timeout.Add (20 * 1000, UpdateBattStat);
		}
		
		void HandleBatteryStateChanged (object sender, EventArgs args)
		{
			UpdateBattStat ();
		}
		
		void GetBatteryCapacity ()
		{
			max_capacity = 0;
			
			DirectoryInfo basePath = new DirectoryInfo (BattBasePath);
			
			foreach (DirectoryInfo battDir in basePath.GetDirectories ()) {
				string path = BattBasePath + "/" + battDir.Name + "/" + BattInfoPath;
				if (File.Exists (path)) {
					using (StreamReader reader = new StreamReader (path)) {
						string line;
						while (!reader.EndOfStream) {
							line = reader.ReadLine ();
							if (!line.StartsWith ("last full capacity"))
								continue;
							
							try {
								max_capacity += Convert.ToInt32 (number_regex.Matches (line) [0].Value);
							} catch { }
						}
					}
				}
			}
			
			max_capacity = Math.Max (1, max_capacity);
		}
		
		bool UpdateBattStat ()
		{
			GetBatteryCapacity ();
			
			string capacity = null;
			string chargeState = null;
			
			current_capacity = 0;
			DirectoryInfo basePath = new DirectoryInfo (BattBasePath);
			
			foreach (DirectoryInfo battDir in basePath.GetDirectories ()) {
				string path = BattBasePath + "/" + battDir.Name + "/" + BattStatePath;
				if (File.Exists (path)) {
					try {
						using (StreamReader reader = new StreamReader (path)) {
							string line;
							while (!reader.EndOfStream) {
								if (!string.IsNullOrEmpty (capacity) && !string.IsNullOrEmpty (chargeState))
									break;
								
								line = reader.ReadLine ();
								if (line.StartsWith ("remaining capacity")) {
									capacity = line;
									continue;
								}
								
								if (line.StartsWith ("charging state"))
									chargeState = line;
							}
						}
					} catch (IOException) {}
					
					try {
						current_capacity += Convert.ToInt32 (number_regex.Matches (capacity) [0].Value);
					} catch { }
				}
			}
			
			if (current_capacity == 0) {
				max_capacity = 1;
				HoverText = "No Battery Found";
				(Owner as BatteryMonitorItemProvider).Hidden = true;
			} else {
				(Owner as BatteryMonitorItemProvider).Hidden = false;
				HoverText = string.Format ("{0:0.0}%", Capacity * 100);
			}
			
			QueueRedraw ();

			return true;
		}
		
		void RenderSvgOnContext (Context cr, string file, int size)
		{
			Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (file, size);
			Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, (pbuf.Width - size) / 2, (pbuf.Height - size) / 2);
			cr.Paint ();
			pbuf.Dispose ();
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			(Owner as BatteryMonitorItemProvider).Hidden = (!DockServices.System.OnBattery && (Capacity > .98 || Capacity < 1));
			
			Context cr = surface.Context;
			int size = Math.Min (surface.Width, surface.Height);
			
			RenderSvgOnContext (cr, BottomSvg + "@" + GetType ().Assembly.FullName, size);
			if (RoundedCapacity > 0)
				RenderSvgOnContext (cr, string.Format (InsideSvg, RoundedCapacity) + "@" + GetType ().Assembly.FullName, size);
			RenderSvgOnContext (cr, TopSvg + "@" + GetType ().Assembly.FullName, size);
			if (!DockServices.System.OnBattery)
				RenderSvgOnContext (cr, PluggedSvg + "@" + GetType ().Assembly.FullName, size);
		}
		
		public override void Dispose ()
		{
			DockServices.System.BatteryStateChanged -= HandleBatteryStateChanged;
			GLib.Source.Remove (timer);
			base.Dispose ();
		}
	}
}
