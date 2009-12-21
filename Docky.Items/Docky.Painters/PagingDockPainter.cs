//  
//  Copyright (C) 2009 Robert Dyer
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

using Cairo;
using Gtk;
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Services;

namespace Docky.Painters
{
	public class PagingDockPainter : AbstractDockPainter
	{
		protected const int BUTTON_SIZE = 24;
		protected const int ICON_SIZE = 16;
		
		protected string prevButtonIcon = "painterleft.svg@" + System.Reflection.Assembly.GetExecutingAssembly ().FullName;
		protected string nextButtonIcon = "painterright.svg@" + System.Reflection.Assembly.GetExecutingAssembly ().FullName;
		
		private Gdk.Rectangle prevButtonRect;
		private Gdk.Rectangle nextButtonRect;
		
		private bool prevHovered;
		private bool nextHovered;
		
		private int page;
		
		/// <value>
		/// Indicates the current page the painter should show.
		/// </value>
		protected int Page {
			get { return page; }
			set {
				if (page == value)
					return;
				LastPage = page;
				page = value;
				
				if (slideTimer > 0)
					GLib.Source.Remove (slideTimer);
				
				slideCounter = 0;
				slideTimer = GLib.Timeout.Add (10, delegate {
					slideCounter++;
					QueueRepaint ();
					return slideCounter < slideSteps;
				});
			}
		}
		
		uint slideTimer;
		int slideCounter;
		int slideSteps = 40;
		
		private int LastPage { get; set; }
		
		/// <value>
		/// The number of pages for this painter.
		/// </value>
		protected int NumPages { get; private set; }
		
		private DockySurface[] buffers;
		
		private DockySurface buttonBuffer;
		
		public PagingDockPainter (int pages) : base ()
		{
			NumPages = pages;
			buffers = new DockySurface [NumPages];
			slideTimer = 0;
			slideCounter = 0;
			Page = 0;
			prevHovered = nextHovered = false;
		}
		
		#region IDockPainter implementation 
		
		protected sealed override void PaintSurface (DockySurface surface)
		{
			surface.Clear ();
			
			lock (buffers) {
				if (slideCounter > 0 && slideCounter < slideSteps) {
					double offset = Allocation.Width * Math.Log (slideCounter) / Math.Log (slideSteps);
					
					if ((LastPage > Page && !(LastPage == NumPages - 1 && Page == 0)) || (LastPage == 0 && Page == NumPages - 1)) {
						ShowBuffer (surface, Page, offset - Allocation.Width);
						ShowBuffer (surface, LastPage, offset);
					} else {
						ShowBuffer (surface, Page, Allocation.Width - offset);
						ShowBuffer (surface, LastPage, 0 - offset);
					}
					
					// fade out the edges during a slide
					Gradient linpat = new LinearGradient(0, surface.Height / 2, surface.Width, surface.Height / 2);
					linpat.AddColorStop(0, new Color(1, 1, 1, 1));
					linpat.AddColorStop(2 * (double) BUTTON_SIZE / surface.Width, new Color(1, 1, 1, 0));
					linpat.AddColorStop(1 - 2 * (double) BUTTON_SIZE / surface.Width, new Color(1, 1, 1, 0));
					linpat.AddColorStop(1, new Color(1, 1, 1, 1));
						
					surface.Context.Save();
					surface.Context.Operator = Operator.Source;
					surface.Context.Color = new Cairo.Color (0, 0, 0, 0);
					surface.Context.Mask (linpat);
					surface.Context.PaintWithAlpha (0);
					surface.Context.Restore ();
					linpat.Destroy ();
				} else {
					ShowBuffer (surface, Page, 0);
				}
			}
			
			if (buttonBuffer != null && (buttonBuffer.Width != surface.Width || buttonBuffer.Height != surface.Height))
				ResetButtons ();
			if (buttonBuffer == null) {
				buttonBuffer = new DockySurface (surface.Width, surface.Height, surface);
				DrawButtonsBuffer ();
			}
			buttonBuffer.Internal.Show (surface.Context, 0, 0);
		}
		
		void ShowBuffer (DockySurface surface, int page, double x)
		{
			// ensure the buffer size matches
			if (buffers [page] != null)
				if (surface.Width != buffers [page].Width || surface.Height != buffers [page].Height)
					ResetBuffers ();
			
			// the buffer is empty
			if (buffers [page] == null) {
				buffers [page] = new DockySurface (surface.Width, surface.Height, surface);
				DrawPageOnSurface (page, buffers [page]);
			}
			
			buffers [page].Internal.Show (surface.Context, x, 0);
		}
		
