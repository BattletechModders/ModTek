#include "simdutf.cpp"
#include "simdutf.h"

// Linux:
// c++ -o libsimdutfexport.so simdutfexport.cpp -shared -std=c++17 -O3 -fPIC
//
// Windows:
// ???

extern "C" ssize_t convert_utf16le_to_utf8(char16_t* utf16, size_t utf16words, char* utf8, size_t utf8space) {
  size_t expected_utf8words = simdutf::utf8_length_from_utf16le(utf16, utf16words);
  if (expected_utf8words > utf8space) {
    return utf8space - expected_utf8words;
  }
  size_t utf8words = simdutf::convert_utf16le_to_utf8(utf16, utf16words, utf8);
  return utf8words;
}
