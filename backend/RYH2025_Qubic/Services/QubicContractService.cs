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

namespace RYH2025_Qubic.Services
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
            var prompt = Prompts.CreateContractAnalysisPrompt(contractCode);

            var response = await SendGroqRequestAsync<ContractAnalysis>(Prompts.GetSystemMessage(), prompt);

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
            var auditPrompt = Prompts.CreateSecurityAuditPrompt(contractCode, analysis);
           
            result.Vulnerabilities = await SendGroqRequestAsync<List<VulnerabilityFound>>(Prompts.GetSystemMessage(), auditPrompt);

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
                    var testPrompt = Prompts.CreateSecurityTestPrompt(method, contractCode);
                    var tests = await SendGroqRequestAsync<List<SecurityTestCase>>(Prompts.GetSystemMessage(), testPrompt);

                    // Validar y limpiar los test cases
                    foreach (var test in tests)
                    {
                        test.ContractMethodId = method.Id;
                        test.MethodName = method.Name;

                        // VALIDACIÓN CRÍTICA: Verificar consistencia con parámetros reales
                        ValidateTestCaseParameters(test, method);
                    }

                    result.AddRange(tests);
                }
                catch (Exception ex)
                {
                    // Log error pero continuar con otros métodos
                    Console.WriteLine($"Error generating tests for method {method.Name}: {ex.Message}");
                }
            }

            return result;
        }

        private void ValidateTestCaseParameters(SecurityTestCase testCase, ContractMethod method)
        {
            var hasInputParams = method.InputStruct.Any();

            if (!hasInputParams)
            {
                // El método no tiene parámetros: testInputs debe estar vacío
                var currentTestInputs = testCase.TestInputs;

                if (currentTestInputs?.ContainsKey("targetInput") == true ||
                    currentTestInputs?.ContainsKey("otherInputs") == true)
                {
                    // Limpiar parámetros incorrectos - solo mantener estructura vacía
                    testCase.TestInputs = new Dictionary<string, object>();

                    // Ajustar descripción y campos
                    testCase.Description = $"Tests internal logic vulnerabilities in {method.Name} (no input parameters)";
                    testCase.VulnerabilityType = "Internal Logic";
                    testCase.TargetVariable = "internal_state";

                    Console.WriteLine($"INFO: Cleaned test inputs for method '{method.Name}' with no parameters");
                }
            }
            else
            {
                // El método tiene parámetros: validar estructura testInputs
                var currentTestInputs = testCase.TestInputs;

                if (currentTestInputs != null)
                {
                    // Validar targetInput si existe
                    if (currentTestInputs.ContainsKey("targetInput"))
                    {
                        var targetInput = currentTestInputs["targetInput"];
                        if (targetInput != null)
                        {
                            var targetInputDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                                System.Text.Json.JsonSerializer.Serialize(targetInput));

                            if (targetInputDict?.ContainsKey("variableName") == true)
                            {
                                var variableName = targetInputDict["variableName"]?.ToString();
                                if (!string.IsNullOrEmpty(variableName) && !method.InputStruct.ContainsKey(variableName))
                                {
                                    // El parámetro objetivo no existe - reemplazar con el primer parámetro real
                                    var firstRealParam = method.InputStruct.First();
                                    targetInputDict["variableName"] = firstRealParam.Key;
                                    targetInputDict["variableType"] = firstRealParam.Value;
                                    testCase.TargetVariable = firstRealParam.Key;

                                    // Actualizar el objeto
                                    currentTestInputs["targetInput"] = targetInputDict;
                                    testCase.TestInputs = currentTestInputs;

                                    Console.WriteLine($"WARNING: Fixed invalid parameter '{variableName}' in test '{testCase.TestName}' for method '{method.Name}'");
                                }
                            }
                        }
                    }

                    // Validar otherInputs si existe
                    if (currentTestInputs.ContainsKey("otherInputs"))
                    {
                        var otherInputs = currentTestInputs["otherInputs"];
                        if (otherInputs != null)
                        {
                            var otherInputsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                                System.Text.Json.JsonSerializer.Serialize(otherInputs));

                            if (otherInputsDict != null)
                            {
                                var invalidParams = otherInputsDict.Keys
                                    .Where(key => !method.InputStruct.ContainsKey(key))
                                    .ToList();

                                foreach (var invalidParam in invalidParams)
                                {
                                    otherInputsDict.Remove(invalidParam);
                                    Console.WriteLine($"WARNING: Removed invalid parameter '{invalidParam}' from test '{testCase.TestName}' for method '{method.Name}'");
                                }

                                // Actualizar si hubo cambios
                                if (invalidParams.Any())
                                {
                                    currentTestInputs["otherInputs"] = otherInputsDict;
                                    testCase.TestInputs = currentTestInputs;
                                }
                            }
                        }
                    }
                }
            }
        }


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

//        private string CreateSecurityAuditPrompt(string contractCode, ContractAnalysis analysis)
//        {
//            var analysisJson = JsonSerializer.Serialize(analysis, _jsonOptions);

