//  
// Copyright (C) 2009 Robert Dyer
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Linq;
using System.Collections.Generic;
using System.Web;

using Cairo;
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace GMail
{
	/// <summary>
	/// </summary>
	public class GMailDockItem : AbstractDockItem
	{
		public override string UniqueID ()
		{
			return "GMailDockItem#" + Atom.CurrentLabel;
		}
		
		public bool Visible {
			get { return Atom.UnreadCount > 0 || Atom.CurrentLabel == "Inbox"; }
		}
		
		public GMailAtom Atom { get; protected set; }
		
		GMailItemProvider parent;
		
		public GMailDockItem (string label, GMailItemProvider parent)
		{
			this.parent = parent;
			Atom = new GMailAtom (label);
			
			Atom.GMailChecked += GMailCheckedHandler;
			Atom.GMailChecking += GMailCheckingHandler;
			Atom.GMailFailed += GMailFailedHandler;
		}
		
		
		static int old_count = 0;
		public void GMailCheckedHandler (object obj, EventArgs e)
		{
			if (old_count < Atom.NewCount)
				UpdateAttention (true);
			old_count = Atom.NewCount;
			
			string status = "";
			if (Atom.UnreadCount == 0)
				status = Catalog.GetString ("No unread mail");
			else
				status = string.Format (Catalog.GetPluralString ("{0} unread message", "{0} unread messages", Atom.UnreadCount), Atom.UnreadCount);
			HoverText = Atom.CurrentLabel + " - " + status;
			
			parent.ItemVisibilityChanged (this, Visible);
			QueueRedraw ();
		}
		
		void UpdateAttention (bool status)
		{
			if (!GMailPreferences.NeedsAttention)
				return;
			
			State = status ? ItemState.Urgent : ItemState.Wait;
			Indicator = status ? ActivityIndicator.Single : ActivityIndicator.None;
		}
		
		public void GMailCheckingHandler (object obj, EventArgs e)
		{
			UpdateAttention (false);
			
			HoverText = Catalog.GetString ("Checking mail...");
			QueueRedraw ();
		}
		
		public void GMailFailedHandler (object obj, GMailErrorArgs e)
		{
			UpdateAttention (false);
			
			HoverText = e.Error;
			QueueRedraw ();
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			int size = Math.Min (surface.Width, surface.Height);
			Context cr = surface.Context;
			
			string icon = Atom.Icon;
			if (Atom.State == GMailState.ManualReload || Atom.State == GMailState.Error)
				icon = Atom.DisabledIcon;
			
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (icon, size))
			{
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, 0, 0);
				if (Atom.State == GMailState.ManualReload || !Atom.HasUnread)
					cr.PaintWithAlpha (.5);
				else
					cr.Paint ();
			}
			
			BadgeText = "";
			if (Atom.HasUnread)
				BadgeText += Atom.UnreadCount;
			
			// no need to draw the label for the Inbox
			if (Atom.CurrentLabel == "Inbox")
				return;
			
			Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
			layout.Width = Pango.Units.FromPixels (size);
			layout.Ellipsize = Pango.EllipsizeMode.None;
			layout.FontDescription = new Gtk.Style().FontDescription;
			layout.FontDescription.Weight = Pango.Weight.Bold;
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (size / 5);
			
			layout.SetText (Atom.CurrentLabel);
			
			Pango.Rectangle inkRect, logicalRect;
			layout.GetPixelExtents (out inkRect, out logicalRect);
			cr.MoveTo ((size - inkRect.Width) / 2, size - logicalRect.Height);
			
			Pango.CairoHelper.LayoutPath (cr, layout);
			cr.LineWidth = 2;
			cr.Color = new Cairo.Color (0, 0, 0, 0.4);
			cr.StrokePreserve ();
			cr.Color = new Cairo.Color (1, 1, 1, 0.6);
			cr.Fill ();
		}
		
		void OpenInbox ()
		{
			string[] login = GMailPreferences.User.Split (new char[] {'@'});
			string domain = login.Length > 1 ? login [1] : "gmail.com";
			string url = "https://mail.google.com/";
			
			// add the domain
			if (domain == "gmail.com" || domain == "googlemail.com")
				url += "mail";
			else
				url += "a/" + domain;
			
			url += "/\\#";

			// going to a custom label
			if (Atom.CurrentLabel != "Inbox")
				url += "label/";
			
			DockServices.System.Open (url + HttpUtility.UrlEncode (Atom.CurrentLabel));
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				UpdateAttention (false);
				
				OpenInbox ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		public override void Dispose ()
		{
			Atom.GMailChecked -= GMailCheckedHandler;
			Atom.GMailChecking -= GMailCheckingHandler;
			Atom.GMailFailed -= GMailFailedHandler;
			Atom.Dispose ();

			base.Dispose ();
		}
		
		public override MenuList GetMenuItems ()
		{
			MenuList list = base.GetMenuItems ();
			
			UpdateAttention (false);
			
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("View ") + Atom.CurrentLabel,
					Atom.Icon,
					delegate {
						Clicked (1, Gdk.ModifierType.None, 0, 0);
					}));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Compose Mail"),
					"mail-message-new",
					delegate {
						DockServices.System.Open ("https://mail.google.com/mail/#compose");
					}));
			
			list[MenuListContainer.Actions].Add (new SeparatorMenuItem ());
			
			if (Atom.HasUnread) {
				foreach (UnreadMessage message in Atom.Messages.Take (10))
					list[MenuListContainer.Actions].Add (new GMailMenuItem (message, Atom.Icon));
				
				list[MenuListContainer.Actions].Add (new SeparatorMenuItem ());
			}
			
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Settings"),
					Gtk.Stock.Preferences,
					delegate {
						if (GMailConfigurationDialog.instance == null)
							GMailConfigurationDialog.instance = new GMailConfigurationDialog ();
						GMailConfigurationDialog.instance.Show ();
					}));
			
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Check Now"),
					Gtk.Stock.Refresh,
					delegate {
						Atom.ResetTimer (true);
					}));
			
			return list;
		}
	}
}
