using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main(string[] args)
    {

        Console.WriteLine(C_debug_Factory.C_Insert_FactoryPoperty(new C_debug_Entity
        {
            P_val_Property = 242,
        }));//109
        Console.Read();
        return;

        Console.WriteLine(C_LinkMutiply_Factory.C_Insert_FactoryPoperty(new C_LinkMutiply_Entity
        {
            P_key1_Property = 157,
            P_key2_Property = 169,
            P_key3_Property = 124,
            P_status_Property = true,
        }));
        Console.Read();
        return;

        var st = new StreamWriter(new FileStream("D:\\Documents\\Visual Studio 2013\\Projects\\ConsoleApplication1\\ConsoleApplication1\\debug.cs", FileMode.Truncate, FileAccess.Write), Encoding.UTF8);
        StringBuilder sb = new StringBuilder();
        using (DB db = new DB())
        {
            db.Render(sb, new RenderConfig());
        }
        st.Write(sb.ToString());
        st.Close();
        //Console.Read();
    }
}

public class DB : IDisposable
{
    private IDbConnection _connection;

    protected IDbConnection GetConnection()
    {
        if (_connection == null)
            _connection = new SqlConnection("Server=.;Database=Debug;User Id=sa;Password=123456;");
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
        return _connection;
    }

    protected IDbCommand GetCommand(string SQL)
    {
        IDbCommand cmd = GetConnection().CreateCommand();
        cmd.CommandText = SQL;
        return cmd;
    }

    ~DB()
    {
        if (_connection != null)
            _connection.Close();
    }

    public void Dispose()
    {
        if (_connection != null)
            _connection.Close();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<Table> GetTables()
    {
        List<Table> tables = new List<Table>();

        IDataReader dataReader = GetCommand("select " +
            "sys.tables.name" +
            ",sys.tables.object_id" +
            ",sys.extended_properties.value summary " +
        " from sys.tables left join sys.extended_properties on sys.tables.object_id = sys.extended_properties.major_id and sys.extended_properties.minor_id = 0;").ExecuteReader();

        while (dataReader.Read())
        {
            Table table = new Table();

            table.Name = dataReader.GetString(0);
            table.Id = dataReader.GetInt32(1);
            if (!dataReader.IsDBNull(2))
                table.Summary = dataReader.GetString(2);
            table.Columns = new List<Column>();
            tables.Add(table);
        }
        dataReader.Close();

        foreach (Table table in tables)
        {
            dataReader = GetCommand(string.Format("select " +
                                                  "sys.columns.name" +
                                                  ",sys.extended_properties.value" +
                                                  ",sys.systypes.name" +
                                                  ",sys.columns.is_nullable" +
                                                  ",sys.columns.is_identity" +
                                                  ",sys.columns.max_length" +
                                                  " from " +
                                                  "sys.columns left join sys.extended_properties on columns.object_id = sys.extended_properties.major_id and sys.columns.column_id = sys.extended_properties.minor_id" +
                                                  " inner join sys.systypes on sys.columns.system_type_id = sys.systypes.xusertype where sys.columns.object_id = ({0})",
                table.Id))
                .ExecuteReader();
            table.Columns = new List<Column>();
            while (dataReader.Read())
            {
                Column column = new Column();
                column.Name = dataReader.GetString(0);
                if (!dataReader.IsDBNull(1))
                    column.Summary = dataReader.GetString(1);
                column.Type = dataReader.GetString(2);
                column.IsNull = dataReader.GetBoolean(3);
                column.IsIdentity = dataReader.GetBoolean(4);
                column.Length = dataReader.GetInt16(5);
                table.Columns.Add(column);
            }
            dataReader.Close();
        }
        return tables;
    }

    public void Render(StringBuilder sb, RenderConfig config)
    {
        string tabs = new string('\t', config.TabCount);
        sb.Append(tabs).Append("using System;\r\n");
        sb.Append(tabs).Append("using System.Data;\r\n");
        sb.Append(tabs).Append("using System.Data.SqlClient;\r\n");
        sb.Append(tabs).Append("using System.Collections.Generic;\r\n");

        new RenderBase().RenderFactoryBase(sb, config);

        RenderEntity(sb, config, GetTables());
        RenderFactory(sb, config, GetTables());
    }

    public void RenderEntity(StringBuilder sb, RenderConfig config, List<Table> tables)
    {
        foreach (Table table in tables)
        {
            table.RenderEntity(sb, config);
        }
    }

    public void RenderFactory(StringBuilder sb, RenderConfig config, List<Table> tables)
    {
        foreach (Table table in tables)
        {
            table.RenderFactory(sb, config);
        }
    }
}

public class RenderConfig
{
    public RenderConfig()
    {
#if DEBUG

        NameSpace = "CodeGenerator";
        TabCount = 1;
        FormatEntity = "C_{0}_Entity";
        FormatEntityProperty = "P_{0}_Property";
        FormatFactory = "C_{0}_Factory";
        FormatFactoryPropery = "C_{0}_FactoryPoperty";
        GetCommandMethod = "BaseConnection.GetCommand(\"{0}\")";
        ConnectionString = "Server=.;Database=Debug;User Id=sa;Password=123456;";
#else
        NameSpace = "CodeGenerator";
        TabCount = 0;
        FormatEntity = "{0}";
        FormatEntityProperty = "{0}";
        FormatFactory = "{0}";
        FormatFactoryPropery = "{0}";
        GetCommandMethod = "BaseConnection.GetCommand()";
        ConnectionString = "Server=.;Database=Debug;User Id=sa;Password=123456;";

#endif
    }

