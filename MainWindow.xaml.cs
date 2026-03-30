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

        // ----------------------------------------------------
        // 1. 設定の保存・読み込み処理
        // ----------------------------------------------------
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
                catch { /* 読み込み失敗時は初期状態 */ }
            }
        }

        private void SaveSettings()
        {
            _settings.TargetFolders.Clear();
            foreach (var item in LstTargetFolders.Items)
            {
                _settings.TargetFolders.Add(item.ToString());
            }
            _settings.OutputDirectory = TxtOutputDir.Text;

            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }

        // ----------------------------------------------------
        // 2. ボタンクリック時のイベント処理
        // ----------------------------------------------------
        private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "ZIPファイルの保存先フォルダを選択してください";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtOutputDir.Text = dialog.SelectedPath;
                SaveSettings();
            }
        }

        private void BtnAddTarget_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "圧縮したい対象のフォルダを選択してください";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!LstTargetFolders.Items.Contains(dialog.SelectedPath))
                {
                    LstTargetFolders.Items.Add(dialog.SelectedPath);
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

            // 除外リストの作成
            var excludeList = new List<string>();
            if (ChkGit.IsChecked == true) excludeList.Add(".git");
            if (ChkVs.IsChecked == true) excludeList.Add(".vs");
            if (ChkNodeModules.IsChecked == true) excludeList.Add("node_modules");
            if (ChkBinObj.IsChecked == true)
            {
                excludeList.Add("bin");
                excludeList.Add("obj");
            }

            int successCount = 0;

            foreach (var item in LstTargetFolders.Items)
            {
                string targetPath = item.ToString();
                if (!Directory.Exists(targetPath)) continue;

                string dirName = new DirectoryInfo(targetPath).Name;
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string zipFileName = $"{dirName}_{timestamp}.zip";
                string zipFilePath = Path.Combine(TxtOutputDir.Text, zipFileName);

                // 同名ファイルがある場合は連番を付与
                int counter = 1;
                while (File.Exists(zipFilePath))
                {
                    zipFileName = $"{dirName}_{timestamp}_{counter}.zip";
                    zipFilePath = Path.Combine(TxtOutputDir.Text, zipFileName);
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

        // ----------------------------------------------------
        // 3. ZIP圧縮のコアロジック
        // ----------------------------------------------------
        private void CreateZipFromDirectory(string sourceDir, string zipPath, List<string> excludeList)
        {
            using FileStream zipToOpen = new FileStream(zipPath, FileMode.Create);
            using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);
            
            AddFilesToZip(archive, sourceDir, sourceDir, excludeList);
        }

        private void AddFilesToZip(ZipArchive archive, string currentDir, string baseDir, List<string> excludeList)
        {
            DirectoryInfo di = new DirectoryInfo(currentDir);

            // 除外対象のフォルダ名なら処理をスキップ
            if (excludeList.Contains(di.Name.ToLower())) return;

            // ファイルをZIPに追加
            foreach (FileInfo file in di.GetFiles())
            {
                string entryName = Path.GetRelativePath(baseDir, file.FullName);
                entryName = entryName.Replace('\\', '/'); 
                archive.CreateEntryFromFile(file.FullName, entryName);
            }

            // サブフォルダを再帰的に処理
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