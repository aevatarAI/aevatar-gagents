# Aevatar.GAgents.ConfigValidateGagent

## 📋 模块概述

**ConfigValidateGagent** 是一个极简的两参数动态验证演示模块。该模块的核心是 `ConfigValidateGAgentConfig` 类，专注于展示如何使用仅两个参数实现强大的动态类型验证功能。

## 🎯 核心功能

### 🆕 两参数动态验证系统
- **ValidationType**：指定验证类型的枚举参数
- **ValidationInput**：待验证内容的字符串参数
- **IValidatableObject实现**：基于两参数的动态验证逻辑
- **7种验证类型**：Email、URL、Phone、Date、JSON、IP、信用卡格式验证
- **中文错误信息**：详细的验证错误提示

## 🏗️ 架构结构

### 核心组件

#### ConfigValidateGAgentConfig ⭐ (重点 - 仅两个参数)
配置类，实现`IValidatableObject`接口，包含：
- **ValidationType**：指定验证类型的枚举参数
- **ValidationInput**：待验证内容的字符串参数
- **动态验证逻辑**：根据ValidationType自动选择验证方法
- **详细的验证错误信息**：中文错误提示

#### ConfigValidateGAgent (极简实现)
主要的GAgent实现类，继承自`GAgentBase<ConfigValidateGAgentState, ConfigValidateGAgentEvent, EventBase, ConfigValidateGAgentConfig>`

#### ConfigValidateGAgentState (极简)
状态类，记录基本信息：
- 配置ID
- 最后配置更新时间

#### ConfigValidateGAgentEvent (极简)
事件系统，包含：
- `ConfigurationUpdatedEvent` - 配置更新事件

## 🔧 两参数动态验证功能

### 验证类型枚举 (ValidationType)
```csharp
public enum ValidationType
{
    Email = 0,      // 邮箱地址格式
    Url = 1,        // URL地址格式  
    Phone = 2,      // 电话号码格式
    Date = 3,       // 日期格式
    Json = 4,       // JSON格式
    IPAddress = 5,  // IP地址格式
    CreditCard = 6  // 信用卡号格式
}
```

### IValidatableObject动态验证
根据`ValidationType`参数自动选择验证逻辑，支持7种格式验证：

1. **Email格式验证**：使用System.Net.Mail.MailAddress验证邮箱格式
2. **URL格式验证**：验证HTTP/HTTPS URL格式
3. **电话号码验证**：支持中国手机号和固话格式
4. **日期格式验证**：使用DateTime.TryParse验证日期
5. **JSON格式验证**：使用System.Text.Json验证JSON格式
6. **IP地址验证**：支持IPv4和IPv6地址格式
7. **信用卡号验证**：支持主流信用卡号格式(Visa、MasterCard等)

## 🚀 使用方式

### 基本用法
```csharp
// 创建配置（仅两个核心参数）
var config = new ConfigValidateGAgentConfig
{
    ValidationType = ValidationType.Email,        // 参数1：指定验证类型
    ValidationInput = "test@example.com"          // 参数2：待验证内容
};

// 通过GAgentFactory创建实例（配置验证自动执行）
var agent = await gAgentFactory.GetGAgentAsync<IConfigValidateGAgent>(
    Guid.NewGuid(), 
    config
);

// 获取验证状态
var status = await agent.GetValidationStatusAsync();
```

### 🆕 动态类型验证演示
```csharp
// 邮箱格式验证
var config1 = new ConfigValidateGAgentConfig
{
    ServiceName = "TestService",
    ValidationType = ValidationType.Email,
    ValidationInput = "invalid-email"  // 会触发邮箱格式错误
};

// URL格式验证
var config2 = new ConfigValidateGAgentConfig
{
    ServiceName = "TestService", 
    ValidationType = ValidationType.Url,
    ValidationInput = "https://example.com"  // 正确的URL格式
};

// 电话号码验证
var config3 = new ConfigValidateGAgentConfig
{
    ServiceName = "TestService",
    ValidationType = ValidationType.Phone,
    ValidationInput = "13812345678"  // 中国手机号格式
};

// JSON格式验证
var config4 = new ConfigValidateGAgentConfig
{
    ServiceName = "TestService",
    ValidationType = ValidationType.Json,
    ValidationInput = "{\"key\":\"value\"}"  // JSON格式
};
```

### 支持的验证类型 (7种)
- **Email**: 邮箱地址格式 (`user@example.com`)
- **Url**: URL地址格式 (`https://example.com`)
- **Phone**: 电话号码格式 (`13812345678`, `010-12345678`)
- **Date**: 日期格式 (`2024-01-01`, `2024/01/01`)
- **Json**: JSON格式 (`{"key":"value"}`)
- **IPAddress**: IP地址格式 (`192.168.1.1`, `2001:db8::1`)
- **CreditCard**: 信用卡号格式 (`4111111111111111`)

### 手动验证配置
```csharp
// 直接验证配置对象
var validationContext = new ValidationContext(config);
var validationResults = new List<ValidationResult>();

// DataAnnotations验证
var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

// IValidatableObject自定义验证
var customResults = config.Validate(validationContext);

// 查看所有验证错误
foreach (var error in validationResults.Concat(customResults))
{
    Console.WriteLine($"错误: {error.ErrorMessage}");
}
```

