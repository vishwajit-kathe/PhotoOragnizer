using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.Win32;
using PhotoOrganizerApp.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Directory = System.IO.Directory;

namespace PhotoOrganizerApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly string[] _supportedExtensions = new[]
                    {
                        // Common formats
                        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff",
                        ".webp", ".heic", ".heif",
    
                        // RAW camera formats
                        ".arw",  // Sony
                        ".cr2", ".cr3", ".crw",  // Canon
                        ".nef", ".nrw",  // Nikon
                        ".orf",  // Olympus
                        ".raf",  // Fuji
                        ".rw2",  // Panasonic
                        ".pef",  // Pentax
                        ".srf", ".sr2",  // Sony
                        ".dng",  // Adobe Digital Negative
                        ".3fr",  // Hasselblad
                        ".ari",  // Arri
                        ".bay",  // Casio
                        ".cap", ".iiq",  // Phase One
                        ".dcr", ".k25", ".kdc",  // Kodak
                        ".erf",  // Epson
                        ".fff",  // Imacon
                        ".mdc",  // Minolta
                        ".mef",  // Mamiya
                        ".mos",  // Leaf
                        ".mrw",  // Minolta
                        ".ptx", ".pxn",  // Pentax
                        ".r3d",  // Red
                        ".raw",  // General RAW
                        ".rwl",  // Leica
                        ".srw",  // Samsung
                        ".x3f"   // Sigma
                    };

        private ObservableCollection<PhotoItem> _photos = new ObservableCollection<PhotoItem>();
        public ObservableCollection<PhotoItem> Photos
        {
            get => _photos;
            set { _photos = value; OnPropertyChanged(); }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select Source Folder" };
            if (dialog.ShowDialog() == true)
            {
                txtSource.Text = dialog.FolderName;
            }
        }

        private void BrowseDestination_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select Destination Folder" };
            if (dialog.ShowDialog() == true)
            {
                txtDestination.Text = dialog.FolderName;
            }
        }

        private async void Organize_Click(object sender, RoutedEventArgs e)
        {
            string source = txtSource.Text;
            string dest = txtDestination.Text;

            if (!ValidateFolders(source, dest)) return;

            try
            {
                // Setup progress UI
                progressBar.Visibility = Visibility.Visible;
                progressBar.Value = 0;
                txtStatus.Text = "Preparing to organize...";
                await Task.Delay(1); // Allow UI to update

                var files = Directory.GetFiles(source)
                    .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                progressBar.Maximum = files.Count;
                int processed = 0;

                foreach (var file in files)
                {
                    try
                    {
                        await Task.Run(() => OrganizeSingleFile(file, dest));

                        processed++;
                        progressBar.Value = processed;
                        txtStatus.Text = $"Organized {processed} of {files.Count}";

                        // Update UI every 10 files
                        if (processed % 10 == 0)
                            await Application.Current.Dispatcher.InvokeAsync(() => { });
                    }
                    catch (Exception fileEx)
                    {
                        Debug.WriteLine($"Error with {file}: {fileEx.Message}");
                    }
                }

                txtStatus.Text = $"Done! Organized {processed} photos";
                MessageBox.Show($"Successfully organized {processed} photos!",
                              "Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Organization failed";
                MessageBox.Show($"Error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void OrganizeSingleFile(string file, string destinationDir)
        {
            var metadata = ImageMetadataReader.ReadMetadata(file);
            var subIfd = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var dateTaken = subIfd?.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal)
                          ?? File.GetCreationTime(file);

            string destFolder = Path.Combine(destinationDir,
                                           dateTaken.ToString("yyyy"),
                                           dateTaken.ToString("MM"));
                                           //dateTaken.ToString("dd"));

            Directory.CreateDirectory(destFolder);

            string destPath = Path.Combine(destFolder, Path.GetFileName(file));

            if (!File.Exists(destPath))
                File.Move(file, destPath);
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            // Use the source path if it exists and is valid
            string folderPath = txtSource.Text;

            // If no source path or invalid, prompt user to select
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                var dialog = new OpenFolderDialog { Title = "Select Photos Folder" };
                if (dialog.ShowDialog() != true)
                {
                    txtStatus.Text = "Import canceled";
                    return;
                }
                folderPath = dialog.FolderName;
            }

            try
            {
                // Setup progress UI
                progressBar.Visibility = Visibility.Visible;
                progressBar.Value = 0;
                txtStatus.Text = "Preparing to import...";
                await Task.Delay(1); // Allow UI to update

                //var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".heic", ".webp" };
                var files = Directory.EnumerateFiles(folderPath)
                                   .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                                   .ToList();

                if (files.Count == 0)
                {
                    txtStatus.Text = "No photos found";
                    MessageBox.Show("No supported images found.", "Info",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                progressBar.Maximum = files.Count;
                Photos.Clear();

                int processed = 0;
                foreach (var file in files)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            // Simulate work (remove in production)
                            // Thread.Sleep(10); 
                        });

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Photos.Add(new PhotoItem
                            {
                                FilePath = file,
                                FileName = Path.GetFileName(file)
                            });
                        });

                        processed++;
                        progressBar.Value = processed;
                        txtStatus.Text = $"Importing {processed} of {files.Count}";

                        if (processed % 10 == 0)
                            await Application.Current.Dispatcher.InvokeAsync(() => { });
                    }
                    catch (Exception fileEx)
                    {
                        Debug.WriteLine($"Error with {file}: {fileEx.Message}");
                    }
                }

                txtStatus.Text = $"Imported {files.Count} photos";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Import failed";
                MessageBox.Show($"Error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private bool ValidateFolders(string source, string dest)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(dest))
            {
                MessageBox.Show("Please select both folders.", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Directory.Exists(source) || !Directory.Exists(dest))
            {
                MessageBox.Show("One or both folders don't exist.", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}