using System;
using System.Runtime.InteropServices;

namespace iCode.GUI.Backend
{
	public class UIHelper
	{
		public static int ShowModal(string title, string text, ModalCategory category, ModalActions actions)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				iCode.GUI.GTK3.GladeUI.GladeHelper.ShowModal(title, text, category, actions);
			}
			
			throw new InvalidInterfaceException(message: "No UI toolkit is available for your operating system.");
		}
		
		public static T CreateFromInterface<T>() where T : iCode.GUI.Backend.Interfaces.IWidget
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return GTK3.GladeUI.GladeHelper.CreateFromInterface<T>();
			
			throw new InvalidInterfaceException(message: "No UI toolkit is available for your operating system.");
		}

		public enum ModalCategory
		{
			Info,
			Error,
			Warning
		}
		
		public enum ModalActions
		{
			YesNo,
			Ok
		}
		
		public class InvalidInterfaceException : Exception
		{
			public InvalidInterfaceException(Exception exception = null, string message = "The toolkit does not implement this UI component.") : base(message, exception)
			{
			}
		}

		public class MixedBackendsException : Exception
		{
			public MixedBackendsException(string message = "Different UI Toolkit has been used in the same instance. ") : base(message)
			{
			}
		}
	}
}