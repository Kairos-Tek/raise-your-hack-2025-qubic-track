import { TestCase } from "./test-case.models";

export interface Contract {
  id: string;
  fileName: string;
  contractCode: string;
  summary: string;                    // Análisis general de la IA
  vulnerabilities: string[];          // Lista simple de vulnerabilidades detectadas
  methods: ContractMethod[];
  fakeContractAddress?: string;       // Dirección fake para el deploy simulado
  fakeTransactionHash?: string;       // Hash fake del deploy
  createdAt: Date;
}

export interface ContractMethod {
  id: string;
  contractId: string;
  name: string;
  signature: string;
  description: string;               // Explicación de la IA sobre qué hace el método
  variables: MethodVariable[];
  testCases: TestCase[];
}

export interface MethodVariable {
  id: string;
  methodId: string;
  name: string;
  type: string;                     // uint256, address, string, etc.
  description: string;              // Descripción de la variable
  position: number;                 // Orden en los parámetros del método
}
