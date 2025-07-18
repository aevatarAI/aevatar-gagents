using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;

namespace Aevatar.GAgents.TypeTestAgent.Dtos;

[GenerateSerializer]
public class TypeTestConfigDto : ConfigurationBase
{
    // =================
    // 基本值类型 (Basic Value Types)
    // =================
    
    [Id(0)] public bool BoolField { get; set; }
    [Id(1)] public byte ByteField { get; set; }
    [Id(2)] public sbyte SByteField { get; set; }
    [Id(3)] public char CharField { get; set; }
    [Id(4)] public short ShortField { get; set; }
    [Id(5)] public ushort UShortField { get; set; }
    [Id(6)] public int IntField { get; set; }
    [Id(7)] public uint UIntField { get; set; }
    [Id(8)] public long LongField { get; set; }
    [Id(9)] public ulong ULongField { get; set; }
    [Id(10)] public float FloatField { get; set; }
    [Id(11)] public double DoubleField { get; set; }
    [Id(12)] public decimal DecimalField { get; set; }

    // =================
    // 可空值类型 (Nullable Value Types)
    // =================
    
    [Id(13)] public bool? NullableBoolField { get; set; }
    [Id(14)] public byte? NullableByteField { get; set; }
    [Id(15)] public sbyte? NullableSByteField { get; set; }
    [Id(16)] public char? NullableCharField { get; set; }
    [Id(17)] public short? NullableShortField { get; set; }
    [Id(18)] public ushort? NullableUShortField { get; set; }
    [Id(19)] public int? NullableIntField { get; set; }
    [Id(20)] public uint? NullableUIntField { get; set; }
    [Id(21)] public long? NullableLongField { get; set; }
    [Id(22)] public ulong? NullableULongField { get; set; }
    [Id(23)] public float? NullableFloatField { get; set; }
    [Id(24)] public double? NullableDoubleField { get; set; }
    [Id(25)] public decimal? NullableDecimalField { get; set; }

    // =================
    // 引用类型 (Reference Types)
    // =================
    
    [Id(26)] public string StringField { get; set; }
    [Id(27)] public object ObjectField { get; set; }

    // =================
    // 特殊类型 (Special Types)
    // =================
    
    [Id(28)] public DateTime DateTimeField { get; set; }
    [Id(29)] public DateTime? NullableDateTimeField { get; set; }
    [Id(30)] public DateTimeOffset DateTimeOffsetField { get; set; }
    [Id(31)] public DateTimeOffset? NullableDateTimeOffsetField { get; set; }
    [Id(32)] public TimeSpan TimeSpanField { get; set; }
    [Id(33)] public TimeSpan? NullableTimeSpanField { get; set; }
    [Id(34)] public Guid GuidField { get; set; }
    [Id(35)] public Guid? NullableGuidField { get; set; }

    // =================
    // 数组类型 (Array Types)
    // =================
    
    [Id(36)] public byte[] ByteArrayField { get; set; }
    [Id(37)] public int[] IntArrayField { get; set; }
    [Id(38)] public string[] StringArrayField { get; set; }
    [Id(39)] public bool[] BoolArrayField { get; set; }

    // =================
    // 集合类型 (Collection Types)
    // =================
    
    [Id(40)] public List<string> StringListField { get; set; }
    [Id(41)] public List<int> IntListField { get; set; }
    [Id(42)] public List<bool> BoolListField { get; set; }
    [Id(43)] public Dictionary<string, object> StringObjectDictionaryField { get; set; }
    [Id(44)] public Dictionary<string, string> StringStringDictionaryField { get; set; }
    [Id(45)] public Dictionary<int, string> IntStringDictionaryField { get; set; }

    // =================
    // 枚举类型 (Enum Types)
    // =================
    
    [Id(46)] public LLMProviderEnum EnumField { get; set; }
    [Id(47)] public LLMProviderEnum? NullableEnumField { get; set; }
    [Id(48)] public TypeTestStatusEnum CustomEnumField { get; set; }
    [Id(49)] public TypeTestStatusEnum? NullableCustomEnumField { get; set; }

    // =================
    // 复杂对象类型 (Complex Object Types)
    // =================
    
    [Id(50)] public LLMConfigDto LlmConfigField { get; set; }
    [Id(51)] public StreamingConfig StreamingConfigField { get; set; }
    [Id(52)] public TypeTestNestedConfig NestedConfigField { get; set; }

    // =================
    // 嵌套集合类型 (Nested Collection Types)
    // =================
    
    [Id(53)] public List<LLMConfigDto> LlmConfigListField { get; set; }
    [Id(54)] public List<Dictionary<string, object>> DictionaryListField { get; set; }
    [Id(55)] public Dictionary<string, List<string>> StringListDictionaryField { get; set; }

    // =================
    // 特殊业务类型 (Special Business Types)
    // =================
    
    [Id(56)] public string JsonField { get; set; }
    [Id(57)] public string XmlField { get; set; }
    [Id(58)] public string Base64Field { get; set; }
    [Id(59)] public Uri UriField { get; set; }

    // =================
    // 极值和边界测试 (Edge Cases and Boundary Testing)
    // =================
    
    [Id(60)] public string EmptyStringField { get; set; }
    [Id(61)] public string VeryLongStringField { get; set; }
    [Id(62)] public List<string> EmptyListField { get; set; }
    [Id(63)] public Dictionary<string, object> EmptyDictionaryField { get; set; }
    [Id(64)] public int MaxIntField { get; set; }
    [Id(65)] public int MinIntField { get; set; }
    [Id(66)] public double PositiveInfinityField { get; set; }
    [Id(67)] public double NegativeInfinityField { get; set; }
    [Id(68)] public double NaNField { get; set; }

    // =================
    // 多语言和特殊字符 (Multilingual and Special Characters)
    // =================
    
    [Id(69)] public string UnicodeStringField { get; set; }
    [Id(70)] public string SpecialCharactersField { get; set; }
    
    // =================
    // 版本和元数据 (Version and Metadata)
    // =================
    
    [Id(71)] public Version VersionField { get; set; }
    [Id(72)] public string DescriptionField { get; set; }
}

[GenerateSerializer]
public class TypeTestNestedConfig
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public int Value { get; set; }
    [Id(2)] public DateTime CreatedAt { get; set; }
}

[GenerateSerializer]
public enum TypeTestStatusEnum
{
    Inactive = 0,
    Active = 1,
    Pending = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
} 