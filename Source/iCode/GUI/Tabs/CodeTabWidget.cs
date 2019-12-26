using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using GtkSourceView;
using iCode.Projects;
using NClang;

namespace iCode.GUI.Tabs
{
    public class CodeTabWidget : SourceView
    {
        private string s;
        private Class file;
        private string temp;
        private string actualText;

        private int editLast = 0;

        private List<Gdk.Key> keys;
        private Dictionary<ClangDiagnostic, TextTag> diagnos;

        private NotebookTabLabel notebookTabLabel;
        private Thread t;

        private bool recolor = true;

        public CodeTabWidget(Class @class)
        {
            keys = new List<Gdk.Key>();
            base.Buffer = new SourceBuffer(new SourceLanguageManager().GetLanguage("objc"));
            notebookTabLabel = new NotebookTabLabel(System.IO.Path.GetFileName(@class.Filename), this);
            this.file = @class;
            var css = new CssProvider();
            css.LoadFromData(@"
                textview {
                    font-family: Monospace;
                }
            ");
            this.StyleContext.AddProvider(css, 1);
            this.temp = System.IO.Path.Combine(ProjectManager.Project.Path, "~" + @class.Filename);
            actualText = File.ReadAllText(System.IO.Path.Combine(ProjectManager.Project.Path, @class.Filename));
            Buffer.Text = actualText;
            this.KeyPressEvent += Handle_KeyPressEvent;
            this.KeyReleaseEvent += Handle_KeyReleaseEvent;
            base.AutoIndent = true;
            base.ShowLineMarks = true;
            base.ShowLineNumbers = true;
            base.HighlightCurrentLine = false;
            base.IndentOnTab = true;
            this.s = base.Buffer.Text;
            base.Buffer.Changed += this.Buffer_Changed;
            diagnos = new Dictionary<ClangDiagnostic, TextTag>();
            t = new Thread(HandleParameterizedThreadStart);
            t.Start();
        }

        void HandleParameterizedThreadStart(object obj)
        {
            return;
            Gtk.Application.Invoke((o, e) =>
            {
                while (!((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds - editLast > 2 && recolor)) ;

                File.WriteAllText(temp, Buffer.Text);
                var index = ClangService.CreateIndex();
                var trans = index.ParseTranslationUnit(temp,
                    file.CompilerFlags.Concat(ProjectManager.Flags).ToArray(),
                    ProjectManager.Project.Classes
                        .Select(a =>
                            new ClangUnsavedFile(
                                a.Filename,
                                File.ReadAllText(System.IO.Path.Combine(ProjectManager.Project.Path, a.Filename))
                            )
                        ).ToArray(),
                    TranslationUnitFlags.SingleFileParse);
                    
                List<ClangDiagnostic> toRemove = new List<ClangDiagnostic>();
                foreach (var diagno in diagnos.Keys)
                    if (!trans.DiagnosticSet.Items.Contains(diagno))
                        toRemove.Add(diagno);
                        
                foreach (var remove in toRemove)
                {
                    Buffer.RemoveTag(diagnos[remove], Buffer.GetIterAtOffset(remove.Location.SpellingLocation.Offset), Buffer.GetIterAtOffset(remove.Location.ExpansionLocation.Offset));
                    diagnos.Remove(remove);
                }

                foreach (var item in trans.DiagnosticSet.Items)
                    if (!diagnos.Keys.Contains(item))
                    {
                        TextTag tag = new TextTag(item.Spelling);
                        tag.Underline = item.Severity == DiagnosticSeverity.Error ? Pango.Underline.Error : item.Severity == DiagnosticSeverity.Warning ? Pango.Underline.Low : Pango.Underline.Single;
                        tag.ForegroundGdk = new Gdk.Color(128, 128, 128);
                        Console.WriteLine($"issue in code found at offset {item.Location.SpellingLocation.Offset}");
                        // Buffer.InsertW
                        Buffer.ApplyTag(tag,
                            Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset),
                            Buffer.GetIterAtOffset(item.Location.SpellingLocation.Offset));
                        diagnos.Add(item, tag);
                        foreach (var kvp in Buffer.TagTable.Data.Keys)
                            Console.WriteLine($"k: {kvp};");
                        foreach (var kvp in Buffer.TagTable.Data.Values)
                            Console.WriteLine($"v: {kvp};");
                    }

                /*foreach (var ta in trans.DiagnosticSet.Items)
                {
                    Console.WriteLine("[{0} at {1}] {2}", ta.Severity, ta.Location.FileLocation, ta.Spelling);
                }*/
                recolor = false;
            });
        }


        private void Buffer_Changed(object sender, EventArgs e)
        {
            if (!recolor)
                recolor = true;

            if (t.ThreadState != ThreadState.Running)
            {
                t = new Thread(HandleParameterizedThreadStart);
                t.Start();
            }

            #region CODE SAVING
            if (actualText != Buffer.Text)
            {
                if (!notebookTabLabel.Label.Text.EndsWith("*", StringComparison.CurrentCulture))
                {
                    notebookTabLabel.Label.Text += "*";
                }
            }
            else
            {
                if (notebookTabLabel.Label.Text.EndsWith("*", StringComparison.CurrentCulture))
                {
                    notebookTabLabel.Label.Text = notebookTabLabel.Label.Text.TrimEnd('*');
                }
            }
            #endregion

            if (e == null)
                return;

            editLast = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            #region CODE COMPLETION
            int cursorPosition = base.Buffer.CursorPosition;
            bool flag = this.s.Length == base.Buffer.Text.Length - 1;
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
                else
                {
                    try
                    {
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Code Completion failed: {0}", new object[]
                        {
                            ex
                        });
                    }
                }
            }
            else
            {
                bool flag2 = this.s.Length == base.Buffer.Text.Length + 1;
                if (flag2)
                {
                    char c4 = this.s[base.Buffer.CursorPosition];
                    Console.WriteLine("Removed {0}", new object[]
                    {
                        c4
                    });
                    int num = cursorPosition;
                    bool flag3 = c4 == '\t';
                    if (flag3)
                    {
                        string text = base.Buffer.Text;
                        while (text[num - 1] == '\t')
                        {
                            Console.WriteLine("At cursor there is {0}, before {1}, and after {2}", new object[]
                            {
                                this.s[num],
                                this.s[num - 1],
                                this.s[num + 1]
                            });
                            text = text.Remove(num - 1, 1);
                            num--;
                        }
                        bool flag4 = text[num - 1] == '\n';
                        if (flag4)
                        {
                            Console.WriteLine("At cursor there is {0}, before {1}, and after {2}", new object[]
                            {
                                this.s[num],
                                this.s[num - 1],
                                this.s[num + 1]
                            });
                            text = text.Remove(num - 1, 1);
                            num--;
                        }
                        this.SetText(text);
                        base.Buffer.PlaceCursor(base.Buffer.GetIterAtOffset(num));
                    }
                }
            }
            this.s = base.Buffer.Text;
            #endregion
        }

        void Handle_KeyPressEvent(object o, KeyPressEventArgs args)
        {
            Console.WriteLine(args.Event.Key + " pressed");
            keys.Add(args.Event.Key);

            if ((keys.Contains(Gdk.Key.s) || keys.Contains(Gdk.Key.S)) && (keys.Contains(Gdk.Key.Control_L) || keys.Contains(Gdk.Key.Control_R)))
            {
                SaveFile();
            }
        }

        void Handle_KeyReleaseEvent(object o, KeyReleaseEventArgs args)
        {
            Console.WriteLine(args.Event.Key + " released");
            keys.Remove(args.Event.Key);
        }

        public void SaveFile()
        {
            File.WriteAllText(System.IO.Path.Combine(ProjectManager.Project.Path, file.Filename), Buffer.Text);
            actualText = Buffer.Text;
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
            return notebookTabLabel;
        }
    }
}