    public string NameSpace { get; set; }

    public int TabCount { get; set; }

    public string FormatEntity { get; set; }

    public string FormatEntityProperty { get; set; }

    public string FormatFactory { get; set; }

    public string FormatFactoryPropery { get; set; }

    public string ConnectionString { get; set; }

    public string GetCommandMethod { get; set; }
}

public class RenderBase
{

    protected const string TemplateComment = "{0}/// <summary>\r\n{0}/// {1}\r\n{0}/// </summary>\r\n";

    public virtual void RenderEntity(StringBuilder sb, RenderConfig config)
    {

    }

    public virtual void RenderFactory(StringBuilder sb, RenderConfig config)
    {

    }

    public void RenderFactoryBase(StringBuilder sb, RenderConfig config)
    {
        string tabs = new string('\t', config.TabCount + 1);
        sb.AppendFormat("{0}public class BaseConnection\r\n", new String('\t', config.TabCount));
        sb.Append(new String('\t', config.TabCount)).Append("{\r\n");
        sb.AppendFormat("{0}private static SqlConnection _connection;\r\n\r\n", tabs);
        sb.AppendFormat("{0}public static SqlConnection GetConnection()\r\n", tabs);
        sb.Append(tabs).Append("{\r\n");
        sb.Append(tabs).Append("\tif (_connection == null)\r\n");
        sb.Append(tabs).AppendFormat("\t\t_connection = new SqlConnection(\"{0}\");\r\n", config.ConnectionString);
        sb.Append(tabs).Append("\tif (_connection.State != ConnectionState.Open)\r\n");
        sb.Append(tabs).Append("\t\t_connection.Open();\r\n");
        sb.Append(tabs).Append("\treturn _connection;\r\n");
        sb.Append(tabs).Append("}\r\n\r\n");
        sb.Append(tabs).Append("public static SqlCommand GetCommand()\r\n");
        sb.Append(tabs).Append("{\r\n");
        sb.Append(tabs).Append("\t").Append("return new SqlCommand(\"\",GetConnection());\r\n");
        sb.Append(tabs).Append("}\r\n");
        sb.Append(new String('\t', config.TabCount)).Append("}\r\n");
    }
}

public class Column : RenderBase
{
    public bool IsIdentity { get; set; }

    public bool IsPrimaryKey { get; set; }

    public bool IsNull { get; set; }

    public string Name { get; set; }

    public string Summary { get; set; }

    public string Type { get; set; }

    public int Index { get; set; }

    public int Length { get; set; }


    private string GetNullType(string type)
    {
        return string.Format("{0}{1}", type, IsNull ? "?" : "");
    }

    /*
geometry
geography
xml
sysname
hierarchyid
sql_variant
     */

    public string GetColumnType()
    {
        if (Type == "tinyint")
            return string.Format("byte{0}", IsNull ? "?" : "");
        if (Type == "bit")
            return string.Format("bool{0}", IsNull ? "?" : "");
        if (Type == "smallint")
            return string.Format("short{0}", IsNull ? "?" : "");
        if (Type == "date" || Type == "smalldatetime" || Type == "time" || Type == "datetime2" || Type == "datetime")
            return string.Format("DateTime{0}", IsNull ? "?" : "");
        if (Type == "int")
            return string.Format("int{0}", IsNull ? "?" : "");
        if (Type == "smallmoney" || Type == "money")
            return string.Format("decimal{0}", IsNull ? "?" : "");
        if (Type == "real")
            return string.Format("real{0}", IsNull ? "?" : "");
        if (Type == "float")
            return string.Format("float{0}", IsNull ? "?" : "");
        if (Type == "bigint")
            return string.Format("long{0}", IsNull ? "?" : "");
        if (Type == "timestamp")
            return string.Format("TimeSpan{0}", IsNull ? "?" : "");
        if (Type == "datetimeoffset")
            return string.Format("DateTimeOffset{0}", IsNull ? "?" : "");
        if (Type == "image" || Type == "varbinary" || Type == "binary")
            return "byte[]";
        if (Type == "text" || Type == "ntext" || Type == "varchar" || Type == "char" || Type == "nvarchar" ||
            Type == "nchar")
            return "string";
        if (Type == "uniqueidentifier")
            return string.Format("Guid{0}", IsNull ? "?" : "");
        if (Type == "decimal" || Type == "numeric")
            return string.Format("decimal{0}", IsNull ? "?" : "");
        return string.Format("object");
    }

