using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableHistorySchemaGenerator.Core
{
    public static class CommonSqlTypeExtensions
    {
        private static Dictionary<CommonHistorySqlType, string> _maxSizes = new Dictionary<CommonHistorySqlType, string>()
        {
            {CommonHistorySqlType.Binary, "(MAX)" },
            {CommonHistorySqlType.Char, "(MAX)" },
            {CommonHistorySqlType.DateTime2, "(7)" },
            {CommonHistorySqlType.DateTimeOffset, "(7)" },
            {CommonHistorySqlType.Decimal, "(38, 38)" },
            {CommonHistorySqlType.NChar, "(MAX)" },
            {CommonHistorySqlType.Numeric, "(38, 38)" },
            {CommonHistorySqlType.NVarChar, "(MAX)" },
            {CommonHistorySqlType.Time, "(7)" },
            {CommonHistorySqlType.VarBinary, "(MAX)" },
            {CommonHistorySqlType.VarChar, "(MAX)" }
        };
        public static string MaxSize(this CommonHistorySqlType type)
        {
            string size = null;
            _maxSizes.TryGetValue(type, out size);
            return size;
        }

        public static string ScriptName(this CommonHistorySqlType type)
        {
            return type.ToString();
        }
    }
    public enum CommonHistorySqlType
    {
        BigInt = 1,
        Int = 2,
        SmallInt = 3,
        TinyInt = 4,
        Bit = 5,
        Decimal = 6,
        Numeric = 7,
        Money = 8,
        SmallMoney = 9,
        Float = 10,
        Real = 11,
        DateTime = 12,
        SmallDateTime = 13,
        Char = 14,
        VarChar = 15,
        Text = 16,
        NChar = 17,
        NVarChar = 18,
        NText = 19,
        Binary = 20,
        VarBinary = 21,
        Image = 22,
        Timestamp = 26,
        Xml = 28,
        Date = 29,
        Time = 30,
        DateTime2 = 31,
        DateTimeOffset = 32,
        Rowversion = 33
    }
}
