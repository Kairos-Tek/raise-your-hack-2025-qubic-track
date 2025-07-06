using RYH2025_Qubic.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RYH2025_Qubic.Services
{
    public class Prompts
    {
        private static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

        public static string GetSystemMessage()
        {
            return @"You are an expert in security testing for Qubic smart contracts.
CRITICAL INSTRUCTIONS:
- Respond with raw JSON array only
- No markdown code blocks
- No explanations or comments
- Start response directly with [ character
- Ensure valid JSON syntax";
        }
        public static string CreateSecurityTestPrompt(ContractMethod method, string contractCode)
        {
            var methodJson = JsonSerializer.Serialize(method, JsonOptions);
            var hasInputParams = method.InputStruct.Any();

            return $@"Generate security test cases with ACTUAL MALICIOUS VALUES for this Qubic method:

CRITICAL REQUIREMENT: You must ONLY use the ACTUAL INPUT PARAMETERS of this method. Do NOT create fake parameters.

METHOD TO TEST:
{methodJson}

INPUT PARAMETERS ANALYSIS FOR {method.Name}:
Method Type: {method.Type}
Has Input Parameters: {(hasInputParams ? "YES" : "NO")}
Input Count: {method.InputStruct.Count}
{(hasInputParams ?
                $"Available Parameters:\n{string.Join("\n", method.InputStruct.Select(kv => $"- {kv.Key}: {kv.Value}"))}" :
                "- NO INPUT PARAMETERS AVAILABLE - This method receives an empty struct")}

IMPORTANT RULES:
1. If the method has NO INPUT PARAMETERS (empty InputStruct), then:
   - Set testInputs to empty object {{}}
   - Focus the test description on internal logic vulnerabilities
   - Mark the test as ""Internal Logic"" or ""State Access"" vulnerability type
   - Lower the severity since no external input can be manipulated

2. If the method HAS INPUT PARAMETERS:
   - ONLY target the actual input parameters listed above
   - Each test case must target ONE specific input parameter
   - Provide valid default values for all OTHER input parameters
   - Use the malicious values specified below

3. NEVER invent parameters that don't exist in the method signature

{(hasInputParams ?
            $@"MALICIOUS VALUE MAPPINGS (use ONLY for targeting actual input parameters):
- uint8: ""255"" (MAX_UINT8)
- uint16: ""65535"" (MAX_UINT16)  
- uint32: ""4294967295"" (MAX_UINT32)
- uint64: ""18446744073709551615"" (MAX_UINT64)
- sint64: ""9223372036854775807"" (MAX_INT64)
- Underflow: ""-1"" for any numeric type
- Zero attack: ""0"" for any numeric type
- PublicKey attack: ""INVALID_KEY_FORMAT_TOO_SHORT""

VALID DEFAULT VALUES (for non-target fields):
- uint64: ""1000""
- uint32: ""100""
- uint16: ""10""
- uint8: ""1""
- id (PublicKey): ""CFBMEMZOIDEXQAUXYYSZIURADQLAPWPMNJXQSNVQZAHYVOPYUKKJBJUCTVJL""
- bool: true
- sint64: ""1000"""
            :
            @"INTERNAL LOGIC TESTING:
Since this method has no input parameters, focus on internal vulnerabilities like:
- State corruption through repeated calls
- Race conditions in state access
- Integer overflow in internal calculations
- Unauthorized access to contract state")}

EXPECTED OUTPUT FORMAT:

{(hasInputParams ?
            $@"For methods WITH input parameters (THIS METHOD HAS {method.InputStruct.Count} PARAMETERS):
[
  {{
    ""testName"": ""Integer Overflow Attack on [ACTUAL_PARAM_NAME]"",
    ""methodName"": ""{method.Name}"",
    ""targetVariable"": ""[ACTUAL_PARAM_NAME]"",
    ""description"": ""Tests integer overflow by providing maximum value to [ACTUAL_PARAM_NAME]"",
    ""vulnerabilityType"": ""Integer Overflow"",
    ""severity"": ""Critical"",
    ""testInputs"": {{
      ""targetInput"": {{
        ""variableName"": ""[ACTUAL_PARAM_NAME]"",
        ""variableType"": ""uint64"",
        ""maliciousValue"": ""18446744073709551615"",
        ""attackReason"": ""Maximum uint64 value causes integer overflow""
      }},
      ""otherInputs"": {{
        ""[OTHER_ACTUAL_PARAM]"": ""valid_default_value""
      }}
    }},
    ""expectedBehavior"": ""Should reject transaction with overflow protection"",
    ""actualRisk"": ""Could manipulate balances or drain contract funds"",
    ""mitigationSteps"": [""Add SafeMath library"", ""Implement bounds checking""]
  }}
]

INPUT FIELD ANALYSIS FOR {method.Name}:
{string.Join("\n", method.InputStruct.Select(kv => $"- {kv.Key}: {kv.Value} (ACTUAL PARAMETER - use this)"))}

GENERATE {Math.Min(3, method.InputStruct.Count)} TEST CASES targeting the ACTUAL parameters listed above."
            :
            $@"For methods WITHOUT input parameters (THIS METHOD HAS 0 PARAMETERS):
[
  {{
    ""testName"": ""Internal Logic Vulnerability - {method.Name}"",
    ""methodName"": ""{method.Name}"",
    ""targetVariable"": ""internal_state"",
    ""description"": ""Tests internal logic vulnerabilities in {method.Name} which accepts no external inputs"",
    ""vulnerabilityType"": ""Internal Logic"",
    ""severity"": ""Medium"",
    ""testInputs"": {{}},
    ""expectedBehavior"": ""Should handle internal operations safely"",
    ""actualRisk"": ""Internal arithmetic operations could cause overflow or unexpected behavior"",
    ""mitigationSteps"": [""Review internal arithmetic operations"", ""Add bounds checking for state variables""]
  }},
  {{
    ""testName"": ""State Race Condition - {method.Name}"",
    ""methodName"": ""{method.Name}"",
    ""targetVariable"": ""contract_state"",
    ""description"": ""Tests for race conditions when {method.Name} is called concurrently"",
    ""vulnerabilityType"": ""Race Condition"",
    ""severity"": ""Medium"",
    ""testInputs"": {{}},
    ""expectedBehavior"": ""Should handle concurrent access safely"",
    ""actualRisk"": ""Concurrent calls could corrupt contract state"",
    ""mitigationSteps"": [""Implement proper state locking"", ""Use atomic operations""]
  }}
]

GENERATE 1-2 INTERNAL LOGIC TEST CASES with EMPTY testInputs object.")}

CONTRACT CONTEXT FOR INTERNAL ANALYSIS:
{contractCode.Substring(0, Math.Min(1000, contractCode.Length))}...

CRITICAL INSTRUCTIONS:
- Respond with raw JSON array only
- No markdown code blocks
- No explanations or comments
- Start response directly with [ character
- Ensure valid JSON syntax
- If method has NO input parameters: Generate tests with empty testInputs {{}}
- If method has input parameters: Generate tests targeting ONLY those actual parameters
- NEVER create tests for non-existent parameters

FINAL VALIDATION:
- Method {method.Name} has {method.InputStruct.Count} input parameters
- {(hasInputParams ? "Generate tests with targetInput and otherInputs" : "Generate tests with empty testInputs object")}
- Only use parameters that exist in the method signature";
        }

        public static string CreateContractAnalysisPrompt(string contractCode)
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
- Use camelCase for field names in JSON (name, qubicType, typeScriptType, etc.)
- Extract procedureIndex from REGISTER_USER_FUNCTION/PROCEDURE calls in the contract

CRITICAL INSTRUCTIONS:
- Respond with raw JSON only
- No markdown code blocks 
- No explanations or comments
- Start response directly with {{ character (NOT [ character)
- Return a SINGLE OBJECT, not an array
- Analyze ALL public methods found in the contract (don't assume specific names)

Return JSON in this EXACT format (SINGLE OBJECT):
{{
  ""contractName"": ""ExtractedContractName"",
  ""namespace"": ""ExtractedNamespace"",
  ""methods"": [
    {{
      ""name"": ""methodName"",
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

        public static string CreateSecurityAuditPrompt(string contractCode, ContractAnalysis analysis)
        {
            var analysisJson = JsonSerializer.Serialize(analysis, Prompts.JsonOptions);

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

    }
}
