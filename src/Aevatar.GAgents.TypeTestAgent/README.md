# Aevatar.GAgents.TypeTestAgent

## 概述

**Aevatar.GAgents.TypeTestAgent** 是一个专门用于前端类型测试的代理。它提供了包含所有 C# 原始类型和常用复杂类型的完整配置结构，用于验证前端界面对不同数据类型的处理能力。

## 核心特性

### 📋 完整类型覆盖

TypeTestConfigDto 包含以下类型类别：

#### 基本值类型 (Basic Value Types)
- `bool`, `byte`, `sbyte`, `char`
- `short`, `ushort`, `int`, `uint`
- `long`, `ulong`, `float`, `double`, `decimal`

#### 可空值类型 (Nullable Value Types)
- 所有基本值类型的可空版本 (`bool?`, `int?`, 等)
- 特殊类型的可空版本 (`DateTime?`, `Guid?`, 等)

#### 引用类型 (Reference Types)
- `string`, `object`
- 空字符串、长字符串、Unicode 字符串
- 特殊字符串、JSON、XML、Base64

#### 特殊类型 (Special Types)
- `DateTime`, `DateTimeOffset`, `TimeSpan`
- `Guid`, `Version`, `Uri`

#### 数组类型 (Array Types)
- `byte[]`, `int[]`, `string[]`, `bool[]`

#### 集合类型 (Collection Types)
- `List<T>` (多种泛型类型)
- `Dictionary<TKey, TValue>` (多种键值对组合)

#### 枚举类型 (Enum Types)
- 系统枚举 (`LLMProviderEnum`)
- 自定义枚举 (`TypeTestStatusEnum`)
- 可空枚举

#### 复杂对象类型 (Complex Object Types)
- 嵌套配置对象 (`LLMConfigDto`, `StreamingConfig`)
- 自定义嵌套对象 (`TypeTestNestedConfig`)

#### 边界值测试 (Edge Cases)
- `int.MaxValue`, `int.MinValue`
- `double.PositiveInfinity`, `double.NegativeInfinity`, `double.NaN`
- 空集合、极长字符串

#### 多语言和特殊字符
- Unicode 字符串 (中文、阿拉伯文、俄文、表情符号)
- 特殊符号和转义字符

## 项目结构

```
src/Aevatar.GAgents.TypeTestAgent/
├── Agent/
│   ├── ITypeTestAgent.cs          # 代理接口定义
│   └── TypeTestAgent.cs           # 代理实现
├── Dtos/
│   └── TypeTestConfigDto.cs       # 完整类型配置 DTO
├── State/
│   └── TypeTestAgentState.cs      # 代理状态
├── Events/
│   └── TypeTestStateLogEvent.cs   # 状态日志事件
└── README.md                      # 项目文档
```

## 使用方法

### 1. 基本配置

```csharp
var typeTestAgent = serviceProvider.GetGrain<ITypeTestAgent>(Guid.NewGuid());

// 应用配置
var config = new TypeTestConfigDto();
await typeTestAgent.ApplyTypeTestConfigAsync(config);
```

### 2. 获取配置 JSON

```csharp
// 获取当前配置的 JSON 表示
var configJson = await typeTestAgent.GetTypeTestConfigJsonAsync();
```

### 3. 类型展示

TypeTestConfigDto 包含 73 个不同的字段，展示了各种类型的结构：

```json
{
  "boolField": false,
  "byteField": 0,
  "intField": 0,
  "stringField": "",
  "dateTimeField": "0001-01-01T00:00:00",
  "guidField": "00000000-0000-0000-0000-000000000000",
  "nullableIntField": null,
  "stringArrayField": [],
  "stringListField": [],
  "enumField": 0
  // ... 更多字段
}
```

## 前端集成测试

此代理专门设计用于前端类型处理测试：

### 🎯 测试场景

1. **类型渲染测试** - 验证前端能否正确显示所有类型
2. **输入验证测试** - 测试表单对不同类型的验证
3. **序列化测试** - 确保 JSON 序列化/反序列化正常
4. **边界值测试** - 验证极值和特殊情况的处理
5. **国际化测试** - 测试多语言字符的支持

### 📊 类型统计

- **73+ 个字段** 覆盖各种数据类型
- **9 个类型类别** 全面覆盖 C# 类型系统
- **多种边界情况** 包括 null、空值、极值
- **国际化支持** 包含多语言和特殊字符

## 技术实现

- 基于 `AIGAgentBase` 实现
- 支持 Orleans 序列化
- 事件溯源状态管理
- 完整的日志记录
- JSON 序列化支持

## 注意事项

1. 所有字段都为必填，无默认值
2. 可空类型用于测试空值处理
3. 引用类型字段需要前端提供值
4. 字符串字段不包含默认示例数据
5. 数值字段展示类型的默认值

此代理为前端开发者提供了一个完整的类型测试环境，确保用户界面能够正确处理所有可能的数据类型和边界情况。 