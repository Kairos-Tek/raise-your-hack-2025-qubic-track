import { ContractField } from './contract-field.model';
import { SecurityTestCase } from './security-test-case.model';

export interface ContractMethod {
    id: string;
    contractAnalysisId: string;
    name: string;
    type: string; // FUNCTION or PROCEDURE
    procedureIndex?: number;
    inputStructRaw: any;
    outputStructRaw: any;
    inputFields: ContractField[];
    outputFields: ContractField[];
    fees: { [key: string]: any };
    validations: string[];
    description: string;
    isAssetRelated: boolean;
    isOrderBookRelated: boolean;
    createdAt: string;
    packageSize: number;
    securityTestCases: SecurityTestCase[];

    // Legacy properties for backward compatibility
    inputStruct?: { [key: string]: string };
    outputStruct?: { [key: string]: string };
}
