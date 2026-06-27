using System;
using System.IO;

namespace NicheStudioWeirdo.Utils
{
    public class BgiDecoderBase
    {
        protected Stream m_input;
        protected int m_cache;
        protected int m_cached_bits;

        protected uint m_key;
        protected uint m_magic;

        public BgiDecoderBase(Stream input)
        {
            m_input = input;
            m_cache = 0;
            m_cached_bits = 0;
        }

        public int GetNextBit()
        {
            if (m_cached_bits <= 0)
            {
                m_cache = m_input.ReadByte();
                if (m_cache == -1) return -1;
                m_cached_bits = 8;
            }
            m_cached_bits--;
            return (m_cache >> m_cached_bits) & 1;
        }

        public int GetBits(int count)
        {
            if (count <= 0) return 0;
            int val = 0;
            while (count > 0)
            {
                if (m_cached_bits == 0)
                {
                    m_cache = m_input.ReadByte();
                    if (m_cache == -1) return -1;
                    m_cached_bits = 8;
                }
                int extract = Math.Min(count, m_cached_bits);
                val = (val << extract) | ((m_cache >> (m_cached_bits - extract)) & ((1 << extract) - 1));
                m_cached_bits -= extract;
                count -= extract;
            }
            return val;
        }

        protected byte UpdateKey()
        {
            uint v0 = 20021 * (m_key & 0xffff);
            uint v1 = m_magic | (m_key >> 16);
            v1 = v1 * 20021 + m_key * 346;
            v1 = (v1 + (v0 >> 16)) & 0xffff;
            m_key = (v1 << 16) + (v0 & 0xffff) + 1;
            return (byte)v1;
        }
    }

    public class BurikoDscDecoder : BgiDecoderBase
    {
        byte[] m_output;
        uint m_dec_count;

        public byte[] Output => m_output;

        public BurikoDscDecoder(Stream input) : base(input)
        {
            using (var reader = new BinaryReader(input, System.Text.Encoding.Default, true))
            {
                m_magic = (uint)reader.ReadUInt16() << 16;
                input.Position = 0x10;
                m_key = reader.ReadUInt32();
                int output_size = reader.ReadInt32();
                m_dec_count = reader.ReadUInt32();
                m_output = new byte[output_size];
            }
        }

        public byte[] Unpack()
        {
            m_input.Position = 0x20;
            HuffmanCode[] hcodes = new HuffmanCode[512];
            HuffmanNode[] hnodes = new HuffmanNode[1023];

            int leaf_node_count = 0;
            for (ushort i = 0; i < 512; i++)
            {
                int src = m_input.ReadByte();
                if (src == -1) throw new EndOfStreamException();
                byte depth = (byte)(src - UpdateKey());
                if (0 != depth)
                {
                    hcodes[leaf_node_count] = new HuffmanCode { Depth = depth, Code = i };
                    leaf_node_count++;
                }
            }
            
            Array.Sort(hcodes, 0, leaf_node_count);
            for (int i=0; i<hnodes.Length; i++) hnodes[i] = new HuffmanNode();
            
            CreateHuffmanTree(hnodes, hcodes, leaf_node_count);
            HuffmanDecompress(hnodes, m_dec_count);
            
            return m_output;
        }

        struct HuffmanCode : IComparable<HuffmanCode>
        {
            public ushort Code;
            public ushort Depth;
            public int CompareTo(HuffmanCode other)
            {
                int cmp = Depth - other.Depth;
                if (cmp == 0) cmp = Code - other.Code;
                return cmp;
            }
        }

        class HuffmanNode
        {
            public bool IsParent;
            public int Code;
            public int LeftChildIndex;
            public int RightChildIndex;
        }

        static void CreateHuffmanTree(HuffmanNode[] hnodes, HuffmanCode[] hcode, int node_count)
        {
            int[,] nodes_index = new int[2, 512];
            int next_node_index = 1;
            int depth_nodes = 1;
            int depth = 0;
            int child_index = 0;
            nodes_index[0, 0] = 0;
            
            for (int n = 0; n < node_count; )
            {
                int huffman_nodes_index = child_index;
                child_index ^= 1;

                int depth_existed_nodes = 0;
                while (n < node_count && hcode[n].Depth == depth)
                {
                    hnodes[nodes_index[huffman_nodes_index, depth_existed_nodes]].IsParent = false;
                    hnodes[nodes_index[huffman_nodes_index, depth_existed_nodes]].Code = hcode[n].Code;
                    n++;
                    depth_existed_nodes++;
                }
                
                int depth_nodes_to_create = depth_nodes - depth_existed_nodes;
                for (int i = 0; i < depth_nodes_to_create; i++)
                {
                    int parent_idx = nodes_index[huffman_nodes_index, depth_existed_nodes + i];
                    hnodes[parent_idx].IsParent = true;
                    
                    hnodes[parent_idx].LeftChildIndex = next_node_index;
                    nodes_index[child_index, i * 2] = next_node_index++;
                    
                    hnodes[parent_idx].RightChildIndex = next_node_index;
                    nodes_index[child_index, i * 2 + 1] = next_node_index++;
                }
                depth++;
                depth_nodes = depth_nodes_to_create * 2;
            }
        }

        void HuffmanDecompress(HuffmanNode[] hnodes, uint dec_count)
        {
            int dst_ptr = 0;
            for (uint k = 0; k < dec_count; k++)
            {
                int node_index = 0;
                do
                {
                    int bit = GetNextBit();
                    if (-1 == bit) throw new EndOfStreamException();
                    if (0 == bit)
                        node_index = hnodes[node_index].LeftChildIndex;
                    else
                        node_index = hnodes[node_index].RightChildIndex;
                }
                while (hnodes[node_index].IsParent);

                int code = hnodes[node_index].Code;
                if (code >= 256)
                {
                    int offset = GetBits(12);
                    if (-1 == offset) break;
                    int count = (code & 0xff) + 2;
                    offset += 2;
                    
                    // CopyOverlapped
                    for (int i = 0; i < count; i++)
                    {
                        m_output[dst_ptr] = m_output[dst_ptr - offset];
                        dst_ptr++;
                    }
                }
                else
                {
                    m_output[dst_ptr++] = (byte)code;
                }
            }
        }
    }
}
