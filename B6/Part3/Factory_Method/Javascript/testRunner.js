'use strict';

// Plain test runner, no framework. Prints one PASS/FAIL line per case and a
// final "PASS n/7"; exits with 1 if any case failed.
const {
  VehicleCategory,
  StandardCategoryRules,
  VehicleCategoryRulesFactory,
} = require('./vehicleCategoryRules');

const WEEKLY_REQUIRED = 'weeklyRate is required for COMMERCIAL car types';
const TOTAL = 7;

const factory = new VehicleCategoryRulesFactory();
let passed = 0;
let number = 0;

function check(description, test) {
  number += 1;
  let ok;
  try {
    ok = test() === true;
  } catch (e) {
    ok = false;
  }
  if (ok) {
    passed += 1;
  }
  console.log(`${ok ? 'PASS' : 'FAIL'} ${number} ${description}`);
}

check('STANDARD: standard rules, deposit(100) = 0, no rate errors', () => {
  const rules = factory.create(VehicleCategory.STANDARD);
  return rules instanceof StandardCategoryRules
    && rules.calculateSecurityDeposit(100) === 0
    && rules.validateRates(100, null, null).length === 0;
});

check('ELECTRIC: deposit(60) = 120, no rate errors', () => {
  const rules = factory.create(VehicleCategory.ELECTRIC);
  return rules.calculateSecurityDeposit(60) === 120
    && rules.validateRates(60, null, null).length === 0;
});

check('COMMERCIAL: deposit(80) = 400', () =>
  factory.create(VehicleCategory.COMMERCIAL).calculateSecurityDeposit(80) === 400);

check('COMMERCIAL: missing weeklyRate gives exactly one error', () => {
  const errors = factory.create(VehicleCategory.COMMERCIAL).validateRates(80, null, null);
  return errors.length === 1 && errors[0] === WEEKLY_REQUIRED;
});

check('COMMERCIAL: weeklyRate 500 gives no errors', () =>
  factory.create(VehicleCategory.COMMERCIAL).validateRates(80, 500, null).length === 0);

check('every category: create(category).category() equals that category', () =>
  Object.values(VehicleCategory).every((category) => factory.create(category).category() === category));

check("unknown category ('HOVERCRAFT') is rejected with an error", () => {
  try {
    factory.create('HOVERCRAFT');
    return false;
  } catch (e) {
    return e instanceof RangeError;
  }
});

console.log(`PASS ${passed}/${TOTAL}`);
process.exitCode = passed === TOTAL && number === TOTAL ? 0 : 1;