		#endregion
		
		void DrawButtonsBuffer ()
		{
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (prevButtonIcon, ICON_SIZE))
			{
				Gdk.CairoHelper.SetSourcePixbuf (buttonBuffer.Context, pbuf,
						prevButtonRect.X + (prevButtonRect.Width - ICON_SIZE) / 2,
						prevButtonRect.Y + (prevButtonRect.Height - ICON_SIZE) / 2);
				buttonBuffer.Context.Paint ();
				if (prevHovered) {
					buttonBuffer.Context.Operator = Operator.Add;
					Gdk.CairoHelper.SetSourcePixbuf (buttonBuffer.Context, pbuf,
							prevButtonRect.X + (prevButtonRect.Width - ICON_SIZE) / 2,
							prevButtonRect.Y + (prevButtonRect.Height - ICON_SIZE) / 2);
					buttonBuffer.Context.PaintWithAlpha (0.4);
					buttonBuffer.Context.Operator = Operator.Over;
				}
			}
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (nextButtonIcon, ICON_SIZE))
			{
				Gdk.CairoHelper.SetSourcePixbuf (buttonBuffer.Context, pbuf,
						nextButtonRect.X + (nextButtonRect.Width - ICON_SIZE) / 2,
						nextButtonRect.Y + (nextButtonRect.Height - ICON_SIZE) / 2);
				buttonBuffer.Context.Paint ();
				if (nextHovered) {
					buttonBuffer.Context.Operator = Operator.Add;
					Gdk.CairoHelper.SetSourcePixbuf (buttonBuffer.Context, pbuf,
							nextButtonRect.X + (nextButtonRect.Width - ICON_SIZE) / 2,
							nextButtonRect.Y + (nextButtonRect.Height - ICON_SIZE) / 2);
					buttonBuffer.Context.PaintWithAlpha (0.4);
					buttonBuffer.Context.Operator = Operator.Over;
				}
			}
		}
		
		protected virtual void DrawPageOnSurface (int page, DockySurface surface)
		{
		}
		
		protected override void OnAllocationSet (Gdk.Rectangle allocation)
		{
			prevButtonRect = new Gdk.Rectangle (0, 0, BUTTON_SIZE, allocation.Height);
			nextButtonRect = new Gdk.Rectangle (allocation.Width - BUTTON_SIZE, 0, BUTTON_SIZE, allocation.Height);
		}
		
		internal override void OnMotionNotify (int x, int y, Gdk.ModifierType mod)
		{
			bool prev = prevButtonRect.Contains (x, y);
			bool next = nextButtonRect.Contains (x, y);
			
			if (prev != prevHovered || next != nextHovered) {
				prevHovered = prev;
				nextHovered = next;
				ResetButtons ();
				QueueRepaint ();
			}
		}
		
		public void NextPage ()
		{
			if (Page < NumPages - 1)
				Page++;
			else
				Page = 0;
		}
		
		public void PreviousPage ()
		{
			if (Page > 0)
				Page--;
			else
				Page = NumPages - 1;
		}
		
		protected override void OnButtonReleased (int x, int y, Gdk.ModifierType mod)
		{
			if (prevButtonRect.Contains (x, y))
				PreviousPage ();
			else if (nextButtonRect.Contains (x, y))
				NextPage ();
			else
				base.OnButtonReleased (x, y, mod);
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, int x, int y, Gdk.ModifierType type)
		{
			if (direction == Gdk.ScrollDirection.Down || direction == Gdk.ScrollDirection.Right)
				NextPage ();
			else if (direction == Gdk.ScrollDirection.Up || direction == Gdk.ScrollDirection.Left)
				PreviousPage ();
		}
		
		protected override void OnShown ()
		{
			slideCounter = 0;
			page = 0;
			ResetBuffers ();
			QueueRepaint ();
		}
		
		protected void ResetBuffers ()
		{
			lock (buffers) {
				for (int i = 0; i < buffers.Length; i++)
					if (buffers [i] != null)
					{
						buffers [i].Dispose ();
						buffers [i] = null;
					}
			}
		}
		
		private void ResetButtons ()
		{
			if (buttonBuffer != null) {
				buttonBuffer.Dispose ();
				buttonBuffer = null;
			}
		}
		
		public override void Dispose ()
		{
			ResetBuffers ();
			base.Dispose ();
		}
	}
}
