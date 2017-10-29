using System;
using System.ServiceModel.Channels;

namespace GaboG.ServiceBusRelayUtil
{
    internal class RawContentTypeMapper : WebContentTypeMapper
    {
        public override WebContentFormat GetMessageFormatForContentType(string contentType)
        {
            return WebContentFormat.Raw;
        }
    }
}