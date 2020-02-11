using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Gdk;
using Gtk;
using GtkSourceView;
using iCode.Projects;
using iCode.Utils;
using NClang;
using Pango;

namespace iCode.GUI.Tabs
{
	public class CodeTabWidget : SourceView
	{
		private string _s;
		private readonly Class _file;
		private readonly string _temp;
		private string _actualText;

		private List<Gdk.Key> _keys;
		private Dictionary<ClangDiagnostic, string> _diagnos;

		private NotebookTabLabel _notebookTabLabel;
		private Thread _t;

		private ClangTranslationUnit _trans;
		private ClangIndex _index;
		private ClangTokenSet _tokens;

		private bool _recoloring;
		
		private bool _recolor = true;

		~CodeTabWidget()
		{
			_trans.Dispose();
			_index.Dispose();
			_t.Interrupt();
			Buffer.Dispose();
			this.Dispose();
		}
		
		public CodeTabWidget(Class @class)
		{
			_keys = new List<Gdk.Key>();
			base.Buffer = new SourceBuffer(new TextTagTable());
			_notebookTabLabel = new NotebookTabLabel(System.IO.Path.GetFileName(@class.Filename), this);
			this._file = @class;
			var css = new CssProvider();
			css.LoadFromData(@"
                textview {
                    font-family: Monospace;
                }

			bracket {
				color: magenta;
			}
            ");
			Console.WriteLine($"Loading {@class.Filename}");
			this.StyleContext.AddProvider(css, 1);
			this._temp = System.IO.Path.Combine(ProjectManager.Project.Path, "~" + @class.Filename);
			
			File.Copy(System.IO.Path.Combine(ProjectManager.Project.Path, @class.Filename), _temp, true);
			
			_actualText = File.ReadAllText(System.IO.Path.Combine(ProjectManager.Project.Path, @class.Filename));
			Buffer.Text = _actualText;
			this.KeyPressEvent += Handle_KeyPressEvent;
			this.KeyReleaseEvent += Handle_KeyReleaseEvent;
			base.AutoIndent = true;
			base.ShowLineMarks = true;
			base.ShowLineNumbers = true;
			// base.HighlightCurrentLine = true;
			base.IndentOnTab = true;
			this._s = base.Buffer.Text;
			base.Buffer.Changed += this.Buffer_Changed;
			
			TextTag error = new TextTag("error");
			error.Underline = Pango.Underline.Error;
			Buffer.TagTable.Add(error);
			
			TextTag warn = new TextTag("warning");
			warn.Underline = Pango.Underline.Error;
			warn.UnderlineRgba = new RGBA()
			{
				Red = 100,
				Green = 100,
				Blue = 0
			};
			Buffer.TagTable.Add(warn);
			
			TextTag type = new TextTag("identifier");
			type.ForegroundGdk = Extensions.RgbaFromHex("84C2DE");
			Buffer.TagTable.Add(type);
			
			TextTag k = new TextTag("keyword");
			k.ForegroundGdk = Extensions.RgbaFromHex("4B219E");
			Buffer.TagTable.Add(k);
			
			TextTag ek = new TextTag("comment");
			ek.ForegroundGdk = Extensions.RgbaFromHex("A4A1C1");
			Buffer.TagTable.Add(ek);

			type.Dispose();
			k.Dispose();
			ek.Dispose();
			
			#region Clang init
			
			_index = ClangService.CreateIndex();
			
			var unsavedFiles = ProjectManager.Project.Classes
											 .Select(a =>
												  new ClangUnsavedFile(
													  a.Filename,
													  File.ReadAllText(
														  System.IO.Path.Combine(
															  ProjectManager.Project.Path,
															  a.Filename)))).ToArray();

			//_trans = _index.CreateTranslationUnitFromSourceFile(_temp,
			//	_file.CompilerFlags.Concat(ProjectManager.Flags).ToArray(),
			//	unsavedFiles);

			_trans = _index.ParseTranslationUnit(_temp,
				_file.CompilerFlags.Concat(ProjectManager.Flags).ToArray(),
				unsavedFiles, 
				TranslationUnitFlags.None);
			
			_tokens = _trans.Tokenize(new ClangSourceRange(2, Buffer.EndIter.Offset + 2));
			
			_diagnos = new Dictionary<ClangDiagnostic, string>();
			_t = new Thread(RecolorThread);
			_t.Start();
			
			#endregion
		}

		TextTag GetTagFromHex(double red, double green, double blue)
		{
			TextTag tag = new TextTag(red.ToString(CultureInfo.InvariantCulture) + green.ToString(CultureInfo.InvariantCulture) + blue.ToString(CultureInfo.InvariantCulture));
			tag.ForegroundRgba = new RGBA
			{
				Red = red,
				Green = green,
				Blue = blue
			};
			return tag;
		}

		string GetTokenFrom (ClangSourceLocation location)
		{
			string text;

			if (System.IO.Path.GetFileName(location.FileLocation.File.FileName) == _file.Filename)
			{
				text = Buffer.GetText(Buffer.GetIterAtOffset(0), Buffer.GetIterAtOffset(Buffer.CharCount - 1),
					false);
			}
			else
			{
				text = File.ReadAllText(location.FileLocation.File.ToString());
			}

			var tokens = text.SplitWithDelims(';', ' ', '\n', ';', '(', ')', '[', ']', '.', ',', '=', '<', '>',
				'+', '-', '/', '*', '%', '@', '"', '\'', '\r');

			var start = 0;
			var end = 0;
			string token = "";

			for (int i = 0; i < tokens.Count; i++)
			{
				end += tokens[i].Length;

				if (start < location.SpellingLocation.Offset + 1 &&
					location.SpellingLocation.Offset + 1 <= end)
				{
					token = tokens[i];
					break;
				}

				start += tokens[i].Length;
			}

			return token;
		}

		void RecolorThread()
		{

			// ThIS cOdE IS SeLF DoCUMenTiNg

			while (_recoloring || !_recolor) ;
			_recoloring = true;

			File.WriteAllText(_temp, Buffer.Text);

			_trans.Reparse(ProjectManager.Project.Classes
										 .Select(a =>
											  new ClangUnsavedFile(
												  a.Filename,
												  File.ReadAllText(
													  System.IO.Path.Combine(
														  ProjectManager.Project.Path,
														  a.Filename)))).ToArray(), ReparseTranslationUnitFlags.None);

			_tokens.Dispose();
			_tokens = _trans.Tokenize(new ClangSourceRange(2, Buffer.EndIter.Offset + 2));
			
			Buffer.RemoveTag("error", Buffer.StartIter, Buffer.EndIter);
			Buffer.RemoveTag("warning", Buffer.StartIter, Buffer.EndIter);
			
			#region Error checking
			
			if (_trans.DiagnosticCount == 0)
			{
				Console.WriteLine("Code is clean, no any error found !");
			}

			foreach (var item in _trans.DiagnosticSet.Items)
				try
				{
					var token = GetTokenFrom(item.Location);

					Console.WriteLine(
						$"{item.Severity} at offset {item.Location.SpellingLocation.Offset} ({item.Location.FileLocation.File}):  {token} ({item.Spelling})");

					Gtk.Application.Invoke((o, a) =>
					{
						if (item.Severity == DiagnosticSeverity.Error)
							Buffer.ApplyTag($"error",
								Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset),
								Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset + token.Length));
						else if (item.Severity == DiagnosticSeverity.Warning)
							Buffer.ApplyTag($"warning",
								Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset),
								Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset + token.Length));

					});

