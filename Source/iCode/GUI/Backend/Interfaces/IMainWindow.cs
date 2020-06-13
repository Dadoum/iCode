using iCode.GUI.Backend.Interfaces.Panels;

namespace iCode.GUI.Backend.Interfaces
{
	public interface IMainWindow : IWindow
	{
		public IOutputWidget OutputWidget { get; }
		public IIssuesWidget IssuesWidget { get; }
		public ICodeWidget CodeWidget { get; }
		public IPropertiesWidget PropertiesWidget { get; }
		public IProjectExplorerWidget ProjectExplorerWidget { get; }
		
		public void SetProgressMaxValue(double value);
		public void SetProgressValue(double value);
		public void AddProgressValue(double value);
		public void SetStatusText(string text);
	}
}