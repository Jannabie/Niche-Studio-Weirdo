using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NicheStudioWeirdo.Utils
{
    /// <summary>
    /// Native C# port of extYbn.go (from chinesize/yuris) for YU-RIS v481+ .ybn files.
    /// Properly guesses msgOp by scanning instruction patterns — no VNTextPatch needed.
    /// </summary>
    public static class YurisYbnParser
    {
        // YSTB header layout (32 bytes total, little-endian) — verified on v481:
        //  0..3   magic "YSTB"
        //  4..7   version  (e.g. 481)
        //  8..11  instCount
        //  12..15 codeSize    (= instCount × 4)
        //  16..19 argSize
        //  20..23 resourceSize
        //  24..27 offSize
        //  28..31 reserved (0)
        //
        // After header:
        //  [HEADER=32]  CODE  ARG  RESOURCE  OFFTABLE
        //
        // Each instruction (CODE section): 4 bytes
        //   byte op, byte argCnt, ushort reserved
        //
        // Each argument (ARG section): 12 bytes
        //   ushort value, ushort type, uint resLen, uint resOff
        //
        // type==3 resource: byte resType, ushort len, data[len]
        // type==0 resource: raw bytes of resLen

        private const int HEADER_SIZE = 32;

        private class Inst
        {
            public byte Op;
            public List<Arg> Args = new List<Arg>();
        }
        private class Arg
        {
            public ushort Value;
            public ushort Type;
            public uint ResLen;
            public uint ResOff;
            public byte ResType;      // only when Type==3
            public byte[] ResBytes;   // type==3 payload
            public byte[] ResRaw;     // type==0 payload
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC: Extract
        // ─────────────────────────────────────────────────────────────────────
        public static bool Extract(string ybnPath, string txtPath)
        {
            try
            {
                byte[] data = File.ReadAllBytes(ybnPath);
                if (!IsYstb(data)) return false;

                var (instCount, codeSize, argSize, resourceSize) = ReadHeader(data);
                if (instCount == 0) return false;

                long argBase      = HEADER_SIZE + codeSize;
                long resourceBase = argBase + argSize;

                // Sanity: sections must fit inside the file
                if (argBase > data.Length || resourceBase > data.Length) return false;

                var insts = ParseInstructions(data, instCount, codeSize, argBase, resourceBase);

                byte msgOp = GuessMsgOp(insts);
                if (msgOp == 0) return false;

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var enc = Encoding.GetEncoding(932); // Shift-JIS

                var sb = new StringBuilder();
                foreach (var inst in insts)
                {
                    if (inst.Op != msgOp || inst.Args.Count != 1) continue;
                    var arg = inst.Args[0];
                    byte[] raw = arg.Type == 3 ? arg.ResBytes : arg.ResRaw;
                    if (raw == null || raw.Length == 0) continue;

                    string msg;
                    try { msg = enc.GetString(raw); }
                    catch { continue; }

                    if (string.IsNullOrWhiteSpace(msg)) continue;

                    sb.AppendLine("◇");
                    sb.AppendLine("[Original]");
                    sb.AppendLine(msg);
                    sb.AppendLine("[Translated]");
                    sb.AppendLine(msg);
                    sb.AppendLine();
                }

                if (sb.Length == 0) return false;
                File.WriteAllText(txtPath, sb.ToString(), new UTF8Encoding(true));
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC: Insert
        // ─────────────────────────────────────────────────────────────────────
        public static bool Insert(string ybnPath, string txtPath)
        {
            try
            {
            // Parse the translation file
            var translated = ReadTranslatedLines(txtPath);
            if (translated.Count == 0) return false;

            byte[] data = File.ReadAllBytes(ybnPath);
            if (!IsYstb(data)) return false;

            var (instCount, codeSize, argSize, resourceSize) = ReadHeader(data);
            long argBase      = HEADER_SIZE + (long)codeSize;
            long resourceBase = argBase + argSize;

            var insts = ParseInstructions(data, instCount, codeSize, argBase, resourceBase);
            byte msgOp = GuessMsgOp(insts);
            if (msgOp == 0) return false;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc = Encoding.GetEncoding(932);

            // We'll build a new resource tail and patch the arg section in-place
            var argBytes = new byte[argSize];
            Array.Copy(data, argBase, argBytes, 0, argSize);

            using var resTail = new MemoryStream();
            uint resNewOffset = (uint)resourceSize;
            int txtIdx = 0;
            int argIdx = 0;

            foreach (var inst in insts)
            {
                if (inst.Op == msgOp && inst.Args.Count == 1 && txtIdx < translated.Count)
                {
                    var arg = inst.Args[0];
                    string t = translated[txtIdx++];
                    byte[] nsBytes;
                    try { nsBytes = enc.GetBytes(t); }
                    catch { nsBytes = Encoding.UTF8.GetBytes(t); }

                    byte[] packedRes;
                    if (arg.Type == 3)
                    {
                        packedRes = new byte[3 + nsBytes.Length];
                        packedRes[0] = arg.ResType;
                        BitConverter.GetBytes((ushort)nsBytes.Length).CopyTo(packedRes, 1);
                        Array.Copy(nsBytes, 0, packedRes, 3, nsBytes.Length);
                    }
                    else
                    {
                        packedRes = nsBytes;
                    }

                    resTail.Write(packedRes, 0, packedRes.Length);

                    // Patch argBytes: at argIdx*12 + 4 write new resLen(uint) and resOff(uint)
                    int argByteOff = argIdx * 12 + 4;
                    BitConverter.GetBytes((uint)packedRes.Length).CopyTo(argBytes, argByteOff);
                    BitConverter.GetBytes(resNewOffset).CopyTo(argBytes, argByteOff + 4);
                    resNewOffset += (uint)packedRes.Length;
                }
                argIdx += inst.Args.Count;
            }

            byte[] tailBytes = resTail.ToArray();

            // Rebuild new header with updated resourceSize
            byte[] newData = new byte[data.Length + tailBytes.Length];
            // Copy original header
            Array.Copy(data, 0, newData, 0, HEADER_SIZE);
            // Patch resourceSize in header (offset 24)
            BitConverter.GetBytes((uint)resourceSize + (uint)tailBytes.Length).CopyTo(newData, 24);
            // Copy code section
            Array.Copy(data, HEADER_SIZE, newData, HEADER_SIZE, codeSize);
            // Write patched arg section
            Array.Copy(argBytes, 0, newData, argBase, argSize);
            // Copy original resource section
            int resStart = (int)(resourceBase);
            Array.Copy(data, resStart, newData, resStart, resourceSize);
            // Append new resource tail
            Array.Copy(tailBytes, 0, newData, resStart + resourceSize, tailBytes.Length);
            // Copy offset table
            int offStart = resStart + resourceSize;
            int offSize  = data.Length - offStart;
            Array.Copy(data, offStart, newData, offStart + tailBytes.Length, offSize);

            File.WriteAllBytes(ybnPath, newData);
            return true;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static bool IsYstb(byte[] data) =>
            data.Length >= HEADER_SIZE &&
            data[0] == 'Y' && data[1] == 'S' && data[2] == 'T' && data[3] == 'B';

        private static (int instCount, int codeSize, int argSize, int resourceSize) ReadHeader(byte[] data)
        {
            // Verified correct layout for YU-RIS v481:
            //   instCount    @ 8
            //   codeSize     @ 12  (= instCount × 4)
            //   argSize      @ 16
            //   resourceSize @ 20
            //   offSize      @ 24
            int instCount    = BitConverter.ToInt32(data, 8);
            int codeSize     = BitConverter.ToInt32(data, 12);
            int argSize      = BitConverter.ToInt32(data, 16);
            int resourceSize = BitConverter.ToInt32(data, 20);
            // Sanity check: codeSize should == instCount * 4
            if (instCount > 0 && codeSize != instCount * 4)
            {
                // Fallback: derive instCount from codeSize
                instCount = codeSize / 4;
            }
            return (instCount, codeSize, argSize, resourceSize);
        }

        private static List<Inst> ParseInstructions(byte[] data, int instCount,
            int codeSize, long argBase, long resourceBase)
        {
            var insts = new List<Inst>(instCount);

            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms, Encoding.ASCII, true);

            // Read CODE section
            ms.Position = HEADER_SIZE;
            var rawOps   = new byte[instCount];
            var rawArgCnt = new byte[instCount];
            for (int i = 0; i < instCount; i++)
            {
                rawOps[i]    = br.ReadByte();
                rawArgCnt[i] = br.ReadByte();
                br.ReadUInt16(); // reserved
            }

            // Count total args — clamp to what actually fits in the buffer
            int totalArgs = rawArgCnt.Sum(x => (int)x);
            int maxFitArgs = (int)((data.Length - argBase) / 12);
            if (totalArgs > maxFitArgs) totalArgs = maxFitArgs;

            // Read ARG section
            ms.Position = argBase;
            var argVals   = new ushort[totalArgs];
            var argTypes  = new ushort[totalArgs];
            var argResLen = new uint[totalArgs];
            var argResOff = new uint[totalArgs];
            for (int i = 0; i < totalArgs; i++)
            {
                if (ms.Position + 12 > data.Length) { totalArgs = i; break; }
                argVals[i]   = br.ReadUInt16();
                argTypes[i]  = br.ReadUInt16();
                argResLen[i] = br.ReadUInt32();
                argResOff[i] = br.ReadUInt32();
            }

            // Build Inst list and read resource payloads
            int argGlobal = 0;
            for (int i = 0; i < instCount; i++)
            {
                var inst = new Inst { Op = rawOps[i] };
                for (int j = 0; j < rawArgCnt[i]; j++)
                {
                    var arg = new Arg
                    {
                        Value  = argVals[argGlobal],
                        Type   = argTypes[argGlobal],
                        ResLen = argResLen[argGlobal],
                        ResOff = argResOff[argGlobal],
                    };

                    // Read resource bytes — but only for args that have a resource
                    // Skip if type==0 and argCnt != 1 (those are value references, no inline resource)
                    bool hasResource = !(arg.Type == 0 && rawArgCnt[i] != 1);
                    if (hasResource && arg.ResLen > 0 && resourceBase + arg.ResOff < data.Length)
                    {
                        ms.Position = resourceBase + arg.ResOff;
                        if (arg.Type == 3)
                        {
                            // type-3 resource: byte resType, ushort len, data
                            if (ms.Position + 3 > data.Length) { inst.Args.Add(arg); argGlobal++; continue; }
                            arg.ResType = br.ReadByte();
                            int len = br.ReadUInt16();
                            int safeLen = Math.Min(len, (int)(data.Length - ms.Position));
                            if (safeLen > 0) arg.ResBytes = br.ReadBytes(safeLen);
                        }
                        else
                        {
                            int safeLen = Math.Min((int)arg.ResLen, (int)(data.Length - ms.Position));
                            if (safeLen > 0) arg.ResRaw = br.ReadBytes(safeLen);
                        }
                    }

                    inst.Args.Add(arg);
                    argGlobal++;
                }
                insts.Add(inst);
            }

            return insts;
        }

        private static byte GuessMsgOp(List<Inst> insts)
        {
            int[] msgStat = new int[256];

            foreach (var inst in insts)
            {
                if (inst.Args.Count != 1) continue;
                var arg = inst.Args[0];

                // Japanese/Chinese check: type==0 OR type==3, raw string starts with byte > 0x80
                byte[] raw = arg.Type == 3 ? arg.ResBytes : arg.ResRaw;
                if (raw != null && raw.Length > 0 && raw[0] > 0x80)
                {
                    msgStat[inst.Op]++;
                    if (msgStat[inst.Op] > 10) return inst.Op; // early exit
                }

                // English long-sentence check: type==3, all ASCII, >5 spaces
                if (arg.Type == 3 && arg.ResBytes != null && arg.ResBytes.Length > 10)
                {
                    bool allAscii = arg.ResBytes.All(b => b < 0x80);
                    int spaces    = arg.ResBytes.Count(b => b == (byte)' ');
                    if (allAscii && spaces > 5)
                    {
                        msgStat[inst.Op]++;
                        if (msgStat[inst.Op] > 10) return inst.Op;
                    }
                }
            }

            int max = msgStat.Max();
            if (max == 0) return 0;
            return (byte)Array.IndexOf(msgStat, max);
        }

        private static List<string> ReadTranslatedLines(string txtPath)
        {
            var result = new List<string>();
            var lines  = File.ReadAllLines(txtPath);
            bool isTrans = false;
            var cur = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Trim() == "◇")
                {
                    if (isTrans && cur.Length > 0)
                    {
                        result.Add(cur.ToString().TrimEnd('\r', '\n'));
                        cur.Clear();
                    }
                    isTrans = false;
                    continue;
                }
                if (line.Trim() == "[Original]")  { isTrans = false; continue; }
                if (line.Trim() == "[Translated]") { isTrans = true;  continue; }
                if (isTrans) cur.AppendLine(line);
            }
            if (isTrans && cur.Length > 0)
                result.Add(cur.ToString().TrimEnd('\r', '\n'));

            return result;
        }
    }
}
