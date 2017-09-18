using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Endogine.Serialization;
using System.Diagnostics;
using System.IO;

namespace PhotoshopFile
{
   public class PsdTextData:PsdObject
    {
        private IDictionary<string, object> properties;
        private int cachedByte = -1;
        private bool useCachedByte;
        public long offset;
        public List<long> fillColorOffsetList = new List<long>();

        public PsdTextData(BinaryReverseReader stream) {
            int size = stream.ReadInt32();
            byte[] array=new byte[size];
            offset = stream.BaseStream.Position;
            array = stream.ReadBytes(size);
            Stream str = new MemoryStream(array);
            BinaryReverseReader br = new BinaryReverseReader(str);
            properties = readMap(br);
        }

        /**
         * Gets the properties.
         *
         * @return the properties
         */
        public IDictionary<string, object> getProperties() {
                return properties;
        }

        private IDictionary<string, object> readFillColor(BinaryReverseReader stream)
        {
            skipWhitespaces(stream);
            char c = (char)readByte(stream);

            if (c == ']')
            {
                return null;
            }
            else if (c == '<')
            {
                skipString(stream, "<");
            }
            Dictionary<string, object> map = new Dictionary<string, object>();
            while (true)
            {
                skipWhitespaces(stream);
                c = (char)readByte(stream);
                if (c == '>')
                {
                    skipString(stream, ">");
                    return map;
                }
                else
                {
                    Debug.Assert(c == '/', "unknown char: " + c + ", byte: " + (sbyte)c);
                    string name = readName(stream);
                    skipWhitespaces(stream);
                    c = (char)lookForwardByte(stream);
                    if (c == '<')
                    {
                        throw new Exception();
                    }
                    else
                    {
                        if (name == "Values")
                        {
                            map[name] = readColor(stream);
                        }
                        else
                            map[name] = readValue(stream);
                    }
                }
            }
        }

        private IDictionary<string, object> readMap(BinaryReverseReader stream)
        {
            skipWhitespaces(stream);
            char c = (char)readByte(stream);

            if (c == ']')
            {
                return null;
            }
            else if (c == '<')
            {
                skipString(stream, "<");
            }
            Dictionary<string, object> map = new Dictionary<string, object>();
            while (true)
            {
                skipWhitespaces(stream);
                c = (char)readByte(stream);
                if (c == '>')
                {
                    skipString(stream, ">");
                    return map;
                }
                else
                {
                    Debug.Assert(c == '/', "unknown char: " + c + ", byte: " + (sbyte)c);
                    string name = readName(stream);
                    skipWhitespaces(stream);
                    c = (char)lookForwardByte(stream);
                    if (c == '<')
                    {
                        if (name == "FillColor")
                        {
                            map[name] = readFillColor(stream);
                        }
                        else
                        {
                            map[name] = readMap(stream);
                        }
                    }
                    else
                    {
                        map[name] = readValue(stream);
                    }
                }
            }
        }

        private String readName(BinaryReverseReader stream)
        {
                string name = "";
                while (true) {
                        char c = (char) readByte(stream);
                        if (c == ' ' || c == 10) {
                                break;
                        }
                        name += c;
                }
                return name;
        }

        private object readColor(BinaryReverseReader stream)
        {
            char c = (char)readByte(stream);
            if (c == ']')
            {
                return null;
            }
            else if (c == '(')
            {
                throw new Exception();
            }
            else if (c == '[')
            {
                List<object> list = new List<object>();
                // array
                c = (char)readByte(stream);
                while (true)
                {
                    skipWhitespaces(stream);
                    c = (char)lookForwardByte(stream);
                    if (c == '<')
                    {
                        object val = readMap(stream);
                        if (val == null)
                        {
                            return list;
                        }
                        else
                        {
                            list.Add(val);
                        }
                    }
                    else
                    {
                        object val = readColor(stream);
                        if (val == null)
                        {
                            return list;
                        }
                        else
                        {
                            list.Add(val);
                        }
                    }
                }
            }
            else
            {
                fillColorOffsetList.Add(stream.BaseStream.Position - 1);
                string val = "";
                do
                {
                    val += c;
                    c = (char)readByte(stream);
                } while (c != 10 && c != ' ');
                if (val.Equals("true") || val.Equals("false"))
                {
                    throw new Exception();
                }
                else
                {
                    return Convert.ToDouble(val);
                }
            }
        }

        private object readValue(BinaryReverseReader stream)
        {
            char c = (char)readByte(stream);
            if (c == ']')
            {
                return null;
            }
            else if (c == '(')
            {
                // unicode string
                string str = "";
                int stringSignature = readShort(stream) & 0xFFFF;
                Debug.Assert(stringSignature == 0xFEFF);
                while (true)
                {
                    byte b1 = readByte(stream);
                    if (b1 == ')')
                    {
                        return str;
                    }
                    byte b2 = readByte(stream);
                    if (b2 == '\\')
                    {
                        b2 = readByte(stream);
                    }
                    if (b2 == 13)
                    {
                        str += '\n';
                    }
                    else
                    {
                        str += (char)((b1 << 8) | b2);
                    }
                }
            }
            else if (c == '[')
            {
                List<object> list = new List<object>();
                // array
                c = (char)readByte(stream);
                while (true)
                {
                    skipWhitespaces(stream);
                    c = (char)lookForwardByte(stream);
                    if (c == '<')
                    {
                        object val = readMap(stream);
                        if (val == null)
                        {
                            return list;
                        }
                        else
                        {
                            list.Add(val);
                        }
                    }
                    else
                    {
                        object val = readValue(stream);
                        if (val == null)
                        {
                            return list;
                        }
                        else
                        {
                            list.Add(val);
                        }
                    }
                }
            }
            else
            {
                string val = "";
                do
                {
                    val += c;
                    c = (char)readByte(stream);
                } while (c != 10 && c != ' ');
                if (val.Equals("true") || val.Equals("false"))
                {
                    return Convert.ToBoolean(val);
                }
                else
                {
                    return Convert.ToDouble(val);
                }
            }
        }

        private void skipWhitespaces(BinaryReverseReader stream) {
                byte b;
                do {
                        b = readByte(stream);
                } while (b == ' ' || b == 10 || b == 9);
                putBack();
        }

        private void skipString(BinaryReverseReader stream, string str) {
                for (int i = 0; i < str.Length; i++) {
                        char streamCh = (char) readByte(stream);
                        Debug.Assert(streamCh == str[i], "char " + streamCh + " mustBe " + str[i]);
                }
        }

        
        public override string ToString() {
                return properties.ToString();
        }

        private byte readByte(BinaryReverseReader stream) {
           if (useCachedByte)
			{
				Debug.Assert(cachedByte != -1);
				useCachedByte = false;
				return (byte) cachedByte;
			}
			else
			{
				cachedByte = stream.ReadByte();
				return (byte) cachedByte;
			}
        }

        private short readShort(BinaryReverseReader stream) {
                cachedByte = -1;
                useCachedByte = false;
                return stream.ReadInt16();
        }

        private void putBack() {
            Debug.Assert(cachedByte != -1);
            Debug.Assert(!useCachedByte);
            useCachedByte = true;
        }

        private byte lookForwardByte(BinaryReverseReader stream)  {
                byte b = readByte(stream);
                putBack();
                return b;
        }
    }
}
