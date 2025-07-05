using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RYH2025_Qubic.Dtos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QubicContractAnalyzer.Services
{
    /// <summary>
    /// Unified service for Qubic contract analysis, code generation, and security auditing using Groq
    /// </summary>
    public class QubicContractService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly GroqConfig _config;

        public QubicContractService(IConfiguration configuration)
        {
            _config = configuration.GetSection("Groq").Get<GroqConfig>()
                     ?? throw new InvalidOperationException("Groq configuration is required");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
        }

        // ===== MAIN METHOD - COMPLETE PROCESSING =====

        /// <summary>
        /// Processes a complete contract: analysis + code generation + security audit
        /// </summary>
        public async Task<CompleteAnalysisResult> ProcessContractCompleteAsync(
            string contractCode,
            ProcessingOptions? options = null)
        {
            try
            {
                options ??= new ProcessingOptions();

                var result = new CompleteAnalysisResult
                {
                    ProcessedAt = DateTime.UtcNow,
                    Options = options
                };

                result.ContractAnalysis = await AnalyzeContractAsync(contractCode);

                // 2. Code generation for each method
                if (options.GenerateCode)
                {
                    result.GeneratedCode = await GenerateCodeForAllMethodsAsync(result.ContractAnalysis, options);
                }

                // 3. Security audit
                if (options.PerformSecurityAudit)
                {
                    result.SecurityAudit = await PerformSecurityAuditAsync(result.ContractAnalysis, contractCode, options);
                }

                // 4. Save results if specified
                if (!string.IsNullOrEmpty(options.OutputDirectory))
                {
                    await SaveResultsAsync(result, options.OutputDirectory);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new QubicProcessingException("Complete contract processing failed", ex);
            }
        }

        // ===== CONTRACT ANALYSIS =====

        public async Task<ContractAnalysis> AnalyzeContractAsync(string contractCode)
        {
            var prompt = CreateContractAnalysisPrompt(contractCode);
            var systemMessage = @"You are an expert analyzing Qubic smart contracts. 
CRITICAL INSTRUCTIONS:
- Respond with raw JSON only
- No markdown code blocks 
- No explanations or comments
- No additional text before or after JSON
- Start response directly with { character
- Ensure valid JSON syntax";

            var response = await SendGroqRequestAsync<ContractAnalysis>(systemMessage, prompt);

            // Validate and enrich the analysis
            ValidateAndEnrichAnalysis(response, contractCode);

            return response;
        }

        // ===== CODE GENERATION =====

        public async Task<Dictionary<string, GeneratedCodeFiles>> GenerateCodeForAllMethodsAsync(
            ContractAnalysis analysis,
            ProcessingOptions options)
        {
            var result = new Dictionary<string, GeneratedCodeFiles>();

            // Process methods in batches to optimize performance
            var batchSize = Math.Min(options.MaxConcurrentRequests, 3);
            var methodBatches = analysis.Methods.Chunk(batchSize);

            foreach (var batch in methodBatches)
            {
                var tasks = batch.Select(method => GenerateCodeForMethodAsync(method, analysis.ContractName, options));
                var batchResults = await Task.WhenAll(tasks);

                for (int i = 0; i < batch.Length; i++)
                {
                    if (batchResults[i] != null)
                    {
                        result[batch[i].Name] = batchResults[i];
                    }
                }
            }

            return result;
        }

        public async Task<GeneratedCodeFiles> GenerateCodeForMethodAsync(
            ContractMethod method,
            string contractName,
            ProcessingOptions options)
        {
            try
            {
                var files = new GeneratedCodeFiles { MethodName = method.Name };

                // Generate main payload
                var payloadPrompt = CreateCodeGenerationPrompt(method, contractName, options);
                var payloadSystemMessage = @"You are an expert generating TypeScript code for Qubic transactions.
CRITICAL INSTRUCTIONS:
- Respond with TypeScript code only
- No markdown code blocks
- No explanations or comments outside the code
- Start response directly with import statements
- Generate complete, executable TypeScript code";

                files.PayloadCode = await SendGroqRequestAsync<string>(payloadSystemMessage, payloadPrompt);

                // Generate interfaces if requested
                if (options.GenerateInterfaces)
                {
                    files.InterfaceCode = GenerateInterfaceDefinition(method);
                }

                // Generate helpers if requested
                if (options.GenerateHelpers)
                {
                    var helperPrompt = CreateHelperGenerationPrompt(method, contractName);
                    files.HelperCode = await SendGroqRequestAsync<string>(payloadSystemMessage, helperPrompt);
                }

                // Generate validations if requested
                if (options.GenerateValidations)
                {
                    var validationPrompt = CreateValidationGenerationPrompt(method);
                    files.ValidationCode = await SendGroqRequestAsync<string>(payloadSystemMessage, validationPrompt);
                }

                return files;
            }
            catch (Exception ex)
            {
                return new GeneratedCodeFiles { MethodName = method.Name, Error = ex.Message };
            }
        }

        // ===== SECURITY AUDIT =====

        public async Task<SecurityAuditResult> PerformSecurityAuditAsync(
            ContractAnalysis analysis,
            string contractCode,
            ProcessingOptions options)
        {
            var result = new SecurityAuditResult
            {
                ContractName = analysis.ContractName,
                AuditDate = DateTime.UtcNow
            };

            // General vulnerability analysis
            var auditPrompt = CreateSecurityAuditPrompt(contractCode, analysis);
            var auditSystemMessage = @"You are an expert smart contract security auditor specialized in Qubic.
CRITICAL INSTRUCTIONS:
- Respond with raw JSON array only
- No markdown code blocks
- No explanations or comments
- Start response directly with [ character
- Ensure valid JSON syntax";

            result.Vulnerabilities = await SendGroqRequestAsync<List<VulnerabilityFound>>(auditSystemMessage, auditPrompt);

            // Generate test cases for each method
            if (options.GenerateSecurityTests)
            {
                result.SecurityTests = await GenerateSecurityTestsForMethodsAsync(analysis.Methods, contractCode, options);
            }

            // Calculate general risk
            result.OverallRisk = CalculateSecurityRisk(result.Vulnerabilities);

            // Generate recommendations
            result.Recommendations = await GenerateSecurityRecommendationsAsync(result);

            return result;
        }

        public async Task<Dictionary<string, List<SecurityTestCase>>> GenerateSecurityTestsForMethodsAsync(
            List<ContractMethod> methods,
            string contractCode,
            ProcessingOptions options)
        {
            var result = new Dictionary<string, List<SecurityTestCase>>();

            foreach (var method in methods)
            {
                try
                {
                    var testPrompt = CreateSecurityTestPrompt(method, contractCode);
                    var testSystemMessage = @"You are an expert in security testing for Qubic smart contracts.
CRITICAL INSTRUCTIONS:
- Respond with raw JSON array only
- No markdown code blocks
- No explanations or comments
- Start response directly with [ character
- Ensure valid JSON syntax";

                    var tests = await SendGroqRequestAsync<List<SecurityTestCase>>(testSystemMessage, testPrompt);
                    result[method.Name] = tests;
                }
                catch (Exception ex)
                {
                    result[method.Name] = new List<SecurityTestCase>();
                }
            }

            return result;
        }

        // ===== GROQ COMMUNICATION =====

        private async Task<T> SendGroqRequestAsync<T>(string systemMessage, string userMessage)
        {
            try
            {
                var request = new GroqRequest
                {
                    Model = _config.Model,
                    Messages = new List<GroqMessage>
                    {
                        new() { Role = "system", Content = systemMessage },
                        new() { Role = "user", Content = userMessage }
                    },
                    MaxTokens = _config.MaxTokens,
                    Temperature = _config.Temperature
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var groqResponse = JsonSerializer.Deserialize<GroqResponse>(responseJson, _jsonOptions);

                var responseText = groqResponse?.Choices?[0]?.Message?.Content ?? "";

                // If T is string, return directly
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)responseText;
                }

                // For other types, try to deserialize JSON
                var cleanedResponse = CleanJsonResponse(responseText);
                var result = JsonSerializer.Deserialize<T>(cleanedResponse, _jsonOptions);

                if (result == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize response to type {typeof(T).Name}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new GroqApiException("Groq API request failed", ex);
            }
        }

        // ===== PROMPT GENERATION =====

        private string CreateContractAnalysisPrompt(string contractCode)
        {
            return $@"Analyze this Qubic smart contract and extract its structure:

QUBIC BLOCKCHAIN CONTEXT:
Qubic is a feeless blockchain that uses smart contracts written in C++ that inherit from ContractBase. Key concepts:
- Assets are managed through shares with ownership and possession
- Transactions require invocationReward (QUs) for execution fees
- Functions are read-only queries, Procedures modify state
- Order book system for trading assets (ask/bid orders)
- State is managed through Collection data structures
- Each contract has specific indices for function/procedure registration

CONTRACT CODE:
{contractCode}

REQUIRED ANALYSIS:
1. Contract name and namespace
2. All PUBLIC_FUNCTION and PUBLIC_PROCEDURE methods
3. Input/output structures for each method (_input/_output suffixes)
4. Fee requirements and validations
5. Registration indices (REGISTER_USER_FUNCTION/PROCEDURE)
6. Asset and share management logic
7. Order book operations (if applicable)

QUBIC DATA TYPES:
- uint8, uint16, uint32, uint64 → unsigned integers (1,2,4,8 bytes)
- sint8, sint16, sint32, sint64 → signed integers (1,2,4,8 bytes)
- id → 32-byte public key identifier
- Array<T, N> → fixed-size arrays
- Collection<T, N> → dynamic collections with indexing
- Asset → struct with issuer and assetName

QUBIC-SPECIFIC PATTERNS TO IDENTIFY:
- invocationReward handling for fees
- Share ownership/possession transfers
- Order book manipulations (ask/bid orders)
- Asset issuance and management
- State synchronization between _assetOrders and _entityOrders
- Fee calculations (assetIssuanceFee, transferFee, tradeFee)

IMPORTANT FORMATTING RULES:
- inputStruct and outputStruct should contain ONLY the actual field names and types from the struct definition
- If a struct is empty (like Echo_input which has no fields), use empty object: {{}}
- Do NOT include the struct name itself as a field
- Only include the actual field names defined inside the struct

EXAMPLES:
- If struct has no fields: ""inputStruct"": {{}}
- If struct has fields: ""inputStruct"": {{""fieldName"": ""fieldType""}}

Return JSON in this exact format:
{{
  ""contractName"": ""name"",
  ""namespace"": ""namespace"",
  ""methods"": [
    {{
      ""name"": ""MethodName"",
      ""type"": ""FUNCTION"",
      ""procedureIndex"": null,
      ""inputStruct"": {{}},
      ""outputStruct"": {{""fieldName"": ""fieldType""}},
      ""fees"": {{""requiresFee"": false, ""feeType"": null, ""amount"": null}},
      ""validations"": [],
      ""description"": ""description"",
      ""isAssetRelated"": false,
      ""isOrderBookRelated"": false
    }}
  ]
}}";
        }


        private string CreateCodeGenerationPrompt(ContractMethod method, string contractName, ProcessingOptions options)
        {
            var methodJson = JsonSerializer.Serialize(method, _jsonOptions);

            return $@"Generate a complete TypeScript class for this Qubic contract method:

QUBIC TRANSACTION CONTEXT:
Qubic transactions are built using payload classes that implement IQubicBuildPackage. Each transaction:
- Has a specific byte size calculated from input struct fields
- Uses QubicPackageBuilder to serialize data in correct order
- May require invocationReward (QUs) for execution
- Can involve asset transfers, order book operations, or state changes

METHOD:
{methodJson}

CONTRACT: {contractName}

Generate code following exactly this pattern:

import {{ IQubicBuildPackage }} from ""../IQubicBuildPackage"";
import {{ PublicKey }} from ""../PublicKey"";
import {{ Long }} from ""../Long"";
import {{ QubicPackageBuilder }} from ""../../QubicPackageBuilder"";
import {{ DynamicPayload }} from ""../DynamicPayload"";

export class Qubic{method.Name}Payload implements IQubicBuildPackage {{
    private _internalPackageSize = [CALCULATE_EXACT_SIZE]; // Comment with calculation
    private {ToLowerCamelCase(method.Name)}Input: {method.Name}Input;

    constructor(actionInput: {method.Name}Input) {{
        this.{ToLowerCamelCase(method.Name)}Input = actionInput;
    }}

    getPackageSize(): number {{
        return this._internalPackageSize;
    }}

    getPackageData(): Uint8Array {{
        const builder = new QubicPackageBuilder(this.getPackageSize());
        // Add fields in correct order according to inputStruct
        return builder.getData();
    }}

    getTransactionPayload(): DynamicPayload {{
        const payload = new DynamicPayload(this.getPackageSize());
        payload.setPayload(this.getPackageData());
        return payload;
    }}

    getTotalAmount(): bigint {{
        // Calculate according to method logic
        return BigInt(0);
    }}

    validateInput(): string[] {{
        const errors: string[] = [];
        // Method-specific validations
        return errors;
    }}
}}

export interface {method.Name}Input {{
    // Fields from inputStruct
}}

TYPE MAPPING:
- uint8/16/32/64, sint8/16/32/64 → Long
- id → PublicKey  
- bool → boolean
- Array<T,N> → Array<T>

BYTE SIZES:
- uint8/sint8 = 1, uint16/sint16 = 2, uint32/sint32 = 4, uint64/sint64 = 8
- id = 32, bool = 1

QUBIC-SPECIFIC CONSIDERATIONS:
- Asset-related methods may need issuer and assetName validation
- Order book methods should validate price > 0 and numberOfShares > 0
- Fee-requiring methods should calculate invocationReward in getTotalAmount()
- Ensure proper field ordering for binary serialization

Generate complete TypeScript code:";
        }

        private string CreateSecurityAuditPrompt(string contractCode, ContractAnalysis analysis)
        {
            var analysisJson = JsonSerializer.Serialize(analysis, _jsonOptions);

            return $@"Perform a comprehensive security audit of this Qubic smart contract:

QUBIC SECURITY CONTEXT:
Qubic contracts have unique security considerations:
- Asset ownership vs possession separation
- Share manipulation through order book operations
- invocationReward fee manipulation
- Concurrent state access by multiple transactions
- Cross-contract share transfers and management rights
- Collection-based state storage vulnerabilities

CONTRACT ANALYSIS:
{analysisJson}

COMPLETE CODE:
{contractCode}

VULNERABILITIES TO SEARCH FOR:

1. QUBIC-SPECIFIC VULNERABILITIES:
- Share Manipulation: Unauthorized ownership/possession transfers
- Order Book Exploitation: Price/volume manipulation in ask/bid orders
- Fee Bypass: Circumventing invocationReward requirements
- Asset Issuance Abuse: Unauthorized asset creation or inflation
- State Desynchronization: _assetOrders vs _entityOrders inconsistency
- Management Rights Abuse: Unauthorized share management transfers

2. STANDARD VULNERABILITIES:
- Reentrancy: Recursive calls during state changes
- Integer Overflow/Underflow: Arithmetic overflows in calculations
- Access Control: Missing authorization checks
- State Manipulation: Unauthorized state modifications
- DoS Attacks: Gas limit exploitation or unexpected reverts
- Front-running: Transaction ordering vulnerabilities
- Business Logic Flaws: Logic errors in contract operations

3. QUBIC-SPECIFIC ANALYSIS POINTS:
- Share balance verification before transfers
- invocationReward validation and refund logic
- Order book state consistency maintenance
- Asset metadata integrity
- Concurrent transaction handling
- Cross-method state dependencies

4. COMMON QUBIC ATTACK VECTORS:
- Draining assets through manipulated orders
- Fee evasion through reward manipulation
- Share dilution attacks
- Order book front-running
- State corruption through concurrent access

Return JSON array with this structure:
[
  {{
    ""name"": ""Vulnerability name"",
    ""type"": ""VulnerabilityType"",
    ""severity"": ""Critical"",
    ""description"": ""Detailed description"",
    ""location"": ""Code location"",
    ""impact"": ""Potential impact"",
    ""exploitScenarios"": [""scenario1"", ""scenario2""],
    ""recommendations"": [""recommendation1"", ""recommendation2""],
    ""isConfirmed"": true,
    ""qubicSpecific"": true
  }}
]";
        }

        private string CreateSecurityTestPrompt(ContractMethod method, string contractCode)
        {
            var methodJson = JsonSerializer.Serialize(method, _jsonOptions);

            return $@"Generate security test cases to exploit vulnerabilities in this Qubic method:

QUBIC TESTING CONTEXT:
Qubic security testing requires understanding:
- Asset and share mechanics
- Order book operations and state
- invocationReward fee handling
- Collection-based storage patterns
- State synchronization requirements

METHOD:
{methodJson}

CONTRACT CONTEXT:
{contractCode.Substring(0, Math.Min(1500, contractCode.Length))}...

Generate 3-5 test cases including:

1. QUBIC-SPECIFIC ATTACKS:
- Share manipulation attempts
- Order book state corruption
- Fee bypass attempts
- Asset ownership spoofing

2. STANDARD ATTACKS:
- Basic exploits
- Edge cases with boundary values
- Combined attack sequences
- State corruption attacks

3. TEST CATEGORIES:
- Asset Tests: Invalid issuer/assetName combinations
- Order Tests: Manipulated price/volume values
- Fee Tests: Insufficient or excessive invocationReward
- State Tests: Concurrent access patterns

For each test case include:
- Executable test code
- Specific malicious inputs
- Expected vs actual behavior
- Severity level
- Qubic-specific context

Return JSON array:
[
  {{
    ""testName"": ""Test name"",
    ""description"": ""What it attempts to exploit"",
    ""vulnerabilityType"": ""Type"",
    ""severity"": ""Critical"",
    ""testInputs"": {{""field"": ""malicious_value""}},
    ""expectedBehavior"": ""Expected behavior"",
    ""actualRisk"": ""Real risk"",
    ""testCode"": ""Executable TypeScript code"",
    ""mitigationSteps"": [""step1"", ""step2""],
    ""qubicSpecific"": true,
    ""requiresAssets"": false,
    ""requiresOrderBook"": false
  }}
]";
        }

        private string CreateHelperGenerationPrompt(ContractMethod method, string contractName)
        {
            return $@"Generate helper functions in TypeScript for the {method.Name} method:

QUBIC HELPERS CONTEXT:
Qubic transactions often need:
- Asset validation utilities
- Fee calculation helpers
- Order book state checkers
- Share balance verifiers

Include:
1. Function to create payload easily
2. Function to validate parameters specific to Qubic
3. Function to calculate fees and invocationReward
4. Function to execute transaction with proper error handling
5. Asset/share validation utilities (if applicable)

Generate TypeScript helper functions for {method.Name} method in contract {contractName}.";
        }

        private string CreateValidationGenerationPrompt(ContractMethod method)
        {
            return $@"Generate detailed validation rules for the {method.Name} method:

QUBIC VALIDATION CONTEXT:
Qubic contracts require validation for:
- Asset existence and ownership
- Share balance sufficiency  
- Order book constraints
- Fee amount correctness
- Public key format validation

Include validations for:
1. Data types and ranges
2. Qubic-specific constraints (asset IDs, share amounts)
3. Business logic requirements
4. Security constraints
5. Order book rules (if applicable)

Generate TypeScript validation functions for {method.Name} method.";
        }

        // ===== HELPER METHODS =====

        private void ValidateAndEnrichAnalysis(ContractAnalysis analysis, string contractCode)
        {
            // Validate that the analysis makes sense
            if (string.IsNullOrEmpty(analysis.ContractName))
            {
                analysis.ContractName = ExtractContractNameFromCode(contractCode);
            }

            // Enrich methods with additional information
            foreach (var method in analysis.Methods)
            {
                if (string.IsNullOrEmpty(method.Description))
                {
                    method.Description = GenerateMethodDescription(method);
                }
            }
        }

        private string ExtractContractNameFromCode(string contractCode)
        {
            var lines = contractCode.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("struct") && line.Contains(": public ContractBase"))
                {
                    var parts = line.Split(new[] { "struct", ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        return parts[0].Trim();
                    }
                }
            }
            return "UnknownContract";
        }

        private string GenerateMethodDescription(ContractMethod method)
        {
            return method.Type switch
            {
                "FUNCTION" => $"Query function that returns {string.Join(", ", method.OutputStruct.Keys)}",
                "PROCEDURE" => $"Transaction procedure that modifies contract state",
                _ => $"Contract method: {method.Name}"
            };
        }

        private string GenerateInterfaceDefinition(ContractMethod method)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"export interface {method.Name}Input {{");
            foreach (var field in method.InputStruct)
            {
                var tsType = ConvertToTypeScriptType(field.Value);
                sb.AppendLine($"    {field.Key}: {tsType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"export interface {method.Name}Output {{");
            foreach (var field in method.OutputStruct)
            {
                var tsType = ConvertToTypeScriptType(field.Value);
                sb.AppendLine($"    {field.Key}: {tsType};");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string ConvertToTypeScriptType(string cppType)
        {
            return cppType.ToLower() switch
            {
                var t when t.Contains("uint") || t.Contains("sint") => "Long",
                "id" => "PublicKey",
                "bool" => "boolean",
                var t when t.Contains("array") => "Array<any>",
                _ => "any"
            };
        }

        private string ToLowerCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        private SecurityRisk CalculateSecurityRisk(List<VulnerabilityFound> vulnerabilities)
        {
            if (!vulnerabilities.Any())
            {
                return new SecurityRisk
                {
                    Level = "Info",
                    Score = 0.0,
                    Summary = "No vulnerabilities detected"
                };
            }

            var severityWeights = new Dictionary<string, double>
            {
                { "Critical", 10.0 },
                { "High", 7.5 },
                { "Medium", 5.0 },
                { "Low", 2.5 }
            };

            var totalScore = vulnerabilities.Sum(v => severityWeights.GetValueOrDefault(v.Severity, 1.0));
            var maxScore = vulnerabilities.Count * 10.0;
            var riskScore = Math.Round((totalScore / maxScore) * 10.0, 1);

            var riskLevel = riskScore switch
            {
                >= 8.0 => "Critical",
                >= 6.0 => "High",
                >= 4.0 => "Medium",
                >= 2.0 => "Low",
                _ => "Info"
            };

            return new SecurityRisk
            {
                Level = riskLevel,
                Score = riskScore,
                Summary = $"Found {vulnerabilities.Count} vulnerabilities. Risk level: {riskLevel} ({riskScore}/10)",
                Factors = vulnerabilities.GroupBy(v => v.Type).Select(g => $"{g.Key}: {g.Count()}").ToList()
            };
        }

        private async Task<List<string>> GenerateSecurityRecommendationsAsync(SecurityAuditResult auditResult)
        {
            if (!auditResult.Vulnerabilities.Any())
            {
                return new List<string> { "Contract appears secure based on current analysis." };
            }

            var prompt = $@"Based on these vulnerabilities found, generate 5-7 prioritized recommendations:

VULNERABILITIES:
{string.Join("\n", auditResult.Vulnerabilities.Take(5).Select(v => $"- {v.Name} ({v.Severity}): {v.Description}"))}

OVERALL RISK: {auditResult.OverallRisk.Level} ({auditResult.OverallRisk.Score}/10)

Generate specific and actionable recommendations to improve contract security, considering Qubic's unique architecture.

Return JSON array of strings:
[""recommendation1"", ""recommendation2"", ""recommendation3""]";

            try
            {
                return await SendGroqRequestAsync<List<string>>(
                    @"You are an expert security consultant for Qubic smart contracts.
CRITICAL INSTRUCTIONS:
- Respond with raw JSON array only
- No markdown code blocks
- Start response directly with [ character
- Each recommendation as a string in the array",
                    prompt);
            }
            catch
            {
                return auditResult.Vulnerabilities.SelectMany(v => v.Recommendations).Distinct().ToList();
            }
        }

        private string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "{}";

            var trimmed = response.Trim();

            // Remove markdown if present
            if (trimmed.StartsWith("```json") || trimmed.StartsWith("```"))
            {
                var startIndex = trimmed.IndexOf('\n') + 1;
                var endIndex = trimmed.LastIndexOf("```");
                if (startIndex > 0 && endIndex > startIndex)
                {
                    trimmed = trimmed.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            // Determine if we're dealing with an object {} or array []
            var firstChar = trimmed.TrimStart()[0];

            if (firstChar == '{')
            {
                return ExtractJsonObject(trimmed);
            }
            else if (firstChar == '[')
            {
                return ExtractJsonArray(trimmed);
            }

            return "{}";
        }

        private string ExtractJsonObject(string text)
        {
            var firstBrace = text.IndexOf('{');
            if (firstBrace == -1) return "{}";

            int braceCount = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = firstBrace; i < text.Length; i++)
            {
                char c = text[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString)
                {
                    if (c == '{')
                    {
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            return text.Substring(firstBrace, i - firstBrace + 1);
                        }
                    }
                }
            }

            return "{}";
        }

        private string ExtractJsonArray(string text)
        {
            var firstBracket = text.IndexOf('[');
            if (firstBracket == -1) return "[]";

            int bracketCount = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = firstBracket; i < text.Length; i++)
            {
                char c = text[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString)
                {
                    if (c == '[')
                    {
                        bracketCount++;
                    }
                    else if (c == ']')
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                        {
                            return text.Substring(firstBracket, i - firstBracket + 1);
                        }
                    }
                }
            }

            return "[]";
        }

        // ===== PERSISTENCE =====

        private async Task SaveResultsAsync(CompleteAnalysisResult result, string outputDirectory)
        {
            try
            {
                var contractDir = Path.Combine(outputDirectory, result.ContractAnalysis.ContractName);
                Directory.CreateDirectory(contractDir);

                // Save main analysis
                var analysisPath = Path.Combine(contractDir, "contract_analysis.json");
                await File.WriteAllTextAsync(analysisPath, JsonSerializer.Serialize(result.ContractAnalysis, _jsonOptions));

                // Save generated code
                if (result.GeneratedCode.Any())
                {
                    var codeDir = Path.Combine(contractDir, "generated");
                    Directory.CreateDirectory(codeDir);

                    foreach (var (methodName, codeFiles) in result.GeneratedCode)
                    {
                        var methodDir = Path.Combine(codeDir, methodName);
                        Directory.CreateDirectory(methodDir);

                        if (!string.IsNullOrEmpty(codeFiles.PayloadCode))
                            await File.WriteAllTextAsync(Path.Combine(methodDir, $"{methodName}Payload.ts"), codeFiles.PayloadCode);

                        if (!string.IsNullOrEmpty(codeFiles.InterfaceCode))
                            await File.WriteAllTextAsync(Path.Combine(methodDir, $"{methodName}Interfaces.ts"), codeFiles.InterfaceCode);

                        if (!string.IsNullOrEmpty(codeFiles.HelperCode))
                            await File.WriteAllTextAsync(Path.Combine(methodDir, $"{methodName}Helpers.ts"), codeFiles.HelperCode);

                        if (!string.IsNullOrEmpty(codeFiles.ValidationCode))
                            await File.WriteAllTextAsync(Path.Combine(methodDir, $"{methodName}Validation.ts"), codeFiles.ValidationCode);
                    }
                }

                // Save security audit
                if (result.SecurityAudit != null)
                {
                    var securityDir = Path.Combine(contractDir, "security");
                    Directory.CreateDirectory(securityDir);

                    var securityPath = Path.Combine(securityDir, "security_audit.json");
                    await File.WriteAllTextAsync(securityPath, JsonSerializer.Serialize(result.SecurityAudit, _jsonOptions));

                    // Save test cases
                    if (result.SecurityAudit.SecurityTests.Any())
                    {
                        var testsDir = Path.Combine(securityDir, "tests");
                        Directory.CreateDirectory(testsDir);

                        foreach (var (methodName, tests) in result.SecurityAudit.SecurityTests)
                        {
                            var testPath = Path.Combine(testsDir, $"{methodName}_security_tests.json");
                            await File.WriteAllTextAsync(testPath, JsonSerializer.Serialize(tests, _jsonOptions));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                // Log error but don't throw - saving is optional
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}