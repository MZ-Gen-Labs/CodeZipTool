using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;

namespace CodeZipTool
{
    public partial class MainWindow : Window
    {
        private const string SettingsFile = "settings.json";
        private AppSettings _settings = new AppSettings();

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    TxtOutputDir.Text = _settings.OutputDirectory;
                    foreach (var folder in _settings.TargetFolders)
                    {
                        LstTargetFolders.Items.Add(folder);
                    }
                }
                catch { }
            }
        }

        private void SaveSettings()
        {
            _settings.TargetFolders.Clear();
            foreach (var item in LstTargetFolders.Items)
            {
                string? folderPath = item?.ToString();
                if (!string.IsNullOrEmpty(folderPath)) _settings.TargetFolders.Add(folderPath);
            }
            _settings.OutputDirectory = TxtOutputDir.Text;
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }

        private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "ZIPファイルの保存先フォルダを選択してください" };
            if (dialog.ShowDialog() == true)
            {
                TxtOutputDir.Text = dialog.FolderName;
                SaveSettings();
            }
        }

        private void BtnAddTarget_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "圧縮したい対象のフォルダを選択してください" };
            if (dialog.ShowDialog() == true)
            {
                if (!LstTargetFolders.Items.Contains(dialog.FolderName))
                {
                    LstTargetFolders.Items.Add(dialog.FolderName);
                    SaveSettings();
                }
            }
        }

        private void BtnRemoveTarget_Click(object sender, RoutedEventArgs e)
        {
            if (LstTargetFolders.SelectedIndex != -1)
            {
                LstTargetFolders.Items.RemoveAt(LstTargetFolders.SelectedIndex);
                SaveSettings();
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtOutputDir.Text) || LstTargetFolders.Items.Count == 0)
            {
                MessageBox.Show("出力先フォルダと対象フォルダを指定してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var excludeList = new List<string>();
            if (ChkGit.IsChecked == true) excludeList.Add(".git");
            if (ChkVs.IsChecked == true) excludeList.Add(".vs");
            if (ChkNodeModules.IsChecked == true) excludeList.Add("node_modules");
            if (ChkBinObj.IsChecked == true) { excludeList.Add("bin"); excludeList.Add("obj"); }

            int successCount = 0;
            foreach (var item in LstTargetFolders.Items)
            {
                string? targetPath = item?.ToString();
                if (string.IsNullOrEmpty(targetPath) || !Directory.Exists(targetPath)) continue;

                string dirName = new DirectoryInfo(targetPath).Name;
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string zipFilePath = Path.Combine(TxtOutputDir.Text, $"{dirName}_{timestamp}.zip");

                int counter = 1;
                while (File.Exists(zipFilePath))
                {
                    zipFilePath = Path.Combine(TxtOutputDir.Text, $"{dirName}_{timestamp}_{counter}.zip");
                    counter++;
                }

                try
                {
                    CreateZipFromDirectory(targetPath, zipFilePath, excludeList);
                    successCount++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"[{dirName}] の圧縮中にエラーが発生しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            MessageBox.Show($"{successCount}個のフォルダの圧縮が完了しました！", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateZipFromDirectory(string sourceDir, string zipPath, List<string> excludeList)
        {
            using FileStream zipToOpen = new FileStream(zipPath, FileMode.Create);
            using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);
            AddFilesToZip(archive, sourceDir, sourceDir, excludeList);
        }

        private void AddFilesToZip(ZipArchive archive, string currentDir, string baseDir, List<string> excludeList)
        {
            DirectoryInfo di = new DirectoryInfo(currentDir);
            if (di.Attributes.HasFlag(FileAttributes.Hidden)) return;
            if (excludeList.Contains(di.Name.ToLower())) return;

            var excludeExtensions = new List<string> { ".exe", ".dll", ".png", ".jpg", ".jpeg", ".gif", ".zip", ".pdb", ".ico" };

            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                if (excludeExtensions.Contains(file.Extension.ToLower())) continue;

                string entryName = Path.GetRelativePath(baseDir, file.FullName).Replace('\\', '/');
                archive.CreateEntryFromFile(file.FullName, entryName);
            }

            foreach (DirectoryInfo subDir in di.GetDirectories())
            {
                AddFilesToZip(archive, subDir.FullName, baseDir, excludeList);
            }
        }
    }

    public class AppSettings
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public List<string> TargetFolders { get; set; } = new List<string>();
    }
}