//            return $@"Perform a comprehensive security audit of this Qubic smart contract:

//QUBIC SECURITY CONTEXT:
//Qubic contracts have unique security considerations:
//- Asset ownership vs possession separation
//- Share manipulation through order book operations
//- invocationReward fee manipulation
//- Concurrent state access by multiple transactions
//- Cross-contract share transfers and management rights
//- Collection-based state storage vulnerabilities

//CONTRACT ANALYSIS:
//{analysisJson}

//COMPLETE CODE:
//{contractCode}

//VULNERABILITIES TO SEARCH FOR:

//1. QUBIC-SPECIFIC VULNERABILITIES:
//- Share Manipulation: Unauthorized ownership/possession transfers
//- Order Book Exploitation: Price/volume manipulation in ask/bid orders
//- Fee Bypass: Circumventing invocationReward requirements
//- Asset Issuance Abuse: Unauthorized asset creation or inflation
//- State Desynchronization: _assetOrders vs _entityOrders inconsistency
//- Management Rights Abuse: Unauthorized share management transfers

//2. STANDARD VULNERABILITIES:
//- Reentrancy: Recursive calls during state changes
//- Integer Overflow/Underflow: Arithmetic overflows in calculations
//- Access Control: Missing authorization checks
//- State Manipulation: Unauthorized state modifications
//- DoS Attacks: Gas limit exploitation or unexpected reverts
//- Front-running: Transaction ordering vulnerabilities
//- Business Logic Flaws: Logic errors in contract operations

//3. QUBIC-SPECIFIC ANALYSIS POINTS:
//- Share balance verification before transfers
//- invocationReward validation and refund logic
//- Order book state consistency maintenance
//- Asset metadata integrity
//- Concurrent transaction handling
//- Cross-method state dependencies

//4. COMMON QUBIC ATTACK VECTORS:
//- Draining assets through manipulated orders
//- Fee evasion through reward manipulation
//- Share dilution attacks
//- Order book front-running
//- State corruption through concurrent access

//Return JSON array with this structure:
//[
//  {{
//    ""name"": ""Vulnerability name"",
//    ""type"": ""VulnerabilityType"",
//    ""severity"": ""Critical"",
//    ""description"": ""Detailed description"",
//    ""location"": ""Code location"",
//    ""impact"": ""Potential impact"",
//    ""exploitScenarios"": [""scenario1"", ""scenario2""],
//    ""recommendations"": [""recommendation1"", ""recommendation2""],
//    ""isConfirmed"": true,
//    ""qubicSpecific"": true
//  }}
//]";
//        }

//        private string CreateSecurityTestPrompt(ContractMethod method, string contractCode)
//        {
//            var methodJson = JsonSerializer.Serialize(method, _jsonOptions);

//            return $@"Generate security test cases with ACTUAL MALICIOUS VALUES for this Qubic method:

//CRITICAL REQUIREMENT: You must provide SPECIFIC MALICIOUS VALUES for each test case, not empty objects.

//QUBIC TESTING CONTEXT:
//Each test must target ONE specific input variable with a malicious value while providing valid values for other variables.

//METHOD TO TEST:
//{methodJson}

//INPUT VARIABLES AVAILABLE:
//{string.Join(", ", method.InputStruct.Select(kv => $"{kv.Key} ({kv.Value})"))}

//QUBIC ATTACK VALUE MAPPINGS:

//1. INTEGER OVERFLOW ATTACKS:
//   - uint8: ""255"" (MAX_UINT8)
//   - uint16: ""65535"" (MAX_UINT16)  
//   - uint32: ""4294967295"" (MAX_UINT32)
//   - uint64: ""18446744073709551615"" (MAX_UINT64)
//   - sint64: ""9223372036854775807"" (MAX_INT64)

//2. INTEGER UNDERFLOW ATTACKS:
//   - Any signed type: ""-1"", ""-999999999""
//   - Any unsigned type: ""-1"" (should cause underflow)

//3. ZERO VALUE ATTACKS:
//   - All numeric types: ""0""

//4. PUBLIC KEY ATTACKS:
//   - Invalid format: ""INVALID_KEY_FORMAT_TOO_SHORT""
//   - Null bytes: ""AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA""
//   - Wrong length: ""WRONG_LENGTH_KEY""
//   - All zeros: ""AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA""
//   - Malicious pattern: ""EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE""

//5. ARRAY ATTACKS:
//   - Empty array: []
//   - Oversized array: [1,2,3,...,999] (if array has size limit)
//   - Null elements: [null, null, null]

//6. VALID DEFAULT VALUES (for non-target fields):
//   - uint64: ""1000""
//   - id (PublicKey): ""CFBMEMZOIDEXQAUXYYSZIURADQLAPWPMNJXQSNVQZAHYVOPYUKKJBJUCTVJL""
//   - bool: true
//   - Arrays: appropriate default arrays

//ATTACK SCENARIOS BY VULNERABILITY TYPE:

