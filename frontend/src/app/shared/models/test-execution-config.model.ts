export interface TestExecutionConfig {
    qubicApiUrl: string;
    contractAddress: string;
    testIdentity: string;
    testSeed: string;
    tickOffset: number;
    delayBetweenTests: number;
}
