using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Configuration;
using Imago.Bots;

namespace Imago.Facebook
{
    /// <summary>
    /// Represents an incoming voice message from facebook messenger, 
    /// where the voice data is in an MP4 file (the message contains a link to download it).
    /// </summary>
    public class IncomingFacebookVoiceMessage
    {
        #region Properties
        /// <summary>
        /// URL of the MP4 file sent by user and stored on facebook's servers.
        /// </summary>
        public Uri MP4FileUrl { get; private set; }
        
        /// <summary>
        /// Local filename of the MP4 file after it has been downloaded from Facebook.
        /// </summary>
        private string MP4LocalFileName { get; set; }

        /// <summary>
        /// Path to the folder on local disk containing the downloaded voice messages from Facebook.
        /// This is configured in Web.config using the FacebookDownloadedVoiceMessagesFolder key.
        /// The path in the Web.config will be relative to the site's root folder.
        /// </summary>
        public string VoiceMessageFolder { get; private set; }

        /// <summary>
        /// Content-type of the attachment (for debugging - it's not always MP4).
        /// </summary>
        public string ContentType { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor that uses an MP4 file link that is 
        /// received in activity.Attachments by bot framework.
        /// </summary>
        /// <param name="MP4FileUrl">URL of the MP4 file sent by user and stored on facebook's servers.</param>
        public IncomingFacebookVoiceMessage(string MP4FileUrl)
        {
            if (string.IsNullOrWhiteSpace(MP4FileUrl))
            {
                throw new Exception("The MP4 file URL was empty.");
            }

            this.MP4FileUrl = new Uri(MP4FileUrl);
            this.VoiceMessageFolder = GetVoiceMessagesFolderFromWebConfig();
        }

        /// <summary>
        /// A shortcut constructor that extracts the URL of the MP4 voice message file
        /// from the Activity object received by the controller in the Bot Framework.
        /// </summary>
        /// <param name="activity">The Activity object that contains an attachment of type video/mp4. If no attachment, throws an exception.</param>
        public IncomingFacebookVoiceMessage(IMessageActivity activity)
        {
            var mp4Attachment = activity.Attachments?.FirstOrDefault(a => a.ContentType.Equals("video/mp4") || a.ContentType.Contains("audio") || a.ContentType.Contains("video"));
            if (mp4Attachment == null)
            {
                throw new Exception("The message didn't have a voice attachment.");
            }
            else
            {
                this.MP4FileUrl = new Uri(mp4Attachment.ContentUrl);
                this.VoiceMessageFolder = GetVoiceMessagesFolderFromWebConfig();
                this.ContentType = mp4Attachment.ContentType; //for debugging. Different devices send different content-types, e.g. audio/aac and video/mp4
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Downloads the MP4 file containing the voice message from Facebook.
        /// </summary>
        /// <returns>The filename (without path) of the MP4 file stored on local disk.</returns>
        public string DownloadFile()
        {
            var filename = GetRandomFileName();
            var filenameWithPath = VoiceMessageFolder + @"\" + filename;
 
            //if folder doesn't exist, create it
            if (!Directory.Exists(VoiceMessageFolder))
            {
                Directory.CreateDirectory(VoiceMessageFolder);
            }

            using (var client = new WebClient())
            {
                client.DownloadFile(this.MP4FileUrl, filenameWithPath);
            }

            MP4LocalFileName = filename;

            return filename;
        }

        /// <summary>
        /// Removes the downloaded MP4 file from the local disk to clean up space.
        /// </summary>
        /// <returns>True if successfully removed, false otherwise.</returns>
        public bool RemoveFromDisk()
        {
            try
            {
                File.Delete(GetLocalPathAndFileName());
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the full local path and filename to the downloaded MP4 voice message.
        /// </summary>
        /// <returns>E.g. D:\home\site\wwwroot\abc.mp4</returns>
        public string GetLocalPathAndFileName()
        {
            if (string.IsNullOrWhiteSpace(MP4LocalFileName))
            {
                throw new Exception("The voice message has not been downloaded yet.");
            }

            return VoiceMessageFolder + @"\" + MP4LocalFileName;
        }

        #endregion

        #region Private methods
        /// <summary>
        /// Reads Web.config and returns the path to the folder which will store downloaded messages.
        /// The folder in the config must be relative to the site's root.
        /// </summary>
        /// <returns>Full path to the folder that will be used to store MP4 voice messages.</returns>
        private string GetVoiceMessagesFolderFromWebConfig()
        {
            return Utils.GetHomeFolder() + WebConfigurationManager.AppSettings["FacebookDownloadedVoiceMessagesFolder"];
        }

        /// <summary>
        /// Generates a random filename using a new GUID.
        /// </summary>
        /// <returns>A random file name in the format "msg-GUID.mp4".</returns>
        private string GetRandomFileName()
        {
            return "msg-" + Guid.NewGuid() + ".mp4";
        }
        #endregion
    }
}