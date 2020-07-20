using System;
using System.Linq;

namespace CodeKicker.BBCode.Core
{
    public class Bitfield
    {
        private string _data;

        public Bitfield(string bitfield = "")
        {
            _data = new string(Convert.FromBase64String(bitfield).Select(x => (char)x).ToArray());
        }

        public bool Exists(int n)
        {
            var @byte = n >> 3;

            if (_data.Length >= @byte + 1)
            {
                var c = _data[@byte];
                var bit = 7 - (n & 7);
                return (c & (1 << bit)) != 0;
            }
            else
            {
                return false;
            }
        }

        public void Set(int n)
        {
            var @byte = n >> 3;
            var bit = 7 - (n & 7);

            if (_data.Length >= @byte + 1)
            {
                var arr = _data.ToCharArray();
                arr[@byte] = (char)(_data[@byte] | (1 << bit));
                _data = new string(arr);
            }
            else
            {
                _data += new string('\0', @byte - _data.Length);
                _data += (char)(1 << bit);
            }
        }

        public void Clear(int n)
        {
            var @byte = n >> 3;

            if (_data.Length >= @byte + 1)
            {
                var bit = 7 - (n & 7);
                var arr = _data.ToCharArray();
                arr[@byte] = (char)(_data[@byte] & ~(1 << bit));
                _data = new string(arr);
            }
        }

        public string GetBlob()
        {
            return _data;
        }

        public string GetBase64()
        {
            return Convert.ToBase64String(_data.ToCharArray().Select(x => (byte)x).ToArray());
        }
    }
}
