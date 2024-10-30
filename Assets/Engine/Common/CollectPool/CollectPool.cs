
using System.Collections.Generic;
using System;

namespace GameFramework.Common
{
    public class CollectPool<T> where T : class
    {
        private string _name;
        private Func<T> _createFunc;
        private Action<T> _deleteFunc;
        private Action<T> _freeFunc;
        private List<T> _freeList;
        private List<T> _itemList;

        public CollectPool(
            string name,
            Func<T> createFunc,
            Action<T> deleteFunc,
            Action<T> freeFunc,
            int reserveNum = 0
        )
        {
            _name = name;
            _createFunc = createFunc;
            _deleteFunc = deleteFunc;
            _freeFunc = freeFunc;

            _freeList = new List<T>();
            _itemList = new List<T>();

            if (reserveNum > 0)
            {
                Reserve(reserveNum);
            }
        }

        public void Destructor()
        {
            Clear();
        }

        public T Create()
        {
            if (_freeList.Count > 0)
            {
                var item = _freeList[^1];
                _freeList.RemoveAt(_freeList.Count - 1);
                return item;
            }
            else
            {
                var item = _createFunc();
                _itemList.Add(item);
                return item;
                ;
            }
        }

        public void Free(T item)
        {
            if (_freeList.Contains(item))
            {
                return;
            }

            _freeFunc(item);
            _freeList.Add(item);
        }

        public void Clear()
        {
            foreach (var item in _itemList)
            {
                _deleteFunc(item);
            }

            _itemList.Clear();
            _freeList.Clear();
        }

        public void ForeachItem(Action<T> func)
        {
            foreach (var item in _itemList)
            {
                func(item);
            }
        }

        public void Reserve(int num)
        {
            int createNum = num - _itemList.Count;
            for (int i = 0; i < createNum; i++)
            {
                var item = _createFunc();
                _itemList.Add(item);
                Free(item);
            }
        }

        public int GetItemNum()
        {
            return _itemList.Count;
        }

        public int GetFreeNum()
        {
            return _freeList.Count;
        }

        public int GetUsedNum()
        {
            return GetItemNum() - GetFreeNum();
        }

        public bool HasFreeNum()
        {
            return GetFreeNum() > 0;
        }

        public void ClearUnusedItems()
        {
            if (GetFreeNum() == 0)
            {
                return;
            }

            var freeSet = new HashSet<T>(_freeList);

            var newItemList = new List<T>();
            foreach (var item in _itemList)
            {
                if (freeSet.Contains(item))
                {
                    _deleteFunc(item);
                }
                else
                {
                    newItemList.Add(item);
                }
            }

            _itemList = newItemList;
            _freeList.Clear();
        }
    }
}
