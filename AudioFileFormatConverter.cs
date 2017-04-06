using Imago.Bots;
using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace Imago.Media
{
    /// <summary>
    /// Converts audio files between various formats using the open-source FFMPEG software.
    /// </summary>
    public class AudioFileFormatConverter
    {
        #region Properties
        /// <summary>
        /// Path + filename to the source file.
        /// </summary>
        public string SourceFileNameAndPath { get; private set; }

        /// <summary>
        /// Path + filename to the file which is the result of the conversion.
        /// </summary>
        public string ConvertedFileNameAndPath { get; private set; }

        /// <summary>
        /// The folder where the converted files will be stored.
        /// </summary>
        public string TargetPath { get; private set; }

        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor. 
        /// </summary>
        /// <param name="sourceFileNameAndPath">Filename and path to the source file.</param>
        /// <param name="targetPath">The folder where the converted files will be stored.</param>
        public AudioFileFormatConverter(string sourceFileNameAndPath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(sourceFileNameAndPath))
            {
                throw new Exception("Empty source filename.");
            }
            else if (string.IsNullOrWhiteSpace(targetPath))
            {
                throw new Exception("Empty target path.");
            }
            else
            {
                this.SourceFileNameAndPath = sourceFileNameAndPath;
                this.TargetPath = targetPath;

                //create folder if it's not there
                if (!Directory.Exists(TargetPath))
                {
                    Directory.CreateDirectory(TargetPath);
                }
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Converts a source MP4 file to a target WAV file and returns the path and filename of the converted file.
        /// </summary>
        /// <returns>The path and filename of the converted file.</returns>
        public string ConvertMP4ToWAV()
        {
            ConvertedFileNameAndPath = TargetPath + @"\" + Path.GetFileNameWithoutExtension(SourceFileNameAndPath) + ".wav"; //use the same file name as original, but different folder and extension

            var inputFile = new MediaFile { Filename = SourceFileNameAndPath };
            var outputFile = new MediaFile { Filename = ConvertedFileNameAndPath };
            using (var engine = new Engine(GetFFMPEGBinaryPath()))
            {
                engine.Convert(inputFile, outputFile);
            }

            return ConvertedFileNameAndPath;
        }

        /// <summary>
        /// Removes the converted file from disk to free up space. 
        /// Doesn't remove the source file.
        /// </summary>
        /// <returns>True if file deleted successfully, false if cannot delete, exception if filename is empty.</returns>
        public bool RemoveTargetFileFromDisk()
        {
            if (string.IsNullOrWhiteSpace(ConvertedFileNameAndPath))
            {
                throw new Exception("The file has not been converted yet, so it cannot be deleted.");
            }

            try
            {
                File.Delete(ConvertedFileNameAndPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Reads the config to determine the location of ffmpeg.exe.
        /// </summary>
        /// <returns>The full path and filename to the ffmpeg.exe program.</returns>
        public string GetFFMPEGBinaryPath()
        {
            return Utils.GetHomeFolder() + WebConfigurationManager.AppSettings["FFMPEGBinaryLocation"];
        }
        #endregion
    }
}
