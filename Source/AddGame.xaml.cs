using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace CollectionLauncher
{
    public partial class AddGame : Window
    {
        private OpenFileDialog openFileDialog = new OpenFileDialog();

        public AddGame()
        {
            InitializeComponent();
        }

        private void btnFile_Click(object sender, RoutedEventArgs e)
        {
            if (openFileDialog.ShowDialog() == true)
                txtFilePath.Text = openFileDialog.FileName;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> failMessage = new List<string>();
                if (txtGameTitle.Text == string.Empty)
                    failMessage.Add("Please enter Title for game.");
                if (txtCoverPath.Text == string.Empty)
                    failMessage.Add("Please select Cover File.");

                if (failMessage.Count > 0)
                {
                    MessageBox.Show(string.Join(Environment.NewLine, failMessage.ToArray()));
                    return;
                }
                var gamePath = MainWindow.currentPath + @"\Games\" + txtGameTitle.Text;
                if (File.Exists(gamePath))
                {
                    MessageBox.Show("This game has already been added.");
                    return;
                }
                Directory.CreateDirectory(gamePath);
                File.Copy(txtCoverPath.Text, gamePath + @"\Cover.jpg", true);

                var game = new Game
                {
                    Name = txtGameTitle.Text,
                    Path = gamePath,
                    InstalledPath = txtFilePath.Text,
                };
                File.WriteAllText(System.IO.Path.Combine(gamePath, "info.json"), JsonConvert.SerializeObject(game));
            }
            catch (Exception)
            {
                MessageBox.Show("Something happened that I don't like !!!");
                throw;
            }
            Close();
        }

        private void btnCover_Click(object sender, RoutedEventArgs e)
        {
            if (openFileDialog.ShowDialog() == true)
            {
                txtCoverPath.Text = openFileDialog.FileName;
                if (txtGameTitle.Text == string.Empty)
                    txtGameTitle.Text = GetLastFolderName(txtCoverPath.Text);
            }
        }
        private string GetLastFolderName(string filePath)
        {
            try
            {
                string directoryPath = System.IO.Path.GetDirectoryName(filePath) ?? "";
                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
                return directoryInfo.Name;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
