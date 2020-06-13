using System;
using System.Collections.Generic;
using System.Linq;
using Gdk;
using Gtk;
using iCode.Native.GtkSource;
using iCode.Projects;
using iCode.Utils;
using NClang;
using Buffer = iCode.Native.GtkSource.Buffer;
using File = System.IO.File;
using Key = Gdk.Key;
using Task = System.Threading.Tasks.Task;

namespace iCode.GUI.GTK3.Tabs
{
	public class CodeTabWidget : View
	{
		#region Frontend
		private NotebookTabLabel _notebookTabLabel;
		private Class _class;
		private Task _highlightingThread;
		
		public CodeTabWidget(Class @class)
		{
			_notebookTabLabel = new NotebookTabLabel(@class.Filename, this);
			_class = @class;
			
			// Initialize settings
			this.ShowLineMarks = true;
			this.ShowLineNumbers = true;
			this.InsertSpacesInsteadOfTabs = false;
			this.TabWidth = (uint) Program.Settings.GetSettingsEntry("tab_width");

			this.KeyPressEvent += (o, args) =>
			{
				if ((args.Event.Key == Key.S || args.Event.Key == Key.s) &&
					args.Event.State == ModifierType.ControlMask)
				{
					File.WriteAllText(System.IO.Path.Combine(ProjectManager.Project.Path, @class.Filename),
						this.Buffer.Text);
					this.Buffer.Modified = false;
					_notebookTabLabel.Label.Text = @class.Filename;
				}
			};
			
			CssProvider css = new CssProvider(); // Monospace
			css.LoadFromData
			(@"
textview
{
	font-family: monospace;
} 
");
			this.StyleContext.AddProvider(css, 1);
			
			// Initialize Buffer
			{
				this.Buffer = new Buffer(new LanguageManager().GetLanguage("none"));
				this.Buffer.Text = File.ReadAllText(System.IO.Path.Combine(ProjectManager.Project.Path, @class.Filename));
				
				this.Buffer.Changed += (sender, args) =>
				{
					_recoloringRequested = true;
					
					if (this.Buffer.Modified && !_notebookTabLabel.Label.Text.EndsWith("*"))
					{
						_notebookTabLabel.Label.Text += "*";
					}
				};
				
				_highlightingThread = new Task(Highlight);
				_highlightingThread.Start();
				_recoloringRequested = true;
			}

			// Initialize tags
			{
				// Error detection tags
				TextTag error = new TextTag("error");
				error.Underline = Pango.Underline.Error;
				this.Buffer.TagTable.Add(error);
				error.Dispose();
			
				TextTag warn = new TextTag("warning");
				warn.Underline = Pango.Underline.Error;
				warn.UnderlineRgba = new RGBA
				{
					Red = 100,
					Green = 100,
					Blue = 0
				};
				this.Buffer.TagTable.Add(warn);
				warn.Dispose();
				
				// Syntax coloring tags
			}
		}

		public NotebookTabLabel GetLabel()
		{
			return _notebookTabLabel;
		}
		
		#endregion
		
		#region Backend

		private bool _recoloringRequested;
		private ClangIndex _index;
		private ClangTranslationUnit _translationUnit;
		
		private void Highlight()
		{
			while (true)
			{
				// Wait for request
				while (!_recoloringRequested) ;
				_recoloringRequested = false;
				
				File.WriteAllText(System.IO.Path.Combine(
					ProjectManager.Project.Path,
					"~" + _class.Filename), this.Buffer.Text);
			
				// iCode stores all unsaved files next to the saved ones, with ~ just before filename
				// For each class loaded, we search if there is a file with a ~, if yes, we take it and create unsaved file from it
				// Else, we use the saved file. We read file contents and then convert the result to an array.
				ClangUnsavedFile[] unsavedFiles = ProjectManager.Project.Classes
																.Select(a =>
																	 new ClangUnsavedFile(
																		 a.Filename,
																		 File.ReadAllText(
																			 File.Exists(System.IO.Path.Combine(
																				 ProjectManager.Project.Path,
																				 "~" + a.Filename))
																				 ? System.IO.Path.Combine(
																					 ProjectManager.Project.Path,
																					 "~" + a.Filename)
																				 : System.IO.Path.Combine(
																					 ProjectManager.Project.Path,
																					 a.Filename)))).ToArray();
			
				// Initialize index
				if (_index == null)
				{
					_index = ClangService.CreateIndex();

					// Crate translation unit with temporary file, 
					_translationUnit = _index.ParseTranslationUnit(System.IO.Path.Combine(
							ProjectManager.Project.Path,
							"~" + _class.Filename), _class.CompilerFlags.Concat(ProjectManager.Flags).ToArray(),
						unsavedFiles, TranslationUnitFlags.None);
				}
				else
				{
					_translationUnit.Reparse(unsavedFiles, ReparseTranslationUnitFlags.None);
				}

				List<ClangToken> tokens = _translationUnit.Tokenize(new ClangSourceRange(2, this.Buffer.EndIter.Offset + 2)).Tokens.ToList();
				Console.WriteLine(string.Join(" ", tokens.Select(t => t.Location.FileLocation.Offset)) + $" [Length: {tokens.Count}]");

				this.Buffer.RemoveAllTags(this.Buffer.StartIter, this.Buffer.EndIter);
				
				foreach (var diagnostic in _translationUnit.DiagnosticSet.Items)
				{
					var problematicToken = ExtractToken(tokens, diagnostic.Location);
					string severity;
					if (diagnostic.Severity == DiagnosticSeverity.Error ||
						(diagnostic.Severity == DiagnosticSeverity.Fatal && diagnostic.Location.IsFromMainFile))
						severity = "error";
					else
						severity = "warning";
					
					Console.WriteLine("A " + severity + " has been found in " + _class.Filename + " ! \"" + diagnostic.Spelling + "\" at " + problematicToken.Offset + " (\"" + problematicToken.Token + "\")");
					
					if (problematicToken.IsFromMainFile)
						this.Buffer.ApplyTag(severity, this.Buffer.GetIterAtOffset(problematicToken.Offset), this.Buffer.GetIterAtOffset(problematicToken.Offset + problematicToken.Token.Length));
					
					Console.WriteLine("Applied tag to show the " + severity + " to user.");
				}
			}
		}

		private ArtificialToken ExtractToken(List<ClangToken> tokens, ClangSourceLocation location)
		{
			if (tokens.Count == 0)
			{
				for (var i = 0; i < tokens.Count; i++)
					if (tokens[i].Location.FileLocation.Offset <= location.FileLocation.Offset &&
						location.FileLocation.Offset < tokens[i + 1].Location.FileLocation.Offset)
						return new ArtificialToken
						{
							Offset = tokens[i].Location.FileLocation.Offset,
							Token = tokens[i].Spelling,
							IsFromMainFile = true
						};
			}
			else
			{
				string text;
				bool isFromMainFile;
				if (System.IO.Path.GetFileName(location.FileLocation.File.FileName) == _class.Filename)
				{
					text = this.Buffer.GetText(this.Buffer.GetIterAtOffset(0), this.Buffer.GetIterAtOffset(this.Buffer.CharCount - 1),
						false);
					isFromMainFile = true;
				}
				else
				{
					text = File.ReadAllText(location.FileLocation.File.ToString());
					isFromMainFile = false;
				}
				
				var stringTokens = text.SplitWithDelims(';', ' ', '\n', ';', '(', ')', '[', ']', '.', ',', '=', '<', '>',
					'+', '-', '/', '*', '%', '@', '"', '\'', '\r');
				
				var start = 0;
				var end = 0;
				string token = "";
				
				for (int i = 0; i < stringTokens.Count; i++)
				{
					end += stringTokens[i].Length;

					if (start < location.SpellingLocation.Offset + 1 &&
						location.SpellingLocation.Offset + 1 <= end)
					{
						token = stringTokens[i];
						break;
					}

					start += stringTokens[i].Length;
				}

				return new ArtificialToken
				{
					Offset = start,
					Token = token,
					IsFromMainFile = isFromMainFile
				};
			}
			
			throw new InvalidOperationException("Sequence contains no matching element");
		}

		private struct ArtificialToken
		{
			internal int Offset;
			internal string Token;
			internal bool IsFromMainFile;
		}
		
		#endregion
		
	}
}