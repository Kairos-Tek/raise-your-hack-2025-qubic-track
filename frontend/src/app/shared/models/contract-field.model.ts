export interface ContractField {
    name: string;
    qubicType: string;
    typeScriptType: string;
    byteSize: number;
    order: number;
    isArray: boolean;
    arraySize?: number;
    description?: string;
    isRequired: boolean;
    defaultValue?: any;
    validations: string[];
}
