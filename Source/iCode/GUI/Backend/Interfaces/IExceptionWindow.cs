using System;

namespace iCode.GUI.Backend.Interfaces
{
	public interface IExceptionWindow : IWindow
	{
		public void ShowException(Exception e, IWidget parent);
	}
}