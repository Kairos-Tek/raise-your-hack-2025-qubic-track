export interface BatchExecution {
  id: string;
  contractId: string;
  status: 'running' | 'completed' | 'failed';
  progress: {
    completed: number;
    total: number;
    percentage: number;
  };
  currentMethodId?: string;         // Método que se está ejecutando actualmente
  startedAt: Date;
  completedAt?: Date;
}

export interface ExecutionSummary {
  executionId: string;
  contractId: string;
  totalTests: number;
  successful: number;
  failed: number;
  errors: number;
  duration: number;                 // Duración total en segundos
  methodSummaries: MethodExecutionSummary[];
}

export interface MethodExecutionSummary {
  methodId: string;
  methodName: string;
  totalTests: number;
  successful: number;
  failed: number;
  errors: number;
  criticalIssuesFound: number;
}