import { FP as FP1 } from '../../lib-public/types/fa1.types';
import { FP as FP2 } from '../../lib-public/types/fa2.types';
import { FP as FP3 } from '../../lib-public/types/fa3.types';

export type ObjectKeysOfFP = Record<string, FP1 | FP2 | FP3>;

export type TypesOfValues = FP1 | FP2 | FP3 | string | number | undefined;

export type FilteredKeysOfValues<T> = {
  [P in keyof T]: T[P] extends TypesOfValues ? P : never;
}[keyof T];

export interface DifferentValues {
  value: string | number;
  count: number;
}
