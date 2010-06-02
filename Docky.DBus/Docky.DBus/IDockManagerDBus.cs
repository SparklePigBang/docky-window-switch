//  
//  Copyright (C) 2010 Robert Dyer
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

using org.freedesktop.DBus;
using NDesk.DBus;

namespace Docky.DBus
{
	public delegate void ItemChangedHandler (string path);

	[Interface("org.freedesktop.DockManager")]
	public interface IDockManagerDBus
	{
		event ItemChangedHandler ItemAdded;
		
		event ItemChangedHandler ItemRemoved;
		
		string[] GetCapabilities ();
		
		string[] GetItems ();
		
		string[] GetItemsByName (string name);
		
		string[] GetItemsByDesktopFile (string path);
		
		string[] GetItemsByPID (uint id);
		
		string GetItemByXid (uint xid);
	}
}