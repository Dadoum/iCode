using System;
using System.Collections.ObjectModel;
using Gtk;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.Plist;
using iCode.Utils;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
{
	public class DeviceSelectorWindow : Dialog
	{
		Builder _builder;

		public string AttributesPlist;

#pragma warning disable 649
		[UI] private Gtk.Button _okButton;
		[UI] private Gtk.Button _cancelButton;

		[UI] private TreeView _devicesList;
#pragma warning restore 649

		public static DeviceSelectorWindow Create()
		{
			Builder builder = new Builder(null, "DeviceSelector", null);
			return new DeviceSelectorWindow(builder, builder.GetObject("DeviceSelectorWindow").Handle);
		}

		private DeviceSelectorWindow(Builder builder, IntPtr handle) : base(handle)
		{
			this._builder = builder;
			builder.Autoconnect(this);
			this.Icon = Identity.ApplicationIcon;

			_okButton.Clicked += (sender, e) =>
			{
				TreeIter outp;
				_devicesList.Selection.GetSelected(out _, out outp);

				var iDevice = LibiMobileDevice.Instance.iDevice;
				var lockdown = LibiMobileDevice.Instance.Lockdown;
				var pList = LibiMobileDevice.Instance.Plist;

				iDeviceHandle deviceHandle;
				iDevice.idevice_new(out deviceHandle, (string)(_devicesList.Model as ListStore).GetValue(outp, 1)).ThrowOnError();

				LockdownClientHandle lockdownHandle;
				lockdown.lockdownd_client_new_with_handshake(deviceHandle, out lockdownHandle, Identity.ApplicationName).ThrowOnError();

				/*PlistHandle producttype;
				Lockdown.lockdownd_get_value(lockdownHandle, null, "ProductType", out producttype);
    
				PlistHandle version;
				Lockdown.lockdownd_get_value(lockdownHandle, null, "ProductVersion", out version);
    
				PlistHandle build;
				Lockdown.lockdownd_get_value(lockdownHandle, null, "BuildVersion", out build);
    
				PList.plist_get_string_val(version, out selectedDevice.iOSVersion);
				PList.plist_get_string_val(producttype, out selectedDevice.DeviceNumber);
				PList.plist_get_string_val(build, out selectedDevice.BuildNumber);
    
				build.Dispose();
				producttype.Dispose();
				version.Dispose();*/

				PlistHandle product;
				lockdown.lockdownd_get_value(lockdownHandle, null, null, out product);

				uint a = 20;
				string xml;
				pList.plist_to_xml(product, out xml, ref a);

				AttributesPlist = xml;

				deviceHandle.Dispose();
				lockdownHandle.Dispose();

				Respond(ResponseType.Ok);
				this.Dispose();
			};

			_cancelButton.Clicked += (sender, e) =>
			{
				Respond(ResponseType.Cancel);
				this.Dispose();
			};

			var list = new ListStore(typeof(string) /*Name*/, typeof(string) /*UDID*/);
			_devicesList.Model = list;


			var cb = new CellRendererText();
			var column = new TreeViewColumn();
			column.PackStart(cb, false);
			column.AddAttribute(cb, "text", 0);
			column.Title = "Device name";
			_devicesList.AppendColumn(column);

			var column1 = new TreeViewColumn();
			var ct = new CellRendererText();
			column1.PackStart(ct, false);
			column1.AddAttribute(ct, "text", 1);
			column1.Title = "UDID";
			_devicesList.AppendColumn(column1);

			ReadOnlyCollection<string> udids;
			int count = 0;

			var idevice = LibiMobileDevice.Instance.iDevice;
			var lockdown = LibiMobileDevice.Instance.Lockdown;

			var ret = idevice.idevice_get_device_list(out udids, ref count);

			if (ret == iDeviceError.NoDevice)
			{
				Extensions.ShowMessage(MessageType.Error, "Cannot launch.", "No device connected", this);
				this.Respond(ResponseType.Cancel);
				this.Dispose();
				this.Dispose();
			}
			else
			{
				ret.ThrowOnError();

				// Get the device name
				foreach (var udid in udids)
				{
					iDeviceHandle deviceHandle;
					idevice.idevice_new(out deviceHandle, udid).ThrowOnError();

					LockdownClientHandle lockdownHandle;
					lockdown.lockdownd_client_new_with_handshake(deviceHandle, out lockdownHandle, "Quamotion").ThrowOnError();

					string deviceName;
					lockdown.lockdownd_get_device_name(lockdownHandle, out deviceName).ThrowOnError();

					((ListStore)_devicesList.Model).AppendValues(deviceName, udid);

					deviceHandle.Dispose();
					lockdownHandle.Dispose();
				}
			}
		}
	}
}