export interface TestExecutionConfig {
    qubicRpcUrl: string;
    contractAddress: string;
    testIdentity: string;
    testSeed: string;
    tickOffset: number;
    delayBetweenTests: number;
}
