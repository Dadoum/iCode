sed -i 's/global::Gtk.ComboBox.NewText()/new global::Gtk.ComboBox()/g' ./gtk-gui/MainWindow.cs
rm -rf ./gtk-gui/generated.cs
yes | cp ./patched-generated.cs ./gtk-gui/generated.cs
