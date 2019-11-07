using System;
using System.Collections.ObjectModel;
using Gtk;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.Plist;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode
{
    public class DeviceSelectorWindow : Dialog
    {
        Builder builder;

        public string attributesPlist;

#pragma warning disable 649
        [UI] private Gtk.Button okButton;
        [UI] private Gtk.Button cancelButton;

        [UI] private TreeView devicesList;
#pragma warning restore 649

        public static DeviceSelectorWindow Create()
        {
            Builder builder = new Builder(null, "DeviceSelector", null);
            return new DeviceSelectorWindow(builder, builder.GetObject("DeviceSelectorWindow").Handle);
        }

        private DeviceSelectorWindow(Builder builder, IntPtr handle) : base(handle)
        {
            this.builder = builder;
            builder.Autoconnect(this);

            okButton.Clicked += (sender, e) =>
            {
                TreeIter outp;
                devicesList.Selection.GetSelected(out _, out outp);

                var iDevice = LibiMobileDevice.Instance.iDevice;
                var Lockdown = LibiMobileDevice.Instance.Lockdown;
                var PList = LibiMobileDevice.Instance.Plist;

                iDeviceHandle deviceHandle;
                iDevice.idevice_new(out deviceHandle, (string)(devicesList.Model as ListStore).GetValue(outp, 1)).ThrowOnError();

                LockdownClientHandle lockdownHandle;
                Lockdown.lockdownd_client_new_with_handshake(deviceHandle, out lockdownHandle, "iCode").ThrowOnError();

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
                Lockdown.lockdownd_get_value(lockdownHandle, null, null, out product);

                uint a = 20;
                string xml;
                PList.plist_to_xml(product, out xml, ref a);

                attributesPlist = xml;

                deviceHandle.Dispose();
                lockdownHandle.Dispose();

                Respond(ResponseType.Ok);
                this.Destroy();
            };

            cancelButton.Clicked += (sender, e) =>
            {
                Respond(ResponseType.Cancel);
                this.Destroy();
            };

            var list = new ListStore(typeof(string) /*Name*/, typeof(string) /*UDID*/);
            devicesList.Model = list;


            var cb = new CellRendererText();
            var column = new TreeViewColumn();
            column.PackStart(cb, false);
            column.AddAttribute(cb, "text", 0);
            column.Title = "Device name";
            devicesList.AppendColumn(column);

            var column1 = new TreeViewColumn();
            var ct = new CellRendererText();
            column1.PackStart(ct, false);
            column1.AddAttribute(ct, "text", 1);
            column1.Title = "UDID";
            devicesList.AppendColumn(column1);

            ReadOnlyCollection<string> udids;
            int count = 0;

            var idevice = LibiMobileDevice.Instance.iDevice;
            var lockdown = LibiMobileDevice.Instance.Lockdown;

            var ret = idevice.idevice_get_device_list(out udids, ref count);

            if (ret == iDeviceError.NoDevice)
            {
                Extensions.ShowMessage(MessageType.Error, "Cannot launch.", "No device connected", this);
                this.Respond(ResponseType.Cancel);
                this.Destroy();
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

                    ((ListStore)devicesList.Model).AppendValues(deviceName, udid);

                    deviceHandle.Dispose();
                    lockdownHandle.Dispose();
                }
            }
        }
    }
}
