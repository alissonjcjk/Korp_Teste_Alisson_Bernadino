import { FormControl } from '@angular/forms';
import { decimalPrecision } from './decimal.validator';

describe('decimalPrecision', () => {
  const validator = decimalPrecision(14, 4);

  it('accepts the exact numeric(18,4) digit boundaries', () => {
    expect(validator(new FormControl('99999999999999.9999'))).toBeNull();
    expect(validator(new FormControl('0.0001'))).toBeNull();
  });

  it('rejects values beyond either integer or decimal precision', () => {
    expect(validator(new FormControl('100000000000000'))).toEqual({
      decimalPrecision: {
        maxIntegerDigits: 14,
        maxDecimalPlaces: 4,
        actualIntegerDigits: 15,
        actualDecimalPlaces: 0
      }
    });
    expect(validator(new FormControl('0.00001'))).toEqual({
      decimalPrecision: {
        maxIntegerDigits: 14,
        maxDecimalPlaces: 4,
        actualIntegerDigits: 1,
        actualDecimalPlaces: 5
      }
    });
  });

  it('accounts for scientific notation without rejecting empty optional values', () => {
    expect(validator(new FormControl('1e13'))).toBeNull();
    expect(validator(new FormControl('1e14'))).not.toBeNull();
    expect(validator(new FormControl('1e-5'))).not.toBeNull();
    expect(validator(new FormControl(null))).toBeNull();
  });

  it('ignores insignificant trailing zeros and safely rejects extreme exponents', () => {
    expect(validator(new FormControl('1.00000'))).toBeNull();
    expect(validator(new FormControl('99999999999999.99990'))).toBeNull();
    expect(validator(new FormControl('1e-999999999'))).not.toBeNull();
  });
});
