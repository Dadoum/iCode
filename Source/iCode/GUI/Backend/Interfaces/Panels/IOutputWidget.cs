namespace iCode.GUI.Backend.Interfaces.Panels
{
	public interface IOutputWidget : IWidget
	{
		public int Run(System.Diagnostics.Process p, int action);
	}
}