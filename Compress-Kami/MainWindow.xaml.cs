using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Forms; // Pour FolderBrowserDialog
using Compress_Kami.Core;

namespace Compress_Kami
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateStatus("Prêt à créer des fichiers .keyce ultimes");
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private void ShowProgress(bool show)
        {
            ProgressBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CompressKeyce_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Sélectionnez le dossier à compresser en .keyce";
                    folderDialog.ShowNewFolderButton = true;

                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        using (var saveDialog = new SaveFileDialog())
                        {
                            saveDialog.Filter = "Fichiers KEYCE|*.keyce";
                            saveDialog.Title = "Enregistrer le fichier .keyce";
                            saveDialog.DefaultExt = "keyce";
                            saveDialog.AddExtension = true;

                            string folderName = new DirectoryInfo(folderDialog.SelectedPath).Name;
                            saveDialog.FileName = $"{folderName}.keyce";

                            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                ShowProgress(true);
                                UpdateStatus("Compression .keyce en cours...");

                                System.Threading.Tasks.Task.Run(() =>
                                {
                                    try
                                    {
                                        HuffmanFolderCompressor.CompressFolder(folderDialog.SelectedPath, saveDialog.FileName);

                                        Dispatcher.Invoke(() =>
                                        {
                                            ShowProgress(false);
                                            UpdateStatus($"✓ Compression réussie !\n{saveDialog.FileName}");
                                            System.Windows.MessageBox.Show(
                                                $"Compression .keyce réussie !\n\nDossier: {folderDialog.SelectedPath}\nFichier: {saveDialog.FileName}",
                                                "Succès",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            ShowProgress(false);
                                            UpdateStatus("❌ Erreur lors de la compression");
                                            System.Windows.MessageBox.Show(
                                                $"Erreur lors de la compression .keyce:\n{ex.Message}",
                                                "Erreur",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Error);
                                        });
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowProgress(false);
                UpdateStatus("❌ Erreur lors de la sélection");
                System.Windows.MessageBox.Show(
                    $"Erreur: {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DecompressKeyce_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = "Fichiers KEYCE|*.keyce";
                    openDialog.Title = "Ouvrir un fichier .keyce";

                    if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        if (!HuffmanFolderCompressor.IsKeyCeFile(openDialog.FileName))
                        {
                            System.Windows.MessageBox.Show(
                                "Ce fichier n'est pas un .keyce valide !",
                                "Format invalide",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        using (var folderDialog = new FolderBrowserDialog())
                        {
                            folderDialog.Description = "Sélectionnez où décompresser le dossier";
                            folderDialog.ShowNewFolderButton = true;

                            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                ShowProgress(true);
                                UpdateStatus("Décompression .keyce en cours...");

                                System.Threading.Tasks.Task.Run(() =>
                                {
                                    try
                                    {
                                        HuffmanFolderCompressor.DecompressFolder(openDialog.FileName, folderDialog.SelectedPath);

                                        Dispatcher.Invoke(() =>
                                        {
                                            ShowProgress(false);
                                            UpdateStatus($"✓ Décompression réussie !\n{folderDialog.SelectedPath}");
                                            System.Windows.MessageBox.Show(
                                                $"Décompression .keyce réussie !\n\nFichier: {openDialog.FileName}\nDossier: {folderDialog.SelectedPath}",
                                                "Succès",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            ShowProgress(false);
                                            UpdateStatus("❌ Erreur lors de la décompression");
                                            System.Windows.MessageBox.Show(
                                                $"Erreur lors de la décompression .keyce:\n{ex.Message}",
                                                "Erreur",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Error);
                                        });
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowProgress(false);
                UpdateStatus("❌ Erreur lors de la sélection");
                System.Windows.MessageBox.Show(
                    $"Erreur: {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CompressZip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Sélectionnez le dossier à zipper";
                    folderDialog.ShowNewFolderButton = true;

                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        using (var saveDialog = new SaveFileDialog())
                        {
                            saveDialog.Filter = "Fichiers ZIP|*.zip";
                            saveDialog.Title = "Enregistrer le fichier ZIP";
                            saveDialog.DefaultExt = "zip";
                            saveDialog.AddExtension = true;

                            string folderName = new DirectoryInfo(folderDialog.SelectedPath).Name;
                            saveDialog.FileName = $"{folderName}.zip";

                            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                ShowProgress(true);
                                UpdateStatus("Création du ZIP en cours...");

                                System.Threading.Tasks.Task.Run(() =>
                                {
                                    try
                                    {
                                        ZipFile.CreateFromDirectory(folderDialog.SelectedPath, saveDialog.FileName);

                                        Dispatcher.Invoke(() =>
                                        {
                                            ShowProgress(false);
                                            UpdateStatus($"✓ ZIP créé avec succès !\n{saveDialog.FileName}");
                                            System.Windows.MessageBox.Show(
                                                $"Fichier ZIP créé avec succès !\n\n{saveDialog.FileName}",
                                                "Succès",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            ShowProgress(false);
                                            UpdateStatus("❌ Erreur lors de la création du ZIP");
                                            System.Windows.MessageBox.Show(
                                                $"Erreur lors de la création du ZIP:\n{ex.Message}",
                                                "Erreur",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Error);
                                        });
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowProgress(false);
                UpdateStatus("❌ Erreur lors de la sélection");
                System.Windows.MessageBox.Show(
                    $"Erreur: {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DecompressZip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = "Fichiers ZIP|*.zip";
                    openDialog.Title = "Ouvrir un fichier ZIP";

                    if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        using (var folderDialog = new FolderBrowserDialog())
                        {
                            folderDialog.Description = "Sélectionnez où extraire le ZIP";
                            folderDialog.ShowNewFolderButton = true;

                            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                ShowProgress(true);
                                UpdateStatus("Extraction du ZIP en cours...");

                                System.Threading.Tasks.Task.Run(() =>
                                {
                                    try
                                    {
                                        ZipFile.ExtractToDirectory(openDialog.FileName, folderDialog.SelectedPath);

                                        Dispatcher.Invoke(() =>
                                        {
                                            ShowProgress(false);
                                            UpdateStatus($"✓ ZIP extrait avec succès !\n{folderDialog.SelectedPath}");
                                            System.Windows.MessageBox.Show(
                                                $"Fichier ZIP extrait avec succès !\n\nVers: {folderDialog.SelectedPath}",
                                                "Succès",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            ShowProgress(false);
                                            UpdateStatus("❌ Erreur lors de l'extraction");
                                            System.Windows.MessageBox.Show(
                                                $"Erreur lors de l'extraction du ZIP:\n{ex.Message}",
                                                "Erreur",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Error);
                                        });
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowProgress(false);
                UpdateStatus("❌ Erreur lors de la sélection");
                System.Windows.MessageBox.Show(
                    $"Erreur: {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}