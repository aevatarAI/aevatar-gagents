# CLAUDE.md

## System Overview

**Aevatar GAgents** - Custom intelligent agent solution built on Microsoft Orleans and ABP Framework, designed to enable developers to create, manage, and deploy distributed AI agents on Aevatar Station.
**Aevatar Station** - Distributed AI agent management platform built on Microsoft Orleans and ABP Framework.

### Architecture
- **src/** - Core library projects containing agent implementations
  - AI modules (Abstractions, AIGAgent, SemanticKernel)
  - Communication agents (ChatAgent, GroupChat, Router)
  - Integration modules (Twitter, Telegram, AElf blockchain)
- **test/** - Test projects with Orleans TestKit and shared infrastructure
- **simples/** - Sample applications demonstrating usage patterns

**Tech Stack**: .NET 9.0, Orleans 9.0, ABP 9.1.0, MongoDB, Redis, SignalR
**AI Support**: Microsoft Semantic Kernel, OpenAI, Azure AI, Google Gemini, Amazon AI
**Patterns**: Actor Model, Event Sourcing, CQRS, Multi-tenancy, Domain-Driven Design

## Development Commands

```bash
# Build & Test
dotnet build
dotnet test
dotnet format && dotnet build --no-incremental  # Pre-commit

# Run Specific Tests
dotnet test test/[ProjectName] --filter "FullyQualifiedName~[TestClass]" --verbosity normal

# Run Sample Applications
cd simples/AIGAgent/SimpleAIGAgent.Silo && dotnet run
cd simples/AIGAgent/SimpleAIGAgent.Client && dotnet run

# Package Management
dotnet restore
dotnet pack src/[ProjectName] --configuration Release -p:PackageVersion=[VERSION]
```

## Core Concepts

### GAgent Framework
Distributed agents inherit from `GAgentBase<TState, TEvent>`:

```csharp
[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class MyAgent : GAgentBase<MyState, MyEvent>, IMyAgent
{
    [EventHandler]
    public async Task HandleEventAsync(SomeEvent @event)
    {
        // Process event and update state
        State.Property = @event.Data;
        await ConfirmEvents();
    }
}
```

### Required Components for Agent Creation
1. **State Class** - Inherits from `StateBase` with `[GenerateSerializer]`
2. **Event Sourcing Event** - Inherits from `SEventBase` 
3. **External Message Event** - Inherits from `EventBase` with `[GenerateSerializer]`
4. **Agent Implementation** - Inherits from `GAgentBase<TState, TEvent>`

### AI Integration
- **IBrain Interface** - Core abstraction for AI functionality
- **SemanticKernel Module** - Integrations with Azure OpenAI, Google Gemini
- **Configuration** - AI services configured via ABP settings

### Event Flow
Client → SignalR Hub → Orleans Grains → Event Sourcing → State Persistence

## Technical Documentation References
- @~/README.md - Getting started and agent creation tutorial
- @~/md/project-structure.md - Detailed module breakdown
- @~/docs/* - Module-specific documentation
- @source-control.md - Source control management strategies and workflows
- @technical-documentation-practices.md - Practices for creating and maintaining technical documentation
- @test-case-generation-guidelines.md - Guidelines for generating test cases when working on TDD

---

## Coding Directives

### Role
Expert C# developer specializing in scalable, maintainable backend systems.

### CRITICAL: Analysis-First Development
**STOP** - Before writing ANY code:
1. **Understand** - Fully comprehend the task requirements
2. **Analyze** - Break down using sequentialthinking and MECE principles  
3. **Research** - Find existing patterns with openmemory/grep/find
4. **Design** - Propose solution architecture and approach
5. **Confirm** - Present analysis and get approval BEFORE implementing

Example response pattern:
```
## Task Analysis
I understand you need [requirement summary].

## Breakdown (MECE)
1. Component A: [description]
2. Component B: [description]
3. Component C: [description]

## Existing Patterns Found
- Similar implementation in [file:line]
- Related pattern in [file:line]

## Proposed Solution
1. Create/modify [component]
2. Implement [pattern]
3. Test with [approach]

Shall I proceed with this approach?
```

### Core Principles
- **Production-ready code** - All implementations must be enterprise-grade, complete, and robust
- **Maintainability over performance** - Clean, readable code is paramount
- **Minimal changes** - Smallest reasonable modifications to achieve goals
- **Consistency** - Match existing code style within files
- **SOLID principles** - Apply throughout implementation
- **Never break existing functionality** - Preserve working code
- **Complete implementation** - No placeholders, stubs, or TODO comments in production code
- **Proper error handling** - Comprehensive exception handling and validation
- **Security-first** - All code must follow security best practices

### Critical Rules
- ❌ **NEVER** use `--no-verify` when committing
- ❌ **NEVER** rewrite implementations without explicit permission
- ❌ **NEVER** remove comments unless provably false
- ❌ **NEVER** implement mock modes - use real data/APIs
- ❌ **NEVER** name things as 'improved', 'new', 'enhanced'
- ❌ **NEVER** start coding without analysis and proposed solution
- ❌ **NEVER** write implementation code before writing tests
- ❌ **NEVER** skip writing tests unless explicitly authorized
- ❌ **NEVER** write test/development code in production implementation
- ❌ **NEVER** use placeholder/stub implementations in production code
- ❌ **NEVER** include debugging/temporary code in production implementation
- ✅ **ALWAYS** analyze and propose BEFORE implementing
- ✅ **ALWAYS** write failing tests FIRST before any implementation
- ✅ **ALWAYS** ask for clarification vs assumptions
- ✅ **ALWAYS** add `ABOUTME:` comments to new files (2 lines)
- ✅ **ALWAYS** preserve unrelated working code
- ✅ **ALWAYS** follow strict TDD: Red → Green → Refactor cycle
- ✅ **ALWAYS** write production-ready, enterprise-grade code
- ✅ **ALWAYS** implement complete, robust solutions without shortcuts
- ✅ **ALWAYS** include proper error handling and validation

### Implementation Standards
```csharp
// File header format (required for new files)
// ABOUTME: This file implements [core functionality]
// ABOUTME: [Brief description of purpose/responsibility]

// Agent pattern
[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class MyAgent : GAgentBase<MyState, MyEvent>, IMyAgent
{
    [EventHandler]
    public async Task HandleEventAsync(SomeEvent @event)
    {
        // Update state, confirm events
        State.Property = @event.Data;
        await ConfirmEvents();
    }
}
```

## Testing Requirements

### STRICT TEST-DRIVEN DEVELOPMENT (TDD) - ABSOLUTELY MANDATORY

#### TDD is NOT OPTIONAL - It is a REQUIREMENT for ALL code changes

**The TDD Cycle MUST be followed for EVERY feature, bug fix, or code modification:**

1. **RED PHASE** - Write failing test FIRST
   - Define expected behavior through test
   - Run test to confirm it fails (proves test is valid)
   - NO implementation code allowed at this stage
   
2. **GREEN PHASE** - Write MINIMAL production code to pass
   - Implement ONLY enough PRODUCTION-READY code that makes test pass
   - Include proper error handling, validation, and security measures
   - No shortcuts, placeholders, or stub implementations
   - Run test to confirm it passes
   
3. **REFACTOR PHASE** - Improve code quality
   - Clean up implementation while tests stay green
   - Extract methods, improve naming, reduce duplication
   - Run all tests after each change
   
4. **REPEAT** - Continue cycle for next requirement

#### TDD Enforcement Rules

**BEFORE writing ANY implementation code, you MUST:**
- Write at least one failing test
- Run the test to verify it fails correctly
- Show the failing test output
- Only THEN proceed to implementation

**VIOLATIONS of TDD will occur if you:**
- Write implementation code before tests
- Skip writing tests "to save time"
- Write tests after implementation
- Modify code without updating tests first

#### Test Case Documentation (Mandatory)

**During the RED PHASE of TDD, you MUST also:**
- Generate comprehensive test case documentation following @test-case-generation-guidelines.md
- Apply ALL six mandatory test design methods:
  1. **Equivalence Class Partitioning** - Valid/invalid input classes
  2. **Boundary Value Analysis** - Min/max/edge conditions
  3. **Decision Table Testing** - Multiple condition combinations
  4. **Scenario-Based Testing** - Real-world user workflows
  5. **Error Guessing** - Predict potential defects
  6. **State Transition Testing** - State changes and events

**Test Case Documentation Requirements:**
- Create/update markdown files in: `test-cases/{version}/{feature-name}-test-cases.md`
- Follow the hierarchical structure (H1-H6) for XMind compatibility
- Document BEFORE writing the actual test code
- Update documentation when tests change
- Include positive, negative, boundary, and exception cases

**Documentation Example:**
```markdown
# PricingService Test Cases
## Discount Calculation
### VIP Customer Discounts
#### Standard VIP Discount Rules
##### VIP customer receives 10% discount on orders over $100
###### Expected Result
- Discount amount equals 10% of order total
- Final amount is reduced by discount
- Discount is recorded in order history
```

**Test-First Development Example:**
```csharp
// STEP 1: Write failing test FIRST
[Fact]
public async Task Should_CalculateDiscount_When_CustomerIsVIP()
{
    // This test MUST be written BEFORE the CalculateDiscount method exists
    var service = new PricingService();
    var result = await service.CalculateDiscount(customerId: "VIP123", amount: 100);
    
    result.ShouldBe(10); // 10% discount for VIP
}

// STEP 2: Run test - it MUST fail (method doesn't exist yet)
// STEP 3: Write MINIMAL implementation to pass
// STEP 4: Refactor if needed
// STEP 5: Write next failing test for next requirement
```

### Test Coverage (Non-negotiable)
- **Unit tests** - Method-level logic (MUST be written FIRST)
- **Integration tests** - Component interactions (MUST be written FIRST)
- **End-to-end tests** - Full system workflows (MUST be written FIRST)

**IMPORTANT:** Tests must ALWAYS be written BEFORE implementation code.

**The ONLY exception:** User provides explicit authorization:
"I AUTHORIZE YOU TO SKIP WRITING TESTS THIS TIME"

**Without this exact authorization, you MUST follow TDD strictly.**

### Test Implementation
```csharp
[Fact] 
public async Task Should_UpdateState_When_EventReceived()
{
    // Arrange
    var agent = await GetGrainAsync<IMyAgent>(Guid.NewGuid());
    var testEvent = new MyEvent { Data = "test" };
    
    // Act
    await agent.HandleEventAsync(testEvent);
    
    // Assert
    var state = await agent.GetStateAsync();
    state.Property.ShouldBe("test");
}
```

### Test Categories
- ✅ Positive cases (happy path)
- ⚠️ Negative cases (invalid inputs)
- 🔍 Boundary cases (edge conditions)
- 💥 Exception cases (error handling)

**All categories MUST be documented using the test case generation guidelines before implementation.**

### What Tests Should Focus On

**DO TEST these meaningful scenarios:**
1. **Business Logic**: Validation rules, computed properties, business constraints
2. **Custom Behavior**: Any custom implementations beyond simple get/set
3. **Serialization**: Orleans serialization behavior (when not already covered by integration tests)
4. **Domain Rules**: Specific business rules about capabilities, versions, constraints
5. **Equality/Comparison**: Custom equality logic implementations
6. **Complex Interactions**: Multi-step workflows, state transitions
7. **Error Handling**: Exception scenarios and recovery logic
8. **Performance**: Critical path optimizations and resource management

**DO NOT TEST these framework/language features:**
- ❌ Basic C# language features (property get/set, constructors)
- ❌ .NET Framework behavior (List.Add(), string assignment)
- ❌ Simple data model properties without validation
- ❌ Auto-property behavior
- ❌ Standard collection operations
- ❌ Null assignment acceptance
- ❌ Basic object instantiation

**Example of GOOD vs BAD tests:**
```csharp
// ❌ BAD: Testing framework behavior
[Fact]
public void Should_AcceptNullValue_When_PropertyIsNull()
{
    var model = new MyModel();
    model.Name = null;
    model.Name.ShouldBeNull(); // This tests C# behavior, not business logic
}

// ✅ GOOD: Testing business logic
[Fact]
public void Should_ThrowException_When_AgentTypeExceedsMaxLength()
{
    var metadata = new AgentTypeMetadata();
    var longName = new string('A', 101); // Exceeds business rule limit
    
    Should.Throw<ArgumentException>(() => metadata.SetAgentType(longName));
}
```

## Development Workflow

### Task Analysis Protocol (MANDATORY)
Before ANY implementation:
1. **Analyze the request** - Understand the full scope and requirements
2. **Use sequentialthinking** - Break down into MECE components
3. **Research existing patterns** - Use openmemory and grep/find tools
4. **Propose solution approach** - Present plan BEFORE coding
5. **Design test cases** - Apply six test design methods from @test-case-generation-guidelines.md
6. **Document test cases** - Generate markdown documentation in test-cases directory
7. **Get confirmation** - Ensure alignment before proceeding
8. **Write failing tests** - Start with RED phase of TDD based on documented cases
9. **Then implement** - Only after tests are written and failing

### Task Execution
1. **Use TodoWrite** for complex multi-step tasks
2. **Analyze existing code** before modifications
3. **Generate test case documentation** using six design methods
4. **Write failing tests FIRST** based on documented test cases
5. **Execute parallel operations** when possible
6. **Implement production-ready code** to make tests pass
7. **Include comprehensive error handling** and input validation
8. **Implement security best practices** throughout
9. **Refactor** while keeping tests green
10. **Update test documentation** if tests change during refactoring
11. **Validate all tests pass** before considering task complete
12. **Clean up temporary files** at completion

### Development Tools
- **sequentialthinking** - Break down complex tasks using MECE (Mutually Exclusive, Collectively Exhaustive) principles
- **context7** - Orleans-specific guidance, patterns, and troubleshooting support
- **openmemory** - Search for related fixes, changes, and implementation patterns

### Quality Gates
- All tests pass (`dotnet test`)
- No compilation errors (`dotnet build`)
- Code formatted (`dotnet format`)
- Orleans grains properly configured
- Logging added at key checkpoints

### Orleans-Specific
- Use Orleans TestKit for grain testing
- Configure clustering (MongoDB/Redis)
- Implement event sourcing patterns
- Handle grain lifecycle properly
- Monitor Orleans dashboard (port 8080)
- Leverage **context7** tool for Orleans-specific issues

## Error Resolution Protocol

1. **Identify root cause** through logs/diagnostics
2. **Locate existing patterns** in codebase
3. **Apply minimal fix** following established patterns
4. **Verify fix** with comprehensive tests
5. **Document fix** in commit message

### Orleans Troubleshooting
- Verify grain registration and interfaces
- Check clustering and storage configuration
- Validate event sourcing setup
- Monitor grain activation/deactivation
- Review stream provider configuration

---

**REMEMBER: Analyze → Propose → Confirm → Write Tests → See Tests Fail → Implement → See Tests Pass → Refactor. 

TDD is MANDATORY - NO EXCEPTIONS without explicit authorization. 
Always prioritize maintainability, STRICTLY follow TDD, and ask for clarification when uncertain.

The sequence is ALWAYS: RED (failing test) → GREEN (minimal production-ready implementation) → REFACTOR (improve code).

ALL IMPLEMENTATIONS MUST BE PRODUCTION-READY, ENTERPRISE-GRADE CODE - NO SHORTCUTS, PLACEHOLDERS, OR DEVELOPMENT-ONLY CODE.**

ONLY lint for the file that is related to the request. DO NOT lint other files.