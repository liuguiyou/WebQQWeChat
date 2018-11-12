using FclEx;
using Newtonsoft.Json;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace WebQQ.Util
{
    public class ImageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SixLabors.ImageSharp.Image<Rgba32>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ((string)reader.Value).Base64StringToImage();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var bmp = (SixLabors.ImageSharp.Image<Rgba32>)value;
            writer.WriteValue(bmp.ToRawBase64String());
        }
    }
}
