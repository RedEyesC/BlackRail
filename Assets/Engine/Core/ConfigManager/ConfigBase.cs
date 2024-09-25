using GameFramework.Asset;
using GameFramework.Common;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Config
{
    public class ConfigBase
    {
        private string _name;

        private ByteBuffer _buffer;
        private static readonly int firstOffest = 6;

        //TODO _cache数据的使用和保存涉及到装箱拆箱，后续可以考虑优化一下
        private Dictionary<int, object> _cache = new Dictionary<int, object>();

        public ConfigBase(string bundleName, string assetName)
        {
            _name = assetName + ".bin";

            TextAsset _text = AssetManager.GetAssetObjWithType<TextAsset>(bundleName, assetName, true);
            _buffer = new ByteBuffer(_text.bytes);
        }

        public int Get(params object[] keys)
        {
            return GetByItem(firstOffest, keys);
        }

        public T GetValue<T>(params object[] keys)
        {
            return GetValueByItem<T>(firstOffest, keys);
        }

        public int GetByItem(int offset, params object[] keys)
        {

            int currentOffset = offset;

            for (int i = 0; i < keys.Length; i++)
            {
                Dictionary<object, int> value = GetData<Dictionary<object, int>>(currentOffset);
                object key = keys[i];
                if (value.ContainsKey(key))
                {
                    currentOffset = value[key];
                }
                else
                {
                    return -1;
                }
            }

            return currentOffset;
        }

        public T GetValueByItem<T>(int item, params object[] keys)
        {
            int offset = GetByItem(item, keys);
            return GetData<T>(offset);
        }

        public T GetData<T>(int offset)
        {
            object item;
            if (_cache.ContainsKey(offset))
            {
                item = _cache[offset];
            }
            else
            {
                item = GetBin<T>(offset);
                _cache[offset] = item;
            }

            return (T)item;
        }





        public T GetBin<T>(int offset)
        {
            int type = _buffer.ReadByteByPos(offset);

            offset++;

            switch (type)
            {
                //字典
                case 1:
                    {
                        ushort dicLen = _buffer.ReadUshort();
                        int dictKeyOffset = _buffer.ReadInt();

                        offset += 6;

                        object[] keys = GetBin<object[]>(dictKeyOffset);

                        Dictionary<object, int> dic = new Dictionary<object, int>();

                        _buffer.position = offset;

                        for (int i = 0; i < keys.Length; i++)
                        {
                            dic[keys[i]] = _buffer.ReadInt();
                        }

                        return (T)(object)dic;
                    }
                //列表
                case 2:
                    {
                        ushort listLen = _buffer.ReadUshort();

                        offset += 2;

                        object[] list = new object[listLen];

                        for (int i = 0; i < listLen; i++)
                        {
                            int keyOffset = _buffer.ReadIntByPos(offset);
                            list[i] = GetBin<object>(keyOffset);
                            offset += 4;
                        }

                        return (T)(object)list;

                    }
                //int32
                case 3:
                    {
                        int num = _buffer.ReadInt();

                        return (T)(object)num;
                    }
                //string
                case 4:
                    {
                        string str = _buffer.ReadString();

                        return (T)(object)str;
                    }
                default:
                    return default(T);

            }



        }

    }
}
