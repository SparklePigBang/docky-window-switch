#!/usr/bin/env python

#  
#  Copyright (C) 2010 Rico Tzschichholz
# 
#  This program is free software: you can redistribute it and/or modify
#  it under the terms of the GNU General Public License as published by
#  the Free Software Foundation, either version 3 of the License, or
#  (at your option) any later version.
# 
#  This program is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#  GNU General Public License for more details.
# 
#  You should have received a copy of the GNU General Public License
#  along with this program.  If not, see <http://www.gnu.org/licenses/>.
#

import atexit
import gobject
import dbus
import dbus.glib
import glib
import sys
import os
import math

try:
	from deluge.ui.client import sclient
	from docky.docky import DockyItem, DockySink
	from signal import signal, SIGTERM
	from sys import exit
except ImportError, e:
	print e
	exit()

ShowUploadRate = False

def bytes2ratestr(bytes):
	if bytes >= 1073741824:
		scaled_bytes = round((bytes / 1073741824.0), 2)
		unit = 'GB'
	elif bytes >= 1048576:
		scaled_bytes = round((bytes / 1048576.0), 1)
		if scaled_bytes >= 100:
			scaled_bytes = int(scaled_bytes)
		unit = 'MB'
	elif bytes >= 1024:
		scaled_bytes = int(bytes / 1024)
		unit = 'kB'
	else:
		scaled_bytes = round((bytes / 1024.0), 1)
		unit = 'kB'

	return str(scaled_bytes) + unit

class DockyLifereaItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)
		
		self.timer = 0
		
		sclient.set_core_uri()
	
		if not self.timer > 0:
			self.timer = gobject.timeout_add (2000, self.update_badge)

	def update_badge(self):
		rate = 0
		try:
			if ShowUploadRate:
				rate = round(sclient.get_upload_rate())
			else:	
				rate = round(sclient.get_download_rate())
	
			if rate > 0:
				self.iface.SetBadgeText("%s" % bytes2ratestr(rate))
			else:
				self.iface.ResetBadgeText()
			return True
		except Exception, e:
			print e
			self.iface.ResetBadgeText()
			return False
	

class DockyLifereaSink(DockySink):
	def item_path_found(self, pathtoitem, item):
		if item.GetOwnsDesktopFile() and item.GetDesktopFile().endswith ("deluge.desktop"):
			self.items[pathtoitem] = DockyLifereaItem(pathtoitem)

dockysink = DockyLifereaSink()

def cleanup ():
	dockysink.dispose ()

if __name__ == "__main__":
	mainloop = gobject.MainLoop(is_running=True)

	atexit.register (cleanup)
	signal(SIGTERM, lambda signum, stack_frame: exit(1))

	mainloop.run()