//INTEGER OVERFLOW: Target numeric fields with MAX values
//INTEGER UNDERFLOW: Target signed fields with negative values  
//SHARE MANIPULATION: Target amount/share fields with 0, negative, or MAX values
//ACCESS CONTROL: Target id fields with invalid public keys
//INVALID INPUT: Target any field with malformed data
//REENTRANCY: Test with valid inputs but document the reentrancy concern
//DOS: Target with values that could cause excessive computation

//REQUIRED TEST CASE FORMAT:
//Generate 2-4 test cases for this method. Each test MUST include:

//1. targetInput with ACTUAL malicious value
//2. otherInputs with VALID default values for all other fields
//3. Clear explanation of the attack

//CONTRACT CONTEXT:
//{contractCode.Substring(0, Math.Min(1000, contractCode.Length))}...

//EXAMPLE OUTPUT FORMAT (follow this EXACTLY):
//[
//  {{
//    ""testName"": ""Integer Overflow Attack on Amount"",
//    ""methodName"": ""{method.Name}"",
//    ""targetVariable"": ""amount"",
//    ""description"": ""Tests integer overflow by providing maximum uint64 value"",
//    ""vulnerabilityType"": ""Integer Overflow"",
//    ""severity"": ""Critical"",
//    ""testInputs"": {{
//      ""targetInput"": {{
//        ""variableName"": ""amount"",
//        ""variableType"": ""uint64"",
//        ""maliciousValue"": ""18446744073709551615"",
//        ""attackReason"": ""Maximum uint64 value causes integer overflow in arithmetic operations""
//      }},
//      ""otherInputs"": {{
//        ""user"": ""CFBMEMZOIDEXQAUXYYSZIURADQLAPWPMNJXQSNVQZAHYVOPYUKKJBJUCTVJL""
//      }}
//    }},
//    ""expectedBehavior"": ""Should reject transaction with overflow protection"",
//    ""actualRisk"": ""Could manipulate balances or drain contract funds"",
//    ""mitigationSteps"": [""Add SafeMath library"", ""Implement bounds checking"", ""Validate input ranges before operations""]
//  }},
//  {{
//    ""testName"": ""Zero Amount Attack"",
//    ""methodName"": ""{method.Name}"",
//    ""targetVariable"": ""amount"",
//    ""description"": ""Tests zero amount handling"",
//    ""vulnerabilityType"": ""Share Manipulation"",
//    ""severity"": ""Medium"",
//    ""testInputs"": {{
//      ""targetInput"": {{
//        ""variableName"": ""amount"",
//        ""variableType"": ""uint64"",
//        ""maliciousValue"": ""0"",
//        ""attackReason"": ""Zero amounts can bypass business logic or waste gas""
//      }},
//      ""otherInputs"": {{
//        ""user"": ""CFBMEMZOIDEXQAUXYYSZIURADQLAPWPMNJXQSNVQZAHYVOPYUKKJBJUCTVJL""
//      }}
//    }},
//    ""expectedBehavior"": ""Should reject zero amount transactions"",
//    ""actualRisk"": ""Could waste contract resources or bypass validations"",
//    ""mitigationSteps"": [""Add minimum amount validation"", ""Implement zero-check guards""]
//  }}
//]

//CRITICAL INSTRUCTIONS:
//- testInputs MUST contain actual values, not empty objects
//- targetInput.maliciousValue MUST be a specific string value 
//- otherInputs MUST contain valid values for ALL other input fields
//- Generate tests that actually exploit the specific vulnerability type
//- Focus on values that would cause real security issues in Qubic

//INPUT FIELD ANALYSIS FOR {method.Name}:
//{string.Join("\n", method.InputStruct.Select(kv => $"- {kv.Key}: {kv.Value} (provide valid default if not target)"))}

//Generate test cases with REAL malicious values that can be used directly in transactions.";
//        }

//        private string CreateHelperGenerationPrompt(ContractMethod method, string contractName)
//        {
//            return $@"Generate helper functions in TypeScript for the {method.Name} method:

//QUBIC HELPERS CONTEXT:
//Qubic transactions often need:
//- Asset validation utilities
//- Fee calculation helpers
//- Order book state checkers
//- Share balance verifiers

//Include:
//1. Function to create payload easily
//2. Function to validate parameters specific to Qubic
//3. Function to calculate fees and invocationReward
//4. Function to execute transaction with proper error handling
//5. Asset/share validation utilities (if applicable)

//Generate TypeScript helper functions for {method.Name} method in contract {contractName}.";
//        }

//        private string CreateValidationGenerationPrompt(ContractMethod method)
//        {
//            return $@"Generate detailed validation rules for the {method.Name} method:

//QUBIC VALIDATION CONTEXT:
//Qubic contracts require validation for:
//- Asset existence and ownership
//- Share balance sufficiency  
//- Order book constraints
//- Fee amount correctness
//- Public key format validation

//Include validations for:
//1. Data types and ranges
//2. Qubic-specific constraints (asset IDs, share amounts)
//3. Business logic requirements
//4. Security constraints
//5. Order book rules (if applicable)

//Generate TypeScript validation functions for {method.Name} method.";
//        }

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