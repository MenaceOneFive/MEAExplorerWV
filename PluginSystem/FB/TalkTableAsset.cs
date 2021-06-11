using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace PluginSystem
{

    public class STR
    {
        public uint ID;
        public String Value;
    }

    public class TalkTableAsset
    {
        public List<STR> Strings;

        List<int> NodeList;
        List<StringID> StringList;
        List<uint> BitStream;

        #region Classes 

        public class HuffNode
        {
            public HuffNode e0;     //좌노드
            public HuffNode e1;     //우노드
            public char c;          //문자
            public long w;          //가중치
            public int index;       //CalcIndex함수에서 설정되는 멤버 변수( 합성된 노드 양수, 문자는 무조건 단말노드 음수)
            public bool hasIndex;   //CalcIndex함수에서 설정됨
            public HuffNode(char chr, long weight)
            {
                e0 = e1 = null;
                c = chr;
                w = weight;
                hasIndex = false;
            }
        }

        //각 문자에 대한 허프만트리에서의 경로를 담는 클래스
        //0왼쪽 1 오른쪽
        public class AlphaEntry
        {
            public bool[] list;     //01010110 왼쪽 오른쪽 왼쪽 오른쪽 왼쪽 오른쪽 오른쪽 오른쪽 왼쪽
            public char c;          //Save함수에서 각 문자를 c와 비교해서 같으면 list를 사용해서 해독함
        }

        public class StringID
        {
            public uint ID;
            public uint offset;
        }
        #endregion

        public uint magic;
        public int unk01;
        public int DataOffset;
        public ushort unk02;
        public ushort unk03;
        public int unk04;
        public int unk05;
        public int NodeCount;
        public int NodeOffset;
        public int StringCount;
        public int StringOffset;
        public int Data3Count;
        public int Data3Offset;
        public int Data4Count;
        public int Data4Offset;
        public int Data5Count;
        public int Data5Offset;
        public List<int> Huffman;
        public List<uint> StringIDs;
        public List<int> StringData;
        public List<uint> Data;


        //모든 문자가 영어일 것으로 예상하고 코드를 작성했음 -> ASCII
        //유니코드를 기준으로 재작성해야함

        public bool Read(MemoryStream stream)
        {

            #region  First16Byte
            //Frosty에서 출력된 처음 16바이트를 소거하는 파트
            byte[] tmpArray1;//입력 받은 원본 스트림을 저장하는 파트 
            byte[] tmpArray2; 
            tmpArray1 = stream.ToArray();
            tmpArray2 = new byte[tmpArray1.Length - 16];
            Array.Copy(tmpArray1, 16, tmpArray2, 0, tmpArray1.Length - 16);
            stream = new MemoryStream(tmpArray2);
            #endregion 
            
            magic = Helpers.ReadUInt(stream);
            if (magic != 0xD78B40EB) //매직넘버 같음
            {
                Debug.Write($"매직넘버 오류! 매직넘버 : {magic}");
                return false;
            }

            unk01 = Helpers.ReadInt(stream);
            DataOffset = Helpers.ReadInt(stream);    //ok
            unk02 = Helpers.ReadUShort(stream);      //2바이트 이동
            unk03 = Helpers.ReadUShort(stream);      //2바이트 이동
            unk04 = Helpers.ReadInt(stream);         //4바이트 이동
            unk05 = Helpers.ReadInt(stream);         //4바이트 이동 -> 총 12바이트 이동
            NodeCount = Helpers.ReadInt(stream);     //허프만 노드 카운트
            NodeOffset = Helpers.ReadInt(stream);    //허프만 노드 오프셋
            StringCount = Helpers.ReadInt(stream);   //스트링 카운트
            StringOffset = Helpers.ReadInt(stream);  //스트링 오프셋
            Data3Count = Helpers.ReadInt(stream); 
            Data3Offset = Helpers.ReadInt(stream);
            Data4Count = Helpers.ReadInt(stream); 
            Data4Offset = Helpers.ReadInt(stream);

            
            if (Data4Count > 0)
            {
                Data5Count = Helpers.ReadInt(stream);
                Data5Offset = Helpers.ReadInt(stream);
            }
            
            stream.Seek(NodeOffset, SeekOrigin.Begin); 
            Huffman = new List<int>();
            
            for (int i = 0; i < NodeCount; i++)
            {
                Huffman.Add(Helpers.ReadInt(stream));
            }
            

            stream.Seek(StringOffset, SeekOrigin.Begin);
            StringIDs = new List<uint>();
            StringData = new List<int>();
            for (int i = 0; i < StringCount; i++)
            {
                StringIDs.Add(Helpers.ReadUInt(stream));
                StringData.Add(Helpers.ReadInt(stream));
            }
            
            stream.Seek(DataOffset, SeekOrigin.Begin);
            Data = new List<uint>();
            while (stream.Position < stream.Length)
                Data.Add(Helpers.ReadUInt(stream));
            
            Strings = new List<STR>();


            
            for (int i = 0; i < StringIDs.Count; i++)
            {
                STR ValueString = new STR();
                ValueString.ID = StringIDs[i];
                ValueString.Value = "";
                int Index = StringData[i] >> 5;
                int Shift = StringData[i] & 0x1F; 

                StringBuilder sb = new StringBuilder();

                while (true)
                {
                    int e = (Huffman.Count / 2) - 1;
                    
                    while (e >= 0)
                    {
                        uint d = Data[Index];
                        int offset = (int)((d >> Shift) & 1);
                        e = Huffman[(e * 2) + offset];

                        Shift++;
                        Index += (Shift >> 5);
                        Shift %= 32;
                    }
                    ushort c;
                    if ((e & 0xFF00) == 0xFF)
                        c = (ushort)(0xFFFFFFFF - (uint)e);
                    else
                        c = (ushort)(0xFFFF - (ushort)e);
                    if (c == 0)
                        break;
                    else
                        sb.Append((char)c);

                }
                ValueString.Value = sb.ToString();
                Strings.Add(ValueString);
            }
            return true;
        }

        
        
        
        
        //파일에 대한 정보를 읽기 -> 허프만 노드 생성 -> 단말 노드의 경로 계산 -> 개개별의 문자를 경로로 바꿈
        public void Save(Stream s)
        {
            long[] weights = new long[256 * 256];
            
            foreach (STR line in Strings)
            {
                weights[0]++;
                foreach (char c in line.Value)
                {
                    weights[(ushort)c]++;
                }

            }
            Dictionary<char, long> weighttable = new Dictionary<char, long>();
            for (int i = 0; i < 256 * 256; i++)
                if (weights[i] > 0)
                {
                    weighttable.Add((char) i, weights[i]);
                }

            List<HuffNode> nodes = new List<HuffNode>();
            foreach (KeyValuePair<char, long> w in weighttable)
                nodes.Add(new HuffNode(w.Key, w.Value));

            while (nodes.Count > 1)
            {
                bool run = true;
                
                while (run)
                {
                    run = false;
                    for (int i = 0; i < nodes.Count - 1; i++)
                        if (nodes[i].w > nodes[i + 1].w)
                        {
                            run = true;
                            HuffNode t = nodes[i];
                            nodes[i] = nodes[i + 1]; 
                            nodes[i + 1] = t;       
                        }
                }
                
                HuffNode e0 = nodes[0];
                HuffNode e1 = nodes[1];
                HuffNode combine = new HuffNode(' ', e0.w + e1.w);
                combine.e0 = e0;
                combine.e1 = e1;
                nodes.RemoveAt(1);
                nodes.RemoveAt(0);
                nodes.Add(combine);
                
            }

            HuffNode root = nodes[0];
            NodeList = new List<int>();
            while (!root.hasIndex)
                CalcIndex(root);
            
            AlphaEntry[] alphabet = GetCharacter(root, new List<bool>());
            BitStream = new List<uint>();
            StringList = new List<StringID>();
            uint curr = 0;
            uint index = 0;
            byte shift = 0;
            
            foreach (STR str in Strings)
            {
                StringID t = new StringID();
                t.ID = str.ID;
                t.offset = index << 5;
                t.offset += shift;
                string line = str.Value + "\0";
                foreach (char c in line)
                {
                    AlphaEntry alpha = null;
                    foreach (AlphaEntry a in alphabet)
                        if (a.c == c)
                            alpha = a;
                    foreach (bool step in alpha.list)
                    {
                        byte b = 0;
                        if (step)
                            b = 1;
                        if (shift < 32)
                        {
                            curr += (uint)(b << shift);
                            shift++;
                        }
                        if (shift == 32)
                        {
                            BitStream.Add(curr);
                            index++;
                            shift = 0;
                            curr = 0;
                        }
                    }
                }
                StringList.Add(t);
            }
            BitStream.Add(curr);

            #region  SaveToMemoryStream
            
            var hex = 0x38;
            var Offset = hex + NodeList.Count * 4 + StringList.Count * 8;

            #region FileHeader 
            
            //내가 추가한 부분:
            //엔진은 첫 4바이트의 오프셋 값을 읽어야 이 파일을 사용할 수 있음 
            Helpers.WriteInt(s, Offset);
            Helpers.WriteInt(s, 0x0000);
            Helpers.WriteInt(s, 0x0000);
            Helpers.WriteInt(s, 0x0000);
            
            #endregion 
            
            Helpers.WriteInt(s, (int)magic);                         //매직넘버
            Helpers.WriteInt(s, (int)unk01);                         //UNK1
            Helpers.WriteInt(s, Offset);                             //오프셋
            Helpers.WriteUShort(s, unk02);                           //UNK2
            Helpers.WriteUShort(s, unk03);                           //UNK3
            Helpers.WriteInt(s, (int)unk04);                         //UNK4
            Helpers.WriteInt(s, (int)unk05);                         //UNK5
            Helpers.WriteInt(s, NodeList.Count);                     //허프만 노드 갯수
            Helpers.WriteInt(s, hex);                                //허프만 노드 오프셋
            Helpers.WriteInt(s, StringList.Count);                   //문자 리스트
            Helpers.WriteInt(s, hex + NodeList.Count * 4);         //문자 오프셋
            Helpers.WriteInt(s, 0);
            Helpers.WriteInt(s, Offset);
            Helpers.WriteInt(s, 0);
            Helpers.WriteInt(s, Offset);

            //허프만 노드 저장
            foreach (int i in NodeList)
            {
                Helpers.WriteInt(s, i);
            }

            //문자열 ID저장
            foreach (StringID sid in StringList)
            {
                Helpers.WriteInt(s, (int)sid.ID);
                Helpers.WriteInt(s, (int)sid.offset);
            }
            //문자열 저장
            foreach (int i in BitStream)
                Helpers.WriteInt(s, i);
            
            #endregion
            
        }
        
        public void CalcIndex(HuffNode h)
        {
            if (h.e0 == null && h.e1 == null && !h.hasIndex)
            {
                int u;
                if (((ushort)h.c) >= 0x100)
                    u = (int)((int)(h.c + 1) * -1); //정답!
                else
                {
                    u = (int) (0xFFFFFFFF - (uint) h.c);
                }

                h.index = u;
                h.hasIndex = true;
            }
            else
            {
                //재귀호출지점
                CalcIndex(h.e0);
                CalcIndex(h.e1);
                if (h.e0.hasIndex && h.e1.hasIndex)
                {
                    h.index = NodeList.Count / 2;
                    h.hasIndex = true;
                    NodeList.Add(h.e0.index);
                    NodeList.Add(h.e1.index);
                }
            }
        }

        public AlphaEntry[] GetCharacter(HuffNode h, List<bool> list)
        {
            List<AlphaEntry> result = new List<AlphaEntry>();
            //자식노드가 단말노드인 경우
            if (h.e0.e0 == null)
            {
                AlphaEntry e = new AlphaEntry();
                e.c = h.e0.c;
                List<bool> t = new List<bool>();
                t.AddRange(list);
                t.Add(false);
                e.list = t.ToArray();
                result.Add(e);
            }
            //자식노드가 단말노드가 아닌 경우
            else
            {
                List<bool> t = new List<bool>();
                t.AddRange(list);
                t.Add(false);
                result.AddRange(GetCharacter(h.e0, t));
            }
            
            if (h.e1.e0 == null)
            {
                AlphaEntry e = new AlphaEntry();
                e.c = h.e1.c;
                List<bool> t = new List<bool>();
                t.AddRange(list);
                t.Add(true);
                e.list = t.ToArray();
                result.Add(e);
            }
            else
            {
                List<bool> t = new List<bool>();
                t.AddRange(list);
                t.Add(true);
                result.AddRange(GetCharacter(h.e1, t));
            }
            
            return result.ToArray();
        }
    }
}
