import { Buffer } from 'buffer';
import { PUBLIC_KEY_LENGTH } from '@qubic-lib/qubic-ts-library/dist/crypto';
import { QubicHelper } from '@qubic-lib/qubic-ts-library/dist/qubicHelper';

// Types
interface InputField {
    name: string;
    type: string;
    elementType?: string;
    size?: string | number;
}

// Helper function to encode a single value based on type
function encodeValue(value: any, type: string, qHelper?: QubicHelper, size: number = 0): Buffer {
    let buffer: Buffer;
    const isAmountField = false; // Assuming amount scaling is handled elsewhere
    const scaleAmount = (val: any): bigint => BigInt(val || 0) * 1000000n;

    if (type === 'uint8') {
        buffer = Buffer.alloc(1);
        buffer.writeUInt8(parseInt(value || 0), 0);
    } else if (type === 'uint16') {
        buffer = Buffer.alloc(2);
        buffer.writeUInt16LE(parseInt(value || 0), 0);
    } else if (type === 'uint32') {
        buffer = Buffer.alloc(4);
        buffer.writeUInt32LE(parseInt(value || 0), 0);
    } else if (type === 'uint64') {
        buffer = Buffer.alloc(8);
        try {
            const valToEncode = isAmountField ? scaleAmount(value) : BigInt(value || 0);
            // Usar writeBigUInt64LE si está disponible, sino usar writeUInt32LE para compatibilidad
            if (typeof buffer.writeBigUInt64LE === 'function') {
                buffer.writeBigUInt64LE(valToEncode, 0);
            } else {
                // Fallback para versiones antiguas - dividir BigInt en dos uint32
                const num = Number(valToEncode);
                if (num <= 0xffffffff) {
                    buffer.writeUInt32LE(num, 0);
                    buffer.writeUInt32LE(0, 4);
                } else {
                    buffer.writeUInt32LE(num & 0xffffffff, 0);
                    buffer.writeUInt32LE(Math.floor(num / 0x100000000), 4);
                }
            }
        } catch (error) {
            console.warn(`Error writing uint64 for ${type}:`, error, 'Using 0 as fallback');
            buffer.writeUInt32LE(0, 0);
            buffer.writeUInt32LE(0, 4);
        }
    } else if (type === 'bit' || type === 'bool') {
        buffer = Buffer.alloc(1);
        buffer.writeUInt8(value ? 1 : 0, 0);
    } else if (type === 'int8' || type === 'sint8') {
        buffer = Buffer.alloc(1);
        buffer.writeInt8(parseInt(value || 0), 0);
    } else if (type === 'int16' || type === 'sint16') {
        buffer = Buffer.alloc(2);
        buffer.writeInt16LE(parseInt(value || 0), 0);
    } else if (type === 'int32' || type === 'sint32') {
        buffer = Buffer.alloc(4);
        buffer.writeInt32LE(parseInt(value || 0), 0);
    } else if (type === 'int64' || type === 'sint64') {
        buffer = Buffer.alloc(8);
        try {
            const valToEncode = isAmountField ? scaleAmount(value) : BigInt(value || 0);
            // Usar writeBigInt64LE si está disponible, sino usar writeInt32LE para compatibilidad
            if (typeof buffer.writeBigInt64LE === 'function') {
                buffer.writeBigInt64LE(valToEncode, 0);
            } else {
                // Fallback para versiones antiguas - dividir BigInt en dos int32
                const num = Number(valToEncode);
                if (num >= -0x80000000 && num <= 0x7fffffff) {
                    buffer.writeInt32LE(num, 0);
                    buffer.writeInt32LE(num < 0 ? -1 : 0, 4); // Extensión de signo
                } else {
                    buffer.writeInt32LE(num & 0xffffffff, 0);
                    buffer.writeInt32LE(Math.floor(num / 0x100000000), 4);
                }
            }
        } catch (error) {
            console.warn(`Error writing int64 for ${type}:`, error, 'Using 0 as fallback');
            buffer.writeInt32LE(0, 0);
            buffer.writeInt32LE(0, 4);
        }
    } else if (type === 'id') {
        buffer = Buffer.alloc(PUBLIC_KEY_LENGTH); // 32 bytes
        if (!qHelper) {
            console.error('qHelper instance not provided to encodeValue for ID type!');
            buffer.fill(0);
        } else {
            try {
                const idBytes = qHelper.getIdentityBytes(value);
                if (idBytes && idBytes.length === PUBLIC_KEY_LENGTH) {
                    Buffer.from(idBytes).copy(buffer);
                } else {
                    console.warn(`Invalid ID format or length for value: ${value}. Using zero buffer.`);
                    buffer.fill(0);
                }
            } catch (error) {
                console.error(`Error converting ID "${value}" to bytes:`, error);
                buffer.fill(0);
            }
        }
    } else if (type.startsWith('char[')) {
        const sizeMatch = type.match(/\[(\d+)\]/);
        const charSize = sizeMatch ? parseInt(sizeMatch[1], 10) : size || 64;
        buffer = Buffer.alloc(charSize);
        const strBuffer = Buffer.from(String(value || ''), 'utf-8');
        strBuffer.copy(buffer, 0, 0, Math.min(strBuffer.length, charSize));
        if (strBuffer.length < charSize) {
            buffer.fill(0, strBuffer.length);
        }
    } else {
        console.warn(`Unsupported type for encoding: ${type}. Returning empty buffer.`);
        buffer = Buffer.alloc(0);
    }
    return buffer;
}

