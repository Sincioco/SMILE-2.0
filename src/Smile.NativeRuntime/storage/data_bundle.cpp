#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdint.h>
#include <string>
#include <vector>
#include <set>
#include <cstring>

extern "C" int smile_storage_data_path(const char*, long long, WCHAR*, int);
extern "C" void smile_sha_bytes(const unsigned char*, SIZE_T, unsigned char*);

namespace {
using Bytes = std::vector<unsigned char>;
constexpr size_t RecordLimit = 1048620;
constexpr size_t BundleLimit = 64 * 1024 * 1024;
struct Record { std::string suffix; Bytes bytes; };

uint32_t number(const unsigned char* value)
{
    uint32_t result;
    memcpy(&result, value, 4);
    return result;
}

void append(Bytes& bytes, uint32_t value)
{
    const auto* start = reinterpret_cast<const unsigned char*>(&value);
    bytes.insert(bytes.end(), start, start + 4);
}

bool envelope(const unsigned char* bytes, size_t count)
{
    if (count < 44 || count > RecordLimit || memcmp(bytes, "SMD4", 4) ||
        number(bytes + 4) != 1 || number(bytes + 8) != count - 44) return false;
    unsigned char digest[32];
    smile_sha_bytes(bytes + 44, count - 44, digest);
    return !memcmp(digest, bytes + 12, 32);
}

bool suffixValid(const std::string& suffix)
{
    if (suffix.empty() || suffix.size() > 128 || suffix[0] != '.') return false;
    for (char ch : suffix)
        if (!(ch == '.' || ch == '_' || ch == '-' || (ch >= '0' && ch <= '9') ||
            (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'))) return false;
    return true;
}

std::wstring stored(const std::string& key)
{
    WCHAR path[2048] = {};
    if (!smile_storage_data_path(key.data(), key.size(), path, 2048)) return L"";
    return path;
}

DWORD read(const std::wstring& path, Bytes& bytes, size_t limit)
{
    HANDLE file = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE,
        nullptr, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, nullptr);
    if (file == INVALID_HANDLE_VALUE) return GetLastError();
    LARGE_INTEGER size = {};
    DWORD error = 0, count = 0;
    if (!GetFileSizeEx(file, &size)) error = GetLastError();
    else if (size.QuadPart < 44 || size.QuadPart > static_cast<LONGLONG>(limit)) error = ERROR_INVALID_DATA;
    else {
        bytes.resize(static_cast<size_t>(size.QuadPart));
        if (!ReadFile(file, bytes.data(), static_cast<DWORD>(bytes.size()), &count, nullptr)) error = GetLastError();
        else if (count != bytes.size()) error = ERROR_READ_FAULT;
    }
    CloseHandle(file);
    return error;
}

// Write and reread a temporary before atomic replacement; retain a .bak on overwrite.
DWORD write(const std::wstring& path, const Bytes& bytes, long long id)
{
    std::wstring temp = path + L".pending." + std::to_wstring(GetCurrentProcessId()) + L"." + std::to_wstring(id);
    HANDLE file = CreateFileW(temp.c_str(), GENERIC_READ | GENERIC_WRITE, 0, nullptr,
        CREATE_NEW, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (file == INVALID_HANDLE_VALUE) return GetLastError();
    DWORD error = 0, count = 0;
    if (!WriteFile(file, bytes.data(), static_cast<DWORD>(bytes.size()), &count, nullptr) ||
        !FlushFileBuffers(file)) error = GetLastError();
    else if (count != bytes.size()) error = ERROR_WRITE_FAULT;
    if (!error) {
        Bytes verify(bytes.size());
        SetFilePointer(file, 0, nullptr, FILE_BEGIN);
        if (!ReadFile(file, verify.data(), static_cast<DWORD>(verify.size()), &count, nullptr)) error = GetLastError();
        else if (count != bytes.size() || verify != bytes) error = ERROR_CRC;
    }
    CloseHandle(file);
    if (!error) {
        if (GetFileAttributesW(path.c_str()) != INVALID_FILE_ATTRIBUTES) {
            const auto backup = path + L".bak";
            if (!ReplaceFileW(path.c_str(), temp.c_str(), backup.c_str(), 0, nullptr, nullptr)) error = GetLastError();
        } else if (!MoveFileExW(temp.c_str(), path.c_str(), MOVEFILE_WRITE_THROUGH)) error = GetLastError();
    }
    if (error) DeleteFileW(temp.c_str());
    return error;
}

DWORD decode(const Bytes& bytes, std::vector<Record>& records)
{
    if (bytes.size() < 44 || memcmp(bytes.data(), "SMD4", 4)) return ERROR_INVALID_DATA;
    size_t offset = 44ULL + number(bytes.data() + 8);
    if (offset > bytes.size() || !envelope(bytes.data(), offset)) return ERROR_CRC;
    records.push_back({"", Bytes(bytes.begin(), bytes.begin() + offset)});
    if (offset == bytes.size()) return 0; // Earlier single-record files remain readable.
    if (bytes.size() - offset < 40 || memcmp(bytes.data() + offset, "SMB1", 4)) return ERROR_INVALID_DATA;
    unsigned char digest[32];
    smile_sha_bytes(bytes.data(), bytes.size() - 32, digest);
    if (memcmp(digest, bytes.data() + bytes.size() - 32, 32)) return ERROR_CRC;
    uint32_t count = number(bytes.data() + offset + 4);
    if (count > 2048) return ERROR_INVALID_DATA;
    offset += 8;
    const size_t end = bytes.size() - 32;
    std::set<std::string> names;
    for (uint32_t index = 0; index < count; ++index) {
        if (end - offset < 8) return ERROR_INVALID_DATA;
        uint32_t nameSize = number(bytes.data() + offset), dataSize = number(bytes.data() + offset + 4);
        offset += 8;
        if (nameSize > 128 || dataSize > RecordLimit || nameSize + static_cast<size_t>(dataSize) > end - offset)
            return ERROR_INVALID_DATA;
        std::string name(reinterpret_cast<const char*>(bytes.data() + offset), nameSize);
        offset += nameSize;
        if (!suffixValid(name) || !names.insert(name).second || !envelope(bytes.data() + offset, dataSize))
            return ERROR_INVALID_DATA;
        records.push_back({name, Bytes(bytes.begin() + offset, bytes.begin() + offset + dataSize)});
        offset += dataSize;
    }
    return offset == end ? 0 : ERROR_INVALID_DATA;
}
}

// Import uses a caller-owned fresh staging key. The main record is the commit point.
// Companions cannot target arbitrary keys or paths: only checked relative suffixes.
DWORD smile_data_bundle_transfer(bool saving, const char* key, const char* manifest,
    const WCHAR* selected, long long id, volatile LONG* progress)
{
    try {
        Bytes bytes;
        DWORD error = read(saving ? stored(key) : selected, bytes, BundleLimit);
        if (error) return error;
        if (saving) {
            if (!envelope(bytes.data(), bytes.size())) return ERROR_CRC;
            std::vector<std::string> suffixes;
            std::set<std::string> names;
            std::string list(manifest);
            size_t start = 0;
            while (start < list.size()) {
                size_t end = list.find('|', start);
                if (end == std::string::npos) end = list.size();
                auto suffix = list.substr(start, end - start);
                if (!suffixValid(suffix) || !names.insert(suffix).second || names.size() > 2048)
                    return ERROR_INVALID_DATA;
                suffixes.push_back(suffix);
                start = end + 1;
            }
            const unsigned char marker[] = {'S','M','B','1'};
            bytes.insert(bytes.end(), marker, marker + 4);
            append(bytes, static_cast<uint32_t>(suffixes.size()));
            for (size_t index = 0; index < suffixes.size(); ++index) {
                Bytes part;
                error = read(stored(std::string(key) + suffixes[index]), part, RecordLimit);
                if (error) return error;
                if (!envelope(part.data(), part.size())) return ERROR_CRC;
                if (bytes.size() + 8 + suffixes[index].size() + part.size() + 32 > BundleLimit)
                    return ERROR_FILE_TOO_LARGE;
                append(bytes, static_cast<uint32_t>(suffixes[index].size()));
                append(bytes, static_cast<uint32_t>(part.size()));
                bytes.insert(bytes.end(), suffixes[index].begin(), suffixes[index].end());
                bytes.insert(bytes.end(), part.begin(), part.end());
                InterlockedExchange(progress, static_cast<LONG>((index + 1) * 70 / suffixes.size()));
            }
            unsigned char digest[32];
            smile_sha_bytes(bytes.data(), bytes.size(), digest);
            bytes.insert(bytes.end(), digest, digest + 32);
            return write(selected, bytes, id);
        }
        std::vector<Record> records;
        error = decode(bytes, records);
        if (error) return error; // Validate the whole bundle before writing any record.
        for (size_t index = 1; index < records.size(); ++index) {
            error = write(stored(std::string(key) + records[index].suffix), records[index].bytes, id);
            if (error) return error;
            InterlockedExchange(progress, static_cast<LONG>(index * 95 / records.size()));
        }
        return write(stored(key), records[0].bytes, id);
    } catch (...) { return ERROR_NOT_ENOUGH_MEMORY; }
}