    public string GetColumnReadMethod()
    {
        if (Type == "tinyint")
            return string.Format("byte{0}", IsNull ? "?" : "");
        if (Type == "bit")
            return string.Format("bool{0}", IsNull ? "?" : "");
        if (Type == "smallint")
            return string.Format("short{0}", IsNull ? "?" : "");
        if (Type == "date" || Type == "smalldatetime" || Type == "time" || Type == "datetime2" || Type == "datetime")
            return string.Format("DateTime{0}", IsNull ? "?" : "");
        if (Type == "int")
            return string.Format("int{0}", IsNull ? "?" : "");
        if (Type == "smallmoney" || Type == "money")
            return string.Format("decimal{0}", IsNull ? "?" : "");
        if (Type == "real")
            return string.Format("real{0}", IsNull ? "?" : "");
        if (Type == "float")
            return string.Format("float{0}", IsNull ? "?" : "");
        if (Type == "bigint")
            return string.Format("long{0}", IsNull ? "?" : "");
        if (Type == "timestamp")
            return string.Format("TimeSpan{0}", IsNull ? "?" : "");
        if (Type == "datetimeoffset")
            return string.Format("DateTimeOffset{0}", IsNull ? "?" : "");
        if (Type == "image" || Type == "varbinary" || Type == "binary")
            return "byte[]";
        if (Type == "text" || Type == "ntext" || Type == "varchar" || Type == "char" || Type == "nvarchar" ||
            Type == "nchar")
            return "string";
        if (Type == "uniqueidentifier")
            return string.Format("Guid{0}", IsNull ? "?" : "");
        if (Type == "decimal" || Type == "numeric")
            return string.Format("decimal{0}", IsNull ? "?" : "");
        return string.Format("object");
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("\t{\r\n").AppendFormat("{0}Name:{1},\r\n{0}Summary:{2},\r\n{0}Type:{3},\r\n{0}IsNull:{4},\r\n{0}IsIdentity:{5},\r\n{0}IsPrimaryKey:{6}", "\t\t", Name, Summary, Type, IsNull, IsIdentity, IsPrimaryKey);
        sb.Append("\r\n\t}");
        return sb.ToString();
    }
    public override void RenderEntity(StringBuilder sb, RenderConfig config)
    {
        string tabs = new string('\t', config.TabCount);

        sb.AppendFormat(TemplateComment, tabs, Summary);
        sb.AppendFormat("{0}public {1} {2}", tabs, GetColumnType(), string.Format(config.FormatEntityProperty, Name));
        sb.Append(" ").Append("{ get; set; }\r\n");

        sb.Append("\r\n");
    }
}

public class Table : RenderBase
{
    /// <summary>
    /// 表编号
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 表面
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Summary { get; set; }

