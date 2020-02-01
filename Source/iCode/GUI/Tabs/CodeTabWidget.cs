using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
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

		private int _editLast = 0;

		private List<Gdk.Key> _keys;
		private Dictionary<ClangDiagnostic, string> _diagnos;

		private NotebookTabLabel _notebookTabLabel;
		private Thread _t;

		private ClangTranslationUnit _trans;
		private ClangIndex _index;
		private IEnumerable<ClangToken> _tokens = new List<ClangToken>();
		
		private bool _recolor = true;

		public CodeTabWidget(Class @class)
		{
			_keys = new List<Gdk.Key>();
			base.Buffer = new TextBuffer(new TextTagTable());// new SourceBuffer(new SourceLanguageManager().GetLanguage("objc"));
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

		#region Clang init
			
			_index = ClangService.CreateIndex();
			_trans = _index.CreateTranslationUnitFromSourceFile(_temp,
				_file.CompilerFlags.Concat(ProjectManager.Flags).ToArray(), ProjectManager.Project.Classes
																		    .Select(a =>
																			    new
																				    ClangUnsavedFile(
																					    a.Filename,
																					    File
																						    .ReadAllText(
																							    System
																								    .IO
																								    .Path
																								    .Combine(
																									    ProjectManager
																										    .Project
																										    .Path,
																									    a.Filename))
																				    )
																		    ).ToArray());
			
			_diagnos = new Dictionary<ClangDiagnostic, string>();
			_t = new Thread(HandleParameterizedThreadStart);
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
		
		void HandleParameterizedThreadStart(object obj)
		{
		#region Error checking
			// Workaround:
			/*Gtk.Application.Invoke((oaz,zaedf) =>
			{
				Buffer.RemoveTag($"warning",
					Buffer.GetIterAtOffset(0),
					Buffer.GetIterAtOffset(Buffer.Text.Length));
				Buffer.RemoveTag($"error",
					Buffer.GetIterAtOffset(0),
					Buffer.GetIterAtOffset(Buffer.Text.Length));
			});*/
			
			// ThIS cOdE IS SeLF DoCUMenTiNg
			while (!((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds - _editLast > 2 && _recolor)) ;
			File.WriteAllText(_temp, Buffer.Text);

			// */

			_trans = _index.CreateTranslationUnitFromSourceFile(_temp,
				_file.CompilerFlags.Concat(ProjectManager.Flags).ToArray(), ProjectManager.Project.Classes
																		    .Select(a =>
																			    new
																				    ClangUnsavedFile(
																					    a.Filename,
																					    File
																						    .ReadAllText(
																							    System
																								    .IO
																								    .Path
																								    .Combine(
																									    ProjectManager
																										    .Project
																										    .Path,
																									    a.Filename))
																				    )
																		    ).ToArray());
			
			List<ClangDiagnostic> toRemove = new List<ClangDiagnostic>();
			foreach (var diagno in _diagnos.Keys)
				if (!_trans.DiagnosticSet.Items.Contains(diagno))
					toRemove.Add(diagno);

			if (_trans.DiagnosticCount == 0)
			{
				toRemove = _diagnos.Keys.ToList();
				Console.WriteLine("Code is clean, no any error found !");
			}
			
			foreach (var item in toRemove)
			{
				Gtk.Application.Invoke((o, a) =>
				{
					Console.WriteLine("Removing squiggle");
					try
					{
						if (item.Severity == DiagnosticSeverity.Error)
							Buffer.RemoveTag($"error",
								Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset),
								Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset + _diagnos[item].Length - 1));
						else if (item.Severity == DiagnosticSeverity.Warning)
							Buffer.RemoveTag($"warning",
								Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset),
								Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset + _diagnos[item].Length - 1));
					}
					catch
					{
						
					}
				});
				_diagnos.Remove(item);
			}
			
			//foreach (var token in tokens)
			//	Console.WriteLine($"Token: {token.Spelling}, Offset: [{token.Extent.Start.SpellingLocation.Offset} -> {token.Extent.End.SpellingLocation.Offset}], Kind: {token.Kind}, Extent: {token.Extent}");
			Console.WriteLine(_tokens.Count().ToString());
			foreach (var item in _trans.DiagnosticSet.Items)
				try
				{
					if (!_diagnos.Keys.Contains(item))
					{
						var startSearch = Buffer.GetIterAtLine(item.Location.SpellingLocation.Line);
						TextIter endSearch;
						try
						{
							endSearch = Buffer.GetIterAtLine(item.Location.SpellingLocation.Line + 1);
						}
						catch
						{
							endSearch = Buffer.GetIterAtOffset(Buffer.CharCount - 1);
						}

						var text = Buffer.GetText(startSearch, endSearch, false);
						var tokens = text.Split(';', ' ', '\n', ';', '(', ')', '[', ']', '.', ',', '=', '<', '>', '+', '-', '/', '*', '%', '@', '"', '\'', '\r');

						var start = 0;
						var end = 0;
						string token = "";
						
						for (int i = 0; i < tokens.Length; i++)
						{
							start += tokens[i].Length;
							if (start <= item.Location.SpellingLocation.Column &&
							    item.Location.SpellingLocation.Column <= end)
							{
								token = tokens[i];
								break;
							}
							end += tokens[i].Length;
						}
						
						/*
						Console.WriteLine(_tokens.Count().ToString());
						foreach (var t in _tokens)
						{
							Console.WriteLine($"{t.Extent.Start.ExpansionLocation.Offset} <= {item.Location.ExpansionLocation.Offset} <= {t.Extent.End.ExpansionLocation.Offset}");
						}
						
						var e = _tokens.First(a => a.Extent.Start.SpellingLocation.Offset == item.Location.ExpansionLocation.Offset);
						 //var e = _trans.GetCursor(item.Location).Spelling.Length;
						var offset = item.Location.SpellingLocation.Offset;*/
						Console.WriteLine(
							$"{item.Severity} at offset {item.Location.SpellingLocation.Offset}:  {token}");

						Gtk.Application.Invoke((o, a) =>
						{
							
							if (item.Severity == DiagnosticSeverity.Error)
								Buffer.ApplyTag($"error",
									Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset),
									Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset + token.Length - 1));
							else if (item.Severity == DiagnosticSeverity.Warning)
								Buffer.ApplyTag($"warning",
									Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset),
									Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset + token.Length - 1));
						});

						_diagnos.Add(item, token);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"The parsing of error failed: {e}");
				}

		#endregion
				
			foreach (var token in _tokens)
			{
				switch (token.Kind)
				{
					case TokenKind.Comment:
						Gtk.Application.Invoke((o, a) =>
						{
							Buffer.ApplyTag("comment", Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset),
								Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset + token.Spelling.Length));
						});
						break;
					case TokenKind.Identifier:
						Gtk.Application.Invoke((o, a) =>
						{
							Buffer.ApplyTag("identifier", Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset),
								Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset + token.Spelling.Length));
						});
						break;
					case TokenKind.Literal:
						TextTag tag = null;
						if (GetCursorKind(token).ToString().Contains("Attribute"))
						{
							tag = GetTagFromHex(0.706328, 0.872545, 0.381282);
						}
						else if (GetCursorKind(token).ToString().Contains("Float") || GetCursorKind(token).ToString().Contains("Int") || GetCursorKind(token).ToString().Contains("Character"))
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
								Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset + token.Spelling.Length));
						});
						break;
					case TokenKind.Keyword:
						Gtk.Application.Invoke((o, a) =>
						{
							Buffer.ApplyTag("keyword", Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset),
								Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset + token.Spelling.Length));
						});
						break;
					default:
						Gtk.Application.Invoke((o, a) =>
						{
							Buffer.ApplyTag(GetTagFromHex(0.926027, 0.924097, 0.966442), Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset),
								Buffer.GetIterAtOffset(token.Location.SpellingLocation.Offset + token.Spelling.Length));
						});
						break;
				}
			}
			/* Parse by CURSor, but it is CURSed
			int it = 0;
			CursorKind? previousKind = null;

			for (int i = 0; i <= Buffer.Text.Length; i++)
			{
				var cursor = _trans.GetCursor(_trans.GetLocationForOffset(_trans.GetFile(_temp), i));

				bool doRoutine = false;
				if (previousKind != cursor.Kind) // || Buffer.Text[i] == '\n')
				{
					// Console.WriteLine($"{cursor.Spelling} (at {i}) is dawn of a new element -> {cursor.Kind}");
					doRoutine = true;
				}

				if (doRoutine)
				{
					if (i - it > 0)
					{
						Console.WriteLine(
							$"{Buffer.Text.Substring(it, i - it)} (from {it} to {i}) is a {previousKind}!");
					
						if (previousKind.ToString().Contains("Reference"))
						{
							int start = it;
							int end = i;

							
						}

						it = i;
					}

					previousKind = cursor.Kind;
						
				}

				// i += cursor.Spelling.Length > 0 ? cursor.Spelling.Length - 1 : 0;
			}
*/

			_recolor = false;
		}

		CursorKind GetCursorKind(ClangToken token)
		{
			return _trans.GetCursor(_trans.GetLocationForOffset(_trans.GetFile(_temp),
				token.Location.SpellingLocation.Offset + (token.Spelling.Length / 2))).Kind;
		}

		void CodeISearchedTooLongForNothingSoIfSomeoneNeedsItThisIsHereButIFoundThatThereWasAwayInTheLibrary_ButItIsCompiledIntoTheBinaryAnyway()
		{
			int it = 0;
			CursorKind? previousKind = null;

			for (int i = 0; i <= Buffer.Text.Length; i++)
			{
				var cursor = _trans.GetCursor(_trans.GetLocationForOffset(_trans.GetFile(_temp), i));

				bool doRoutine = previousKind != cursor.Kind;

				if (doRoutine)
				{
					if (i - it > 0)
					{
						Console.WriteLine(
							$"{Buffer.Text.Substring(it, i - it)} (from {it} to {i}) is a {previousKind}!");

						if (previousKind.ToString().Contains("Reference"))
						{
							int start = it;
							int end = i;
						}

						

						it = i;
					}

					previousKind = cursor.Kind;

				}

				// i += cursor.Spelling.Length > 0 ? cursor.Spelling.Length - 1 : 0;
			}

		}

		private void Buffer_Changed(object sender, EventArgs e)
		{
			_tokens = _trans.Tokenize(ClangSourceRange.GetRange(_trans.GetLocationForOffset(_trans.GetFile(_temp), 0),
				_trans.GetLocationForOffset(_trans.GetFile(_temp), Buffer.Text.Length))).Tokens;

			Console.WriteLine(_tokens.Count().ToString());
			
			if (!_recolor)
				_recolor = true;

			if (_t.ThreadState != ThreadState.Running)
			{
				_t = new Thread(HandleParameterizedThreadStart);
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

			_editLast = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

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