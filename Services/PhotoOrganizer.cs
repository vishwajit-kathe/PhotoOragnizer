using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.IO;

namespace PhotoOrganizerApp.Services
{
    public class PhotoOrganizer
    {
        public void OrganizeByDate(string sourceDir, string destinationDir)
        {
            var files = System.IO.Directory.GetFiles(sourceDir, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                try
                {
                    var directories = ImageMetadataReader.ReadMetadata(file);
                    var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                    DateTime dateTaken = subIfdDirectory?.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal) ?? File.GetCreationTime(file);
                    string dateFolder = Path.Combine(destinationDir, dateTaken.ToString("yyyy\\MM\\dd"));

                    System.IO.Directory.CreateDirectory(dateFolder);
                    string destFile = Path.Combine(dateFolder, Path.GetFileName(file));
                    File.Move(file, destFile); // Use File.Copy for copy
                }
                catch (Exception ex)
                {
                    // Handle exception or log
                }
            }
        }
    }
}