    /// <summary>
    /// 列集合
    /// </summary>
    public List<Column> Columns { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{\r\n").AppendFormat("{0}Id:{1},\r\n{0}Name:{2},\r\n{0}Summary:{3},\r\n{0}Columns:\r\n", "\t", Id, Name, Summary);
        foreach (var item in Columns)
        {
            sb.AppendLine(item.ToString());
        }
        sb.Append("\r\n}");

        sb.Append("\r\n");

        return sb.ToString();
    }

    public override void RenderEntity(StringBuilder sb, RenderConfig config)
    {
        string tabs = new string('\t', config.TabCount);

        sb.AppendFormat("{0}/// <summary>\r\n", tabs);
        sb.AppendFormat("{0}/// {1}\r\n", tabs, Summary);
        sb.AppendFormat("{0}/// </summary>\r\n", tabs);
        sb.AppendFormat("{0}public partial class {1}\r\n", tabs, string.Format(config.FormatEntity, Name));
        sb.Append(tabs).Append("{\r\n ");

        config.TabCount++;
        foreach (Column item in Columns)
        {
            item.RenderEntity(sb, config);
        }
        config.TabCount--;

        sb.Append(tabs).Append(" }\r\n");

        sb.Append("\r\n");
    }

    public override void RenderFactory(StringBuilder sb, RenderConfig config)
    {

        string tabs = new string('\t', config.TabCount);

        sb.AppendFormat(TemplateComment, tabs, Summary);
        sb.AppendFormat("{0}public static partial class {1}\r\n", tabs, string.Format(config.FormatFactory, Name));
        sb.Append(tabs).Append("{\r\n ");

        config.TabCount++;
        RenderFactoryMethod(sb, config);
        config.TabCount--;
        sb.Append("\r\n").Append(tabs).Append("}\r\n");

        sb.Append("\r\n");
    }

    private void RenderFactoryMethod(StringBuilder sb, RenderConfig config)
    {
        string tabs = new string('\t', config.TabCount);
        string tabsItem = new string('\t', config.TabCount + 1);


        List<Column> columnsIdentity = new List<Column>();
        List<Column> columnsNotIdentity = new List<Column>();
        List<Column> columnsPrimary = new List<Column>();
        foreach (Column item in Columns)
        {
            if (item.IsIdentity)
                columnsIdentity.Add(item);
            else
                columnsNotIdentity.Add(item);
            if (item.IsPrimaryKey)
                columnsPrimary.Add(item);
        }


        #region Insert

        sb.AppendFormat(TemplateComment, tabs, "添加");
        sb.AppendFormat("{0}public static {1} {2}({3} model)\r\n",
            tabs,
            columnsIdentity.Count == 1 ? "decimal" : "int",
            string.Format(config.FormatFactoryPropery, "Insert"),
            string.Format(config.FormatEntity, Name));
        sb.Append(tabs).Append("{\r\n");

        sb.Append(tabsItem).Append("SqlCommand cmd = BaseConnection.GetCommand();\r\n");
        sb.Append(tabsItem).Append("List<string> sqlColumnName = new List<string>();\r\n");
        sb.Append(tabsItem).Append("List<string> sqlColumnValue = new List<string>();\r\n");

        bool first = true;

        foreach (Column item in columnsNotIdentity)
        {
            if (item.IsNull)
            {
                sb.Append(tabsItem).AppendFormat("if(model.{0} != null)\r\n",string.Format(config.FormatEntityProperty, item.Name));
                sb.Append(tabsItem).Append("{\r\n");
                sb.Append(tabsItem).Append("\t").AppendFormat("sqlColumnName.Add(\"{0}\");\r\n",  item.Name);
                sb.Append(tabsItem).Append("\t").AppendFormat("sqlColumnValue.Add(\"@{0}\");\r\n", item.Name);
                sb.Append(tabsItem).Append("\t").AppendFormat("cmd.Parameters.Add(\"{0}\",model.{1});\r\n", item.Name,string.Format(config.FormatEntityProperty,item.Name));
                sb.Append(tabsItem).Append("}\r\n");
            }
            else
            {
                sb.Append(tabsItem).AppendFormat("sqlColumnName.Add(\"{0}\");\r\n", item.Name);
                sb.Append(tabsItem).AppendFormat("sqlColumnValue.Add(\"@{0}\");\r\n", item.Name);
                sb.Append(tabsItem).Append("\t").AppendFormat("cmd.Parameters.Add(\"{0}\",model.{1});\r\n", item.Name,string.Format(config.FormatEntityProperty,item.Name));
            }
        }

        sb.Append(tabs)
            .Append(" cmd.CommandText = string.Format(\"insert into [" + Name +
                    "] ({0})values({1});" + (columnsIdentity.Count == 1 ? "select @@identity;" : "") +
                    "\", String.Join(\",\",sqlColumnName),String.Join(\",\",sqlColumnValue));\r\n");
        
        if (columnsIdentity.Count == 1)
        {
            sb.Append(tabsItem).Append("return (decimal)cmd.ExecuteScalar();\r\n");
        }
        else
        {
            sb.Append(tabsItem).Append("return cmd.ExecuteNonQuery();\r\n");
        }
        
        sb.Append(tabs).Append("}\r\n");

        #endregion


        #region update

        sb.AppendFormat(TemplateComment, tabs, "修改");
        sb.AppendFormat("{0}public static {1} {2}({3} model)\r\n",
            tabs,
            columnsIdentity.Count == 1 ? "decimal" : "int",
            string.Format(config.FormatFactoryPropery, "Insert"),
            string.Format(config.FormatEntity, Name));
        sb.Append(tabs).Append("{\r\n");

        sb.Append(tabs).Append("}\r\n");



        #endregion

    }
}
