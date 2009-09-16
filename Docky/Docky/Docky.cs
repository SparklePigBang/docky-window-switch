//  
//  Copyright (C) 2009 Jason Smith
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
using System.ComponentModel;
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Gtk;

namespace Docky
{


	public static class Docky
	{

		public static UserArgs CommandLinePreferences { get; private set; }
		
		static DockController controller;
		internal static DockController Controller { 
			get {
				if (controller == null)
					controller = new DockController ();
				return controller;
			}
		}
		
		
		public static void Main (string[] args)
		{
			CommandLinePreferences = new UserArgs (args);
			
			//Init gtk and related
			Gdk.Threads.Init ();
			Gtk.Application.Init ("Docky", ref args);
			Gnome.Vfs.Vfs.Initialize ();
			
			Windowing.ScreenUtils.Initialize ();
			Wnck.Global.ClientType = Wnck.ClientType.Pager;
			
			Controller.Initialize ();
			
			ConfigurationWindow config = new ConfigurationWindow ();
			config.Show ();
			
			Gdk.Threads.Enter ();
			Gtk.Application.Run ();
			Gdk.Threads.Leave ();
			
			Gnome.Vfs.Vfs.Shutdown ();
		}
	}
}
