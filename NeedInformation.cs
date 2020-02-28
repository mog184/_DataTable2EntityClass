using System;
using System.Collections.Generic;
using System.Text;

namespace sql2entity
{
    class NeedInformation
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        /// 列名
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// 是否为空
        /// </summary>
        public string IsNullable { get; set; }

        /// <summary>
        /// 数据类型:varchar
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 主键
        /// </summary>
        public string ColumnKey { get; set; }

        /// <summary>
        /// 最大长度:150
        /// </summary>
        public string MaxLength { get; set; }

        /// <summary>
        /// 数据类型:varchar(150)
        /// </summary>
        public string ColumnType { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string ColumnComment { get; set; }

        public NeedInformation()
        {
        }

        public NeedInformation(string tableName, string columnName, string isNullable, string dataType, string columnKey, string maxLength, string columnType, string columnComment)
        {
            TableName = tableName;
            ColumnName = columnName;
            IsNullable = isNullable;
            DataType = dataType;
            ColumnKey = columnKey;
            MaxLength = maxLength;
            ColumnType = columnType;
            ColumnComment = columnComment;
        }
    }
}
