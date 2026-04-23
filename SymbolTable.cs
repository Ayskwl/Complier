using System.Collections.Generic;

namespace RGRCompilator
{
    public class SymbolInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }
    }

    public class SymbolTable
    {
        private readonly Dictionary<string, SymbolInfo> _symbols =
            new Dictionary<string, SymbolInfo>();

        public bool Declare(string name, string type, object value, int line, int position)
        {
            if (_symbols.ContainsKey(name))
                return false;

            _symbols[name] = new SymbolInfo
            {
                Name = name,
                Type = type,
                Value = value,
                Line = line,
                Position = position
            };

            return true;
        }

        public SymbolInfo Lookup(string name)
        {
            if (_symbols.TryGetValue(name, out SymbolInfo info))
                return info;

            return null;
        }

        public bool CheckDuplicate(string name)
        {
            return _symbols.ContainsKey(name);
        }

        public void Clear()
        {
            _symbols.Clear();
        }
        public bool Declare(string name, string type, object value)
        {
            if (_symbols.ContainsKey(name))
                return false;

            _symbols[name] = new SymbolInfo
            {
                Name = name,
                Type = type,
                Value = value
            };

            return true;
        }
    }
}