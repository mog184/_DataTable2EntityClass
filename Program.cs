
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace sql2entity
{
    class Program
    {
        static void Main(string[] args)
        {
            //读取appsettings.json,获取数据库连接字符串
            var connection = Readjson("DefaultConnection");
            //从配置文件的连接数据中截取出数据库名称
            var databaseName = GetDatabaseName(connection);
            //连接数据库,查出所有表名并让用户键入需要生成实体类的表的序列号
            var tableNameList = ConnectionDB(connection, databaseName);
            //查询用户指定表的所有信息并生成实体类
            foreach (var tableName in tableNameList)
            {
                var list = new List<NeedInformation>();
                using (MySqlConnection conn = new MySqlConnection(connection))
                {
                    CreateOneTable2Entity(conn, tableName, databaseName, list);
                }
                //3.生成实体类
                CreateNewEntity(list, tableName);
            }
        }

        /// <summary>
        /// 从配置文件的连接数据中截取出数据库名称
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private static string GetDatabaseName(string connection)
        {
            //"Server=192.168.0.250;Port=13306;database=construction_site;uid=root;pwd=123456;SslMode=None;AllowUserVariables=True;"
            return connection.Split(";")[2].Split("=")[1];
        }

        /// <summary>
        /// 生成实体类
        /// </summary>
        private static void CreateNewEntity(List<NeedInformation> list, string tableName)
        {
            var className = FirstToUpper(tableName);
            StreamWriter strmsave = new StreamWriter($@"..\..\..\EntityClass\{className}.cs", false, System.Text.Encoding.Default);
            string str = System.IO.File.ReadAllText(@"..\..\..\Templete.txt");
            //模板替换
            var stringBuilder = GetTempleteContent(list);
            //替换命名空间&替换关联的表名&类名&属性等
            str = str.Replace("TempleteNamespace", "sql2entity")
                     .Replace("TableName", tableName)
                     .Replace("Templete", className)
                     .Replace("public string columnName { get; set; }", stringBuilder.ToString());
            strmsave.Write(str);
            Console.WriteLine("写入完成");
            strmsave.Close();
        }

        /// <summary>
        /// 拼接属性内容F
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static StringBuilder GetTempleteContent(List<NeedInformation> list)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                /// <summary>
                /// ID
                /// </summary>
                sb.Append("\t\t/// <summary>\n");
                sb.Append("\t\t/// " + list[i].ColumnComment);
                sb.Append("\n\t\t/// </summary>\n");
                //[Key]
                if (list[i].ColumnKey == "PRI")
                    sb.Append("\t\t[Key]\n");
                //[Required]
                if (list[i].IsNullable == "NO")
                    sb.Append("\t\t[Required]\n");
                //[StringLength(32)]
                if (list[i].MaxLength != "" && int.Parse(list[i].MaxLength) > 0)
                    sb.Append($"\t\t[StringLength({list[i].MaxLength})]\n");
                if (list[i].DataType == "bit")
                {
                    sb.Append("\t\t[Column((" + '"' + $"{list[i].ColumnName}" + '"' + "), TypeName = "+'"'+"bit(1)"+'"'+")]\n");
                    sb.Append("\t\tpublic bool " + FirstToUpper(list[i].ColumnName) + " { get; set; }\n\n");
                }
                else {
                    //[Column("id")]
                    sb.Append("\t\t[Column(" + '"' + $"{list[i].ColumnName}" + '"' + ")]\n");
                    //public string Id { get; set; }
                    sb.Append("\t\tpublic " + list[i].DataType + " " + FirstToUpper(list[i].ColumnName) + " { get; set; }\n\n");
                }
            }
            return sb;
        }

        /// <summary>
        /// 生成属性名格式,首字母大写并去掉_
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private static string FirstToUpper(string columnName)
        {
            string newName = null;
            string[] names = columnName.Split("_");
            foreach (var name in names)
            {
                newName += name.Substring(0, 1).ToUpper() + name.Substring(1);
            }
            return newName;
        }

        /// <summary>
        /// 读取sc_worker数据
        /// </summary>
        private static void ReadWorker(string tableName, MySqlDataReader reader, List<NeedInformation> list)
        {
            //reader.GetOrdinal("id")是得到ID所在列的index,  
            //reader.GetInt32(int n)这是将第n列的数据以Int32的格式返回  
            //reader.GetString(int n)这是将第n列的数据以string 格式返回 
            //key->列名,value->备注
            var columnName = reader.GetFieldValue<string>(3);
            var isNullable = reader.GetFieldValue<string>(6);
            var maxLength = reader.GetFieldValue<object>(8).ToString();
            string dataType = reader.GetFieldValue<string>(7).ToString();
            if (dataType == "varchar")
                dataType = "string";
            if (dataType == "date" || dataType == "datetime")
            {
                if (isNullable == "YES") 
                { 
                    dataType = "DateTime?"; 
                }
                else
                {
                    dataType = "DateTime";
                }
            }
            if (dataType == "bigint")
                dataType = "long";
            var columnKey = reader.GetFieldValue<string>(16);
            var columnType = reader.GetFieldValue<string>(15);
            var columnComment = reader.GetFieldValue<string>(19);
            list.Add(new NeedInformation(tableName, columnName, isNullable, dataType, columnKey, maxLength, columnType, columnComment));
        }

        /// <summary>
        /// 读取appsettings.json的JSON文件
        /// </summary>
        /// <param name="key">JSON文件中的key值</param>
        /// <returns>JSON文件中的value值</returns>
        public static string Readjson(string key)
        {


            string jsonfile = @"..\..\..\appsettings.json";//JSON文件路径

            using (System.IO.StreamReader file = System.IO.File.OpenText(jsonfile))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject o = (JObject)JToken.ReadFrom(reader);
                    var value = o[key].ToString();
                    return value;
                }
            }
        }

        /// <summary>
        /// 连接数据库
        /// </summary>
        private static List<string> ConnectionDB(string connection, string databaseName)
        {
            //新建一个数据库连接  
            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                conn.Open();//打开数据库  
                //创建数据库命令  
                MySqlCommand cmd = conn.CreateCommand();
                //查询出所有表名
                cmd.CommandText = $"select table_name from information_schema.tables where table_schema='{databaseName}';";
                //从数据库中读取数据流存入reader中  
                MySqlDataReader reader = cmd.ExecuteReader();
                int countNum = 1;
                //读取所有表名,并让用户键入表序号
                return ReadAllTableName(reader, countNum, new Dictionary<int, string>());
            }
        }

        /// <summary>
        /// 查询指定表的所有信息并生成实体类
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="reader"></param>
        /// <param name="list"></param>
        private static void CreateOneTable2Entity(MySqlConnection conn, string tableName, string databaseName, List<NeedInformation> list)
        {
            conn.Open();//打开数据库  
            MySqlCommand cmd = conn.CreateCommand();
            //创建查询语句  
            cmd.CommandText = $"select * from information_schema.columns where table_name = '{tableName}' and table_schema='{databaseName}'";
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                //读取表中数据
                ReadWorker(tableName, reader, list);
            }
        }

        /// <summary>
        /// 读取库中所有表名
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="countNum"></param>
        /// <param name="tableNames"></param>
        /// <returns></returns>
        private static List<string> ReadAllTableName(MySqlDataReader reader, int countNum, Dictionary<int, string> tableNames)
        {
            while (reader.Read())
            {
                string tableName = reader.GetFieldValue<string>(0);
                Console.WriteLine($"{countNum}.{tableName}");
                tableNames.Add(countNum++, tableName);
            }
            if (tableNames.Count > 0)
            {
                try
                {
                    return UserWrite(tableNames);
                }
                catch (Exception)
                {
                    Console.WriteLine("输入错误，请重新输入");
                    return UserWrite(tableNames);
                }
            }
            else
            {
                throw new Exception("该库中无表,请去配置文件重新配置数据库");
            }
        }

        /// <summary>
        /// 用户键入数字转为字符串返回
        /// </summary>
        /// <param name="tableNames"></param>
        /// <returns></returns>
        private static List<string> UserWrite(Dictionary<int, string> tableNames)
        {
            Console.WriteLine("请输入要生成实体类的表的序号,如果批量添加请用英文符号','隔开。例：1,2,3");
            string tableName = Console.ReadLine();
            var nums = GetTableNames(tableName);
            var tableNameList = new List<string>();
            foreach (var num in nums)
            {
                tableNameList.Add(tableNames[num]);
            }
            return tableNameList;
        }

        ///获取切割数字状态数组
        private static int[] GetTableNames(string tableName)
        {
            if (!string.IsNullOrEmpty(tableName.Replace(" ", "")))
                return tableName.Split(',').Select(x => { return Convert.ToInt32(x); }).ToArray();
            else
            {
                throw new Exception("请重新输入");
            }
        }
    }
}



