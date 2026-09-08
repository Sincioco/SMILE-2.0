// Distinct SMILE binary64 operations; integer Number and Enum helpers are unchanged.
function doubleFinite(value) {
    if (typeof value !== "number" || !Number.isFinite(value))
        throw new Error("Nonfinite Double result.");
    return value;
}
function doubleMath(operation, a, b = 0.0, c = 0.0) {
    doubleFinite(a); doubleFinite(b); doubleFinite(c);
    let result;
    switch (operation) {
        case 1: result = a + b; break;
        case 2: result = a - b; break;
        case 3: result = a * b; break;
        case 4:
            if (b === 0) throw new Error("Double division by zero.");
            result = a / b; break;
        case 5: result = Math.abs(a); break;
        case 6: result = a <= b ? a : b; break;
        case 7: result = a >= b ? a : b; break;
        case 8:
            if (b > c) throw new Error("Double domain error.");
            result = a < b ? b : a > c ? c : a; break;
        case 9:
            if (a < 0) throw new Error("Double domain error.");
            result = Math.sqrt(a); break;
        case 10: result = Math.sin(a); break;
        case 11: result = Math.cos(a); break;
        case 12: result = Math.atan2(a, b); break;
        case 13: result = Math.floor(a); break;
        case 14: result = Math.ceil(a); break;
        case 15: result = Math.trunc(a); break;
        case 16: {
            if (Math.abs(a) >= 4503599627370496) return a;
            const lower = Math.floor(a), fraction = a - lower;
            result = fraction < 0.5 ? lower : fraction > 0.5 ? lower + 1
                : lower % 2 === 0 ? lower : lower + 1;
            if (result === 0 && (a < 0 || Object.is(a, -0))) result = -0;
            break;
        }
        default: throw new Error("Double domain error.");
    }
    return doubleFinite(result);
}
function toDouble(value) { return safe(value); }
function toNumber(value) {
    doubleFinite(value);
    const result = Math.trunc(value);
    if (!Number.isSafeInteger(result)) throw new Error("Double conversion out of range.");
    return result === 0 ? 0 : result;
}
function textFromDouble(value) {
    doubleFinite(value);
    if (Object.is(value, -0)) return "-0.0";
    const text = String(value);
    return /[.eE]/.test(text) ? text : text + ".0";
}
function textToDouble(text) {
    if (typeof text !== "string" ||
        !/^[\x09-\x0D ]*[+-]?[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?[\x09-\x0D ]*$/.test(text))
        throw new Error("Invalid Double text.");
    const result = Number(text);
    if (!Number.isFinite(result)) throw new Error("Invalid Double text.");
    return result;
}
