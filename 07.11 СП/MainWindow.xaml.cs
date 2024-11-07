using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace _07._11_СП
{
    public partial class MainWindow : Window
    {
        private string selectedDirectory;
        private FileSystemWatcher watcher;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedDirectory = dialog.SelectedPath;
                await ScanDirectory(selectedDirectory);
                StartMonitoring(selectedDirectory);
            }
        }

        private async Task ScanDirectory(string path)
        {
            var result = await Task.Run(() => GetDirectoryInfo(path));
            DirectoryTree.Items.Clear();
            foreach (var item in result)
            {
                DirectoryTree.Items.Add(item);
            }

            // Обновление статистики
            ShowStatistics(result);
        }

        private List<DirectoryItem> GetDirectoryInfo(string path)
        {
            var items = new List<DirectoryItem>();
            var directories = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path);

            foreach (var directory in directories)
            {
                var dirInfo = new DirectoryInfo(directory);
                items.Add(new DirectoryItem { Name = dirInfo.Name, Type = "Directory", Size = 0 });
                items.AddRange(GetDirectoryInfo(directory));
            }

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                items.Add(new DirectoryItem { Name = fileInfo.Name, Type = "File", Size = fileInfo.Length });
            }

            return items;
        }

        private void ShowStatistics(List<DirectoryItem> items)
        {
            int directoryCount = items.Count(i => i.Type == "Directory");
            int fileCount = items.Count(i => i.Type == "File");
            long totalSize = items.Where(i => i.Type == "File").Sum(i => i.Size);

            DirectoryCountText.Text = $"Total directories: {directoryCount}";
            FileCountText.Text = $"Total files: {fileCount}";
            TotalSizeText.Text = $"Total size: {totalSize} bytes";

            var fileTypes = items.Where(i => i.Type == "File")
                                  .GroupBy(i => Path.GetExtension(i.Name).ToLower())
                                  .Select(g => new { Type = g.Key, Count = g.Count(), Size = g.Sum(i => i.Size) });

            FileTypesText.Text = "File types:\n" + string.Join("\n", fileTypes.Select(ft => $"{ft.Type}: {ft.Count} files, {ft.Size} bytes"));
        }

        private void StartMonitoring(string path)
        {
            watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
            };

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;
            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            FileInfo fileInfo = new FileInfo(e.FullPath);
            Dispatcher.Invoke(() =>
                System.Windows.MessageBox.Show($"File {e.ChangeType}: {e.FullPath}"));
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
                System.Windows.MessageBox.Show($"File Renamed: {e.OldFullPath} to {e.FullPath}"));
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog1 = new FolderBrowserDialog();
            var dialog2 = new FolderBrowserDialog();

            if (dialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK && dialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CompareDirectories(dialog1.SelectedPath, dialog2.SelectedPath);
            }
        }

        private void CompareDirectories(string path1, string path2)
        {
            var dir1Files = Directory.GetFiles(path1).Select(f => new FileInfo(f)).ToList();
            var dir2Files = Directory.GetFiles(path2).Select(f => new FileInfo(f)).ToList();

            var uniqueToDir1 = dir1Files.Where(f1 => !dir2Files.Any(f2 => f1.Name == f2.Name)).ToList();
            var uniqueToDir2 = dir2Files.Where(f2 => !dir1Files.Any(f1 => f1.Name == f2.Name)).ToList();
            var commonFiles = dir1Files.Where(f1 => dir2Files.Any(f2 => f1.Name == f2.Name)).ToList();

            // Отображение результатов сравнения
            ShowComparisonResults(uniqueToDir1, uniqueToDir2, commonFiles);
        }

        private void ShowComparisonResults(List<FileInfo> uniqueToDir1, List<FileInfo> uniqueToDir2, List<FileInfo> commonFiles)
        {
            var resultMessage = "Comparison Results:\n\n";
            resultMessage += "Unique to Directory 1:\n";
            resultMessage += string.Join("\n", uniqueToDir1.Select(f => f.Name)) + "\n\n";
            resultMessage += "Unique to Directory 2:\n";
            resultMessage += string.Join("\n", uniqueToDir2.Select(f => f.Name)) + "\n\n";
            resultMessage += "Common Files:\n";
            resultMessage += string.Join("\n", commonFiles.Select(f => f.Name));

            System.Windows.MessageBox.Show(resultMessage);
        }

        private void DirectoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is DirectoryItem selectedItem)
            {
                LoadFilesIntoDataGrid(selectedItem);
            }
        }

        private void LoadFilesIntoDataGrid(DirectoryItem selectedItem)
        {
            if (selectedItem.Type == "Directory")
            {
                // Создаем полный путь к выбранному каталогу
                string directoryPath = Path.Combine(selectedDirectory, selectedItem.Name);

                // Проверяем, существует ли каталог
                if (Directory.Exists(directoryPath))
                {
                    var files = Directory.GetFiles(directoryPath);
                    FileDataGrid.ItemsSource = files.Select(f => new FileData { Name = Path.GetFileName(f), Size = new FileInfo(f).Length }).ToList();
                }
                else
                {
                    // Если каталог не существует, выводим сообщение об ошибке
                    System.Windows.MessageBox.Show($"Каталог не найден: {directoryPath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class DirectoryItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public long Size { get; set; }

        public override string ToString()
        {
            return Name; // Отображаем только имя элемента
        }
    }

    public class FileData
    {
        public string Name { get; set; }
        public long Size { get; set; }
    }
}