﻿using System.Net;
using Newtonsoft.Json;

namespace PandApache3.src.Core.Configuration;

public class IPAddressConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IPAddress);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var ip = (IPAddress)value;
        writer.WriteValue(ip.ToString());
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var ipString = (string)reader.Value;
        return IPAddress.Parse(ipString);
    }
}