### 验证失败示例
```csharp
var invalidConfig = new ConfigValidateGAgentConfig
{
    ServiceName = "test-service",  // 包含'test'
    Environment = "Production",   // 生产环境
    ConnectionString = "Server=localhost;", // localhost连接
    PoolSize = 3,  // 小于生产环境最小值5
    // NotificationEmail = null  // 生产环境缺少通知邮箱
};

// 验证会失败，产生多个错误：
// - 生产环境不能使用localhost连接字符串
// - 生产环境服务名称不能包含'test'
// - 生产环境连接池大小不能少于5
// - 生产环境必须提供通知邮箱
```

## 📊 验证结果格式

### 成功状态
```
配置验证状态报告:
服务名称: ProductionService
环境: Production
配置有效: 是
最后验证时间: 2024-01-01 10:00:00
验证次数: 1
当前设置数量: 0
最后操作结果: 配置验证成功
```

### 失败状态
```
配置验证状态报告:
服务名称: test-service
环境: Production
配置有效: 否
最后验证时间: 2024-01-01 10:00:00
验证次数: 1
当前设置数量: 0
最后操作结果: 配置验证失败: 生产环境不能使用localhost连接字符串, ...

验证错误:
- 生产环境不能使用localhost连接字符串
- 生产环境服务名称不能包含'test'
- 生产环境连接池大小不能少于5
- 生产环境必须提供通知邮箱
```

## 🔍 开发指南

### 命名约定
遵循说明书的命名约定：
- GAgent类型：`Aevatar.GAgents.ConfigValidateGagent.GAgents.ConfigValidateGAgent.ConfigValidateGAgent`
- Config类型：`Aevatar.GAgents.ConfigValidateGagent.GAgents.ConfigValidateGAgent.ConfigValidateGAgentConfig`

### 扩展验证规则
在`ConfigValidateGAgentConfig.Validate`方法中添加新的验证规则：
```csharp
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
{
    var errors = new List<ValidationResult>();
    
    // 添加新的验证规则
    if (YourCustomCondition)
    {
        errors.Add(new ValidationResult(
            "自定义错误信息",
            new[] { nameof(PropertyName) }));
    }
    
    return errors;
}
```

## ✨ 特色功能

1. **零配置自动发现**：符合说明书中的自动映射要求
2. **类型安全保证**：完整命名空间验证
3. **高性能设计**：事件溯源状态管理
4. **开发者友好**：详细的中文错误信息
5. **生产就绪**：完整的日志记录和状态跟踪

## 📋 总结

ConfigValidateGagent模块主要展示了 **ConfigValidateGAgentConfig** 的配置验证功能：

### 🎯 核心价值 (ConfigValidateGAgentConfig)
- ✅ **DataAnnotations验证**：Required、StringLength、Range、EmailAddress、Url等多种验证特性
- ✅ **IValidatableObject实现**：10种复杂自定义验证逻辑，包含业务规则验证
- ✅ **🆕 动态类型验证**：支持7种验证类型（Email、URL、Phone、Date、JSON、IP、信用卡）
- ✅ **命名约定遵循**：符合说明书要求的`{GAgentFullName}Config`模式
- ✅ **同程序集部署**：GAgent和Config在同一程序集中
- ✅ **中文错误信息**：详细的验证错误提示

### 🔧 技术特性
- ✅ **多层验证机制**：结合标准验证和自定义验证
- ✅ **完整错误收集**：收集并返回所有验证错误
- ✅ **Orleans兼容**：可直接用于Orleans GAgent配置
- ✅ **简化架构**：其他组件简化，专注于配置验证演示

该模块为GAgent配置验证提供了完整的技术演示，重点展示了如何正确实现 `IValidatableObject` 和使用 `DataAnnotations` 进行配置验证。

## 🌟 动态验证示例

```csharp
// 完整的动态验证演示
var testConfigs = new[]
{
    new ConfigValidateGAgentConfig
    {
        ServiceName = "EmailValidator",
        ValidationType = ValidationType.Email,
        ValidationInput = "invalid-email",  // ❌ 会触发：不是有效的邮箱格式
        Environment = "Development"
    },
    new ConfigValidateGAgentConfig  
    {
        ServiceName = "PhoneValidator",
        ValidationType = ValidationType.Phone,
        ValidationInput = "13812345678",    // ✅ 正确的手机号格式
        Environment = "Development"
    },
    new ConfigValidateGAgentConfig
    {
        ServiceName = "DateValidator", 
        ValidationType = ValidationType.Date,
        ValidationInput = "2024-01-01",     // ✅ 正确的日期格式
        Environment = "Development"
    },
    new ConfigValidateGAgentConfig
    {
        ServiceName = "JsonValidator", 
        ValidationType = ValidationType.Json,
        ValidationInput = "{\"key\":\"value\"}", // ✅ 正确的JSON格式
        Environment = "Development"
    }
};

foreach (var config in testConfigs)
{
    var context = new ValidationContext(config);
    var results = new List<ValidationResult>();
    
    // 执行完整验证（DataAnnotations + IValidatableObject）
    Validator.TryValidateObject(config, context, results, true);
    var customResults = config.Validate(context);
    
    Console.WriteLine($"=== {config.ServiceName} 验证结果 ===");
    foreach (var error in results.Concat(customResults))
    {
        Console.WriteLine($"❌ {error.ErrorMessage}");
    }
    Console.WriteLine();
}
```

这个模块完美展示了：
- 🎯 **参数化验证**：通过 `ValidationType` 指定验证类型
- 📝 **输入验证**：通过 `ValidationInput` 提供待验证内容  
- 🔧 **格式检查**：根据指定类型自动执行相应的格式验证
- 💡 **简洁设计**：仅使用两个核心参数实现多种验证类型