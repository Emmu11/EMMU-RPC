using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace EmmuRpc
{
    internal static class VersionResourceWriter
    {
        private const int RT_VERSION = 16;
        private static readonly IntPtr ResourceId = new IntPtr(1);

        public static void Apply(string executablePath, string displayName, string originalFilename)
        {
            byte[] resource = BuildVersionResource(displayName, originalFilename);
            IntPtr update = BeginUpdateResource(executablePath, false);
            if (update == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            bool completed = false;
            try
            {
                if (!UpdateResource(update, new IntPtr(RT_VERSION), ResourceId, 0, resource, (uint)resource.Length))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                if (!EndUpdateResource(update, false))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                completed = true;
            }
            finally
            {
                if (!completed)
                    EndUpdateResource(update, true);
            }
        }

        private static byte[] BuildVersionResource(string displayName, string originalFilename)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode))
            {
                long root = BeginBlock(writer, "VS_VERSION_INFO", 52, 0);
                writer.Write(0xFEEF04BDu);
                writer.Write(0x00010000u);
                writer.Write(0x00010000u);
                writer.Write(0x00000000u);
                writer.Write(0x00010000u);
                writer.Write(0x00000000u);
                writer.Write(0x0000003Fu);
                writer.Write(0x00000000u);
                writer.Write(0x00040004u);
                writer.Write(0x00000001u);
                writer.Write(0x00000000u);
                writer.Write(0x00000000u);
                writer.Write(0x00000000u);
                Align(writer);

                long stringFileInfo = BeginBlock(writer, "StringFileInfo", 0, 1);
                Align(writer);
                long stringTable = BeginBlock(writer, "040904B0", 0, 1);
                Align(writer);

                WriteString(writer, "CompanyName", "EMMU");
                WriteString(writer, "FileDescription", displayName);
                WriteString(writer, "FileVersion", "1.0.0.0");
                WriteString(writer, "InternalName", displayName);
                WriteString(writer, "LegalCopyright", "Copyright © EMMU");
                WriteString(writer, "OriginalFilename", originalFilename);
                WriteString(writer, "ProductName", displayName);
                WriteString(writer, "ProductVersion", "1.0.0.0");

                EndBlock(writer, stringTable);
                EndBlock(writer, stringFileInfo);
                Align(writer);

                long varFileInfo = BeginBlock(writer, "VarFileInfo", 0, 1);
                Align(writer);
                long translation = BeginBlock(writer, "Translation", 4, 0);
                writer.Write((ushort)0x0409);
                writer.Write((ushort)0x04B0);
                EndBlock(writer, translation);
                EndBlock(writer, varFileInfo);
                EndBlock(writer, root);

                return stream.ToArray();
            }
        }

        private static void WriteString(BinaryWriter writer, string key, string value)
        {
            Align(writer);
            long block = BeginBlock(writer, key, (ushort)(value.Length + 1), 1);
            WriteUnicodeZ(writer, value);
            EndBlock(writer, block);
        }

        private static long BeginBlock(BinaryWriter writer, string key, ushort valueLength, ushort type)
        {
            long start = writer.BaseStream.Position;
            writer.Write((ushort)0);
            writer.Write(valueLength);
            writer.Write(type);
            WriteUnicodeZ(writer, key);
            Align(writer);
            return start;
        }

        private static void EndBlock(BinaryWriter writer, long start)
        {
            Align(writer);
            long end = writer.BaseStream.Position;
            if (end - start > UInt16.MaxValue)
                throw new InvalidOperationException("Version resource is too large.");
            long current = writer.BaseStream.Position;
            writer.BaseStream.Position = start;
            writer.Write((ushort)(end - start));
            writer.BaseStream.Position = current;
        }

        private static void WriteUnicodeZ(BinaryWriter writer, string value)
        {
            writer.Write(Encoding.Unicode.GetBytes(value));
            writer.Write((ushort)0);
        }

        private static void Align(BinaryWriter writer)
        {
            while ((writer.BaseStream.Position & 3) != 0)
                writer.Write((byte)0);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr BeginUpdateResource(string fileName, bool deleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UpdateResource(IntPtr update, IntPtr type, IntPtr name, ushort language, byte[] data, uint dataSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool EndUpdateResource(IntPtr update, bool discard);
    }
}
