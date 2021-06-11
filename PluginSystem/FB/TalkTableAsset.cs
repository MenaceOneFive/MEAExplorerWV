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

            Debug.WriteLine("매직넘버 정상 -> 해독 시작");
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

            #region DebugLogs
            
            Debug.WriteLine($"데이터 오프셋 : {DataOffset.ToString("X8")}");
            Debug.WriteLine($"2바이트 이동 : {unk02}");
            Debug.WriteLine($"2바이트 이동 : {unk03}");
            Debug.WriteLine($"4바이트 이동 : {unk04}");
            Debug.WriteLine($"4바이트 이동 : {unk05}");
            Debug.WriteLine($"노드 개수 : {NodeCount}, 노드 오프셋 : {NodeOffset.ToString("X8")}");
            Debug.WriteLine($"문자열 개수 : {StringCount}, 문자열 오프셋 : {StringOffset.ToString("X8")}");
            
            #endregion
            
            if (Data4Count > 0)
            {
                Data5Count = Helpers.ReadInt(stream);
                Data5Offset = Helpers.ReadInt(stream);
            }
            
            //MemoryStream.Seek Method
            //현재 스트림 내의 위치를 지정된 값( (NodeOffset) + SeekOrigin.Begin )으로 설정합니다
            //노드, 문자열 오프셋은 노드의 시작위치 문자열의 시작위치임
            //노드 = 허프만노드인듯
            stream.Seek(NodeOffset, SeekOrigin.Begin); 
            Huffman = new List<int>();
            
            //허프만 노드를 리스트에 담는다
            for (int i = 0; i < NodeCount; i++)
            {
                Huffman.Add(Helpers.ReadInt(stream));
            }
            
            Debug.WriteLine("허프만 리스트");
            foreach (var item in Huffman)
            {
                Debug.Write($"({item} )\t");
            }

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


            
            //Q:) 해당 StringID에 매핑된 문자가 아무것도 없으면?
            //STR(대사) 형태로 변환하는 작업
            for (int i = 0; i < StringIDs.Count; i++)
            {
                STR ValueString = new STR();
                ValueString.ID = StringIDs[i];
                ValueString.Value = "";
                int Index = StringData[i] >> 5;
                int Shift = StringData[i] & 0x1F; //왜 시프트를 하는지 모르겠다 시프트도 아닌 거 같은데 &이면 AND연산임 10진법으로 31

                StringBuilder sb = new StringBuilder();

                int count2 = 0;
                while (true)
                {
                    //허프만 노드가 자식노드가 2개라서?
                    int e = (Huffman.Count / 2) - 1;
                    
                    //허프만 노드가 1개 이상이라면
                    while (e >= 0)
                    {
                        //offset == 시작위치
                        //Data배열의 INDEX번 위치에서 
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
                    // // Debug.WriteLine($"허프만e {e.ToString("x8")} 컨버트 문자:{ c.ToString("x8") } => {(char)c} \t");
                    // if (count2 % 5 == 0)
                    // {
                    //     count2 = 0;
                    //     Debug.WriteLine("");
                    // }

                    count2++;
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
        /// 
        public void Save(Stream s)
        {
            //weights.length = 65,536;
            //한글 완성형은 2350개
            //인덱스오류가능성은 없음
            long[] weights = new long[256 * 256];
            
            foreach (STR line in Strings)
            {
                weights[0]++;
                foreach (char c in line.Value)
                {
                    weights[(ushort)c]++;
                }

            }
            //계속 65,536사이즈 배열을 들고 갈 수 없으니 딕셔너리에 담기
            //(비어있는 공간이 매우 많음!)
            //int로 케스팅된 char값을 사용하여 해당 char의 가중치를 가져옴
            Dictionary<char, long> weighttable = new Dictionary<char, long>();
            for (int i = 0; i < 256 * 256; i++)
                if (weights[i] > 0)
                {
                    weighttable.Add((char) i, weights[i]);
                }

            //본격적으로 허프만 압축이 시작되는 부분 
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
                    //허프만 알고리즘 : 빈도를 비교해서 순서바꾸기 오름차순으로 정렬
                    for (int i = 0; i < nodes.Count - 1; i++)
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
            
            //각 문자의 경로를 계산 
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
                //대사의 각 문자를 테이블과 대조해서 암호화 하는 부분
                foreach (char c in line)
                {
                    //대사에 있는 각 C가 alpha에 있는 요소 a의 c와 동일하다면
                    //a를 사용해서 부호화 할 것!
                    AlphaEntry alpha = null;
                    foreach (AlphaEntry a in alphabet)
                        if (a.c == c)
                            alpha = a;
                    // 문자 C를 허프만 트리에 있는 노드의 위치로 치환하는 부분
                    // 의심존 2
                    foreach (bool step in alpha.list)
                    {
                        //오른쪽이면
                        byte b = 0;
                        //왼쪽이면
                        if (step)
                            b = 1;
                        if (shift < 32)
                        {
                            //
                            curr += (uint)(b << shift);
                            //현재 위치
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
            //내가 추가한 부분:
            //엔진은 첫 4바이트의 오프셋 값을 읽어야 이 파일을 사용할 수 있음 
            Helpers.WriteInt(s, Offset);
            Helpers.WriteInt(s, 0x0000);
            Helpers.WriteInt(s, 0x0000);
            Helpers.WriteInt(s, 0x0000);
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

            Debug.WriteLine($"허프만 노드 수{NodeList.Count}");
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
                    u = (int)((int)(h.c + 1) * -1); //-> 한국어 제한적 출력 성공 1차 외계어 2차 한국어
                    //u = (int)((uint)h.c - 0xFF00);
                    //u = (int)((uint)h.c - 0XFFF0); -> 1차 한국어 2차 외계어
                    //u = (int)((uint)h.c - 0X0FFF);튕김
                    //u = (int)((int)h.c - 0XFFFF); //-> 한국어 제한적 출력 성공 1차 외계어 2차 한국어
                else
                {
                    u = (int) (0xFFFFFFFF - (uint) h.c);
                }

                h.index = u;
                h.hasIndex = true;
                Debug.Write((char)h.c);
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
