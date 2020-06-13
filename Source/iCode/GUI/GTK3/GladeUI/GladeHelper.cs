using System;
using System.Linq;
using Gtk;
using iCode.GUI.Backend;
using iCode.GUI.Backend.Interfaces;

namespace iCode.GUI.GTK3.GladeUI
{
	public static class GladeHelper 
	{
		public static int ShowModal(string title, string text, UIHelper.ModalCategory messageType, UIHelper.ModalActions buttons)
		{
			MessageDialog md = new MessageDialog(null, DialogFlags.Modal,
				messageType == UIHelper.ModalCategory.Error ? MessageType.Error :
				messageType == UIHelper.ModalCategory.Warning ? MessageType.Warning : MessageType.Info,
				buttons == UIHelper.ModalActions.YesNo ? ButtonsType.YesNo : ButtonsType.Ok, text);
			md.Title = title;
			md.ShowAll();
			md.Present();
			var outp = md.Run();
			md.Dispose();

			return outp;
		}
		
		/// <summary>
		/// Return from iCode.GUI.Interfaces interface a new object implemented with the GTK+ 3
		/// </summary>
		/// <typeparam name="T">Widget that has to be implemented</typeparam>
		/// <returns>The GTK+ 3 implementation of the widget</returns>
		/// <exception cref="UIHelper.InvalidInterfaceException">The provided interface is not implemented in this toolkit, or the implementation is incorrect.</exception>
		public static T CreateFromInterface<T>() where T : IWidget
		{
			try
			{
				try
				{
					// Get widget if it is a glade widget
					var gladeWidgets = typeof(T).Assembly.GetTypes().Where(t => typeof(IGladeWidget).IsAssignableFrom(t));
					var widgetT = gladeWidgets.First(t => typeof(T).IsAssignableFrom(t));
					_ = widgetT.GetConstructor(Type.EmptyTypes);
					return (T) CreateWidget(widgetT);
				}
				catch 
				{
					// Or if it a standard GTK Widget
					var gladeWidgets = typeof(T).Assembly.GetTypes().Where(t => typeof(Widget).IsAssignableFrom(t));
					var widgetT = gladeWidgets.First(t => typeof(T).IsAssignableFrom(t));
					var widget = (T) Activator.CreateInstance(widgetT);
					widget!.Initialize();
					return widget;
				}
			}
			catch (Exception e)
			{
				throw new UIHelper.InvalidInterfaceException(e, $"The toolkit does not implement {typeof(T).FullName}.");
			}
		}

		public static T Create<T>() where T : IGladeWidget, new()
		{
			return (T) CreateWidget(typeof(T));
		}

		private static IGladeWidget CreateWidget(Type t)
		{
			var instance = Activator.CreateInstance(t);
			string resourceName = (string) t.GetProperty("ResourceName")!.GetValue(instance);
			string widgetName = (string) t.GetProperty("WidgetName")!.GetValue(instance);
			Builder builder = new Builder(null, resourceName, null);
			_ = typeof(Widget).GetConstructor(new[] {typeof(IntPtr)})!.Invoke(instance, new object[] { builder.GetObject(widgetName).Handle });
			builder.Autoconnect(instance);
			((IGladeWidget) instance)!.Initialize();
			return (IGladeWidget) instance;
		}
	}
}