// Encode parameters for contract calls
export function encodeParams(params: Record<string, any>, inputFields: InputField[] = []): string {
    try {
        const qHelper = new QubicHelper();

        if (!params || Object.keys(params).length === 0 || inputFields.length === 0) return '';

        const buffers: Buffer[] = [];
        let totalSize = 0;

        // Resolve constants like MSVAULT_MAX_OWNERS
        const resolveSize = (sizeStr: string | number): number => {
            if (typeof sizeStr === 'number') return sizeStr;
            if (/^\d+$/.test(sizeStr)) return parseInt(sizeStr, 10);

            const knownConstants: Record<string, number> = {
                MSVAULT_MAX_OWNERS: 16,
                MSVAULT_MAX_COOWNER: 8,
                '1024': 1024,
            };
            return knownConstants[sizeStr] || 0;
        };

        inputFields.forEach((field) => {
            const value = params[field.name];

            if (field.type === 'Array') {
                let items: any[] = [];
                if (typeof value === 'string') {
                    try {
                        items = JSON.parse(value);
                    } catch (e) {
                        console.error(`Invalid JSON for array field ${field.name}:`, value);
                        items = [];
                    }
                } else if (Array.isArray(value)) {
                    items = value;
                }

                const arraySize = field.size ? resolveSize(field.size) : 0;
                if (arraySize === 0) {
                    console.warn(`Could not resolve size for array ${field.name} (size: ${field.size}). Skipping field.`);
                    return;
                }

                for (let i = 0; i < arraySize; i++) {
                    const itemValue = items[i] !== undefined ? items[i] : null;
                    const itemBuffer = encodeValue(itemValue, field.elementType || '', qHelper);
                    buffers.push(itemBuffer);
                    totalSize += itemBuffer.length;
                }
            } else if (field.type === 'ProposalDataT' || field.type === 'ProposalDataYesNo') {
                console.warn(`Complex type ${field.type} encoding not fully implemented here.`);
                const complexBuffer = Buffer.alloc(0);
                buffers.push(complexBuffer);
                totalSize += complexBuffer.length;
            } else {
                const fieldBuffer = encodeValue(value, field.type, qHelper);
                buffers.push(fieldBuffer);
                totalSize += fieldBuffer.length;
            }
        });

        const finalBuffer = Buffer.concat(buffers, totalSize);

        // Usar btoa nativo del navegador en lugar de base64 library
        const uint8Array = new Uint8Array(finalBuffer);
        const binaryString = String.fromCharCode(...uint8Array);
        return btoa(binaryString);
    } catch (error) {
        console.error('Error encoding parameters:', error);
        return '';
    }
}
