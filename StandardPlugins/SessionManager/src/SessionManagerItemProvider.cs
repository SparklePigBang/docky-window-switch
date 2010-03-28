//  
//  Copyright (C) 2010 Claudio Melis
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
using System.Xml;
using System.Linq;
using System.Collections.Generic;

using GLib;

using Docky.Items;

namespace SessionManager
{
	public class SessionManagerItemProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation

		public override string Name {
			get { 
				return "SessionManager"; 
			}
		}

		public override string Icon {
			get { 
				return "gnome-session"; 
			}
		}

		#endregion

		SessionManagerItem session_manager;

		public SessionManagerItemProvider ()
		{
			session_manager = new SessionManagerItem ();
			Items = session_manager.AsSingle<AbstractDockItem> ();
		}

		#region IDisposable implementation

		public override void Dispose ()
		{
			session_manager.Dispose ();
		}
		
		#endregion
		
		
	}
	
	
}