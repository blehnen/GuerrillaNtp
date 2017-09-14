﻿using System;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents a packet for communications to and from a network time server
    /// </summary>
    public class NtpPacket
    {
        readonly DateTime _primeEpoch = new DateTime(1900, 1, 1);

        /// <summary>
        /// Gets the byte array representing this packet
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// Gets or sets the value indicating which, if any, warning should be sent due to an impending leap second
        /// </summary>
        public NtpLeapIndicator LeapIndicator
        {
            get => (NtpLeapIndicator)((Bytes[0] & 0xC0) >> 6);
            set => Bytes[0] = (byte)((Bytes[0] & ~0xC0) | ((int)value << 6));
        }

        /// <summary>
        /// Gets or sets the version number
        /// </summary>
        public int VersionNumber
        {
            get => (Bytes[0] & 0x38) >> 3;
            set => Bytes[0] = (byte)((Bytes[0] & ~0x38) | (value << 3));
        }

        /// <summary>
        /// Gets or sets the association mode
        /// </summary>
        public NtpMode Mode
        {
            get => (NtpMode)(Bytes[0] & 0x07);
            set => Bytes[0] = (byte)((Bytes[0] & ~0x07) | (int)value);
        }

        /// <summary>
        /// Gets the server's distance from the reference clock
        /// </summary>
        public int Stratum => Bytes[1];

        /// <summary>
        /// Gets the polling interval (in log₂ seconds)
        /// </summary>
        public int Poll => Bytes[2];

        /// <summary>
        /// Gets the precision of the system clock (in log₂ seconds)
        /// </summary>
        public int Precision => Bytes[3];

        /// <summary>
        /// Gets the total round trip delay from the server to the reference clock
        /// </summary>
        public int RootDelay => GetInt32Be(4);

        /// <summary>
        /// Gets the amount of jitter the server observes in the reference clock
        /// </summary>
        public int RootDispersion => GetInt32Be(8);

        /// <summary>
        /// Gets the ID of the server or reference clock
        /// </summary>
        public uint ReferenceId => GetUInt32Be(12);

        /// <summary>
        /// Gets the date and time the server was last set or corrected
        /// </summary>
        public DateTime? ReferenceTimestamp { get => GetDateTime64(16);
            set => SetDateTime64(16, value);
        }

        /// <summary>
        /// Gets the date and time this packet left the server
        /// </summary>
        public DateTime? OriginTimestamp { get => GetDateTime64(24);
            set => SetDateTime64(24, value);
        }

        /// <summary>
        /// Gets the date and time this packet was received by the server
        /// </summary>
        public DateTime? ReceiveTimestamp { get => GetDateTime64(32);
            set => SetDateTime64(32, value);
        }

        /// <summary>
        /// Gets the date and time that the packet was transmitted from the server
        /// </summary>
        public DateTime? TransmitTimestamp { get => GetDateTime64(40);
            set => SetDateTime64(40, value);
        }

        /// <summary>
        /// Gets or sets the time of reception of response NTP packet on the client.
        /// This property is not part of the protocol. It is set by NtpClient.
        /// </summary>
        public DateTime? DestinationTimestamp { get; set; }

        /// <summary>
        /// Time spent on the wire in both directions together
        /// </summary>
        public TimeSpan RoundTripTime
        {
            get
            {
                if (OriginTimestamp == null || ReceiveTimestamp == null || TransmitTimestamp == null || DestinationTimestamp == null)
                    throw new InvalidOperationException();
                return ReceiveTimestamp.Value - OriginTimestamp.Value + (DestinationTimestamp.Value - TransmitTimestamp.Value);
            }
        }

        /// <summary>
        /// Offset that should be added to local time to synchronize it with server time
        /// </summary>
        public TimeSpan CorrectionOffset
        {
            get
            {
                if (OriginTimestamp == null || ReceiveTimestamp == null || TransmitTimestamp == null || DestinationTimestamp == null)
                    throw new InvalidOperationException();
                return TimeSpan.FromTicks((ReceiveTimestamp.Value - OriginTimestamp.Value - (DestinationTimestamp.Value - TransmitTimestamp.Value)).Ticks / 2);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:GuerrillaNtp.NtpPacket" /> class
        /// </summary>
        public NtpPacket()
            : this(new byte[48])
        {
            Mode = NtpMode.Client;
            VersionNumber = 4;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtpPacket"/> class
        /// </summary>
        /// <param name="bytes">
        /// A byte array representing an NTP packet
        /// </param>
        public NtpPacket(byte[] bytes)
        {
            if (bytes.Length < 48)
                throw new ArgumentOutOfRangeException("bytes", "The byte array must be at least length 48.");
            Bytes = bytes;
        }

        private DateTime? GetDateTime64(int offset)
        {
            var field = GetUInt64Be(offset);
            if (field == 0)
                return null;
            return new DateTime(_primeEpoch.Ticks + Convert.ToInt64(field * (1.0 / (1L << 32) * 10000000.0)));
        }
        private void SetDateTime64(int offset, DateTime? value) { SetUInt64Be(offset, value == null ? 0 : Convert.ToUInt64((value.Value.Ticks - _primeEpoch.Ticks) * (0.0000001 * (1L << 32)))); }
        private ulong GetUInt64Be(int offset) { return SwapEndianness(BitConverter.ToUInt64(Bytes, offset)); }
        private void SetUInt64Be(int offset, ulong value) { Array.Copy(BitConverter.GetBytes(SwapEndianness(value)), 0, Bytes, offset, 8); }
        private int GetInt32Be(int offset) { return (int)GetUInt32Be(offset); }
        private uint GetUInt32Be(int offset) { return SwapEndianness(BitConverter.ToUInt32(Bytes, offset)); }
        private static uint SwapEndianness(uint x) { return ((x & 0xff) << 24) | ((x & 0xff00) << 8) | ((x & 0xff0000) >> 8) | ((x & 0xff000000) >> 24); }
        private static ulong SwapEndianness(ulong x) { return ((ulong)SwapEndianness((uint)x) << 32) | SwapEndianness((uint)(x >> 32)); }
    }
}