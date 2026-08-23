import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export interface DecimalPrecisionError {
  maxIntegerDigits: number;
  maxDecimalPlaces: number;
  actualIntegerDigits: number;
  actualDecimalPlaces: number;
}

export function decimalPrecision(
  maxIntegerDigits: number,
  maxDecimalPlaces: number
): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;

    if (value === null || value === undefined || value === '') {
      return null;
    }

    const text = String(value).trim().toLowerCase();
    const match = /^[-+]?(\d+(?:\.\d*)?|\.\d+)(?:e([-+]?\d+))?$/.exec(text);
    if (!match || !Number.isFinite(Number(value))) {
      return {
        decimalPrecision: {
          maxIntegerDigits,
          maxDecimalPlaces,
          actualIntegerDigits: 0,
          actualDecimalPlaces: 0
        } satisfies DecimalPrecisionError
      };
    }

    const coefficient = match[1].replace(/^[-+]/, '');
    const exponent = Number(match[2] ?? 0);
    const [whole = '', fraction = ''] = coefficient.split('.');
    const digits = `${whole}${fraction}`;
    const decimalIndex = whole.length + exponent;

    // Evita expansões gigantes para entradas científicas que arredondam para zero
    // em JavaScript (por exemplo, 1e-999999999).
    if (!Number.isSafeInteger(exponent) || Math.abs(exponent) > digits.length + maxIntegerDigits + maxDecimalPlaces) {
      return {
        decimalPrecision: {
          maxIntegerDigits,
          maxDecimalPlaces,
          actualIntegerDigits: exponent > 0 ? maxIntegerDigits + 1 : 1,
          actualDecimalPlaces: exponent < 0 ? maxDecimalPlaces + 1 : 0
        } satisfies DecimalPrecisionError
      };
    }

    let expandedWhole: string;
    let expandedFraction: string;
    if (decimalIndex <= 0) {
      expandedWhole = '0';
      expandedFraction = `${'0'.repeat(-decimalIndex)}${digits}`;
    } else if (decimalIndex >= digits.length) {
      expandedWhole = `${digits}${'0'.repeat(decimalIndex - digits.length)}`;
      expandedFraction = '';
    } else {
      expandedWhole = digits.slice(0, decimalIndex);
      expandedFraction = digits.slice(decimalIndex);
    }

    const significantFraction = expandedFraction.replace(/0+$/, '');
    const actualIntegerDigits = Math.max(1, expandedWhole.replace(/^0+/, '').length);
    const actualDecimalPlaces = significantFraction.length;

    if (
      actualIntegerDigits <= maxIntegerDigits &&
      actualDecimalPlaces <= maxDecimalPlaces
    ) {
      return null;
    }

    return {
      decimalPrecision: {
        maxIntegerDigits,
        maxDecimalPlaces,
        actualIntegerDigits,
        actualDecimalPlaces
      } satisfies DecimalPrecisionError
    };
  };
}
