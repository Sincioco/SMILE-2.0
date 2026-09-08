#include <cmath>
#include <charconv>
#include <locale.h>
#include <stdlib.h>
#include <string>
#include <system_error>

extern "C" {
void smile_print_text(const char*, long long);
void smile_print_newline(void);
void* smile_text_from_utf8(const char*, long long);
const char* smile_text_utf8(void*);
long long smile_text_byte_length(void*);
void smile_text_release(void*);
}

// Internal status never becomes a source value. The emitter checks it before
// committing a destination and enters the existing staged/frame cleanup path.
static int double_error;
static double checked(double value)
{
    if (!std::isfinite(value)) { double_error = 1; return 0.0; }
    return value;
}

extern "C" long long smile_double_status(void) { return double_error; }
extern "C" void smile_double_report(const char* location, long long length)
{
    smile_print_text(location, length);
    const char* message = double_error == 2 ? "Double division by zero."
        : double_error == 3 ? "Double domain error."
        : double_error == 4 ? "Double conversion out of range."
        : double_error == 5 ? "Invalid Double text." : "Nonfinite Double result.";
    smile_print_text(message, static_cast<long long>(std::char_traits<char>::length(message)));
    smile_print_newline();
}

extern "C" double smile_to_double(long long value)
{
    double_error = 0;
    return static_cast<double>(value);
}

extern "C" long long smile_to_number(double value)
{
    double_error = 0;
    const double truncated = std::trunc(value);
    if (!std::isfinite(value) || truncated < -9223372036854775808.0 || truncated >= 9223372036854775808.0)
    { double_error = 4; return 0; }
    return static_cast<long long>(truncated);
}

// Position 0 is the operation; Windows x64 places a/b/c in xmm1/xmm2/xmm3.
// Each operation is evaluated separately, with no contraction or reassociation.
extern "C" double smile_double_math(long long operation, double a, double b, double c)
{
    double_error = 0;
    switch (operation)
    {
    case 1: return checked(a + b);
    case 2: return checked(a - b);
    case 3: return checked(a * b);
    case 4:
        if (b == 0.0) { double_error = 2; return 0.0; }
        return checked(a / b);
    case 5: return std::fabs(a);
    // Keep a in the tie arm: MSVC lowers these to MINSD/MAXSD, whose second
    // operand wins a signed-zero tie even with /fp:strict.
    case 6: return b < a ? b : a;
    case 7: return b > a ? b : a;
    case 8:
        if (b > c) { double_error = 3; return 0.0; }
        return a < b ? b : a > c ? c : a;
    case 9:
        if (a < 0.0) { double_error = 3; return 0.0; }
        return checked(std::sqrt(a));
    case 10: return checked(std::sin(a));
    case 11: return checked(std::cos(a));
    case 12: return checked(std::atan2(a, b));
    case 13: return std::floor(a);
    case 14: return std::ceil(a);
    case 15: return std::trunc(a);
    case 16:
    {
        if (std::fabs(a) >= 4503599627370496.0) return a;
        const double lower = std::floor(a), fraction = a - lower;
        const double result = fraction < 0.5 ? lower : fraction > 0.5 ? lower + 1.0
            : std::fmod(lower, 2.0) == 0.0 ? lower : lower + 1.0;
        return result == 0.0 ? std::copysign(0.0, a) : result;
    }
    default: double_error = 3; return 0.0;
    }
}

static std::string format(double value)
{
    if (value == 0.0) return std::signbit(value) ? "-0.0" : "0.0";
    char buffer[64];
    const auto result = std::to_chars(buffer, buffer + sizeof(buffer), value);
    std::string text(buffer, result.ptr);
    if (text.find_first_of(".eE") == std::string::npos) text += ".0";
    return text;
}

extern "C" void smile_print_double(double value)
{
    const auto text = format(value);
    smile_print_text(text.data(), static_cast<long long>(text.size()));
}

extern "C" void* smile_text_from_double(double value)
{
    double_error = 0;
    if (!std::isfinite(value)) { double_error = 1; return nullptr; }
    const auto text = format(value);
    return smile_text_from_utf8(text.data(), static_cast<long long>(text.size()));
}

static bool whitespace(char value) { return value == ' ' || value >= '\t' && value <= '\r'; }
static bool digit(char value) { return value >= '0' && value <= '9'; }

static bool decimal(const std::string& text)
{
    size_t index = 0;
    while (index < text.size() && whitespace(text[index])) ++index;
    if (index < text.size() && (text[index] == '+' || text[index] == '-')) ++index;
    auto start = index;
    while (index < text.size() && digit(text[index])) ++index;
    if (index == start) return false;
    if (index < text.size() && text[index] == '.')
    {
        start = ++index;
        while (index < text.size() && digit(text[index])) ++index;
        if (index == start) return false;
    }
    if (index < text.size() && (text[index] == 'e' || text[index] == 'E'))
    {
        ++index;
        if (index < text.size() && (text[index] == '+' || text[index] == '-')) ++index;
        start = index;
        while (index < text.size() && digit(text[index])) ++index;
        if (index == start) return false;
    }
    while (index < text.size() && whitespace(text[index])) ++index;
    return index == text.size();
}

extern "C" double smile_text_to_double(void* owned)
{
    double_error = 0;
    const std::string text(smile_text_utf8(owned), static_cast<size_t>(smile_text_byte_length(owned)));
    smile_text_release(owned);
    if (!decimal(text)) { double_error = 5; return 0.0; }
    const auto locale = _create_locale(LC_NUMERIC, "C");
    const double result = _strtod_l(text.c_str(), nullptr, locale);
    _free_locale(locale);
    if (!std::isfinite(result)) { double_error = 5; return 0.0; }
    return result;
}
