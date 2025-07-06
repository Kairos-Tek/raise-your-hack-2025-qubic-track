using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RYH2025_Qubic.Dtos;
using RYH2025_Qubic.Models;
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
        public async Task<ContractAnalysis> ProcessContractCompleteAsync(
            string contractCode,
            ProcessingOptions? options = null)
        {
            try
            {
                options ??= new ProcessingOptions();

               
                var contractAnalysis = await AnalyzeContractAsync(contractCode);

                
                // 3. Security audit
                if (options.PerformSecurityAudit)
                {
                    contractAnalysis.SecurityAudit = await PerformSecurityAuditAsync(contractAnalysis, contractCode, options);
                }

                // 4. Save results if specified
                if (!string.IsNullOrEmpty(options.OutputDirectory))
                {
                    await SaveResultsAsync(contractAnalysis, options.OutputDirectory);
                }

                return contractAnalysis;
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
                result.SecurityTests = await GenerateSecurityTestsForMethodsAsync(analysis.Methods.ToList(), contractCode, options);
            }

            // Calculate general risk
            result.OverallRisk = CalculateSecurityRisk(result.Vulnerabilities.ToList());

            // Generate recommendations
            result.Recommendations = await GenerateSecurityRecommendationsAsync(result);

            return result;
        }

        public async Task<List<SecurityTestCase>> GenerateSecurityTestsForMethodsAsync(
    List<ContractMethod> methods,
    string contractCode,
    ProcessingOptions options)
        {
            var result = new List<SecurityTestCase>();

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

                    // Asignar el ID del método a cada test case
                    foreach (var test in tests)
                    {
                        test.ContractMethodId = method.Id;  // Asignar la FK
                        test.MethodName = method.Name;      // Ya estaba, pero por consistencia
                    }

                    result.AddRange(tests);
                }
                catch (Exception ex)
                {
                    // Log error pero continuar con otros métodos
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

        // Enhanced CreateContractAnalysisPrompt method in QubicContractService.cs

        private string CreateContractAnalysisPrompt(string contractCode)
        {
            return $@"Analyze this Qubic smart contract and extract its structure INCLUDING EXACT BYTE SIZES for dynamic transaction generation:

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

QUBIC DATA TYPES AND THEIR EXACT BYTE SIZES:
- uint8 → 1 byte
- uint16 → 2 bytes  
- uint32 → 4 bytes
- uint64 → 8 bytes
- sint8 → 1 byte
- sint16 → 2 bytes
- sint32 → 4 bytes
- sint64 → 8 bytes
- id → 32 bytes (public key identifier)
- bool → 1 byte
- char → 1 byte
- Array<T, N> → sizeof(T) * N bytes
- Collection<T, N> → Variable size (not included in transaction payload)

TYPESCRIPT TYPE MAPPING FOR FRONTEND:
- uint8, uint16, uint32, uint64, sint8, sint16, sint32, sint64 → Long
- id → PublicKey
- bool → boolean
- char → string
- Array<T, N> → Array<TypeScriptType>

REQUIRED ANALYSIS:
1. Contract name and namespace
2. ALL PUBLIC_FUNCTION and PUBLIC_PROCEDURE methods
3. Input/output structures with DETAILED FIELD INFORMATION
4. EXACT byte size calculation for each field
5. Field ordering for correct serialization
6. Total package size for each method
7. Fee requirements and validations
8. Registration indices (REGISTER_USER_FUNCTION/PROCEDURE)
9. Asset and share management logic
10. Order book operations (if applicable)

CRITICAL FIELD ANALYSIS:
For each struct field, you must provide:
- Field name (exact as in C++)
- Qubic type (uint64, id, sint64, etc.)
- TypeScript type for frontend
- Exact byte size
- Order position (0-based index for serialization)
- Whether it's an array and array size
- Any validation requirements

EXAMPLE INPUT STRUCT ANALYSIS:
struct AddToAskOrder_input
{{
    id issuer;           // Field 0: 32 bytes
    uint64 assetName;    // Field 1: 8 bytes  
    sint64 price;        // Field 2: 8 bytes
    sint64 numberOfShares; // Field 3: 8 bytes
}}
// Total package size: 32 + 8 + 8 + 8 = 56 bytes

IMPORTANT FORMATTING RULES:
- If a struct is empty (no fields), use empty arrays for inputFields/outputFields
- Calculate exact packageSize by summing all input field byte sizes
- Maintain field order exactly as defined in the struct
- Include TypeScript types for frontend code generation

Return JSON in this EXACT format:
{{
  ""contractName"": ""ContractName"",
  ""namespace"": ""QPI"",
  ""methods"": [
    {{
      ""name"": ""MethodName"",
      ""type"": ""FUNCTION"" or ""PROCEDURE"",
      ""procedureIndex"": null or number,
      ""packageSize"": 56,
      ""inputFields"": [
        {{
          ""name"": ""issuer"",
          ""qubicType"": ""id"",
          ""typeScriptType"": ""PublicKey"",
          ""byteSize"": 32,
          ""order"": 0,
          ""isArray"": false,
          ""arraySize"": null,
          ""description"": ""Asset issuer public key"",
          ""isRequired"": true,
          ""defaultValue"": null,
          ""validations"": [""Must be valid public key format""]
        }},
        {{
          ""name"": ""assetName"",
          ""qubicType"": ""uint64"",
          ""typeScriptType"": ""Long"",
          ""byteSize"": 8,
          ""order"": 1,
          ""isArray"": false,
          ""arraySize"": null,
          ""description"": ""Asset name as 64-bit integer"",
          ""isRequired"": true,
          ""defaultValue"": null,
          ""validations"": [""Must be non-zero""]
        }}
      ],
      ""outputFields"": [
        {{
          ""name"": ""addedNumberOfShares"",
          ""qubicType"": ""sint64"",
          ""typeScriptType"": ""Long"",
          ""byteSize"": 8,
          ""order"": 0,
          ""isArray"": false,
          ""arraySize"": null,
          ""description"": ""Number of shares successfully added to order"",
          ""isRequired"": true,
          ""defaultValue"": null,
          ""validations"": []
        }}
      ],
      ""inputStruct"": {{""issuer"": ""id"", ""assetName"": ""uint64""}},
      ""outputStruct"": {{""addedNumberOfShares"": ""sint64""}},
      ""fees"": {{
        ""requiresFee"": true,
        ""feeType"": ""none"",
        ""amount"": 0,
        ""calculation"": ""No fee for ask orders""
      }},
      ""validations"": [
        ""price must be greater than 0"",
        ""numberOfShares must be greater than 0"",
        ""Must own sufficient shares""
      ],
      ""description"": ""Adds shares to ask order book for selling"",
      ""isAssetRelated"": true,
      ""isOrderBookRelated"": true
    }}
  ]
}}

SPECIAL CONSIDERATIONS:
- Arrays: Calculate total bytes as elementSize * arrayLength
- Nested structs: Sum all internal field sizes
- Collection types: NOT included in transaction payload (state only)
- Empty structs: packageSize = 0, empty inputFields array
- Order matters: Fields must be in exact C++ struct definition order
- Validation rules: Extract from contract code logic (fee checks, bounds, etc.)

PROCEDURE vs FUNCTION IDENTIFICATION:
- FUNCTION: Read-only, returns data, no state changes, no fees usually
- PROCEDURE: Modifies state, may require fees, can transfer assets

FEE ANALYSIS:
- Look for invocationReward() checks in the code
- Identify fee amounts from contract constants
- Note fee calculation formulas (percentage-based, fixed amounts)
- Identify which methods require payment vs which refund excess

Generate complete analysis for ALL public methods found in the contract.";
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

            return $@"Generate security test cases targeting specific variables in this Qubic method:

QUBIC TESTING CONTEXT:
Each test must target ONE specific input variable with a malicious value while providing valid values for other variables.

METHOD TO TEST:
{methodJson}

INPUT VARIABLES AVAILABLE:
{string.Join(", ", method.InputStruct.Select(kv => $"{kv.Key} ({kv.Value})"))}

QUBIC-SPECIFIC ATTACK VECTORS BY VARIABLE TYPE:
- uint/sint variables: Overflow/underflow attacks (0, MAX_VALUE, negative)
- id variables: Invalid public keys, null bytes, wrong format
- Array variables: Buffer overflows, empty arrays, oversized arrays
- Asset variables: Non-existent assets, unauthorized assets
- Share amounts: Zero shares, negative shares, exceeding balance

CONTRACT CONTEXT:
{contractCode.Substring(0, Math.Min(1000, contractCode.Length))}...

Generate 3-5 test cases, each targeting a DIFFERENT input variable.

For each test case:
1. Pick ONE specific input variable to attack
2. Choose a malicious value for that variable
3. Provide valid default values for all other variables
4. Explain WHY that specific value is dangerous

QUBIC ATTACK CATEGORIES:
- Share Manipulation: numberOfShares = MAX_UINT64, = 0, = -1
- Asset Exploitation: assetName = """", issuer = null_id
- Fee Bypass: invocationReward = 0, = negative
- Order Book: price = 0, = MAX_VALUE
- Access Control: targetId = contract_id, = invalid_format

Return JSON array with this exact structure:
[
  {{
    ""testName"": ""Descriptive test name"",
    ""methodName"": ""{method.Name}"",
    ""targetVariable"": ""specific_variable_name"",
    ""description"": ""What vulnerability this test exploits"",
    ""vulnerabilityType"": ""Integer Overflow"",
    ""severity"": ""Critical"",
    ""targetInput"": {{
      ""variableName"": ""specific_variable_name"",
      ""variableType"": ""uint64"",
      ""maliciousValue"": ""18446744073709551615"",
      ""attackReason"": ""Causes integer overflow leading to...""
    }},
    ""otherInputs"": {{
      ""otherVar1"": ""valid_default_value"",
      ""otherVar2"": ""valid_default_value""
    }},
    ""expectedBehavior"": ""Should reject transaction"",
    ""actualRisk"": ""Could drain contract balance"",
    ""mitigationSteps"": [""Add bounds checking"", ""Validate input ranges""]
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

        private async Task SaveResultsAsync(ContractAnalysis result, string outputDirectory)
        {
            try
            {
                var contractDir = Path.Combine(outputDirectory, result.ContractName);
                Directory.CreateDirectory(contractDir);

                // Save main analysis
                var analysisPath = Path.Combine(contractDir, "contract_analysis.json");
                await File.WriteAllTextAsync(analysisPath, JsonSerializer.Serialize(result, _jsonOptions));

                // Save security audit
                if (result.SecurityAudit != null)
                {
                    var securityDir = Path.Combine(contractDir, "security");
                    Directory.CreateDirectory(securityDir);

                    var securityPath = Path.Combine(securityDir, "security_audit.json");
                    await File.WriteAllTextAsync(securityPath, JsonSerializer.Serialize(result.SecurityAudit, _jsonOptions));

                    // Save test cases - CAMBIO AQUÍ
                    if (result.SecurityAudit.SecurityTests.Any())
                    {
                        var testsDir = Path.Combine(securityDir, "tests");
                        Directory.CreateDirectory(testsDir);

                        // Agrupar por método para guardar en archivos separados
                        var testsByMethod = result.SecurityAudit.SecurityTests.GroupBy(t => t.MethodName);

                        foreach (var methodGroup in testsByMethod)
                        {
                            var testPath = Path.Combine(testsDir, $"{methodGroup.Key}_security_tests.json");
                            await File.WriteAllTextAsync(testPath, JsonSerializer.Serialize(methodGroup.ToList(), _jsonOptions));
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