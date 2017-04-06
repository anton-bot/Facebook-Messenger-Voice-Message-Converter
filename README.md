# Facebook-Messenger-Voice-Message-Converter
Converts the Facebook Messenger voice messages from video/mp4 or audio/aac to the PCM/WAV format suitable for Bing Speech API

This repository includes the IncomingFacebookVoiceMessage class (receives a voice file from a facebook voice message) and the converter class (converts MP4/AAC to WAV).

The WAV file can then be sent to Bing Speech API because that's the format it understands.

## Requirements

* Azure app service
* MediaToolKit from nuget
* FFMPEG.exe binary
* A bot connected to Microsoft Bot Framework and Facebook Messenger

## Sample implementation

### General idea

* Receives a message from Facebook
* Downloads the voice file from that message
* Converts MP4/AAC audio file to WAV using open-source FFMPEG library
* Sends the WAV file to Bing Speech API to convert speech to text

### Code

`MicrosoftCognitiveSpeechService` is not included. Use Microsoft's samples as a starting point, and modify them as necessary.

`SendReply()` is not included, just use the standard way of sending messages to the user. 

`Utils.GetHomeFolder()` is not included. It returns the root folder of the site. 

        //MessagesController.cs
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                string message = string.Empty;
                IncomingFacebookVoiceMessage voice = null;
                
                try
                {
                    voice = new IncomingFacebookVoiceMessage(activity);
                    //await SendReply(activity, "The type of your voice file is " + voice.ContentType);
                }
                catch
                {
                    message = "Send me a voice message instead!"; // no voice file found
                }

                try
                {
                    if (voice != null)
                    {
                        //Download original MP4 voice message
                        voice.DownloadFile();
                        var mp4 = voice.GetLocalPathAndFileName();

                        //Convert MP4 to WAV
                        var wavFolder = Utils.GetHomeFolder() + WebConfigurationManager.AppSettings["WAVFilesFolder"];
                        var converter = new AudioFileFormatConverter(mp4, wavFolder);
                        var wav = converter.ConvertMP4ToWAV();

                        //Convert .WAV file to text
                        var bing = new MicrosoftCognitiveSpeechService(); //gets the path + filename
                        var text = await bing.GetTextFromAudioAsync(wav); //takes path+filename to WAV file, returns text

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            message = "Looks like you didn't say anything.";
                        }
                        else
                        {
                            message = text;
                        }

                        //Clean up files from disk
                        voice.RemoveFromDisk();
                        converter.RemoveTargetFileFromDisk();
                    }
                }
                catch (Exception ex)
                {
                    message = "Woah! " + ex.Message.Trim().Trim('.') + "!";
                }

                await SendReply(activity, message);
            }
            else
            {
                await this.HandleSystemMessage(activity);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }
