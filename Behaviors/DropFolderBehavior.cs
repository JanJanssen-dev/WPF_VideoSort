using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using WPF_VideoSort.ViewModels;

namespace WPF_VideoSort.Behaviors
{
    public class DropFolderBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AllowDrop = true;
            AssociatedObject.PreviewDragOver += TextBox_PreviewDragOver;
            AssociatedObject.Drop += TextBox_Drop;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewDragOver -= TextBox_PreviewDragOver;
            AssociatedObject.Drop -= TextBox_Drop;
            base.OnDetaching();
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && System.IO.Directory.Exists(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0 || !System.IO.Directory.Exists(files[0]))
                return;

            if (sender is TextBox textBox && textBox.DataContext is MainViewModel viewModel)
            {
                string droppedFolder = files[0];

                // Wenn es sich um das Quellordner-Textfeld handelt, setzen wir beide Ordner
                if (textBox.Tag?.ToString() == "source")
                {
                    viewModel.SourceFolder = droppedFolder;
                    viewModel.DestinationFolder = droppedFolder;
                }
                // Wenn es sich um das Zielordner-Textfeld handelt, setzen wir nur den Zielordner
                else if (textBox.Tag?.ToString() == "destination")
                {
                    viewModel.DestinationFolder = droppedFolder;
                }
            }
        }
    }
}