/* Native process-policy counterexamples; no game assets or production storage. */
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>

int main(int argc, char **argv) {
    int mode = argc > 1 ? atoi(argv[1]) : 0;
    if (mode != 2) puts("PASS Neris Town Routes");
    fflush(stdout);
    if (mode == 1) return 7;
    if (mode == 3) puts("FAIL Deliberate native failure after success");
    if (mode == 4) Sleep(INFINITE);
    return 0;
}
