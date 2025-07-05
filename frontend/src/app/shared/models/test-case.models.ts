
export interface TestCase {
  id: string;
  methodId: string;
  description: string;              // Descripción de qué prueba este caso
  riskLevel: 'low' | 'medium' | 'high';
  category: 'boundary' | 'overflow' | 'logic' | 'access' | 'reentrancy';
  testValues: TestValue[];
  result?: TestResult;              // Resultado después de ejecutar
}

export interface TestValue {
  id: string;
  testCaseId: string;
  variableId: string;               // Relación con MethodVariable
  value: string;                    // Valor a enviar (siempre string, se parsea según el tipo)
  description: string;              // Por qué se eligió este valor
}

export interface TestResult {
  id: string;
  testCaseId: string;
  status: 'pending' | 'running' | 'success' | 'failed' | 'error';
  executionTime: number;            // Tiempo en ms
  gasUsed?: number;                 // Gas simulado
  error?: string;                   // Error si falló
  returnValue?: string;             // Valor retornado si aplica
  fakeTransactionHash?: string;     // Hash fake de la transacción
  executedAt?: Date;
}