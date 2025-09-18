using System.Text;

namespace MotionMatching
{
    public partial struct Binary
    {
        internal int NumStrings
        {
            get { return stringTable.Length; }
        }

        internal struct String
        {
            public int offset;
            public int count;
        }

        public string GetString(int index)
        {
            unsafe
            {
                byte* ptr = (byte*)stringBuffer.GetUnsafePtr();
                int offset = stringTable[index].offset;
                int count = stringTable[index].count;
                return Encoding.UTF8.GetString(ptr + offset, count);
            }
        }

        internal int GetStringIndex(string value)
        {
            int numStrings = NumStrings;

            for (int i = 0; i < numStrings; ++i)
            {
                if (string.Equals(value, GetString(i)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