					_diagnos.Add(item, token);

				}
				catch (Exception e)
				{
					Console.WriteLine($"The parsing of error failed: {e}");
				}

			#endregion

			var clangTokens = _tokens.Tokens.ToList();
			if (clangTokens.Count() != 0)
				foreach (var token in clangTokens)
				{
					TextTag tag = null;

					switch (token.Kind)
					{
						case TokenKind.Comment:
							Gtk.Application.Invoke((o, a) =>
							{
								Buffer.ApplyTag("comment",
									Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset),
									Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset +
														   token.Spelling.Length));
							});
							break;
						case TokenKind.Identifier:
							Gtk.Application.Invoke((o, a) =>
							{
								Buffer.ApplyTag("identifier",
									Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset),
									Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset +
														   token.Spelling.Length));
							});
							break;
						case TokenKind.Literal:
							if (GetCursorKind(token).ToString().Contains("Attribute"))
							{
								tag = GetTagFromHex(0.706328, 0.872545, 0.381282);
							}
							else if (GetCursorKind(token).ToString().Contains("Float") ||
									 GetCursorKind(token).ToString().Contains("Int") ||
									 GetCursorKind(token).ToString().Contains("Character"))
							{
								tag = GetTagFromHex(0.870818, 0.832689, 0.24896);
							}
							else if (GetCursorKind(token).ToString().Contains("String"))
							{
								tag = GetTagFromHex(0.609991, 0.725519, 0.809682);
							}
							else if (GetCursorKind(token).ToString().Contains("Constant"))
							{
								tag = GetTagFromHex(0.72719, 0.59597, 0.87424);
							}
							else if (GetCursorKind(token).ToString().Contains("Preprocessing"))
							{
								tag = GetTagFromHex(0.663419, 0.64803, 0.849437);
							}
							else
							{
								tag = GetTagFromHex(0.926027, 0.924097, 0.966442);
							}

							Gtk.Application.Invoke((o, a) =>
							{
								Buffer.ApplyTag(tag, Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset),
									Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset +
														   token.Spelling.Length));
								tag.Dispose();
							});
							break;
						case TokenKind.Keyword:
							Gtk.Application.Invoke((o, a) =>
							{
								Buffer.ApplyTag("keyword",
									Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset),
									Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset +
														   token.Spelling.Length));
							});
							break;
						default:
							Gtk.Application.Invoke((o, a) =>
							{
								var tag = GetTagFromHex(0.926027, 0.924097, 0.966442);
								Buffer.ApplyTag(tag,
									Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset),
									Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset +
														   token.Spelling.Length));
								tag.Dispose();
							});
							break;
					}

				}
			else
				Console.WriteLine("Weird");

			_recoloring = _recolor = false;
		}

		CursorKind GetCursorKind(ClangToken token)
		{
			return _trans.GetCursor(_trans.GetLocationForOffset(_trans.GetFile(_temp),
				token.Location.SpellingLocation.Offset + (token.Spelling.Length / 2))).Kind;
		}

		private void Buffer_Changed(object sender, EventArgs e)
		{
			if (!_recolor)
				_recolor = true;

			if (_t.ThreadState != ThreadState.Running)
			{
				_t = new Thread(RecolorThread);
				_t.Start();
			}

			#region CODE SAVING
			if (_actualText != Buffer.Text)
			{
				if (!_notebookTabLabel.Label.Text.EndsWith("*", StringComparison.CurrentCulture))
				{
					_notebookTabLabel.Label.Text += "*";
				}
			}
			else
			{
				if (_notebookTabLabel.Label.Text.EndsWith("*", StringComparison.CurrentCulture))
				{
					_notebookTabLabel.Label.Text = _notebookTabLabel.Label.Text.TrimEnd('*');
				}
			}
			#endregion

			if (e == null)
				return;

			#region CODE COMPLETION
			int cursorPosition = base.Buffer.CursorPosition;
			bool flag = this._s.Length == base.Buffer.Text.Length - 1;
			if (flag)
			{
				char c = base.Buffer.Text[cursorPosition - 1];
				char c2 = c;
				char c3 = c2;

				if (c3 != '\n')
				{
					if (c3 == '{')
					{
						this.SetText(base.Buffer.Text.Insert(cursorPosition, "}"));
						base.Buffer.PlaceCursor(base.Buffer.GetIterAtOffset(cursorPosition));
					}
				}
			}
			else
			{
				bool flag2 = this._s.Length == base.Buffer.Text.Length + 1;
				if (flag2)
				{
					char c4 = this._s[base.Buffer.CursorPosition];
					// Console.WriteLine("Removed {0}", c4);
					int num = cursorPosition;
					bool flag3 = c4 == '\t';
					if (flag3)
					{
						string text = base.Buffer.Text;
						while (text[num - 1] == '\t')
						{
							text = text.Remove(num - 1, 1);
							num--;
						}
						bool flag4 = text[num - 1] == '\n';
						if (flag4)
						{
							text = text.Remove(num - 1, 1);
							num--;
						}
						this.SetText(text);
						Gtk.Application.Invoke((o, a) =>
						{
							base.Buffer.PlaceCursor(base.Buffer.GetIterAtOffset(num));
						});
					}
				}
			}
			this._s = base.Buffer.Text;
			#endregion
		}

		void Handle_KeyPressEvent(object o, KeyPressEventArgs args)
		{
			// Console.WriteLine(args.Event.Key + " pressed");
			_keys.Add(args.Event.Key);

			if ((_keys.Contains(Gdk.Key.s) || _keys.Contains(Gdk.Key.S)) && (_keys.Contains(Gdk.Key.Control_L) || _keys.Contains(Gdk.Key.Control_R)))
			{
				SaveFile();
			}
		}

		void Handle_KeyReleaseEvent(object o, KeyReleaseEventArgs args)
		{
			// Console.WriteLine(args.Event.Key + " released");
			_keys.Remove(args.Event.Key);
		}

		public void SaveFile()
		{
			File.WriteAllText(System.IO.Path.Combine(ProjectManager.Project.Path, _file.Filename), Buffer.Text);
			_actualText = Buffer.Text;
			Buffer_Changed(null, null);
		}

		public void SetText(string text)
		{
			Application.Invoke((a, b) =>
			{
				this.Buffer.Text = text;
			});
		}

		public NotebookTabLabel GetLabel()
		{
			return _notebookTabLabel;
		}
	}
}