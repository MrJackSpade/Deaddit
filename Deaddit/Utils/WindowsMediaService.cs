//using CommunityToolkit.Maui.Views;
//using Deaddit.Core.Extensions;
//using Deaddit.Core.Interfaces;
//using Deaddit.Core.Utils;

//namespace Deaddit.Utils
//{
//    public class WindowsMediaService : IMediaService
//    {
//        public async Task<byte[]> DownloadAndMuxAsync(string m3u8Url)
//        {
//            // Download the M3U8 playlist and parse it
//            M3U8Downloader.MediaData mediaData = await M3U8Downloader.DownloadM3U8Async(m3u8Url);

//            // Save the video and audio data to temporary files
//            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
//            string videoFileName = Guid.NewGuid().ToString() + "_video.mp4";
//            string audioFileName = Guid.NewGuid().ToString() + "_audio.mp4";
//            string outputFileName = Guid.NewGuid().ToString() + "_output.mp4";

//            StorageFile videoFile = await tempFolder.CreateFileAsync(videoFileName, CreationCollisionOption.ReplaceExisting);
//            StorageFile audioFile = await tempFolder.CreateFileAsync(audioFileName, CreationCollisionOption.ReplaceExisting);
//            StorageFile outputFile = await tempFolder.CreateFileAsync(outputFileName, CreationCollisionOption.ReplaceExisting);

//            try
//            {
//                await FileIO.WriteBytesAsync(videoFile, mediaData.VideoData);
//                await FileIO.WriteBytesAsync(audioFile, mediaData.AudioData);

//                // Mux the audio and video into a single MP4 file
//                await this.MuxStreamsAsync(videoFile, audioFile, outputFile);

//                // Read the output file into a byte array
//                byte[] outputData = await this.ReadFileAsync(outputFile);

//                return outputData;
//            }
//            finally
//            {
//                // Clean up temporary files
//                await videoFile.DeleteAsync();
//                await audioFile.DeleteAsync();
//                await outputFile.DeleteAsync();
//            }
//        }

//        private async Task MuxStreamsAsync(StorageFile videoFile, StorageFile audioFile, StorageFile outputFile)
//        {
//            MediaEncodingProfile profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);

//            MediaComposition composition = new();

//            // Add video
//            MediaClip videoClip = await MediaClip.CreateFromFileAsync(videoFile);
//            composition.Clips.Add(videoClip);

//            // Add audio overlay
//            composition.BackgroundAudioTracks.Add(await BackgroundAudioTrack.CreateFromFileAsync(audioFile));

//            // Render the composition to the output file
//            Windows.Foundation.IAsyncOperationWithProgress<TranscodeFailureReason, double> saveOperation = composition.RenderToFileAsync(outputFile, MediaTrimmingPreference.Precise, profile);

//            saveOperation.Progress = (info, progress) =>
//            {
//                // Optionally, handle progress updates
//            };

//            TranscodeFailureReason result = await saveOperation;

//            if (result != TranscodeFailureReason.None)
//            {
//                throw new Exception("Transcoding failed: " + result.ToString());
//            }
//        }

//        private async Task<byte[]> ReadFileAsync(StorageFile file)
//        {
//            using Windows.Storage.Streams.IRandomAccessStreamWithContentType stream = await file.OpenReadAsync();
//            byte[] buffer = new byte[stream.Size];
//            using (Windows.Storage.Streams.DataReader dataReader = new(stream))
//            {
//                await dataReader.LoadAsync((uint)stream.Size);
//                dataReader.ReadBytes(buffer);
//            }

//            return buffer;
//        }
//    }
//}
