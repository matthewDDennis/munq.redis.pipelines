using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Utf8;

namespace Munq.Redis.Client
{
    public static class RedisProtocol
    {
        public const byte ArrayStart        = (byte)'*';
        public const byte SimpleStringStart = (byte)'+';
        public const byte BulkStringStart   = (byte)'$';
        public const byte NumberStart       = (byte)':';
        public const byte ErrorStart        = (byte)'-';

        public static readonly Utf8String Utf8CRLF              = (Utf8String)"\r\n";
        public static readonly Utf8String Utf8NullString        = (Utf8String)"$-1\r\n";
        public static readonly Utf8String Utf8RedisTrue         = (Utf8String)"1";
        public static readonly Utf8String Utf8RedisFalse        = (Utf8String)"0";
        public static readonly Utf8String Utf8ArrayStart        = new Utf8String(new byte[] {ArrayStart       });
        public static readonly Utf8String Utf8SimpleStringStart = new Utf8String(new byte[] {SimpleStringStart});
        public static readonly Utf8String Utf8BulkStringStart   = new Utf8String(new byte[] {BulkStringStart  });
        public static readonly Utf8String Utf8NumberStart       = new Utf8String(new byte[] {NumberStart      });
        public static readonly Utf8String Utf8ErrorStart        = new Utf8String(new byte[] {ErrorStart       });

    }
}
