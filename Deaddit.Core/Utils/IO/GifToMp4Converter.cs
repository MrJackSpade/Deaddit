using Deaddit.Core.Interfaces;
using Loxifi.FFmpeg.Transcoding;
using Loxifi.FFmpeg.Transcoding.Codecs;

namespace Deaddit.Core.Utils.IO
{
    public class GifToMp4Converter : IStreamConverter
    {
        public bool CanConvert(string fileName)
        {
            return Path.GetExtension(fileName).Equals(".gif", StringComparison.OrdinalIgnoreCase);
        }

        public string ConvertFileName(string fileName)
        {
            return Path.ChangeExtension(fileName, ".mp4");
        }

        public async Task<Stream> ConvertAsync(Stream input)
        {
            MemoryStream gifMemory = new();
            await input.CopyToAsync(gifMemory);
            gifMemory.Position = 0;

            MemoryStream mp4Output = new();

            using MediaTranscoder transcoder = new();
            transcoder.Transcode(new StreamTranscodeOptions
            {
                InputStream = gifMemory,
                OutputStream = mp4Output,
                OutputFormat = ContainerFormat.Mp4,
                VideoCodec = GPL.Video.X264,
            });

            mp4Output.Position = 0;
            return mp4Output;
        }
    }
}
