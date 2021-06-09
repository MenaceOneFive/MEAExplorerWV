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

        public class HuffNode
        {
            public HuffNode e0; //좌노드
            public HuffNode e1; //우노드
            public char c; //문자
            public long w; //가중치
            public int index;
            public bool hasIndex;
            public HuffNode(char chr, long weight)
            {
                e0 = e1 = null;
                c = chr;
                w = weight;
                hasIndex = false;
            }
        }

        public class AlphaEntry
        {
            public bool[] list;
            public char c;
        }

        public class StringID
        {
            public uint ID;
            public uint offset;
        }

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
            magic = Helpers.ReadUInt(stream);
            if (magic != 0xD78B40EB) //매직넘버 같음
            {
                Debug.Write($"매직넘버 오류! 매직넘버 : {magic}");
                return false;
            }

            Debug.WriteLine("매직넘버 정상 -> 해독 시작");
            unk01 = Helpers.ReadInt(stream);
            DataOffset = Helpers.ReadInt(stream);    //ok
            unk02 = Helpers.ReadUShort(stream);      //2바이트 이동
            unk03 = Helpers.ReadUShort(stream);      //2바이트 이동
            unk04 = Helpers.ReadInt(stream);         //4바이트 이동
            unk05 = Helpers.ReadInt(stream);         //4바이트 이동 -> 총 12바이트 이동
            NodeCount = Helpers.ReadInt(stream);     
            NodeOffset = Helpers.ReadInt(stream);   
            StringCount = Helpers.ReadInt(stream);   
            StringOffset = Helpers.ReadInt(stream); 
            Data3Count = Helpers.ReadInt(stream); 
            Data3Offset = Helpers.ReadInt(stream);
            Data4Count = Helpers.ReadInt(stream); 
            Data4Offset = Helpers.ReadInt(stream);
            
            Debug.WriteLine($"데이터 오프셋 : {DataOffset.ToString("X8")}");
            Debug.WriteLine($"2바이트 이동 : {unk02}");
            Debug.WriteLine($"2바이트 이동 : {unk03}");
            Debug.WriteLine($"4바이트 이동 : {unk04}");
            Debug.WriteLine($"4바이트 이동 : {unk05}");
            Debug.WriteLine($"노드 개수 : {NodeCount}, 노드 오프셋 : {NodeOffset.ToString("X8")}");
            Debug.WriteLine($"문자열 개수 : {StringCount}, 문자열 오프셋 : {StringOffset.ToString("X8")}");
            
            if (Data4Count > 0)
            {
                Data5Count = Helpers.ReadInt(stream);
                Data5Offset = Helpers.ReadInt(stream);
            }
            
            //MemoryStream.Seek Method
            //현재 스트림 내의 위치를 지정된 값으로 설정합니다
            //노드, 문자열 오프셋은 노드의 시작위치 문자열의 시작위치임
            //노드 = 허프만노드인듯
            stream.Seek(NodeOffset, SeekOrigin.Begin); 
            Huffman = new List<int>();
            
            //허프만 노드를 리스트에 담는다
            for (int i = 0; i < NodeCount; i++)
                Huffman.Add(Helpers.ReadInt(stream));
            
            //스트링의 ID와 데이터를 리스트에 담는다.
            stream.Seek(StringOffset, SeekOrigin.Begin);
            StringIDs = new List<uint>();
            StringData = new List<int>();
            for (int i = 0; i < StringCount; i++)
            {
                StringIDs.Add(Helpers.ReadUInt(stream));
                StringData.Add(Helpers.ReadInt(stream));
            }
            
            //문자열 읽어오기
            stream.Seek(DataOffset, SeekOrigin.Begin);
            Data = new List<uint>();
            while (stream.Position < stream.Length)
                Data.Add(Helpers.ReadUInt(stream));
            
            //TalkTableAsset의 멤버
            Strings = new List<STR>();
            bool firstOne = false;

            Debug.WriteLine($"허프만 노드 개수 : {Huffman.Count}");
            foreach (var Node in Huffman)
            {
                Debug.Write($"{(char)Node}");
            }
            Debug.WriteLine("");
            //STR(대사) 형태로 변환하는 작업
            for (int i = 0; i < StringIDs.Count; i++)
            {
                STR ValueString = new STR();
                ValueString.ID = StringIDs[i];
                ValueString.Value = "";
                int Index = StringData[i] >> 5;
                int Shift = StringData[i] & 0x1F; //왜 시프트를 하는지 모르겠다 시프트도 아닌 거 같은데 &이면 AND연산임 10진법으로 31

                if (!firstOne)
                {
                    //값 변경 조사용으로 3회 실시 + 시프트연산 A >> B = A / 2^B 를 의미함
                    Debug.WriteLine($"StringData[{i}] : {StringData[i]} Index : {Index} Shift : {Shift}");
                    if (i < 3)
                        firstOne = true;
                }

                StringBuilder sb = new StringBuilder();
                while (true)
                {
                    //허프만 노드가 자식노드가 2개라서?
                    int e = (Huffman.Count / 2) - 1;
                    
                    //허프만 노드가 1개 이상이라면
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

        /// <summary>
        /// 유용한 링크
        /// https://daeguowl.tistory.com/98 -> 아스키, 유니코드, UTF-8 정리
        /// </summary>
        /// <param name="s"></param>
        public void Save(Stream s)
        {
            long[] weights = new long[256 * 256];
            //weights.length = 65,536;
            //한글 완성형은 2350개
            //인덱스오류가능성은 없음
            foreach (STR line in Strings)
            {
                weights[0]++;
                foreach (char c in line.Value)
                {
                    //ASCII 기준으로 치환하는 듯
                    //놉 C#의 Char은 16비트 -> 2바이트임 즉 한글도 문제없음
                    //하지만 프로스트바이트 엔진이 뭘 쓰는지가 문제인데 C++이랑 C#둘 다 쓴다고 함
                    //정확히는 바이오웨어가 이 호프만 압축을 위해 작성한 코드의 언어가 중요할 듯
                    weights[(ushort)c]++;
                }

            }
            //계속 65,536사이즈 배열을 들고 갈 수 없으니 딕셔너리에 담기
            //(비어있는 공간이 매우 많음!)
            //int로 케스팅된 char값을 사용하여 해당 char의 가중치를 가져옴
            Dictionary<char, long> weighttable = new Dictionary<char, long>();
            for (int i = 0; i < 256 * 256; i++)
                if (weights[i] > 0)
                    weighttable.Add((char)i, weights[i]);

            //본격적으로 허프만 압축이 시작되는 부분
            //있는 문자들만 노드로 추가함
            List<HuffNode> nodes = new List<HuffNode>();
            foreach (KeyValuePair<char, long> w in weighttable)
                nodes.Add(new HuffNode(w.Key, w.Value));

            //개별 노드에 대한 좌, 우 할당
            while (nodes.Count > 1)
            {
                bool run = true;
                
                while (run)
                {
                    run = false;
                    for (int i = 0; i < nodes.Count - 1; i++)
                        //크기비교해서 순서바꾸기 오름차순으로 정렬
                        if (nodes[i].w > nodes[i + 1].w)
                        {
                            run = true;
                            HuffNode t = nodes[i];
                            nodes[i] = nodes[i + 1]; 
                            nodes[i + 1] = t;       
                        }
                }
                //허프만 알고리즘의 핵심 제일 작은 2개를 합해서 새로운 노드를 생성
                //그 노드를 기존 배열에 추가하고 이 과정을 반복
                //여기서 e0과 e1은 제일 작은 노드들임
                //두 노드 중에 작은 노드가 좌로 가고 큰 노드가 우로감 
                //새 합성노드생성 -> 기존의 노드를 하위노드로 추가 -> 기존의 노드를 제거하고 합성노드를 배열에 추가
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
            
            
            //<버그 의심존>
            AlphaEntry[] alphabet = GetAlphabet(root, new List<bool>());
            BitStream = new List<uint>();
            StringList = new List<StringID>();
            uint curr = 0;
            uint index = 0;
            byte shift = 0;
            //</버그 의심존>
            //노가다가 아~~~~주 많을 것으로 예정되는 파트
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
            Helpers.WriteInt(s, (int)magic);
            Helpers.WriteInt(s, (int)unk01);
            Helpers.WriteInt(s, 0x38 + NodeList.Count * 4 + StringList.Count * 8);
            Helpers.WriteUShort(s, unk02);
            //Helpers.WriteUShort(s, 4);
            Helpers.WriteUShort(s, unk03);
            Helpers.WriteInt(s, (int)unk04);
            Helpers.WriteInt(s, (int)unk05);
            Helpers.WriteInt(s, NodeList.Count);
            Helpers.WriteInt(s, 0x38);
            Helpers.WriteInt(s, StringList.Count);
            Helpers.WriteInt(s, 0x38 + NodeList.Count * 4);
            Helpers.WriteInt(s, 0);
            Helpers.WriteInt(s, 0x38 + NodeList.Count * 4 + StringList.Count * 8);
            Helpers.WriteInt(s, 0);
            Helpers.WriteInt(s, 0x38 + NodeList.Count * 4 + StringList.Count * 8);

            foreach (int i in NodeList)
                Helpers.WriteInt(s, i);
            foreach (StringID sid in StringList)
            {
                Helpers.WriteInt(s, (int)sid.ID);
                Helpers.WriteInt(s, (int)sid.offset);
            }
            foreach (int i in BitStream)
                Helpers.WriteInt(s, i);
            
        }
        
        public void CalcIndex(HuffNode h)
        {
            //재귀함수임
            
            //재귀함수 탈출조건
            if (h.e0 == null && h.e1 == null && !h.hasIndex)
            {
                int u;
                if (((ushort)h.c) >= 0x100)
                    u = (short)(0xFFFF - (ushort)h.c);
                else
                    u = (int)(0xFFFFFFFF - (uint)h.c);
                h.index = u;
                h.hasIndex = true;
            }
            else
            {
                //재귀호출지점
                CalcIndex(h.e0);
                CalcIndex(h.e1);
                //작업중단점
                if (h.e0.hasIndex && h.e1.hasIndex)
                {
                    h.index = NodeList.Count / 2;
                    h.hasIndex = true;
                    NodeList.Add(h.e0.index);
                    NodeList.Add(h.e1.index);
                }
            }
        }

        public AlphaEntry[] GetAlphabet(HuffNode h, List<bool> list)
        {
            List<AlphaEntry> result = new List<AlphaEntry>();
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
            else
            {
                List<bool> t = new List<bool>();
                t.AddRange(list);
                t.Add(false);
                result.AddRange(GetAlphabet(h.e0, t));
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
                result.AddRange(GetAlphabet(h.e1, t));
            }
            return result.ToArray();
        }
    }
}
